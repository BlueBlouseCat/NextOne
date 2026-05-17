using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private SettingsCanvas _settingsCanvas;
    [SerializeField] private PlayerMovement _playerMovement;

    private bool _holdsInteractionLock;

    public bool IsOpen
    {
        get
        {
            ResolveReferences();
            return _settingsCanvas != null && _settingsCanvas.gameObject.activeSelf;
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        ReleaseGameplayLock();
    }

    private void OnDestroy()
    {
        ReleaseGameplayLock();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        ResolveReferences();
        if (_settingsCanvas == null) return;

        _settingsCanvas.OpenCanvas();
        AcquireGameplayLock();
    }

    public void Close()
    {
        ResolveReferences();

        if (_settingsCanvas == null) return;

        _settingsCanvas.ClosedCanvas();
        ReleaseGameplayLock();
    }

    private void AcquireGameplayLock()
    {
        if (_holdsInteractionLock) return;

        if (_playerMovement == null)
            _playerMovement = FindObjectOfType<PlayerMovement>();

        _playerMovement?.SetExternalInputLocked(true);
        GlobalInteractionLock.Acquire();
        _holdsInteractionLock = true;

        InventoryUI.Instance?.ShowInteractHint(false);
    }

    private void ReleaseGameplayLock()
    {
        if (!_holdsInteractionLock) return;

        if (_playerMovement == null)
            _playerMovement = FindObjectOfType<PlayerMovement>();

        _playerMovement?.SetExternalInputLocked(false);
        GlobalInteractionLock.Release();
        _holdsInteractionLock = false;
    }

    private void ResolveReferences()
    {
        if (_settingsCanvas == null)
        {
            SettingsCanvas[] canvases = Resources.FindObjectsOfTypeAll<SettingsCanvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                SettingsCanvas canvas = canvases[i];
                if (canvas == null || canvas.gameObject == null) continue;
                if (!canvas.gameObject.scene.IsValid()) continue;

                _settingsCanvas = canvas;
                break;
            }
        }

        if (_playerMovement == null)
            _playerMovement = FindObjectOfType<PlayerMovement>();
    }
}
