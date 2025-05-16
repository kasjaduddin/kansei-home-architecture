using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class CanvasPopUpManager : MonoBehaviour
{
    [SerializeField] private GameObject furnitureCanvas;
    [SerializeField] private GameObject coloringCanvas;
    [SerializeField] private GameObject embededCanvas;
    [SerializeField] private GameObject simulationCanvas;
    [SerializeField] private GameObject lightCanvas;
    [SerializeField] private GameObject mainMenuUICanvas;
    [SerializeField] private MenuUIEditorManager menuUICanvas;
    private InputDevice rightController;
    private bool previousButtonState = false;

    private void Start()
    {
        TryInitializeController();
    }

    private void TryInitializeController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
        {
            rightController = devices[0];
        }
    }

    private void Update()
    {
        if (!rightController.isValid)
        {
            TryInitializeController();
            return;
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            // Toggle canvas visibility
            if (mainMenuUICanvas.activeSelf)
            {
                menuUICanvas.HideAllCanvases();
            }
            else
            {
                menuUICanvas.ShowCanvas(mainMenuUICanvas);
            }
        }

        // Read B button (secondaryButton)
        if (rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool buttonPressed))
        {
            // Detect press down (not hold)
            if (buttonPressed && !previousButtonState)
            {
                // Toggle canvas visibility
                if (mainMenuUICanvas.activeSelf)
                {
                    menuUICanvas.HideAllCanvases();
                }
                else
                {
                    menuUICanvas.ShowCanvas(mainMenuUICanvas);
                }
            }

            previousButtonState = buttonPressed;
        }
    }
}