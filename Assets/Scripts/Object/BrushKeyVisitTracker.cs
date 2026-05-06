using UnityEngine;
using UnityEngine.SceneManagement;

public class BrushKeyVisitTracker : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = SceneName.Scene2; // Brush

    [Header("Flags")]
    [SerializeField] private string _keyCollectedFlag = "item.house_key.collected";
    [SerializeField] private string _enteredBrushAfterKeyFlag = "entered_brush_after_key";
    [SerializeField] private string _catsActivatedFlag = "outside_cats_activated";

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
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.GetFlag(_catsActivatedFlag))
            return;

        bool hasCollectedKey = GameManager.Instance.GetFlag(_keyCollectedFlag);
        if (!hasCollectedKey) return;

        GameManager.Instance.SetFlag(_enteredBrushAfterKeyFlag, true);
    }
}
