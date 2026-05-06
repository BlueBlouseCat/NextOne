using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RectTransformAutoWindow : EditorWindow
{
    [MenuItem("Tools/RectTransform Auto")]
    public static void ShowWindow()
    {
        GetWindow<RectTransformAutoWindow>("RectTransform Auto");
    }
    
    // 静态列表用于存储多个RectTransform
    private static List<RectTransform> targetRectTransforms = new List<RectTransform>();
    
    // 设置参数
    private Vector2 knownSize = new Vector2(100, 100);
    private Vector4 margins = Vector4.zero; // left, top, right, bottom
    private FillMode fillMode = FillMode.KeepAspectRatio;
    
    // UI滚动位置
    private Vector2 scrollPosition;
    
    public enum FillMode
    {
        StretchBoth,        // 拉伸宽高
        StretchWidth,       // 只拉伸宽度
        StretchHeight,      // 只拉伸高度
        KeepAspectRatio     // 保持宽高比
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("RectTransform 自动锚点工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // 目标列表管理
        DrawTargetManagement();
        
        EditorGUILayout.Space(10);
        
        // 位置转锚点功能
        DrawPositionToAnchor();
        
        EditorGUILayout.Space(10);
        
        // 填充模式功能
        DrawFillModeSection();
        
        EditorGUILayout.Space(10);
        
        // 信息显示
        DrawInfoSection();
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// 绘制目标管理区域
    /// </summary>
    private void DrawTargetManagement()
    {
        EditorGUILayout.LabelField("目标管理", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("box");
        
        // 添加当前选中的物体
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("添加选中物体", GUILayout.Height(30)))
        {
            AddSelectedObjects();
        }
        
        if (GUILayout.Button("清空列表", GUILayout.Height(30)))
        {
            ClearTargets();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 显示当前目标列表
        EditorGUILayout.LabelField($"当前目标数量: {GetValidTargetCount()}");
        
        // 清理空引用
        targetRectTransforms.RemoveAll(t => t == null);
        
        // 显示目标列表
        if (targetRectTransforms.Count > 0)
        {
            EditorGUILayout.LabelField("目标列表:", EditorStyles.miniLabel);
            for (int i = 0; i < targetRectTransforms.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 显示目标名称
                if (targetRectTransforms[i] != null)
                {
                    EditorGUILayout.LabelField($"• {targetRectTransforms[i].name}", EditorStyles.miniLabel);
                    
                    // 选中按钮
                    if (GUILayout.Button("选中", GUILayout.Width(40)))
                    {
                        Selection.activeGameObject = targetRectTransforms[i].gameObject;
                    }
                    
                    // 移除按钮
                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        targetRectTransforms.RemoveAt(i);
                        i--;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 绘制位置转锚点区域
    /// </summary>
    private void DrawPositionToAnchor()
    {
        EditorGUILayout.LabelField("位置转锚点", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("当前位置 → 锚点", GUILayout.Height(40)))
        {
            ConvertPositionToAnchors();
        }
        
        if (GUILayout.Button("撤销", GUILayout.Height(40), GUILayout.Width(60)))
        {
            Undo.PerformUndo();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox("将当前列表中所有UI物体的位置和大小转换为对应的锚点值", MessageType.Info);
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 绘制填充模式区域
    /// </summary>
    private void DrawFillModeSection()
    {
        EditorGUILayout.LabelField("填充模式", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("box");
        
        // 参数设置
        knownSize = EditorGUILayout.Vector2Field("已知尺寸 (中心锚点时)", knownSize);
        
        EditorGUILayout.Space(5);
        
        fillMode = (FillMode)EditorGUILayout.EnumPopup("填充模式", fillMode);
        
        EditorGUILayout.Space(5);
        
        // 边距设置
        EditorGUILayout.LabelField("边距设置", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        margins.x = EditorGUILayout.FloatField("左", margins.x);
        margins.y = EditorGUILayout.FloatField("上", margins.y);
        margins.z = EditorGUILayout.FloatField("右", margins.z);
        margins.w = EditorGUILayout.FloatField("下", margins.w);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("无边距"))
        {
            margins = Vector4.zero;
        }
        
        if (GUILayout.Button("5px"))
        {
            margins = new Vector4(5, 5, 5, 5);
        }
        
        if (GUILayout.Button("10px"))
        {
            margins = new Vector4(10, 10, 10, 10);
        }
        
        if (GUILayout.Button("20px"))
        {
            margins = new Vector4(20, 20, 20, 20);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 填充模式按钮
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("拉伸全部", GUILayout.Height(30)))
        {
            ApplyFillMode(FillMode.StretchBoth);
        }
        
        if (GUILayout.Button("拉伸宽度", GUILayout.Height(30)))
        {
            ApplyFillMode(FillMode.StretchWidth);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("拉伸高度", GUILayout.Height(30)))
        {
            ApplyFillMode(FillMode.StretchHeight);
        }
        
        if (GUILayout.Button("保持比例", GUILayout.Height(30)))
        {
            ApplyFillMode(FillMode.KeepAspectRatio);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("重置为中心锚点", GUILayout.Height(25)))
        {
            ResetToCenterAnchor();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 绘制信息显示区域
    /// </summary>
    private void DrawInfoSection()
    {
        if (targetRectTransforms.Count > 0)
        {
            EditorGUILayout.LabelField("当前信息", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            RectTransform firstValid = targetRectTransforms.Find(t => t != null);
            if (firstValid != null)
            {
                EditorGUILayout.LabelField($"目标数量: {GetValidTargetCount()}");
                EditorGUILayout.LabelField($"示例 ({firstValid.name}):");
                EditorGUILayout.LabelField($"  锚点Min: {firstValid.anchorMin:F2}");
                EditorGUILayout.LabelField($"  锚点Max: {firstValid.anchorMax:F2}");
                EditorGUILayout.LabelField($"  尺寸: {firstValid.rect.size:F1}");
                EditorGUILayout.LabelField($"  位置: {firstValid.anchoredPosition:F1}");
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    /// <summary>
    /// 添加当前选中的物体
    /// </summary>
    private void AddSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int addedCount = 0;
        
        foreach (GameObject obj in selectedObjects)
        {
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null && !targetRectTransforms.Contains(rectTransform))
            {
                targetRectTransforms.Add(rectTransform);
                addedCount++;
            }
        }
        
        if (addedCount > 0)
        {
            Debug.Log($"已添加 {addedCount} 个RectTransform到列表");
        }
        else
        {
            Debug.LogWarning("未找到新的RectTransform或已存在于列表中");
        }
        
        Repaint();
    }
    
    /// <summary>
    /// 清空目标列表
    /// </summary>
    private void ClearTargets()
    {
        int count = targetRectTransforms.Count;
        targetRectTransforms.Clear();
        Debug.Log($"已清空目标列表，移除了 {count} 个目标");
        Repaint();
    }
    
    /// <summary>
    /// 获取有效目标数量
    /// </summary>
    private int GetValidTargetCount()
    {
        targetRectTransforms.RemoveAll(t => t == null);
        return targetRectTransforms.Count;
    }
    
    /// <summary>
    /// 位置转锚点
    /// </summary>
    private void ConvertPositionToAnchors()
    {
        if (targetRectTransforms.Count == 0)
        {
            Debug.LogError("目标列表为空！请先添加目标");
            return;
        }
        
        // 清理空引用
        targetRectTransforms.RemoveAll(t => t == null);
        
        if (targetRectTransforms.Count == 0)
        {
            Debug.LogError("没有有效的目标可以转换！");
            return;
        }
        
        // 过滤有效目标，处理父子关系冲突
        List<RectTransform> validTargets = FilterValidTargets(targetRectTransforms);
        
        if (validTargets.Count == 0)
        {
            Debug.LogError("没有有效的目标可以转换！");
            return;
        }
        
        // 记录批量Undo操作
        Undo.RecordObjects(validTargets.ToArray(), "Position to Anchors");
        
        // 转换所有有效目标
        int successCount = 0;
        foreach (RectTransform rect in validTargets)
        {
            if (ConvertSingleRectTransform(rect))
            {
                successCount++;
            }
        }
        
        Debug.Log($"位置转锚点完成！成功转换 {successCount}/{validTargets.Count} 个目标");
    }
    
    /// <summary>
    /// 应用填充模式
    /// </summary>
    private void ApplyFillMode(FillMode mode)
    {
        if (targetRectTransforms.Count == 0)
        {
            Debug.LogError("目标列表为空！请先添加目标");
            return;
        }
        
        // 清理空引用
        targetRectTransforms.RemoveAll(t => t == null);
        
        // 过滤有效目标
        List<RectTransform> validTargets = FilterValidTargets(targetRectTransforms);
        
        if (validTargets.Count == 0)
        {
            Debug.LogError("没有有效的目标可以应用！");
            return;
        }
        
        // 记录Undo操作
        Undo.RecordObjects(validTargets.ToArray(), $"Apply {mode}");
        
        // 应用填充模式到所有有效目标
        foreach (RectTransform rect in validTargets)
        {
            ApplyFillModeToSingle(rect, mode);
        }
        
        Debug.Log($"已应用填充模式 {mode} 到 {validTargets.Count} 个目标");
    }
    
    /// <summary>
    /// 重置为中心锚点
    /// </summary>
    private void ResetToCenterAnchor()
    {
        if (targetRectTransforms.Count == 0)
        {
            Debug.LogError("目标列表为空！请先添加目标");
            return;
        }
        
        // 清理空引用
        targetRectTransforms.RemoveAll(t => t == null);
        
        if (targetRectTransforms.Count == 0)
        {
            Debug.LogError("没有有效的目标！");
            return;
        }
        
        // 记录Undo操作
        Undo.RecordObjects(targetRectTransforms.ToArray(), "Reset to Center Anchor");
        
        foreach (RectTransform rect in targetRectTransforms)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = knownSize;
        }
        
        Debug.Log($"已重置 {targetRectTransforms.Count} 个目标为中心锚点模式");
    }
    
    // 以下是核心转换方法，与之前的实现相同
    
    /// <summary>
    /// 过滤有效目标，处理父子关系冲突
    /// </summary>
    private List<RectTransform> FilterValidTargets(List<RectTransform> targets)
    {
        List<RectTransform> validTargets = new List<RectTransform>();
        
        foreach (RectTransform target in targets)
        {
            if (target == null) continue;
            
            bool hasChildInList = false;
            bool hasParentInList = false;
            
            foreach (RectTransform other in targets)
            {
                if (other == null || other == target) continue;
                
                if (IsChildOf(other, target))
                {
                    hasChildInList = true;
                    Debug.LogWarning($"检测到父子关系：{target.name} 是 {other.name} 的父物体，将跳过父物体 {target.name}");
                    break;
                }
                
                if (IsChildOf(target, other))
                {
                    hasParentInList = true;
                }
            }
            
            if (!hasChildInList)
            {
                validTargets.Add(target);
                if (hasParentInList)
                {
                    Debug.LogWarning($"检测到父子关系：{target.name} 的父物体也在列表中，将优先处理子物体 {target.name}");
                }
            }
        }
        
        return validTargets;
    }
    
    /// <summary>
    /// 检查target是否是parent的子物体
    /// </summary>
    private bool IsChildOf(RectTransform target, RectTransform parent)
    {
        Transform current = target.parent;
        while (current != null)
        {
            if (current == parent)
                return true;
            current = current.parent;
        }
        return false;
    }
    
    /// <summary>
    /// 转换单个RectTransform
    /// </summary>
    private bool ConvertSingleRectTransform(RectTransform rect)
    {
        if (rect == null)
        {
            Debug.LogError("RectTransform为空");
            return false;
        }
        
        if (rect.parent == null)
        {
            Debug.LogError($"{rect.name} 没有父物体，无法计算相对锚点！");
            return false;
        }
        
        RectTransform parentRect = rect.parent.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogError($"{rect.name} 的父物体不是RectTransform！");
            return false;
        }
        
        // 获取当前物体的世界边界
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        
        Vector3[] parentCorners = new Vector3[4];
        parentRect.GetWorldCorners(parentCorners);
        
        // 转换为父容器的本地坐标，并计算包围盒（解决旋转导致的Min/Max翻转问题）
        Vector2 localMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 localMax = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < 4; i++)
        {
            Vector3 localPoint = parentRect.InverseTransformPoint(corners[i]);
            localMin.x = Mathf.Min(localMin.x, localPoint.x);
            localMin.y = Mathf.Min(localMin.y, localPoint.y);
            localMax.x = Mathf.Max(localMax.x, localPoint.x);
            localMax.y = Mathf.Max(localMax.y, localPoint.y);
        }
        
        Vector2 parentMin = parentRect.InverseTransformPoint(parentCorners[0]);     
        // 同理，父物体也需要确保正确的Min/Max（虽然通常父物体的WorldCorners 0和2是对应MinMax，但为了保险起见）
        // 简单处理：父物体通常作为容器，直接取 Rect 信息可能更准，但这里沿用 InverseTransformPoint 保持一致性
        // 注意：InverseTransformPoint(parentCorners[0]) 在 parentRect 自身坐标系下应该是 (rect.xMin, rect.yMin)
        
        // 直接使用 rect 不受旋转影响的数据来做分母可能更安全，因为我们是在父容器的本地空间计算
        // 父容器的 locally aligned rect 就是它的 local space 范围
        Rect parentLocalRect = parentRect.rect;
        float parentWidth = parentLocalRect.width;
        float parentHeight = parentLocalRect.height;
        
        // 父容器 local space 的 min 通常是 (-pivot.x * width, -pivot.y * height)
        // 我们计算相对于父容器左下角的 normalized pos
        
        // 增加安全检查：父容器尺寸如果为0，会导致除零错误生成NaN/Infinity，进而导致UI变红叉
        if (parentWidth <= 0.001f || parentHeight <= 0.001f)
        {
            Debug.LogError($"父物体 {parentRect.name} 的尺寸过小 ({parentWidth}x{parentHeight})，无法计算相对锚点！请检查父物体是否已并在/LayoutGroup下未刷新。");
            return false;
        }

        // parentMin 应为 parentRect 在自己坐标系下的左下角
        parentMin = new Vector2(parentLocalRect.xMin, parentLocalRect.yMin);
        
        // 计算相对于父容器的比例
        // 归一化坐标 = (LocalPos - ParentMin) / ParentSize
        
        Vector2 anchorMin = new Vector2(
            (localMin.x - parentMin.x) / parentWidth,
            (localMin.y - parentMin.y) / parentHeight
        );
        
        Vector2 anchorMax = new Vector2(
            (localMax.x - parentMin.x) / parentWidth,
            (localMax.y - parentMin.y) / parentHeight
        );
        
        // 修复：确保 Min <= Max (防止浮点误差导致的微小翻转)
        if (anchorMin.x > anchorMax.x) { float temp = anchorMin.x; anchorMin.x = anchorMax.x; anchorMax.x = temp; }
        if (anchorMin.y > anchorMax.y) { float temp = anchorMin.y; anchorMin.y = anchorMax.y; anchorMax.y = temp; }
        
        // 修复：重置旋转和缩放，防止转换后的红叉（因为我们是按照AABB计算的锚点，旋转已无意义且会导致变形）
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        Vector3 finalLocalPos = rect.localPosition;
        rect.localPosition = new Vector3(finalLocalPos.x, finalLocalPos.y, 0f); // Z轴归零

        // 应用新的锚点
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // 标记为已修改
        EditorUtility.SetDirty(rect);
        
        Debug.Log($"{rect.name} 转换完成 (已重置Rotation) - Min: {anchorMin}, Max: {anchorMax}");
        return true;
    }
    
    /// <summary>
    /// 对单个RectTransform应用填充模式
    /// </summary>
    private void ApplyFillModeToSingle(RectTransform rect, FillMode mode)
    {
        // 步骤1：重置基本变换属性，清除之前的任何异常状态
        // 这一步非常关键，很多红叉/不可见问题是因为RectTransform如果不受控制，scale可能为0，或者rotated无法显示
        rect.localScale = Vector3.one; 
        rect.localRotation = Quaternion.identity;
        
        // 强制位置归零，特别是Z轴
        Vector3 localPos = rect.localPosition;
        rect.localPosition = new Vector3(localPos.x, localPos.y, 0f);

        switch (mode)
        {
            case FillMode.StretchBoth:
                SetStretchBoth(rect);
                break;
            case FillMode.StretchWidth:
                SetStretchWidth(rect);
                break;
            case FillMode.StretchHeight:
                SetStretchHeight(rect);
                break;
            case FillMode.KeepAspectRatio:
                SetKeepAspectRatio(rect);
                break;
        }
        
        // 步骤2：再次确保anchoredPosition3D的Z轴为0
        // 有些Stretch模式操作可能会意外改变位置
        Vector3 finalPos = rect.anchoredPosition3D;
        rect.anchoredPosition3D = new Vector3(finalPos.x, finalPos.y, 0f);
    }
    
    /// <summary>
    /// 拉伸宽度和高度
    /// </summary>
    private void SetStretchBoth(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero; // 关键：拉伸模式下 sizeDelta 应该为 0
        rect.anchoredPosition = Vector2.zero;
        
        // 应用边距
        rect.offsetMin = new Vector2(margins.x, margins.w); // left, bottom
        rect.offsetMax = new Vector2(-margins.z, -margins.y); // right, top
    }
    
    /// <summary>
    /// 只拉伸宽度，高度保持固定
    /// </summary>
    private void SetStretchWidth(RectTransform rect)
    {
        // 获取当前高度作为固定高度，如果 height 无效则使用 knownSize
        float height = rect.rect.height;
        if (height <= 0.001f) height = knownSize.y;

        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, height); 

        rect.offsetMin = new Vector2(margins.x, -height * 0.5f);
        rect.offsetMax = new Vector2(-margins.z, height * 0.5f);
    }
    
    /// <summary>
    /// 只拉伸高度，宽度保持固定
    /// </summary>
    private void SetStretchHeight(RectTransform rect)
    {
        // 获取当前宽度
        float width = rect.rect.width;
        if (width <= 0.001f) width = knownSize.x;

        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, 0);
        
        rect.offsetMin = new Vector2(-width * 0.5f, margins.w);
        rect.offsetMax = new Vector2(width * 0.5f, -margins.y);
    }
    
    /// <summary>
    /// 保持宽高比，根据父物体大小自适应
    /// </summary>
    private void SetKeepAspectRatio(RectTransform rect)
    {
        if (rect.parent == null)
        {
            Debug.LogWarning($"{rect.name} 没有父物体，无法计算宽高比适应");
            SetStretchBoth(rect);
            return;
        }
        
        RectTransform parentRect = rect.parent.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogWarning($"{rect.name} 的父物体不是RectTransform");
            SetStretchBoth(rect);
            return;
        }
        
        // 关键逻辑修复：验证 knownSize
        // 如果 knownSize 为 0 或无效，尝试读取当前 rect 的大小
        Vector2 targetSize = knownSize;
        if (targetSize.x <= 0.1f || targetSize.y <= 0.1f)
        {
            targetSize = rect.rect.size;
            // 如果 still invalid，给个默认值防止除零崩溃
            if (targetSize.x <= 0.1f || targetSize.y <= 0.1f)
            {
                 targetSize = new Vector2(100, 100);
                 Debug.LogWarning($"检测到 {rect.name} 没有有效的尺寸信息，已使用默认值 100x100 进行宽高比计算");
            }
        }

        // 获取父物体的尺寸
        Vector2 parentSize = parentRect.rect.size;
        
        // 修复：确保父尺寸有效，防止计算错误导致UI偏移出屏幕/除零
        if (parentSize.x <= 0.001f || parentSize.y <= 0.001f)
        {
            Debug.LogWarning($"父物体 {parentRect.name} 的尺寸过小 ({parentSize})，无法计算宽高比适配，已跳过。");
            return;
        }

        float parentAspect = parentSize.x / parentSize.y;
        float targetAspect = targetSize.x / targetSize.y;
        
        // 安全检查
        if (float.IsNaN(targetAspect) || float.IsInfinity(targetAspect))
        {
            Debug.LogError($"计算出的宽高比无效 (NaN/Infinity)。TargetSize: {targetSize}");
            return;
        }

        if (parentAspect > targetAspect)
        {
            // 父物体更宽，按高度适应
            float targetWidth = parentSize.y * targetAspect;
            float horizontalMargin = (parentSize.x - targetWidth) * 0.5f;
            
            // 安全检查边距
            if (float.IsNaN(horizontalMargin)) horizontalMargin = 0;
            
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalMargin + margins.x, margins.w);
            rect.offsetMax = new Vector2(-horizontalMargin - margins.z, -margins.y); // 注意括号
        }
        else
        {
            // 父物体更高，按宽度适应
            float targetHeight = parentSize.x / targetAspect;
            float verticalMargin = (parentSize.y - targetHeight) * 0.5f;
            
            // 安全检查边距
            if (float.IsNaN(verticalMargin)) verticalMargin = 0;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margins.x, verticalMargin + margins.w);
            rect.offsetMax = new Vector2(-margins.z, -verticalMargin - margins.y); // 注意括号
        }
    }
}
