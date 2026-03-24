using UnityEngine;

namespace VRHomeArch.Tutorial
{
    // A tutorial step that completes when the respondent walks into a trigger collider.
    //
    // A world-space arrow image (IndicatorCanvas) sits above the target position
    // as a navigation cue. Any Collider type can be used — attach a BoxCollider
    // for area-shaped zones or a SphereCollider for radial proximity detection,
    // then mark it as Is Trigger in the Inspector.
    //
    // Used for walk, pass-gate, and exit-zone steps. The exit zone step is simply
    // the last TutorialWaypoint in the guide's step array — no separate exit
    // trigger script is needed.
    public class TutorialWaypoint : TutorialStep
    {
        // Root of the world-space Canvas that holds the arrow image.
        // Shown when this waypoint is the active step, hidden otherwise.
        [SerializeField] private GameObject _indicatorCanvas;

        private Collider _triggerCollider;
        private bool _isActive;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider>();

            if (_triggerCollider == null)
            {
                Debug.LogError($"[TutorialWaypoint] '{name}' has no Collider. " +
                               "Add a BoxCollider or SphereCollider and mark it Is Trigger.");
                return;
            }

            if (!_triggerCollider.isTrigger)
            {
                _triggerCollider.isTrigger = true;
                Debug.LogWarning($"[TutorialWaypoint] '{name}' Collider was not marked Is Trigger — fixed automatically.");
            }

            // Start inactive — the guide script activates waypoints in sequence
            SetVisuals(false);
            _triggerCollider.enabled = false;
        }

        public override void Activate()
        {
            _isActive = true;

            if (_triggerCollider != null)
                _triggerCollider.enabled = true;

            SetVisuals(true);
        }

        public override void Deactivate()
        {
            _isActive = false;

            if (_triggerCollider != null)
                _triggerCollider.enabled = false;

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
    }
}