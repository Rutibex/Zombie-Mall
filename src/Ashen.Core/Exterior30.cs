using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed partial class Actor
{
 int Exterior30Skill(Skill skill)=>(skill==Skill.Athletics&&Inventory.Items.Any(i=>i.ItemId=="item.exterior.06.loot.0"&&i.Wear<100)?2:0)-(SkillRules.AbilityFor(skill)==Ability.Dexterity&&Character.HasCondition("ext30-frostbite")?1:0)-(skill==Skill.Perception&&Character.HasCondition("ext30-beer")?2:0);
}
public sealed partial class World
{
 static bool Exterior30Site(LowerMallSite? d)=>d?.Level=="exterior"&&d.DisplayNumber is >=1 and <=13;
 double ExteriorClock=>CampaignMinutes*60d+CampaignSeconds;
 [JsonIgnore] public string Exterior30Status=>CurrentLowerMallSite is {} d&&Exterior30Site(d)&&LowerMallProgress(d.Number) is {} s?$"{(s.Secured?"SECURED":"UNSECURED")} | ALARM WAVES: {s.ExteriorRolls.GetValueOrDefault("waves")} | "+(d.DisplayNumber==13?$"PUMP REFILLS: {5-s.ExteriorRolls.GetValueOrDefault("pumped")}/5":$"VEHICLE: {(HasDialogueFlag("flag.exterior.vehicle-ready")?"READY":"UNSECURED")}"):"";
 bool ExteriorOwn(Actor a,int n,int j)=>a.Inventory.Items.Concat(a.Equipment.All()).Any(i=>i.ItemId==$"item.exterior.{n:00}.loot.{j}"&&i.Wear<100);
 void ConfigureExterior30Container(Feature f)
 {
  if(f.Id=="feature.exterior.13.pump-reserve"){f.Locked=true;f.Hidden=true;}
  if(f.Id=="feature.exterior.05.loot.0")f.Contents.Items.Single().Wear=100;
  if(f.Id=="feature.exterior.07.loot.1")f.Contents.Items.Single().Charges=0;
 }
 void ExteriorTrim(Feature f,int quantity)
 {
  foreach(var i in f.Contents.Items){i.Quantity=quantity;i.ResourceUnits=i.ResourceUnits.Take(quantity).ToList();}
 }
 void EnterExterior30(LowerMallSite d,LowerMallState s)
 {
  if(!Exterior30Site(d))return;int n=d.DisplayNumber;s.BranchRoll=Roll(6);s.BonusRoll=Roll(4);s.ExteriorRolls["event"]=Roll(6);s.ExteriorRolls["special"]=Roll(6);s.ExteriorRolls["freezerDelay"]=Roll(6);s.ExteriorTimers["eventAt"]=ExteriorClock+1800;
  if(n<=3)for(int j=0;j<3;j++)
  {
   int type=Roll(6);s.ExteriorRolls["car"+j]=type;s.ExteriorRolls["alarm"+j]=Roll(100);var f=Features.Single(f=>f.Id==$"feature.exterior.{n:00}.car.{j}");string prefix=$"item.exterior.{n:00}.car.{j}.type.{type}.";f.Contents.Items.RemoveAll(i=>!i.ItemId.StartsWith(prefix));
   if(n==3&&type==3)f.Contents.Items.RemoveAll(i=>!i.ItemId.EndsWith(s.BonusRoll<=2?".0":".1"));
   if(n==3&&type==2)ExteriorTrim(f,RollDamage(2,6));ConfigureRoof28Container(f);
   if(n==2&&type==2&&f.Contents.Items.FirstOrDefault(i=>i.LoadedAmmunition!=null)?.LoadedAmmunition is {} ammo){int q=Roll(6);ammo.Quantity=q;ammo.ResourceUnits=ammo.ResourceUnits.Take(q).ToList();}
  }
  if(n==3){int keep=s.BranchRoll<=2?5:s.BranchRoll<=4?2:s.BranchRoll==5?3:4;foreach(int j in new[]{2,3,4,5})if(j!=keep)Features.Single(f=>f.Id==$"feature.exterior.03.loot.{j}").Contents.Items.Clear();}
  if(n==4){ExteriorTrim(Features.Single(f=>f.Id=="feature.exterior.04.loot.1"),RollDamage(2,10));if(s.BranchRoll is not (3 or 4))foreach(int j in new[]{7,8})Features.Single(f=>f.Id==$"feature.exterior.04.loot.{j}").Contents.Items.Clear();}
  if(n==6&&s.BranchRoll<=3)Features.Single(f=>f.Id=="feature.exterior.06.loot.4").Contents.Items.Clear();
  if(n==8){int count=Roll(3);s.ExteriorRolls["lockbox"]=count;foreach(int j in new[]{3,4,5,6})if(j>2+count)Features.Single(f=>f.Id==$"feature.exterior.08.loot.{j}").Contents.Items.Clear();if(s.BranchRoll<=3)Features.Single(f=>f.Id=="feature.exterior.08.loot.2").Contents.Items.Clear();s.ExteriorTimers["benchAt"]=ExteriorClock+Roll(4)*6;}
  if(n==10){if(s.BonusRoll!=2)Features.Single(f=>f.Id=="feature.exterior.10.loot.7").Contents.Items.Clear();if(s.BonusRoll!=4)Features.Single(f=>f.Id=="feature.exterior.10.loot.8").Contents.Items.Clear();}
  if(n==11&&s.HazardId!="action.exterior.11.hazard.2")Features.Single(f=>f.Id=="feature.exterior.11.loot.7").Contents.Items.Clear();
  if(n==2&&s.HazardId.EndsWith(".0")||n==5&&s.HazardId.EndsWith(".5")||n==9&&s.HazardId.EndsWith(".2")||n==10&&s.HazardId.EndsWith(".1")||n==13)s.Events.Add("gas-active");
  if(n==3&&s.EncounterRoll==5||n==4&&s.EncounterRoll==6||n==5&&s.ExteriorRolls["special"]==3||n==8&&s.HazardId.EndsWith(".0")||n==13&&s.EncounterRoll==5&&s.BranchRoll<=3)s.Events.Add("exterior-survivor");
  if(n==13&&s.EncounterRoll==5&&s.BranchRoll>3)SpawnChapterNamed(d,s,"Paranoid Fuel Scavenger");
  if(n==9)SpawnChapterNamed(d,s,"Burger Crawler");if(n==12)SpawnChapterNamed(d,s,"Grease Ghoul",Roll(3));
 }
 void ConfigureExterior30Actors(IEnumerable<string> ids)
 {
  foreach(var id in ids)
  {
   var a=Actors.Single(a=>a.Id==id);var tags=LowerTraits(a);string key=tags.Contains("ext30-pistol")?"pistol":tags.Contains("ext30-shotgun")?"shotgun":"";
   if(key!=""){a.Equipment.Weapon=CreateItemStack("item.exterior.enemy."+key);a.Equipment.Weapon.LoadedAmmunition=CreateItemStack(key=="pistol"?"item.mall.ammo-9mm":"item.mall.ammo-shells",key=="pistol"?6:2);}
  }
 }
 string Exterior30Reason(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Exterior30Site(d))return "";int n=d.DisplayNumber;string e=a.Effect;
  if(a.NpcActorId!=""&&!s.Events.Contains("exterior-survivor"))return "No living recruit was found on this saved source branch.";
  if(e=="ext30-vehicle"&&(!s.Resolved.Any(id=>id.StartsWith($"action.{d.Prefix}.car."))||!RoofTools(actor)||!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)))return "Search a vehicle, carry usable tools and supply one real fuel unit.";
  if(n==1&&e=="ext30-vehicle"&&!Enumerable.Range(0,3).Any(j=>s.ExteriorRolls["car"+j]!=1&&s.Resolved.Contains($"action.{d.Prefix}.car.{j}")))return "Search an actual combustion-engine vehicle; a depleted electric car cannot use gasoline.";
  if(n==3&&e=="ext30-vehicle"&&!Enumerable.Range(0,3).Any(j=>s.ExteriorRolls["car"+j]==1&&s.Resolved.Contains($"action.{d.Prefix}.car.{j}")))return "None of the three saved vehicle rolls revealed a working sedan.";
  if(e=="ext30-survey"&&s.Resolved.Count(id=>id.StartsWith($"action.{d.Prefix}.car."))<3)return "Finish searching all three physical vehicles.";
  if(e=="ext30-siren"&&(!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)||!s.ExteriorRolls.Any(k=>k.Key.StartsWith("car")&&k.Value==6)))return "Find a police cruiser and carry usable fuel before powering its siren.";
  if(n==3&&e=="ext30-door"&&!ExteriorOwn(actor,3,6))return "Carry the actual employee entrance keycard.";
  if(n==4&&e=="ext30-vault"&&s.BranchRoll<=2&&!actor.Inventory.Items.Any(i=>i.Definition.Name.Contains("Crowbar")))return "These jammed vault doors require a carried crowbar.";
  if(e=="ext30-forklift"&&(!ExteriorOwn(actor,4,4)||!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)))return "Recover the forklift keys and carry one compatible fuel unit.";
  if(n==4&&e=="ext30-briefcase"&&s.EncounterRoll!=5)return "No Delivery Man appeared with a briefcase on this saved branch.";
  if(n==5&&e=="ext30-repair"&&(!actor.Inventory.Items.Any(i=>i.ItemId=="item.exterior.05.loot.0"&&i.Broken)||!RoofTools(actor)||!RoofMaterial(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId)))return "Carry the broken police shotgun, tools and one repair-material unit.";
  if(n==5&&e=="ext30-logs"&&!ExteriorOwn(actor,5,4)||n==5&&e=="ext30-message"&&!ExteriorOwn(actor,5,5))return "Carry the recovered security log or last message.";
  if(n==5&&e=="ext30-storage"&&!actor.Inventory.Items.Any(i=>i.ItemId=="item.roof.21.loot.7"))return "Carry the Parking Garage Keycard recovered at the skybridge.";
  if(n==6&&e=="ext30-medical"&&!actor.Inventory.Items.Any(i=>i.Definition.Name.Contains("Pry Bar")||i.Definition.Name.Contains("Crowbar")))return "Carry a real pry bar or crowbar to open the sealed medical crate.";
  if(e=="ext30-lift"&&(!RoofTools(actor)||!RoofGridOnline))return "Carry usable tools and restore the live rooftop power grid.";
  if(n==7&&e=="ext30-repair"&&(!actor.Inventory.Items.Any(i=>i.ItemId=="item.exterior.07.loot.1"&&i.Charges==0)||!actor.ClassIs("smart")&&!RoofMaterial(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId)))return "Carry the broken flashlight; use a Smart survivor or one real repair-material unit.";
  if(n==8&&e=="ext30-terry"&&!actor.Inventory.Items.Any(i=>i.Definition.Name.Contains("Cigarette")||i.Definition.Equipment?.Family=="ext30-beer"))return "Carry actual cigarettes or beer for Terry's bribe.";
  if(n==8&&e=="ext30-bench"&&s.Events.Contains("bench-turned"))return "The bench corpse already reanimated.";
  if(n==10&&e=="ext30-locker"&&!ExteriorOwn(actor,10,6))return "Carry the recovered employee locker key.";
  if(n==10&&e=="ext30-invoice"&&!ExteriorOwn(actor,10,2))return "Carry the actual BioDyne supplier clipboard.";
  if(e=="ext30-close"&&!s.Events.Contains("freezer-open"))return "The freezer is already closed.";
  if(n==11&&e=="ext30-storage"&&!ExteriorOwn(actor,11,5))return "Carry the recovered employee keycard.";
  if(e=="ext30-quiet"&&(!s.Quiet||n==11&&!s.Secured||n==12&&s.Resolved.Contains("action.exterior.12.device.freezer")))return "This quiet bonus requires the corresponding quiet clear or undisturbed freezer.";
  if(e=="ext30-fryer"&&(!RoofGridOnline&&!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)||!Actors.Any(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=2)))return "Supply live grid power or a fuel unit and bring a hostile within ten feet.";
  if(e=="ext30-pump")
  {
   if(s.Events.Contains("ext30-explosion"))return "The fuel reserve was destroyed by the explosion.";
   if(!actor.Inventory.Contains("item.exterior.13.loot.6"))return "Carry an empty siphoning fuel can.";
   if(s.ExteriorRolls.GetValueOrDefault("pumped")>=5)return "The five-container reserve is depleted.";
   if(!actor.Inventory.CanAdd(new(){ItemId="item.exterior.13.pump-fuel",CampaignGear=ResolveItemDefinition("item.exterior.13.pump-fuel")}))return "Make backpack space for a filled fuel container.";
  }
  if(e=="ext30-patrol"&&(!Actors.Any(t=>t.Alive&&t.Name.StartsWith("Military Fuel Patrol"))||!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)))return "Find a living fuel patrol and carry one real fuel unit to trade.";
  return "";
 }
 void Exterior30Attempt(LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Exterior30Site(d)||a.Effect!="ext30-car")return;string key=a.Id.Split('.').Last();if(!s.Events.Contains("car-opened:"+key))
  {s.Events.Add("car-opened:"+key);if(s.ExteriorRolls["alarm"+key]<=(d.DisplayNumber==1?25:d.DisplayNumber==2?50:30))ExteriorAlarm(d,s,3);}
 }
 void ExteriorAlarm(LowerMallSite d,LowerMallState s,int waves)
 {
  ChangeTension(Roll(6),"Exterior vehicle alarm");s.ExteriorRolls["waves"]=Math.Min(9,s.ExteriorRolls.GetValueOrDefault("waves")+waves);s.ExteriorTimers["alarmAt"]=ExteriorClock+6;s.Quiet=false;
 }
 void ExteriorReinforce(LowerMallSite d,LowerMallState s,int count)
 {
  var e=Campaign!.Encounters.First(e=>e.AreaId==d.AreaId&&e.ActorIds.Any(id=>Campaign.Actors.Single(a=>a.Id==id).Role=="Shambler"));int had=s.Releases.FirstOrDefault(r=>r.EncounterId==e.Id)?.Count??0;SpawnLower(d,s,e.Id,had+count,"reaction");
 }
 void Exterior30Hazard(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a,bool pass)
 {
  if(!Exterior30Site(d)||pass)return;int n=d.DisplayNumber,j=int.Parse(a.Id.Split('.').Last());
  if(n==1&&j is 1 or 3||n==2&&j==1||n==3&&j==2)ExteriorAlarm(d,s,3);
  if(n==3&&j==1)ExteriorReinforce(d,s,RollDamage(2,6));if(n==4&&j==2)s.ExteriorTimers["arrivalAt"]=ExteriorClock+300;
  if(n==5&&j==4)SpawnChapterNamed(d,s,"Silent Garage Stalker");if(n==6&&j==0)ExteriorReinforce(d,s,Roll(4));
  if(n==7&&j==1)SpawnChapterNamed(d,s,"Diseased Rat Swarm");if(n==8&&j==1)SpawnChapterNamed(d,s,"Smoking Area Crawler");
  if(n==9&&j==4)SpawnChapterNamed(d,s,"Diseased Rat Swarm");if(n==10&&j==3)SpawnChapterNamed(d,s,"Deep Fryer Rats",Roll(6));
  if(n==11&&j==0)ExteriorReinforce(d,s,Roll(4));if(n==13&&j==3)SpawnChapterNamed(d,s,"Flammable Zombie");
 }
 void Exterior30Device(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Exterior30Site(d))return;int n=d.DisplayNumber;string e=a.Effect;
  if(e=="ext30-car")
  {
   int car=int.Parse(a.Id.Split('.').Last()),type=s.ExteriorRolls["car"+car];Message(MessageCategory.Narrative,$"Saved vehicle {car+1}: source table result {type}.");
   if(n==1&&type==6||n==2&&type==4)SpawnChapterNamed(d,s,"Trapped Car Zombie");if(n==3&&type==4)ExteriorReinforce(d,s,1);
  }
  switch(e)
  {
   case "ext30-vehicle":RoofConsume(actor,Campaign!.Infrastructure!.FuelItemId);SetDialogueFlag("flag.exterior.vehicle-ready");s.Events.Add("vehicle-ready");break;
   case "ext30-siren":RoofConsume(actor,Campaign!.Infrastructure!.FuelItemId);ChangeTension(RollDamage(3,6),"Powered police siren");ExteriorReinforce(d,s,RollDamage(2,6));break;
   case "ext30-light":s.Events.Add("traffic-light-cleared");break;
   case "ext30-barricade":ChangeTension(-Roll(4),"Overflow lot barricaded");break;
   case "ext30-herd":s.Events.Add("herd-contained");break;
   case "ext30-door":SetDialogueFlag("flag."+d.Prefix+".door");break;
   case "ext30-forklift":RoofConsume(actor,Campaign!.Infrastructure!.FuelItemId);SetDialogueFlag("flag.exterior.forklift-secured");ChangeTension(Roll(4),"Forklift moved to the loading docks");break;
   case "ext30-vault":if(s.BranchRoll==5)SpawnChapterNamed(d,s,"Armored Truck Mutant");if(s.BranchRoll==6&&!s.Events.Contains("exterior-survivor"))s.Events.Add("exterior-survivor");break;
   case "ext30-repair":if(n==5){RoofConsume(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId);actor.Inventory.Items.Single(i=>i.ItemId=="item.exterior.05.loot.0").Wear=0;}else {if(!actor.ClassIs("smart"))RoofConsume(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId);actor.Inventory.Items.Single(i=>i.ItemId=="item.exterior.07.loot.1").Charges=1;}break;
   case "ext30-watcher":s.ExteriorTimers["arrivalAt"]=ExteriorClock+600;AddStoryJournalEntry(a.Id,"The Watcher's warning","The intercom grants ten minutes before an approaching finite group. The Watcher's identity remains unresolved.","Exterior");break;
   case "ext30-helipad":SpawnChapterNamed(d,s,"Helicopter Corpse");break;
   case "ext30-storage":s.Events.Add("storage-open");SetDialogueFlag("flag."+d.Prefix+".storage");break;
   case "ext30-trailer":if(s.BranchRoll<=3)ExteriorReinforce(d,s,Roll(4));break;
   case "ext30-lift":s.Events.Add("lift-circuit-restored");SetDialogueFlag("flag.exterior.06.lift");Message(MessageCategory.Narrative,"The cargo power circuit is restored. The book's unspecified basement remains a clue, not an invented region.");break;
   case "ext30-special":if(s.ExteriorRolls["special"]==1)SpawnChapterNamed(d,s,"Trash Pile Mimic");if(s.ExteriorRolls["special"]==2&&!s.Events.Contains("exterior-survivor"))s.Events.Add("exterior-survivor");break;
   case "ext30-terry":var bribe=actor.Inventory.Items.First(i=>i.Definition.Name.Contains("Cigarette")||i.Definition.Equipment?.Family=="ext30-beer");RoofConsume(actor,bribe.ItemId);ChangeTension(-Roll(6),"Terry trusts the expedition");goto case "ext30-rescue";
   case "ext30-rescue":LowerRecruit(a,s);s.Events.Remove("exterior-survivor");if(n==3&&s.BranchRoll<=2&&Actors.FirstOrDefault(p=>p.Id==a.NpcActorId) is {} recruit)LowerEffect(recruit,"book-infected",86400);break;
   case "ext30-bench":s.Events.Add("bench-searched");s.ExteriorTimers.Remove("benchAt");break;
   case "ext30-freezer":
    if(n==9){SpawnChapterNamed(d,s,"Frozen Burger Horror");if(Roll(2)==1)ChangeTension(RollDamage(2,6),"Crashing Burger Prince freezer");}
    if(n==10){s.Events.Add("freezer-open");s.ExteriorTimers["freezerAt"]=ExteriorClock+s.ExteriorRolls["freezerDelay"]*60;if(s.BranchRoll<=3)SpawnChapterNamed(d,s,"Frozen Taco Walker");}
    if(n==12){SpawnChapterNamed(d,s,"Freezer Crawler",Roll(6));ChangeTension(RollDamage(2,6),"Chicken freezer ambush");}break;
   case "ext30-close":s.Events.Remove("freezer-open");s.ExteriorTimers.Remove("freezerAt");break;
   case "ext30-fryer":if(!RoofGridOnline)RoofConsume(actor,Campaign!.Infrastructure!.FuelItemId);foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=2).ToArray())Hurt(t,RollDamage(2,8),DamageType.Fire,actor);break;
   case "ext30-leak":s.Events.Remove("gas-active");s.Events.Add("leak-isolated");break;
   case "ext30-speaker":ChangeTension(Roll(4),"Exterior drive-thru speaker");AddStoryJournalEntry(a.Id,"The drive-thru response",new[]{"Static answers.","Static answers.","Whispers speak of hunger beneath.","Whispers speak of hunger beneath.","You are food. You will feed it.","Come to the basement. The feast awaits."}[s.ExteriorRolls["special"]-1],"Exterior");break;
   case "ext30-soda":foreach(var t in Actors.Where(t=>t.Alive&&t.Position.Distance(actor.Position)<=2))LowerEffect(t,"prone",6);break;
   case "ext30-pump":var reserve=Features.Single(f=>f.Id=="feature.exterior.13.pump-reserve").Contents.Items.Single();var filled=reserve.Clone();filled.InstanceId="";filled.Quantity=1;filled.ResourceUnits=reserve.ResourceUnits.Take(1).ToList();reserve.ResourceUnits=reserve.ResourceUnits.Skip(1).ToList();if(--reserve.Quantity==0)Features.Single(f=>f.Id=="feature.exterior.13.pump-reserve").Contents.Items.Clear();RoofConsume(actor,"item.exterior.13.loot.6");actor.Inventory.Add(filled);s.ExteriorRolls["pumped"]=s.ExteriorRolls.GetValueOrDefault("pumped")+1;ChangeTension(1,"Quiet finite fuel siphoning");if(Roll(4)==1)ExteriorReinforce(d,s,Roll(4));break;
   case "ext30-patrol":RoofConsume(actor,Campaign!.Infrastructure!.FuelItemId);foreach(var t in Actors.Where(t=>t.Alive&&t.Name.StartsWith("Military Fuel Patrol"))){t.Hp=0;t.Character.SetCondition("unconscious","Departed after fuel trade",true);}s.Events.Add("patrol-traded");CheckEnd();break;
   case "ext30-quiet":ChangeTension(-Roll(4),"Quiet exterior sweep");break;
   case "ext30-clue":case "ext30-logs":case "ext30-message":case "ext30-invoice":case "ext30-radio":case "ext30-briefcase":SetDialogueFlag("flag."+d.Prefix+"."+e[6..]);break;
  }
 }
 void TickExterior30()
 {
  TickExterior31();
  if(Campaign?.SchemaVersion<28||!LowerMallEnabled)return;
  // Saved revival clocks run offscreen without moving the corpse to the wrong map.
  foreach(var s in LowerMallStates.Where(s=>s.Number is >=301 and <=313))foreach(var timer in s.ExteriorTimers.Where(k=>k.Key.StartsWith("revive:")&&k.Value<=ExteriorClock).ToArray())
  {s.ExteriorTimers.Remove(timer.Key);var enemy=Actors.Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).FirstOrDefault(a=>a.Id==timer.Key[7..]);if(enemy!=null&&!s.Events.Contains("burn:"+enemy.Id)){enemy.Hp=1;enemy.Character.SetCondition("unconscious","Unconscious",false);}}
  var d=CurrentLowerMallSite;if(!Exterior30Site(d))return;var state=LowerMallProgress(d!.Number);if(state==null)return;int n=d.DisplayNumber;
  if(state.ExteriorRolls.GetValueOrDefault("waves")>0&&ExteriorClock>=state.ExteriorTimers.GetValueOrDefault("alarmAt",double.MaxValue)){state.ExteriorRolls["waves"]--;state.ExteriorTimers["alarmAt"]=ExteriorClock+6;ExteriorReinforce(d,state,Roll(4));}
  if(state.ExteriorTimers.TryGetValue("arrivalAt",out double at)&&ExteriorClock>=at){state.ExteriorTimers.Remove("arrivalAt");ExteriorReinforce(d,state,RollDamage(2,6));}
  if(n==8&&state.HazardId.EndsWith(".4")&&!state.Events.Contains("bench-searched")&&!state.Events.Contains("bench-turned")&&ExteriorClock>=state.ExteriorTimers.GetValueOrDefault("benchAt",double.MaxValue)){state.Events.Add("bench-turned");ExteriorReinforce(d,state,1);}
  if(n==10&&state.Events.Contains("freezer-open")&&!state.Events.Contains("hungry-released")&&ExteriorClock>=state.ExteriorTimers.GetValueOrDefault("freezerAt",double.MaxValue)){state.Events.Add("hungry-released");state.Events.Remove("freezer-open");SpawnChapterNamed(d,state,"The Hungry One");}
  if(n==11&&TensionState.Score>=50&&!state.Events.Contains("hungry-swarm")){state.Events.Add("hungry-swarm");SpawnChapterNamed(d,state,"Lot Sprinter",RollDamage(2,4));}
  if(n==1&&ExteriorClock>=state.ExteriorTimers.GetValueOrDefault("eventAt",double.MaxValue))
  {state.ExteriorTimers["eventAt"]=ExteriorClock+1800;int e=Roll(4);if(e==1)ChangeTension(2,"Distant gunshot");if(e==2)SpawnChapterNamed(d,state,"Biker Scout",2);if(e==3)state.ExteriorTimers["arrivalAt"]=ExteriorClock+600;if(e==4&&!state.Events.Contains("rescued:actor.exterior.01.survivor")&&!state.Events.Contains("exterior-survivor"))state.Events.Add("exterior-survivor");Message(MessageCategory.Narrative,"North lot event: "+new[]{"a distant shot","biker scouts arrive","a horde approaches in ten minutes","a wounded survivor hides in a car"}[e-1]);}
 }
 void SecureExterior30(LowerMallSite d,LowerMallState s)
 {
  if(!Exterior30Site(d))return;if(d.DisplayNumber==2)ChangeTension(-RollDamage(2,6),"Overflow lot cleared");if(LowerMallStates.Count(s=>s.Number is >=301 and <=307&&s.Secured)==7)RunCampaignEvent("event.exterior.one-seven");if(LowerMallStates.Count(s=>s.Number is >=308 and <=313&&s.Secured)==6)RunCampaignEvent("event.exterior.eight-thirteen");
 }
 string Exterior30SecureReason(LowerMallSite d,LowerMallState s)=>Exterior30Site(d)&&s.ExteriorRolls.GetValueOrDefault("waves")>0?"Wait for and clear the remaining finite alarm waves before securing.":"";
 string Exterior30TravelReason(string id)
 {
  var route=CampaignConnectionTo(id);if(route?.Id.StartsWith("route.exterior.drive")==true&&!RoofMaterial(PartyLeader,Campaign!.Infrastructure!.FuelItemId))return "The designated driver must carry one real fuel unit for the vehicle trip.";
  if(route?.Id.StartsWith("route.exterior.roof-bridge")==true&&LowerMallProgress(221)?.Events.Contains("crossed")!=true)return "Establish the skybridge crossing first.";
  return "";
 }
 void Exterior30Travel(CampaignConnection route)
 {
  if(route.Id.StartsWith("route.exterior.drive")){RoofConsume(PartyLeader,Campaign!.Infrastructure!.FuelItemId);ChangeTension(Roll(4),"Exterior vehicle journey");}
 }
 void ValidateExterior30(LowerMallSite d,LowerMallState s)
 {
  if(s.ExteriorRolls==null||s.ExteriorTimers==null)throw new InvalidDataException("Missing exterior state maps.");if(!Exterior30Site(d)){if(s.ExteriorRolls.Count+s.ExteriorTimers.Count>0)throw new InvalidDataException("Foreign exterior progress.");return;}
  if(s.BranchRoll is <1 or >6||s.BonusRoll is <1 or >4||s.ExteriorRolls.Count>64||s.ExteriorTimers.Count>64||s.ExteriorTimers.Any(k=>!double.IsFinite(k.Value)||k.Value<0)||s.ExteriorRolls.GetValueOrDefault("event") is <1 or >6||s.ExteriorRolls.GetValueOrDefault("special") is <1 or >6||s.ExteriorRolls.GetValueOrDefault("freezerDelay") is <1 or >6)throw new InvalidDataException("Invalid saved exterior clocks or branches.");
  string[] keys=["event","special","freezerDelay","waves","pumped","lockbox","car0","car1","car2","alarm0","alarm1","alarm2"];
  if(s.ExteriorRolls.Any(k=>!keys.Contains(k.Key)||k.Value<0)||s.ExteriorTimers.Any(k=>k.Key is not ("eventAt" or "alarmAt" or "arrivalAt" or "benchAt" or "freezerAt")&&!k.Key.StartsWith("revive:")))throw new InvalidDataException("Unknown exterior state key.");
  if(s.ExteriorRolls.GetValueOrDefault("waves")>9||s.ExteriorRolls.GetValueOrDefault("pumped")>5||s.ExteriorRolls.ContainsKey("pumped")&&d.Number!=313||s.ExteriorTimers.ContainsKey("benchAt")&&d.Number!=308||s.ExteriorTimers.ContainsKey("freezerAt")&&d.Number!=310)throw new InvalidDataException("Foreign exterior timer or reserve ownership.");
  for(int j=0;j<3;j++)if(d.DisplayNumber<=3&&(s.ExteriorRolls.GetValueOrDefault("car"+j) is <1 or >6||s.ExteriorRolls.GetValueOrDefault("alarm"+j) is <1 or >100))throw new InvalidDataException("Invalid saved vehicle type or alarm roll.");
  if(d.DisplayNumber>3&&s.ExteriorRolls.Keys.Any(k=>k.StartsWith("car")||k.StartsWith("alarm")))throw new InvalidDataException("Vehicle rolls on a non-vehicle site.");
  if(s.ExteriorTimers.Keys.Any(k=>k.StartsWith("revive:")&&(d.Number!=306||!Campaign!.LowerMall!.Monsters.Any(m=>m.ActorId==k[7..]&&m.Traits.Contains("ext30-revive"))||!s.Events.Contains("ext-revived:"+k[7..]))))throw new InvalidDataException("Foreign exterior revival ownership.");
  if(d.Number==313){int total=Features.Where(f=>CurrentTravelDestinationId==d.AreaId&&f.Id=="feature.exterior.13.pump-reserve").Concat(StoredTravelAreas.Where(a=>a.DestinationId==d.AreaId).SelectMany(a=>a.Features).Where(f=>f.Id=="feature.exterior.13.pump-reserve")).Sum(f=>f.Contents.Items.Sum(i=>i.Quantity));if(total+s.ExteriorRolls.GetValueOrDefault("pumped")!=5)throw new InvalidDataException("Exterior pump reserve does not match saved draws.");}
 }
 int Exterior30Damage(Actor target,int amount,DamageType type)
 {
  var tags=LowerTraits(target);if(tags.Contains("ext30-grease")){if(type==DamageType.Fire)return 0;if(type==DamageType.Cold)amount*=2;}if(tags.Contains("ext30-frozen")&&type is DamageType.Cold or DamageType.Piercing or DamageType.Slashing)amount/=2;return amount;
 }
 void Exterior30DamageResolved(Actor target,int damage,DamageType type,Actor? source,bool wasAlive)
 {
  var d=CurrentLowerMallSite;if(!Exterior30Site(d))return;var s=LowerMallProgress(d!.Number)!;var tags=LowerTraits(target);
  if(type==DamageType.Fire){if(damage>0)s.Events.Add("burn:"+target.Id);IgniteExterior30(source??target,false);}
  if(damage>0&&tags.Contains("ext30-rats")&&type==DamageType.Fire){target.Hp=0;target.Character.SetCondition("unconscious","Scattered from fire",true);}
  if(damage>0&&tags.Contains("ext30-screech"))ChangeTension(RollDamage(2,6),"Mutated horror screech");
  if(source is {Enemy:true}&&LowerTraits(source).Contains("ext30-frost")&&!target.Enemy&&damage>0){Hurt(target,Roll(6),DamageType.Cold);LowerEffect(target,"ext30-frostbite",86400);}
  if(!wasAlive||target.Alive||s.Events.Contains("ext-death:"+target.Id))return;s.Events.Add("ext-death:"+target.Id);
  if(tags.Contains("ext30-brute")){var p=Party.FirstOrDefault(p=>p.Alive&&p.Position.Distance(target.Position)<=1.5f);if(p!=null&&!SurvivorSave(p,Ability.Strength,14,"final grasp"))LowerEffect(p,"restrained",12);}
  if(tags.Contains("ext30-revive")&&type!=DamageType.Fire&&Roll(2)==1&&!s.Events.Contains("ext-revived:"+target.Id)){s.Events.Add("ext-revived:"+target.Id);s.ExteriorTimers["revive:"+target.Id]=ExteriorClock+6;}
  if(tags.Contains("ext30-exploder")||tags.Contains("ext30-grease")||tags.Contains("ext30-flammable"))foreach(var p in Actors.Where(p=>p!=target&&p.Alive&&p.Position.Distance(target.Position)<=(tags.Contains("ext30-flammable")?4:tags.Contains("ext30-grease")?1:2)).ToArray())Hurt(p,RollDamage(tags.Contains("ext30-flammable")?4:tags.Contains("ext30-exploder")?2:1,6),tags.Contains("ext30-exploder")?DamageType.Acid:DamageType.Fire,target);
  if(tags.Contains("ext30-doughnut")){s.Events.Add("kitchen-fire");foreach(var p in Party.Where(p=>p.Alive))LowerEffect(p,"ext30-burning",Roll(4)*6);}
 }
 void Exterior30Turn(Actor actor,LowerMallState s,IReadOnlyList<string> tags)
 {
  if(tags.Contains("ext30-exploder"))actor.Economy.Spend(ActionResourceKind.Action);
  if(tags.Contains("ext30-regen")&&actor.Alive&&!s.Events.Remove("burn:"+actor.Id))actor.Hp=Math.Min(actor.MaxHp,actor.Hp+5);
  if(tags.Contains("ext30-doughnut"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=3&&Line(p.Position,actor.Position)).ToArray()){int hit=RollDamage(2,6);Hurt(p,SurvivorSave(p,Ability.Constitution,14,"acid vomit")?hit/2:hit,DamageType.Acid,actor);}
  if(actor.Equipment.Weapon?.ItemId.StartsWith("item.exterior.enemy.")==true&&actor.Equipment.Weapon.LoadedAmmunition==null)actor.Equipment.Weapon=null;
 }
 void Exterior30PartyTurn(Actor actor){if(actor.Character.HasCondition("ext30-burning"))Hurt(actor,Roll(6),DamageType.Fire);}
 void IgniteExterior30(Actor source,bool gun)
 {
  var d=CurrentLowerMallSite;if(!Exterior30Site(d))return;var s=LowerMallProgress(d!.Number)!;int n=d.DisplayNumber;if(s.Events.Contains("ext30-explosion"))return;
  if(!s.Events.Contains("gas-active")&&n is not (9 or 12))return;if(n is 9 or 12&&gun&&source.Pos.X<25)return;
  s.Events.Add("ext30-explosion");s.Events.Remove("gas-active");int dice=n is 2 or 13?4:3;foreach(var a in Actors.Where(a=>a.Alive&&a.Position.Distance(source.Position)<=(n==13?4:3)).ToArray())Hurt(a,RollDamage(n==5?2:dice,n==5?10:6),DamageType.Fire);ChangeTension(RollDamage(3,6),"Exterior fuel or fryer explosion");
 }
 void Exterior30Noise(Actor actor,string kind)
 {
  var d=CurrentLowerMallSite;if(!Exterior30Site(d))return;var s=LowerMallProgress(d!.Number)!;if(kind is "gunshot" or "explosion" or "shout")s.Quiet=false;
  if(kind=="gunshot"){int n=d.DisplayNumber;ChangeTension(n==4?Roll(6)+2:n==13?Roll(6)+2:RollDamage(n is 8 or 9?3:n==12?1:2,n==12?4:6),"Exterior gunfire");IgniteExterior30(actor,true);if(n==8)ExteriorReinforce(d,s,Roll(6));}
  if(kind=="explosion")IgniteExterior30(actor,false);
 }
 void UseExterior30Item(Actor actor,ItemStack stack,string effect)
 {
  switch(effect)
  {
   case "ext30-flashlight":stack.Charges--;EquipmentEffects.Add(new(){Kind="light",ActorId=actor.Id,AreaId=CurrentTravelDestinationId,ItemId=stack.ItemId,Cell=actor.Pos,RadiusFeet=15,Expires=ExteriorClock+1800});break;
   case "ext30-medkit":UseRoof28Item(actor,stack,"roof-medkit");break;
   case "ext30-morale":UseRoof28Item(actor,stack,"roof-morale");break;
   case "ext30-energy":LowerEffect(actor,"ext30-energy",3600);break;
   case "ext30-stamina":actor.Needs.Exhaustion=Math.Max(0,actor.Needs.Exhaustion-1);break;
   case "ext30-doughnut":actor.Hp=Math.Min(actor.MaxHp,actor.Hp+1);break;
   case "ext30-beer":LowerEffect(actor,"upper-morale",600);LowerEffect(actor,"ext30-beer",600);break;
   case "ext30-extinguisher":if(CurrentLowerMallSite is {} d&&Exterior30Site(d))LowerMallProgress(d.Number)!.Events.Remove("kitchen-fire");foreach(var p in Actors){p.Character.SetCondition("ext30-burning","Burning",false);LowerMallEffects.RemoveAll(e=>e.ActorId==p.Id&&e.Condition=="ext30-burning");}break;
   case "ext30-radio":EmitLocalNoise(actor,"door",60);break;
   case "ext30-flare":EmitLocalNoise(actor,"explosion",100);break;
  }
 }
}
