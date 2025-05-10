using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreviewGeneratorManager : MonoBehaviour
{
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private GameObject parentObject;
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private Vector3 spawnRotation = new Vector3(0, 180, 0);
    [SerializeField] private Vector3 spawnScale = Vector3.one;
    [SerializeField] private LayerMask previewLayerMask;

    private GameObject currentPreviewObject;

    private void Awake()
    {
        // Ensure camera is properly configured
        if (previewCamera != null)
        {
            previewCamera.enabled = false; // We'll use manual rendering
            previewCamera.targetTexture = renderTexture;
            previewCamera.cullingMask = previewLayerMask;
        }
    }

    public void GeneratePreview(GameObject prefab, RawImage targetImage)
    {
        if (previewCamera == null || renderTexture == null || parentObject == null)
        {
            Debug.LogError("PreviewGeneratorManager is missing critical references!");
            return;
        }

        // Clean up previous preview object
        if (currentPreviewObject != null)
        {
            Destroy(currentPreviewObject);
        }

        // Instantiate the object under the parentObject
        currentPreviewObject = Instantiate(prefab, parentObject.transform);
        currentPreviewObject.transform.localPosition = spawnPosition;
        currentPreviewObject.transform.localRotation = Quaternion.Euler(spawnRotation);
        currentPreviewObject.transform.localScale = spawnScale;

        // Set layer for all parts of the object
        SetLayerRecursively(currentPreviewObject, LayerMask.NameToLayer("PreviewLayer"));

        // Wait for end of frame to ensure everything is ready
        StartCoroutine(RenderPreviewAfterFrame(targetImage));
    }

    private IEnumerator RenderPreviewAfterFrame(RawImage targetImage)
    {
        // Wait for the end of the frame to ensure all transforms are updated
        yield return new WaitForEndOfFrame();

        // Render the preview
        previewCamera.Render();

        // Assign the RenderTexture to the RawImage
        if (targetImage != null)
        {
            targetImage.texture = renderTexture;
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}