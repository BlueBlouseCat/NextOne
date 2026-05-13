using UnityEngine;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;

    [Header("Other Box")]
    [SerializeField] private GameObject _otherBox;
    [SerializeField] private TMP_Text _otherText;

    [Header("Player Box")]
    [SerializeField] private GameObject _playerBox;
    [SerializeField] private TMP_Text _playerText;

    [Header("Optional")]
    [SerializeField] private TMP_Text _continueHintText;

    public bool IsOpen => _root != null && _root.activeSelf;

    private const string DefaultContinueHint = "- 按 F 键继续 -";

    private void Awake()
    {
        Close();
    }

    public void Open()
    {
        if (_root != null)
            _root.SetActive(true);
    }

    public void ShowLine(DialogueLine line)
    {
        ShowLine(line, true, DefaultContinueHint);
    }

    public void ShowLine(DialogueLine line, bool showContinueHint)
    {
        ShowLine(line, showContinueHint, DefaultContinueHint);
    }

    public void ShowLine(DialogueLine line, bool showContinueHint, string continueHintText)
    {
        if (line == null) return;

        Open();

        if (_continueHintText != null)
        {
            _continueHintText.gameObject.SetActive(showContinueHint);
            _continueHintText.text = showContinueHint ? continueHintText : string.Empty;
        }

        bool isOther = line.speaker == DialogueSpeaker.Other;

        if (_otherBox != null)
            _otherBox.SetActive(isOther);

        if (_playerBox != null)
            _playerBox.SetActive(!isOther);

        if (isOther)
        {
            if (_otherText != null)
                _otherText.text = line.content;
        }
        else
        {
            if (_playerText != null)
                _playerText.text = line.content;
        }
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);

        if (_otherBox != null)
            _otherBox.SetActive(false);

        if (_playerBox != null)
            _playerBox.SetActive(false);

        if (_continueHintText != null)
        {
            _continueHintText.text = string.Empty;
            _continueHintText.gameObject.SetActive(false);
        }
    }
}
