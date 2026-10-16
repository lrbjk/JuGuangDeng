using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent,RequireComponent(typeof(Button))]
 public sealed class AudioUiFeedback:MonoBehaviour,IPointerDownHandler,IPointerClickHandler,ISubmitHandler {
  public bool mainMenuButton,interaction;
  private Button button;
  private bool pressed,accepted;
  private void Awake(){button=GetComponent<Button>();}
  public void OnPointerDown(PointerEventData e){pressed=true;accepted=e.button==PointerEventData.InputButton.Left&&button.IsActive()&&button.IsInteractable();}
  public void OnPointerClick(PointerEventData e){bool valid=e.button==PointerEventData.InputButton.Left&&(pressed?accepted:button.IsActive()&&button.IsInteractable());pressed=false;if(valid)Emit();}
  public void OnSubmit(BaseEventData e){if(button.IsActive()&&button.IsInteractable())Emit();}
  private void Emit(){GetComponentInParent<GameView>()?.GetComponent<GameAudioController>()?.Play(mainMenuButton?GameAudioCue.MainMenuButton:interaction?GameAudioCue.Click:GameAudioCue.Button);}
 }
}
