using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class SequenceView : MonoBehaviour {
 public Text title,description;
 public Button primaryEffect,secondaryEffect;
 public Button[] digits;
 public Text[] numbers;
 public void Initialize(SequenceModule module,int index){for(int d=0;d<3;d++){int digit=d;digits[d].onClick.AddListener(()=>module.ChangeDigit(index,digit));}}
 public void Bind(SequenceRule rule,bool editable){if(primaryEffect!=null){primaryEffect.gameObject.SetActive(editable);primaryEffect.GetComponentInChildren<Text>().text="切换效果 1";}if(secondaryEffect!=null){secondaryEffect.gameObject.SetActive(editable);secondaryEffect.GetComponentInChildren<Text>().text=rule.AdditionalEffect==null?"添加效果 2":"切换 / 移除效果 2";}title.text=rule.Name;description.text=rule.Description+(rule.Triggers>0?" · "+rule.Triggers+" 次":"");for(int d=0;d<3;d++){numbers[d].text=rule.Numbers[d]==0?"—":rule.Numbers[d].ToString();digits[d].interactable=editable;}}
}}
