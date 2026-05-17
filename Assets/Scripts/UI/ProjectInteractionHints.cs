public static class ProjectInteractionHints
{
    public static string Inspect => $"按 {GameplayInputUtil.GetInteractDisplayName()} 调查";
    public static string Interact => $"按 {GameplayInputUtil.GetInteractDisplayName()} 互动";
    public static string Travel => $"按 {GameplayInputUtil.GetInteractDisplayName()} 前往";
    public static string Rotate => $"按 {GameplayInputUtil.GetInteractDisplayName()} 转动";
    public static string Deliver => $"按 {GameplayInputUtil.GetInteractDisplayName()} 交付";
    public static string PickupDirect => $"按 {GameplayInputUtil.GetPickupDisplayName()} 拾取";
    public static string PickupInspect => $"按 {GameplayInputUtil.GetInteractDisplayName()} 调查";
    public static string Close => $"按 {GameplayInputUtil.GetCancelDisplayName()} 关闭";
    public static string PopupClose => $"按 {GameplayInputUtil.GetCancelDisplayName()} 关闭";
    public static string PopupPickup => $"按 {GameplayInputUtil.GetPickupDisplayName()} 拾取";
    public static string Continue => $"- 按 {GameplayInputUtil.GetInteractDisplayName()} 继续 -";
}
