using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRHomeArch.Tutorial
{
    // A tutorial step that completes when the respondent performs snap turns
    // in the required direction using the right thumbstick.
    //
    // Each push past the input threshold counts as one snap. The step completes
    // after the required number of snaps — allowing multi-snap steps if needed,
    // though one snap per step is the standard training configuration.
    //
    // Detection reads the right controller's primary 2D axis each frame.
    // A cooldown between snaps prevents a single slow push from counting multiple times.
    public class TutorialSnapLook : TutorialStep
    {
        public enum TurnDirection { Right, Left }

        // Direction the respondent must snap toward.
        [SerializeField] private TurnDirection _requiredDirection;

        // Number of snaps required to complete this step.
        [SerializeField] private int _requiredSnapCount = 1;

        // Axis X magnitude required to count as a deliberate push.
        // 0.5 matches the default XRI snap-turn deadzone.
        [SerializeField] private float _inputThreshold = 0.5f;

        // Seconds the stick must return below threshold before the next snap is counted.
        // Prevents one long push from registering as multiple snaps.
        [SerializeField] private float _snapCooldown = 0.3f;

        private bool _isActive;
        private int _snapCount;
        private float _cooldownTimer;
        private bool _waitingForRelease;
        private List<InputDevice> _rightControllerDevices;

        private void Awake()
        {
            _rightControllerDevices = new List<InputDevice>();
        }

        public override void Activate()
        {
            _isActive = true;
            _snapCount = 0;
            _cooldownTimer = 0f;
            _waitingForRelease = false;
            RefreshRightController();
        }

        public override void Deactivate()
        {
            _isActive = false;
        }

        private void Update()
        {
            if (!_isActive) return;

            if (_rightControllerDevices.Count == 0)
            {
                RefreshRightController();
                return;
            }

            if (!_rightControllerDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
                return;

            // Count down cooldown while stick is released
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                return;
            }

            bool inputActive = _requiredDirection == TurnDirection.Right
                ? axis.x > _inputThreshold
                : axis.x < -_inputThreshold;

            if (inputActive && !_waitingForRelease)
            {
                _waitingForRelease = true;
                _snapCount++;
                _cooldownTimer = _snapCooldown;

                Debug.Log($"[TutorialSnapLook] Snap {_snapCount}/{_requiredSnapCount} detected ({_requiredDirection})");

                if (_snapCount >= _requiredSnapCount)
                {
                    _isActive = false;
                    CompleteStep();
                }
            }
            else if (!inputActive)
            {
                // Stick returned to neutral — ready for the next snap
                _waitingForRelease = false;
            }
        }

        private void RefreshRightController()
        {
            _rightControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                _rightControllerDevices);
        }
    }
}