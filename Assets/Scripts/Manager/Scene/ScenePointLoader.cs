using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ScenePointLoader : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = SceneName.Scene1;

    [Header("Target Scene")]
    [SerializeField] private string _targetScene = SceneName.Scene2;
    [SerializeField] private bool _useFade = false;
    [SerializeField] private string _targetSpawnPointId = "";

    [TextArea(2, 4)]
    [SerializeField] private string _transitionMessage = "";

    [Header("Trigger")]
    [SerializeField] private Vector2 _targetPosition = new Vector2(0.7301737f, -10.62705f);
    [SerializeField] private float _triggerRadius = 1.2f;

    private Transform _player;
    private bool _isPlayerInRange;
    private bool _hintShownByThisScript;

    private void OnEnable()
    {
        HideHint();
    }

    private void OnDisable()
    {
        HideHint();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            UpdateRangeState(false);
            return;
        }

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;

        if (_player == null)
            _player = GameManager.Instance.CurrentPlayer;

        if (_player == null)
        {
            UpdateRangeState(false);
            return;
        }

        float sqrDistance = ((Vector2)_player.position - _targetPosition).sqrMagnitude;
        bool inRange = sqrDistance <= _triggerRadius * _triggerRadius;

        UpdateRangeState(inRange);

        if (!inRange) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;

        HideHint();

        if (_useFade)
        {
            if (string.IsNullOrEmpty(_transitionMessage))
            {
                if (string.IsNullOrEmpty(_targetSpawnPointId))
                    GameManager.Instance.LoadSceneWithFade(_targetScene);
                else
                    GameManager.Instance.LoadSceneWithFade(_targetScene, _targetSpawnPointId);
            }
            else
            {
                if (string.IsNullOrEmpty(_targetSpawnPointId))
                    GameManager.Instance.LoadSceneWithFadeMessage(_targetScene, _transitionMessage);
                else
                    GameManager.Instance.LoadSceneWithFadeMessage(_targetScene, _targetSpawnPointId, _transitionMessage);
            }
        }
        else
        {
            if (string.IsNullOrEmpty(_targetSpawnPointId))
                GameManager.Instance.LoadScene(_targetScene);
            else
                GameManager.Instance.LoadScene(_targetScene, _targetSpawnPointId);
        }
    }

    private void UpdateRangeState(bool inRange)
    {
        if (_isPlayerInRange == inRange) return;

        _isPlayerInRange = inRange;

        if (inRange)
            ShowHint();
        else
            HideHint();
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
}
