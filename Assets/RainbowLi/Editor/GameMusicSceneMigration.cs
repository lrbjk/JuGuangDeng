using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class GameMusicSceneMigration {
  const string Root="Assets/RainbowLi";
  static AudioClip Clip(string name){var path=Root+"/Art/音效包/背景音乐/"+name;var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(clip==null)throw new InvalidOperationException("缺少背景音乐："+path);return clip;}
  static AudioSource Source(Transform parent,string name){var child=parent.Find(name);var go=child==null?new GameObject(name):child.gameObject;go.transform.SetParent(parent,false);var source=go.GetComponent<AudioSource>();if(source==null)source=go.AddComponent<AudioSource>();source.loop=true;source.playOnAwake=false;source.spatialBlend=0;source.pitch=1;source.volume=0;return source;}
  [MenuItem("RainbowLi/音效/接入背景音乐")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请先打开RainbowLi场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();}
  static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null)throw new InvalidOperationException("缺少GameView");
   var path=Root+"/Data/DefaultGameMusic.asset";var profile=AssetDatabase.LoadAssetAtPath<GameMusicProfile>(path);
   if(profile==null){profile=ScriptableObject.CreateInstance<GameMusicProfile>();profile.map=Clip("选关.mp3");profile.enemies=new[]{new GameMusicProfile.EnemyMusic{idPrefix="机械类",normal=Clip("机械敌人.mp3"),boss=Clip("机械boss曲.MP3")},new GameMusicProfile.EnemyMusic{idPrefix="生物类",normal=Clip("生物敌人.mp3"),boss=Clip("生物boss曲.MP3")},new GameMusicProfile.EnemyMusic{idPrefix="抽象概念类",normal=Clip("抽象概念敌人.mp3"),boss=Clip("抽象概念boss曲.MP3")}};AssetDatabase.CreateAsset(profile,path);}
   var music=view.GetComponent<GameMusicController>();if(music==null)music=view.gameObject.AddComponent<GameMusicController>();music.view=view;music.profile=profile;music.audioController=view.GetComponent<GameAudioController>();music.first=Source(view.transform,"BackgroundMusicA");music.second=Source(view.transform,"BackgroundMusicB");EditorUtility.SetDirty(music);
  }
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PASS: Seven background tracks configured, music sources wired.");EditorApplication.Exit(0);}
 }
}

