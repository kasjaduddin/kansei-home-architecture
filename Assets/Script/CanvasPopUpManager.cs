using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasPopUpManager : MonoBehaviour
{
    [SerializeField] private GameObject furnitureCanvas;
    [SerializeField] private GameObject coloringCanvas;
    [SerializeField] private GameObject embededCanvas;
    [SerializeField] private GameObject simulationCanvas;
    [SerializeField] private MenuUIEditorManager menuUICanvas;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            menuUICanvas.ShowCanvas(menuUICanvas.gameObject);

        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            menuUICanvas.HideAllCanvases();
        }
    }
}
