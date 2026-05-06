using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowController : MonoBehaviour
{
    [SerializeField] private GameObject _closedWindow;
    [SerializeField] private GameObject _openWindow;
    [SerializeField] private GameObject _windowFrame;
    [SerializeField] private GameObject _stick;
    [SerializeField] string _stateKey = "outside_window_open";

    private void Start()
    {
        Refresh();
    }

    public void OpenWindow()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.SetFlag(_stateKey, true);
        Refresh();
    }

    public void Refresh()
    {
        bool isOpen = GameManager.Instance != null && GameManager.Instance.GetFlag(_stateKey);

        if(_closedWindow != null)
            _closedWindow.SetActive(!isOpen);

        if(_openWindow != null && _windowFrame != null && _stick != null)
            _openWindow.SetActive(isOpen);
            _windowFrame.SetActive(isOpen);
            _stick.SetActive(isOpen);
    }
}
