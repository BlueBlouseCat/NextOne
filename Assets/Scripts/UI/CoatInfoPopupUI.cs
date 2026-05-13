using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CoatInfoPopupUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _root;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descText;

    private bool _ignoreCloseUntilMouseRelease;

    public bool IsOpen => _root != null && _root.activeSelf;

    public event Action Opened;
    public event Action Closed;

    private void Awake()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Mouse.current == null) return;

        if (_ignoreCloseUntilMouseRelease)
        {
            if (!Mouse.current.leftButton.isPressed)
                _ignoreCloseUntilMouseRelease = false;

            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
            Close();
    }

    public void Open(string title, string description)
    {
        if (_titleText != null)
            _titleText.text = title;

        if (_descText != null)
            _descText.text = description;

        if (_root != null)
            _root.SetActive(true);

        _ignoreCloseUntilMouseRelease = true;
        Opened?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;

        if (_root != null)
            _root.SetActive(false);

        _ignoreCloseUntilMouseRelease = true;
        Closed?.Invoke();
    }

    public void OnClickCloseButton()
    {
        Close();
    }
}
