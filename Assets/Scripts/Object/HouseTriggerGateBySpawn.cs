using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HouseTriggerGateBySpawn : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "House";

    [Header("Gate Rule")]
    [SerializeField] private bool _gateOnFirstVisitOnly = true;
    [SerializeField] private string _firstVisitConsumedFlag = "house_first_visit_interaction_consumed";

    [Header("Legacy Spawn Rule")]
    [SerializeField] private string _enableWhenSpawnPointId = "hole7_to_house";

    [Header("Targets")]
    [SerializeField] private GameObject[] _gatedRoots;
    [SerializeField] private GameObject[] _alwaysEnabledRoots;

    [Header("Options")]
    [SerializeField] private bool _includeInactiveChildren = true;

    // 关键：永远保留桌椅柜这类实体碰撞，只控制 trigger 交互碰撞
    [SerializeField] private bool _preserveSolidColliders = true;

    private Coroutine _applyRoutine;
    private bool _hasResolvedEntryState;
    private bool _shouldGateInteractionsThisEntry;

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

        _hasResolvedEntryState = false;
    }

    private void BeginApply()
    {
        if (_applyRoutine != null)
            StopCoroutine(_applyRoutine);

        _applyRoutine = StartCoroutine(ApplyNextFrameRoutine());
    }

    private IEnumerator ApplyNextFrameRoutine()
    {
        yield return null;
        ResolveEntryState();
        ApplyState();
        _applyRoutine = null;
    }

    private void ResolveEntryState()
    {
        if (_hasResolvedEntryState) return;
        if (SceneManager.GetActiveScene().name != _currentScene) return;

        if (_gateOnFirstVisitOnly)
        {
            bool isFirstVisit = true;

            if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_firstVisitConsumedFlag))
                isFirstVisit = !GameManager.Instance.GetFlag(_firstVisitConsumedFlag);

            _shouldGateInteractionsThisEntry = isFirstVisit;

            if (isFirstVisit && GameManager.Instance != null && !string.IsNullOrWhiteSpace(_firstVisitConsumedFlag))
                GameManager.Instance.SetFlag(_firstVisitConsumedFlag, true);
        }
        else
        {
            bool shouldEnableGatedRoots =
                GameManager.Instance != null &&
                GameManager.Instance.LastSpawnPointId == _enableWhenSpawnPointId;

            _shouldGateInteractionsThisEntry = !shouldEnableGatedRoots;
        }

        _hasResolvedEntryState = true;
    }

    [ContextMenu("Apply State")]
    public void ApplyState()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
            return;

        ResolveEntryState();

        bool enableGatedInteractions = !_shouldGateInteractionsThisEntry;

        SetRootsInteractionEnabled(_gatedRoots, enableGatedInteractions);
        SetRootsInteractionEnabled(_alwaysEnabledRoots, true);
    }

    private void SetRootsInteractionEnabled(GameObject[] roots, bool enabled)
    {
        if (roots == null) return;

        HashSet<Collider2D> visited = new HashSet<Collider2D>();

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null) continue;

            Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(_includeInactiveChildren);
            for (int j = 0; j < colliders.Length; j++)
            {
                Collider2D col = colliders[j];
                if (col == null) continue;
                if (!visited.Add(col)) continue;

                // 保留桌子/椅子/柜子这类实体碰撞体
                if (_preserveSolidColliders && !col.isTrigger)
                    continue;

                col.enabled = enabled;
            }
        }
    }
}
