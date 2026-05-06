using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PaperBallInteractionTrigger : MonoBehaviour
{
    private enum InteractionStep
    {
        None,
        PaperShown,
        Completed
    }

    [Header("Scene")]
    [SerializeField] private string _currentScene = "MouseHole3";

    [Header("Targets")]
    [SerializeField] private GameObject _paperBallRoot;
    [SerializeField] private GameObject _paperRoot;

    [Header("Optional")]
    [SerializeField] private bool _completeOnlyOnce = true;
    [SerializeField] private string _completeFlag = "mousehole3_paper_sequence_done";

    private bool _playerInRange;
    private bool _hintShownByThisScript;
    private InteractionStep _step;

    private void Start()
    {
        RefreshState();
    }

    private void OnEnable()
    {
        RefreshState();
    }

    private void OnDisable()
    {
        _playerInRange = false;
        HideHint();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            HideHint();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsLoadingScene())
        {
            HideHint();
            return;
        }

        if (_completeOnlyOnce &&
            _step != InteractionStep.Completed &&
            GameManager.Instance != null &&
            GameManager.Instance.GetFlag(_completeFlag))
        {
            ApplyCompletedState();
            return;
        }

        if (_step == InteractionStep.Completed)
        {
            HideHint();
            return;
        }

        bool popupOpen = InventoryUI.Instance != null && InventoryUI.Instance.IsPopupOpen;

        if (_playerInRange && !popupOpen)
            ShowHint();
        else
            HideHint();

        if (!_playerInRange) return;
        if (popupOpen) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;

        AdvanceInteraction();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SetPlayerInRange(other, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        SetPlayerInRange(other, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        SetPlayerInRange(other, false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        SetPlayerInRange(collision.collider, true);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        SetPlayerInRange(collision.collider, true);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        SetPlayerInRange(collision.collider, false);
    }

    private void SetPlayerInRange(Collider2D other, bool inRange)
    {
        if (other == null || !other.CompareTag("Player")) return;
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (_step == InteractionStep.Completed) return;

        _playerInRange = inRange;

        if (!inRange)
            HideHint();
    }

    private void AdvanceInteraction()
    {
        switch (_step)
        {
            case InteractionStep.None:
                ShowPaperAndHideBall();
                break;

            case InteractionStep.PaperShown:
                FinishInteraction();
                break;
        }
    }

    private void ShowPaperAndHideBall()
    {
        if (_paperBallRoot != null)
            _paperBallRoot.SetActive(false);

        if (_paperRoot != null)
            _paperRoot.SetActive(true);

        _step = InteractionStep.PaperShown;
    }

    private void FinishInteraction()
    {
        if (_paperRoot != null)
            _paperRoot.SetActive(false);

        _step = InteractionStep.Completed;
        _playerInRange = false;
        HideHint();

        if (_completeOnlyOnce &&
            GameManager.Instance != null &&
            !string.IsNullOrWhiteSpace(_completeFlag))
        {
            GameManager.Instance.SetFlag(_completeFlag, true);
        }
    }

    private void RefreshState()
    {
        _playerInRange = false;
        HideHint();

        bool alreadyCompleted = _completeOnlyOnce &&
                                GameManager.Instance != null &&
                                GameManager.Instance.GetFlag(_completeFlag);

        if (alreadyCompleted)
        {
            ApplyCompletedState();
            return;
        }

        _step = InteractionStep.None;

        if (_paperBallRoot != null)
            _paperBallRoot.SetActive(true);

        if (_paperRoot != null)
            _paperRoot.SetActive(false);
    }

    private void ApplyCompletedState()
    {
        _step = InteractionStep.Completed;
        _playerInRange = false;
        HideHint();

        if (_paperBallRoot != null)
            _paperBallRoot.SetActive(false);

        if (_paperRoot != null)
            _paperRoot.SetActive(false);
    }

    private void ShowHint()
    {
        if (_hintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.ShowInteractHint(true);
        _hintShownByThisScript = true;
    }

    private void HideHint()
    {
        if (!_hintShownByThisScript) return;
        if (InventoryUI.Instance == null) return;

        InventoryUI.Instance.ShowInteractHint(false);
        _hintShownByThisScript = false;
    }
}
