using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public abstract class PresentationBoundComponentEditor:Editor {
 static readonly HashSet<string> Locked=new HashSet<string>{"drawDuration","placeDuration","discardDuration","dealStagger","flightArc","chargeDuration","projectileDuration","impactDuration","attackLunge","maxChainGain","attackColor","shieldColor","healColor","statusColor","maxShake","maxParticles","maxConcurrentBursts","chainWindow","maxComboGain","accelerateRepeats","accelerateAfter","speedGainPerRepeat","maxRepeatSpeed","actionInterval","ruleInterval"};
 public override void OnInspectorGUI(){serializedObject.Update();var profile=serializedObject.FindProperty("profile").objectReferenceValue;if(profile!=null){EditorGUILayout.HelpBox("动效参数由共享配置控制，下方旧参数仅作兼容显示。请打开配置进行调整；播放倍率由游戏设置按钮控制。",MessageType.Info);if(GUILayout.Button("打开策划动效配置"))Selection.activeObject=profile;}var p=serializedObject.GetIterator();bool first=true;while(p.NextVisible(first)){first=false;using(new EditorGUI.DisabledScope(p.name=="m_Script"||profile!=null&&Locked.Contains(p.name)))EditorGUILayout.PropertyField(p,true);}serializedObject.ApplyModifiedProperties();}
}
[CustomEditor(typeof(CardMotionController))]public sealed class CardMotionProfileEditor:PresentationBoundComponentEditor {}
[CustomEditor(typeof(BattleEffects))]public sealed class BattleEffectsProfileEditor:PresentationBoundComponentEditor {}
[CustomEditor(typeof(BattleModule))]public sealed class BattleModuleProfileEditor:PresentationBoundComponentEditor {}
}
