using Ashen.Core;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 /// <summary>Semantic world cursor resolver. It mirrors the actual left-click path rather
 /// than guessing from art: target modes, rule previews, object state and pause/turn ownership
 /// all participate in the cursor decision.</summary>
 UiCursorKind ResolveWorldCursor()
 {
  if(!WorldHover)return UiCursorKind.Default;
  if(paused)
  {
   if(hoverActor!=null||hoverCorpse!=null||hoverFeature!=null||hoverGround!=null)return UiCursorKind.InvalidTarget;
   return UiCursorKind.Blocked;
  }

  if(FeatureAbilityTargetingActive&&hoverActor is {} abilityTarget)
  {
   if(!AbilityTargetLegal(abilityTarget))return UiCursorKind.InvalidTarget;
   return armedFeatureTargetMode==FeatureTargetMode.Friendly?UiCursorKind.Friendly:UiCursorKind.SpellTarget;
  }

  if(SpellTargetingActive&&armedSpellPlan is {} plan&&ArmedSpell is {} spell)
  {
   var target=SpellHoverTarget(spell,hoverActor);var point=(spell.Target==SpellTargetKind.Point||IsDirectionalSpell(spell))?MouseWorld():target?.Position??MouseWorld();
   var preview=spellTargetPreview??world.PreviewSpell(Controller,plan,target,point);
   if(!preview.Allowed)return UiCursorKind.InvalidTarget;
   if(spell.Area.HasArea)return UiCursorKind.AoE;
   if(preview.FriendlyTarget)return UiCursorKind.Friendly;
   return UiCursorKind.SpellTarget;
  }

  if(hoverActor is {} actor)
  {
   if(actor.Enemy)
   {
    if(action is (TargetIntent.Heal or TargetIntent.Help or TargetIntent.UseObject) || action==TargetIntent.Ability)return UiCursorKind.InvalidTarget;
    if(world.Mode==Mode.Explore)return UiCursorKind.Attack;
    if(action is (TargetIntent.Attack or TargetIntent.Special))
    {
     var preview=attackPreview??world.PreviewAttack(Controller,actor,action==TargetIntent.Special?AttackKind.Special:AttackKind.Basic);
     return preview.Allowed?UiCursorKind.Attack:UiCursorKind.InvalidTarget;
    }
    return UiCursorKind.InvalidTarget;
   }

   if(action==TargetIntent.Ability)return AbilityTargetLegal(actor)?(armedFeatureTargetMode==FeatureTargetMode.Friendly?UiCursorKind.Friendly:UiCursorKind.SpellTarget):UiCursorKind.InvalidTarget;
   if(action==TargetIntent.Talk)return actor.DialogueTarget?UiCursorKind.Talk:UiCursorKind.InvalidTarget;
   if(action==TargetIntent.Heal)return CanHealCursorTarget(actor)?UiCursorKind.Friendly:UiCursorKind.InvalidTarget;
   if(action==TargetIntent.Help&&world.Mode==Mode.Combat)return CanHelpCursorTarget(actor)?UiCursorKind.Friendly:UiCursorKind.InvalidTarget;
   if(actor.DialogueTarget)return UiCursorKind.Talk;
   return UiCursorKind.Select;
  }

  if(hoverCorpse!=null)return world.Mode==Mode.Explore&&Selection.Any()?UiCursorKind.Container:UiCursorKind.InvalidTarget;
  if(hoverFeature is {} feature)return CursorForFeature(feature);
  if(hoverGround!=null)return world.Mode==Mode.Explore&&Selection.Any()?UiCursorKind.Pickup:UiCursorKind.InvalidTarget;

  if(action==TargetIntent.Talk)return UiCursorKind.InvalidTarget;
  if(world.Mode==Mode.Combat&&action is (TargetIntent.Help or TargetIntent.UseObject or TargetIntent.Heal or TargetIntent.Ability))return UiCursorKind.InvalidTarget;
  return moveReason==""?UiCursorKind.Move:UiCursorKind.Blocked;
 }

 bool CanHealCursorTarget(Actor target)
 {
  var source=Controller;var healing=source.HealingFeature;var resource=healing==null?null:source.Character.Resource(healing.Mechanics.ResourceId);
  if(healing==null||resource==null||resource.Current<1||target.Enemy||!target.Alive||target.Hp>=target.MaxHp)return false;
  if(source.Position.Distance(target.Position)>Math.Max(1,healing.Mechanics.RangeFeet/5f)||!world.Line(source.Position,target.Position))return false;
  return world.Mode!=Mode.Combat||world.CanAct(source);
 }

 bool CanHelpCursorTarget(Actor target)
 {
  var source=Controller;
  return world.Mode==Mode.Combat&&target!=source&&!target.Enemy&&target.Alive&&world.CanAct(source)&&source.Position.Distance(target.Position)<=1.25f;
 }

 UiCursorKind CursorForFeature(Feature feature)
 {
  if(action==TargetIntent.Talk)return feature.Kind=="npc"&&world.IsDialogueFeature(feature)?UiCursorKind.Talk:UiCursorKind.InvalidTarget;
  if(feature.Locked)return UiCursorKind.Locked;
  if(feature.DetectedTrap)return feature.Disarmable?UiCursorKind.Disarm:UiCursorKind.Trap;
  if(world.Mode==Mode.Combat)
  {
   if(action!=TargetIntent.UseObject)return UiCursorKind.InvalidTarget;
   bool legal=feature.Kind=="door"&&!feature.Used&&world.CanAct(Controller)&&Controller.Position.Distance(feature.Pos.Center)<=1.25f;
   return legal?UiCursorKind.Door:UiCursorKind.InvalidTarget;
  }
  if(!Selection.Any()||!world.IsFeatureInteractable(feature))return UiCursorKind.InvalidTarget;
  return feature.Kind.ToLowerInvariant() switch
  {
   "door"=>UiCursorKind.Door,
   "chest" or "container"=>UiCursorKind.Container,
   "relic" or "pickup" or "ground" or "loot"=>UiCursorKind.Pickup,
   "travel" or "transition"=>UiCursorKind.Travel,
   "exit" or "stairs"=>UiCursorKind.Stairs,
   "trap"=>feature.Disarmable?UiCursorKind.Disarm:UiCursorKind.Trap,
   "npc"=>UiCursorKind.Talk,
   _=>UiCursorKind.Interact
  };
 }

 // Stable hooks for later spell/thievery systems. These are intentionally semantic and
 // renderer-only: calling code remains responsible for making left click perform the action.
 void SetSpellTargetCursor(bool legal,bool area=false,bool friendly=false)=>ui.Cursors.Set(!legal?UiCursorKind.InvalidTarget:area?UiCursorKind.AoE:friendly?UiCursorKind.Friendly:UiCursorKind.SpellTarget);
 void SetDisarmCursor(bool legal)=>ui.Cursors.Set(legal?UiCursorKind.Disarm:UiCursorKind.InvalidTarget);
 void SetPickpocketCursor(bool legal)=>ui.Cursors.Set(legal?UiCursorKind.Pickpocket:UiCursorKind.InvalidTarget);
}
