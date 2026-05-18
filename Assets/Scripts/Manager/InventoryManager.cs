using System;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private readonly ItemDefinition[] _slots = new ItemDefinition[3];
    public event Action OnInventoryChanged;

    public int SlotCount => _slots.Length;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public ItemDefinition GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Length) return null;
        return _slots[index];
    }

    public bool HasItem(string itemId)
    {
        return FindSlotIndexByItemId(itemId) >= 0;
    }

    public int FindSlotIndexByItemId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return -1;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && _slots[i].itemId == itemId)
                return i;
        }

        return -1;
    }

    public int FindFirstEmptySlot()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
                return i;
        }

        return -1;
    }

    public bool TryAdd(ItemDefinition item)
    {
        if (item == null) return false;

        if (!string.IsNullOrWhiteSpace(item.itemId) && HasItem(item.itemId))
            return false;

        int emptySlot = FindFirstEmptySlot();
        if (emptySlot < 0) return false;

        _slots[emptySlot] = item;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryConsumeItem(ItemDefinition item)
    {
        if (item == null) return false;
        return TryConsumeItem(item.itemId);
    }

    public bool TryConsumeItem(string itemId)
    {
        int slotIndex = FindSlotIndexByItemId(itemId);
        if (slotIndex < 0) return false;

        _slots[slotIndex] = null;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void TryUseSlot(int slotIndex)
    {
        ItemDefinition item = GetSlot(slotIndex);
        if (item == null) return;

        ItemEffectController effectController = FindObjectOfType<ItemEffectController>();
        if (effectController == null) return;

        bool used = effectController.TryUse(item);
        if (!used) return;

        if (item.consumeOnUse)
        {
            _slots[slotIndex] = null;
            OnInventoryChanged?.Invoke();
        }
    }

    public void ClearAll()
    {
        for (int i = 0; i < _slots.Length; i++)
            _slots[i] = null;

        OnInventoryChanged?.Invoke();
    }
}
