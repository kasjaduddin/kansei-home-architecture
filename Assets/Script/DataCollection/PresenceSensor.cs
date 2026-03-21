using System;
using UnityEngine;
using UnityEngine.XR;

namespace VRHomeArch.DataCollection
{
    // Polls the XR headset proximity sensor to detect when the respondent
    // puts on or removes the headset. These transitions are the primary driver
    // for phase changes (Training->Baseline, HouseExploration->WaitingForBreak->Neutral).
    //
    // Uses CommonUsages.userPresence which is supported on Meta Quest 3 via OpenXR.
    // Falls back to "always present" on devices that do not report this feature,
    // so the session is not blocked on unsupported hardware.
    public class PresenceSensor : MonoBehaviour
    {
        public event Action OnHeadsetPutOn;
        public event Action OnHeadsetRemoved;

        // How often to check presence state. 0.5s is sufficient — headset removal
        // is not a sub-second event in normal usage.
        [SerializeField] private float _pollIntervalSeconds = 0.5f;

#if UNITY_EDITOR
        // Allows manual simulation of presence changes during Editor testing.
        // Toggle via Inspector or the context menu items below.
        [SerializeField] private bool _simulatePresent = true;
#endif

        private bool _lastKnownPresence;
        private float _pollTimer;

        private void Start()
        {
            _lastKnownPresence = SamplePresence();
        }

        private void Update()
        {
            _pollTimer += Time.deltaTime;
            if (_pollTimer < _pollIntervalSeconds)
                return;

            _pollTimer = 0f;
            bool currentPresence = SamplePresence();

            if (currentPresence && !_lastKnownPresence)
            {
                _lastKnownPresence = true;
                OnHeadsetPutOn?.Invoke();
            }
            else if (!currentPresence && _lastKnownPresence)
            {
                _lastKnownPresence = false;
                OnHeadsetRemoved?.Invoke();
            }
        }

        private bool SamplePresence()
        {
#if UNITY_EDITOR
            return _simulatePresent;
#else
            InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);

            // TryGetFeatureValue returns false if the device does not support this feature.
            // In that case we assume the headset is on so the session is not blocked.
            if (headDevice.TryGetFeatureValue(CommonUsages.userPresence, out bool isPresent))
                return isPresent;

            return true;
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Simulate: Put On Headset")]
        private void SimulatePutOn() => _simulatePresent = true;

        [ContextMenu("Simulate: Remove Headset")]
        private void SimulateRemove() => _simulatePresent = false;
#endif
    }
}