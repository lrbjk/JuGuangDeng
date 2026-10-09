using UnityEditor;
namespace RainbowLi.EditorTools {
 public sealed class PlayerCardEditorWindow : CardTableEditorWindow {
  protected override bool Enemy=>false;
  [MenuItem("RainbowLi/配表/玩家卡牌编辑器")]
  public static void Open(){var window=GetWindow<PlayerCardEditorWindow>("玩家卡牌表");window.OpenDefault();}
 }
}
