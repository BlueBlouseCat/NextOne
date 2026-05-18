using UnityEngine;
using UnityEngine.SceneManagement;

public class OutsideCatsActivator : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = SceneName.Scene1; // OutsideOfHouse

    [Header("Cats Root")]
    [SerializeField] private GameObject _catsRoot;

    [Header("Flags")]
    [SerializeField] private string _keyCollectedFlag = "item.house_key.collected";
    [SerializeField] private string _catsActivatedFlag = "outside_cats_activated";

    private void Start()
    {
        RefreshVisibility();
    }

    private void OnEnable()
    {
        RefreshVisibility();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
            return;

        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.IsLoadingScene())
            return;

        TryUnlockCats();
        RefreshVisibility();
    }

    private void TryUnlockCats()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.GetFlag(_catsActivatedFlag))
            return;

        if (string.IsNullOrWhiteSpace(_keyCollectedFlag))
            return;

        bool hasCollectedKey = GameManager.Instance.GetFlag(_keyCollectedFlag);
        if (!hasCollectedKey)
            return;

        GameManager.Instance.SetFlag(_catsActivatedFlag, true);
    }

    private void RefreshVisibility()
    {
        if (_catsRoot == null)
            return;

        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            _catsRoot.SetActive(false);
            return;
        }

        if (GameManager.Instance == null)
        {
            _catsRoot.SetActive(false);
            return;
        }

        bool shouldShowCats =
            GameManager.Instance.GetFlag(_catsActivatedFlag) ||
            (!string.IsNullOrWhiteSpace(_keyCollectedFlag) && GameManager.Instance.GetFlag(_keyCollectedFlag));

        _catsRoot.SetActive(shouldShowCats);
    }
}
