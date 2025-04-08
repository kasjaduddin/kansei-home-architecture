using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesignSelectionManager : MonoBehaviour
{
    public static DesignSelectionManager Instance { get; private set; }

    public GameObject SelectedDesignPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
