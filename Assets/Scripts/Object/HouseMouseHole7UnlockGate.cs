using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HouseMouseHole7UnlockGate : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "House";

    [Header("Unlock Rule")]
    [SerializeField] private string _unlockWhenSpawnPointId = "hole7_to_house";
    [SerializeField] private string _unlockedFlag = "house_interactions_unlocked_from_hole7";

    [Header("Solid Colliders Disabled Before Unlock")]
    [SerializeField] private Collider2D[] _lockedSolidColliders;

    [Header("Interaction Scripts Disabled Before Unlock")]
    [SerializeField] private Behaviour[] _lockedInteractionBehaviours;

    [Header("Interaction Trigger Colliders Disabled Before Unlock")]
    [SerializeField] private Collider2D[] _lockedInteractionTriggerColliders;

    [Header("Always Enabled")]
    [SerializeField] private Behaviour[] _alwaysEnabledBehaviours;
    [SerializeField] private Collider2D[] _alwaysEnabledColliders;

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
        SetColliderStates(_lockedInteractionTriggerColliders, unlocked);
        SetColliderStates(_lockedSolidColliders, unlocked);

        SetBehaviourStates(_alwaysEnabledBehaviours, true);
        SetColliderStates(_alwaysEnabledColliders, true);

        if (!unlocked && _hideInteractHintWhenLocked && InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInteractHint(false);
    }

    private bool ResolveUnlockedState()
    {
        if (GameManager.Instance == null)
            return false;

        if (GameManager.Instance.GetFlag(_unlockedFlag))
            return true;

        bool cameFromMouseHole7 = GameManager.Instance.LastSpawnPointId == _unlockWhenSpawnPointId;
        if (!cameFromMouseHole7)
            return false;

        if (!string.IsNullOrWhiteSpace(_unlockedFlag))
            GameManager.Instance.SetFlag(_unlockedFlag, true);

        return true;
    }

    private void SetBehaviourStates(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null) return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            behaviours[i].enabled = enabled;
        }
    }

    private void SetColliderStates(Collider2D[] colliders, bool enabled)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;
            colliders[i].enabled = enabled;
        }
    }
}
