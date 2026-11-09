using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 /// <summary>
 /// Permanent turn-based replacement for the classic Infinity Engine clock/pause control.
 /// It is intentionally not a generic pause button: outside combat it is visibly disabled,
 /// on enemy turns it reports automatic advancement, and on a player turn it ends the
 /// authoritative active actor's turn rather than whichever portrait happens to be inspected.
 /// </summary>
 sealed class TurnControl
 {
  readonly CovenantGame g;
  public TurnControl(CovenantGame game)=>g=game;

  public void Draw(Rectangle region)
  {
   var active=g.world.Active;
   bool combat=g.world.Mode==Mode.Combat;
   bool playerTurn=combat&&active is {Enemy:false,Alive:true};
   bool enemyTurn=combat&&active is {Enemy:true,Alive:true};
   bool confirmation=playerTurn&&g.EndTurnConfirmationArmed(active!);
   bool enabled=playerTurn&&!g.paused;

   string header=combat&&active!=null?$"ROUND {g.world.Round}  •  {ShortName(active.Name).ToUpperInvariant()}":"EXPLORATION";
   string label=confirmation?"CONFIRM END [ENTER]":enemyTurn?"ENEMY TURN":"END TURN [ENTER]";
   string footer;
   string hint;

   if(!combat)
   {
    footer="AVAILABLE IN COMBAT";
    hint="End Turn is disabled during exploration. The classic pause/clock role is reserved for turn-based combat.";
   }
   else if(enemyTurn)
   {
    footer="ADVANCES AUTOMATICALLY";
    hint=$"{active!.Name} is taking its turn. Enemy turns advance automatically; player End Turn input is disabled.";
   }
   else if(active==null||!active.Alive)
   {
    footer="WAITING FOR TURN";
    hint="No living player-controlled combatant currently owns the turn.";
   }
   else if(g.paused)
   {
    footer="RESUME TO CONTINUE";
    hint="Resume the simulation before ending the current turn.";
   }
   else
   {
    footer=g.EndTurnResourceSummary(active);
    hint=confirmation
     ?$"{active.Name} still has a valuable unused turn resource. Press End Turn or Enter again to confirm."
     :$"End {active.Name}'s turn. Unused movement, Action and Bonus Action are discarded. An unused Reaction remains available off-turn until {active.Name}'s next turn.";
   }

   g.Box(region,new Color(15,19,21),confirmation?Red*.88f:playerTurn?Gold*.7f:Gold*.28f);g.CovenantFrame(region,false);
   int inset=Math.Max(4,(int)Math.Round(5*g.ui.Layout.Scale));
   int headerH=Math.Max(15,(int)Math.Round(18*g.ui.Layout.Scale));
   int footerH=Math.Max(13,(int)Math.Round(15*g.ui.Layout.Scale));
   var headerRect=new Rectangle(region.X+inset,region.Y+inset,region.Width-inset*2,headerH);
   var buttonRect=new Rectangle(region.X+inset,headerRect.Bottom+2,region.Width-inset*2,Math.Max(28,region.Height-headerH-footerH-inset*2-6));
   var footerRect=new Rectangle(region.X+inset,buttonRect.Bottom+2,region.Width-inset*2,footerH);

   g.Center(header,headerRect,active is {Enemy:true}?Red:playerTurn?Bright:Muted,.45f);
   g.CommandButton(buttonRect,label,g.RequestEndTurn,enabled,hint,confirmation,.54f);
   g.Center(footer,footerRect,confirmation?Bright:Muted,.38f);
   if(region.Contains(g.cursor))g.tooltip=hint;
  }

  static string ShortName(string name)=>name.Length<=18?name:name[..17]+".";
 }
}
