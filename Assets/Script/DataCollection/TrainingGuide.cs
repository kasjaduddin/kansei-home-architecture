using System;
using TMPro;
using UnityEngine;
using VRHomeArch.Tutorial;

namespace VRHomeArch.DataCollection
{
    // Orchestrates the data collection training sequence using TutorialStep components.
    //
    // Steps are activated one at a time in Inspector order. When all steps complete,
    // OnTrainingCompleted fires and SessionManager transitions to WaitingForBaseline.
    //
    // This class is intentionally specific to the data collection flow — it owns
    // the 5-step sequence (look right, look left, walk, pass gate, reach exit).
    // The production app will have its own guide script using the same TutorialStep
    // components with a different step arrangement.
    //
    // The instruction panel floats in front of the respondent throughout training
    // using yaw-only rotation so the panel stays upright regardless of head pitch.
    public class TrainingGuide : MonoBehaviour
    {
        // Ordered tutorial steps — assign in Inspector.
        // Mix TutorialInputStep and TutorialWaypoint GameObjects freely.
        [Header("Steps")]
        [SerializeField] private TutorialStep[] _steps;

        // World-space Canvas that shows the current instruction text.
        [Header("Instruction Panel")]
        [SerializeField] private Canvas _instructionPanel;
        [SerializeField] private TextMeshProUGUI _instructionText;

        // Main Camera transform — used to position the panel each frame.
        // Assign: XR Origin (XR Rig) / Camera Offset / Main Camera
        [SerializeField] private Transform _cameraTransform;

        // Distance in front of the camera the panel floats (metres).
        [SerializeField] private float _panelDistance = 1.8f;

        // Vertical offset from camera position. Negative = slightly below eye level.
        [SerializeField] private float _panelVerticalOffset = -0.1f;

        // SessionManager subscribes to this to trigger the transition to WaitingForBaseline.
        public event Action OnTrainingCompleted;

        private int _currentStepIndex;
        private bool _isActive;

        // -----------------------------------------------------------------------
        // Public API — called by SessionManager
        // -----------------------------------------------------------------------

        // Start the training sequence from step 0.
        // Resets all state first — safe to call again between respondents.
        public void BeginTraining()
        {
            ResetTraining();
            _isActive = true;

            if (_instructionPanel != null)
                _instructionPanel.gameObject.SetActive(true);

            AdvanceToStep(0);
        }

        // Deactivate all steps and hide the panel.
        // Called when SessionManager returns to IDLE between respondents.
        public void ResetTraining()
        {
            _isActive = false;
            _currentStepIndex = 0;

            if (_steps != null)
            {
                foreach (TutorialStep step in _steps)
                {
                    if (step != null)
                        step.Deactivate();
                }
            }

            if (_instructionPanel != null)
                _instructionPanel.gameObject.SetActive(false);
        }

        // -----------------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (_steps == null || _steps.Length == 0)
                Debug.LogWarning("[TrainingGuide] No steps assigned. Training will complete immediately on BeginTraining.");

            if (_cameraTransform == null)
                Debug.LogError("[TrainingGuide] Camera Transform is not assigned. The instruction panel will not follow the respondent.");

            if (_instructionPanel != null)
                _instructionPanel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isActive) return;
            if (_instructionPanel == null || _cameraTransform == null) return;

            PositionPanelInFrontOfCamera();
        }

        // -----------------------------------------------------------------------
        // Step sequencing
        // -----------------------------------------------------------------------

        private void AdvanceToStep(int index)
        {
            _currentStepIndex = index;

            if (_steps == null || index >= _steps.Length)
            {
                OnAllStepsCompleted();
                return;
            }

            TutorialStep step = _steps[index];
            if (step == null)
            {
                Debug.LogWarning($"[TrainingGuide] Step at index {index} is null — skipping");
                AdvanceToStep(index + 1);
                return;
            }

            step.OnCompleted += HandleStepCompleted;
            step.Activate();

            SetInstructionText(step.InstructionText);
            Debug.Log($"[TrainingGuide] Step {index + 1}/{_steps.Length} active: '{step.name}'");
        }

        private void HandleStepCompleted()
        {
            // Unsubscribe from the just-completed step before advancing
            if (_steps != null && _currentStepIndex < _steps.Length)
            {
                TutorialStep current = _steps[_currentStepIndex];
                if (current != null)
                    current.OnCompleted -= HandleStepCompleted;
            }

            Debug.Log($"[TrainingGuide] Step {_currentStepIndex + 1} completed");
            AdvanceToStep(_currentStepIndex + 1);
        }

        private void OnAllStepsCompleted()
        {
            Debug.Log("[TrainingGuide] All steps completed — firing OnTrainingCompleted");

            _isActive = false;

            if (_instructionPanel != null)
                _instructionPanel.gameObject.SetActive(false);

            OnTrainingCompleted?.Invoke();
        }

        // -----------------------------------------------------------------------
        // Panel positioning — yaw-only billboard, same approach as RemoveHeadsetPrompt
        // -----------------------------------------------------------------------

        private void PositionPanelInFrontOfCamera()
        {
            Vector3 cameraPos = _cameraTransform.position;
            Vector3 cameraForward = _cameraTransform.forward;

            // Project to horizontal plane — ignore pitch so the panel stays upright
            Vector3 flatForward = new Vector3(cameraForward.x, 0f, cameraForward.z);

            // Fallback when camera points straight up or down
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = Vector3.forward;
            else
                flatForward.Normalize();

            Vector3 targetPosition = cameraPos
                + flatForward * _panelDistance
                + Vector3.up * _panelVerticalOffset;

            // Panel faces toward the camera so text is readable
            Quaternion targetRotation = Quaternion.LookRotation(flatForward);

            Transform panelTransform = _instructionPanel.transform;
            panelTransform.position = targetPosition;
            panelTransform.rotation = targetRotation;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private void SetInstructionText(string text)
        {
            if (_instructionText != null)
                _instructionText.text = text;
        }
    }
}