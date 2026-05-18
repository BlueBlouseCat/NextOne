using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneUIVisibilityController : MonoBehaviour
{
    [Header("Hide In These Scenes")]
    [SerializeField] private string[] _hideInScenes = { "CG1" };

    [Header("Optional Manual Overrides")]
    [SerializeField] private GameObject _inventoryRootOverride;
    [SerializeField] private GameObject _settingsRootOverride;

    [Header("Auto Find")]
    [SerializeField] private bool _autoHideInventoryUI = true;
    [SerializeField] private bool _autoHideSettingsCanvas = true;

    [Header("Lifecycle")]
    [SerializeField] private bool _dontDestroyOnLoadSelf = false;
    [SerializeField] private bool _applyAgainNextFrame = true;

    private HiddenState _inventoryState;
    private HiddenState _settingsState;
    private Coroutine _delayedApplyRoutine;

    private struct HiddenState
    {
        public GameObject target;
        public bool hasValue;
        public bool originalActiveSelf;
    }

    private void Awake()
    {
        if (_dontDestroyOnLoadSelf)
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyForCurrentScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_delayedApplyRoutine != null)
        {
            StopCoroutine(_delayedApplyRoutine);
            _delayedApplyRoutine = null;
        }

        RestoreAll();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForCurrentScene();

        if (_applyAgainNextFrame)
        {
            if (_delayedApplyRoutine != null)
                StopCoroutine(_delayedApplyRoutine);

            _delayedApplyRoutine = StartCoroutine(ApplyAgainNextFrameRoutine());
        }
    }

    private IEnumerator ApplyAgainNextFrameRoutine()
    {
        yield return null;
        ApplyForCurrentScene();
        _delayedApplyRoutine = null;
    }

    private void ApplyForCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool shouldHide = ShouldHideInScene(currentSceneName);

        if (shouldHide)
            HideTargets();
        else
            RestoreAll();
    }

    private bool ShouldHideInScene(string sceneName)
    {
        if (_hideInScenes == null || _hideInScenes.Length == 0)
            return false;

        for (int i = 0; i < _hideInScenes.Length; i++)
        {
            string targetScene = _hideInScenes[i];
            if (string.IsNullOrWhiteSpace(targetScene))
                continue;

            if (targetScene == sceneName)
                return true;
        }

        return false;
    }

    private void HideTargets()
    {
        GameObject inventoryRoot = ResolveInventoryRoot();
        if (inventoryRoot != null)
        {
            InventoryUI inventoryUI = inventoryRoot.GetComponent<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.ShowInteractHint(false);
                inventoryUI.ClosePickupPopup();
                inventoryUI.StopSlotHint();
            }

            TrackAndHide(inventoryRoot, ref _inventoryState);
        }

        GameObject settingsRoot = ResolveSettingsRoot();
        if (settingsRoot != null)
        {
            SettingsCanvas settingsCanvas = settingsRoot.GetComponent<SettingsCanvas>();
            if (settingsCanvas != null)
                settingsCanvas.ClosedCanvas();

            TrackAndHide(settingsRoot, ref _settingsState);
        }
    }

    private void RestoreAll()
    {
        RestoreTracked(ref _inventoryState);
        RestoreTracked(ref _settingsState);
    }

    private void TrackAndHide(GameObject target, ref HiddenState state)
    {
        if (target == null)
            return;

        if (!state.hasValue || state.target != target)
        {
            RestoreTracked(ref state);

            state.target = target;
            state.originalActiveSelf = target.activeSelf;
            state.hasValue = true;
        }

        if (target.activeSelf)
            target.SetActive(false);
    }

    private void RestoreTracked(ref HiddenState state)
    {
        if (!state.hasValue)
            return;

        if (state.target != null)
            state.target.SetActive(state.originalActiveSelf);

        state.target = null;
        state.originalActiveSelf = false;
        state.hasValue = false;
    }

    private GameObject ResolveInventoryRoot()
    {
        if (_inventoryRootOverride != null)
            return _inventoryRootOverride;

        if (!_autoHideInventoryUI)
            return null;

        InventoryUI inventory = InventoryUI.Instance;
        if (IsValidSceneObject(inventory))
            return inventory.gameObject;

        InventoryUI[] all = Resources.FindObjectsOfTypeAll<InventoryUI>();
        for (int i = 0; i < all.Length; i++)
        {
            if (IsValidSceneObject(all[i]))
                return all[i].gameObject;
        }

        return null;
    }

    private GameObject ResolveSettingsRoot()
    {
        if (_settingsRootOverride != null)
            return _settingsRootOverride;

        if (!_autoHideSettingsCanvas)
            return null;

        SettingsCanvas[] all = Resources.FindObjectsOfTypeAll<SettingsCanvas>();
        for (int i = 0; i < all.Length; i++)
        {
            if (IsValidSceneObject(all[i]))
                return all[i].gameObject;
        }

        return null;
    }

    private bool IsValidSceneObject(Component component)
    {
        if (component == null)
            return false;

        GameObject go = component.gameObject;
        if (go == null)
            return false;

        return go.scene.IsValid();
    }
}