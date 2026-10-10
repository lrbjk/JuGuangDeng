using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class CardCompoundExamples {
  const string Folder="Assets/RainbowLi/Data/Examples";
  [MenuItem("RainbowLi/配表/创建复合效果配置示例")]
  public static void Create(){
   if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/RainbowLi/Data","Examples");
   if(AssetDatabase.LoadAssetAtPath<PlayerCardTable>(Folder+"/CompoundPlayerCards.asset")==null){var p=ScriptableObject.CreateInstance<PlayerCardTable>();p.rows=new[]{Example("demo_player","示例 / 攻击后护盾",CardEffect.Guard,"护盾 X","4")};AssetDatabase.CreateAsset(p,Folder+"/CompoundPlayerCards.asset");}
   if(AssetDatabase.LoadAssetAtPath<EnemyCardTable>(Folder+"/CompoundEnemyCards.asset")==null){var e=ScriptableObject.CreateInstance<EnemyCardTable>();e.rows=new[]{Example("demo_enemy","示例 / 攻击后施加烧伤",CardEffect.Burn,"对对手施加 X 层烧伤","2")};e.rows[0].additionalEffects[0].lifetime=EffectLifetime.Persistent;e.rows[0].additionalEffects[0].polarity=EffectPolarity.Negative;AssetDatabase.CreateAsset(e,Folder+"/CompoundEnemyCards.asset");}
   AssetDatabase.SaveAssets();Selection.activeObject=AssetDatabase.LoadAssetAtPath<PlayerCardTable>(Folder+"/CompoundPlayerCards.asset");
  }
  static CardTableRow Example(string id,string name,CardEffect extra,string text,string range)=>new CardTableRow{id=id,name=name,description="对对手造成 X 点伤害，然后执行追加效果",range="6",min=6,max=6,effect=CardEffect.Strike,number=3,additionalEffects=new[]{new CardTableEffect{effect=extra,description=text,range=range}}};
 }
}
