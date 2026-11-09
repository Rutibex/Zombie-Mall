namespace Ashen.Core;

public sealed partial class World
{
 float CampaignAwareness(Actor actor)=>Campaign?.Actors.FirstOrDefault(a=>a.Id==actor.Id)?.AwarenessFeet/5f??7;
 Actor AddCampaignActor(CampaignActor d,Cell position)
 {
  bool survivor=d.Archetype=="survivor";string cls=d.Enemy?"Monster":survivor?"Survivor":"Fighter";
  int die=d.WeaponId==""?d.DamageDie:Campaign!.Items.First(i=>i.Id==d.WeaponId).DamageDie;
  var a=new Actor{UsesEquipmentCatalogue=EquipmentEnabled,UsesSurvivorClasses=SurvivorClassesEnabled&&!d.Enemy,UsesModernCombat=ModernTacticalCombat,Id=d.Id,Name=d.Name,Portrait=d.Portrait,Pos=position,Enemy=d.Enemy,CreatureKind=d.Enemy?(d.Archetype==""?d.Role:d.Archetype):"",Equipment=new(){Weapon=null,Armor=null},Character=new()
  {
   Background=d.Role,SpeedFeet=d.SpeedFeet,Abilities=new(){Strength=d.Abilities.Strength,Dexterity=d.Abilities.Dexterity,Constitution=d.Abilities.Constitution,Intelligence=d.Abilities.Intelligence,Wisdom=d.Abilities.Wisdom,Charisma=d.Abilities.Charisma},
   Narrative=new(){Biography=d.Biography!=""?d.Biography:d.Source.Note,Personality=d.Personality!=""?d.Personality:"Campaign actor."},
   Classes=[new(){ClassId=cls,Level=1,HitDieSize=survivor?8:10}],HitPoints=new(){Current=d.HitPoints,Maximum=d.HitPoints},
   HitDice=[new(){SourceClass=cls,DieSize=survivor?8:10,Current=1,Maximum=1}],Defenses=new(){ArmorClassBase=d.ArmorClass},
   Proficiencies=new(){Skills=d.Skills.Select(s=>new SkillTraining{Skill=s.Skill,Rank=s.Rank}).ToList()},
   MainAttack=new(){Id=d.Id+".attack",Name=d.AttackName,DamageDieSize=die,RangeFeet=5,AttackBonusOverride=d.AttackBonus,DamageBonusOverride=d.DamageBonus,DamageType=d.DamageType}
  }};
  if(d.Archetype is "shambler" or "runner"){a.Character.Defenses.Immunities.Add(DamageType.Poison);a.Character.Defenses.ConditionImmunities.Add("Poisoned");}
  if(d.Enemy)a.Group=Campaign!.Encounters.First(e=>e.ActorIds.Contains(d.Id)).Group;
  Actors.Add(a);if(!d.Enemy){PartyOrder.Add(a.Id);a.UsesDailySurvival=DailySurvivalEnabled;if(DailySurvivalEnabled){a.Needs.NextDailyMinute=checked(CampaignMinutes+1440);a.Needs.LastFoodMinute=CampaignMinutes;a.Needs.LastWaterMinute=CampaignMinutes;}}
  if(d.WeaponId!="")a.Equipment.Weapon=CreateItemStack(d.WeaponId);
  if(d.ArmorId!="")a.Equipment.Armor=CreateItemStack(d.ArmorId);
  foreach(var s in d.Inventory)a.Inventory.Add(CreateItemStack(s.ItemId,s.Quantity));return a;
 }
 public bool CanRecruitCampaignActor(string id,out Cell arrival)
 {
  arrival=default;
  var d=Campaign?.Actors.FirstOrDefault(a=>a.Id==id&&a.Recruitable&&!a.Enemy);
  if(d==null||Mode!=Mode.Explore||PartyCount>=MaxPartySize||Actors.Any(a=>a.Id==id)||ShelterResidents.Any(r=>r.Actor.Id==id)||StoredTravelAreas.Any(a=>a.Actors.Any(x=>x.Id==id)))return false;
  var eventIds=Campaign!.Events.Where(e=>e.Effects.Any(x=>x.Handler=="recruit"&&x.ActorId==id)).Select(e=>e.Id).ToHashSet();
  if(!Features.Any(f=>eventIds.Contains(f.CampaignEventId)&&IsFeatureDiscovered(f)&&Party.Any(a=>a.Alive&&a.Position.Distance(f.Pos.Center)<=1.25f)))return false;
  var placement=CurrentCampaignArea?.Actors.FirstOrDefault(p=>p.ActorId==id);if(placement==null)return false;
  // Use a connected local region; never place a recruit through a closed office door or over an actor.
  var cells=CampaignTravelRules.ArrivalCells(Floors,Floors.Where(Blocked),Actors.Where(a=>a.Alive).Select(a=>a.Pos),placement.Position);
  foreach(var cell in cells.Where(c=>c.Distance(placement.Position)<=2))if(CanStand(cell.Center,null,true)){arrival=cell;return true;}
  return false;
 }
 bool RecruitCampaignActor(string id)
 {
  if(!CanRecruitCampaignActor(id,out var position))return false;
  var d=Campaign!.Actors.First(a=>a.Id==id);AddCampaignActor(d,position);AssignMissingItemIds();Reveal();
  Message(MessageCategory.Narrative,$"{d.Name} joins the expedition.",$"{d.Role}. {d.Personality}");return true;
 }
}
