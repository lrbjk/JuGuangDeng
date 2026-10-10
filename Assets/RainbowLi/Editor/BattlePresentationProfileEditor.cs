using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
[CustomEditor(typeof(BattlePresentationProfile))]
public sealed class BattlePresentationProfileEditor:Editor {
 int damage=20,repeats=1;bool targetPlayer;
 static readonly Dictionary<string,string> Labels=new Dictionary<string,string>{
 {"drawDuration","抽牌飞行时长（秒）"},{"placeDuration","出牌飞行时长（秒）"},{"discardDuration","弃牌飞行时长（秒）"},{"dealStagger","逐张发牌间隔（秒）"},{"flightArc","飞行弧线高度"},{"flightRotation","飞牌摇摆角度"},
 {"chargeDuration","蓄力时长（秒）"},{"projectileDuration","攻击飞行时长（秒）"},{"impactDuration","命中光圈时长（秒）"},{"attackLunge","卡牌突进比例"},{"damageIntensity","伤害 → 视觉强度曲线"},
 {"chainWindow","连续效果判定时间（秒）"},{"maxChainGain","攻击连锁最大倍率"},{"maxComboGain","命中连锁最大倍率"},{"chainRiseActions","攻击连锁增长缓急"},{"comboRiseActions","命中连锁增长缓急"},
 {"burstDuration","命中粒子与飘字时长（秒）"},{"fallbackTravelDuration","无飞牌时命中飞行时长（秒）"},{"maxParticles","单次粒子数量上限"},{"maxConcurrentBursts","同时命中效果上限"},{"particleSpread","粒子扩散速度"},{"particleSize","粒子大小随强度增长"},{"damageTextBaseSize","伤害飘字基础字号"},{"damageTextGrowth","伤害飘字增大幅度"},{"damageTextRise","飘字上升速度"},
 {"screenShake","启用震屏"},{"hitFlash","启用血条闪光"},{"maxShake","震屏最大位移"},{"shakeDuration","震屏持续时长（秒）"},{"flashDuration","闪光持续时长（秒）"},{"shakeStrength","震屏强度倍率"},{"flashStrength","闪光强度倍率"},
 {"attackColor","攻击颜色"},{"shieldColor","护盾颜色"},{"healColor","治疗颜色"},{"statusColor","状态颜色"},{"sparkColor","命中粒子颜色"},{"sparkAccentColor","命中点缀颜色"},
 {"lightHit","轻击音效"},{"heavyHit","重击音效"},{"soundVolume","命中音量倍率"},{"lightPitch","轻击音调"},{"heavyPitch","重击音调"},{"actionInterval","普通行动间隔（1×秒）"},{"ruleInterval","牌序行动间隔（1×秒）"},{"accelerateRepeats","启用长连锁自动加速"},{"accelerateAfter","加速前保持原速的次数"},{"speedGainPerRepeat","之后每次增加的速度倍率"},{"maxRepeatSpeed","重复效果最大播放倍率"}};
 public override void OnInspectorGUI(){
  EditorGUILayout.HelpBox("这里统一控制表现，不改变真实伤害和效果目标。运行中调整后，下一次动效读取新值；正在飞行的卡牌仍使用启动时长。运行时修改配置资源会保留，请及时保存或撤销。",MessageType.Info);
  serializedObject.Update();var property=serializedObject.GetIterator();bool first=true;while(property.NextVisible(first)){first=false;if(property.name=="m_Script"){using(new EditorGUI.DisabledScope(true))EditorGUILayout.PropertyField(property);continue;}EditorGUILayout.PropertyField(property,new GUIContent(Labels.TryGetValue(property.name,out var label)?label:property.displayName,property.tooltip),true);}serializedObject.ApplyModifiedProperties();
  using(new EditorGUI.DisabledScope(Application.isPlaying||!UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path.StartsWith("Assets/RainbowLi/",System.StringComparison.Ordinal)))if(GUILayout.Button("将此配置绑定到当前 RainbowLi 场景"))PresentationProfileSceneMigration.BindCurrent((BattlePresentationProfile)target);
  EditorGUILayout.Space();EditorGUILayout.LabelField("运行时预览（自动暂停播放）",EditorStyles.boldLabel);damage=EditorGUILayout.IntSlider("预览伤害",damage,1,150);repeats=EditorGUILayout.IntSlider("预览连锁次数",repeats,1,12);targetPlayer=EditorGUILayout.Toggle("命中玩家（否则敌人）",targetPlayer);EditorGUILayout.LabelField("当前曲线强度",((BattlePresentationProfile)target).DamageStrength(damage).ToString("0.00"));
  var motion=Object.FindFirstObjectByType<CardMotionController>();bool ready=Application.isPlaying&&motion!=null&&motion.profile==target&&motion.enableMotion&&motion.view.battlePanel.activeSelf;
  if(!ready)EditorGUILayout.HelpBox("进入播放模式并进入战斗。预览只操作视觉，不出牌、不扣血；当前场景需要绑定这个配置。",MessageType.None);
  using(new EditorGUI.DisabledScope(!ready)){EditorGUILayout.BeginHorizontal();if(GUILayout.Button("预览抽牌")){Pause(motion);motion.PreviewFlight(false);}if(GUILayout.Button("预览出牌")){Pause(motion);motion.PreviewFlight(true);}if(GUILayout.Button("预览攻击")){Pause(motion);motion.PreviewAttack(damage,repeats,targetPlayer);}EditorGUILayout.EndHorizontal();if(GUILayout.Button("清理预览 / 恢复牌面"))motion.ClearDesignerPreview();}
 }
 static void Pause(CardMotionController motion){motion.view.battle.autoPlay=false;motion.view.session.Notify();}
}
}
