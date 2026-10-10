using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 public abstract class CardTableDefinition : ScriptableObject {
  public CardTableRow[] rows=Array.Empty<CardTableRow>();
  public CardEffectSpec[] GetEffects(string id){var row=rows.FirstOrDefault(r=>r!=null&&r.id==id);if(row==null)throw new ArgumentException("没有卡牌编号："+id);return row.EffectSpecs();}
  public void ValidateTable(bool enemy) {
   var issues=CardTableValidation.Check(rows,enemy);if(issues.Length>0)throw new InvalidOperationException(string.Join("\n",issues.Select(e=>e.ToString())));
  }
 }
}
