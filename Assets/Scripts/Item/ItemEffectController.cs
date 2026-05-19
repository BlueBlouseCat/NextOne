using UnityEngine;

public class ItemEffectController : MonoBehaviour
{
    [Header("Optional Fallback Targets")]
    [SerializeField] private WindowController _windowController;
    [SerializeField] private GameObject _targetToEnable;
    [SerializeField] private ItemPreviewViewerUI _previewViewer;

    public bool TryUse(ItemDefinition item)
    {
        if (item == null) return false;

        Platform1BranchInteractionTrigger branchTrigger = FindObjectOfType<Platform1BranchInteractionTrigger>();
        if (branchTrigger != null && branchTrigger.CanHandleItem(item))
            return branchTrigger.TryUseItem(item);

        PetDoorKeyTrigger petDoorTrigger = FindObjectOfType<PetDoorKeyTrigger>();
        if (petDoorTrigger != null && petDoorTrigger.CanHandleItem(item))
            return petDoorTrigger.TryUseItem(item);

        BottlePartUseTrigger bottlePartTrigger = FindObjectOfType<BottlePartUseTrigger>();
        if (bottlePartTrigger != null && bottlePartTrigger.CanHandleItem(item))
            return bottlePartTrigger.TryUseItem(item);

        LockUnlockLetterTrigger[] lockTriggers = FindObjectsOfType<LockUnlockLetterTrigger>();
        for (int i = 0; i < lockTriggers.Length; i++)
        {
            LockUnlockLetterTrigger lockTrigger = lockTriggers[i];
            if (lockTrigger == null) continue;
            if (!lockTrigger.CanHandleItem(item)) continue;

            if (lockTrigger.TryUseItem(item))
                return true;
        }

        switch (item.effectType)
        {
            case ItemEffectType.OpenWindow:
            {
                WindowController windowController = ResolveWindowController();
                if (windowController == null) return false;

                windowController.OpenWindow();
                return true;
            }

            case ItemEffectType.EnableTarget:
            {
                if (_targetToEnable == null) return false;

                _targetToEnable.SetActive(true);
                return true;
            }

            case ItemEffectType.ShowPreviewImage:
            {
                ItemPreviewViewerUI previewViewer = ResolvePreviewViewer();
                if (previewViewer == null) return false;

                return previewViewer.TryOpen(item);
            }

            default:
                return false;
        }
    }

    private WindowController ResolveWindowController()
    {
        if (_windowController != null)
            return _windowController;

        _windowController = FindObjectOfType<WindowController>();
        return _windowController;
    }

    private ItemPreviewViewerUI ResolvePreviewViewer()
    {
        if (_previewViewer != null)
            return _previewViewer;

        _previewViewer = ItemPreviewViewerUI.EnsureInstance();
        return _previewViewer;
    }
}
