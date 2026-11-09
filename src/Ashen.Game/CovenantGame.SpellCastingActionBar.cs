using Ashen.Core;
using Microsoft.Xna.Framework;

namespace Ashen.Game;

public sealed partial class CovenantGame
{
 string spellActionActorId="";
 string spellActionSourceId="";
 int spellActionLevelFilter=-1;
 int spellActionPageIndex;
 SpellCastPlan? pendingSpellPlan;
 SpellCastPlan? armedSpellPlan;
 string armedSpellCasterId="";
 SpellTargetPreview? spellTargetPreview;

 bool SpellTargetingActive=>action==TargetIntent.Spell&&armedSpellPlan!=null&&armedSpellCasterId==Controller.Id;

 void ClearArmedSpell()
 {
  armedSpellPlan=null;armedSpellCasterId="";spellTargetPreview=null;
  if(action==TargetIntent.Spell)action=TargetIntent.Attack;
 }

 void ResetSpellActionUi(Actor actor)
 {
  spellActionActorId=actor.Id;spellActionSourceId=actor.Character.Spellcasting.FirstOrDefault()?.SourceId??"";spellActionLevelFilter=-1;spellActionPageIndex=0;pendingSpellPlan=null;
 }

 SpellcastingSource? ActiveSpellActionSource(Actor actor)
 {
  if(actor.Character.Spellcasting.Count==0)return null;
  if(spellActionActorId!=actor.Id)ResetSpellActionUi(actor);
  var source=actor.Character.Spellcasting.FirstOrDefault(s=>s.SourceId.Equals(spellActionSourceId,StringComparison.OrdinalIgnoreCase));
  if(source==null){source=actor.Character.Spellcasting[0];spellActionSourceId=source.SourceId;spellActionLevelFilter=-1;spellActionPageIndex=0;}
  return source;
 }

 public void OpenSpellActionBar(Actor actor)
 {
  if(actor.Character.Spellcasting.Count==0){Notify($"{actor.Name} has no spellcasting source.");return;}
  ResetSpellActionUi(actor);ClearArmedSpell();actionBarPage=ActionBarPage.Spells;action=TargetIntent.Attack;
 }

 IReadOnlyList<SpellDefinition> AvailableActionSpells(Actor actor,SpellcastingSource source)
 {
  var ids=source.Cantrips.Concat(SpellCatalog.All.Where(s=>s.Level>0&&SpellcastingRules.SpellAvailable(actor.Character,source,s.Id)).Select(s=>s.Id));
  return ids.Distinct(StringComparer.OrdinalIgnoreCase).Select(SpellCatalog.Get)
   .Where(world.IsManualSpell)
   .Where(s=>spellActionLevelFilter<0||s.Level==spellActionLevelFilter)
   .OrderBy(s=>s.Level).ThenBy(s=>s.Name).ToArray();
 }

 int[] SpellActionLevels(Actor actor,SpellcastingSource source)
 {
  return source.Cantrips.Concat(SpellCatalog.All.Where(s=>s.Level>0&&SpellcastingRules.SpellAvailable(actor.Character,source,s.Id)).Select(s=>s.Id))
   .Distinct(StringComparer.OrdinalIgnoreCase).Select(SpellCatalog.Get).Where(world.IsManualSpell).Select(s=>s.Level).Distinct().OrderBy(x=>x).ToArray();
 }

 void CycleSpellActionSource(Actor actor)
 {
  if(actor.Character.Spellcasting.Count<2)return;int index=actor.Character.Spellcasting.FindIndex(s=>s.SourceId.Equals(spellActionSourceId,StringComparison.OrdinalIgnoreCase));index=(index+1)%actor.Character.Spellcasting.Count;spellActionSourceId=actor.Character.Spellcasting[index].SourceId;spellActionLevelFilter=-1;spellActionPageIndex=0;
 }

 void CycleSpellActionLevel(Actor actor,SpellcastingSource source)
 {
  var levels=SpellActionLevels(actor,source);var options=new[]{-1}.Concat(levels).ToArray();int index=Array.IndexOf(options,spellActionLevelFilter);spellActionLevelFilter=options[(Math.Max(0,index)+1)%options.Length];spellActionPageIndex=0;
 }

 static string CastTimingLabel(SpellDefinition spell)=>spell.CastingTime switch{SpellCastingTime.BonusAction=>"BA",SpellCastingTime.Reaction=>"R",_=>"A"};
 static string CompactSpellName(string name)=>name.Length<=17?name:name[..16]+"…";

 string SpellActionHint(Actor actor,SpellcastingSource source,SpellDefinition spell,int slotLevel,string reason)
 {
  var cost=spell.CastingTime switch{SpellCastingTime.Action=>"Action",SpellCastingTime.BonusAction=>"Bonus Action",SpellCastingTime.Reaction=>"Reaction",_=>spell.CastingTime.ToString()};
  var area=spell.Area.HasArea?$" | {spell.Area}":"";var save=spell.SavingThrow is {} ability?$" | {ability} save DC {source.SpellSaveDc(actor.Character)}":spell.SpellAttack?$" | spell attack +{source.SpellAttackBonus(actor.Character)}":"";
  var up=spell.Level>0?$" | slot L{slotLevel}":" | cantrip";var conc=spell.Concentration?" | CONCENTRATION":"";var unavailable=string.IsNullOrWhiteSpace(reason)?"":$" UNAVAILABLE: {reason}";
  return $"{spell.Name} [{cost}] — {spell.RangeText()}{area}{save}{up}{conc}. {spell.Description}{unavailable}";
 }

 (bool Enabled,int SlotLevel,string Reason) SpellActionAvailability(Actor actor,SpellcastingSource source,SpellDefinition spell)
 {
  int slot=spell.Level==0?0:world.LegalSpellSlotLevels(actor.Character,source,spell).DefaultIfEmpty(0).Min();
  if(spell.Level>0&&slot==0)return(false,0,$"No level {spell.Level}+ slot remains.");
  var plan=new SpellCastPlan{SourceId=source.SourceId,SpellId=spell.Id,SlotLevel=slot};
  return world.CanBeginSpell(actor,plan,out var reason)?(true,slot,""):(false,slot,reason);
 }

 IReadOnlyList<ActionBarCommandModel> BuildSpellActionCommands(Actor actor)
 {
  var source=ActiveSpellActionSource(actor);var result=new List<ActionBarCommandModel>();
  result.Add(new("spell-back","< BACK","Return to the primary contextual action bar.",ActionBarCommandCategory.Utility,true,false,()=>{actionBarPage=ActionBarPage.Primary;ClearArmedSpell();}));
  if(source==null)return result;
  result.Add(new("spell-source",$"SRC {source.SourceId}",actor.Character.Spellcasting.Count>1?"Cycle this multiclass character's spellcasting sources.":"This character has one spellcasting source.",ActionBarCommandCategory.Spell,actor.Character.Spellcasting.Count>1,false,()=>CycleSpellActionSource(actor)));
  string level=spellActionLevelFilter<0?"ALL":spellActionLevelFilter==0?"CANTRIP":$"L{spellActionLevelFilter}";
  result.Add(new("spell-level",$"LEVEL {level}","Cycle the spell strip filter through ALL and the spell levels this source can currently cast.",ActionBarCommandCategory.Spell,true,false,()=>CycleSpellActionLevel(actor,source)));
  var spells=AvailableActionSpells(actor,source);const int pageSize=5;int pages=Math.Max(1,(spells.Count+pageSize-1)/pageSize);spellActionPageIndex=Math.Clamp(spellActionPageIndex,0,pages-1);
  if(pages>1)result.Add(new("spell-prev","< SPELLS",$"Previous spell page ({spellActionPageIndex+1}/{pages}).",ActionBarCommandCategory.Utility,spellActionPageIndex>0,false,()=>spellActionPageIndex--));
  foreach(var spell in spells.Skip(spellActionPageIndex*pageSize).Take(pageSize))
  {
   var captured=spell;var availability=SpellActionAvailability(actor,source,captured);string prefix=captured.Level==0?"C":$"L{captured.Level}";string label=$"{prefix} {CompactSpellName(captured.Name)} [{CastTimingLabel(captured)}]";
   result.Add(new($"cast-spell:{source.SourceId}:{captured.Id}",label,SpellActionHint(actor,source,captured,availability.SlotLevel,availability.Reason),ActionBarCommandCategory.Spell,availability.Enabled,armedSpellPlan?.SpellId==captured.Id,()=>ChooseSpellForCast(actor,source,captured)));
  }
  if(pages>1)result.Add(new("spell-next","SPELLS >",$"Next spell page ({spellActionPageIndex+1}/{pages}).",ActionBarCommandCategory.Utility,spellActionPageIndex<pages-1,false,()=>spellActionPageIndex++));
  return result;
 }

 void ChooseSpellForCast(Actor actor,SpellcastingSource source,SpellDefinition spell)
 {
  var availability=SpellActionAvailability(actor,source,spell);if(!availability.Enabled){Notify(availability.Reason);return;}
  var slots=world.LegalSpellSlotLevels(actor.Character,source,spell);
  if(spell.Level>0&&slots.Count>1){pendingSpellPlan=new(){SourceId=source.SourceId,SpellId=spell.Id,SlotLevel=slots[0]};actionBarPage=ActionBarPage.SpellSlots;return;}
  ConfirmSpellPlan(actor,new(){SourceId=source.SourceId,SpellId=spell.Id,SlotLevel=availability.SlotLevel});
 }

 IReadOnlyList<ActionBarCommandModel> BuildSpellSlotCommands(Actor actor)
 {
  var result=new List<ActionBarCommandModel>{new("spell-slot-back","< BACK","Return to the spell list without spending a slot.",ActionBarCommandCategory.Utility,true,false,()=>actionBarPage=ActionBarPage.Spells)};
  if(pendingSpellPlan==null||!SpellCatalog.TryGet(pendingSpellPlan.SpellId,out var spell))return result;
  var source=actor.Character.Spellcasting.FirstOrDefault(s=>s.SourceId.Equals(pendingSpellPlan.SourceId,StringComparison.OrdinalIgnoreCase));if(source==null)return result;
  foreach(int slot in world.LegalSpellSlotLevels(actor.Character,source,spell))
  {
   int captured=slot;var pool=source.Slots.First(s=>s.Level==slot);string scaling=slot==spell.Level?"base level":spell.Upcast.HasScaling?spell.Upcast.Description:"higher slot; no additional scaling defined";
   result.Add(new($"spell-slot:{slot}",$"CAST L{slot} ({pool.Current})",$"Cast {spell.Name} with a level {slot} slot — {scaling}.",ActionBarCommandCategory.Spell,true,false,()=>ConfirmSpellPlan(actor,new(){SourceId=source.SourceId,SpellId=spell.Id,SlotLevel=captured})));
  }
  return result;
 }

 void ConfirmSpellPlan(Actor actor,SpellCastPlan plan)
 {
  if(!SpellCatalog.TryGet(plan.SpellId,out var spell)){Notify("Unknown spell.");return;}
  pendingSpellPlan=plan;
  if(spell.Concentration&&actor.Character.Concentration is {} current)
  {
   actionBarPage=ActionBarPage.SpellConcentration;return;
  }
  ArmOrCastSpell(actor,plan);
 }

 IReadOnlyList<ActionBarCommandModel> BuildSpellConcentrationCommands(Actor actor)
 {
  var result=new List<ActionBarCommandModel>();if(pendingSpellPlan==null||!SpellCatalog.TryGet(pendingSpellPlan.SpellId,out var next)){actionBarPage=ActionBarPage.Spells;return BuildSpellActionCommands(actor);}
  string current=actor.Character.Concentration is {} concentration&&SpellCatalog.TryGet(concentration.SpellId,out var existing)?existing.Name:actor.Character.Concentration?.SpellId??"current spell";
  result.Add(new("spell-concentration-back","KEEP CURRENT",$"Keep concentrating on {current} and return to the spell list without spending a resource.",ActionBarCommandCategory.Utility,true,false,()=>{pendingSpellPlan=null;actionBarPage=ActionBarPage.Spells;}));
  var captured=pendingSpellPlan;
  result.Add(new("spell-concentration-confirm",$"END {CompactSpellName(current)}",$"WARNING: casting {next.Name} requires concentration and will immediately end {current} if the new cast succeeds. Continue?",ActionBarCommandCategory.Spell,true,true,()=>ArmOrCastSpell(actor,captured)));
  result.Add(new("spell-concentration-warning","CONCENTRATION WARNING",$"{actor.Name} is concentrating on {current}. 5e permits only one concentration spell at a time.",ActionBarCommandCategory.Utility,false,false,()=>{}));
  return result;
 }

 void ArmOrCastSpell(Actor actor,SpellCastPlan plan)
 {
  pendingSpellPlan=null;if(!SpellCatalog.TryGet(plan.SpellId,out var spell)){Notify("Unknown spell.");return;}
  bool directional=spell.Area.HasArea&&spell.Area.OriginatesFromCaster&&spell.Area.Shape is SpellAreaShape.Cone or SpellAreaShape.Line or SpellAreaShape.Cube;
  if(spell.Target==SpellTargetKind.Self&&!directional)
  {
   if(world.CastSpell(actor,plan,actor,actor.Position)){action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;ClearArmedSpell();}
   return;
  }
  armedSpellPlan=plan;armedSpellCasterId=actor.Id;action=TargetIntent.Spell;actionBarPage=ActionBarPage.Primary;spellTargetPreview=null;
  bool pointTarget=(spell.Target is SpellTargetKind.Point or SpellTargetKind.CreatureOrPoint)||directional;
  Notify($"{spell.Name}: select {(pointTarget?"a target point":"a target")}. Right-click or Esc cancels.");
 }

 bool TryCastArmedSpell(Actor? target,WorldPoint point)
 {
  if(!SpellTargetingActive||armedSpellPlan==null)return false;var caster=Controller;var preview=world.PreviewSpell(caster,armedSpellPlan,target,point);spellTargetPreview=preview;
  if(!preview.Allowed){Notify(preview.Reason);return true;}
  if(world.CastSpell(caster,armedSpellPlan,target,point)){ClearArmedSpell();action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;}
  return true;
 }

 SpellDefinition? ArmedSpell=>armedSpellPlan!=null&&SpellCatalog.TryGet(armedSpellPlan.SpellId,out var spell)?spell:null;
 SpellcastingSource? ArmedSpellSource=>armedSpellPlan==null?null:Controller.Character.Spellcasting.FirstOrDefault(s=>s.SourceId.Equals(armedSpellPlan.SourceId,StringComparison.OrdinalIgnoreCase));
 bool ArmedSpellFriendly=>ArmedSpell?.Effect.Disposition==SpellTargetDisposition.Friendly;
 static bool IsDirectionalSpell(SpellDefinition spell)=>spell.Area.HasArea&&spell.Area.OriginatesFromCaster&&spell.Area.Shape is SpellAreaShape.Cone or SpellAreaShape.Line or SpellAreaShape.Cube;

 Actor? SpellHoverTarget(SpellDefinition spell,Actor? hovered)
 {
  var candidate=hovered??(spell.Id=="revivify"?hoverCorpse:null);
  if(candidate==null)return null;
  return spell.Target is SpellTargetKind.Creature or SpellTargetKind.CreatureOrObject or SpellTargetKind.CreatureOrPoint?candidate:null;
 }

 void UpdateSpellTargetPreview(WorldPoint point)
 {
  if(!SpellTargetingActive||armedSpellPlan==null){spellTargetPreview=null;return;}
  var spell=ArmedSpell;if(spell==null){spellTargetPreview=null;return;}
  var target=SpellHoverTarget(spell,hoverActor);
  spellTargetPreview=world.PreviewSpell(Controller,armedSpellPlan,target,target?.Position??point);
 }

 void DrawSpellTargetingGuidance()
 {
  if(!SpellTargetingActive||armedSpellPlan==null||ArmedSpell is not {} spell)return;
  var caster=Controller;var aim=hoverActor?.Position??MouseWorld();
  if(spell.Target==SpellTargetKind.Point||IsDirectionalSpell(spell))aim=MouseWorld();
  var preview=spellTargetPreview??world.PreviewSpell(caster,armedSpellPlan,SpellHoverTarget(spell,hoverActor),aim);
  var legal=preview.Allowed?Teal:Red;
  if(spell.RangeFeet>0&&!IsDirectionalSpell(spell))GroundRing(Screen(caster.Position),(spell.RangeFeet/5f)*54*zoom,Teal*.30f);
  var center=Screen(aim);
  if(spell.Area.HasArea)
  {
   float radius=Math.Max(12,(spell.Area.SizeFeet/5f)*54*zoom);
   if(spell.Area.Shape is SpellAreaShape.Sphere or SpellAreaShape.Cylinder)GroundRing(center,radius,legal*.75f);
   else if(spell.Area.Shape==SpellAreaShape.Cube)
   {var r=new Rectangle((int)(center.X-radius/2),(int)(center.Y-radius/4),(int)radius,(int)(radius/2));Border(r,legal*.8f);}
   else
   {
    var from=Screen(caster.Position);var delta=center-from;if(delta.LengthSquared()<1)delta=new Microsoft.Xna.Framework.Vector2(1,0);delta.Normalize();
    float length=(spell.Area.SizeFeet/5f)*54*zoom;var end=from+delta*length;Line(from,end,legal*.9f,3);
    if(spell.Area.Shape==SpellAreaShape.Cone)
    {var normal=new Microsoft.Xna.Framework.Vector2(-delta.Y,delta.X);float width=length*.42f;Line(from,end+normal*width,legal*.65f,2);Line(from,end-normal*width,legal*.65f,2);Line(end-normal*width,end+normal*width,legal*.65f,2);}
    else GroundRing(end,12*zoom,legal*.8f);
   }
  }
  else GroundRing(center,(26+(preferences.ReducedMotion?0:3*MathF.Sin(elapsed*7)))*zoom,legal*.8f);
  foreach(var id in preview.AffectedActorIds)if(world.Actors.FirstOrDefault(a=>a.Id==id) is {} affected)GroundRing(Screen(affected.Position),31*zoom,preview.FriendlyTarget?Teal:Gold);
 }
}
