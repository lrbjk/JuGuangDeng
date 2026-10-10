using UnityEditor;
namespace RainbowLi.EditorTools {
 public sealed class SequenceEffectEditorWindow : EffectTableEditorWindow {
  protected override bool Sequence=>true;
  [MenuItem("RainbowLi/配表/牌序效果编辑器")]
  public static void Open(){var w=GetWindow<SequenceEffectEditorWindow>("牌序效果配表");w.OpenDefault();w.Show();}
 }
}
