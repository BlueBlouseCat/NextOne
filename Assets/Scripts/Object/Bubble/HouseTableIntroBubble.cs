using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HouseTableIntroBubble : MonoBehaviour
{
    [System.Serializable]
    public class BubbleLine
    {
        [TextArea(2, 5)]
        public string content;

        public float showDuration = 1.8f;
        public float gapAfter = 0.35f;
    }

    [Header("Scene")]
    [SerializeField] private string _currentScene = "House";

    [Header("Dialogue UI")]
    [SerializeField] private DialogueUI _dialogueUI;

    [Header("Lines")]
    [SerializeField] private BubbleLine[] _lines =
    {
        new BubbleLine
        {
            content = "这里是阿瑟的军帐？看来那扇窗户就是梦的出口了。",
            showDuration = 2f,
            gapAfter = 0.4f
        },
        new BubbleLine
        {
            content = "这里有一股甜腻和腐朽混合的气味，哦，真上头……",
            showDuration = 2f,
            gapAfter = 0f
        }
    };

    [Header("Optional")]
    [SerializeField] private bool _playOnlyOnce = true;
    [SerializeField] private string _playedFlag = "house_table_intro_played";
    [SerializeField] private bool _disableTriggerAfterPlayed = true;

    private Coroutine _playRoutine;
    private PlayerMovement _lockedPlayer;
    private bool _isPlaying;
    private bool _hasPlayed;
    private bool _hasAcquiredInteractionLock;

    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        if (_dialogueUI == null)
            _dialogueUI = FindObjectOfType<DialogueUI>();
    }

    private void Start()
    {
        _hasPlayed = GameManager.Instance != null && GameManager.Instance.GetFlag(_playedFlag);

        if (_playOnlyOnce && _hasPlayed && _disableTriggerAfterPlayed)
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        _isPlaying = false;

        if (_dialogueUI != null)
            _dialogueUI.Close();

        ReleaseInteractionLock();
        UnlockPlayer();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isPlaying) return;
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (!other.CompareTag("Player")) return;

        if (_playOnlyOnce)
        {
            _hasPlayed = GameManager.Instance != null && GameManager.Instance.GetFlag(_playedFlag);
            if (_hasPlayed) return;
        }

        if (_lines == null || _lines.Length == 0) return;

        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null) return;

        _lockedPlayer = playerMovement;
        _playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        _isPlaying = true;
        AcquireInteractionLock();
        LockPlayer();

        if (_playOnlyOnce && GameManager.Instance != null && !string.IsNullOrWhiteSpace(_playedFlag))
            GameManager.Instance.SetFlag(_playedFlag, true);

        yield return null;

        for (int i = 0; i < _lines.Length; i++)
        {
            BubbleLine bubbleLine = _lines[i];
            if (bubbleLine == null) continue;
            if (string.IsNullOrWhiteSpace(bubbleLine.content)) continue;

            if (_dialogueUI != null)
            {
                DialogueLine line = new DialogueLine
                {
                    speaker = DialogueSpeaker.Player,
                    content = bubbleLine.content
                };

                _dialogueUI.ShowLine(line, false);
            }

            float showDuration = Mathf.Max(0f, bubbleLine.showDuration);
            if (showDuration > 0f)
                yield return new WaitForSeconds(showDuration);

            _dialogueUI?.Close();

            float gapAfter = Mathf.Max(0f, bubbleLine.gapAfter);
            if (gapAfter > 0f && i < _lines.Length - 1)
                yield return new WaitForSeconds(gapAfter);
        }

        _dialogueUI?.Close();
        ReleaseInteractionLock();
        UnlockPlayer();

        _isPlaying = false;
        _playRoutine = null;
        _hasPlayed = true;

        if (_playOnlyOnce && _disableTriggerAfterPlayed)
            gameObject.SetActive(false);
    }

    private void AcquireInteractionLock()
    {
        if (_hasAcquiredInteractionLock) return;

        GlobalInteractionLock.Acquire();
        _hasAcquiredInteractionLock = true;

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInteractHint(false);
    }

    private void ReleaseInteractionLock()
    {
        if (!_hasAcquiredInteractionLock) return;

        GlobalInteractionLock.Release();
        _hasAcquiredInteractionLock = false;
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
