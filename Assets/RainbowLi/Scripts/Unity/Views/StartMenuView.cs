using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class StartMenuView : MonoBehaviour {
  public GameView view;
  public GameObject panel,settingsPanel,tutorialPanel;
  [Header("文案直接编辑对应 Text，不会被运行时代码覆盖")]
  public Text introduction,credits,flavor,tutorial;
  public ScrollRect introductionScroll;
  public Button start,quit,settings,manual,closeSettings,closeManual,speed;
  public Slider volume;
  public bool IsOpen=>panel!=null&&panel.activeSelf;
  void Awake(){
   start.onClick.AddListener(StartGame);quit.onClick.AddListener(QuitGame);
   settings.onClick.AddListener(()=>settingsPanel.SetActive(true));manual.onClick.AddListener(()=>tutorialPanel.SetActive(true));
   closeManual.onClick.AddListener(()=>tutorialPanel.SetActive(false));
   view.session.PauseElapsedTime=IsOpen;
  }
  void OnDisable(){if(view!=null&&view.session!=null)view.session.PauseElapsedTime=false;}
  public void StartGame(){
   settingsPanel.SetActive(false);tutorialPanel.SetActive(false);
   view.GetComponent<DeckOperationView>()?.Close();view.GetComponent<SequenceEditorView>()?.Close();
   view.session.NewGame();panel.SetActive(false);view.session.PauseElapsedTime=false;
  }
  public void QuitGame(){Application.Quit();}
 }
}
