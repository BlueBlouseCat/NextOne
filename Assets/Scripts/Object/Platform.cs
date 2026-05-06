using UnityEngine;

public class Platform : MonoBehaviour
{
    [SerializeField] private Collider2D _targetCollider;
    [SerializeField] private string _requiredSpawnPointId = "brush1_to_outside";

    private void Awake()
    {
        if (_targetCollider == null)
            _targetCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        RefreshColliderState();
    }

    private void OnEnable()
    {
        RefreshColliderState();
    }

    private void RefreshColliderState()
    {
        if (_targetCollider == null) return;

        bool shouldEnable =
            GameManager.Instance != null &&
            GameManager.Instance.LastSpawnPointId == _requiredSpawnPointId;

        _targetCollider.enabled = shouldEnable;
    }
}
