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
 public int VisibleSlots {get;private set;}
 private bool previewDirty=true;
 private void OnValidate(){previewDirty=true;}
 private void Update(){if(!Application.isPlaying&&previewDirty&&view!=null&&hud!=null){previewDirty=false;Layout(Mathf.Min(2,view.actions.Length));}}
 public void EnsureVisuals(){
  if(view==null||hud==null)return;
  var content=hud.actionArea;var parent=hud.actionScroll==null?content.parent:hud.actionScroll.viewport.parent;
  if(surface==null){var found=parent.Find("PokerTableSurface");surface=found==null?CreateGraphic("PokerTableSurface",parent):found.GetComponent<PokerTableGraphic>();var r=surface.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.SetAsFirstSibling();}
  surface.color=feltColor;surface.borderColor=railColor;surface.cornerRadius=26;surface.borderWidth=3;surface.raycastTarget=false;surface.SetVerticesDirty();
 }
 public void Layout(int timelineCount){
  if(view==null||hud==null)return;EnsureVisuals();var g=view.session==null?null:view.session.Game;
  int remaining=g!=null&&g.Phase==GamePhase.Planning?Mathf.Max(0,g.PlayerActionLimit-g.PlayerActionCount):0;
  VisibleSlots=Mathf.Max(Mathf.Max(1,minimumSlots),timelineCount+remaining);
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
   emptyLabels[i].text=(i+1).ToString("00");emptyLabels[i].gameObject.SetActive(i>=timelineCount);
  }
  for(int i=0;i<view.actions.Length;i++){
   float x=start+i*stride;var r=(RectTransform)view.actions[i].transform;var p=new Vector2(x,-cardTop);
   if((r.anchoredPosition-p).sqrMagnitude>1||Mathf.Abs(r.localScale.x-cardScale)>.001f)view.actions[i].SetLayout(p,cardScale);
   var label=view.actionLabels[i];var lr=label.rectTransform;lr.anchorMin=lr.anchorMax=lr.pivot=new Vector2(0,1);lr.anchoredPosition=new Vector2(x,-4);lr.sizeDelta=new Vector2(width,21);label.fontSize=14;label.alignment=TextAnchor.MiddleCenter;
  }
  for(int i=0;i<view.insertButtons.Length;i++){var r=(RectTransform)view.insertButtons[i].transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(24,36);r.anchoredPosition=new Vector2(start+i*stride-slotGap*.5f-12,-69);}
 }
 private void EnsureSlots(int count){
  var graphics=new List<PokerTableGraphic>(slots);var labels=new List<Text>(emptyLabels);
  while(graphics.Count<count){int i=graphics.Count;var frame=CreateGraphic("PokerSlot_"+i.ToString("00"),hud.actionArea);frame.rectTransform.SetAsFirstSibling();var go=new GameObject("EmptyNumber",typeof(RectTransform),typeof(Text));var r=(RectTransform)go.transform;r.SetParent(frame.transform,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;var text=go.GetComponent<Text>();text.font=view.actionLabels[0].font;text.fontSize=21;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.66f,.71f,.58f,.5f);text.raycastTarget=false;graphics.Add(frame);labels.Add(text);}
  slots=graphics.ToArray();emptyLabels=labels.ToArray();
 }
 private static PokerTableGraphic CreateGraphic(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(PokerTableGraphic));go.transform.SetParent(parent,false);var graphic=go.GetComponent<PokerTableGraphic>();graphic.raycastTarget=false;return graphic;}
}
}
