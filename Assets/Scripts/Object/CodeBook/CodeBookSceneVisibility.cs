using UnityEngine;
using UnityEngine.SceneManagement;

public class CodeBookSceneVisibility : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "House";

    [Header("CodeBook Item")]
    [SerializeField] private ItemDefinition _codeBookItem;

    [Header("Target Root")]
    [SerializeField] private GameObject _targetRoot;

    private bool _subscribedInventoryChanged;

    private void Awake()
    {
        if (_targetRoot == null)
            _targetRoot = gameObject;
    }

    private void Start()
    {
        TrySubscribeInventoryChanged();
        RefreshVisibility();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TrySubscribeInventoryChanged();
        RefreshVisibility();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeInventoryChanged();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySubscribeInventoryChanged();
        RefreshVisibility();
    }

    private void TrySubscribeInventoryChanged()
    {
        if (_subscribedInventoryChanged) return;
        if (InventoryManager.Instance == null) return;

        InventoryManager.Instance.OnInventoryChanged += RefreshVisibility;
        _subscribedInventoryChanged = true;
    }

    private void UnsubscribeInventoryChanged()
    {
        if (!_subscribedInventoryChanged) return;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshVisibility;

        _subscribedInventoryChanged = false;
    }

    [ContextMenu("Refresh Visibility")]
    public void RefreshVisibility()
    {
        if (_targetRoot == null) return;
        if (!IsInCurrentScene()) return;

        bool inInventory = IsCodeBookInInventory();
        _targetRoot.SetActive(!inInventory);
    }

    private bool IsInCurrentScene()
    {
        if (string.IsNullOrWhiteSpace(_currentScene))
            return true;

        return SceneManager.GetActiveScene().name == _currentScene;
    }

    private bool IsCodeBookInInventory()
    {
        if (_codeBookItem == null)
            return false;

        if (InventoryManager.Instance == null)
            return false;

        if (string.IsNullOrWhiteSpace(_codeBookItem.itemId))
        {
            Debug.LogWarning("CodeBookSceneVisibility: _codeBookItem.itemId 为空，无法根据背包判断 CodeBook 显隐。");
            return false;
        }

        return InventoryManager.Instance.HasItem(_codeBookItem.itemId);
    }
}