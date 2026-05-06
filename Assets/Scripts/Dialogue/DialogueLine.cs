using System;
using UnityEngine;

public enum DialogueSpeaker
{
    Player,
    Other
}

[Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker;

    [TextArea(2, 5)]
    public string content;
}
