using Ashen.Core;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 string armedFeatureAbilityId="";
 string armedFeatureCasterId="";
 string armedItemPowerInstanceId="";
 FeatureTargetMode armedFeatureTargetMode=FeatureTargetMode.None;
 int abilityPage;

 bool FeatureAbilityTargetingActive=>action==TargetIntent.Ability&&(!string.IsNullOrWhiteSpace(armedFeatureAbilityId)||!string.IsNullOrWhiteSpace(armedItemPowerInstanceId))&&armedFeatureCasterId==Controller.Id;

 void ClearArmedFeatureAbility()
 {
  armedFeatureAbilityId="";armedFeatureCasterId="";armedItemPowerInstanceId="";armedFeatureTargetMode=FeatureTargetMode.None;
  if(action==TargetIntent.Ability)action=TargetIntent.Attack;
 }

 void OpenAbilityActionBar(Actor actor)
 {
  if(actor.Enemy||!actor.Alive)return;
  ClearArmedSpell();ClearArmedFeatureAbility();action=TargetIntent.Attack;actionBarPage=ActionBarPage.Abilities;abilityPage=0;contextOpen=false;
 }

 IReadOnlyList<ActionBarCommandModel> BuildAbilityActionCommands(Actor actor)
 {
  var result=new List<ActionBarCommandModel>
  {
   new("abilities-back","< BACK","Return to weapons, quick items, quick spells and the primary command bar.",ActionBarCommandCategory.Utility,true,false,()=>{ClearArmedFeatureAbility();actionBarPage=ActionBarPage.Primary;}),
   new("abilities-details","VIEW ALL","Open the full Abilities page with passive features, resources, traits and right-click descriptions.",ActionBarCommandCategory.Ability,true,false,()=>NavigateFromRail(UiScreenId.Abilities))
  };

  var commands=new List<ActionBarCommandModel>();
  foreach(var feature in actor.Character.Features)
  {
   var definition=ClassAbilityRules.Definition(feature);
   if(definition==null)
   {
    if(feature.HasTag("reaction"))
     commands.Add(new($"ability-reaction:{feature.Id}",feature.Name.ToUpperInvariant(),$"{feature.Description} This is a Reaction feature and is controlled by the IIP15 interrupt system rather than manually activated here.",ActionBarCommandCategory.Ability,false,false,()=>{}));
    continue;
   }
   if(feature.Id.Equals("cunning-action",StringComparison.OrdinalIgnoreCase))
   {
    foreach(var kind in new[]{StandardActionKind.Dash,StandardActionKind.Disengage,StandardActionKind.Hide})
    {
     var captured=kind;bool enabled=world.Mode==Mode.Combat&&world.Active==actor&&actor.Economy.BonusActionAvailable&&!paused;
     commands.Add(new($"ability:cunning:{kind.ToString().ToLowerInvariant()}",$"CUNNING {kind.ToString().ToUpperInvariant()}",$"Cunning Action: use {kind} as a Bonus Action.",ActionBarCommandCategory.Ability,enabled,false,()=>{if(world.ExecuteCunningAction(actor,captured))actionBarPage=ActionBarPage.Primary;}));
    }
    continue;
   }
   bool self=definition.TargetMode is FeatureTargetMode.Self or FeatureTargetMode.None;
   bool available=world.FeatureAbilityAvailable(actor,feature.Id,self?actor:null,out string why);
   string resource="";if(!string.IsNullOrWhiteSpace(definition.ResourceId)&&actor.Character.Resource(definition.ResourceId) is {} pool)resource=$" {pool.Current}/{pool.Maximum}";
   string label=(feature.Id.Equals("sneak-attack",StringComparison.OrdinalIgnoreCase)&&actor.Character.HasCondition("sneak-attack-disabled")?"SNEAK: OFF":feature.Name.ToUpperInvariant())+resource;
   string hint=$"{feature.Description} Cost: {ClassAbilityRules.CostLabel(definition)}{(definition.RangeFeet>0?$"; range {definition.RangeFeet} ft":"")}. {(available?"Ready.":why)}";
   var capturedFeature=feature;
   commands.Add(new($"ability:{feature.Id}",label,hint,ActionBarCommandCategory.Ability,available,FeatureAbilityTargetingActive&&armedFeatureAbilityId==feature.Id,()=>ActivateFeatureCommand(actor,capturedFeature)));
  }

  // IIP34 item powers share the same expandable panel but retain exact-instance ownership.
  if(world.ChargedItemPower(actor) is {} itemPower)
  {
   bool enabled=!paused&&world.Mode==Mode.Combat&&world.Active==actor&&actor.Economy.ActionAvailable;
   commands.Add(new($"ability-item:{itemPower.InstanceId}",$"WAND {itemPower.Charges}c","Wand of Sparks: Action, hostile target within 30 ft, 1d6 + Intelligence lightning damage. Uses the exact charged item instance.",ActionBarCommandCategory.Ability,enabled,FeatureAbilityTargetingActive&&armedItemPowerInstanceId==itemPower.InstanceId,()=>ArmItemPower(actor,itemPower)));
  }

  const int pageSize=8;int pages=Math.Max(1,(commands.Count+pageSize-1)/pageSize);abilityPage=Math.Clamp(abilityPage,0,pages-1);
  result.AddRange(commands.Skip(abilityPage*pageSize).Take(pageSize));
  if(pages>1)
  {
   result.Add(new("abilities-prev","< PAGE","Previous abilities page.",ActionBarCommandCategory.Utility,true,false,()=>abilityPage=(abilityPage+pages-1)%pages));
   result.Add(new("abilities-next","PAGE >",$"Next abilities page ({abilityPage+1}/{pages}).",ActionBarCommandCategory.Utility,true,false,()=>abilityPage=(abilityPage+1)%pages));
  }
  if(commands.Count==0)result.Add(new("abilities-none","NO ACTIVE POWERS","This character currently has no manually activated class features. VIEW ALL still shows passive traits and resources.",ActionBarCommandCategory.Ability,false,false,()=>{}));
  return result;
 }

 void ActivateFeatureCommand(Actor actor,FeatureEntry feature)
 {
  var definition=ClassAbilityRules.Definition(feature);if(definition==null)return;
  // Existing first-playable targeted powers retain their mature targeting authorities.
  if(feature.HasTag("healing")){action=TargetIntent.Heal;actionBarPage=ActionBarPage.Primary;return;}
  if(feature.HasTag("signature")||feature.HasTag("special")){action=TargetIntent.Special;actionBarPage=ActionBarPage.Primary;return;}
  if(definition.TargetMode is FeatureTargetMode.Friendly or FeatureTargetMode.Hostile)
  {
   armedFeatureAbilityId=feature.Id;armedFeatureCasterId=actor.Id;armedItemPowerInstanceId="";armedFeatureTargetMode=definition.TargetMode;action=TargetIntent.Ability;actionBarPage=ActionBarPage.Primary;Notify($"{feature.Name}: choose a {(definition.TargetMode==FeatureTargetMode.Friendly?"friendly":"hostile")} target.");return;
  }
  if(world.UseFeatureAbility(actor,feature.Id)){action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;}
 }

 void ArmItemPower(Actor actor,ItemStack item)
 {
  armedFeatureAbilityId="";armedFeatureCasterId=actor.Id;armedItemPowerInstanceId=item.InstanceId;armedFeatureTargetMode=FeatureTargetMode.Hostile;action=TargetIntent.Ability;actionBarPage=ActionBarPage.Primary;Notify("Wand of Sparks: choose an enemy within 30 ft.");
 }

 bool TryUseArmedFeatureAbility(Actor target)
 {
  if(!FeatureAbilityTargetingActive)return false;var caster=Controller;bool used=false;
  if(!string.IsNullOrWhiteSpace(armedItemPowerInstanceId))used=world.UseChargedItemPower(caster,armedItemPowerInstanceId,target);
  else if(!string.IsNullOrWhiteSpace(armedFeatureAbilityId))used=world.UseFeatureAbility(caster,armedFeatureAbilityId,target);
  if(used){ClearArmedFeatureAbility();action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;}
  return true;
 }

 bool AbilityTargetLegal(Actor target)
 {
  if(!FeatureAbilityTargetingActive)return false;var caster=Controller;
  if(!string.IsNullOrWhiteSpace(armedItemPowerInstanceId))return target.Enemy&&target.Alive&&caster.Position.Distance(target.Position)<=6&&world.Line(caster.Position,target.Position);
  return world.FeatureAbilityAvailable(caster,armedFeatureAbilityId,target,out _);
 }
}
