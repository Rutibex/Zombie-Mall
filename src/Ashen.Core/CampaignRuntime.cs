using System.Text.Json.Serialization;
namespace Ashen.Core;

public sealed partial class World
{
 // Save-owned definition snapshot: another campaign or external JSON edit cannot change an existing journey.
 public CampaignDefinition? Campaign {get;set;}
 public List<string> ExecutedCampaignEvents {get;set;}=[];
 [JsonIgnore] public bool UsesCampaignDefinitions=>Campaign!=null;
 [JsonIgnore] public string CampaignName=>Campaign?.Name??"Ashen Covenant engine test";
 [JsonIgnore] public IReadOnlyList<TravelDestinationDefinition> TravelDefinitions=>Campaign==null?TravelCatalog.Destinations:Campaign.Areas.Select(a=>new TravelDestinationDefinition{Id=a.Id,Name=a.Name,Region=Campaign.Name,Description=Campaign.Description,MapX=a.MapX,MapY=a.MapY,InitiallyDiscovered=a.Id==Campaign.StartAreaId,InitiallyUnlocked=a.Id==Campaign.StartAreaId,Arrival=a.Actors.FirstOrDefault()?.Position.Center??new(0.5f,.5f)}).ToArray();
 public TravelDestinationDefinition ResolveTravelDestination(string id)=>TravelDefinitions.FirstOrDefault(a=>a.Id==id)??throw new KeyNotFoundException($"Unknown area '{id}'.");
 public TravelRouteDefinition? ResolveTravelRoute(string a,string b)=>Campaign==null?TravelCatalog.Route(a,b):Campaign.Connections.FirstOrDefault(r=>r.FromAreaId==a&&r.ToAreaId==b) is {} route?CampaignRouteDefinition(route):null;
 TravelRouteDefinition CampaignRouteDefinition(CampaignConnection r)=>new(){A=r.FromAreaId,B=r.ToAreaId,Minutes=r.Minutes,ExitA=Campaign!.Areas.First(a=>a.Id==r.FromAreaId).Features.First(f=>f.Id==r.ExitFeatureId).Name};
 public TravelEventDefinition? LookupTravelEvent(string id)=>Campaign==null?TravelCatalog.Event(id):null;
 public Gear ResolveItemDefinition(string id)=>Campaign==null?Items.Get(id):(Campaign.Items.FirstOrDefault(i=>i.Id==id)?.ToGear()??throw new InvalidDataException($"Unknown campaign item '{id}'."));
 public bool TryGetDialogue(string id,out DialogueDefinition dialogue)
 {
  if(Campaign==null)return DialogueCatalog.TryGet(id,out dialogue);
  dialogue=Campaign.Dialogues.FirstOrDefault(d=>d.Id==id)!;return dialogue!=null;
 }
 public static World New(CampaignDefinition definition)
 {
  var c=CampaignLoader.Clone(definition);var area=c.Areas.First(a=>a.Id==c.StartAreaId);
  var w=new World{Campaign=c,CurrentTravelDestinationId=area.Id,CampaignMinutes=c.StartMinute,Floors=area.Floors.ToList(),Gold=0,Rations=0};
  var initial=w.MaterializeCampaignArea(area.Id,true);w.Actors=initial.Actors;w.Features=initial.Features;
  w.TravelDestinations=c.Areas.Select(a=>new TravelDestinationState{Id=a.Id,Discovered=a.Id==area.Id,Unlocked=a.Id==area.Id,Visits=a.Id==area.Id?1:0}).ToList();w.DiscoverCampaignNeighbours();
  w.InfrastructureState.LastMinute=w.CampaignMinutes;
  w.InitializeTension();w.InitializeFinale();w.InitializeWorkshop();
  w.InitializeCampaignJournal();w.InitializeCompanionConversationState();w.InitializeRestState();w.AssignMissingItemIds();
  w.LogQuest(c.Description);w.LogSystem(c.SchemaVersion>=3?"West entrance reached. Search the southwest shops and find a defensible office.":"Campaign definitions loaded. This is a small campaign fixture, not the completed mall.");w.Reveal();w.EnterLowerMall(true);if(c.Expedition!=null){w.AddStoryJournalEntry("expedition-briefing",c.Expedition.Title,c.Expedition.Briefing,"Expedition");w.SyncCampaignObjectives();}return w;
 }
 WorldAreaSnapshot MaterializeCampaignArea(string id,bool includeParty=false)
 {
  var w=this;var c=Campaign??throw new InvalidOperationException("No campaign definitions.");var area=c.Areas.First(a=>a.Id==id);
  foreach(var p in area.Actors)
  {
   var d=c.Actors.First(a=>a.Id==p.ActorId);if(d.Recruitable||!includeParty&&!d.Enemy||c.Hordes.Any(h=>h.MemberActorIds.Contains(d.Id))||w.StoryReservedActor(d.Id)||w.LowerMallReservedActor(d.Id))continue;
   w.AddCampaignActor(d,p.Position);

  }
  foreach(var d in area.Features)
  {
   var f=new Feature{Id=d.Id,Name=d.Name,Kind=d.Kind=="event"?"campaign-event":d.Kind,Pos=d.Position,DialogueId=d.DialogueId,CampaignEventId=d.EventId};
   for(int i=0;i<d.Contents.Count;i++){var s=d.Contents[i];var stack=w.CreateItemStack(s.ItemId,s.Quantity);stack.Wear=s.Wear;if(w.LowerMallEnabled&&stack.Definition.Name=="Glock 19")stack.LoadedAmmunition=w.CreateItemStack("item.mall.ammo-9mm",12);if(w.LowerMallEnabled&&stack.Definition.Name.Contains("Jukebox revolver"))stack.LoadedAmmunition=w.CreateItemStack("item.mall.ammo-38",3);if(w.LowerMallEnabled&&stack.Definition.Name=="Loaded .22 pistol")stack.LoadedAmmunition=w.CreateItemStack("item.lower.native-22lr",6);if(w.LowerMallEnabled&&stack.Definition.Name=="Single-shot flare gun")stack.LoadedAmmunition=w.CreateItemStack("item.lower.native-flare",1);if(w.LowerMallEnabled&&stack.Definition.Name=="Loaded Book Nook revolver")stack.LoadedAmmunition=w.CreateItemStack("item.mall.ammo-38",6);if(w.LowerMallEnabled&&stack.Definition.Name=="Loaded optical revolver")stack.LoadedAmmunition=w.CreateItemStack("item.mall.ammo-38",3);if(w.LowerMallEnabled&&stack.Definition.Name=="Loaded .357 Magnum")stack.LoadedAmmunition=w.CreateItemStack("item.lower.native-357-magnum",6);if(stack.Definition.Name is "Piano Cache Loaded Revolver" or "Chest Loaded Revolver" or "Tin Revolver Three Rounds")stack.LoadedAmmunition=w.CreateItemStack("item.mall.ammo-38",stack.Definition.Name=="Tin Revolver Three Rounds"?3:6);if(stack.ItemId=="item.upper.37.loot.5")stack.LoadedAmmunition=w.CreateItemStack("item.mall.ammo-38",5);if(stack.ItemId=="item.upper.43.loot.5")stack.LoadedAmmunition=w.CreateItemStack("item.mall.ammo-38",6);if(stack.ItemId=="item.upper.49.loot.8")stack.LoadedAmmunition=w.CreateItemStack("item.mall.ammo-9mm",10);if(stack.ItemId=="item.upper.54.loot.4")stack.LoadedAmmunition=w.CreateItemStack("item.mall.ammo-9mm",6);if(stack.ItemId=="item.upper.46.loot.2")stack.Charges=0;if(d.Scavengable&&c.Items.First(x=>x.Id==s.ItemId).ResourceSv>0)stack.ResourceUnits=Enumerable.Range(0,s.Quantity).Select(n=>ResourceUnitId(d.Id,i,n)).ToList();f.Contents.Add(stack);}w.ConfigureRoof28Container(f);w.ConfigureRoof29Container(f);w.ConfigureExterior30Container(f);w.ConfigureExterior31Container(f);w.Features.Add(f);
  }
  return new(){DestinationId=id,Floors=area.Floors.ToList(),Actors=w.Actors,Features=w.Features};
 }
 void InitializeCampaignJournal()
 {
  if(Campaign==null)return;
  Quests=[];JournalEntries=[];UserJournalNotes=[];NextJournalSequence=1;NextUserJournalNoteNumber=1;
  StartQuest(new(){Id=Campaign.Id+".objectives",Title=Campaign.Name,Summary=Campaign.Description,Category=Campaign.SchemaVersion>=3?"Mall expedition":"Campaign fixture",Objectives=Campaign.Objectives.Select(o=>new QuestObjectiveState{Id=o.Id,Text=o.Text}).ToList(),StartedMinute=CampaignMinutes});
 }
 public void SyncCampaignObjectives()
 {
  if(Campaign==null)return;var q=Quest(Campaign.Id+".objectives");if(q==null)return;
  foreach(var d in Campaign.Objectives)
  {
   bool done=d.Trigger switch
   {
    CampaignObjectiveTrigger.ResourcesSecured=>SecuredSurvivalValue>=d.Quantity,
    CampaignObjectiveTrigger.AreaVisited=>TravelDestinations.Any(a=>a.Id==d.TargetId&&a.Visits>0),
    CampaignObjectiveTrigger.FeatureUsed=>Features.Concat(StoredTravelAreas.SelectMany(a=>a.Features)).Any(f=>f.Id==d.TargetId&&f.Used),
    CampaignObjectiveTrigger.ItemOwned=>Party.Sum(a=>a.Inventory.Count(d.TargetId)+a.Equipment.All().Where(s=>s.ItemId==d.TargetId).Sum(s=>s.Quantity))+SharedStorage.Count(d.TargetId)>=d.Quantity,
    CampaignObjectiveTrigger.EncounterCleared=>ClearedCampaignEncounters.Contains(d.TargetId)||Campaign.Encounters.First(e=>e.Id==d.TargetId) is var enc&&CampaignAreaEnemies(enc.AreaId) is {} enemies&&enc.ActorIds.All(id=>enemies.Any(a=>a.Id==id&&!a.Alive)),
    CampaignObjectiveTrigger.FlagSet=>HasDialogueFlag(d.TargetId),_=>false
   };
   if(done)SetQuestObjective(q.Id,d.Id,true,d.Text);
  }
  SyncExpeditionStageJournal();SyncContracts();SyncSocial();
  if(Campaign.Expedition==null&&q.Status==QuestStatus.Active&&q.Objectives.Count>0&&q.Objectives.All(o=>o.Completed))CompleteQuest(q.Id,Campaign.SchemaVersion>=3?"The southwest section has been explored. The office can serve as a temporary refuge.":"All fixture objectives completed. The expedition can continue through the connected areas.");
 }
 public bool RunCampaignEvent(string id)
 {
  if(Campaign==null)return false;var d=Campaign.Events.FirstOrDefault(e=>e.Id==id);if(d==null)return false;
  if(d.Once&&ExecutedCampaignEvents.Contains(id))return false;
  if(d.Effects.Any(e=>e.Handler=="recruit"&&!CanRecruitCampaignActor(e.ActorId,out _))){LogSystem("There is no room for this survivor to join. Clear a nearby space and keep the party below six.",true);return false;}
  foreach(var effect in d.Effects)switch(effect.Handler){case "recruit":RecruitCampaignActor(effect.ActorId);break;case "log":Say(effect.Text);break;case "set-flag":SetDialogueFlag(effect.Flag);break;default:throw new InvalidDataException("Unregistered campaign effect: "+effect.Handler);}
  if(!ExecutedCampaignEvents.Contains(id))ExecutedCampaignEvents.Add(id);SyncCampaignObjectives();return true;
 }
 // Rebind transient item definitions after loading a self-contained save snapshot.
 void BindCampaignContent()
 {
  ValidateShelterShape();if(Campaign==null)return;CampaignLoader.ValidateOrThrow(Campaign);
  if(StoredTravelAreas==null)throw new InvalidDataException("Null stored-area collection.");
  foreach(var actor in Actors.Concat(ShelterResidents.Select(r=>r.Actor)).Concat(StoredTravelAreas.SelectMany(a=>a.Actors))){actor.UsesEquipmentCatalogue=EquipmentEnabled;actor.UsesSurvivorClasses=SurvivorClassesEnabled&&!actor.Enemy;actor.UsesModernCombat=ModernTacticalCombat;actor.UsesDailySurvival=DailySurvivalEnabled&&!actor.Enemy;}
  foreach(var stack in AllCampaignStacks())stack.CampaignGear=ResolveItemDefinition(stack.ItemId);
  ValidateCampaignAreaStates();ValidateResourceState();ValidateExpeditionState();ValidateFirearmState();ValidateLocalAwareness();ValidateHordeState();ValidateSurvivalState();ValidateShelterState();ValidateInfrastructure();ValidateTension();ValidateSurvivorClasses();ValidateEquipmentState();ValidateWorkshopState();ValidateStoryState();ValidateSocialState();ValidateLowerMall();ValidateFinale();
  if(ExecutedCampaignEvents==null||ExecutedCampaignEvents.Distinct().Count()!=ExecutedCampaignEvents.Count||ExecutedCampaignEvents.Any(id=>Campaign.Events.All(e=>e.Id!=id)))throw new InvalidDataException("Invalid campaign event state.");
  if(Campaign.Areas.All(a=>a.Id!=CurrentTravelDestinationId))throw new InvalidDataException("Save refers to an unknown campaign area.");
  if(Actors.Any(a=>Campaign.Actors.All(d=>d.Id!=a.Id))||Features.Any(f=>Campaign.Areas.All(a=>a.Features.All(d=>d.Id!=f.Id))))throw new InvalidDataException("Save contains unknown campaign actor/feature IDs.");
 }
}
