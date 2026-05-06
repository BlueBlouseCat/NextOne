using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HouseIntroBubble : MonoBehaviour
{
    [System.Serializable]
    public class BubbleLine
    {
        [TextArea(2, 5)]
        public string content;

        public float showDuration = 1.5f;
        public float gapAfter = 0.3f;
    }

    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = "House";

    [Header("Dialogue UI")]
    [SerializeField] private DialogueUI _dialogueUI;

    [Header("Lines")]
    [SerializeField] private BubbleLine[] _lines;

    [Header("Optional")]
    [SerializeField] private bool _playOnlyOnce = true;
    [SerializeField] private string _playedFlag = "house_intro_bubble_played";
    [SerializeField] private bool _lockPlayerWhilePlaying = true;

    private Coroutine _playRoutine;
    private PlayerMovement _lockedPlayer;

    private void Start()
    {
        TryPlay();
    }

    private void OnEnable()
    {
        TryPlay();
    }

    private void OnDisable()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        _dialogueUI?.Close();
        UnlockPlayer();
    }

    private void TryPlay()
    {
        if (SceneManager.GetActiveScene().name != _currentScene) return;

        if (_dialogueUI == null)
            _dialogueUI = FindObjectOfType<DialogueUI>();

        if (GameManager.Instance == null) return;
        if (_lines == null || _lines.Length == 0) return;

        if (_playOnlyOnce && GameManager.Instance.GetFlag(_playedFlag))
        {
            UnlockPlayer();
            return;
        }

        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        _playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        yield return null;
        yield return ResolvePlayerRoutine();

        LockPlayer();

        if (_playOnlyOnce && GameManager.Instance != null && !string.IsNullOrWhiteSpace(_playedFlag))
            GameManager.Instance.SetFlag(_playedFlag, true);

        for (int i = 0; i < _lines.Length; i++)
        {
            BubbleLine bubbleLine = _lines[i];
            if (bubbleLine == null) continue;
            if (string.IsNullOrWhiteSpace(bubbleLine.content)) continue;

            DialogueLine line = new DialogueLine
            {
                speaker = DialogueSpeaker.Player,
                content = bubbleLine.content
            };

            _dialogueUI.ShowLine(line, false);

            float showDuration = Mathf.Max(0f, bubbleLine.showDuration);
            if (showDuration > 0f)
                yield return new WaitForSeconds(showDuration);

            _dialogueUI.Close();

            float gapAfter = Mathf.Max(0f, bubbleLine.gapAfter);
            if (gapAfter > 0f && i < _lines.Length - 1)
                yield return new WaitForSeconds(gapAfter);
        }

        _dialogueUI?.Close();
        UnlockPlayer();
        _playRoutine = null;
    }

    private IEnumerator ResolvePlayerRoutine()
    {
        const int maxFrames = 60;

        for (int i = 0; i < maxFrames; i++)
        {
            ResolvePlayerMovement();
            if (_lockedPlayer != null)
                yield break;

            yield return null;
        }
    }

    private void ResolvePlayerMovement()
    {
        if (_lockedPlayer != null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            _lockedPlayer = GameManager.Instance.CurrentPlayer.GetComponent<PlayerMovement>();
            if (_lockedPlayer == null)
                _lockedPlayer = GameManager.Instance.CurrentPlayer.GetComponentInParent<PlayerMovement>();

            if (_lockedPlayer != null)
                return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        _lockedPlayer = player.GetComponent<PlayerMovement>();
        if (_lockedPlayer == null)
            _lockedPlayer = player.GetComponentInParent<PlayerMovement>();
    }

    private void LockPlayer()
    {
        if (!_lockPlayerWhilePlaying) return;

        ResolvePlayerMovement();

        if (_lockedPlayer != null)
            _lockedPlayer.SetExternalInputLocked(true);
    }

    private void UnlockPlayer()
    {
        if (!_lockPlayerWhilePlaying) return;

        if (_lockedPlayer != null)
            _lockedPlayer.SetExternalInputLocked(false);

        _lockedPlayer = null;
    }
}
