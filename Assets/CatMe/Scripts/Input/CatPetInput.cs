using CatMe.CameraSystem;
using CatMe.Cat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CatMe.GameInput
{
    /// <summary>
    /// Claims a pointer that begins on the stationary cat and turns deliberate
    /// on-cat motion into CatPetReaction requests.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class CatPetInput : MonoBehaviour
    {
        [SerializeField] private float strokeThresholdPixels = 32f;
        [SerializeField] private float pointerRayDistance = 100f;

        private CatPetReaction reaction;
        private CatMotor motor;
        private RoomOrbitCamera roomCamera;
        private Camera inputCamera;
        private bool mouseCaptured;
        private int touchId = -1;
        private bool crossedThreshold;
        private float strokeDistance;
        private Vector2 previousPosition;
        private float startYaw;
        private float startPitch;
        private float startDistance;
        private float maximumCameraDelta;

        public bool IsPointerCaptured => mouseCaptured || touchId >= 0;
        public int ValidStrokeCount => reaction == null ? 0 : reaction.ValidStrokeCount;
        public int RejectedTapCount => reaction == null ? 0 : reaction.RejectedTapCount;
        public float MaximumCameraDelta => maximumCameraDelta;

        public void Initialize(CatPetReaction readyReaction, CatMotor readyMotor)
        {
            reaction = readyReaction;
            motor = readyMotor;
            roomCamera = FindFirstObjectByType<RoomOrbitCamera>();
            inputCamera = Camera.main;
        }

        private void Update()
        {
            if (reaction == null || motor == null || motor.IsMoving)
            {
                CancelCapture();
                return;
            }

            ProcessTouches();
            ProcessMouse();
            TrackCameraDelta();
        }

        private void ProcessTouches()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            int pressedCount = 0;
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (touch.press.isPressed)
                {
                    if (roomCamera == null || roomCamera.IsEligibleWorldTouch(touch.touchId.ReadValue())) pressedCount++;
                }
            }

            if (pressedCount >= 2 && touchId >= 0)
            {
                CancelCapture();
            }

            foreach (TouchControl touch in touchscreen.touches)
            {
                int id = touch.touchId.ReadValue();
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if (IsPointerOverUi(id) || touchId >= 0 || mouseCaptured)
                    {
                        continue;
                    }

                    Vector2 position = touch.position.ReadValue();
                    if (TryBeginPet(id, position))
                    {
                        previousPosition = position;
                    }
                }
                else if (touchId == id && touch.press.isPressed)
                {
                    ProcessPosition(touch.position.ReadValue());
                }

                if (touchId == id && (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled || !touch.press.isPressed))
                {
                    EndCapture();
                }
            }
        }

        private void ProcessMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || touchId >= 0)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 position = mouse.position.ReadValue();
                if (!IsPointerOverUi(-1) && TryBeginPet(-1, position))
                {
                    previousPosition = position;
                }
            }

            if (mouseCaptured && mouse.leftButton.isPressed)
            {
                ProcessPosition(mouse.position.ReadValue());
            }

            if (mouseCaptured && mouse.leftButton.wasReleasedThisFrame)
            {
                EndCapture();
            }
        }

        private bool TryBeginPet(int pointerId, Vector2 position)
        {
            if (reaction == null || motor == null || motor.IsMoving || motor.IsActivityLocked || inputCamera == null)
            {
                return false;
            }

            Ray ray = inputCamera.ScreenPointToRay(position);
            if (!Physics.Raycast(ray, out RaycastHit hit, pointerRayDistance, ~0, QueryTriggerInteraction.Collide) ||
                !reaction.IsPetTarget(hit.collider))
            {
                return false;
            }

            if (pointerId < 0)
            {
                mouseCaptured = true;
                roomCamera?.ReserveMousePointer();
            }
            else
            {
                touchId = pointerId;
                roomCamera?.ReserveTouchPointer(pointerId);
            }

            crossedThreshold = false;
            strokeDistance = 0f;
            if (roomCamera != null)
            {
                startYaw = roomCamera.CurrentYaw;
                startPitch = roomCamera.CurrentPitch;
                startDistance = roomCamera.CurrentDistance;
            }
            return true;
        }

        private void ProcessPosition(Vector2 position)
        {
            Vector2 delta = position - previousPosition;
            if (delta.sqrMagnitude < 0.25f)
            {
                return;
            }

            previousPosition = position;
            if (IsOnPetTarget(position))
            {
                strokeDistance += delta.magnitude;
                if (!crossedThreshold && strokeDistance >= strokeThresholdPixels)
                {
                    crossedThreshold = true;
                    reaction.TryReact(delta.x);
                }
            }
        }

        private bool IsOnPetTarget(Vector2 position)
        {
            if (inputCamera == null || reaction == null)
            {
                return false;
            }

            Ray ray = inputCamera.ScreenPointToRay(position);
            return Physics.Raycast(ray, out RaycastHit hit, pointerRayDistance, ~0, QueryTriggerInteraction.Collide) && reaction.IsPetTarget(hit.collider);
        }

        private void EndCapture()
        {
            if (!IsPointerCaptured)
            {
                return;
            }

            if (!crossedThreshold)
            {
                reaction.RejectTap();
                roomCamera?.FocusOnCat(motor.transform);
            }
            reaction.EndPetting();
            if (mouseCaptured)
            {
                roomCamera?.ReleaseMousePointer();
            }
            else if (touchId >= 0)
            {
                roomCamera?.ReleaseTouchPointer(touchId);
            }

            mouseCaptured = false;
            touchId = -1;
            crossedThreshold = false;
            strokeDistance = 0f;
        }

        private void CancelCapture()
        {
            if (!IsPointerCaptured)
            {
                return;
            }

            reaction.CancelPetting();
            if (mouseCaptured)
            {
                roomCamera?.ReleaseMousePointer();
            }
            else if (touchId >= 0)
            {
                roomCamera?.ReleaseTouchPointer(touchId);
            }

            mouseCaptured = false;
            touchId = -1;
            crossedThreshold = false;
            strokeDistance = 0f;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelCapture();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) CancelCapture();
        }

        private void TrackCameraDelta()
        {
            if (!IsPointerCaptured || roomCamera == null)
            {
                return;
            }

            maximumCameraDelta = Mathf.Max(maximumCameraDelta,
                Mathf.Max(Mathf.Abs(roomCamera.CurrentYaw - startYaw),
                    Mathf.Max(Mathf.Abs(roomCamera.CurrentPitch - startPitch), Mathf.Abs(roomCamera.CurrentDistance - startDistance))));
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);
        }
    }
}
