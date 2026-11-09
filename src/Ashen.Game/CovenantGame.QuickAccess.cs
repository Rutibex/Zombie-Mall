using Ashen.Core;
using Microsoft.Xna.Framework;

namespace Ashen.Game;

public sealed partial class CovenantGame
{
 int quickSpellAssignmentSlot=-1;

 void OpenQuickWeaponConfiguration(Actor actor,int slot,Point anchor)
 {
  if(slot<0||slot>=4)return;actor.QuickAccess.Normalize(actor);contextHeadingOverride=$"QUICK WEAPON {slot+1}";contextActor=null;contextFeature=null;contextScreen=anchor;contextCommands.Clear();
  contextCommands.Add(("CLEAR SLOT",()=>{actor.QuickAccess.WeaponInstanceIds[slot]="";world.LogSystem($"{actor.Name} clears quick weapon {slot+1}.");},!string.IsNullOrWhiteSpace(actor.QuickAccess.WeaponInstanceIds[slot]),"Remove this shortcut without moving or unequipping the weapon."));
  foreach(var stack in actor.Inventory.Items.Concat(actor.Equipment.All()).Where(x=>x.Definition.Kind==ItemKind.Weapon&&x.Definition.CanEquipIn(EquipmentSlot.MainHand)).GroupBy(x=>x.InstanceId).Select(g=>g.First()))
  {
   var captured=stack;string marker=actor.QuickAccess.WeaponInstanceIds[slot]==captured.InstanceId?"* ":"";string equipped=actor.Equipment.MainHand?.InstanceId==captured.InstanceId?" [READY]":"";
   contextCommands.Add(($"{marker}{captured.Definition.Name}{equipped}",()=>world.AssignQuickWeapon(actor,captured.InstanceId,slot),true,$"Assign this exact {captured.Definition.Name} instance to quick weapon {slot+1}. Left-clicking the slot later switches to it immediately."));
  }
  OpenQuickContext(anchor);
 }

 void OpenQuickItemConfiguration(Actor actor,int slot,Point anchor)
 {
  if(slot<0||slot>=3)return;actor.QuickAccess.Normalize(actor);contextHeadingOverride=$"QUICK ITEM {slot+1}";contextActor=null;contextFeature=null;contextScreen=anchor;contextCommands.Clear();
  contextCommands.Add(("CLEAR SLOT",()=>{actor.QuickAccess.ItemInstanceIds[slot]="";world.LogSystem($"{actor.Name} clears quick item {slot+1}.");},!string.IsNullOrWhiteSpace(actor.QuickAccess.ItemInstanceIds[slot]),"Remove this shortcut without moving the item."));
  foreach(var stack in actor.Inventory.Items.Concat(actor.Equipment.All()).Where(x=>x.Definition.Kind is ItemKind.Consumable or ItemKind.Tool).GroupBy(x=>x.InstanceId).Select(g=>g.First()))
  {
   var captured=stack;string marker=actor.QuickAccess.ItemInstanceIds[slot]==captured.InstanceId?"* ":"";string resource=captured.Quantity>1?$" x{captured.Quantity}":captured.Definition.MaxCharges>0?$" ({captured.Charges}c)":"";
   contextCommands.Add(($"{marker}{captured.Definition.Name}{resource}",()=>world.AssignQuickItem(actor,captured.InstanceId,slot),true,$"Assign this exact item instance to quick item {slot+1}. Quantity/charges shown on the action bar update live."));
  }
  OpenQuickContext(anchor);
 }

 void OpenQuickContext(Point anchor)
 {
  int height=39+contextCommands.Count*33;contextRect=new(Math.Clamp(anchor.X,MapRect.Left,MapRect.Right-238),Math.Clamp(anchor.Y,MapRect.Top,MapRect.Bottom-height),238,height);contextOpen=true;leftWorldPress=boxDragging=false;
 }

 public void OpenQuickSpellAssignment(Actor actor,int slot)
 {
  if(slot<0||slot>=3||actor.Character.Spellcasting.Count==0){Notify("This character has no spells available for a quick spell slot.");return;}
  quickSpellAssignmentSlot=slot;ResetSpellActionUi(actor);ClearArmedSpell();ClearArmedFeatureAbility();pendingSpellPlan=null;actionBarPage=ActionBarPage.QuickSpellAssign;action=TargetIntent.Attack;contextOpen=false;ResetFormationContext();
 }

 IReadOnlyList<ActionBarCommandModel> BuildQuickSpellAssignmentCommands(Actor actor)
 {
  var source=ActiveSpellActionSource(actor);var result=new List<ActionBarCommandModel>
  {
   new("quick-spell-assign-back","< BACK","Return without changing the quick spell slot.",ActionBarCommandCategory.Utility,true,false,()=>{quickSpellAssignmentSlot=-1;actionBarPage=ActionBarPage.Primary;}),
   new("quick-spell-clear","CLEAR",quickSpellAssignmentSlot<0?"No quick slot is being configured.":$"Clear quick spell {quickSpellAssignmentSlot+1}.",ActionBarCommandCategory.Utility,quickSpellAssignmentSlot>=0&&world.QuickSpell(actor,quickSpellAssignmentSlot)!=null,false,()=>{if(quickSpellAssignmentSlot>=0){actor.QuickAccess.SpellBindings[quickSpellAssignmentSlot].Clear();world.LogSystem($"{actor.Name} clears quick spell {quickSpellAssignmentSlot+1}.");}quickSpellAssignmentSlot=-1;actionBarPage=ActionBarPage.Primary;})
  };
  if(source==null||quickSpellAssignmentSlot<0)return result;
  result.Add(new("quick-spell-source",$"SRC {source.SourceId}",actor.Character.Spellcasting.Count>1?"Cycle spellcasting source for this quick-slot assignment.":"This character has one spellcasting source.",ActionBarCommandCategory.Spell,actor.Character.Spellcasting.Count>1,false,()=>CycleSpellActionSource(actor)));
  string level=spellActionLevelFilter<0?"ALL":spellActionLevelFilter==0?"CANTRIP":$"L{spellActionLevelFilter}";result.Add(new("quick-spell-level",$"LEVEL {level}","Cycle assignment filter through available spell levels.",ActionBarCommandCategory.Spell,true,false,()=>CycleSpellActionLevel(actor,source)));
  var spells=AvailableActionSpells(actor,source);const int pageSize=5;int pages=Math.Max(1,(spells.Count+pageSize-1)/pageSize);spellActionPageIndex=Math.Clamp(spellActionPageIndex,0,pages-1);
  if(pages>1)result.Add(new("quick-spell-prev","< SPELLS",$"Previous quick-spell assignment page ({spellActionPageIndex+1}/{pages}).",ActionBarCommandCategory.Utility,spellActionPageIndex>0,false,()=>spellActionPageIndex--));
  foreach(var spell in spells.Skip(spellActionPageIndex*pageSize).Take(pageSize))
  {
   var captured=spell;int slot=quickSpellAssignmentSlot;var existing=world.QuickSpell(actor,slot);bool assigned=existing!=null&&existing.SourceId.Equals(source.SourceId,StringComparison.OrdinalIgnoreCase)&&existing.SpellId.Equals(captured.Id,StringComparison.OrdinalIgnoreCase);
   string prefix=captured.Level==0?"C":$"L{captured.Level}";string hint=$"Assign {captured.Name} ({source.SourceId}) to quick spell {slot+1}. Right-click this spell entry for its complete IIP30 rules description.";
   result.Add(new($"quick-spell-option:{source.SourceId}:{captured.Id}",$"{(assigned?"* ":"")}{prefix} {CompactSpellName(captured.Name)}",hint,ActionBarCommandCategory.Spell,true,assigned,()=>{if(world.AssignQuickSpell(actor,source.SourceId,captured.Id,slot)){quickSpellAssignmentSlot=-1;actionBarPage=ActionBarPage.Primary;}}));
  }
  if(pages>1)result.Add(new("quick-spell-next","SPELLS >",$"Next quick-spell assignment page ({spellActionPageIndex+1}/{pages}).",ActionBarCommandCategory.Utility,spellActionPageIndex<pages-1,false,()=>spellActionPageIndex++));
  return result;
 }

 public void ActivateQuickSpell(Actor actor,int slot)
 {
  var binding=world.QuickSpell(actor,slot);if(binding==null){Notify($"Quick spell {slot+1} is empty. Right-click it to assign a spell.");return;}
  var source=actor.Character.Spellcasting.FirstOrDefault(s=>s.SourceId.Equals(binding.SourceId,StringComparison.OrdinalIgnoreCase));if(source==null||!SpellCatalog.TryGet(binding.SpellId,out var spell)){actor.QuickAccess.SpellBindings[slot].Clear();Notify("That quick spell is no longer available.");return;}
  var availability=SpellActionAvailability(actor,source,spell);if(!availability.Enabled){Notify(availability.Reason);return;}ChooseSpellForCast(actor,source,spell);
 }
}
