using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CatMe.Garden
{
    /// <summary>Direct Tripo and Meshy generation for the single-user local prototype.</summary>
    internal static class CompanionProviderApi
    {
        private const string TripoBase = "https://openapi.tripo3d.ai/v3";
        private const string MeshyBase = "https://api.meshy.ai/openapi/v1";
        private const int PollDelayMs = 3000;
        private const int MaxPolls = 600;

        [Serializable] private sealed class Keys { public string tripo; public string meshy; }
        [Serializable] private sealed class TripoEnvelope { public int code; public string message; public string suggestion; public string request_id; public TripoData data; }
        [Serializable] private sealed class MeshyHttpError { public string message; public string detail; public string suggestion; public string code; }
        [Serializable] private sealed class TripoData
        {
            public string task_id, file_token, status, error_message;
            public int progress;
            public TripoOutput output;
        }
        [Serializable] private sealed class TripoOutput { public string model_url, rig_type; public bool riggable; }
        [Serializable] private sealed class MeshyEnvelope { public string result; }
        [Serializable] private sealed class MeshyImageStatus
        {
            public string status;
            public int progress;
            public string[] image_urls;
            public MeshyTaskError task_error;
        }
        [Serializable] private sealed class MeshyModelStatus
        {
            public string status;
            public int progress;
            public MeshyModelUrls model_urls;
            public MeshyTaskError task_error;
        }
        [Serializable] private sealed class MeshyModelUrls { public string glb; }
        [Serializable] private sealed class MeshyRigStatus
        {
            public string status;
            public int progress;
            public MeshyRigResult result;
            public MeshyTaskError task_error;
        }
        [Serializable] private sealed class MeshyRigResult
        {
            public string rigged_character_glb_url;
            public MeshyBasicAnimations basic_animations;
        }
        [Serializable] private sealed class MeshyBasicAnimations { public string walking_glb_url; }
        [Serializable] private sealed class MeshyTaskError { public string message; }
        [Serializable] private sealed class MeshyReferenceRequest
        {
            public string ai_model = "gpt-image-2";
            public string prompt;
            public string[] reference_image_urls;
            public bool generate_multi_view;
            public string aspect_ratio = "3:4";
        }
        [Serializable] private sealed class MeshyModelRequest
        {
            public string input_task_id;
            public string model_type = "standard";
            public string ai_model = "meshy-7.1";
            public bool ultra_mode;
            public bool should_texture = true;
            public bool enable_pbr = true;
            public bool should_remesh = true;
            public bool save_pre_remeshed_model;
            public int target_polycount = 100000;
            public string topology = "triangle";
            public string texture_resolution = "4k";
            public string[] target_formats = { "glb" };
            public bool image_enhancement = true;
            public string pose_mode = "a-pose";
            public bool moderation = true;
        }
        [Serializable] private sealed class MeshyRigRequest
        {
            public string input_task_id;
            public float height_meters = 1.7f;
            public bool generate_basic_animations = true;
        }
        [Serializable] private sealed class TripoInputRequest { public string input; public TripoInputRequest(string value) { input = value; } }
        [Serializable] private sealed class TripoModelRequest
        {
            public string input;
            public string model = "v3.1-20260211";
            public bool texture = true;
            public bool pbr = true;
            public string texture_quality = "standard";
            public int face_limit = 30000;
            public TripoModelRequest(string value) { input = value; }
        }
        [Serializable] private sealed class TripoRigRequest
        {
            public string input;
            public string model = "v2.5-20260210";
            public string rig_type = "quadruped";
            public string spec = "tripo";
            public string out_format = "glb";
            public TripoRigRequest(string value) { input = value; }
        }
        [Serializable] private sealed class TripoWalkRequest
        {
            public string input;
            public string animation = "preset:quadruped:walk";
            public string out_format = "glb";
            public bool bake_animation = true;
            public bool export_with_geometry = true;
            public bool animate_in_place = true;
            public TripoWalkRequest(string value) { input = value; }
        }

        private static Keys keys;
        private const string HumanPrompt = @"TASK

Create one single, photorealistic, full-body reference image of the exact same person in the uploaded photo. This image will be converted to a textured 3D humanoid and automatically rigged for a walking animation.

PRESERVE THE PERSON
Keep the person's recognizable identity and appearance: face shape and facial features, skin tone and visible skin details, apparent age, body build and proportions, hair colour and style, facial hair, glasses, and the same outfit and footwear. Do not beautify, slim, stylize, change age, change clothing, or replace them with a generic person. If the photo crops or hides body parts, continue them with conservative, anatomically plausible proportions consistent with the visible person. Keep the face clear and recognizable.

POSE FOR HUMANOID RIGGING
Show the whole person from the top of the head to the soles of both feet, with comfortable space around the silhouette. Stand upright, facing directly toward the camera in a relaxed, symmetrical A-pose. Keep the torso straight, shoulders level, feet about hip-width apart and flat on the ground. Hold both arms slightly down and away from the torso, with a visible gap between each upper arm and the body; keep the elbows gently extended, wrists straight, hands visible beside the hips and fingers naturally together. Keep both legs straight, separate and fully visible. Make the shoulders, elbows, wrists, hips, knees and ankles easy to locate. Use normal human anatomy and balanced weight. Do not cross limbs, hide hands, overlap arms with the torso, or let clothing obscure the joints.

CAMERA AND BACKGROUND
Use one front-facing, eye-level, near-orthographic full-body view. Keep the person centered, facing forward without twisting or turning. Use a plain, uniform, neutral mid-grey studio background and soft, even, neutral lighting that shows the silhouette and clothing clearly without strong cast shadows.

OUTPUT RESTRICTIONS
Return exactly one person in exactly one image. No collage, contact sheet, multiple views, text, labels, watermark, border, props, furniture, other people, cropped limbs, extra or fused limbs, dramatic action pose, sitting, walking, running, bulky accessories, flowing cape, or loose garments covering the arm or leg joints. Optimize the clear standard biped silhouette for automatic humanoid pose estimation, skeleton placement and skin weighting.";

        public static bool IsConfigured(bool human, out string reason)
        {
            Keys config = LoadKeys();
            string key = human ? config?.meshy : config?.tripo;
            if (!string.IsNullOrWhiteSpace(key)) { reason = null; return true; }
            reason = "This local build has no " + (human ? "Meshy" : "Tripo") + " key. Add it to the ignored CompanionProviderKeys resource, then rebuild.";
            return false;
        }

        public static Task<byte[]> CreateWalkingCat(byte[] photo, Action<string> status) => RunCat(photo, status);
        public static Task<byte[]> CreateWalkingHuman(byte[] photo, Action<string> status) => RunHuman(photo, status);

        private static async Task<byte[]> RunCat(byte[] photo, Action<string> status)
        {
            string key = LoadKeys()?.tripo;
            if (string.IsNullOrWhiteSpace(key)) throw new Exception("The local Tripo key is missing.");
            string token = await UploadTripoPhoto(photo, key);
            status?.Invoke("Tripo: creating your cat model…");
            string modelTask = await CreateTripoTask("/generation/image-to-model", JsonUtility.ToJson(new TripoModelRequest(token)), key);
            TripoData model = await PollTripo(modelTask, key, "Tripo: creating your cat model", status);
            status?.Invoke("Tripo: checking that your cat can be rigged…");
            string checkTask = await CreateTripoTask("/animations/rig-check", JsonUtility.ToJson(new TripoInputRequest(modelTask)), key);
            TripoData check = await PollTripo(checkTask, key, "Tripo: checking rig compatibility", status);
            if (check.output == null || !check.output.riggable || check.output.rig_type != "quadruped")
                throw new Exception("Tripo could not prepare a quadruped rig from this photo. Try a clear full-body cat photo.");
            status?.Invoke("Tripo: building the quadruped rig…");
            string rigTask = await CreateTripoTask("/animations/rig", JsonUtility.ToJson(new TripoRigRequest(modelTask)), key);
            await PollTripo(rigTask, key, "Tripo: rigging your cat", status);
            status?.Invoke("Tripo: adding the walking animation…");
            string walkTask = await CreateTripoTask("/animations/retarget", JsonUtility.ToJson(new TripoWalkRequest(rigTask)), key);
            TripoData walk = await PollTripo(walkTask, key, "Tripo: animating the walk", status);
            if (string.IsNullOrEmpty(walk.output?.model_url)) throw new Exception("Tripo finished without a walking model.");
            status?.Invoke("Downloading your Tripo walking cat…");
            return await Download(walk.output.model_url, null, "Tripo");
        }

        private static async Task<byte[]> RunHuman(byte[] photo, Action<string> status)
        {
            string key = LoadKeys()?.meshy;
            if (string.IsNullOrWhiteSpace(key)) throw new Exception("The local Meshy key is missing.");
            status?.Invoke("Meshy: preparing a rig-friendly full-body reference…");
            string dataUrl = "data:image/jpeg;base64," + Convert.ToBase64String(photo);
            var reference = new MeshyReferenceRequest { prompt = HumanPrompt, reference_image_urls = new[] { dataUrl } };
            string referenceTask = await CreateMeshyTask("/image-to-image", JsonUtility.ToJson(reference), key);
            MeshyImageStatus image = await PollMeshy<MeshyImageStatus>("/image-to-image/" + Uri.EscapeDataString(referenceTask), key, "Meshy: preparing your person reference", status);
            if (image.image_urls == null || image.image_urls.Length == 0) throw new Exception("Meshy did not return the full-body person reference.");
            status?.Invoke("Meshy: creating your 3D person…");
            var modelRequest = new MeshyModelRequest { input_task_id = referenceTask };
            string modelTask = await CreateMeshyTask("/image-to-3d", JsonUtility.ToJson(modelRequest), key);
            MeshyModelStatus model = await PollMeshy<MeshyModelStatus>("/image-to-3d/" + Uri.EscapeDataString(modelTask), key, "Meshy: creating the 3D person", status);
            if (model.model_urls == null || string.IsNullOrEmpty(model.model_urls.glb)) throw new Exception("Meshy finished without a 3D person model.");
            status?.Invoke("Meshy: fitting the humanoid rig and walking animation…");
            string rigTask = await CreateMeshyTask("/rigging", JsonUtility.ToJson(new MeshyRigRequest { input_task_id = modelTask }), key);
            MeshyRigStatus rig = await PollMeshy<MeshyRigStatus>("/rigging/" + Uri.EscapeDataString(rigTask), key, "Meshy: rigging and animating your person", status);
            string walkingUrl = rig.result?.basic_animations?.walking_glb_url;
            if (string.IsNullOrEmpty(walkingUrl)) throw new Exception("Meshy finished without a walking humanoid GLB.");
            status?.Invoke("Downloading your Meshy walking person…");
            return await Download(walkingUrl, null, "Meshy");
        }

        private static async Task<string> UploadTripoPhoto(byte[] bytes, string key)
        {
            string boundary = "----CatMe" + Guid.NewGuid().ToString("N");
            byte[] prefix = Encoding.UTF8.GetBytes("--" + boundary + "\r\nContent-Disposition: form-data; name=\"file\"; filename=\"cat-photo.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n");
            byte[] suffix = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
            byte[] body = new byte[prefix.Length + bytes.Length + suffix.Length];
            Buffer.BlockCopy(prefix, 0, body, 0, prefix.Length); Buffer.BlockCopy(bytes, 0, body, prefix.Length, bytes.Length); Buffer.BlockCopy(suffix, 0, body, prefix.Length + bytes.Length, suffix.Length);
            string json = await Send(TripoBase + "/files", "POST", key, body, "multipart/form-data; boundary=" + boundary, 120);
            TripoData result = ParseTripo(json);
            if (string.IsNullOrEmpty(result.file_token)) throw new Exception("Tripo did not return an image upload token.");
            return result.file_token;
        }

        private static async Task<string> CreateTripoTask(string path, string body, string key)
        {
            TripoData result = ParseTripo(await Send(TripoBase + path, "POST", key, Encoding.UTF8.GetBytes(body), "application/json", 120));
            if (string.IsNullOrEmpty(result.task_id)) throw new Exception("Tripo did not return a task ID.");
            return result.task_id;
        }

        private static async Task<TripoData> PollTripo(string id, string key, string label, Action<string> status)
        {
            for (int i = 0; i < MaxPolls; i++)
            {
                await Task.Delay(PollDelayMs);
                TripoData task = ParseTripo(await Send(TripoBase + "/tasks/" + Uri.EscapeDataString(id), "GET", key));
                string state = (task.status ?? "").ToLowerInvariant();
                if (state == "success" || state == "succeeded") return task;
                if (state == "failed" || state == "cancelled" || state == "canceled") throw new Exception(string.IsNullOrEmpty(task.error_message) ? "Tripo could not finish this task." : task.error_message);
                int progress = Mathf.Clamp(task.progress, 0, 100);
                status?.Invoke(label + "… " + progress + "%");
            }
            throw new Exception("Tripo generation timed out. You can try again later.");
        }

        private static async Task<string> CreateMeshyTask(string path, string body, string key)
        {
            MeshyEnvelope result = JsonUtility.FromJson<MeshyEnvelope>(await Send(MeshyBase + path, "POST", key, Encoding.UTF8.GetBytes(body), "application/json", 120));
            if (result == null || string.IsNullOrEmpty(result.result)) throw new Exception("Meshy did not return a task ID.");
            return result.result;
        }

        private static async Task<T> PollMeshy<T>(string path, string key, string label, Action<string> status) where T : class
        {
            for (int i = 0; i < MaxPolls; i++)
            {
                await Task.Delay(PollDelayMs);
                string json = await Send(MeshyBase + path, "GET", key);
                T value = JsonUtility.FromJson<T>(json);
                string state = ReadStatus(json);
                int progress = ReadProgress(json);
                if (state == "SUCCEEDED") return value;
                if (state == "FAILED" || state == "CANCELED" || state == "CANCELLED")
                    throw new Exception(ExplainMeshyTaskError(ReadTaskError(json)));
                status?.Invoke(label + "… " + progress + "%");
            }
            throw new Exception("Meshy generation timed out. You can try again later.");
        }

        private static async Task<byte[]> Download(string url, string key, string provider)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 240;
                if (!string.IsNullOrEmpty(key)) request.SetRequestHeader("Authorization", "Bearer " + key);
                await Wait(request);
                if (request.result != UnityWebRequest.Result.Success) throw new Exception(provider + " model download failed (HTTP " + request.responseCode + ").");
                byte[] bytes = request.downloadHandler.data;
                if (bytes == null || bytes.Length < 20 || bytes[0] != 0x67 || bytes[1] != 0x6c || bytes[2] != 0x54 || bytes[3] != 0x46 || BitConverter.ToUInt32(bytes, 8) != (uint)bytes.Length)
                    throw new Exception(provider + " returned a model that is not a valid GLB.");
                return bytes;
            }
        }

        private static async Task<string> Send(string url, string method, string key, byte[] body = null, string contentType = null, int timeout = 90)
        {
            using (var request = new UnityWebRequest(url, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = timeout;
                request.SetRequestHeader("Authorization", "Bearer " + key);
                if (body != null) { request.uploadHandler = new UploadHandlerRaw(body); request.uploadHandler.contentType = contentType ?? "application/json"; }
                await Wait(request);
                if (request.result != UnityWebRequest.Result.Success)
                    throw new Exception(FormatHttpError(url, request.responseCode, request.downloadHandler?.text));
                return request.downloadHandler.text;
            }
        }

        private static string FormatHttpError(string url, long status, string body)
        {
            bool tripo = url.StartsWith(TripoBase, StringComparison.OrdinalIgnoreCase);
            string provider = tripo ? "Tripo" : "Meshy";
            string guidance = status == 401
                ? "The API key was rejected. Check that this app build has the current key."
                : status == 402
                    ? "The provider reports insufficient API credits."
                    : status == 403 && tripo
                        ? "Tripo denied this operation. Check the key's permissions and available credits."
                        : status == 403
                            ? "Meshy denied this operation. Check the API key and account access."
                            : status == 429
                                ? "The provider rate limit was reached. Wait before trying again."
                                : "Check the provider account and network, then try again.";

            string detail = null;
            string requestId = null;
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    if (tripo)
                    {
                        TripoEnvelope error = JsonUtility.FromJson<TripoEnvelope>(body);
                        detail = error == null ? null : JoinDetails(error.message, error.suggestion);
                        requestId = error?.request_id;
                    }
                    else
                    {
                        MeshyHttpError error = JsonUtility.FromJson<MeshyHttpError>(body);
                        detail = error == null ? null : JoinDetails(error.message, error.detail, error.suggestion);
                    }
                }
                catch { }
            }

            string message = provider + " request failed (HTTP " + status + "). " + guidance;
            if (!string.IsNullOrWhiteSpace(detail)) message += " Provider: " + SanitizeDetail(detail, tripo ? LoadKeys()?.tripo : LoadKeys()?.meshy);
            if (!string.IsNullOrWhiteSpace(requestId)) message += " Ref: " + requestId;
            return message;
        }

        private static string ExplainMeshyTaskError(string detail)
        {
            if (!string.IsNullOrWhiteSpace(detail) && detail.IndexOf("could not process this content", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Meshy rejected this photo or prompt. Try a standing, full-body photo in everyday, fully covering clothes with a plain background.";
            return string.IsNullOrWhiteSpace(detail) ? "Meshy could not finish this task. Try a clear standing full-body photo." : "Meshy: " + detail;
        }

        private static string JoinDetails(params string[] values)
        {
            var parts = new List<string>();
            foreach (string value in values) if (!string.IsNullOrWhiteSpace(value)) parts.Add(value.Trim());
            return string.Join(" ", parts);
        }

        private static string SanitizeDetail(string value, string key)
        {
            string detail = value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (!string.IsNullOrEmpty(key)) detail = detail.Replace(key, "[redacted]");
            return detail.Length > 180 ? detail.Substring(0, 177) + "..." : detail;
        }

        private static async Task Wait(UnityWebRequest request)
        {
            var operation = request.SendWebRequest();
            if (!operation.isDone)
            {
                var completion = new TaskCompletionSource<bool>();
                operation.completed += _ => completion.TrySetResult(true);
                if (!operation.isDone) await completion.Task;
            }
        }

        private static TripoData ParseTripo(string json)
        {
            TripoEnvelope response = JsonUtility.FromJson<TripoEnvelope>(json);
            if (response == null || response.code != 0 || response.data == null) throw new Exception("Tripo returned an invalid response.");
            return response.data;
        }
        private static string ReadStatus(string json) => JsonUtility.FromJson<StatusFields>(json)?.status?.ToUpperInvariant();
        private static int ReadProgress(string json) => JsonUtility.FromJson<StatusFields>(json)?.progress ?? 0;
        private static string ReadTaskError(string json) => JsonUtility.FromJson<MeshyTaskErrorEnvelope>(json)?.task_error?.message;
        [Serializable] private sealed class StatusFields { public string status; public int progress; }
        [Serializable] private sealed class MeshyTaskErrorEnvelope { public MeshyTaskError task_error; }

        private static Keys LoadKeys()
        {
            if (keys != null) return keys;
            TextAsset asset = Resources.Load<TextAsset>("CompanionProviderKeys");
            if (asset == null) return null;
            try { keys = JsonUtility.FromJson<Keys>(asset.text); } catch { keys = null; }
            return keys;
        }

    }
}
