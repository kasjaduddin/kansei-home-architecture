using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Preview2DDesignManager : MonoBehaviour
{
    [SerializeField] private Image HomeDesignImage;


    public void Initialize()
    {
        HomeDesignParentManager parentManager = FindObjectOfType<HomeDesignParentManager>();
        if (parentManager != null)
        {
            HomeStructureManager homeManager = parentManager.GetCurrentHomeStructureManager();
            if (homeManager != null)
            {
                UpdateProperty(homeManager.previewSprite);
            }

        }
    }

    public void UpdateProperty(Sprite image)
    {
        if (image != null)
        {
            HomeDesignImage.sprite = image;
        }
    }
}
