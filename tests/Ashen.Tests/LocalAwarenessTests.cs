using Ashen.Core;
using System.Text.Json.Nodes;
static class LocalAwarenessTests
{
 public static void Run(Action<bool,string> check)
 {
  var definition=CampaignLoader.Load(CampaignLoader.BundledAwarenessPath);
  World Fresh()
  {
   var w=World.New(CampaignLoader.Clone(definition));foreach(var f in w.Features.Where(f=>f.Kind=="door"))f.Used=true;
   w.PartyLeader.Pos=new(13,16);var enemy=w.Actors.First(e=>e.Enemy);enemy.Pos=new(13,12);w.Reveal();return w;
  }
  var w=Fresh();var a=w.PartyLeader;var e=w.Actors.First(x=>x.Enemy);var cafeDoor=w.Features.Single(f=>f.Id=="feature.mall.door.cafe");
  check(w.LocalAwarenessEnabled&&definition.SchemaVersion==9&&w.NoiseHistory.Count==0,"Phase 11 new campaign starts quietly with opt-in awareness");
  var open=w.LocalSoundField(a.Pos,20);check(open.TryGetValue(e.Pos,out int distance)&&distance==20,"Phase 11 open corridor transmits four tiles of sound at 20 feet");
  cafeDoor.Used=false;check(!w.LocalSoundField(a.Pos,20).ContainsKey(e.Pos)&&w.LocalSoundField(a.Pos,60).ContainsKey(e.Pos),"Phase 11 closed door muffles quiet sounds while a loud sound can pass");
  check(!w.LocalSoundField(a.Pos,200).ContainsKey(new(0,0))&&w.LocalSoundField(a.Pos,0).Count==0,"Phase 11 walls and invalid sound budgets do not propagate");cafeDoor.Used=true;
  check(!w.CanEnemySee(e,a)&&w.EmitLocalNoise(a,"shout",60)>0&&e.Awareness.State==AwarenessKind.Investigating&&e.Awareness.LastKnown==a.Pos,"Phase 11 hearing records an unseen source without granting visual contact");
  a.Pos=new(3,16);foreach(var p in w.Party)p.Character.SetCondition("hidden","Hidden",true);var start=e.Position;
  for(int i=0;i<30;i++)w.UpdateMovement(.1f);
  check(e.Position!=start&&e.Awareness.LastKnown==new Cell(13,16)&&w.Mode==Mode.Explore,"Phase 11 investigator moves toward the last heard cell rather than the hidden survivor's current location");
  for(int i=0;i<220;i++)w.UpdateMovement(.1f);check(e.Awareness.State==AwarenessKind.Idle&&e.Awareness.LastKnown==null&&!w.IsMoving(e),"Phase 11 fruitless investigation searches then expires without patrol respawning");
  w=Fresh();a=w.PartyLeader;e=w.Actors.First(x=>x.Enemy);w.EmitLocalNoise(a,"shout",60);string path=Path.Combine(Path.GetTempPath(),"zombie-awareness-"+Guid.NewGuid()+".json");w.Save(path);var loaded=World.Load(path);
  check(loaded.Actors.First(x=>x.Id==e.Id).Awareness.LastKnown==a.Pos&&loaded.NoiseHistory.Count==1&&loaded.NextNoiseSequence==2,"Phase 11 investigation and bounded noise history survive save/load without replay");
  w=loaded;a=w.PartyLeader;e=w.Actors.First(x=>x.Enemy);w.EmitLocalNoise(a,"shout",60);check(e.Awareness.LastNoise==2&&w.NoiseHistory.Count==2,"Phase 11 new noise retargets an existing listener with a fresh identity");
  for(int i=0;i<40;i++)w.EmitLocalNoise(a,"door",5);check(w.NoiseHistory.Count==32&&w.NoiseHistory[0].Sequence==11,"Phase 11 noise history retains only the newest 32 events");
  var count=w.NoiseHistory.Count;check(w.EmitLocalNoise(a,"invalid",60)==0&&w.NoiseHistory.Count==count,"Phase 11 invalid noise type is rejected without mutation");
  w=Fresh();a=w.PartyLeader;e=w.Actors.First(x=>x.Enemy);a.Character.SetCondition("hidden","Hidden",true);check(w.Shout(a)&&!a.Character.HasCondition("hidden")&&w.NoiseHistory.Last().Kind=="shout","Phase 11 manual shout breaks hiding and produces a local sound");
  e.Pos=new(13,15);w.Reveal();w.CheckEncounter();check(w.Mode==Mode.Combat&&e.Awareness.State==AwarenessKind.Alerted,"Phase 11 visual acquisition starts combat and records contact");
  w.Order=[a.Id,e.Id];w.Turn=0;a.Economy.Reset(a.Character.SpeedFeet,1);
  check(w.Shout(a)&&!a.Economy.ActionAvailable&&!w.Shout(a),"Phase 11 combat shout costs one Action and cannot repeat on spent Action");
  w=Fresh();a=w.PartyLeader;e=w.Actors.First(x=>x.Enemy);w.Equip(a,a.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));w.ReloadFirearm(a);
  e.Pos=new(13,15);w.Reveal();w.StartCombat(e.Group);w.Order=[a.Id,e.Id];w.Turn=0;a.Economy.Reset(a.Character.SpeedFeet,1);e.MaxHp=1000;e.Hp=1000;
  int before=w.NoiseHistory.Count;check(w.Attack(a,e,AttackKind.Basic)&&w.NoiseHistory.Count==before+1&&w.NoiseHistory.Last().Kind=="gunshot"&&a.EquippedAmmunitionCount==11,"Phase 11 each legal gunshot emits exactly one sound and consumes one round");
  check(!w.Attack(a,e,AttackKind.Basic)&&w.NoiseHistory.Count==before+1,"Phase 11 rejected attack emits no extra sound");
  check(w.ReactionAttack(a,e)&&w.NoiseHistory.Count==before+2&&a.EquippedAmmunitionCount==10,"Phase 11 readied/reaction firing uses the same noise path");
  // Hearing recruits a reachable nearby enemy without replacing its encounter group or the initiative owner.
  var reinforcement=w.Actors.First(x=>x.Enemy&&x!=e);reinforcement.Pos=new(15,16);int group=reinforcement.Group;var owner=w.Active;
  w.EmitLocalNoise(a,"shout",60);check(w.Order.Contains(reinforcement.Id)&&reinforcement.Group==group&&w.Active==owner,"Phase 11 nearby listener joins initiative once while preserving group identity and active turn");
  int participants=w.Order.Count;w.EmitLocalNoise(a,"shout",60);check(w.Order.Count==participants,"Phase 11 repeated noise cannot duplicate initiative participants");
  w=Fresh();a=w.PartyLeader;e=w.Actors.First(x=>x.Enemy);foreach(var p in w.Party)p.Character.SetCondition("hidden","Hidden",true);w.EmitLocalNoise(a,"door",60);w.StartCombat(e.Group);w.AddInitiativeParticipant(e);w.Order=[e.Id,a.Id];w.Turn=0;e.Awareness.MemorySeconds=6;
  w.EnemyTurn();check(!w.Order.Contains(e.Id)&&e.Awareness.State==AwarenessKind.Idle&&w.Mode==Mode.Explore&&!w.ClearedCampaignEncounters.Contains(definition.Encounters.First(c=>c.Group==e.Group).Id),"Phase 11 lost contact ends without falsely clearing or rewarding a live encounter");
  check(w.Cleared==0&&w.Party.All(p=>p.Statistics.EncountersWon==0),"Phase 11 losing contact cannot farm victory counters");
  var travelDefinition=CampaignLoader.Load(CampaignLoader.BundledTravelPath);travelDefinition.SchemaVersion=9;travelDefinition.NoiseAwareness=true;var travelling=World.New(travelDefinition);foreach(var party in travelling.Party)party.Character.SetCondition("hidden","Hidden",true);foreach(var door in travelling.Features.Where(f=>f.Kind=="door"))door.Used=true;
  travelling.EmitLocalNoise(travelling.PartyLeader,"shout",120);var parkedEnemy=travelling.Actors.First(p=>p.Enemy);float memory=parkedEnemy.Awareness.MemorySeconds;long heardId=parkedEnemy.Awareness.LastNoise;var parkedPosition=parkedEnemy.Position;
  void Gather(World world,string destination){var route=world.Campaign!.Connections.First(c=>c.FromAreaId==world.CurrentTravelDestinationId&&c.ToAreaId==destination);var exit=world.Features.First(f=>f.Id==route.ExitFeatureId);int index=0;var cells=world.Floors.Where(c=>c.Distance(exit.Pos)<=2.5f&&!world.Blocked(c)&&!world.Actors.Any(e=>e.Enemy&&e.Alive&&e.Pos==c)).ToArray();foreach(var survivor in world.Party)survivor.Pos=cells[index++];world.Reveal();}
  string origin=travelling.CurrentTravelDestinationId;Gather(travelling,"area.travel.service");check(travelling.TravelTo("area.travel.service"),"Phase 11 investigating enemies do not prevent a legal area exit");travelling.EmitLocalNoise(travelling.PartyLeader,"door",5);for(int i=0;i<10;i++)travelling.UpdateMovement(.1f);
  var parked=travelling.StoredTravelAreas.Single(area=>area.DestinationId==origin).Actors.Single(p=>p.Id==parkedEnemy.Id);check(parked.Awareness.MemorySeconds==memory&&parked.Awareness.LastNoise==heardId&&parked.Position==parkedPosition,"Phase 11 offscreen awareness freezes and ignores noise in another area");
  travelling.Save(path);travelling=World.Load(path);Gather(travelling,origin);check(travelling.TravelTo(origin)&&travelling.Actors.Single(p=>p.Id==parkedEnemy.Id).Awareness.MemorySeconds==memory,"Phase 11 parked awareness survives save/load and return travel without replay");
  var legacy=World.New(CampaignLoader.Load(CampaignLoader.BundledTacticalPath));check(!legacy.LocalAwarenessEnabled&&!legacy.Shout(legacy.PartyLeader)&&legacy.NoiseHistory.Count==0,"Phase 11 earlier tactical campaign retains silent legacy behavior");
  legacy.Save(path);var json=JsonNode.Parse(File.ReadAllText(path))!;json["Version"]=23;File.WriteAllText(path,json.ToJsonString());check(World.Load(path).Version==World.CurrentSaveVersion&&!World.Load(path).LocalAwarenessEnabled,"Phase 11 v23 save migration does not invent awareness rules");
  w=Fresh();w.EmitLocalNoise(w.PartyLeader,"shout",60);w.Save(path);json=JsonNode.Parse(File.ReadAllText(path))!;json["NoiseHistory"]![0]!["Sequence"]=0;File.WriteAllText(path,json.ToJsonString());bool rejected=false;try{World.Load(path);}catch(InvalidDataException){rejected=true;}check(rejected,"Phase 11 malformed noise sequence is rejected on load");File.Delete(path);
  var bad=CampaignLoader.Clone(definition);bad.SchemaVersion=8;check(CampaignLoader.Validate(bad).Count>0,"Phase 11 rejects awareness opt-in in older content schema");
  ExpeditionTests.RunFullRoute((ok,name)=>check(ok,name.Replace("Phase 8","Phase 11 awareness")),CampaignLoader.BundledAwarenessPath);
 }
}
