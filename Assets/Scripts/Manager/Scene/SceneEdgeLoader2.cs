using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEdgeLoader2 : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _currentScene = "MouseHole2";

    [Header("Edges")]
    [SerializeField] private float _leftEdgeX = -9f;
    [SerializeField] private float _rightEdgeX = 9f;
    [SerializeField] private float _rightSplitY = 0f;

    [Header("Left -> MouseHole4")]
    [SerializeField] private string _leftTargetScene = "MouseHole4";
    [SerializeField] private string _leftTargetSpawnPointId = "";
    [SerializeField] private bool _leftUseFade = false;

    [Header("Right Upper -> MouseHole1")]
    [SerializeField] private string _rightUpperTargetScene = "House";
    [SerializeField] private string _rightUpperTargetSpawnPointId = "hole1_to_house";
    [SerializeField] private bool _rightUpperUseFade = false;

    [Header("Right Lower -> MouseHole3")]
    [SerializeField] private string _rightLowerTargetScene = "MouseHole3";
    [SerializeField] private string _rightLowerTargetSpawnPointId = "";
    [SerializeField] private bool _rightLowerUseFade = false;

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsLoadingScene()) return;
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (GameManager.Instance.CurrentPlayer == null) return;

        Vector3 playerPos = GameManager.Instance.CurrentPlayer.position;

        if (playerPos.x <= _leftEdgeX)
        {
            LoadScene(_leftTargetScene, _leftTargetSpawnPointId, _leftUseFade);
            return;
        }

        if (playerPos.x >= _rightEdgeX)
        {
            if (playerPos.y >= _rightSplitY)
                LoadScene(_rightUpperTargetScene, _rightUpperTargetSpawnPointId, _rightUpperUseFade);
            else
                LoadScene(_rightLowerTargetScene, _rightLowerTargetSpawnPointId, _rightLowerUseFade);
        }
    }

    private void LoadScene(string sceneName, string spawnPointId, bool useFade)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

        if (useFade)
        {
            if (string.IsNullOrWhiteSpace(spawnPointId))
                GameManager.Instance.LoadSceneWithFade(sceneName);
            else
                GameManager.Instance.LoadSceneWithFade(sceneName, spawnPointId);

            return;
        }

        if (string.IsNullOrWhiteSpace(spawnPointId))
            GameManager.Instance.LoadScene(sceneName);
        else
            GameManager.Instance.LoadScene(sceneName, spawnPointId);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(_leftEdgeX, -20f, 0f), new Vector3(_leftEdgeX, 20f, 0f));
        Gizmos.DrawLine(new Vector3(_rightEdgeX, -20f, 0f), new Vector3(_rightEdgeX, 20f, 0f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(_rightEdgeX - 0.1f, _rightSplitY, 0f), new Vector3(_rightEdgeX + 0.1f, _rightSplitY, 0f));
    }
}
