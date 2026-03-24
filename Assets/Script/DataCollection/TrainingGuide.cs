using System;
using UnityEngine;
using VRHomeArch.Tutorial;

namespace VRHomeArch.DataCollection
{
    // Orchestrates the data collection training sequence using TutorialStep components.
    //
    // Steps are activated one at a time in Inspector order. When all steps complete,
    // OnTrainingCompleted fires and SessionManager transitions to WaitingForBaseline.
    //
    // Panel positioning is fully handled by TutorialInstructionPanel — this class
    // only calls Show(), Hide(), and SetText() on it.
    //
    // The production app will have its own guide script using the same TutorialStep
    // and TutorialInstructionPanel components with a different step arrangement.
    public class TrainingGuide : MonoBehaviour
    {
        // Ordered tutorial steps — assign in Inspector.
        // Mix TutorialInputStep and TutorialWaypoint GameObjects freely.
        [Header("Steps")]
        [SerializeField] private TutorialStep[] _steps;

        // Assign the TutorialInstructionPanel component from the
        // Tutorial_Panel_Instruction prefab instance in the scene.
        [Header("Instruction Panel")]
        [SerializeField] private TutorialInstructionPanel _instructionPanel;

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

            _instructionPanel?.Show();

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

            _instructionPanel?.Hide();
        }

        // -----------------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (_steps == null || _steps.Length == 0)
                Debug.LogWarning("[TrainingGuide] No steps assigned. Training will complete immediately on BeginTraining.");

            if (_instructionPanel == null)
                Debug.LogError("[TrainingGuide] TutorialInstructionPanel is not assigned.");
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

            _instructionPanel?.SetText(step.InstructionText);
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
            _instructionPanel?.Hide();
            OnTrainingCompleted?.Invoke();
        }
    }
}