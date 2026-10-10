using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
[DisallowMultipleComponent]
public sealed class CardMotionController : MonoBehaviour {
 [Header("场景引用")]
 public GameView view;
 public BattleEffects effects;
 public CardView cardPrefab;
 public RectTransform motionLayer,drawPile,discardPile,playerTarget,enemyTarget,ruleOrigin;
 [Header("卡牌飞行（秒，跟随播放速度）")]
 [Min(.05f)] public float drawDuration=.46f,placeDuration=.34f,discardDuration=.38f;
 [Range(0,.2f)] public float dealStagger=.065f;
 [Range(0,240)] public float flightArc=90;
 [Header("蓄力、攻击和命中")]
 [Min(.01f)] public float chargeDuration=.18f,projectileDuration=.22f,impactDuration=.3f;
 [Range(0,1)] public float attackLunge=.3f;
 [Range(1,3)] public float maxChainGain=2.4f;
 public Color attackColor=new Color(1,.35f,.2f),shieldColor=new Color(.25f,.7f,1),healColor=new Color(.3f,1,.64f),statusColor=new Color(.75f,.4f,1);
 public bool enableMotion=true;
 public int ActiveTransfers=>transfers.Count;
 public int ActiveCasts=>casts.Count;
 public int TotalDraws {get;private set;}
 public int TotalPlacements {get;private set;}
 public int TotalDiscards {get;private set;}
 public int TotalCasts {get;private set;}
 public RectTransform LastEffectTarget {get;private set;}
 public bool BlocksResolution=>isActiveAndEnabled&&enableMotion&&(transfers.Count>0||casts.Count>0||KeepBattleVisible);
 public bool KeepBattleVisible {get;private set;}
 private sealed class Visual {
  public int number;public CardEffect effect;public string label;public bool enemy,engraved;
  public Vector2 point,size;public float angle;
 }
 private sealed class Transfer {
  public CardView ghost;public Image back;public RectTransform destination;
  public Visual from,to;public float start,duration,arc;public bool flip;
  public readonly List<Image> trails=new List<Image>();
 }
 private sealed class Cast {
  public ResolutionEvent resolution;public CardView ghost;public Visual origin;
  public Vector2 target;public float start,charge,travel,impact,gain,strength;
  public bool hit,statusReaction;public Color color;public string caption;
  public DuelRingGraphic sourceRing,targetRing;public Image head;
  public Text label;public RectTransform sourceView;public Vitals after;public readonly List<Image> trails=new List<Image>();
 }
 private sealed class Vitals {public int hp,enemyHp,hpLimit,enemyLimit;public string playerText,enemyText,playerShield,enemyShield;}
 private readonly List<Transfer> transfers=new List<Transfer>();
 private readonly List<Cast> casts=new List<Cast>();
 private readonly Dictionary<Card,Visual> hands=new Dictionary<Card,Visual>();
 private readonly Dictionary<BattleAction,Visual> actions=new Dictionary<BattleAction,Visual>();
 private readonly Dictionary<RectTransform,int> hidden=new Dictionary<RectTransform,int>();
 private ContainmentGame observed;
 private int round,drawCount,chain,chainRound;
 private bool wasBattle,chainEnemy;
 private float closeAt,lastCast=-10;
 private int health,enemyHealth,shield,enemyShield,power,enemyPower;
 private readonly int[] playerStatuses=new int[26],enemyStatuses=new int[26];
 private int lastHeal,lastEnemyHeal,lastShield,lastEnemyShield,lastPower,lastEnemyPower;
 private readonly int[] playerChanges=new int[26],enemyChanges=new int[26];
 private bool animateAfterReset;
 private bool waitingForImpact;
 private BattleAction latestAction;
 private Vitals displayedVitals;
 private void OnEnable(){if(view!=null&&view.session!=null)view.session.Resolved+=OnResolution;}
 private void OnDisable(){if(view!=null&&view.session!=null)view.session.Resolved-=OnResolution;ResetPresentation();observed=null;}
 public void PrepareRefresh(ContainmentGame g){
  if(observed!=g){ResetPresentation();observed=g;round=0;wasBattle=false;hands.Clear();actions.Clear();animateAfterReset=true;latestAction=null;displayedVitals=null;waitingForImpact=false;}
  bool battle=g.Phase==GamePhase.Planning||g.Phase==GamePhase.Resolving;
  if(enableMotion&&isActiveAndEnabled&&wasBattle&&g.Round==round&&g.LastResolvedAction!=null&&g.LastResolvedAction!=latestAction)waitingForImpact=true;
  if(enableMotion&&isActiveAndEnabled&&wasBattle&&!battle&&!KeepBattleVisible){
   KeepBattleVisible=true;closeAt=Time.unscaledTime+(chargeDuration+projectileDuration+impactDuration+discardDuration+dealStagger*hands.Count+.15f)*Pace;
  }
 }
 public void Observe(ContainmentGame g){
  if(motionLayer==null||cardPrefab==null)return;
  bool battle=g.Phase==GamePhase.Planning||g.Phase==GamePhase.Resolving;
  bool canAnimate=enableMotion&&isActiveAndEnabled&&Application.isPlaying;
  var nextHands=new Dictionary<Card,Visual>();var nextActions=new Dictionary<BattleAction,Visual>();
  for(int i=0;i<g.Hand.Count&&i<view.hand.Length;i++)nextHands[g.Hand[i]]=Read(view.hand[i],g.Hand[i],false);
  for(int i=0;i<g.Timeline.Count&&i<view.actions.Length;i++)nextActions[g.Timeline[i]]=Read(view.actions[i],g.Timeline[i].Card,g.Timeline[i].Enemy);
  bool newRound=battle&&(!wasBattle||round!=g.Round||animateAfterReset);
  bool changed=newRound||wasBattle&&!battle||actions.Count!=nextActions.Count||actions.Any(p=>!nextActions.ContainsKey(p.Key)||(nextActions[p.Key].point-p.Value.point).sqrMagnitude>4);
  if(canAnimate&&changed){
   ClearTransfers();float delay=0;
   if(wasBattle&&(newRound||!battle)){
    int i=0;foreach(var item in hands){var origin=actions.FirstOrDefault(a=>a.Key.Card==item.Key).Value??item.Value;
     float wait=(!battle?chargeDuration+projectileDuration+impactDuration:0)*Pace+i++*dealStagger*Pace;
     Fly(origin,At(discardPile,origin,.22f),null,discardDuration*Pace,wait,false);TotalDiscards++;
    }
    delay=(discardDuration+dealStagger*Mathf.Max(0,hands.Count-1)+.06f)*Pace;
   }
   if(newRound){
    // A reshuffle happens in the domain when the remaining draw pile cannot fill the hand.
    if(wasBattle&&drawCount<nextHands.Count){
     var sample=nextHands.Values.FirstOrDefault();if(sample!=null){
      for(int shuffleIndex=0;shuffleIndex<3;shuffleIndex++)Fly(At(discardPile,sample,.24f),At(drawPile,sample,.24f),null,placeDuration*Pace,delay+shuffleIndex*.04f*Pace,true);
      delay+=(placeDuration+.1f)*Pace;
     }
    }
    int i=0;foreach(var item in nextHands){Fly(At(drawPile,item.Value,.24f),item.Value,HandRect(g,item.Key),drawDuration*Pace,delay+i++*dealStagger*Pace,true);TotalDraws++;}
    i=0;foreach(var item in nextActions){var origin=EnemyOrigin(item.Key,item.Value);Fly(origin,item.Value,ActionRect(g,item.Key),placeDuration*Pace,delay+i++*dealStagger*Pace,false);}
   }else if(battle){
    foreach(var item in nextActions){Visual origin;
     if(!actions.TryGetValue(item.Key,out origin)){
      if(item.Key.Card!=null&&hands.TryGetValue(item.Key.Card,out origin))TotalPlacements++;
      else origin=EnemyOrigin(item.Key,item.Value);
     }
     if(!actions.ContainsKey(item.Key)||(origin.point-item.Value.point).sqrMagnitude>4)
      Fly(origin,item.Value,ActionRect(g,item.Key),placeDuration*Pace,0,false);
    }
    foreach(var item in actions)if(!nextActions.ContainsKey(item.Key)&&item.Key.Card!=null&&nextHands.TryGetValue(item.Key.Card,out var target))
     Fly(item.Value,target,HandRect(g,item.Key.Card),placeDuration*Pace,0,false);
   }
  }
  lastHeal=Mathf.Max(0,g.Health-health);lastEnemyHeal=Mathf.Max(0,g.EnemyHealth-enemyHealth);
  lastShield=Mathf.Max(0,g.Shield-shield);lastEnemyShield=Mathf.Max(0,g.EnemyShield-enemyShield);
  lastPower=Mathf.Max(0,g.Power-power);lastEnemyPower=Mathf.Max(0,g.EnemyPower-enemyPower);
  health=g.Health;enemyHealth=g.EnemyHealth;shield=g.Shield;enemyShield=g.EnemyShield;power=g.Power;enemyPower=g.EnemyPower;
  for(int i=0;i<26;i++){playerChanges[i]=Mathf.Max(0,g.Player.Statuses[(CardEffect)i]-playerStatuses[i]);enemyChanges[i]=Mathf.Max(0,g.Enemy.Statuses[(CardEffect)i]-enemyStatuses[i]);playerStatuses[i]=g.Player.Statuses[(CardEffect)i];enemyStatuses[i]=g.Enemy.Statuses[(CardEffect)i];}
  hands.Clear();foreach(var p in nextHands)hands.Add(p.Key,p.Value);actions.Clear();foreach(var p in nextActions)actions.Add(p.Key,p.Value);
  round=g.Round;wasBattle=battle;drawCount=g.DrawPileCards.Count;animateAfterReset=false;ApplyVisibility();
  if(canAnimate&&(waitingForImpact||casts.Count>0)&&displayedVitals!=null)ShowVitals(displayedVitals);
  else {displayedVitals=VitalsFor(g);waitingForImpact=false;}
 }
 private float Pace=>view==null||view.battle==null?1:Mathf.Clamp(view.battle.actionInterval/.95f,.35f,1.6f);
 private RectTransform HandRect(ContainmentGame g,Card c){int i=g.Hand.IndexOf(c);return i<0||i>=view.hand.Length?null:(RectTransform)view.hand[i].transform;}
 private RectTransform ActionRect(ContainmentGame g,BattleAction a){int i=g.Timeline.IndexOf(a);return i<0||i>=view.actions.Length?null:(RectTransform)view.actions[i].transform;}
 private Visual Read(CardView card,Card data,bool enemy){
  var r=(RectTransform)card.transform;var corners=new Vector3[4];r.GetWorldCorners(corners);
  Vector2 a=motionLayer.InverseTransformPoint(corners[0]),b=motionLayer.InverseTransformPoint(corners[2]);
  return new Visual{point=Point(r),size=new Vector2(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y)),angle=r.eulerAngles.z-motionLayer.eulerAngles.z,
   number=data?.Number??int.Parse(card.topRank.text),effect=data?.Effect??card.BoundEffect,label=card.caption.text,enemy=enemy,engraved=data?.Engraved??false};
 }
 private Vector2 Point(RectTransform r)=>r==null?Vector2.zero:(Vector2)motionLayer.InverseTransformPoint(r.TransformPoint(r.rect.center));
 private Visual At(RectTransform r,Visual card,float scale)=>new Visual{point=Point(r),size=new Vector2(130,225)*scale,number=card.number,effect=card.effect,label=card.label,enemy=card.enemy,engraved=card.engraved};
 private Visual EnemyOrigin(BattleAction a,Visual target){
  if(a.Enemy&&view.battleHud!=null){var skills=view.battleHud.enemySkills;var match=skills.FirstOrDefault(c=>c!=null&&c.gameObject.activeInHierarchy&&c.topRank.text==a.Number.ToString()&&c.BoundEffect==a.Effect);if(match!=null)return Read(match,null,true);}
  return At(ruleOrigin,target,.3f);
 }
 private CardView Ghost(Visual v){
  var c=Instantiate(cardPrefab,motionLayer);c.name="FlyingCard";c.gameObject.SetActive(true);
  c.SetPreview(v.number,v.effect,v.label);c.Bind(v.number,v.effect,v.label,v.enemy,v.engraved,false,false,null);
  c.SetPresentationOnly();if(c.caption!=null)c.caption.gameObject.SetActive(false);
  var r=(RectTransform)c.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.localRotation=Quaternion.identity;
  r.anchoredPosition=v.point;r.localScale=new Vector3(v.size.x/r.rect.width,v.size.y/r.rect.height,1);return c;
 }
 private Image Image(string name,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(motionLayer,false);var im=go.GetComponent<Image>();im.color=color;im.raycastTarget=false;return im;}
 private DuelRingGraphic Ring(string name,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(DuelRingGraphic));go.transform.SetParent(motionLayer,false);var r=go.GetComponent<DuelRingGraphic>();r.color=color;r.raycastTarget=false;return r;}
 private Image Back(CardView ghost){var go=new GameObject("CardBack",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(ghost.face.transform,false);var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;var im=go.GetComponent<Image>();im.color=new Color(.08f,.22f,.35f);im.raycastTarget=false;var outline=go.AddComponent<Outline>();outline.effectColor=new Color(.9f,.76f,.45f);outline.effectDistance=new Vector2(3,-3);return im;}
 private void Fly(Visual from,Visual to,RectTransform destination,float duration,float delay,bool flip){
  var t=new Transfer{from=from,to=to,destination=destination,start=Time.unscaledTime+delay,duration=Mathf.Max(.05f,duration),arc=flightArc,flip=flip};
  t.ghost=Ghost(from);if(flip)t.back=Back(t.ghost);t.ghost.gameObject.SetActive(delay<=0);
  for(int i=0;i<6;i++){var trail=Image("CardFlightTrail",from.enemy?attackColor:shieldColor);trail.gameObject.SetActive(false);t.trails.Add(trail);}
  if(destination!=null){hidden.TryGetValue(destination,out int count);hidden[destination]=count+1;}transfers.Add(t);
 }
 private void OnResolution(ResolutionEvent e){
  if(!enableMotion||!isActiveAndEnabled||e.Action==null||motionLayer==null)return;
  var a=e.Action;bool cancelledByBleed=!a.IsNumberOnly&&!a.IsStatusTick&&(a.Enemy?view.session.Game.EnemyHealth:view.session.Game.Health)<=0;
  bool targetEnemy=cancelledByBleed?a.Enemy:TargetsEnemy(a);
  int damage=targetEnemy?e.EnemyDamage+e.EnemyBlocked:e.PlayerDamage+e.PlayerBlocked;
  chain=chainRound==e.Round&&chainEnemy==a.Enemy&&Time.unscaledTime-lastCast<1.6f?Mathf.Min(chain+1,99):1;
  if(!a.IsNumberOnly){chainRound=e.Round;chainEnemy=a.Enemy;lastCast=Time.unscaledTime;}
  float gain=1+(maxChainGain-1)*(1-Mathf.Exp(-Mathf.Max(0,chain-1)/3f));
  var color=cancelledByBleed?statusColor:EffectColor(a);var source=a.SourceAction??a;actions.TryGetValue(source,out var origin);
  if(origin==null&&a.Card!=null)hands.TryGetValue(a.Card,out origin);
  if(origin==null)origin=new Visual{point=Point(a.IsStatusTick?(a.Enemy?enemyTarget:playerTarget):ruleOrigin),size=new Vector2(90,156),number=Mathf.Clamp(a.Number,1,6),effect=a.Effect,label=a.Label,enemy=a.Enemy};
  if(a.IsNumberOnly){color=shieldColor;targetEnemy=a.Enemy;}
  LastEffectTarget=a.IsNumberOnly||a.Rule!=null&&a.Rule.Effect==RuleEffect.Insert&&!cancelledByBleed?ruleOrigin:targetEnemy?enemyTarget:playerTarget;
  if(cancelledByBleed)origin=At(LastEffectTarget,origin,.3f);
  var c=new Cast{resolution=e,origin=origin,target=Point(LastEffectTarget),start=Time.unscaledTime,charge=chargeDuration*Pace,travel=projectileDuration*Pace,impact=impactDuration*Pace,gain=gain,strength=1-Mathf.Exp(-damage/18f),color=color,caption=cancelledByBleed?"流血 · 行动中断":Caption(a,targetEnemy),statusReaction=cancelledByBleed||a.IsStatusTick,after=VitalsFor(view.session.Game)};
  latestAction=a;
  if(!cancelledByBleed&&a.Rule==null&&!a.IsStatusTick&&!a.IsNumberOnly){c.ghost=Ghost(origin);c.sourceView=ActionRect(view.session.Game,source);if(c.sourceView!=null){hidden.TryGetValue(c.sourceView,out int count);hidden[c.sourceView]=count+1;}}
  c.sourceRing=Ring("CardActivationRing",color);c.targetRing=Ring("TargetImpactRing",color);c.targetRing.gameObject.SetActive(false);
  c.head=Image("EffectProjectile",color);c.head.gameObject.SetActive(false);
  for(int i=0;i<9;i++){var trail=Image("EffectTrail",color);trail.gameObject.SetActive(false);c.trails.Add(trail);}
  if(effects!=null&&effects.numberPrefab!=null){c.label=Instantiate(effects.numberPrefab,motionLayer);c.label.name="EffectCaption";c.label.text=c.caption;c.label.color=color;c.label.raycastTarget=false;c.label.gameObject.SetActive(false);}
  casts.Add(c);TotalCasts++;
 }
 public static bool TargetsEnemy(BattleAction a)=>CardEffectTargeting.TargetsEnemy(a);
 private Color EffectColor(BattleAction a){
  if(a.IsStatusTick)return statusColor;
  if(a.Rule!=null)return a.Rule.Effect==RuleEffect.Damage?attackColor:shieldColor;
  if(a.Effect==CardEffect.Strike||a.Effect==CardEffect.IncreasedStrike||a.Effect==CardEffect.InverseStrike)return attackColor;
  if(a.Effect==CardEffect.Mend||a.Effect==CardEffect.Regeneration)return healColor;
  if(a.Effect==CardEffect.Guard||a.Effect==CardEffect.Parry||a.Effect==CardEffect.Barrier||a.Effect==CardEffect.IronWall)return shieldColor;
  return statusColor;
 }
 private string Caption(BattleAction a,bool enemy){
  if(a.IsNumberOnly)return "填入数字 "+a.Number;
  if(a.Rule!=null){int amount=enemy?lastEnemyShield:lastShield;if(a.Rule.Effect==RuleEffect.Shield)return "护盾 +"+amount;if(a.Rule.Effect==RuleEffect.Power)return "增幅 +"+(enemy?lastEnemyPower:lastPower);return a.Rule.Effect==RuleEffect.Insert?"填入数字 "+a.Rule.Value:"牌序触发";}
  if(a.IsStatusTick)return "中毒结算";
  if(a.Effect==CardEffect.Mend)return "恢复 +"+(enemy?lastEnemyHeal:lastHeal);
  if(a.Effect==CardEffect.Guard)return "护盾 +"+(enemy?lastEnemyShield:lastShield);
  int stacks=(enemy?enemyChanges:playerChanges)[(int)a.Effect];return stacks>0?CombatStatuses.Name(a.Effect)+" +"+stacks:CombatStatuses.Name(a.Effect);
 }
 private void Update(){
  if(!enableMotion){if(transfers.Count>0||casts.Count>0||KeepBattleVisible){ResetPresentation();view.Refresh();}return;}
  for(int i=transfers.Count-1;i>=0;i--){var t=transfers[i];float age=Time.unscaledTime-t.start;if(age<0)continue;
   float p=Mathf.Clamp01(age/t.duration),ease=1-Mathf.Pow(1-p,3);t.ghost.gameObject.SetActive(true);
   var r=(RectTransform)t.ghost.transform;r.anchoredPosition=Curve(t.from.point,t.to.point,ease,t.arc);var size=Vector2.Lerp(t.from.size,t.to.size,ease);
   float flip=t.flip?Mathf.Max(.06f,Mathf.Abs(Mathf.Cos(p*Mathf.PI))):1;r.localScale=new Vector3(size.x/r.rect.width*flip,size.y/r.rect.height,1);
   r.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(t.from.angle,t.to.angle,ease)+Mathf.Sin(p*Mathf.PI)*14);
   if(t.back!=null)t.back.gameObject.SetActive(p<.5f);
   Trail(t.trails,t.from.point,t.to.point,ease,t.arc,shieldColor,1,Mathf.Sin(p*Mathf.PI));
   if(p>=1){DestroyTransfer(t);transfers.RemoveAt(i);}
  }
  for(int i=casts.Count-1;i>=0;i--){var c=casts[i];float age=Time.unscaledTime-c.start;
   float charge=Mathf.Clamp01(age/c.charge),travel=Mathf.Clamp01((age-c.charge)/c.travel),hit=Mathf.Clamp01((age-c.charge-c.travel)/c.impact);
   var ring=c.sourceRing.rectTransform;ring.anchoredPosition=c.origin.point;ring.sizeDelta=Vector2.one*(45+charge*95*c.gain);c.sourceRing.color=Alpha(c.color,(1-travel)*.8f);
   if(c.ghost!=null){var r=(RectTransform)c.ghost.transform;float lunge=travel<.5f?travel*2:(1-travel)*2;r.anchoredPosition=c.origin.point+(c.target-c.origin.point)*attackLunge*lunge;r.localScale=new Vector3(c.origin.size.x/r.rect.width,c.origin.size.y/r.rect.height,1)*(1+Mathf.Sin(charge*Mathf.PI)*.12f);c.ghost.face.color=Color.Lerp(c.origin.enemy?c.ghost.enemyFace:c.ghost.playerFace,Color.white,Mathf.Sin(charge*Mathf.PI)*.6f);}
   bool moving=age>=c.charge&&travel<1;c.head.gameObject.SetActive(moving);
   if(moving){c.head.rectTransform.anchoredPosition=Curve(c.origin.point,c.target,travel,flightArc*.25f);c.head.rectTransform.sizeDelta=Vector2.one*(12+12*c.strength)*c.gain;c.head.rectTransform.localRotation=Quaternion.Euler(0,0,age*900);}
   Trail(c.trails,c.origin.point,c.target,travel,flightArc*.25f,c.color,c.gain,moving?1:0);
   if(travel>=1&&!c.hit){c.hit=true;waitingForImpact=false;displayedVitals=c.after;ShowVitals(displayedVitals);if(effects!=null)effects.PlayResolution(c.resolution,true);}
   c.targetRing.gameObject.SetActive(c.hit);c.targetRing.rectTransform.anchoredPosition=c.target;c.targetRing.rectTransform.sizeDelta=Vector2.one*(30+hit*(150+100*c.strength)*c.gain);c.targetRing.color=Alpha(c.color,1-hit);
   if(c.label!=null){c.label.gameObject.SetActive(c.hit&&(c.statusReaction||c.resolution.EnemyDamage+c.resolution.PlayerDamage+c.resolution.EnemyBlocked+c.resolution.PlayerBlocked==0));c.label.rectTransform.anchoredPosition=c.target+Vector2.up*(24+hit*40);c.label.fontSize=Mathf.RoundToInt(23+Mathf.Min(8,chain)*2);c.label.color=Alpha(c.color,1-hit);}
   if(hit>=1){DestroyCast(c);casts.RemoveAt(i);}
  }
  ApplyVisibility();
  if(KeepBattleVisible&&Time.unscaledTime>=closeAt&&transfers.Count==0&&casts.Count==0){KeepBattleVisible=false;view.Refresh();}
 }
 private static Color Alpha(Color c,float a){c.a=Mathf.Clamp01(a);return c;}
 private static Vitals VitalsFor(ContainmentGame g)=>new Vitals{hp=g.Health,enemyHp=g.EnemyHealth,hpLimit=g.HealthLimit,enemyLimit=g.EnemyMaxHealth,
  playerText="玩家   "+g.Health+" / "+g.HealthLimit+"    增伤 "+g.Power,enemyText=g.EnemyHealth+" / "+g.EnemyMaxHealth+"    增伤 "+g.EnemyPower,playerShield="护盾\n"+g.Shield,enemyShield="护盾\n"+g.EnemyShield};
 private void ShowVitals(Vitals v){view.playerHealth.maxValue=v.hpLimit;view.playerHealth.value=v.hp;view.enemyHealth.maxValue=Mathf.Max(1,v.enemyLimit);view.enemyHealth.value=v.enemyHp;view.playerStats.text=v.playerText;view.enemyStats.text=v.enemyText;if(view.battleHud!=null){view.battleHud.playerShield.text=v.playerShield;view.battleHud.enemyShield.text=v.enemyShield;}}
 private static Vector2 Curve(Vector2 from,Vector2 to,float p,float arc)=>Vector2.LerpUnclamped(from,to,p)+Vector2.up*Mathf.Sin(p*Mathf.PI)*arc;
 private void Trail(List<Image> trails,Vector2 from,Vector2 to,float p,float arc,Color color,float gain,float alpha){
  for(int i=0;i<trails.Count;i++){var image=trails[i];image.gameObject.SetActive(alpha>.01f);if(alpha<=.01f)continue;float q=Mathf.Clamp01(p-i*.027f);var a=Curve(from,to,q,arc);var b=Curve(from,to,Mathf.Clamp01(q-.032f),arc);var r=image.rectTransform;r.anchoredPosition=(a+b)*.5f;r.sizeDelta=new Vector2(Vector2.Distance(a,b)+1,(7-i*.5f)*gain);r.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(a.y-b.y,a.x-b.x)*Mathf.Rad2Deg);image.color=Alpha(color,alpha*(1-(float)i/trails.Count)*.7f);}
 }
 private void ApplyVisibility(){if(view==null||view.session.Game==null)return;var g=view.session.Game;
  for(int i=0;i<view.hand.Length;i++){bool placed=i<g.Hand.Count&&g.Timeline.Any(a=>a.Card==g.Hand[i]);Visibility(view.hand[i],!enableMotion||!placed&&!hidden.ContainsKey((RectTransform)view.hand[i].transform));}
  foreach(var c in view.actions)Visibility(c,!enableMotion||!hidden.ContainsKey((RectTransform)c.transform));
 }
 private static void Visibility(CardView c,bool show){if(c==null)return;var group=c.GetComponent<CanvasGroup>();if(group==null)group=c.gameObject.AddComponent<CanvasGroup>();group.alpha=show?1:0;group.blocksRaycasts=show;}
 private void DestroyTransfer(Transfer t){if(t.ghost!=null)Destroy(t.ghost.gameObject);foreach(var im in t.trails)if(im!=null)Destroy(im.gameObject);if(t.destination!=null&&hidden.TryGetValue(t.destination,out int count)){if(count<=1)hidden.Remove(t.destination);else hidden[t.destination]=count-1;}}
 private void DestroyCast(Cast c){if(c.sourceView!=null&&hidden.TryGetValue(c.sourceView,out int count)){if(count<=1)hidden.Remove(c.sourceView);else hidden[c.sourceView]=count-1;}if(c.ghost!=null)Destroy(c.ghost.gameObject);if(c.sourceRing!=null)Destroy(c.sourceRing.gameObject);if(c.targetRing!=null)Destroy(c.targetRing.gameObject);if(c.head!=null)Destroy(c.head.gameObject);if(c.label!=null)Destroy(c.label.gameObject);foreach(var im in c.trails)if(im!=null)Destroy(im.gameObject);}
 private void ClearTransfers(){foreach(var t in transfers)DestroyTransfer(t);transfers.Clear();hidden.Clear();}
 private void ResetPresentation(){ClearTransfers();foreach(var c in casts)DestroyCast(c);casts.Clear();KeepBattleVisible=false;waitingForImpact=false;chain=0;lastCast=-10;if(view!=null){foreach(var c in view.hand)Visibility(c,true);foreach(var c in view.actions)Visibility(c,true);}}
}
}
