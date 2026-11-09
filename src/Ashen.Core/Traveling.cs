using System.Text.Json;
using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TravelDestinationAvailability { Locked, Unlocked, Current }

public sealed record TravelDestinationDefinition
{
 public string Id {get;init;}="";
 public string Name {get;init;}="";
 public string Region {get;init;}="Orlan Vale";
 public string Description {get;init;}="";
 public int MapX {get;init;}
 public int MapY {get;init;}
 public WorldPoint Arrival {get;init;}
 public string ExitFeatureName {get;init;}="";
 public bool InitiallyDiscovered {get;init;}
 public bool InitiallyUnlocked {get;init;}
}

public sealed record TravelRouteDefinition
{
 public string A {get;init;}="";
 public string B {get;init;}="";
 public int Minutes {get;init;}
 public int EncounterChancePercent {get;init;}
 public string EventId {get;init;}="";
 public string ExitA {get;init;}="";
 public string ExitB {get;init;}="";
 public bool Connects(string from,string to)=>(A==from&&B==to)||(A==to&&B==from);
 public string Other(string id)=>id==A?B:id==B?A:"";
 public string ExitFor(string id)=>id==A?ExitA:id==B?ExitB:"";
}

public sealed record TravelEventDefinition
{
 public string Id {get;init;}="";
 public string Name {get;init;}="";
 public string Text {get;init;}="";
 public int GoldDelta {get;init;}
 public int RationsDelta {get;init;}
}

public sealed class TravelDestinationState
{
 public string Id {get;set;}="";
 public bool Discovered {get;set;}
 public bool Unlocked {get;set;}
 public int Visits {get;set;}
}

/// <summary>
/// Durable non-current area snapshot. The active area continues to live in World.Floors,
/// Features, Actors, GroundPiles and MapNotes so the rest of the rules layer does not need
/// an area-manager abstraction. A snapshot is made only when the party leaves an area.
/// </summary>
public sealed class WorldAreaSnapshot
{
 public string DestinationId {get;set;}="";
 public List<Cell> Floors {get;set;}=[];
 public List<Cell> Explored {get;set;}=[];
 public List<Feature> Features {get;set;}=[];
 public List<Actor> Actors {get;set;}=[];
 public List<GroundPile> GroundPiles {get;set;}=[];
 public List<MapNote> MapNotes {get;set;}=[];
 public int NextMapNoteNumber {get;set;}=1;
}

public readonly record struct TravelPreview(bool Allowed,string Reason,int Minutes,int EncounterChancePercent,string EventName)
{
 public static TravelPreview Denied(string reason)=>new(false,reason,0,0,"");
}

public static class TravelCatalog
{
 static readonly TravelDestinationDefinition[] destinations=
 [
  new(){Id="saint-orlans-crypt",Name="Saint Orlan's Crypt",Description="The sealed burial complex beneath the shrine district, where the Ember Heart was taken.",MapX=270,MapY=390,Arrival=new(4.8f,16.5f),ExitFeatureName="Stairs to daylight",InitiallyDiscovered=true,InitiallyUnlocked=true},
  new(){Id="saint-orlan",Name="Saint Orlan",Description="A fortified shrine-town where pilgrims, wardens, and frightened townsfolk wait above the old crypts.",MapX=520,MapY=315,Arrival=new(7.5f,16.5f),ExitFeatureName="West Pilgrim Gate",InitiallyDiscovered=true,InitiallyUnlocked=true},
  new(){Id="briarwood-road",Name="Briarwood Road",Description="A forest road of standing stones and abandoned toll shrines leading north from Saint Orlan.",MapX=760,MapY=225,Arrival=new(7.5f,15.5f),ExitFeatureName="Orlan Road Marker",InitiallyDiscovered=false,InitiallyUnlocked=false},
  new(){Id="ashen-ford",Name="Ashen Ford",Description="A broad river crossing choked with grey reeds. The far bank is visible, but the safe route must be learned.",MapX=980,MapY=355,Arrival=new(8.5f,20.5f),ExitFeatureName="Ford Road Cairn",InitiallyDiscovered=true,InitiallyUnlocked=false},
  new(){Id="old-watchtower",Name="Old Watchtower",Description="A ruined hill tower overlooking the eastern road and the black pines beyond Orlan Vale.",MapX=1115,MapY=165,Arrival=new(11.5f,23.5f),ExitFeatureName="Watchtower Trail",InitiallyDiscovered=false,InitiallyUnlocked=false}
 ];
 static readonly TravelRouteDefinition[] routes=
 [
  new(){A="saint-orlans-crypt",B="saint-orlan",Minutes=45,EncounterChancePercent=100,EventId="pilgrim-bells",ExitA="Stairs to daylight",ExitB="West Pilgrim Gate"},
  new(){A="saint-orlan",B="briarwood-road",Minutes=90,EncounterChancePercent=30,EventId="fallen-cart",ExitA="North Road Gate",ExitB="Orlan Road Marker"},
  new(){A="briarwood-road",B="ashen-ford",Minutes=120,EncounterChancePercent=35,EventId="roadside-sign",ExitA="Fordward Trail",ExitB="Ford Road Cairn"},
  new(){A="ashen-ford",B="old-watchtower",Minutes=60,EncounterChancePercent=25,EventId="watchfire",ExitA="Watchtower Track",ExitB="Watchtower Trail"}
 ];
 static readonly TravelEventDefinition[] events=
 [
  new(){Id="pilgrim-bells",Name="Pilgrim Bells",Text="Halfway to Saint Orlan, the wind carries three slow bells from the shrine hill. The road is clear, but everyone knows the town has seen you return."},
  new(){Id="fallen-cart",Name="Fallen Pilgrim Cart",Text="A broken pilgrim cart lies in the ditch. The party recovers a few scattered coins before moving on.",GoldDelta=7},
  new(){Id="roadside-sign",Name="Ash-Marked Sign",Text="Fresh ash marks on an old road sign warn of movement along the river. The party takes the slower guarded path and reaches the ford without incident."},
  new(){Id="watchfire",Name="Distant Watchfire",Text="A solitary fire burns in the ruined tower before vanishing behind the ridge. Someone has been there recently."}
 ];
 public static IReadOnlyList<TravelDestinationDefinition> Destinations=>destinations;
 public static IReadOnlyList<TravelRouteDefinition> Routes=>routes;
 public static TravelDestinationDefinition Destination(string id)=>destinations.FirstOrDefault(x=>x.Id==id)??throw new KeyNotFoundException($"Unknown travel destination '{id}'.");
 public static TravelRouteDefinition? Route(string from,string to)=>routes.FirstOrDefault(x=>x.Connects(from,to));
 public static TravelEventDefinition? Event(string id)=>events.FirstOrDefault(x=>x.Id==id);
}

public sealed partial class World
{
 public string CurrentTravelDestinationId {get;set;}="saint-orlans-crypt";
 public List<TravelDestinationState> TravelDestinations {get;set;}=[];
 public List<WorldAreaSnapshot> StoredTravelAreas {get;set;}=[];
 public int CampaignMinutes {get;set;}=8*60;
 public int TravelCount {get;set;}
 public string LastTravelEventId {get;set;}="";
 [JsonIgnore] public TravelDestinationDefinition CurrentTravelDestination=>ResolveTravelDestination(CurrentTravelDestinationId);
 [JsonIgnore] public string CurrentAreaName=>CurrentTravelDestination.Name;
 [JsonIgnore] public int CampaignDay=>1+Math.Max(0,CampaignMinutes)/1440;
 [JsonIgnore] public int CampaignHour=>(Math.Max(0,CampaignMinutes)%1440)/60;
 [JsonIgnore] public int CampaignMinute=>Math.Max(0,CampaignMinutes)%60;
 [JsonIgnore] public string CampaignTimeText=>$"Day {CampaignDay}, {CampaignHour:00}:{CampaignMinute:00}";
 [JsonIgnore] public IReadOnlyList<TravelRouteDefinition> TravelRoutes=>Campaign==null?TravelCatalog.Routes:Campaign.Connections.GroupBy(r=>string.Join("|",new[]{r.FromAreaId,r.ToAreaId}.OrderBy(x=>x,StringComparer.Ordinal))).Select(g=>CampaignRouteDefinition(g.First())).ToArray();

 public void InitializeTravelState()
 {
  TravelDestinations=TravelDefinitions.Select(d=>new TravelDestinationState{Id=d.Id,Discovered=d.InitiallyDiscovered,Unlocked=d.InitiallyUnlocked,Visits=d.Id==CurrentTravelDestinationId?1:0}).ToList();
  StoredTravelAreas=[];CampaignMinutes=Math.Max(0,CampaignMinutes);TravelCount=0;LastTravelEventId="";
 }
 public TravelDestinationState TravelState(string id)
 {
  var state=TravelDestinations.FirstOrDefault(x=>x.Id==id);
  if(state!=null)return state;
  var def=ResolveTravelDestination(id);state=new(){Id=id,Discovered=def.InitiallyDiscovered,Unlocked=def.InitiallyUnlocked};TravelDestinations.Add(state);return state;
 }
 public TravelDestinationAvailability TravelAvailability(string id)
 {
  if(id==CurrentTravelDestinationId)return TravelDestinationAvailability.Current;
  var state=TravelState(id);return state.Unlocked?TravelDestinationAvailability.Unlocked:TravelDestinationAvailability.Locked;
 }
 public IEnumerable<TravelDestinationDefinition> DiscoveredTravelDestinations()=>TravelDefinitions.Where(d=>TravelState(d.Id).Discovered);
 public string TravelExitName(string destinationId)
 {
  var route=ResolveTravelRoute(CurrentTravelDestinationId,destinationId);return route?.ExitFor(CurrentTravelDestinationId)??"";
 }
 public Feature? LegalTravelExit(string destinationId)
 {
  if(Campaign!=null){var route=CampaignConnectionTo(destinationId);return route==null?null:Features.FirstOrDefault(f=>f.Id==route.ExitFeatureId&&IsFeatureDiscovered(f));}
  string expected=TravelExitName(destinationId);if(string.IsNullOrWhiteSpace(expected))return null;return Features.FirstOrDefault(f=>f.Name.Equals(expected,StringComparison.Ordinal)&&IsFeatureDiscovered(f));
 }
 public bool PartyAtTravelExit(string destinationId)
 {
  var exit=LegalTravelExit(destinationId);return exit!=null&&Party.Where(a=>a.Alive).Any()&&Party.Where(a=>a.Alive).All(a=>a.Position.Distance(exit.Pos.Center)<=2.25f);
 }
 public Feature? LegalTravelExit()=>TravelRoutes.Where(r=>r.A==CurrentTravelDestinationId||r.B==CurrentTravelDestinationId).Select(r=>r.Other(CurrentTravelDestinationId)).Select(id=>LegalTravelExit(id)).FirstOrDefault(f=>f!=null);
 public bool PartyAtLegalTravelExit()=>TravelRoutes.Where(r=>r.A==CurrentTravelDestinationId||r.B==CurrentTravelDestinationId).Select(r=>r.Other(CurrentTravelDestinationId)).Any(PartyAtTravelExit);
 public TravelPreview PreviewTravel(string destinationId)
 {
  if(Campaign!=null)return PreviewCampaignTravel(destinationId);
  if(Mode!=Mode.Explore)return TravelPreview.Denied("World travel is only available while exploring outside combat.");
  if(destinationId==CurrentTravelDestinationId)return TravelPreview.Denied("The party is already here.");
  TravelDestinationDefinition destination;try{destination=ResolveTravelDestination(destinationId);}catch{return TravelPreview.Denied("That destination is not part of the known world map.");}
  var state=TravelState(destination.Id);if(!state.Discovered)return TravelPreview.Denied("That destination has not been discovered.");if(!state.Unlocked)return TravelPreview.Denied("The route is known, but it is still locked.");
  var route=ResolveTravelRoute(CurrentTravelDestinationId,destination.Id);if(route==null)return TravelPreview.Denied("No direct route leaves the current region for that destination.");
  string exitName=route.ExitFor(CurrentTravelDestinationId);if(!PartyAtTravelExit(destination.Id))return TravelPreview.Denied($"Reach {exitName} before beginning world travel.");
  var evt=LookupTravelEvent(route.EventId);return new(true,"",route.Minutes,route.EncounterChancePercent,evt?.Name??"");
 }
 public bool DiscoverTravelDestination(string id,bool unlock=false)
 {
  var state=TravelState(id);bool changed=!state.Discovered||(unlock&&!state.Unlocked);state.Discovered=true;if(unlock)state.Unlocked=true;return changed;
 }
 public bool UnlockTravelDestination(string id){var state=TravelState(id);bool changed=!state.Discovered||!state.Unlocked;state.Discovered=true;state.Unlocked=true;return changed;}

 static T TravelClone<T>(T value)
 {
  var clone=JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
  if(clone is null)throw new InvalidDataException("Could not clone travel-area state.");
  return clone;
 }
 WorldAreaSnapshot SnapshotCurrentArea()=>new()
 {
  DestinationId=CurrentTravelDestinationId,
  Floors=Floors.ToList(),Explored=Explored.ToList(),Features=TravelClone(Features),
  Actors=TravelClone(Actors.Where(a=>a.Enemy).ToList()),GroundPiles=TravelClone(GroundPiles),MapNotes=TravelClone(MapNotes),NextMapNoteNumber=NextMapNoteNumber
 };
 void StoreCurrentTravelArea()
 {
  StoredTravelAreas.RemoveAll(a=>a.DestinationId==CurrentTravelDestinationId);StoredTravelAreas.Add(SnapshotCurrentArea());
 }
 WorldAreaSnapshot TravelAreaFor(string id)
 {
  var stored=StoredTravelAreas.FirstOrDefault(a=>a.DestinationId==id);return stored!=null?TravelClone(stored):CreateTravelArea(id);
 }
 void LoadTravelArea(string id)
 {
  var party=Party.ToList();var area=TravelAreaFor(id);StoredTravelAreas.RemoveAll(a=>a.DestinationId==id);
  CurrentTravelDestinationId=id;Floors=area.Floors.ToList();Explored=area.Explored.ToList();Features=TravelClone(area.Features);GroundPiles=TravelClone(area.GroundPiles);MapNotes=TravelClone(area.MapNotes);NextMapNoteNumber=Math.Max(1,area.NextMapNoteNumber);
  Actors=party.Concat(TravelClone(area.Actors)).ToList();floorCache=null;Visible.Clear();Routes.Clear();Order.Clear();Turn=0;CombatGroup=-1;Mode=Mode.Explore;
  PlacePartyAtTravelArrival(CurrentTravelDestination.Arrival);Reveal();
 }
 void PlacePartyAtTravelArrival(WorldPoint arrival)
 {
  WorldPoint[] offsets=[new(0,0),new(-.72f,-.56f),new(-.72f,.56f),new(-1.42f,0),new(-1.42f,-.82f),new(-1.42f,.82f)];int i=0;
  foreach(var actor in Party)
  {
   var target=arrival+offsets[Math.Min(i,offsets.Length-1)];if(!CanStand(target,actor,false))target=arrival;actor.Position=target;actor.Motion=0;i++;
  }
 }
 public bool TravelTo(string destinationId)
 {
  if(Campaign!=null)return TravelCampaignTo(destinationId);
  var preview=PreviewTravel(destinationId);if(!preview.Allowed){LogSystem(preview.Reason,true);return false;}
  var sourceId=CurrentTravelDestinationId;var source=CurrentTravelDestination;var destination=ResolveTravelDestination(destinationId);var route=ResolveTravelRoute(sourceId,destinationId)!;
  StopAll();StoreCurrentTravelArea();CampaignMinutes=Math.Max(0,CampaignMinutes+route.Minutes);TravelCount++;LoadTravelArea(destinationId);var state=TravelState(destinationId);state.Discovered=true;state.Unlocked=true;state.Visits++;
  UnlockFromArrival(destinationId);
  LastTravelEventId="";if(route.EncounterChancePercent>0&&Roll(100)<=route.EncounterChancePercent)ResolveTravelEvent(route.EventId);
  foreach(var member in Party)member.Statistics.DistanceTravelledFeet+=route.Minutes*35f;
  Message(MessageCategory.Travel,$"The party travels from {source.Name} to {destination.Name}.",$"Travel time: {route.Minutes} minutes. Arrival: {CampaignTimeText}. {(LastTravelEventId.Length>0?$"Event: {LookupTravelEvent(LastTravelEventId)?.Name}.":"No travel event occurred.")}");
  RecordTravelArrival(destinationId);SyncJournalProgress();QueueTravelCompanionConversations(destinationId);
  return true;
 }
 void UnlockFromArrival(string id)
 {
  if(id=="saint-orlan")
  {
   if(UnlockTravelDestination("briarwood-road"))Message(MessageCategory.Travel,"Briarwood Road is added to the world map.");
   DiscoverTravelDestination("ashen-ford",false);
  }
  else if(id=="briarwood-road")
  {
   if(UnlockTravelDestination("ashen-ford"))Message(MessageCategory.Travel,"The safe approach to Ashen Ford is now known.");
  }
  else if(id=="ashen-ford")
  {
   if(UnlockTravelDestination("old-watchtower"))Message(MessageCategory.Travel,"The Old Watchtower trail is added to the world map.");
  }
 }
 public bool ResolveTravelEvent(string eventId)
 {
  var evt=LookupTravelEvent(eventId);if(evt==null)return false;LastTravelEventId=evt.Id;Gold=Math.Max(0,Gold+evt.GoldDelta);Rations=Math.Max(0,Rations+evt.RationsDelta);Message(MessageCategory.Travel,$"Travel event — {evt.Name}: {evt.Text}");RecordTravelEvent(evt);return true;
 }
 public void NormalizeTravelState()
 {
  if(Campaign!=null){if(TravelDefinitions.All(a=>a.Id!=CurrentTravelDestinationId))throw new InvalidDataException("Unknown campaign area.");return;}
  if(string.IsNullOrWhiteSpace(CurrentTravelDestinationId)||!TravelDefinitions.Any(d=>d.Id==CurrentTravelDestinationId))CurrentTravelDestinationId="saint-orlans-crypt";
  TravelDestinations??=[];StoredTravelAreas??=[];
  var stateById=TravelDestinations.Where(s=>TravelDefinitions.Any(d=>d.Id==s.Id)).GroupBy(s=>s.Id,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.First(),StringComparer.Ordinal);
  TravelDestinations=[];foreach(var def in TravelDefinitions)
  {
   if(!stateById.TryGetValue(def.Id,out var state))state=new(){Id=def.Id,Discovered=def.InitiallyDiscovered,Unlocked=def.InitiallyUnlocked};
   if(def.Id==CurrentTravelDestinationId){state.Discovered=true;state.Unlocked=true;state.Visits=Math.Max(1,state.Visits);}TravelDestinations.Add(state);
  }
  StoredTravelAreas=StoredTravelAreas.Where(a=>a!=null&&a.DestinationId!=CurrentTravelDestinationId&&TravelDefinitions.Any(d=>d.Id==a.DestinationId)).GroupBy(a=>a.DestinationId,StringComparer.Ordinal).Select(g=>g.First()).ToList();
  CampaignMinutes=Math.Max(0,CampaignMinutes);TravelCount=Math.Max(0,TravelCount);LastTravelEventId??="";
 }
 public void ValidateTravelState()
 {
  if(!TravelDefinitions.Any(d=>d.Id==CurrentTravelDestinationId))throw new InvalidDataException("Invalid current travel destination.");
  if(TravelDestinations.Select(s=>s.Id).Distinct(StringComparer.Ordinal).Count()!=TravelDestinations.Count)throw new InvalidDataException("Duplicate travel destination state.");
  var current=TravelState(CurrentTravelDestinationId);if(!current.Discovered||!current.Unlocked)throw new InvalidDataException("Current destination must be discovered and unlocked.");
  if(CampaignMinutes<0||TravelCount<0)throw new InvalidDataException("Invalid world travel clock/state.");
  if(StoredTravelAreas.Any(a=>a.DestinationId==CurrentTravelDestinationId)||StoredTravelAreas.Select(a=>a.DestinationId).Distinct(StringComparer.Ordinal).Count()!=StoredTravelAreas.Count)throw new InvalidDataException("Invalid stored travel-area state.");
 }

 static WorldAreaSnapshot CreateTravelArea(string id)
 {
  var floors=new HashSet<Cell>();void Room(int x,int y,int w,int h){for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)floors.Add(new(xx,yy));}
  var features=new List<Feature>();WorldPoint arrival=TravelCatalog.Destination(id).Arrival;
  switch(id)
  {
   case "saint-orlan":
    Room(5,9,34,15);Room(18,6,10,20);features=[new(){Kind="travel",Name="West Pilgrim Gate",Pos=new(6,16)},new(){Kind="travel",Name="North Road Gate",Pos=new(37,16)},new(){Kind="npc",Name="Sister Maelin",Pos=new(18,12),DialogueId="sister-maelin",DialoguePortrait=3},new(){Kind="service",Name="Shrine of the Last Dawn",Pos=new(22,11),ServiceId="saint-orlan-shrine"},new(){Kind="relic-return",Name="Ember Hearth",Pos=new(25,16)},new(){Kind="service",Name="The Pilgrim's Lantern",Pos=new(12,20),ServiceId="saint-orlan-inn"},new(){Kind="service",Name="Saint Orlan Market",Pos=new(31,13),StoreId="saint-orlan-market"}];break;
   case "briarwood-road":
    Room(5,13,35,6);Room(14,9,7,14);Room(29,10,6,12);features=[new(){Kind="travel",Name="Orlan Road Marker",Pos=new(6,15)},new(){Kind="location",Name="Standing Stones",Pos=new(18,10)},new(){Kind="travel",Name="Fordward Trail",Pos=new(38,16)}];break;
   case "ashen-ford":
    Room(6,17,34,7);Room(17,12,8,15);Room(31,14,6,13);features=[new(){Kind="travel",Name="Ford Road Cairn",Pos=new(7,20)},new(){Kind="location",Name="Ashen Ford Crossing",Pos=new(22,20)},new(){Kind="travel",Name="Watchtower Track",Pos=new(38,20)}];break;
   case "old-watchtower":
    Room(8,18,28,8);Room(20,9,8,17);features=[new(){Kind="travel",Name="Watchtower Trail",Pos=new(10,23)},new(){Kind="location",Name="Old Watchtower",Pos=new(24,11)},new(){Kind="service",Name="Abandoned Watchfire",Pos=new(24,21)}];break;
   default:throw new InvalidDataException($"No generated travel area exists for '{id}'.");
  }
  var explored=floors.Where(c=>c.Center.Distance(arrival)<=8).ToList();return new(){DestinationId=id,Floors=floors.ToList(),Explored=explored,Features=features,NextMapNoteNumber=1};
 }
}
