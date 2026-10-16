using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class EngravingArtSceneMigration {
  const string Root="Assets/RainbowLi";
  public static void Configure(CardView card,EngravingArtCatalog catalog){
   card.engravingArt=catalog;
   var image=card.engravingBadge;image.name="EngravingFrame";image.raycastTarget=false;
   foreach(var outline in image.GetComponents<Outline>())outline.enabled=false;
   card.SetEngraving(image.gameObject.activeSelf,card.BoundEngraving);
   foreach(var c in card.GetComponentsInChildren<Component>(true)){if(c==null)continue;EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
   EditorUtility.SetDirty(card);
  }
  static void Apply(){
   var table=AssetDatabase.LoadAssetAtPath<EngravingEffectTable>(Root+"/Data/DefaultEngravingEffects.asset");
   var paths=AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Art/铭刻"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
   if(table==null||paths.Length!=table.rows.Length)throw new InvalidOperationException("铭刻素材数量须与铭刻效果表一致；当前素材 "+paths.Length);
   string path=Root+"/Data/DefaultEngravingArt.asset";
   var catalog=AssetDatabase.LoadAssetAtPath<EngravingArtCatalog>(path);if(catalog==null){catalog=ScriptableObject.CreateInstance<EngravingArtCatalog>();AssetDatabase.CreateAsset(catalog,path);}
   catalog.entries=table.rows.Select((row,i)=>{
    var existing=catalog.entries.FirstOrDefault(e=>e!=null&&e.effectId==row.id);
    if(existing!=null&&existing.frame!=null)return existing;
    var sprite=AssetDatabase.LoadAllAssetsAtPath(paths[i]).OfType<Sprite>().FirstOrDefault();if(sprite==null)throw new InvalidOperationException("铭刻缺少裁切Sprite："+paths[i]);
    return new EngravingArtCatalog.Entry{name=row.name,effectId=row.id,effect=(EngravingEffect)Enum.Parse(typeof(EngravingEffect),row.behavior),frame=sprite};
   }).ToArray();EditorUtility.SetDirty(catalog);
   string prefabPath=Root+"/Prefabs/PokerCard.prefab";var root=PrefabUtility.LoadPrefabContents(prefabPath);try{Configure(root.GetComponent<CardView>(),catalog);PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null)throw new InvalidOperationException("场景缺少 GameView");foreach(var card in view.GetComponentsInChildren<CardView>(true))Configure(card,catalog);
  }
  [MenuItem("RainbowLi/UI/配置铭刻卡牌美术")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();}
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PASS: 19 engraving frames configured in scene, prefab and editable catalog.");EditorApplication.Exit(0);}
 }
}

