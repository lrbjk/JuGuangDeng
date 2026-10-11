using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RainbowLi;
using RainbowLi.UnityGame;
public static class NativeCoreChecks {
 public static void Run(){try {
  var definition=AssetDatabase.LoadAssetAtPath<GameDefinition>("Assets/RainbowLi/Data/DefaultGame.asset");if(definition==null)throw new Exception("Game asset missing");var setup=definition.CreateSetup();
  if(setup.SequencePool.Count!=23||setup.EngravingPool.Count!=19)throw new Exception("Effect pool binding mismatch");
  foreach(var row in definition.sequenceEffects.rows){row.TryRange(out int min,out _);var effect=definition.sequenceEffects.CreateEffect(row.id,min);if(effect.Code!=row.id)throw new Exception("Sequence code mismatch");}
  foreach(var row in definition.engravingEffects.rows){row.TryRange(out int min,out _);var effect=definition.engravingEffects.CreateEffect(row.id,min);if(effect.Code!=row.id)throw new Exception("Engraving code mismatch");}
  var go=new GameObject("Session verification");var session=go.AddComponent<GameSession>();session.definition=definition;session.NewGame();var g=session.Game;g.StartBattle();g.AddCard(g.Hand[0]);g.Confirm();g.Step();
  string json=JsonUtility.ToJson(g.CaptureSave());var restored=ContainmentGame.RestoreSave(JsonUtility.FromJson<RunSave>(json),setup);
  if(g.RandomState!=restored.RandomState||g.Deck[0].InstanceId!=restored.Deck[0].InstanceId)throw new Exception("Native JSON state mismatch");
  for(int i=0;i<20&&g.Phase==GamePhase.Resolving;i++){g.Step();restored.Step();if(g.Health!=restored.Health||g.EnemyHealth!=restored.EnemyHealth||g.RandomState!=restored.RandomState)throw new Exception("Native saved continuation mismatch");}
  session.NewGame();session.Game.Coins=100;session.Game.OpenService(true);var engravingModule=go.AddComponent<EngravingModule>();engravingModule.session=session;engravingModule.definition=AssetDatabase.LoadAssetAtPath<EngravingDefinition>("Assets/RainbowLi/Data/DefaultEngraving.asset");var purchased=session.Game.Deck[0];engravingModule.Engrave(purchased);if(purchased.Engraving==null||purchased.EngravingCode=="LegacyEngraving"||session.Game.Coins!=80)throw new Exception("Default engraving module overrides configured pool");
  var mapRestored=ContainmentGame.RestoreSave(JsonUtility.FromJson<RunSave>(JsonUtility.ToJson(session.Game.CaptureSave())),setup);if(mapRestored.Phase!=GamePhase.Shop)throw new Exception("Fresh map/shop save mismatch");
  AdvancedRuntimeChecks.Main();UnityEngine.Object.DestroyImmediate(go);
  File.WriteAllText("native-result.txt","PASS: native Unity config, 42 effect adapters, JSON and managed rules");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);File.WriteAllText("native-result.txt",e.ToString());EditorApplication.Exit(1);}}
}
