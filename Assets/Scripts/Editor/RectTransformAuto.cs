using System.Collections.Generic;
using UnityEngine;

public class RectTransformAuto : MonoBehaviour
{
    [Header("目标RectTransform列表")]
    public List<RectTransform> targetRectTransforms = new List<RectTransform>();
    
    [Header("已知的宽高（中心锚点时）")]
    public Vector2 knownSize = new Vector2(100, 100);
    
    [Header("填充模式")]
    public FillMode fillMode = FillMode.KeepAspectRatio;
    
    [Header("边距设置")]
    public Vector4 margins = Vector4.zero; // left, top, right, bottom
    
    public enum FillMode
    {
        StretchBoth,        // 拉伸宽高
        StretchWidth,       // 只拉伸宽度
        StretchHeight,      // 只拉伸高度
        KeepAspectRatio     // 保持宽高比
    }
    
    [Header("运行时操作")]
    [Space(10)]
    public bool autoApplyOnStart = false;
    
    void Start()
    {
        if (autoApplyOnStart)
        {
            ApplyFillMode();
        }
    }
    
    /// <summary>
    /// 应用填充模式
    /// </summary>
    [ContextMenu("应用填充模式")]
    public void ApplyFillMode()
    {
        if (targetRectTransforms == null || targetRectTransforms.Count == 0)
        {
            // 如果列表为空，尝试添加当前物体
            RectTransform currentRect = GetComponent<RectTransform>();
            if (currentRect != null)
            {
                targetRectTransforms.Add(currentRect);
            }
            else
            {
                Debug.LogError("未找到目标RectTransform！请添加目标到列表中");
                return;
            }
        }
        
        // 清理空引用
        targetRectTransforms.RemoveAll(t => t == null);
        
        if (targetRectTransforms.Count == 0)
        {
            Debug.LogError("目标列表为空！");
            return;
        }
        
        // 检查并处理父子关系冲突
        List<RectTransform> validTargets = FilterValidTargets(targetRectTransforms);
        
        // 应用填充模式到所有有效目标
        foreach (RectTransform rect in validTargets)
        {
            ApplyFillModeToSingle(rect);
        }
        
        Debug.Log($"已应用填充模式 {fillMode} 到 {validTargets.Count} 个目标");
    }
    
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
            
            // 检查是否有子物体在列表中
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
                    // 不直接break，因为还要检查其他关系
                }
            }
            
            // 优先处理子物体，跳过有子物体在列表中的父物体
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
    /// 对单个RectTransform应用填充模式
    /// </summary>
    private void ApplyFillModeToSingle(RectTransform rect)
    {
        // 修复：重置Z轴坐标和缩放，防止UI不可见或位置异常
        Vector3 pos = rect.anchoredPosition3D;
        rect.anchoredPosition3D = new Vector3(pos.x, pos.y, 0f);
        rect.localScale = Vector3.one;

        switch (fillMode)
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
    }
    
    /// <summary>
    /// 拉伸宽度和高度
    /// </summary>
    private void SetStretchBoth(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margins.x, margins.w); // left, bottom
        rect.offsetMax = new Vector2(-margins.z, -margins.y); // right, top
    }
    
    /// <summary>
    /// 只拉伸宽度，高度保持固定
    /// </summary>
    private void SetStretchWidth(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.offsetMin = new Vector2(margins.x, -knownSize.y * 0.5f);
        rect.offsetMax = new Vector2(-margins.z, knownSize.y * 0.5f);
    }
    
    /// <summary>
    /// 只拉伸高度，宽度保持固定
    /// </summary>
    private void SetStretchHeight(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.offsetMin = new Vector2(-knownSize.x * 0.5f, margins.w);
        rect.offsetMax = new Vector2(knownSize.x * 0.5f, -margins.y);
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
        
        // 获取父物体的尺寸
        Vector2 parentSize = parentRect.rect.size;
        
        // 修复：确保父尺寸有效，防止计算错误导致UI偏移出屏幕/除零
        if (parentSize.x <= 0.001f || parentSize.y <= 0.001f)
        {
            Debug.LogWarning($"父物体 {parentRect.name} 的尺寸过小 ({parentSize})，无法计算宽高比适配，已跳过。");
            return;
        }

        float parentAspect = parentSize.x / parentSize.y;
        float targetAspect = knownSize.x / knownSize.y;
        
        if (targetAspect <= 0.001f) // 防止 knownSize 无效
        {
            return;
        }
        
        if (parentAspect > targetAspect)
        {
            // 父物体更宽，按高度适应
            float targetWidth = parentSize.y * targetAspect;
            float horizontalMargin = (parentSize.x - targetWidth) * 0.5f;
            
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalMargin + margins.x, margins.w);
            rect.offsetMax = new Vector2(-horizontalMargin - margins.z, -margins.y);
        }
        else
        {
            // 父物体更高，按宽度适应
            float targetHeight = parentSize.x / targetAspect;
            float verticalMargin = (parentSize.y - targetHeight) * 0.5f;
            
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margins.x, verticalMargin + margins.w);
            rect.offsetMax = new Vector2(-margins.z, -verticalMargin - margins.y);
        }
    }
    
    /// <summary>
    /// 重置为中心锚点模式
    /// </summary>
    [ContextMenu("重置为中心锚点")]
    public void ResetToCenterAnchor()
    {
        if (targetRectTransforms == null || targetRectTransforms.Count == 0)
        {
            RectTransform currentRect = GetComponent<RectTransform>();
            if (currentRect != null)
            {
                targetRectTransforms.Add(currentRect);
            }
            else
            {
                Debug.LogError("未找到RectTransform！");
                return;
            }
        }
        
        // 清理空引用
        targetRectTransforms.RemoveAll(t => t == null);
        
        foreach (RectTransform rect in targetRectTransforms)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = knownSize;
        }
        
        Debug.Log($"已重置 {targetRectTransforms.Count} 个目标为中心锚点模式");
    }
    
    /// <summary>
    /// 从当前状态获取尺寸（如果当前是中心锚点）
    /// </summary>
    [ContextMenu("获取当前尺寸")]
    public void GetCurrentSize()
    {
        RectTransform currentRect = GetComponent<RectTransform>();
        if (currentRect == null)
        {
            Debug.LogError("未找到RectTransform组件！");
            return;
        }
        
        // 检查是否是中心锚点
        Vector2 anchorMin = currentRect.anchorMin;
        Vector2 anchorMax = currentRect.anchorMax;
        
        if (Mathf.Approximately(anchorMin.x, 0.5f) && Mathf.Approximately(anchorMin.y, 0.5f) &&
            Mathf.Approximately(anchorMax.x, 0.5f) && Mathf.Approximately(anchorMax.y, 0.5f))
        {
            knownSize = currentRect.sizeDelta;
            Debug.Log($"当前尺寸: {knownSize}");
        }
        else
        {
            knownSize = currentRect.rect.size;
            Debug.Log($"当前rect尺寸: {knownSize} (注意：不是中心锚点模式)");
        }
    }
    
    /// <summary>
    /// 添加目标RectTransform
    /// </summary>
    public void AddTarget(RectTransform target)
    {
        if (target != null && !targetRectTransforms.Contains(target))
        {
            targetRectTransforms.Add(target);
            Debug.Log($"已添加目标: {target.name}");
        }
    }
    
    /// <summary>
    /// 移除目标RectTransform
    /// </summary>
    public void RemoveTarget(RectTransform target)
    {
        if (targetRectTransforms.Contains(target))
        {
            targetRectTransforms.Remove(target);
            Debug.Log($"已移除目标: {target.name}");
        }
    }
    
    /// <summary>
    /// 清空目标列表
    /// </summary>
    public void ClearTargets()
    {
        int count = targetRectTransforms.Count;
        targetRectTransforms.Clear();
        Debug.Log($"已清空目标列表，移除了 {count} 个目标");
    }
    
    /// <summary>
    /// 设置边距
    /// </summary>
    public void SetMargins(float left, float top, float right, float bottom)
    {
        margins = new Vector4(left, top, right, bottom);
    }
    
    /// <summary>
    /// 设置统一边距
    /// </summary>
    public void SetUniformMargin(float margin)
    {
        margins = new Vector4(margin, margin, margin, margin);
    }
}
