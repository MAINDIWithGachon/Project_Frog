using UnityEngine;

[ExecuteAlways]
public class ScaleByScreenHeightUI : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float referenceWidth = 1080f;
    [SerializeField] private Vector3 baseScale = Vector3.one;
    [SerializeField] private bool useCanvasWidthWhenAvailable = true;

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private float lastMeasuredWidth = -1f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

        ApplyScale(force: true);
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

        ApplyScale(force: true);
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyScale(force: true);
    }

    private void Update()
    {
        ApplyScale(force: false);
    }

    private void ApplyScale(bool force)
    {
        if (referenceWidth <= 0f)
            return;

        float currentWidth = GetCurrentWidth();
        if (currentWidth <= 0f)
            return;

        if (!force && Mathf.Approximately(currentWidth, lastMeasuredWidth))
            return;

        float scaleRatio = currentWidth / referenceWidth;
        transform.localScale = new Vector3(
            baseScale.x * scaleRatio,
            baseScale.y * scaleRatio,
            baseScale.z);
        lastMeasuredWidth = currentWidth;
    }

    public void CaptureCurrentScaleAsBase()
    {
        float currentWidth = GetCurrentWidth();
        if (referenceWidth <= 0f || currentWidth <= 0f)
            return;

        float scaleRatio = currentWidth / referenceWidth;
        if (Mathf.Approximately(scaleRatio, 0f))
            return;

        Vector3 currentScale = transform.localScale;
        baseScale = new Vector3(
            currentScale.x / scaleRatio,
            currentScale.y / scaleRatio,
            currentScale.z);
        ApplyScale(force: true);
    }

    private float GetCurrentWidth()
    {
        if (useCanvasWidthWhenAvailable && rootCanvas != null)
        {
            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect != null)
                return canvasRect.rect.width;
        }

        if (rectTransform != null && rectTransform.rect.width > 0f)
            return rectTransform.rect.width;

        return Screen.width;
    }
}
