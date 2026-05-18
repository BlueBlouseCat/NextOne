using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class OutsideInteractionUnlockBySpawn : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = SceneName.Scene1; // OutsideOfHouse

    [Header("Unlock Rule")]
    [SerializeField] private string _unlockWhenSpawnPointId = "brush1_to_outside";
    [SerializeField] private string _unlockedFlag = "outside.brush1_interactions_unlocked";
    [SerializeField] private bool _unlockPermanentlyAfterFirstMatch = true;

    [Header("Locked Before Unlock")]
    [SerializeField] private Behaviour[] _lockedInteractionBehaviours;
    [SerializeField] private Collider2D[] _lockedInteractionColliders;

    [Header("Always Enabled")]
    [SerializeField] private Behaviour[] _alwaysEnabledBehaviours;
    [SerializeField] private Collider2D[] _alwaysEnabledColliders;

    [Header("Optional Visual State")]
    [SerializeField] private GameObject[] _showAfterUnlock;
    [SerializeField] private GameObject[] _hideWhileLocked;

    [Header("Options")]
    [SerializeField] private bool _applyOneFrameLater = true;
    [SerializeField] private bool _hideInteractHintWhenLocked = true;

    private Coroutine _applyRoutine;

    private void Start()
    {
        BeginApply();
    }

    private void OnEnable()
    {
        BeginApply();
    }

    private void OnDisable()
    {
        if (_applyRoutine != null)
        {
            StopCoroutine(_applyRoutine);
            _applyRoutine = null;
        }
    }

    private void BeginApply()
    {
        if (_applyRoutine != null)
            StopCoroutine(_applyRoutine);

        _applyRoutine = StartCoroutine(ApplyRoutine());
    }

    private IEnumerator ApplyRoutine()
    {
        if (_applyOneFrameLater)
            yield return null;

        ApplyState();
        _applyRoutine = null;
    }

    [ContextMenu("Apply State")]
    public void ApplyState()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
            return;

        bool unlocked = ResolveUnlockedState();

        SetBehaviourStates(_lockedInteractionBehaviours, unlocked);
        SetColliderStates(_lockedInteractionColliders, unlocked);

        SetBehaviourStates(_alwaysEnabledBehaviours, true);
        SetColliderStates(_alwaysEnabledColliders, true);

        SetGameObjectStates(_showAfterUnlock, unlocked);
        SetGameObjectStates(_hideWhileLocked, !unlocked);

        if (!unlocked && _hideInteractHintWhenLocked && InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInteractHint(false);
    }

    private bool ResolveUnlockedState()
    {
        if (GameManager.Instance == null)
            return false;

        if (_unlockPermanentlyAfterFirstMatch &&
            !string.IsNullOrWhiteSpace(_unlockedFlag) &&
            GameManager.Instance.GetFlag(_unlockedFlag))
        {
            return true;
        }

        bool cameFromRequiredSpawn =
            !string.IsNullOrWhiteSpace(_unlockWhenSpawnPointId) &&
            GameManager.Instance.LastSpawnPointId == _unlockWhenSpawnPointId;

        if (!cameFromRequiredSpawn)
            return false;

        if (_unlockPermanentlyAfterFirstMatch && !string.IsNullOrWhiteSpace(_unlockedFlag))
            GameManager.Instance.SetFlag(_unlockedFlag, true);

        return true;
    }

    private static void SetBehaviourStates(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null) return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            behaviours[i].enabled = enabled;
        }
    }

    private static void SetColliderStates(Collider2D[] colliders, bool enabled)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;
            colliders[i].enabled = enabled;
        }
    }

    private static void SetGameObjectStates(GameObject[] targets, bool active)
    {
        if (targets == null) return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;
            if (targets[i].activeSelf != active)
                targets[i].SetActive(active);
        }
    }
}
