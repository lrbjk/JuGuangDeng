using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class StartMenuView : MonoBehaviour {
  public GameView view;
  public GameObject panel,settingsPanel,tutorialPanel;
  [Header("主界面文案直接编辑 Text；教学手册正文由 TutorialManual 文本资源载入")]
  public Text introduction,credits,flavor,tutorial;
  public ScrollRect introductionScroll;
  public Button start,quit,settings,manual,closeSettings,closeManual,speed;
  public Slider volume;
  public Button continueGame,resume,confirmReplace,cancelReplace;
  public Text message,replaceMessage;
  public GameObject replacePanel;
  bool hasActiveRun;
  int pendingReplacement;
  public bool IsOpen=>panel!=null&&panel.activeSelf;
  void Awake(){
   start.onClick.AddListener(StartGame);quit.onClick.AddListener(QuitGame);
   var handbook=TutorialManualView.Ensure(this);
   settings.onClick.AddListener(()=>settingsPanel.SetActive(true));manual.onClick.AddListener(handbook.Open);
   closeManual.onClick.AddListener(()=>tutorialPanel.SetActive(false));
   if(continueGame!=null)continueGame.onClick.AddListener(()=>RequestReplacement(2));
   if(resume!=null)resume.onClick.AddListener(ResumeGame);
   if(confirmReplace!=null)confirmReplace.onClick.AddListener(ConfirmReplacement);
   if(cancelReplace!=null)cancelReplace.onClick.AddListener(CancelReplacement);
   view.session.PauseElapsedTime=IsOpen;
  }
  void OnDisable(){if(view!=null&&view.session!=null)view.session.PauseElapsedTime=false;}
  void Update(){if(!IsOpen)return;if(continueGame!=null)continueGame.interactable=System.IO.File.Exists(view.session.SavePath);if(resume!=null)resume.gameObject.SetActive(hasActiveRun);}
  public void StartGame(){RequestReplacement(1);}
  void RequestReplacement(int operation){if(operation==2&&!System.IO.File.Exists(view.session.SavePath)){message.text="没有可读取的存档。";return;}if(hasActiveRun){pendingReplacement=operation;replaceMessage.text=(operation==1?"开始新游戏":"读取存档")+"将替换当前一局。未保存的进度将丢失。是否继续？";replacePanel.SetActive(true);return;}ExecuteReplacement(operation);}
  public void ConfirmReplacement(){int operation=pendingReplacement;CancelReplacement();if(operation!=0)ExecuteReplacement(operation);}
  public void CancelReplacement(){pendingReplacement=0;if(replacePanel!=null)replacePanel.SetActive(false);}
  void ExecuteReplacement(int operation){
   if(operation==2&&!view.session.ContinueGame()){message.text="存档读取失败，当前游戏未改变。";return;}
   if(operation==1)view.session.NewGame();hasActiveRun=true;ResumeGame();
  }
  public void ReturnToMenu(){
   hasActiveRun=true;view.session.PauseElapsedTime=true;settingsPanel.SetActive(false);tutorialPanel.SetActive(false);CancelReplacement();
   view.GetComponent<DeckOperationView>()?.Close();view.GetComponent<SequenceEditorView>()?.Close();view.battleHud.ClosePile();
   panel.SetActive(true);panel.transform.SetAsLastSibling();message.text="当前一局已暂停，可以继续本局。";
  }
  public void ResumeGame(){
   if(!hasActiveRun)return;
   settingsPanel.SetActive(false);tutorialPanel.SetActive(false);
   view.GetComponent<DeckOperationView>()?.Close();view.GetComponent<SequenceEditorView>()?.Close();
   CancelReplacement();panel.SetActive(false);view.session.PauseElapsedTime=false;view.session.Notify();
  }
  public void QuitGame(){Application.Quit();}
 }
}
