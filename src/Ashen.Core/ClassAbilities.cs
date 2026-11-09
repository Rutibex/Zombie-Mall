using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureTargetMode { None, Self, Friendly, Hostile }

public sealed record FeatureAbilityDefinition(
 string FeatureId,string Name,ActionResourceKind? Cost=ActionResourceKind.Action,
 FeatureTargetMode TargetMode=FeatureTargetMode.None,int RangeFeet=0,string ResourceId="",string Description="");

/// <summary>IIP34 executable class-feature catalogue. Passive features stay on the Character/Abilities screens; this catalogue contains only features that can be deliberately activated or configured.</summary>
public static class ClassAbilityRules
{
 public static FeatureAbilityDefinition? Definition(FeatureEntry feature)=>feature.Id.ToLowerInvariant() switch
 {
  "second-wind"=>new(feature.Id,feature.Name,ActionResourceKind.BonusAction,FeatureTargetMode.Self,0,"second-wind","Regain 1d10 + Fighter level hit points."),
  "action-surge"=>new(feature.Id,feature.Name,null,FeatureTargetMode.Self,0,"action-surge","Regain your Action for this turn. This does not restore Bonus Action or movement."),
  "bardic-inspiration"=>new(feature.Id,feature.Name,ActionResourceKind.BonusAction,FeatureTargetMode.Friendly,60,"bardic-inspiration","Grant a nearby ally an inspiration die for their next attack roll or saving throw."),
  "channel-divinity"=>new(feature.Id,feature.Name,ActionResourceKind.Action,FeatureTargetMode.Self,0,"channel-divinity","Turn nearby undead with divine force."),
  "cunning-action"=>new(feature.Id,feature.Name,ActionResourceKind.BonusAction,FeatureTargetMode.None,0,"","Open Dash, Disengage and Hide as Bonus Action choices."),
  "sneak-attack"=>new(feature.Id,feature.Name,null,FeatureTargetMode.None,0,"","Toggle automatic Sneak Attack use when its conditions are met."),
  "stroke-of-luck"=>new(feature.Id,feature.Name,null,FeatureTargetMode.Self,0,"stroke-of-luck","Prime Stroke of Luck for the next eligible failed attack or ability check."),
  _ when feature.HasTag("healing")=>new(feature.Id,feature.Name,ActionResourceKind.Action,FeatureTargetMode.Friendly,Math.Max(5,feature.Mechanics.RangeFeet),feature.Mechanics.ResourceId,feature.Description),
  _ when feature.HasTag("signature")||feature.HasTag("special")=>new(feature.Id,feature.Name,ActionResourceKind.Action,FeatureTargetMode.Hostile,Math.Max(5,feature.Mechanics.RangeFeet),feature.Mechanics.ResourceId,feature.Description),
  _=>null
 };
 public static bool IsActionable(FeatureEntry feature)=>Definition(feature)!=null||feature.HasTag("reaction");
 public static string CostLabel(FeatureAbilityDefinition definition)=>definition.Cost switch
 {
  ActionResourceKind.Action=>"Action",ActionResourceKind.BonusAction=>"Bonus Action",ActionResourceKind.Reaction=>"Reaction",_=>"Free"
 };
}

public sealed partial class World
{
 public bool FeatureAbilityAvailable(Actor actor,string featureId,Actor? target,out string reason)
 {
  reason="";var feature=actor.Character.Feature(featureId);if(feature==null){reason="Feature not known.";return false;}
  var definition=ClassAbilityRules.Definition(feature);if(definition==null){reason=feature.HasTag("reaction")?"This feature triggers through the Reaction interrupt system.":"This feature is passive.";return false;}
  if(!actor.Alive||actor.Enemy){reason="Only a living companion can use this feature.";return false;}
  if(Mode is Mode.Defeat or Mode.Victory){reason="The encounter is over.";return false;}
  if(Mode==Mode.Combat&&Active!=actor){reason="It is not this character's turn.";return false;}
  if(definition.Cost is {} cost&&Mode==Mode.Combat&&!actor.Economy.Available(cost)){reason=$"{ClassAbilityRules.CostLabel(definition)} already spent.";return false;}
  if(!string.IsNullOrWhiteSpace(definition.ResourceId))
  {
   var pool=actor.Character.Resource(definition.ResourceId);if(pool==null||pool.Current<=0){reason=$"No {pool?.Name??definition.ResourceId} uses remain.";return false;}
  }
  if(definition.TargetMode is FeatureTargetMode.Friendly or FeatureTargetMode.Hostile)
  {
   if(target==null){reason="Choose a target.";return true;}
   bool friendly=definition.TargetMode==FeatureTargetMode.Friendly;if(target.Alive==false||(friendly&&target.Enemy)||(definition.TargetMode==FeatureTargetMode.Hostile&&!target.Enemy)){reason=friendly?"Choose a living ally.":"Choose a living enemy.";return false;}
   if(actor.Position.Distance(target.Position)>Math.Max(1,definition.RangeFeet/5f)+.001f||!Line(actor.Position,target.Position)){reason="Target is out of range or behind a wall.";return false;}
  }
  if(feature.Id.Equals("second-wind",StringComparison.OrdinalIgnoreCase)&&actor.Hp>=actor.MaxHp){reason="Already at full hit points.";return false;}
  if(feature.Id is "action-surge" or "channel-divinity" or "stroke-of-luck"&&Mode!=Mode.Combat){reason="This feature can only be used during combat.";return false;}
  if(feature.Id.Equals("action-surge",StringComparison.OrdinalIgnoreCase)&&Mode==Mode.Combat&&actor.Economy.ActionAvailable){reason="Your Action is already available.";return false;}
  return true;
 }

 bool SpendFeatureCost(Actor actor,FeatureAbilityDefinition definition,ResourcePool? pool)
 {
  if(definition.Cost is {} cost&&Mode==Mode.Combat&&!SpendResource(actor,cost,definition.Name))return false;
  if(pool!=null){if(pool.Current<=0)return false;pool.Current--;}
  return true;
 }

 public bool UseFeatureAbility(Actor actor,string featureId,Actor? target=null)
 {
  if(!FeatureAbilityAvailable(actor,featureId,target,out var reason)){LogSystem(reason,true);return false;}
  var feature=actor.Character.Feature(featureId)!;var definition=ClassAbilityRules.Definition(feature)!;
  // Mature first-playable targeted class actions retain their existing authoritative implementations.
  if(feature.HasTag("healing"))return target!=null&&Heal(actor,target);
  if(feature.HasTag("signature")||feature.HasTag("special"))return target!=null&&Attack(actor,target,AttackKind.Special);
  var pool=string.IsNullOrWhiteSpace(definition.ResourceId)?null:actor.Character.Resource(definition.ResourceId);
  if(!SpendFeatureCost(actor,definition,pool))return false;
  switch(feature.Id.ToLowerInvariant())
  {
   case "second-wind":
   {
    int fighterLevel=LevelingRules.ClassLevel(actor.Character,"Fighter"),amount=Roll(10)+Math.Max(1,fighterLevel),before=actor.Hp;actor.Hp=Math.Min(actor.MaxHp,actor.Hp+amount);int restored=actor.Hp-before;actor.Statistics.HealingDone+=restored;actor.Statistics.HealingReceived+=restored;Message(MessageCategory.Healing,$"{actor.Name} uses Second Wind and regains {restored} HP.");Sound?.Invoke("heal");return true;
   }
   case "action-surge":
    actor.Economy.ActionAvailable=true;actor.Economy.AttackActionStarted=false;actor.Economy.AttacksRemaining=0;Message(MessageCategory.Combat,$"{actor.Name} uses Action Surge and regains an Action.");return true;
   case "bardic-inspiration":
    target!.Character.SetCondition("bardic-inspiration","Bardic Inspiration",true,actor.Name,tags:["inspiration"]);LogCondition(target,"Bardic Inspiration",true,$"granted by {actor.Name}; consumed on the next attack roll or saving throw");return true;
   case "channel-divinity":
   {
    int affected=0;foreach(var enemy in Actors.Where(x=>x.Enemy&&x.Alive&&actor.Position.Distance(x.Position)<=6&&Line(actor.Position,x.Position)))
    {enemy.Character.SetCondition("turned-undead","Turned",true,actor.Name,tags:["turned"]);affected++;LogCondition(enemy,"Turned",true,actor.Name);}
    Message(MessageCategory.Combat,$"{actor.Name} channels divinity; {affected} nearby undead {(affected==1?"is":"are")} turned.");return true;
   }
   case "sneak-attack":
   {
    bool disabled=actor.Character.HasCondition("sneak-attack-disabled");actor.Character.SetCondition("sneak-attack-disabled","Sneak Attack Disabled",!disabled,"IIP34 setting");Message(MessageCategory.System,$"{actor.Name}: Sneak Attack automatic use {(!disabled?"OFF":"ON")}.");return true;
   }
   case "stroke-of-luck":actor.Character.SetCondition("stroke-of-luck-ready","Stroke of Luck Ready",true,"Stroke of Luck");Message(MessageCategory.System,$"{actor.Name} primes Stroke of Luck.");return true;
  }
  return false;
 }

 public bool ExecuteCunningAction(Actor actor,StandardActionKind kind)
 {
  if(actor.Character.Feature("cunning-action")==null){LogSystem("Cunning Action is not known.",true);return false;}
  if(kind is not (StandardActionKind.Dash or StandardActionKind.Disengage or StandardActionKind.Hide)){LogSystem("Cunning Action supports Dash, Disengage, or Hide.",true);return false;}
  if(Mode!=Mode.Combat||Active!=actor||actor.Enemy||!actor.Alive||IsMoving(actor)){LogSystem("Cunning Action requires the active rogue to finish moving.",true);return false;}
  if(!SpendBonusAction(actor,$"Cunning Action: {kind}"))return false;
  switch(kind)
  {
   case StandardActionKind.Dash:actor.Economy.AddMovement(actor.EffectiveSpeedFeet);Message(MessageCategory.Combat,$"{actor.Name} uses Cunning Action to Dash.");break;
   case StandardActionKind.Disengage:actor.Character.SetCondition("disengaged","Disengaged",true,"Cunning Action");LogCondition(actor,"Disengaged",true,"until end of turn");break;
   case StandardActionKind.Hide:
   {
    int roll=Roll(20),bonus=actor.SkillModifier(Skill.Stealth),dc=Actors.Where(x=>x.Enemy!=actor.Enemy&&x.Alive&&x.Position.Distance(actor.Position)<=12).Select(x=>10+x.SkillModifier(Skill.Perception)).DefaultIfEmpty(10).Max();bool success=roll+bonus>=dc;LogAbilityCheck(actor,Skill.Stealth,roll,bonus,dc,success);actor.Character.SetCondition("hidden","Hidden",success,"Cunning Action");break;
   }
  }
  return true;
 }

 public ItemStack? ChargedItemPower(Actor actor)=>actor.Inventory.Items.Concat(actor.Equipment.All()).FirstOrDefault(x=>x.Identified&&x.ItemId=="wand-sparks"&&x.Charges>0);
 public bool UseChargedItemPower(Actor actor,string instanceId,Actor target)
 {
  var stack=actor.Inventory.Items.Concat(actor.Equipment.All()).FirstOrDefault(x=>x.InstanceId==instanceId);if(stack==null||stack.ItemId!="wand-sparks"||!stack.Identified||stack.Charges<=0){LogSystem("That item power is unavailable.",true);return false;}
  if(!target.Alive||!target.Enemy||actor.Position.Distance(target.Position)>6||!Line(actor.Position,target.Position)){LogSystem("Wand of Sparks needs a living enemy within 30 feet and line of sight.",true);return false;}
  if(!SpendAction(actor,"Wand of Sparks"))return false;stack.Charges--;int damage=Roll(6)+Math.Max(0,actor.Character.Abilities.Modifier(Ability.Intelligence));Message(MessageCategory.Combat,$"{actor.Name} releases the Wand of Sparks ({stack.Charges} charges remain).");Hurt(target,damage,DamageType.Lightning,actor);actor.Statistics.ItemsUsed++;return true;
 }
}
