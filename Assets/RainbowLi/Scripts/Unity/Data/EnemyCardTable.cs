using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/配表/敌人卡牌表")]
 public sealed class EnemyCardTable : CardTableDefinition {
  public void Apply(GameSetup setup){
   ValidateTable(true);var active=rows.Where(r=>r.enabled).ToArray();var specs=active.Select(r=>r.Spec()).ToArray();setup.EnemySkills.Clear();
   float baseWeight=active.Where(r=>!r.special).Average(r=>r.weight);
   for(int i=0;i<active.Length;i++){var r=active[i];setup.EnemySkills.Add(new EnemySkill {Name=r.name,Number=r.number,Effect=r.effect,Spec=specs[i],Weight=r.special?baseWeight*r.specialWeightRatio:r.weight,Special=r.special,SpecialWeightRatio=r.specialWeightRatio,Condition=r.condition,Threshold=r.threshold});}
  }
 }
}
