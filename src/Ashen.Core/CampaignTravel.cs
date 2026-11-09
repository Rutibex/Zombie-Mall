using System.Text.Json.Serialization;
namespace Ashen.Core;

public static class CampaignTravelRules
{
 // Breadth-first walkable entry region. Deterministic ordering, no actor overlap and no crossing closed doors.
 public static List<Cell> ArrivalCells(IEnumerable<Cell> floorCells,IEnumerable<Cell> closedDoors,IEnumerable<Cell> occupied,Cell entry)
 {
  var floor=floorCells.ToHashSet();floor.ExceptWith(closedDoors);var taken=occupied.ToHashSet();List<Cell> result=[];
  if(!floor.Contains(entry)||taken.Contains(entry))return result;
  var seen=new HashSet<Cell>{entry};var q=new Queue<Cell>();q.Enqueue(entry);
  while(q.Count>0){var p=q.Dequeue();if(!taken.Contains(p))result.Add(p);foreach(var n in p.Neighbors())if(entry.Distance(n)<=4&&floor.Contains(n)&&!taken.Contains(n)&&seen.Add(n))q.Enqueue(n);}
  return result;
 }
}

public sealed partial class World
{
 public List<string> ClearedCampaignEncounters {get;set;}=[];
 public double CampaignSeconds {get;set;}
 [JsonIgnore] public Action? CampaignAreaChanged {get;set;}
 public void AdvanceCampaignSeconds(double seconds)
 {
  if(Campaign==null||Mode!=Mode.Explore)return;
  if(!double.IsFinite(seconds)||seconds<0||seconds>3600)throw new ArgumentOutOfRangeException(nameof(seconds));
  double total=CampaignSeconds+seconds;int minutes=(int)(total/60);CampaignMinutes=checked(CampaignMinutes+minutes);CampaignSeconds=total-minutes*60;ProcessSurvivalTime();
 }
 IEnumerable<Actor>? CampaignAreaEnemies(string id)=>id==CurrentTravelDestinationId?Actors.Where(a=>a.Enemy):StoredTravelAreas.FirstOrDefault(a=>a.DestinationId==id)?.Actors;
 internal IEnumerable<ItemStack> AllCampaignStacks()
 {
  IEnumerable<ItemStack> ActorItems(IEnumerable<Actor> actors)=>actors.SelectMany(a=>a.Inventory.Items.Concat(a.Equipment.All()));
  return ActorItems(Actors).Concat(ActorItems(ShelterResidents.Select(r=>r.Actor))).Concat(SharedStorage.Items).Concat(Features.SelectMany(f=>f.Contents.Items)).Concat(GroundPiles.SelectMany(p=>p.Inventory.Items)).Concat(TraderStock.Items).Concat((Stores??[]).SelectMany(s=>s.Stock.Items))
   .Concat(StoredTravelAreas.SelectMany(a=>ActorItems(a.Actors).Concat(a.Features.SelectMany(f=>f.Contents.Items)).Concat(a.GroundPiles.SelectMany(p=>p.Inventory.Items)))).SelectMany(s=>s.LoadedAmmunition is {} ammo?new[]{s,ammo}:new[]{s});
 }
 void DiscoverCampaignNeighbours()
 {
  if(Campaign==null)return;
  foreach(var r in Campaign.Connections.Where(r=>r.FromAreaId==CurrentTravelDestinationId))
  {var state=TravelState(r.ToAreaId);state.Discovered=true;state.Unlocked=r.RequiredFlag==""||HasDialogueFlag(r.RequiredFlag);}
 }
 CampaignConnection? CampaignConnectionTo(string id)=>Campaign?.Connections.FirstOrDefault(r=>r.FromAreaId==CurrentTravelDestinationId&&r.ToAreaId==id);
 public bool UseCampaignExit(Feature feature)
 {
  if(Campaign==null)return false;var route=Campaign.Connections.FirstOrDefault(r=>r.FromAreaId==CurrentTravelDestinationId&&r.ExitFeatureId==feature.Id);
  if(route==null){LogSystem("This exit has no valid connection.",true);return false;}return TravelCampaignTo(route.ToAreaId);
 }
 List<Cell> CampaignArrival(string destinationId,string entryId)
 {
  var definition=Campaign!.Areas.First(a=>a.Id==destinationId);var entry=definition.EntryPoints.First(e=>e.Id==entryId);
  var state=StoredTravelAreas.FirstOrDefault(a=>a.DestinationId==destinationId);
  return state==null
   ?CampaignTravelRules.ArrivalCells(definition.Floors,definition.Features.Where(f=>f.Kind=="door").Select(f=>f.Position).Concat(CampaignGeometry.SolidCells(definition)),definition.Actors.Where(p=>Campaign.Actors.First(a=>a.Id==p.ActorId).Enemy).Select(p=>p.Position),entry.Position)
   :CampaignTravelRules.ArrivalCells(state.Floors,state.Features.Where(f=>f.Kind=="door"&&!f.Used).Select(f=>f.Pos).Concat(CampaignGeometry.SolidCells(definition)),state.Actors.Where(a=>a.Alive).Select(a=>a.Pos),entry.Position);
 }
 public TravelPreview PreviewCampaignTravel(string id)
 {
  if(Campaign==null)return TravelPreview.Denied("No campaign is loaded.");
  if(Mode!=Mode.Explore||PendingReaction!=null)return TravelPreview.Denied("Finish combat before travelling.");
  if(Exterior30TravelReason(id) is {} ext&&ext!="")return TravelPreview.Denied(ext);
  if(Roof29TravelReason(id) is {} finalRoof&&finalRoof!="")return TravelPreview.Denied(finalRoof);
  if(Roof28TravelReason() is {} roof&&roof!="")return TravelPreview.Denied(roof);
  if(Party.Any(a=>a.Character.HasCondition("upper26-freezer-trapped")))return TravelPreview.Denied("Release the trapped survivor from the freezer before leaving.");
  if(CurrentLowerMallSite?.Number==27&&Party.Any(a=>a.HasLowerGear("the black cup")))return TravelPreview.Denied("The Black Cup cannot leave Perk Down. Put it back into a café cache first.");
  if(Party.Any(a=>!a.Alive))return TravelPreview.Denied("Every expedition member must be conscious before travelling.");
  var route=CampaignConnectionTo(id);if(route==null)return TravelPreview.Denied("There is no outgoing connection to that area.");
  if(route.RequiredFlag!=""&&!HasDialogueFlag(route.RequiredFlag))return TravelPreview.Denied("This route is locked.");
  var exit=Features.FirstOrDefault(f=>f.Id==route.ExitFeatureId&&f.Kind=="transition");
  if(exit==null||!IsFeatureDiscovered(exit)||exit.Hidden||exit.Locked||exit.DetectedTrap)return TravelPreview.Denied("Find and unlock the exit before travelling.");
  if(Party.Any(a=>a.Position.Distance(exit.Pos.Center)>3.0f))return TravelPreview.Denied("Gather the entire party beside the exit.");
  if((long)CampaignMinutes+route.Minutes>int.MaxValue)return TravelPreview.Denied("Travel would exceed the campaign clock limit.");
  if(CampaignArrival(id,route.EntryPointId).Count<PartyCount)return TravelPreview.Denied("The destination entry is blocked or has no room for the party.");
  return new(true,"",route.Minutes,0,"");
 }
 public bool TravelCampaignTo(string id)
 {
  var preview=PreviewCampaignTravel(id);if(!preview.Allowed){LogSystem(preview.Reason,true);return false;}
  var route=CampaignConnectionTo(id)!;var arrival=CampaignArrival(id,route.EntryPointId);var party=Party.ToList();
  var target=StoredTravelAreas.FirstOrDefault(a=>a.DestinationId==id);int nextId=NextItemInstanceNumber;
  // Prepare a never-visited area on a separate builder: preview/failure must not consume live item IDs.
  if(target==null){var builder=new World{Campaign=Campaign,NextItemInstanceNumber=NextItemInstanceNumber};target=builder.MaterializeCampaignArea(id);nextId=builder.NextItemInstanceNumber;}
  ValidateCampaignArea(target,false);
  string sourceName=CurrentAreaName;var source=new WorldAreaSnapshot{DestinationId=CurrentTravelDestinationId,Floors=Floors,Explored=Explored,Features=Features,Actors=Actors.Where(a=>a.Enemy).ToList(),GroundPiles=GroundPiles,MapNotes=MapNotes,NextMapNoteNumber=NextMapNoteNumber};
  if(CurrentLowerMallSite?.Number==42&&LowerMallProgress(42) is {} training&&training.Events.Contains("training-started")&&CampaignMinutes<training.HarvestMinute){training.Events.Remove("training-started");training.Resolved.Remove("action.lower.42.device.train");training.HarvestMinute=-1;}StopAll();ClearReactionInterrupts();foreach(var member in party){member.Character.SetCondition("chapter-looking-away","Looking away",false);LowerMallEffects.RemoveAll(e=>e.ActorId==member.Id&&e.Condition=="chapter-looking-away");}
  StoredTravelAreas.RemoveAll(a=>a.DestinationId==id||a.DestinationId==CurrentTravelDestinationId);StoredTravelAreas.Add(source);
  CurrentTravelDestinationId=id;Floors=target.Floors;Explored=target.Explored;Features=target.Features;Actors=party.Concat(target.Actors).ToList();GroundPiles=target.GroundPiles;MapNotes=target.MapNotes;NextMapNoteNumber=target.NextMapNoteNumber;
  NextItemInstanceNumber=nextId;floorCache=null;Visible.Clear();Routes.Clear();Order.Clear();Turn=0;CombatGroup=-1;Mode=Mode.Explore;
  for(int i=0;i<party.Count;i++){party[i].Pos=arrival[i];party[i].Motion=0;}
  Exterior30Travel(route);Roof29Travel(route);CampaignMinutes+=route.Minutes;ProcessSurvivalTime();TravelCount++;LastTravelEventId="";
  var dest=TravelState(id);dest.Discovered=true;dest.Unlocked=true;dest.Visits++;DiscoverCampaignNeighbours();AssignMissingItemIds();
  Reveal();EnterLowerMall(true);SyncCampaignObjectives();RecordTravelArrival(id);
  Message(MessageCategory.Travel,$"Travelled from {sourceName} to {CurrentAreaName}.",$"Elapsed: {route.Minutes} minutes. {CampaignTimeText}.");
  CampaignAreaChanged?.Invoke();return true;
 }
 (int Xp,int Currency) CampaignEncounterReward(int group)
 {
  var encounter=Campaign!.Encounters.FirstOrDefault(e=>e.AreaId==CurrentTravelDestinationId&&e.Group==group);
  if(LocalAwarenessEnabled&&Actors.Any(a=>a.Enemy&&a.Group==group&&a.Alive))return (0,0);
  if(encounter!=null&&StoryEnabled&&Campaign!.Story!.EncounterPlans.Any(p=>p.Waves.Contains(encounter.Id))&&!encounter.ActorIds.All(id=>Actors.Any(a=>a.Id==id&&!a.Alive)))return (0,0);
  if(encounter==null||ClearedCampaignEncounters.Contains(encounter.Id))return (0,0);
  ClearedCampaignEncounters.Add(encounter.Id);return(encounter.RewardXp,encounter.RewardCurrency);
 }
 void ValidateCampaignArea(WorldAreaSnapshot area,bool allowParty)
 {
  var def=Campaign!.Areas.FirstOrDefault(a=>a.Id==area.DestinationId)??throw new InvalidDataException("Unknown stored campaign area.");
  if(area.Floors==null||area.Explored==null||area.Actors==null||area.Features==null||area.GroundPiles==null||area.MapNotes==null)throw new InvalidDataException("Null campaign area collection.");
  var floor=area.Floors.ToHashSet();
  if(floor.Count==0||floor.Count!=area.Floors.Count||floor.Any(c=>c.X<0||c.X>=Width||c.Y<0||c.Y>=Height)||area.Explored.Any(c=>!floor.Contains(c)))throw new InvalidDataException("Invalid saved campaign geometry or exploration.");
  if(area.Features.Select(f=>f.Id).Distinct().Count()!=area.Features.Count||area.Features.Any(f=>def.Features.All(d=>d.Id!=f.Id||(d.Kind=="event"?"campaign-event":d.Kind)!=f.Kind||d.EventId!=f.CampaignEventId||d.DialogueId!=f.DialogueId)||!floor.Contains(f.Pos)))throw new InvalidDataException("Invalid saved campaign feature IDs or positions.");
  if(area.Actors.Select(a=>a.Id).Distinct().Count()!=area.Actors.Count)throw new InvalidDataException("Duplicate saved area actors.");
  var geometry=new World{Floors=area.Floors,Features=area.Features,Campaign=Campaign,CurrentTravelDestinationId=area.DestinationId};
  foreach(var a in area.Actors)
  {
   if(Campaign.Actors.All(d=>d.Id!=a.Id||d.Enemy!=a.Enemy)||(!allowParty&&!a.Enemy)||!a.Enemy&&Campaign.Actors.All(d=>d.Id!=a.Id||d.Enemy)||a.Enemy&&def.Actors.All(p=>p.ActorId!=a.Id))throw new InvalidDataException("Actor belongs to a different campaign area.");
   if(!float.IsFinite(a.Position.X)||!float.IsFinite(a.Position.Y)||!geometry.SavedActorCanStand(a))throw new InvalidDataException("Invalid stored actor position.");
   a.Character.ValidateOrThrow(a.Id);a.Inventory.ValidateOrThrow(a.Id);a.Equipment.ValidateOrThrow(a.Id);
  }
  foreach(var f in area.Features)f.Contents.ValidateOrThrow(f.Id);
  foreach(var pile in area.GroundPiles){if(!floor.Contains(pile.Pos))throw new InvalidDataException("Invalid stored ground pile.");pile.Inventory.ValidateOrThrow("stored ground");}
  if(area.NextMapNoteNumber<1||area.MapNotes.Select(n=>n.Id).Distinct().Count()!=area.MapNotes.Count||area.MapNotes.Any(n=>!float.IsFinite(n.Position.X)||!float.IsFinite(n.Position.Y)||!floor.Contains(n.Position.Cell)))throw new InvalidDataException("Invalid stored map notes.");
 }
 void ValidateCampaignAreaStates()
 {
  if(TravelDestinations==null||TravelDestinations.Any(s=>s.Visits<0||Campaign!.Areas.All(a=>a.Id!=s.Id)))throw new InvalidDataException("Invalid campaign destination state.");
  if(StoredTravelAreas==null||StoredTravelAreas.Any(a=>a.DestinationId==CurrentTravelDestinationId)||StoredTravelAreas.Select(a=>a.DestinationId).Distinct().Count()!=StoredTravelAreas.Count)throw new InvalidDataException("Duplicate/current stored campaign area.");
  if(ClearedCampaignEncounters==null||ClearedCampaignEncounters.Distinct().Count()!=ClearedCampaignEncounters.Count||ClearedCampaignEncounters.Any(id=>Campaign!.Encounters.All(e=>e.Id!=id)))throw new InvalidDataException("Invalid cleared encounter ledger.");
  if(!double.IsFinite(CampaignSeconds)||CampaignSeconds<0||CampaignSeconds>=60)throw new InvalidDataException("Invalid campaign clock remainder.");
  ValidateCampaignArea(new(){DestinationId=CurrentTravelDestinationId,Floors=Floors,Explored=Explored,Actors=Actors,Features=Features,GroundPiles=GroundPiles,MapNotes=MapNotes,NextMapNoteNumber=NextMapNoteNumber},true);
  foreach(var area in StoredTravelAreas)ValidateCampaignArea(area,false);
 }
}
