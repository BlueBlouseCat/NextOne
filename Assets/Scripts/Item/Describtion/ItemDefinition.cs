using UnityEngine;

public enum ItemEffectType
{
    None,
    OpenWindow,
    EnableTarget,
    ShowPreviewImage
}

[CreateAssetMenu(menuName = "Scriptable Objects/Item Defination")]
public class ItemDefinition : ScriptableObject
{
    public string itemId;
    public string displayName;
    [TextArea(3, 6)] public string description;
    public Sprite icon;

    [Header("Preview")]
    public Sprite previewSprite;
    public Sprite previewOverlaySprite;
    public Vector2 previewOverlayOffset = new Vector2(140f, -40f);
    public Vector2 previewOverlaySizeMultiplier = Vector2.one;
    [Range(0f, 1f)] public float previewOverlayAlpha = 1f;

    [Range(0, 2)] public int slotIndex;
    public bool consumeOnUse = true;
    public string collectedFlagKey;
    public ItemEffectType effectType;

    public string CollectedFlag =>
        string.IsNullOrWhiteSpace(collectedFlagKey)
            ? $"item.{itemId}.collected"
            : collectedFlagKey;
}
