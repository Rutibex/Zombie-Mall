using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActionResourceKind { Action, BonusAction, Reaction }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StandardActionKind { Dash, Disengage, Dodge, Help, Hide, Ready, Search, UseObject }

/// <summary>
/// Per-turn 5e action economy. Movement is stored in feet so the rules layer and
/// UI speak the same units as the character sheet. The world adapter converts
/// five feet to one continuous-map tile when pathfinding/moving.
/// </summary>
public sealed class ActionEconomyState
{
 public float MovementMaximumFeet {get;set;}
 public float MovementRemainingFeet {get;set;}
 public bool ActionAvailable {get;set;}=true;
 public bool BonusActionAvailable {get;set;}=true;
 public bool ReactionAvailable {get;set;}=true;
 public bool AttackActionStarted {get;set;}
 public int AttacksPerAction {get;set;}=1;
 public int AttacksRemaining {get;set;}
 public bool Dashed {get;set;}

 public void Reset(int speedFeet,int attacksPerAction)
 {
  MovementMaximumFeet=Math.Max(0,speedFeet);
  MovementRemainingFeet=MovementMaximumFeet;
  ActionAvailable=true;BonusActionAvailable=true;ReactionAvailable=true;
  AttackActionStarted=false;AttacksPerAction=Math.Max(1,attacksPerAction);AttacksRemaining=0;Dashed=false;
 }
 public bool Available(ActionResourceKind kind)=>kind switch
 {
  ActionResourceKind.Action=>ActionAvailable,
  ActionResourceKind.BonusAction=>BonusActionAvailable,
  _=>ReactionAvailable
 };
 public bool Spend(ActionResourceKind kind)
 {
  if(!Available(kind))return false;
  switch(kind)
  {
   case ActionResourceKind.Action:ActionAvailable=false;break;
   case ActionResourceKind.BonusAction:BonusActionAvailable=false;break;
   case ActionResourceKind.Reaction:ReactionAvailable=false;break;
  }
  return true;
 }
 public bool CanAttack=>AttackActionStarted?AttacksRemaining>0:ActionAvailable;
 public bool SpendAttack()
 {
  if(!AttackActionStarted)
  {
   if(!Spend(ActionResourceKind.Action))return false;
   AttackActionStarted=true;AttacksRemaining=Math.Max(1,AttacksPerAction);
  }
  if(AttacksRemaining<=0)return false;
  AttacksRemaining--;return true;
 }
 public void AddMovement(float feet)
 {
  if(!float.IsFinite(feet)||feet<=0)return;
  MovementRemainingFeet+=feet;
  MovementMaximumFeet+=feet;
 }
 public void SpendMovement(float feet)
 {
  if(!float.IsFinite(feet)||feet<=0)return;
  MovementRemainingFeet=Math.Max(0,MovementRemainingFeet-feet);
 }
 public void FinishTurn()
 {
  // Movement, Action, Bonus Action, and any remaining attacks are turn-scoped.
  // Reaction deliberately survives End Turn so IIP15 can spend it off-turn.
  MovementRemainingFeet=0;ActionAvailable=false;BonusActionAvailable=false;
  AttacksRemaining=0;AttackActionStarted=false;Dashed=false;
 }
}

public sealed record StandardActionDefinition(StandardActionKind Kind,string Name,string ShortLabel,string Description,ActionResourceKind Cost,bool NeedsTarget=false);

public static class StandardActions
{
 public static readonly StandardActionDefinition[] All=
 [
  new(StandardActionKind.Dash,"Dash","DASH","Spend your Action to gain additional movement equal to your speed for this turn.",ActionResourceKind.Action),
  new(StandardActionKind.Disengage,"Disengage","DISENGAGE","Spend your Action to move without provoking opportunity attacks for the rest of this turn.",ActionResourceKind.Action),
  new(StandardActionKind.Dodge,"Dodge","DODGE","Spend your Action to impose disadvantage on attacks against you until the start of your next turn.",ActionResourceKind.Action),
  new(StandardActionKind.Help,"Help","HELP","Spend your Action to aid a nearby ally; their next attack is made with advantage.",ActionResourceKind.Action,true),
  new(StandardActionKind.Hide,"Hide","HIDE","Spend your Action to make a Dexterity (Stealth) check against nearby observers.",ActionResourceKind.Action),
  new(StandardActionKind.Ready,"Ready","READY","Spend your Action to ready one basic weapon attack. The first hostile that enters or begins a turn in weapon range opens a Reaction interrupt.",ActionResourceKind.Action),
  new(StandardActionKind.Search,"Search","SEARCH","Spend your Action to make a Wisdom (Perception) check of the nearby area.",ActionResourceKind.Action),
  new(StandardActionKind.UseObject,"Use Object","USE OBJECT","Spend your Action to manipulate an adjacent combat object such as a door.",ActionResourceKind.Action,true)
 ];
 public static StandardActionDefinition Get(StandardActionKind kind)=>All.First(x=>x.Kind==kind);
}
