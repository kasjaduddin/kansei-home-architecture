using UnityEngine;

namespace VRHomeArch.DataCollection
{
    // Placed on a trigger collider at the exit boundary of the training area
    // (e.g. an archway or a line on the floor marked with a visible element).
    //
    // When the XR Rig's head or body collider crosses into this zone,
    // SessionManager is notified to end the Training phase.
    //
    // The trigger fires only once per activation — re-entering training area
    // does not re-trigger it (session has already progressed past Training).
    public class TrainingExitTrigger : MonoBehaviour
    {
        [SerializeField] private SessionManager _sessionManager;

        // Tag on the XR Rig collider that represents the respondent's body/head.
        // Default is "Player" — match whatever tag is on your XR Rig collider.
        [SerializeField] private string _playerTag = "Player";

        private bool _triggered;

        private void Awake()
        {
            if (_sessionManager == null)
                Debug.LogError("[TrainingExitTrigger] SessionManager reference is not assigned.");

            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogError("[TrainingExitTrigger] No Collider component found. Add a Collider and mark it as Trigger.");
                return;
            }

            if (!col.isTrigger)
            {
                // Auto-correct rather than silently fail.
                col.isTrigger = true;
                Debug.LogWarning("[TrainingExitTrigger] Collider was not marked as Trigger — fixed automatically.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag(_playerTag)) return;
            if (_sessionManager == null) return;

            // Only meaningful during Training phase — guard against stray collisions in other phases.
            if (_sessionManager.CurrentPhase != SessionPhase.Training) return;

            _triggered = true;
            Debug.Log("[TrainingExitTrigger] Player exited training area — notifying SessionManager.");
            _sessionManager.NotifyTrainingExitTriggered();
        }

        // Reset allows the trigger to fire again if the scene is reloaded or
        // SessionManager returns to IDLE between respondents.
        public void Reset()
        {
            _triggered = false;
        }
    }
}