using System;
using UnityEngine;

namespace VRHomeArch.Tutorial
{
    // Abstract base for all tutorial step types.
    //
    // A step is a single guided task in a tutorial sequence. Two concrete
    // implementations are provided:
    //   - TutorialInputStep : completes when the respondent pushes a thumbstick
    //   - TutorialWaypoint  : completes when the respondent walks to a location
    //
    // The guide script (e.g. TrainingGuide for data collection, or a production
    // equivalent) owns an ordered array of TutorialStep and advances through it
    // by listening to OnCompleted on the currently active step.
    public abstract class TutorialStep : MonoBehaviour
    {
        // Text displayed on the floating instruction panel while this step is active.
        [SerializeField] private string _instructionText;

        // Fired exactly once when the step's completion condition is met.
        // The guide script subscribes and advances the sequence.
        public event Action OnCompleted;

        public string InstructionText => _instructionText;

        // Called by the guide script when this step becomes the current task.
        public abstract void Activate();

        // Called by the guide script when advancing past this step, or on reset.
        public abstract void Deactivate();

        // Subclasses call this to signal completion — never invoke OnCompleted directly.
        protected void CompleteStep()
        {
            OnCompleted?.Invoke();
        }
    }
}