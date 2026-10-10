namespace RainbowLi.UnityGame {
// One shared presentation clock; UI, input and the game simulation retain their own clocks.
public sealed class BattlePlaybackClock {
 private int frame=-1;private float time;
 public float Tick(int currentFrame,float delta,bool advance,float speed){if(frame!=currentFrame){frame=currentFrame;if(advance)time+=UnityEngine.Mathf.Max(0,delta)*speed;}return time;}
 public void Reset(){time=0;frame=-1;}
}
}
