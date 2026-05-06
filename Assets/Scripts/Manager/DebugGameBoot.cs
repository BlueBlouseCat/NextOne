using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugGameBoot : MonoBehaviour
{
    private const string MouseHole1PaintingPuzzleSolvedFlag = "mousehole1_painting_puzzle_solved";
    private const string MouseHole2MouseDialogueDoneFlag = "mouse_blocked_dialogue_done";
    private const string MouseHole7HankySequenceDoneFlag = "mousehole7_hanky_sequence_done";
    private const string HankyCollectedFlag = "item.hanky.collected";

#if UNITY_EDITOR
    private const string HankyAssetPath = "Assets/Scripts/Item/Describtion/Hanky.asset";
    private const string CodeBookAssetPath = "Assets/Scripts/Item/Describtion/CodeBook.asset";
#endif

    [Header("Enable")]
    [SerializeField] private bool _enableDebugBoot = true;

    [Header("Scene")]
    [SerializeField] private bool _loadTargetSceneOnStart = false;
    [SerializeField] private string _targetSceneName = "OutsideOfHouse";
    [SerializeField] private string _targetSpawnPointId = "";

    [Header("Flags")]
    [SerializeField] private bool _setWindowOpened = false;
    [SerializeField] private bool _setCrowIntroDone = false;
    [SerializeField] private bool _setCrowDeliveryDone = false;
    [SerializeField] private bool _setPlatformBranchUsed = false;
    [SerializeField] private bool _setHouseKeyCollected = false;
    [SerializeField] private bool _setEnteredBrushAfterKey = false;
    [SerializeField] private bool _setOutsideCatsActivated = false;
    [SerializeField] private bool _setMotherCatDialogueDone = false;

    [Header("MouseHole Progress")]
    [SerializeField] private bool _setMouseHole1PaintingPuzzleSolved = true;
    [SerializeField] private bool _setMouseHole2MouseDialogueDone = true;
    [SerializeField] private bool _setHankyCollected = true;
    [SerializeField] private bool _setCodeBookCollected = true;

    [Header("Optional Inventory")]
    [SerializeField] private InventoryManager _inventoryManager;
    [SerializeField] private ItemDefinition _hankyItem;
    [SerializeField] private ItemDefinition _codeBookItem;
    [SerializeField] private ItemDefinition[] _itemsToGive;

    private bool _hasApplied;

    private void Awake()
    {
        if (!_enableDebugBoot) return;
        ApplyDebugState();
    }

    private void Start()
    {
        if (!_enableDebugBoot) return;

        if (_loadTargetSceneOnStart)
        {
            if (GameManager.Instance == null) return;

            if (string.IsNullOrWhiteSpace(_targetSpawnPointId))
                GameManager.Instance.LoadScene(_targetSceneName);
            else
                GameManager.Instance.LoadScene(_targetSceneName, _targetSpawnPointId);
        }
    }

    private void ApplyDebugState()
    {
        if (_hasApplied) return;
        _hasApplied = true;

        if (GameManager.Instance != null)
        {
            SetFlag("outside_window_open", _setWindowOpened);
            SetFlag("crow_intro_dialogue_done", _setCrowIntroDone);
            SetFlag("crow_delivery_done", _setCrowDeliveryDone);
            SetFlag("platform1_branch_used", _setPlatformBranchUsed);
            SetFlag("item.house_key.collected", _setHouseKeyCollected);
            SetFlag("entered_brush_after_key", _setEnteredBrushAfterKey);
            SetFlag("outside_cats_activated", _setOutsideCatsActivated);
            SetFlag("mother_cat_dialogue_done", _setMotherCatDialogueDone);
            SetFlag(MouseHole1PaintingPuzzleSolvedFlag, _setMouseHole1PaintingPuzzleSolved);
            SetFlag(MouseHole2MouseDialogueDoneFlag, _setMouseHole2MouseDialogueDone);
            SetFlag(MouseHole7HankySequenceDoneFlag, _setHankyCollected);

            if (_setHankyCollected)
                SetFlag(HankyCollectedFlag, true);
        }

        ResolveDebugItemReferences();
        ApplyMouseHoleInventoryState();
        GiveDebugItems();
    }

    private void SetFlag(string key, bool value)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.SetFlag(key, value);
    }

    private void GiveDebugItems()
    {
        if (_itemsToGive == null || _itemsToGive.Length == 0) return;

        if (_inventoryManager == null)
            _inventoryManager = InventoryManager.Instance;

        if (_inventoryManager == null) return;

        for (int i = 0; i < _itemsToGive.Length; i++)
        {
            ItemDefinition item = _itemsToGive[i];
            if (item == null) continue;

            GiveItemIfMissing(item);
        }
    }

    private void ApplyMouseHoleInventoryState()
    {
        if (_setHankyCollected)
            GiveItemIfMissing(_hankyItem);

        if (_setCodeBookCollected)
            GiveItemIfMissing(_codeBookItem);

        if (GameManager.Instance != null && _setCodeBookCollected && _codeBookItem != null)
            GameManager.Instance.SetFlag(_codeBookItem.CollectedFlag, true);
    }

    private void GiveItemIfMissing(ItemDefinition item)
    {
        if (item == null) return;

        if (_inventoryManager == null)
            _inventoryManager = InventoryManager.Instance;

        if (_inventoryManager == null) return;

        if (!string.IsNullOrWhiteSpace(item.itemId) && _inventoryManager.HasItem(item.itemId))
            return;

        _inventoryManager.TryAdd(item);
    }

    private void ResolveDebugItemReferences()
    {
#if UNITY_EDITOR
        if (_hankyItem == null)
            _hankyItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(HankyAssetPath);

        if (_codeBookItem == null)
            _codeBookItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(CodeBookAssetPath);
#endif
    }
}
