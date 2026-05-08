using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEdgeLoader : MonoBehaviour
{
    [SerializeField] private string _currentScene;

    [Header("Left Edge")]
    [SerializeField] private bool _useLeftEdge = false;
    [SerializeField] private float _leftEdgeX = -9f;
    [SerializeField] private string _leftTargetScene;
    [SerializeField] private string _leftTargetSpawnPointId = "";
    [SerializeField] private bool _leftUseFade = false;
    [TextArea(2, 4)]
    [SerializeField] private string _leftTransitionMessage = "";

    [Header("Right Edge")]
    [SerializeField] private bool _useRightEdge = false;
    [SerializeField] private float _rightEdgeX = 9f;
    [SerializeField] private string _rightTargetScene;
    [SerializeField] private string _rightTargetSpawnPointId = "";
    [SerializeField] private bool _rightUseFade = false;
    [TextArea(2, 4)]
    [SerializeField] private string _rightTransitionMessage = "";

    [Header("Up Edge")]
    [SerializeField] private bool _useUpEdge = false;
    [SerializeField] private float _upEdgeY = 7f;
    [SerializeField] private string _upTargetScene;
    [SerializeField] private string _upTargetSpawnPointId = "";
    [SerializeField] private bool _upUseFade = false;
    [TextArea(2, 4)]
    [SerializeField] private string _upTransitionMessage = "";

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (GameManager.Instance.CurrentPlayer == null) return;

        float playerX = GameManager.Instance.CurrentPlayer.position.x;
        float playerY = GameManager.Instance.CurrentPlayer.position.y;

        if (_useLeftEdge && playerX <= _leftEdgeX)
        {
            LoadTargetScene(_leftTargetScene, _leftTargetSpawnPointId, _leftUseFade, _leftTransitionMessage);
            return;
        }

        if (_useRightEdge && playerX >= _rightEdgeX)
        {
            LoadTargetScene(_rightTargetScene, _rightTargetSpawnPointId, _rightUseFade, _rightTransitionMessage);
            return;
        }

        if (_useUpEdge && playerY >= _upEdgeY)
        {
            LoadTargetScene(_upTargetScene, _upTargetSpawnPointId, _upUseFade, _upTransitionMessage);
            return;
        }
    }

    private void LoadTargetScene(string targetScene, string targetSpawnPointId, bool useFade, string transitionMessage)
    {
        if (useFade)
        {
            if (string.IsNullOrEmpty(transitionMessage))
            {
                if (string.IsNullOrEmpty(targetSpawnPointId))
                    GameManager.Instance.LoadSceneWithFade(targetScene);
                else
                    GameManager.Instance.LoadSceneWithFade(targetScene, targetSpawnPointId);
            }
            else
            {
                if (string.IsNullOrEmpty(targetSpawnPointId))
                    GameManager.Instance.LoadSceneWithFadeMessage(targetScene, transitionMessage);
                else
                    GameManager.Instance.LoadSceneWithFadeMessage(targetScene, targetSpawnPointId, transitionMessage);
            }

            return;
        }

        if (string.IsNullOrEmpty(targetSpawnPointId))
            GameManager.Instance.LoadScene(targetScene);
        else
            GameManager.Instance.LoadScene(targetScene, targetSpawnPointId);
    }
}
