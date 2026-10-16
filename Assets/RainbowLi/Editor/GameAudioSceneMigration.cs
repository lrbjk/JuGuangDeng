using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class GameAudioSceneMigration {
  const string Root="Assets/RainbowLi";
  static readonly string[] Names={"主界面按钮音效","场景切换音效","点击音效","按钮音效","击杀音效","受击音效","获得效果音效","卡牌触发音效","卡序触发音效"};
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PASS: Nine documented sound cues configured in editable assets, scene and UI prefabs.");EditorApplication.Exit(0);}
  [MenuItem("RainbowLi/音效/按音效包文档接入当前场景")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请先打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();}
  static void Feedback(Button button,bool main=false){var feedback=button.GetComponent<AudioUiFeedback>()??button.gameObject.AddComponent<AudioUiFeedback>();feedback.mainMenuButton=main;feedback.interaction=button.GetComponentInParent<CardView>()!=null||button.GetComponentInParent<DeckRowView>()!=null;EditorUtility.SetDirty(feedback);if(PrefabUtility.IsPartOfPrefabInstance(feedback))PrefabUtility.RecordPrefabInstancePropertyModifications(feedback);}
  static AudioSource Source(Transform parent,string name){var child=parent.Find(name);var go=child==null?new GameObject(name):child.gameObject;go.transform.SetParent(parent,false);var source=go.GetComponent<AudioSource>();if(source==null)source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.volume=.3f;return source;}
  static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null)throw new InvalidOperationException("缺少 GameView");
   string path=Root+"/Data/DefaultGameAudio.asset";var profile=AssetDatabase.LoadAssetAtPath<GameAudioProfile>(path);if(profile==null){profile=ScriptableObject.CreateInstance<GameAudioProfile>();AssetDatabase.CreateAsset(profile,path);}
   profile.entries=Names.Select((name,index)=>{string audio=Root+"/Art/音效包/音效/"+name+".MP3";var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(audio);if(clip==null)throw new InvalidOperationException("缺少音效："+audio);var previous=profile.entries.FirstOrDefault(e=>e!=null&&e.cue==(GameAudioCue)index);return previous??new GameAudioProfile.Entry{cue=(GameAudioCue)index,clip=clip,volume=.8f,pitch=1};}).ToArray();EditorUtility.SetDirty(profile);
   var controller=view.GetComponent<GameAudioController>()??view.gameObject.AddComponent<GameAudioController>();controller.view=view;controller.profile=profile;controller.uiSource=Source(view.transform,"UiAudioSource");controller.battleSource=Source(view.transform,"BattleAudioSource");EditorUtility.SetDirty(controller);
   foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"})){
    string prefabPath=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(prefabPath);try{var buttons=root.GetComponentsInChildren<Button>(true);if(buttons.Length>0){foreach(var b in buttons)Feedback(b);PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}}finally{PrefabUtility.UnloadPrefabContents(root);}
   }
   var menu=view.GetComponent<StartMenuView>();foreach(var button in view.GetComponentsInChildren<Button>(true))Feedback(button,button==view.confirm||menu!=null&&button.transform.IsChildOf(menu.panel.transform));
   foreach(var component in view.GetComponentsInChildren<Component>(true)){if(component==null)continue;EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
  }
 }
}
