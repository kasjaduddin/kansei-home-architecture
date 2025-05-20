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
    [SerializeField] private Button lightButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private GameObject furnitureCanvas;
    [SerializeField] private GameObject coloringCanvas;
    [SerializeField] private GameObject embededCanvas;
    [SerializeField] private GameObject simulationCanvas;
    [SerializeField] private GameObject menuUICanvas;
    [SerializeField] private GameObject lightCanvas;
    [SerializeField] private GameObject infoCanvas;
    [SerializeField] private GameObject exitConfirmationCanvas;

    [SerializeField] private InputManagerVR inputManager;

    private Transform positionSource;
    public float distance = 1.5f;
    public float verticalOffset = -0.5f;
    private GameObject currentActiveCanvas = null;
    private bool isInfoVisible = false;
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
        lightButton.onClick.AddListener(() => {
            ShowCanvas(lightCanvas);
        });
        simulationButton.onClick.AddListener(() => {
            ShowCanvas(simulationCanvas);
        });
        infoButton.onClick.AddListener(() => {
            ShowCanvas(infoCanvas);
        });
        exitButton.onClick.AddListener(() => {
            //Loader.Load(Loader.Scene.MainMenu);
            ShowCanvas(exitConfirmationCanvas);
        });

        if (inputManager != null)
        {
            inputManager.OnToggleInfoCanvas += HandleToggleInfoCanvas;
        }

        ShowMainMenuCanvas();
    }
    public void ExitToMainMenu()
    {
        Loader.Load(Loader.Scene.MainMenu);
    }
    private void HandleToggleInfoCanvas()
    {
        isInfoVisible = !isInfoVisible;
        if (isInfoVisible)
        {
            ShowInfoCanvas();
        }
        else
        {
            ShowMainMenuCanvas();
        }
    }
    public void ShowCanvas(GameObject canvas)
    {
        currentActiveCanvas = canvas;
        HideAllCanvases(); // Hide other canvases
        ShowCanvasInFrontOfCamera(canvas);
    }
    public void ShowMainMenuCanvas()
    {
        ShowCanvas(menuUICanvas);
    }
    public void ShowFurnitureCanvas()
    {
        ShowCanvas(furnitureCanvas);
    }
    public void ShowInfoCanvas()
    {
        ShowCanvas(infoCanvas);
    }
    public void HideAllCanvases()
    {
        coloringCanvas.SetActive(false);
        furnitureCanvas.SetActive(false);
        embededCanvas.SetActive(false);
        simulationCanvas.SetActive(false);
        lightCanvas.SetActive(false);
        menuUICanvas.SetActive(false);
        infoCanvas.SetActive(false);
        exitConfirmationCanvas.SetActive(false);
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