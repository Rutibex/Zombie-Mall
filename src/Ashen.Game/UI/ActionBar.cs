using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 sealed class ActionBar
 {
  readonly CovenantGame g;
  readonly ActionBarCatalog catalog;
  readonly List<(Rectangle Bounds,int Slot)> formationHotspots=[];
  readonly List<(Rectangle Bounds,int Slot)> quickWeaponHotspots=[];
  readonly List<(Rectangle Bounds,int Slot)> quickItemHotspots=[];
  readonly List<(Rectangle Bounds,int Slot)> quickSpellHotspots=[];
  public ActionBar(CovenantGame game){g=game;catalog=new(game);}
  public IReadOnlyList<ActionBarCommandModel> CommandsForCurrentSelection()=>catalog.Build();

  public bool HandleRightPress(Point point)
  {
   if(g.ui.Store.IsOpen||g.ui.Services.IsOpen||g.ui.Dialogue.IsOpen||g.ui.Loot.IsOpen||g.ui.Screens.IsOpen)return false;
   foreach(var hit in quickSpellHotspots.AsEnumerable().Reverse())if(hit.Bounds.Contains(point)){g.OpenQuickSpellAssignment(g.Selection.FirstOrDefault()??g.Controller,hit.Slot);return true;}
   foreach(var hit in quickItemHotspots.AsEnumerable().Reverse())if(hit.Bounds.Contains(point)){g.OpenQuickItemConfiguration(g.Selection.FirstOrDefault()??g.Controller,hit.Slot,point);return true;}
   foreach(var hit in quickWeaponHotspots.AsEnumerable().Reverse())if(hit.Bounds.Contains(point)){g.OpenQuickWeaponConfiguration(g.Selection.FirstOrDefault()??g.Controller,hit.Slot,point);return true;}
   foreach(var hit in formationHotspots.AsEnumerable().Reverse())
   {
    if(!hit.Bounds.Contains(point))continue;
    g.OpenFormationQuickSlotConfiguration(hit.Slot,point);return true;
   }
   return false;
  }

  public void Draw(Rectangle actions,Rectangle turnControl)
  {
   // IIP13: the classic clock/pause slot is now a dedicated turn-based control.
   // Keep the turn region layout-owned here so earlier shell/layout contracts remain stable.
   g.ui.TurnControl.Draw(turnControl);

   formationHotspots.Clear();quickWeaponHotspots.Clear();quickItemHotspots.Clear();quickSpellHotspots.Clear();
   var commands=catalog.Build();
   int gap=Math.Max(4,(int)Math.Round(5*g.ui.Layout.Scale));
   int footer=Math.Max(16,(int)Math.Round(18*g.ui.Layout.Scale));
   int top=actions.Y+2,buttonHeight=Math.Max(36,actions.Height-footer-4);
   int count=Math.Max(1,commands.Count),buttonWidth=Math.Max(50,(actions.Width-gap*(count-1))/count);
   int x=actions.X;
   foreach(var command in commands)
   {
    int width=Math.Min(buttonWidth,actions.Right-x);
    var cell=new Rectangle(x,top,Math.Max(1,width),buttonHeight);
    string hover=command.Enabled?command.Hint:$"UNAVAILABLE — {DisabledReason(command)} {command.Hint}";
    if(command.Category==ActionBarCommandCategory.Formation)hover+=" Right-click this formation quick slot to assign another formation.";
    if(command.Id.StartsWith("quick-weapon:",StringComparison.Ordinal))hover+=" Right-click to assign another carried weapon.";
    if(command.Id.StartsWith("quick-item:",StringComparison.Ordinal))hover+=" Right-click to assign another consumable/tool.";
    if(command.Id.StartsWith("quick-spell:",StringComparison.Ordinal))hover+=" Right-click to choose another currently available spell.";
    g.CommandButton(cell,command.Label,command.Run,command.Enabled,hover,command.Active,.50f);
    if(command.Category==ActionBarCommandCategory.Formation&&command.Id.StartsWith("formation-slot:",StringComparison.Ordinal)&&int.TryParse(command.Id[15..],out int formationSlot))formationHotspots.Add((cell,formationSlot));
    if(command.Id.StartsWith("quick-weapon:",StringComparison.Ordinal)&&int.TryParse(command.Id[13..],out int quickWeaponSlot))quickWeaponHotspots.Add((cell,quickWeaponSlot));
    if(command.Id.StartsWith("quick-item:",StringComparison.Ordinal)&&int.TryParse(command.Id[11..],out int quickItemSlot))quickItemHotspots.Add((cell,quickItemSlot));
    if(command.Id.StartsWith("quick-spell:",StringComparison.Ordinal)&&int.TryParse(command.Id[12..],out int quickSpellSlot))quickSpellHotspots.Add((cell,quickSpellSlot));
    if(command.Id.StartsWith("cast-spell:",StringComparison.Ordinal)||command.Id.StartsWith("quick-spell-option:",StringComparison.Ordinal))
    {
     var parts=command.Id.Split(':');var actor=g.Selection.FirstOrDefault();var source=actor?.Character.Spellcasting.FirstOrDefault(s=>parts.Length>=3&&s.SourceId.Equals(parts[1],StringComparison.OrdinalIgnoreCase));
     if(parts.Length>=3&&actor!=null&&source!=null)g.ui.Information.Register(cell,g.SpellInformation(actor,source,parts[2],command.Enabled?"CASTABLE":"UNAVAILABLE"),hover);
     else g.ui.Information.Register(cell,$"action:{command.Id}",command.Label,command.Category.ToString(),hover,command.Hint);
    }
    else if(command.Id.StartsWith("ability:",StringComparison.Ordinal)&&!command.Id.StartsWith("ability:cunning:",StringComparison.Ordinal)&&g.Selection.FirstOrDefault() is {} abilityActor&&abilityActor.Character.Feature(command.Id[8..]) is {} abilityFeature)
     g.ui.Information.Register(cell,g.FeatureInformation(abilityActor,abilityFeature),hover);
    else g.ui.Information.Register(cell,$"action:{command.Id}",command.Label,command.Category.ToString(),hover,command.Hint,
     $"Status: {(command.Enabled?"Ready":"Unavailable")}",command.Active?"Currently selected/armed.":"",$"Command id: {command.Id}");
    x+=buttonWidth+gap;if(x>=actions.Right)break;
   }

   string context=ContextLabel(commands);
   g.Text(context,actions.X,actions.Bottom-footer+2,g.world.Mode==Mode.Combat?Gold:Muted,.43f);
  }

  string DisabledReason(ActionBarCommandModel command)
  {
   if(command.Enabled)return "";
   if(g.paused)return "Resume time first.";
   if(command.Id.StartsWith("quick-item:",StringComparison.Ordinal))return "The assigned item is missing, unusable, or its required Action is unavailable.";
   if(command.Id.StartsWith("quick-spell:",StringComparison.Ordinal))return "The assigned spell is unavailable, unprepared, out of slots, or lacks its required action resource.";
   if(command.Id.StartsWith("quick-weapon:",StringComparison.Ordinal))return "The assigned weapon cannot be readied by the current character right now.";
   if(command.Id.StartsWith("standard:",StringComparison.Ordinal))return "The active player character needs an unused Action.";
   if(command.Id.StartsWith("ability",StringComparison.Ordinal))return "The required action economy, feature resource, target, or trigger is unavailable.";
   if(command.Category is ActionBarCommandCategory.Weapon or ActionBarCommandCategory.ClassAction)return g.world.Mode==Mode.Combat&&g.world.Active!=g.Controller?"It is another combatant's turn.":"The required Action, resource, or legal target state is unavailable.";
   if(command.Category is ActionBarCommandCategory.Group or ActionBarCommandCategory.Formation)return "The current selection or game mode does not allow this group command.";
   return "The current game state does not meet this command's requirements.";
  }

  string ContextLabel(IReadOnlyList<ActionBarCommandModel> commands)
  {
   int selected=g.Selection.Count();
   if(g.FeatureAbilityTargetingActive)return $"ABILITY TARGETING | {(string.IsNullOrWhiteSpace(g.armedFeatureAbilityId)?"ITEM POWER":g.armedFeatureAbilityId.ToUpperInvariant())} | click legal target | right-click / Esc cancels";
   if(g.SpellTargetingActive&&g.ArmedSpell is {} spell&&g.armedSpellPlan is {} plan)return $"CASTING {spell.Name.ToUpperInvariant()} | {(spell.Level==0?"CANTRIP":$"SLOT L{plan.SlotLevel}")} | click target | right-click / Esc cancels";
   if(selected==0)return "No party member selected - select a portrait, drag a box, or press G.";
   if(selected>1)return $"{selected} selected - GROUP COMMANDS - formation: {FormationPatterns.Name(g.groupFormation)}";
   var actor=g.Selection.First();
   if(g.world.Mode==Mode.Combat)
   {
    string turn=g.world.Active==actor&&!actor.Enemy?"ACTIVE":"INSPECT";
    var e=actor.Economy;
    string attacks=e.AttackActionStarted?$" | attacks {e.AttacksRemaining}/{e.AttacksPerAction}":$" | attacks/action {e.AttacksPerAction}";
    return $"{actor.Name} {turn} | Move {e.MovementRemainingFeet:0}/{e.MovementMaximumFeet:0} ft | Action {(e.ActionAvailable?"READY":"SPENT")} | Bonus {(e.BonusActionAvailable?"READY":"SPENT")} | Reaction {(e.ReactionAvailable?"READY":"SPENT")}{attacks}";
   }
   return $"{actor.Name} - {actor.Character.PrimaryClassName} - HP {actor.Hp}/{actor.MaxHp} - {commands.Count} contextual commands";
  }
 }
}
