using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed partial class World
{
 static void OuterMark(LowerMallState s,string key){if(!s.Events.Contains(key))s.Events.Add(key);}
 static bool Exterior31Site(LowerMallSite? d)=>d?.Level=="exterior"&&d.DisplayNumber is >=14 and <=26;
 [JsonIgnore] public string Exterior31Status=>CurrentLowerMallSite is {} d&&Exterior31Site(d)&&LowerMallProgress(d.Number) is {} s?$"{(s.Secured?"SECURED":"UNSECURED")} | SAVED OUTCOME {s.BranchRoll} | {(Exterior31Power?"POWER ON":"POWER OFF")} | CLOCKS: {s.OuterTimers.Count} ":"";
 bool Exterior31Power=>Exterior31Site(CurrentLowerMallSite)&&(LowerMallProgress(CurrentLowerMallSite!.Number) is {} local&&local.Events.Contains("local-power")&&local.OuterTimers.GetValueOrDefault("powerUntil")>ExteriorClock||LowerMallProgress(325) is {} boxcar&&boxcar.Events.Contains("local-power")&&boxcar.OuterTimers.GetValueOrDefault("powerUntil")>ExteriorClock);
 bool OuterOwn(Actor a,int n,int j)=>a.Inventory.Items.Concat(a.Equipment.All()).Any(i=>i.ItemId==$"item.exterior.{n:00}.loot.{j}"&&i.Wear<100);
 void ConfigureExterior31Container(Feature f)
 {
  if(!f.Id.StartsWith("feature.exterior."))return;
  var gun=f.Contents.Items.FirstOrDefault(i=>i.Definition.MagazineSize>0);if(gun!=null){var ammo=f.Contents.Items.FirstOrDefault(i=>i.Definition.Kind==ItemKind.Ammunition&&i.Definition.AmmoType==gun.Definition.AmmunitionType);if(ammo!=null){f.Contents.Items.Remove(ammo);gun.LoadedAmmunition=ammo;}}
  if(f.Id=="feature.exterior.21.loot.3")f.Contents.Items.Single().Wear=100;
 }
 void OuterStock(int n,int j,int quantity)
 {
  var f=Features.Single(f=>f.Id==$"feature.exterior.{n:00}.loot.{j}");if(quantity==0){f.Contents.Items.Clear();return;}foreach(var i in f.Contents.Items){i.Quantity=quantity;i.ResourceUnits=i.ResourceUnits.Take(quantity).ToList();}
 }
 void EnterExterior31(LowerMallSite d,LowerMallState s)
 {
  if(!Exterior31Site(d))return;int n=d.DisplayNumber;s.BranchRoll=Roll(n==25?4:6);s.OuterRolls["special"]=Roll(6);s.OuterRolls["vehicle"]=Roll(100);s.OuterTimers["eventAt"]=ExteriorClock+600;
  if(n==14){OuterStock(n,3,RollDamage(2,4));if(s.BranchRoll is not (3 or 4)){OuterStock(n,8,0);OuterStock(n,9,0);}}
  if(n==17)OuterStock(n,0,Roll(4));if(n==19)OuterStock(n,3,Roll(6));if(n==25){OuterStock(n,3,Roll(12));if(s.BranchRoll!=2)OuterStock(n,8,0);if(s.BranchRoll!=4){OuterStock(n,9,0);OuterStock(n,10,0);}}
  if(n==26)OuterStock(n,1,Roll(4));if(n==23&&s.BranchRoll is not (4 or 5))OuterStock(n,6,0);
  if(n==15||n==17&&s.HazardId.EndsWith(".1")||n==23||n==24&&s.HazardId.EndsWith(".3"))OuterMark(s,"gas-active");
  if(n==16&&s.EncounterRoll==3)s.OuterTimers["runnerAt"]=ExteriorClock+6;
  if(n==20)SpawnChapterNamed(d,s,"Crawlspace Crawler",Roll(2));
  if(n==15&&s.EncounterRoll==6||n==17&&s.EncounterRoll==4||n==18&&s.BranchRoll<=3||n==21||n==26&&s.EncounterRoll==6)OuterMark(s,"outer-survivor");
  if(n==26&&s.EncounterRoll==5){OuterMark(s,"thing-below");s.OuterTimers["preyAt"]=ExteriorClock+180;ChangeTension(RollDamage(2,6),"The Thing Below watches");}
 }
 string Exterior31Reason(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Exterior31Site(d))return "";int n=d.DisplayNumber;string e=a.Effect;
  if(a.NpcActorId!=""&&!s.Events.Contains("outer-survivor"))return "No living survivor is available on this saved branch.";
  if(n==16&&e=="ext31-rooms"&&!OuterOwn(actor,n,0))return "Carry the actual motel keyring.";
  if(e=="ext31-generator"||n==26&&e=="ext31-power")
  {
   if(!RoofTools(actor)||!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)||!RoofMaterial(actor,Campaign.Infrastructure.GeneratorCost[0].ItemId))return "Carry usable tools, one structural repair material and one real fuel unit.";
   if(n==25&&(!OuterOwn(actor,n,9)||!s.Resolved.Contains("action.exterior.25.device.locked")))return "Recover the generator from the opened boxcar's saved generator branch.";
   if(n==26&&!actor.ClassIs("smart"))return "Use a Smart survivor to restore this utility circuit.";
  }
  if(n==17&&e=="ext31-radio"&&(!s.Events.Contains("local-power")||ShelterHomeStockpile() is not {} home||StoredCreditedSv(home)==0))return "Restore local generator power and retain credited supplies in the original refuge stockpile.";
  if(n==18&&e=="ext31-trade"&&!actor.Inventory.Items.Any(i=>Campaign!.Items.Single(x=>x.Id==i.ItemId).SurvivalUse=="food"))return "Bring a real food unit for the pharmacist.";
  if(n==18&&e=="ext31-credentials"&&!OuterOwn(actor,n,5))return "Carry the prescription pad and doctor's badge.";
  if(n==19&&e=="ext31-disarm"&&s.Events.Contains("bomb-fired"))return "The bomb already detonated; there is nothing left to disarm.";
  if(n==19&&e=="ext31-cage"&&!s.Resolved.Contains("action.exterior.19.device.disarm")&&!s.Events.Contains("bomb-fired"))return "Resolve the dead man's switch before opening the gun cage.";
  if(n==19&&e=="ext31-ticket"&&!OuterOwn(actor,n,5))return "Carry the actual pawn ticket.";
  if(n==20&&e=="ext31-footage"&&!RoofGridOnline&&!Exterior31Power)return "Restore live mall grid power to view the security footage.";
  if(n==20&&e=="ext31-restricted"&&!OuterOwn(actor,n,6))return "Carry the bank manager's keycard.";
  if(n==21&&e=="ext31-repair"&&(!OuterOwn(actor,n,3)&&!actor.Inventory.Items.Any(i=>i.ItemId=="item.exterior.21.loot.3"&&i.Broken)||!RoofTools(actor)||!RoofMaterial(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId)))return "Carry the broken counter revolver, tools and repair material.";
  if(n==21&&e=="ext31-emergency"&&!OuterOwn(actor,n,0))return "Carry the bowling alley keycard.";
  if(n==22&&e=="ext31-repair"&&(!OuterOwn(actor,n,2)||!RoofTools(actor)||!RoofMaterial(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId)))return "Carry the skateboard, usable tools and one repair material.";
  if(n==22&&e=="ext31-ride"&&actor.Character.HasCondition("ext31-board"))return "Finish this round of skateboard movement before riding again.";
  if(n==22&&e=="ext31-ride"&&(!OuterOwn(actor,n,2)||!s.Events.Contains("board-ready")))return "Carry and repair the actual skateboard first.";
  if(e=="ext31-vehicle")
  {
   if(n==23&&s.OuterRolls["vehicle"]>40)return "No functional road car was found on this saved 40-percent branch.";
   if(n==15&&!OuterOwn(actor,n,5))return "Carry the recovered vehicle keys.";
   if(!RoofTools(actor)||!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)||!RoofMaterial(actor,Campaign.Infrastructure.GeneratorCost[0].ItemId))return "Carry tools, repair material and real fuel to prepare the vehicle.";
  }
  if(n==23&&e=="ext31-truck"&&(!s.Resolved.Contains("action.exterior.23.device.semi")||Actors.Any(t=>t.Alive&&t.Enemy&&t.Name=="Mutant Brute")))return "Open the semi and defeat its surviving brute before moving it.";
  if(e=="ext31-siren"&&!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId))return "Carry a real fuel unit to power the cruiser siren.";
  if(e=="ext31-burn"&&!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId))return "Carry a real fuel unit for the diversion.";
  if(n==25&&e=="ext31-locked"&&actor.Inventory.Items.Any(i=>i.Definition.Name.Contains("Bolt Cutters")))return "";
  if(n==26&&e=="ext31-return"&&(!OuterOwn(actor,n,2)||!s.Events.Contains("local-power")))return "Restore the circuit and carry the maintenance keycard.";
  if(n==26&&e=="ext31-light"&&!EquipmentEffects.Any(l=>l.Kind=="light"&&l.AreaId==CurrentTravelDestinationId))return "Activate an actual flashlight here first.";
  if(n==26&&e=="ext31-rescue"&&!s.Events.Contains("prey-grabbed"))return "No companion needs rescue from the tunnel darkness.";
  if(e=="ext31-quiet"&&(!s.Quiet||CampaignMinutes-s.EnteredMinute<10||Actors.Any(a=>a.Enemy&&a.Alive)))return "Remain quiet for ten minutes and clear active threats first.";
  return "";
 }
 void Exterior31Device(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Exterior31Site(d))return;int n=d.DisplayNumber;string e=a.Effect;
  switch(e)
  {
   case "ext31-isolate":s.Events.Remove("gas-active");OuterMark(s,"isolated");s.OuterTimers.Remove("alarmAt");break;
   case "ext31-disarm":OuterMark(s,"disarmed");s.Events.Remove("gas-active");if(n==15)s.HazardResolved=true;break;
   case "ext31-engine":OuterMark(s,"engine-off");break;
   case "ext31-car":if(s.BranchRoll<=2){ChangeTension(RollDamage(2,6),"Locked sedan alarm");OuterReinforce(d,s,Roll(6));}break;
   case "ext31-generator":case "ext31-power":RoofConsume(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId);RoofConsume(actor,Campaign.Infrastructure.FuelItemId);OuterMark(s,"local-power");s.OuterTimers["powerUntil"]=ExteriorClock+14400;break;
   case "ext31-radio":
    if(n==17){var h=ShelterHomeStockpile()!;s.OuterSignalUnits=h.Contents.Items.SelectMany(StackResourceUnits).Where(SecuredResourceUnits.Contains).Distinct().ToList();CreditLower(s,"outer-signal",Exterior31SignalCredit(d,s));}else{ChangeTension(Roll(6),"Police radio response");AddStoryJournalEntry(a.Id,"Police radio","Saved transmission: "+s.OuterRolls["special"]+". The numbers and military orders remain unverified clues.","Exterior");}break;
   case "ext31-trade":var food=actor.Inventory.Items.First(i=>Campaign!.Items.Single(x=>x.Id==i.ItemId).SurvivalUse=="food");RoofConsume(actor,food.ItemId);actor.Hp=Math.Min(actor.MaxHp,actor.Hp+5);goto case "ext31-survivor";
   case "ext31-survivor":case "ext31-champion":case "ext31-cultist":LowerRecruit(a,s);s.Events.Remove("outer-survivor");if(n==25&&Actors.FirstOrDefault(p=>p.Id==a.NpcActorId) is {} recruit)LowerEffect(recruit,"book-infected",86400);break;
   case "ext31-breakroom":if(s.BranchRoll>3)AddStoryJournalEntry(a.Id,"Last pharmacist recording","The recorded final message points toward research below the mall, without proving its cause.","Exterior");break;
   case "ext31-cage":SpawnChapterNamed(d,s,"Ravenous Ghoul");break;
   case "ext31-vault":SpawnChapterNamed(d,s,"Vault Horror");ChangeTension(RollDamage(5,6),"Bank vault opened");break;
   case "ext31-restricted":if(!s.Resolved.Contains("action.exterior.20.device.box"))s.Resolved.Add("action.exterior.20.device.box");break;
   case "ext31-security":case "ext31-counter":OuterMark(s,"security-off");s.OuterTimers.Remove("alarmAt");break;
   case "ext31-repair":RoofConsume(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId);if(n==21)actor.Inventory.Items.Single(i=>i.ItemId=="item.exterior.21.loot.3").Wear=0;else OuterMark(s,"board-ready");break;
   case "ext31-ride":LowerEffect(actor,"ext31-board",6);if(Mode==Mode.Combat)actor.Economy.AddMovement(10);break;
   case "ext31-ball":OuterReinforce(d,s,Roll(6));ChangeTension(Roll(4),"Ball return mechanism");break;
   case "ext31-pool":SpawnChapterNamed(d,s,"Ritual Zombie",Roll(4));ChangeTension(RollDamage(2,6),"Ritual pool disturbed");break;
   case "ext31-symbols":LowerEffect(actor,"frightened",6);break;
   case "ext31-semi":if(s.BranchRoll<=3||s.BranchRoll<=5&&s.OuterRolls["special"]<=3)SpawnChapterNamed(d,s,"Mutant Brute");if(s.BranchRoll==6)OuterMark(s,"outer-survivor");break;
   case "ext31-truck":if(n==24){if(s.BranchRoll is 4 or 5)SpawnChapterNamed(d,s,"Truck Horror");if(s.BranchRoll==6)OuterMark(s,"outer-survivor");}else OuterMark(s,"road-cleared");break;
   case "ext31-vehicle":RoofConsume(actor,Campaign!.Infrastructure!.FuelItemId);RoofConsume(actor,Campaign.Infrastructure.GeneratorCost[0].ItemId);SetDialogueFlag("flag.exterior.vehicle-ready");OuterMark(s,"vehicle-ready");break;
   case "ext31-siren":if(RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)){RoofConsume(actor,Campaign.Infrastructure.FuelItemId);OuterReinforce(d,s,Roll(6));ChangeTension(RollDamage(2,6),"Road police siren");}break;
   case "ext31-burn":RoofConsume(actor,Campaign!.Infrastructure!.FuelItemId);foreach(var f in Features.Where(f=>f.Kind=="container"))f.Contents.Items.Clear();foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive).ToArray())Hurt(t,RollDamage(2,10),DamageType.Fire,actor);ChangeTension(RollDamage(2,10),"Motel fire diversion");EmitLocalNoise(actor,"explosion",150);OuterMark(s,"burned");break;
   case "ext31-locked":CreditLower(s,"outer-boxcar",Exterior31BoxcarCredit(d,s));ChangeTension(RollDamage(2,6),"Sealed boxcar opened");if(s.BranchRoll==1){SpawnChapterNamed(d,s,"Thing in the Crate");ChangeTension(RollDamage(3,6),"Crate mutant released");}if(s.BranchRoll==3)OuterMark(s,"outer-survivor");break;
   case "ext31-entrance":case "ext31-return":SetDialogueFlag($"flag.exterior.{n}.{e[6..]}");break;
   case "ext31-emergency":foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=true;break;
   case "ext31-light":ChangeTension(Roll(4),"Sewer light disturbs unseen things");OuterMark(s,"tunnel-lit");break;
   case "ext31-rescue":s.Events.Remove("prey-grabbed");foreach(var p in Party){p.Character.SetCondition("restrained","Restrained",false);LowerMallEffects.RemoveAll(f=>f.ActorId==p.Id&&f.Condition=="restrained");}s.Events.Remove("thing-below");s.OuterTimers.Remove("preyAt");break;
   case "ext31-quiet":ChangeTension(-Roll(4),"Quiet trailer sweep");break;
  }
 }
 void Exterior31Failure(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(Exterior31Site(d)&&a.Effect=="ext31-ride")LowerEffect(actor,"prone",6);
  if(!Exterior31Site(d)||a.Effect!="ext31-disarm"||d.Number!=319||s.Events.Contains("bomb-fired"))return;OuterMark(s,"bomb-fired");foreach(var p in Actors.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=2).ToArray()){int hit=RollDamage(6,6);Hurt(p,SurvivorSave(p,Ability.Dexterity,14,"pipe bomb")?hit/2:hit,DamageType.Fire);}ChangeTension(RollDamage(4,6),"Dead man's switch exploded");OuterReinforce(d,s,Roll(6));
 }
 void Exterior31Hazard(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a,bool pass)
 {
  if(!Exterior31Site(d)||pass)return;int n=d.DisplayNumber,j=int.Parse(a.Id.Split('.').Last());
  if(n==14&&j==3)SpawnChapterNamed(d,s,"Crawling Mechanic");if(n==16&&j is 1 or 2)SpawnChapterNamed(d,s,"Crawlspace Crawler",1);
  if(n==18&&j==1){s.OuterTimers["alarmAt"]=ExteriorClock+Roll(6)*60;s.OuterRolls["alarmCount"]=Roll(4);}if(n==18&&j==3)SpawnChapterNamed(d,s,"Gasbag",Roll(4));
  if(n==19&&j==1){OuterMark(s,"bomb-fired");OuterReinforce(d,s,Roll(6));}if(n==24&&j==1)SpawnChapterNamed(d,s,"Crawlspace Crawler",Roll(4));if(n==25&&j==3)SpawnChapterNamed(d,s,"Rat Swarm");
 }
 void OuterReinforce(LowerMallSite d,LowerMallState s,int q)
 {
  var e=Campaign!.Encounters.First(x=>x.AreaId==d.AreaId&&x.ActorIds.Any(id=>Campaign.Actors.Single(a=>a.Id==id).Role=="Shambler"));int had=s.Releases.FirstOrDefault(r=>r.EncounterId==e.Id)?.Count??0;SpawnLower(d,s,e.Id,had+q,"reaction");
 }
 void TickExterior31()
 {
  if(Campaign?.SchemaVersion<29||!LowerMallEnabled)return;
  foreach(var state in LowerMallStates.Where(s=>s.Number is >=314 and <=326))foreach(var timer in state.OuterTimers.Where(t=>t.Key.StartsWith("revive:")&&t.Value<=ExteriorClock).ToArray())
  {state.OuterTimers.Remove(timer.Key);var a=Actors.Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).FirstOrDefault(a=>a.Id==timer.Key[7..]);if(a!=null&&!state.Events.Contains("burn:"+a.Id)){a.Hp=Math.Min(a.MaxHp,10);a.Character.SetCondition("unconscious","Unconscious",false);}}
  var d=CurrentLowerMallSite;if(!Exterior31Site(d))return;var s=LowerMallProgress(d!.Number);if(s==null)return;int n=d.DisplayNumber;
  if(s.OuterTimers.TryGetValue("powerUntil",out double end)&&end<=ExteriorClock){s.OuterTimers.Remove("powerUntil");s.Events.Remove("local-power");}
  if(s.OuterTimers.TryGetValue("runnerAt",out double runner)&&runner<=ExteriorClock){s.OuterTimers.Remove("runnerAt");SpawnChapterNamed(d,s,"Motel Runner");}
  if(s.OuterTimers.TryGetValue("alarmAt",out double alarm)&&alarm<=ExteriorClock){s.OuterTimers.Remove("alarmAt");OuterReinforce(d,s,s.OuterRolls.GetValueOrDefault("alarmCount",1));}
  if(s.OuterTimers.TryGetValue("preyAt",out double prey)&&prey<=ExteriorClock){s.OuterTimers.Remove("preyAt");var last=Party.LastOrDefault(a=>a.Alive);if(last!=null&&!SurvivorSave(last,Ability.Intelligence,15,"The Thing Below")){LowerEffect(last,"restrained",60);OuterMark(s,"prey-grabbed");Message(MessageCategory.Combat,"The last companion is seized at the tunnel edge. Rescue them at the entrance machinery.");}}
  if(s.Secured)return;
  if(s.OuterTimers.GetValueOrDefault("eventAt")<=ExteriorClock){s.OuterTimers["eventAt"]=ExteriorClock+600;if(n==15&&!s.Events.Contains("engine-off"))ChangeTension(Roll(4),"QuickFix engine and compressor");if(n==17&&s.Quiet)ChangeTension(-Roll(4),"Quiet trailer interval");if(n==18) {if(s.OuterRolls["special"]==3)OuterReinforce(d,s,Roll(4));else AddStoryJournalEntry("outer.drugstore."+s.OuterRolls["special"],"PharmCo ten-minute event","Saved intercom, ceiling, bottle, camera or warning-radio event: "+s.OuterRolls["special"]+". No unidentified medicine is automatically consumed.","Exterior");}if(n==22&&Actors.Any(a=>a.Alive&&a.Enemy))ChangeTension(Roll(4),"Cultist ritual remains active");}
 }
 int Exterior31Damage(Actor target,int damage,DamageType type)
 {
  var t=LowerTraits(target);if(t.Contains("ext31-oil")&&type==DamageType.Fire)damage/=2;if(t.Contains("ext31-water")&&type==DamageType.Piercing)damage/=2;if(t.Contains("ext31-brute")){if(type==DamageType.Piercing)damage/=2;if(type==DamageType.Fire)damage*=2;}return damage;
 }
 void Exterior31DamageResolved(Actor target,int damage,DamageType type,Actor? source,bool wasAlive)
 {
  var d=CurrentLowerMallSite;if(!Exterior31Site(d))return;var s=LowerMallProgress(d!.Number)!;var t=LowerTraits(target);
  if((type==DamageType.Fire||type==DamageType.Lightning&&t.Contains("ext31-perfect"))&&damage>0)OuterMark(s,"outer-stop:"+target.Id);
  if(type==DamageType.Fire&&damage>0){IgniteExterior31(source??target);if((t.Contains("ext31-tire")||t.Contains("ext31-oil"))&&!s.Events.Contains("oil-ignited:"+target.Id)){OuterMark(s,"oil-ignited:"+target.Id);foreach(var p in Actors.Where(p=>p!=target&&p.Alive&&p.Position.Distance(target.Position)<=1).ToArray())Hurt(p,Roll(4),DamageType.Fire,target);}}
  if(damage>0&&t.Contains("ext31-crate"))foreach(var p in Actors.Where(a=>a!=target&&a.Alive&&a.Position.Distance(target.Position)<=1).ToArray())Hurt(p,Roll(4),DamageType.Acid,target);
  if(t.Contains("ext31-rats")&&target.Hp<5){target.Hp=0;target.Character.SetCondition("unconscious","Dispersed",true);}
  if(!wasAlive||target.Alive)return;
  if((t.Contains("ext31-mechanic")||t.Contains("ext31-revive"))&&type!=DamageType.Fire&&!s.Events.Contains("outer-revived:"+target.Id)){OuterMark(s,"outer-revived:"+target.Id);s.OuterTimers["revive:"+target.Id]=ExteriorClock+(t.Contains("ext31-mechanic")?6:Roll(4)*6);}
  if(t.Contains("ext31-gasbag")&&!s.Events.Contains("outer-burst:"+target.Id)){OuterMark(s,"outer-burst:"+target.Id);foreach(var p in Actors.Where(a=>a!=target&&a.Alive&&a.Position.Distance(target.Position)<=2).ToArray()){Hurt(p,Roll(d!.DisplayNumber==23?8:6),DamageType.Poison,target);if(!SurvivorSave(p,Ability.Constitution,12,"gasbag fumes"))LowerEffect(p,"chapter-sick",60);}}
 }
 void Exterior31Turn(Actor actor,LowerMallState s,IReadOnlyList<string> t)
 {
  if((t.Contains("ext31-crate")||t.Contains("ext31-perfect"))&&actor.Alive&&!s.Events.Remove("outer-stop:"+actor.Id))actor.Hp=Math.Min(actor.MaxHp,actor.Hp+5);
  if(t.Contains("ext31-burning"))Hurt(actor,Roll(6),DamageType.Fire);
  if(t.Contains("ext31-radio")&&Round%3==0)ChangeTension(Roll(4),"Radio Man number broadcast");
  if(t.Contains("ext31-screamer")&&!s.Events.Contains("screamed:"+actor.Id)){OuterMark(s,"screamed:"+actor.Id);OuterReinforce(CurrentLowerMallSite!,s,RollDamage(2,6));}
  if(t.Contains("ext31-paint")){var p=Party.FirstOrDefault(p=>p.Alive&&p.Position.Distance(actor.Position)<=2);if(p!=null&&!SurvivorSave(p,Ability.Dexterity,13,"cultist paint"))LowerEffect(p,"blinded",6);}
  if(t.Contains("ext31-brute"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=6))if(!SurvivorSave(p,Ability.Strength,14,"ground slam"))LowerEffect(p,"prone",6);
 }
 void IgniteExterior31(Actor source)
 {
  var d=CurrentLowerMallSite;if(!Exterior31Site(d))return;var s=LowerMallProgress(d!.Number)!;if(!s.Events.Contains("gas-active")||s.Events.Contains("outer-explosion"))return;if(d.DisplayNumber==24&&Roll(100)>20)return;OuterMark(s,"outer-explosion");s.Events.Remove("gas-active");foreach(var a in Actors.Where(a=>a.Alive&&a.Position.Distance(source.Position)<=3).ToArray())Hurt(a,RollDamage(3,6),DamageType.Fire);ChangeTension(RollDamage(2,6),"Exterior gas explosion");
 }
 void Exterior31Noise(Actor actor,string kind)
 {
  var d=CurrentLowerMallSite;if(!Exterior31Site(d))return;var s=LowerMallProgress(d!.Number)!;if(kind is "gunshot" or "explosion" or "shout")s.Quiet=false;if(kind=="gunshot"){ChangeTension(Roll(d.DisplayNumber is 15 or 16 or 21?4:6),"Outer exterior gunfire");IgniteExterior31(actor);}if(kind=="explosion")IgniteExterior31(actor);
 }
 string Exterior31SecureReason(LowerMallSite d,LowerMallState s)=>!Exterior31Site(d)?"":s.OuterTimers.Keys.Any(k=>k.StartsWith("revive:")||k is "runnerAt" or "alarmAt")?"Resolve the saved arrivals and reanimations first.":s.Events.Contains("prey-grabbed")?"Rescue the grabbed companion first.":"";
 void SecureExterior31(LowerMallSite d,LowerMallState s)
 {
  if(!Exterior31Site(d))return;if(LowerMallStates.Count(s=>s.Number is >=314 and <=320&&s.Secured)==7)RunCampaignEvent("event.exterior.fourteen-twenty");if(LowerMallStates.Count(s=>s.Number is >=321 and <=326&&s.Secured)==6)RunCampaignEvent("event.exterior.twentyone-twentysix");
 }
 int Exterior31SignalCredit(LowerMallSite d,LowerMallState s)
 {
  if(d.Number!=317||!s.Resolved.Contains("action.exterior.17.device.radio")||s.OuterSignalUnits.Distinct().Count()!=s.OuterSignalUnits.Count||s.OuterSignalUnits.Any(id=>!SecuredResourceUnits.Contains(id)))throw new InvalidDataException("Invalid trailer-radio stored-SV snapshot.");var cat=ResourceCatalogue();return s.OuterSignalUnits.Sum(id=>cat[id].Sv)/10;
 }
 int Exterior31BoxcarCredit(LowerMallSite d,LowerMallState s)
 {
  if(d.Number!=325||!s.Resolved.Contains("action.exterior.25.device.locked"))throw new InvalidDataException("Invalid boxcar opening credit.");return s.BranchRoll switch{1=>100,2=>150,3=>250,4=>500,_=>throw new InvalidDataException("Invalid boxcar outcome.")};
 }
 void ValidateExterior31(LowerMallSite d,LowerMallState s)
 {
  if(s.OuterRolls==null||s.OuterTimers==null||s.OuterSignalUnits==null)throw new InvalidDataException("Missing outer exterior state.");if(!Exterior31Site(d)){if(s.OuterRolls.Count+s.OuterTimers.Count+s.OuterSignalUnits.Count>0)throw new InvalidDataException("Foreign outer exterior state.");return;}
  if(s.BranchRoll<1||s.BranchRoll>(d.Number==325?4:6)||s.OuterRolls.GetValueOrDefault("special") is <1 or >6||s.OuterRolls.GetValueOrDefault("vehicle") is <1 or >100||s.OuterRolls.Any(k=>k.Key is not ("special" or "vehicle" or "alarmCount")||k.Value<0)||s.OuterTimers.Count>64||s.OuterTimers.Any(t=>!double.IsFinite(t.Value)||t.Value<0||t.Key is not ("eventAt" or "powerUntil" or "runnerAt" or "alarmAt" or "preyAt")&&!t.Key.StartsWith("revive:")))throw new InvalidDataException("Invalid outer exterior saved outcome or clock.");
  if(s.OuterTimers.ContainsKey("runnerAt")&&d.Number!=316||s.OuterTimers.ContainsKey("alarmAt")&&d.Number!=318||s.OuterTimers.ContainsKey("preyAt")&&d.Number!=326)throw new InvalidDataException("Foreign exterior delayed-event owner.");
  foreach(var k in s.OuterTimers.Keys.Where(k=>k.StartsWith("revive:")))if(!s.Releases.Any(r=>Campaign!.Encounters.Single(e=>e.Id==r.EncounterId).ActorIds.Take(r.Count).Contains(k[7..]))||!s.Events.Contains("outer-revived:"+k[7..]))throw new InvalidDataException("Foreign exterior revival.");
  if(s.Credits.Contains("outer-signal"))Exterior31SignalCredit(d,s);else if(s.OuterSignalUnits.Count>0)throw new InvalidDataException("Uncredited signal snapshot.");
 }
 void UseExterior31Item(Actor actor,ItemStack stack,string effect)
 {
  if(effect=="ext31-adrenaline"){LowerEffect(actor,"ext31-adrenaline",60);actor.Needs.Exhaustion=Math.Min(6,actor.Needs.Exhaustion+1);}
  if(effect=="ext31-spray")foreach(var e in Actors.Where(e=>e.Enemy&&e.Alive&&e.Position.Distance(actor.Position)<=2&&Line(actor.Position,e.Position)))if(!SurvivorSave(e,Ability.Dexterity,13,"spray paint"))LowerEffect(e,"blinded",6);
 }
}
