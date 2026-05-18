using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CrowDialogueTrigger : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = SceneName.Scene1;

    [Header("Crow")]
    [SerializeField] private GameObject _crowRoot;
    [SerializeField] private CrowSpineController _crowSpine;
    //[SerializeField] private BoxCollider2D _crowBox;

    [Header("Trigger Point")]
    [SerializeField] private Vector2 _dialoguePoint = new Vector2(-4.98f, 6.43f);
    [SerializeField] private float _triggerRadius = 1.2f;

    [Header("Dialogue UI")]
    [SerializeField] private DialogueUI _dialogueUI;

    [Header("Dialogue Lines")]
    [SerializeField] private DialogueLine[] _lines;

    [Header("Optional Fly On Window Open")]
    [SerializeField] private bool _flyWhenWindowOpens = false;
    [SerializeField] private string _windowOpenStateKey = "outside_window_open";

    [Header("Progress Flags")]
    [SerializeField] private string _introDialogueCompleteFlag = "crow_intro_dialogue_done";

    [Header("Optional")]
    [SerializeField] private bool _playDialogueOnlyOnce = true;

    private Transform _player;
    private PlayerMovement _playerMovement;
    private bool _isDialogueRunning;
    private bool _hintShownByThisScript;
    private int _currentLineIndex;
    private bool _hasFinishedDialogue;
    private bool _hasTriggeredFly;

    private void OnEnable()
    {
        _hasFinishedDialogue = GameManager.Instance != null && GameManager.Instance.GetFlag(_introDialogueCompleteFlag);

        HideHint();
        CloseDialogueUI();

        if (_crowSpine != null && !_crowSpine.HasFlownAway())
            _crowSpine.PlayIdle();
    }

    private void OnDisable()
    {
        HideHint();
        CloseDialogueUI();
        UnlockPlayer();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            HideHint();
            return;
        }

        if (_crowRoot != null && !_crowRoot.activeInHierarchy)
        {
            HideHint();
            return;
        }

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;

        if (!_hasFinishedDialogue)
            _hasFinishedDialogue = GameManager.Instance.GetFlag(_introDialogueCompleteFlag);

        if (_flyWhenWindowOpens && !_hasTriggeredFly && GameManager.Instance.GetFlag(_windowOpenStateKey))
        {
            TriggerFly();
            return;
        }

        if (_hasTriggeredFly)
        {
            HideHint();
            return;
        }

        if (_player == null)
            _player = GameManager.Instance.CurrentPlayer;

        if (_player == null)
        {
            HideHint();
            return;
        }

        if (_playerMovement == null)
            _playerMovement = _player.GetComponent<PlayerMovement>();

        float sqrDistance = ((Vector2)_player.position - _dialoguePoint).sqrMagnitude;
        bool inRange = sqrDistance <= _triggerRadius * _triggerRadius;

        if (_playDialogueOnlyOnce && _hasFinishedDialogue)
        {
            HideHint();
            return;
        }

        if (_isDialogueRunning)
        {
            HideHint();

            if (GameplayInputUtil.InteractPressedThisFrame())
                AdvanceDialogue();

            return;
        }

        if (inRange)
            ShowHint();
        else
            HideHint();

        if (!inRange) return;
        if (_dialogueUI == null) return;
        if (!GameplayInputUtil.InteractPressedThisFrame()) return;

        StartDialogue();
    }

    private void StartDialogue()
    {
        if (_playDialogueOnlyOnce && _hasFinishedDialogue) return;
        if (_lines == null || _lines.Length == 0) return;
        if (_hasTriggeredFly) return;

        _isDialogueRunning = true;
        _currentLineIndex = 0;

        HideHint();
        LockPlayer();
        ShowCurrentLine();
    }

    private void AdvanceDialogue()
    {
        _currentLineIndex++;

        if (_lines == null || _currentLineIndex >= _lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (_dialogueUI == null) return;

        DialogueLine line = _lines[_currentLineIndex];
        _dialogueUI.ShowLine(line);

        if (_crowSpine == null) return;

        if (line.speaker == DialogueSpeaker.Player)
            _crowSpine.PlayIdle();
        else
            _crowSpine.PlaySpeak();
    }

    private void EndDialogue()
    {
        _isDialogueRunning = false;
        _currentLineIndex = 0;

        CloseDialogueUI();
        UnlockPlayer();

        _hasFinishedDialogue = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_introDialogueCompleteFlag))
            GameManager.Instance.SetFlag(_introDialogueCompleteFlag, true);

        _crowSpine?.PlayIdle();
        HideHint();
        //_crowBox.enabled = false;
    }

    private void TriggerFly()
    {
        _hasTriggeredFly = true;
        _isDialogueRunning = false;
        _currentLineIndex = 0;

        HideHint();
        CloseDialogueUI();
        UnlockPlayer();

        _crowSpine?.PlayFly();
    }

    private void ShowHint()
    {
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

    private void LockPlayer()
    {
        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(true);
    }

    private void UnlockPlayer()
    {
        if (_playerMovement != null)
            _playerMovement.SetExternalInputLocked(false);
    }

    private void CloseDialogueUI()
    {
        if (_dialogueUI != null)
            _dialogueUI.Close();
    }
}
