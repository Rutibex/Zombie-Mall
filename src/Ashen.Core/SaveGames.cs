using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ashen.Core;

/// <summary>IIP44 save-slot categories. These are profile metadata, not world-state rules.</summary>
public enum SaveGameKind { Manual, Quick, Auto, Legacy }

/// <summary>Whether a save can be safely offered to the current executable.</summary>
public enum SaveCompatibility { Current, Upgradeable, UnsupportedNewer, Invalid }

public sealed class SaveGameMetadata
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public SaveGameKind Kind {get;set;}=SaveGameKind.Manual;
 public DateTime SavedAtUtc {get;set;}=DateTime.UtcNow;
 public int WorldVersion {get;set;}=World.CurrentSaveVersion;
 public int CampaignMinutes {get;set;}
 public string AreaId {get;set;}="";
 public string AreaName {get;set;}="";
 public string LeaderName {get;set;}="";
 public int PartyCount {get;set;}
 public int HighestLevel {get;set;}
 public string Reason {get;set;}="";
 public string ThumbnailFile {get;set;}="";
}

public sealed record SaveGameEntry(
 string Id,
 string WorldPath,
 string MetadataPath,
 string ThumbnailPath,
 SaveGameMetadata Metadata,
 SaveCompatibility Compatibility,
 string CompatibilityText,
 string Error="")
{
 public bool Loadable=>Compatibility is SaveCompatibility.Current or SaveCompatibility.Upgradeable;
 public bool Deletable=>Metadata.Kind!=SaveGameKind.Legacy;
 public string KindText=>Metadata.Kind switch{SaveGameKind.Manual=>"MANUAL",SaveGameKind.Quick=>"QUICK SAVE",SaveGameKind.Auto=>"AUTOSAVE",_=>"LEGACY"};
}

/// <summary>
/// IIP44 profile save authority. World serialization remains owned by World.Save/Load;
/// this class owns slot naming, sidecar metadata, compatibility discovery and rotation.
/// </summary>
public sealed class SaveGameRepository
{
 public const string QuickSlotId="quicksave";
 readonly string root;
 readonly string? legacyPath;
 static readonly JsonSerializerOptions json=new(){WriteIndented=true};
 public string RootPath=>root;

 public SaveGameRepository(string rootPath,string? legacyWorldPath=null)
 {
  root=Path.GetFullPath(rootPath);legacyPath=string.IsNullOrWhiteSpace(legacyWorldPath)?null:Path.GetFullPath(legacyWorldPath);
  Directory.CreateDirectory(root);
 }

 public static string CleanName(string? value,string fallback="Journey")
 {
  string raw=(value??"").Trim();var chars=raw.Where(c=>!char.IsControl(c)).ToArray();string clean=new(chars);
  while(clean.Contains("  ",StringComparison.Ordinal))clean=clean.Replace("  "," ",StringComparison.Ordinal);
  if(clean.Length>48)clean=clean[..48].Trim();return clean.Length==0?fallback:clean;
 }
 public static string CampaignTimeText(int minutes)
 {
  minutes=Math.Max(0,minutes);int day=1+minutes/1440;int tod=minutes%1440;return $"Day {day}, {tod/60:00}:{tod%60:00}";
 }
 public static SaveCompatibility CompatibilityFor(int version)=>version switch
 {
  < World.MinimumSaveVersion=>SaveCompatibility.Invalid,
  > World.CurrentSaveVersion=>SaveCompatibility.UnsupportedNewer,
  World.CurrentSaveVersion=>SaveCompatibility.Current,
  _=>SaveCompatibility.Upgradeable
 };
 static string CompatibilityLabel(SaveCompatibility status,int version)=>status switch
 {
  SaveCompatibility.Current=>$"Compatible · save v{version}",
  SaveCompatibility.Upgradeable=>$"Compatible · upgrades v{version} → v{World.CurrentSaveVersion}",
  SaveCompatibility.UnsupportedNewer=>$"Newer save v{version} · requires a newer game build",
  _=>"Invalid or corrupt save"
 };
 string WorldPath(string id)=>Path.Combine(root,id+".json");
 string MetadataPath(string id)=>Path.Combine(root,id+".meta.json");
 public string ThumbnailPath(string id)=>Path.Combine(root,id+".png");

 static string NewManualId()=>"manual-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N")[..6];

 SaveGameMetadata MetadataFromWorld(World world,string id,string name,SaveGameKind kind,string reason="")=>new()
 {
  Id=id,Name=CleanName(name),Kind=kind,SavedAtUtc=DateTime.UtcNow,WorldVersion=world.Version,
  CampaignMinutes=world.CampaignMinutes,AreaId=world.CurrentTravelDestinationId,AreaName=world.CurrentAreaName,
  LeaderName=world.PartyLeader.Name,PartyCount=world.PartyCount,HighestLevel=world.Party.Select(a=>a.TotalLevel).DefaultIfEmpty(1).Max(),
  Reason=reason??"",ThumbnailFile=id+".png"
 };
 void WriteMetadata(string path,SaveGameMetadata metadata)
 {
  string temp=path+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(metadata,json));File.Move(temp,path,true);
 }
 SaveGameEntry SaveSlot(World world,string id,string name,SaveGameKind kind,string reason="")
 {
  Directory.CreateDirectory(root);string worldPath=WorldPath(id),metadataPath=MetadataPath(id);world.Save(worldPath);
  var metadata=MetadataFromWorld(world,id,name,kind,reason);WriteMetadata(metadataPath,metadata);
  return Describe(worldPath,metadataPath,metadata,kind);
 }
 public SaveGameEntry SaveManual(World world,string name,string? overwriteId=null)
 {
  string id=NewManualId();
  if(!string.IsNullOrWhiteSpace(overwriteId)&&Find(overwriteId) is {Metadata.Kind:SaveGameKind.Manual,Deletable:true} existing)id=existing.Id;
  return SaveSlot(world,id,CleanName(name,"Manual Save"),SaveGameKind.Manual,"Manual save");
 }
 public SaveGameEntry SaveQuick(World world)=>SaveSlot(world,QuickSlotId,"Quick Save",SaveGameKind.Quick,"Quick save");
 public SaveGameEntry SaveAuto(World world,string reason,int slots=3)
 {
  slots=Math.Clamp(slots,1,5);
  // Shrinking the rotation count also retires now-out-of-range automatic slots.
  foreach(var stale in List().Where(e=>e.Metadata.Kind==SaveGameKind.Auto&&e.Deletable&&int.TryParse(e.Id.Replace("autosave-","",StringComparison.Ordinal),out int n)&&n>slots).ToList())Delete(stale);
  var existing=List().Where(e=>e.Metadata.Kind==SaveGameKind.Auto&&e.Deletable).ToList();
  string id;
  var unused=Enumerable.Range(1,slots).Select(i=>$"autosave-{i}").FirstOrDefault(candidate=>existing.All(e=>e.Id!=candidate));
  if(unused!=null)id=unused;else id=existing.Where(e=>Enumerable.Range(1,slots).Any(i=>e.Id==$"autosave-{i}")).OrderBy(e=>e.Metadata.SavedAtUtc).FirstOrDefault()?.Id??"autosave-1";
  return SaveSlot(world,id,"Autosave — "+CleanName(reason,"Checkpoint"),SaveGameKind.Auto,reason);
 }

 static SaveGameKind InferKind(string id)=>id==QuickSlotId?SaveGameKind.Quick:id.StartsWith("autosave-",StringComparison.Ordinal)?SaveGameKind.Auto:id=="legacy-journey"?SaveGameKind.Legacy:SaveGameKind.Manual;
 SaveGameEntry Describe(string worldPath,string metadataPath,SaveGameMetadata? supplied=null,SaveGameKind? forcedKind=null)
 {
  string id=Path.GetFileNameWithoutExtension(worldPath);SaveGameMetadata metadata=supplied??new(){Id=id,Name=id,Kind=forcedKind??InferKind(id),SavedAtUtc=File.GetLastWriteTimeUtc(worldPath)};
  int version=0;string error="";
  try
  {
   using var doc=JsonDocument.Parse(File.ReadAllText(worldPath));var rootElement=doc.RootElement;
   version=rootElement.TryGetProperty("Version",out var v)&&v.TryGetInt32(out int parsed)?parsed:0;
   if(rootElement.ValueKind!=JsonValueKind.Object||!rootElement.TryGetProperty("Actors",out var actors)||actors.ValueKind!=JsonValueKind.Array)throw new InvalidDataException("Save has no actor state.");
   if(supplied==null)
   {
    metadata.WorldVersion=version;
    if(rootElement.TryGetProperty("CampaignMinutes",out var minutes)&&minutes.TryGetInt32(out int campaign))metadata.CampaignMinutes=Math.Max(0,campaign);
    if(rootElement.TryGetProperty("CurrentTravelDestinationId",out var area)&&area.ValueKind==JsonValueKind.String)metadata.AreaId=area.GetString()??"";
    if(string.IsNullOrWhiteSpace(metadata.AreaName)&&!string.IsNullOrWhiteSpace(metadata.AreaId))
    {try{metadata.AreaName=TravelCatalog.Destination(metadata.AreaId).Name;}catch{metadata.AreaName=metadata.AreaId;}}
    metadata.PartyCount=actors.EnumerateArray().Count(a=>a.TryGetProperty("Enemy",out var enemy)&&enemy.ValueKind==JsonValueKind.False);
   }
  }
  catch(Exception ex){error=ex.Message;version=0;}
  if(version>0)metadata.WorldVersion=version;
  metadata.Id=id;metadata.Name=CleanName(metadata.Name,id);metadata.ThumbnailFile=id+".png";
  if(metadata.SavedAtUtc==default)metadata.SavedAtUtc=File.GetLastWriteTimeUtc(worldPath);
  var compatibility=error.Length>0?SaveCompatibility.Invalid:CompatibilityFor(metadata.WorldVersion);
  return new(id,worldPath,metadataPath,ThumbnailPath(id),metadata,compatibility,CompatibilityLabel(compatibility,metadata.WorldVersion),error);
 }
 SaveGameMetadata? ReadMetadata(string path)
 {
  try{return File.Exists(path)?JsonSerializer.Deserialize<SaveGameMetadata>(File.ReadAllText(path)):null;}catch{return null;}
 }
 public IReadOnlyList<SaveGameEntry> List()
 {
  Directory.CreateDirectory(root);var entries=new List<SaveGameEntry>();
  foreach(var worldPath in Directory.GetFiles(root,"*.json").Where(p=>!p.EndsWith(".meta.json",StringComparison.OrdinalIgnoreCase)))
  {
   string id=Path.GetFileNameWithoutExtension(worldPath),metaPath=MetadataPath(id);var metadata=ReadMetadata(metaPath);
   entries.Add(Describe(worldPath,metaPath,metadata));
  }
  if(legacyPath!=null&&File.Exists(legacyPath)&&!Path.GetDirectoryName(legacyPath)!.Equals(root,StringComparison.OrdinalIgnoreCase))
  {
   var legacy=new SaveGameMetadata{Id="legacy-journey",Name="Legacy Journey",Kind=SaveGameKind.Legacy,SavedAtUtc=File.GetLastWriteTimeUtc(legacyPath),ThumbnailFile=""};
   entries.Add(Describe(legacyPath,"",legacy,SaveGameKind.Legacy));
  }
  return entries.OrderByDescending(e=>e.Metadata.SavedAtUtc).ThenBy(e=>e.Metadata.Name,StringComparer.OrdinalIgnoreCase).ToList();
 }
 public SaveGameEntry? Find(string id)=>List().FirstOrDefault(e=>e.Id==id);
 public SaveGameEntry? LatestLoadable()=>List().Where(e=>e.Loadable).OrderByDescending(e=>e.Metadata.SavedAtUtc).FirstOrDefault();
 public SaveGameEntry? QuickSave()=>Find(QuickSlotId) is {Loadable:true} quick?quick:null;
 public World Load(SaveGameEntry entry)
 {
  if(!entry.Loadable)throw new InvalidDataException(entry.CompatibilityText);return World.Load(entry.WorldPath);
 }
 public bool Delete(SaveGameEntry entry)
 {
  if(!entry.Deletable)return false;bool changed=false;
  foreach(var path in new[]{entry.WorldPath,entry.MetadataPath,entry.ThumbnailPath})try{if(!string.IsNullOrWhiteSpace(path)&&File.Exists(path)){File.Delete(path);changed=true;}}catch{return false;}
  return changed;
 }
}
