using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ItemKind { Weapon, Armor, Shield, Ammunition, Accessory, Charm, Consumable, Quest, Tool, Misc }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EquipmentSlot
{
 // Weapon is retained as a source/save compatibility name for the main-hand slot.
 MainHand=0,Weapon=MainHand,
 OffHand=1,Armor=2,Head=3,Neck=4,Cloak=5,Gloves=6,Boots=7,
 RingLeft=8,RingRight=9,Charm=10,Ammo1=11,Ammo2=12,Ammo3=13
}

public static class EquipmentSlots
{
 public static readonly EquipmentSlot[] Body=[
  EquipmentSlot.MainHand,EquipmentSlot.OffHand,EquipmentSlot.Armor,EquipmentSlot.Head,EquipmentSlot.Neck,
  EquipmentSlot.Cloak,EquipmentSlot.Gloves,EquipmentSlot.Boots,EquipmentSlot.RingLeft,EquipmentSlot.RingRight,EquipmentSlot.Charm
 ];
 public static readonly EquipmentSlot[] Ammunition=[EquipmentSlot.Ammo1,EquipmentSlot.Ammo2,EquipmentSlot.Ammo3];
 public static readonly EquipmentSlot[] All=[..Body,..Ammunition];
 public static string Display(EquipmentSlot slot)=>slot switch
 {
  EquipmentSlot.MainHand=>"Main Hand",EquipmentSlot.OffHand=>"Off Hand",EquipmentSlot.RingLeft=>"Left Ring",EquipmentSlot.RingRight=>"Right Ring",
  EquipmentSlot.Ammo1=>"Quiver 1",EquipmentSlot.Ammo2=>"Quiver 2",EquipmentSlot.Ammo3=>"Quiver 3",_=>slot.ToString()
 };
}

public sealed class ItemRequirement
{
 public int MinimumLevel {get;set;}=1;
 public List<string> AllowedClasses {get;set;}=[];
 public List<string> AnyWeaponProficiency {get;set;}=[];
 public List<string> AnyArmorProficiency {get;set;}=[];
 public bool Allows(Actor actor,out string reason)
 {
  if(actor.TotalLevel<MinimumLevel){reason=$"Requires level {MinimumLevel}.";return false;}
  if(AllowedClasses.Count>0&&!AllowedClasses.Contains(actor.Character.PrimaryClassName,StringComparer.OrdinalIgnoreCase))
  {reason=$"Requires class: {string.Join(" / ",AllowedClasses)}.";return false;}
  if(AnyWeaponProficiency.Count>0&&!AnyWeaponProficiency.Any(p=>actor.Character.Proficiencies.Weapons.Contains(p,StringComparer.OrdinalIgnoreCase)))
  {reason=$"Requires weapon proficiency: {string.Join(" / ",AnyWeaponProficiency)}.";return false;}
  if(AnyArmorProficiency.Count>0&&!AnyArmorProficiency.Any(p=>actor.Character.Proficiencies.Armor.Contains(p,StringComparer.OrdinalIgnoreCase)))
  {reason=$"Requires armor proficiency: {string.Join(" / ",AnyArmorProficiency)}.";return false;}
  reason="";return true;
 }
 public string Summary()
 {
  var parts=new List<string>();if(MinimumLevel>1)parts.Add($"level {MinimumLevel}");
  if(AllowedClasses.Count>0)parts.Add("class "+string.Join("/",AllowedClasses));
  if(AnyWeaponProficiency.Count>0)parts.Add("weapon proficiency "+string.Join("/",AnyWeaponProficiency));
  if(AnyArmorProficiency.Count>0)parts.Add("armor proficiency "+string.Join("/",AnyArmorProficiency));
  return parts.Count==0?"None":string.Join("; ",parts);
 }
}

public sealed record Gear(
 string Id,string Name,string Slot,int Bonus,string Description,
 float Weight=1f,int MaxStack=1,int MaxCharges=0,int Value=0,ItemKind Kind=ItemKind.Misc,
 ItemRequirement? Requirements=null,bool IdentifiedByDefault=true,
 string[]? AlternateSlots=null,bool TwoHanded=false,string AmmunitionType="",string AmmoType="",
 int AttackModifier=0,int DamageModifier=0,int ArmorModifier=0,bool Magical=false,string Lore="",int? WeaponDamageDieSize=null,int MagazineSize=0,int WeaponRangeFeet=0,int WeaponDamageDice=0,EquipmentProfile? Equipment=null)
{
 [JsonIgnore] public bool Stackable=>MaxStack>1;
 [JsonIgnore] public EquipmentSlot? EquipSlot=>Enum.TryParse<EquipmentSlot>(Slot,true,out var parsed)?parsed:null;
 [JsonIgnore] public ItemRequirement EffectiveRequirements=>Requirements??new();
 [JsonIgnore] public bool RequiresAmmunition=>!string.IsNullOrWhiteSpace(AmmunitionType);
 [JsonIgnore] public bool IsAmmunition=>Kind==ItemKind.Ammunition;
 [JsonIgnore] public int EffectiveAttackModifier=>AttackModifier!=0?AttackModifier:(Kind is ItemKind.Weapon or ItemKind.Charm?Bonus:0);
 [JsonIgnore] public int EffectiveDamageModifier=>DamageModifier!=0?DamageModifier:(Kind is ItemKind.Weapon or ItemKind.Charm?Bonus:0);
 [JsonIgnore] public int EffectiveArmorModifier=>ArmorModifier!=0?ArmorModifier:(Kind is ItemKind.Armor or ItemKind.Shield?Bonus:0);
 public IEnumerable<EquipmentSlot> EquipSlots()
 {
  if(EquipSlot is {} primary)yield return primary;
  if(AlternateSlots!=null)foreach(var raw in AlternateSlots)if(Enum.TryParse<EquipmentSlot>(raw,true,out var slot)&&slot!=EquipSlot)yield return slot;
 }
 public bool CanEquipIn(EquipmentSlot slot)=>EquipSlots().Any(s=>s==slot);
 public string SlotSummary()=>string.Join(" / ",EquipSlots().Select(EquipmentSlots.Display));
}

public static class Items
{
 public static readonly Gear[] All=[
  new("sword","Iron longsword","Weapon",0,"Reliable steel. Knight: 1d8 damage, melee.",3f,1,0,15,ItemKind.Weapon,
   new(){AnyWeaponProficiency=["Martial weapons","Longswords"]},AlternateSlots:["OffHand"],Lore:"A plain crypt-road longsword with a dark leather grip and a maker mark worn nearly smooth. Nothing about it is glamorous, but generations of caravan guards trusted blades exactly like this one."),
  new("bow","Yew longbow","Weapon",0,"Ranger: 1d8 damage, range 7. Requires arrows in an equipped quiver slot.",2f,1,0,20,ItemKind.Weapon,
   AlternateSlots:null,TwoHanded:true,AmmunitionType:"arrow",Lore:"Cut from a single stave of seasoned yew, this bow carries the polish and small repairs of long use. Its string has been replaced often enough that no one remembers the original."),
  new("staff","Ashwood staff","Weapon",0,"Mage: 1d6 arcane bolt, range 6. The staff occupies both hands when treated as physical equipment.",4f,1,0,5,ItemKind.Weapon,
   new(){AnyWeaponProficiency=["Quarterstaffs"]},AlternateSlots:null,TwoHanded:true,Lore:"Ash was once planted beside old covenant roads as a ward against restless spirits. This staff is cut from one of those trees and still shows faint spiral knife marks beneath the hand wrap."),
  new("mace","Pilgrim's mace","Weapon",0,"Cleric: 1d6 damage, melee.",4f,1,0,5,ItemKind.Weapon,
   new(){AnyWeaponProficiency=["Simple weapons"]},AlternateSlots:["OffHand"],Lore:"Pilgrims of Saint Orlan carried compact iron maces because the same tool could break kindling, tent pegs, and the bones of things that followed the road after dark."),
  new("mail","Traveler's mail","Armor",0,"Your class's starting armor. It occupies the armor slot.",20f,1,0,30,ItemKind.Armor,Lore:"A practical coat of linked iron reinforced where a traveler is most likely to be struck. Several rings are newer than the rest, evidence of repairs made far from any proper armorer."),
  new("shield","Ironbound shield","OffHand",2,"A sturdy shield. +2 Armor Class while equipped in the off hand.",6f,1,0,25,ItemKind.Shield,
   new(){AnyArmorProficiency=["Shields"]},ArmorModifier:2,Lore:"Oak planks are clamped beneath a broad iron rim. The face bears old dents rather than decoration; whoever carried it before preferred proof of service to heraldry."),
  new("arrows","Broadhead arrows","Ammo1",0,"A bundle of arrows for bows. Quiver stacks are consumed one shot at a time.",0.05f,80,0,1,ItemKind.Ammunition,
   AlternateSlots:["Ammo2","Ammo3"],AmmoType:"arrow",Lore:"Short iron broadheads are lashed to straight ash shafts with waxed thread. They are ordinary arrows, which is precisely why experienced archers keep so many of them."),
  new("silver","Silvered weapon","Weapon",2,"Any proficient wielder. +2 to attack rolls and damage.",3f,1,0,100,ItemKind.Weapon,
   AlternateSlots:["OffHand"],AttackModifier:2,DamageModifier:2,Magical:true,Lore:"The blade has been worked with old silver and etched with a covenant oath. In lamplight the lettering is almost invisible, but against unnatural darkness it seems to catch light that is not there."),
  new("ward","Warden's armor","Armor",2,"Protective enchanted armor. +2 Armor Class.",20f,1,0,150,ItemKind.Armor,
   ArmorModifier:2,Magical:true,Lore:"The wardens who sealed Saint Orlan's crypt painted prayers between the links where no enemy could read them. The paint has long since vanished, yet the protection remains."),
  new("seal","Sunfire seal","Charm",1,"A worn magical seal. +1 to attack rolls and damage while equipped.",0.2f,1,0,200,ItemKind.Charm,
   AttackModifier:1,DamageModifier:1,Magical:true,Lore:"A thumb-sized seal stamped with a many-rayed sun. Its edges are smooth from generations of nervous hands rubbing it before battle."),
  new("dawn-amulet","Amulet of the Last Dawn","Neck",1,"A warm silver amulet. +1 Armor Class while worn.",0.2f,1,0,175,ItemKind.Accessory,
   ArmorModifier:1,Magical:true,Lore:"The amulet depicts a sunrise beneath a blackened horizon. Tradition says it was given to those expected to stand watch until dawn even when dawn seemed unlikely to come."),
  new("ashen-cloak","Ashen Covenant cloak","Cloak",1,"A soot-dark enchanted cloak. +1 Armor Class while worn.",1f,1,0,175,ItemKind.Accessory,
   ArmorModifier:1,Magical:true,Lore:"This dark mantle is woven with threads that refuse to burn. Older members of the Covenant claimed that every cloak carried a pinch of ash from the order's first sanctuary."),
  new("ember-ring","Ember ring","RingLeft",1,"A small old-school magic ring. +1 to attack and damage; either ring slot accepts it.",0.05f,1,0,250,ItemKind.Accessory,
   AlternateSlots:["RingRight"],AttackModifier:1,DamageModifier:1,Magical:true,Lore:"A coal-red stone sits in a battered bronze band. It is never hot enough to burn, but becomes noticeably warmer whenever violence is close."),
  new("ward-ring","Ring of warding","RingLeft",1,"A protective old-school magic ring. +1 Armor Class; either ring slot accepts it.",0.05f,1,0,250,ItemKind.Accessory,
   AlternateSlots:["RingRight"],ArmorModifier:1,Magical:true,Lore:"Tiny geometric wards circle the inner face of this ring. They are meant to be hidden against the skin, where hostile eyes cannot study the pattern."),
  new("grip-gloves","Gauntlets of sure grip","Gloves",1,"Enchanted gauntlets that steady weapon work. +1 to attack rolls.",0.5f,1,0,180,ItemKind.Accessory,
   AttackModifier:1,Magical:true,Lore:"The palms are stitched with silver thread in a pattern that seems to tighten around any hilt. The enchantment is subtle: the hand simply goes where the wielder intended."),
  new("trail-boots","Trail boots","Boots",0,"Hard-wearing boots. The equipment slot is fully functional even when an item has no numeric bonus.",1.5f,1,0,20,ItemKind.Accessory,Lore:"Thick-soled boots cut for long roads, wet cellars, and stone stairs. Their owner has replaced the laces with cord twice and the left heel once."),
  new("iron-helm","Iron helm","Head",0,"A plain protective helm.",3f,1,0,20,ItemKind.Accessory,Lore:"A simple iron cap with a padded liner. The rim is scarred by old blows, but the metal has not split."),
  new("healing-potion","Healing potion","Consumable",0,"Restores up to 14 HP. Drinking it uses the Action in combat.",0.5f,20,0,50,ItemKind.Consumable,Lore:"A stoppered vial of dark crimson medicine brewed with bitterroot and ember-salt. A faint metallic warmth rises from it when the glass is held to the light."),
  new("wand-sparks","Wand of sparks","Tool",0,"A charge-bearing test item for the inventory runtime. Future item-power phases can bind its effect.",1f,1,7,125,ItemKind.Tool,
   new(){MinimumLevel=2},false,Lore:"A narrow black wand tipped with a copper cage. Static crawls over the cage when the wielder whispers near it, though its complete invocation has not yet been recovered."),
  new("relic","The Ember Heart","Relic",0,"Return this stolen relic to the entrance.",4f,1,0,0,ItemKind.Quest,Lore:"The Ember Heart is a fist-sized reliquary of black iron and cloudy crystal. It was stolen from the crypt seal; even wrapped in cloth, it beats with a slow warmth like something remembering life.")
 ];
 public static Gear Get(string id)=>All.FirstOrDefault(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase))??throw new KeyNotFoundException($"Unknown item id '{id}'.");
 public static bool TryGet(string id,out Gear item){item=All.FirstOrDefault(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase))!;return item!=null;}
}

public sealed class ItemStack
{
 public ItemStack? LoadedAmmunition {get;set;}
 public string InstanceId {get;set;}="";
 public string ItemId {get;set;}="";
 public bool SurvivorAccuracy {get;set;}
 public int Wear {get;set;}
 [JsonIgnore] public bool Broken=>WorkshopRules.Durable(Definition)&&Wear>=100;
 public int Quantity {get;set;}=1;
 public int Charges {get;set;}=-1;
 public bool Identified {get;set;}=true;
 [JsonIgnore] public Gear? CampaignGear {get;set;}
 [JsonIgnore] public Gear Definition {get{var gear=CampaignGear??Items.Get(ItemId);return Wear>=100&&WorkshopRules.Durable(gear)?gear with{ArmorModifier=0,Bonus=0}:gear;}}
 [JsonIgnore] public float Weight=>Definition.Weight*Math.Max(0,Quantity)+(LoadedAmmunition?.Weight??0);
 public List<string> ResourceUnits {get;set;}=[];
 public ItemStack Portion(int quantity){var result=Clone();result.Quantity=Math.Clamp(quantity,1,Quantity);result.ResourceUnits=ResourceUnits.Take(result.Quantity).ToList();if(result.Quantity<Quantity)result.InstanceId="";return result;}
 public void ConsumeResourceUnits(int quantity){ResourceUnits.RemoveRange(0,Math.Min(quantity,ResourceUnits.Count));}
 public ItemStack Clone()=>new(){Wear=Wear,SurvivorAccuracy=SurvivorAccuracy,LoadedAmmunition=LoadedAmmunition?.Clone(),InstanceId=InstanceId,ItemId=ItemId,Quantity=Quantity,Charges=Charges,Identified=Identified,CampaignGear=CampaignGear,ResourceUnits=ResourceUnits.ToList()};
 public void ValidateOrThrow()
 {
  if(Wear is <0 or >100||Wear>0&&(!WorkshopRules.Durable(Definition)||Quantity!=1))throw new InvalidDataException("Invalid equipment condition.");
  if(LoadedAmmunition is {} loaded){if(Definition.MagazineSize<1||loaded.LoadedAmmunition!=null||loaded.Quantity>Definition.MagazineSize||loaded.Definition.Kind!=ItemKind.Ammunition||loaded.Definition.AmmoType!=Definition.AmmunitionType)throw new InvalidDataException("Invalid loaded ammunition.");loaded.ValidateOrThrow();}
  if(ResourceUnits==null||ResourceUnits.Count>Quantity||ResourceUnits.Any(string.IsNullOrWhiteSpace)||ResourceUnits.Distinct().Count()!=ResourceUnits.Count)throw new InvalidDataException("Invalid resource-unit provenance.");
  var item=Definition;if(Quantity<1||Quantity>Math.Max(1,item.MaxStack))throw new InvalidDataException($"Invalid quantity for {ItemId}.");
  if(item.MaxCharges>0&&(Charges<0||Charges>item.MaxCharges))throw new InvalidDataException($"Invalid charges for {ItemId}.");
  if(item.MaxCharges==0&&Charges!=-1)throw new InvalidDataException($"Unexpected charges for {ItemId}.");
 }
}

public sealed class InventoryState
{
 public int MaxSlots {get;set;}=18;
 public List<ItemStack> Items {get;set;}=[];
 [JsonIgnore] public int UsedSlots=>Items.Count;
 [JsonIgnore] public float TotalWeight=>Items.Sum(x=>x.Weight);
 public int Count(string itemId)=>Items.Where(x=>x.ItemId.Equals(itemId,StringComparison.OrdinalIgnoreCase)).Sum(x=>x.Quantity);
 public bool Contains(string itemId)=>Count(itemId)>0;
 public ItemStack? FindInstance(string instanceId)=>Items.FirstOrDefault(x=>x.InstanceId==instanceId);
 public bool CanAdd(ItemStack stack)
 {
  stack.ValidateOrThrow();var def=stack.Definition;int remaining=stack.Quantity;
  if(def.Stackable)foreach(var existing in Items.Where(x=>x.ItemId.Equals(stack.ItemId,StringComparison.OrdinalIgnoreCase)&&x.Charges==stack.Charges&&x.Identified==stack.Identified))remaining-=Math.Max(0,def.MaxStack-existing.Quantity);
  int newSlots=(int)Math.Ceiling(Math.Max(0,remaining)/(double)Math.Max(1,def.MaxStack));return Items.Count+newSlots<=MaxSlots;
 }
 public bool Add(ItemStack stack)
 {
  stack.ValidateOrThrow();if(!CanAdd(stack))return false;var def=stack.Definition;int remaining=stack.Quantity;
  if(def.Stackable)
  {
   foreach(var existing in Items.Where(x=>x.ItemId.Equals(stack.ItemId,StringComparison.OrdinalIgnoreCase)&&x.Charges==stack.Charges&&x.Identified==stack.Identified))
   {
    int room=Math.Max(0,def.MaxStack-existing.Quantity),take=Math.Min(room,remaining);existing.ResourceUnits.AddRange(stack.ResourceUnits.Skip(stack.Quantity-remaining).Take(take));existing.Quantity+=take;remaining-=take;if(remaining==0)return true;
   }
  }
  while(remaining>0)
  {
   int take=Math.Min(Math.Max(1,def.MaxStack),remaining);Items.Add(new(){Wear=stack.Wear,SurvivorAccuracy=stack.SurvivorAccuracy,InstanceId=remaining==stack.Quantity?stack.InstanceId:"",ItemId=stack.ItemId,Quantity=take,Charges=stack.Charges,Identified=stack.Identified,LoadedAmmunition=stack.LoadedAmmunition?.Clone(),CampaignGear=stack.CampaignGear,ResourceUnits=stack.ResourceUnits.Skip(stack.Quantity-remaining).Take(take).ToList()});remaining-=take;
  }
  return true;
 }
 public bool Remove(string itemId,int quantity=1)
 {
  if(quantity<=0||Count(itemId)<quantity)return false;
  for(int i=Items.Count-1;i>=0&&quantity>0;i--)if(Items[i].ItemId.Equals(itemId,StringComparison.OrdinalIgnoreCase))
  {int take=Math.Min(quantity,Items[i].Quantity);Items[i].ConsumeResourceUnits(take);Items[i].Quantity-=take;quantity-=take;if(Items[i].Quantity<=0)Items.RemoveAt(i);}
  return true;
 }
 public ItemStack? TakeAt(int index,int quantity=1)
 {
  if(index<0||index>=Items.Count||quantity<1)return null;var source=Items[index];int take=Math.Min(quantity,source.Quantity);
  var result=source.Portion(take);source.ConsumeResourceUnits(take);source.Quantity-=take;
  if(source.Quantity<=0)Items.RemoveAt(index);else result.InstanceId="";return result;
 }
 public void ValidateOrThrow(string owner)
 {
  if(MaxSlots<1||Items.Count>MaxSlots)throw new InvalidDataException($"Invalid inventory capacity for {owner}.");
  foreach(var item in Items)item.ValidateOrThrow();
 }
}

public sealed class QuickSpellBinding
{
 public string SourceId {get;set;}="";
 public string SpellId {get;set;}="";
 [JsonIgnore] public bool Empty=>string.IsNullOrWhiteSpace(SourceId)||string.IsNullOrWhiteSpace(SpellId);
 public void Clear(){SourceId="";SpellId="";}
}

public sealed class QuickAccessState
{
 public List<string> WeaponInstanceIds {get;set;}=["","","",""];
 public List<string> ItemInstanceIds {get;set;}=["","",""];
 // IIP33: spell shortcuts are source-aware so multiclass casters can bind the same spell
 // independently from different casting traditions without inventing a second spell list.
 public List<QuickSpellBinding> SpellBindings {get;set;}=[new(),new(),new()];
 public void Normalize(Actor actor)
 {
  WeaponInstanceIds??=[];ItemInstanceIds??=[];SpellBindings??=[];
  while(WeaponInstanceIds.Count<4)WeaponInstanceIds.Add("");if(WeaponInstanceIds.Count>4)WeaponInstanceIds=WeaponInstanceIds.Take(4).ToList();
  while(ItemInstanceIds.Count<3)ItemInstanceIds.Add("");if(ItemInstanceIds.Count>3)ItemInstanceIds=ItemInstanceIds.Take(3).ToList();
  while(SpellBindings.Count<3)SpellBindings.Add(new());if(SpellBindings.Count>3)SpellBindings=SpellBindings.Take(3).ToList();
  var owned=actor.Inventory.Items.Concat(actor.Equipment.All()).Where(x=>!string.IsNullOrWhiteSpace(x.InstanceId)).ToDictionary(x=>x.InstanceId,x=>x,StringComparer.Ordinal);
  for(int i=0;i<WeaponInstanceIds.Count;i++)if(!owned.TryGetValue(WeaponInstanceIds[i],out var stack)||stack.Definition.Kind!=ItemKind.Weapon||!stack.Definition.CanEquipIn(EquipmentSlot.MainHand))WeaponInstanceIds[i]="";
  for(int i=0;i<ItemInstanceIds.Count;i++)if(!owned.TryGetValue(ItemInstanceIds[i],out var stack)||stack.Definition.Kind is not (ItemKind.Consumable or ItemKind.Tool))ItemInstanceIds[i]="";
  for(int i=0;i<SpellBindings.Count;i++)
  {
   var binding=SpellBindings[i]??new();SpellBindings[i]=binding;if(binding.Empty){binding.Clear();continue;}
   if(actor.Character.Spellcasting.All(s=>!s.SourceId.Equals(binding.SourceId,StringComparison.OrdinalIgnoreCase))||!SpellCatalog.TryGet(binding.SpellId,out _))binding.Clear();
  }
 }
 public void ClearInstance(string instanceId)
 {
  for(int i=0;i<WeaponInstanceIds.Count;i++)if(WeaponInstanceIds[i]==instanceId)WeaponInstanceIds[i]="";
  for(int i=0;i<ItemInstanceIds.Count;i++)if(ItemInstanceIds[i]==instanceId)ItemInstanceIds[i]="";
 }
}

public sealed class EquipmentState
{
 // Weapon/Armor/Charm property names are preserved so existing v5 saves deserialize without losing gear.
 public ItemStack? Weapon {get;set;}
 public ItemStack? OffHand {get;set;}
 public ItemStack? Armor {get;set;}
 public ItemStack? Head {get;set;}
 public ItemStack? Neck {get;set;}
 public ItemStack? Cloak {get;set;}
 public ItemStack? Gloves {get;set;}
 public ItemStack? Boots {get;set;}
 public ItemStack? RingLeft {get;set;}
 public ItemStack? RingRight {get;set;}
 public ItemStack? Charm {get;set;}
 public ItemStack? Ammo1 {get;set;}
 public ItemStack? Ammo2 {get;set;}
 public ItemStack? Ammo3 {get;set;}
 [JsonIgnore] public ItemStack? MainHand {get=>Weapon;set=>Weapon=value;}
 public ItemStack? Get(EquipmentSlot slot)=>slot switch
 {
  EquipmentSlot.MainHand=>Weapon,EquipmentSlot.OffHand=>OffHand,EquipmentSlot.Armor=>Armor,EquipmentSlot.Head=>Head,EquipmentSlot.Neck=>Neck,
  EquipmentSlot.Cloak=>Cloak,EquipmentSlot.Gloves=>Gloves,EquipmentSlot.Boots=>Boots,EquipmentSlot.RingLeft=>RingLeft,EquipmentSlot.RingRight=>RingRight,
  EquipmentSlot.Charm=>Charm,EquipmentSlot.Ammo1=>Ammo1,EquipmentSlot.Ammo2=>Ammo2,EquipmentSlot.Ammo3=>Ammo3,_=>null
 };
 public void Set(EquipmentSlot slot,ItemStack? item)
 {
  switch(slot)
  {
   case EquipmentSlot.MainHand:Weapon=item;break;case EquipmentSlot.OffHand:OffHand=item;break;case EquipmentSlot.Armor:Armor=item;break;
   case EquipmentSlot.Head:Head=item;break;case EquipmentSlot.Neck:Neck=item;break;case EquipmentSlot.Cloak:Cloak=item;break;case EquipmentSlot.Gloves:Gloves=item;break;
   case EquipmentSlot.Boots:Boots=item;break;case EquipmentSlot.RingLeft:RingLeft=item;break;case EquipmentSlot.RingRight:RingRight=item;break;case EquipmentSlot.Charm:Charm=item;break;
   case EquipmentSlot.Ammo1:Ammo1=item;break;case EquipmentSlot.Ammo2:Ammo2=item;break;case EquipmentSlot.Ammo3:Ammo3=item;break;
  }
 }
 public IEnumerable<(EquipmentSlot Slot,ItemStack? Item)> SlotPairs()
 {
  foreach(var slot in EquipmentSlots.All)yield return(slot,Get(slot));
 }
 public IEnumerable<ItemStack> All(){foreach(var (_,item) in SlotPairs())if(item!=null)yield return item;}
 [JsonIgnore] public float TotalWeight=>All().Sum(x=>x.Weight);
 [JsonIgnore] public int MagicItemCount=>All().Count(x=>x.Definition.Magical);
 public EquipmentSlot PreferredSlot(Gear item)
 {
  var allowed=item.EquipSlots().ToArray();if(allowed.Length==0)return EquipmentSlot.MainHand;
  var empty=allowed.FirstOrDefault(slot=>Get(slot)==null);return allowed.Contains(empty)&&Get(empty)==null?empty:allowed[0];
 }
 public ItemStack? AmmunitionFor(string ammoType)
 {
  if(string.IsNullOrWhiteSpace(ammoType))return null;
  return new[]{Ammo1,Ammo2,Ammo3}.FirstOrDefault(x=>x!=null&&x.Quantity>0&&x.Definition.AmmoType.Equals(ammoType,StringComparison.OrdinalIgnoreCase));
 }
 public EquipmentSlot? SlotOf(string instanceId)
 {
  foreach(var (slot,item) in SlotPairs())if(item?.InstanceId==instanceId)return slot;return null;
 }
 public void ValidateOrThrow(string owner)
 {
  var seen=new HashSet<string>(StringComparer.Ordinal);
  foreach(var (slot,item) in SlotPairs())if(item!=null)
  {
   item.ValidateOrThrow();if(item.Definition.Kind!=ItemKind.Ammunition&&item.Quantity!=1)throw new InvalidDataException($"Equipped stack quantity must be 1 for {owner}.");
   if(!item.Definition.CanEquipIn(slot))throw new InvalidDataException($"{item.ItemId} is in the wrong equipment slot {EquipmentSlots.Display(slot)} for {owner}.");
   if(!string.IsNullOrWhiteSpace(item.InstanceId)&&!seen.Add(item.InstanceId))throw new InvalidDataException($"{owner} equips duplicate instance {item.InstanceId}.");
  }
  if(Weapon?.Definition.TwoHanded==true&&OffHand!=null)throw new InvalidDataException($"{owner} cannot use off-hand equipment with a two-handed main-hand item.");
 }
}
