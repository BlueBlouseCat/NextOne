using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CodeBookPickupTrigger : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "MouseHole7";

    [Header("Item")]
    [SerializeField] private ItemDefinition _codeBookItem;
    [SerializeField] private ItemPreviewViewerUI _previewViewer;

    [Header("Optional")]
    [SerializeField] private GameObject _outlineRoot;

    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private bool _collected;

    private void Awake()
    {
        if (_previewViewer == null)
            _previewViewer = FindObjectOfType<ItemPreviewViewerUI>();
    }

    private void Start()
    {
        RefreshState();
    }

    private void OnEnable()
    {
        RefreshState();
    }

    private void OnDisable()
    {
        _playerInRange = false;
        HideHint();
        SetOutline(false);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            HideHint();
            return;
        }

        if (_collected)
        {
            HideHint();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsLoadingScene())
        {
            HideHint();
            return;
        }

        if (_playerInRange)
            ShowHint();
        else
            HideHint();

        if (!_playerInRange) return;
        if (!GameplayInputUtil.InteractPressedThisFrame()) return;

        PickupCodeBook();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SetPlayerInRange(other, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        SetPlayerInRange(other, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        SetPlayerInRange(other, false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        SetPlayerInRange(collision.collider, true);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        SetPlayerInRange(collision.collider, true);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        SetPlayerInRange(collision.collider, false);
    }

    private void SetPlayerInRange(Collider2D other, bool inRange)
    {
        if (other == null || !other.CompareTag("Player")) return;
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (_collected) return;

        _playerInRange = inRange;

        if (inRange)
            SetOutline(true);
        else
        {
            HideHint();
            SetOutline(false);
        }
    }

    private void PickupCodeBook()
    {
        if (_codeBookItem == null) return;
        if (InventoryManager.Instance == null) return;

        bool added = InventoryManager.Instance.TryAdd(_codeBookItem);
        if (!added)
        {
            Debug.LogWarning("CodeBookPickupTrigger: 背包已满，或该物品已存在，无法拾取 CodeBook。");
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.SetFlag(_codeBookItem.CollectedFlag, true);

        InventoryUI.Instance?.Refresh();

        _collected = true;
        _playerInRange = false;

        HideHint();
        SetOutline(false);

        if (_previewViewer != null)
            _previewViewer.TryOpen(_codeBookItem);

        gameObject.SetActive(false);
    }

    private void RefreshState()
    {
        _playerInRange = false;
        HideHint();
        SetOutline(false);

        bool alreadyCollected =
            _codeBookItem != null &&
            GameManager.Instance != null &&
            GameManager.Instance.GetFlag(_codeBookItem.CollectedFlag);

        _collected = alreadyCollected;

        if (alreadyCollected)
            gameObject.SetActive(false);
    }

    private void ShowHint()
    {
        if (_hintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.ShowInteractHint(true);
        _hintShownByThisScript = true;
    }

    private void HideHint()
    {
        if (!_hintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.ShowInteractHint(false);
        _hintShownByThisScript = false;
    }

    private void SetOutline(bool visible)
    {
        if (_outlineRoot != null)
            _outlineRoot.SetActive(visible);
    }
}
