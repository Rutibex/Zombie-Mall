using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MapMarkerKind { Location, Quest, Exit, Service }

/// <summary>
/// Authored semantic marker for the Area Map. Marker visibility is always gated
/// by the same explored/discovery authority used by the world renderer so the map
/// cannot reveal hidden geometry, secret doors, or undetected traps.
/// </summary>
public sealed record MapMarkerDefinition
{
 public string Id {get;init;}="";
 public string Label {get;init;}="";
 public string Description {get;init;}="";
 public WorldPoint Position {get;init;}
 public MapMarkerKind Kind {get;init;}=MapMarkerKind.Location;
 public string FeatureName {get;init;}="";
 public string QuestId {get;init;}="";
}

/// <summary>Persistent player-authored note anchored to explored world space.</summary>
public sealed class MapNote
{
 public string Id {get;set;}="";
 public string Text {get;set;}="";
 public WorldPoint Position {get;set;}
}

public static class MapMarkerCatalog
{
 static readonly MapMarkerDefinition[] crypt=
 [
  new(){Id="crypt-exit",Label="Stairs to Daylight",Description="The known route back to the surface.",Position=new(3.5f,16.5f),Kind=MapMarkerKind.Exit,FeatureName="Stairs to daylight"},
  new(){Id="warden-hall",Label="Warden's Hall",Description="A guarded chamber near the old warden's coffer.",Position=new(19.5f,3.5f),Kind=MapMarkerKind.Location,FeatureName="Warden's coffer"},
  new(){Id="last-dawn-shrine",Label="Shrine of the Last Dawn",Description="An old sanctuary preserved inside the crypt.",Position=new(25.5f,3.5f),Kind=MapMarkerKind.Service,FeatureName="Shrine of the last dawn"},
  new(){Id="acolyte-reliquary",Label="Acolyte Reliquary",Description="A trapped reliquary chamber on the eastern side of the crypt.",Position=new(41.5f,9.5f),Kind=MapMarkerKind.Location,FeatureName="Acolyte's reliquary"},
  new(){Id="ember-heart-objective",Label="The Ember Heart",Description="Recover the stolen Ember Heart from the Ashen Prior.",Position=new(41.5f,27.5f),Kind=MapMarkerKind.Quest,FeatureName="The Ember Heart",QuestId="recover-ember-heart"}
 ];
 static readonly MapMarkerDefinition[] orlan=
 [
  new(){Id="orlan-gate",Label="West Pilgrim Gate",Description="The road back toward the crypt district.",Position=new(6.5f,16.5f),Kind=MapMarkerKind.Exit,FeatureName="West Pilgrim Gate"},
  new(){Id="orlan-north-gate",Label="North Road Gate",Description="The road leading toward Briarwood.",Position=new(37.5f,16.5f),Kind=MapMarkerKind.Exit,FeatureName="North Road Gate"},
  new(){Id="orlan-shrine",Label="Shrine of the Last Dawn",Description="Temple healing, identification, and resurrection services for travelers.",Position=new(22.5f,11.5f),Kind=MapMarkerKind.Service,FeatureName="Shrine of the Last Dawn"},
  new(){Id="orlan-hearth",Label="Ember Hearth",Description="Return the Ember Heart here when it has been recovered.",Position=new(25.5f,16.5f),Kind=MapMarkerKind.Quest,FeatureName="Ember Hearth",QuestId="restore-ember-heart"},
  new(){Id="orlan-inn",Label="The Pilgrim's Lantern",Description="Inn rooms, drinks, and road rumors near the west gate.",Position=new(12.5f,20.5f),Kind=MapMarkerKind.Service,FeatureName="The Pilgrim's Lantern"},
  new(){Id="orlan-market",Label="Saint Orlan Market",Description="A small market square near the shrine road.",Position=new(31.5f,13.5f),Kind=MapMarkerKind.Service,FeatureName="Saint Orlan Market"}
 ];
 static readonly MapMarkerDefinition[] briarwood=
 [
  new(){Id="briarwood-orlan",Label="Orlan Road Marker",Description="The road back toward Saint Orlan.",Position=new(6.5f,15.5f),Kind=MapMarkerKind.Exit,FeatureName="Orlan Road Marker"},
  new(){Id="briarwood-stones",Label="Standing Stones",Description="Weathered stones beside the old forest road.",Position=new(18.5f,10.5f),Kind=MapMarkerKind.Location,FeatureName="Standing Stones"},
  new(){Id="briarwood-ford",Label="Fordward Trail",Description="The eastern trail toward Ashen Ford.",Position=new(38.5f,16.5f),Kind=MapMarkerKind.Exit,FeatureName="Fordward Trail"}
 ];
 static readonly MapMarkerDefinition[] ford=
 [
  new(){Id="ford-cairn",Label="Ford Road Cairn",Description="The western road marker.",Position=new(7.5f,20.5f),Kind=MapMarkerKind.Exit,FeatureName="Ford Road Cairn"},
  new(){Id="ford-crossing",Label="Ashen Ford Crossing",Description="The shallowest known crossing of the river.",Position=new(22.5f,20.5f),Kind=MapMarkerKind.Location,FeatureName="Ashen Ford Crossing"},
  new(){Id="ford-watchtower",Label="Watchtower Track",Description="A narrow trail climbing toward the old watchtower.",Position=new(38.5f,20.5f),Kind=MapMarkerKind.Exit,FeatureName="Watchtower Track"}
 ];
 static readonly MapMarkerDefinition[] watchtower=
 [
  new(){Id="watchtower-trail",Label="Watchtower Trail",Description="The trail back toward Ashen Ford.",Position=new(10.5f,23.5f),Kind=MapMarkerKind.Exit,FeatureName="Watchtower Trail"},
  new(){Id="watchtower-ruin",Label="Old Watchtower",Description="A ruined hill tower overlooking the eastern road.",Position=new(24.5f,11.5f),Kind=MapMarkerKind.Location,FeatureName="Old Watchtower"},
  new(){Id="watchtower-fire",Label="Abandoned Watchfire",Description="A cold firepit with signs of recent use.",Position=new(24.5f,21.5f),Kind=MapMarkerKind.Service,FeatureName="Abandoned Watchfire"}
 ];
 public static IReadOnlyList<MapMarkerDefinition> SaintOrlansCrypt=>crypt;
 public static IReadOnlyList<MapMarkerDefinition> ForArea(string areaId)=>areaId switch
 {
  "saint-orlan"=>orlan,
  "briarwood-road"=>briarwood,
  "ashen-ford"=>ford,
  "old-watchtower"=>watchtower,
  _=>crypt
 };
}


public sealed partial class World
{
 public List<MapNote> MapNotes {get;set;}=[];
 public int NextMapNoteNumber {get;set;}=1;
 [JsonIgnore] public IReadOnlyList<MapMarkerDefinition> AreaMapMarkers=>Campaign==null?MapMarkerCatalog.ForArea(CurrentTravelDestinationId):CampaignMapMarkers();

 public bool IsMapMarkerDiscovered(MapMarkerDefinition marker)
 {
  if(!float.IsFinite(marker.Position.X)||!float.IsFinite(marker.Position.Y)||!Explored.Contains(marker.Position.Cell))return false;
  if(!string.IsNullOrWhiteSpace(marker.QuestId)&&!IsQuestActive(marker.QuestId))return false;
  if(string.IsNullOrWhiteSpace(marker.FeatureName))return true;
  var feature=Features.FirstOrDefault(f=>f.Name.Equals(marker.FeatureName,StringComparison.Ordinal));
  return feature!=null&&IsFeatureDiscovered(feature);
 }
 public IEnumerable<MapMarkerDefinition> DiscoveredMapMarkers()=>AreaMapMarkers.Where(IsMapMarkerDiscovered);

 public bool CanPlaceMapNote(WorldPoint position)=>float.IsFinite(position.X)&&float.IsFinite(position.Y)&&Explored.Contains(position.Cell);
 public MapNote? AddMapNote(WorldPoint position,string text)
 {
  text=(text??"").Trim();if(!CanPlaceMapNote(position)||text.Length==0)return null;
  if(text.Length>160)text=text[..160];
  var note=new MapNote{Id=$"map-note-{Math.Max(1,NextMapNoteNumber++):D4}",Text=text,Position=position};MapNotes.Add(note);return note;
 }
 public bool UpdateMapNote(string id,string text)
 {
  var note=MapNotes.FirstOrDefault(n=>n.Id==id);if(note==null)return false;text=(text??"").Trim();if(text.Length==0)return false;if(text.Length>160)text=text[..160];note.Text=text;return true;
 }
 public bool DeleteMapNote(string id)=>MapNotes.RemoveAll(n=>n.Id==id)>0;
 public void NormalizeMapState()
 {
  MapNotes??=[];var seen=new HashSet<string>(StringComparer.Ordinal);int serial=Math.Max(1,NextMapNoteNumber);
  foreach(var note in MapNotes)
  {
   note.Text=(note.Text??"").Trim();if(note.Text.Length>160)note.Text=note.Text[..160];
   if(string.IsNullOrWhiteSpace(note.Id)||!seen.Add(note.Id)){while(!seen.Add($"map-note-{serial:D4}"))serial++;note.Id=$"map-note-{serial++:D4}";}
  }
  MapNotes=MapNotes.Where(n=>n.Text.Length>0&&float.IsFinite(n.Position.X)&&float.IsFinite(n.Position.Y)&&Explored.Contains(n.Position.Cell)).ToList();
  NextMapNoteNumber=Math.Max(serial,MapNotes.Select(n=>ParseNoteNumber(n.Id)+1).DefaultIfEmpty(1).Max());
 }
 static int ParseNoteNumber(string id)
 {
  int dash=id.LastIndexOf('-');return dash>=0&&int.TryParse(id[(dash+1)..],out var n)?Math.Max(0,n):0;
 }
 public void ValidateMapState()
 {
  if(NextMapNoteNumber<1)throw new InvalidDataException("Invalid map-note sequence.");
  if(MapNotes.Any(n=>string.IsNullOrWhiteSpace(n.Id)||string.IsNullOrWhiteSpace(n.Text)||n.Text.Length>160||!CanPlaceMapNote(n.Position)))throw new InvalidDataException("Invalid map note.");
  if(MapNotes.Select(n=>n.Id).Distinct(StringComparer.Ordinal).Count()!=MapNotes.Count)throw new InvalidDataException("Duplicate map-note id.");
 }
}
