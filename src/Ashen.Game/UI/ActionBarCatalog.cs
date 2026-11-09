using Ashen.Core;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum ActionBarCommandCategory { Talk, Weapon, ClassAction, Spell, Item, Ability, StandardAction, Group, Formation, Utility }
 sealed record ActionBarCommandModel(
  string Id,string Label,string Hint,ActionBarCommandCategory Category,bool Enabled,bool Active,System.Action Run);

 sealed class ActionBarCatalog
 {
  readonly CovenantGame g;
  public ActionBarCatalog(CovenantGame game)=>g=game;

  public IReadOnlyList<ActionBarCommandModel> Build()
  {
   var selected=g.Selection.ToArray();
   if(selected.Length==0)return BuildEmpty();
   if(selected.Length>1)return BuildGroup(selected);
   return BuildSingle(selected[0]);
  }

  IReadOnlyList<ActionBarCommandModel> BuildEmpty()=>
  [
   new("select-all","SELECT ALL [G]","Select every living controllable party member.",ActionBarCommandCategory.Utility,g.world.Party.Any(a=>a.Alive),false,g.SelectAll),
   new("select-leader","SELECT LEADER","Select portrait slot 1, the nominal party leader.",ActionBarCommandCategory.Utility,g.world.PartyLeader.Alive,g.world.Chosen==g.world.PartyLeader,()=>g.SelectCharacter(g.world.PartyIndex(g.world.PartyLeader)))
  ];

  IReadOnlyList<ActionBarCommandModel> BuildSingle(Actor actor)
  {
   bool explore=g.world.Mode==Mode.Explore;
   bool combatTurn=g.world.Mode==Mode.Combat&&g.world.Active==actor&&!actor.Enemy;
   if(g.actionBarPage==ActionBarPage.Spells)return g.BuildSpellActionCommands(actor);
   if(g.actionBarPage==ActionBarPage.SpellSlots)return g.BuildSpellSlotCommands(actor);
   if(g.actionBarPage==ActionBarPage.SpellConcentration)return g.BuildSpellConcentrationCommands(actor);
   if(g.actionBarPage==ActionBarPage.QuickSpellAssign)return g.BuildQuickSpellAssignmentCommands(actor);
   if(combatTurn&&g.actionBarPage==ActionBarPage.StandardActions)return BuildStandardActions(actor);
   if(g.actionBarPage==ActionBarPage.Abilities)return g.BuildAbilityActionCommands(actor);

   var result=new List<ActionBarCommandModel>();
   bool canTarget=actor.Alive&&!g.paused&&(explore||combatTurn);
   bool canUseAction=actor.Alive&&!g.paused&&(explore||g.world.CanAct(actor));
   bool canAttack=actor.Alive&&!g.paused&&(explore||g.world.CanAttack(actor));

   result.Add(new("talk","TALK","Arm dialogue targeting. Dialogue NPCs use the talk cursor; clicking one approaches them and expands the lower message region into conversation mode.",ActionBarCommandCategory.Talk,explore&&!g.paused,g.action==TargetIntent.Talk,()=>{g.action=TargetIntent.Talk;g.actionBarPage=ActionBarPage.Primary;g.Notify("Talk targeting armed. Click a dialogue target.");}));

   // IIP33: four persistent quick weapon slots are first-class action-bar controls.
   // Activating one immediately equips/readies that exact item instance, then arms Attack.
   for(int slot=0;slot<4;slot++)
   {
    int captured=slot;var stack=g.world.QuickWeapon(actor,slot);bool assigned=stack!=null;string why="";bool ready=assigned&&g.world.CanActivateQuickWeapon(actor,slot,out why);
    string name=assigned?CompactQuick(stack!.Definition.Name):"—";bool active=assigned&&actor.Equipment.MainHand?.InstanceId==stack!.InstanceId;
    string hint=assigned?$"Quick weapon {slot+1}: {stack!.Definition.Name}. Left-click equips/readies this weapon and arms Attack. {(ready?"Ready.":why)}":$"Quick weapon {slot+1} is empty. Right-click to assign a carried weapon.";
    result.Add(new($"quick-weapon:{slot}",$"W{slot+1} {name}",hint,ActionBarCommandCategory.Weapon,assigned&&ready,active,()=>{if(g.world.ActivateQuickWeapon(actor,captured)){g.action=TargetIntent.Attack;g.actionBarPage=ActionBarPage.Primary;}}));
   }

   // IIP34: class/subclass/species/feat/item powers no longer spill directly across the primary bar.
   // One expandable ABILITIES control owns active features; passive traits remain on the full Abilities/Record pages.


   if(actor.Character.Spellcasting.Count>0)
   {
    int current=actor.Character.Spellcasting.SelectMany(s=>s.Slots).Sum(s=>s.Current),maximum=actor.Character.Spellcasting.SelectMany(s=>s.Slots).Sum(s=>s.Maximum);
    string label=maximum>0?$"CAST {current}/{maximum}":"CAST SPELL";
    result.Add(new("spells",label,"Open the IIP32 battlefield spell strip. Available prepared/known spells are filtered by source, level, casting time, slots, and the current Action/Bonus Action state. Right-click a spell in the strip for full information.",ActionBarCommandCategory.Spell,canTarget,false,()=>g.OpenSpellActionBar(actor)));
   }

   // IIP33: quick spells invoke the same IIP32 plan/slot/targeting pipeline; they never
   // create an alternate spell-casting authority.
   for(int slot=0;slot<3;slot++)
   {
    int captured=slot;var binding=g.world.QuickSpell(actor,slot);SpellDefinition? spell=null;SpellcastingSource? source=null;bool ready=false;string why="";
    if(binding!=null&&SpellCatalog.TryGet(binding.SpellId,out spell))source=actor.Character.Spellcasting.FirstOrDefault(x=>x.SourceId.Equals(binding.SourceId,StringComparison.OrdinalIgnoreCase));
    if(spell!=null&&source!=null){var availability=g.SpellActionAvailability(actor,source,spell);ready=availability.Enabled;why=availability.Reason;}
    string name=spell==null?"—":CompactQuick(spell.Name);string hint=spell==null?$"Quick spell {slot+1} is empty. Right-click to assign a currently available spell.":$"Quick spell {slot+1}: {spell.Name} ({source?.SourceId}). Left-click begins the normal IIP32 cast workflow, including upcast choice, concentration warning and targeting. {(ready?"Ready.":why)}";
    result.Add(new($"quick-spell:{slot}",$"S{slot+1} {name}",hint,ActionBarCommandCategory.Spell,spell!=null&&source!=null&&ready,false,()=>g.ActivateQuickSpell(actor,captured)));
   }

   // Three quick item slots follow live instance quantities/charges. Consuming the last
   // object clears only that binding through QuickAccess.Normalize.
   for(int slot=0;slot<3;slot++)
   {
    int captured=slot;var stack=g.world.QuickItem(actor,slot);bool assigned=stack!=null;string why="";bool usable=assigned&&g.world.CanUseOwnedItem(actor,stack!.InstanceId,out why);
    string resource=stack==null?"":stack.Quantity>1?$" x{stack.Quantity}":stack.Definition.MaxCharges>0?$" {stack.Charges}c":"";string name=stack==null?"—":CompactQuick(stack.Definition.Name);
    string hint=stack==null?$"Quick item {slot+1} is empty. Right-click to assign a consumable or usable tool.":$"Quick item {slot+1}: {stack.Definition.Name}{resource}. Left-click uses this exact carried item instance. {(usable?"Ready.":why)}";
    result.Add(new($"quick-item:{slot}",$"I{slot+1} {name}{resource}",hint,ActionBarCommandCategory.Item,assigned&&usable,false,()=>{if(stack!=null)g.world.UseOwnedItem(actor,stack.InstanceId);}));
   }

   if(g.world.Mode==Mode.Combat)
    result.Add(new("standard-actions","ACTIONS","Open the 5e standard action page: Dash, Disengage, Dodge, Help, Hide, Ready, Search and Use Object.",ActionBarCommandCategory.StandardAction,combatTurn&&!g.paused,false,()=>g.actionBarPage=ActionBarPage.StandardActions));

   result.Add(new("abilities","ABILITIES [S/K]","Open the IIP34 active class-feature panel. S opens the same panel from the keyboard; K opens the full Abilities reference screen.",ActionBarCommandCategory.Ability,true,g.actionBarPage==ActionBarPage.Abilities,()=>g.OpenAbilityActionBar(actor)));
   return result;
  }

  IReadOnlyList<ActionBarCommandModel> BuildStandardActions(Actor actor)
  {
   var result=new List<ActionBarCommandModel>
   {
    new("actions-back","< BACK","Return to the character's primary contextual actions.",ActionBarCommandCategory.Utility,true,false,()=>{g.actionBarPage=ActionBarPage.Primary;g.action=TargetIntent.Attack;})
   };
   foreach(var definition in StandardActions.All)
   {
    var captured=definition;
    bool enabled=g.world.CanAct(actor)&&!g.paused;
    bool active=captured.Kind switch
    {
     StandardActionKind.Help=>g.action==TargetIntent.Help,
     StandardActionKind.UseObject=>g.action==TargetIntent.UseObject,
     _=>false
    };
    System.Action run=captured.Kind switch
    {
     StandardActionKind.Help=>()=>g.action=TargetIntent.Help,
     StandardActionKind.UseObject=>()=>g.action=TargetIntent.UseObject,
     _=>()=>{if(g.world.ExecuteStandardAction(actor,captured.Kind)){g.actionBarPage=ActionBarPage.Primary;g.action=TargetIntent.Attack;}}
    };
    string hint=$"{captured.Name} [Action]: {captured.Description}";
    result.Add(new($"standard:{captured.Kind.ToString().ToLowerInvariant()}",captured.ShortLabel,hint,ActionBarCommandCategory.StandardAction,enabled,active,run));
   }
   return result;
  }

  IReadOnlyList<ActionBarCommandModel> BuildGroup(Actor[] selected)
  {
   var result=new List<ActionBarCommandModel>();
   bool exploration=g.world.Mode==Mode.Explore;
   bool canOrder=!g.paused&&(exploration||g.world.Active is {Enemy:false} active&&selected.Contains(active));
   var speaker=g.world.FormationLeader(selected.Select(a=>a.Id));

   result.Add(new("group-talk","TALK",$"Arm dialogue targeting for the selected group. The highest selected companion in portrait order leads the approach{(speaker==null?".":$": {speaker.Name}.")} Conversation itself defaults to the party leader.",ActionBarCommandCategory.Group,exploration&&!g.paused,g.action==TargetIntent.Talk,()=>
   {
    var current=g.world.FormationLeader(g.selectedParty);g.action=TargetIntent.Talk;g.actionBarPage=ActionBarPage.Primary;
    g.Notify(current==null?"No living selected companion is available.":$"{current.Name} leads the selected group toward the dialogue target; the party leader speaks by default. Click a dialogue target.");
   }));
   result.Add(new("group-attack","ATTACK [A]","Arm hostile targeting for the selected group. Exploration clicks engage the encounter as a party; in combat only the active initiative character can execute the attack.",ActionBarCommandCategory.Group,canOrder,g.action==TargetIntent.Attack,()=>
   {
    g.action=TargetIntent.Attack;g.actionBarPage=ActionBarPage.Primary;g.Notify("Group attack targeting armed.");
   }));
   result.Add(new("group-stop","STOP [X]","Cancel current routes and pending movement orders for the selected party members. Follow Leader remains enabled if it was already active.",ActionBarCommandCategory.Group,selected.Any(a=>a.Alive),false,g.StopSelection));
   result.Add(new("group-hold","HOLD / GUARD","Stop the selected party, cancel Follow Leader, and hold the current ground. Autonomous guard behavior belongs to IIP21 tactics/AI.",ActionBarCommandCategory.Group,selected.Any(a=>a.Alive),false,g.HoldParty));

   for(int slot=0;slot<g.formationQuickSlots.Length;slot++)
   {
    int capturedSlot=slot;var preset=g.FormationAtQuickSlot(slot);
    string hint=$"Formation quick slot {slot+1}: {FormationPatterns.Hint(preset)} Portrait order determines slot order. Right-click this button to reassign the quick slot from the full formation catalogue.";
    result.Add(new($"formation-slot:{slot}",$"F{slot+1} {FormationPatterns.ShortName(preset)}",hint,ActionBarCommandCategory.Formation,exploration,g.IsActiveFormationSlot(slot),()=>g.ActivateFormationQuickSlot(capturedSlot)));
   }
   return result;
  }

  static string CompactQuick(string name)=>name.Length<=9?name.ToUpperInvariant():name[..8].ToUpperInvariant()+"…";
  static string ResourceHint(ResourcePool? resource)=>resource==null?"":$"{resource.Name}: {resource.Current}/{resource.Maximum}.";
 }
}
