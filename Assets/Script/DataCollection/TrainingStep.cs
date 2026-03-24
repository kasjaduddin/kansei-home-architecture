using System;
using UnityEngine;

namespace VRHomeArch.DataCollection
{
    // Abstract base for all training step types.
    //
    // A step is a single guided task in the training sequence. There are two
    // concrete implementations:
    //   - TrainingInputStep : detects a thumbstick input from the respondent
    //   - TrainingWaypoint  : detects proximity — respondent walks to a marked location
    //
    // TrainingGuide owns an ordered array of TrainingStep and advances through it
    // by listening to OnCompleted on the currently active step.
    public abstract class TrainingStep : MonoBehaviour
    {
        // Text displayed on the floating instruction panel while this step is active.
        [SerializeField] private string _instructionText;

        // Fired exactly once when the step's completion condition is met.
        // TrainingGuide subscribes and advances the sequence.
        public event Action OnCompleted;

        public string InstructionText => _instructionText;

        // Called by TrainingGuide when this step becomes the current task.
        public abstract void Activate();

        // Called by TrainingGuide when advancing past this step, or on reset.
        public abstract void Deactivate();

        // Subclasses call this to signal completion — never call OnCompleted directly.
        protected void CompleteStep()
        {
            OnCompleted?.Invoke();
        }
    }
}