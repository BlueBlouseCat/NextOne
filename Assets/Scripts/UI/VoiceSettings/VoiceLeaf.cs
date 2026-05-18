using UnityEngine;
using UnityEngine.EventSystems;

public class VoiceLeaf : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform leftBound;
    [SerializeField] private RectTransform rightBound;
    [SerializeField] private RectTransform leftPercentRef;
    [SerializeField] private RectTransform rightPercentRef;
    [SerializeField] private RectTransform pivotImage;
    [SerializeField] private UnityEngine.UI.Image fillImage;
    [SerializeField] private UnityEngine.UI.Image unfillImage;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private float minX;
    private float maxX;
    private float percentLeftX;
    private float percentRightX;

    public float Percent;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent as RectTransform;

        Canvas.ForceUpdateCanvases();
        RefreshBounds();
        UpdatePercentAndFill();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RefreshBounds();
    }

    private void RefreshBounds()
    {
        minX = parentRect.InverseTransformPoint(leftBound.position).x;
        maxX = parentRect.InverseTransformPoint(rightBound.position).x;

        percentLeftX = leftPercentRef != null
            ? parentRect.InverseTransformPoint(leftPercentRef.position).x
            : minX;
        percentRightX = rightPercentRef != null
            ? parentRect.InverseTransformPoint(rightPercentRef.position).x
            : maxX;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localNow, localPrev;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, eventData.pressEventCamera, out localNow);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position - eventData.delta, eventData.pressEventCamera, out localPrev);

        float deltaX = localNow.x - localPrev.x;
        float newX = Mathf.Clamp(rectTransform.anchoredPosition.x + deltaX, minX, maxX);
        rectTransform.anchoredPosition = new Vector2(newX, rectTransform.anchoredPosition.y);
        UpdatePercentAndFill();
    }

    private void UpdatePercentAndFill()
    {
        Percent = GetPercent();
        if (fillImage != null) fillImage.fillAmount = Percent;
        if (unfillImage != null) unfillImage.fillAmount = 1f - Percent;
    }

    private float GetPercent()
    {
        if (Mathf.Approximately(percentRightX, percentLeftX)) return 0f;
        float pivotX = parentRect.InverseTransformPoint(pivotImage.position).x;
        return Mathf.InverseLerp(percentLeftX, percentRightX, pivotX);
    }
}
