using System.Text.Json.Serialization;
namespace Ashen.Core;
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SurvivorAction { Rally,Help,Inspire,Influence,Rush,Phantom,Vault,BreakDoor,Punch,Shove,Unstoppable,SecondWind,Fortitude,Medic,Repair,Hack,Trap,Incendiary,ModifyWeapon,Scout,StretchRations,Foresee,Lockpick }
public sealed record SurvivorActionDefinition(SurvivorAction Action,string ClassId,int Level,string Name,string Cost,string Resource="",ActionResourceKind Economy=ActionResourceKind.Action,string Path="");
public sealed class SurvivorTrap
{
 public string AreaId {get;set;}="";
 public string OwnerId {get;set;}="";
 public Cell Cell {get;set;}
 public int Dice {get;set;}
 public bool Spent {get;set;}
}
public sealed partial class World
{
 public List<SurvivorTrap> SurvivorTraps {get;set;}=[];
 public static readonly SurvivorActionDefinition[] SurvivorActions=
 [
  new(SurvivorAction.Rally,"charismatic",2,"Rallying Cry","Action; 1/short rest","rally"),
  new(SurvivorAction.Help,"charismatic",6,"Quick Thinking","Bonus Action; ally within 30 feet","",ActionResourceKind.BonusAction),
  new(SurvivorAction.Inspire,"charismatic",10,"Indomitable Inspiration","Reaction; 2/long rest","inspire",ActionResourceKind.Reaction),
  new(SurvivorAction.Influence,"charismatic",7,"Call in a Favour","One finite donation per journey; safe refuge"),
  new(SurvivorAction.Rush,"agile",7,"Adrenaline Rush","Bonus Action; 1/long rest","rush",ActionResourceKind.BonusAction),
  new(SurvivorAction.Phantom,"agile",10,"Phantom Survivor","Action; 60 seconds; 1/long rest","phantom"),
  new(SurvivorAction.Lockpick,"agile",3,"Silent Lockpicking","30 seconds + improvised mechanical part; DC12","",ActionResourceKind.Action,"infiltrator"),
  new(SurvivorAction.Vault,"agile",2,"Parkour Vault","Action; legal landing within 15 feet"),
  new(SurvivorAction.BreakDoor,"strong",2,"Breaker","Action + 6 seconds; adjacent ordinary door"),
  new(SurvivorAction.Punch,"strong",3,"Brawler Strike","Bonus Action; adjacent enemy","",ActionResourceKind.BonusAction,"brawler"),
  new(SurvivorAction.Shove,"strong",3,"Shove / Trip","Action; Brawler 5: free once per turn"),
  new(SurvivorAction.Unstoppable,"strong",6,"Unstoppable","Bonus Action; one extra Action; 1/long rest","unstoppable",ActionResourceKind.BonusAction),
  new(SurvivorAction.SecondWind,"tough",5,"Second Wind","Bonus Action; 1/short rest","second-wind",ActionResourceKind.BonusAction),
  new(SurvivorAction.Fortitude,"tough",7,"Survivor's Fortitude","Action; 1/long rest","fortitude"),
  new(SurvivorAction.Medic,"smart",3,"Field Medic","Action; physical kit; 1/short rest","medic",ActionResourceKind.Action,"field-medic"),
  new(SurvivorAction.Repair,"smart",1,"Quick Repair","10 minutes + plank; Master Mechanic 2d6/short rest"),
  new(SurvivorAction.Hack,"smart",2,"Disable Shutter Alarm","10 minutes + mechanical/electrical part; once"),
  new(SurvivorAction.Trap,"smart",1,"Place Proximity Trap","Action; plank + mechanical part; maximum 8 live traps"),
  new(SurvivorAction.Incendiary,"smart",3,"Improvised Incendiary","Action; fuel + mechanical part; 10-ft radius, allies included; DEX half","",ActionResourceKind.Action,"demolitions"),
  new(SurvivorAction.ModifyWeapon,"smart",7,"Accuracy Modification","10 minutes + mechanical/electrical part; firearm +1"),
  new(SurvivorAction.Scout,"wise",2,"Eagle-Eyed Scout","Action; locate existing finite supplies within 30 feet"),
  new(SurvivorAction.StretchRations,"wise",3,"Stretch a Meal","Action; physical food feeds two; once per 24 hours"),
  new(SurvivorAction.Foresee,"wise",10,"Foresee Daily Threat","Action; Insight DC15; 1/long rest","foresee")
 ];
 public IEnumerable<SurvivorActionDefinition> AvailableSurvivorActions(Actor a)=>SurvivorClassesEnabled?SurvivorActions.Where(d=>a.ClassIs(d.ClassId,d.Level)&&(d.Path==""||d.Path==a.Training.Path)):[];
 InventoryState[] ClassInventories(Actor a)=>SurvivalSources(a).ToArray();
 int ClassMaterialCount(Actor a,string id)=>ClassInventories(a).SelectMany(i=>i.Items).Where(s=>EquipmentRules.MaterialMatches(s,id)).Sum(s=>s.Quantity);
 List<CampaignStack> ClassCost(Actor a,SurvivorAction action)=>action switch
 {
  SurvivorAction.Lockpick=>[new(){ItemId="item.works.mechanical"}],
  SurvivorAction.Repair=>[new(){ItemId="item.works.planks"}],
  SurvivorAction.Hack or SurvivorAction.ModifyWeapon=>[new(){ItemId="item.works.mechanical"},new(){ItemId="item.works.electrical"}],
  SurvivorAction.Trap=>[new(){ItemId="item.works.planks"},new(){ItemId=a.ClassIs("smart",9)&&ClassMaterialCount(a,"item.works.mechanical")==0?"item.works.electrical":"item.works.mechanical"}],
  SurvivorAction.Incendiary=>[new(){ItemId="item.works.fuel"},new(){ItemId="item.works.mechanical"}],_=>[]
 };
 public string SurvivorActionReason(Actor a,SurvivorAction action,Actor? target=null,Cell? cell=null)
 {
  var d=AvailableSurvivorActions(a).FirstOrDefault(d=>d.Action==action);if(d==null)return "This class, specialization or level does not grant that ability.";
  if(!Party.Contains(a)||!a.Alive||Mode is not (Mode.Explore or Mode.Combat)||PendingReaction!=null||ReactionInterruptActive||IsMoving(a))return "Select a conscious active survivor and finish movement or interrupts.";
  bool freeShove=action==SurvivorAction.Shove&&a.Training.Path=="brawler"&&a.TotalLevel>=5;
  if(Mode==Mode.Combat&&(Active!=a&&d.Economy!=ActionResourceKind.Reaction||(freeShove?a.Character.HasCondition("class-shove-used"):!CanSpend(a,d.Economy))))return "Wait for this survivor's turn and the required action.";
  if(d.Resource!=""&&SurvivorResource(a,d.Resource)?.Current is not >0)return "No uses remain; take the required rest with daily needs met.";
  if(action is SurvivorAction.Repair or SurvivorAction.Hack or SurvivorAction.ModifyWeapon&&(long)CampaignMinutes+1450>int.MaxValue)return "Campaign clock limit reached.";
  if(action is SurvivorAction.Repair or SurvivorAction.Hack or SurvivorAction.ModifyWeapon or SurvivorAction.Influence)
  {string reason=ShelterManagementReason();if(reason!="")return reason;if(a.Position.Distance(ShelterHomeStockpile()!.Pos.Center)>5)return "Return to the refuge stockpile.";}
  if(action==SurvivorAction.Repair)
  {if(InfrastructureState.BarricadeHp<=0||InfrastructureState.BarricadeHp>=Campaign!.Infrastructure!.BarricadeMaxHp)return "Repair requires a damaged standing barricade.";if(a.Training.Path=="master-mechanic"&&SurvivorResource(a,"mechanic")?.Current is not >0)return "Master Mechanic repair has been spent for this short rest.";}
  if(action==SurvivorAction.Hack&&HasDialogueFlag("flag.class.alarm-disabled"))return "The local alarm is already disabled.";
  if(action==SurvivorAction.Influence&&HasDialogueFlag("flag.class.favour"))return "This finite contact donation has already been received.";
  if(action==SurvivorAction.ModifyWeapon&&(a.Equipment.Weapon?.Definition.MagazineSize is not >0||a.Equipment.Weapon.SurvivorAccuracy))return "Equip an unmodified firearm.";
  if(action is SurvivorAction.Rush or SurvivorAction.Unstoppable&&Mode!=Mode.Combat)return "Use this combat surge during combat.";
  if(action==SurvivorAction.SecondWind&&a.Hp>=a.MaxHp)return "No HP are missing.";
  if(action==SurvivorAction.Fortitude&&a.Needs.Exhaustion==0&&!a.Character.HasCondition("prone")&&!a.Character.HasCondition("blinded")&&!a.Character.HasCondition("restrained"))return "No supported injury or fatigue effect is present.";
  if(action is SurvivorAction.Help or SurvivorAction.Inspire or SurvivorAction.Medic or SurvivorAction.StretchRations)
  {
   if(target==null||!Party.Contains(target)||target.Dead||a.Position.Distance(target.Position)>6||!Line(a.Position,target.Position))return "Select a living ally within 30 feet and clear sight.";
   if(action==SurvivorAction.Medic&&(a.Position.Distance(target.Position)>1.5f||!PhysicalAttackLine(a.Position,target.Position)||target.Hp>=target.MaxHp||AvailableSurvivalSupply(a,"first-aid")==0))return "Stand beside an injured living patient and carry a physical first-aid treatment.";
   if(action==SurvivorAction.StretchRations&&(a.Training.LastMealMinute+1440>CampaignMinutes||a.Needs.FoodTaken||target==a||target.Needs.FoodTaken||AvailableSurvivalSupply(a,"food")==0))return "Two hungry survivors, one physical food unit and a 24-hour meal cooldown are required.";
  }
  if(action is SurvivorAction.Punch or SurvivorAction.Shove)
  {if(Mode!=Mode.Combat||target==null||!target.Enemy||!target.Alive||!Actors.Contains(target)||a.Position.Distance(target.Position)>1.25f||!PhysicalAttackLine(a.Position,target.Position))return "Choose an adjacent visible enemy during combat.";if(action==SurvivorAction.Shove&&LowerTraits(target).Contains("final-immovable"))return "Broseidon cannot be shoved.";if(action==SurvivorAction.Shove&&a.Character.HasCondition("class-shove-used"))return "The free shove has already been used this turn.";}
  if(action==SurvivorAction.Lockpick&&(Mode!=Mode.Explore||cell==null||!Features.Any(f=>f.Pos==cell&&f.Locked&&a.Position.Distance(f.Pos.Center)<=1.25f&&CanSee(a.Position,f.Pos.Center))))return "Choose an adjacent locked object while exploring.";
  if(action==SurvivorAction.BreakDoor&&(cell==null||!Features.Any(f=>f.Kind=="door"&&!f.Used&&f.Pos==cell&&a.Pos.Distance(f.Pos)<=1.5f&&!IsEngagedBarricade(f))))return "Choose an adjacent closed ordinary door; owned barricades require normal unbarring.";
  if(action is SurvivorAction.Vault or SurvivorAction.Trap or SurvivorAction.Incendiary)
  {
   if(cell==null||!Explored.Contains(cell.Value)||!CanStand(cell.Value.Center,a,action!=SurvivorAction.Incendiary)||a.Pos.Distance(cell.Value)>(action==SurvivorAction.Incendiary?6:action==SurvivorAction.Vault?3:1.5f)||!Line(a.Position,cell.Value.Center))return "Choose a discovered legal floor position in range with clear sight.";
   if(action==SurvivorAction.Trap&&(SurvivorTraps.Count(t=>!t.Spent)>=8||SurvivorTraps.Any(t=>!t.Spent&&t.AreaId==CurrentTravelDestinationId&&t.Cell==cell)))return "Only eight distinct live proximity devices may be deployed.";
   if(action==SurvivorAction.Vault&&Mode==Mode.Combat&&a.Economy.MovementRemainingFeet<a.Pos.Distance(cell.Value)*5)return "Not enough movement to reach that landing.";
  }
  foreach(var cost in ClassCost(a,action))if(ClassMaterialCount(a,cost.ItemId)<cost.Quantity)return "Requires physical "+ResolveItemDefinition(cost.ItemId).Name+" in this backpack or nearby safe refuge.";
  if(action==SurvivorAction.Influence)
  {var stock=ShelterHomeStockpile()!.Contents;var stack=new ItemStack{ItemId=Campaign!.Infrastructure!.CleanWaterItemId,Quantity=2,CampaignGear=ResolveItemDefinition(Campaign.Infrastructure.CleanWaterItemId)};if(!stock.CanAdd(stack))return "Make room in the refuge stockpile for two donated water units.";}
  return "";
 }
 public bool UseSurvivorAction(Actor a,SurvivorAction action,Actor? target=null,Cell? cell=null)
 {
  string reason=SurvivorActionReason(a,action,target,cell);if(reason!=""){LogSystem(reason,true);return false;}
  var d=AvailableSurvivorActions(a).First(d=>d.Action==action);bool freeShove=action==SurvivorAction.Shove&&a.Training.Path=="brawler"&&a.TotalLevel>=5;
  if(Mode==Mode.Combat&&!freeShove&&!SpendResource(a,d.Economy,d.Name))return false;
  if(d.Resource!="")SpendSurvivorResource(a,d.Resource);ConsumeWorksCost(ClassInventories(a),ClassCost(a,action));Stop(a);
  void Heal(Actor patient,int n){int before=patient.Hp;patient.Hp=Math.Min(patient.MaxHp,patient.Hp+Math.Max(1,n));patient.Character.SetCondition("unconscious","Unconscious",false);a.Statistics.HealingDone+=patient.Hp-before;patient.Statistics.HealingReceived+=patient.Hp-before;LogHealing(patient,patient.Hp-before,d.Name);}
  void Temp(Actor patient,int n)=>patient.Character.HitPoints.Temporary=Math.Max(patient.Character.HitPoints.Temporary,Math.Max(1,n));
  void Timed(string id){a.Training.TimedEffect=id;a.Training.EffectSeconds=60;a.Training.EffectEndSeconds=CampaignMinutes*60d+CampaignSeconds+60;a.Character.SetCondition(id,id,true);}
  void Supply(string use){var inventory=ClassInventories(a).First(i=>i.Items.Any(s=>SupplyUse(s)==use));var stack=inventory.Items.First(s=>SupplyUse(s)==use);ConsumeSupplyUnit(inventory,stack);a.Statistics.ItemsUsed++;}
  switch(action)
  {
   case SurvivorAction.Rally:foreach(var ally in Party.Where(p=>p.Alive&&p.Position.Distance(a.Position)<=6&&Line(a.Position,p.Position)))Temp(ally,a.AbilityModifier(Ability.Charisma)+a.TotalLevel/2);break;
   case SurvivorAction.Help:case SurvivorAction.Inspire:target!.Character.SetCondition("helped","Helped",true,d.Name);break;
   case SurvivorAction.Influence:SetDialogueFlag("flag.class.favour");ShelterHomeStockpile()!.Contents.Add(CreateItemStack(Campaign!.Infrastructure!.CleanWaterItemId,2));AssignMissingItemIds();break;
   case SurvivorAction.Rush:a.Economy.AddMovement(a.EffectiveSpeedFeet);Temp(a,a.TotalLevel);break;
   case SurvivorAction.Phantom:Timed("class-phantom");break;
   case SurvivorAction.Unstoppable:Timed("class-unstoppable");a.Economy.ActionAvailable=true;a.Economy.AttackActionStarted=false;a.Economy.AttacksRemaining=0;break;
   case SurvivorAction.SecondWind:Heal(a,Roll(10)+a.AbilityModifier(Ability.Constitution));break;
   case SurvivorAction.Fortitude:if(a.Needs.Exhaustion>0)a.Needs.Exhaustion--;else foreach(var c in new[]{"prone","blinded","restrained"})a.Character.SetCondition(c,c,false);break;
   case SurvivorAction.Medic:Supply("first-aid");Heal(target!,RollDamage(2,6)+a.AbilityModifier(Ability.Intelligence));break;
   case SurvivorAction.Repair:int healing=a.Training.Path=="master-mechanic"?RollDamage(2,6):Roll(6);if(a.Training.Path=="master-mechanic")SpendSurvivorResource(a,"mechanic");InfrastructureState.BarricadeHp=Math.Min(Campaign!.Infrastructure!.BarricadeMaxHp,InfrastructureState.BarricadeHp+healing);break;
   case SurvivorAction.Hack:SetDialogueFlag("flag.class.alarm-disabled");ChangeTension(-2,"Disabled shutter alarm");break;
   case SurvivorAction.ModifyWeapon:a.Equipment.Weapon!.SurvivorAccuracy=true;break;
   case SurvivorAction.Trap:SurvivorTraps.RemoveAll(t=>t.Spent);SurvivorTraps.Add(new(){AreaId=CurrentTravelDestinationId,OwnerId=a.Id,Cell=cell!.Value,Dice=a.TotalLevel>=10?4:a.TotalLevel>=5?3:2});break;
   case SurvivorAction.Incendiary:
    foreach(var enemy in Actors.Where(e=>e.Alive&&e.Position.Distance(cell!.Value.Center)<=2&&Line(cell.Value.Center,e.Position)).ToArray()){bool save=SurvivorSave(enemy,Ability.Dexterity,12+a.ProficiencyBonus,"incendiary");Hurt(enemy,SurvivorSaveDamage(enemy,RollDamage(2,6),Ability.Dexterity,save,true),DamageType.Fire,a);}EmitLocalNoise(a,"shout",60);ResolveClassCasualties();break;
   case SurvivorAction.Scout:var caches=Features.Where(f=>f.Contents.Items.Count>0&&f.Pos.Distance(a.Pos)<=6&&Line(a.Position,f.Pos.Center)).OrderBy(f=>f.Pos.Distance(a.Pos)).ToArray();Message(MessageCategory.Narrative,caches.Length==0?"No accessible finite caches detected nearby.":"Existing nearby supplies: "+string.Join(", ",caches.Select(f=>f.Name)));break;
   case SurvivorAction.StretchRations:Supply("food");foreach(var p in new[]{a,target!}){p.Needs.FoodTaken=true;p.Needs.LastFoodMinute=CampaignMinutes;p.Needs.DaysWithoutFood=0;}a.Training.LastMealMinute=CampaignMinutes;break;
   case SurvivorAction.Foresee:bool success=SurvivorSkillCheck(a,Skill.Insight,15);if(success&&TensionEnabled){int roll=DailyDie(TensionState.Seed,TensionState.NextDayMinute/1440,0,100);Message(MessageCategory.Narrative,$"Foresight: next day's fixed D100 is {roll}; current Tension would give {roll+TensionState.Score}. Actions before midnight can change the danger.");}break;
   case SurvivorAction.Vault:float feet=a.Position.Distance(cell!.Value.Center)*5;a.Position=cell.Value.Center;if(Mode==Mode.Combat)a.Economy.SpendMovement(feet);a.Statistics.DistanceTravelledFeet+=feet;Reveal();CheckEncounter();break;
   case SurvivorAction.Lockpick:if(SurvivorSkillCheck(a,Skill.Lockpicking,12))Features.First(f=>f.Pos==cell&&f.Locked).Locked=false;AdvanceCampaignSeconds(30);break;
   case SurvivorAction.BreakDoor:var door=Features.Single(f=>f.Kind=="door"&&f.Pos==cell);door.Used=true;door.Locked=false;EmitLocalNoise(a,"shout",60);if(Mode==Mode.Explore)AdvanceCampaignSeconds(6);Reveal();break;
   case SurvivorAction.Punch:
    int punches=a.TotalLevel>=10?2:1;for(int i=0;i<punches&&target!.Alive;i++){int roll=Roll(20);if(roll==20||roll!=1&&roll+Math.Max(a.AbilityModifier(Ability.Strength),a.AbilityModifier(Ability.Dexterity))+a.ProficiencyBonus>=target.Ac)Hurt(target,RollDamage(roll==20?2:1,a.TotalLevel>=7?8:6)+Math.Max(a.AbilityModifier(Ability.Strength),a.AbilityModifier(Ability.Dexterity)),DamageType.Bludgeoning,a,roll==20);}CheckEnd();break;
   case SurvivorAction.Shove:
    a.Character.SetCondition("class-shove-used","Shove used",true);if(Roll(20)+a.SkillModifier(Skill.Athletics)>=Roll(20)+Math.Max(target!.SkillModifier(Skill.Athletics),target.SkillModifier(Skill.Acrobatics)))
    {var delta=target.Position-a.Position;float length=delta.Distance(new(0,0));if(length>.01f){var next=target.Position+delta*(1/length)*(a.TotalLevel>=9?2:1);if(CanStand(next,target)&&SegmentClear(target.Position,next,target))target.Position=next;else if(a.TotalLevel>=10&&a.Training.Path=="juggernaut")Hurt(target,Roll(6),DamageType.Bludgeoning,a);}target.Character.SetCondition("prone","Prone",true,d.Name);}break;
  }
  if(action is SurvivorAction.Repair or SurvivorAction.Hack or SurvivorAction.ModifyWeapon){CampaignMinutes=checked(CampaignMinutes+10);ProcessSurvivalTime();}
  a.QuickAccess.Normalize(a);Message(MessageCategory.Combat,$"{a.Name} uses {d.Name}.");SyncCampaignObjectives();return true;
 }
 void TriggerSurvivorTraps()
 {
  if(!SurvivorClassesEnabled)return;
  foreach(var trap in SurvivorTraps.Where(t=>t.AreaId==CurrentTravelDestinationId&&!t.Spent).ToArray())
  {var target=Actors.FirstOrDefault(a=>a.Enemy&&a.Alive&&a.Pos==trap.Cell);if(target==null)continue;trap.Spent=true;var owner=Actors.FirstOrDefault(a=>a.Id==trap.OwnerId);bool save=SurvivorSave(target,Ability.Dexterity,13,"trap");Hurt(target,SurvivorSaveDamage(target,RollDamage(trap.Dice,6),Ability.Dexterity,save,true),DamageType.Piercing,owner);if(target.Alive)target.Character.SetCondition("prone","Prone",true,"Proximity trap");Message(MessageCategory.Combat,"A finite proximity device triggers and is spent.");if(Mode==Mode.Combat)CheckEnd();}
  ResolveClassCasualties();
 }
 void ResolveClassCasualties()
 {
  if(!SurvivorClassesEnabled||CheckEnd()||Mode!=Mode.Explore)return;
  foreach(var encounter in Campaign!.Encounters.Where(e=>e.AreaId==CurrentTravelDestinationId&&!ClearedCampaignEncounters.Contains(e.Id)&&e.ActorIds.All(id=>Actors.Any(a=>a.Id==id&&!a.Alive))).ToArray())
  {
   var reward=CampaignEncounterReward(encounter.Group);Gold+=reward.Currency;Cleared++;foreach(var member in Party){member.Character.Experience+=reward.Xp;member.Statistics.EncountersWon++;}
   Message(MessageCategory.Quest,$"{encounter.Id} cleared by a finite class device. Rewards are credited once.");SyncCampaignObjectives();
  }
 }
 void ValidateSurvivorTraps()
 {
  if(SurvivorTraps==null||SurvivorTraps.Count>8||!SurvivorClassesEnabled&&SurvivorTraps.Count>0)throw new InvalidDataException("Invalid proximity device ledger.");
  var live=new HashSet<(string,Cell)>();foreach(var t in SurvivorTraps){var area=Campaign?.Areas.FirstOrDefault(a=>a.Id==t.AreaId);var owner=CommunityMembers.FirstOrDefault(a=>a.Id==t.OwnerId);if(t==null||area==null||!area.Floors.Contains(t.Cell)||owner==null||!owner.ClassIs("smart")||t.Dice is <2 or >4||!t.Spent&&!live.Add((t.AreaId,t.Cell)))throw new InvalidDataException("Invalid class device ownership or location.");}
  foreach(var stack in AllCampaignStacks())if(stack.Definition.MaxCharges>0&&SupplyUse(stack)=="first-aid"&&stack.Charges<1)throw new InvalidDataException("Depleted treatment kit retained in inventory.");
  foreach(var stack in AllCampaignStacks())if(stack.SurvivorAccuracy&&(!SurvivorClassesEnabled||stack.Definition.MagazineSize<=0))throw new InvalidDataException("Unsupported class firearm modification.");
 }
}
