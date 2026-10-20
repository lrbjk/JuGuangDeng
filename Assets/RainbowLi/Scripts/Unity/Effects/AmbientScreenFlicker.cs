using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class AmbientScreenFlicker : MonoBehaviour {
  [Tooltip("策划调参入口。未绑定时自动读取 Resources/AmbientGlitchProfile。")]
  public AmbientGlitchProfile profile;
  public bool effectEnabled=true;
  [Tooltip("随机等待区间，单位秒；默认每 2–4 分钟触发一次。")][Min(1)] public float minIntervalSeconds=120,maxIntervalSeconds=240;
  [Tooltip("一次短暂花屏的时长，不受战斗倍速影响。")][Min(.05f)] public float duration=.65f;
  [Tooltip("控制横向撕裂距离和噪点强度。")][Range(0,1)] public float intensity=.9f;
  [HideInInspector] public Image overlay;
  private readonly System.Random random=new System.Random();
  private readonly RawImage[] tears=new RawImage[14],noiseBands=new RawImage[4];
  private readonly Image[] scanLines=new Image[20];
  private readonly Color32[] noisePixels=new Color32[256*32];
  private Texture2D snapshot,noise;
  private Coroutine captureRoutine;
  private float remaining,age;
  private int lastFrame=-1;
  private bool flashing,captureRequested;
  public static AmbientScreenFlicker Ensure(GameView view){
   var effect=view.GetComponent<AmbientScreenFlicker>();if(effect==null)effect=view.gameObject.AddComponent<AmbientScreenFlicker>();
   effect.ApplyProfile();effect.EnsureOverlay();return effect;
  }
  private void ApplyProfile(){
   if(profile==null)profile=Resources.Load<AmbientGlitchProfile>("AmbientGlitchProfile");if(profile==null)return;
   profile.Validate();bool changed=minIntervalSeconds!=profile.minIntervalSeconds||maxIntervalSeconds!=profile.maxIntervalSeconds;
   effectEnabled=profile.effectEnabled;minIntervalSeconds=profile.minIntervalSeconds;maxIntervalSeconds=profile.maxIntervalSeconds;duration=profile.duration;intensity=profile.intensity;
   if(changed)Schedule();
  }
  public void PreviewNow(){if(!Application.isPlaying||!isActiveAndEnabled)return;ApplyProfile();if(!effectEnabled)return;EnsureOverlay();CancelGlitch(false);captureRequested=true;}
  public void EnsureOverlay(){
   if(overlay==null){
    var canvas=GetComponentInParent<Canvas>();if(canvas==null)return;
    var go=new GameObject("AmbientScreenGlitch",typeof(RectTransform),typeof(Canvas),typeof(Image),typeof(LayoutElement));
    go.transform.SetParent(canvas.rootCanvas.transform,false);var rect=(RectTransform)go.transform;
    rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
    var layer=go.GetComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=canvas.rootCanvas.sortingOrder+90;
    go.GetComponent<LayoutElement>().ignoreLayout=true;overlay=go.GetComponent<Image>();overlay.raycastTarget=false;overlay.maskable=false;
   }
   overlay.color=Color.clear;overlay.enabled=false;if(tears[0]!=null)return;
   noise=new Texture2D(256,32,TextureFormat.RGBA32,false){name="Ambient signal noise",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
   for(int i=0;i<tears.Length;i++)tears[i]=Create<RawImage>("SignalTear_"+i);
   for(int i=0;i<noiseBands.Length;i++)noiseBands[i]=Create<RawImage>("StaticNoise_"+i);
   for(int i=0;i<scanLines.Length;i++)scanLines[i]=Create<Image>("ScanLine_"+i);
  }
  private T Create<T>(string name) where T:MaskableGraphic {
   var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(T));go.transform.SetParent(overlay.transform,false);
   var graphic=go.GetComponent<T>();graphic.raycastTarget=false;graphic.maskable=false;graphic.enabled=false;return graphic;
  }
  private float Next(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
  private void Schedule(){float min=Mathf.Max(1,minIntervalSeconds);remaining=Next(min,Mathf.Max(min,maxIntervalSeconds));}
  private static void Band(RectTransform rect,float y,float height){rect.anchorMin=new Vector2(0,y);rect.anchorMax=new Vector2(1,Mathf.Min(1,y+height));rect.offsetMin=rect.offsetMax=Vector2.zero;}
  private IEnumerator CaptureFrame(){
   // Capture the composited frame, including ScreenSpaceOverlay UI.
   yield return new WaitForEndOfFrame();captureRoutine=null;
   if(!effectEnabled||!Application.isFocused){CancelGlitch(true);yield break;}
   Texture2D frame=null;try{frame=ScreenCapture.CaptureScreenshotAsTexture();}catch(System.Exception e){Debug.LogWarning("Ambient screen capture unavailable: "+e.Message);}
   if(frame==null){captureRequested=false;Schedule();yield break;}BeginGlitch(frame);
  }
  private void BeginGlitch(Texture2D frame){ReleaseSnapshot();snapshot=frame;snapshot.wrapMode=TextureWrapMode.Clamp;captureRequested=false;flashing=true;age=0;lastFrame=-1;UpdateGlitch();}
  private void UpdateGlitch(){
   if(snapshot==null||overlay==null)return;int frame=Mathf.FloorToInt(age*(profile==null?24:profile.changesPerSecond));if(frame==lastFrame)return;lastFrame=frame;
   float strength=Mathf.Clamp01(intensity),shift=(profile==null?.14f:profile.horizontalShift)*Mathf.Lerp(.25f,1,strength);
   // Sample shifted strips of the actual screen to tear the picture horizontally.
   foreach(var tear in tears){float y=Next(.01f,.89f),height=Next(.022f,.025f+.085f*strength);Band(tear.rectTransform,y,height);tear.texture=snapshot;tear.uvRect=new Rect(Next(-shift,shift),y,1,height);tear.color=Color.white;tear.enabled=strength>0;}
   for(int i=0;i<noisePixels.Length;i++){byte v=(byte)random.Next(20,235);noisePixels[i]=random.Next(14)==0?new Color32((byte)random.Next(256),(byte)random.Next(256),(byte)random.Next(256),255):new Color32(v,v,v,255);}
   noise.SetPixels32(noisePixels);noise.Apply(false,false);
   foreach(var band in noiseBands){Band(band.rectTransform,Next(.02f,.9f),Next(.018f,.025f+.035f*strength));band.texture=noise;band.uvRect=new Rect(Next(0,1),0,Mathf.Max(1,overlay.rectTransform.rect.width/256),1);band.color=new Color(1,1,1,(profile==null?.75f:profile.noiseOpacity)*Mathf.Lerp(.3f,1,strength));band.enabled=strength>0;}
   for(int i=0;i<scanLines.Length;i++){
    var line=scanLines[i];Band(line.rectTransform,Next(0,.98f),Next(.001f,.004f));
    line.color=i==0?new Color(1,.12f,.16f,.45f*strength):i==1?new Color(.15f,.9f,1,.4f*strength):new Color(i%3==0?1:0,i%3==0?1:0,i%3==0?1:0,.18f+.2f*strength);line.enabled=strength>0;
   }
  }
  private void Hide(){foreach(var tear in tears)if(tear!=null){tear.enabled=false;tear.texture=null;}foreach(var band in noiseBands)if(band!=null)band.enabled=false;foreach(var line in scanLines)if(line!=null)line.enabled=false;if(overlay!=null){overlay.enabled=false;overlay.color=Color.clear;}}
  private static void Dispose(Object item){if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
  private void ReleaseSnapshot(){if(snapshot!=null){Dispose(snapshot);snapshot=null;}}
  private void CancelGlitch(bool schedule){if(captureRoutine!=null){StopCoroutine(captureRoutine);captureRoutine=null;}captureRequested=flashing=false;age=0;Hide();ReleaseSnapshot();if(schedule)Schedule();}
  private void OnEnable(){ApplyProfile();EnsureOverlay();CancelGlitch(true);}
  private void OnDisable(){CancelGlitch(false);}
  private void OnDestroy(){CancelGlitch(false);Dispose(noise);if(overlay!=null)Dispose(overlay.gameObject);}
  private void OnApplicationFocus(bool focused){if(!focused)CancelGlitch(true);}
  private void LateUpdate(){Advance(Time.unscaledDeltaTime,Application.isFocused);if(captureRequested&&captureRoutine==null)captureRoutine=StartCoroutine(CaptureFrame());}
  private void Advance(float deltaTime,bool focused){
   ApplyProfile();
   if(!focused||!effectEnabled){if(flashing||captureRequested)CancelGlitch(true);return;}if(overlay==null||captureRequested)return;
   if(!flashing){remaining-=Mathf.Max(0,deltaTime);if(remaining<=0)captureRequested=true;return;}
   age+=Mathf.Max(0,deltaTime);if(age>=Mathf.Max(.05f,duration)){CancelGlitch(true);return;}UpdateGlitch();
  }
 }
}
