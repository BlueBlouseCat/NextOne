using UnityEngine;

public class InventoryHintTrigger : MonoBehaviour
{
    [SerializeField] private ItemDefinition _item;
    [SerializeField] private bool _onlyOnce = true; // 是否只触发一次
    [SerializeField] private string _onceFlagKey;   // 只触发一次的存档标记

     private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_item == null) return;
        if (InventoryManager.Instance == null || InventoryUI.Instance == null) return;

        if (_onlyOnce && GameManager.Instance != null && GameManager.Instance.GetFlag(_onceFlagKey))
            return;

        if (!InventoryManager.Instance.HasItem(_item.itemId))
            return;

        InventoryUI.Instance.PlaySlotHint(_item.slotIndex);

        if (_onlyOnce && GameManager.Instance != null && !string.IsNullOrWhiteSpace(_onceFlagKey))
            GameManager.Instance.SetFlag(_onceFlagKey, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        InventoryUI.Instance?.StopSlotHint();
    }
}
