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

    [Header("Right Edge")]
    [SerializeField] private bool _useRightEdge = false;
    [SerializeField] private float _rightEdgeX = 9f;
    [SerializeField] private string _rightTargetScene;
    [SerializeField] private string _rightTargetSpawnPointId = "";
    [SerializeField] private bool _rightUseFade = false;


    [Header("Up Edge")]
    [SerializeField] private bool _useUpEdge = false;
    [SerializeField] private float _upEdgeY = 7f;
    [SerializeField] private string _upTargetScene;
    [SerializeField] private string _upTargetSpawnPointId = "";
    [SerializeField] private bool _upUseFade = false;

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (GameManager.Instance.CurrentPlayer == null) return;

        float playerX = GameManager.Instance.CurrentPlayer.position.x;
        float playerY = GameManager.Instance.CurrentPlayer.position.y;

        if(_useLeftEdge && playerX <= _leftEdgeX)
        {
            LoadFromLeftEdge();
            return;
        }

        if(_useRightEdge && playerX >= _rightEdgeX)
        {
            LoadFromRightEdge();
            return;
        }

        if(_useUpEdge && playerY >= _upEdgeY)
        {
            LoadFromUpEdge();
            return;
        }
    }

    private void LoadFromLeftEdge()
    {
        LoadTargetScene(_leftTargetScene, _leftTargetSpawnPointId, _leftUseFade);
    }

    private void LoadFromRightEdge()
    {
        LoadTargetScene(_rightTargetScene, _rightTargetSpawnPointId, _rightUseFade);
    }

    private void LoadFromUpEdge()
    {
        LoadTargetScene(_upTargetScene, _upTargetSpawnPointId, _upUseFade);
    }


    private void LoadTargetScene(string targetScene, string targetSpawnPointId, bool useFade)
    {
        if (useFade)
        {
            if (string.IsNullOrEmpty(targetSpawnPointId))
                GameManager.Instance.LoadSceneWithFade(targetScene);
            else
                GameManager.Instance.LoadSceneWithFade(targetScene, targetSpawnPointId);

            return;
        }

        if (string.IsNullOrEmpty(targetSpawnPointId))
            GameManager.Instance.LoadScene(targetScene);
        else
            GameManager.Instance.LoadScene(targetScene, targetSpawnPointId);
    }

}
