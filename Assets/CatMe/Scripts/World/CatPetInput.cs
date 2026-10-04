using System.Collections.Generic;
using CatMe.Garden;
using CatMe.Session;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatMe.World
{
    /// <summary>
    /// Direct cat interaction. Tap the cat: it comes to the owner, meows, a heart pops. Press and hold on the cat: it stays still, the camera lowers to it,
    /// purring plays with soft haptic pulses and hearts drift up; release to let it go. Works outdoors and inside the rooms.
    /// </summary>
    public sealed class CatPetInput : MonoBehaviour
    {
        const float HoldSeconds = .45f, TapSeconds = .3f, HitPixels = 150f, MovePixels = 36f;
        GardenCompanionDemo garden; Transform canvas; Camera view;
        bool pressed, petting; float pressedAt, nextHeartAt, nextPulseAt; Vector2 pressPos;
        AudioSource purr;   // the petting loop: a cat purrs, a dog pants happily (PetSounds)
        CameraProfile petProfile = new CameraProfile { name = "Pet", distance = 1.5f, height = .85f, pitch = 14f, fov = 46f, blendIn = 1.0f, zoomMax = 2.4f };   // petting: only the zoom range tightens
        Sprite heart; readonly List<(RectTransform rt, Image img, float t)> hearts = new List<(RectTransform, Image, float)>();

        public void Initialize(GardenCompanionDemo demo, Transform canvasRoot)
        {
            garden = demo; canvas = canvasRoot; view = Camera.main;
            purr = gameObject.AddComponent<AudioSource>(); purr.loop = true; purr.volume = 0f; purr.spatialBlend = 0f;
        }

        void Update()
        {
            AnimateHearts();
            if (garden == null || garden.CatTransform == null || garden.IsInputBlocked && !petting) { if (petting) EndPet(); return; }
            if (view == null) view = Camera.main;
            bool down = false, up = false, held = false; Vector2 pos = default;
            var touch = Touchscreen.current; var mouse = Mouse.current;
            if (touch != null && touch.primaryTouch.press.isPressed) { held = true; pos = touch.primaryTouch.position.ReadValue(); down = touch.primaryTouch.press.wasPressedThisFrame; }
            else if (mouse != null && mouse.leftButton.isPressed) { held = true; pos = mouse.position.ReadValue(); down = mouse.leftButton.wasPressedThisFrame; }
            if (touch != null && touch.primaryTouch.press.wasReleasedThisFrame || mouse != null && mouse.leftButton.wasReleasedThisFrame) up = true;
            if (purr != null) purr.volume = Mathf.MoveTowards(purr.volume, petting && PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 1 ? .5f : 0f, Time.unscaledDeltaTime * 1.5f);

            if (down && !pressed && NearCat(pos) && !CatMe.Garden.MobileGardenJoystick.OverUiExceptStick(pos)) { pressed = true; pressedAt = Time.unscaledTime; pressPos = pos; }   // the stick's invisible zone doesn't block the pet
            if (pressed && held && !petting && Time.unscaledTime - pressedAt >= HoldSeconds && (pos - pressPos).magnitude < MovePixels && CatNear()) StartPet();
            if (pressed && held && !petting && (pos - pressPos).magnitude > MovePixels) pressed = false; // it was a drag, not a pet
            if (petting && held && NearCat(pos, 1.6f)) { Pulse(); }
            if (up)
            {
                if (petting) EndPet();
                else if (pressed && Time.unscaledTime - pressedAt <= TapSeconds) Tap();
                pressed = false;
            }
            if (petting && !held) EndPet();
        }

        Pets.Species Species => garden.Behavior != null ? garden.Behavior.Species : Pets.PetRoster.Active.Kind;

        bool CatNear() => Vector3.Distance(garden.CatPosition, garden.OwnerPosition) < 4f;

        bool NearCat(Vector2 screen, float scale = 1f)
        {
            if (view == null) return false;
            Vector3 sp = view.WorldToScreenPoint(garden.CatPosition + Vector3.up * .22f);
            return sp.z > 0f && ((Vector2)sp - screen).magnitude < HitPixels * scale * Mathf.Clamp(Screen.height / 1080f, .7f, 1.6f);
        }

        static bool OverUi(int id) { var es = EventSystem.current; return es != null && (id >= 0 ? es.IsPointerOverGameObject(id) : es.IsPointerOverGameObject()); }

        void Tap()
        {
            Haptics.Tap();
            garden.CallCat();                         // it comes to the owner and answers
            Pets.PetSounds.PlayAt(garden.CatPosition, Species, Pets.PetCue.Chirp, .6f);   // a tap gets a chirp, not a meow (PET_AUDIO section 5)
            SpawnHeart();
        }

        void StartPet()
        {
            petting = true; nextHeartAt = nextPulseAt = 0f;
            garden.SetCatStill(true);
            garden.CameraRig?.PushProfile(petProfile);
            var clip = Pets.PetSounds.Clip(Species, Pets.PetCue.Happy);
            if (purr != null && clip != null) { if (purr.clip != clip) purr.clip = clip; if (!purr.isPlaying) purr.Play(); }
            garden.Voice?.Say(Pets.Moment.Petted, 2.2f);
            Haptics.Soft();
        }

        void EndPet()
        {
            petting = false; garden.SetCatStill(false);
            garden.CameraRig?.PopProfile(petProfile);
        }

        void Pulse()
        {
            // Cat turns toward its owner and enjoys it.
            Vector3 look = garden.OwnerPosition - garden.CatPosition; look.y = 0f;
            if (look.sqrMagnitude > .01f) garden.CatTransform.rotation = Quaternion.Slerp(garden.CatTransform.rotation, Quaternion.LookRotation(look), 3f * Time.deltaTime);
            if (Time.unscaledTime >= nextPulseAt) { nextPulseAt = Time.unscaledTime + .7f; Haptics.Soft(); }
            if (Time.unscaledTime >= nextHeartAt) { nextHeartAt = Time.unscaledTime + .55f; SpawnHeart(); }
        }

        // ---- floating hearts (procedural sprite, no asset needed) ----------------------------------------------------

        /// <summary>A few hearts float up from the pet (used by other moments, such as eating or admiring the photo frame).</summary>
        public void PopHearts(int count) { for (int i = 0; i < count; i++) SpawnHeart(); }

        void SpawnHeart()
        {
            if (canvas == null || view == null) return;
            if (heart == null) heart = MakeHeart();
            var go = new GameObject("Heart", typeof(RectTransform), typeof(Image)); go.transform.SetParent(canvas, false);
            var img = go.GetComponent<Image>(); img.sprite = heart; img.color = new Color(1f, .45f, .55f, 1f); img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(44f, 44f);
            Vector3 sp = view.WorldToScreenPoint(garden.CatPosition + Vector3.up * .6f + new Vector3(Random.Range(-.1f, .1f), 0, 0));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas as RectTransform, sp, null, out Vector2 local);
            rt.anchoredPosition = local + new Vector2(Random.Range(-18f, 18f), 0f);
            hearts.Add((rt, img, 0f));
        }

        void AnimateHearts()
        {
            for (int i = hearts.Count - 1; i >= 0; i--)
            {
                var h = hearts[i]; float t = h.t + Time.unscaledDeltaTime / 1.3f;
                if (h.rt == null) { hearts.RemoveAt(i); continue; }
                h.rt.anchoredPosition += new Vector2(0f, 90f * Time.unscaledDeltaTime);
                var c = h.img.color; c.a = 1f - t; h.img.color = c; h.rt.localScale = Vector3.one * Mathf.Lerp(.6f, 1.1f, Mathf.Min(1f, t * 3f));
                if (t >= 1f) { Destroy(h.rt.gameObject); hearts.RemoveAt(i); } else hearts[i] = (h.rt, h.img, t);
            }
        }

        static Sprite MakeHeart()
        {
            const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false); var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float fx = (x / (float)(n - 1) - .5f) * 2.6f, fy = (y / (float)(n - 1) - .42f) * 2.6f;
                float v = Mathf.Pow(fx * fx + fy * fy - 1f, 3f) - fx * fx * fy * fy * fy;
                px[y * n + x] = v <= 0f ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(px); tex.Apply(); tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f);
        }
    }
}
