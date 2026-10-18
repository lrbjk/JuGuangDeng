using System;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/背景音乐配置",fileName="GameMusicProfile")]
 public sealed class GameMusicProfile:ScriptableObject {
  [Serializable] public sealed class EnemyMusic {
   [Tooltip("敌人ID前缀；也可填写完整ID，为特定敌人配曲。更长的匹配优先。")] public string idPrefix;
   public AudioClip normal,boss;
  }
  [Header("主菜单与非战斗房间")] public AudioClip map;
  [Header("按敌人ID匹配战斗音乐")] public EnemyMusic[] enemies=Array.Empty<EnemyMusic>();
  [Range(0,1),Tooltip("背景音乐相对音量，另乘设置中的总音量。")] public float volume=.65f;
  [Min(0),Tooltip("换曲的交叉淡入淡出秒数；0表示直接切换。")] public float fadeSeconds=.6f;
  public AudioClip Resolve(string enemyId,bool isBoss){
   EnemyMusic selected=null;int length=-1;
   foreach(var entry in enemies??Array.Empty<EnemyMusic>())if(entry!=null&&!string.IsNullOrEmpty(entry.idPrefix)&&(enemyId??"").StartsWith(entry.idPrefix,StringComparison.Ordinal)&&entry.idPrefix.Length>length){selected=entry;length=entry.idPrefix.Length;}
   return (isBoss?selected?.boss:selected?.normal)??map;
  }
 }
}
