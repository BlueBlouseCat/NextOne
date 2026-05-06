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

    [Header("Optional Override Flag")]
    [SerializeField] private string _collectedFlagOverride = "";

    private void Awake()
    {
        if (_targetRoot == null)
            _targetRoot = gameObject;
    }

    private void Start()
    {
        RefreshVisibility();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshVisibility();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshVisibility();
    }

    [ContextMenu("Refresh Visibility")]
    public void RefreshVisibility()
    {
        if (_targetRoot == null) return;

        if (!string.IsNullOrWhiteSpace(_currentScene) &&
            SceneManager.GetActiveScene().name != _currentScene)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            _targetRoot.SetActive(true);
            return;
        }

        string collectedFlag = ResolveCollectedFlag();
        if (string.IsNullOrWhiteSpace(collectedFlag))
        {
            _targetRoot.SetActive(true);
            return;
        }

        bool alreadyCollected = GameManager.Instance.GetFlag(collectedFlag);
        _targetRoot.SetActive(!alreadyCollected);
    }

    private string ResolveCollectedFlag()
    {
        if (!string.IsNullOrWhiteSpace(_collectedFlagOverride))
            return _collectedFlagOverride;

        if (_codeBookItem != null)
            return _codeBookItem.CollectedFlag;

        return string.Empty;
    }
}
