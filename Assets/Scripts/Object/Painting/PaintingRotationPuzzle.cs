using UnityEngine;
using UnityEngine.SceneManagement;

public class PaintingRotationPuzzle : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = "MouseHole1";

    [Header("Paintings")]
    [SerializeField] private PaintingRotateInteractable _painting1;
    [SerializeField] private PaintingRotateInteractable _painting2;
    [SerializeField] private float _zeroTolerance = 0.1f;

    [Header("Result")]
    [SerializeField] private GameObject _stairsObject;
    [SerializeField] private string _solvedFlag = "mousehole1_painting_puzzle_solved";
    [SerializeField] private bool _snapPaintingsToZeroWhenSolved = true;

    private bool _isSolved;

    public bool IsSolved => _isSolved;

    private void Start()
    {
        RefreshState();
    }

    private void OnEnable()
    {
        RefreshState();
    }

    public void EvaluatePuzzle()
    {
        if (_isSolved) return;
        if (SceneManager.GetActiveScene().name != _currentScene) return;
        if (_painting1 == null || _painting2 == null) return;

        bool painting1Correct = _painting1.IsAtIdentityRotation(_zeroTolerance);
        bool painting2Correct = _painting2.IsAtIdentityRotation(_zeroTolerance);

        if (painting1Correct && painting2Correct)
            SolvePuzzle();
    }

    private void RefreshState()
    {
        _isSolved = GameManager.Instance != null && GameManager.Instance.GetFlag(_solvedFlag);

        if (_isSolved)
        {
            ApplySolvedState();
            return;
        }

        if (_stairsObject != null)
            _stairsObject.SetActive(false);

        EvaluatePuzzle();
    }

    private void SolvePuzzle()
    {
        _isSolved = true;

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(_solvedFlag))
            GameManager.Instance.SetFlag(_solvedFlag, true);

        ApplySolvedState();
    }

    private void ApplySolvedState()
    {
        if (_snapPaintingsToZeroWhenSolved)
        {
            _painting1?.SnapToIdentityRotation();
            _painting2?.SnapToIdentityRotation();
        }

        if (_stairsObject != null)
            _stairsObject.SetActive(true);

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInteractHint(false);
    }
}
