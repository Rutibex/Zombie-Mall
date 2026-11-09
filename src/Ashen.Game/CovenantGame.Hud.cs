using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 void CommandButton(Rectangle r,string label,System.Action run,bool enabled,string help,bool active=false,float scale=.7f)
 {
  if(enabled)Button(r,label,run,active,scale);
  else{Box(r,new Color(23,27,29),Gold*.25f);CovenantButtonFrame(r,false,false,false);Center(label,new(r.X,r.Center.Y-10,r.Width,24),Muted*.55f,scale);buttons.Add((r,()=>Notify(help)));}
  if(r.Contains(cursor))ui.Tooltips.Offer("command:"+label,help);
 }
 void DrawHud()=>ui.Hud.Draw();
}
