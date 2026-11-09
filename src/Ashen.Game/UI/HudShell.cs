using Microsoft.Xna.Framework;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 sealed class HudShell
 {
  readonly CovenantGame g;
  public HudShell(CovenantGame game)=>g=game;
  public void Draw()
  {
   var l=g.ui.Layout;
   // Permanent Infinity-style chrome: widescreen growth remains in the center world.
   g.Panel(l.LeftRail);g.Panel(l.RightRail);
   g.Border(l.WorldViewport,Gold*.72f,2);
   g.ui.Navigation.Draw(l.LeftRail);
   g.ui.Portraits.Draw(l.RightRail);
   if(g.ui.Loot.IsOpen)g.ui.Loot.Draw();
   else if(g.ui.Dialogue.IsOpen)g.ui.Dialogue.Draw();
   else if(g.ui.Store.IsOpen)g.ui.Store.Draw();
   else if(g.ui.Services.IsOpen)g.ui.Services.Draw();
   else if(g.ui.Rest.IsOpen)g.ui.Rest.Draw();
   else
   {
    g.Panel(l.BottomShell);g.ui.Messages.Draw(l.MessageRegion);g.ui.Actions.Draw(l.ActionRegion,l.TurnControlRegion);
   }
   g.ui.Initiative.Draw(l.WorldViewport);
  }
 }
}
