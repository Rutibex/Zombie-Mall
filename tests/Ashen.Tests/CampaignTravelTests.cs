using Ashen.Core;
using System.Text.Json.Nodes;

static class CampaignTravelTests
{
 public static void Run(Action<bool,string> check)
 {
  const string start="loc.lower.01",service="area.travel.service",roof="area.travel.roof";
  CampaignDefinition Definition()=>CampaignLoader.Load(CampaignLoader.BundledTravelPath);
  World Fresh()=>World.New(Definition());
  void Gather(World w,string to)
  {
   var route=w.Campaign!.Connections.Single(r=>r.FromAreaId==w.CurrentTravelDestinationId&&r.ToAreaId==to);var exit=w.Features.Single(f=>f.Id==route.ExitFeatureId);
   var cells=w.Floors.Where(c=>c.Center.Distance(exit.Pos.Center)<=2.5f&&!w.Blocked(c)&&!w.Actors.Any(a=>a.Enemy&&a.Alive&&a.Pos==c)).OrderBy(c=>c.Distance(exit.Pos)).ToArray();int i=0;
   foreach(var a in w.Party)a.Pos=cells[i++];w.Reveal();
  }
  void Clear(World w){w.StartCombat(0);foreach(var e in w.Actors.Where(a=>a.Enemy))e.Hp=0;w.CheckEnd();}
  var w=Fresh();check(w.Campaign!.SchemaVersion==2&&w.Campaign.Areas.Count==3&&w.Campaign.Connections.Count==4,"Phase 4 three-area fixture has explicit directed connections and entries");
  check(w.TravelState(service).Discovered&&!w.TravelState(roof).Discovered&&w.TravelRoutes.Count==2,"Phase 4 world map discovers neighbours without leaking unseen destinations");
  check(!w.PreviewTravel(service).Allowed&&!w.PreviewTravel(roof).Allowed,"Phase 4 distant and non-adjacent travel are denied");
  int serial=w.NextItemInstanceNumber,time=w.CampaignMinutes;check(!w.TravelTo(service)&&w.CurrentTravelDestinationId==start&&w.NextItemInstanceNumber==serial&&w.CampaignMinutes==time&&w.StoredTravelAreas.Count==0,"Phase 4 failed travel does not materialize areas, consume IDs or advance time");
  w.AdvanceCampaignSeconds(59.5);w.AdvanceCampaignSeconds(1.25);check(w.CampaignMinutes==time+1&&Math.Abs(w.CampaignSeconds-.75)<.001,"Phase 4 exploration clock retains fractional seconds");
  w.StartCombat(0);w.AdvanceCampaignSeconds(100);check(w.CampaignMinutes==time+1,"Phase 4 turn-based combat does not consume real-time exploration clock");foreach(var e in w.Actors.Where(a=>a.Enemy))e.Hp=0;w.CheckEnd();
  var hero=w.PartyLeader;hero.Hp-=3;int hp=hero.Hp;
  var box=w.Features.Single(f=>f.Id=="feature.fixture.supply-box");hero.Pos=new(9,18);w.Reveal();check(w.TakeContainerItem(hero,box,0,1)&&w.DropInventoryItem(hero,0,1),"Phase 4 persistent fixture creates container and ground-loot changes through real actions");
  var door=w.Features.Single(f=>f.Id=="feature.fixture.door");hero.Pos=new(15,16);w.Reveal();check(w.Interact(door),"Phase 4 source door opened");
  var radio=w.Features.Single(f=>f.Id=="feature.fixture.radio");hero.Pos=new(9,15);w.Reveal();check(w.Interact(radio),"Phase 4 source event executed");
  var note=w.AddMapNote(new(9.5f,15.5f),"Return here");check(note!=null,"Phase 4 source map note added");
  var explored=w.Explored.ToHashSet();Gather(w,service);int minutes=w.CampaignMinutes;serial=w.NextItemInstanceNumber;
  check(w.PreviewTravel(service).Allowed&&w.PreviewTravel(service).Allowed&&w.NextItemInstanceNumber==serial,"Phase 4 repeated preview is free of materialization side effects");
  var oldExit=w.Features.Single(f=>f.Id=="feature.travel.to-service");
  check(w.Interact(oldExit)&&w.CurrentTravelDestinationId==service&&w.CampaignMinutes==minutes+2&&w.CampaignSeconds==.75,"Phase 4 clicking an exit travels via its explicit entry and duration");
  check(w.PartyLeader.Hp==hp&&w.Party.Select(a=>a.Pos).Distinct().Count()==2&&w.Party.All(a=>w.CanStand(a.Position,a,false)),"Phase 4 party health and non-overlapping legal arrival positions survive travel");
  check(w.TravelState(roof).Discovered&&w.StoredTravelAreas.Count==1&&w.StoredTravelAreas[0].DestinationId==start,"Phase 4 leaving an area parks its state and reveals the next connection");
  check(!w.Interact(oldExit)&&!w.TakeContainerItem(w.PartyLeader,box,0),"Phase 4 stale source feature and loot references cannot act across areas");
  string save=Path.Combine(Path.GetTempPath(),"zombie-travel-"+Guid.NewGuid()+".json");
  try
  {
   void RoundTrip(string expected){w.Save(save);w=World.Load(save);check(w.CurrentTravelDestinationId==expected&&w.Version==World.CurrentSaveVersion,"Phase 4 save/load in "+expected);}
   RoundTrip(service);
   check(w.StoredTravelAreas[0].GroundPiles.Single().Inventory.Items.Single().Definition.Name=="Canned Beans","Phase 4 stored-area custom item definitions rebind before load validation");
   w.PartyLeader.Pos=new(15,16);w.Reveal();check(w.Interact(w.Features.Single(f=>f.Id=="feature.travel.service.door")),"Phase 4 second area door changed");
   w.RunCampaignEvent("event.travel.service");
   var serviceEnemy=w.Actors.Single(a=>a.Enemy);serviceEnemy.Hp=7;serviceEnemy.Character.SetCondition("fixture-marked","Marked",true,"spell:fixture");
   Gather(w,roof);check(w.TravelTo(roof),"Phase 4 second connection reaches the third area");RoundTrip(roof);
   check(w.StoredTravelAreas.Count==2&&w.PartyLeader.Hp==hp,"Phase 4 saving in third area retains both earlier areas and shared party state");
   Clear(w);check(w.ClearedCampaignEncounters.Contains("encounter.travel.roof")&&w.ClearedCampaignEncounters.Contains("encounter.fixture.shambler"),"Phase 4 encounter ledger separates equal group numbers in different areas");
   Gather(w,service);check(w.TravelTo(service)&&w.Actors.Single(a=>a.Enemy).Hp==7,"Phase 4 damaged enemy does not heal or respawn on revisit");
   check(w.Actors.Single(a=>a.Enemy).Character.HasCondition("fixture-marked"),"Phase 4 actor conditions survive area travel and save/load");
   check(w.Features.Single(f=>f.Id=="feature.travel.service.door").Used&&w.HasDialogueFlag("flag.travel.service"),"Phase 4 second area door and global event flags persist");
   Gather(w,start);check(w.TravelTo(start),"Phase 4 return to original area");RoundTrip(start);
   check(w.Features.Single(f=>f.Id==door.Id).Used&&w.Features.Single(f=>f.Id==radio.Id).Used&&!w.RunCampaignEvent("event.fixture.radio"),"Phase 4 opened door and one-time event remain changed after two travel/load cycles");
   check(w.Features.Single(f=>f.Id==box.Id).Contents.Items.First().Quantity==2&&w.GroundPiles.Single().Inventory.Count("item.commodity.001")==1,"Phase 4 looted container and dropped supplies persist without duplication");
   check(w.Actors.Where(a=>a.Enemy).All(a=>!a.Alive)&&explored.IsSubsetOf(w.Explored.ToHashSet())&&w.MapNotes.Any(n=>n.Text=="Return here"),"Phase 4 defeated enemies, exploration and map notes persist");
   check(w.Quests[0].Objectives.Single(o=>o.Id=="objective.travel.roof").Completed&&w.Quests[0].Objectives.Single(o=>o.Id=="objective.travel.roof-cleared").Completed,"Phase 4 visited-area and encounter objectives remain complete away from their area");
   var state=w.StoredTravelAreas.Single(a=>a.DestinationId==service);state.Features.Single(f=>f.Id=="feature.travel.service.door").Used=true;w.SyncCampaignObjectives();
   check(w.Quests[0].Objectives.Single(o=>o.Id=="objective.travel.service-door").Completed,"Phase 4 feature objectives resolve against stored areas");
   // Block the actual saved arrival anchor: failure cannot replace state or charge time.
   var entry=w.Campaign!.Areas.Single(a=>a.Id==service).EntryPoints.First(e=>e.Id=="entry.travel.service.west");var removed=state.Floors.Remove(entry.Position);Gather(w,service);int before=w.CampaignMinutes;int beforeSerial=w.NextItemInstanceNumber;
   check(removed&&!w.PreviewTravel(service).Allowed&&!w.TravelTo(service)&&w.CurrentTravelDestinationId==start&&w.CampaignMinutes==before&&w.NextItemInstanceNumber==beforeSerial,"Phase 4 blocked saved arrival fails atomically");state.Floors.Add(entry.Position);
   w.Save(save);var root=JsonNode.Parse(File.ReadAllText(save))!.AsObject();root["StoredTravelAreas"]![0]!["DestinationId"]="missing.area";File.WriteAllText(save,root.ToJsonString());bool bad=false;try{World.Load(save);}catch(InvalidDataException){bad=true;}check(bad,"Phase 4 rejects unknown saved area identities instead of dropping state");
   w.Save(save);root=JsonNode.Parse(File.ReadAllText(save))!.AsObject();root["StoredTravelAreas"]!.AsArray().Add(root["StoredTravelAreas"]![0]!.DeepClone());File.WriteAllText(save,root.ToJsonString());bad=false;try{World.Load(save);}catch(InvalidDataException){bad=true;}check(bad,"Phase 4 rejects duplicate area snapshots");
  }
  finally{File.Delete(save);}
  void Invalid(string name,Action<CampaignDefinition> change,string code){var c=Definition();change(c);check(CampaignLoader.Validate(c).Any(e=>e.Code==code),"Phase 4 rejects "+name);}
  Invalid("missing destination entry",c=>c.Connections[0].EntryPointId="missing.entry","reference");
  Invalid("exit from another area",c=>c.Connections[0].ExitFeatureId="feature.travel.roof.to-service","reference");
  Invalid("unknown destination",c=>c.Connections[0].ToAreaId="missing.area","reference");
  Invalid("duplicate route",c=>c.Connections.Add(c.Connections[0]),"duplicate");
  Invalid("unwalkable arrival",c=>c.Areas[1].EntryPoints[0].Position=new(45,31),"entry");
  Invalid("route flags without a producer",c=>c.Connections[0].RequiredFlag="missing.flag","reference");
  Invalid("negative travel time",c=>c.Connections[0].Minutes=-1,"range");
  Invalid("unreachable authored area",c=>c.Connections.RemoveAll(r=>r.ToAreaId==roof),"connection");
  var locked=Definition();locked.Connections[0].RequiredFlag="flag.fixture.radio";var lw=World.New(locked);Clear(lw);Gather(lw,service);check(!lw.PreviewTravel(service).Allowed,"Phase 4 route flags gate travel");lw.SetDialogueFlag("flag.fixture.radio");check(lw.PreviewTravel(service).Allowed,"Phase 4 satisfied route flag permits travel");
  lw.SetDialogueFlag("flag.fixture.radio",false);check(!lw.PreviewTravel(service).Allowed&&!lw.TravelState(service).Unlocked,"Phase 4 clearing a route flag updates both gate and map state");lw.SetDialogueFlag("flag.fixture.radio");
  lw.Party.Last().Hp=0;check(!lw.PreviewTravel(service).Allowed,"Phase 4 unconscious party members block travel");
  var six=Definition();for(int i=0;i<4;i++)
  {
   var a=new CampaignActor{Id="actor.travel.extra-"+i,Name="Extra "+i,Source=new(){AuditId="class.strong",PageStart=112,PageEnd=118},WeaponId="item.melee.006"};six.Actors.Add(a);six.Areas[0].Actors.Add(new(){ActorId=a.Id,Position=new(3+i,13)});
  }
  var full=World.New(six);Clear(full);Gather(full,service);check(full.TravelTo(service)&&full.PartyCount==6&&full.Party.Select(a=>a.Pos).Distinct().Count()==6,"Phase 4 full six-member party gets distinct legal arrivals");
  check(full.StoredTravelAreas.Single().Actors.All(a=>a.Enemy),"Phase 4 area snapshots never duplicate the travelling party");
  var rewards=Definition();rewards.Encounters[0].RewardXp=10;rewards.Encounters[0].RewardCurrency=3;var rw=World.New(rewards);Clear(rw);Clear(rw);
  check(rw.Party.All(a=>a.Character.Experience==10)&&rw.Gold==3,"Phase 4 cleared encounter reward is paid only once");
  string oldSave=Path.Combine(Path.GetTempPath(),"zombie-v16-"+Guid.NewGuid()+".json");
  try
  {
   var old=World.New(CampaignLoader.Load(CampaignLoader.BundledFixturePath));old.Save(oldSave);var root=JsonNode.Parse(File.ReadAllText(oldSave))!.AsObject();root["Version"]=16;root.Remove("CampaignSeconds");root.Remove("ClearedCampaignEncounters");root["Campaign"]!.AsObject().Remove("Connections");foreach(var a in root["Campaign"]!["Areas"]!.AsArray()){a!.AsObject().Remove("EntryPoints");a.AsObject().Remove("MapX");a.AsObject().Remove("MapY");}File.WriteAllText(oldSave,root.ToJsonString());var migrated=World.Load(oldSave);
   check(migrated.Version==World.CurrentSaveVersion&&migrated.Campaign!.Areas.Count==1&&migrated.Campaign.Connections.Count==0,"Phase 4 v16 campaign saves migrate without injecting new travel areas");
  }
  finally{File.Delete(oldSave);}
 }
}
