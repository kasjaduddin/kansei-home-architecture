using Unity.VisualScripting;
using UnityEngine;

public class PreviewSystem : MonoBehaviour
{
    [SerializeField] private float previewOffset = 0.06f;

    [SerializeField] 
    private GameObject cellIndicator;
    private GameObject previewObject;

    [SerializeField] 
    private Material previewMaterialPrefab;
    private Material previewMaterialInstance;

    private Renderer cellIndicatorRenderer;
    private Vector3 bottomLeftOffset = Vector3.zero;
    private int rotationAngle = 0; // Track rotation in 90° steps
    private Vector2Int originalSize;

    private void Start()
    {
        previewMaterialInstance = new Material(previewMaterialPrefab);
        cellIndicator.SetActive(false);
        cellIndicatorRenderer = cellIndicator.GetComponentInChildren<Renderer>();
    }

    public void StartShowingPlacementPreview(GameObject prefab, Vector2Int size)
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        previewObject = Instantiate(prefab);
        previewObject.SetActive(true);

        originalSize = size; // Store unrotated size
        CalculatePivotOffset(previewObject);

        PreparePreview(previewObject);
        UpdatePreviewSize(); // Adjust preview for rotation
        cellIndicator.SetActive(true);

        rotationAngle = 0; // Reset rotation
    }

    private void CalculatePivotOffset(GameObject obj)
    {
        bottomLeftOffset = Vector3.zero;
        Renderer objectRenderer = obj.GetComponentInChildren<Renderer>();
        if (objectRenderer != null)
        {
            float rotationY = obj.transform.eulerAngles.y;
            print("rotasinya bang " + rotationY);
            bottomLeftOffset = objectRenderer.bounds.min - obj.transform.position;
        }
    }
    private void UpdatePreviewSize()
    {
        Vector2Int adjustedSize = GetRotatedSize();
        cellIndicator.transform.localScale = new Vector3(adjustedSize.x, 1, adjustedSize.y);
    }
    private Vector2Int GetRotatedSize()
    {
        return (rotationAngle % 180 == 0) ? originalSize : new Vector2Int(originalSize.y, originalSize.x);
    }
    private void PrepareCursor(Vector2Int size)
    {
        if (size.x > 0 || size.y > 0)
        {
            cellIndicator.transform.localScale = new Vector3(size.x,1,size.y);
        }
    }

    private void PreparePreview(GameObject previewObject)
    {
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = previewMaterialInstance;
            }
            renderer.materials = materials;
        }
    }
    /* private void PreparePreview(GameObject previewObject)
     {
         Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
         foreach (Renderer renderer in renderers)
         {
             renderer.sharedMaterial = previewMaterialInstance; 
         }
     }*/

    public void StopShowingPreview()
    {
        cellIndicator.SetActive(false);
        if (previewObject != null)
            Destroy(previewObject);
    }

    public void UpdatePosition(Vector3 position, bool validity)
    {
        if (previewObject != null)
        {
            MovePreview(position);
            ApplyFeedbackToPreview(validity);
        }
        
        MoveCursor(position);
        ApplyFeedbackToCursor(validity);
    }

    private void ApplyFeedbackToPreview(bool validity)
    {
        Color c = validity? Color.white : Color.red;
        c.a = 0.5f;
        previewMaterialInstance.color = c;
    }
    private void ApplyFeedbackToCursor(bool validity)
    {
        Color c = validity ? Color.white : Color.red;
        c.a = 0.5f;
        cellIndicatorRenderer.material.color = c;
    }

    private void MoveCursor(Vector3 position)
    {
        cellIndicator.transform.position = position;
    }

    private void MovePreview(Vector3 position)
    {
        previewObject.transform.position = position - bottomLeftOffset + Vector3.up * previewOffset;
        previewObject.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
    }
    public void RotateLeft()
    {
        rotationAngle -= 90;
        if (previewObject != null)
        {
            previewObject.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
            UpdatePreviewSize();
            CalculatePivotOffset(previewObject);
        }
    }

    public void RotateRight()
    {
        rotationAngle += 90;
        if (previewObject != null)
        {
            previewObject.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
            UpdatePreviewSize();
            CalculatePivotOffset(previewObject);
        }
    }

    internal void StartShowingRemovePreview()
    {
        cellIndicator.SetActive(true);
        PrepareCursor(Vector2Int.one);
        ApplyFeedbackToCursor(false);
    }

    public Quaternion GetRotation()
    {
        return Quaternion.Euler(0, rotationAngle, 0);
    }

    public Vector2Int GetAdjustedSize()
    {
        return GetRotatedSize();
    }
}