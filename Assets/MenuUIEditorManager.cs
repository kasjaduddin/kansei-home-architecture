using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIEditorManager : MonoBehaviour
{
    [SerializeField] private Button coloringButton;
    [SerializeField] private Button furnitureButton;
    [SerializeField] private Button embededButton;
    [SerializeField] private Button simulationButton;
    [SerializeField] private GameObject furnitureCanvas;
    [SerializeField] private GameObject coloringCanvas;
    [SerializeField] private GameObject embededCanvas;
    [SerializeField] private GameObject simulationCanvas;
    [SerializeField] private GameObject menuUICanvas;
    private Transform positionSource;
    public float distance = 1.5f;
    public float verticalOffset = -0.5f;
    private GameObject currentActiveCanvas = null;
    
    private void Start()
    {
        // Dynamically find a camera to use
        positionSource = GetCameraTransform();

        coloringButton.onClick.AddListener(() => {
            ShowCanvas(coloringCanvas);
        });

        furnitureButton.onClick.AddListener(() => {
            ShowCanvas(furnitureCanvas);
        });

        embededButton.onClick.AddListener(() => {
            ShowCanvas(embededCanvas);
        });

        simulationButton.onClick.AddListener(() => {
            ShowCanvas(simulationCanvas);
        });
    }
    public void ShowCanvas(GameObject canvas)
    {
        currentActiveCanvas = canvas;
        HideAllCanvases(); // Hide other canvases
        ShowCanvasInFrontOfCamera(canvas);
    }
    public void HideAllCanvases()
    {
        coloringCanvas.SetActive(false);
        furnitureCanvas.SetActive(false);
        embededCanvas.SetActive(false);
        simulationCanvas.SetActive(false);
        menuUICanvas.SetActive(false);
    }
    private Transform GetCameraTransform()
    {
        if (Camera.main != null)
            return Camera.main.transform;
        else if (Camera.current != null)
            return Camera.current.transform;
        else if (Camera.allCameras.Length > 0)
            return Camera.allCameras[0].transform;
        else
        {
            Debug.LogWarning("No camera found in scene.");
            return null;
        }
    }

    private void ShowCanvasInFrontOfCamera(GameObject canvasObject)
    {
        if (canvasObject == null || positionSource == null) return;

        canvasObject.SetActive(true);

        Vector3 direction = positionSource.forward;
        direction.y = 0;
        direction.Normalize();

        Vector3 targetPosition = positionSource.position + direction * distance + Vector3.up * verticalOffset;

        canvasObject.transform.position = targetPosition;
        canvasObject.transform.rotation = Quaternion.LookRotation(canvasObject.transform.position - positionSource.position);
    }
}