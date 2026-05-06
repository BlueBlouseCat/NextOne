#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

[CustomEditor(typeof(SceneEdgeLoader))]
public class SceneEdgeLoaderEditor : Editor
{
   // 重写 Inspector 绘制方法
    public override void OnInspectorGUI()
    {
        // 更新序列化对象，确保拿到最新数据
        serializedObject.Update();

        // 找到脚本里对应的序列化字段
        var currentScene = serializedObject.FindProperty("_currentScene");
        var useLeftEdge = serializedObject.FindProperty("_useLeftEdge");
        var leftEdgeX = serializedObject.FindProperty("_leftEdgeX");
        var leftTargetScene = serializedObject.FindProperty("_leftTargetScene");
        var leftTargetSpawnPointId = serializedObject.FindProperty("_leftTargetSpawnPointId");
        var useRightEdge = serializedObject.FindProperty("_useRightEdge");
        var rightEdgeX = serializedObject.FindProperty("_rightEdgeX");
        var rightTargetScene = serializedObject.FindProperty("_rightTargetScene");
        var rightTargetSpawnPointId = serializedObject.FindProperty("_rightTargetSpawnPointId");
        var useUpEdge = serializedObject.FindProperty("_useUpEdge");
        var upEdgeY = serializedObject.FindProperty("_upEdgeY");
        var upTargetScene = serializedObject.FindProperty("_upTargetScene");
        var upTargetSpawnPointId = serializedObject.FindProperty("_upTargetSpawnPointId");

        // 从 Build Settings 中读取所有已启用的场景，提取场景名
        string[] sceneNames = EditorBuildSettings.scenes
            .Where(s => s.enabled)        // 只保留勾选启用的场景
            .Select(s => Path.GetFileNameWithoutExtension(s.path)) // 取文件名（去掉路径和后缀）
            .ToArray();

        // 绘制当前场景的下拉选择框
        DrawScenePopup("Current Scene", currentScene, sceneNames);

        EditorGUILayout.Space(); // 空一行

        // 绘制左侧边界相关字段
        EditorGUILayout.PropertyField(useLeftEdge);
        EditorGUILayout.PropertyField(leftEdgeX);
        DrawScenePopup("Left Target Scene", leftTargetScene, sceneNames);
        EditorGUILayout.PropertyField(leftTargetSpawnPointId);

        EditorGUILayout.Space(); // 空一行

        // 绘制右侧边界相关字段
        EditorGUILayout.PropertyField(useRightEdge);
        EditorGUILayout.PropertyField(rightEdgeX);
        DrawScenePopup("Right Target Scene", rightTargetScene, sceneNames);
        EditorGUILayout.PropertyField(rightTargetSpawnPointId);

        EditorGUILayout.Space(); // 空一行

        // 绘制右侧边界相关字段
        EditorGUILayout.PropertyField(useUpEdge);
        EditorGUILayout.PropertyField(upEdgeY);
        DrawScenePopup("Up Target Scene", upTargetScene, sceneNames);
        EditorGUILayout.PropertyField(upTargetSpawnPointId);

        // 应用在 Inspector 上修改的所有属性
        serializedObject.ApplyModifiedProperties();
    }

    // 自定义方法：绘制场景名称的下拉选择框（Popup）
    private void DrawScenePopup(string label, SerializedProperty prop, string[] sceneNames)
    {
        // 查找当前字符串值在场景数组中的索引
        int index = System.Array.IndexOf(sceneNames, prop.stringValue);
        // 如果没找到，默认选中第 0 个
        if (index < 0) index = 0;

        // 绘制下拉框，获取用户新选择的索引
        int newIndex = EditorGUILayout.Popup(label, index, sceneNames);
        // 如果有场景列表，就把选中的场景名赋值给字符串属性
        if (sceneNames.Length > 0)
            prop.stringValue = sceneNames[newIndex];
    }
}
#endif