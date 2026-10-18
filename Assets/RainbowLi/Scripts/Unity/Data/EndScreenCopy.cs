using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/结算文案",fileName="EndScreenCopy")]
 public sealed class EndScreenCopy:ScriptableObject {
  [Header("游戏成功")] public string successTitle;
  [TextArea(6,16)] public string successBody;
  [Header("游戏失败")] public string failureTitle;
  [TextArea(6,16)] public string failureBody;
 }
}
