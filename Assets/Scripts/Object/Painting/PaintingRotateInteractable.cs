using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PaintingRotateInteractable : MonoBehaviour
{
    [Header("Scene Limit")]
    [SerializeField] private string _currentScene = "MouseHole1";

    [Header("Painting")]
    [SerializeField] private Transform _paintingRoot;
    [SerializeField] private float _rotateStep = 30f;
    [SerializeField] private bool _clockwise = true;

    [Header("Puzzle")]
    [SerializeField] private PaintingRotationPuzzle _puzzleController;

    private bool _playerInRange;
    private bool _hintShownByThisScript;

    public Transform PaintingTransform => _paintingRoot != null ? _paintingRoot : transform;

    private void Awake()
    {
        if (_paintingRoot == null)
            _paintingRoot = transform;
    }

    private void OnEnable()
    {
        HideHint();
    }

    private void OnDisable()
    {
        _playerInRange = false;
        HideHint();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != _currentScene)
        {
            HideHint();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsLoadingScene())
        {
            HideHint();
            return;
        }

        if (_puzzleController != null && _puzzleController.IsSolved)
        {
            HideHint();
            return;
        }

        bool popupOpen = InventoryUI.Instance != null && InventoryUI.Instance.IsPopupOpen;

        if (_playerInRange && !popupOpen)
            ShowHint();
        else
            HideHint();

        if (!_playerInRange) return;
        if (popupOpen) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;

        RotateOnce();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        HideHint();
    }

    public bool IsAtIdentityRotation(float tolerance)
    {
        Vector3 euler = PaintingTransform.localEulerAngles;

        float x = NormalizeAngle(euler.x);
        float y = NormalizeAngle(euler.y);
        float z = NormalizeAngle(euler.z);

        return IsAngleZero(x, tolerance)
            && IsAngleZero(y, tolerance)
            && IsAngleZero(z, tolerance);
    }

    public void SnapToIdentityRotation()
    {
        PaintingTransform.localEulerAngles = Vector3.zero;
    }

    private void RotateOnce()
    {
        float signedStep = Mathf.Abs(_rotateStep) * (_clockwise ? -1f : 1f);

        Vector3 euler = PaintingTransform.localEulerAngles;
        euler.z = NormalizeAngle(euler.z + signedStep);
        PaintingTransform.localEulerAngles = euler;

        _puzzleController?.EvaluatePuzzle();
    }

    private bool IsAngleZero(float angle, float tolerance)
    {
        return angle <= tolerance || angle >= 360f - tolerance;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f)
            angle += 360f;

        return angle;
    }

    private void ShowHint()
    {
        if (_hintShownByThisScript) return;

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ShowInteractHint(true);
            _hintShownByThisScript = true;
        }
    }

    private void HideHint()
    {
        if (!_hintShownByThisScript) return;

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInteractHint(false);

        _hintShownByThisScript = false;
    }
}
