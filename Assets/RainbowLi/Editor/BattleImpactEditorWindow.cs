using System;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public sealed class BattleImpactEditorWindow : EditorWindow {
  [SerializeField] BattlePresentationProfile profile;
  [SerializeField] int tab,damage=20,repeats=1;
  [SerializeField] bool hitPlayer;
  Vector2 scroll;string originalJson,message;BattlePresentationProfile capturedProfile;
  CardMotionController previewMotion;bool ownsPreview,previousAutoPlay;
  public BattlePresentationProfile SelectedProfile=>profile;
  static readonly string[] Tabs={"卡牌冲击","命中反馈","震屏与闪光","音效与颜色","连锁与节奏","悬停与飞牌"};
  public static readonly string[][] Groups={
   new[]{"chargeDuration","projectileDuration","impactDuration","attackLunge","cardRecoil","cardTilt","cardChargeScale","flightArc","damageIntensity"},
   new[]{"burstDuration","fallbackTravelDuration","maxParticles","maxConcurrentBursts","particleSpread","particleSize","damageTextBaseSize","damageTextGrowth","damageTextRise"},
   new[]{"screenShake","maxShake","shakeDuration","shakeStrength","hitFlash","flashDuration","flashStrength"},
   new[]{"lightHit","heavyHit","soundVolume","lightPitch","heavyPitch","attackColor","shieldColor","healColor","statusColor","sparkColor","sparkAccentColor"},
   new[]{"chainWindow","maxChainGain","maxComboGain","chainRiseActions","comboRiseActions","actionInterval","ruleInterval","accelerateRepeats","accelerateAfter","speedGainPerRepeat","maxRepeatSpeed"},
   new[]{"hoverScale","hoverDuration","hoverLift","drawDuration","placeDuration","discardDuration","dealStagger","flightRotation"}
  };
  [MenuItem("RainbowLi/动效/打击感调试器")]
  public static void Open(){var w=GetWindow<BattleImpactEditorWindow>("打击感调试器");if(w.profile==null){var motion=UnityEngine.Object.FindFirstObjectByType<CardMotionController>();w.SelectProfile(motion!=null&&motion.profile!=null?motion.profile:AssetDatabase.LoadAssetAtPath<BattlePresentationProfile>("Assets/RainbowLi/Data/DefaultBattlePresentation.asset"));}w.minSize=new Vector2(620,560);}
  public static void Open(BattlePresentationProfile asset){Open();GetWindow<BattleImpactEditorWindow>().SelectProfile(asset);}
  public void SelectProfile(BattlePresentationProfile asset){if(profile!=asset)ClearPreview();profile=asset;if(asset!=capturedProfile||originalJson==null){capturedProfile=asset;originalJson=asset==null?null:EditorJsonUtility.ToJson(asset);}Repaint();}
  void OnEnable(){EditorApplication.update+=Refresh;}
  void OnDisable(){EditorApplication.update-=Refresh;ClearPreview();}
  void Refresh(){if(Application.isPlaying)Repaint();}
  public bool CanPreview {
   get{var m=UnityEngine.Object.FindFirstObjectByType<CardMotionController>();return Application.isPlaying&&profile!=null&&m!=null&&m.profile==profile&&m.isActiveAndEnabled&&m.enableMotion&&m.view!=null&&m.view.IsReady&&m.view.battlePanel.activeInHierarchy&&m.view.session.Game!=null&&m.view.session.Game.Phase==GamePhase.Planning&&m.ActiveTransfers==0&&(ownsPreview||m.ActiveCasts==0);}
  }
  void OnGUI(){
   EditorGUILayout.LabelField("打击感调试器",EditorStyles.boldLabel);
   var next=(BattlePresentationProfile)EditorGUILayout.ObjectField("动效配置资源",profile,typeof(BattlePresentationProfile),false);if(next!=profile)SelectProfile(next);
   if(profile==null){EditorGUILayout.HelpBox("选择动效配置，或点击载入默认配置。",MessageType.Info);if(GUILayout.Button("载入默认配置"))SelectProfile(AssetDatabase.LoadAssetAtPath<BattlePresentationProfile>("Assets/RainbowLi/Data/DefaultBattlePresentation.asset"));return;}
   if(capturedProfile!=profile||originalJson==null)SelectProfile(profile);
   using(new EditorGUILayout.HorizontalScope()){
    if(GUILayout.Button("轻击预设"))ApplyPreset(profile,0);
    if(GUILayout.Button("重击预设"))ApplyPreset(profile,1);
    if(GUILayout.Button("夸张连锁预设"))ApplyPreset(profile,2);
    if(GUILayout.Button("保存配置")){AssetDatabase.SaveAssetIfDirty(profile);message="已保存当前配置。";}
    if(GUILayout.Button("还原打开时参数")){RestoreOpening();message="已还原，可通过Undo撤销。";}
   }
   if(!string.IsNullOrEmpty(message))EditorGUILayout.LabelField(message,EditorStyles.miniLabel);
   EditorGUILayout.HelpBox("编辑资源后，下一次动效读取新参数；运行中改动也会保留。Ctrl+Z可撤销。预设只修改打击感，不覆盖音效资源、颜色和悬停参数。",MessageType.Info);
   tab=GUILayout.Toolbar(Mathf.Clamp(tab,0,Tabs.Length-1),Tabs);scroll=EditorGUILayout.BeginScrollView(scroll);
   var so=new SerializedObject(profile);so.Update();foreach(string name in Groups[tab]){var p=so.FindProperty(name);if(p!=null)EditorGUILayout.PropertyField(p,new GUIContent(BattlePresentationProfileEditor.FieldLabel(name),p.tooltip),true);}
   so.ApplyModifiedProperties();EditorGUILayout.EndScrollView();
   EditorGUILayout.Space();EditorGUILayout.LabelField("战斗中预览（纯表现，不扣血）",EditorStyles.boldLabel);
   damage=EditorGUILayout.IntSlider("模拟伤害",damage,1,160);repeats=EditorGUILayout.IntSlider("模拟連锁次数",repeats,1,12);hitPlayer=EditorGUILayout.Toggle("命中玩家",hitPlayer);
   EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false,18),profile.DamageStrength(damage),"当前伤害视觉强度："+profile.DamageStrength(damage).ToString("0.00"));
   if(!CanPreview)EditorGUILayout.HelpBox("进入游戏战斗的安排阶段，等待发牌结束后预览。当前场景需绑定选中的配置。",MessageType.None);
   using(new EditorGUI.DisabledScope(!CanPreview)){using(new EditorGUILayout.HorizontalScope()){if(GUILayout.Button("预览当前伤害"))Preview(damage,repeats,hitPlayer);if(GUILayout.Button("5伤害轻击"))Preview(5,1,hitPlayer);if(GUILayout.Button("60伤害重击"))Preview(60,1,hitPlayer);if(GUILayout.Button("8次连锁"))Preview(damage,8,hitPlayer);}}
   using(new EditorGUI.DisabledScope(!ownsPreview))if(GUILayout.Button("清理预览 / 恢复原播放状态"))ClearPreview();
   using(new EditorGUI.DisabledScope(Application.isPlaying||!UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path.StartsWith("Assets/RainbowLi/",StringComparison.Ordinal)))if(GUILayout.Button("将配置绑定到当前RainbowLi场景"))PresentationProfileSceneMigration.BindCurrent(profile);
  }
  public void RestoreOpening(){if(profile==null||originalJson==null||capturedProfile!=profile)return;Undo.RecordObject(profile,"还原打击感参数");EditorJsonUtility.FromJsonOverwrite(originalJson,profile);EditorUtility.SetDirty(profile);}
  public bool Preview(int simulatedDamage,int chain,bool player){
   if(!CanPreview)return false;var m=UnityEngine.Object.FindFirstObjectByType<CardMotionController>();
   if(!ownsPreview){previousAutoPlay=m.view.battle.autoPlay;previewMotion=m;ownsPreview=true;}else m.ClearDesignerPreview();
   m.view.battle.autoPlay=false;m.view.session.Notify();m.PreviewAttack(Mathf.Max(1,simulatedDamage),Mathf.Clamp(chain,1,12),player);return true;
  }
  public void ClearPreview(){if(!ownsPreview)return;if(previewMotion!=null){previewMotion.ClearDesignerPreview();previewMotion.view.battle.autoPlay=previousAutoPlay;previewMotion.view.session.Notify();}previewMotion=null;ownsPreview=false;}
  public static void ApplyPreset(BattlePresentationProfile p,int kind){
   if(p==null)throw new ArgumentNullException(nameof(p));if(kind<0||kind>2)throw new ArgumentOutOfRangeException(nameof(kind));Undo.RecordObject(p,"应用打击感预设");
   p.chargeDuration=kind==0?.12f:kind==1?.24f:.3f;p.projectileDuration=kind==0?.16f:.2f;p.impactDuration=kind==0?.2f:kind==1?.4f:.5f;
   p.attackLunge=.94f;p.cardRecoil=kind==0?20:kind==1?55:75;p.cardTilt=kind==0?10:kind==1?24:32;p.cardChargeScale=kind==0?.08f:kind==1?.25f:.35f;
   p.screenShake=true;p.hitFlash=true;p.maxShake=kind==0?12:kind==1?32:45;p.shakeDuration=kind==0?.2f:kind==1?.4f:.5f;p.shakeStrength=kind==0?.7f:1.25f;p.flashDuration=kind==0?.18f:.3f;p.flashStrength=kind==0?.65f:1.2f;
   p.maxParticles=kind==0?24:kind==1?65:85;p.particleSpread=kind==0?100:kind==1?210:280;p.particleSize=kind==0?4:kind==1?7:9;p.burstDuration=kind==0?.75f:kind==1?1.15f:1.4f;
   p.damageTextBaseSize=kind==0?22:kind==1?28:32;p.damageTextGrowth=kind==0?12:kind==1?26:36;p.damageTextRise=kind==0?45:75;
   p.maxChainGain=kind==2?3.2f:2.4f;p.maxComboGain=kind==2?3.5f:2.8f;p.chainRiseActions=kind==2?2:3;p.comboRiseActions=kind==2?2.5f:3.5f;
   EditorUtility.SetDirty(p);
  }
 }
}
