using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 public abstract class CardTableDefinition : ScriptableObject {
  public CardTableRow[] rows=Array.Empty<CardTableRow>();
  public void ValidateTable(bool enemy) {
   if(rows==null||!rows.Any(r=>r!=null&&r.enabled))throw new InvalidOperationException("至少启用一行卡牌。");
   var ids=new HashSet<string>();
   foreach(var r in rows){
    if(r==null)throw new InvalidOperationException("配表存在空行。");
    if(string.IsNullOrWhiteSpace(r.id)||!ids.Add(r.id))throw new InvalidOperationException("卡牌编号为空或重复："+r.id);
    if(string.IsNullOrWhiteSpace(r.name))throw new InvalidOperationException(r.id+"：名称不能为空。");
    if(r.number<1||r.number>6||r.min<1||r.max<r.min||r.max>10000||r.weight<=0||float.IsNaN(r.weight)||float.IsInfinity(r.weight))throw new InvalidOperationException(r.id+"：数字须为 1–6，X 范围须为 1–10000，权重须大于零。");
    if(r.special&&(r.specialWeightRatio<.3f||r.specialWeightRatio>.7f||r.threshold<1||r.condition==ActivationCondition.HealthPercentAtMost&&r.threshold>100))throw new InvalidOperationException(r.id+"：特殊比例须为 0.3–0.7，生命阈值须为 1–100。");
   }
   if(enemy&&!rows.Any(r=>r.enabled&&!r.special))throw new InvalidOperationException("敌人至少需要一种启用的基础卡牌。");
   if(!enemy&&!rows.Any(r=>r.enabled&&r.starting))throw new InvalidOperationException("玩家至少需要一种启用的起始牌效果。");
  }
 }
}
