using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RainbowLi.UnityGame {
 // Highlight assets include a glow margin; keep the normal button's hit area unchanged.
 [DisallowMultipleComponent,RequireComponent(typeof(Button))]
 public sealed class MainMenuButtonArt : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,
  IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler {
  public Image normal,glow;
  Button button;
  bool hovered,selected,pressed;
  void Awake(){button=GetComponent<Button>();}
  void OnEnable(){Refresh();}
  void OnDisable(){hovered=selected=pressed=false;if(glow!=null)glow.enabled=false;}
  void LateUpdate(){Refresh();}
  void Refresh(){
   if(button==null)button=GetComponent<Button>();
   bool enabled=button.IsInteractable();
   if(normal!=null)normal.color=!enabled?new Color(.4f,.48f,.48f,1):pressed?new Color(.7f,.86f,.86f,1):Color.white;
   if(glow!=null){glow.enabled=enabled&&(hovered||selected);glow.color=pressed?new Color(.7f,.86f,.86f,1):Color.white;}
  }
  public void OnPointerEnter(PointerEventData e){hovered=true;Refresh();}
  public void OnPointerExit(PointerEventData e){hovered=false;pressed=false;Refresh();}
  public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left){pressed=true;Refresh();}}
  public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left){pressed=false;Refresh();}}
  public void OnSelect(BaseEventData e){selected=true;Refresh();}
  public void OnDeselect(BaseEventData e){selected=pressed=false;Refresh();}
 }
}
