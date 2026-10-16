using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class CardSkinSceneMigration {
  const string Root="Assets/RainbowLi";
  sealed class Source {public string path;public int number,suit;public bool enemy;}
  static readonly Source[] Sources={
    new Source{path="Art/卡牌无铭刻/1花色/0E4A0BA085900244BDCFFC837C29A485.png",number=1,enemy=false,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/191A2662C859D28AEFE1FB85C3F36B89.png",number=6,enemy=true,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/224C8C5DDACE3EBD397C0A90A219F687.png",number=4,enemy=true,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/42A8E2193A00292CE0EB08DD91188552.png",number=6,enemy=false,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/4E66D0F22EA2F4D09D00C8D9E81386F1.png",number=1,enemy=true,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/5479EDF3833E1D546197B97D3B6FABA5.png",number=3,enemy=true,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/6E8EF30C7CC8984ADB21D2F53C8EFC15.png",number=2,enemy=true,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/BB131D00C70B69EBE998304B059BA5C0.png",number=4,enemy=false,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/C7CA66FC098928A166C27F7487194538.png",number=2,enemy=false,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/DF9FA508FE3BD0E0EC41489FA7387058.png",number=3,enemy=false,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/E44BE6C08759C23243B2CBA2EFE53713.png",number=5,enemy=true,suit=1},
    new Source{path="Art/卡牌无铭刻/1花色/E5852EC5AC14D1BDCFA2A125D94FAE5A.png",number=5,enemy=false,suit=1},
    new Source{path="Art/卡牌无铭刻/2/42E3A336975AD7FEB8B1721DEFA32BF2.png",number=6,enemy=false,suit=2},
    new Source{path="Art/卡牌无铭刻/2/5CD3F3249F91C1A61BC1E3C76C2318D0.png",number=1,enemy=true,suit=2},
    new Source{path="Art/卡牌无铭刻/2/61905C31E458059BEB1D252D1D408239.png",number=3,enemy=false,suit=2},
    new Source{path="Art/卡牌无铭刻/2/6E38FBF8FEE1A52F1085A984029E7C9D.png",number=3,enemy=true,suit=2},
    new Source{path="Art/卡牌无铭刻/2/7145FD33F230366A60BC77B225B56EED.png",number=2,enemy=false,suit=2},
    new Source{path="Art/卡牌无铭刻/2/8B41C67F91FE9E03C2ED947ABF084F8F.png",number=6,enemy=true,suit=2},
    new Source{path="Art/卡牌无铭刻/2/8FC631F7F185B52383ABF4FD741DA46A.png",number=4,enemy=false,suit=2},
    new Source{path="Art/卡牌无铭刻/2/936B98DB339ACD8E74167B18461579AB.png",number=5,enemy=true,suit=2},
    new Source{path="Art/卡牌无铭刻/2/995F1CE528B1CE15546D831CB616F60B.png",number=5,enemy=false,suit=2},
    new Source{path="Art/卡牌无铭刻/2/D0F54BC29D4ABD050338918505826DE4.png",number=2,enemy=true,suit=2},
    new Source{path="Art/卡牌无铭刻/2/DDE5D441EF9232C5035BD3CE68B6EAE5.png",number=4,enemy=true,suit=2},
    new Source{path="Art/卡牌无铭刻/2/F43EC53B85D93864794AC345D2106549.png",number=1,enemy=false,suit=2},
    new Source{path="Art/卡牌无铭刻/3/58DC6FE032171922F5370BCAC5975C72.png",number=2,enemy=true,suit=3},
    new Source{path="Art/卡牌无铭刻/3/58E11B9CA90BA8F810079B213108823A.png",number=5,enemy=true,suit=3},
    new Source{path="Art/卡牌无铭刻/3/70FB0290233BBAFB24E3BF3545A7FEC7.png",number=1,enemy=true,suit=3},
    new Source{path="Art/卡牌无铭刻/3/7ABE1CAB3D807DCF1EED40D74665D3CE.png",number=3,enemy=true,suit=3},
    new Source{path="Art/卡牌无铭刻/3/7E5E1032EE6A5E20883CE8FC656B4A6D.png",number=3,enemy=false,suit=3},
    new Source{path="Art/卡牌无铭刻/3/867C93F090E2AAB2FB0806B73C1043E0.png",number=2,enemy=false,suit=3},
    new Source{path="Art/卡牌无铭刻/3/9CD6483BE6C2E3DE01A25B6158A0983A.png",number=6,enemy=true,suit=3},
    new Source{path="Art/卡牌无铭刻/3/AC40F6A7B80094028522D99152D25EA8.png",number=5,enemy=false,suit=3},
    new Source{path="Art/卡牌无铭刻/3/B4384F5B3AB6D545C436ACA33192D71D.png",number=6,enemy=false,suit=3},
    new Source{path="Art/卡牌无铭刻/3/B56724BA548969B3EE8A9CC05BCD7786.png",number=4,enemy=true,suit=3},
    new Source{path="Art/卡牌无铭刻/3/DDF49AAD3C9472481CB7D886EFA1C8A3.png",number=1,enemy=false,suit=3},
    new Source{path="Art/卡牌无铭刻/3/E37EAF411AD0A21FFFA853620D352805.png",number=4,enemy=false,suit=3},
  };
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PASS: 36 player/enemy card skins configured in shared catalog, scene and card prefab.");EditorApplication.Exit(0);}
  [MenuItem("RainbowLi/UI/替换红蓝卡牌美术")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开RainbowLi场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();}
  public static void Configure(CardView card,CardSkinCatalog catalog){
   card.cardSkins=catalog;card.illustrated=true;card.face.material=null;card.face.type=Image.Type.Simple;card.face.preserveAspect=true;card.face.color=Color.white;
   foreach(var outline in card.face.GetComponents<Outline>())outline.enabled=false;
   var face=card.face.rectTransform;face.anchorMin=face.anchorMax=face.pivot=new Vector2(0,1);face.anchoredPosition=Vector2.zero;face.sizeDelta=new Vector2(130,195);
   card.topRank.gameObject.SetActive(false);card.bottomRank.gameObject.SetActive(false);card.topSuit.gameObject.SetActive(false);card.bottomSuit.gameObject.SetActive(false);foreach(var pip in card.pips)pip.gameObject.SetActive(false);
   var plate=card.transform.Find("CaptionPlate");if(plate!=null)plate.gameObject.SetActive(false);
   var rect=card.caption.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(13,-152);rect.sizeDelta=new Vector2(104,23);rect.localRotation=Quaternion.identity;
   card.caption.font=SourceHanFontMigration.Body;card.caption.fontStyle=FontStyle.Normal;card.caption.fontSize=15;card.caption.resizeTextForBestFit=true;card.caption.resizeTextMinSize=9;card.caption.resizeTextMaxSize=15;card.caption.alignment=TextAnchor.MiddleCenter;card.caption.color=Color.white;card.caption.transform.SetAsLastSibling();
   int number=int.TryParse(card.topRank.text,out var n)?n:1;bool enemy=card.name.StartsWith("EnemySkill",StringComparison.Ordinal)||card.name.StartsWith("ActionCard",StringComparison.Ordinal);
   card.face.sprite=catalog.Find(number,card.BoundEffect,enemy)??catalog.Find(1,CardEffect.Strike,enemy);
   var engraving=AssetDatabase.LoadAssetAtPath<EngravingArtCatalog>(Root+"/Data/DefaultEngravingArt.asset");if(engraving!=null)EngravingArtSceneMigration.Configure(card,engraving);
   foreach(var component in card.GetComponentsInChildren<Component>(true)){if(component==null)continue;EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
  }
  static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null)throw new InvalidOperationException("缺少GameView");
   string path=Root+"/Data/DefaultCardSkins.asset";var catalog=AssetDatabase.LoadAssetAtPath<CardSkinCatalog>(path);if(catalog==null){catalog=ScriptableObject.CreateInstance<CardSkinCatalog>();AssetDatabase.CreateAsset(catalog,path);}
   catalog.fallbackMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/GJ 战斗ui分解/CardPaper.mat");
   catalog.entries=Sources.Select(source=>{var previous=catalog.entries.FirstOrDefault(e=>e!=null&&e.suit==source.suit&&e.number==source.number&&e.enemy==source.enemy);if(previous!=null&&previous.sprite!=null)return previous;var sprite=AssetDatabase.LoadAllAssetsAtPath(Root+"/"+source.path).OfType<Sprite>().FirstOrDefault();if(sprite==null)throw new InvalidOperationException("缺少裁切Sprite："+source.path);return new CardSkinCatalog.Entry{suit=source.suit,number=source.number,enemy=source.enemy,sprite=sprite};}).ToArray();EditorUtility.SetDirty(catalog);
   string prefabPath=Root+"/Prefabs/PokerCard.prefab";var root=PrefabUtility.LoadPrefabContents(prefabPath);try{Configure(root.GetComponent<CardView>(),catalog);PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
   foreach(var card in view.GetComponentsInChildren<CardView>(true))Configure(card,catalog);
  }
 }
}
