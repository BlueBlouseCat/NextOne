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

    [Range(0, 2)] public int slotIndex;
    public bool consumeOnUse = true;
    public string collectedFlagKey;
    public ItemEffectType effectType;

    public string CollectedFlag =>
        string.IsNullOrWhiteSpace(collectedFlagKey)
            ? $"item.{itemId}.collected"
            : collectedFlagKey;
}
