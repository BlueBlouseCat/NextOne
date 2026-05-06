using UnityEngine;
using UnityEngine.SceneManagement;

public class MouseDisappearTrigger : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = "House";

    [Header("Mouse")]
    [SerializeField] private MouseSpineController _mouseSpine;

    [Header("Optional")]
    [SerializeField] private bool _triggerOnlyOnce = true;

    private bool _hasTriggered;
    private PlayerMovement _lockedPlayer;

    private void Start()
    {
        if (_mouseSpine != null && _mouseSpine.HasDisappeared)
            _hasTriggered = true;
    }

    private void OnDisable()
    {
        UnlockPlayer();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (!other.CompareTag("Player")) return;
        if (_mouseSpine == null) return;
        if (_triggerOnlyOnce && _hasTriggered) return;

        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = other.GetComponentInParent<PlayerMovement>();

        _hasTriggered = true;
        _lockedPlayer = playerMovement;
        LockPlayer();

        _mouseSpine.PlayDisappear(OnMouseDisappearFinished);
    }

    private void OnMouseDisappearFinished()
    {
        UnlockPlayer();
    }

    private void LockPlayer()
    {
        if (_lockedPlayer != null)
            _lockedPlayer.SetExternalInputLocked(true);
    }

    private void UnlockPlayer()
    {
        if (_lockedPlayer != null)
            _lockedPlayer.SetExternalInputLocked(false);

        _lockedPlayer = null;
    }
}
