using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace RainbowLi.UnityGame {
public sealed class CardView : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerMoveHandler {
 public Image face, engravingBadge;
 [Header("美术牌面")] public bool illustrated;
 public Sprite illustratedFace,attackIcon,guardIcon,healIcon,otherIcon;
 public CardSkinCatalog cardSkins;
 public EngravingArtCatalog engravingArt;
 public RuntimeEngraving BoundEngraving {get;private set;}
 public Color illustratedInk=new Color(.04f,.1f,.12f);
 public Text topRank,bottomRank,caption;
 public Image topSuit,bottomSuit;
 public Image[] pips;
 public Sprite[] suits;
 public Button button;
 public CardEffect BoundEffect {get;private set;}
 public Color playerFace=new Color(.98f,.95f,.85f),enemyFace=new Color(.96f,.84f,.78f);
 [Range(0,30)] public float hoverLift=14, selectedLift=20;
 private UnityAction callback;
 private RectTransform rect;
 private Vector2 home;
 private bool selected,attacking;
 private Coroutine movement;
 private TooltipView tooltip;private string detailTitle,detailBody;
 private CardView hoverPreview;
 private Canvas hoverCanvas;
 private Vector2 hoverPointer;
 private float hoverProgress;
 private static CardView hovered;
 private GameView ownerView;
 private readonly System.Collections.Generic.List<RaycastResult> hoverHits=new System.Collections.Generic.List<RaycastResult>();
 private readonly System.Collections.Generic.Dictionary<Graphic,float> hoverSourceAlphas=new System.Collections.Generic.Dictionary<Graphic,float>();
 public bool HasHoverPreview=>hoverPreview!=null;
 private void HideHoverSource(){
  foreach(var graphic in GetComponentsInChildren<Graphic>(true)){
   if(!hoverSourceAlphas.ContainsKey(graphic))hoverSourceAlphas[graphic]=graphic.color.a;
   var color=graphic.color;color.a=0;graphic.color=color;
  }
 }
 private void RestoreHoverSource(){
  foreach(var pair in hoverSourceAlphas)if(pair.Key!=null){var color=pair.Key.color;color.a=pair.Value;pair.Key.color=color;}
  hoverSourceAlphas.Clear();
 }
 public void SetDetails(TooltipView target,string title,string body){tooltip=target;detailTitle=title;detailBody=body;}
 private void Awake(){rect=(RectTransform)transform;home=rect.anchoredPosition;ownerView=GetComponentInParent<GameView>();}
 public void Bind(int number,CardEffect effect,string description,bool enemy,bool engraved,bool active,bool enabled,Action clicked,RuntimeEngraving engraving=null) {
  if(rect==null){rect=(RectTransform)transform;home=rect.anchoredPosition;}
  if(callback!=null)button.onClick.RemoveListener(callback);
  callback=()=>clicked?.Invoke(); button.onClick.AddListener(callback);button.interactable=enabled;
  selected=active; face.color=enemy?enemyFace:playerFace;
  BoundEffect=effect;
  topRank.text=bottomRank.text=number.ToString();caption.text=description;
  int suit=enemy?3:effect==CardEffect.Strike||effect==CardEffect.IncreasedStrike||effect==CardEffect.InverseStrike?0:effect==CardEffect.Guard||effect==CardEffect.IronWall||effect==CardEffect.Fortify||effect==CardEffect.Parry||effect==CardEffect.Barrier||effect==CardEffect.DamageCap?1:effect==CardEffect.Mend||effect==CardEffect.Regeneration?2:3;
  Color color=suit==1?new Color(.1f,.22f,.28f):new Color(.68f,.12f,.14f);
  topRank.color=bottomRank.color=color;
  topSuit.sprite=bottomSuit.sprite=suits[suit];topSuit.color=bottomSuit.color=color;
  for(int i=0;i<pips.Length;i++){ pips[i].gameObject.SetActive(i<number);pips[i].sprite=suits[suit];pips[i].color=color; }
  if(illustrated){
   face.sprite=illustratedFace;face.type=Image.Type.Simple;
   topRank.color=bottomRank.color=illustratedInk;caption.color=Color.white;
   topSuit.sprite=suit==0?attackIcon:suit==1?guardIcon:suit==2?healIcon:otherIcon;topSuit.color=Color.white;
   bottomSuit.gameObject.SetActive(false);foreach(var pip in pips)pip.gameObject.SetActive(false);
  }
  if(illustrated&&cardSkins!=null){
   var artwork=cardSkins.Find(number,effect,enemy);bool baked=artwork!=null;
   face.sprite=baked?artwork:illustratedFace;face.material=baked?null:cardSkins.fallbackMaterial;face.color=baked?Color.white:(enemy?enemyFace:playerFace);face.preserveAspect=baked;if(baked)caption.color=Color.white;
   topRank.gameObject.SetActive(!baked);bottomRank.gameObject.SetActive(!baked);topSuit.gameObject.SetActive(!baked);
   var plate=transform.Find("CaptionPlate");if(plate!=null)plate.gameObject.SetActive(!baked);
  }
  SetEngraving(engraved,engraving);
  Lift(active?selectedLift:0);
 }
 public void SetEngraving(bool engraved,RuntimeEngraving engraving){
  BoundEngraving=engraved?engraving:null;
  engravingBadge.gameObject.SetActive(engraved);if(!engraved)return;
  var frame=illustrated&&engravingArt!=null?engravingArt.Find(engraving):null;
  if(frame==null)return;
  engravingBadge.sprite=frame;engravingBadge.color=Color.white;engravingBadge.material=null;
  engravingBadge.raycastTarget=false;engravingBadge.type=Image.Type.Simple;engravingBadge.preserveAspect=false;
  // Both textures use the same source canvas. Align their trimmed Sprite coordinates,
  // retaining the frame's diamond above the card instead of stretching it into the face.
  var baseRect=face.sprite.rect;var area=face.rectTransform.rect;
  float scale=Mathf.Min(area.width/baseRect.width,area.height/baseRect.height);
  var r=engravingBadge.rectTransform;r.SetParent(face.transform,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
  r.anchoredPosition=new Vector2((area.width-baseRect.width*scale)*.5f+(frame.rect.x-baseRect.x)*scale,
   -(area.height-baseRect.height*scale)*.5f+(frame.rect.yMax-baseRect.yMax)*scale);
  r.sizeDelta=frame.rect.size*scale;r.localScale=Vector3.one;r.localRotation=Quaternion.identity;
 }
 public void OnPointerEnter(PointerEventData e){
  if(!Application.isPlaying||!isActiveAndEnabled||attacking||GetComponent<HandCardDrag>()?.IsDragging==true)return;
  if(hovered!=null&&hovered!=this)hovered.CancelHover();hoverPointer=e.position;
  var canvas=GetComponentInParent<Canvas>();if(canvas!=null&&hoverPreview==null){
   hoverCanvas=canvas.rootCanvas;hoverPreview=Instantiate(this,hoverCanvas.transform);hoverPreview.name="HoveredCardPreview";
   hoverPreview.SetPresentationOnly();var drag=hoverPreview.GetComponent<HandCardDrag>();if(drag!=null)drag.enabled=false;
   var group=hoverPreview.GetComponent<CanvasGroup>();if(group==null)group=hoverPreview.gameObject.AddComponent<CanvasGroup>();group.alpha=1;group.blocksRaycasts=false;group.interactable=false;group.ignoreParentGroups=true;
   hoverProgress=0;hovered=this;PositionHover();HideHoverSource();
  }
  if(tooltip!=null&&!string.IsNullOrEmpty(detailBody))tooltip.Show(this,detailTitle,detailBody,e.position);
 }
 public void OnPointerMove(PointerEventData e){hoverPointer=e.position;}
 public void OnPointerExit(PointerEventData e){CancelHover();if(tooltip!=null)tooltip.Hide(this);}
 public void CancelHover(){RestoreHoverSource();if(hoverPreview!=null){hoverPreview.gameObject.SetActive(false);Destroy(hoverPreview.gameObject);}hoverPreview=null;hoverCanvas=null;hoverProgress=0;if(hovered==this)hovered=null;}
 private void LateUpdate(){
  if(hoverPreview==null)return;
  foreach(var group in GetComponentsInParent<CanvasGroup>())if(!group.ignoreParentGroups&&(!group.blocksRaycasts||group.alpha<=.01f)){CancelHover();return;}
  if(EventSystem.current!=null){hoverHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=hoverPointer},hoverHits);if(hoverHits.Count>0&&!hoverHits[0].gameObject.transform.IsChildOf(transform)){CancelHover();return;}}
  HideHoverSource();
  var profile=ownerView?.cardMotion?.profile;hoverProgress=Mathf.Clamp01(hoverProgress+Time.unscaledDeltaTime/Mathf.Max(.01f,profile==null?.12f:profile.hoverDuration));PositionHover();
 }
 private void PositionHover(){
  var parent=(RectTransform)hoverCanvas.transform;var preview=(RectTransform)hoverPreview.transform;var profile=ownerView?.cardMotion?.profile;
  float ease=1-Mathf.Pow(1-hoverProgress,3),scale=Mathf.Lerp(1,profile==null?1.25f:profile.hoverScale,ease);
  preview.anchorMin=preview.anchorMax=preview.pivot=new Vector2(.5f,.5f);preview.localRotation=Quaternion.identity;
  preview.localScale=new Vector3(transform.lossyScale.x/parent.lossyScale.x,transform.lossyScale.y/parent.lossyScale.y,1)*scale;
  var center=(Vector2)parent.InverseTransformPoint(rect.TransformPoint(rect.rect.center))+Vector2.up*(profile==null?hoverLift:profile.hoverLift)*ease;
  var half=Vector2.Scale(preview.rect.size,(Vector2)preview.localScale)*.5f;var bounds=parent.rect;
  center.x=Mathf.Clamp(center.x,bounds.xMin+Mathf.Min(half.x+8,bounds.width*.5f),bounds.xMax-Mathf.Min(half.x+8,bounds.width*.5f));center.y=Mathf.Clamp(center.y,bounds.yMin+Mathf.Min(half.y+8,bounds.height*.5f),bounds.yMax-Mathf.Min(half.y+8,bounds.height*.5f));
  preview.localPosition=new Vector3(center.x,center.y,0);
 }
 private void Lift(float amount){ if(attacking||!Application.isPlaying||!isActiveAndEnabled)return;if(movement!=null)StopCoroutine(movement);movement=StartCoroutine(MoveTo(home+Vector2.up*amount)); }
 private IEnumerator MoveTo(Vector2 target){Vector2 from=rect.anchoredPosition;for(float t=0;t<1;t+=Time.unscaledDeltaTime*9){rect.anchoredPosition=Vector2.LerpUnclamped(from,target,1-Mathf.Pow(1-t,3));yield return null;}rect.anchoredPosition=target;movement=null;}
 public void AttackToward(Vector3 worldTarget,float amount,BattleModule playback=null){
  CancelHover();
  if(!isActiveAndEnabled)return;
  if(movement!=null)StopCoroutine(movement);
  Vector3 localTarget=rect.parent.InverseTransformPoint(worldTarget);
  Vector2 direction=((Vector2)localTarget-(Vector2)rect.localPosition).normalized;
  attacking=true;movement=StartCoroutine(Punch(direction,amount,playback));
 }
 private IEnumerator Punch(Vector2 direction,float amount,BattleModule playback){
  float began=playback==null?Time.unscaledTime:playback.PresentationTime;
  while(true){float age=(playback==null?Time.unscaledTime:playback.PresentationTime)-began;if(age>=.42f)break;rect.anchoredPosition=home+direction*Mathf.Sin(age/.42f*Mathf.PI)*amount;yield return null;}
  rect.anchoredPosition=home;movement=null;attacking=false;
 }
 public void ClearAttack(){if(!attacking)return;if(movement!=null)StopCoroutine(movement);movement=null;attacking=false;if(rect!=null)rect.anchoredPosition=home;}
 private void OnDisable(){CancelHover();attacking=false;if(tooltip!=null)tooltip.Hide(this);if(movement!=null)StopCoroutine(movement);movement=null;if(rect!=null)rect.anchoredPosition=home;}
 public void SetLayout(Vector2 position,float scale){attacking=false;if(movement!=null)StopCoroutine(movement);movement=null;rect=(RectTransform)transform;home=position;rect.anchoredPosition=position;rect.localScale=Vector3.one*scale;}
 public void SetPreview(int number,CardEffect effect,string label){ Bind(number,effect,label,false,false,false,false,null); }
 public void SetPresentationOnly(){
  CancelHover();
  if(movement!=null)StopCoroutine(movement);movement=null;button.interactable=false;
  foreach(var graphic in GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
  enabled=false;
 }
}}
