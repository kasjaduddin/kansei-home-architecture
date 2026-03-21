using UnityEngine;

namespace VRHomeArch.DataCollection
{
    // Positions this World Space Canvas in front of the player's camera each time it is shown.
    // Rotation is yaw-only so the panel always stands upright regardless of head pitch.
    // Attach to the RemoveHeadsetPrompt Canvas GameObject.
    [RequireComponent(typeof(Canvas))]
    public class HeadsetRemovalPrompt : MonoBehaviour
    {
        [Header("Placement")]
        // Distance in metres from the camera at which the panel appears
        [SerializeField] private float _distanceFromCamera = 1.5f;
        // Vertical offset relative to the camera position (negative = slightly below eye level)
        [SerializeField] private float _verticalOffset = -0.1f;

        // The main camera is resolved once in Awake and cached.
        // Camera.main uses FindObjectOfType internally, so it must not be called per-frame.
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;

            if (_mainCamera == null)
                Debug.LogError("[HeadsetRemovalPrompt] No camera tagged MainCamera found in scene.");
        }

        // OnEnable is called each time SessionManager does SetActive(true) on this GameObject.
        // Placement happens here so the panel is always in front of wherever the user is
        // looking at the moment the prompt appears.
        private void OnEnable()
        {
            if (_mainCamera == null)
            {
                Debug.LogWarning("[HeadsetRemovalPrompt] Cannot place prompt — main camera reference is null.");
                return;
            }

            PlaceInFrontOfCamera();
        }

        private void PlaceInFrontOfCamera()
        {
            // Extract yaw only from the camera's current rotation so the panel stands upright.
            // Using only the Y Euler angle discards pitch and roll entirely.
            float yaw = _mainCamera.transform.eulerAngles.y;
            Quaternion yawOnlyRotation = Quaternion.Euler(0f, yaw, 0f);

            // Forward direction on the horizontal plane, derived from the yaw-only rotation.
            Vector3 flatForward = yawOnlyRotation * Vector3.forward;

            // Position: camera world position + flat forward * distance + vertical offset.
            // Vertical offset is applied in world space so it is independent of camera tilt.
            Vector3 targetPosition = _mainCamera.transform.position
                + flatForward * _distanceFromCamera
                + Vector3.up * _verticalOffset;

            transform.position = targetPosition;

            // Face the panel toward the camera using the same yaw-only forward,
            // so the canvas normal points back at the player's face.
            transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }
    }
}