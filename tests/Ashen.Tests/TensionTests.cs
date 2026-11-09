using Ashen.Core;
using System.Text.Json.Nodes;
static class TensionTests
{
 public static void Run(Action<bool,string> check)
 {
  var definition=CampaignLoader.Load(CampaignLoader.BundledTensionPath);string path=Path.Combine(Path.GetTempPath(),"zombie-tension-"+Guid.NewGuid()+".json");
  World Fresh(bool refuge=true,int startMinute=480)
  {
   var authored=CampaignLoader.Clone(definition);authored.StartMinute=startMinute;var w=World.New(authored);foreach(var f in w.Features.Where(f=>f.Kind=="door"))f.Used=true;if(refuge)w.SetDialogueFlag("flag.mall.shelter");w.PartyLeader.Pos=new(12,27);w.Party.Skip(1).First().Pos=new(13,27);w.Reveal();return w;
  }
  World Incident(string kind)
  {
   // Pick a seed before any time advances; exercise the real daily scheduler, never inject an outcome.
   for(uint seed=1;seed<10000;seed++)
   {
    var w=Fresh();w.TensionState.Seed=seed;
    if(kind=="outage"){w.PerformWorks(WorksAction.InstallGenerator);w.PerformWorks(WorksAction.Refuel);w.PerformWorks(WorksAction.TogglePower);}
    if(kind=="weakness"){w.PerformWorks(WorksAction.BuildBarricade);}
    w.CampaignMinutes=1440;w.ProcessSurvivalTime();if(w.TensionState.History.Single().Kind==kind)return w;
   }
   throw new Exception("No seed for "+kind);
  }
  var w=Fresh();check(definition.SchemaVersion==14&&w.TensionEnabled&&w.TensionState.Score==0&&w.TensionState.NextDayMinute==1440,"Phase 16 new campaign starts at zero Tension with next-midnight scheduling");
  check(w.Campaign!.Hordes.Count==4&&w.Campaign.Tension!.AttackHordeIds.Select(id=>w.Campaign.Hordes.First(h=>h.Id==id).MemberActorIds.Count).SequenceEqual(new[]{2,3,4}),"Phase 16 three finite perimeter pools contain two, three and four authored shamblers");
  check(w.Actors.All(a=>!a.Id.StartsWith("actor.daily.")),"Phase 16 dormant daily enemies do not materialize with the initial map");
  w.EmitLocalNoise(w.PartyLeader,"gunshot",120);check(w.TensionState.Score==2,"Phase 16 a real gunshot adds exactly two Tension");
  w.EmitLocalNoise(w.PartyLeader,"door",20);check(w.TensionState.Score==2,"Phase 16 ordinary door noise does not acquire an invented global penalty");
  int old=w.TensionState.Score;w.Shout(w.PartyLeader);check(w.TensionState.Score>=old+1&&w.TensionState.Score<=old+6,"Phase 16 a real shout adds one d6 Tension");
  old=w.TensionState.Score;w.EmitLocalNoise(w.Actors.First(a=>a.Enemy),"shout",60);check(w.TensionState.Score==old,"Phase 16 enemy noise cannot recursively inflate global Tension");
  for(int i=0;i<60;i++)w.EmitLocalNoise(w.PartyLeader,"gunshot",120);check(w.TensionState.Score==100&&w.TensionBand=="EXTREME","Phase 16 Tension clamps at 100 without automatic campaign destruction");
  w.PerformWorks(WorksAction.BuildBarricade);check(w.TensionState.Score is >=96 and <=99,"Phase 16 first reinforcement reduces Tension by one d4");
  old=w.TensionState.Score;w.PerformWorks(WorksAction.Unbar);check(w.TensionState.Score>old,"Phase 16 moving a standing barricade raises Tension");
  w.PerformWorks(WorksAction.Secure);old=w.TensionState.Score;w.PerformWorks(WorksAction.Unbar);check(w.TensionState.Score>=old,"Phase 16 unbar and secure cycles never farm defensive reductions");
  w=Fresh(false);w.CampaignMinutes=1440;w.ProcessSurvivalTime();check(w.TensionState.History.Count==0&&w.TensionState.DaysChecked==1&&w.TensionState.NextDayMinute==2880,"Phase 16 days before refuge establishment advance the clock without spawning shelter incidents");
  w.SetDialogueFlag("flag.mall.shelter");w.CampaignMinutes=2880;w.ProcessSurvivalTime();check(w.TensionState.History.Count==1&&w.TensionState.History[0].Day==3,"Phase 16 establishing a refuge does not replay past days");
  int count=w.TensionState.History.Count;w.ProcessSurvivalTime();w.ProcessSurvivalTime();check(w.TensionState.History.Count==count,"Phase 16 repeated processing cannot reroll or duplicate the day's report");
  w.Save(path);var loaded=World.Load(path);check(loaded.Version==World.CurrentSaveVersion&&loaded.TensionState.Seed==w.TensionState.Seed&&loaded.TensionState.History.Single().Result==w.TensionState.History.Single().Result,"Phase 16 seed, daily roll and exact report persist in save v29");
  w.CampaignMinutes=4320;loaded.CampaignMinutes=4320;w.ProcessSurvivalTime();loaded.ProcessSurvivalTime();check(w.TensionState.History.Last().Roll==loaded.TensionState.History.Last().Roll&&w.TensionState.History.Last().DetailRoll==loaded.TensionState.History.Last().DetailRoll,"Phase 16 continued and loaded worlds receive identical next-day dice");
  foreach(string kind in new[]{"quiet","spoilage","outage","weakness","attack"})
  {var e=Incident(kind);check(e.TensionState.History.Single().Kind==kind,"Phase 16 real daily scheduler produces "+kind);e.Save(path);check(World.Load(path).TensionState.History.Single().Kind==kind,"Phase 16 "+kind+" outcome survives reload");}
  w=Incident("weakness");check(w.InfrastructureState.BarricadeHp==20&&w.InfrastructureState.BarricadeEngaged&&w.InfrastructureState.XpAwarded==10,"Phase 16 structural weakness removes four real HP without duplicating construction XP");
  int score=w.TensionState.Score;check(w.PerformWorks(WorksAction.RepairBarricade)&&w.InfrastructureState.BarricadeHp==24&&w.TensionState.Score<=score&&w.TensionState.RepairsCredited==1,"Phase 16 paid physical repairs restore durability and reduce Tension");
  check(!w.PerformWorks(WorksAction.RepairBarricade)&&w.TensionState.RepairsCredited==1,"Phase 16 failed full-health repairs grant no Tension credit");
  w=Incident("spoilage");check(w.ShelterSupplyCount("food")==0&&w.ResourceXpAwarded==0,"Phase 16 spoilage exhausts limited initial food without inventing debt or XP");
  uint spoilSeed=w.TensionState.Seed;w=Fresh();w.TensionState.Seed=spoilSeed;var site=w.Features.Single(f=>f.Id==w.Campaign!.Shelter!.StockpileFeatureId);var food=w.Campaign!.Items.First(i=>i.SurvivalUse=="food");site.Contents.Add(new ItemStack{ItemId=food.Id,Quantity=4,CampaignGear=w.ResolveItemDefinition(food.Id)});int serial=w.NextItemInstanceNumber,foodBefore=w.ShelterSupplyCount("food");w.CampaignMinutes=1440;w.ProcessSurvivalTime();check(w.ShelterSupplyCount("food")==foodBefore-3&&w.ShelterSupplyCount("food")>=0&&w.NextItemInstanceNumber==serial,"Phase 16 spoilage consumes actual stock without creating replacement item identities");
  w=Fresh();w.PerformWorks(WorksAction.InstallGenerator);w.PerformWorks(WorksAction.Refuel);w.PerformWorks(WorksAction.TogglePower);w.PerformWorks(WorksAction.TogglePower);score=w.TensionState.Score;w.PerformWorks(WorksAction.TogglePower);int afterStart=w.TensionState.Score;w.PerformWorks(WorksAction.TogglePower);check(w.TensionState.Score==afterStart&&w.TensionState.LastStopDay==0,"Phase 16 stopping generator noise grants at most one reduction per day");
  w=Incident("attack");var s=w.TensionState;string first=s.PendingAttacks.Single();check(!w.ReleaseHorde(first)&&!w.Features.Single(f=>f.Id==w.Campaign!.Hordes.First(h=>h.Id==first).TriggerFeatureId).Used,"Phase 16 manual feature interaction cannot release a managed attack early");
  for(int i=0;i<15;i++)w.EmitLocalNoise(w.PartyLeader,"gunshot",120);check(!w.ReleasedHordes.Contains(first),"Phase 16 local noise cannot bypass the daily attack ledger");
  s.Score=100;w.CampaignMinutes=4320;w.ProcessSurvivalTime();check(s.PendingAttacks.Count==3&&s.PendingAttacks.Distinct().Count()==3,"Phase 16 catch-up days reserve each authored attack at most once");
  w.Save(path);loaded=World.Load(path);check(loaded.TensionState.PendingAttacks.SequenceEqual(s.PendingAttacks)&&loaded.Actors.All(a=>!a.Id.StartsWith("actor.daily.")),"Phase 16 pending attacks save without premature actors or spawn duplication");
  w=loaded;s=w.TensionState;w.Mode=Mode.Combat;check(!w.DispatchDailyAttack()&&s.PendingAttacks.Count==3,"Phase 16 queued waves cannot steal an active combat turn");w.Mode=Mode.Explore;
  check(w.DispatchDailyAttack()&&w.ReleasedHordes.Contains(first)&&s.PendingAttacks.Count==2,"Phase 16 a queued attack deploys once at an exploration boundary");
  check(w.Actors.Count(a=>a.Id.StartsWith("actor.daily."))==2&&w.Actors.Where(a=>a.Id.StartsWith("actor.daily.")).All(a=>a.Enemy&&a.Hp==22&&a.Awareness.LastKnown==w.Features.Single(f=>f.Id==w.Campaign!.Shelter!.StockpileFeatureId).Pos),"Phase 16 deployed members use the real shambler profile and investigate the known refuge");
  check(!w.DispatchDailyAttack()&&s.PendingAttacks.Count==2,"Phase 16 a live perimeter wave prevents stacked simultaneous spawns");
  foreach(var a in w.Actors.Where(a=>a.Id.StartsWith("actor.daily.")))a.Hp=0;w.Mode=Mode.Explore;w.Order.Clear();w.ProcessSurvivalTime();score=s.Score;check(s.CreditedHordes.Contains(first),"Phase 16 whole-horde completion credits a single Tension reduction");w.ProcessSurvivalTime();check(s.Score==score,"Phase 16 repeated corpse checks never repeat horde reduction");
  w.Save(path);loaded=World.Load(path);check(loaded.Actors.Count(a=>a.Id.StartsWith("actor.daily.")&&!a.Alive)==2&&loaded.TensionState.PendingAttacks.Count==2,"Phase 16 dead attack members and remaining queues persist together");
  foreach(var a in loaded.Actors.Where(a=>a.Enemy))a.Hp=0;loaded.Mode=Mode.Explore;loaded.Order.Clear();check(loaded.DispatchDailyAttack()&&loaded.Actors.Count(a=>a.Id.StartsWith("actor.daily."))==5,"Phase 16 second wave adds only its three unique members");
  foreach(var a in loaded.Actors.Where(a=>a.Enemy))a.Hp=0;loaded.Mode=Mode.Explore;loaded.Order.Clear();check(loaded.DispatchDailyAttack()&&loaded.Actors.Count(a=>a.Id.StartsWith("actor.daily."))==9,"Phase 16 third wave adds only its four unique members");
  foreach(var a in loaded.Actors.Where(a=>a.Enemy))a.Hp=0;loaded.Mode=Mode.Explore;loaded.Order.Clear();loaded.TensionState.Score=100;loaded.CampaignMinutes=5760;loaded.ProcessSurvivalTime();check(loaded.TensionState.PendingAttacks.Count==0&&!loaded.DispatchDailyAttack()&&loaded.Actors.Count(a=>a.Id.StartsWith("actor.daily."))==9,"Phase 16 exhausted finite pools never recycle corpses or generate infinite hordes");
  loaded.Save(path);check(World.Load(path).TensionState.CreditedHordes.Count>=3,"Phase 16 completed wave credits persist without repeat rewards");
  w=Fresh();var fallen=w.Party.Skip(1).First();fallen.Hp=0;fallen.Character.SetCondition("dead","Dead",true);w.ProcessSurvivalTime();score=w.TensionState.Score;check(score is >=4 and <=24&&w.TensionState.RecordedDeaths.Contains(fallen.Id),"Phase 16 an actual survivor death adds four d6 once");w.ProcessSurvivalTime();check(w.TensionState.Score==score,"Phase 16 repeated processing never repeats a death penalty");
  // Save rejection is important: invalid ledgers must not silently invent different attacks.
  w=Incident("attack");w.Save(path);var original=File.ReadAllText(path);
  void Reject(Action<JsonNode> mutate,string description)
  {var root=JsonNode.Parse(original)!;mutate(root);File.WriteAllText(path,root.ToJsonString());bool rejected=false;try{World.Load(path);}catch(Exception){rejected=true;}check(rejected,"Phase 16 rejects "+description);}
  Reject(n=>n["TensionState"]!["Score"]=101,"out-of-range pressure");
  Reject(n=>n["TensionState"]!["NextDayMinute"]=1440,"replayable daily deadline");
  Reject(n=>n["TensionState"]!["History"]![0]!["Roll"]=0,"invalid daily dice");
  Reject(n=>n["TensionState"]!["PendingAttacks"]!.AsArray().Add("unknown"),"unknown attack identity");
  Reject(n=>n["TensionState"]!["PendingAttacks"]!.AsArray().Add(w.TensionState.PendingAttacks[0]),"duplicate attack reservation");
  Reject(n=>n["TensionState"]!["History"]![0]!["Day"]=0,"invalid incident date");
  var oldCampaign=World.New(CampaignLoader.Load(CampaignLoader.BundledInfrastructurePath));oldCampaign.Save(path);var oldRoot=JsonNode.Parse(File.ReadAllText(path))!;oldRoot["Version"]=28;oldRoot.AsObject().Remove("TensionState");File.WriteAllText(path,oldRoot.ToJsonString());loaded=World.Load(path);check(loaded.Version==World.CurrentSaveVersion&&!loaded.TensionEnabled&&loaded.Campaign!.SchemaVersion==13&&loaded.TensionState.Seed==0,"Phase 16 v28 saves retain their old campaign without retroactive daily attacks");
  foreach(var invalid in new[]{0,1,2})
  {var bad=CampaignLoader.Clone(definition);if(invalid==0)bad.SchemaVersion=13;if(invalid==1)bad.Tension!.AttackHordeIds.Add("unknown");if(invalid==2)bad.Tension!.Source.PageStart=0;check(CampaignLoader.Validate(bad).Count>0,"Phase 16 authored validation catches invalid tension definition "+invalid);}
  uint outageSeed=Incident("outage").TensionState.Seed;w=Fresh(true,1370);w.TensionState.Seed=outageSeed;w.PerformWorks(WorksAction.InstallGenerator);w.PerformWorks(WorksAction.Refuel);w.PerformWorks(WorksAction.TogglePower);w.AdvanceCampaignSeconds(600);
  check(w.TensionState.History.Single().Kind=="outage"&&!w.ShelterPowered&&w.InfrastructureState.FuelMinutes==50,"Phase 16 an actual midnight outage stops running power and retains exactly the remaining fuel");
  check(w.PerformWorks(WorksAction.TogglePower)&&w.ShelterPowered&&w.InfrastructureState.FuelMinutes==50,"Phase 16 manual restart uses the same installed generator and fuel");
  w=Incident("attack");first=w.TensionState.PendingAttacks[0];var trigger=w.Features.Single(f=>f.Id==w.Campaign!.Hordes.First(h=>h.Id==first).TriggerFeatureId);var obstruction=new Feature{Id="test.blocker",Kind="door",Pos=trigger.Pos};w.Features.Add(obstruction);serial=w.NextItemInstanceNumber;
  check(!w.DispatchDailyAttack()&&w.TensionState.PendingAttacks.Count==1&&!w.ReleasedHordes.Contains(first)&&w.NextItemInstanceNumber==serial,"Phase 16 a blocked arrival fails atomically without spending the queue or creating identities");w.Features.Remove(obstruction);
  w.PerformWorks(WorksAction.BuildBarricade);foreach(var actor in w.Party)actor.Character.SetCondition("hidden","Hidden",true);check(w.DispatchDailyAttack(),"Phase 16 an incoming wave deploys outside a secured occupied refuge");
  for(int tick=0;tick<5400&&w.InfrastructureState.BarricadeHp>0;tick++)w.UpdateMovement(1f/60);
  check(w.InfrastructureState.BarricadeHp==0&&!w.InfrastructureState.BarricadeEngaged&&w.Features.Single(f=>f.Id==w.Campaign!.Infrastructure!.DoorFeatureId).Used,"Phase 16 real perimeter AI keeps the known refuge target and breaches a 24-HP barricade through normal timed strikes");
  w=Fresh();w.TensionState.Seed=1;for(int i=0;i<5;i++)w.EmitLocalNoise(w.PartyLeader,"gunshot",120);w.PerformWorks(WorksAction.BuildBarricade);w.CampaignMinutes=1440;w.ProcessSurvivalTime();score=w.TensionState.Score;w.CampaignMinutes=2880;w.ProcessSurvivalTime();
  check(w.TensionState.Score<=score&&w.TensionState.LastReason=="Quiet secured day","Phase 16 a full quiet secured day earns one d6 reduction after the noisy partial day");
  File.Delete(path);
  ExpeditionTests.RunFullRoute((ok,name)=>check(ok,name.Replace("Phase 8","Phase 16 daily expedition")),CampaignLoader.BundledTensionPath,true,false,true,true,true);
 }
}
