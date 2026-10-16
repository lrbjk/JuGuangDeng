using System;
using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 public enum GameAudioCue { [InspectorName("主界面按钮 / 开始执行")] MainMenuButton,[InspectorName("场景 / 楼层切换")] SceneChange,[InspectorName("点击 / 抽牌 / 弃牌")] Click,[InspectorName("普通按钮 / 放牌 / 排序")] Button,[InspectorName("击杀 / 死亡")] Kill,[InspectorName("受击")] Hit,[InspectorName("获得效果 / 完成交互")] GainEffect,[InspectorName("卡牌效果触发")] CardTrigger,[InspectorName("牌序效果触发")] SequenceTrigger }
 [CreateAssetMenu(menuName="RainbowLi/音效配置",fileName="GameAudioProfile")]
 public sealed class GameAudioProfile:ScriptableObject {
  [Serializable] public sealed class Entry {
   public GameAudioCue cue;
   public AudioClip clip;
   [Range(0,1)] public float volume=.8f;
   [Range(.5f,2)] public float pitch=1;
  }
  [Tooltip("按音效对应文档配置9类声音；替换文件或调整音量无需改代码。")]
  public Entry[] entries=Array.Empty<Entry>();
  [Min(0),Tooltip("同类音效的最短间隔，限制大量连锁的叠音。")]
  public float minimumInterval=.025f;
  public Entry Find(GameAudioCue cue)=>entries.FirstOrDefault(e=>e!=null&&e.cue==cue&&e.clip!=null);
 }
}
