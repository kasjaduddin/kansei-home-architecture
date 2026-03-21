using UnityEngine;

namespace VRHomeArch.DataCollection
{
    // Opens and closes a door by playing dedicated open and close animation clips.
    // The Animator is disabled on Awake so the door holds its prefab-authored closed pose
    // until the player first enters the trigger zone.
    //
    // Animator Controller setup (one-time, per door):
    //   - State "Door_Open"  containing the open  clip (Loop Time = false)
    //   - State "Door_Close" containing the close clip (Loop Time = false)
    //   - No parameters, no transitions required
    //
    // Scene setup:
    //   Door_Classic_SinglePanel_Dark:
    //     - Add child "DoorTrigger" to the prefab root
    //     - Attach Box Collider (isTrigger = true) + this script to DoorTrigger
    //     - Assign the "Door" child (which has the Animator) to _animatorTarget
    //
    //   Mini_Park Gate:
    //     - Add sibling "GateTrigger" next to the Gate GameObject (not as child,
    //       so the collider does not move when the gate animates)
    //     - Attach Box Collider (isTrigger = true) + this script to GateTrigger
    //     - Assign the "Gate" child (which has the Animator) to _animatorTarget
    [RequireComponent(typeof(Collider))]
    public class AutoDoor : MonoBehaviour
    {
        // The GameObject that carries the Animator component.
        [SerializeField] private Animator _animatorTarget;

        // Must match the state names in the Animator Controller exactly.
        [SerializeField] private string _openStateName = "Door_Open";
        [SerializeField] private string _closeStateName = "Door_Close";

        private const string PlayerTag = "Player";

        private void Awake()
        {
            if (_animatorTarget == null)
            {
                Debug.LogError($"[AutoDoor] Animator Target is not assigned on {gameObject.name}.");
                return;
            }

            // Disable the Animator so it does not apply any pose on startup.
            // The door holds its prefab-authored closed transform until first trigger entry.
            _animatorTarget.enabled = false;

            Collider col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[AutoDoor] Collider on {gameObject.name} was not a trigger — corrected at runtime.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(PlayerTag))
                return;

            _animatorTarget.enabled = true;
            _animatorTarget.speed = 1f;
            _animatorTarget.Play(_openStateName, layer: 0, normalizedTime: 0f);
            Debug.Log($"[AutoDoor] {gameObject.name} opening");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(PlayerTag))
                return;

            _animatorTarget.enabled = true;
            _animatorTarget.speed = 1f;
            _animatorTarget.Play(_closeStateName, layer: 0, normalizedTime: 0f);
            Debug.Log($"[AutoDoor] {gameObject.name} closing");
        }
    }
}