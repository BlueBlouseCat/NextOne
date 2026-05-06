using UnityEngine;
using UnityEngine.SceneManagement;

public class OutsideCatsActivator : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = SceneName.Scene1; // OutsideOfHouse

    [Header("Cats Root")]
    [SerializeField] private GameObject _catsRoot;

    [Header("Flags")]
    [SerializeField] private string _enteredBrushAfterKeyFlag = "entered_brush_after_key";
    [SerializeField] private string _catsActivatedFlag = "outside_cats_activated";

    [Header("Valid Return Spawn Points")]
    [SerializeField] private string _brushReturnSpawnPointId = "brush_to_outside";
    [SerializeField] private string _brush1ReturnSpawnPointId = "brush1_to_outside";

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
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;

        TryUnlockCats();
        RefreshVisibility();
    }

    private void TryUnlockCats()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.GetFlag(_catsActivatedFlag)) return;
        if (!GameManager.Instance.GetFlag(_enteredBrushAfterKeyFlag)) return;

        string lastSpawnPointId = GameManager.Instance.LastSpawnPointId;
        bool returnedFromBrush =
            lastSpawnPointId == _brushReturnSpawnPointId ||
            lastSpawnPointId == _brush1ReturnSpawnPointId;

        if (!returnedFromBrush) return;

        GameManager.Instance.SetFlag(_catsActivatedFlag, true);
    }

    private void RefreshVisibility()
    {
        if (_catsRoot == null) return;

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

        bool shouldShowCats = GameManager.Instance.GetFlag(_catsActivatedFlag);
        _catsRoot.SetActive(shouldShowCats);
    }
}
