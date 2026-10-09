using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/配表/敌人卡牌表")]
 public sealed class EnemyCardTable : CardTableDefinition {
  public void Apply(GameSetup setup){
   ValidateTable(true);setup.EnemySkills.Clear();var active=rows.Where(r=>r.enabled).ToArray();
   float baseWeight=active.Where(r=>!r.special).Average(r=>r.weight);
   foreach(var r in active)setup.EnemySkills.Add(new EnemySkill {Name=r.name,Number=r.number,Effect=r.effect,Spec=r.Spec(),Weight=r.special?baseWeight*r.specialWeightRatio:r.weight,Special=r.special,SpecialWeightRatio=r.specialWeightRatio,Condition=r.condition,Threshold=r.threshold});
  }
 }
}
