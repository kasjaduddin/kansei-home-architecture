using TMPro;
using UnityEngine;

namespace VRHomeArch.Tutorial
{
    // Attach to the root Canvas of the Tutorial_Panel_Instruction prefab.
    //
    // Handles its own world-space positioning — floats in front of the camera
    // each frame using yaw-only rotation so the panel stays upright regardless
    // of head pitch. Any guide script (TrainingGuide, ProductionTutorialGuide,
    // etc.) only needs to call Show(), Hide(), and SetText().
    [RequireComponent(typeof(Canvas))]
    public class TutorialInstructionPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _instructionText;

        // Main Camera transform — assign: XR Origin (XR Rig) / Camera Offset / Main Camera
        [SerializeField] private Transform _cameraTransform;

        // Distance in front of the camera the panel floats (metres).
        [SerializeField] private float _panelDistance = 1.8f;

        // Vertical offset from camera position. Negative = slightly below eye level.
        [SerializeField] private float _panelVerticalOffset = -0.1f;

        private bool _isVisible;

        // -----------------------------------------------------------------------
        // Public API — called by guide scripts
        // -----------------------------------------------------------------------

        public void Show()
        {
            _isVisible = true;
            gameObject.SetActive(true);

            // Snap to correct position immediately on show — avoids a single frame
            // where the panel appears at its last known or default position.
            UpdatePosition();
        }

        public void Hide()
        {
            _isVisible = false;
            gameObject.SetActive(false);
        }

        public void SetText(string text)
        {
            if (_instructionText != null)
                _instructionText.text = text;
        }

        // -----------------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (_instructionText == null)
                Debug.LogError("[TutorialInstructionPanel] InstructionText (TextMeshProUGUI) is not assigned.");

            if (_cameraTransform == null)
                Debug.LogError("[TutorialInstructionPanel] Camera Transform is not assigned. Panel will not follow the respondent.");

            // Panels start hidden — the guide script calls Show() when training begins.
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isVisible) return;
            UpdatePosition();
        }

        // -----------------------------------------------------------------------
        // Positioning — yaw-only billboard
        // -----------------------------------------------------------------------

        // Positions the panel in front of the camera using only the horizontal
        // (yaw) component of camera rotation. This keeps the panel upright —
        // it does not tilt when the respondent looks up or down.
        private void UpdatePosition()
        {
            if (_cameraTransform == null) return;

            Vector3 cameraPos = _cameraTransform.position;
            Vector3 cameraForward = _cameraTransform.forward;

            // Project to horizontal plane — strip pitch
            Vector3 flatForward = new Vector3(cameraForward.x, 0f, cameraForward.z);

            // Fallback when camera points straight up or down
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = Vector3.forward;
            else
                flatForward.Normalize();

            transform.position = cameraPos
                + flatForward * _panelDistance
                + Vector3.up * _panelVerticalOffset;

            // Face toward the camera so text is readable
            transform.rotation = Quaternion.LookRotation(flatForward);
        }
    }
}