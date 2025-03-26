using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIEditorManager : MonoBehaviour
{
    [SerializeField] private Button coloringButton;
    [SerializeField] private Button furnitureButton;
    [SerializeField] private Button embededButton;
    [SerializeField] private GameObject furnitureCanvas;
    [SerializeField] private GameObject coloringCanvas;
    [SerializeField] private GameObject embededCanvas;

    private void Start()
    {
        coloringButton.onClick.AddListener(() => {
            coloringCanvas.gameObject.SetActive(true);
            furnitureCanvas.gameObject.SetActive(false);
            embededCanvas.gameObject.SetActive(false);
        });
        furnitureButton.onClick.AddListener(() => {
            coloringCanvas.gameObject.SetActive(false);
            furnitureCanvas.gameObject.SetActive(true);
            embededCanvas.gameObject.SetActive(false);
        });
        embededButton.onClick.AddListener(() => {
            coloringCanvas.gameObject.SetActive(false);
            furnitureCanvas.gameObject.SetActive(false);
            embededCanvas.gameObject.SetActive(true);
        });
    }
}
