using NUnit.Framework;
using RainbowLi;
using UnityEngine;
public sealed class RuntimeEditModeTests {
 [Test] public void AdvancedRulesAndSaveContinuation(){AdvancedRuntimeChecks.Main();}
 [Test] public void EffectPoolWeightsAndSaveContinuation(){EffectPoolRuntimeChecks.Main();}
 [Test] public void UnityJsonPreservesIdentityAndParameters(){
  var g=new ContainmentGame(14);g.StartBattle();g.AddCard(g.Hand[0]);g.Confirm();g.Step();
  var restored=ContainmentGame.RestoreSave(JsonUtility.FromJson<RunSave>(JsonUtility.ToJson(g.CaptureSave())));
  Assert.AreEqual(g.RandomState,restored.RandomState);Assert.AreEqual(g.Deck[0].InstanceId,restored.Deck[0].InstanceId);
  Assert.AreEqual(g.CaptureSave().Pending.Length,restored.CaptureSave().Pending.Length);
 }
}
