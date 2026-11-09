using Ashen.Core;
using System.Text.Json.Nodes;
static class HordeTests
{
 public static void Run(Action<bool,string> check)
 {
  var definition=CampaignLoader.Load(CampaignLoader.BundledHordePath);var h=definition.Hordes.Single();
  World Fresh(bool shelter=true)
  {
   var w=World.New(CampaignLoader.Clone(definition));w.PartyLeader.Pos=new(42,16);w.Party.Skip(1).First().Pos=new(40,16);foreach(var p in w.Party)p.Character.SetCondition("hidden","Hidden",true);if(shelter)w.SetDialogueFlag("flag.mall.shelter");w.Reveal();return w;
  }
  var w=Fresh();check(definition.SchemaVersion==10&&h.MemberActorIds.Count==6&&w.Actors.Count(a=>a.Enemy)==3&&w.HordeStatus(h.Id)=="Dormant","Phase 12 dormant horde templates are authored but not yet live actors");
  var gated=Fresh(false);check(!gated.ReleaseHorde(h.Id)&&gated.ReleasedHordes.Count==0,"Phase 12 refuge prerequisite denies an early breach without mutation");
  int serial=w.NextItemInstanceNumber;check(w.ReleaseHorde(h.Id)&&w.ReleasedHordes.Count==1&&h.MemberActorIds.All(id=>w.Actors.Any(a=>a.Id==id)),"Phase 12 explicit producer releases exactly its finite six-member group");
  var members=w.Actors.Where(a=>h.MemberActorIds.Contains(a.Id)).ToArray();check(members.Select(a=>a.Pos).Distinct().Count()==6&&members.All(a=>w.CanStand(a.Position,a)&&a.Group==3&&a.Hp==22),"Phase 12 breach placement respects collision, occupancy and module shambler profiles");
  check(members.All(a=>a.Awareness.State==AwarenessKind.Investigating)&&w.NextItemInstanceNumber==serial,"Phase 12 horde investigates without generating loot, gear or item identities");
  int count=w.Actors.Count;check(!w.ReleaseHorde(h.Id)&&w.Actors.Count==count,"Phase 12 repeated producer call cannot duplicate a wave");
  string path=Path.Combine(Path.GetTempPath(),"zombie-horde-"+Guid.NewGuid()+".json");members[0].Hp=0;w.Save(path);w=World.Load(path);
  check(w.HordeStatus(h.Id)=="5/6 remaining"&&w.Actors.Single(a=>a.Id==members[0].Id).Hp==0&&!w.ReleaseHorde(h.Id),"Phase 12 partial losses and the one-shot ledger survive save/load");
  foreach(var actor in w.Actors.Where(a=>h.MemberActorIds.Contains(a.Id)))actor.Hp=0;w.Save(path);w=World.Load(path);check(w.HordeStatus(h.Id)=="Cleared"&&!w.ReleaseHorde(h.Id)&&w.ResourceXpAwarded==0,"Phase 12 cleared horde never respawns or farms resource XP");
  w=Fresh();for(int i=0;i<2;i++)w.EmitLocalNoise(w.PartyLeader,"shout",60);check(w.ReleasedHordes.Count==0&&w.HordeNoiseCounts[h.Id]==2,"Phase 12 two audible disturbances build pressure without spawning early");w.Save(path);w=World.Load(path);w.EmitLocalNoise(w.PartyLeader,"shout",60);
  check(w.ReleasedHordes.Count==1&&w.Actors.Count(a=>h.MemberActorIds.Contains(a.Id))==6,"Phase 12 third disturbance crosses a saved threshold exactly once");for(int i=0;i<5;i++)w.EmitLocalNoise(w.PartyLeader,"shout",60);check(w.Actors.Count(a=>h.MemberActorIds.Contains(a.Id))==6,"Phase 12 further loud sounds do not create more horde members");
  var reactive=Fresh();var shooter=reactive.PartyLeader;var mover=reactive.Actors.First(a=>a.Enemy);mover.Pos=new(33,16);mover.MaxHp=1000;mover.Hp=1000;reactive.Equip(shooter,shooter.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));reactive.ReloadFirearm(shooter);reactive.StartCombat(mover.Group);reactive.Order=[mover.Id,shooter.Id];reactive.Turn=0;shooter.Character.SetCondition("readying","Readied Action",true);reactive.ReactionPolicyResolver=(_,_)=>ReactionPolicy.Auto;mover.Economy.Reset(mover.Character.SpeedFeet,1);reactive.HordeNoiseCounts[h.Id]=2;reactive.MoveTo(mover,new Cell(35,16).Center);
  for(int i=0;i<40&&reactive.ReleasedHordes.Count==0;i++)reactive.UpdateMovement(.1f);
  check(reactive.ReleasedHordes.Count==1&&shooter.EquippedAmmunitionCount==11&&reactive.Active==mover&&reactive.Order.Distinct().Count()==reactive.Order.Count,$"Phase 12 readied shot safely releases horde during movement (released {reactive.ReleasedHordes.Count}, ammo {shooter.EquippedAmmunitionCount}, active {reactive.Active?.Id})");
  var blocked=Fresh();var origin=blocked.Features.Single(f=>f.Id==h.TriggerFeatureId).Pos;
  foreach(var cell in blocked.Floors.Where(c=>c.Distance(origin)<=3&&!blocked.Actors.Any(a=>a.Alive&&a.Pos==c)).ToArray())blocked.Actors.Add(new(){Id="unit.blocker."+cell.X+"-"+cell.Y,Pos=cell});
  check(!blocked.ReleaseHorde(h.Id)&&blocked.ReleasedHordes.Count==0&&!blocked.Actors.Any(a=>h.MemberActorIds.Contains(a.Id)),"Phase 12 blocked breach performs no partial spawn or release mutation");
  blocked.Actors.RemoveAll(a=>a.Id.StartsWith("unit.blocker."));check(blocked.ReleaseHorde(h.Id),"Phase 12 blocked producer can release once space becomes available");
  var bad=CampaignLoader.Clone(definition);bad.SchemaVersion=9;check(CampaignLoader.Validate(bad).Count>0,"Phase 12 horde definitions require schema 10");bad=CampaignLoader.Clone(definition);bad.Hordes[0].MemberActorIds.Add(bad.Hordes[0].MemberActorIds[0]);check(CampaignLoader.Validate(bad).Count>0,"Phase 12 duplicate horde membership is rejected");bad=CampaignLoader.Clone(definition);bad.Hordes[0].NoiseThreshold=0;check(CampaignLoader.Validate(bad).Count>0,"Phase 12 unbounded or zero noise threshold is rejected");
  w=Fresh();w.ReleaseHorde(h.Id);w.Save(path);var root=JsonNode.Parse(File.ReadAllText(path))!;root["ReleasedHordes"]!.AsArray().Clear();File.WriteAllText(path,root.ToJsonString());bool rejected=false;try{World.Load(path);}catch(InvalidDataException){rejected=true;}check(rejected,"Phase 12 saved members without release ownership are rejected");
  // A small second-area fixture isolates retreat/return persistence without adding a new production location.
  var travel=CampaignLoader.Clone(definition);var home=travel.Areas[0];home.EntryPoints.Add(new(){Id="entry.horde.test.home",Position=new(39,16)});home.Features.Add(new(){Id="feature.horde.test.exit",Name="Test service exit",Kind="transition",Position=new(39,17)});
  var annex=new CampaignArea{Id="area.horde.test.annex",Name="Test annex",Source=new(){AuditId="test.horde.travel",PageStart=68,PageEnd=68},EntryPoints=[new(){Id="entry.horde.test.annex",Position=new(5,16)}],Features=[new(){Id="feature.horde.test.return",Name="Test return",Kind="transition",Position=new(6,16)}]};
  for(int y=14;y<=18;y++)for(int x=4;x<=10;x++)annex.Floors.Add(new(x,y));travel.Areas.Add(annex);
  travel.Connections.Add(new(){Id="route.horde.test.out",FromAreaId=home.Id,ToAreaId=annex.Id,ExitFeatureId="feature.horde.test.exit",EntryPointId="entry.horde.test.annex",Minutes=1});travel.Connections.Add(new(){Id="route.horde.test.back",FromAreaId=annex.Id,ToAreaId=home.Id,ExitFeatureId="feature.horde.test.return",EntryPointId="entry.horde.test.home",Minutes=1});
  var away=World.New(travel);away.SetDialogueFlag("flag.mall.shelter");away.PartyLeader.Pos=new(42,16);away.Party.Skip(1).First().Pos=new(40,16);foreach(var survivor in away.Party)survivor.Character.SetCondition("hidden","Hidden",true);away.Reveal();away.ReleaseHorde(h.Id);var casualty=away.Actors.First(a=>h.MemberActorIds.Contains(a.Id));casualty.Hp=0;
  away.PartyLeader.Pos=new(38,17);away.Party.Skip(1).First().Pos=new(39,16);away.Reveal();check(away.TravelTo(annex.Id),"Phase 12 released horde can be left behind through a legal area exit");
  away.EmitLocalNoise(away.PartyLeader,"shout",60);for(int i=0;i<10;i++)away.UpdateMovement(.1f);away.Save(path);away=World.Load(path);away.PartyLeader.Pos=new(5,16);away.Party.Skip(1).First().Pos=new(6,17);away.Reveal();
  check(away.TravelTo(home.Id)&&away.HordeStatus(h.Id)=="5/6 remaining"&&away.Actors.Single(a=>a.Id==casualty.Id).Hp==0&&!away.ReleaseHorde(h.Id),"Phase 12 casualties, wave ownership and counts survive retreat, offscreen noise, save/load and return");
  var old=World.New(CampaignLoader.Load(CampaignLoader.BundledAwarenessPath));old.Save(path);root=JsonNode.Parse(File.ReadAllText(path))!;root["Version"]=24;File.WriteAllText(path,root.ToJsonString());var migrated=World.Load(path);check(migrated.Version==World.CurrentSaveVersion&&migrated.Campaign!.Hordes.Count==0&&migrated.ReleasedHordes.Count==0,"Phase 12 v24 migration does not introduce hordes into old campaigns");File.Delete(path);
  ExpeditionTests.RunFullRoute((ok,name)=>check(ok,name.Replace("Phase 8","Phase 12 horde expedition")),CampaignLoader.BundledHordePath,true);
 }
}
