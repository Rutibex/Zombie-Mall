using System.Text.Json;
using System.Text.Json.Serialization;
namespace Ashen.Core;

// Authored definitions are cloned into a new world's save snapshot. They never mutate global catalogues.
public sealed class CampaignDefinition
{
 public CampaignFinale? Finale {get;set;}
 public CampaignLowerMall? LowerMall {get;set;}
 public CampaignSocial? Social {get;set;}
 public CampaignStory? Story {get;set;}
 public CampaignWorkshop? Workshop {get;set;}
 public CampaignExpedition? Expedition {get;set;}
 public int SchemaVersion {get;set;}=1;
 public bool EquipmentCatalogue {get;set;}
 public bool SurvivorClasses {get;set;}
 public bool TacticalCombat {get;set;}
 public CampaignTension? Tension {get;set;}
 public CampaignInfrastructure? Infrastructure {get;set;}
 public CampaignShelter? Shelter {get;set;}
 public bool DailySurvival {get;set;}
 public bool NoiseAwareness {get;set;}
 public List<CampaignHorde> Hordes {get;set;}=[];
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public string Description {get;set;}="";
 public string StartAreaId {get;set;}="";
 public int StartMinute {get;set;}=480;
 public List<CampaignArea> Areas {get;set;}=[];
 public List<CampaignConnection> Connections {get;set;}=[];
 public List<CampaignItem> Items {get;set;}=[];
 public List<CampaignActor> Actors {get;set;}=[];
 public List<CampaignEncounter> Encounters {get;set;}=[];
 public List<DialogueDefinition> Dialogues {get;set;}=[];
 public List<CampaignObjective> Objectives {get;set;}=[];
 public List<CampaignEvent> Events {get;set;}=[];
}
public sealed class SourceReference
{
 public string AuditId {get;set;}="";
 public int PageStart {get;set;}
 public int PageEnd {get;set;}
 public string Note {get;set;}="";
}
public sealed class CampaignArea
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public int MapX {get;set;}=640;
 public int MapY {get;set;}=270;
 public List<CampaignZone> Zones {get;set;}=[];
 public List<CampaignProp> Props {get;set;}=[];
 public List<CampaignEntryPoint> EntryPoints {get;set;}=[];
 public List<Cell> Floors {get;set;}=[];
 public List<CampaignPlacement> Actors {get;set;}=[];
 public List<CampaignFeature> Features {get;set;}=[];
}
public sealed class CampaignEntryPoint
{
 public string Id {get;set;}="";
 public Cell Position {get;set;}
}
public sealed class CampaignConnection
{
 public string Id {get;set;}="";
 public string FromAreaId {get;set;}="";
 public string ExitFeatureId {get;set;}="";
 public string ToAreaId {get;set;}="";
 public string EntryPointId {get;set;}="";
 public int Minutes {get;set;}=1;
 public string RequiredFlag {get;set;}="";
}
public sealed class CampaignPlacement
{
 public string ActorId {get;set;}="";
 public Cell Position {get;set;}
}
public sealed class CampaignFeature
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public bool Scavengable {get;set;}
 public bool SecuringSite {get;set;}
 public string SecuringFlag {get;set;}="";
 public string Kind {get;set;}="container";
 public Cell Position {get;set;}
 public string DialogueId {get;set;}="";
 public string EventId {get;set;}="";
 public List<CampaignStack> Contents {get;set;}=[];
}
public sealed class CampaignStack
{
 public int Wear {get;set;}
 public string ItemId {get;set;}="";
 public int Quantity {get;set;}=1;
}
public sealed class CampaignItem
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public string Description {get;set;}="";
 public ItemKind Kind {get;set;}=ItemKind.Misc;
 public string Slot {get;set;}="";
 public float Weight {get;set;}=1;
 public int MaxStack {get;set;}=1;
 public EquipmentProfile? Equipment {get;set;}
 public int TreatmentUses {get;set;}=1;
 public string SurvivalUse {get;set;}="";
 public int ResourceSv {get;set;}
 public int Value {get;set;}
 public int ArmorBonus {get;set;}
 public int DamageDie {get;set;}=6;
 public int DamageDice {get;set;}=1;
 public int RangeFeet {get;set;}=5;
 public int MagazineSize {get;set;}
 public string AmmunitionType {get;set;}="";
 public string AmmoType {get;set;}="";
 public Gear ToGear()=>new(Id,Name,Slot,0,Description,Weight,MaxStack,MaxCharges:TreatmentUses>1?TreatmentUses:Equipment?.Charges??0,Value:Value,Kind:Kind,DamageModifier:Equipment?.DamageBonus??0,TwoHanded:Equipment?.TwoHanded??false,Equipment:Equipment,ArmorModifier:ArmorBonus,Lore:Source.Note,WeaponDamageDieSize:Kind==ItemKind.Weapon||Equipment?.Category is "explosive" or "ranged"?DamageDie:null,AmmunitionType:AmmunitionType,AmmoType:AmmoType,MagazineSize:MagazineSize,WeaponRangeFeet:RangeFeet,WeaponDamageDice:DamageDice);
}
public sealed class CampaignActor
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public string Archetype {get;set;}="";
 public bool Recruitable {get;set;}
 public int AwarenessFeet {get;set;}=35;
 public string Biography {get;set;}="";
 public string Personality {get;set;}="";
 public List<SkillTraining> Skills {get;set;}=[];
 public DamageType DamageType {get;set;}=DamageType.Bludgeoning;
 public bool Enemy {get;set;}
 public int Portrait {get;set;}
 public int HitPoints {get;set;}=20;
 public int ArmorClass {get;set;}=10;
 public int SpeedFeet {get;set;}=30;
 public int AttackBonus {get;set;}=2;
 public int DamageBonus {get;set;}
 public int DamageDie {get;set;}=6;
 public string AttackName {get;set;}="Strike";
 public string Role {get;set;}="Fixture survivor";
 public AbilityScores Abilities {get;set;}=new();
 public string WeaponId {get;set;}="";
 public string ArmorId {get;set;}="";
 public List<CampaignStack> Inventory {get;set;}=[];
}
public sealed class CampaignEncounter
{
 public string Id {get;set;}="";
 public string AreaId {get;set;}="";
 public int Group {get;set;}
 public int RewardXp {get;set;}
 public int RewardCurrency {get;set;}
 public List<string> ActorIds {get;set;}=[];
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CampaignObjectiveTrigger { FeatureUsed, ItemOwned, EncounterCleared, FlagSet, AreaVisited, ResourcesSecured }
public sealed class CampaignObjective
{
 public string Id {get;set;}="";
 public string Text {get;set;}="";
 public CampaignObjectiveTrigger Trigger {get;set;}
 public string TargetId {get;set;}="";
 public int Quantity {get;set;}=1;
}
public sealed class CampaignEvent
{
 public string Id {get;set;}="";
 public bool Once {get;set;}=true;
 public List<CampaignEffect> Effects {get;set;}=[];
}
// Only registered handlers can be authored. JSON never contains code or arbitrary method names.
public sealed class CampaignEffect
{
 public string Handler {get;set;}="";
 public string Text {get;set;}="";
 public string Flag {get;set;}="";
 public string ActorId {get;set;}="";
}
public sealed record CampaignValidationIssue(string Code,string Path,string Message);
public sealed class CampaignValidationException(IReadOnlyList<CampaignValidationIssue> issues)
 :IOException(string.Join(Environment.NewLine,issues.Select(i=>$"{i.Code} {i.Path}: {i.Message}")))
{
 public IReadOnlyList<CampaignValidationIssue> Issues {get;}=issues;
}
public static class CampaignLoader
{
 public const string TravelFixtureFile="zombie-mall.travel.json";
 public static string BundledTravelPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",TravelFixtureFile);
 public const string SurvivorFile="zombie-mall.survivors.json";
 public static string BundledMallPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",MallFile);
 public const string MallFile="zombie-mall.southwest.json";
 public static string BundledSurvivorPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",SurvivorFile);
 public const string ScavengingFile="zombie-mall.scavenging.json";
 public static string BundledScavengingPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",ScavengingFile);
 public const string ExpeditionFile="zombie-mall.expedition.json";
 public const string FirearmsFile="zombie-mall.firearms.json";
 public static string BundledExpeditionPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",ExpeditionFile);
 public static string BundledFirearmsPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",FirearmsFile);
 public static string BundledTacticalPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",TacticalFile);
 public static string BundledAwarenessPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",NoiseFile);
 public const string StoryFile="zombie-mall.quests.json";
 public static string BundledWorkshopPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",WorkshopFile);
 public const string WorkshopFile="zombie-mall.workshop.json";
 public static string BundledEquipmentPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",EquipmentFile);
 public const string EquipmentFile="zombie-mall.equipment.json";
 public static string BundledClassesPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",ClassesFile);
 public const string ClassesFile="zombie-mall.classes.json";
 public static string BundledTensionPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",TensionFile);
 public const string TensionFile="zombie-mall.tension.json";
 public static string BundledInfrastructurePath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",InfrastructureFile);
 public const string InfrastructureFile="zombie-mall.infrastructure.json";
 public static string BundledPopulationPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",PopulationFile);
 public const string PopulationFile="zombie-mall.population.json";
 public static string BundledSurvivalPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",SurvivalFile);
 public const string SurvivalFile="zombie-mall.survival.json";
 public static string BundledHordePath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",HordeFile);
 public const string HordeFile="zombie-mall.hordes.json";
 public const string NoiseFile="zombie-mall.awareness.json";
 public const string TacticalFile="zombie-mall.tactical.json";
 public static string BundledCampaignPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",LowerMallFile);
 public const string LowerMallFile="zombie-mall.finale.json";
 public static string BundledUpper54Path=>Path.Combine(AppContext.BaseDirectory,"Campaigns","zombie-mall.upper54.json");
 public static string BundledUpper36Path=>Path.Combine(AppContext.BaseDirectory,"Campaigns","zombie-mall.upper36.json");
 public static string BundledUpper18Path=>Path.Combine(AppContext.BaseDirectory,"Campaigns","zombie-mall.upper18.json");
 public static string BundledLower54Path=>Path.Combine(AppContext.BaseDirectory,"Campaigns","zombie-mall.lower54.json");
 public static string BundledLower36Path=>Path.Combine(AppContext.BaseDirectory,"Campaigns","zombie-mall.lower36.json");
 public static string BundledLower18Path=>Path.Combine(AppContext.BaseDirectory,"Campaigns","zombie-mall.lower18.json");
 public static string BundledSocialPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",SocialFile);
 public const string SocialFile="zombie-mall.social.json";
 public static string BundledStoryPath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",StoryFile);
 public const string FixtureFile="zombie-mall.fixture.json";
 public static string BundledFixturePath=>Path.Combine(AppContext.BaseDirectory,"Campaigns",FixtureFile);
 internal static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=false,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,RespectNullableAnnotations=true,WriteIndented=true};
 public static CampaignDefinition Load(string path)
 {
  try{return Parse(File.ReadAllText(path));}
  catch(JsonException e){throw new InvalidDataException($"Invalid campaign JSON in '{path}', {e.Path}: {e.Message}",e);}
 }
 public static CampaignDefinition Parse(string json)
 {
  var result=JsonSerializer.Deserialize<CampaignDefinition>(json,Json)??throw new InvalidDataException("Campaign document is null.");
  ValidateOrThrow(result);return result;
 }
 public static CampaignDefinition Clone(CampaignDefinition source)=>Parse(JsonSerializer.Serialize(source,Json));
 public static void ValidateOrThrow(CampaignDefinition source)
 {
  var issues=Validate(source);if(issues.Count>0)throw new CampaignValidationException(issues);
 }
 public static IReadOnlyList<CampaignValidationIssue> Validate(CampaignDefinition c)
 {
  List<CampaignValidationIssue> e=[];
  void Error(string code,string path,string text)=>e.Add(new(code,path,text));
  bool Present<T>(List<T>? list,string path){if(list==null||list.Any(x=>x is null)){Error("collection",path,"Collection and entries must not be null.");return false;}return true;}
  if(!Present(c.Areas,"areas")|!Present(c.Items,"items")|!Present(c.Actors,"actors")|!Present(c.Encounters,"encounters")|!Present(c.Dialogues,"dialogues")|!Present(c.Objectives,"objectives")|!Present(c.Events,"events"))return e;
  foreach(var a in c.Areas){Present(a.Zones,a.Id+".zones");Present(a.Props,a.Id+".props");Present(a.Floors,a.Id+".floors");Present(a.Actors,a.Id+".actors");if(Present(a.Features,a.Id+".features"))foreach(var f in a.Features)Present(f.Contents,f.Id+".contents");}
  foreach(var a in c.Actors){Present(a.Inventory,a.Id+".inventory");Present(a.Skills,a.Id+".skills");}foreach(var evt in c.Events)Present(evt.Effects,evt.Id+".effects");
  if(e.Count>0)return e;
  if(c.SchemaVersion is not (1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 18 or 19 or 20 or 21 or 22 or 23 or 24 or 25 or 26 or 27 or 28 or 29 or 30))Error("schema","schemaVersion","Campaign schema 1–30 is required.");
  if(!Present(c.Connections,"connections"))return e;
  foreach(var area in c.Areas)if(!Present(area.EntryPoints,area.Id+".entryPoints"))return e;
  if(!Present(c.Hordes,"hordes"))return e;
  if(c.Hordes.Count>4||c.Hordes.Count>0&&(c.SchemaVersion<10||!c.NoiseAwareness))Error("horde","hordes","At most four hordes require schema 10 and local awareness.");
  if(c.SchemaVersion<9&&c.NoiseAwareness)Error("schema","noiseAwareness","Local awareness requires schema 9.");
  if(c.SchemaVersion<8&&(c.TacticalCombat||c.Areas.Any(a=>a.Props.Any(p=>p.CoverBonus!=0||!p.BlocksSight))))Error("schema","tacticalCombat","Tactical combat and cover require schema 8.");
  if(c.SchemaVersion<6&&(c.Expedition!=null||c.Areas.Any(a=>a.Features.Any(f=>f.Kind=="debrief"))))Error("schema","expedition","Expedition definitions require schema 6.");
  if(c.SchemaVersion<5&&(c.Items.Any(i=>i.ResourceSv!=0)||c.Areas.Any(a=>a.Features.Any(f=>f.Scavengable||f.SecuringSite||f.SecuringFlag!=""))))Error("schema","resources","Resource metadata requires schema 5.");
  if(c.SchemaVersion<4&&(c.Actors.Any(a=>a.Archetype!=""||a.Recruitable||a.Skills.Count>0||a.Biography!=""||a.Personality!=""||a.AwarenessFeet!=35||a.DamageType!=DamageType.Bludgeoning)||c.Events.Any(e=>e.Effects.Any(x=>x.ActorId!=""||x.Handler=="recruit"))))Error("schema","actors","Survivor profiles and recruitment require schema 4.");
  if(c.SchemaVersion<3&&c.Areas.Any(a=>a.Zones.Count>0||a.Props.Count>0))Error("schema","areas","Zones and props require campaign schema 3.");
  if(c.SchemaVersion==1&&(c.Connections.Count>0||c.Areas.Any(a=>a.EntryPoints.Count>0)))Error("schema","connections","Travel definitions require campaign schema 2.");
  if(string.IsNullOrWhiteSpace(c.Id)||string.IsNullOrWhiteSpace(c.Name))Error("required","campaign","Id and name are required.");
  var allIds=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  void Id(string id,string path){if(string.IsNullOrWhiteSpace(id)||!System.Text.RegularExpressions.Regex.IsMatch(id,"^[a-z0-9][a-z0-9._-]*$"))Error("id",path,"Use a stable lower-case ASCII ID.");else if(!allIds.Add(id))Error("duplicate",path,$"Duplicate id '{id}'.");}
  void Source(SourceReference? s,string path){if(s==null||string.IsNullOrWhiteSpace(s.AuditId)||s.PageStart<1||s.PageEnd<s.PageStart||s.PageEnd>513)Error("source",path,"An audit ID and PDF page range 1–513 are required.");}
  foreach(var a in c.Areas)Id(a.Id,"areas.id");foreach(var i in c.Items)Id(i.Id,"items.id");foreach(var a in c.Actors)Id(a.Id,"actors.id");foreach(var x in c.Encounters)Id(x.Id,"encounters.id");foreach(var d in c.Dialogues)Id(d.Id,"dialogues.id");foreach(var o in c.Objectives)Id(o.Id,"objectives.id");foreach(var x in c.Events)Id(x.Id,"events.id");
  bool Ref(IEnumerable<string> ids,string id,string path){if(!ids.Contains(id,StringComparer.Ordinal)){Error("reference",path,$"Unknown reference '{id}'.");return false;}return true;}
  Ref(c.Areas.Select(a=>a.Id),c.StartAreaId,"startAreaId");if(c.StartMinute<0)Error("range","startMinute","Start minute must not be negative.");
  var features=c.Areas.SelectMany(a=>a.Features??[]).Where(f=>f!=null).ToList();
  void Stack(CampaignStack? s,string path){if(s==null){Error("stack",path,"Null stack.");return;}if(Ref(c.Items.Select(i=>i.Id),s.ItemId,path)){var item=c.Items.First(i=>i.Id==s.ItemId);if(s.Quantity<1||s.Quantity>item.MaxStack)Error("range",path,"Quantity outside item stack limit.");}}
  var placements=new HashSet<string>();
  foreach(var a in c.Areas)
  {
   Source(a.Source,$"areas[{a.Id}].source");if(string.IsNullOrWhiteSpace(a.Name))Error("required",a.Id,"Area name is required.");
   if(!Present(a.Floors,a.Id+".floors")|!Present(a.Actors,a.Id+".actors")|!Present(a.Features,a.Id+".features"))continue;
   var floors=a.Floors.ToHashSet();if(floors.Count==0||floors.Count!=a.Floors.Count||floors.Any(p=>p.X<0||p.X>=World.Width||p.Y<0||p.Y>=World.Height))Error("geometry",a.Id,"Floors must be nonempty, unique and within the engine canvas.");
   var solids=new HashSet<Cell>();
   foreach(var z in a.Zones)
   {
    Id(z.Id,a.Id+".zones");Source(z.Source,z.Id+".source");
    if(string.IsNullOrWhiteSpace(z.Name)||!CampaignGeometry.Styles.Contains(z.FloorStyle)||!CampaignGeometry.ValidRect(z.X,z.Y,z.Width,z.Height)||!a.Floors.Any(z.Contains))Error("zone",z.Id,"Named zone needs a supported floor style and an in-bounds rectangle containing floor.");
   }
   foreach(var prop in a.Props)
   {
    Id(prop.Id,a.Id+".props");
    if(prop.CoverBonus is not (0 or 2 or 5)||prop.CoverBonus>0&&!prop.BlocksMovement)Error("cover",prop.Id,"Cover must be 0, 2 or 5 on solid furniture.");
    if(!CampaignGeometry.PropKinds.Contains(prop.Kind)||!CampaignGeometry.ValidRect(prop.Position.X,prop.Position.Y,prop.Width,prop.Height)){Error("prop",prop.Id,"Invalid furniture kind or bounds.");continue;}
    foreach(var cell in prop.Cells())
    {
     if(!floors.Contains(cell))Error("prop",prop.Id,"Furniture must stand on authored floor.");
     if(prop.BlocksMovement&&(!solids.Add(cell)||a.Features.Any(f=>f.Position==cell)||a.Actors.Any(p=>p.Position==cell)||a.EntryPoints.Any(p=>p.Position==cell)))Error("prop",prop.Id,"Solid furniture overlaps another solid prop, feature, actor or entry.");
    }
   }
   var occupied=new HashSet<Cell>();
   foreach(var p in a.Actors){Ref(c.Actors.Select(x=>x.Id),p.ActorId,a.Id+".actors");if(!placements.Add(p.ActorId))Error("placement",p.ActorId,"An actor must be placed exactly once.");if(!floors.Contains(p.Position)||!occupied.Add(p.Position))Error("position",p.ActorId,"Actor position must be a distinct walkable cell.");}
   foreach(var f in a.Features)
   {
    Id(f.Id,a.Id+".features");if(!floors.Contains(f.Position)||string.IsNullOrWhiteSpace(f.Name))Error("position",f.Id,"Named feature needs a walkable position.");
    if(f.Kind is not ("container" or "door" or "npc" or "event" or "transition" or "debrief" or "horde" or "quest-board" or "encounter" or "location"))Error("handler",f.Id,"Unsupported feature handler.");
    if((f.Scavengable||f.SecuringSite||f.SecuringFlag!="")&&f.Kind!="container"||f.Scavengable&&f.SecuringSite||f.SecuringFlag!=""&&!f.SecuringSite)Error("resource",f.Id,"Resource source/site must be distinct containers; only sites can require a flag.");
    if(f.SecuringFlag!=""&&!c.Events.SelectMany(e=>e.Effects).Any(e=>e.Handler=="set-flag"&&e.Flag==f.SecuringFlag))Error("resource",f.Id,"Securing flag needs an authored event producer.");
    if(f.Kind=="npc")Ref(c.Dialogues.Select(d=>d.Id),f.DialogueId,f.Id+".dialogueId");else if(f.DialogueId!="")Error("handler",f.Id,"Only NPCs bind dialogue.");
    if(f.Kind=="event")Ref(c.Events.Select(x=>x.Id),f.EventId,f.Id+".eventId");else if(f.EventId!="")Error("handler",f.Id,"Only event features bind events.");
    if(f.Contents.Count>32)Error("capacity",f.Id,"Fixture container exceeds 32 stacks.");
    if(Present(f.Contents,f.Id+".contents")){foreach(var s in f.Contents)Stack(s,f.Id+".contents");if(f.Contents.Count>0&&f.Kind!="container")Error("handler",f.Id,"Only containers can hold authored loot.");}
    if(f.Kind=="door"&&a.Actors.Any(p=>p.Position==f.Position))Error("position",f.Id,"Closed door overlaps an actor.");
   }
  }
  foreach(var area in c.Areas)
  {
   if(area.MapX<0||area.MapX>1280||area.MapY<0||area.MapY>540)Error("range",area.Id,"Map coordinates must fit the world-map canvas.");
   foreach(var entry in area.EntryPoints)
   {
    Id(entry.Id,area.Id+".entryPoints");
    var candidates=CampaignTravelRules.ArrivalCells(area.Floors,area.Features.Where(f=>f.Kind=="door").Select(f=>f.Position).Concat(CampaignGeometry.SolidCells(area)),area.Actors.Where(p=>c.Actors.Any(a=>a.Id==p.ActorId&&a.Enemy)).Select(p=>p.Position),entry.Position);
    if(candidates.Count<c.Actors.Count(a=>!a.Enemy&&!a.Recruitable))Error("entry",entry.Id,"Entry must be walkable and have enough connected, unoccupied nearby cells for the party.");
   }
  }
  foreach(var route in c.Connections)
  {
   Id(route.Id,"connections.id");
   var from=c.Areas.FirstOrDefault(a=>a.Id==route.FromAreaId);var to=c.Areas.FirstOrDefault(a=>a.Id==route.ToAreaId);
   if(from==null||to==null){Error("reference",route.Id,"Connection areas must exist.");continue;}
   if(from==to)Error("connection",route.Id,"A connection must link different areas.");
   if(route.RequiredFlag!="")
   {
    var produced=c.Events.SelectMany(x=>x.Effects).Where(x=>x.Handler=="set-flag").Select(x=>x.Flag).Concat(c.Dialogues.Select(d=>d.CompletionFlag)).Concat(c.Dialogues.SelectMany(d=>d.Nodes??[]).Where(n=>n!=null).SelectMany(n=>n.Choices??[]).Where(ch=>ch!=null).SelectMany(ch=>ch.Consequence?.SetFlags??[]));
    if(!produced.Contains(route.RequiredFlag,StringComparer.Ordinal))Error("reference",route.Id,"Route requires a flag with no authored producer.");
   }
   if(route.Minutes<0||route.Minutes>10080)Error("range",route.Id,"Travel duration must be 0–10080 minutes.");
   if(from.Features.All(f=>f.Id!=route.ExitFeatureId||f.Kind!="transition"))Error("reference",route.Id,"Exit must name a transition feature in the source area.");
   if(to.EntryPoints.All(x=>x.Id!=route.EntryPointId))Error("reference",route.Id,"Arrival entry must belong to the destination area.");
   if(c.Connections.Count(x=>x.FromAreaId==route.FromAreaId&&x.ToAreaId==route.ToAreaId)>1||c.Connections.Count(x=>x.FromAreaId==route.FromAreaId&&x.ExitFeatureId==route.ExitFeatureId)>1)Error("connection",route.Id,"Ambiguous duplicate destination/exit connections.");
  }
  foreach(var area in c.Areas)foreach(var feature in area.Features.Where(f=>f.Kind=="transition"))if(!c.Connections.Any(r=>r.FromAreaId==area.Id&&r.ExitFeatureId==feature.Id))Error("reference",feature.Id,"Transition has no outgoing connection.");
  ShelterRules.Validate(c,Error);InfrastructureRules.Validate(c,Error);TensionRules.Validate(c,Error);EquipmentRules.Validate(c,Error);WorkshopRules.Validate(c,Error);StoryRules.Validate(c,Error);FinaleRules.Validate(c,Error);SocialRules.Validate(c,Error);LowerMallRules.Validate(c,Error);
  if(c.SurvivorClasses&&new[]{"item.works.planks","item.works.mechanical","item.works.electrical","item.works.fuel"}.Any(id=>!c.Items.Any(i=>i.Id==id&&i.Kind==ItemKind.Misc)))Error("classes","materials","Class work requires the four authored maintenance material categories.");
  if(c.SurvivorClasses&&(c.SchemaVersion<15||!c.TacticalCombat||!c.DailySurvival||c.Shelter==null||c.Infrastructure==null||c.Tension==null))Error("classes","survivorClasses","Survivor classes require schema 15, modern combat, survival and shelter.");
  if(c.DailySurvival&&(!c.Items.Any(i=>i.SurvivalUse=="food")||!c.Items.Any(i=>i.SurvivalUse=="water")||!c.Areas.Any(a=>a.Features.Any(f=>f.SecuringSite))))Error("survival","dailySurvival","Daily survival needs food, water and an authored securing site.");
  if(c.DailySurvival&&c.SchemaVersion<11)Error("schema","dailySurvival","Daily survival requires schema 11.");
  foreach(var i in c.Items)
  {
   if(i.TreatmentUses is <1 or >5||i.TreatmentUses>1&&(c.SchemaVersion<15||i.SurvivalUse!="first-aid"||i.MaxStack!=1))Error("survival",i.Id,"Multi-use kits require schema 15, first-aid use, one slot per kit and at most five uses.");
   if(i.SurvivalUse is not ("" or "food" or "water" or "bandage" or "first-aid")||i.SurvivalUse!=""&&(c.SchemaVersion<11||i.Kind!=ItemKind.Misc))Error("survival",i.Id,"Invalid survival supply; schema 11 misc items are required.");
   if(i.DamageDice<1||i.DamageDice>(c.EquipmentCatalogue?100:10)||i.RangeFeet is <5 or >500||i.RangeFeet%5!=0||i.MagazineSize<0||i.MagazineSize>(c.EquipmentCatalogue?1000:100)||i.MagazineSize>0&&(i.Kind!=ItemKind.Weapon||i.MaxStack!=1||i.AmmunitionType==""||!c.Items.Any(x=>x.Kind==ItemKind.Ammunition&&x.AmmoType==i.AmmunitionType))||i.Kind==ItemKind.Ammunition&&(i.AmmoType==""||i.Slot!="Ammo1")||i.Kind!=ItemKind.Ammunition&&i.AmmoType!=""||i.AmmunitionType!=""&&i.MagazineSize==0&&!(c.SchemaVersion>=25&&i.Kind==ItemKind.Weapon&&i.Equipment?.Family=="bow"&&i.AmmunitionType=="arrow"))Error("firearm",i.Id,"Invalid firearm or ammunition definition.");
   if(c.SchemaVersion<7&&(i.DamageDice!=1||i.RangeFeet!=5||i.MagazineSize!=0||i.AmmunitionType!=""||i.AmmoType!=""))Error("schema",i.Id,"Firearms require schema 7.");
   if(i.Kind==ItemKind.Shield&&(!c.EquipmentCatalogue||i.Slot!="OffHand"))Error("slot",i.Id,"Catalogue shields require the off-hand slot.");
   Source(i.Source,i.Id+".source");if(string.IsNullOrWhiteSpace(i.Name)||!float.IsFinite(i.Weight)||i.Weight<0||i.MaxStack is <1 or >10000||i.Value<0||i.ResourceSv<0||i.ResourceSv>(c.EquipmentCatalogue?100000:10000)||!Enum.IsDefined(i.Kind)||i.DamageDie is <2 or >100||i.ArmorBonus is <0 or >20)Error("range",i.Id,"Invalid item fields.");
   if(i.Kind==ItemKind.Weapon&&i.Slot!="Weapon"&&i.Slot!="MainHand"||i.Kind==ItemKind.Armor&&i.Slot!="Armor")Error("slot",i.Id,"Weapon/armour needs its supported equipment slot.");
   if(i.Kind is not (ItemKind.Weapon or ItemKind.Armor or ItemKind.Ammunition or ItemKind.Shield) && i.Slot!=""&&!(c.SchemaVersion>=23&&i.Kind==ItemKind.Accessory&&Enum.TryParse<EquipmentSlot>(i.Slot,out var accessorySlot)&&accessorySlot is EquipmentSlot.Boots or EquipmentSlot.Head or EquipmentSlot.Gloves or EquipmentSlot.Neck))Error("slot",i.Id,"Fixture items support only weapon and armour equipment slots.");
  }
  foreach(var a in c.Actors)
  {
   Source(a.Source,a.Id+".source");if(!placements.Contains(a.Id))Error("placement",a.Id,"Actor has no placement.");
   if(a.Abilities==null||a.Abilities.All().Any(x=>x.Score<1||x.Score>30)||string.IsNullOrWhiteSpace(a.Name)||a.HitPoints is <1 or >100000||a.ArmorClass is <0 or >40||a.SpeedFeet is <0 or >240||a.Portrait is <0 or >5||a.DamageDie is <2 or >100)Error("range",a.Id,"Invalid actor stats.");
   if(a.Archetype is not ("" or "survivor" or "shambler" or "runner")||a.Archetype=="survivor"&&a.Enemy||(a.Archetype is "shambler" or "runner")&&!a.Enemy||a.Recruitable&&(a.Enemy||a.Archetype!="survivor"))Error("archetype",a.Id,"Invalid survivor/enemy archetype or recruit allegiance.");
   if(a.AwarenessFeet is <5 or >60||!Enum.IsDefined(a.DamageType)||a.Biography.Length>2000||a.Personality.Length>1000||a.Skills.Any(s=>!Enum.IsDefined(s.Skill)||!Enum.IsDefined(s.Rank))||a.Skills.Select(s=>s.Skill).Distinct().Count()!=a.Skills.Count)Error("profile",a.Id,"Invalid awareness, damage type, biography or skill training.");
   foreach(var (id,kind) in new[]{(a.WeaponId,ItemKind.Weapon),(a.ArmorId,ItemKind.Armor)})if(id!=""&&Ref(c.Items.Select(i=>i.Id),id,a.Id+".equipment")&&c.Items.First(i=>i.Id==id).Kind!=kind)Error("slot",a.Id,"Equipment type mismatch.");
   if(a.Inventory.Count>24)Error("capacity",a.Id,"Fixture inventory exceeds 24 stacks.");
   if(Present(a.Inventory,a.Id+".inventory"))foreach(var s in a.Inventory)Stack(s,a.Id+".inventory");
  }
  var party=c.Actors.Where(a=>!a.Enemy&&!a.Recruitable).ToList();if(party.Count is <1 or >World.MaxPartySize||party.Any(a=>!c.Areas.Any(area=>area.Id==c.StartAreaId&&area.Actors.Any(p=>p.ActorId==a.Id))))Error("party","actors","Place 1–6 party actors in the start area.");
  var grouped=new HashSet<string>();
  foreach(var x in c.Encounters)
  {
   Ref(c.Areas.Select(a=>a.Id),x.AreaId,x.Id+".areaId");if(x.RewardXp<0||x.RewardCurrency<0)Error("range",x.Id,"Encounter rewards must be non-negative.");if(x.Group<0||c.Encounters.Count(y=>y.AreaId==x.AreaId&&y.Group==x.Group)>1)Error("group",x.Id,"Encounter groups must be unique per area and non-negative.");
   if(!Present(x.ActorIds,x.Id+".actors"))continue;if(x.ActorIds.Count==0&&!(c.SchemaVersion>=23&&c.LowerMall?.Sites.Any(s=>s.Level is "upper" or "roof" or "exterior"&&s.BackEncounterId==x.Id)==true))Error("encounter",x.Id,"Encounter needs enemies unless it is an authored empty upper back room.");
   foreach(var id in x.ActorIds)if(Ref(c.Actors.Select(a=>a.Id),id,x.Id)){if(!grouped.Add(id)||!c.Actors.First(a=>a.Id==id).Enemy||!c.Areas.Any(a=>a.Id==x.AreaId&&a.Actors.Any(p=>p.ActorId==id)))Error("encounter",x.Id,"Each enemy must be placed in this encounter area and belong to exactly one group.");}
  }
  foreach(var a in c.Actors.Where(a=>a.Enemy))if(!grouped.Contains(a.Id))Error("encounter",a.Id,"Enemy must belong to an encounter.");
  foreach(var d in c.Dialogues)
  {
   try{d.ValidateOrThrow();}catch(Exception ex)when(ex is InvalidDataException or NullReferenceException or ArgumentNullException or KeyNotFoundException){Error("dialogue",d.Id,ex.Message);}
   if(d.Nodes==null)continue;
   foreach(var n in d.Nodes.Where(n=>n!=null))
   {
    if(string.IsNullOrWhiteSpace(n.Id)||string.IsNullOrWhiteSpace(n.Text)||n.Choices==null||n.Choices.Length==0)Error("dialogue",d.Id,"Each named dialogue node needs text and explicit choices.");
    if(n.Choices!=null&&(n.Choices.Any(x=>x==null||string.IsNullOrWhiteSpace(x.Id)||string.IsNullOrWhiteSpace(x.Text))||n.Choices.Where(x=>x!=null).Select(x=>x.Id).Distinct().Count()!=n.Choices.Length))Error("dialogue",d.Id,"Choices need distinct IDs and text.");
    foreach(var ch in n.Choices??[])if(ch!=null&&!ch.EndsConversation&&ch.Check==null&&string.IsNullOrWhiteSpace(ch.NextNodeId))Error("dialogue",d.Id,"A choice must have an explicit destination or ending.");
   }

   foreach(var n in d.Nodes.Where(n=>n!=null))foreach(var choice in n.Choices??[])
   {
    if(choice==null)continue;
    // Phase 3 supports ordinary graph/check/flag consequences, not legacy companion scripts.
    if(choice.SpeakerActorId!=""||choice.Conditions?.Length>0||choice.Consequence?.ApprovalChanges?.Length>0||choice.Consequence?.QueueCompanionConversations?.Length>0||choice.Consequence?.SwitchPlayerSpeakerId.Length>0||n.SpeakerActorId!=""||n.Interjections?.Length>0||d.ScriptedPlayerSpeakerId!=""||d.CompanionConversation)Error("handler",d.Id,"Legacy companion scripting is not supported by campaign schema 1.");
   }
  }
  foreach(var x in c.Events){if(!Present(x.Effects,x.Id+".effects"))continue;foreach(var effect in x.Effects){if(effect.Handler is not ("log" or "set-flag" or "recruit"))Error("handler",x.Id,"Unknown effect handler: "+effect.Handler);if(effect.Handler=="set-flag"&&string.IsNullOrWhiteSpace(effect.Flag)||effect.Handler=="log"&&string.IsNullOrWhiteSpace(effect.Text))Error("required",x.Id,"Effect requires its text or flag.");}}
  long resourceUnits=0,totalSv=0;foreach(var f in features.Where(f=>f.Scavengable))foreach(var stack in f.Contents){var item=c.Items.FirstOrDefault(i=>i.Id==stack.ItemId);if(item?.ResourceSv>0){resourceUnits+=stack.Quantity;totalSv+=(long)stack.Quantity*item.ResourceSv;}}
  if(resourceUnits>100000||totalSv>10000000)Error("capacity","resources","Campaign resource provenance exceeds 100,000 units or 10,000,000 SV.");
  var recruitEffects=c.Events.SelectMany(evt=>evt.Effects.Where(effect=>effect.Handler=="recruit").Select(effect=>(evt,effect))).ToArray();
  foreach(var (evt,effect) in recruitEffects)
  {
   var recruit=c.Actors.FirstOrDefault(a=>a.Id==effect.ActorId&&a.Recruitable&&!a.Enemy);
   if(recruit==null||!evt.Once||evt.Effects.Count(x=>x.Handler=="recruit")!=1||recruitEffects.Count(x=>x.effect.ActorId==effect.ActorId)!=1)Error("recruit",evt.Id,"Recruit event must be once-only and bind one unique recruitable survivor.");
   if(recruit!=null){var home=c.Areas.FirstOrDefault(a=>a.Actors.Any(p=>p.ActorId==recruit.Id));if(home==null||!home.Features.Any(f=>f.EventId==evt.Id)&&!(c.LowerMall?.Sites.Any(s=>s.AreaId==home.Id&&s.Actions.Any(a=>a.NpcActorId==recruit.Id))??false)&&!(c.Finale!=null&&home.Id==c.Shelter?.AreaId&&recruit.Id is "actor.finale.volunteer.0" or "actor.finale.volunteer.1"))Error("recruit",evt.Id,"Recruit event must have an interaction in the recruit's authored area.");}
  }
  foreach(var a in c.Actors.Where(a=>a.Recruitable))if(!recruitEffects.Any(x=>x.effect.ActorId==a.Id))Error("recruit",a.Id,"Recruitable survivor has no recruitment event.");
  foreach(var effect in c.Events.SelectMany(e=>e.Effects).Where(e=>e.Handler!="recruit"))if(effect.ActorId!="")Error("handler",effect.ActorId,"Only recruitment effects bind an actor.");
  foreach(var o in c.Objectives)
  {
   if(string.IsNullOrWhiteSpace(o.Text)||o.Quantity<1||!Enum.IsDefined(o.Trigger))Error("range",o.Id,"Invalid objective fields.");
   switch(o.Trigger){case CampaignObjectiveTrigger.ResourcesSecured:if(c.SchemaVersion<5||o.TargetId!="")Error("resource",o.Id,"Secured resource objective requires schema 5 and no target ID.");break;case CampaignObjectiveTrigger.AreaVisited:Ref(c.Areas.Select(a=>a.Id),o.TargetId,o.Id);break;case CampaignObjectiveTrigger.FeatureUsed:Ref(features.Select(f=>f.Id),o.TargetId,o.Id);break;case CampaignObjectiveTrigger.ItemOwned:Ref(c.Items.Select(i=>i.Id),o.TargetId,o.Id);break;case CampaignObjectiveTrigger.EncounterCleared:Ref(c.Encounters.Select(x=>x.Id),o.TargetId,o.Id);break;case CampaignObjectiveTrigger.FlagSet:
    var flags=c.Events.SelectMany(x=>x.Effects).Where(x=>x.Handler=="set-flag").Select(x=>x.Flag).Concat(c.Dialogues.Select(d=>d.CompletionFlag)).Concat(c.Dialogues.SelectMany(d=>d.Nodes??[]).Where(n=>n!=null).SelectMany(n=>n.Choices??[]).Where(ch=>ch!=null).SelectMany(ch=>ch.Consequence?.SetFlags??[]));
    Ref(flags,o.TargetId,o.Id);break;}
  }
  if(c.Expedition is {} expedition)
  {
   if(string.IsNullOrWhiteSpace(expedition.Title)||string.IsNullOrWhiteSpace(expedition.Briefing)||string.IsNullOrWhiteSpace(expedition.SuccessText)||expedition.MinimumStoredSv<1)Error("expedition","expedition","Named expedition needs briefing, outcome and positive stored-SV target.");
   if(!features.Any(f=>f.Id==expedition.DebriefFeatureId&&f.Kind=="debrief")||!features.Any(f=>f.Id==expedition.StockpileFeatureId&&f.SecuringSite)||!c.Areas.Any(a=>a.Features.Any(f=>f.Id==expedition.DebriefFeatureId)&&a.Features.Any(f=>f.Id==expedition.StockpileFeatureId)))Error("expedition","expedition","Debrief and securing stockpile must belong to the same area.");
   if(Present(expedition.Stages,"expedition.stages"))
   {
    var used=new HashSet<string>();if(expedition.Stages.Count is <1 or >8)Error("expedition","stages","Use one to eight stages.");
    foreach(var stage in expedition.Stages){Id(stage.Id,"expedition.stages");if(string.IsNullOrWhiteSpace(stage.Title)||string.IsNullOrWhiteSpace(stage.Hint))Error("expedition",stage.Id,"Stage needs title and hint.");if(!Present(stage.ObjectiveIds,stage.Id+".objectives"))continue;if(stage.ObjectiveIds.Count==0)Error("expedition",stage.Id,"Stage needs objectives.");foreach(var id in stage.ObjectiveIds){Ref(c.Objectives.Select(o=>o.Id),id,stage.Id);if(!used.Add(id))Error("expedition",stage.Id,"Objective belongs to more than one stage.");}}
    if(c.Objectives.Any(o=>!used.Contains(o.Id)))Error("expedition","stages","Every campaign objective must belong to a stage.");
   }
  }
  e.AddRange(HordeRules.Validate(c));
  foreach(var horde in c.Hordes)Id(horde.Id,"hordes.id");
  foreach(var feature in features.Where(f=>f.Kind=="debrief"))if(c.Expedition?.DebriefFeatureId!=feature.Id)Error("expedition",feature.Id,"Debrief needs an expedition binding.");
  if(c.SchemaVersion>=2)
  {
   var reached=new HashSet<string>{c.StartAreaId};bool more=true;while(more){more=false;foreach(var r in c.Connections)if(reached.Contains(r.FromAreaId)&&reached.Add(r.ToAreaId))more=true;}
   foreach(var a in c.Areas)if(!reached.Contains(a.Id))Error("connection",a.Id,"Area is unreachable from start in the directed connection graph.");
  }
  return e;
 }
}
