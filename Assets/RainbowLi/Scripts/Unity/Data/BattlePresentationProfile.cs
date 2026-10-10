using UnityEngine;
namespace RainbowLi.UnityGame {
[CreateAssetMenu(menuName="RainbowLi/战斗动效配置")]
public sealed class BattlePresentationProfile:ScriptableObject {
 [Header("卡牌飞行：时长单位为秒，距离单位为 UI 像素")]
 [Min(.05f)] public float drawDuration=.46f,placeDuration=.34f,discardDuration=.38f;
 [Tooltip("每张牌依次飞出的间隔。越大，发牌越慢。")][Range(0,.3f)]public float dealStagger=.065f;
 [Range(0,300)]public float flightArc=90;
 [Range(0,45)]public float flightRotation=14;
 [Header("攻击过程")]
 [Min(.01f)]public float chargeDuration=.18f,projectileDuration=.22f,impactDuration=.3f;
 [Tooltip("卡牌向目标突进的距离占比，0 为不突进。")][Range(0,1)]public float attackLunge=.3f;
 [Tooltip("横轴为伤害数值，纵轴为视觉强度 0～1。只影响表现，不修改伤害。")]
 public AnimationCurve damageIntensity=DefaultDamageCurve();
 [Header("连续效果的夸张程度")]
 [Tooltip("同一方效果在此时间内连续触发时，视为连锁。")][Min(.1f)]public float chainWindow=1.25f;
 [Range(1,4)]public float maxChainGain=2.4f,maxComboGain=2.8f;
 [Tooltip("越小，连锁越快接近最大强度。")][Min(.1f)]public float chainRiseActions=3,comboRiseActions=3.5f;
 [Header("命中、飘字和粒子")]
 [Min(.1f)]public float burstDuration=1.15f;
 [Tooltip("非飞牌模式下的命中飞行时间；普通模式由攻击投射物决定。")][Min(.01f)]public float fallbackTravelDuration=.14f;
 [Range(0,90)]public int maxParticles=60;
 [Range(1,20)]public int maxConcurrentBursts=10;
 [Min(0)]public float particleSpread=160,particleSize=6;
 [Range(12,72)]public int damageTextBaseSize=24;
 [Range(0,48)]public int damageTextGrowth=24;
 [Min(0)]public float damageTextRise=65;
 [Header("震屏与闪光")]
 public bool screenShake=true,hitFlash=true;
 [Range(0,60)]public float maxShake=28;
 [Min(.01f)]public float shakeDuration=.45f,flashDuration=.3f;
 [Range(0,2)]public float shakeStrength=1,flashStrength=1;
 [Header("效果颜色")]
 public Color attackColor=new Color(1,.35f,.2f),shieldColor=new Color(.25f,.7f,1),healColor=new Color(.3f,1,.64f),statusColor=new Color(.75f,.4f,1);
 public Color sparkColor=new Color(1,.48f,.43f),sparkAccentColor=new Color(1,.85f,.48f);
 [Header("命中音效：留空沿用组件音效或生成的默认音效")]
 public AudioClip lightHit,heavyHit;
 [Range(0,1)]public float soundVolume=1;
 [Min(.1f)]public float lightPitch=1.2f,heavyPitch=.7f;
 [Header("播放节奏与长连锁加速")]
 [Tooltip("1×速度时的普通行动和牌序行动间隔，不影响规则数值。")][Min(.05f)]public float actionInterval=.95f,ruleInterval=.16f;
 public bool accelerateRepeats=true;
 [Tooltip("前 N 次重复保持原速，从下一次开始逐步加速。")][Min(1)]public int accelerateAfter=6;
 [Range(.05f,1)]public float speedGainPerRepeat=.25f;
 [Range(1,12)]public float maxRepeatSpeed=6;
 public float DamageStrength(int damage)=>damageIntensity==null?1-Mathf.Exp(-Mathf.Max(0,damage)/18f):Mathf.Clamp01(damageIntensity.Evaluate(Mathf.Max(0,damage)));
 public static AnimationCurve DefaultDamageCurve(){var curve=new AnimationCurve();foreach(float damage in new[]{0f,5f,10f,18f,30f,50f,80f,160f})curve.AddKey(damage,1-Mathf.Exp(-damage/18f));for(int i=0;i<curve.length;i++)curve.SmoothTangents(i,0);curve.preWrapMode=curve.postWrapMode=WrapMode.ClampForever;return curve;}
 void OnValidate(){drawDuration=Mathf.Max(.05f,drawDuration);placeDuration=Mathf.Max(.05f,placeDuration);discardDuration=Mathf.Max(.05f,discardDuration);chargeDuration=Mathf.Max(.01f,chargeDuration);projectileDuration=Mathf.Max(.01f,projectileDuration);impactDuration=Mathf.Max(.01f,impactDuration);chainWindow=Mathf.Max(.1f,chainWindow);chainRiseActions=Mathf.Max(.1f,chainRiseActions);comboRiseActions=Mathf.Max(.1f,comboRiseActions);fallbackTravelDuration=Mathf.Max(.01f,fallbackTravelDuration);burstDuration=Mathf.Max(fallbackTravelDuration+.05f,burstDuration);shakeDuration=Mathf.Max(.01f,shakeDuration);flashDuration=Mathf.Max(.01f,flashDuration);actionInterval=Mathf.Max(.05f,actionInterval);ruleInterval=Mathf.Max(.05f,ruleInterval);accelerateAfter=Mathf.Max(1,accelerateAfter);maxRepeatSpeed=Mathf.Max(1,maxRepeatSpeed);maxConcurrentBursts=Mathf.Max(1,maxConcurrentBursts);maxParticles=Mathf.Max(0,maxParticles);}
}
}
