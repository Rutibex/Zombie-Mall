using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellSchool { Abjuration, Conjuration, Divination, Enchantment, Evocation, Illusion, Necromancy, Transmutation }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellCastingTime { Action, BonusAction, Reaction, Minute, TenMinutes, Hour, Special }
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellComponent { None=0, Verbal=1, Somatic=2, Material=4 }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellTargetKind { Self, Creature, Point, Object, CreatureOrObject, CreatureOrPoint, Special }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellAreaShape { None, Sphere, Cone, Cube, Cylinder, Line }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellDurationKind { Instantaneous, Rounds, Minutes, Hours, UntilDispelled, Special }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellSlotProgression { None, Full, Half, Third, Pact }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellScalingKind { None, DamageDicePerSlot, HealingDicePerSlot, TargetsPerSlot, Custom }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellEffectKind { None, Damage, Healing, Condition, MagicMissile, Teleport, Utility }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellTargetDisposition { Any, Friendly, Hostile }

public sealed class SpellEffectDefinition
{
 public SpellEffectKind Kind {get;set;}=SpellEffectKind.None;
 public SpellTargetDisposition Disposition {get;set;}=SpellTargetDisposition.Any;
 public int Dice {get;set;}
 public int DieSize {get;set;}
 public int FlatBonus {get;set;}
 public bool AddCastingModifier {get;set;}
 public bool HalfOnSave {get;set;}
 public string ConditionId {get;set;}="";
 public string ConditionName {get;set;}="";
 public string RuntimeNote {get;set;}="";
}

public sealed class SpellAreaDefinition
{
 public SpellAreaShape Shape {get;set;}=SpellAreaShape.None;
 public int SizeFeet {get;set;}
 public int HeightFeet {get;set;}
 public bool OriginatesFromCaster {get;set;}
 [JsonIgnore] public bool HasArea=>Shape!=SpellAreaShape.None&&SizeFeet>0;
 public override string ToString()=>!HasArea?"None":$"{SizeFeet}-ft {Shape.ToString().ToLowerInvariant()}";
}

public sealed class SpellDurationDefinition
{
 public SpellDurationKind Kind {get;set;}=SpellDurationKind.Instantaneous;
 public int Amount {get;set;}
 public string Display=>Kind switch
 {
  SpellDurationKind.Instantaneous=>"Instantaneous",
  SpellDurationKind.UntilDispelled=>"Until dispelled",
  SpellDurationKind.Special=>"Special",
  _=>$"{Math.Max(1,Amount)} {Kind.ToString().ToLowerInvariant()}"
 };
}

public sealed class SpellUpcastRule
{
 public SpellScalingKind Kind {get;set;}=SpellScalingKind.None;
 public int DicePerSlot {get;set;}
 public int TargetsPerSlot {get;set;}
 public string Description {get;set;}="";
 [JsonIgnore] public bool HasScaling=>Kind!=SpellScalingKind.None||!string.IsNullOrWhiteSpace(Description);
}

/// <summary>IIP30 authoritative spell definition. Effects are descriptive/data-oriented until IIP32 executes them.</summary>
public sealed class SpellDefinition
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public int Level {get;set;}
 public SpellSchool School {get;set;}=SpellSchool.Evocation;
 public SpellCastingTime CastingTime {get;set;}=SpellCastingTime.Action;
 public SpellComponent Components {get;set;}=SpellComponent.Verbal|SpellComponent.Somatic;
 public string MaterialComponent {get;set;}="";
 public SpellTargetKind Target {get;set;}=SpellTargetKind.CreatureOrPoint;
 public int RangeFeet {get;set;}=60;
 public SpellAreaDefinition Area {get;set;}=new();
 public SpellDurationDefinition Duration {get;set;}=new();
 public bool Concentration {get;set;}
 public bool Ritual {get;set;}
 public Ability? SavingThrow {get;set;}
 public bool SpellAttack {get;set;}
 public DamageType? DamageType {get;set;}
 public SpellUpcastRule Upcast {get;set;}=new();
 public SpellEffectDefinition Effect {get;set;}=new();
 public List<string> Classes {get;set;}=[];
 public List<string> Tags {get;set;}=[];
 public string Description {get;set;}="";
 public string HigherLevels {get;set;}="";
 [JsonIgnore] public bool IsCantrip=>Level==0;
 [JsonIgnore] public bool HasMaterialComponent=>Components.HasFlag(SpellComponent.Material);
 public bool OnClassList(string classId)=>Classes.Contains(classId,StringComparer.OrdinalIgnoreCase);
 public string ComponentsText()
 {
  var bits=new List<string>();if(Components.HasFlag(SpellComponent.Verbal))bits.Add("V");if(Components.HasFlag(SpellComponent.Somatic))bits.Add("S");if(Components.HasFlag(SpellComponent.Material))bits.Add(string.IsNullOrWhiteSpace(MaterialComponent)?"M":$"M ({MaterialComponent})");return bits.Count==0?"None":string.Join(", ",bits);
 }
 public string RangeText()=>Target==SpellTargetKind.Self&&RangeFeet<=0?"Self":RangeFeet<=0?"Touch":$"{RangeFeet} ft";
}

public sealed class SpellConcentrationState
{
 public string SpellId {get;set;}="";
 public string SourceId {get;set;}="";
 public int SlotLevel {get;set;}
 public int RoundsRemaining {get;set;}=-1;
}

public sealed class SpellCastPlan
{
 public string SourceId {get;set;}="";
 public string SpellId {get;set;}="";
 /// <summary>0 requests the lowest legal slot. Cantrips and rituals resolve to zero.</summary>
 public int SlotLevel {get;set;}
 public bool Ritual {get;set;}
}

public sealed record SpellCastValidation(bool Allowed,string Reason,SpellDefinition? Spell,SpellcastingSource? Source,int SlotLevel,bool Ritual);
public sealed record SpellcastingTradition(string SourceId,Ability Ability,SpellPreparationMode Preparation,SpellSlotProgression Progression,string SpellListId,bool CanRitualCast);

/// <summary>
/// Campaign spell catalogue. IIP30 makes spell identity/level/timing/components/range/area/duration/concentration/ritual/upcast data authoritative.
/// IIP32 will attach executable battlefield effects to these definitions instead of inventing a second spell database.
/// </summary>
public static class SpellCatalog
{
 static readonly Dictionary<string,SpellDefinition> spells=new(StringComparer.OrdinalIgnoreCase);
 static string Title(string id)=>string.Join(" ",id.Split('-',StringSplitOptions.RemoveEmptyEntries).Select(p=>char.ToUpperInvariant(p[0])+p[1..]));
 static SpellDefinition Generic(string id,int level,string classId)=>new()
 {
  Id=id,Name=Title(id),Level=level,School=SpellSchool.Evocation,CastingTime=SpellCastingTime.Action,
  Components=SpellComponent.Verbal|SpellComponent.Somatic,Target=SpellTargetKind.CreatureOrPoint,RangeFeet=60,
  Duration=new(){Kind=SpellDurationKind.Instantaneous},Classes=[classId],
  Description="Structured spell definition for Ashen Covenant. IIP32 owns executable effect resolution; IIP30 owns this spell's rules metadata and casting eligibility."
 };
 static void AddClass(string classId,int level,params string[] ids)
 {
  foreach(var id in ids)
  {
   if(!spells.TryGetValue(id,out var spell)){spell=Generic(id,level,classId);spells[id]=spell;}
   else if(spell.Level!=level)throw new InvalidOperationException($"Spell {id} registered at conflicting levels {spell.Level} and {level}.");
   if(!spell.Classes.Contains(classId,StringComparer.OrdinalIgnoreCase))spell.Classes.Add(classId);
  }
 }
 static SpellDefinition Edit(string id,SpellSchool school,SpellCastingTime time,SpellComponent components,int range,SpellTargetKind target,SpellDurationKind durationKind=SpellDurationKind.Instantaneous,int durationAmount=0,bool concentration=false,bool ritual=false)
 {
  var s=Get(id);s.School=school;s.CastingTime=time;s.Components=components;s.RangeFeet=range;s.Target=target;s.Duration=new(){Kind=durationKind,Amount=durationAmount};s.Concentration=concentration;s.Ritual=ritual;return s;
 }
 static SpellDefinition Save(string id,Ability save){var s=Get(id);s.SavingThrow=save;return s;}
 static SpellDefinition Attack(string id){var s=Get(id);s.SpellAttack=true;return s;}
 static SpellDefinition Area(string id,SpellAreaShape shape,int size,bool caster=false){var s=Get(id);s.Area=new(){Shape=shape,SizeFeet=size,OriginatesFromCaster=caster};return s;}
 static SpellDefinition Material(string id,string text){var s=Get(id);s.Components|=SpellComponent.Material;s.MaterialComponent=text;return s;}
 static SpellDefinition Upcast(string id,SpellScalingKind kind,string description,int dice=0,int targets=0){var s=Get(id);s.Upcast=new(){Kind=kind,Description=description,DicePerSlot=dice,TargetsPerSlot=targets};s.HigherLevels=description;return s;}
 static SpellDefinition Runtime(string id,SpellEffectKind kind,SpellTargetDisposition disposition=SpellTargetDisposition.Any,int dice=0,int die=0,int flat=0,bool addAbility=false,bool halfOnSave=false,string conditionId="",string conditionName="",string note="")
 {
  var s=Get(id);s.Effect=new(){Kind=kind,Disposition=disposition,Dice=dice,DieSize=die,FlatBonus=flat,AddCastingModifier=addAbility,HalfOnSave=halfOnSave,ConditionId=conditionId,ConditionName=conditionName,RuntimeNote=note};return s;
 }
 static void Describe(string id,string text,params string[] tags){var s=Get(id);s.Description=text;s.Tags=tags.ToList();}

 static SpellCatalog()
 {
  AddClass("Wizard",0,"fire-bolt","mage-hand","prestidigitation","minor-illusion");
  AddClass("Wizard",1,"burning-hands","magic-missile","shield","sleep","detect-magic","identify","chromatic-orb","feather-fall","fog-cloud","grease","silent-image","thunderwave","charm-person","hideous-laughter");
  AddClass("Wizard",2,"mirror-image","misty-step","scorching-ray","web","invisibility","shatter","suggestion","darkvision");
  AddClass("Wizard",3,"counterspell","fireball","fly","haste","lightning-bolt","dispel-magic","hypnotic-pattern","fear","major-image","sending","nondetection","water-breathing");
  AddClass("Wizard",4,"dimension-door","greater-invisibility","ice-storm","polymorph","wall-of-fire","arcane-eye","banishment","stoneskin");
  AddClass("Wizard",5,"cone-of-cold","hold-monster","telekinesis","wall-of-force","animate-objects");
  AddClass("Wizard",6,"chain-lightning","disintegrate","globe-of-invulnerability","true-seeing","eyebite","mass-suggestion");
  AddClass("Wizard",7,"finger-of-death","forcecage","plane-shift","reverse-gravity");
  AddClass("Wizard",8,"demiplane","dominate-monster","maze","power-word-stun","feeblemind");
  AddClass("Wizard",9,"foresight","meteor-swarm","time-stop","wish","true-polymorph");

  AddClass("Bard",0,"vicious-mockery","minor-illusion","mage-hand");
  AddClass("Bard",1,"dissonant-whispers","faerie-fire","healing-word","thunderwave","charm-person","cure-wounds","heroism","hideous-laughter","silent-image","sleep","detect-magic");
  AddClass("Bard",2,"enhance-ability","hold-person","invisibility","shatter","heat-metal","suggestion","lesser-restoration","silence");
  AddClass("Bard",3,"hypnotic-pattern","dispel-magic","fear","major-image","sending");
  AddClass("Bard",4,"greater-invisibility","dimension-door","polymorph");
  AddClass("Bard",5,"animate-objects","greater-restoration","hold-monster","mass-cure-wounds");
  AddClass("Bard",6,"eyebite","heroes-feast","mass-suggestion","true-seeing");
  AddClass("Bard",7,"forcecage","regenerate","resurrection");
  AddClass("Bard",8,"dominate-monster","feeblemind");
  AddClass("Bard",9,"foresight","power-word-heal","true-polymorph");

  AddClass("Ranger",1,"hunters-mark","goodberry","ensnaring-strike","fog-cloud","longstrider","speak-with-animals");
  AddClass("Ranger",2,"pass-without-trace","lesser-restoration","silence","spike-growth","animal-messenger","barkskin","darkvision","locate-object","protection-from-poison");
  AddClass("Ranger",3,"conjure-animals","daylight","lightning-arrow","nondetection","plant-growth","water-breathing");
  AddClass("Ranger",4,"conjure-woodland-beings","freedom-of-movement","grasping-vine","locate-creature","stoneskin");
  AddClass("Ranger",5,"commune-with-nature","conjure-volley","swift-quiver","tree-stride");

  AddClass("Cleric",0,"guidance","sacred-flame","thaumaturgy");
  AddClass("Cleric",1,"bless","cure-wounds","guiding-bolt","healing-word","sanctuary","detect-magic");
  AddClass("Cleric",2,"aid","hold-person","lesser-restoration","silence","spiritual-weapon");
  AddClass("Cleric",3,"dispel-magic","mass-healing-word","revivify","sending","spirit-guardians");
  AddClass("Cleric",4,"banishment","death-ward","freedom-of-movement","guardian-of-faith");
  AddClass("Cleric",5,"flame-strike","greater-restoration","mass-cure-wounds");
  AddClass("Cleric",6,"heal","heroes-feast","true-seeing");
  AddClass("Cleric",7,"plane-shift","regenerate","resurrection");
  AddClass("Cleric",8,"holy-aura");
  AddClass("Cleric",9,"mass-heal","true-resurrection");

  // Representative current-campaign spells get exact interaction metadata now. The remaining catalogue entries still carry complete typed fields and can be refined without schema changes.
  Edit("fire-bolt",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,120,SpellTargetKind.CreatureOrObject);Attack("fire-bolt");Describe("fire-bolt","Hurl a mote of fire with a ranged spell attack.","damage","cantrip");
  Edit("sacred-flame",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,60,SpellTargetKind.Creature);Save("sacred-flame",Ability.Dexterity);Describe("sacred-flame","Radiant flame descends on a creature that fails a Dexterity save.","damage","cantrip");
  Edit("guidance",SpellSchool.Divination,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,0,SpellTargetKind.Creature,SpellDurationKind.Minutes,1,true);Describe("guidance","Bolster one creature's next suitable ability check while concentration lasts.","support","concentration");
  Edit("vicious-mockery",SpellSchool.Enchantment,SpellCastingTime.Action,SpellComponent.Verbal,60,SpellTargetKind.Creature);Save("vicious-mockery",Ability.Wisdom);Describe("vicious-mockery","A barbed enchantment harms and distracts a creature that fails a Wisdom save.","damage","control","cantrip");
  Edit("minor-illusion",SpellSchool.Illusion,SpellCastingTime.Action,SpellComponent.Somatic|SpellComponent.Material,30,SpellTargetKind.Point,SpellDurationKind.Minutes,1);Material("minor-illusion","a bit of fleece");Describe("minor-illusion","Create a brief sound or image illusion within range.","utility","illusion","cantrip");
  Edit("mage-hand",SpellSchool.Conjuration,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,30,SpellTargetKind.Point,SpellDurationKind.Minutes,1);Describe("mage-hand","Create a spectral hand for simple remote manipulation.","utility","cantrip");
  Edit("prestidigitation",SpellSchool.Transmutation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,10,SpellTargetKind.Point,SpellDurationKind.Special);Describe("prestidigitation","Produce a small harmless magical effect.","utility","cantrip");
  Edit("thaumaturgy",SpellSchool.Transmutation,SpellCastingTime.Action,SpellComponent.Verbal,30,SpellTargetKind.Point,SpellDurationKind.Minutes,1);Describe("thaumaturgy","Manifest a minor divine wonder.","utility","cantrip");

  Edit("magic-missile",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,120,SpellTargetKind.Creature);Upcast("magic-missile",SpellScalingKind.TargetsPerSlot,"One additional dart for each slot level above 1st.",targets:1);Describe("magic-missile","Create force darts that strike chosen creatures without an attack roll.","damage","force");Get("magic-missile").DamageType=DamageType.Force;
  Edit("shield",SpellSchool.Abjuration,SpellCastingTime.Reaction,SpellComponent.Verbal|SpellComponent.Somatic,0,SpellTargetKind.Self,SpellDurationKind.Rounds,1);Describe("shield","Raise a brief arcane barrier as a reaction; the existing reaction engine applies +5 AC for the interrupt.","reaction","defense");
  Edit("counterspell",SpellSchool.Abjuration,SpellCastingTime.Reaction,SpellComponent.Somatic,60,SpellTargetKind.Creature);Describe("counterspell","Interrupt a creature casting a spell. IIP15 already resolves the reaction window and ability check.","reaction","interrupt");
  Edit("burning-hands",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,0,SpellTargetKind.Self);Area("burning-hands",SpellAreaShape.Cone,15,true);Save("burning-hands",Ability.Dexterity);Upcast("burning-hands",SpellScalingKind.DamageDicePerSlot,"Damage increases by one die for each slot level above 1st.",1);Describe("burning-hands","A short cone of flame erupts from the caster.","damage","area");Get("burning-hands").DamageType=DamageType.Fire;
  Edit("sleep",SpellSchool.Enchantment,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,90,SpellTargetKind.Point,SpellDurationKind.Minutes,1);Area("sleep",SpellAreaShape.Sphere,20);Describe("sleep","A soporific enchantment drops vulnerable creatures into magical slumber.","control","area");
  Edit("cure-wounds",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,0,SpellTargetKind.Creature);Upcast("cure-wounds",SpellScalingKind.HealingDicePerSlot,"Healing increases by one die for each slot level above 1st.",1);Describe("cure-wounds","Restore hit points to a touched living creature.","healing");
  Edit("healing-word",SpellSchool.Evocation,SpellCastingTime.BonusAction,SpellComponent.Verbal,60,SpellTargetKind.Creature);Upcast("healing-word",SpellScalingKind.HealingDicePerSlot,"Healing increases by one die for each slot level above 1st.",1);Describe("healing-word","Restore hit points to a creature at range as a Bonus Action.","healing","bonus-action");
  Edit("bless",SpellSchool.Enchantment,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,30,SpellTargetKind.Creature,SpellDurationKind.Minutes,1,true);Material("bless","holy focus");Upcast("bless",SpellScalingKind.TargetsPerSlot,"Affect one additional creature for each slot level above 1st.",targets:1);Describe("bless","Bolster several allies' attacks and saving throws while concentrating.","support","concentration");
  Edit("sanctuary",SpellSchool.Abjuration,SpellCastingTime.BonusAction,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,30,SpellTargetKind.Creature,SpellDurationKind.Minutes,1);Material("sanctuary","a small silver mirror");Describe("sanctuary","Ward a creature so hostile creatures must overcome the ward to attack it.","defense","bonus-action");
  Edit("guiding-bolt",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,120,SpellTargetKind.Creature,SpellDurationKind.Rounds,1);Attack("guiding-bolt");Upcast("guiding-bolt",SpellScalingKind.DamageDicePerSlot,"Damage increases by one die for each slot level above 1st.",1);Describe("guiding-bolt","A radiant bolt harms a target and briefly guides the next attack against it.","damage","support");Get("guiding-bolt").DamageType=DamageType.Radiant;
  Edit("detect-magic",SpellSchool.Divination,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,0,SpellTargetKind.Self,SpellDurationKind.Minutes,10,true,true);Describe("detect-magic","Sense nearby magic while concentrating. This spell can be cast as a ritual by a source with Ritual Casting.","utility","concentration","ritual");
  Edit("identify",SpellSchool.Divination,SpellCastingTime.Minute,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,0,SpellTargetKind.Object,SpellDurationKind.Instantaneous,0,false,true);Material("identify","a pearl and an owl feather");Describe("identify","Learn the magical properties of a touched object. Ritual-capable sources may cast it as a ritual.","utility","ritual");
  Edit("feather-fall",SpellSchool.Transmutation,SpellCastingTime.Reaction,SpellComponent.Verbal|SpellComponent.Material,60,SpellTargetKind.Creature,SpellDurationKind.Minutes,1);Material("feather-fall","a feather");Describe("feather-fall","Slow falling creatures as a reaction.","reaction","utility");
  Edit("fog-cloud",SpellSchool.Conjuration,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,120,SpellTargetKind.Point,SpellDurationKind.Hours,1,true);Area("fog-cloud",SpellAreaShape.Sphere,20);Upcast("fog-cloud",SpellScalingKind.Custom,"The cloud's radius grows when cast using higher-level slots.");Describe("fog-cloud","Create a heavily obscuring cloud while concentrating.","area","control","concentration");
  Edit("hunters-mark",SpellSchool.Divination,SpellCastingTime.BonusAction,SpellComponent.Verbal,90,SpellTargetKind.Creature,SpellDurationKind.Hours,1,true);Describe("hunters-mark","Mark quarry while concentrating, enabling additional weapon damage and tracking benefits.","concentration","bonus-action","damage");
  Edit("ensnaring-strike",SpellSchool.Conjuration,SpellCastingTime.BonusAction,SpellComponent.Verbal,0,SpellTargetKind.Self,SpellDurationKind.Minutes,1,true);Save("ensnaring-strike",Ability.Strength);Describe("ensnaring-strike","Empower the next weapon hit to entangle a target while concentrating.","concentration","bonus-action","control");
  Edit("pass-without-trace",SpellSchool.Abjuration,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,0,SpellTargetKind.Self,SpellDurationKind.Hours,1,true);Material("pass-without-trace","ashes and spruce");Area("pass-without-trace",SpellAreaShape.Sphere,30,true);Describe("pass-without-trace","Shroud nearby allies in stealth-aiding magic while concentrating.","support","concentration");
  Edit("spike-growth",SpellSchool.Transmutation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,150,SpellTargetKind.Point,SpellDurationKind.Minutes,10,true);Area("spike-growth",SpellAreaShape.Sphere,20);Describe("spike-growth","Transform ground into concealed damaging spikes while concentrating.","area","control","concentration");
  Edit("misty-step",SpellSchool.Conjuration,SpellCastingTime.BonusAction,SpellComponent.Verbal,30,SpellTargetKind.Point);Describe("misty-step","Teleport a short distance to an unoccupied point you can see.","movement","bonus-action");
  Edit("web",SpellSchool.Conjuration,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,60,SpellTargetKind.Point,SpellDurationKind.Hours,1,true);Area("web",SpellAreaShape.Cube,20);Save("web",Ability.Dexterity);Describe("web","Fill an area with restraining webs while concentrating.","area","control","concentration");
  Edit("invisibility",SpellSchool.Illusion,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,0,SpellTargetKind.Creature,SpellDurationKind.Hours,1,true);Upcast("invisibility",SpellScalingKind.TargetsPerSlot,"Affect one additional creature for each slot level above 2nd.",targets:1);Describe("invisibility","Render a creature invisible while concentrating until the effect is broken.","utility","concentration");
  Edit("hold-person",SpellSchool.Enchantment,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,60,SpellTargetKind.Creature,SpellDurationKind.Minutes,1,true);Save("hold-person",Ability.Wisdom);Upcast("hold-person",SpellScalingKind.TargetsPerSlot,"Target one additional humanoid for each slot level above 2nd.",targets:1);Describe("hold-person","Paralyze a humanoid that fails a Wisdom save while concentration lasts.","control","concentration");
  Edit("fireball",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,150,SpellTargetKind.Point);Area("fireball",SpellAreaShape.Sphere,20);Save("fireball",Ability.Dexterity);Upcast("fireball",SpellScalingKind.DamageDicePerSlot,"Damage increases by one die for each slot level above 3rd.",1);Describe("fireball","Detonate an explosive sphere of flame at a point within range.","damage","area");Get("fireball").DamageType=DamageType.Fire;
  Edit("lightning-bolt",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,0,SpellTargetKind.Self);Area("lightning-bolt",SpellAreaShape.Line,100,true);Save("lightning-bolt",Ability.Dexterity);Upcast("lightning-bolt",SpellScalingKind.DamageDicePerSlot,"Damage increases by one die for each slot level above 3rd.",1);Describe("lightning-bolt","A long line of lightning erupts from the caster.","damage","area");Get("lightning-bolt").DamageType=DamageType.Lightning;
  Edit("fly",SpellSchool.Transmutation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,0,SpellTargetKind.Creature,SpellDurationKind.Minutes,10,true);Upcast("fly",SpellScalingKind.TargetsPerSlot,"Affect one additional creature for each slot level above 3rd.",targets:1);Describe("fly","Grant magical flight while concentrating.","movement","concentration");
  Edit("haste",SpellSchool.Transmutation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,30,SpellTargetKind.Creature,SpellDurationKind.Minutes,1,true);Describe("haste","Accelerate one creature while concentrating.","support","concentration");
  Edit("spirit-guardians",SpellSchool.Conjuration,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,0,SpellTargetKind.Self,SpellDurationKind.Minutes,10,true);Area("spirit-guardians",SpellAreaShape.Sphere,15,true);Save("spirit-guardians",Ability.Wisdom);Upcast("spirit-guardians",SpellScalingKind.DamageDicePerSlot,"Damage increases with higher-level slots.",1);Describe("spirit-guardians","Protect the caster with a damaging aura of spirits while concentrating.","damage","area","concentration");
  Edit("revivify",SpellSchool.Necromancy,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,0,SpellTargetKind.Creature);Material("revivify","diamonds consumed by the spell");Describe("revivify","Return a recently dead creature to life under the campaign's resurrection rules.","healing","resurrection");

  // IIP32 executable effect profiles live on the authoritative spell definitions.
  Runtime("fire-bolt",SpellEffectKind.Damage,SpellTargetDisposition.Hostile,1,10);Get("fire-bolt").DamageType=DamageType.Fire;
  Runtime("sacred-flame",SpellEffectKind.Damage,SpellTargetDisposition.Hostile,1,8);Get("sacred-flame").DamageType=DamageType.Radiant;
  Runtime("mage-hand",SpellEffectKind.Utility,note:"The spectral hand manifests at the chosen point; object-manipulation hooks are handled by the world interaction layer.");
  Runtime("prestidigitation",SpellEffectKind.Utility,note:"Produces a minor harmless magical effect at the chosen point.");
  Runtime("minor-illusion",SpellEffectKind.Utility,note:"Creates a minor illusion at the selected point.");
  Runtime("thaumaturgy",SpellEffectKind.Utility,note:"Produces a minor divine wonder at the selected point.");
  Runtime("vicious-mockery",SpellEffectKind.Damage,SpellTargetDisposition.Hostile,1,4,conditionId:"vicious-mockery",conditionName:"Vicious Mockery");Get("vicious-mockery").DamageType=DamageType.Psychic;
  Runtime("guidance",SpellEffectKind.Condition,SpellTargetDisposition.Friendly,conditionId:"guidance",conditionName:"Guidance");
  Runtime("magic-missile",SpellEffectKind.MagicMissile,SpellTargetDisposition.Hostile,1,4,1);
  Runtime("burning-hands",SpellEffectKind.Damage,SpellTargetDisposition.Any,3,6,halfOnSave:true);
  Runtime("sleep",SpellEffectKind.Condition,SpellTargetDisposition.Hostile,conditionId:"asleep",conditionName:"Magically Asleep",note:"The compact tactical runtime applies sleep to hostile creatures in the area; later content may deepen hit-point ordering and wake conditions.");
  Runtime("cure-wounds",SpellEffectKind.Healing,SpellTargetDisposition.Friendly,1,8,addAbility:true);
  Runtime("healing-word",SpellEffectKind.Healing,SpellTargetDisposition.Friendly,1,4,addAbility:true);
  Runtime("bless",SpellEffectKind.Condition,SpellTargetDisposition.Friendly,conditionId:"blessed",conditionName:"Blessed");
  Runtime("sanctuary",SpellEffectKind.Condition,SpellTargetDisposition.Friendly,conditionId:"sanctuary",conditionName:"Sanctuary");
  Runtime("guiding-bolt",SpellEffectKind.Damage,SpellTargetDisposition.Hostile,4,6,conditionId:"guided",conditionName:"Guided");
  Runtime("detect-magic",SpellEffectKind.Condition,SpellTargetDisposition.Friendly,conditionId:"detect-magic",conditionName:"Detect Magic");
  Runtime("hunters-mark",SpellEffectKind.Condition,SpellTargetDisposition.Hostile,conditionId:"hunters-mark",conditionName:"Hunter's Mark");
  Runtime("ensnaring-strike",SpellEffectKind.Condition,SpellTargetDisposition.Friendly,conditionId:"ensnaring-strike",conditionName:"Ensnaring Strike");
  Runtime("pass-without-trace",SpellEffectKind.Condition,SpellTargetDisposition.Friendly,conditionId:"pass-without-trace",conditionName:"Pass Without Trace");
  Runtime("misty-step",SpellEffectKind.Teleport,SpellTargetDisposition.Friendly);
  Runtime("web",SpellEffectKind.Condition,SpellTargetDisposition.Hostile,conditionId:"restrained-web",conditionName:"Restrained by Web");
  Runtime("invisibility",SpellEffectKind.Condition,SpellTargetDisposition.Friendly,conditionId:"invisible",conditionName:"Invisible");
  Runtime("hold-person",SpellEffectKind.Condition,SpellTargetDisposition.Hostile,conditionId:"paralyzed",conditionName:"Paralyzed");
  Runtime("fireball",SpellEffectKind.Damage,SpellTargetDisposition.Any,8,6,halfOnSave:true);
  Runtime("lightning-bolt",SpellEffectKind.Damage,SpellTargetDisposition.Any,8,6,halfOnSave:true);
  Runtime("fly",SpellEffectKind.Condition,SpellTargetDisposition.Friendly,conditionId:"flying",conditionName:"Flying");
  Runtime("haste",SpellEffectKind.Condition,SpellTargetDisposition.Friendly,conditionId:"hasted",conditionName:"Hasted");
  Runtime("spirit-guardians",SpellEffectKind.Condition,SpellTargetDisposition.Friendly,conditionId:"spirit-guardians",conditionName:"Spirit Guardians");
  Runtime("revivify",SpellEffectKind.Healing,SpellTargetDisposition.Friendly,flat:1,note:"Restores a fallen ally to 1 HP in this tactical runtime.");
  // Starting Bard/Ranger spells get complete combat semantics rather than placeholder casts.
  Edit("dissonant-whispers",SpellSchool.Enchantment,SpellCastingTime.Action,SpellComponent.Verbal,60,SpellTargetKind.Creature);Save("dissonant-whispers",Ability.Wisdom);Runtime("dissonant-whispers",SpellEffectKind.Damage,SpellTargetDisposition.Hostile,3,6,halfOnSave:true);Get("dissonant-whispers").DamageType=DamageType.Psychic;
  Edit("faerie-fire",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal,60,SpellTargetKind.Point,SpellDurationKind.Minutes,1,true);Area("faerie-fire",SpellAreaShape.Cube,20);Save("faerie-fire",Ability.Dexterity);Runtime("faerie-fire",SpellEffectKind.Condition,SpellTargetDisposition.Hostile,conditionId:"faerie-fire",conditionName:"Faerie Fire");
  Edit("thunderwave",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic,0,SpellTargetKind.Self);Area("thunderwave",SpellAreaShape.Cube,15,true);Save("thunderwave",Ability.Constitution);Upcast("thunderwave",SpellScalingKind.DamageDicePerSlot,"Damage increases by one die for each slot level above 1st.",1);Runtime("thunderwave",SpellEffectKind.Damage,SpellTargetDisposition.Any,2,8,halfOnSave:true);Get("thunderwave").DamageType=DamageType.Thunder;
  Edit("goodberry",SpellSchool.Transmutation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,0,SpellTargetKind.Self);Runtime("goodberry",SpellEffectKind.Healing,SpellTargetDisposition.Friendly,flat:10,note:"The compact tactical runtime immediately applies the restorative value to the caster.");
  Edit("shatter",SpellSchool.Evocation,SpellCastingTime.Action,SpellComponent.Verbal|SpellComponent.Somatic|SpellComponent.Material,60,SpellTargetKind.Point);Area("shatter",SpellAreaShape.Sphere,10);Save("shatter",Ability.Constitution);Upcast("shatter",SpellScalingKind.DamageDicePerSlot,"Damage increases by one die for each slot level above 2nd.",1);Runtime("shatter",SpellEffectKind.Damage,SpellTargetDisposition.Any,3,8,halfOnSave:true);Get("shatter").DamageType=DamageType.Thunder;
  Edit("hypnotic-pattern",SpellSchool.Illusion,SpellCastingTime.Action,SpellComponent.Somatic|SpellComponent.Material,120,SpellTargetKind.Point,SpellDurationKind.Minutes,1,true);Area("hypnotic-pattern",SpellAreaShape.Cube,30);Save("hypnotic-pattern",Ability.Wisdom);Runtime("hypnotic-pattern",SpellEffectKind.Condition,SpellTargetDisposition.Hostile,conditionId:"hypnotized",conditionName:"Hypnotized");
 }

 public static IReadOnlyCollection<SpellDefinition> All=>spells.Values.OrderBy(s=>s.Level).ThenBy(s=>s.Name).ToArray();
 public static SpellDefinition Get(string id)=>spells.TryGetValue(id,out var s)?s:throw new KeyNotFoundException($"Unknown spell '{id}'.");
 public static bool TryGet(string id,out SpellDefinition definition)
 {
  if(spells.TryGetValue(id,out var found)){definition=found;return true;}definition=null!;return false;
 }
 public static IReadOnlyList<SpellDefinition> ForClass(string classId,int maximumLevel=9)=>spells.Values.Where(s=>s.Level<=maximumLevel&&s.OnClassList(classId)).OrderBy(s=>s.Level).ThenBy(s=>s.Name).ToArray();
 public static int CountByLevel(int level)=>spells.Values.Count(s=>s.Level==level);
}

/// <summary>IIP30 class-aware spellcasting rules and durable resource authority.</summary>
public static class SpellcastingRules
{
 static readonly Dictionary<string,SpellcastingTradition> traditions=new(StringComparer.OrdinalIgnoreCase)
 {
  ["Wizard"]=new("Wizard",Ability.Intelligence,SpellPreparationMode.Spellbook,SpellSlotProgression.Full,"Wizard",true),
  ["Cleric"]=new("Cleric",Ability.Wisdom,SpellPreparationMode.Prepared,SpellSlotProgression.Full,"Cleric",true),
  ["Bard"]=new("Bard",Ability.Charisma,SpellPreparationMode.Known,SpellSlotProgression.Full,"Bard",true),
  ["Ranger"]=new("Ranger",Ability.Wisdom,SpellPreparationMode.Known,SpellSlotProgression.Half,"Ranger",false),
  // Pact is represented even before a Warlock becomes a player-facing class: all Pact slots share one level and recharge separately.
  ["Warlock"]=new("Warlock",Ability.Charisma,SpellPreparationMode.Pact,SpellSlotProgression.Pact,"Warlock",false),
  ["Eldritch Knight"]=new("Eldritch Knight",Ability.Intelligence,SpellPreparationMode.Known,SpellSlotProgression.Third,"Wizard",false),
  ["Arcane Trickster"]=new("Arcane Trickster",Ability.Intelligence,SpellPreparationMode.Known,SpellSlotProgression.Third,"Wizard",false)
 };
 static readonly Dictionary<string,string[]> startingCantrips=new(StringComparer.OrdinalIgnoreCase)
 {
  ["Wizard"]=["fire-bolt","mage-hand","prestidigitation"],
  ["Cleric"]=["sacred-flame","guidance","thaumaturgy"],
  ["Bard"]=["vicious-mockery","minor-illusion"],
  ["Ranger"]=[],["Warlock"]=[],["Eldritch Knight"]=[],["Arcane Trickster"]=[]
 };
 public static IReadOnlyCollection<SpellcastingTradition> Traditions=>traditions.Values.ToArray();
 public static bool TryTradition(string sourceId,out SpellcastingTradition tradition)
 {
  if(traditions.TryGetValue(sourceId,out var found)){tradition=found;return true;}tradition=null!;return false;
 }
 public static SpellcastingTradition Tradition(string sourceId)=>traditions.TryGetValue(sourceId,out var t)?t:throw new KeyNotFoundException($"Unknown spellcasting tradition '{sourceId}'.");

 public static SpellcastingSource CreateSource(string sourceId)
 {
  var t=Tradition(sourceId);var source=new SpellcastingSource{SourceId=t.SourceId,SpellListId=t.SpellListId,CastingAbility=t.Ability,PreparationMode=t.Preparation,SlotProgression=t.Progression,CanRitualCast=t.CanRitualCast};
  if(startingCantrips.TryGetValue(sourceId,out var cantrips))source.Cantrips.AddRange(cantrips);return source;
 }
 public static int ClassLevel(CharacterSheet character,SpellcastingSource source)
 {
  string id=string.IsNullOrWhiteSpace(source.SourceClassId)?source.SourceId:source.SourceClassId;
  int direct=character.Classes.FirstOrDefault(c=>c.ClassId.Equals(id,StringComparison.OrdinalIgnoreCase))?.Level??0;
  if(direct>0)return direct;
  if(source.SourceId.Equals("Eldritch Knight",StringComparison.OrdinalIgnoreCase))return character.Classes.FirstOrDefault(c=>c.SubclassId.Equals("Eldritch Knight",StringComparison.OrdinalIgnoreCase))?.Level??0;
  if(source.SourceId.Equals("Arcane Trickster",StringComparison.OrdinalIgnoreCase))return character.Classes.FirstOrDefault(c=>c.SubclassId.Equals("Arcane Trickster",StringComparison.OrdinalIgnoreCase))?.Level??0;
  return character.TotalLevel;
 }
 public static int MaximumSpellLevel(SpellSlotProgression progression,int classLevel)
 {
  classLevel=Math.Clamp(classLevel,0,20);if(classLevel==0)return 0;
  return progression switch
  {
   SpellSlotProgression.Full=>Math.Min(9,(classLevel+1)/2),
   SpellSlotProgression.Half=>classLevel<2?0:Math.Min(5,(classLevel+3)/4),
   SpellSlotProgression.Third=>classLevel<3?0:classLevel<7?1:classLevel<13?2:classLevel<19?3:4,
   SpellSlotProgression.Pact=>classLevel<3?1:classLevel<5?2:classLevel<7?3:classLevel<9?4:5,
   _=>0
  };
 }
 public static int ClassLevelRequired(string sourceId,int spellLevel)
 {
  var t=Tradition(sourceId);if(spellLevel<=0)return 1;
  return t.Progression switch
  {
   SpellSlotProgression.Full=>spellLevel*2-1,
   SpellSlotProgression.Half=>spellLevel switch{1=>2,2=>5,3=>9,4=>13,_=>17},
   SpellSlotProgression.Third=>spellLevel switch{1=>3,2=>7,3=>13,_=>19},
   SpellSlotProgression.Pact=>spellLevel switch{1=>1,2=>3,3=>5,4=>7,_=>9},
   _=>int.MaxValue
  };
 }
 static int[] FullSlots(int level)=>Math.Clamp(level,1,20) switch
 {
  1=>[2],2=>[3],3=>[4,2],4=>[4,3],5=>[4,3,2],6=>[4,3,3],7=>[4,3,3,1],8=>[4,3,3,2],9=>[4,3,3,3,1],10=>[4,3,3,3,2],11=>[4,3,3,3,2,1],12=>[4,3,3,3,2,1],13=>[4,3,3,3,2,1,1],14=>[4,3,3,3,2,1,1],15=>[4,3,3,3,2,1,1,1],16=>[4,3,3,3,2,1,1,1],17=>[4,3,3,3,2,1,1,1,1],18=>[4,3,3,3,3,1,1,1,1],19=>[4,3,3,3,3,2,1,1,1],20=>[4,3,3,3,3,2,2,1,1],_=>[]
 };
 static int[] HalfSlots(int level)=>level switch
 {
  <2=>[],2=>[2],3=>[3],4=>[3],5=>[4,2],6=>[4,2],7=>[4,3],8=>[4,3],9=>[4,3,2],10=>[4,3,2],11=>[4,3,3],12=>[4,3,3],13=>[4,3,3,1],14=>[4,3,3,1],15=>[4,3,3,2],16=>[4,3,3,2],17=>[4,3,3,3,1],18=>[4,3,3,3,1],19=>[4,3,3,3,2],20=>[4,3,3,3,2],_=>[]
 };
 static int[] ThirdSlots(int level)=>level switch
 {
  <3=>[],3=>[2],4=>[3],5=>[3],6=>[3],7=>[4,2],8=>[4,2],9=>[4,2],10=>[4,3],11=>[4,3],12=>[4,3],13=>[4,3,2],14=>[4,3,2],15=>[4,3,2],16=>[4,3,3],17=>[4,3,3],18=>[4,3,3],19=>[4,3,3,1],20=>[4,3,3,1],_=>[]
 };
 static (int Level,int Count) PactSlots(int level)
 {
  int slotLevel=MaximumSpellLevel(SpellSlotProgression.Pact,level);int count=level<=1?1:level<=10?2:level<=16?3:4;return(slotLevel,count);
 }
 static void ApplyMaxima(SpellcastingSource source,int[] maxima)
 {
  var old=source.Slots.ToDictionary(s=>s.Level);var rebuilt=new List<SpellSlotPool>();
  for(int i=0;i<maxima.Length;i++)
  {
   int level=i+1,max=maxima[i],spent=old.TryGetValue(level,out var previous)?Math.Max(0,previous.Maximum-previous.Current):0;
   rebuilt.Add(new(){Level=level,Maximum=max,Current=Math.Max(0,max-spent)});
  }
  source.Slots=rebuilt;
 }
 public static void RebuildSlots(SpellcastingSource source,int classLevel)
 {
  switch(source.SlotProgression)
  {
   case SpellSlotProgression.Full:ApplyMaxima(source,FullSlots(classLevel));break;
   case SpellSlotProgression.Half:ApplyMaxima(source,HalfSlots(classLevel));break;
   case SpellSlotProgression.Third:ApplyMaxima(source,ThirdSlots(classLevel));break;
   case SpellSlotProgression.Pact:
    var oldSpent=source.Slots.Sum(s=>Math.Max(0,s.Maximum-s.Current));var pact=PactSlots(classLevel);source.Slots=pact.Level<=0?[]:[new(){Level=pact.Level,Maximum=pact.Count,Current=Math.Max(0,pact.Count-oldSpent)}];break;
   default:source.Slots=[];break;
  }
 }
 public static void NormalizeSource(CharacterSheet character,SpellcastingSource source,bool rebuildSlots=false)
 {
  if(TryTradition(source.SourceId,out var t))
  {
   source.SourceClassId=string.IsNullOrWhiteSpace(source.SourceClassId)?t.SourceId:source.SourceClassId;source.SpellListId=t.SpellListId;source.CastingAbility=t.Ability;source.PreparationMode=t.Preparation;source.SlotProgression=t.Progression;source.CanRitualCast=t.CanRitualCast;
   if(source.Cantrips.Count==0&&startingCantrips.TryGetValue(source.SourceId,out var cantrips))source.Cantrips.AddRange(cantrips);
  }
  source.Cantrips=source.Cantrips.Where(id=>SpellCatalog.TryGet(id,out _)).Select(id=>SpellCatalog.Get(id).Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
  source.KnownSpells=source.KnownSpells.Where(id=>SpellCatalog.TryGet(id,out _)).Select(id=>SpellCatalog.Get(id).Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
  source.PreparedSpells=source.PreparedSpells.Where(id=>SpellCatalog.TryGet(id,out _)).Where(id=>source.Cantrips.All(c=>!c.Equals(id,StringComparison.OrdinalIgnoreCase))).Select(id=>SpellCatalog.Get(id).Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
  if(rebuildSlots&&source.SlotProgression!=SpellSlotProgression.None)RebuildSlots(source,ClassLevel(character,source));
  int limit=PreparationLimit(character,source);if(limit>=0&&source.PreparedSpells.Count>limit)source.PreparedSpells=source.PreparedSpells.Take(limit).ToList();
 }
 public static void NormalizeCharacter(CharacterSheet character,bool rebuildSlots=false)
 {
  foreach(var source in character.Spellcasting)NormalizeSource(character,source,rebuildSlots);
  if(character.Concentration!=null)
  {
   if(!SpellCatalog.TryGet(character.Concentration.SpellId,out var spell)||!spell.Concentration)BreakConcentration(character);
   else if(!character.HasCondition("spell-concentration"))character.SetCondition("spell-concentration",$"Concentrating: {spell.Name}",true,spell.Name,tags:["concentration","spell"]);
  }
 }
 public static int PreparationLimit(CharacterSheet character,SpellcastingSource source)
 {
  int level=ClassLevel(character,source),modifier=character.Abilities.Modifier(source.CastingAbility);
  return source.PreparationMode switch
  {
   SpellPreparationMode.Prepared or SpellPreparationMode.Spellbook=>Math.Max(1,level+modifier),
   _=>-1
  };
 }
 public static IReadOnlyList<string> AccessibleSpellIds(CharacterSheet character,SpellcastingSource source)
 {
  int max=MaximumSpellLevel(source.SlotProgression,ClassLevel(character,source));
  if(source.PreparationMode==SpellPreparationMode.Prepared)return SpellCatalog.ForClass(source.SpellListId,max).Where(s=>s.Level>0).Select(s=>s.Id).ToArray();
  return source.KnownSpells.Where(id=>SpellCatalog.TryGet(id,out var spell)&&spell.Level<=max).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
 }
 public static bool CanPrepare(CharacterSheet character,SpellcastingSource source,string spellId,out string reason)
 {
  reason="";if(!SpellCatalog.TryGet(spellId,out var spell)){reason="Unknown spell.";return false;}if(spell.Level==0){reason="Cantrips are always available and are not prepared in slots.";return false;}
  int max=MaximumSpellLevel(source.SlotProgression,ClassLevel(character,source));if(spell.Level>max){reason=$"This source can currently prepare only spell levels 1-{max}.";return false;}
  if(source.PreparationMode==SpellPreparationMode.Spellbook&&!source.KnownSpells.Contains(spellId,StringComparer.OrdinalIgnoreCase)){reason="The spell is not in this spellbook.";return false;}
  if(source.PreparationMode==SpellPreparationMode.Prepared&&!spell.OnClassList(source.SpellListId)){reason=$"The spell is not on the {source.SpellListId} spell list.";return false;}
  if(source.PreparationMode is not (SpellPreparationMode.Spellbook or SpellPreparationMode.Prepared)){reason="This casting source does not use prepared spells.";return false;}
  return true;
 }
 public static bool SetPrepared(CharacterSheet character,SpellcastingSource source,IEnumerable<string> spellIds,out string reason)
 {
  reason="";var ids=spellIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();int limit=PreparationLimit(character,source);if(limit<0){reason="This source does not prepare spells.";return false;}if(ids.Count>limit){reason=$"Prepare at most {limit} spells.";return false;}
  foreach(var id in ids)if(!CanPrepare(character,source,id,out reason))return false;source.PreparedSpells=ids;return true;
 }
 public static bool SpellAvailable(CharacterSheet character,SpellcastingSource source,string spellId)
 {
  if(!SpellCatalog.TryGet(spellId,out var spell))return false;
  if(spell.Level==0)return source.Cantrips.Contains(spellId,StringComparer.OrdinalIgnoreCase);
  return source.PreparationMode switch
  {
   SpellPreparationMode.Prepared=>source.PreparedSpells.Contains(spellId,StringComparer.OrdinalIgnoreCase)&&spell.OnClassList(source.SpellListId),
   SpellPreparationMode.Spellbook=>source.KnownSpells.Contains(spellId,StringComparer.OrdinalIgnoreCase)&&source.PreparedSpells.Contains(spellId,StringComparer.OrdinalIgnoreCase),
   SpellPreparationMode.Known or SpellPreparationMode.Pact=>source.KnownSpells.Contains(spellId,StringComparer.OrdinalIgnoreCase),
   _=>false
  };
 }
 static bool RitualAvailable(CharacterSheet character,SpellcastingSource source,SpellDefinition spell)
 {
  if(!spell.Ritual||!source.CanRitualCast)return false;
  return source.PreparationMode switch
  {
   SpellPreparationMode.Spellbook=>source.KnownSpells.Contains(spell.Id,StringComparer.OrdinalIgnoreCase),
   SpellPreparationMode.Prepared=>source.PreparedSpells.Contains(spell.Id,StringComparer.OrdinalIgnoreCase)&&spell.OnClassList(source.SpellListId),
   SpellPreparationMode.Known or SpellPreparationMode.Pact=>source.KnownSpells.Contains(spell.Id,StringComparer.OrdinalIgnoreCase),
   _=>false
  };
 }
 public static SpellCastValidation ValidateCast(CharacterSheet character,SpellCastPlan plan)
 {
  var source=character.Spellcasting.FirstOrDefault(s=>s.SourceId.Equals(plan.SourceId,StringComparison.OrdinalIgnoreCase));if(source==null)return new(false,"Spellcasting source not found.",null,null,0,plan.Ritual);
  if(!SpellCatalog.TryGet(plan.SpellId,out var spell))return new(false,"Spell definition not found.",null,source,0,plan.Ritual);
  if(plan.Ritual)
  {
   if(!RitualAvailable(character,source,spell))return new(false,"This spell cannot be cast as a ritual by this source.",spell,source,0,true);
   return new(true,"",spell,source,0,true);
  }
  if(!SpellAvailable(character,source,spell.Id))return new(false,"The spell is not currently available to cast from this source.",spell,source,0,false);
  if(spell.Level==0)return new(true,"",spell,source,0,false);
  int requested=plan.SlotLevel<=0?source.Slots.Where(s=>s.Level>=spell.Level&&s.Current>0).Select(s=>s.Level).DefaultIfEmpty(0).Min():plan.SlotLevel;
  if(requested<spell.Level||requested>9)return new(false,$"{spell.Name} requires a slot of level {spell.Level} or higher.",spell,source,requested,false);
  if(!source.Slots.Any(s=>s.Level==requested&&s.Current>0))return new(false,$"No level {requested} spell slot remains.",spell,source,requested,false);
  return new(true,"",spell,source,requested,false);
 }
 public static bool SpendSpellResources(CharacterSheet character,SpellCastPlan plan,out int slotLevel,out string reason)
 {
  var result=ValidateCast(character,plan);slotLevel=result.SlotLevel;reason=result.Reason;if(!result.Allowed||result.Spell==null||result.Source==null)return false;
  if(!result.Ritual&&result.Spell.Level>0)
  {
   var slot=result.Source.Slots.First(s=>s.Level==result.SlotLevel&&s.Current>0);slot.Current--;
  }
  if(result.Spell.Concentration)BeginConcentration(character,result.Spell.Id,result.Source.SourceId,result.SlotLevel);return true;
 }
 public static bool TrySpendSlot(CharacterSheet character,SpellcastingSource source,string spellId,int minimumLevel,out int slotLevel)
 {
  slotLevel=0;if(!SpellCatalog.TryGet(spellId,out var spell)||!SpellAvailable(character,source,spellId)||spell.Level==0)return false;
  var slot=source.Slots.Where(s=>s.Level>=Math.Max(spell.Level,minimumLevel)&&s.Current>0).OrderBy(s=>s.Level).FirstOrDefault();if(slot==null)return false;slot.Current--;slotLevel=slot.Level;return true;
 }
 public static bool BeginConcentration(CharacterSheet character,string spellId,string sourceId,int slotLevel)
 {
  if(!SpellCatalog.TryGet(spellId,out var spell)||!spell.Concentration)return false;BreakConcentration(character);character.Concentration=new(){SpellId=spell.Id,SourceId=sourceId,SlotLevel=Math.Max(0,slotLevel)};character.SetCondition("spell-concentration",$"Concentrating: {spell.Name}",true,spell.Name,tags:["concentration","spell"]);return true;
 }
 public static bool BreakConcentration(CharacterSheet character)
 {
  bool had=character.Concentration!=null||character.HasCondition("spell-concentration");character.Concentration=null;character.SetCondition("spell-concentration","Concentration",false);return had;
 }
 public static int ConcentrationSaveDc(int damage)=>Math.Max(10,Math.Max(0,damage)/2);
 public static ActionResourceKind? ActionCost(SpellDefinition spell)=>spell.CastingTime switch
 {
  SpellCastingTime.Action=>ActionResourceKind.Action,SpellCastingTime.BonusAction=>ActionResourceKind.BonusAction,SpellCastingTime.Reaction=>ActionResourceKind.Reaction,_=>null
 };
 public static void RestoreSlots(CharacterSheet character,bool pactOnly=false)
 {
  foreach(var source in character.Spellcasting.Where(s=>pactOnly?s.SlotProgression==SpellSlotProgression.Pact:s.SlotProgression!=SpellSlotProgression.None))source.RestoreSlots();
 }
}
