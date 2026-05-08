using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class WorldItemUnlockByFlag : MonoBehaviour
{
    [Header("Optional scene limit")]
    [SerializeField] private string currentScene = "OutsideOfHouse";

    [Header("Unlock condition")]
    [SerializeField] private string unlockFlag;

    [Header("Target item")]
    [SerializeField] private WorldItemInteractable targetInteractable;
    [SerializeField] private bool lockInteractableBeforeUnlock = true;

    [Header("Optional visuals")]
    [SerializeField] private GameObject[] showAfterUnlock;
    [SerializeField] private GameObject[] hideWhileLocked;

    private bool _initialized;
    private bool _lastUnlocked;

    private void Reset()
    {
        targetInteractable = GetComponentInChildren<WorldItemInteractable>(true);
    }

    private void Awake()
    {
        ResolveReferences();
        Refresh(force: true);
    }

    private void OnEnable()
    {
        Refresh(force: true);
    }

    private void Start()
    {
        Refresh(force: true);
    }

    private void Update()
    {
        Refresh(force: false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetInteractable == null)
        {
            targetInteractable = GetComponentInChildren<WorldItemInteractable>(true);
        }
    }
#endif

    private void ResolveReferences()
    {
        if (targetInteractable == null)
        {
            targetInteractable = GetComponentInChildren<WorldItemInteractable>(true);
        }
    }

    private void Refresh(bool force)
    {
        if (!IsInTargetScene())
        {
            return;
        }

        bool unlocked = IsUnlocked();

        if (!force && _initialized && unlocked == _lastUnlocked)
        {
            return;
        }

        _initialized = true;
        _lastUnlocked = unlocked;

        if (lockInteractableBeforeUnlock && targetInteractable != null)
        {
            if (targetInteractable.enabled != unlocked)
            {
                targetInteractable.enabled = unlocked;
            }
        }

        SetObjectsActive(showAfterUnlock, unlocked);
        SetObjectsActive(hideWhileLocked, !unlocked);
    }

    private bool IsInTargetScene()
    {
        if (string.IsNullOrWhiteSpace(currentScene))
        {
            return true;
        }

        return SceneManager.GetActiveScene().name == currentScene;
    }

    private bool IsUnlocked()
    {
        if (string.IsNullOrWhiteSpace(unlockFlag))
        {
            return true;
        }

        if (GameManager.Instance == null)
        {
            return false;
        }

        return GameManager.Instance.GetFlag(unlockFlag);
    }

    private static void SetObjectsActive(GameObject[] targets, bool active)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject go = targets[i];
            if (go == null)
            {
                continue;
            }

            if (go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }
    }
}
