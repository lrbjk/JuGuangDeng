using UnityEditor;
namespace RainbowLi.EditorTools {
 public sealed class EngravingEditorWindow : EffectTableEditorWindow {
  protected override bool Sequence=>false;
  [MenuItem("RainbowLi/配表/铭刻效果编辑器")]
  public static void Open(){var w=GetWindow<EngravingEditorWindow>("铭刻效果配表");w.OpenDefault();w.Show();}
 }
}
