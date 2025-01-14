using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICanvasManager : MonoBehaviour
{
    [SerializeField] private GameObject MainCanvas;
    [SerializeField] private Canvas ExploreCanvas;
    [SerializeField] private Canvas EditCanvas;
    [SerializeField] private Canvas DesignCanvas;

    private Dictionary<CanvasType, Canvas> canvasDictionary;

    public enum CanvasType
    {
        MainCanvas,
        ExploreCanvas,
        EditCanvas,
        DesignCanvas
    }

    private void Awake()
    {
        MainCanvas.gameObject.SetActive(true);
        // Inisialisasi dictionary untuk kemudahan akses canvas berdasarkan nama
        canvasDictionary = new Dictionary<CanvasType, Canvas>
        {
            { CanvasType.ExploreCanvas, ExploreCanvas },
            { CanvasType.EditCanvas, EditCanvas },
            { CanvasType.DesignCanvas, DesignCanvas }
        };
    }

    public void ActivateCanvas(CanvasType canvasType)
    {
        foreach (var canvas in canvasDictionary.Values)
        {
            canvas.gameObject.SetActive(false);
        }
        MainCanvas.gameObject.SetActive(false);

        switch (canvasType)
        {
            case CanvasType.MainCanvas:
                MainCanvas.gameObject.SetActive(true);
                break;

            case CanvasType.ExploreCanvas:
                ExploreCanvas.gameObject.SetActive(true);
                DesignCanvas.gameObject.SetActive(true);
                break;

            case CanvasType.EditCanvas:
                EditCanvas.gameObject.SetActive(true);
                DesignCanvas.gameObject.SetActive(true);
                break;

            default:
                Debug.LogWarning($"Canvas {canvasType} is not configured!");
                break;
        }
    }
}