using UnityEngine;
using UnityEngine.SceneManagement;

public class PetDoorKeyTrigger : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = SceneName.Scene1; // OutsideOfHouse

    [Header("Required Item")]
    [SerializeField] private ItemDefinition _requiredKeyItem;

    [Header("Require Cats Visible")]
    [SerializeField] private GameObject _catsRoot;
    [SerializeField] private string _catsUnlockedFlag = "outside_cats_activated";

    [Header("One-time State")]
    [SerializeField] private string _doorOpenedFlag = "pet_door_opened";

    [Header("After Use")]
    [SerializeField] private GameObject _aseRoot;
    [SerializeField] private bool _consumeKeyOnUse = false;

    [Header("Scene Load")]
    [SerializeField] private string _targetScene = "House";
    [SerializeField] private bool _useFade = true;
    [SerializeField] private string _targetSpawnPointId = "";

    private bool _playerInRange;
    private bool _slotHintShownByThisScript;
    private bool _hasCompleted;

    private void OnEnable()
    {
        _hasCompleted = GameManager.Instance != null && GameManager.Instance.GetFlag(_doorOpenedFlag);
        HideSlotHint();
        RefreshASeState();
    }

    private void OnDisable()
    {
        HideSlotHint();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            HideSlotHint();
            return;
        }

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;

        if (!_hasCompleted)
            _hasCompleted = GameManager.Instance.GetFlag(_doorOpenedFlag);

        RefreshASeState();

        if (_hasCompleted)
        {
            HideSlotHint();
            return;
        }

        if (!_playerInRange)
        {
            HideSlotHint();
            return;
        }

        if (!AreCatsReady())
        {
            HideSlotHint();
            return;
        }

        if (_requiredKeyItem == null || InventoryManager.Instance == null)
        {
            HideSlotHint();
            return;
        }

        bool hasKey = InventoryManager.Instance.HasItem(_requiredKeyItem.itemId);

        if (hasKey)
            ShowSlotHint();
        else
            HideSlotHint();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInRange = false;
        HideSlotHint();
    }

    public bool CanHandleItem(ItemDefinition item)
    {
        if (item == null) return false;
        if (_requiredKeyItem == null) return false;

        return item.itemId == _requiredKeyItem.itemId;
    }

    public bool TryUseItem(ItemDefinition item)
    {
        if (!CanHandleItem(item)) return false;
        if (SceneManager.GetActiveScene().name != _currentScene) return false;
        if (GameManager.Instance == null || GameManager.Instance.IsLoadingScene()) return false;
        if (_hasCompleted || GameManager.Instance.GetFlag(_doorOpenedFlag)) return false;
        if (!_playerInRange) return false;
        if (!AreCatsReady()) return false;

        ExecuteInteraction(item);
        return true;
    }

    private void ExecuteInteraction(ItemDefinition item)
    {
        if (_consumeKeyOnUse && InventoryManager.Instance != null)
        {
            bool consumed = InventoryManager.Instance.TryConsumeItem(item);
            if (!consumed)
                return;
        }

        _hasCompleted = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_doorOpenedFlag))
            GameManager.Instance.SetFlag(_doorOpenedFlag, true);

        HideSlotHint();
        RefreshASeState();
        LoadTargetScene();
    }

    private bool AreCatsReady()
    {
        if (GameManager.Instance == null) return false;
        if (!GameManager.Instance.GetFlag(_catsUnlockedFlag)) return false;
        if (_catsRoot == null) return true;

        return _catsRoot.activeInHierarchy;
    }

    private void RefreshASeState()
    {
        if (_aseRoot == null) return;

        bool shouldShow = GameManager.Instance != null && GameManager.Instance.GetFlag(_doorOpenedFlag);
        _aseRoot.SetActive(shouldShow);
    }

    private void LoadTargetScene()
    {
        if (GameManager.Instance == null) return;
        if (string.IsNullOrWhiteSpace(_targetScene)) return;

        if (_useFade)
        {
            if (string.IsNullOrWhiteSpace(_targetSpawnPointId))
                GameManager.Instance.LoadSceneWithFade(_targetScene);
            else
                GameManager.Instance.LoadSceneWithFade(_targetScene, _targetSpawnPointId);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_targetSpawnPointId))
                GameManager.Instance.LoadScene(_targetScene);
            else
                GameManager.Instance.LoadScene(_targetScene, _targetSpawnPointId);
        }
    }

    private int GetCurrentRequiredItemSlot()
    {
        if (_requiredKeyItem == null) return -1;
        if (InventoryManager.Instance == null) return -1;

        return InventoryManager.Instance.FindSlotIndexByItemId(_requiredKeyItem.itemId);
    }

    private void ShowSlotHint()
    {
        int slot = GetCurrentRequiredItemSlot();
        if (slot < 0) return;

        if (_slotHintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.PlaySlotHint(slot);
        _slotHintShownByThisScript = true;
    }

    private void HideSlotHint()
    {
        if (!_slotHintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.StopSlotHint();
        _slotHintShownByThisScript = false;
    }
}
