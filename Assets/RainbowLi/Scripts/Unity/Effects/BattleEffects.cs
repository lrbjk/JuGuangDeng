using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class BattleEffects : MonoBehaviour {
 public GameSession session;
 public GameView gameView;
 public RectTransform overlay,shakeRoot,playerTarget,enemyTarget,ruleOrigin;
 public CardView[] actionViews;
 public Image particlePrefab;
 public Text numberPrefab,comboLabel;
 public Image playerFill,enemyFill;
 public AudioSource audioSource;
 public AudioClip lightHit,heavyHit;
 [Range(0,40)] public float maxShake=28;
 [Range(4,90)] public int maxParticles=60;
 [Range(1,20)] public int maxConcurrentBursts=10;
 [Min(0.1f)] public float chainWindow=1.25f;
 [Range(1,4)] public float maxComboGain=2.8f;
 private sealed class Burst {public float started,strength,gain;public bool targetEnemy,aura;public Vector2 from,target;public RectTransform beam;public Text number;public readonly List<Image> particles=new List<Image>();}
 private readonly List<Burst> bursts=new List<Burst>();
 private int combo,lastRound;
 private bool lastEnemy;
 private float lastEffect=-10;
 private Vector3 restPosition;
 private Color playerColor,enemyColor;
 private ContainmentGame observedGame;
 private AudioClip generatedHit;
 private void Awake(){restPosition=shakeRoot.localPosition;playerColor=playerFill.color;enemyColor=enemyFill.color;}
 private void OnEnable(){session.Resolved+=OnResolution;session.Changed+=CheckReset;}
 private void OnDisable(){session.Resolved-=OnResolution;session.Changed-=CheckReset;Clear();}
 private void CheckReset(){if(observedGame!=session.Game){observedGame=session.Game;Clear();}}
 private void Clear(){foreach(var b in bursts)DestroyBurst(b);bursts.Clear();combo=0;lastEffect=-10;if(shakeRoot!=null)shakeRoot.localPosition=restPosition;if(comboLabel!=null)comboLabel.text="";if(playerFill!=null)playerFill.color=playerColor;if(enemyFill!=null)enemyFill.color=enemyColor;}
 private Vector2 Point(RectTransform rect){return overlay.InverseTransformPoint(rect.TransformPoint(rect.rect.center));}
 private void OnResolution(ResolutionEvent e){
  if(gameView!=null)actionViews=gameView.actions;
  if(e.Action==null)return;
  if(!e.Action.IsNumberOnly){combo=lastRound==e.Round&&lastEnemy==e.Action.Enemy&&Time.unscaledTime-lastEffect<=chainWindow?Mathf.Min(99,combo+1):1;lastRound=e.Round;lastEnemy=e.Action.Enemy;lastEffect=Time.unscaledTime;}
  if(combo>=2)comboLabel.text=(lastEnemy?"敌方":"你的")+"效果连锁 ×"+combo+(combo>=5?"  爆发！":"");
  int damage=e.EnemyDamage+e.EnemyBlocked+e.PlayerDamage+e.PlayerBlocked;
  int raw=e.Action.Rule!=null?e.Action.Rule.Value:e.Action.Card!=null?e.Action.Card.Value:e.Action.Value;
  int nominal=e.Action.IsStatusTick?raw:(e.Action.Enemy?session.Game.Enemy: (CombatantState)session.Game.Player).OutgoingDamage(raw+(e.Action.Enemy?session.Game.EnemyPower:session.Game.Power));
  Vector2 from=e.Slot>=0&&e.Slot<actionViews.Length?Point((RectTransform)actionViews[e.Slot].transform):Point(ruleOrigin);
  if(e.EnemyDamage+e.EnemyBlocked>0||e.EnemyParried)SpawnBurst(from,Point(enemyTarget),e.EnemyParried?nominal:e.EnemyDamage+e.EnemyBlocked,e.EnemyDamage,false,true);
  if(e.PlayerDamage+e.PlayerBlocked>0||e.PlayerParried)SpawnBurst(from,Point(playerTarget),e.PlayerParried?nominal:e.PlayerDamage+e.PlayerBlocked,e.PlayerDamage,false,false);
  if(damage==0&&!e.PlayerParried&&!e.EnemyParried){if(combo>=2)SpawnBurst(Point(ruleOrigin),Point(ruleOrigin),0,0,true,false);return;}
  bool attack=e.Action.Rule!=null?e.Action.Rule.Effect==RuleEffect.Damage:e.Action.Effect==CardEffect.Strike||e.Action.Effect==CardEffect.IncreasedStrike||e.Action.Effect==CardEffect.InverseStrike;
  if(!e.Action.IsStatusTick&&attack&&(e.Action.Enemy?session.Game.EnemyHealth:session.Game.Health)>0&&e.Slot>=0&&e.Slot<actionViews.Length){var target=e.Action.Enemy?playerTarget:enemyTarget;actionViews[e.Slot].AttackToward(target.TransformPoint(target.rect.center),12+36*(1-Mathf.Exp(-nominal/18f)));}
 }
 private void SpawnBurst(Vector2 from,Vector2 target,int strengthDamage,int healthDamage,bool aura,bool toEnemy){
  if(bursts.Count>=maxConcurrentBursts){DestroyBurst(bursts[0]);bursts.RemoveAt(0);}
  float strength=1-Mathf.Exp(-strengthDamage/18f),gain=1+(maxComboGain-1)*(1-Mathf.Exp(-Mathf.Max(0,combo-1)/3.5f));
  var b=new Burst{started=Time.unscaledTime,strength=strength,gain=gain,from=from,target=target,targetEnemy=toEnemy,aura=aura};
  var beam=Instantiate(particlePrefab,overlay);b.beam=(RectTransform)beam.transform;beam.color=aura?new Color(.92f,.75f,.4f):new Color(1,.48f,.43f);
  b.number=Instantiate(numberPrefab,overlay);b.number.text=aura?"牌序叠加":healthDamage==0?"格挡":"伤害 "+healthDamage;b.number.fontSize=Mathf.RoundToInt((24+strength*24)*Mathf.Sqrt(gain));
  int count=Mathf.Min(maxParticles,Mathf.RoundToInt((8+strength*36)*gain));
  for(int i=0;i<count;i++){var spark=Instantiate(particlePrefab,overlay);spark.color=i%3==0?new Color(1,.85f,.48f):new Color(1,.48f,.43f);b.particles.Add(spark);}
  bursts.Add(b);
  if(!aura&&audioSource!=null){var clip=strength*gain>.65f?heavyHit:lightHit;if(clip==null){if(generatedHit==null)generatedHit=MakeHit();clip=generatedHit;}audioSource.pitch=Mathf.Lerp(1.2f,.7f,Mathf.Clamp01(strength*gain));audioSource.PlayOneShot(clip,Mathf.Lerp(.4f,1,strength));}
 }
 private void LateUpdate(){
  Vector2 shake=Vector2.zero;float playerFlash=0,enemyFlash=0;
  for(int n=bursts.Count-1;n>=0;n--){var b=bursts[n];float age=Time.unscaledTime-b.started;
   if(age>1.15f){DestroyBurst(b);bursts.RemoveAt(n);continue;}
   float hit=Mathf.Max(0,age-.14f),fade=Mathf.Clamp01(1-hit),radius=(25+hit*160)*(0.5f+b.strength)*b.gain;
   Vector2 head=Vector2.Lerp(b.from,b.target,Mathf.Clamp01(age/.14f)),tail=Vector2.Lerp(b.from,b.target,Mathf.Clamp01(age/.14f-.3f));
   b.beam.anchoredPosition=(head+tail)/2;b.beam.sizeDelta=new Vector2(Vector2.Distance(head,tail),3+12*b.strength*b.gain);b.beam.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(head.y-tail.y,head.x-tail.x)*Mathf.Rad2Deg);b.beam.gameObject.SetActive(age<.2f);
   b.number.rectTransform.anchoredPosition=b.target+Vector2.up*(38+hit*65);var nc=b.number.color;nc.a=fade;b.number.color=nc;
   for(int i=0;i<b.particles.Count;i++){var image=b.particles[i];var r=(RectTransform)image.transform;float angle=i*2.39996f;Vector2 dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));r.anchoredPosition=b.target+dir*radius*(.55f+i%7*.1f)+Vector2.down*85*hit*hit;r.sizeDelta=Vector2.one*(3+b.strength*6)*Mathf.Sqrt(b.gain)*fade;var c=image.color;c.a=fade*fade;image.color=c;image.gameObject.SetActive(age>=.14f);}
   if(!b.aura&&age>=.14f&&hit<.3f){float flash=(1-hit/.3f)*(.4f+b.strength*.6f);if(b.targetEnemy)enemyFlash=Mathf.Max(enemyFlash,flash);else playerFlash=Mathf.Max(playerFlash,flash);}
   if(age>=.14f&&hit<.45f)shake+=new Vector2(Mathf.Sin(hit*93),Mathf.Sin(hit*71+1.8f))*(1.5f+9.5f*b.strength)*b.gain*Mathf.Exp(-hit*9);
  }
  playerFill.color=Color.Lerp(playerColor,Color.white,playerFlash);enemyFill.color=Color.Lerp(enemyColor,Color.white,enemyFlash);
  shake=Vector2.ClampMagnitude(shake,maxShake);shakeRoot.localPosition=restPosition+new Vector3(shake.x,shake.y,0);
  float comboAge=Time.unscaledTime-lastEffect;if(comboAge>1.1f)comboLabel.text="";else{comboLabel.fontSize=Mathf.RoundToInt(30+Mathf.Min(combo,8)*3+Mathf.Sin(Mathf.Clamp01(comboAge/.28f)*Mathf.PI)*10);var c=comboLabel.color;c.a=Mathf.Clamp01((1.1f-comboAge)/.4f);comboLabel.color=c;}
 }
 private void DestroyBurst(Burst b){if(b.beam!=null)Destroy(b.beam.gameObject);if(b.number!=null)Destroy(b.number.gameObject);foreach(var p in b.particles)if(p!=null)Destroy(p.gameObject);}
 private AudioClip MakeHit(){const int rate=22050;var data=new float[4400];uint noise=314159;for(int i=0;i<data.Length;i++){noise=noise*1664525u+1013904223u;float t=(float)i/rate,p=(float)i/data.Length;data[i]=(((noise>>8)/16777215f*2-1)*.3f+Mathf.Sin(2*Mathf.PI*100*t)*.6f)*Mathf.Pow(1-p,3)*Mathf.Min(1,t*1500);}var clip=AudioClip.Create("RainbowLi native hit",data.Length,1,rate,false);clip.SetData(data,0);return clip;}
 private void OnDestroy(){if(generatedHit!=null)Destroy(generatedHit);}
}}
