using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditDesignManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown HomeTypeDropdown;
    [SerializeField] private Button BackButton;
    [SerializeField] private Button EditButton;

    private UICanvasManager canvasManager;
    private void Start()
    {
        canvasManager = FindObjectOfType<UICanvasManager>();
        BackButton.onClick.AddListener(() =>
        {
            canvasManager.ActivateCanvas(UICanvasManager.CanvasType.MainCanvas);
        });
        EditButton.onClick.AddListener(() =>
        {
            //next scene editing
        });
        HomeTypeDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void OnDropdownValueChanged(int value)
    {
        // Get the selected option text
        string selectedOption = HomeTypeDropdown.options[value].text;

        Debug.Log($"Selected Option: {selectedOption}");

        switch (value)
        {
            case 0:
                Debug.Log("tipe rumah 36");
                break;
            case 1:
                Debug.Log("tipe rumah 45");
                break;
            case 2:
                Debug.Log("tipe rumah 54");
                break;
            case 3:
                Debug.Log("tipe rumah 60");
                break;
            default:
                Debug.Log("tidak ada di dropdown");
                break;
        }
    }
}
