using UnityEngine;

namespace VRHomeArch.Tutorial
{
    // A tutorial step that completes when the respondent walks within proximity
    // of a marked world-space location.
    //
    // A world-space arrow image (IndicatorCanvas) sits above the target position
    // as a navigation cue. The SphereCollider defines the arrival radius.
    //
    // Used for "walk here", "pass the gate", and "reach the exit zone" steps.
    // The exit zone step in data collection is simply the last TutorialWaypoint
    // in the array — no separate exit trigger script is needed.
    [RequireComponent(typeof(SphereCollider))]
    public class TutorialWaypoint : TutorialStep
    {
        // Root of the world-space Canvas that holds the arrow image.
        // Shown when the waypoint is active, hidden otherwise.
        [SerializeField] private GameObject _indicatorCanvas;

        // Arrival radius in metres. The SphereCollider radius is kept in sync
        // via OnValidate so the gizmo always reflects the actual detection area.
        [SerializeField] private float _proximityRadius = 1.5f;

        private SphereCollider _sphereCollider;
        private bool _isActive;

        private void Awake()
        {
            _sphereCollider = GetComponent<SphereCollider>();
            _sphereCollider.isTrigger = true;
            _sphereCollider.radius = _proximityRadius;

            // Start inactive — the guide script activates waypoints in sequence
            SetVisuals(false);
            _sphereCollider.enabled = false;
        }

        public override void Activate()
        {
            _isActive = true;
            _sphereCollider.enabled = true;
            SetVisuals(true);
        }

        public override void Deactivate()
        {
            _isActive = false;
            _sphereCollider.enabled = false;
            SetVisuals(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;
            if (!other.CompareTag("Player")) return;

            Deactivate();
            Debug.Log($"[TutorialWaypoint] '{name}' reached — step complete");
            CompleteStep();
        }

        private void SetVisuals(bool visible)
        {
            if (_indicatorCanvas != null)
                _indicatorCanvas.SetActive(visible);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SphereCollider col = GetComponent<SphereCollider>();
            if (col != null)
                col.radius = _proximityRadius;
        }
#endif
    }
}