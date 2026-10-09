using UnityEditor;
namespace RainbowLi.EditorTools {
 public sealed class EnemyCardEditorWindow : CardTableEditorWindow {
  protected override bool Enemy=>true;
  [MenuItem("RainbowLi/配表/敌人卡牌编辑器")]
  public static void Open(){var window=GetWindow<EnemyCardEditorWindow>("敌人卡牌表");window.OpenDefault();}
 }
}
