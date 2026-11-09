using System.Text.Json.Serialization;
namespace Ashen.Core;

/// <summary>IIP41 merchant catalogue entry. Prices are expressed as percentages of Gear.Value.
/// Store inventory itself is durable World state; the catalogue only supplies authored defaults.</summary>
public sealed record StoreStockDefinition(string ItemId,int Quantity=1,bool Identified=true);

public sealed record StoreDefinition
{
 public string Id {get;init;}="";
 public string Name {get;init;}="";
 public string Proprietor {get;init;}="";
 public string Description {get;init;}="";
 public int BuyPercent {get;init;}=125;
 public int SellPercent {get;init;}=50;
 public int StartingGold {get;init;}=2000;
 public StoreStockDefinition[] InitialStock {get;init;}=[];
}

public sealed class StoreState
{
 public string Id {get;set;}="";
 public InventoryState Stock {get;set;}=new(){MaxSlots=120};
 public int Gold {get;set;}=2000;
}

public readonly record struct StorePricePreview(int UnitPrice,int Quantity,int TotalPrice,bool Affordable,string Reason);
public readonly record struct StoreGearComparison(
 bool IsEquipment,bool Usable,string Reason,string Slot,string EquippedName,
 int AttackDelta,int DamageDelta,int ArmorClassDelta,float WeightDelta);

public static class StoreCatalog
{
 static readonly StoreDefinition[] stores=
 [
  new()
  {
   Id="saint-orlan-market",Name="Saint Orlan Market",Proprietor="Mara Venn",
   Description="A compact pilgrim market under patched canvas awnings. Mara Venn sells road gear, ammunition, medicine, and a few curios recovered from abandoned shrines.",
   BuyPercent=125,SellPercent=50,StartingGold=2500,
   InitialStock=[
    new("healing-potion",12),new("arrows",80),new("sword",2),new("bow",1),new("mace",1),new("staff",1),
    new("shield",2),new("mail",2),new("iron-helm",2),new("trail-boots",2),new("grip-gloves",1),new("wand-sparks",1)
   ]
  }
 ];
 public static IReadOnlyList<StoreDefinition> All=>stores;
 public static StoreDefinition Get(string id)=>stores.FirstOrDefault(s=>s.Id.Equals(id,StringComparison.OrdinalIgnoreCase))??throw new KeyNotFoundException($"Unknown store '{id}'.");
 public static bool TryGet(string id,out StoreDefinition store){store=stores.FirstOrDefault(s=>s.Id.Equals(id,StringComparison.OrdinalIgnoreCase))!;return store!=null;}
}

public sealed partial class World
{
 // IIP41: merchant inventory and cash are durable campaign state. Store definitions remain authored data.
 public List<StoreState> Stores {get;set;}=[];
 [JsonIgnore] public Action<Feature>? StoreRequested {get;set;}

 StoreState CreateStoreState(StoreDefinition definition)
 {
  var state=new StoreState{Id=definition.Id,Stock=new(){MaxSlots=120},Gold=Math.Max(0,definition.StartingGold)};
  foreach(var authored in definition.InitialStock)
  {
   if(authored.Quantity<1)continue;var item=Items.Get(authored.ItemId);int remaining=authored.Quantity;
   while(remaining>0)
   {
    int take=Math.Min(Math.Max(1,item.MaxStack),remaining);var stack=CreateItemStack(item.Id,take);stack.Identified=authored.Identified;
    if(!state.Stock.Add(stack))throw new InvalidDataException($"Initial stock exceeds capacity for {definition.Id}.");remaining-=take;
   }
  }
  return state;
 }

 public void InitializeStoreState()
 {
  Stores=StoreCatalog.All.Select(CreateStoreState).ToList();NormalizeStoreFeatures();AssignMissingItemIds();
 }

 void NormalizeStoreFeatures()
 {
  void Normalize(IEnumerable<Feature> features)
  {
   foreach(var feature in features)
   {
    if(feature.Kind.Equals("service",StringComparison.OrdinalIgnoreCase)&&feature.Name.Equals("Saint Orlan Market",StringComparison.OrdinalIgnoreCase)&&string.IsNullOrWhiteSpace(feature.StoreId))feature.StoreId="saint-orlan-market";
   }
  }
  Normalize(Features);foreach(var area in StoredTravelAreas??[])Normalize(area.Features??[]);
 }

 public void NormalizeStoreState()
 {
  if(Campaign!=null)return;
  Stores??=[];NormalizeStoreFeatures();
  var byId=Stores.Where(s=>s!=null&&StoreCatalog.All.Any(d=>d.Id==s.Id)).GroupBy(s=>s.Id,StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>g.First(),StringComparer.OrdinalIgnoreCase);
  Stores=[];
  foreach(var definition in StoreCatalog.All)
  {
   if(!byId.TryGetValue(definition.Id,out var state))state=CreateStoreState(definition);
   state.Id=definition.Id;state.Stock??=new(){MaxSlots=120};state.Stock.MaxSlots=Math.Max(1,state.Stock.MaxSlots);state.Gold=Math.Max(0,state.Gold);Stores.Add(state);
  }
  AssignMissingItemIds();
 }

 public void ValidateStoreState()
 {
  if(Stores.Select(s=>s.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Stores.Count)throw new InvalidDataException("Duplicate merchant store state.");
  foreach(var store in Stores)
  {
   _=StoreCatalog.Get(store.Id);if(store.Gold<0)throw new InvalidDataException($"Invalid merchant gold for {store.Id}.");store.Stock.ValidateOrThrow($"store {store.Id}");
  }
  IEnumerable<Feature> FeaturesEverywhere(){foreach(var f in Features)yield return f;foreach(var area in StoredTravelAreas??[])foreach(var f in area.Features??[])yield return f;}
  foreach(var feature in FeaturesEverywhere().Where(f=>!string.IsNullOrWhiteSpace(f.StoreId)))if(!StoreCatalog.TryGet(feature.StoreId,out _))throw new InvalidDataException($"Unknown store feature binding '{feature.StoreId}'.");
 }

 public StoreState StoreState(string id)=>Stores.FirstOrDefault(s=>s.Id.Equals(id,StringComparison.OrdinalIgnoreCase))??throw new KeyNotFoundException($"Store state '{id}' is unavailable.");
 public bool IsStoreFeature(Feature feature)=>feature.Kind.Equals("service",StringComparison.OrdinalIgnoreCase)&&!string.IsNullOrWhiteSpace(feature.StoreId)&&StoreCatalog.TryGet(feature.StoreId,out _);

 public int StoreBuyUnitPrice(string storeId,Gear item)
 {
  if(item.Value<=0)return 0;var store=StoreCatalog.Get(storeId);return Math.Max(1,(item.Value*Math.Max(1,store.BuyPercent)+99)/100);
 }
 public int StoreSellUnitPrice(string storeId,Gear item)
 {
  if(item.Value<=0||item.Kind==ItemKind.Quest)return 0;var store=StoreCatalog.Get(storeId);return Math.Max(1,item.Value*Math.Max(0,store.SellPercent)/100);
 }
 public StorePricePreview PreviewStoreBuy(string storeId,Actor buyer,string instanceId,int quantity)
 {
  var state=StoreState(storeId);var stack=state.Stock.FindInstance(instanceId);if(stack==null)return new(0,0,0,false,"That merchant item is no longer available.");
  int q=Math.Clamp(quantity,1,stack.Quantity),unit=StoreBuyUnitPrice(storeId,stack.Definition),total=unit*q;if(unit<=0)return new(unit,q,total,false,"That item is not for sale.");
  if(Mode!=Mode.Explore)return new(unit,q,total,false,"Trading is only available while exploring outside combat.");if(!buyer.Alive||buyer.Enemy)return new(unit,q,total,false,"Choose a living party member to receive the purchase.");if(Gold<total)return new(unit,q,total,false,$"Insufficient funds: need {total-Gold} more gp.");
  var probe=stack.Portion(q);if(q<stack.Quantity)probe.InstanceId="";if(!CanCarry(buyer,probe,out var reason))return new(unit,q,total,false,reason);return new(unit,q,total,true,"");
 }
 public StorePricePreview PreviewStoreSell(string storeId,Actor seller,string instanceId,int quantity)
 {
  var state=StoreState(storeId);var stack=seller.Inventory.FindInstance(instanceId);if(stack==null)return new(0,0,0,false,"That carried item is no longer available.");
  int q=Math.Clamp(quantity,1,stack.Quantity),unit=StoreSellUnitPrice(storeId,stack.Definition),total=unit*q;if(unit<=0)return new(unit,q,total,false,stack.Definition.Kind==ItemKind.Quest?"Quest items cannot be sold.":"The merchant will not buy an item with no trade value.");
  if(Mode!=Mode.Explore)return new(unit,q,total,false,"Trading is only available while exploring outside combat.");if(!seller.Alive||seller.Enemy)return new(unit,q,total,false,"Choose a living party member whose backpack can be traded.");if(state.Gold<total)return new(unit,q,total,false,$"The merchant cannot afford this sale; short by {total-state.Gold} gp.");
  var probe=stack.Portion(q);if(q<stack.Quantity)probe.InstanceId="";if(!state.Stock.CanAdd(probe))return new(unit,q,total,false,"The merchant has no room for that stock.");return new(unit,q,total,true,"");
 }

 public bool BuyFromStore(string storeId,Actor buyer,string instanceId,int quantity=1)
 {
  var preview=PreviewStoreBuy(storeId,buyer,instanceId,quantity);if(!preview.Affordable){LogSystem(preview.Reason,true);return false;}var state=StoreState(storeId);int index=state.Stock.Items.FindIndex(x=>x.InstanceId==instanceId);if(index<0)return false;
  var moved=state.Stock.TakeAt(index,preview.Quantity);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();
  if(!buyer.Inventory.Add(moved)){state.Stock.Add(moved);AssignMissingItemIds();LogSystem("The purchase could not be placed in that backpack.",true);return false;}
  Gold-=preview.TotalPrice;state.Gold+=preview.TotalPrice;AssignMissingItemIds();buyer.QuickAccess.Normalize(buyer);Message(MessageCategory.Loot,$"{buyer.Name} buys {moved.Definition.Name}{(moved.Quantity>1?$" x{moved.Quantity}":"")} for {preview.TotalPrice} gp from {StoreCatalog.Get(storeId).Name}.");Sound?.Invoke("loot");return true;
 }

 public bool SellToStore(string storeId,Actor seller,string instanceId,int quantity=1)
 {
  var preview=PreviewStoreSell(storeId,seller,instanceId,quantity);if(!preview.Affordable){LogSystem(preview.Reason,true);return false;}var state=StoreState(storeId);int index=seller.Inventory.Items.FindIndex(x=>x.InstanceId==instanceId);if(index<0)return false;
  var moved=seller.Inventory.TakeAt(index,preview.Quantity);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();
  if(!state.Stock.Add(moved)){seller.Inventory.Add(moved);AssignMissingItemIds();LogSystem("The merchant cannot accept that stock right now.",true);return false;}
  Gold+=preview.TotalPrice;state.Gold-=preview.TotalPrice;seller.QuickAccess.Normalize(seller);AssignMissingItemIds();Message(MessageCategory.Loot,$"{seller.Name} sells {moved.Definition.Name}{(moved.Quantity>1?$" x{moved.Quantity}":"")} for {preview.TotalPrice} gp to {StoreCatalog.Get(storeId).Name}.");Sound?.Invoke("loot");return true;
 }

 public StoreGearComparison CompareStoreGear(Actor actor,Gear item)
 {
  var slots=item.EquipSlots().ToArray();
  if(slots.Length==0)
  {
   bool usableWithoutSlot=item.EffectiveRequirements.Allows(actor,out var noEquipReason);
   return new(false,usableWithoutSlot,noEquipReason,"","",0,0,0,0);
  }
  var slot=actor.Equipment.PreferredSlot(item);bool usable=CanEquipInSlot(actor,new ItemStack{InstanceId="preview",ItemId=item.Id,Quantity=1,Charges=item.MaxCharges>0?item.MaxCharges:-1,Identified=true},slot,out var reason);var equipped=actor.Equipment.Get(slot)?.Definition;
  return new(true,usable,reason,EquipmentSlots.Display(slot),equipped?.Name??"Empty",item.EffectiveAttackModifier-(equipped?.EffectiveAttackModifier??0),item.EffectiveDamageModifier-(equipped?.EffectiveDamageModifier??0),item.EffectiveArmorModifier-(equipped?.EffectiveArmorModifier??0),item.Weight-(equipped?.Weight??0));
 }
}
