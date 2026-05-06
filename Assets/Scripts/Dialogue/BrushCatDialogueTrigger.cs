using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BrushCatDialogueTrigger : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = SceneName.Scene2; // Brush

    [Header("Mother Cat")]
    [SerializeField] private GameObject _motherCatRoot;
    [SerializeField] private MotherCatSpineController _motherCatSpine;

    [Header("Trigger Point")]
    [SerializeField] private Vector2 _dialoguePoint = new Vector2(-3f, -3.64f);
    [SerializeField] private float _triggerRadius = 1.2f;

    [Header("Dialogue UI")]
    [SerializeField] private DialogueUI _dialogueUI;

    [Header("Dialogue Lines")]
    [SerializeField] private DialogueLine[] _lines;

    [Header("Flags")]
    [SerializeField] private string _dialogueCompleteFlag = "mother_cat_dialogue_done";
    [SerializeField] private string _motherCatGoneSceneVisitedFlag = "mother_cat_gone_scene_visited";

    private Transform _player;
    private PlayerMovement _playerMovement;
    private bool _isInRange;
    private bool _isDialogueRunning;
    private bool _hintShownByThisScript;
    private int _currentLineIndex;
    private bool _hasFinishedDialogue;

    private void OnEnable()
    {
        _hasFinishedDialogue = GameManager.Instance != null && GameManager.Instance.GetFlag(_dialogueCompleteFlag);

        if (!_hasFinishedDialogue)
            _motherCatSpine?.PlayBlockedIdle();

        HideHint();
        CloseDialogueUI();
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
            UpdateRangeState(false);
            return;
        }

        if (_motherCatRoot != null && !_motherCatRoot.activeInHierarchy)
        {
            UpdateRangeState(false);
            return;
        }

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;

        if (!_hasFinishedDialogue)
            _hasFinishedDialogue = GameManager.Instance.GetFlag(_dialogueCompleteFlag);

        if (_player == null)
            _player = GameManager.Instance.CurrentPlayer;

        if (_player == null)
        {
            UpdateRangeState(false);
            return;
        }

        if (_playerMovement == null)
            _playerMovement = _player.GetComponent<PlayerMovement>();

        float sqrDistance = ((Vector2)_player.position - _dialoguePoint).sqrMagnitude;
        bool inRange = sqrDistance <= _triggerRadius * _triggerRadius;

        if (_hasFinishedDialogue)
        {
            UpdateRangeState(false);
            return;
        }

        UpdateRangeState(inRange);

        if (Keyboard.current == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;

        if (_isDialogueRunning)
        {
            AdvanceDialogue();
            return;
        }

        if (!inRange) return;
        if (_dialogueUI == null) return;

        StartDialogue();
    }

    private void UpdateRangeState(bool inRange)
    {
        _isInRange = inRange;

        if (_isDialogueRunning)
        {
            HideHint();
            return;
        }

        if (inRange)
            ShowHint();
        else
            HideHint();
    }

    private void StartDialogue()
    {
        if (_hasFinishedDialogue) return;
        if (_lines == null || _lines.Length == 0) return;

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

        if (_motherCatSpine == null) return;

        if (line.speaker == DialogueSpeaker.Other)
            _motherCatSpine.PlayTalk();
        else
            _motherCatSpine.PlayClosedIdle();
    }

    private void EndDialogue()
    {
        _isDialogueRunning = false;
        _currentLineIndex = 0;

        CloseDialogueUI();
        UnlockPlayer();
        HideHint();

        _motherCatSpine?.PlayOpenIdle();
        _hasFinishedDialogue = true;

        if (GameManager.Instance != null)
        {
            if (!string.IsNullOrWhiteSpace(_dialogueCompleteFlag))
                GameManager.Instance.SetFlag(_dialogueCompleteFlag, true);

            if (!string.IsNullOrWhiteSpace(_motherCatGoneSceneVisitedFlag))
                GameManager.Instance.SetFlag(_motherCatGoneSceneVisitedFlag, true);
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_dialoguePoint, _triggerRadius);
    }
}
