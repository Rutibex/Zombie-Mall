using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 void DrawSurvivalHud()
 {
  if(!world.DailySurvivalEnabled||title||ui.Screens.IsOpen||ui.Loot.IsOpen||ui.Dialogue.IsOpen||ui.Rest.IsOpen||InterfaceViewportExpanded)return;
  var a=PrimaryPartyActor;int x=MapRect.Right-300,y=MapRect.Top+(world.Mode==Mode.Combat?176:101);var r=new Rectangle(x,y,285,66);
  Box(r,new Color(27,30,19)*.95f,Gold*.45f);Text("DAILY NEEDS - "+a.Name.ToUpperInvariant(),x+10,y+8,Gold,.39f,true);
  Paragraph(world.SurvivalSummary(a)+". Open Rest for supplies and first aid.",x+10,y+30,265,a.Needs.Exhaustion>0?Red:Muted,.35f,14);
 }
 void StageSurvivalCapture()
 {
  title=false;if(scene=="survival-title"){title=true;return;}
  if(scene!="survival-playthrough")
  {
   foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;
   world.SetDialogueFlag("flag.mall.shelter");int i=0;foreach(var a in world.Party){a.Pos=new(12+i++,27);if(scene=="survival-shortage"){a.Needs.Exhaustion=2;a.Needs.DaysWithoutWater=2;a.Hp=Math.Max(1,a.MaxHp-8);}}
   world.Reveal();
  }
  camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectCharacter(0);ui.Rest.Open();
 }
}
