using UnityEngine;

namespace VRHomeArch.UI
{
    // Attach to any world-space GameObject that should face the camera each frame.
    //
    // Axes can be configured independently — lock or unlock X, Y, Z rotation.
    // An optional bobbing effect animates the object up and down on the Y axis,
    // useful for waypoint indicators to draw the respondent's attention.
    public class BillboardFacing : MonoBehaviour
    {
        // The transform to face toward each frame.
        // Assign: XR Origin (XR Rig) / Camera Offset / Main Camera
        [Header("Target")]
        [SerializeField] private Transform _cameraTransform;

        // Which rotation axes are allowed to update each frame.
        // Uncheck an axis to lock it — the object will not rotate on that axis.
        [Header("Rotation Axes")]
        [SerializeField] private bool _rotateX = false;
        [SerializeField] private bool _rotateY = true;
        [SerializeField] private bool _rotateZ = false;

        // Optional Y-axis bobbing effect.
        // Animates the object up and down to draw attention without moving
        // on X or Z — the object stays above its anchor position.
        [Header("Bobbing")]
        [SerializeField] private bool _enableBobbing = false;

        // Total vertical travel distance (peak to peak) in metres.
        [SerializeField] private float _bobbingAmplitude = 0.08f;

        // Complete cycles per second.
        [SerializeField] private float _bobbingFrequency = 0.8f;

        private Vector3 _anchorPosition;

        private void Awake()
        {
            if (_cameraTransform == null)
                Debug.LogWarning("[BillboardFacing] Camera Transform is not assigned. BillboardFacing will not update.");

            // Record the authored position as the bobbing anchor point.
            // Bobbing oscillates around this position rather than drifting over time.
            _anchorPosition = transform.position;
        }

        private void Update()
        {
            if (_cameraTransform == null) return;

            UpdateFacing();

            if (_enableBobbing)
                UpdateBobbing();
        }

        // -----------------------------------------------------------------------
        // Facing
        // -----------------------------------------------------------------------

        private void UpdateFacing()
        {
            // Direction from this object toward the camera
            Vector3 toCamera = _cameraTransform.position - transform.position;

            if (toCamera.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(-toCamera);

            // Blend target rotation with current rotation on locked axes
            Vector3 currentEuler = transform.rotation.eulerAngles;
            Vector3 targetEuler = targetRotation.eulerAngles;

            transform.rotation = Quaternion.Euler(
                _rotateX ? targetEuler.x : currentEuler.x,
                _rotateY ? targetEuler.y : currentEuler.y,
                _rotateZ ? targetEuler.z : currentEuler.z
            );
        }

        // -----------------------------------------------------------------------
        // Bobbing
        // -----------------------------------------------------------------------

        private void UpdateBobbing()
        {
            // Sine wave oscillation centered on the anchor Y position.
            // Amplitude is halved because sine swings from -1 to +1 (full range = 2).
            float offset = Mathf.Sin(Time.time * _bobbingFrequency * Mathf.PI * 2f)
                           * (_bobbingAmplitude * 0.5f);

            transform.position = new Vector3(
                _anchorPosition.x,
                _anchorPosition.y + offset,
                _anchorPosition.z
            );
        }

        // Call this if the object is repositioned at runtime so bobbing
        // anchors to the new position instead of the original authored position.
        public void RefreshAnchor()
        {
            _anchorPosition = transform.position;
        }
    }
}