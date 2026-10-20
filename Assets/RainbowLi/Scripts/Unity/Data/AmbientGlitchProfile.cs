using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/花屏表现配置")]
 public sealed class AmbientGlitchProfile : ScriptableObject {
  public bool effectEnabled=true;
  [Tooltip("间隔随机区间；两个值相同即为固定间隔。单位秒。")][Min(1)] public float minIntervalSeconds=120,maxIntervalSeconds=240;
  [Tooltip("一次花屏持续的真实时间，单位秒。")][Min(.05f)] public float duration=.65f;
  [Range(0,1)] public float intensity=.9f;
  [Tooltip("横带最大错位占屏幕宽度的比例。")][Range(0,.3f)] public float horizontalShift=.14f;
  [Range(0,1)] public float noiseOpacity=.75f;
  [Tooltip("每秒更新干扰图案的次数。")][Range(1,40)] public int changesPerSecond=24;
  public void Validate(){
   minIntervalSeconds=Finite(minIntervalSeconds,120,1);maxIntervalSeconds=Finite(maxIntervalSeconds,240,minIntervalSeconds);
   duration=Finite(duration,.65f,.05f);intensity=Mathf.Clamp01(Finite(intensity,.9f,0));horizontalShift=Mathf.Clamp(Finite(horizontalShift,.14f,0),0,.3f);noiseOpacity=Mathf.Clamp01(Finite(noiseOpacity,.75f,0));changesPerSecond=Mathf.Clamp(changesPerSecond,1,40);
  }
  private static float Finite(float value,float fallback,float min)=>float.IsNaN(value)||float.IsInfinity(value)?Mathf.Max(min,fallback):Mathf.Max(min,value);
  private void OnValidate(){Validate();}
 }
}
