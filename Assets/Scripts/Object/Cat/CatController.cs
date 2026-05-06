using UnityEngine;
using UnityEngine.SceneManagement;

public class CatController : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = SceneName.Scene2; // Brush

    [Header("Mother Cat")]
    [SerializeField] private GameObject _motherCatRoot;

    [Header("Flags")]
    [SerializeField] private string _dialogueCompleteFlag = "mother_cat_dialogue_done";
    [SerializeField] private string _motherCatGoneSceneVisitedFlag = "mother_cat_gone_scene_visited";

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (_motherCatRoot == null) return;
        if (GameManager.Instance == null) return;

        bool dialogueCompleted = GameManager.Instance.GetFlag(_dialogueCompleteFlag);

        if (!dialogueCompleted)
        {
            _motherCatRoot.SetActive(true);
            return;
        }

        _motherCatRoot.SetActive(false);

        if (!string.IsNullOrWhiteSpace(_motherCatGoneSceneVisitedFlag))
            GameManager.Instance.SetFlag(_motherCatGoneSceneVisitedFlag, true);
    }
}
