using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRHomeArch.DataCollection
{
    // A training step that completes when the respondent pushes the right thumbstick
    // in the required direction (left or right).
    //
    // Used for the "look left" and "look right" steps at the start of training —
    // the respondent must deliberately use the right thumbstick to rotate the camera,
    // proving they understand the snap-turn interaction before proceeding.
    //
    // Detection reads the right controller's primary 2D axis each frame.
    // The step completes on the first frame the axis magnitude exceeds the threshold
    // in the configured direction — a single intentional push is sufficient.
    public class TrainingInputStep : TrainingStep
    {
        public enum TurnDirection { Right, Left }

        // Whether the respondent must push the stick right or left.
        [SerializeField] private TurnDirection _requiredDirection;

        // Axis X magnitude required to count as a deliberate push.
        // 0.5 matches the default XRI snap-turn deadzone.
        [SerializeField] private float _inputThreshold = 0.5f;

        private bool _isActive;
        private List<InputDevice> _rightControllerDevices;

        private void Awake()
        {
            _rightControllerDevices = new List<InputDevice>();
        }

        public override void Activate()
        {
            _isActive = true;
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
                // Device list can be empty early in the frame — retry each update
                RefreshRightController();
                return;
            }

            if (!_rightControllerDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
                return;

            bool conditionMet = _requiredDirection == TurnDirection.Right
                ? axis.x > _inputThreshold
                : axis.x < -_inputThreshold;

            if (conditionMet)
            {
                _isActive = false;
                Debug.Log($"[TrainingInputStep] {_requiredDirection} turn input detected — step complete");
                CompleteStep();
            }
        }

        // Queries XR input system for the right hand controller.
        // Called on Activate and as fallback in Update if the list is still empty.
        private void RefreshRightController()
        {
            _rightControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                _rightControllerDevices);
        }
    }
}