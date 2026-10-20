using UnityEngine;
namespace RainbowLi.UnityGame {
[CreateAssetMenu(menuName="RainbowLi/牌序定义")]
public sealed class SequenceDefinition : ScriptableObject {
 public string displayName = "新牌序";
 public int[] numbers = {1,2,3};
 public RuleEffect effect;
 [Min(0)] public int value = 10;
 [Tooltip("手工铁壁效果的护盾增幅；绑定配表时使用表格值")][Range(0,10000)]public int shieldBonusPercent=25;public bool insertTwo;
 public PatternRequirement requirement;
 public bool secondEffectEnabled;public SequenceEffect secondEffect=new SequenceEffect();
 public SequenceEffectTable effectTable;
 public string primaryEffectId,secondaryEffectId;
 [Tooltip("0 使用配表范围下限")][Min(0)] public int primaryX,secondaryX;
 public SequenceRule CreateRule(){
  var primary=Resolve(primaryEffectId,primaryX,new SequenceEffect {Effect=effect,ShieldBonusPercent=shieldBonusPercent,Value=value,InsertsTwo=insertTwo,Requirement=requirement});
  var secondary=!secondEffectEnabled?null:Resolve(secondaryEffectId,secondaryX,secondEffect);
  return new SequenceRule {Name=displayName,ShieldBonusPercent=primary.ShieldBonusPercent,Code=primary.Code,BaseX=primary.BaseX,Multiplier=primary.Multiplier,FixedValue=primary.FixedValue,MinX=primary.MinX,MaxX=primary.MaxX,RollX=primary.RollX,Numbers=(int[])numbers.Clone(),Effect=primary.Effect,Value=primary.Value,InsertsTwo=primary.InsertsTwo,Requirement=primary.Requirement,AdditionalEffect=secondary==null?null:new SequenceEffect {ShieldBonusPercent=secondary.ShieldBonusPercent,Code=secondary.Code,BaseX=secondary.BaseX,Multiplier=secondary.Multiplier,FixedValue=secondary.FixedValue,MinX=secondary.MinX,MaxX=secondary.MaxX,RollX=secondary.RollX,Effect=secondary.Effect,Value=secondary.Value,InsertsTwo=secondary.InsertsTwo,Requirement=secondary.Requirement}};
 }
 private SequenceEffect Resolve(string id,int x,SequenceEffect fallback){if(effectTable==null||string.IsNullOrWhiteSpace(id))return fallback;var row=effectTable.Find(id);row.TryRange(out int min,out _);return effectTable.CreateEffect(id,x,fallback==null?PatternRequirement.Any:fallback.Requirement);}
 private void OnValidate() {
  if(numbers == null || numbers.Length != 3) numbers = new[]{1,2,3};
  for(int i=0;i<3;i++) numbers[i]=Mathf.Clamp(numbers[i],0,6);
 }
}}
