using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RainbowLi.UnityGame {
[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class PokerActionLayout : MonoBehaviour {
 public GameView view;
 public BattleHudView hud;
 [Min(1)] public int minimumSlots=5;
 [Range(.3f,.8f)] public float cardScale=.64f;
 [Min(4)] public float slotGap=30,horizontalPadding=40;
 [Min(20)] public float cardTop=28;
 public Color feltColor=new Color(.035f,.16f,.13f),railColor=new Color(.64f,.53f,.32f,.85f);
 public Color emptySlotColor=new Color(.075f,.21f,.17f),slotBorderColor=new Color(.65f,.68f,.49f,.36f);
 public PokerTableGraphic surface;
 public PokerTableGraphic[] slots=Array.Empty<PokerTableGraphic>();
 public Text[] emptyLabels=Array.Empty<Text>();
 [Header("美术卡槽")] public bool illustratedSlots;
 public Sprite slotArtwork,surfaceArtwork;
 [Min(0),Tooltip("美术布局前方保留的空卡槽。")] public int leadingEmptySlots=3;
 [Min(0),Tooltip("前一行动与后续敌方行动之间保留的插牌空位。")] public int enemyInsertionGap=1;
 private void ApplyArtwork(PokerTableGraphic target,Sprite sprite){
  if(!illustratedSlots||sprite==null)return;
  target.color=Color.clear;target.borderColor=Color.clear;target.SetVerticesDirty();
  var child=target.transform.Find("Art");Image image;
  if(child==null){var go=new GameObject("Art",typeof(RectTransform),typeof(Image));go.transform.SetParent(target.transform,false);image=go.GetComponent<Image>();image.rectTransform.anchorMin=Vector2.zero;image.rectTransform.anchorMax=Vector2.one;image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;go.transform.SetAsFirstSibling();}else image=child.GetComponent<Image>();
  image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;
 }
 public int VisibleSlots {get;private set;}
 private bool previewDirty=true;
 private void OnValidate(){previewDirty=true;}
 private void Update(){if(!Application.isPlaying&&previewDirty&&view!=null&&hud!=null){previewDirty=false;Layout(Mathf.Min(2,view.actions.Length));}}
 public void EnsureVisuals(){
  if(view==null||hud==null)return;
  var content=hud.actionArea;var parent=hud.actionScroll==null?content.parent:hud.actionScroll.viewport.parent;
  if(surface==null){var found=parent.Find("PokerTableSurface");surface=found==null?CreateGraphic("PokerTableSurface",parent):found.GetComponent<PokerTableGraphic>();var r=surface.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.SetAsFirstSibling();}
  surface.color=feltColor;surface.borderColor=railColor;surface.cornerRadius=26;surface.borderWidth=3;surface.raycastTarget=false;surface.SetVerticesDirty();ApplyArtwork(surface,surfaceArtwork);
 }
 public void Layout(int timelineCount){
  if(view==null||hud==null)return;EnsureVisuals();var g=view.session==null?null:view.session.Game;
  int remaining=g!=null&&g.Phase==GamePhase.Planning?Mathf.Max(0,g.PlayerActionLimit-g.PlayerActionCount):0;
  var owners=new List<bool>();for(int i=0;i<timelineCount;i++)owners.Add(g!=null&&i<g.Timeline.Count?g.Timeline[i].Enemy:true);
  var positions=BuildSlotPositions(owners);int cursor=positions.Length>0?positions[positions.Length-1]+1:0;
  int occupiedEnd=timelineCount>0&&timelineCount<=positions.Length?positions[timelineCount-1]+1:0;
  VisibleSlots=Mathf.Max(Mathf.Max(1,minimumSlots),illustratedSlots?occupiedEnd+remaining:timelineCount+remaining);
  var occupied=new HashSet<int>();for(int i=0;i<timelineCount&&i<positions.Length;i++)occupied.Add(positions[i]);
  var content=hud.actionArea;float width=130*cardScale,stride=width+slotGap;
  float viewportWidth=hud.actionScroll==null?content.rect.width:hud.actionScroll.viewport.rect.width;
  float rowWidth=VisibleSlots*stride-slotGap;
  if(hud.actionScroll!=null)content.sizeDelta=new Vector2(Mathf.Max(viewportWidth,rowWidth+horizontalPadding*2),content.sizeDelta.y);
  float start=(content.rect.width-rowWidth)*.5f;
  EnsureSlots(VisibleSlots);
  for(int i=0;i<slots.Length;i++){
   bool active=i<VisibleSlots;slots[i].gameObject.SetActive(active);if(!active)continue;
   float x=start+i*stride;var r=slots[i].rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-cardTop);r.sizeDelta=new Vector2(width,195*cardScale);
   slots[i].color=emptySlotColor;slots[i].borderColor=slotBorderColor;slots[i].cornerRadius=8;slots[i].borderWidth=1.5f;slots[i].SetVerticesDirty();
   ApplyArtwork(slots[i],slotArtwork);
   emptyLabels[i].text=illustratedSlots?"":(i+1).ToString("00");emptyLabels[i].gameObject.SetActive(!occupied.Contains(i));
  }
  for(int i=0;i<view.actions.Length;i++){
   if(i>=positions.Length)continue;float x=start+positions[i]*stride;var r=(RectTransform)view.actions[i].transform;var p=new Vector2(x,-cardTop);
   if((r.anchoredPosition-p).sqrMagnitude>1||Mathf.Abs(r.localScale.x-cardScale)>.001f)view.actions[i].SetLayout(p,cardScale);
   var label=view.actionLabels[i];var lr=label.rectTransform;lr.anchorMin=lr.anchorMax=lr.pivot=new Vector2(0,1);lr.anchoredPosition=new Vector2(x,-4);lr.sizeDelta=new Vector2(width,21);label.fontSize=14;label.alignment=TextAnchor.MiddleCenter;label.enabled=!illustratedSlots;
  }
  for(int i=0;i<view.insertButtons.Length;i++){var r=(RectTransform)view.insertButtons[i].transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(24,36);float insertSlot=i<positions.Length?positions[i]:cursor;r.anchoredPosition=new Vector2(start+insertSlot*stride-slotGap*.5f-12,-69);}
 }
 // Keep enemy anchors stable while player cards consume the empty spaces before them.
 public int[] BuildSlotPositions(IReadOnlyList<bool> enemies){
  var result=new int[enemies.Count];int cursor=0,enemy=0,segment=0;
  for(int i=0;i<enemies.Count;i++){
   if(!enemies[i])continue;
   int anchor=illustratedSlots?Mathf.Max(0,leadingEmptySlots)+enemy*(1+Mathf.Max(0,enemyInsertionGap)):i;
   int first=Mathf.Max(cursor,anchor-(i-segment));
   for(int j=segment;j<=i;j++)result[j]=first+j-segment;
   cursor=result[i]+1;segment=i+1;enemy++;
  }
  for(int i=segment;i<enemies.Count;i++)result[i]=cursor++;
  return result;
 }
 public bool TryGetInsertionRect(int insertion,BattleAction moving,out Rect rect){
  rect=default;var g=view?.session?.Game;if(g==null||insertion<0||insertion>g.Timeline.Count)return false;
  var owners=new List<bool>();int target=insertion;
  for(int i=0;i<g.Timeline.Count;i++){if(g.Timeline[i]==moving){if(i<insertion)target--;continue;}owners.Add(g.Timeline[i].Enemy);}
  owners.Insert(target,false);var positions=BuildSlotPositions(owners);
  float width=130*cardScale,stride=width+slotGap;
  float rowWidth=VisibleSlots*stride-slotGap,start=(hud.actionArea.rect.width-rowWidth)*.5f;
  rect=new Rect(start+positions[target]*stride,-cardTop-195*cardScale,width,195*cardScale);return true;
 }
 private void EnsureSlots(int count){
  var graphics=new List<PokerTableGraphic>(slots);var labels=new List<Text>(emptyLabels);
  while(graphics.Count<count){int i=graphics.Count;var frame=CreateGraphic("PokerSlot_"+i.ToString("00"),hud.actionArea);frame.rectTransform.SetAsFirstSibling();var go=new GameObject("EmptyNumber",typeof(RectTransform),typeof(Text));var r=(RectTransform)go.transform;r.SetParent(frame.transform,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;var text=go.GetComponent<Text>();text.font=view.actionLabels[0].font;text.fontSize=21;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.66f,.71f,.58f,.5f);text.raycastTarget=false;graphics.Add(frame);labels.Add(text);}
  slots=graphics.ToArray();emptyLabels=labels.ToArray();
 }
 private static PokerTableGraphic CreateGraphic(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(PokerTableGraphic));go.transform.SetParent(parent,false);var graphic=go.GetComponent<PokerTableGraphic>();graphic.raycastTarget=false;return graphic;}
}
}
