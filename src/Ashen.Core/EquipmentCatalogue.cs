using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed class EquipmentProfile
{
 public string Category {get;set;}="campaign";
 public string Status {get;set;}="standard";
 public string Use {get;set;}="supply";
 public string Family {get;set;}="";
 public string Note {get;set;}="";
 public string Quantity {get;set;}="";
 public Dictionary<string,string> SourceCells {get;set;}=[];
 public int Roll {get;set;}
 public int RollEnd {get;set;}
 public int DamageBonus {get;set;}
 public DamageType DamageType {get;set;}=DamageType.Bludgeoning;
 public int Critical {get;set;}=20;
 public int MaxDexterity {get;set;}=10;
 public int SkillPenalty {get;set;}
 public int SpeedFeet {get;set;}=30;
 public bool TwoHanded {get;set;}
 public int NoiseFeet {get;set;}=120;
 public int BurstFeet {get;set;}
 public int SaveDc {get;set;}=12;
 public int Charges {get;set;}
 public string Material {get;set;}="";
 public string PackAmmoId {get;set;}="";
 public int PackQuantity {get;set;}
}
public static class EquipmentRules
{
 public static bool MaterialMatches(ItemStack stack,string id)=>stack.ItemId==id||stack.Definition.Equipment?.Material==id;
 public static int Speed(Actor a)
 {
  int speed=a.Character.SpeedFeet;if(!a.UsesEquipmentCatalogue||a.Character.HasCondition("class-unstoppable"))return speed;
  if(a.Equipment.Armor?.Definition.Equipment is {Category:"armour"} armor)speed=Math.Min(speed,armor.SpeedFeet);
  if(a.CarriedWeight>a.CarryingCapacity*.75f&&!a.ClassIs("strong")&&!a.ClassIs("tough",9))speed=Math.Max(5,speed-10);
  return speed;
 }
 public static int ArmorClass(Actor a)
 {
  if(a.UsesEquipmentCatalogue&&a.Equipment.Armor?.Definition.Equipment is {Category:"armour"} p)return 10+a.Equipment.Armor.Definition.EffectiveArmorModifier+Math.Min(a.Dex,p.MaxDexterity)+a.Equipment.All().Where(s=>s!=a.Equipment.Armor).Sum(s=>s.Definition.EffectiveArmorModifier);
  return a.BaseAc+a.EquipmentArmorBonus;
 }
 public static int CheckPenalty(Actor a,Skill skill)=>a.UsesEquipmentCatalogue&&a.Equipment.Armor?.Definition.Equipment is {Category:"armour"} p&&skill is Skill.Acrobatics or Skill.Athletics or Skill.SleightOfHand or Skill.Stealth or Skill.Lockpicking?p.SkillPenalty:0;
 public static void Validate(CampaignDefinition c,Action<string,string,string> error)
 {
  if(c.EquipmentCatalogue&&(c.SchemaVersion<16||!c.NoiseAwareness||c.Infrastructure==null||!c.SurvivorClasses))error("schema","equipmentCatalogue","Equipment requires schema 16 and the established survival/class/infrastructure foundation.");
  foreach(var i in c.Items.Where(i=>i.Equipment!=null))
  {
   var p=i.Equipment!;
   if(!c.EquipmentCatalogue||p.Category is not ("commodity" or "melee" or "ranged" or "armour" or "explosive" or "ammunition" or "campaign")||p.Status is not ("standard" or "scripted" or "reference")||p.Use is not ("supply" or "attack" or "throw" or "blast" or "mine" or "flash" or "smoke" or "light" or "purify" or "ammo-pack" or "lockpick")||p.Note==null||p.Family==null||p.SourceCells==null||p.SourceCells.Count>20||p.SourceCells.Any(k=>k.Key==null||k.Value==null||k.Value.Length>2000))error("equipment",i.Id,"Invalid equipment profile, opt-in, category or supported use.");
   if(p.Critical is <18 or >20||p.MaxDexterity is <0 or >20||p.SkillPenalty is < -20 or >0||p.SpeedFeet is <5 or >100||p.SpeedFeet%5!=0||p.NoiseFeet is <5 or >200||p.BurstFeet is <0 or >1000||p.SaveDc is <1 or >40||p.Charges is <0 or >100||p.DamageBonus is < -20 or >100||!Enum.IsDefined(p.DamageType)||p.Roll is <0 or >100||p.RollEnd<p.Roll||p.RollEnd>100||p.PackQuantity is <0 or >1000)error("equipment",i.Id,"Invalid equipment numeric bounds.");
   if(p.Material!=""&&!c.Items.Any(t=>t.Id==p.Material&&t.Kind==ItemKind.Misc))error("equipment",i.Id,"Unknown maintenance-material equivalent.");
   if(p.Use=="ammo-pack"&&(!c.Items.Any(t=>t.Id==p.PackAmmoId&&t.Kind==ItemKind.Ammunition)||p.PackQuantity<1||c.Items.Any(t=>t.Id==p.PackAmmoId&&p.PackQuantity>t.MaxStack)))error("equipment",i.Id,"Ammunition packs require finite compatible rounds.");
   if(p.Status!="standard"&&p.Use!="supply")error("equipment",i.Id,"Scripted/reference entries must not advertise executable generic powers.");
   if(p.Status=="reference"&&c.Areas.SelectMany(a=>a.Features).Any(f=>f.Contents.Any(s=>s.ItemId==i.Id)))error("equipment",i.Id,"GM roll instructions are references, not physical loot.");
  }
 }
}
public sealed partial class World
{
 [JsonIgnore] public bool EquipmentEnabled=>Campaign?.EquipmentCatalogue==true;
 public IEnumerable<CampaignItem> EquipmentEntries(string category="all",string query="")=>Campaign==null?Enumerable.Empty<CampaignItem>():Campaign.Items.Where(i=>(category=="all"||(i.Equipment?.Category??"campaign")==category)&&(query==""||i.Name.Contains(query,StringComparison.OrdinalIgnoreCase)||i.Id.Contains(query,StringComparison.OrdinalIgnoreCase))).OrderBy(i=>i.Equipment?.Category??"campaign").ThenBy(i=>i.Id);
 public string EquipmentSummary(CampaignItem i)
 {
  var p=i.Equipment;if(p?.Status=="reference")return "GM roll instruction; not a physical item. Literal source cells are preserved.";if(p?.Status=="scripted")return $"Scripted scenario item; generic activation disabled. {i.Weight:0.##} lb; {i.ResourceSv} SV per printed unit.";string stats=i.Kind==ItemKind.Weapon?$"{i.DamageDice}d{i.DamageDie}{(p?.DamageBonus>0?"+"+p.DamageBonus:"")} {p?.DamageType}; range {i.RangeFeet} ft; critical {p?.Critical??20}+":i.Kind==ItemKind.Armor?$"AC +{i.ArmorBonus}; max DEX {p?.MaxDexterity}; speed {p?.SpeedFeet} ft; checks {p?.SkillPenalty}":p?.Use is "blast" or "mine" or "throw"?$"{i.DamageDice}d{i.DamageDie} {p.DamageType}; radius {p.BurstFeet} ft; save {p.SaveDc}":i.SurvivalUse!=""?$"Daily survival: {i.SurvivalUse}; treatments {i.TreatmentUses}":"Finite physical supply / scenario resource";
  return stats+$" | {i.Weight:0.##} lb; stack {i.MaxStack}; {i.ResourceSv} SV per unit.";
 }
 public string EquipmentWeaponFamily(Actor a)=>a.Equipment.Weapon?.Definition.Equipment?.Family??"";
}
