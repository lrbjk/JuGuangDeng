using UnityEngine;
namespace RainbowLi.UnityGame {
[CreateAssetMenu(menuName="RainbowLi/牌序定义")]
public sealed class SequenceDefinition : ScriptableObject {
 public string displayName = "新牌序";
 public int[] numbers = {1,2,3};
 public RuleEffect effect;
 [Min(0)] public int value = 10;
 public bool insertTwo;
 public PatternRequirement requirement;
 public bool secondEffectEnabled;public SequenceEffect secondEffect=new SequenceEffect();
 public SequenceRule CreateRule() => new SequenceRule { Name=displayName, Numbers=(int[])numbers.Clone(), Effect=effect, Value=value, InsertsTwo=insertTwo,Requirement=requirement,AdditionalEffect=!secondEffectEnabled?null:new SequenceEffect {Effect=secondEffect.Effect,Value=secondEffect.Value,InsertsTwo=secondEffect.InsertsTwo,Requirement=secondEffect.Requirement} };
 private void OnValidate() {
  if(numbers == null || numbers.Length != 3) numbers = new[]{1,2,3};
  for(int i=0;i<3;i++) numbers[i]=Mathf.Clamp(numbers[i],0,6);
 }
}}
