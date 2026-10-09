using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class ContentSceneMigration {
  public static void Run(){var scene=EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("CONTENT SCENE PASS: nine hand slots, expandable intents and sequence editors saved.");}
  public static void Apply(){
   var v=Object.FindFirstObjectByType<GameView>();
   var hand=v.hand.ToList();while(hand.Count<9){var c=CloneCard(hand[0]);c.name="HandCard_"+hand.Count;hand.Add(c);}
   if(v.hand.Length<9){v.hand=hand.ToArray();for(int i=0;i<v.hand.Length;i++){var r=(RectTransform)v.hand[i].transform;r.anchoredPosition=new Vector2(25+i*115,-344);v.hand[i].transform.localScale=Vector3.one*.8f;}}
   var actions=v.actions.ToList();var labels=v.actionLabels.ToList();var left=v.leftButtons.ToList();var right=v.rightButtons.ToList();var remove=v.removeButtons.ToList();var inserts=v.insertButtons.ToList();
   while(actions.Count<10){int i=actions.Count;var c=CloneCard(actions[0]);c.name="ActionCard_"+i;actions.Add(c);labels.Add(Clone(v.actionLabels[0],"Actor_"+i));left.Add(Clone(v.leftButtons[0],"MoveLeft_"+i));right.Add(Clone(v.rightButtons[0],"MoveRight_"+i));remove.Add(Clone(v.removeButtons[0],"Remove_"+i));}
   while(inserts.Count<11)inserts.Add(Clone(v.insertButtons[0],"InsertSlot_"+inserts.Count));
   if(v.actions.Length<10){v.actions=actions.ToArray();v.actionLabels=labels.ToArray();v.leftButtons=left.ToArray();v.rightButtons=right.ToArray();v.removeButtons=remove.ToArray();v.insertButtons=inserts.ToArray();
    for(int i=0;i<10;i++){float x=36+i*101;Place(v.actions[i],x,85);v.actions[i].transform.localScale=Vector3.one*.68f;Place(v.actionLabels[i],x,50);v.actionLabels[i].fontSize=13;Place(v.leftButtons[i],x,260);Place(v.rightButtons[i],x+32,260);Place(v.removeButtons[i],x+64,260);}for(int i=0;i<11;i++)Place(v.insertButtons[i],8+i*101,145);
   }
   for(int i=0;i<v.ruleViews.Length;i++){var rule=v.ruleViews[i];rule.description.resizeTextForBestFit=true;rule.description.resizeTextMinSize=10;rule.description.resizeTextMaxSize=16;if(rule.primaryEffect==null){rule.primaryEffect=EffectButton(rule,"PrimaryEffect",188,"切换效果 1");UnityEventTools.AddIntPersistentListener(rule.primaryEffect.onClick,v.sequences.EditPrimary,i);}if(rule.secondaryEffect==null){rule.secondaryEffect=EffectButton(rule,"SecondaryEffect",211,"添加效果 2");UnityEventTools.AddIntPersistentListener(rule.secondaryEffect.onClick,v.sequences.EditSecondary,i);}EditorUtility.SetDirty(rule);}
   for(int i=0;i<v.hand.Length;i++)v.hand[i]=EnsurePrefab(v.hand[i]);
   for(int i=0;i<v.actions.Length;i++)v.actions[i]=EnsurePrefab(v.actions[i]);
   EditorUtility.SetDirty(v);
  }
  static CardView CloneCard(CardView source){var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RainbowLi/Prefabs/PokerCard.prefab");return ((GameObject)PrefabUtility.InstantiatePrefab(asset,source.transform.parent)).GetComponent<CardView>();}
  static CardView EnsurePrefab(CardView source){if(PrefabUtility.IsPartOfPrefabInstance(source))return source;var card=CloneCard(source);card.name=source.name;var old=(RectTransform)source.transform;var next=(RectTransform)card.transform;next.anchorMin=old.anchorMin;next.anchorMax=old.anchorMax;next.pivot=old.pivot;next.anchoredPosition=old.anchoredPosition;next.sizeDelta=old.sizeDelta;next.localScale=old.localScale;next.localRotation=old.localRotation;Object.DestroyImmediate(source.gameObject);return card;}
  static T Clone<T>(T source,string name)where T:Component{var c=Object.Instantiate(source,source.transform.parent);c.name=name;return c;}
  static void Place(Component c,float x,float y){((RectTransform)c.transform).anchoredPosition=new Vector2(x,-y);}
  static Button EffectButton(SequenceView rule,string name,float y,string caption){var b=Object.Instantiate(rule.digits[0],rule.transform);b.name=name;b.onClick=new Button.ButtonClickedEvent();var r=(RectTransform)b.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(5,-y);r.sizeDelta=new Vector2(142,21);var text=b.GetComponentInChildren<Text>();text.fontSize=12;text.text=caption;return b;}
 }
}
