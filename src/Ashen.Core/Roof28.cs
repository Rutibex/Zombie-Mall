using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed partial class Actor
{
 int Roof28GearSkill(Skill skill)=>(skill==Skill.Mechanics&&Character.HasCondition("roof-defense")?1:0)+(skill==Skill.Mechanics&&Inventory.Items.Concat(Equipment.All()).Any(i=>i.ItemId=="item.roof.03.loot.0"&&i.Wear<100)?2:0)+(skill==Skill.Survival&&Character.HasCondition("roof-blueprints")?2:0)+(skill==Skill.Perception&&Character.HasCondition("roof-spores")?-2:0)+(skill==Skill.Mechanics&&Character.HasCondition("roof-electrical-manual")?2:0)-(Character.HasCondition("roof-nausea")?2:0)-(SkillRules.AbilityFor(skill) is Ability.Strength or Ability.Dexterity&&Character.HasCondition("roof-deathbloom")?2:0);
}
public sealed partial class World
{
 static bool Roof28Site(LowerMallSite? d)=>d?.Level=="roof"&&d.DisplayNumber is >=1 and <=13;
 [JsonIgnore] public bool RoofGridOnline=>LowerMallProgress(203)?.Events.Contains("control-restored")==true&&LowerMallProgress(206) is {} s&&s.Events.Contains("grid-connected")&&s.RoofTimers.GetValueOrDefault("fuelUntil")>CampaignMinutes;
 [JsonIgnore] public bool RoofWaterPurified=>LowerMallProgress(205)?.Events.Contains("filter-repaired")==true;
 bool RoofDaylight=>CampaignMinutes%1440 is >=360 and <1200;
 bool RoofTools(Actor a)=>a.Inventory.Items.Concat(a.Equipment.All()).Any(i=>i.Wear<100&&(i.Definition.Name.Contains("Tool",StringComparison.OrdinalIgnoreCase)||i.Definition.Name.Contains("Wrench",StringComparison.OrdinalIgnoreCase)||i.Definition.Name.Contains("Hammer",StringComparison.OrdinalIgnoreCase)));
 bool RoofBattery(Actor a)=>a.Inventory.Items.Any(i=>i.Definition.Name.Contains("Battery",StringComparison.OrdinalIgnoreCase)||i.Definition.Name.Contains("Power Bank",StringComparison.OrdinalIgnoreCase)||i.ItemId=="item.roof.09.loot.2"&&RoofDaylight);
 bool RoofPower(Actor a,LowerMallState s)=>RoofGridOnline||s.RoofTimers.GetValueOrDefault("batteryUntil")>CampaignMinutes||RoofBattery(a);
 void RoofUsePower(Actor a,LowerMallState s)
 {
  if(RoofGridOnline||s.RoofTimers.GetValueOrDefault("batteryUntil")>CampaignMinutes)return;var battery=a.Inventory.Items.First(i=>i.Definition.Name.Contains("Battery",StringComparison.OrdinalIgnoreCase)||i.Definition.Name.Contains("Power Bank",StringComparison.OrdinalIgnoreCase)||i.ItemId=="item.roof.09.loot.2"&&RoofDaylight);if(battery.ItemId!="item.roof.09.loot.2"){battery.ConsumeResourceUnits(1);if(--battery.Quantity==0)a.Inventory.Items.Remove(battery);}s.RoofTimers["batteryUntil"]=CampaignMinutes+60;
 }
 void RoofConsume(Actor a,string item,int count=1)
 {
  foreach(var i in a.Inventory.Items.Where(i=>EquipmentRules.MaterialMatches(i,item)).ToArray()){int used=Math.Min(count,i.Quantity);i.ConsumeResourceUnits(used);i.Quantity-=used;count-=used;if(i.Quantity==0)a.Inventory.Items.Remove(i);if(count==0)return;}throw new InvalidOperationException("Rooftop material preflight disagreed.");
 }
 bool RoofMaterial(Actor a,string item)=>a.Inventory.Items.Any(i=>EquipmentRules.MaterialMatches(i,item));
 void ConfigureRoof28Container(Feature f)
 {
  if(!f.Id.StartsWith("feature.roof."))return;var gun=f.Contents.Items.FirstOrDefault(i=>i.Definition.MagazineSize>0);if(gun!=null){var ammo=f.Contents.Items.FirstOrDefault(i=>i.Definition.Kind==ItemKind.Ammunition&&i.Definition.AmmoType==gun.Definition.AmmunitionType);if(ammo!=null){f.Contents.Items.Remove(ammo);gun.LoadedAmmunition=ammo;}}
  if(f.Id=="feature.roof.12.loot.4")f.Contents.Items.Single().Wear=100;
 }
 void EnterRoof28(LowerMallSite d,LowerMallState s)
 {
  if(!Roof28Site(d))return;int n=d.DisplayNumber;s.BranchRoll=Roll(6);s.BonusRoll=Roll(4);s.RoofTimers["dayAt"]=CampaignMinutes/1440;s.RoofTimers["eventAt"]=CampaignMinutes+10;
  void Trim(string fid,int q){var stack=Features.Single(f=>f.Id==fid).Contents.Items.Single();stack.Quantity=q;stack.ResourceUnits=stack.ResourceUnits.Take(q).ToList();}
  if(n==1){Trim("feature.roof.01.rounds",Roll(4));var sites=Campaign!.LowerMall!.Sites.Where(d=>d.Level!="roof").Select(d=>d.Number).ToList();for(int j=1;j<=3;j++){int i=Roll(sites.Count)-1;s.RoofRolls["feed"+j]=sites[i];sites.RemoveAt(i);}if(s.EncounterRoll==5&&Roll(10)==1)SpawnChapterNamed(d,s,"Security Robot");}
  if(n==2)s.RoofRolls["touch"]=Roll(20);
  if(n==3&&s.EncounterRoll==6)ChangeTension(Roll(6),"Something watches from the power vents");
  if(n==4&&s.BranchRoll==2||n==8&&s.BranchRoll<=3)s.Events.Add("roof-survivor");
  if(n==6||n==11)s.Events.Add("gas-active");
  if(n==7){if(s.EncounterRoll==5)foreach(var p in Party.Where(p=>p.Alive))if(!SurvivorSave(p,Ability.Wisdom,12,"mental shelter whisper"))LowerEffect(p,"upper-paranoia",3600);if(s.HazardId.EndsWith(".0")){s.Events.Add("oxygen-active");s.RoofTimers["oxygenAt"]=CampaignMinutes+20;}if(s.HazardId.EndsWith(".1"))s.RoofTimers["doorAt"]=CampaignMinutes+1;if(s.HazardId.EndsWith(".3"))s.RoofTimers["ratsAt"]=CampaignMinutes+RollDamage(2,6);}
  if(n==8)Trim("feature.roof.08.loot.3",s.BonusRoll<=2?5:s.BonusRoll==3?10:20);
  if(n==10){s.RoofRolls["event"]=Roll(4);s.RoofRolls["linger"]=Roll(6);s.RoofRolls["approach"]=Roll(10);}
  if(n==11){Trim("feature.roof.11.loot.4",Roll(4));s.RoofRolls["pilotDelay"]=Roll(6);}
 }
 void ConfigureRoof28Actors(IEnumerable<string> ids)
 {
  foreach(var id in ids){var a=Actors.Single(a=>a.Id==id);var tags=LowerTraits(a);if(tags.Contains("ambush")&&id.StartsWith("actor.roof."))LowerEffect(a,"hidden",0);if(tags.Contains("roof-waterthing"))a.Character.MainAttack.RangeFeet=10;
   string weapon=tags.Contains("roof-shotgun")?"shelter-shotgun":tags.Contains("roof-revolver")?"panic-revolver":"";if(weapon!=""){a.Equipment.Weapon=CreateItemStack("item.roof.enemy."+weapon);a.Equipment.Weapon.LoadedAmmunition=CreateItemStack(weapon=="shelter-shotgun"?"item.mall.ammo-shells":"item.mall.ammo-38",weapon=="shelter-shotgun"?2:6);}
  }
 }
 void ReturnRoof28(LowerMallSite d,LowerMallState s){if(Roof28Site(d))TickRoof28();}
 string Roof28TravelReason()=>CurrentLowerMallSite is {} d&&Roof28Site(d)&&LowerMallProgress(d.Number)?.Events.Contains("door-locked")==true?"Release the local security lock before leaving. A manual override remains available.":"";
 string Roof28SecureReason(LowerMallSite d,LowerMallState s)
 {
  if(!Roof28Site(d))return "";if(s.Events.Contains("oxygen-active"))return "Repair the active shelter air failure before securing.";if(s.Events.Contains("door-locked"))return "Release the local lockdown before securing.";return "";
 }
 string Roof28Reason(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Roof28Site(d))return "";int n=d.DisplayNumber;string e=a.Effect;
  if(a.NpcActorId!=""&&e!="roof-negotiate"&&!s.Events.Contains("roof-survivor"))return "No living survivor was found on this saved source branch.";
  if(e=="roof-gate"&&!s.Events.Contains("door-locked")&&!s.Events.Contains("oxygen-active"))return "The room's exit is not locked.";
  if(n==2&&e=="roof-panic"&&s.RoofTimers.GetValueOrDefault("lockUntil")>CampaignMinutes&&!actor.Inventory.Items.Any(i=>i.Definition.Name.Contains("Security Keycard")))return "The panic-room lock needs its security keycard or the remaining one-hour lockout to expire.";
  if(n==2&&e=="roof-talk"&&!Actors.Any(t=>t.Alive&&t.Name=="R. Howarth"))return "The conscious manager did not appear or no longer survives.";
  if(n==2&&e=="roof-touch"&&s.BranchRoll!=6)return "No corner figure appeared on this saved panic-room branch.";
  if(e is "roof-cameras" or "roof-usb" or "roof-cctv" or "roof-laptop"&&!RoofPower(actor,s))return "Supply rooftop grid power, a carried battery/power bank, or a solar charger in daylight.";
  if(e=="roof-usb"&&!actor.Inventory.Contains("item.roof.02.loot.4"))return "Carry the encrypted BioDyne USB drive.";
  if(e=="roof-laptop"&&!actor.Inventory.Contains("item.roof.10.loot.2"))return "Recover the dead BioDyne laptop before powering it.";
  if(e=="roof-blueprints"&&!actor.Inventory.Contains("item.roof.02.loot.3"))return "Carry the actual recovered mall blueprints.";
  if(e is "roof-reroute" or "roof-power" or "roof-generator" or "roof-grid" or "roof-filter"&&!RoofTools(actor))return "Carry a usable repair toolkit or wrench.";
  if(n==6&&e=="roof-grid"&&LowerMallProgress(203)?.Events.Contains("control-restored")!=true)return "Repair and reroute the Power Control Room first.";
  if(e=="roof-refuel"&&(!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)||s.RoofTimers.GetValueOrDefault("fuelUntil")>CampaignMinutes+1200))return "Carry usable fuel; the rooftop reserve holds at most 24 hours.";
  if(n==4&&e=="roof-seal"&&(!actor.Inventory.Contains("item.roof.04.loot.1")||!actor.Inventory.Contains("item.roof.04.loot.2")))return "Recover the portable welder and actual scrap/wire bundle before sealing vents.";
  if(e=="roof-drone"&&!Actors.Any(t=>t.Alive&&LowerTraits(t).Contains("roof-drone")))return "No functioning hostile repair drone remains to reprogram.";
  if(e=="roof-negotiate"&&!Actors.Any(t=>t.Alive&&LowerTraits(t).Contains("roof-shotgun")))return "Find the living Last Survivor before negotiating.";
  if(n==9&&a.Id=="action.roof.09.loot.0"&&TensionState.Score>=50)return "The crop harvest requires tension below 50.";
  if(e=="roof-pump"&&(!s.Secured||!s.Events.Contains("filter-repaired")||s.RoofTimers.GetValueOrDefault("pumpDay",-1)==CampaignMinutes/1440))return "Secure and repair filtration first; one daily water draw is available per campaign day.";
  if(e=="roof-pump"&&!actor.Inventory.CanAdd(new(){ItemId=Campaign!.Infrastructure!.CleanWaterItemId,CampaignGear=ResolveItemDefinition(Campaign.Infrastructure.CleanWaterItemId),Quantity=3}))return "Make backpack space for three daily water portions.";
  if(e=="roof-pistol"&&(!actor.ClassIs("smart")||!actor.Inventory.Contains("item.roof.12.loot.4")))return "A Smart survivor must carry the jammed security pistol.";
  if(e=="roof-repair"&&(!actor.Inventory.Contains("item.roof.11.loot.3")||!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)||!RoofMaterial(actor,Campaign.Infrastructure.GeneratorCost[0].ItemId)))return "Carry aviation tools, usable fuel and one structural repair material. Repairing the airframe does not authorize takeoff.";
  if(n==11&&e is "roof-pilot" or "roof-mercy"&&(s.Events.Contains("pilot-revived")||s.Events.Contains("pilot-ended")))return "The pilot's fate was already decided.";
  if(n==13&&e=="roof-signal"&&(!s.Secured||!RoofGridOnline||ShelterHomeStockpile() is not {} home||StoredCreditedSv(home)==0))return "Secure the billboard, power the rooftop grid, and retain credited supplies in the original refuge stockpile.";
  return "";
 }
 void Roof28HazardResult(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a,bool pass)
 {
  if(!Roof28Site(d))return;int n=d.DisplayNumber,j=int.Parse(a.Id.Split('.').Last());
  if(pass){if(n==7&&j==0){s.Events.Remove("oxygen-active");s.Events.Add("air-repaired");}if(n==7&&j==1)s.RoofTimers.Remove("doorAt");if(n==7&&j==3)s.RoofTimers.Remove("ratsAt");return;}
  if(n==1&&j==0){s.RoofTimers["alarmAt"]=CampaignMinutes+1;}if(n==1&&j==3||n==2&&j==0)s.Events.Add("door-locked");if(n==2&&j==3)s.RoofTimers["lockUntil"]=CampaignMinutes+60;
  if(n==5&&j==0)Roof28Reinforce(d,s,Roll(6));if(n==8&&j==2)s.Events.Add("loose-ammo");if(n==9&&j==4)SpawnChapterNamed(d,s,"Vine-Twisted Horror");
  if(n==12&&j==2)Roof28Reinforce(d,s,Roll(4));if(n==12&&j==5)SpawnChapterNamed(d,s,"Clinger Zombie");
 }
 void Roof28Milestone(LowerMallSite d,LowerMallState s,string key)
 {
  var a=d.Actions.FirstOrDefault(a=>a.Id.EndsWith("milestone."+key));if(a!=null&&!s.Resolved.Contains(a.Id)){s.Resolved.Add(a.Id);CreditLower(s,a.Id,a.Xp);}
 }
 void Roof28Reinforce(LowerMallSite d,LowerMallState s,int count)
 {
  var e=Campaign!.Encounters.First(e=>e.AreaId==d.AreaId&&e.ActorIds.Any(id=>Campaign.Actors.Single(a=>a.Id==id).Role=="Shambler"));int had=s.Releases.FirstOrDefault(r=>r.EncounterId==e.Id)?.Count??0;SpawnLower(d,s,e.Id,had+count,"reaction");
 }
 void Roof28Device(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Roof28Site(d))return;int n=d.DisplayNumber;string e=a.Effect;
  if(a.Kind=="loot"){if(n==1&&a.Id.EndsWith("loot.0"))Roof28Milestone(d,s,"permit");if(n==3&&a.Id.EndsWith("loot.3"))Roof28Milestone(d,s,"logs");if(n==11)Roof28Noise(actor,"search");}
  switch(e)
  {
   case "roof-cameras":RoofUsePower(actor,s);s.Events.Add("cameras-on");s.RoofTimers["cameraAt"]=CampaignMinutes+60;foreach(var feed in s.RoofRolls.Where(p=>p.Key.StartsWith("feed"))){var room=Campaign!.LowerMall!.Sites.Single(d=>d.Number==feed.Value);Message(MessageCategory.Narrative,"Surveillance feed: "+room.Level+" "+room.DisplayNumber+" "+room.Name+" — "+(LowerMallProgress(room.Number)==null?"unexplored, movement cannot yet be identified":LowerMallProgress(room.Number)!.Secured?"previously secured":"previously visited; approach with caution"));}break;
   case "roof-radio":Roof28Radio(d,s,actor);break;
   case "roof-scout":ChangeTension(-Roll(6),"Saved surveillance route scouted");break;
   case "roof-barricade":ChangeTension(-2,"Security office barricaded");break;
   case "roof-panic":if(s.BranchRoll is 3 or 4)SpawnChapterNamed(d,s,"R. Howarth");if(s.BranchRoll==5)SpawnChapterNamed(d,s,"Throat Parasite");if(s.BranchRoll<=2)Message(MessageCategory.Narrative,"A desiccated manager clutches a bloodied resignation letter and a parking-garage keycard. The garage is a later region.");break;
   case "roof-talk":AddStoryJournalEntry("roof.howarth.testimony","Howarth’s residual humanity","Howarth whispers that the mall remained open under corporate orders. His testimony is evidence, not a resolved apocalypse origin.","Roof");break;
   case "roof-touch":if(s.RoofRolls["touch"]>10)SpawnChapterNamed(d,s,"Corner Figure");else s.Events.Add("figure-vanished");break;
   case "roof-usb":case "roof-laptop":case "roof-cctv":RoofUsePower(actor,s);SetDialogueFlag("flag."+d.Prefix+".decoded");break;
   case "roof-blueprints":LowerEffect(actor,"roof-blueprints",0);break;
   case "roof-gate":s.Events.Remove("door-locked");s.RoofTimers.Remove("doorAt");s.RoofTimers.Remove("lockUntil");foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=true;break;
   case "roof-reroute":s.Events.Add("rerouted");break;
   case "roof-power":s.Events.Add(n==3?"control-restored":"relay-restored");ChangeTension(RollDamage(n==4?3:1,6),"Rooftop power restored");break;
   case "roof-drone":PacifyUpper26("Repair Drone");s.Events.Add("drone-reprogrammed");break;
   case "roof-manual":LowerEffect(actor,n==7?"roof-defense":n==6?"roof-electrical-manual":"roof-generator-manual",0);break;
   case "roof-seal":RoofConsume(actor,"item.roof.04.loot.2");s.Events.Add("vents-sealed");ChangeTension(-RollDamage(2,6),"HVAC vents sealed");break;
   case "roof-rescue":LowerRecruit(a,s);break;
   case "roof-tank":SpawnChapterNamed(d,s,"Thing in the Water");break;
   case "roof-filter":s.Events.Add("filter-repaired");ChangeTension(-Roll(4),"Rooftop water purified");break;
   case "roof-pump":s.RoofTimers["pumpDay"]=CampaignMinutes/1440;actor.Inventory.Add(CreateItemStack(Campaign!.Infrastructure!.CleanWaterItemId,3));break;
   case "roof-leak":s.Events.Remove("gas-active");s.Events.Add("gas-disabled");break;
   case "roof-generator":s.Events.Add("generator-repaired");ChangeTension(-Roll(6),"Rooftop generator repaired");break;
   case "roof-grid":s.Events.Add("grid-connected");SetDialogueFlag("flag.roof.grid-connected");break;
   case "roof-refuel":RoofConsume(actor,Campaign!.Infrastructure!.FuelItemId);s.RoofTimers["fuelUntil"]=Math.Max(CampaignMinutes,s.RoofTimers.GetValueOrDefault("fuelUntil"))+240;break;
   case "roof-light":s.Events.Add("bright-light");break;
   case "roof-air":s.Events.Remove("oxygen-active");s.Events.Add("air-repaired");break;
   case "roof-vault":if(s.EncounterRoll==4)SpawnChapterNamed(d,s,"Elder Thing Spawn");else Message(MessageCategory.Narrative,"The vault holds classified containment documents. The module leaves its contents to the GM; this campaign does not create an unpriced advanced arsenal.");break;
   case "roof-negotiate":PacifyUpper26("The Last Survivor");LowerRecruit(a,s);break;
   case "roof-entry":if(n==8){SpawnChapterNamed(d,s,"Armory Security Shambler",s.BonusRoll+2);SpawnChapterNamed(d,s,"One-Armed Brute");ChangeTension(RollDamage(2,6),"Armory intrusion");}if(n==10)s.RoofTimers["eventAt"]=CampaignMinutes+10;break;
   case "roof-bury":s.Events.Add("remains-buried");break;
   case "roof-exit":ChangeTension(Roll(4),"A muffled wail beyond the sealed stairwell");s.Events.Add("stairwell-clue");break;
   case "roof-repair":RoofConsume(actor,Campaign!.Infrastructure!.FuelItemId);RoofConsume(actor,Campaign.Infrastructure.GeneratorCost[0].ItemId);s.Events.Add("airframe-repaired");SetDialogueFlag("flag.roof.helicopter-airframe-repaired");Message(MessageCategory.Narrative,FinaleEnabled?"The preliminary SkyEye 7 airframe is repaired. Use its flight console for inspection, remaining issues, pilot training and departure.":"The grounded SkyEye 7 airframe is repaired. Takeoff, pilot requirements and escape resolution belong to a later campaign phase.");break;
   case "roof-pilot":s.Events.Add("pilot-revived");s.RoofTimers["pilotAt"]=CampaignMinutes+s.RoofRolls["pilotDelay"];AddStoryJournalEntry("roof.pilot.testimony","The bitten news pilot","The pilot wakes long enough to describe the last broadcast. Their final infection is irreversible; turning follows within the saved one-to-six-minute deadline.","Roof");break;
   case "roof-mercy":s.Events.Add("pilot-ended");break;
   case "roof-pistol":actor.Inventory.Items.Single(i=>i.ItemId=="item.roof.12.loot.4").Wear=0;break;
   case "roof-adman":SpawnChapterNamed(d,s,"The Ad Man");break;
   case "roof-signal":var home=ShelterHomeStockpile()!;s.RoofSignalUnits=home.Contents.Items.SelectMany(StackResourceUnits).Where(SecuredResourceUnits.Contains).Distinct().ToList();CreditLower(s,"roof-signal",Roof28SignalCredit(d,s));SetDialogueFlag("flag.roof.strategic-signal");break;
  }
 }
 void Roof28Radio(LowerMallSite d,LowerMallState s,Actor actor)
 {
  string text=d.DisplayNumber==1?new[]{"Zeta Protocol compromised. Containment breach detected. Evacuation terminated.","It’s not the bites. It’s the air. Do not breathe the air near the vents!","Distorted Morse repeats coordinates leading toward an island.","We see you now. You should not be here."}[s.BonusRoll-1]:"The receiver repeats a saved fragment of military code and unfamiliar whispering.";
  AddStoryJournalEntry("radio."+d.Prefix,d.Name+" radio",text,"Roof");Message(MessageCategory.Narrative,text);if(d.DisplayNumber==10){int roll=s.BranchRoll;if(roll==4)ChangeTension(Roll(4),"Garbled military signal");if(roll==5)ChangeTension(Roll(6),"Whispering signal");if(roll==6){ChangeTension(RollDamage(2,6),"Signal draws attention");Roof28Reinforce(d,s,Roll(4));}}if(d.DisplayNumber==7)ChangeTension(Roll(6),"Shelter speaker broadcast");
 }
 bool Roof28Disadvantage(Actor attacker,Actor target)=>LowerTraits(target).Contains("roof-crawler")&&attacker.Range>1||CurrentLowerMallSite?.Number==211&&attacker.Range>1&&attacker.Position.Distance(target.Position)>6||LowerTraits(attacker).Contains("roof-shadow")&&(RoofDaylight||CurrentLowerMallSite is {} d&&LowerMallProgress(d.Number)?.Events.Contains("bright-light")==true);
 int Roof28Damage(Actor target,int amount,DamageType type,Actor? source)
 {
  var tags=LowerTraits(target);if(tags.Contains("roof-waterthing")&&type is DamageType.Fire or DamageType.Lightning)amount*=2;
  bool gun=source?.Equipment.Weapon?.Definition.MagazineSize>0&&source.Equipment.Weapon.Definition.AmmunitionType!="flare";
  if(gun&&tags.Contains("roof-officer"))amount/=2;
  if(gun&&tags.Contains("roof-vest")&&LowerMonsterState(target) is {} s&&!s.Events.Contains("vest:"+target.Id)){s.Events.Add("vest:"+target.Id);amount/=2;}
  if(source!=null&&LowerTraits(source).Contains("roof-rat")&&source.Hp*2<=source.MaxHp)amount/=2;
  return amount;
 }
 void Roof28DamageResolved(Actor target,int damage,DamageType type,Actor? source,bool wasAlive,bool critical)
 {
  var d=CurrentLowerMallSite;if(!Roof28Site(d))return;var s=LowerMallProgress(d!.Number)!;var tags=LowerTraits(target);
  if(damage>0&&type==DamageType.Fire)IgniteRoof28(source??target,false);
  if(damage>0&&tags.Contains("roof-vine")&&type==DamageType.Fire)s.Events.Add("vine-burned");
  if(damage>0&&tags.Contains("roof-subject")&&target.Alive&&target.Hp<=10)target.BaseAc=16;
  if(damage>0&&tags.Contains("roof-revolver")&&target.Hp is >0 and <5&&!s.Events.Contains("desperate-shot")){s.Events.Add("desperate-shot");var p=Party.Where(p=>p.Alive).OrderBy(p=>p.Position.Distance(target.Position)).FirstOrDefault();var ammo=target.Equipment.Weapon?.LoadedAmmunition;if(p!=null&&ammo?.Quantity>0){ammo.Quantity--;if(ammo.Quantity==0)target.Equipment.Weapon!.LoadedAmmunition=null;Hurt(p,8,DamageType.Piercing,target);}}
  if(source!=null&&damage>0&&target.Alive)
  {
   var st=LowerTraits(source);if(st.Contains("roof-brute")&&!SurvivorSave(target,Ability.Strength,14,"brute knockdown"))LowerEffect(target,"prone",6);if(st.Contains("roof-shadow")||st.Contains("roof-howarth"))LowerEffect(target,"book-disoriented",6);if(st.Contains("roof-technician"))LowerEffect(target,"roof-dex-shock",6);
   if(st.Contains("roof-rat")&&!SurvivorSave(target,Ability.Constitution,12,"infection rat bite"))LowerEffect(target,"roof-wasting",0);
   if(st.Contains("roof-ooze")&&target.Equipment.Weapon is {} w)w.Wear=Math.Min(100,w.Wear+10);
   if(source.Equipment.Weapon?.ItemId=="item.roof.08.loot.2"&&critical)LowerEffect(target,"stunned",6);
   if(source.Equipment.Weapon?.Definition.AmmunitionType=="flare")LowerEffect(target,"roof-burning",18);
  }
  if(wasAlive&&!target.Alive)
  {
   var def=Campaign!.LowerMall!.Monsters.FirstOrDefault(m=>m.ActorId==target.Id);if(def?.RewardXp>0)CreditLower(s,"defeat:"+target.Id,def.RewardXp);
   if(tags.Contains("roof-drowned")&&type is DamageType.Slashing or DamageType.Piercing)foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=2))if(!SurvivorSave(p,Ability.Constitution,13,"toxic sludge"))LowerEffect(p,"roof-nausea",3600);
   if(tags.Contains("roof-scorched"))foreach(var p in Actors.Where(p=>p!=target&&p.Alive&&p.Position.Distance(target.Position)<=2).ToArray())Hurt(p,SurvivorSave(p,Ability.Dexterity,13,"fireball")?RollDamage(2,6)/2:RollDamage(2,6),DamageType.Fire,target);
   if(tags.Contains("roof-vine"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=6))if(!SurvivorSave(p,Ability.Constitution,15,"infection death bloom"))LowerEffect(p,"roof-deathbloom",3600);
   if(tags.Contains("roof-clinger"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=1.5f))if(!SurvivorSave(p,Ability.Strength,14,"reflexive clinger"))LowerEffect(p,"restrained",6);
   if(tags.Contains("roof-adman"))Message(MessageCategory.Narrative,"Thank you for shopping with us. Your transaction is… incomplete.");
  }
 }
 void Roof28Turn(Actor actor,LowerMallState s,IReadOnlyList<string> tags)
 {
  var d=CurrentLowerMallSite;if(!Roof28Site(d))return;
  if(actor.Equipment.Weapon?.Definition.MagazineSize>0&&actor.Equipment.Weapon.LoadedAmmunition==null){actor.Equipment.Weapon=null;actor.Character.MainAttack.DamageDice=1;actor.Character.MainAttack.DamageDieSize=4;actor.Character.MainAttack.DamageType=DamageType.Slashing;}
  if(tags.Contains("roof-brute")&&actor.Hp<10)actor.Economy.Reset(actor.EffectiveSpeedFeet,2);
  if(tags.Contains("roof-brute")&&!s.Events.Contains("brute-roar")){s.Events.Add("brute-roar");ChangeTension(Roll(6),"Armory brute roar");}
  if(tags.Contains("roof-vine")&&RoofDaylight&&!s.Events.Contains("vine-burned"))actor.Hp=Math.Min(actor.MaxHp,actor.Hp+5);
  if(tags.Contains("roof-shelterrat")&&actor.Hp*2<=actor.MaxHp){actor.Hp=0;actor.Character.SetCondition("unconscious","Scattered",true);CheckEnd();}
  if(tags.Contains("roof-guard")&&actor.Hp<=10&&!s.Events.Contains("guard-revolver")){s.Events.Add("guard-revolver");actor.Equipment.Weapon=CreateItemStack("item.roof.enemy.guard-revolver");actor.Equipment.Weapon.LoadedAmmunition=CreateItemStack("item.mall.ammo-38",6);}
  if(tags.Contains("roof-vent")&&LowerMallProgress(204)?.Events.Contains("vents-sealed")!=true&&Round%2==0)actor.Character.SetCondition("disengaged","Vent retreat",true);
  if(tags.Contains("roof-shadow")){var p=Party.FirstOrDefault(p=>p.Alive);if(p!=null){var point=Floors.Where(c=>c.Distance(actor.Pos)<=2&&CanStand(c.Center,actor,true)).OrderBy(c=>c.Distance(p.Pos)).FirstOrDefault();if(IsFloor(point))actor.Pos=point;}}
  if(tags.Contains("roof-ooze")&&Round%3==0){var dead=Actors.FirstOrDefault(a=>a.Enemy&&!a.Alive&&a!=actor&&!s.Events.Contains("ooze:"+a.Id));if(dead!=null){s.Events.Add("ooze:"+dead.Id);dead.Hp=dead.MaxHp;dead.Character.SetCondition("unconscious","Unconscious",false);AddInitiativeParticipant(dead,reason:"ooze reanimation");}}
  if(tags.Contains("roof-screamer")&&Roll(6)>=5){ChangeTension(RollDamage(2,6),"Rooftop screamer");foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=12))if(!SurvivorSave(p,Ability.Constitution,15,"shriek"))LowerEffect(p,"stunned",6);actor.Economy.Spend(ActionResourceKind.Action);}
  if(tags.Contains("roof-drone")&&Roll(6)>=5){foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=2))if(!SurvivorSave(p,Ability.Constitution,14,"welding flash"))LowerEffect(p,"blinded",6);}
  if((tags.Contains("roof-waterthing")||tags.Contains("roof-adman")||tags.Contains("roof-elder")||tags.Contains("roof-vine"))&&Roll(6)>=5)
  {
   foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=(tags.Contains("roof-vine")?6:tags.Contains("roof-adman")?3:2)&&Line(p.Position,actor.Position)).ToArray())
   {if(tags.Contains("roof-vine")){if(!SurvivorSave(p,Ability.Constitution,13,"spore hallucinations"))LowerEffect(p,"book-disoriented",Roll(4)*6);}else if(tags.Contains("roof-adman")){if(!SurvivorSave(p,Ability.Constitution,13,"sonic advert"))Hurt(p,Roll(8),DamageType.Psychic,actor);if(!SurvivorSave(p,Ability.Dexterity,13,"neon burst"))LowerEffect(p,"blinded",6);}else{int damage=tags.Contains("roof-waterthing")?RollDamage(2,6):Roll(8);Hurt(p,SurvivorSave(p,Ability.Dexterity,14,"acid cone")?damage/2:damage,tags.Contains("roof-waterthing")?DamageType.Acid:DamageType.Psychic,actor);}}
   actor.Economy.Spend(ActionResourceKind.Action);
  }
  if(s.Events.Contains("loose-ammo")&&Roll(6)==1){var p=Actors.FirstOrDefault(p=>p.Alive&&p.Position.Distance(actor.Position)<=2);if(p!=null)Hurt(p,Roll(4),DamageType.Piercing);}
 }
 void Roof28PartyTurn(Actor actor){if(actor.Character.HasCondition("roof-burning"))Hurt(actor,Roll(6),DamageType.Fire);}
 void Roof28Noise(Actor source,string kind)
 {
  var d=CurrentLowerMallSite;if(!Roof28Site(d))return;var s=LowerMallProgress(d!.Number);if(s==null)return;s.Quiet=false;if(d.DisplayNumber==11){int roll=Roll(6);if(roll is 4 or 5)SpawnChapterNamed(d,s,"Zombie Climber",Math.Min(4,1+(s.Releases.FirstOrDefault(r=>r.EncounterId.EndsWith("zombie-climber"))?.Count??0)));if(roll==6){SpawnChapterNamed(d,s,"Rooftop Screamer");ChangeTension(RollDamage(2,6),"Noise spots a rooftop screamer");}}
 }
 void Roof28Gunshot(Actor actor)
 {
  var d=CurrentLowerMallSite;if(!Roof28Site(d))return;var s=LowerMallProgress(d!.Number)!;bool flare=actor.Equipment.Weapon?.Definition.AmmunitionType=="flare";ChangeTension(flare?(d.DisplayNumber==11?RollDamage(3,6):10):RollDamage(d.DisplayNumber is 7 or 8?3:2,6),flare?"Rooftop flare":"Rooftop gunfire");IgniteRoof28(actor,true);
 }
 void IgniteRoof28(Actor source,bool gun)
 {
  var d=CurrentLowerMallSite;if(!Roof28Site(d))return;var s=LowerMallProgress(d!.Number)!;if(!s.Events.Contains("gas-active")||s.Events.Contains("roof-ignition"))return;if(gun&&d.DisplayNumber==6&&Roll(20)>5)return;s.Events.Add("roof-ignition");s.Events.Remove("gas-active");foreach(var a in Actors.Where(a=>a.Alive&&(d.DisplayNumber==6||a.Position.Distance(source.Position)<=2)).ToArray())Hurt(a,RollDamage(d.DisplayNumber==6?4:3,6),DamageType.Fire);ChangeTension(RollDamage(3,6),"Rooftop fuel ignition");
 }
 void TickRoof28()
 {
  if(Campaign?.SchemaVersion<26||!LowerMallEnabled)return;
  foreach(var s in LowerMallStates.Where(s=>s.Number is >=201 and <=213))
  {
   int day=CampaignMinutes/1440,had=s.RoofTimers.GetValueOrDefault("dayAt",day);if(day>had){s.RoofTimers["dayAt"]=day;if(s.Number==204&&!s.Secured)ChangeTension(RollDamage(Math.Min(5,day-had),6),"Unsecured HVAC day");}
   if(s.Number==211&&s.Events.Contains("pilot-revived")&&!s.Events.Contains("pilot-turned")&&CampaignMinutes>=s.RoofTimers.GetValueOrDefault("pilotAt",int.MaxValue)){s.Events.Add("pilot-turned");AddStoryJournalEntry("roof.pilot.turned","The pilot turns","The pilot’s final infection reaches its saved deadline. Returning to the helipad reveals the turned runner.","Roof");}
  }
  var d=CurrentLowerMallSite;if(!Roof28Site(d))return;var state=LowerMallProgress(d!.Number);if(state==null)return;int n=d.DisplayNumber;
  if(state.RoofTimers.TryGetValue("alarmAt",out int alarm)&&CampaignMinutes>=alarm){state.RoofTimers.Remove("alarmAt");Roof28Reinforce(d,state,Roll(4));}
  if(state.RoofTimers.TryGetValue("doorAt",out int door)&&CampaignMinutes>=door){state.RoofTimers.Remove("doorAt");if(!RoofGridOnline)state.Events.Add("door-locked");}
  if(n==7&&state.Events.Contains("oxygen-active")&&CampaignMinutes>=state.RoofTimers.GetValueOrDefault("oxygenAt",int.MaxValue)&&!state.Events.Contains("oxygen-penalty")){state.Events.Add("oxygen-penalty");foreach(var p in Party.Where(p=>p.Alive))p.Needs.Exhaustion=Math.Min(6,p.Needs.Exhaustion+1);}
  if(n==7&&state.RoofTimers.TryGetValue("ratsAt",out int rats)&&CampaignMinutes>=rats){state.RoofTimers.Remove("ratsAt");SpawnChapterNamed(d,state,"Shelter Rat Swarm");}
  if(n==11&&state.Events.Contains("pilot-turned")&&!state.Events.Contains("pilot-released")){SpawnChapterNamed(d,state,"Turned Pilot");state.Events.Add("pilot-released");}
  if(n==1&&state.Events.Contains("cameras-on")&&RoofGridOnline&&CampaignMinutes>=state.RoofTimers.GetValueOrDefault("cameraAt",int.MaxValue)){state.RoofTimers["cameraAt"]=CampaignMinutes+60;ChangeTension(Roll(4),"Active roof camera lighting");}
  if(CampaignMinutes<state.RoofTimers.GetValueOrDefault("eventAt",int.MaxValue))return;state.RoofTimers["eventAt"]=CampaignMinutes+10;
  if(n==10&&!state.Events.Contains("linger-resolved")&&state.Resolved.Contains("action.roof.10.device.entry")){state.Events.Add("linger-resolved");if(state.RoofRolls["event"]==2||state.RoofRolls["linger"]==6)state.Events.Add("door-locked");if(state.RoofRolls["approach"]<=3){int e=state.RoofRolls["linger"];if(e==3)SpawnChapterNamed(d,state,"Panic Room Crawler",Roll(4));if(e==4)SpawnChapterNamed(d,state,"Panic Room Survivor");if(e==5)SpawnChapterNamed(d,state,"Failed Subject");}}
  if(n==9&&!state.Events.Contains("remains-buried")&&Actors.Any(a=>a.Enemy&&!a.Alive)&&CampaignMinutes-state.EnteredMinute>=60&&!state.Events.Contains("decay-tension")){state.Events.Add("decay-tension");ChangeTension(1,"Unburied greenhouse remains");}
 }
 void SecureRoof28(LowerMallSite d,LowerMallState s)
 {
  if(!Roof28Site(d))return;if(d.DisplayNumber==4)Roof28Milestone(d,s,"clear");if(d.DisplayNumber==5&&s.Releases.Any(r=>r.EncounterId.EndsWith("thing-in-the-water")))Roof28Milestone(d,s,"clear");
  if(LowerMallStates.Count(s=>s.Number is >=201 and <=207&&s.Secured)==7)RunCampaignEvent("event.roof.first-seven");if(LowerMallStates.Count(s=>s.Number is >=208 and <=213&&s.Secured)==6)RunCampaignEvent("event.roof.eight-thirteen");
 }
 int Roof28SignalCredit(LowerMallSite d,LowerMallState s)
 {
  if(d.Number!=213||!s.Resolved.Contains("action.roof.13.device.signal")||!s.Secured||s.RoofSignalUnits.Count==0||s.RoofSignalUnits.Distinct().Count()!=s.RoofSignalUnits.Count||s.RoofSignalUnits.Any(id=>!SecuredResourceUnits.Contains(id)))throw new InvalidDataException("Invalid stored-SV signal snapshot.");var catalogue=ResourceCatalogue();return s.RoofSignalUnits.Sum(id=>catalogue[id].Sv)/10;
 }
 void ValidateRoof28State(LowerMallSite d,LowerMallState s)
 {
  if(s.RoofTimers==null||s.RoofRolls==null||s.RoofSignalUnits==null)throw new InvalidDataException("Missing roof progress maps.");
  if(Roof29Site(d)){ValidateRoof29State(d,s);return;}
  if(!Roof28Site(d)){if(s.RoofTimers.Count+s.RoofRolls.Count+s.RoofSignalUnits.Count>0)throw new InvalidDataException("Foreign roof progress.");return;}
  string[] timers=["dayAt","eventAt","batteryUntil","cameraAt","alarmAt","lockUntil","oxygenAt","doorAt","ratsAt","fuelUntil","pumpDay","pilotAt"];
  if(s.BranchRoll is <1 or >6||s.BonusRoll is <1 or >4||s.RoofTimers.Count>16||s.RoofTimers.Any(k=>!timers.Contains(k.Key)||k.Value<0)||s.RoofRolls.Count>8||s.RoofSignalUnits.Count>100000)throw new InvalidDataException("Invalid roof timing/roll bounds.");
  if(s.RoofTimers.ContainsKey("fuelUntil")&&d.Number!=206||s.RoofTimers.ContainsKey("pumpDay")&&d.Number!=205||s.RoofTimers.ContainsKey("pilotAt")&&d.Number!=211||s.RoofSignalUnits.Count>0&&d.Number!=213)throw new InvalidDataException("Foreign rooftop timer ownership.");
  if(d.Number==201){if(s.RoofRolls.Count!=3||s.RoofRolls.Keys.Any(k=>!new[]{"feed1","feed2","feed3"}.Contains(k))||s.RoofRolls.Values.Distinct().Count()!=3||s.RoofRolls.Values.Any(v=>!Campaign!.LowerMall!.Sites.Any(d=>d.Number==v&&d.Level!="roof")))throw new InvalidDataException("Invalid saved surveillance feeds.");}
  else if(d.Number==202){if(s.RoofRolls.Count!=1||s.RoofRolls.GetValueOrDefault("touch") is <1 or >20)throw new InvalidDataException("Invalid corner figure roll.");}
  else if(d.Number==210){if(s.RoofRolls.Count!=3||s.RoofRolls.GetValueOrDefault("event") is <1 or >4||s.RoofRolls.GetValueOrDefault("linger") is <1 or >6||s.RoofRolls.GetValueOrDefault("approach") is <1 or >10)throw new InvalidDataException("Invalid panic-room rolls.");}
  else if(d.Number==211){if(s.RoofRolls.Count!=1||s.RoofRolls.GetValueOrDefault("pilotDelay") is <1 or >6||s.Events.Contains("pilot-revived")!=s.Resolved.Contains("action.roof.11.device.pilot")||s.Events.Contains("pilot-ended")&&s.Events.Contains("pilot-revived")||s.Events.Contains("pilot-turned")&&(!s.Events.Contains("pilot-revived")||CampaignMinutes<s.RoofTimers.GetValueOrDefault("pilotAt",int.MaxValue)))throw new InvalidDataException("Invalid pilot fate or deadline.");}
  else if(s.RoofRolls.Count>0)throw new InvalidDataException("Foreign rooftop branch map.");
  if(s.Credits.Contains("roof-signal"))Roof28SignalCredit(d,s);
 }
 void Roof28LongRest(Actor a){if(a.Character.HasCondition("roof-wasting")){a.MaxHp=Math.Max(1,a.MaxHp-Roll(4));a.Hp=Math.Min(a.Hp,a.MaxHp);}}
 string Roof28ItemReason(Actor actor,ItemStack s)=>s.Definition.Equipment?.Family=="roof-purify"&&!RoofMaterial(actor,Campaign!.Infrastructure!.DirtyWaterItemId)?"Carry an untreated water portion to purify.":"";
 void UseRoof28Item(Actor a,ItemStack stack,string effect)
 {
  switch(effect)
  {
   case "roof-radio":EmitLocalNoise(a,"door",60);break;
   case "roof-jitters":a.Hp=Math.Min(a.MaxHp,a.Hp+1);LowerEffect(a,"roof29-jitters",600);break;
   case "roof-energy":a.Needs.Exhaustion=Math.Max(0,a.Needs.Exhaustion-1);break;
   case "roof-medkit":a.Hp=Math.Min(a.MaxHp,a.Hp+RollDamage(2,6));a.Character.SetCondition("book-infected","Infection",false);a.Character.SetCondition("roof-wasting","Wasting infection",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition is "book-infected" or "roof-wasting");break;
   case "roof-morale":LowerEffect(a,"upper-morale",Roll(4)*3600);break;
   case "roof-flash":foreach(var target in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(a.Position)<=3&&Line(t.Position,a.Position)))LowerEffect(target,"blinded",6);EmitLocalNoise(a,"explosion",100);break;
   case "roof-purify":RoofConsume(a,Campaign!.Infrastructure!.DirtyWaterItemId);a.Inventory.Add(CreateItemStack(Campaign.Infrastructure.CleanWaterItemId));break;
  }
 }
}
