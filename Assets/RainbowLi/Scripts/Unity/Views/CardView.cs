using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace RainbowLi.UnityGame {
public sealed class CardView : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler {
 public Image face, engravingBadge;
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
 public void SetDetails(TooltipView target,string title,string body){tooltip=target;detailTitle=title;detailBody=body;}
 private void Awake(){rect=(RectTransform)transform;home=rect.anchoredPosition;}
 public void Bind(int number,CardEffect effect,string description,bool enemy,bool engraved,bool active,bool enabled,Action clicked) {
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
  engravingBadge.gameObject.SetActive(engraved);
  Lift(active?selectedLift:0);
 }
 public void OnPointerEnter(PointerEventData e){if(button.interactable)Lift(hoverLift+(selected?selectedLift:0));if(tooltip!=null&&!string.IsNullOrEmpty(detailBody))tooltip.Show(this,detailTitle,detailBody,e.position);}
 public void OnPointerExit(PointerEventData e){Lift(selected?selectedLift:0);if(tooltip!=null)tooltip.Hide(this);}
 private void Lift(float amount){ if(attacking||!Application.isPlaying||!isActiveAndEnabled)return;if(movement!=null)StopCoroutine(movement);movement=StartCoroutine(MoveTo(home+Vector2.up*amount)); }
 private IEnumerator MoveTo(Vector2 target){Vector2 from=rect.anchoredPosition;for(float t=0;t<1;t+=Time.unscaledDeltaTime*9){rect.anchoredPosition=Vector2.LerpUnclamped(from,target,1-Mathf.Pow(1-t,3));yield return null;}rect.anchoredPosition=target;movement=null;}
 public void AttackToward(Vector3 worldTarget,float amount,BattleModule playback=null){
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
 private void OnDisable(){attacking=false;if(tooltip!=null)tooltip.Hide(this);if(movement!=null)StopCoroutine(movement);movement=null;if(rect!=null)rect.anchoredPosition=home;}
 public void SetLayout(Vector2 position,float scale){attacking=false;if(movement!=null)StopCoroutine(movement);movement=null;rect=(RectTransform)transform;home=position;rect.anchoredPosition=position;rect.localScale=Vector3.one*scale;}
 public void SetPreview(int number,CardEffect effect,string label){ Bind(number,effect,label,false,false,false,false,null); }
 public void SetPresentationOnly(){
  if(movement!=null)StopCoroutine(movement);movement=null;button.interactable=false;
  foreach(var graphic in GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
  enabled=false;
 }
}}
