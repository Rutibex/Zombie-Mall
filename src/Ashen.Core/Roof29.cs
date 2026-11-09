using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed partial class Actor
{
 int Roof29GearSkill(Skill skill)=>(skill==Skill.Athletics&&Inventory.Items.Any(i=>i.ItemId=="item.roof.15.loot.1")?1:0)+(skill==Skill.Survival&&Character.HasCondition("roof29-blueprints")?2:0)-(skill==Skill.Perception&&Character.HasCondition("roof29-fog")?4:0)-(Character.HasCondition("roof29-panic")?2:0);
}
public sealed partial class World
{
 static bool Roof29Site(LowerMallSite? d)=>d?.Level=="roof"&&d.DisplayNumber is >=14 and <=25;
 bool Roof29CrossingBlocked(WorldPoint p)=>CurrentTravelDestinationId=="area.roof.21"&&LowerMallProgress(221) is {} s&&!s.Events.Contains("crossed")&&!s.Events.Contains("bridge-repaired")&&p.Y>=12-BodyRadius&&p.Y<18+BodyRadius;
 bool Roof29Own(Actor a,int n,int j)=>a.Inventory.Items.Concat(a.Equipment.All()).Any(i=>i.ItemId==$"item.roof.{n:00}.loot.{j}"&&i.Wear<100);
 bool Roof29Fuelled(LowerMallState s)=>RoofGridOnline||s.RoofTimers.GetValueOrDefault("powerUntil")>CampaignMinutes;
 [JsonIgnore] public string Roof29Status=>CurrentLowerMallSite is {} d&&Roof29Site(d)&&LowerMallProgress(d.Number) is {} s?
  d.DisplayNumber==20?$"RADIO POWER: {(Roof29Fuelled(s)?"ONLINE":"OFFLINE")} | DISTRESS: {(s.Resolved.Contains("action.roof.20.device.signal")?"SENT":"UNSENT")}":
  d.DisplayNumber==22?$"LIFT: {(s.Events.Contains("repaired")?"REPAIRED":"BROKEN")} | POWER: {(Roof29Fuelled(s)?"ONLINE":"OFFLINE")} | BRAKE: {(s.Events.Contains("lift-trapped")?"LOCKED":"CLEAR")}":
  d.DisplayNumber==21?$"CROSSING: {(s.Events.Contains("bridge-repaired")?"REPAIRED":s.Events.Contains("crossed")?"CROSSED":"UNSECURED")} | GARAGE: {(Campaign?.SchemaVersion>=28?"AVAILABLE":"FUTURE REGION")}":
  d.DisplayNumber==23?$"VENT: {(s.Events.Contains("sealed")?"SEALED":"OPEN")} | OCCUPANT: SAVED BRANCH {s.EncounterRoll}":"ROOF: "+(s.Secured?"SECURED":"UNSECURED"):"";
 void ConfigureRoof29Container(Feature f)
 {
  if(f.Id=="feature.roof.22.loot.5")f.Contents.Items.Single(i=>i.Definition.Kind==ItemKind.Weapon).Wear=100;
  if(f.Id=="feature.roof.24.loot.1")
  {
   var gun=f.Contents.Items.Single(i=>i.Definition.MagazineSize>0);var ammo=gun.LoadedAmmunition!;int extra=ammo.Quantity-1;if(extra>0){var loose=ammo.Clone();loose.Quantity=extra;loose.InstanceId="";loose.ResourceUnits=ammo.ResourceUnits.Skip(1).ToList();ammo.Quantity=1;ammo.ResourceUnits=ammo.ResourceUnits.Take(1).ToList();f.Contents.Add(loose);}
  }
 }
 void EnterRoof29(LowerMallSite d,LowerMallState s)
 {
  if(!Roof29Site(d))return;int n=d.DisplayNumber;s.BranchRoll=Roll(6);s.BonusRoll=Roll(4);s.RoofTimers["eventAt"]=CampaignMinutes+(n==18?5:10);s.RoofTimers["hourAt"]=CampaignMinutes+60;s.RoofRolls["signal"]=Roll(6);s.RoofRolls["bonus"]=Roll(8);
  if(n==19&&s.EncounterRoll==6||n==25&&(s.EncounterRoll==5||s.HazardId.EndsWith(".2"))&&s.BranchRoll<=3)s.Events.Add("survivor-present");
  if(n==16)s.Events.Add("alcohol-active");if(n==25&&s.HazardId.EndsWith(".3"))s.Events.Add("chlorine-active");
  if(n==25){var stack=Features.Single(f=>f.Id=="feature.roof.25.loot.4").Contents.Items.Single();int q=RollDamage(2,6);stack.Quantity=q;stack.ResourceUnits=stack.ResourceUnits.Take(q).ToList();s.RoofRolls["rounds"]=q;}
  if(n==24)
  {
   int keep=6+s.RoofRolls["bonus"];foreach(var f in Features.Where(f=>f.Id.StartsWith("feature.roof.24.loot."))){int j=int.Parse(f.Id.Split('.').Last());if(j>=7&&j!=keep)f.Contents.Items.Clear();}
   s.RoofTimers["flares"]=3;
  }
 }
 string Roof29Reason(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Roof29Site(d))return "";int n=d.DisplayNumber;string e=a.Effect;
  if(a.NpcActorId!=""&&!s.Events.Contains("survivor-present"))return "No living survivor is present on this saved branch.";
  if(n==21&&a.Kind=="loot"&&!s.Events.Contains("crossed")&&!s.Events.Contains("bridge-repaired")&&a.Id.EndsWith("loot.6"))return "Cross or repair the gap before searching the far-side rifle cache.";
  if(e=="roof29-climb"&&!Roof29Own(actor,15,2))return "Carry the recovered climbing harness and carabiners.";
  if(e=="roof29-blueprint"&&!Roof29Own(actor,15,6)||e=="roof29-drill"&&!Roof29Own(actor,15,3))return "Carry the recovered blueprint or usable drill for this interaction.";
  if(n==16&&e=="roof29-key"&&!Roof29Own(actor,16,7))return "Recover and carry the bartender's physical mystery key.";
  if(n==21&&e=="roof29-quiet"&&!s.Quiet)return "This crossing was noisy; its quiet repair bonus is unavailable.";
  if(e=="roof29-barricade"&&(!s.Quiet||Actors.Any(t=>t.Enemy&&t.Alive)))return "Clear the lounge without raising local noise before claiming the quiet barricade bonus.";
  if(e=="roof29-fence"&&!Roof29Own(actor,17,1))return "Carry the recovered bolt cutters to open the stairwell fence.";
  if(n==18&&e=="roof29-map"&&!Roof29Own(actor,18,5)||n==18&&e=="roof29-logs"&&!Roof29Own(actor,18,4))return "Carry the recovered security map or logbook.";
  if(e is "roof29-repair" or "roof29-generator" or "roof29-seal" or "roof29-bypass" or "roof29-escape"&&!RoofTools(actor))return "Carry a usable toolkit or wrench.";
  if(e=="roof29-seal"&&!RoofMaterial(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId))return "Carry one structural repair material to seal the damaged hatch.";
  if(n==20&&e=="roof29-generator"&&!RoofMaterial(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId))return "Carry one repair-material unit for the radio generator.";
  if(e=="roof29-refuel"&&(!RoofMaterial(actor,Campaign!.Infrastructure!.FuelItemId)||s.RoofTimers.GetValueOrDefault("powerUntil")>CampaignMinutes+1200))return "Carry compatible fuel; local reserve is limited to 24 hours.";
  if(n==20&&e is "roof29-decode" or "roof29-signal"&&!Roof29Fuelled(s))return "Energize the rooftop grid or repair and fuel the local radio generator.";
  if(n==20&&e=="roof29-decode"&&!Roof29Own(actor,20,1))return "Carry the encrypted tower hard drive.";
  if(n==20&&e=="roof29-signal"&&!Roof29Own(actor,20,0))return "Carry the recovered working ham radio.";
  if(n==21&&e=="roof29-repair"&&(!Roof29Own(actor,21,4)||!Roof29Own(actor,21,5)))return "Carry the bridge hammer/nails and recovered structural scrap.";
  if(n==21&&e is "roof29-rope" or "roof29-leap"&&s.Events.Contains("crossed"))return "The party has already crossed this gap.";
  if(n==22&&e=="roof29-repair"&&(!RoofMaterial(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId)||!RoofGridOnline&&!RoofMaterial(actor,Campaign.Infrastructure.FuelItemId)))return "Bring a structural repair unit and either rooftop grid power or a compatible fuel unit.";
  if(e=="roof29-release"&&!s.Events.Contains("lift-trapped"))return "The emergency brake is already clear.";
  if(n==22&&e=="roof29-pistol"&&!Roof29OwnBroken(actor,"item.roof.22.loot.5"))return "Carry the malfunctioning security revolver; it must still need repair.";
  if(n==22&&e=="roof29-key"&&!Roof29Own(actor,22,4))return "Carry the actual high-clearance security keycard.";
  if(n==23&&e=="roof29-food"&&(!Actors.Any(t=>t.Alive&&LowerTraits(t).Contains("roof29-hungerling"))||!actor.Inventory.Items.Any(i=>SupplyUse(i)=="food")))return "A living Hungerling and a real carried food portion are required.";
  if(n==23&&e=="roof29-manual"&&!Roof29Own(actor,23,2))return "Carry the recovered Safe Room Manual.";
  if(n==23&&e=="roof29-testimony"&&s.BranchRoll!=1)return "No dying witness survives on this saved branch.";
  if(n==24&&e=="roof29-tripwire"&&!s.HazardId.EndsWith(".3"))return "No airhorn tripwire was found on this saved entry branch.";
  if(n==24&&e=="roof29-trail"&&!s.HazardId.EndsWith(".2"))return "No blood-trail hazard was selected on this saved entry branch.";
  if(n==24&&e=="roof29-notebook"&&!Roof29Own(actor,24,2)||n==24&&e=="roof29-drive"&&!Roof29Own(actor,24,6))return "Carry the actual encrypted notebook or BioDyne drive.";
  if(n==24&&e=="roof29-flare"&&(!Roof29Own(actor,24,4)||s.RoofTimers.GetValueOrDefault("flares")<1))return "Carry the flare kit with at least one saved flare remaining.";
  if(n==25&&e=="roof29-laptop"&&!Roof29Own(actor,25,2))return "Carry the recovered DJ laptop with its remaining internal charge.";
  if(n==25&&e=="roof29-bar"&&Actors.Any(t=>t.Enemy&&t.Alive))return "Clear the active pool threats before securing the bar.";
  return "";
 }
 bool Roof29OwnBroken(Actor actor,string id)=>actor.Inventory.Items.Any(i=>i.ItemId==id&&i.Broken);
 void Roof29Attempt(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Roof29Site(d))return;
  if(d.DisplayNumber==19&&a.Effect=="roof29-fan")s.Events.Add("fan-attempt");
 }
 void Roof29Failure(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Roof29Site(d))return;s.Quiet=false;int n=d.DisplayNumber;string e=a.Effect;
  if(n==21&&e is "roof29-leap" or "roof29-rope") {if(!Roof29Own(actor,15,2))Hurt(actor,RollDamage(3,6),DamageType.Bludgeoning);LowerEffect(actor,"prone",6);Roof29Reinforce(d,s,RollDamage(2,6));ChangeTension(RollDamage(2,6),"Failed skybridge crossing");}
  if(n==15&&e=="roof29-climb"){LowerEffect(actor,"restrained",12);ChangeTension(Roll(4),"Harness catches a failed climb");}
  if(n==23&&e=="roof29-entry"){Hurt(actor,Roll(6),DamageType.Bludgeoning);ChangeTension(Roll(6),"Pressure door misopened");}
  if(n==19&&e=="roof29-fan")Hurt(actor,RollDamage(3,6),DamageType.Slashing);
 }
 void Roof29HazardResult(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a,bool pass)
 {
  if(!Roof29Site(d))return;int n=d.DisplayNumber,j=int.Parse(a.Id.Split('.').Last());
  if(n==19&&j==1&&s.Events.Contains("fan-off"))return;
  if(n==24&&j==3&&pass)s.Events.Add("tripwire-found");
  if(pass)return;
  if(n==14&&j==0)SpawnChapterNamed(d,s,"Drain Drowned One",Roll(3));
  if(n==17&&j==2)s.RoofTimers["reinforceAt"]=CampaignMinutes+1;
  if(n==18&&j==2)SpawnChapterNamed(d,s,"Locker Corpse",Math.Min(5,(s.Releases.FirstOrDefault(r=>r.EncounterId.EndsWith("locker-corpse"))?.Count??0)+1));
  if(n==19&&j==3||n==22&&j==0||n==24&&j==1){s.RoofTimers["reinforceAt"]=CampaignMinutes+1;}
  if(n==22&&j==3)s.Events.Add("lift-trapped");
  if(n==23&&j==2)SpawnChapterNamed(d,s,"Vent Dweller Ghoul");
  if(n==24&&j==0)SpawnChapterNamed(d,s,"Camp Rooftop Lurker");
  if(n==25&&j==2&&!s.Events.Contains("survivor-present"))SpawnChapterNamed(d,s,"Waterlogged Pool Shambler");
 }
 void Roof29Reinforce(LowerMallSite d,LowerMallState s,int count)
 {
  var enc=Campaign!.Encounters.First(e=>e.AreaId==d.AreaId&&e.ActorIds.Any(id=>Campaign.Actors.Single(a=>a.Id==id).Role=="Shamble"));int had=s.Releases.FirstOrDefault(r=>r.EncounterId==enc.Id)?.Count??0;SpawnLower(d,s,enc.Id,had+count,"reaction");
 }
 void Roof29Device(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Roof29Site(d))return;int n=d.DisplayNumber;string e=a.Effect;
  if(a.Kind=="loot"&&n==25&&a.Id.EndsWith("loot.4"))Message(MessageCategory.Narrative,$"The lockbox contains its saved {s.RoofRolls["rounds"]} real 9mm rounds.");
  switch(e)
  {
   case "roof29-drain":ChangeTension(RollDamage(2,6),"The Drain Whisperer answers");if(!SurvivorSave(actor,Ability.Wisdom,14,"drain mental influence"))LowerEffect(actor,"upper-paranoia",600);AddStoryJournalEntry("roof29.drain","The unseen Drain Whisperer","Only overlapping voices answer. No creature body or invented combat statistics are revealed.","Roof");break;
   case "roof29-collect":s.Events.Add("clean-water-secured");break;
   case "roof29-climb":s.Events.Add("climbed");SetDialogueFlag("flag.roof.15.climbed");break;
   case "roof29-blueprint":case "roof29-manual":LowerEffect(actor,"roof29-blueprints",0);break;
   case "roof29-drill":ChangeTension(10,"Five minutes of construction power-tool noise");s.Events.Add("hatch-open");break;
   case "roof29-bartender":s.Events.Add("bartender-answered");AddStoryJournalEntry("roof29.bartender","The last order","A key is revealed under the cocktail napkin. Recover it before opening the wine safe.","Roof");break;
   case "roof29-key":if(n==16)s.Events.Add("wine-safe-open");else {s.Events.Add("high-gate-open");SetDialogueFlag("flag.roof.22.high-gate");}break;
   case "roof29-karaoke":case "roof29-music":ChangeTension(RollDamage(2,6),"Rooftop music broadcasts");Roof29Reinforce(d,s,Roll(4));EmitLocalNoise(actor,"door",150);break;
   case "roof29-barricade":case "roof29-quiet":ChangeTension(-Roll(6),"Rooftop noise hazards disabled");s.Events.Add("quiet-defences");break;
   case "roof29-alarm":s.Events.Add("alarm-disabled");break;
   case "roof29-fence":SetDialogueFlag("flag.roof.17.fence");s.Events.Add("fence-open");break;
   case "roof29-distraction":ChangeTension(-Roll(6),"Finite dock distraction left behind");EmitLocalNoise(actor,"door",100);break;
   case "roof29-map":case "roof29-logs":case "roof29-notebook":case "roof29-drive":case "roof29-decode":case "roof29-laptop":SetDialogueFlag("flag."+d.Prefix+"."+e[7..]);AddStoryJournalEntry(a.Id,a.Name,"Recovered evidence is decoded and retained. Contradictory outbreak clues remain unresolved; this does not complete an ending.","Roof");break;
   case "roof29-fan":s.Events.Add("fan-off");if(s.HazardId.EndsWith(".1")&&!s.HazardResolved){s.HazardResolved=true;s.Resolved.Add(s.HazardId);}ChangeTension(-Roll(4),"Industrial fans isolated");break;
   case "roof29-seal":RoofConsume(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId);s.Events.Add("sealed");ChangeTension(-Roll(4),"Damaged ventilation sealed");break;
   case "roof29-bypass":s.Events.Add("bypass-open");SetDialogueFlag("flag.roof.19.bypass");break;
   case "roof29-rescue":LowerRecruit(a,s);s.Events.Remove("survivor-present");if(n==25){var rescued=Actors.FirstOrDefault(t=>t.Id==a.NpcActorId);if(rescued!=null)LowerEffect(rescued,"book-infected",86400);}break;
   case "roof29-generator":RoofConsume(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId);s.Events.Add("local-generator-repaired");break;
   case "roof29-refuel":RoofConsume(actor,Campaign!.Infrastructure!.FuelItemId);s.RoofTimers["powerUntil"]=Math.Max(CampaignMinutes,s.RoofTimers.GetValueOrDefault("powerUntil"))+240;break;
   case "roof29-align":ChangeTension(Roll(6),"Satellite dishes realigned");break;
   case "roof29-signal":ChangeTension(RollDamage(2,6),"One secure distress transmission");SetDialogueFlag("flag.roof.radio-distress");if(s.RoofRolls["signal"]<=3){s.Events.Add("contact-help");AddStoryJournalEntry("roof29.radio.reply","A possible friendly contact","A voice acknowledges the distress call. Identity and rescue remain unverified; campaign escape resolution follows later.","Roof");}else {s.Events.Add("contact-hostile");Roof29Reinforce(d,s,Roll(4));}break;
   case "roof29-rope":case "roof29-leap":s.Events.Add("crossed");SetDialogueFlag("flag.roof.21.crossed");break;
   case "roof29-repair":
    if(n==21){RoofConsume(actor,"item.roof.21.loot.5");s.Events.Add("bridge-repaired");s.Events.Add("crossed");SetDialogueFlag("flag.roof.21.repaired");}
    if(n==22){RoofConsume(actor,Campaign!.Infrastructure!.GeneratorCost[0].ItemId);if(!RoofGridOnline){RoofConsume(actor,Campaign.Infrastructure.FuelItemId);s.RoofTimers["powerUntil"]=CampaignMinutes+240;}s.Events.Add("repaired");SetDialogueFlag("flag.roof.22.repaired");ChangeTension(RollDamage(2,6),"Cargo elevator restored");}
    break;
   case "roof29-release":s.Events.Remove("lift-trapped");break;
   case "roof29-ladder":if(!SurvivorSave(actor,Ability.Dexterity,14,"shaft floor")){if(!Roof29Own(actor,15,2))Hurt(actor,RollDamage(6,6),DamageType.Bludgeoning);LowerEffect(actor,"restrained",12);SpawnChapterNamed(d,s,"Horrifying Maw");}break;
   case "roof29-pistol":actor.Inventory.Items.Single(i=>i.ItemId=="item.roof.22.loot.5").Wear=0;break;
   case "roof29-entry":s.Events.Add("door-open");if(s.EncounterRoll is 4 or 5)SpawnChapterNamed(d,s,"Hungerling");break;
   case "roof29-food":var food=actor.Inventory.Items.First(i=>SupplyUse(i)=="food");RoofConsume(actor,food.ItemId);foreach(var h in Actors.Where(t=>t.Alive&&LowerTraits(t).Contains("roof29-hungerling")))if(!SurvivorSave(h,Ability.Wisdom,12,"irresistible hunger"))LowerEffect(h,"stunned",18);break;
   case "roof29-testimony":AddStoryJournalEntry("roof29.final-words","The dying survivor's words",new[]{"They ate it… and it ate them.","I locked the real door. This one was already open.","The meat wasn't fresh… but neither were we.","You hear them knocking, too? You're next."}[s.BonusRoll-1],"Roof");break;
   case "roof29-tripwire":s.Events.Add("tripwire-disabled");if(s.HazardId.EndsWith(".3")&&!s.HazardResolved){s.HazardResolved=true;s.Resolved.Add(s.HazardId);}break;
   case "roof29-trail":s.Events.Add("trail-investigated");break;
   case "roof29-escape":s.Events.Add("fire-escape-stabilized");SetDialogueFlag("flag.roof.24.escape-ready");break;
   case "roof29-flare":s.RoofTimers["flares"]--;ChangeTension(Roll(6),"One real camp flare used");break;
   case "roof29-chlorine":s.Events.Remove("chlorine-active");s.Events.Add("chlorine-isolated");foreach(var p in Party){p.Character.SetCondition("roof29-chlorine","Chlorine",false);LowerMallEffects.RemoveAll(e=>e.ActorId==p.Id&&e.Condition=="roof29-chlorine");}break;
   case "roof29-bar":s.Events.Add("bar-secured");ChangeTension(-Roll(6),"Corpse pit cleared and bar barricaded");break;
   case "roof29-boundary":case "roof29-garage":Message(MessageCategory.Travel,Campaign?.SchemaVersion>=28?"Use the nearby marked exterior transition after establishing the local access route.":"The exterior and parking garage are not playable in this build. Repaired access and recovered keys persist for the later exterior phases.");break;
  }
 }
 string Roof29TravelReason(string destination)
 {
  var route=CampaignConnectionTo(destination);if(route?.Id.StartsWith("route.roof.lift")==true)
  {
   var lift=LowerMallProgress(222);if(lift==null||!lift.Events.Contains("repaired"))return "Repair the rooftop cargo elevator controls first.";
   if(lift.Events.Contains("lift-trapped"))return "Release the cargo elevator emergency brake at the rooftop controls.";
   if(!RoofGridOnline&&lift.RoofTimers.GetValueOrDefault("powerUntil")<=CampaignMinutes+route.Minutes)return "The cargo lift needs live rooftop grid power or remaining local generator fuel.";
  }
  return "";
 }
 void Roof29Travel(CampaignConnection route)
 {
  if(route.Id.StartsWith("route.roof.lift")){ChangeTension(Roll(6),"Cargo elevator journey");if(LowerMallProgress(222) is {} s)s.Events.Add("lift-used");}
 }
 void TickRoof29()
 {
  if(Campaign?.SchemaVersion<27||!LowerMallEnabled)return;
  foreach(var s in LowerMallStates.Where(s=>s.Number==223&&s.Events.Contains("door-open")&&!s.Events.Contains("sealed")))
  {
   if(CampaignMinutes>=s.RoofTimers.GetValueOrDefault("hourAt",int.MaxValue)){int count=(CampaignMinutes-s.RoofTimers["hourAt"])/60+1;s.RoofTimers["hourAt"]=CampaignMinutes+60;ChangeTension(Math.Min(100,count),"Unsealed hungry shelter");}
  }
  var d=CurrentLowerMallSite;if(!Roof29Site(d))return;var state=LowerMallProgress(d!.Number);if(state==null)return;int n=d.DisplayNumber;
  if(state.RoofTimers.TryGetValue("reinforceAt",out int at)&&CampaignMinutes>=at){state.RoofTimers.Remove("reinforceAt");Roof29Reinforce(d,state,Roll(6));}
  if(n==23&&CampaignMinutes>=state.RoofTimers["eventAt"]&&!state.Events.Contains("paranoia-resolved")){state.Events.Add("paranoia-resolved");foreach(var p in Party.Where(p=>p.Alive))if(!SurvivorSave(p,Ability.Wisdom,13,"safe room hunger"))LowerEffect(p,"roof29-hunger",1440*60);}
  if(n==19&&Party.Any(p=>p.Character.HasCondition("roof29-scent")))foreach(var v in Actors.Where(a=>a.Alive&&LowerTraits(a).Contains("roof29-ventcrawler")))v.Awareness.State=AwarenessKind.Alerted;
  if(n==18&&CampaignMinutes>=state.RoofTimers["eventAt"]&&!state.Events.Contains("oddity-resolved")){state.Events.Add("oddity-resolved");Message(MessageCategory.Narrative,new[]{"Fluorescents reveal handprints overhead.","The coffee machine displays Error 66: Wake Up.","A locker slams shut unaided.","Whispers drift through the wall vents.","A radio repeats Downstairs.","Three knocks from the other side of the wall."}[state.BranchRoll-1]);}
  if(n==20&&CampaignMinutes>=state.RoofTimers["eventAt"]){state.RoofTimers["eventAt"]=CampaignMinutes+10;int roll=Roll(6);if(roll==1)foreach(var p in Party.Where(p=>p.Alive))if(!SurvivorSave(p,Ability.Constitution,12,"static surge"))LowerEffect(p,"book-deafened",60);if(roll==3)Roof29Reinforce(d,state,1);if(roll==6)foreach(var p in Party.Where(p=>p.Alive&&p.Pos.X>20).ToArray())if(!SurvivorSave(p,Ability.Dexterity,15,"tower lightning"))Hurt(p,RollDamage(3,6),DamageType.Lightning);Message(MessageCategory.Narrative,"Tower event: "+new[]{"static screeches","a helicopter shadow passes, then vanishes","a howl rises from below","a voice asks if anyone is alive","a dead technician's note becomes visible","lightning strikes the radio mast"}[roll-1]);}
 }
 void SecureRoof29(LowerMallSite d,LowerMallState s)
 {
  if(!Roof29Site(d))return;if(LowerMallStates.Count(s=>s.Number is >=214 and <=219&&s.Secured)==6)RunCampaignEvent("event.roof.fourteen-nineteen");if(LowerMallStates.Count(s=>s.Number is >=220 and <=225&&s.Secured)==6)RunCampaignEvent("event.roof.twenty-twentyfive");
 }
 string Roof29SecureReason(LowerMallSite d,LowerMallState s)=>!Roof29Site(d)?"":d.Number==221&&!s.Events.Contains("crossed")?"Establish the skybridge crossing before securing.":d.Number==223&&!s.Events.Contains("sealed")?"Seal the breached shelter vent before securing.":d.Number==225&&s.Events.Contains("chlorine-active")?"Isolate leaking chlorine before securing.":"";
 void ValidateRoof29State(LowerMallSite d,LowerMallState s)
 {
  if(!Roof29Site(d))return;
  string[] timers=["eventAt","hourAt","powerUntil","reinforceAt","flares"];
  string[] rolls=d.Number==225?["signal","bonus","rounds"]:["signal","bonus"];
  if(s.BranchRoll is <1 or >6||s.BonusRoll is <1 or >4||s.RoofSignalUnits.Count!=0||s.RoofTimers.Count>5||s.RoofTimers.Any(k=>!timers.Contains(k.Key)||k.Value<0)||!s.RoofTimers.ContainsKey("eventAt")||!s.RoofTimers.ContainsKey("hourAt")||s.RoofRolls.Count!=rolls.Length||s.RoofRolls.Keys.Any(k=>!rolls.Contains(k))||s.RoofRolls.GetValueOrDefault("signal") is <1 or >6||s.RoofRolls.GetValueOrDefault("bonus") is <1 or >8)throw new InvalidDataException("Invalid final-roof saved rolls or timers.");
  if(s.RoofTimers.ContainsKey("powerUntil")&&d.Number is not (220 or 222)||s.RoofTimers.ContainsKey("flares")&&(d.Number!=224||s.RoofTimers["flares"]>3)||d.Number==225&&s.RoofRolls.GetValueOrDefault("rounds") is <2 or >12)throw new InvalidDataException("Foreign final-roof device state.");
  if(s.Events.Contains("bridge-repaired")&&!s.Resolved.Contains("action.roof.21.device.repair")||s.Events.Contains("repaired")&&!s.Resolved.Contains("action.roof.22.device.repair")||s.Events.Contains("sealed")&&!s.Resolved.Contains($"action.{d.Prefix}.device.seal"))throw new InvalidDataException("Unearned final-roof repair state.");
 }
 int Roof29Damage(Actor target,int amount,DamageType type,bool critical,Actor? source)
 {
  var tags=LowerTraits(target);if(source!=null&&LowerTraits(source).Contains("roof29-drop")&&LowerMonsterState(source) is {} from&&!from.Events.Contains("drop-used:"+source.Id))amount*=2;if(tags.Contains("roof29-horror")&&type is DamageType.Slashing or DamageType.Piercing)amount/=2;if(tags.Contains("roof29-guard")&&!critical)amount/=2;return amount;
 }
 void Roof29DamageResolved(Actor target,int damage,DamageType type,Actor? source,bool wasAlive,bool critical)
 {
  if(damage<=0)return;var tags=LowerTraits(target);var s=LowerMonsterState(target);var d=CurrentLowerMallSite;
  if(source is {Enemy:true}&&!target.Enemy&&target.Alive)
  {
   var st=LowerTraits(source);if(st.Contains("roof29-drowned")&&!SurvivorSave(target,Ability.Constitution,12,"drowning grip"))target.Needs.Exhaustion=Math.Min(6,target.Needs.Exhaustion+1);
   if(st.Contains("roof29-bird")&&critical)LowerEffect(target,"blinded",Roll(4)*6);
   if(st.Contains("roof29-ventcrawler")){LowerEffect(target,"roof29-scent",0);var marked=LowerMonsterState(source);if(marked!=null&&!marked.Events.Contains("marked:"+target.Id))marked.Events.Add("marked:"+target.Id);}
   if(st.Contains("roof29-drop")){var from=LowerMonsterState(source);if(from!=null&&!from.Events.Contains("drop-used:"+source.Id))from.Events.Add("drop-used:"+source.Id);}
   if(st.Contains("roof29-drinker")&&!SurvivorSave(target,Ability.Constitution,12,"glass shard bleeding"))LowerEffect(target,"roof29-bleeding",24);
  }
  if(tags.Contains("roof29-wet")&&source is {Enemy:false}&&type is DamageType.Bludgeoning or DamageType.Piercing or DamageType.Slashing&&!SurvivorSave(source,Ability.Dexterity,12,"slippery bile"))LowerEffect(source,"prone",6);
  if(type==DamageType.Fire&&d?.Number==216&&LowerMallProgress(216) is {} fire&&!fire.Events.Contains("alcohol-ignited")){fire.Events.Add("alcohol-ignited");foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=2))LowerEffect(p,"roof29-burning",18);}
  if(!wasAlive||target.Alive||s==null||!Roof29Site(d)||s.Events.Contains("roof29-death:"+target.Id))return;s.Events.Add("roof29-death:"+target.Id);
  if(tags.Contains("roof29-clinger"))ChangeTension(RollDamage(2,6),"Suspended corpse falls");
  if(tags.Contains("roof29-gasbag"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=2).ToArray())if(!SurvivorSave(p,Ability.Constitution,13,"gasbag cloud"))Hurt(p,RollDamage(2,6),DamageType.Poison,target);
  if(tags.Contains("roof29-screamer")||tags.Contains("roof29-horror")){ChangeTension(RollDamage(tags.Contains("roof29-horror")?3:2,6),"Rooftop death scream");Roof29Reinforce(d!,s,Roll(tags.Contains("roof29-horror")?4:6));}
 }
 void Roof29Turn(Actor actor,LowerMallState s,IReadOnlyList<string> tags)
 {
  if(tags.Contains("roof29-maw")){var meal=Party.FirstOrDefault(p=>p.Alive&&p.Character.HasCondition("restrained")&&p.Position.Distance(actor.Position)<=1.5f);if(meal!=null){Hurt(meal,Roll(10),DamageType.Piercing,actor);actor.Economy.Spend(ActionResourceKind.Action);}}
  if(tags.Contains("roof29-feral")&&actor.Hp<=actor.MaxHp/2)actor.Economy.Reset(actor.EffectiveSpeedFeet,2);
  var d=CurrentLowerMallSite;if(!Roof29Site(d))return;
  if(tags.Contains("roof29-horror")){var p=Party.FirstOrDefault(p=>p.Alive&&p.Position.Distance(actor.Position)<=2);if(p!=null&&!SurvivorSave(p,Ability.Strength,15,"dragging hands"))LowerEffect(p,"restrained",12);}
  if(tags.Contains("roof29-toast")&&!s.Events.Contains("vomit:"+actor.Id)){s.Events.Add("vomit:"+actor.Id);foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=2&&Line(p.Position,actor.Position)).ToArray())Hurt(p,SurvivorSave(p,Ability.Dexterity,13,"acid vomit")?RollDamage(2,6)/2:RollDamage(2,6),DamageType.Acid,actor);}
  if(tags.Contains("roof29-repeater"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=2).ToArray()){int hit=Roll(8);Hurt(p,SurvivorSave(p,Ability.Constitution,13,"static discharge")?hit/2:hit,DamageType.Lightning,actor);if(!SurvivorSave(p,Ability.Wisdom,14,"static whispers"))LowerEffect(p,"roof29-panic",Roll(4)*60);}
  if(tags.Contains("roof29-biker")){/* finite reinforcement timer is stored as an event counter, not another random roll */int prior=s.Events.Count(e=>e.StartsWith("biker:"+actor.Id+":"));if(prior<3)s.Events.Add("biker:"+actor.Id+":"+prior);if(prior==2)SpawnChapterNamed(d!,s,"Biker Scout",4);}
 }
 void Roof29PartyTurn(Actor actor)
 {
  if(actor.Character.HasCondition("roof29-mold"))Hurt(actor,Roll(4),DamageType.Poison);if(actor.Character.HasCondition("roof29-chlorine"))Hurt(actor,Roll(6),DamageType.Poison);if(actor.Character.HasCondition("roof29-bleeding"))Hurt(actor,Roll(4),DamageType.Slashing);if(actor.Character.HasCondition("roof29-burning"))Hurt(actor,Roll(6),DamageType.Fire);
 }
 void Roof29Noise(Actor source,string kind)
 {
  var d=CurrentLowerMallSite;if(!Roof29Site(d)||source.Enemy)return;var s=LowerMallProgress(d!.Number);if(s==null)return;
  if(kind is "gunshot" or "explosion" or "shout" or "door")
  {
   s.Quiet=false;
   if(d.Number is 215 or 216)foreach(var a in Actors.Where(a=>a.Enemy&&a.Alive&&LowerTraits(a).Contains("dormant"))){a.Character.SetCondition("chapter-asleep","Sleeping",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="chapter-asleep");}
  }
 }
 void Roof29Gunshot(Actor actor)
 {
  var d=CurrentLowerMallSite;if(!Roof29Site(d))return;int n=d!.DisplayNumber;ChangeTension(n==18?3:n==24?2:RollDamage(n==17?2:1,n==21?4:6),"Rooftop gunshot");
  if(n==16&&LowerMallProgress(216) is {} s&&!s.Events.Contains("alcohol-ignited")){s.Events.Add("alcohol-ignited");foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=2))LowerEffect(p,"roof29-burning",18);}
 }
}
