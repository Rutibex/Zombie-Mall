using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed partial class World
{
 public FinaleState Finale {get;set;}=new();
 [JsonIgnore] public bool FinaleEnabled=>Campaign?.Finale!=null;
 [JsonIgnore] public bool CampaignEnded=>FinaleEnabled&&Finale.Ending!=null;
 [JsonIgnore] public Action? FinaleOpened {get;set;}
 [JsonIgnore] public int WinterDay=>Finale.WinterStart<0?0:Math.Max(0,(CampaignMinutes-Finale.WinterStart)/1440);
 [JsonIgnore] public IReadOnlyList<MysteryEvidence> DiscoveredEvidence=>Campaign?.Finale?.Evidence.Where(e=>LowerMallProgress(e.Location)?.Resolved.Contains(e.Id)==true).ToArray()??[];
 [JsonIgnore] public string FinaleStatus=>$"{CampaignTimeText} | MORALE {Finale.Morale}/100 | WINTER {WinterDay}/150 DAYS | {Finale.Weeks.Count} WEEKS";
 [JsonIgnore] public string HelicopterStatus=>$"INSPECTION {(Finale.Inspected?"DONE":"DUE")} | REPAIRS {Finale.Repairs.Count}/3 | FUEL {Finale.Fuel}/3 | PILOT {(Finale.PilotId==""?"UNTRAINED":Finale.PilotId)} | {(Finale.Airworthy?"AIRWORTHY":"GROUNDED")}";
 static readonly string[] FinaleTheories=["undetermined","biodyne","cult","military","anomaly"];
 void InitializeFinale(){if(FinaleEnabled)Finale.Seed=(uint)Random.Shared.Next(1,int.MaxValue);}
 void SelectFinaleClue(LowerMallSite d)
 {
  if(!FinaleEnabled||Finale.Clues.ContainsKey(d.Number))return;var choices=Campaign!.Finale!.Evidence.Where(e=>e.Location==d.Number).ToArray();if(choices.Length>0)Finale.Clues[d.Number]=choices[DailyDie(Finale.Seed,d.Number,71,choices.Length)-1].Id;
 }
 string FinaleClueReason(LowerMallSite d,LowerMallAction a)=>FinaleEnabled&&a.Kind=="lore"&&Finale.Clues.TryGetValue(d.Number,out string? id)&&a.Id!=id?"This investigation's saved clue variant is absent. Other theories remain possible at other locations.":"";
 public string SetMysteryTheoryReason(string theory)=>!FinaleEnabled?"This saved campaign predates the mystery board.":CampaignEnded?"This campaign already has its final record.":!FinaleTheories.Contains(theory)?"Unknown theory.":Finale.Theory==theory&&Finale.TheoryMinute>=0?"This is already your working interpretation.":DiscoveredEvidence.Count<3||DiscoveredEvidence.Select(e=>e.AreaId).Distinct().Count()<2?"Investigate three actual clues from at least two locations.":FinaleConsoleReason(PartyLeader,"home");
 public bool SetMysteryTheory(string theory)
 {
  var why=SetMysteryTheoryReason(theory);if(why!=""){LogSystem(why,true);return false;}CampaignMinutes+=10;Finale.Theory=theory;Finale.TheoryMinute=CampaignMinutes;ProcessSurvivalTime();AddStoryJournalEntry("finale.theory."+CampaignMinutes,"Working interpretation: "+theory,"The expedition records its interpretation of the recovered evidence. This is a belief, not proof of a single canonical cause.","Mysteries");return true;
 }
 string FinaleConsoleReason(Actor actor,string console)
 {
  if(!FinaleEnabled)return "Start a new Phase 32 campaign for mysteries and endings.";if(CampaignEnded)return "This campaign already has its final record.";
  if(!Party.Contains(actor)||!actor.Alive||Mode!=Mode.Explore||PendingReaction!=null||ReactionInterruptActive||Party.Any(IsMoving))return "Choose a conscious survivor and finish movement, combat and reactions.";
  var d=Campaign!.Finale!;string id=console=="heli"?d.HelicopterFeatureId:console=="crisis"?d.CrisisFeatureId:d.RefugeFeatureId;var f=Features.FirstOrDefault(f=>f.Id==id);
  if(f==null||!IsFeatureDiscovered(f)||actor.Position.Distance(f.Pos.Center)>2.25f||!Line(actor.Position,f.Pos.Center))return "Stand beside the "+(console=="heli"?"SkyEye 7 flight console":console=="crisis"?"communications-tower emergency console":"refuge mystery and escape board")+".";
  if(Actors.Any(a=>a.Enemy&&a.Alive))return "Clear active threats before committing to campaign preparations.";
  if((long)CampaignMinutes+220000>int.MaxValue)return "The campaign clock limit is too close.";return "";
 }
 int FinaleFood(string use,bool community=false)=> (community?CommunityMembers:Party).Where(a=>a.Alive).Sum(a=>a.Inventory.Items.Where(i=>SupplyUse(i)==use).Sum(SupplyUnits));
 int LiveCommunity=>CommunityMembers.Count(a=>a.Alive);
 int MaterialCount(Actor a,string id)=>a.Inventory.Items.Where(i=>EquipmentRules.MaterialMatches(i,id)).Sum(i=>i.Quantity);
 bool FinaleTools(Actor a)=>a.Inventory.Items.Any(i=>i.ItemId=="item.roof.11.loot.3"&&i.Wear<100);
 string BaseReadinessReason()=>CommunityMembers.Any(a=>!a.Alive&&!a.Dead)?"Recover unconscious community members first.":!ShelterWorkSafe()?"Secure the original refuge first.":InfrastructureState.BarricadeHp<12||!InfrastructureState.BarricadeEngaged?"Engage a refuge barricade with at least 12 HP.":ShelterSupplyCount("food")+FinaleFood("food",true)<LiveCommunity*2||ShelterSupplyCount("water")+FinaleFood("water",true)<LiveCommunity*2?"Provide two daily food and water units per living community member in the refuge or community backpacks.":TensionState.PendingAttacks.Count>0?"Resolve the pending refuge attack first.":"";
 public string FinaleActionReason(Actor actor,string action)
 {
  string console=action.StartsWith("heli-")?"heli":action is "crisis-nuclear" or "crisis-ragnarok"?"crisis":"home";string why=FinaleConsoleReason(actor,console);if(why!="")return why;var s=Finale;
  if(action.StartsWith("heli-")&&LowerMallProgress(211)?.Secured!=true)return "Secure the source helicopter pad first.";
  if(action is "readiness" or "winter-start"){if(action=="winter-start"&&s.WinterStart>=0)return "The 150-day watch is already running.";return BaseReadinessReason();}
  if(action=="winter-day")return s.WinterStart<0?"Begin the winter watch first.":BaseReadinessReason() is {} r&&r!=""?r:"";
  if(action=="heli-inspect")return s.Inspected?"The current inspection is already recorded.":!FinaleTools(actor)?"Carry the recovered aviation tools.":"";
  if(action.StartsWith("heli-repair-"))return !new[]{"rotor","pump","electrics"}.Contains(action[12..])?"Unknown airframe issue.":!s.Inspected?"Inspect the aircraft first.":s.Repairs.Contains(action[12..])?"This issue is already repaired.":s.RarePartNeeded?"Resolve the catastrophic component failure first.":CampaignMinutes<s.DelayUntil?"The repair setback lasts until "+JournalTimeText(s.DelayUntil)+".":!FinaleTools(actor)||!RoofMaterial(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId)?"Carry aviation tools and a real structural repair material.":"";
  if(action=="heli-salvage")return !s.RarePartNeeded?"No catastrophic component is missing.":!FinaleTools(actor)||!RoofMaterial(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId)?"Carry aviation tools and one structural material for the high-risk DC 18 salvage attempt.":"";
  if(action is "heli-launch" or "heli-airworthy"&&s.DelayUntil>CampaignMinutes)return "The repair delay must elapse before flight testing or departure. Spend a repair hour or continue surviving.";
  if(action=="heli-wait")return s.DelayUntil>CampaignMinutes?"":"No aircraft repair delay remains.";
  if(action=="heli-refuel")return s.Repairs.Count<3||s.RarePartNeeded?"Finish all three repairs and outstanding salvage first.":s.Fuel>=3?"The three-unit flight reserve is full.":!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)?"Carry an actual compatible fuel unit; transfer attempts spend it even on failure.":"";
  if(action=="heli-pilot")return actor.ClassIs("smart")&&!s.PilotId.Equals(actor.Id)&&FinaleTools(actor)?"":"A Smart survivor carrying aviation tools must complete the two-hour flight rehearsal; a trained pilot cannot repeat it.";
  if(action=="heli-airworthy")return s.Airworthy?"Airworthiness is already signed off.":s.Repairs.Count<3||s.RarePartNeeded||s.Fuel<(s.Efficient?2:3)?"Finish repairs and fill the required flight reserve.":s.PilotId==""||!Party.Any(a=>a.Id==s.PilotId&&a.Alive)?"Train a living expedition pilot.":"";
  if(action=="heli-launch")return !s.Airworthy?"Pass final airworthiness first.":s.ReadyUntil<CampaignMinutes?"Complete a fresh refuge readiness check (valid for 24 hours).":s.PilotId==""||!Party.Any(a=>a.Id==s.PilotId&&a.Alive)?"Bring the trained living pilot.":PartyCount>6||Party.Sum(a=>75+a.CarriedWeight)>600?"SkyEye 7 carries at most six people including the pilot and 600 kg including an assumed 75 kg per person.":Party.Any(a=>!a.Alive)?"Recover every boarding expedition member.":Party.Any(a=>a.Position.Distance(actor.Position)>5||!Line(a.Position,actor.Position))?"Gather the boarding party within 25 feet of the flight console.":FinaleFood("food")<PartyCount||FinaleFood("water")<PartyCount?"Carry one actual food and water unit per boarding survivor.":s.Fuel<(s.Efficient?2:3)?"Refill the flight reserve.":"";
  if(action=="freeze-final")return s.WinterStart<0||WinterDay<150?"Complete 150 actual days of the winter watch.":s.Emergency?"Choose an emergency crossing or another week after the failed check.":CampaignMinutes<s.NextFreezeAttempt?"The next freeze attempt is due at "+JournalTimeText(s.NextFreezeAttempt)+".":BaseReadinessReason();
  if(action=="freeze-wait")return s.Emergency?"":"No failed freeze attempt is awaiting a decision.";
  if(action=="freeze-emergency")return !s.Emergency?"Fail the normal freeze check before choosing the emergency route.":!HasDialogueFlag("flag.exterior.26.return")&&!HasDialogueFlag("flag.exterior.vehicle-ready")?"Establish the powered sewer return or a working exterior vehicle as your approach route.":!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)||ShelterSupplyCount("food")<LiveCommunity||ShelterSupplyCount("water")<LiveCommunity?"Stage one fuel unit and a refuge food/water unit per living community member for the crossing.":"";
  if(action is "crisis-nuclear" or "crisis-ragnarok")return s.Crisis!=""?"A saved terminal countdown is already running.":TensionState.Score<91||DiscoveredEvidence.Count<3?"Extreme tension (91+) and three discovered clues are required before accepting this optional terminal crisis.":"";
  if(action=="crisis-escape")return s.Crisis==""?"No terminal crisis is active.":!HasDialogueFlag("flag.exterior.26.return")&&!HasDialogueFlag("flag.exterior.vehicle-ready")?"Establish a real sewer return or working exterior vehicle before evacuation.":!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)||FinaleFood("food")<PartyCount||FinaleFood("water")<PartyCount?"Carry a real fuel unit and one food/water unit per expedition member.":"";
  if(action=="cosmic-core")return actor.Inventory.Contains("item.explosive.095")?"":"No actual Unstable Cosmic Core is in this backpack. Browsing its catalogue entry does not grant one.";
  return "Unknown campaign action.";
 }
 FinaleRoll FinaleCheck(Actor a,string action,int dc,Skill skill,int bonus=0,int minutes=15,int fuel=0,int material=0)
 {
  int natural=Roll(20),modifier=a.SkillModifier(skill)+bonus;var r=new FinaleRoll{Action=action,Minute=CampaignMinutes,Roll=natural,Bonus=modifier,Dc=dc,Success=natural+modifier>=dc,ActorId=a.Id,FuelSpent=fuel,MaterialSpent=material};Finale.Rolls.Add(r);CampaignMinutes+=natural==20?Math.Max(1,minutes/2):minutes;Message(MessageCategory.Roll,$"{action}: d20 {natural} + {modifier} vs DC {dc}: {(r.Success?"success":"failure")}");return r;
 }
 public bool PerformFinaleAction(Actor actor,string action)
 {
  string why=FinaleActionReason(actor,action);if(why!=""){LogSystem(why,true);return false;}var s=Finale;int pressure=TensionState.Score>70?2:0;string material=Campaign!.Infrastructure!.GeneratorCost[0].ItemId;string fuel=Campaign.Infrastructure.FuelItemId;
  switch(action)
  {
   case "readiness":var ready=FinaleCheck(actor,action,13+pressure,Skill.Survival,(s.Morale>=60?2:0)+(InfrastructureState.BarricadeHp>=20?1:0),30);if(ready.Success)s.ReadyUntil=CampaignMinutes+1440;break;
   case "heli-inspect":var inspect=FinaleCheck(actor,action,12,Skill.Mechanics,actor.ClassIs("smart")?2:0);s.Inspected=true;s.HiddenFlaw=!inspect.Success;break;
   case "heli-salvage":RoofConsume(actor,material);var salvage=FinaleCheck(actor,action,18+pressure,Skill.Mechanics,minutes:60,material:1);if(salvage.Success)s.RarePartNeeded=false;ChangeTension(Roll(6),"High-risk aircraft salvage");break;
   case "heli-wait":foreach(var a in Party.Where(a=>a.Alive)){foreach(var use in new[]{"food","water"})if(use=="food"?!a.Needs.FoodTaken:!a.Needs.WaterTaken){var owner=Party.FirstOrDefault(p=>p.Inventory.Items.Any(i=>SupplyUse(i)==use));var stack=owner?.Inventory.Items.First(i=>SupplyUse(i)==use);if(owner!=null&&stack!=null)UseSurvivalSupply(owner,a,use,stack.InstanceId);}}CampaignMinutes+=Math.Min(60,s.DelayUntil-CampaignMinutes);break;
   case "heli-refuel":RoofConsume(actor,fuel);var transfer=FinaleCheck(actor,action,(s.Efficient?11:13)+pressure,Skill.Mechanics,fuel:1);if(transfer.Success)s.Fuel++;else ChangeTension(Roll(6),"Fuel transfer spill");break;
   case "heli-pilot":var pilot=FinaleCheck(actor,action,15+pressure,Skill.Technology,minutes:120);if(pilot.Success)s.PilotId=actor.Id;break;
   case "heli-airworthy":var air=FinaleCheck(actor,action,15+pressure,Skill.Mechanics,s.FlightBonus);if(air.Success&&!s.HiddenFlaw)s.Airworthy=true;else if(s.HiddenFlaw){s.HiddenFlaw=false;s.Followup=true;Message(MessageCategory.Narrative,"The final test exposes the hidden inspection flaw. Corrective sign-off is still required.");}break;
   case "heli-launch":
    if(s.Followup){var follow=FinaleCheck(actor,"heli-followup",14+pressure,Skill.Mechanics);if(!follow.Success){s.Airworthy=false;break;}s.Followup=false;}if(CampaignEnded)break;
    int needed=s.Efficient?2:3;s.Fuel-=needed;s.FuelSpent+=needed;ConsumeFinaleTravelSupplies(false);CampaignMinutes+=45;ProcessSurvivalTime();if(!CampaignEnded)FinishCampaign("ending.helicopter","SkyEye 7 reaches The Island","The expedition crosses the dark water in the repaired news helicopter. The Island offers landfall, not proof of a cure or a single explanation for the outbreak.",false,needed);break;
   case "winter-start":s.WinterStart=CampaignMinutes;s.NextFreezeAttempt=CampaignMinutes+150*1440;ProcessFinaleTime();break;
   case "winter-day":AdvanceWinterDay();break;
   case "freeze-final":var final=FinaleCheck(actor,action,15+pressure,Skill.Survival,s.FinalWinterBonus+(s.Morale>=60?2:s.Morale<25?-2:0)+(InfrastructureState.BarricadeHp>=20?1:0),30);ProcessSurvivalTime();if(CampaignEnded)break;if(final.Success)FinishCampaign("ending.freeze","The winter crossing","After 150 days of the watch, frozen streets open a route to a waiting vessel. The surviving community reaches The Island together. The recovered clues remain contradictory.",true,0);else s.Emergency=true;break;
   case "freeze-wait":s.Emergency=false;s.NextFreezeAttempt=CampaignMinutes+7*1440;Message(MessageCategory.Narrative,"The community chooses another seven real days of survival before trying the freeze crossing again.");break;
   case "freeze-emergency":RoofConsume(actor,fuel);ConsumeFinaleTravelSupplies(true);var emergency=FinaleCheck(actor,action,18+pressure,Skill.Survival,s.FinalWinterBonus,60,fuel:1);ProcessSurvivalTime();if(!CampaignEnded)FinishCampaign(emergency.Success?"ending.freeze-emergency":"ending.winter-lost",emergency.Success?"The last crossing":"Winter closes the route",emergency.Success?"The surviving community makes the risky approach and boards the waiting escape vessel. Its passage to The Island is hard-won.":"The emergency approach fails. Supplies spent on the attempt cannot be recovered; the campaign ends with the escape route lost.",emergency.Success,1);break;
   case "crisis-nuclear":case "crisis-ragnarok":s.Crisis=action[7..];s.CrisisStart=CampaignMinutes;s.CrisisDeadline=CampaignMinutes+60*(s.Crisis=="nuclear"?RollDamage(2,6):Roll(12));AddStoryJournalEntry("finale.crisis",s.Crisis=="nuclear"?"Containment measures enacted":"Ragnarok Protocol",$"A real terminal countdown is running. Deadline: {JournalTimeText(s.CrisisDeadline)}. Escape through an established route before it expires.","Final escape");break;
   case "crisis-escape":RoofConsume(actor,fuel);ConsumeFinaleTravelSupplies(false);CampaignMinutes+=30;ProcessSurvivalTime();if(!CampaignEnded)FinishCampaign("ending.exclusion-zone","Beyond the exclusion zone","The expedition uses its established vehicle or utility route to leave the threatened mall before the deadline. Its destination is uncertain; this is survival outside the exclusion zone, not confirmed Island landfall.",false,1);break;
   case "cosmic-core":RoofConsume(actor,"item.explosive.095");FinishCampaign("ending.erasure","The last light goes out","The physical Unstable Cosmic Core is activated. The setting collapses into itself. There is no escape, payout or alternate mystery solution.",false,0);break;
   default:
    if(action.StartsWith("heli-repair-")){RoofConsume(actor,material);var repair=FinaleCheck(actor,action,14+pressure,Skill.Mechanics,minutes:60,material:1);ChangeTension(RollDamage(2,6)+Roll(6),"Aircraft repair noise");if(repair.Success){s.Repairs.Add(action[12..]);if(s.Repairs.Count==3){var pad=Campaign.LowerMall!.Sites.Single(d=>d.Number==211);var old=pad.Actions.Single(a=>a.Effect=="roof-repair");var progress=LowerMallProgress(211)!;if(!progress.Resolved.Contains(old.Id))progress.Resolved.Add(old.Id);if(!progress.Events.Contains("airframe-repaired"))progress.Events.Add("airframe-repaired");SetDialogueFlag("flag.roof.helicopter-airframe-repaired");SyncLowerMall();}}if(repair.Roll==1||s.Repairs.Count==3&&repair.Success){repair.TableRoll=Roll(20);ApplyHelicopterOutcome(repair.TableRoll);}EmitLocalNoise(actor,"door",100);}break;
  }
  if(!CampaignEnded){ProcessSurvivalTime();SyncContracts();AssignMissingItemIds();}return true;
 }
 void ApplyHelicopterOutcome(int roll)
 {
  var s=Finale;if(roll<=2)s.RarePartNeeded=true;else if(roll<=5){s.DelayUntil=CampaignMinutes+1440;if(Campaign?.Tension is {} t){var h=t.AttackHordeIds.FirstOrDefault(id=>!ReleasedHordes.Contains(id)&&!TensionState.PendingAttacks.Contains(id));if(h!=null)TensionState.PendingAttacks.Add(h);}}else if(roll<=10)s.Followup=true;else if(roll<=19&&roll>=16)s.FlightBonus=2;else if(roll==20){s.Efficient=true;ChangeTension(-10,"Exceptional aircraft overhaul");}AddStoryJournalEntry("finale.repair."+Finale.Rolls.Count,"Aircraft repair outcome "+roll,roll<=2?"Catastrophic component failure: complete DC 18 salvage.":roll<=5?"One-day delay; one finite existing attack group is alerted if available.":roll<=10?"Unstable systems: a DC 14 follow-up is required during departure.":roll<=15?"Standard repairs.":roll<=19?"Smooth repairs: +2 airworthiness.":"Exceptional overhaul: reduced refuel DC and two-unit rather than three-unit flight reserve.","Final escape");
 }
 void ConsumeFinaleTravelSupplies(bool community)
 {
  foreach(var use in new[]{"food","water"}){int left=community?LiveCommunity:PartyCount;foreach(var bag in (community?new[]{ShelterHomeStockpile()!.Contents}:(IEnumerable<InventoryState>)Party.Select(a=>a.Inventory))){foreach(var stack in bag.Items.Where(i=>SupplyUse(i)==use).ToArray()){while(stack.Quantity>0&&left>0){ConsumeSupplyUnit(bag,stack);left--;}if(left==0)break;}if(left==0)break;}}
 }
 public void ProcessFinaleTime()
 {
  if(!FinaleEnabled||CampaignEnded)return;var s=Finale;
  if(s.CrisisDeadline>=0&&CampaignMinutes>=s.CrisisDeadline){FinishCampaign(s.Crisis=="nuclear"?"ending.nuclear":"ending.purge",s.Crisis=="nuclear"?"Containment strike":"The mall falls silent","The warned deadline expires before evacuation. The mall is consumed. The final record preserves the expedition's choices and unresolved evidence.",false,0);return;}
  if(s.WinterStart<0)return;if(!CommunityMembers.Any(a=>a.Alive)){FinishCampaign("ending.winter-lost","The watch falls silent","No one survives to make the winter crossing.",false,0);return;}int due=Math.Min(1040,1+Math.Max(0,CampaignMinutes-s.WinterStart)/10080);while(s.Weeks.Count<due&&!CampaignEnded)ResolveWinterWeek(s.Weeks.Count+1);
 }
 void ResolveWinterWeek(int week)
 {
  var s=Finale;var leader=CommunityMembers.Where(a=>a.Alive).OrderByDescending(a=>a.SkillModifier(Skill.Survival)).FirstOrDefault();if(leader==null)return;var builder=CommunityMembers.Where(a=>a.Alive).OrderByDescending(a=>a.SkillModifier(Skill.Mechanics)).First();
  int roll=DailyDie(s.Seed,week,1,20);var e=new WinterWeek{Week=week,Minute=s.WinterStart+(week-1)*10080,EventRoll=roll,SupplyRoll=DailyDie(s.Seed,week,2,20),SupplyBonus=leader.SkillModifier(Skill.Survival)+(roll is >=6 and <=8?2:0),SupplyDc=12+(TensionState.Score>70?2:0),FortRoll=DailyDie(s.Seed,week,3,20),FortBonus=builder.SkillModifier(Skill.Mechanics)};s.Weeks.Add(e);
  var home=ShelterHomeStockpile();int members=Math.Max(1,LiveCommunity);
  if(e.SupplyRoll+e.SupplyBonus<e.SupplyDc){e.FoodLost+=LoseWinterSupply("food",Math.Max(1,members));s.Morale=Math.Max(0,s.Morale-5);}
  if(e.FortRoll+e.FortBonus<13){InfrastructureState.BarricadeHp=Math.Max(0,InfrastructureState.BarricadeHp-4);if(InfrastructureState.BarricadeHp==0){InfrastructureState.BarricadeEngaged=false;if(InfrastructureDoor() is {} door)door.Used=true;}}
  if(roll<=2){e.FoodLost+=LoseWinterSupply("food",(int)Math.Ceiling(Math.Min(ShelterSupplyCount("food"),members*7)*.2));e.WaterLost+=LoseWinterSupply("water",(int)Math.Ceiling(Math.Min(ShelterSupplyCount("water"),members*7)*.2));e.Result="Supply sabotage: twenty percent of the staged week's supplies is lost.";}
  else if(roll<=5){bool defense=DailyDie(s.Seed,week,4,20)+builder.SkillModifier(Skill.Mechanics)>=14;if(!defense){InfrastructureState.BarricadeHp=Math.Max(0,InfrastructureState.BarricadeHp-4);foreach(var a in CommunityMembers.Where(a=>a.Alive))a.Hp=Math.Max(1,a.Hp-2);e.FoodLost+=LoseWinterSupply("food",members);}e.Result=defense?"The weekly incursion is held at the perimeter.":"Incursion losses: barricade damage, wounds and food expenditure.";}
  else if(roll<=8){s.Morale=Math.Min(100,s.Morale+5);string id="actor.finale.volunteer."+s.Volunteers.Count;if(s.Volunteers.Count<2&&PartyCount<6&&CurrentTravelDestinationId==Campaign!.Shelter!.AreaId&&ResidentArrival() is {} cell){AddCampaignActor(Campaign.Actors.Single(a=>a.Id==id),cell);s.Volunteers.Add(id);AssignMissingItemIds();}e.Result="Unexpected reinforcements: +2 weekly supply check; up to two finite volunteers can join while the expedition is at home with room.";}
  else if(roll<=11){bool repaired=DailyDie(s.Seed,week,5,20)+builder.SkillModifier(Skill.Mechanics)>=15&&RoofMaterial(builder,Campaign!.Infrastructure!.GeneratorCost[0].ItemId);if(repaired)RoofConsume(builder,Campaign!.Infrastructure!.GeneratorCost[0].ItemId);else {InfrastructureState.Running=false;e.WaterLost+=LoseWinterSupply("water",members);}e.Result=repaired?"Equipment breakdown repaired using a physical material.":"Equipment breakdown: power stops and water supplies are lost.";}
  else if(roll<=14){s.Morale=Math.Min(100,s.Morale+5);ChangeTension(-5,"Calm winter week");e.Result="Calm before the storm: +5 morale and reduced tension.";}
  else if(roll<=17){e.FoodAdded=AddWinterSupply("food",Math.Min(20,ShelterSupplyCount("food")/10));e.WaterAdded=AddWinterSupply("water",Math.Min(20,ShelterSupplyCount("water")/10));e.Result="Scavenger windfall: ten percent extra physical supplies, bounded to twenty per category; no resource XP.";}
  else if(roll<=19){e.WaterLost+=LoseWinterSupply("water",members);s.Morale=Math.Max(0,s.Morale-3);e.Result="Minor sabotage: the water category's weekly margin is lost.";}
  else{s.FinalWinterBonus=Math.Min(6,s.FinalWinterBonus+3);e.FoodAdded=AddWinterSupply("food",4);e.WaterAdded=AddWinterSupply("water",4);e.Result="Miraculous breakthrough: physical supplies and +3 final endurance (capped at +6).";}
  if(InfrastructureState.BarricadeHp==0){InfrastructureState.BarricadeEngaged=false;if(InfrastructureDoor() is {} d)d.Used=true;}
  AddStoryJournalEntry("finale.week."+week,"Winter watch: week "+week,$"D20 {roll}; supply {e.SupplyRoll}+{e.SupplyBonus} vs {e.SupplyDc}; upkeep {e.FortRoll}+{e.FortBonus} vs 13. {e.Result}","Winter watch");AssignMissingItemIds();
 }
 int LoseWinterSupply(string use,int q)
 {
  var h=ShelterHomeStockpile();if(h==null)return 0;int spent=0;foreach(var stack in h.Contents.Items.Where(i=>SupplyUse(i)==use).ToArray()){while(stack.Quantity>0&&spent<q){ConsumeSupplyUnit(h.Contents,stack);spent++;}if(spent==q)break;}return spent;
 }
 int AddWinterSupply(string use,int q)
 {
  var h=ShelterHomeStockpile();if(h==null||q<=0)return 0;var stack=CreateItemStack("item.finale.weekly-"+use,q);if(!h.Contents.CanAdd(stack))return 0;h.Contents.Add(stack);return q;
 }
 void AdvanceWinterDay()
 {
  for(int hour=0;hour<24&&!CampaignEnded;hour++)
  {
   if(Mode!=Mode.Explore||!ShelterWorkSafe()||TensionState.PendingAttacks.Count>0)break;
   foreach(var a in CommunityMembers.Where(a=>a.Alive))ProvisionResident(a,CampaignMinutes,true);
   CampaignMinutes+=60;ProcessSurvivalTime();if(CampaignEnded)break;ProcessFinaleTime();
   if(CommunityMembers.Any(a=>!a.Alive||a.Needs.Exhaustion>0)){Message(MessageCategory.Rest,"The winter advance pauses: recover the crew or replenish supplies.");break;}
  }
 }
 void FinishCampaign(string id,string title,string text,bool wholeCommunity,int fuel)
 {
  if(CampaignEnded)return;var living=CommunityMembers.Where(a=>!a.Dead).ToArray();bool success=id is "ending.helicopter" or "ending.freeze" or "ending.freeze-emergency" or "ending.exclusion-zone";var departing=success?(wholeCommunity?living:Party.Where(a=>a.Alive).ToArray()):[];
  Finale.Ending=new(){Id=id,Title=title,Text=text,Minute=CampaignMinutes,Theory=Finale.Theory,Evacuated=departing.Select(a=>a.Id).ToList(),LeftBehind=living.Except(departing).Select(a=>a.Id).ToList(),Dead=CommunityMembers.Where(a=>a.Dead).Select(a=>a.Id).ToList(),EvidenceIds=DiscoveredEvidence.Select(e=>e.Id).ToList(),SecuredLocations=LowerMallStates.Count(s=>s.Secured),ResourceXp=ResourceXpAwarded,FuelConsumed=Math.Max(Finale.FuelSpent,fuel),Factions=Campaign!.Social?.Factions.ToDictionary(f=>f.Id,f=>FactionStanding(f.Id))??[],Trust=CommunityMembers.ToDictionary(a=>a.Id,a=>SurvivorTrust(a.Id))};StopAll();ClearReactionInterrupts();Order.Clear();Mode=success?Mode.Victory:Mode.Defeat;AddStoryJournalEntry("finale.completed",title,text,"Campaign ending");
 }
 void ValidateFinale()
 {
  var s=Finale;if(s==null||s.Clues==null||s.Repairs==null||s.Rolls==null||s.Weeks==null||s.Volunteers==null)throw new InvalidDataException("Missing finale state.");
  if(!FinaleEnabled){if(s.Seed!=0||s.Clues.Count+s.Repairs.Count+s.Rolls.Count+s.Weeks.Count+s.Volunteers.Count>0||s.Ending!=null||s.WinterStart!=-1||s.Crisis!="")throw new InvalidDataException("Finale state on an older campaign.");return;}
  if(s.Seed==0||!FinaleTheories.Contains(s.Theory)||s.Morale is <0 or >100||s.Fuel is <0 or >3||s.Repairs.Distinct().Count()!=s.Repairs.Count||s.Repairs.Any(r=>r is not ("rotor" or "pump" or "electrics"))||s.Rolls.Count>20000||s.Weeks.Count>1040||s.FinalWinterBonus is <0 or >6||s.FlightBonus is not (0 or 2)||s.TheoryMinute>CampaignMinutes||s.ReadyUntil>CampaignMinutes+1440||s.Clues.Any(k=>!Campaign!.Finale!.Evidence.Any(e=>e.Location==k.Key&&e.Id==k.Value)||k.Value!=Campaign!.Finale!.Evidence.Where(e=>e.Location==k.Key).ToArray()[DailyDie(s.Seed,k.Key,71,Campaign.Finale.Evidence.Count(e=>e.Location==k.Key))-1].Id))throw new InvalidDataException("Invalid finale progress or mystery variant.");
  foreach(var r in s.Rolls)if(r.Minute<Campaign!.StartMinute||r.Minute>CampaignMinutes||r.Roll is <1 or >20||r.Dc is <1 or >30||r.Success!=(r.Roll+r.Bonus>=r.Dc)||!Campaign.Actors.Any(a=>a.Id==r.ActorId)||r.FuelSpent is <0 or >1||r.MaterialSpent is <0 or >1||r.TableRoll is <0 or >20)throw new InvalidDataException("Invalid recorded finale check.");
  if(s.Repairs.Any(key=>!s.Rolls.Any(r=>r.Action=="heli-repair-"+key&&r.Success&&r.MaterialSpent==1))||s.Inspected&&!s.Rolls.Any(r=>r.Action=="heli-inspect")||s.PilotId!=""&&!s.Rolls.Any(r=>r.Action=="heli-pilot"&&r.ActorId==s.PilotId&&r.Success)||s.Airworthy&&!s.Rolls.Any(r=>r.Action=="heli-airworthy"&&r.Success))throw new InvalidDataException("Unearned aircraft preparation.");
  int loaded=s.Rolls.Count(r=>r.Action=="heli-refuel"&&r.Success);int flown=s.FuelSpent;if(flown is <0 or >3||s.Fuel!=loaded-flown)throw new InvalidDataException("Flight reserve disagrees with physical transfer checks.");
  if(s.WinterStart< -1||s.WinterStart>CampaignMinutes||s.WinterStart<0&&s.Weeks.Count>0)throw new InvalidDataException("Invalid winter start.");
  for(int i=0;i<s.Weeks.Count;i++){var w=s.Weeks[i];if(w.Week!=i+1||w.Minute!=s.WinterStart+i*10080||w.Minute>CampaignMinutes||w.EventRoll!=DailyDie(s.Seed,i+1,1,20)||w.SupplyRoll!=DailyDie(s.Seed,i+1,2,20)||w.FortRoll!=DailyDie(s.Seed,i+1,3,20)||w.FoodLost<0||w.WaterLost<0||w.FoodAdded is <0 or >20||w.WaterAdded is <0 or >20)throw new InvalidDataException("Invalid or rerolled winter week.");}
  if(s.Crisis is not ("" or "nuclear" or "ragnarok")||s.Crisis!=""&&(s.CrisisStart<Campaign!.StartMinute||s.CrisisStart>CampaignMinutes||s.CrisisDeadline<=s.CrisisStart||s.CrisisDeadline>s.CrisisStart+720))throw new InvalidDataException("Invalid terminal warning.");
  if(s.Ending is {} end){if(end.Factions==null||end.Trust==null||end.Factions.Any(f=>Campaign!.Social?.Factions.Any(d=>d.Id==f.Key)!=true||f.Value!=FactionStanding(f.Key))||end.Trust.Any(t=>!CommunityMembers.Any(a=>a.Id==t.Key)||t.Value!=SurvivorTrust(t.Key)))throw new InvalidDataException("Invalid final social snapshot.");string[] ids=["ending.helicopter","ending.freeze","ending.freeze-emergency","ending.winter-lost","ending.exclusion-zone","ending.nuclear","ending.purge","ending.erasure"];if(!ids.Contains(end.Id)||end.Minute>CampaignMinutes||end.Minute<Campaign!.StartMinute||!FinaleTheories.Contains(end.Theory)||end.Evacuated.Intersect(end.LeftBehind).Any()||end.EvidenceIds.Distinct().Count()!=end.EvidenceIds.Count||end.EvidenceIds.Any(id=>!DiscoveredEvidence.Any(e=>e.Id==id))||end.Evacuated.Concat(end.LeftBehind).Concat(end.Dead).Any(id=>!CommunityMembers.Any(a=>a.Id==id))||Mode is not (Mode.Victory or Mode.Defeat))throw new InvalidDataException("Invalid final campaign record.");if(end.Id=="ending.helicopter"&&(!s.Airworthy||s.PilotId=="")||end.Id.StartsWith("ending.freeze")&&WinterDay<150||end.Id is "ending.nuclear" or "ending.purge"&&CampaignMinutes<s.CrisisDeadline)throw new InvalidDataException("Ending prerequisites were not earned.");}
 }
}
