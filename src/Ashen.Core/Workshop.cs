using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed class CampaignRecipe
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public List<CampaignStack> Ingredients {get;set;}=[];
 public string OutputItemId {get;set;}="";
 public int OutputQuantity {get;set;}=1;
 public int Minutes {get;set;}=30;
}
public sealed class CampaignWorkshop
{
 public SourceReference Source {get;set;}=new();
 public string AreaId {get;set;}="";
 public string BenchFeatureId {get;set;}="";
 public string TraderFeatureId {get;set;}="";
 public string TraderName {get;set;}="";
 public string ToolItemId {get;set;}="";
 public string ScrapItemId {get;set;}="";
 public string WoodItemId {get;set;}="";
 public string ClothItemId {get;set;}="";
 public List<CampaignRecipe> Recipes {get;set;}=[];
 public List<CampaignStack> TraderStock {get;set;}=[];
 public int TradeXpBudget {get;set;}=10;
}
public sealed class WorkshopRecord
{
 public int Minute {get;set;}
 public string Text {get;set;}="";
}
public sealed record BarterQuote(bool Allowed,string Reason,int OfferSv,int AskSv,int BonusSv);
public static class WorkshopRules
{
 public static bool Durable(Gear gear)=>gear.Kind is ItemKind.Weapon or ItemKind.Armor or ItemKind.Shield&&gear.Equipment?.Status is not ("scripted" or "reference");
 public static void Validate(CampaignDefinition c,Action<string,string,string> error)
 {
  var d=c.Workshop;
  bool Physical(string id,bool priced=true)=>c.Items.Any(i=>i.Id==id&&(!priced||i.ResourceSv>0)&&i.Equipment?.Status is not ("scripted" or "reference")&&i.Kind!=ItemKind.Quest);
  foreach(var f in c.Areas.SelectMany(a=>a.Features))foreach(var s in f.Contents)if(s.Wear is <0 or >100||s.Wear>0&&(d==null||s.Quantity!=1||!c.Items.Any(i=>i.Id==s.ItemId&&Durable(i.ToGear()))))error("wear",f.Id,"Initial wear requires one durable item and workshop rules.");
  if(d==null)return;
  bool SourceOk(SourceReference? source)=>source!=null&&!string.IsNullOrWhiteSpace(source.AuditId)&&source.PageStart>=1&&source.PageEnd>=source.PageStart&&source.PageEnd<=513;
  if(!SourceOk(d.Source))error("source","workshop","A source page range is required.");
  var area=c.Areas.FirstOrDefault(a=>a.Id==d.AreaId);
  if(c.SchemaVersion<17||!c.EquipmentCatalogue||c.Shelter==null||area==null||d.AreaId!=c.Shelter.AreaId||!area.Features.Any(f=>f.Id==d.BenchFeatureId)||!area.Features.Any(f=>f.Id==d.TraderFeatureId)||d.BenchFeatureId==d.TraderFeatureId||string.IsNullOrWhiteSpace(d.TraderName))error("workshop","workshop","Workshop requires schema 17, the refuge area and distinct authored facilities.");
  if(!Physical(d.ToolItemId)||!Physical(d.ScrapItemId)||!Physical(d.WoodItemId)||!Physical(d.ClothItemId)||d.Recipes==null||d.TraderStock==null){error("workshop","workshop","Invalid materials, tools or collections.");return;}
  if(d.TradeXpBudget is <0 or >100||d.Recipes.Count is <1 or >32||d.Recipes.Any(r=>r==null)||d.Recipes.Where(r=>r!=null).Select(r=>r.Id).Distinct().Count()!=d.Recipes.Count||d.TraderStock.Count is <1 or >40)error("workshop","workshop","Invalid counts or reward cap.");
  foreach(var r in d.Recipes.Where(r=>r!=null))
  {
   var item=c.Items.FirstOrDefault(i=>i.Id==r.OutputItemId);
   if(string.IsNullOrWhiteSpace(r.Id)||string.IsNullOrWhiteSpace(r.Name)||!SourceOk(r.Source)||r.Minutes is <1 or >240||item==null||!Physical(r.OutputItemId,false)||r.OutputQuantity<1||r.OutputQuantity>item.MaxStack||r.Ingredients==null||r.Ingredients.Count is <1 or >8){error("recipe",r.Id,"Invalid finite recipe.");continue;}
   if(r.Ingredients.Select(i=>i.ItemId).Distinct().Count()!=r.Ingredients.Count||r.Ingredients.Any(i=>i.Wear!=0||i.Quantity is <1 or >100||!Physical(i.ItemId)||c.Items.Any(d=>d.Id==i.ItemId&&(d.TreatmentUses>1||d.Equipment?.Charges>0||Durable(d.ToGear())))))error("recipe",r.Id,"Ingredients must be distinct whole supply units.");
   long cost=r.Ingredients.Sum(s=>(long)s.Quantity*(c.Items.FirstOrDefault(i=>i.Id==s.ItemId)?.ResourceSv??0));if((long)item.ResourceSv*r.OutputQuantity>cost)error("recipe",r.Id,"Outputs cannot exceed inputs' printed SV.");
  }
  foreach(var s in d.TraderStock)if(s==null||!Physical(s.ItemId)||s.Wear!=0||s.Quantity<1||!c.Items.Any(i=>i.Id==s.ItemId&&s.Quantity<=i.MaxStack))error("trader",s?.ItemId??"stock","Invalid finite initial stock.");
 }
}
public sealed partial class World
{
 public InventoryState TraderStock {get;set;}=new(){MaxSlots=64};
 public int BarterXpAwarded {get;set;}
 public int BarterXpRemainder {get;set;}
 public int BarterRewardUsed {get;set;}
 public List<WorkshopRecord> WorkshopHistory {get;set;}=[];
 [JsonIgnore] public bool WorkshopEnabled=>Campaign?.Workshop!=null;
 void InitializeWorkshop(){if(WorkshopEnabled)foreach(var s in Campaign!.Workshop!.TraderStock)TraderStock.Add(CreateItemStack(s.ItemId,s.Quantity));}
 public string WorkshopAccessReason(Actor a,bool barter=false)
 {
  if(!WorkshopEnabled)return "This saved campaign does not enable workshops or barter.";
  if(!Party.Contains(a)||!a.Alive)return "Choose a conscious active survivor.";
  if(ReactionInterruptActive||PendingReaction!=null||Party.Any(IsMoving))return "Finish movement and reactions first.";
  string safety=ShelterManagementReason();if(safety!="")return safety;
  var d=Campaign!.Workshop!;var f=Features.FirstOrDefault(f=>f.Id==(barter?d.TraderFeatureId:d.BenchFeatureId));
  if(f==null||a.Position.Distance(f.Pos.Center)>2.25f||!CanSee(a.Position,f.Pos.Center))return barter?"Stand beside the trading post at the refuge.":"Stand beside the refuge workbench.";
  if((long)CampaignMinutes+240+1440>int.MaxValue)return "The campaign clock limit has been reached.";
  return "";
 }
 IEnumerable<InventoryState> WorkshopSources(Actor a)=>new[]{a.Inventory,ShelterHomeStockpile()!.Contents};
 public int WorkshopMaterialCount(Actor a,string id)=>WorkshopEnabled&&ShelterHomeStockpile()!=null?WorkshopSources(a).Sum(i=>i.Count(id)):0;
 bool WorkshopHasTools(Actor a)=>WorkshopMaterialCount(a,Campaign!.Workshop!.ToolItemId)>0;
 static void WorkshopPay(IEnumerable<InventoryState> sources,IEnumerable<CampaignStack> ingredients)
 {
  foreach(var cost in ingredients){int left=cost.Quantity;foreach(var source in sources){foreach(var s in source.Items.Where(s=>s.ItemId==cost.ItemId).ToArray()){int n=Math.Min(left,s.Quantity);s.ConsumeResourceUnits(n);s.Quantity-=n;left-=n;if(s.Quantity==0)source.Items.Remove(s);if(left==0)break;}if(left==0)break;}if(left!=0)throw new InvalidOperationException("Workshop preflight payment mismatch.");}
 }
 public int CraftMinutes(Actor a,CampaignRecipe recipe)=>Math.Max(1,a.ClassIs("smart")?(recipe.Minutes+1)/2:recipe.Minutes);
 public string CraftReason(Actor a,string recipeId)
 {
  string reason=WorkshopAccessReason(a);if(reason!="")return reason;var r=Campaign!.Workshop!.Recipes.FirstOrDefault(r=>r.Id==recipeId);if(r==null)return "Unknown recipe.";
  if(!WorkshopHasTools(a))return "A reusable toolkit must be in this backpack or the refuge stockpile.";
  foreach(var c in r.Ingredients)if(WorkshopMaterialCount(a,c.ItemId)<c.Quantity)return $"Need {c.Quantity} {ResolveItemDefinition(c.ItemId).Name}.";
  var copies=WorkshopSources(a).Select(i=>new InventoryState{MaxSlots=i.MaxSlots,Items=i.Items.Select(s=>s.Clone()).ToList()}).ToArray();WorkshopPay(copies,r.Ingredients);
  var gear=ResolveItemDefinition(r.OutputItemId);var output=new ItemStack{ItemId=r.OutputItemId,Quantity=r.OutputQuantity,Charges=gear.MaxCharges>0?gear.MaxCharges:-1,CampaignGear=gear};
  return copies[1].CanAdd(output)?"":"Make stockpile space for the crafted output.";
 }
 void WorkshopRemember(string text,int minutes)
 {
  CampaignMinutes=checked(CampaignMinutes+minutes);ProcessSurvivalTime();WorkshopHistory.Add(new(){Minute=CampaignMinutes,Text=text});if(WorkshopHistory.Count>32)WorkshopHistory.RemoveAt(0);AssignMissingItemIds();foreach(var a in Party)a.QuickAccess.Normalize(a);Message(MessageCategory.Rest,text);SyncCampaignObjectives();Reveal();
 }
 public bool Craft(Actor a,string recipeId)
 {
  string reason=CraftReason(a,recipeId);if(reason!=""){LogSystem(reason,true);return false;}var r=Campaign!.Workshop!.Recipes.Single(r=>r.Id==recipeId);StopAll();WorkshopPay(WorkshopSources(a),r.Ingredients);ShelterHomeStockpile()!.Contents.Add(CreateItemStack(r.OutputItemId,r.OutputQuantity));WorkshopRemember($"{a.Name} crafts {r.OutputQuantity} {ResolveItemDefinition(r.OutputItemId).Name}; {CraftMinutes(a,r)} minutes. Output is uncredited.",CraftMinutes(a,r));return true;
 }
 public IEnumerable<ItemStack> WorkshopOwned(Actor a)=>a.Inventory.Items.Concat(a.Equipment.All());
 public string RepairReason(Actor a,string instance,bool quick=false)
 {
  string reason=WorkshopAccessReason(a);if(reason!="")return reason;var s=WorkshopOwned(a).FirstOrDefault(s=>s.InstanceId==instance);if(s==null||!WorkshopRules.Durable(s.Definition)||s.Wear<1)return "Choose this survivor's damaged weapon, armour or shield.";
  if(quick&&!a.ClassIs("smart"))return "Quick Repairs requires Tinkerer's Instinct.";
  if(!WorkshopHasTools(a))return "Keep a reusable toolkit in the backpack or refuge stockpile.";
  int cost=quick?1:2;return WorkshopMaterialCount(a,Campaign!.Workshop!.ScrapItemId)<cost?$"Need {cost} scrap-metal units.":"";
 }
 public bool RepairEquipment(Actor a,string instance,bool quick=false)
 {
  string reason=RepairReason(a,instance,quick);if(reason!=""){LogSystem(reason,true);return false;}var s=WorkshopOwned(a).First(s=>s.InstanceId==instance);WorkshopPay(WorkshopSources(a),[new(){ItemId=Campaign!.Workshop!.ScrapItemId,Quantity=quick?1:2}]);int restored=Math.Min(s.Wear,quick?Roll(6):25);s.Wear-=restored;WorkshopRemember($"{a.Name} repairs {restored} condition on {s.Definition.Name}; {100-s.Wear}/100 remaining.",quick?10:30);return true;
 }
 public string SalvageOutput(ItemStack s)=>s.Definition.Kind is ItemKind.Armor?Campaign!.Workshop!.ClothItemId:s.Definition.Equipment?.Family is "bow" or "crossbow"?Campaign!.Workshop!.WoodItemId:Campaign!.Workshop!.ScrapItemId;
 public string SalvageReason(Actor a,string instance)
 {
  string reason=WorkshopAccessReason(a);if(reason!="")return reason;var s=a.Inventory.Items.FirstOrDefault(s=>s.InstanceId==instance);if(s==null||!WorkshopRules.Durable(s.Definition))return "Stow one ordinary weapon, armour or shield in this backpack.";
  if(s.LoadedAmmunition!=null)return "Unload every physical round before dismantling the weapon.";
  if(!WorkshopHasTools(a))return "A reusable toolkit is required.";
  return ShelterHomeStockpile()!.Contents.CanAdd(new(){ItemId=SalvageOutput(s),CampaignGear=ResolveItemDefinition(SalvageOutput(s))})?"":"Make refuge stockpile space for the recovered material.";
 }
 public bool SalvageEquipment(Actor a,string instance)
 {
  string reason=SalvageReason(a,instance);if(reason!=""){LogSystem(reason,true);return false;}var s=a.Inventory.Items.First(s=>s.InstanceId==instance);string output=SalvageOutput(s);a.Inventory.Items.Remove(s);ShelterHomeStockpile()!.Contents.Add(CreateItemStack(output));WorkshopRemember($"{a.Name} dismantles {s.Definition.Name}; one {ResolveItemDefinition(output).Name} recovered without new scavenging credit.",a.ClassIs("smart")?5:10);return true;
 }
 public int BarterValue(ItemStack s,int quantity)
 {
  if(!WorkshopEnabled||quantity<1||quantity>s.Quantity||!s.Identified||s.Definition.Kind==ItemKind.Quest||s.Definition.Equipment?.Status is "scripted" or "reference"||s.LoadedAmmunition!=null)return 0;
  int sv=Campaign!.Items.Single(i=>i.Id==s.ItemId).ResourceSv;if(sv<=0||s.Broken)return 0;long value=(long)sv*quantity;
  if(WorkshopRules.Durable(s.Definition))value=value*(100-s.Wear)/100;
  if(s.Definition.MaxCharges>0)value=value*s.Charges/s.Definition.MaxCharges;
  return (int)Math.Min(int.MaxValue,value);
 }
 public BarterQuote PreviewBarter(Actor a,string offerId,int offerQuantity,string wantId,int wantQuantity)
 {
  BarterQuote Deny(string r,int offer=0,int ask=0)=>new(false,r,offer,ask,0);string reason=WorkshopAccessReason(a,true);if(reason!="")return Deny(reason);
  var offered=a.Inventory.FindInstance(offerId);var wanted=TraderStock.FindInstance(wantId);
  if(offered==null||wanted==null||offerQuantity<1||offerQuantity>offered.Quantity||wantQuantity<1||wantQuantity>wanted.Quantity)return Deny("Choose owned backpack goods and available trader stock in valid quantities.");
  if(offered.ItemId==wanted.ItemId)return Deny("Exchange different goods; identical goods cannot create a commission.");
  int value=BarterValue(offered,offerQuantity),basePrice=BarterValue(wanted,wantQuantity),markup=125-Math.Clamp(a.SkillModifier(Skill.Persuasion),0,10)+SocialTradeMarkup;long askLong=((long)basePrice*markup+99)/100;
  if(value<=0||basePrice<=0||askLong>int.MaxValue)return Deny("Unpriced, unidentified, broken, loaded or scripted goods are not accepted.");int ask=(int)askLong;
  if(value<ask)return Deny("Offer more goods or request fewer units. Excess offered value is not returned as change.",value,ask);
  var bag=new InventoryState{MaxSlots=a.Inventory.MaxSlots,Items=a.Inventory.Items.Select(s=>s.Clone()).ToList()};var stock=new InventoryState{MaxSlots=TraderStock.MaxSlots,Items=TraderStock.Items.Select(s=>s.Clone()).ToList()};
  var give=bag.TakeAt(bag.Items.FindIndex(s=>s.InstanceId==offerId),offerQuantity)!;var take=stock.TakeAt(stock.Items.FindIndex(s=>s.InstanceId==wantId),wantQuantity)!;
  if(!bag.CanAdd(take)||!stock.CanAdd(give)||a.CarriedWeight-give.Weight+take.Weight>a.CarryingCapacity+.001f)return Deny("Make backpack/merchant space and carrying capacity before trading.",value,ask);
  int bonus=Math.Min(Campaign!.Workshop!.TradeXpBudget-BarterRewardUsed,offerQuantity+wantQuantity),each=(BarterXpRemainder+bonus)/Party.Count();if(Party.Any(a=>(long)a.Character.Experience+each>int.MaxValue))return Deny("Trade XP would exceed a survivor's experience limit.",value,ask);
  return new(true,"",value,ask,bonus);
 }
 public bool Barter(Actor a,string offerId,int offerQuantity,string wantId,int wantQuantity)
 {
  var q=PreviewBarter(a,offerId,offerQuantity,wantId,wantQuantity);if(!q.Allowed){LogSystem(q.Reason,true);return false;}
  var give=a.Inventory.TakeAt(a.Inventory.Items.FindIndex(s=>s.InstanceId==offerId),offerQuantity)!;var take=TraderStock.TakeAt(TraderStock.Items.FindIndex(s=>s.InstanceId==wantId),wantQuantity)!;TraderStock.Add(give);a.Inventory.Add(take);
  int pool=BarterXpRemainder+q.BonusSv,each=pool/Party.Count();foreach(var member in Party)member.Character.Experience+=each;BarterXpAwarded+=each*Party.Count();BarterXpRemainder=pool%Party.Count();BarterRewardUsed+=q.BonusSv;
  WorkshopRemember($"{a.Name} trades {offerQuantity} {give.Definition.Name} for {wantQuantity} {take.Definition.Name} with {Campaign!.Workshop!.TraderName}. Offer {q.OfferSv} SV, asking {q.AskSv}; no change. Trade reward {q.BonusSv} SV ({BarterRewardUsed}/{Campaign.Workshop.TradeXpBudget} lifetime cap).",5);return true;
 }
 void WearWeapon(Actor a,bool hit){if(WorkshopEnabled&&!a.Enemy&&a.Equipment.Weapon is {} s&&WorkshopRules.Durable(s.Definition)&&(hit||s.Definition.MagazineSize>0))ApplyWear(s);}
 void WearArmor(Actor a,int damage,DamageType type){if(WorkshopEnabled&&!a.Enemy&&damage>0&&type is DamageType.Bludgeoning or DamageType.Piercing or DamageType.Slashing){if(a.Equipment.Armor is {} s&&WorkshopRules.Durable(s.Definition))ApplyWear(s);if(a.Equipment.OffHand is {} shield&&shield.Definition.Kind==ItemKind.Shield)ApplyWear(shield);}}
 void ApplyWear(ItemStack s){if(s.Wear<100){s.Wear++;if(s.Wear==100)LogSystem($"{s.Definition.Name} breaks. Its physical item and ammunition remain; repair or replace it.",true);}}
 void ValidateWorkshopState()
 {
  if(TraderStock==null||WorkshopHistory==null||WorkshopHistory.Count>32||WorkshopHistory.Any(e=>e==null||e.Minute<0||e.Minute>CampaignMinutes||string.IsNullOrWhiteSpace(e.Text)||e.Text.Length>2000)||TraderStock.MaxSlots!=64||BarterRewardUsed<0||BarterXpAwarded<0||BarterXpRemainder is <0 or >5||BarterXpAwarded+BarterXpRemainder!=BarterRewardUsed)throw new InvalidDataException("Invalid workshop/trade state.");
  if(!WorkshopEnabled&&(TraderStock.Items.Count>0||WorkshopHistory.Count>0||BarterRewardUsed>0||Campaign!=null&&AllCampaignStacks().Any(s=>s.Wear>0)))throw new InvalidDataException("Workshop state requires campaign opt-in.");
  if(WorkshopEnabled&&BarterRewardUsed>Campaign!.Workshop!.TradeXpBudget)throw new InvalidDataException("Trade reward budget exceeded.");TraderStock.ValidateOrThrow("scavenger trader");
 }
}
