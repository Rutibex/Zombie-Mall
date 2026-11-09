using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceOfferingKind { Healing, Identification, Resurrection, Donation, Room, Drink, Rumor }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceTargetKind { None, Companion, FallenCompanion, UnidentifiedItem }

/// <summary>IIP42 authored service entry. The definition describes price, target and
/// presentation; World remains the authority for availability and mutation.</summary>
public sealed record ServiceOfferingDefinition
{
 public string Id {get;init;}="";
 public string Name {get;init;}="";
 public ServiceOfferingKind Kind {get;init;}
 public ServiceTargetKind Target {get;init;}
 public int Price {get;init;}
 public int Minutes {get;init;}
 public string Description {get;init;}="";
 public string[] Rumors {get;init;}=[];
}

public sealed record ServiceDefinition
{
 public string Id {get;init;}="";
 public string Name {get;init;}="";
 public string Proprietor {get;init;}="";
 public string Category {get;init;}="SERVICE";
 public string Description {get;init;}="";
 public ServiceOfferingDefinition[] Offerings {get;init;}=[];
 public ServiceOfferingDefinition Offering(string id)=>Offerings.FirstOrDefault(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase))??throw new KeyNotFoundException($"Unknown service offering '{id}' for '{Id}'.");
}

/// <summary>Durable per-provider accounting. Usage counts make repeatable service
/// content such as tavern rumors deterministic across save/load.</summary>
public sealed class ServiceProviderState
{
 public string Id {get;set;}="";
 public int Revenue {get;set;}
 public int Favor {get;set;}
 public Dictionary<string,int> Uses {get;set;}=[];
 public int UseCount(string offeringId)=>Uses.TryGetValue(offeringId,out var count)?Math.Max(0,count):0;
 public void RecordUse(string offeringId,int price){Uses[offeringId]=UseCount(offeringId)+1;Revenue=Math.Max(0,Revenue+Math.Max(0,price));}
}

public readonly record struct ServicePreview(int Price,bool Available,string Reason,string TargetLabel,string OutcomeText);

public static class ServiceCatalog
{
 static readonly ServiceDefinition[] services=
 [
  new()
  {
   Id="saint-orlan-shrine",Name="Shrine of the Last Dawn",Proprietor="Sister Maelin",Category="TEMPLE",
   Description="A public sanctuary of pale stone and ember-glass. The shrine offers practical covenant rites to travelers who can meet the cost of incense, medicine, and rare reagents.",
   Offerings=
   [
    new(){Id="healing",Name="Prayer of Mending",Kind=ServiceOfferingKind.Healing,Target=ServiceTargetKind.Companion,Price=25,Description="Restore the selected living companion to full hit points and clear the ordinary unconscious condition."},
    new(){Id="identify",Name="Identify Item",Kind=ServiceOfferingKind.Identification,Target=ServiceTargetKind.UnidentifiedItem,Price=50,Description="Reveal the full identity and magical properties of one unidentified item carried or equipped by the selected companion."},
    new(){Id="resurrection",Name="Raise Fallen",Kind=ServiceOfferingKind.Resurrection,Target=ServiceTargetKind.FallenCompanion,Price=500,Description="Return one dead companion to life at half maximum hit points. The rite does not restore spent class resources or spell slots."},
    new(){Id="donation",Name="Make a Donation",Kind=ServiceOfferingKind.Donation,Target=ServiceTargetKind.None,Price=100,Description="Contribute to the shrine's medicines, lamps, and pilgrim relief. Each donation raises the shrine's durable favor toward the party, up to 10."}
   ]
  },
  new()
  {
   Id="saint-orlan-inn",Name="The Pilgrim's Lantern",Proprietor="Tomas Pell",Category="INN & TAVERN",
   Description="A low-beamed pilgrims' inn near the west gate. Tomas keeps a clean common room, hot stew, and an excellent memory for road gossip.",
   Offerings=
   [
    new(){Id="room",Name="Rent a Room",Kind=ServiceOfferingKind.Room,Target=ServiceTargetKind.None,Price=8,Minutes=480,Description="Spend eight safe hours at the inn. The current party long-rest recovery routine restores hit points, long-rest resources, and spell slots without consuming camping supplies."},
    new(){Id="drink",Name="Spiced Ale",Kind=ServiceOfferingKind.Drink,Target=ServiceTargetKind.None,Price=1,Minutes=15,Description="Buy a mug and spend a quarter-hour in the common room. This is a social service only; intoxication rules are outside the current campaign model."},
    new(){Id="rumor",Name="Ask for Rumors",Kind=ServiceOfferingKind.Rumor,Target=ServiceTargetKind.None,Price=2,Minutes=10,Description="Pay for a round and hear one of the inn's current rumors. Heard rumors are copied into the Story Journal.",Rumors=
     [
      "A charcoal burner swears a signal fire burned for one night in the Old Watchtower, though no watch has been posted there in years.",
      "Pilgrims coming from Briarwood say the standing stones hum just before dawn, always in the direction of Saint Orlan.",
      "Mara Venn has started buying old shrine gear again. She claims road patrols are asking for anything sturdy enough to survive the eastern track.",
      "Someone at Ashen Ford has been leaving bowls of salt on the river stones. By morning the bowls are empty and the footprints around them point upstream."
     ]}
   ]
  }
 ];
 public static IReadOnlyList<ServiceDefinition> All=>services;
 public static ServiceDefinition Get(string id)=>services.FirstOrDefault(s=>s.Id.Equals(id,StringComparison.OrdinalIgnoreCase))??throw new KeyNotFoundException($"Unknown service provider '{id}'.");
 public static bool TryGet(string id,out ServiceDefinition service){service=services.FirstOrDefault(s=>s.Id.Equals(id,StringComparison.OrdinalIgnoreCase))!;return service!=null;}
}

public sealed partial class World
{
 // IIP42 service usage/revenue persist independently from merchant stock.
 public List<ServiceProviderState> Services {get;set;}=[];
 [JsonIgnore] public Action<Feature>? ServiceRequested {get;set;}

 ServiceProviderState CreateServiceState(ServiceDefinition definition)=>new(){Id=definition.Id};

 public void InitializeServiceState()
 {
  Services=ServiceCatalog.All.Select(CreateServiceState).ToList();NormalizeServiceFeatures();
 }

 void EnsureSaintOrlanServiceFeatures(List<Feature> features)
 {
  var shrine=features.FirstOrDefault(f=>f.Name.Equals("Shrine of the Last Dawn",StringComparison.OrdinalIgnoreCase));
  if(shrine==null){shrine=new(){Kind="service",Name="Shrine of the Last Dawn",Pos=new(22,11)};features.Add(shrine);}
  shrine.Kind="service";shrine.ServiceId="saint-orlan-shrine";
  var inn=features.FirstOrDefault(f=>f.Name.Equals("The Pilgrim's Lantern",StringComparison.OrdinalIgnoreCase));
  if(inn==null)features.Add(new(){Kind="service",Name="The Pilgrim's Lantern",Pos=new(12,20),ServiceId="saint-orlan-inn"});
  else {inn.Kind="service";inn.ServiceId="saint-orlan-inn";}
  // Strengthen IIP41 migration behavior: a pre-IIP41 Saint Orlan snapshot may not contain the market feature at all.
  var market=features.FirstOrDefault(f=>f.Name.Equals("Saint Orlan Market",StringComparison.OrdinalIgnoreCase));
  if(market==null)features.Add(new(){Kind="service",Name="Saint Orlan Market",Pos=new(31,13),StoreId="saint-orlan-market"});
  else {market.Kind="service";market.StoreId="saint-orlan-market";}
 }

 void NormalizeServiceFeatures()
 {
  if(CurrentTravelDestinationId.Equals("saint-orlan",StringComparison.OrdinalIgnoreCase))EnsureSaintOrlanServiceFeatures(Features);
  foreach(var area in StoredTravelAreas??[])if(area.DestinationId.Equals("saint-orlan",StringComparison.OrdinalIgnoreCase)){area.Features??=[];EnsureSaintOrlanServiceFeatures(area.Features);}
 }

 public void NormalizeServiceState()
 {
  if(Campaign!=null)return;
  Services??=[];NormalizeServiceFeatures();
  var byId=Services.Where(s=>s!=null&&ServiceCatalog.All.Any(d=>d.Id.Equals(s.Id,StringComparison.OrdinalIgnoreCase)))
   .GroupBy(s=>s.Id,StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>g.First(),StringComparer.OrdinalIgnoreCase);
  Services=[];
  foreach(var definition in ServiceCatalog.All)
  {
   if(!byId.TryGetValue(definition.Id,out var state))state=CreateServiceState(definition);
   state.Id=definition.Id;state.Revenue=Math.Max(0,state.Revenue);state.Favor=Math.Clamp(state.Favor,0,10);state.Uses??=[];
   state.Uses=state.Uses.Where(kv=>definition.Offerings.Any(o=>o.Id.Equals(kv.Key,StringComparison.OrdinalIgnoreCase))).ToDictionary(kv=>kv.Key,kv=>Math.Max(0,kv.Value),StringComparer.OrdinalIgnoreCase);
   Services.Add(state);
  }
 }

 public void ValidateServiceState()
 {
  if(Services.Select(s=>s.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Services.Count)throw new InvalidDataException("Duplicate service-provider state.");
  foreach(var state in Services)
  {
   var definition=ServiceCatalog.Get(state.Id);if(state.Revenue<0||state.Favor<0||state.Favor>10)throw new InvalidDataException($"Invalid service state for {state.Id}.");
   if(state.Uses.Any(kv=>kv.Value<0||!definition.Offerings.Any(o=>o.Id.Equals(kv.Key,StringComparison.OrdinalIgnoreCase))))throw new InvalidDataException($"Invalid service usage state for {state.Id}.");
  }
  IEnumerable<Feature> FeaturesEverywhere(){foreach(var f in Features)yield return f;foreach(var area in StoredTravelAreas??[])foreach(var f in area.Features??[])yield return f;}
  foreach(var feature in FeaturesEverywhere().Where(f=>!string.IsNullOrWhiteSpace(f.ServiceId)))if(!ServiceCatalog.TryGet(feature.ServiceId,out _))throw new InvalidDataException($"Unknown service feature binding '{feature.ServiceId}'.");
 }

 public ServiceProviderState ServiceState(string id)=>Services.FirstOrDefault(s=>s.Id.Equals(id,StringComparison.OrdinalIgnoreCase))??throw new KeyNotFoundException($"Service state '{id}' is unavailable.");
 public bool IsServiceFeature(Feature feature)=>feature.Kind.Equals("service",StringComparison.OrdinalIgnoreCase)&&!string.IsNullOrWhiteSpace(feature.ServiceId)&&ServiceCatalog.TryGet(feature.ServiceId,out _);
 public IEnumerable<ItemStack> ServiceIdentifiableItems(Actor actor)=>actor.Inventory.Items.Concat(actor.Equipment.All()).Where(i=>!i.Identified);

 public ServicePreview PreviewService(string serviceId,string offeringId,Actor? actor=null,string itemInstanceId="")
 {
  var definition=ServiceCatalog.Get(serviceId);var offering=definition.Offering(offeringId);string target=actor?.Name??"Party";
  if(Mode!=Mode.Explore)return new(offering.Price,false,"Services are only available while exploring outside combat.",target,offering.Description);
  if(Gold<offering.Price)return new(offering.Price,false,$"Insufficient funds: need {offering.Price-Gold} more gp.",target,offering.Description);
  switch(offering.Kind)
  {
   case ServiceOfferingKind.Healing:
    if(actor==null||actor.Enemy||!Party.Contains(actor))return new(offering.Price,false,"Choose a party companion to receive healing.","No target",offering.Description);
    if(actor.Dead)return new(offering.Price,false,"A dead companion requires resurrection, not ordinary healing.",actor.Name,offering.Description);
    if(actor.Hp>=actor.MaxHp&&!actor.Unconscious)return new(offering.Price,false,$"{actor.Name} is already at full hit points.",actor.Name,offering.Description);
    return new(offering.Price,true,"",actor.Name,$"Restore {actor.Name} to {actor.MaxHp}/{actor.MaxHp} HP.");
   case ServiceOfferingKind.Identification:
    if(actor==null||actor.Enemy||!Party.Contains(actor))return new(offering.Price,false,"Choose a party companion carrying an unidentified item.","No target",offering.Description);
    var item=OwnedItem(actor,itemInstanceId);if(item==null)return new(offering.Price,false,"Select an unidentified item from this companion's backpack or equipment.",actor.Name,offering.Description);
    if(item.Identified)return new(offering.Price,false,"That item is already identified.",item.Definition.Name,offering.Description);
    return new(offering.Price,true,"",$"Unidentified {item.Definition.Kind.ToString().ToLowerInvariant()}","Reveal this item's full name, lore, and mechanical properties.");
   case ServiceOfferingKind.Resurrection:
    if(actor==null||actor.Enemy||!Party.Contains(actor))return new(offering.Price,false,"Choose a fallen party companion.","No target",offering.Description);
    if(!actor.Dead)return new(offering.Price,false,$"{actor.Name} is not dead and does not require resurrection.",actor.Name,offering.Description);
    return new(offering.Price,true,"",actor.Name,$"Return {actor.Name} to life at {Math.Max(1,actor.MaxHp/2)} HP.");
   case ServiceOfferingKind.Donation:
    var shrineState=ServiceState(serviceId);if(shrineState.Favor>=10)return new(offering.Price,false,"The shrine already regards the party with its highest favor.","Shrine of the Last Dawn","The donation would support the shrine, but its favor cannot rise further.");
    return new(offering.Price,true,"","Shrine of the Last Dawn",$"Raise shrine favor from {shrineState.Favor} to {shrineState.Favor+1}.");
   case ServiceOfferingKind.Room:
    if(!Party.Any(a=>!a.Dead))return new(offering.Price,false,"No companion can benefit from lodging.","Party",offering.Description);
    return new(offering.Price,true,"","Entire party",$"Advance {offering.Minutes/60} hours and apply IIP43 long-rest recovery, including Hit Die recovery, without consuming camping supplies.");
   case ServiceOfferingKind.Drink:
    return new(offering.Price,true,"","Party",$"Advance {Math.Max(0,offering.Minutes)} minutes in the inn's common room.");
   case ServiceOfferingKind.Rumor:
    return new(offering.Price,true,"","Party","Hear one current road rumor and copy it into the Story Journal.");
   default:return new(offering.Price,false,"Unknown service.",target,offering.Description);
  }
 }

 public bool PerformService(string serviceId,string offeringId,Actor? actor=null,string itemInstanceId="")
 {
  var definition=ServiceCatalog.Get(serviceId);var offering=definition.Offering(offeringId);var preview=PreviewService(serviceId,offeringId,actor,itemInstanceId);
  if(!preview.Available){LogSystem(preview.Reason,true);return false;}
  var state=ServiceState(serviceId);int useNumber=state.UseCount(offering.Id);Gold-=offering.Price;state.RecordUse(offering.Id,offering.Price);
  switch(offering.Kind)
  {
   case ServiceOfferingKind.Healing:
    actor!.Hp=actor.MaxHp;actor.Character.SetCondition("unconscious","Unconscious",false);Message(MessageCategory.Healing,$"{definition.Proprietor} tends {actor.Name}'s wounds. {actor.Name} is restored to full hit points.");Sound?.Invoke("heal");break;
   case ServiceOfferingKind.Identification:
    var item=OwnedItem(actor!,itemInstanceId)!;string itemName=item.Definition.Name;item.Identified=true;Message(MessageCategory.System,$"{definition.Proprietor} identifies {itemName} for {actor!.Name}.");Sound?.Invoke("loot");break;
   case ServiceOfferingKind.Resurrection:
    actor!.Character.SetCondition("dead","Dead",false);actor.Character.SetCondition("unconscious","Unconscious",false);actor.Hp=Math.Max(1,actor.MaxHp/2);Message(MessageCategory.Healing,$"{definition.Proprietor} completes the resurrection rite. {actor.Name} returns to life with {actor.Hp} HP.");Sound?.Invoke("heal");break;
   case ServiceOfferingKind.Donation:
    state.Favor=Math.Min(10,state.Favor+1);Message(MessageCategory.Narrative,$"{definition.Proprietor} accepts the party's donation. Shrine favor rises to {state.Favor}/10.");AddStoryJournalEntry($"service-donation-{serviceId}-{useNumber+1:D3}","Donation to the Last Dawn",$"The party donated {offering.Price} gp to {definition.Name}. Shrine favor is now {state.Favor}/10.","Faith",CurrentJournalChapter,CampaignMinutes);break;
   case ServiceOfferingKind.Room:
    // IIP42 originally called member.Character.RestoreLongRest() directly. IIP43 centralizes
    // safe lodging through the same long-rest recovery policy, including Hit Die recovery.
    CompleteSafeLodging(offering.Minutes);Message(MessageCategory.Rest,$"The party rents a room at {definition.Name}. Eight safe hours pass; living companions complete a long rest without consuming camping supplies.");Sound?.Invoke("heal");break;
   case ServiceOfferingKind.Drink:
    CampaignMinutes=Math.Max(0,CampaignMinutes+Math.Max(0,offering.Minutes));Message(MessageCategory.Narrative,$"The party spends a little time over spiced ale at {definition.Name}.");break;
   case ServiceOfferingKind.Rumor:
    CampaignMinutes=Math.Max(0,CampaignMinutes+Math.Max(0,offering.Minutes));string rumor=offering.Rumors.Length==0?"No useful rumor is circulating tonight.":offering.Rumors[useNumber%offering.Rumors.Length];
    Message(MessageCategory.Narrative,$"Rumor — {rumor}");AddStoryJournalEntry($"service-rumor-{serviceId}-{useNumber+1:D3}","Tavern Rumor",rumor,"Rumor",CurrentJournalChapter,CampaignMinutes);break;
  }
  return true;
 }
}
