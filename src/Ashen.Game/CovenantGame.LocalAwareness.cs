using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 void DrawLocalAwarenessHud()
 {
  if(!world.LocalAwarenessEnabled||title||ui.Screens.IsOpen||ui.Loot.IsOpen||ui.Dialogue.IsOpen||InterfaceViewportExpanded)return;
  var noise=world.NoiseHistory.LastOrDefault(n=>n.AreaId==world.CurrentTravelDestinationId);int x=MapRect.Right-260,y=MapRect.Top+(world.Mode==Mode.Combat?100:25);var r=new Rectangle(x,y,245,66);Box(r,new Color(16,28,31)*.95f,Teal*.45f);
  Text(noise?.Kind=="generator"?"GENERATOR STARTUP":noise==null?"LOCAL AWARENESS":"LAST SOUND: "+noise.Kind.ToUpperInvariant(),x+10,y+8,Gold,.42f,true);
  Paragraph(noise==null?"Stay out of sight. Right-click to shout.":$"{noise.RangeFeet} ft sound budget. Doors muffle; walls block.",x+10,y+30,225,Muted,.39f,15);
 }
 // Disposable offscreen fixtures; normal play retains authored positions and starts without noise.
 void StageAwarenessCapture()
 {
  if(scene=="awareness-title"){title=true;return;}
  title=false;foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;var hero=world.PartyLeader;hero.Pos=new(13,16);var enemy=world.Actors.First(a=>a.Enemy);enemy.Pos=new(14,16);
  foreach(var actor in world.Party)actor.Character.SetCondition("hidden","Hidden",true);world.EmitLocalNoise(hero,"shout",60);world.Reveal();camera=new(13,16);SelectCharacter(0);
  if(scene is "awareness-alert" or "awareness-context"){hero.Character.SetCondition("hidden","Hidden",false);world.CheckEncounter();}
  if(scene=="awareness-context"){contextScreen=new(850,280);contextPoint=hero.Position;contextOpen=true;BuildContextCommands();}
 }
}
