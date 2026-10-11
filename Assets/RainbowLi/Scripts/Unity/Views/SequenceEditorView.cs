using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [Serializable]
 public sealed class SequenceEditorRow {
  public RectTransform root;
  public Text title,primaryText,secondaryText;
  public Button[] digits;
  public Text[] numbers;
  public Button primarySlot,secondarySlot,removeSecondary,serviceAction;
  public Image primaryFrame,secondaryFrame;
  public ScrollRect reserves;
  public Button effectTemplate;
  [NonSerialized] public readonly List<Button> choices=new List<Button>();
  [NonSerialized] public bool secondarySelected;
 }
 [DisallowMultipleComponent]
 public sealed class SequenceEditorView : MonoBehaviour {
  public GameSession session;
  public SequenceModule sequences;
  public GameObject panel,mapRoot,mapPanel;
  public Button exitButton;
  public Text instruction;
  public SequenceEditorRow[] rows;
  [Header("备用效果列表布局")]
  [Min(48)] public float effectWidth=104,effectHeight=96;
  [Min(0)] public float effectGap=14,effectPadding=8;
  public Color selectedColor=new Color(.28f,.27f,.105f),normalColor=new Color(.08f,.22f,.18f);
  private bool initialized;
  private int selectedRule;
  private int savedMapSibling=-1;
  private ContainmentGame editingGame;
  public bool IsOpen=>panel!=null&&panel.activeInHierarchy&&editingGame==session.Game;
  private void Awake(){InitializeControls();}
  private void OnEnable(){session.Changed+=Refresh;}
  private void OnDisable(){session.Changed-=Refresh;Close();}
  public void InitializeControls(){
   if(initialized)return;initialized=true;
   exitButton.onClick.AddListener(Close);
   for(int i=0;i<rows.Length;i++){
    int rule=i;var row=rows[i];
    for(int d=0;d<row.digits.Length;d++){int digit=d;row.digits[d].onClick.AddListener(()=>{selectedRule=rule;sequences.ChangeDigit(rule,digit);});}
    row.primarySlot.onClick.AddListener(()=>SelectSlot(rule,false));
    row.secondarySlot.onClick.AddListener(()=>SelectSlot(rule,true));
    row.removeSecondary.onClick.AddListener(()=>sequences.EquipEffect(rule,true,-1));
    row.serviceAction.onClick.AddListener(()=>{if(session.Game.Phase==GamePhase.Shop)sequences.BuyEffect(rule);else sequences.Rebuild(rule);});
   }
  }
  public void Open(int index){
   var game=session.Game;if(game==null||!AllowedPhase(game.Phase)||index<0||index>=game.PlayerRules.Count||index>=rows.Length)return;
   InitializeControls();bool entering=!IsOpen;editingGame=game;
   if(game.Phase!=GamePhase.Map&&savedMapSibling<0){savedMapSibling=mapPanel.transform.GetSiblingIndex();mapPanel.transform.SetAsLastSibling();}
   mapPanel.SetActive(true);mapRoot.SetActive(true);panel.SetActive(true);panel.transform.SetAsLastSibling();
   if(entering)foreach(var row in rows){row.secondarySelected=false;row.reserves.StopMovement();row.reserves.horizontalNormalizedPosition=0;}
   selectedRule=index;rows[index].secondarySelected=false;Refresh();
  }
  public void Close(){if(panel!=null)panel.SetActive(false);if(mapRoot!=null&&session!=null&&session.Game!=null&&session.Game.Phase!=GamePhase.Map){mapRoot.SetActive(false);mapPanel.SetActive(false);}if(savedMapSibling>=0){mapPanel.transform.SetSiblingIndex(savedMapSibling);savedMapSibling=-1;}editingGame=null;}
  public void SelectSlot(int rule,bool secondary){if(!IsOpen||rule<0||rule>=rows.Length)return;selectedRule=rule;rows[rule].secondarySelected=secondary;Refresh();}
  public void ChooseEffect(int rule,int availableIndex){if(!IsOpen||rule<0||rule>=rows.Length)return;sequences.EquipEffect(rule,rows[rule].secondarySelected,availableIndex);}
  public void Refresh(){
   if(panel==null||!panel.activeSelf)return;
   var game=session.Game;if(game==null||game!=editingGame||!AllowedPhase(game.Phase)){Close();return;}mapPanel.SetActive(true);mapRoot.SetActive(true);bool manual=game.Phase==GamePhase.Map;
   var pool=game.Player.AvailableEffects;
   instruction.text=manual?"正在调整牌序 "+(selectedRule+1)+" · 效果槽 "+(rows[selectedRule].secondarySelected?"2":"1")+" · 点击数字轮换 0–6（0 为未设置）\n先选择效果槽，再点击备用效果装入；备用效果可拖动或滚轮浏览。":game.Phase==GamePhase.Shop?"商店 · 在此购买并装备随机效果，消耗金币\n返回选关后，可自由调整数字和装配已获得的备用效果。":"休息 · 在此随机重构牌序数字，本次休息仅可使用一次服务\n返回选关后，可自由调整数字和装配已获得的备用效果。";
   for(int i=0;i<rows.Length;i++){
    var row=rows[i];bool present=i<game.PlayerRules.Count;row.root.gameObject.SetActive(present);if(!present)continue;
    var rule=game.PlayerRules[i];row.title.text="牌序 "+(i+1)+" · "+rule.Name;
    for(int d=0;d<row.numbers.Length;d++){row.numbers[d].text=rule.Numbers[d]==0?"未设置":rule.Numbers[d].ToString();row.digits[d].interactable=manual;}
    row.primaryText.text=Describe(new SequenceEffect{Effect=rule.Effect,Value=rule.Value,InsertsTwo=rule.InsertsTwo,Requirement=rule.Requirement})+ActiveHint(rule,rule.Requirement);
    row.secondaryText.text=rule.AdditionalEffect==null?"空槽\n点击选择":Describe(rule.AdditionalEffect)+ActiveHint(rule,rule.AdditionalEffect.Requirement);
    row.removeSecondary.gameObject.SetActive(manual&&rule.AdditionalEffect!=null);row.primarySlot.interactable=row.secondarySlot.interactable=manual;
    int price=session.definition==null?18:session.definition.rulePrice;
    row.serviceAction.gameObject.SetActive(!manual);row.serviceAction.interactable=game.Phase==GamePhase.Shop?game.Coins>=price:!game.ServiceUsed;row.serviceAction.GetComponentInChildren<Text>().text=game.Phase==GamePhase.Shop?"购买并装备随机效果 / "+price+" 金币":"随机重构数字 / 本次休息服务";
    row.primaryFrame.color=row.secondarySelected?normalColor:selectedColor;row.secondaryFrame.color=row.secondarySelected?selectedColor:normalColor;
    while(row.choices.Count<pool.Count){int effect=row.choices.Count;int owner=i;var tile=Instantiate(row.effectTemplate,row.reserves.content);tile.name="AvailableEffect_"+effect;tile.onClick.RemoveAllListeners();tile.onClick.AddListener(()=>ChooseEffect(owner,effect));row.choices.Add(tile);}
    float width=Mathf.Max(row.reserves.viewport.rect.width,effectPadding*2+pool.Count*effectWidth+Mathf.Max(0,pool.Count-1)*effectGap);
    row.reserves.content.sizeDelta=new Vector2(width,row.reserves.content.sizeDelta.y);
    for(int e=0;e<row.choices.Count;e++){
     var tile=row.choices[e];tile.gameObject.SetActive(e<pool.Count);if(e>=pool.Count)continue;
     var rect=(RectTransform)tile.transform;rect.anchoredPosition=new Vector2(effectPadding+e*(effectWidth+effectGap),-4);rect.sizeDelta=new Vector2(effectWidth,effectHeight);
     tile.GetComponentInChildren<Text>().text=Describe(pool[e]);tile.interactable=manual;
    }
   }
  }
  private static string ActiveHint(SequenceRule rule,PatternRequirement requirement)=>rule.Accepts(requirement)?"":"\n当前数字不满足条件";
  private static bool AllowedPhase(GamePhase phase)=>phase==GamePhase.Map||phase==GamePhase.Rest||phase==GamePhase.Shop;
  public static string Describe(SequenceEffect effect)=>new SequenceRule{Effect=effect.Effect,Value=effect.Value,InsertsTwo=effect.InsertsTwo,Requirement=effect.Requirement}.Description;
 }
}
