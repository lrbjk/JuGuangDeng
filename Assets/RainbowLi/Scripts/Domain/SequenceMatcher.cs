using System.Collections.Generic;
namespace RainbowLi {
 public static class SequenceMatcher {
  // Always includes the newly appended entry; bridge may skip following positions.
  public static List<NumberEntry> Match(IReadOnlyList<NumberEntry> stream,int[] pattern){
   if(pattern==null||pattern.Length!=3||stream.Count<3)return null;
   int end=stream.Count-1;if(!stream[end].Accepts(pattern[2]))return null;
   for(int middle=end-1;middle>=1;middle--){
    if(!stream[middle].Accepts(pattern[1]))continue;
    bool bridge=stream[middle].Action?.Card?.Engraving?.Effect==EngravingEffect.Bridge;
    for(int first=middle-1;first>=0;first--){
     bool headBridge=stream[first].Action?.Card?.Engraving?.Effect==EngravingEffect.Bridge;
     if(first!=middle-1&&!headBridge)continue;
     if(middle!=end-1&&!bridge&&!headBridge)continue;
     if(stream[first].Accepts(pattern[0]))return new List<NumberEntry>{stream[first],stream[middle],stream[end]};
    }
   }
   return null;
  }
 }
}
