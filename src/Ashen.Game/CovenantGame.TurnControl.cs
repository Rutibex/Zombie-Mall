using Ashen.Core;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 /// <summary>True only while the permanent IIP13 control may advance a player-owned turn.</summary>
 bool CanEndTurnFromControl=>world.Mode==Mode.Combat&&world.Active is {Enemy:false,Alive:true}&&!paused;

 bool EndTurnConfirmationArmed(Actor actor)=>
  endTurnConfirmActorId==actor.Id&&elapsed<=endTurnConfirmUntil;

 void ClearEndTurnConfirmation()
 {
  endTurnConfirmActorId="";endTurnConfirmUntil=0;
 }

 /// <summary>
 /// Short color-independent status line for the permanent End Turn region. It reports
 /// the resources that would be left unused without pretending every character has a
 /// legal generic Bonus Action.
 /// </summary>
 string EndTurnResourceSummary(Actor actor)
 {
  var e=actor.Economy;var bits=new List<string>();
  if(e.AttackActionStarted&&e.AttacksRemaining>0)bits.Add($"{e.AttacksRemaining} ATTACK{(e.AttacksRemaining==1?"":"S")} LEFT");
  else if(e.ActionAvailable)bits.Add("ACTION READY");
  if(e.MovementRemainingFeet>=.5f)bits.Add($"{e.MovementRemainingFeet:0} FT MOVE");
  if(e.BonusActionAvailable)bits.Add("BONUS READY");
  return bits.Count==0?"TURN RESOURCES SPENT":string.Join("  •  ",bits.Take(2));
 }

 string EndTurnCriticalWarning(Actor actor)
 {
  var e=actor.Economy;
  if(e.AttackActionStarted&&e.AttacksRemaining>0)return $"{e.AttacksRemaining} granted attack{(e.AttacksRemaining==1?"":"s")}";
  if(e.ActionAvailable)return "an unused Action";
  return "";
 }

 /// <summary>
 /// Single authority for mouse and Enter End Turn requests. Pending targeting/movement
 /// is cancelled on the first request rather than silently wasting it. A still-unused
 /// Action or granted Extra Attack requires an explicit second request within 2.5 s.
 /// </summary>
 void RequestEndTurn()
 {
  if(world.Mode!=Mode.Combat){ClearEndTurnConfirmation();Notify("End Turn is available only during combat.");return;}
  if(world.Active is not { } active){ClearEndTurnConfirmation();Notify("No combatant currently owns the turn.");return;}
  if(!active.Alive){ClearEndTurnConfirmation();world.EndTurn();return;}
  if(active.Enemy){ClearEndTurnConfirmation();Notify($"{active.Name}'s enemy turn advances automatically.");return;}
  if(paused){ClearEndTurnConfirmation();Notify("Resume the simulation before ending the turn.");return;}

  if(ui.Portraits.IsDragging)
  {
   ui.Portraits.CancelInteraction();ClearEndTurnConfirmation();Notify("Portrait drag cancelled. Press End Turn again to finish the turn.");return;
  }
  if(world.IsMoving(active))
  {
   world.Stop(active);ClearArmedSpell();ClearArmedFeatureAbility();pendingSpellPlan=null;action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;ClearEndTurnConfirmation();
   Notify($"{active.Name}'s movement/approach was cancelled. Press End Turn again to finish the turn.");return;
  }
  if(action!=TargetIntent.Attack||actionBarPage!=ActionBarPage.Primary)
  {
   ClearArmedSpell();ClearArmedFeatureAbility();pendingSpellPlan=null;action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;ClearEndTurnConfirmation();
   Notify("Pending targeting/action menu cancelled. Press End Turn again to finish the turn.");return;
  }

  string warning=EndTurnCriticalWarning(active);
  if(preferences.ConfirmEndTurn&&warning.Length>0&&!EndTurnConfirmationArmed(active))
  {
   endTurnConfirmActorId=active.Id;endTurnConfirmUntil=elapsed+2.5f;
   Notify($"{active.Name} still has {warning}. Press End Turn [{BindingLabel(InputAction.EndTurn,false)}] again to confirm.");return;
  }

  var e=active.Economy;
  string unused=$"Move {e.MovementRemainingFeet:0} ft; Action {(e.ActionAvailable?"unused":"spent")}; Bonus Action {(e.BonusActionAvailable?"unused":"spent")}; Reaction {(e.ReactionAvailable?"available off-turn":"spent")}.";
  world.Message(MessageCategory.Combat,$"{active.Name} ends the turn.",$"{active.Name} ends the turn. {unused}");
  ClearEndTurnConfirmation();contextOpen=false;ClearArmedSpell();ClearArmedFeatureAbility();pendingSpellPlan=null;action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;
  world.EndTurn();SyncCombatSelection();audio?.PlayUi("turn");
 }
}
