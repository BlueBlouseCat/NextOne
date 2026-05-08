using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsCanvas : MonoBehaviour
{
    public void OpenCanvas()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public bool isOpened()
    {
        return gameObject.activeSelf;
    }

    public void ClosedCanvas()
    {
        gameObject.SetActive(false);
    }

    public void ExitGame() // 建议改名：ExitGame 比 ExitGames 更符合单数语义
    {
#if UNITY_EDITOR
        // 仅停止播放模式（推荐）
        UnityEditor.EditorApplication.isPlaying = false;
    
        // 如果你的本意确实是“测试时直接关闭整个Unity编辑器”，可改用：
        // UnityEditor.EditorApplication.Exit(0);
#else
    // 非 Web 平台正常退出
#if !UNITY_WEBGL
    Application.Quit();
#endif
#endif
    }

    public void ReturnToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainStart");
        ClosedCanvas();
    }
    
}
