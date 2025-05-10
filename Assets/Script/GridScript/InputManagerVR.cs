using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.XR;

public class InputManagerVR : MonoBehaviour
{
    [SerializeField] private Transform controllerTransform; // VR controller transform
    [SerializeField] private LayerMask placementLayermask;
    public event Action OnClicked, OnExit;

    private Vector3 lastPosition;

    private InputDevice controllerDevice;
    private bool triggerPressedLastFrame = false;
    private bool bButtonPressedLastFrame = false;

    private void Start()
    {
        // Get Right Controller
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, inputDevices);

        if (inputDevices.Count > 0)
        {
            controllerDevice = inputDevices[0];
        }
        else
        {
            Debug.LogWarning("Right hand controller not found.");
        }
    }

    private void Update()
    {
        if (!controllerDevice.isValid) return;

        // Trigger (Primary Index Trigger)
        if (controllerDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed))
        {
            if (triggerPressed && !triggerPressedLastFrame)
            {
                OnClicked?.Invoke();
            }
            triggerPressedLastFrame = triggerPressed;
        }

        // B Button (Secondary Button)
        if (controllerDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bButtonPressed))
        {
            if (bButtonPressed && !bButtonPressedLastFrame)
            {
                OnExit?.Invoke();
            }
            bButtonPressedLastFrame = bButtonPressed;
        }
    }

    public bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public Vector3 GetSelectedMapPosition()
    {
        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 100, placementLayermask))
        {
            lastPosition = hit.point;
        }

        return lastPosition;
    }
}