using CatMe.CameraSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CatMe.Toys
{
    /// <summary>Lets a player tap the mouse toy on the rug to start the existing ball chase.</summary>
    [DefaultExecutionOrder(-105)]
    [DisallowMultipleComponent]
    public sealed class CatFloorToyInput : MonoBehaviour
    {
        [SerializeField] private float rayDistance = 100f;

        private Camera inputCamera;
        private CatBallInteraction ballInteraction;
        private RoomOrbitCamera roomCamera;
        private int capturedTouchId = -1;
        private bool capturedMouse;
        private readonly System.Collections.Generic.List<Material> runtimeMaterials = new System.Collections.Generic.List<Material>();

        private void Awake()
        {
            RestyleAsToyMouse();
        }

        /// <summary>Replace the imported gray cat silhouette with a small, clear toy mouse.</summary>
        private void RestyleAsToyMouse()
        {
            Renderer[] existingRenderers = GetComponentsInChildren<Renderer>(true);
            if (existingRenderers.Length == 0) return;

            Bounds sourceBounds = existingRenderers[0].bounds;
            Material sourceMaterial = null;
            for (int i = 0; i < existingRenderers.Length; i++)
            {
                sourceBounds.Encapsulate(existingRenderers[i].bounds);
                if (sourceMaterial == null && existingRenderers[i].sharedMaterial != null)
                    sourceMaterial = existingRenderers[i].sharedMaterial;
                existingRenderers[i].enabled = false;
            }

            Vector3 scale = transform.lossyScale;
            Vector3 safeScale = new Vector3(Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                Mathf.Max(0.001f, Mathf.Abs(scale.y)), Mathf.Max(0.001f, Mathf.Abs(scale.z)));
            Vector3 floorPoint = new Vector3(transform.position.x, sourceBounds.min.y, transform.position.z);
            Vector3 localFloor = transform.InverseTransformPoint(floorPoint);
            Material bodyMaterial = MakeTintedMaterial(sourceMaterial, new Color(0.78f, 0.57f, 0.37f));
            Material earMaterial = MakeTintedMaterial(sourceMaterial, new Color(0.88f, 0.55f, 0.58f));
            Material noseMaterial = MakeTintedMaterial(sourceMaterial, new Color(0.24f, 0.16f, 0.17f));

            CreateToyPart("MouseBody", PrimitiveType.Sphere, localFloor + Vector3.up * (0.036f / safeScale.y),
                new Vector3(0.14f, 0.052f, 0.075f), bodyMaterial, safeScale);
            CreateToyPart("MouseHead", PrimitiveType.Sphere, localFloor + new Vector3(0f, 0.043f / safeScale.y, 0.045f / safeScale.z),
                new Vector3(0.064f, 0.053f, 0.050f), bodyMaterial, safeScale);
            CreateToyPart("MouseLeftEar", PrimitiveType.Sphere, localFloor + new Vector3(-0.027f / safeScale.x, 0.075f / safeScale.y, 0.055f / safeScale.z),
                new Vector3(0.036f, 0.034f, 0.018f), earMaterial, safeScale);
            CreateToyPart("MouseRightEar", PrimitiveType.Sphere, localFloor + new Vector3(0.027f / safeScale.x, 0.075f / safeScale.y, 0.055f / safeScale.z),
                new Vector3(0.036f, 0.034f, 0.018f), earMaterial, safeScale);
            CreateToyPart("MouseNose", PrimitiveType.Sphere, localFloor + new Vector3(0f, 0.043f / safeScale.y, 0.073f / safeScale.z),
                new Vector3(0.018f, 0.015f, 0.014f), noseMaterial, safeScale);

            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tail.name = "MouseTail";
            tail.transform.SetParent(transform, false);
            tail.transform.localPosition = localFloor + new Vector3(0f, 0.019f / safeScale.y, -0.064f / safeScale.z);
            tail.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tail.transform.localScale = new Vector3(0.009f / safeScale.x, 0.048f / safeScale.y, 0.009f / safeScale.z);
            Collider tailCollider = tail.GetComponent<Collider>();
            if (tailCollider != null) Destroy(tailCollider);
            tail.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
            tail.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private Material MakeTintedMaterial(Material source, Color tint)
        {
            if (source == null) return null;
            Material material = new Material(source);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            runtimeMaterials.Add(material);
            return material;
        }

        private void CreateToyPart(string partName, PrimitiveType type, Vector3 localPosition,
            Vector3 worldSize, Material material, Vector3 lossyScale)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = new Vector3(worldSize.x / Mathf.Max(0.001f, Mathf.Abs(lossyScale.x)),
                worldSize.y / Mathf.Max(0.001f, Mathf.Abs(lossyScale.y)),
                worldSize.z / Mathf.Max(0.001f, Mathf.Abs(lossyScale.z)));
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = part.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Update()
        {
            if (inputCamera == null) inputCamera = Camera.main;
            if (ballInteraction == null) ballInteraction = FindAnyObjectByType<CatBallInteraction>();
            if (roomCamera == null) roomCamera = FindAnyObjectByType<RoomOrbitCamera>();
            if (inputCamera == null || ballInteraction == null) return;

            ProcessTouch();
            ProcessMouse();
        }

        private void ProcessTouch()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null) return;

            foreach (TouchControl touch in screen.touches)
            {
                int id = touch.touchId.ReadValue();
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                if (capturedTouchId == id &&
                    (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                     phase == UnityEngine.InputSystem.TouchPhase.Canceled || !touch.press.isPressed))
                {
                    roomCamera?.ReleaseTouchPointer(id);
                    capturedTouchId = -1;
                    continue;
                }

                if (capturedTouchId < 0 && phase == UnityEngine.InputSystem.TouchPhase.Began &&
                    !IsPointerOverUi(id) && TryActivate(touch.position.ReadValue(), id))
                {
                    capturedTouchId = id;
                    roomCamera?.ReserveTouchPointer(id);
                }
            }
        }

        private void ProcessMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || capturedTouchId >= 0) return;

            if (capturedMouse && mouse.leftButton.wasReleasedThisFrame)
            {
                roomCamera?.ReleaseMousePointer();
                capturedMouse = false;
            }

            if (!capturedMouse && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi(-1) &&
                TryActivate(mouse.position.ReadValue(), -1))
            {
                capturedMouse = true;
                roomCamera?.ReserveMousePointer();
            }
        }

        private bool TryActivate(Vector2 screenPosition, int pointerId)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId)) return false;
            Ray ray = inputCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform)) return false;
            return ballInteraction.TryPlayWithFloorToy(transform);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeMaterials.Count; i++)
                if (runtimeMaterials[i] != null) Destroy(runtimeMaterials[i]);
            runtimeMaterials.Clear();
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);
        }
    }
}
