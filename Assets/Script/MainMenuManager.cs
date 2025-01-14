using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button ExploreButton;
    [SerializeField] private Button EditButton;

    private UICanvasManager canvasManager;

    private void Start()
    {
        canvasManager = FindObjectOfType<UICanvasManager>();
        ExploreButton.onClick.AddListener(() =>
        {
            canvasManager.ActivateCanvas(UICanvasManager.CanvasType.ExploreCanvas);
        });
        EditButton.onClick.AddListener(() =>
        {
            canvasManager.ActivateCanvas(UICanvasManager.CanvasType.EditCanvas);
        });
    }
}
