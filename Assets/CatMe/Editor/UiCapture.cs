#if UNITY_EDITOR
using System;
using System.IO;
using CatMe.Garden;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Editor
{
    /// <summary>Batch tool: renders each Phase 6 screen to a PNG through a camera-space canvas. Overlay canvases cannot be rendered by Camera.Render().</summary>
    public static class UiCapture
    {
        public static void Run()
        {
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                string outDir = Environment.GetEnvironmentVariable("CATME_RENDER_OUT") ?? "/private/tmp/catme-renders";
                Directory.CreateDirectory(outDir);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camGo = new GameObject("UiCam", typeof(Camera)); var cam = camGo.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.36f, .52f, .32f); cam.orthographic = true; cam.orthographicSize = 5f; cam.transform.position = new Vector3(0, 0, -10);
                foreach (string name in UiPreview.Names)
                {
                    var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 5f;
                    var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1f;
                    UiPreview.Show(canvasGo.transform, name);
                    Canvas.ForceUpdateCanvases();
                    var rt = new RenderTexture(1920, 1080, 24); cam.targetTexture = rt; cam.Render();
                    RenderTexture.active = rt; var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
                    File.WriteAllBytes(Path.Combine(outDir, "ui-" + name + ".png"), tex.EncodeToPNG());
                    RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(canvasGo);
                }
                EditorApplication.Exit(0);
            }
            catch (Exception e) { File.WriteAllText("/tmp/catme-capture-error.txt", e.ToString()); EditorApplication.Exit(1); }
        }
    }
}
#endif
