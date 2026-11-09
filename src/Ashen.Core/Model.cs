using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
namespace Ashen.Core;

public readonly record struct Cell(int X,int Y)
{
 public override string ToString()=>$"({X}, {Y})";
 [JsonIgnore] public WorldPoint Center=>new(X+.5f,Y+.5f);
 public int Distance(Cell b)=>Math.Abs(X-b.X)+Math.Abs(Y-b.Y);
 public IEnumerable<Cell> Neighbors(){yield return new(X+1,Y);yield return new(X,Y+1);yield return new(X-1,Y);yield return new(X,Y-1);}
}
public enum Mode { Explore, Combat, Defeat, Victory }
public enum AttackKind { Basic, Special }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TacticsProfile { Balanced, Aggressive, Defensive, Support, Ranged }
public sealed partial class Actor
{
 public string Id {get;set;}="";public string Name {get;set;}="";
 // CreatureKind is the non-player stat-block/archetype identity. Player class identity lives in Character.Classes.
 public string CreatureKind {get;set;}="";
 public int Portrait {get;set;}
 // IIP47: player identity is durable campaign state rather than UI-only creation metadata.
 public bool PlayerCreated {get;set;}
 public string Pronouns {get;set;}="they/them";
 public string VoiceSet {get;set;}="";
 public bool DialogueTarget {get;set;}
 WorldPoint position;
 // Pos remains a save migration / map-index adapter. Position is authoritative.
 public Cell Pos {get=>position.Cell;set=>position=value.Center;}
 public WorldPoint Position {get=>position;set=>position=value;}
 public bool FacingLeft {get;set;}
 [JsonIgnore] public float WalkDistance {get;set;}
 [JsonIgnore] public float Motion {get;set;}
 public CharacterSheet Character {get;set;}=new();
 // IIP28: durable journey history shown on Character Information. Old saves default cleanly to zero.
 public CharacterStatistics Statistics {get;set;}=new();
 public ShelterDutyState ShelterWork {get;set;}=new();
 public SurvivorTraining Training {get;set;}=new();
 [JsonIgnore] public bool UsesSurvivorClasses {get;internal set;}
 public SurvivalNeeds Needs {get;set;}=new();
 [JsonIgnore] public bool UsesDailySurvival {get;internal set;}
 [JsonIgnore] public int EffectiveSpeedFeet=>Math.Max(5,(EquipmentRules.Speed(this)-(WearsUpper25Gear("Steel-Toed Boots")?5:0)-(Character.HasCondition("final-slow")?5:0)+(Character.HasCondition("chapter-berserk")?5:0)+(Character.HasCondition("ext30-energy")?5:0)+(Character.HasCondition("ext31-board")?10:0)+(Character.HasCondition("book-bike")||HasLowerGear("running shoes")||WearsUpper25Gear("High-End Running Shoes")||WearsUpper25Gear("Athletic Running Shoes")?5:0)-(UsesDailySurvival&&!Character.HasCondition("class-unstoppable")?(Character.HasCondition("upper-energy")||Character.HasCondition("upper27-energy")?0:Needs.Exhaustion)*5:0))/(ClassIs("agile")&&Character.HasCondition("hidden")?2:1));
 public bool ClassIs(string id,int level=1)=>UsesSurvivorClasses&&Training.ClassId==id&&TotalLevel>=level;
 [JsonIgnore] public bool UsesEquipmentCatalogue {get;internal set;}
 public LocalAwareness Awareness {get;set;}=new();
 public bool Enemy {get;set;} public int Group {get;set;} public int Initiative {get;set;}
 // IIP21: serialized companion tactics are journey state. Global automation remains an independent preference.
 public TacticsProfile Tactics {get;set;}=TacticsProfile.Balanced;
 public ActionEconomyState Economy {get;set;}=new();
 [JsonIgnore] public float Move {get=>Economy.MovementRemainingFeet/5f;set=>Economy.MovementRemainingFeet=Math.Max(0,value*5f);}
 public InventoryState Inventory {get;set;}=new();
 public EquipmentState Equipment {get;set;}=new(){Weapon=new(){ItemId="sword",Quantity=1,Charges=-1},Armor=new(){ItemId="mail",Quantity=1,Charges=-1}};
 public QuickAccessState QuickAccess {get;set;}=new();
 // IIP24 keeps the old serialized Weapon/Armor/Charm surface compatible while expanding to the complete paper-doll slot set.
 [JsonIgnore] public string Weapon {get=>Equipment.Weapon?.ItemId??"sword";set=>Equipment.Weapon=string.IsNullOrWhiteSpace(value)?null:new(){ItemId=value,Quantity=1,Charges=-1};}
 [JsonIgnore] public string Armor {get=>Equipment.Armor?.ItemId??"mail";set=>Equipment.Armor=string.IsNullOrWhiteSpace(value)?null:new(){ItemId=value,Quantity=1,Charges=-1};}
 [JsonIgnore] public string Charm {get=>Equipment.Charm?.ItemId??"";set=>Equipment.Charm=string.IsNullOrWhiteSpace(value)?null:new(){ItemId=value,Quantity=1,Charges=-1};}
 [JsonIgnore] public float CarriedWeight=>Inventory.TotalWeight+Equipment.TotalWeight;
 [JsonIgnore] public float CarryingCapacity=>Math.Max(1,Character.Abilities.Strength*15f*(ClassIs("strong")||ClassIs("tough",9)?2:1)+(HasUpper25Gear("Handmade Leather Satchel")||HasUpper25Gear("Collector Backpack")||HasUpper25Gear("Oven Cache Backpack")?10:0)+(HasLowerGear("hiking backpack")||HasLowerGear("tactical backpack")||HasUpper25Gear("Travel Backpack")||HasUpper25Gear("Shopping Cart")?5:0));
 [JsonIgnore] public string EncumbranceState=>CarriedWeight>CarryingCapacity?"OVERLOADED":CarriedWeight>CarryingCapacity*.75f?"HEAVY":"LIGHT";
 [JsonIgnore] public int EquippedMagicItems=>Equipment.MagicItemCount;
 public void Face(WorldPoint goal){float dx=goal.X-Position.X;if(Math.Abs(dx)>.002f)FacingLeft=dx<0;}
 [JsonIgnore] public bool Dead=>Character.HasCondition("dead");
 [JsonIgnore] public bool Unconscious=>!Dead&&(Hp<=0||Character.HasCondition("unconscious"));
 [JsonIgnore] public bool Alive=>Hp>0&&!Dead;
 [JsonIgnore] public bool Concentrating=>Character.Concentration!=null||Character.HasConditionTag("concentration");
 [JsonIgnore] public bool CanLevelUp=>Character.CanLevelUp;
 [JsonIgnore] public int Hp {get=>Character.HitPoints.Current;set=>Character.HitPoints.Current=value;}
 [JsonIgnore] public int MaxHp {get=>Character.HitPoints.Maximum;set=>Character.HitPoints.Maximum=value;}
 [JsonIgnore] public int BaseAc {get=>Character.Defenses.ArmorClassBase;set=>Character.Defenses.ArmorClassBase=value;}
 [JsonIgnore] public int Dex=>AbilityModifier(Ability.Dexterity);
 [JsonIgnore] public bool UsesModernCombat {get;internal set;}
 [JsonIgnore] public bool ModernSurvivor=>UsesModernCombat&&!Enemy&&Character.PrimaryClassName=="Survivor";
 [JsonIgnore] public int Bonus=>ModernSurvivor?(Equipment.Weapon?.Definition.MagazineSize>0?Dex:AbilityModifier(Ability.Strength))+(SurvivorClassesRules.WeaponProficient(this)?ProficiencyBonus:0):Character.MainAttack.AttackBonus(Character);
 [JsonIgnore] public int Die=>Equipment.Weapon?.Definition.WeaponDamageDieSize??Character.MainAttack.DamageDieSize;
 [JsonIgnore] public int Range=>Equipment.Weapon?.Definition.WeaponRangeFeet is >0 and var feet?feet/5:Character.MainAttack.RangeTiles;
 [JsonIgnore] public int DamageDice=>Equipment.Weapon?.Definition.WeaponDamageDice is >0 and var dice?dice:Character.MainAttack.DamageDice;
 [JsonIgnore] public DamageType AttackDamageType=>Equipment.Weapon?.Definition.Equipment is {} profile?profile.DamageType:Equipment.Weapon?.Definition.MagazineSize>0?DamageType.Piercing:Character.MainAttack.DamageType;
 [JsonIgnore] public int Mana {get=>SignatureResource?.Current??0;set{var r=SignatureResource;if(r!=null)r.Current=Math.Clamp(value,0,r.Maximum);}}
 [JsonIgnore] public int MaxMana {get=>SignatureResource?.Maximum??0;set{var r=SignatureResource;if(r!=null){r.Maximum=Math.Max(0,value);r.Current=Math.Min(r.Current,r.Maximum);}}}
 [JsonIgnore] public string Class=>Enemy?(string.IsNullOrWhiteSpace(CreatureKind)?Character.SubclassName:CreatureKind):UsesSurvivorClasses&&Training.ClassId!=""?SurvivorClassesRules.Find(Training.ClassId)?.Name??"Survivor":Character.PrimaryClassName;
 [JsonIgnore] public int TotalLevel=>Character.TotalLevel;
 [JsonIgnore] public int ProficiencyBonus=>Character.ProficiencyBonus;
 [JsonIgnore] public float SpeedTiles=>EffectiveSpeedFeet/5f;
 [JsonIgnore] public int EquipmentArmorBonus=>Equipment.All().Sum(x=>x.Definition.EffectiveArmorModifier);
 [JsonIgnore] public int Ac=>(Character.HasCondition("upper26-jump")?2:0)-(Character.HasCondition("upper26-groove")?2:0)+(WearsUpper25Gear("Glassproof Steel-Toed Boots")?1:0)+(Character.HasCondition("upper-reinforced")?1:0)+(WearsUpper25Gear("Steel-Toed Boots")?1:0)+SurvivorClassesRules.ArmorBonus(this)+EquipmentRules.ArmorClass(this)+(Character.HasCondition("shielded")?5:0);
 [JsonIgnore] public int GearAttackBonus=>(Equipment.Weapon?.ItemId=="item.lower.39.loot.5.0"?-1:0)+(Equipment.Weapon?.ItemId=="item.lower.30.loot.3.0"?1:0)+(Equipment.Weapon?.Definition.EffectiveAttackModifier??0)+Equipment.All().Where(x=>x!=Equipment.Weapon&&x.Definition.Kind is not (ItemKind.Weapon or ItemKind.Ammunition)).Sum(x=>x.Definition.EffectiveAttackModifier);
 [JsonIgnore] public int GearDamageBonus=>(Equipment.Weapon?.Definition.EffectiveDamageModifier??0)+Equipment.All().Where(x=>x!=Equipment.Weapon&&x.Definition.Kind is not (ItemKind.Weapon or ItemKind.Ammunition)).Sum(x=>x.Definition.EffectiveDamageModifier);
 [JsonIgnore] public int GearBonus=>GearAttackBonus;
 [JsonIgnore] public int AttackBonus=>(Character.HasCondition("upper26-groove")?2:0)-(Character.HasCondition("upper26-mold")?1:0)+Bonus+GearAttackBonus-(UsesDailySurvival?Needs.Exhaustion:0);
 [JsonIgnore] public int DamageBonus=>(ModernSurvivor?(Equipment.Weapon?.Definition.MagazineSize>0?Dex:AbilityModifier(Ability.Strength)):Character.MainAttack.DamageBonus(Character))+GearDamageBonus;
 [JsonIgnore] public string RequiredAmmunitionType=>Equipment.Weapon?.Definition.AmmunitionType??"";
 [JsonIgnore] public bool RequiresAmmunition=>!string.IsNullOrWhiteSpace(RequiredAmmunitionType);
 [JsonIgnore] public ItemStack? EquippedAmmunition=>Equipment.Weapon?.Definition.MagazineSize>0?Equipment.Weapon.LoadedAmmunition:Equipment.AmmunitionFor(RequiredAmmunitionType);
 [JsonIgnore] public int EquippedAmmunitionCount=>EquippedAmmunition?.Quantity??0;
 [JsonIgnore] public ResourcePool? SignatureResource=>Character.Resource("signature");
 [JsonIgnore] public FeatureEntry? SignatureFeature=>Character.FeatureWithTag("signature");
 [JsonIgnore] public FeatureEntry? HealingFeature=>Character.FeatureWithTag("healing");
 [JsonIgnore] public bool CanHeal=>HealingFeature!=null;
 [JsonIgnore] public string Special=>SignatureFeature?.Name??"Special Ability";
 [JsonIgnore] public int SpecialRange=>Math.Max(1,(SignatureFeature?.Mechanics.RangeFeet??0)>0?(int)MathF.Ceiling(SignatureFeature!.Mechanics.RangeFeet/5f):Range);
 [JsonIgnore] public bool SpecialSplashes=>SignatureFeature?.Mechanics.Splash==true;
 [JsonIgnore] public int OpticalDexBonus=>(Character.HasCondition("final-optical")||Character.HasCondition("upper26-dexdrink"))?((int)MathF.Floor((Character.Abilities.Dexterity+(Character.HasCondition("final-optical")?1:0)+(Character.HasCondition("upper26-dexdrink")?1:0)-10)/2f)-Character.AbilityModifier(Ability.Dexterity)):0;
 public int AbilityModifier(Ability ability)=>Character.AbilityModifier(ability)+(ability==Ability.Dexterity?OpticalDexBonus:0)+(ability==Ability.Strength&&Character.HasCondition("final-preworkout")?1:0);
 public int SaveModifier(Ability ability)=>Character.SaveModifier(ability)+(ability==Ability.Dexterity?OpticalDexBonus:0)+(ability==Ability.Strength&&Character.HasCondition("final-preworkout")?1:0);
 public bool HasLowerGear(string name)=>Inventory.Items.Concat(Equipment.All()).Any(i=>i.Quantity>0&&i.ItemId.StartsWith("item.lower.")&&i.Definition.Name.Contains(name,StringComparison.OrdinalIgnoreCase));
 public int SkillModifier(Skill skill)=>LowerGearSkill(skill)+Character.SkillModifier(skill)+EquipmentRules.CheckPenalty(this,skill)+(skill==Skill.Survival&&Character.HasCondition("book-studied")?1:0)+(skill==Skill.Perception&&Character.HasCondition("book-warning")?2:0)+(skill==Skill.Perception&&Character.HasCondition("book-eyedrops")?1:0)+(skill==Skill.Stealth&&Character.HasCondition("book-glasses")?1:0);
 public int AdjustDamage(int amount,DamageType type)=>Character.Defenses.AdjustDamage(amount,type);
}
public sealed class Feature
{
 public string Id {get;set;}="";public string CampaignEventId {get;set;}="";
 public string Kind {get;set;}="";public string Name {get;set;}="";public Cell Pos {get;set;} public bool Used {get;set;} public string Loot {get;set;}="";
 // IIP16 semantic interaction metadata. Defaults preserve every pre-IIP16 save/content file.
 public bool Locked {get;set;} public bool DetectedTrap {get;set;} public bool Disarmable {get;set;}
 // IIP17 discovery metadata. Hidden content is excluded from render/highlight/input until found.
 public bool Hidden {get;set;}
 // IIP38: dialogue-target features remain ordinary world interactions while carrying authored conversation identity.
 public string DialogueId {get;set;}="";
 public int DialoguePortrait {get;set;}
 // IIP41/IIP42: persistent service features may bind either merchant trade or a non-trade service provider.
 public string StoreId {get;set;}="";
 public string ServiceId {get;set;}="";
 // IIP22: world containers own durable contents/currency even before IIP26 adds the transfer panel.
 public InventoryState Contents {get;set;}=new(){MaxSlots=32};
 public int Gold {get;set;}
}
public sealed class GroundPile
{
 public Cell Pos {get;set;}
 public InventoryState Inventory {get;set;}=new(){MaxSlots=24};
}

public sealed partial class World
{
 public const int Width=46,Height=32,MaxPartySize=6;
 public const int MinimumSaveVersion=1,CurrentSaveVersion=41;
 public int Version {get;set;}=41;
 public List<Cell> Floors {get;set;}=[];public List<Cell> Explored {get;set;}=[];
 public List<Actor> Actors {get;set;}=[];public List<Feature> Features {get;set;}=[];
 // Gold and camp supplies remain shared party resources; physical items belong to actors/containers.
 public InventoryState SharedStorage {get;set;}=new(){MaxSlots=120};
 // IIP23: spatial ground piles back the Inventory screen's GROUND pane.
 public List<GroundPile> GroundPiles {get;set;}=[];
 public int NextItemInstanceNumber {get;set;}=1;
 public List<string> Log {get;set;}=[];
 public List<MessageEntry> MessageHistory {get;set;}=[]; public int NextMessageSequence {get;set;}=1;
 public Mode Mode {get;set;} public int Selected {get;set;} public int Gold {get;set;}=26;
 public int Rations {get;set;}=3;public int Round {get;set;} public int Turn {get;set;} public int CombatGroup {get;set;}=-1;
 public List<string> Order {get;set;}=[];
 // IIP3: party order is persistent game state and is deliberately independent of Actors list order.
 public List<string> PartyOrder {get;set;}=[];
 public int Cleared {get;set;} public int Level {get;set;}=1;
 public uint RandomState {get;set;}=12413;
 [JsonIgnore] public HashSet<Cell> Visible {get;}=[];
 [JsonIgnore] public Dictionary<string,Queue<WorldPoint>> Routes {get;}=[];
 [JsonIgnore] public Action<string>? Sound;
 [JsonIgnore] public IEnumerable<Actor> Party=>OrderedParty();
 [JsonIgnore] public int PartyCount=>Party.Count();
 [JsonIgnore] public Actor Chosen
 {
  get
  {
   var party=Party.ToList();
   if(party.Count==0)throw new InvalidOperationException("World has no party members.");
   Selected=Math.Clamp(Selected,0,party.Count-1);return party[Selected];
  }
 }
 [JsonIgnore] public Actor? Active=>Mode==Mode.Combat&&Order.Count>0&&Turn>=0&&Turn<Order.Count?Actors.FirstOrDefault(a=>a.Id==Order[Turn]):null;
 public int PartyIndex(string actorId){int i=0;foreach(var a in Party){if(a.Id==actorId)return i;i++;}return -1;}
 public int PartyIndex(Actor actor)=>PartyIndex(actor.Id);
 public Actor? PartyAt(int index)=>index>=0&&index<PartyCount?Party.ElementAt(index):null;
 // IIP7: portrait order is semantic game state. Slot zero is the nominal party leader;
 // availability fallbacks never rewrite PartyOrder, so reordering is the only ordinary way to change leadership.
 [JsonIgnore] public Actor PartyLeader=>Party.First();
 [JsonIgnore] public Actor? AvailablePartyLeader=>Party.FirstOrDefault(a=>a.Alive);
 public Actor? ResolveDialogueSpeaker(string? scriptedActorId=null)
 {
  if(!string.IsNullOrWhiteSpace(scriptedActorId))
  {
   var scripted=Party.FirstOrDefault(a=>a.Id==scriptedActorId);
   if(scripted?.Alive==true)return scripted;
  }
  return AvailablePartyLeader;
 }
 public Actor[] FormationMembers(IEnumerable<string> actorIds,string? scriptedLeaderId=null)
 {
  var selected=actorIds.ToHashSet(StringComparer.Ordinal);var ordered=Party.Where(a=>a.Alive&&selected.Contains(a.Id)).ToList();
  if(!string.IsNullOrWhiteSpace(scriptedLeaderId))
  {
   int index=ordered.FindIndex(a=>a.Id==scriptedLeaderId);
   if(index>0){var scripted=ordered[index];ordered.RemoveAt(index);ordered.Insert(0,scripted);}
  }
  return ordered.ToArray();
 }
 public Actor? FormationLeader(IEnumerable<string> actorIds,string? scriptedLeaderId=null)=>FormationMembers(actorIds,scriptedLeaderId).FirstOrDefault();
 public void NormalizePartyOrder()
 {
  var valid=Actors.Where(a=>!a.Enemy).Select(a=>a.Id).ToHashSet(StringComparer.Ordinal);
  var seen=new HashSet<string>(StringComparer.Ordinal);
  PartyOrder=PartyOrder.Where(id=>valid.Contains(id)&&seen.Add(id)).ToList();
  foreach(var a in Actors.Where(a=>!a.Enemy))if(seen.Add(a.Id))PartyOrder.Add(a.Id);
  if(PartyOrder.Count>0)Selected=Math.Clamp(Selected,0,PartyOrder.Count-1);else Selected=0;
 }
 public bool MovePartySlot(int from,int to)
 {
  NormalizePartyOrder();if(from<0||from>=PartyOrder.Count||to<0||to>=PartyOrder.Count||from==to)return false;
  string chosen=Chosen.Id,id=PartyOrder[from];PartyOrder.RemoveAt(from);PartyOrder.Insert(to,id);Selected=Math.Max(0,PartyIndex(chosen));return true;
 }
 IEnumerable<Actor> OrderedParty()
 {
  var seen=new HashSet<string>(StringComparer.Ordinal);
  foreach(var id in PartyOrder){var a=Actors.FirstOrDefault(x=>!x.Enemy&&x.Id==id);if(a!=null&&seen.Add(a.Id))yield return a;}
  foreach(var a in Actors.Where(a=>!a.Enemy))if(seen.Add(a.Id))yield return a;
 }
 [JsonIgnore] public bool HasRelic=>Party.Any(a=>a.Inventory.Contains("relic")||a.Equipment.All().Any(x=>x.ItemId=="relic"))||SharedStorage.Contains("relic");
 public int ItemCount(Actor actor,string itemId)=>actor.Inventory.Count(itemId);
 public ItemStack? OwnedItem(Actor actor,string instanceId)=>actor.Inventory.Items.Concat(actor.Equipment.All()).FirstOrDefault(x=>x.InstanceId==instanceId);
 public GroundPile? GroundPileAt(Cell pos)=>GroundPiles.FirstOrDefault(p=>p.Pos==pos);
 public InventoryState GroundInventoryAt(Cell pos,bool create=false)
 {
  var pile=GroundPileAt(pos);if(pile!=null)return pile.Inventory;if(!create)return new InventoryState{MaxSlots=24};
  pile=new(){Pos=pos,Inventory=new(){MaxSlots=24}};GroundPiles.Add(pile);return pile.Inventory;
 }
 void RemoveEmptyGroundPiles()=>GroundPiles.RemoveAll(p=>p.Inventory.Items.Count==0);
 public bool AssignQuickWeapon(Actor actor,string instanceId,int slot)
 {
  if(slot<0||slot>=4)return false;var stack=OwnedItem(actor,instanceId);if(stack?.Definition.Kind!=ItemKind.Weapon||!stack.Definition.CanEquipIn(EquipmentSlot.MainHand))return false;actor.QuickAccess.Normalize(actor);actor.QuickAccess.WeaponInstanceIds[slot]=instanceId;Message(MessageCategory.System,$"{actor.Name} assigns {stack.Definition.Name} to quick weapon {slot+1}.");return true;
 }
 public bool AssignQuickItem(Actor actor,string instanceId,int slot)
 {
  if(slot<0||slot>=3)return false;var stack=OwnedItem(actor,instanceId);if(stack==null||stack.Definition.Kind is not (ItemKind.Consumable or ItemKind.Tool))return false;actor.QuickAccess.Normalize(actor);actor.QuickAccess.ItemInstanceIds[slot]=instanceId;Message(MessageCategory.System,$"{actor.Name} assigns {stack.Definition.Name} to quick item {slot+1}.");return true;
 }
 public bool AssignQuickSpell(Actor actor,string sourceId,string spellId,int slot)
 {
  if(slot<0||slot>=3)return false;actor.QuickAccess.Normalize(actor);var source=actor.Character.Spellcasting.FirstOrDefault(s=>s.SourceId.Equals(sourceId,StringComparison.OrdinalIgnoreCase));
  if(source==null||!SpellCatalog.TryGet(spellId,out var spell)||!SpellcastingRules.SpellAvailable(actor.Character,source,spell.Id))return false;
  actor.QuickAccess.SpellBindings[slot]=new(){SourceId=source.SourceId,SpellId=spell.Id};Message(MessageCategory.System,$"{actor.Name} assigns {spell.Name} to quick spell {slot+1}.");return true;
 }
 public ItemStack? QuickWeapon(Actor actor,int slot){actor.QuickAccess.Normalize(actor);return slot>=0&&slot<actor.QuickAccess.WeaponInstanceIds.Count?OwnedItem(actor,actor.QuickAccess.WeaponInstanceIds[slot]):null;}
 public ItemStack? QuickItem(Actor actor,int slot){actor.QuickAccess.Normalize(actor);return slot>=0&&slot<actor.QuickAccess.ItemInstanceIds.Count?OwnedItem(actor,actor.QuickAccess.ItemInstanceIds[slot]):null;}
 public QuickSpellBinding? QuickSpell(Actor actor,int slot){actor.QuickAccess.Normalize(actor);return slot>=0&&slot<actor.QuickAccess.SpellBindings.Count&&!actor.QuickAccess.SpellBindings[slot].Empty?actor.QuickAccess.SpellBindings[slot]:null;}
 public bool CanActivateQuickWeapon(Actor actor,int slot,out string reason)
 {
  reason="";if(!actor.Alive){reason="An unconscious or dead character cannot switch weapons.";return false;}if(Mode is Mode.Defeat or Mode.Victory){reason="Weapons cannot be switched after the encounter has ended.";return false;}
  if(Mode==Mode.Combat&&(Active!=actor||actor.Enemy)){reason="Only the active companion can switch a combat quick weapon.";return false;}var target=QuickWeapon(actor,slot);if(target==null){reason=$"Quick weapon {slot+1} is not assigned.";return false;}
  if(actor.Equipment.MainHand?.InstanceId==target.InstanceId)return true;if(!CanEquipInSlot(actor,target,EquipmentSlot.MainHand,out reason))return false;
  if(ModernTacticalCombat&&(ReactionInterruptActive||IsMoving(actor)||Mode==Mode.Combat&&!CanUseBonusAction(actor))){reason="Finish moving; switching weapons costs a Bonus Action on your turn.";return false;}
  var where=actor.Equipment.SlotOf(target.InstanceId);if(where is {} equipmentSlot&&equipmentSlot!=EquipmentSlot.OffHand){reason="The assigned weapon is already occupying another incompatible equipment slot.";return false;}
  return true;
 }
 public bool ActivateQuickWeapon(Actor actor,int slot)
 {
  if(!CanActivateQuickWeapon(actor,slot,out var reason)){LogSystem(reason,true);return false;}var target=QuickWeapon(actor,slot)!;if(actor.Equipment.MainHand?.InstanceId==target.InstanceId){Message(MessageCategory.System,$"{actor.Name} readies {target.Definition.Name}.");return true;}
  var old=actor.Equipment.MainHand;var sourceSlot=actor.Equipment.SlotOf(target.InstanceId);
  if(sourceSlot==EquipmentSlot.OffHand)
  {
   if(old!=null&&old.Definition.CanEquipIn(EquipmentSlot.OffHand)&&CanEquipInSlot(actor,old,EquipmentSlot.OffHand,out _)){actor.Equipment.MainHand=target;actor.Equipment.OffHand=old;}
   else{if(old!=null&&!actor.Inventory.CanAdd(old)){LogSystem("No backpack slot is available for the replaced weapon.",true);return false;}actor.Equipment.OffHand=null;actor.Equipment.MainHand=target;if(old!=null)actor.Inventory.Add(old);}
  }
  else
  {
   int index=actor.Inventory.Items.FindIndex(x=>x.InstanceId==target.InstanceId);if(index<0){LogSystem("The assigned weapon is no longer carried by this character.",true);return false;}
   var moved=actor.Inventory.TakeAt(index,1);if(moved==null)return false;actor.Equipment.MainHand=moved;if(old!=null&&!actor.Inventory.Add(old)){actor.Equipment.MainHand=old;actor.Inventory.Add(moved);LogSystem("No backpack slot is available for the replaced weapon.",true);return false;}
  }
  if(ModernTacticalCombat&&Mode==Mode.Combat)SpendBonusAction(actor,"Switch weapon");
  AssignMissingItemIds();actor.QuickAccess.Normalize(actor);Message(MessageCategory.System,$"{actor.Name} switches to {target.Definition.Name}.");return true;
 }
 public int TotalItemCount(string itemId)=>Party.Sum(a=>a.Inventory.Count(itemId))+SharedStorage.Count(itemId);
 string NextItemId()=> $"itm-{NextItemInstanceNumber++:D6}";
 ItemStack CreateItemStack(string itemId,int quantity=1)
 {
  var def=ResolveItemDefinition(itemId);return new(){InstanceId=NextItemId(),ItemId=def.Id,CampaignGear=Campaign==null?null:def,Quantity=Math.Clamp(quantity,1,Math.Max(1,def.MaxStack)),Charges=def.MaxCharges>0?def.MaxCharges:-1,Identified=def.IdentifiedByDefault};
 }
 void AssignMissingItemIds()
 {
  var seen=new HashSet<string>(StringComparer.Ordinal);
  IEnumerable<ItemStack> AllItems()
  {
   foreach(var a in Actors.Concat(ShelterResidents.Select(r=>r.Actor))){foreach(var i in a.Inventory.Items)yield return i;foreach(var i in a.Equipment.All())yield return i;}
   if(Campaign!=null)foreach(var area in StoredTravelAreas)
   {foreach(var a in area.Actors){foreach(var i in a.Inventory.Items)yield return i;foreach(var i in a.Equipment.All())yield return i;}foreach(var f in area.Features)foreach(var i in f.Contents.Items)yield return i;foreach(var pile in area.GroundPiles)foreach(var i in pile.Inventory.Items)yield return i;}
   foreach(var i in SharedStorage.Items)yield return i;foreach(var f in Features)foreach(var i in f.Contents.Items)yield return i;foreach(var pile in GroundPiles)foreach(var i in pile.Inventory.Items)yield return i;foreach(var store in Stores??[])foreach(var i in store.Stock.Items)yield return i;foreach(var i in TraderStock.Items)yield return i;
  }
  foreach(var item in AllItems().SelectMany(s=>s.LoadedAmmunition is {} ammo?new[]{s,ammo}:new[]{s}))
  {
   if(string.IsNullOrWhiteSpace(item.InstanceId))item.InstanceId=NextItemId();
   if(!seen.Add(item.InstanceId))throw new InvalidDataException($"Duplicate item instance id {item.InstanceId}.");
  }
  foreach(var actor in Actors.Concat(ShelterResidents.Select(r=>r.Actor)))actor.QuickAccess.Normalize(actor);
 }
 public bool CanCarry(Actor actor,ItemStack stack,out string reason)
 {
  if(!actor.Inventory.CanAdd(stack)){reason="No backpack slot is available.";return false;}
  if(actor.CarriedWeight+stack.Weight>actor.CarryingCapacity+.001f){reason=$"Too heavy: {actor.CarriedWeight+stack.Weight:0.#}/{actor.CarryingCapacity:0.#} lb.";return false;}
  reason="";return true;
 }
 public bool AddItem(Actor actor,string itemId,int quantity=1)
 {
  if(quantity<1)return true;var def=ResolveItemDefinition(itemId);int remaining=quantity;
  while(remaining>0)
  {
   int take=Math.Min(def.MaxStack,remaining);var stack=CreateItemStack(def.Id,take);
   if(!CanCarry(actor,stack,out _))return false;if(!actor.Inventory.Add(stack))return false;remaining-=take;
  }
  AssignMissingItemIds();return true;
 }
 public bool AddSharedItem(string itemId,int quantity=1)
 {
  if(quantity<1)return true;var def=ResolveItemDefinition(itemId);int remaining=quantity;while(remaining>0){int take=Math.Min(def.MaxStack,remaining);var stack=CreateItemStack(def.Id,take);if(!SharedStorage.Add(stack))return false;remaining-=take;}AssignMissingItemIds();return true;
 }
 public bool TransferItem(Actor from,Actor to,int index,int quantity=1)
 {
  if(Mode!=Mode.Explore||from==to||!from.Alive||!to.Alive||index<0||index>=from.Inventory.Items.Count||quantity<1)return false;
  var source=from.Inventory.Items[index];int move=Math.Min(quantity,source.Quantity);var probe=source.Portion(move);if(move<source.Quantity)probe.InstanceId="";
  if(!CanCarry(to,probe,out var reason)){LogSystem($"{to.Name} cannot receive {source.Definition.Name}: {reason}",true);return false;}
  var moved=from.Inventory.TakeAt(index,move);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();
  if(!to.Inventory.Add(moved)){from.Inventory.Add(moved);AssignMissingItemIds();return false;}from.QuickAccess.Normalize(from);to.QuickAccess.Normalize(to);AssignMissingItemIds();Message(MessageCategory.System,$"{from.Name} gives {moved.Definition.Name}{(moved.Quantity>1?$" x{moved.Quantity}":"")} to {to.Name}.");return true;
 }
 public bool IdentifyItem(string instanceId)
 {
  ItemStack? found=Actors.SelectMany(a=>a.Inventory.Items.Concat(a.Equipment.All())).Concat(SharedStorage.Items).Concat(Features.SelectMany(f=>f.Contents.Items)).Concat(GroundPiles.SelectMany(p=>p.Inventory.Items)).FirstOrDefault(i=>i.InstanceId==instanceId);
  if(found==null)return false;found.Identified=true;return true;
 }
 public bool SpendItemCharge(Actor actor,string instanceId,int amount=1)
 {
  if(amount<1)return false;var item=actor.Inventory.Items.Concat(actor.Equipment.All()).FirstOrDefault(i=>i.InstanceId==instanceId);if(item==null||item.Definition.MaxCharges<=0||item.Charges<amount)return false;item.Charges-=amount;return true;
 }
 Actor? LootRecipient(Feature f)
 {
  var chosen=Party.FirstOrDefault(a=>a.Id==Chosen.Id&&a.Alive&&a.Position.Distance(f.Pos.Center)<=1.25f);
  return chosen??Party.Where(a=>a.Alive&&a.Position.Distance(f.Pos.Center)<=1.25f).OrderBy(a=>PartyIndex(a)).FirstOrDefault();
 }
 bool CanReceiveContainer(Actor actor,Feature feature,out string reason)
 {
  var copy=new InventoryState{MaxSlots=actor.Inventory.MaxSlots,Items=actor.Inventory.Items.Select(x=>x.Clone()).ToList()};float extra=0;
  foreach(var stack in feature.Contents.Items){if(!copy.CanAdd(stack)){reason="No backpack slot is available for all of the loot.";return false;}copy.Add(stack.Clone());extra+=stack.Weight;}
  if(actor.CarriedWeight+extra>actor.CarryingCapacity+.001f){reason=$"The loot is too heavy ({actor.CarriedWeight+extra:0.#}/{actor.CarryingCapacity:0.#} lb).";return false;}reason="";return true;
 }
 void ValidateInventoryState()
 {
  foreach(var a in Actors.Concat(ShelterResidents.Select(r=>r.Actor))){a.Inventory.ValidateOrThrow(a.Id);a.Equipment.ValidateOrThrow(a.Id);a.QuickAccess??=new();if(!a.Enemy)foreach(var item in a.Equipment.All())if(!item.Definition.EffectiveRequirements.Allows(a,out var why))throw new InvalidDataException($"{a.Id} cannot equip {item.ItemId}: {why}");}
  SharedStorage.ValidateOrThrow("shared storage");GroundPiles??=[];foreach(var pile in GroundPiles)pile.Inventory.ValidateOrThrow($"ground {pile.Pos.X},{pile.Pos.Y}");foreach(var f in Features)f.Contents.ValidateOrThrow($"feature {f.Name}");AssignMissingItemIds();RemoveEmptyGroundPiles();
 }
 public int Roll(int sides){RandomState^=RandomState<<13;RandomState^=RandomState>>17;RandomState^=RandomState<<5;return (int)(RandomState%(uint)sides)+1;}
 public void Say(string text)=>Message(MessageCategory.Narrative,text);
 public void Message(MessageCategory category,string summary,string? detail=null)
 {
  Log??=[];MessageHistory??=[];summary??="";detail??="";
  Log.Add(summary);if(Log.Count>500)Log.RemoveRange(0,Log.Count-500);
  if(NextMessageSequence<1)NextMessageSequence=1;int sequence=NextMessageSequence++;
  MessageHistory.Add(new(){Sequence=sequence,Category=category,Summary=summary,Detail=detail});
  if(MessageHistory.Count>500)MessageHistory.RemoveRange(0,MessageHistory.Count-500);
 }
 public void NormalizeMessageHistory()
 {
  Log??=[];MessageHistory??=[];if(NextMessageSequence<1)NextMessageSequence=1;
  if(MessageHistory.Count==0&&Log.Count>0)foreach(var text in Log.TakeLast(500))MessageHistory.Add(new(){Sequence=NextMessageSequence++,Category=InferLegacyCategory(text),Summary=text});
  int max=MessageHistory.Count==0?0:MessageHistory.Max(x=>x.Sequence);NextMessageSequence=Math.Max(NextMessageSequence,max+1);
  if(MessageHistory.Count>500)MessageHistory=MessageHistory.TakeLast(500).ToList();
  if(Log.Count>500)Log=Log.TakeLast(500).ToList();
 }
 static MessageCategory InferLegacyCategory(string text)
 {
  if(text.StartsWith("Initiative:",StringComparison.Ordinal)||text.Contains("d20 "))return MessageCategory.Roll;
  if(text.Contains(" damage",StringComparison.OrdinalIgnoreCase))return MessageCategory.Damage;
  if(text.Contains("heals ",StringComparison.OrdinalIgnoreCase)||text.Contains("potion",StringComparison.OrdinalIgnoreCase))return MessageCategory.Healing;
  if(text.Contains("Recovered ",StringComparison.Ordinal)||text.Contains("gold",StringComparison.OrdinalIgnoreCase))return MessageCategory.Loot;
  if(text.Contains("rest",StringComparison.OrdinalIgnoreCase)||text.Contains("camp",StringComparison.OrdinalIgnoreCase)||text.Contains("shrine",StringComparison.OrdinalIgnoreCase))return MessageCategory.Rest;
  if(text.Contains("Ember Heart",StringComparison.Ordinal)||text.Contains("LEVEL ",StringComparison.Ordinal))return MessageCategory.Quest;
  if(text.Contains("COMBAT",StringComparison.Ordinal)||text.StartsWith("Round ",StringComparison.Ordinal)||text.StartsWith("Victory",StringComparison.Ordinal))return MessageCategory.Combat;
  if(text.Contains("cannot",StringComparison.OrdinalIgnoreCase)||text.Contains("requires",StringComparison.OrdinalIgnoreCase)||text.Contains("blocked",StringComparison.OrdinalIgnoreCase)||text.Contains("No clear",StringComparison.OrdinalIgnoreCase)||text.Contains("Not enough",StringComparison.OrdinalIgnoreCase))return MessageCategory.Error;
  return MessageCategory.Narrative;
 }
 public void LogAttackRoll(Actor actor,Actor target,int roll,int bonus,int total,bool hit)=>Message(MessageCategory.Roll,$"{actor.Name} attacks {target.Name}: {(hit?"HIT":"MISS")}.",$"{actor.Name}: d20 {roll} + {bonus} = {total} vs AC {target.Ac}: {(hit?"HIT":"MISS")}.");
 public void LogSavingThrow(Actor actor,Ability ability,int roll,int bonus,int dc,bool success)
 {
  if(success)actor.Statistics.SuccessfulSavingThrows++;else actor.Statistics.FailedSavingThrows++;
  Message(MessageCategory.Roll,$"{actor.Name} {(success?"succeeds":"fails")} a {ability} save.",$"{actor.Name}: {ability} save d20 {roll} + {bonus} = {roll+bonus} vs DC {dc}: {(success?"SUCCESS":"FAILURE")}.");
 }
 public void LogAbilityCheck(Actor actor,Skill skill,int roll,int bonus,int dc,bool success)
 {
  if(success)actor.Statistics.SuccessfulSkillChecks++;else actor.Statistics.FailedSkillChecks++;
  Message(MessageCategory.Roll,$"{actor.Name} {(success?"succeeds":"fails")} a {skill} check.",$"{actor.Name}: {skill} check d20 {roll} + {bonus} = {roll+bonus} vs DC {dc}: {(success?"SUCCESS":"FAILURE")}.");
 }
 public void LogDamage(Actor actor,int amount,DamageType type,string suffix="")=>Message(MessageCategory.Damage,$"{actor.Name} takes {amount} {type.ToString().ToLowerInvariant()} damage{suffix}");
 public void LogHealing(Actor actor,int amount,string source)=>Message(MessageCategory.Healing,$"{actor.Name} regains {amount} HP from {source}.");
 public void LogCondition(Actor actor,string condition,bool gained,string source="")=>Message(MessageCategory.Condition,$"{actor.Name} {(gained?"gains":"loses")} {condition}{(string.IsNullOrWhiteSpace(source)?"":$" ({source})")}.");
 public void LogQuest(string text)=>Message(MessageCategory.Quest,text);
 public void LogSystem(string text,bool error=false)=>Message(error?MessageCategory.Error:MessageCategory.System,text);
 public static World New()=>CreateLegacyCampaign();
 /// <summary>IIP47 new-journey path. A legal custom protagonist replaces Rook while the authored five companions remain intact.</summary>
 public static World New(CharacterCreationDraft creation)
 {
  var w=New();var old=w.Actors.First(a=>!a.Enemy&&a.Id=="rogue");int actorIndex=w.Actors.IndexOf(old);var protagonist=CharacterCreationRules.BuildActor(creation,"protagonist",old.Pos);w.Actors[actorIndex]=protagonist;
  w.PartyOrder=["protagonist","knight","ranger","mage","cleric","bard"];w.Selected=0;w.AssignMissingItemIds();
  foreach(var member in w.Party){member.QuickAccess.Normalize(member);if(member.Equipment.Weapon is {} weapon&&string.IsNullOrWhiteSpace(member.QuickAccess.WeaponInstanceIds[0]))member.QuickAccess.WeaponInstanceIds[0]=weapon.InstanceId;var potion=member.Inventory.Items.FirstOrDefault(x=>x.ItemId=="healing-potion");if(potion!=null&&string.IsNullOrWhiteSpace(member.QuickAccess.ItemInstanceIds[0]))member.QuickAccess.ItemInstanceIds[0]=potion.InstanceId;var source=member.Character.Spellcasting.FirstOrDefault();if(source!=null){var first=source.Cantrips.FirstOrDefault()??source.PreparedSpells.FirstOrDefault()??source.KnownSpells.FirstOrDefault();if(!string.IsNullOrWhiteSpace(first))member.QuickAccess.SpellBindings[0]=new(){SourceId=source.SourceId,SpellId=first};}}
  w.InitializeCompanionConversationState();w.Message(MessageCategory.System,$"{protagonist.Name} leads the Ashen Covenant into Saint Orlan's crypt.");w.Reveal();return w;
 }
 HashSet<Cell>? floorCache;
 public bool IsFloor(Cell p)=>(floorCache??=Floors.ToHashSet()).Contains(p);
 public bool Blocked(Cell p)=>!IsFloor(p)||CampaignPropBlocks(p)||Features.Any(f=>f.Kind=="door"&&!f.Used&&f.Pos==p);
 public static bool PersistentInteractionKind(string kind)=>kind is "exit" or "stairs" or "travel" or "transition" or "chest" or "container" or "npc" or "service" or "debrief" or "horde" or "quest-board" or "encounter";
 public bool IsFeatureDiscovered(Feature feature)=>Explored.Contains(feature.Pos)&&!feature.Hidden&&(feature.Kind!="trap"||feature.DetectedTrap);
 public bool IsFeatureInteractable(Feature feature)=>IsFeatureDiscovered(feature)&&(!feature.Used||PersistentInteractionKind(feature.Kind));
 public Actor? At(Cell p)=>Actors.FirstOrDefault(a=>a.Alive&&a.Pos==p);
 public bool Line(Cell a,Cell b)
 {
  int x=a.X,y=a.Y,dx=Math.Abs(b.X-x),sx=x<b.X?1:-1,dy=-Math.Abs(b.Y-y),sy=y<b.Y?1:-1,err=dx+dy;
  while(true){if(x==b.X&&y==b.Y)return true;if((x!=a.X||y!=a.Y)&&SightBlocked(new(x,y)))return false;int e=2*err;if(e>=dy){err+=dy;x+=sx;}if(e<=dx){err+=dx;y+=sy;}}
 }
 public void Reveal()
 {
  Visible.Clear();foreach(var a in Party.Where(a=>a.Alive))foreach(var p in Floors.Where(p=>p.Center.Distance(a.Position)<=9))if(CanSee(a.Position,p.Center))Visible.Add(p);
  foreach(var p in Visible)if(!Explored.Contains(p))Explored.Add(p);
 }
 public void CheckEncounter()
 {
  if(LocalAwarenessEnabled){RefreshLocalSight();return;}
  if(Mode!=Mode.Explore)return;
  var e=Actors.FirstOrDefault(e=>e.Enemy&&e.Alive&&Party.Any(p=>p.Alive&&p.Position.Distance(e.Position)<=CampaignAwareness(e)&&Line(e.Position,p.Position)));
  if(e!=null)StartCombat(e.Group);
 }
 public void StartCombat(int group)
 {
  if(Mode!=Mode.Explore)return;Mode=Mode.Combat;CombatGroup=group;StopAll();ClearReactionInterrupts();Round=1;Turn=0;
  var participants=Actors.Where(a=>a.Alive&&(!a.Enemy||a.Group==group&&(!LocalAwarenessEnabled||a.Awareness.State==AwarenessKind.Alerted||Party.Any(p=>CanEnemySee(a,p))))).ToList();
  foreach(var a in participants){a.Economy.Reset(a.EffectiveSpeedFeet,a.Character.AttacksPerAction);int dex=a.AbilityModifier(Ability.Dexterity),roll=Roll(20);if(a.ClassIs("wise",9)||LowerTraits(a).Contains("upper-fast"))roll=Math.Max(roll,Roll(20));a.Initiative=roll+dex+(a.Character.HasCondition("book-caffeine")?2:0);ClearTurnConditions(a);Message(MessageCategory.Roll,$"{a.Name} initiative {a.Initiative}.",$"Initiative: {a.Name} rolls {roll} + {dex} = {a.Initiative}.");}
  Order=participants.Select(a=>a.Id).ToList();SortInitiativeOrder();
  Message(MessageCategory.Combat,"COMBAT - individual initiative. Movement, Action, Bonus Action and Reaction are tracked separately.");Sound?.Invoke("combat");BeginTurn();
 }
 void ClearTurnConditions(Actor a)
 {
  // Dodge and a readied response last until the start of this creature's next turn.
  foreach(var id in new[]{"dodging","readying","shielded","sneak-attack-used","class-power-used","class-evasion-used","class-shove-used"})a.Character.SetCondition(id,id,false);
  // Defensive cleanup for saves created by older IIP12 development builds.
  a.Character.SetCondition("disengaged","Disengaged",false);
 }
 void ClearEndOfTurnConditions(Actor a)
 {
  // Disengage only protects movement made during the current turn.
  a.Character.SetCondition("disengaged","Disengaged",false);
 }
 void BeginTurn()
 {
  if(Active is not {} a)return;ClearTurnConditions(a);a.Economy.Reset(a.EffectiveSpeedFeet,a.Character.AttacksPerAction);
  foreach(var r in a.Character.Resources.Where(r=>r.Recharge==ResourceRecharge.Turn))r.Restore();
  if(!a.Enemy)Selected=Party.ToList().IndexOf(a);
  Message(MessageCategory.Combat,$"Round {Round}: {a.Name}'s turn.",$"Round {Round}: {a.Name}'s turn. Move {a.Economy.MovementRemainingFeet:0}/{a.Economy.MovementMaximumFeet:0} ft; Action, Bonus Action and Reaction ready; {a.Economy.AttacksPerAction} attack{(a.Economy.AttacksPerAction==1?"":"s")} per Attack action.");
  LowerTurn(a);TriggerReadiedAtTurnStart(a);
 }
 public void EndTurn()
 {
  if(Mode!=Mode.Combat||ReactionInterruptActive)return;
  if(Active is {} moving){Stop(moving);ClearEndOfTurnConditions(moving);moving.Economy.FinishTurn();}
  if(CheckEnd())return;
  for(int i=0;i<Order.Count;i++){Turn++;if(Turn>=Order.Count){Turn=0;Round++;AdvanceSurvivalCombatRound();}if(Active?.Alive==true)break;}
  BeginTurn();
 }
 public bool CheckEnd()
 {
  if(!Party.Any(a=>a.Alive)){StopAll();ClearReactionInterrupts();Mode=Mode.Defeat;Message(MessageCategory.System,Campaign==null?"The covenant has fallen. Load a save or begin again.":"The expedition has fallen. Load a save or begin again.");return true;}
  if(Mode!=Mode.Combat)return false;
  if(Order.Select(id=>Actors.FirstOrDefault(a=>a.Id==id)).Any(a=>a?.Enemy==true&&a.Alive))return false;
  bool contactLost=LocalAwarenessEnabled&&Actors.Any(a=>a.Enemy&&a.Group==CombatGroup&&a.Alive);
  StopAll();ClearReactionInterrupts();Mode=Mode.Explore;if(!contactLost)Cleared++;var campaignReward=Campaign==null?(Xp:0,Currency:0):CampaignEncounterReward(CombatGroup);
  if(LocalAwarenessEnabled)foreach(int otherGroup in Order.Select(id=>Actors.First(a=>a.Id==id)).Where(a=>a.Enemy&&a.Group!=CombatGroup).Select(a=>a.Group).Distinct()){var reward=CampaignEncounterReward(otherGroup);campaignReward=(campaignReward.Item1+reward.Xp,campaignReward.Item2+reward.Currency);}
  int currency=Campaign==null?35+CombatGroup*20:campaignReward.Currency;Gold+=currency;Order.Clear();Turn=0;
  if(!contactLost)foreach(var member in Party)member.Statistics.EncountersWon++;
  foreach(var a in Party){ClearTurnConditions(a);a.Character.SetCondition("hidden","Hidden",false);a.Character.SetCondition("helped","Helped",false);if(a.Unconscious){a.Hp=Math.Max(1,a.MaxHp/3);a.Character.SetCondition("unconscious","Unconscious",false);LogCondition(a,"Unconscious",false,"battle recovery");}}
  int earnedXp=Campaign==null?(CombatGroup switch{0=>150,1=>150,2=>300,_=>100}):campaignReward.Xp;
  foreach(var a in Party)a.Character.Experience+=earnedXp;
  Message(MessageCategory.Quest,$"Each companion earns {earnedXp} XP.",Party.Any(a=>a.CanLevelUp)?"One or more companions can now level up. Open Character Record and choose LEVEL UP; advancement is never automatic.":"Experience is cumulative. Character Record shows the next level threshold.");
  Message(MessageCategory.Combat,contactLost?"Contact lost. Exploration resumes.":currency>0?"Victory. Exploration resumes. Currency recovered.":"Victory. Exploration resumes.");Sound?.Invoke("loot");SyncCampaignObjectives();Reveal();return true;
 }
 public bool CanSpend(Actor a,ActionResourceKind resource)
 {
  if(!a.Alive||LowerMallEnabled&&(a.Character.HasCondition("stunned")||a.Character.HasCondition("chapter-asleep"))||Mode!=Mode.Combat||ReactionInterruptActive||!a.Economy.Available(resource))return false;
  // Reactions intentionally remain legal off-turn; Action and Bonus Action belong to the active turn.
  return resource==ActionResourceKind.Reaction||Active==a&&!IsMoving(a);
 }
 public bool CanAct(Actor a)=>CanSpend(a,ActionResourceKind.Action);
 public bool CanUseBonusAction(Actor a)=>CanSpend(a,ActionResourceKind.BonusAction);
 public bool CanReact(Actor a)=>CanSpend(a,ActionResourceKind.Reaction);
 public bool HasRequiredAmmunition(Actor a)=>(a.Equipment.Weapon?.ItemId!="item.lower.50.loot.4.0"||!a.Character.HasCondition("final-defib-cooldown"))&&(!a.Character.HasCondition("chapter-emp")||a.Equipment.Weapon?.Definition.Equipment?.DamageType!=DamageType.Lightning)&&(!(a.Equipment.Weapon?.ItemId.StartsWith("item.lower.")==true||a.Equipment.Weapon?.ItemId.StartsWith("item.upper.")==true||a.Equipment.Weapon?.ItemId.StartsWith("item.roof.")==true)||a.Equipment.Weapon?.Definition.MaxCharges is null or 0||a.Equipment.Weapon.Charges>0)&&!(WorkshopEnabled&&a.Equipment.Weapon?.Broken==true)&&(!a.RequiresAmmunition||a.EquippedAmmunitionCount>0);
 public bool CanAttack(Actor a)=>a.Alive&&!a.Character.HasCondition("stunned")&&!a.Character.HasCondition("chapter-asleep")&&Mode==Mode.Combat&&!ReactionInterruptActive&&Active==a&&!IsMoving(a)&&a.Economy.CanAttack&&HasRequiredAmmunition(a);
 bool ConsumeRequiredAmmunition(Actor a)
 {
  if(a.Equipment.Weapon?.ItemId=="item.lower.50.loot.4.0")LowerEffect(a,"final-defib-cooldown",6);
  if(a.Equipment.Weapon is {} charged&&(charged.ItemId.StartsWith("item.lower.")||charged.ItemId.StartsWith("item.upper.")||charged.ItemId.StartsWith("item.roof."))&&charged.Definition.MaxCharges>0){if(charged.Charges<=0)return false;charged.Charges--;}
  if(!a.RequiresAmmunition)return true;var ammo=a.EquippedAmmunition;if(ammo==null||ammo.Quantity<1)return false;
  if(LocalAwarenessEnabled&&a.Equipment.Weapon?.Definition.MagazineSize>0){if(!a.UsesSurvivorClasses)a.Character.SetCondition("hidden","Hidden",false);var profile=a.Equipment.Weapon.Definition.Equipment;EmitLocalNoise(a,profile?.Family is "bow" or "crossbow"?"weapon":"gunshot",profile?.NoiseFeet??120);}
  ammo.ConsumeResourceUnits(1);ammo.Quantity--;if(ammo.Quantity<=0){if(a.Equipment.Weapon?.Definition.MagazineSize>0){a.Equipment.Weapon.LoadedAmmunition=null;return true;}var slot=a.Equipment.SlotOf(ammo.InstanceId);if(slot!=null)a.Equipment.Set(slot.Value,null);a.QuickAccess.ClearInstance(ammo.InstanceId);}
  return true;
 }
 public bool SpendResource(Actor a,ActionResourceKind resource,string reason)
 {
  if(!CanSpend(a,resource)||!a.Economy.Spend(resource))return false;
  string label=resource switch{ActionResourceKind.Action=>"Action",ActionResourceKind.BonusAction=>"Bonus Action",_=>"Reaction"};
  Message(MessageCategory.Combat,$"{a.Name} uses a {label}: {reason}.");return true;
 }
 public bool SpendBonusAction(Actor a,string reason="Bonus Action")=>SpendResource(a,ActionResourceKind.BonusAction,reason);
 public bool SpendReaction(Actor a,string reason="Reaction")
 {
  if(!SpendResource(a,ActionResourceKind.Reaction,reason))return false;a.Statistics.ReactionsUsed++;return true;
 }
 bool SpendAction(Actor a,string reason)=>SpendResource(a,ActionResourceKind.Action,reason);
 int AttackRoll(Actor attacker,Actor target,out int first,out int second,out string mode)
 {
  var modifiers=AttackModifiers(attacker,target);bool advantage=modifiers.Advantage,disadvantage=modifiers.Disadvantage;
  first=Roll(20);second=first;mode="";
  if(advantage==disadvantage)return first;
  second=Roll(20);mode=advantage?"advantage":"disadvantage";return advantage?Math.Max(first,second):Math.Min(first,second);
 }
 public bool Attack(Actor a,Actor target,AttackKind kind)
 {
  bool special=kind==AttackKind.Special;
  if(ModernTacticalCombat&&(!Actors.Contains(a)||!Actors.Contains(target)||!a.Enemy&&!Visible.Contains(target.Pos)))return false;
  if(!target.Alive||target.Enemy==a.Enemy)return false;
  if(WorkshopEnabled&&a.Equipment.Weapon?.Broken==true){LogSystem("Repair or replace the broken weapon.",true);return false;}
  if(a.Equipment.Weapon?.Definition.Equipment?.Status is "scripted" or "reference")return false;
  if(!HasRequiredAmmunition(a)){LogSystem($"{a.Name} has no {a.RequiredAmmunitionType} ammunition ready. Reload the firearm or equip ammunition.",true);return false;}
  if((special?!CanAct(a):!CanAttack(a)))return false;
  var signature=a.SignatureFeature;int range=special?a.SpecialRange:a.Range;
  if(a.Position.Distance(target.Position)>range+.001f||!(special?Line(a.Position,target.Position):WeaponLine(a,target))){LogSystem("Target is out of range or behind a wall.",true);return false;}
  var resource=special?a.SignatureResource:null;if(special&&(signature==null||resource==null||resource.Current<1)){LogSystem("No ability charges. Rest or use a shrine.",true);return false;}
  if(special){if(!SpendAction(a,signature?.Name??"Special Ability"))return false;if(resource!=null)resource.Current--;}
  else if(!a.Economy.SpendAttack())return false;
  if(!ConsumeRequiredAmmunition(a)){LogSystem($"{a.Name}'s quiver is empty.",true);return false;}
  ResolveAttackWithReactions(a,target,special,signature,special?signature?.Name??"Special Ability":"Attack");return true;
 }
 int RollDamage(int count,int die){int total=0;if(count<=0||die<2)return 0;for(int i=0;i<count;i++)total+=Roll(die);return total;}
 void Hurt(Actor a,int n,DamageType type,Actor? source=null,bool critical=false)
 {
  if(source is {Enemy:true}&&!a.Enemy)n=DifficultyRules.ScaleIncomingHostileDamage(n,Difficulty);
  bool wasAlive=a.Alive;n=LowerDamage(a,n,type,source,critical);int adjusted=SurvivorReduceDamage(a,a.AdjustDamage(n,type),type,source,critical);int absorbed=Math.Min(adjusted,a.Character.HitPoints.Temporary);a.Character.HitPoints.Temporary-=absorbed;a.Hp=Math.Max(0,a.Hp-(adjusted-absorbed));if(wasAlive)SurvivorLastStand(a,adjusted);LowerDamageResolved(a,adjusted,type,source,wasAlive,critical);WearArmor(a,adjusted,type);a.Statistics.DamageTaken+=adjusted;if(source!=null)source.Statistics.DamageDealt+=adjusted;
  if(!a.Alive)a.Character.SetCondition("unconscious","Unconscious",true,"0 HP");string defense=adjusted==0?" (immune)":adjusted<n?" (resisted)":adjusted>n?" (vulnerable)":"";LogDamage(a,adjusted,type,$"{defense}{(a.Alive?".":" and falls!")}");
  if(adjusted>0&&a.Alive&&a.Character.Concentration!=null)
  {
   int dc=SpellcastingRules.ConcentrationSaveDc(adjusted),roll=Roll(20),bless=a.Character.HasCondition("blessed")?Roll(4):0,bonus=a.SaveModifier(Ability.Constitution)+bless;bool success=roll+bonus>=dc;LogSavingThrow(a,Ability.Constitution,roll,bonus,dc,success);
   if(!success){var spell=SpellCatalog.TryGet(a.Character.Concentration.SpellId,out var definition)?definition.Name:a.Character.Concentration.SpellId;BreakConcentration(a,$"failed DC {dc} concentration save");}
  }
  if(wasAlive&&!a.Alive){if(a.Character.Concentration!=null){var spell=SpellCatalog.TryGet(a.Character.Concentration.SpellId,out var definition)?definition.Name:a.Character.Concentration.SpellId;BreakConcentration(a,"incapacitated");}a.Statistics.TimesDowned++;if(source!=null&&source.Enemy!=a.Enemy)source.Statistics.EnemiesDefeated++;Stop(a);LogCondition(a,"Unconscious",true,"0 HP");RemoveInitiativeParticipant(a.Id,"falls and is removed from initiative");}
 }
 public bool Heal(Actor a,Actor target)
 {
  var feature=a.HealingFeature;var resource=feature==null?null:a.Character.Resource(feature.Mechanics.ResourceId);
  if(feature==null||resource==null||resource.Current<1||!target.Alive||target.Enemy||a.Position.Distance(target.Position)>Math.Max(1,feature.Mechanics.RangeFeet/5f)||!Line(a.Position,target.Position)||target.Hp==target.MaxHp)return false;
  if(Mode==Mode.Combat&&!CanAct(a)||Mode is Mode.Defeat or Mode.Victory)return false;
  Stop(a);a.Face(target.Position);if(Mode==Mode.Combat&&!SpendAction(a,feature.Name))return false;resource.Current--;var m=feature.Mechanics;int heal=RollDamage(m.HealingDice,m.HealingDieSize)+m.HealingBonus;int before=target.Hp;target.Hp=Math.Min(target.MaxHp,target.Hp+heal);int restored=target.Hp-before;a.Statistics.HealingDone+=restored;target.Statistics.HealingReceived+=restored;Message(MessageCategory.Healing,$"{a.Name} heals {target.Name} for {restored} HP.");Sound?.Invoke("heal");return true;
 }
 public bool CanUseOwnedItem(Actor a,string instanceId,out string reason)
 {
  var stack=a.Inventory.FindInstance(instanceId);if(stack==null){reason="The item must be in this character's backpack to use it directly.";return false;}
  if(LowerItemEffect(stack)!=""){reason=LowerItemUseReason(a,stack);return reason=="";}
  if(stack.ItemId=="healing-potion")
  {
   if(!a.Alive){reason="An unconscious or dead character cannot drink a potion.";return false;}
   if(a.Hp>=a.MaxHp){reason="This character is already at full HP.";return false;}
   if(Mode is Mode.Defeat or Mode.Victory){reason="Items cannot be used after the encounter has ended.";return false;}
   if(Mode==Mode.Combat&&!CanAct(a)){reason="Only the active companion with an available Action can drink this potion.";return false;}
   reason="";return true;
  }
  reason="This item has no direct-use action in the current rules layer.";return false;
 }
 public bool UseOwnedItem(Actor a,string instanceId)
 {
  if(!CanUseOwnedItem(a,instanceId,out var reason)){LogSystem(reason,true);return false;}
  var stack=a.Inventory.FindInstance(instanceId);if(stack==null)return false;
  if(LowerItemEffect(stack)!="")return UseLowerMallItem(a,instanceId);
  if(stack.ItemId=="healing-potion")
  {
   if(Mode==Mode.Combat&&!SpendAction(a,"Use Object (healing potion)"))return false;
   int before=a.Hp;stack.ConsumeResourceUnits(1);stack.Quantity--;if(stack.Quantity<=0)a.Inventory.Items.Remove(stack);a.QuickAccess.Normalize(a);a.Hp=Math.Min(a.MaxHp,a.Hp+14);int restored=a.Hp-before;a.Statistics.ItemsUsed++;a.Statistics.HealingDone+=restored;a.Statistics.HealingReceived+=restored;Message(MessageCategory.Healing,$"{a.Name} drinks a potion and regains {restored} HP.");Sound?.Invoke("heal");return true;
  }
  return false;
 }
 public bool Potion(Actor a)
 {
  var potion=a.Inventory.Items.FirstOrDefault(x=>x.ItemId=="healing-potion");return potion!=null&&UseOwnedItem(a,potion.InstanceId);
 }
 public bool ExecuteStandardAction(Actor a,StandardActionKind kind)
 {
  if(Mode!=Mode.Combat||Active!=a||a.Enemy||!a.Alive||IsMoving(a)){LogSystem("Standard combat actions require the active companion to finish moving.",true);return false;}
  if(WorkshopEnabled&&kind==StandardActionKind.Ready&&a.Equipment.Weapon?.Broken==true){LogSystem("Repair or replace the broken weapon before readying an attack.",true);return false;}
  if(ModernTacticalCombat&&kind==StandardActionKind.Ready&&!HasRequiredAmmunition(a)){LogSystem("Load ammunition before readying a firearm.",true);return false;}
  var definition=StandardActions.Get(kind);if(definition.NeedsTarget){LogSystem($"{definition.Name} needs a target.",true);return false;}
  if(!SpendResource(a,definition.Cost,definition.Name))return false;
  switch(kind)
  {
   case StandardActionKind.Dash:
    a.Economy.AddMovement(a.EffectiveSpeedFeet);a.Economy.Dashed=true;Message(MessageCategory.Combat,$"{a.Name} Dashes and gains {a.EffectiveSpeedFeet} ft of movement.");break;
   case StandardActionKind.Disengage:
    a.Character.SetCondition("disengaged","Disengaged",true,"Disengage action");LogCondition(a,"Disengaged",true,"until end of turn");break;
   case StandardActionKind.Dodge:
    a.Character.SetCondition("dodging","Dodging",true,"Dodge action");LogCondition(a,"Dodging",true,"attacks have disadvantage until next turn");break;
   case StandardActionKind.Hide:
   {
    int roll=Roll(20),bonus=a.SkillModifier(Skill.Stealth);if(SurvivorSkillAdvantage(a,Skill.Stealth))roll=Math.Max(roll,Roll(20));int total=roll+bonus;
    int dc=Actors.Where(x=>x.Enemy!=a.Enemy&&x.Alive&&x.Position.Distance(a.Position)<=12).Select(x=>10+x.SkillModifier(Skill.Perception)).DefaultIfEmpty(10).Max();
    bool success=total>=dc;LogAbilityCheck(a,Skill.Stealth,roll,bonus,dc,success);a.Character.SetCondition("hidden","Hidden",success,"Hide action");if(success)LogCondition(a,"Hidden",true,"Stealth check");break;
   }
   case StandardActionKind.Ready:
    a.Character.SetCondition("readying","Readied Action",true,"Ready: basic weapon attack when a hostile enters or begins a turn in range",tags:["reaction","ready"]);LogCondition(a,"Readied Action",true,"basic attack triggers when a hostile enters or begins a turn in weapon range");break;
   case StandardActionKind.Search:
   {
    int roll=Roll(20),bonus=a.SkillModifier(Skill.Perception),dc=12,total=roll+bonus;bool success=total>=dc;LogAbilityCheck(a,Skill.Perception,roll,bonus,dc,success);
    var nearby=Features.Where(f=>a.Position.Distance(f.Pos.Center)<=6&&Explored.Contains(f.Pos)).ToList();int revealed=0;
    if(success)foreach(var feature in nearby)
    {
     bool found=false;if(feature.Hidden){feature.Hidden=false;found=true;}
     if(feature.Kind=="trap"&&!feature.DetectedTrap){feature.DetectedTrap=true;found=true;}
     if(found)revealed++;
    }
    int visible=nearby.Count(IsFeatureDiscovered);
    Message(MessageCategory.System,success?$"{a.Name} searches carefully and notices {visible} nearby feature{(visible==1?"":"s")}{(revealed>0?$", revealing {revealed} hidden interaction{(revealed==1?"":"s")}":"") }.":$"{a.Name} searches but finds nothing new.");break;
   }
   default:return false;
  }
  return true;
 }
 public bool Help(Actor a,Actor target)
 {
  if(target.Enemy||!target.Alive||target==a||a.Position.Distance(target.Position)>1.25f){LogSystem("Help requires a living ally within 5 feet.",true);return false;}
  if(!SpendAction(a,"Help"))return false;target.Character.SetCondition("helped","Helped",true,a.Name);LogCondition(target,"Helped",true,$"{a.Name}'s Help; next attack has advantage");return true;
 }
 public bool UseObject(Actor a,Feature feature)
 {
  if(Mode!=Mode.Combat||Active!=a||!a.Alive||!IsFeatureDiscovered(feature)||a.Position.Distance(feature.Pos.Center)>1.25f){LogSystem("Use Object requires the active companion beside a discovered object.",true);return false;}
  if(IsEngagedBarricade(feature))return OpenOwnedBarricade(a,feature,true);
  if(feature.Locked){LogSystem($"{feature.Name} is locked.",true);return false;}
  if(feature.DetectedTrap){LogSystem($"{feature.Name} is trapped. Disarm it before interacting.",true);return false;}
  if(feature.Kind!="door"||feature.Used){LogSystem("That object has no combat Use Object action yet.",true);return false;}
  if(!SpendAction(a,"Use Object"))return false;feature.Used=true;if(LocalAwarenessEnabled)EmitLocalNoise(a,"door",20,feature.Pos);Message(MessageCategory.Narrative,$"{a.Name} uses an object action to open {feature.Name}.");Reveal();return true;
 }
 public bool Interact(Feature f)
 {
  if(Campaign!=null&&!Features.Contains(f)){LogSystem("That object belongs to another area.",true);return false;}
  if(Mode!=Mode.Explore){LogSystem("Finish the battle before interacting.",true);return false;}
  if(!IsFeatureDiscovered(f)){LogSystem("That interaction has not been discovered.",true);return false;}
  if(!Party.Any(a=>a.Alive&&a.Position.Distance(f.Pos.Center)<=1.25f)){LogSystem("Move beside it, then click again to interact.",true);return false;}
  if(FinaleEnabled&&new[]{Campaign!.Finale!.RefugeFeatureId,Campaign.Finale.HelicopterFeatureId,Campaign.Finale.CrisisFeatureId}.Contains(f.Id)){FinaleOpened?.Invoke();return true;}
  if(LowerMallEnabled&&f.Kind=="location"){LowerMallOpened?.Invoke(f.Id);return true;}
  if(StoryEnabled&&f.Kind=="quest-board"){ContractsOpened?.Invoke();LogSystem("Refuge contracts: accept, review objectives and report at this board.");return true;}
  if(StoryEnabled&&f.Kind=="encounter")return StartEncounterPlan(Campaign!.Story!.EncounterPlans.Single(p=>p.TriggerFeatureId==f.Id).Id);
  if(Campaign!=null&&f.Kind=="horde")return InteractHorde(f);
  if(Campaign!=null&&f.Kind=="debrief")return ReportExpedition(f);
  if(Campaign!=null&&f.Kind=="transition")return UseCampaignExit(f);
  if(Campaign!=null&&f.Kind=="campaign-event")
  {if(f.Used)return false;if(!RunCampaignEvent(f.CampaignEventId))return false;f.Used=Campaign.Events.First(e=>e.Id==f.CampaignEventId).Once;SyncCampaignObjectives();return true;}
  if(Campaign==null&&f.Kind=="relic"&&Actors.Any(a=>a.Enemy&&a.Group==2&&a.Alive)){LogSystem("The Ashen Prior still holds the relic's seal.",true);return false;}
  if(IsLootContainer(f)&&!LowerMallContainerReady(f)){LowerMallOpened?.Invoke(f.Id);return true;}
  if(IsLootContainer(f))
  {
   f.Used=true;
   if(f.Locked)LogSystem($"{f.Name} is locked.",true);
   else if(f.DetectedTrap)LogSystem($"{f.Name} is trapped. Disarm it before taking anything.",true);
   else Message(MessageCategory.Narrative,$"{f.Name} opens for inspection.");
   ContainerLootRequested?.Invoke(f);return true;
  }
  if(f.Locked){LogSystem($"{f.Name} is locked.",true);return false;}
  if(f.DetectedTrap){LogSystem($"{f.Name} is trapped. Disarm it before interacting.",true);return false;}
  if(f.Kind=="npc")
  {
   if(!IsDialogueFeature(f)){LogSystem($"{f.Name} has no available conversation.",true);return false;}
   DialogueRequested?.Invoke(f);return true;
  }
  if(f.Kind=="service")
  {
   if(IsStoreFeature(f)){StoreRequested?.Invoke(f);return true;}
   if(IsServiceFeature(f)){ServiceRequested?.Invoke(f);return true;}
   LogSystem($"{f.Name} has no available service.",true);return false;
  }
  if(f.Kind is "exit" or "travel" or "transition")
  {Message(MessageCategory.Travel,$"{f.Name} is a legal world-travel exit. Open Map > World Map to choose a discovered destination.");return true;}
  if(f.Kind=="relic-return")
  {if(HasRelic){Mode=Mode.Victory;LogQuest("The Ember Heart is restored to Saint Orlan. Dawn returns to the vale.");Sound?.Invoke("loot");}else LogQuest("The Ember Hearth is cold. The stolen Ember Heart is still below the crypt.");SyncJournalProgress();return true;}
  if(IsEngagedBarricade(f))return OpenOwnedBarricade(Party.Where(a=>a.Alive).OrderBy(a=>a.Position.Distance(f.Pos.Center)).First(),f,false);
  if(f.Used){LogSystem("There is nothing more here.",true);return false;}
  f.Used=true;
  if(f.Kind=="door"){if(LocalAwarenessEnabled){var opener=Party.Where(a=>a.Alive).OrderBy(a=>a.Position.Distance(f.Pos.Center)).FirstOrDefault();if(opener!=null)EmitLocalNoise(opener,"door",20,f.Pos);}
   Message(MessageCategory.Narrative,Campaign==null?"The ironbound door swings open.":$"{f.Name} opens.");Reveal();CheckEncounter();SyncJournalProgress();return true;}
  if(f.Kind=="shrine"){Restore();Message(MessageCategory.Rest,"The last dawn restores the entire party. The shrine grows cold.");Sound?.Invoke("heal");SyncJournalProgress();return true;}
  SyncJournalProgress();return true;
 }
 void Restore(){ClearAllSpellEffects();foreach(var a in Party)a.Character.RestoreLongRest();}
 // IIP28 historical audit marker: Statistics.RestsTaken++ now occurs in Resting.cs under the IIP43 rest authority.
 public bool CanEquipInSlot(Actor actor,ItemStack stack,EquipmentSlot slot,out string reason)
 {
  if(WorkshopEnabled&&stack.Broken){reason="Repair this broken item before equipping it.";return false;}
  var item=stack.Definition;if(item.Equipment?.Status is "scripted" or "reference"){reason=item.Equipment.Note;return false;}if(!item.CanEquipIn(slot)){reason=$"{item.Name} does not fit the {EquipmentSlots.Display(slot)} slot.";return false;}
  if(!item.EffectiveRequirements.Allows(actor,out reason))return false;
  if(slot==EquipmentSlot.OffHand&&actor.Equipment.Weapon?.Definition.TwoHanded==true){reason="A two-handed main-hand item occupies the off hand.";return false;}
  if(slot==EquipmentSlot.MainHand&&item.TwoHanded&&actor.Equipment.OffHand!=null){reason="Remove the off-hand item before equipping a two-handed weapon.";return false;}
  reason="";return true;
 }
 public bool Equip(Actor a,int index,EquipmentSlot? requestedSlot=null)
 {
  if(Mode!=Mode.Explore||index<0||index>=a.Inventory.Items.Count||!a.Alive)return false;var stack=a.Inventory.Items[index];var item=stack.Definition;
  var slots=item.EquipSlots().ToArray();if(slots.Length==0){LogSystem($"{item.Name} cannot be equipped.",true);return false;}
  var slot=requestedSlot??a.Equipment.PreferredSlot(item);if(!CanEquipInSlot(a,stack,slot,out var reason)){LogSystem($"{a.Name} cannot equip {item.Name}: {reason}",true);return false;}
  int amount=item.Kind==ItemKind.Ammunition?stack.Quantity:1;var equipped=a.Inventory.TakeAt(index,amount);if(equipped==null)return false;var old=a.Equipment.Get(slot);a.Equipment.Set(slot,equipped);
  if(old!=null&&!a.Inventory.Add(old)){a.Equipment.Set(slot,old);a.Inventory.Add(equipped);LogSystem("No backpack slot is available for the replaced equipment.",true);return false;}
  AssignMissingItemIds();a.QuickAccess.Normalize(a);Message(MessageCategory.System,$"{a.Name} equips {item.Name} in {EquipmentSlots.Display(slot)}.");return true;
 }
 public bool MoveEquippedItem(Actor actor,EquipmentSlot from,EquipmentSlot to)
 {
  if(Mode!=Mode.Explore||!actor.Alive||from==to)return false;var moving=actor.Equipment.Get(from);if(moving==null)return false;
  if(!CanEquipInSlot(actor,moving,to,out var reason)){LogSystem($"{actor.Name} cannot move {moving.Definition.Name}: {reason}",true);return false;}
  var displaced=actor.Equipment.Get(to);if(displaced!=null)
  {
   if(displaced.Definition.CanEquipIn(from)&&CanEquipInSlot(actor,displaced,from,out _)){actor.Equipment.Set(to,moving);actor.Equipment.Set(from,displaced);}
   else
   {
    if(!actor.Inventory.CanAdd(displaced)){LogSystem("No backpack slot is available for the displaced equipment.",true);return false;}
    actor.Equipment.Set(from,null);actor.Equipment.Set(to,moving);actor.Inventory.Add(displaced);
   }
  }
  else{actor.Equipment.Set(from,null);actor.Equipment.Set(to,moving);}
  AssignMissingItemIds();actor.QuickAccess.Normalize(actor);Message(MessageCategory.System,$"{actor.Name} moves {moving.Definition.Name} to {EquipmentSlots.Display(to)}.");return true;
 }
 public bool Unequip(Actor a,EquipmentSlot slot)
 {
  if(Mode!=Mode.Explore||!a.Alive)return false;var item=a.Equipment.Get(slot);if(item==null)return false;if(!a.Inventory.CanAdd(item)){LogSystem("No backpack slot is available.",true);return false;}a.Equipment.Set(slot,null);a.Inventory.Add(item);AssignMissingItemIds();a.QuickAccess.Normalize(a);Message(MessageCategory.System,$"{a.Name} removes {item.Definition.Name}.");return true;
 }
 public bool ReorderInventory(Actor actor,int fromIndex,int toIndex)
 {
  if(fromIndex<0||fromIndex>=actor.Inventory.Items.Count||toIndex<0||toIndex>=actor.Inventory.MaxSlots)return false;
  var item=actor.Inventory.Items[fromIndex];actor.Inventory.Items.RemoveAt(fromIndex);toIndex=Math.Min(toIndex,actor.Inventory.Items.Count);actor.Inventory.Items.Insert(toIndex,item);return true;
 }
 public bool TransferEquippedItem(Actor from,Actor to,EquipmentSlot slot)
 {
  if(Mode!=Mode.Explore||from==to||!from.Alive||!to.Alive)return false;var stack=from.Equipment.Get(slot);if(stack==null)return false;
  if(!CanCarry(to,stack,out var reason)){LogSystem($"{to.Name} cannot receive {stack.Definition.Name}: {reason}",true);return false;}
  from.Equipment.Set(slot,null);if(!to.Inventory.Add(stack)){from.Equipment.Set(slot,stack);return false;}from.QuickAccess.ClearInstance(stack.InstanceId);AssignMissingItemIds();to.QuickAccess.Normalize(to);Message(MessageCategory.System,$"{from.Name} gives {stack.Definition.Name} to {to.Name}.");return true;
 }
 public bool DropInventoryItem(Actor actor,int index,int quantity=1)
 {
  if(Mode!=Mode.Explore||!actor.Alive||index<0||index>=actor.Inventory.Items.Count)return false;var source=actor.Inventory.Items[index];int move=Math.Clamp(quantity,1,source.Quantity);var moved=actor.Inventory.TakeAt(index,move);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();
  var ground=GroundInventoryAt(actor.Pos,true);if(!ground.Add(moved)){actor.Inventory.Add(moved);AssignMissingItemIds();return false;}actor.QuickAccess.Normalize(actor);AssignMissingItemIds();Message(MessageCategory.System,$"{actor.Name} drops {moved.Definition.Name}{(moved.Quantity>1?$" x{moved.Quantity}":"")}.");return true;
 }
 public bool DropEquippedItem(Actor actor,EquipmentSlot slot)
 {
  if(Mode!=Mode.Explore||!actor.Alive)return false;var stack=actor.Equipment.Get(slot);if(stack==null)return false;var ground=GroundInventoryAt(actor.Pos,true);if(!ground.CanAdd(stack))return false;actor.Equipment.Set(slot,null);ground.Add(stack);actor.QuickAccess.ClearInstance(stack.InstanceId);AssignMissingItemIds();Message(MessageCategory.System,$"{actor.Name} drops {stack.Definition.Name}.");return true;
 }
 public bool PickupGroundItem(Actor actor,int index,int quantity=1)
 {
  if(Mode!=Mode.Explore||!actor.Alive)return false;var pile=GroundPileAt(actor.Pos);if(pile==null||index<0||index>=pile.Inventory.Items.Count)return false;var source=pile.Inventory.Items[index];int move=Math.Clamp(quantity,1,source.Quantity);var probe=source.Portion(move);if(move<source.Quantity)probe.InstanceId="";if(!CanCarry(actor,probe,out var reason)){LogSystem($"{actor.Name} cannot pick up {source.Definition.Name}: {reason}",true);return false;}
  var moved=pile.Inventory.TakeAt(index,move);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();if(!actor.Inventory.Add(moved)){pile.Inventory.Add(moved);AssignMissingItemIds();return false;}AssignMissingItemIds();RemoveEmptyGroundPiles();Message(MessageCategory.System,$"{actor.Name} picks up {moved.Definition.Name}{(moved.Quantity>1?$" x{moved.Quantity}":"")}.");return true;
 }
 public bool TransferGroundItem(Actor actor,int groundIndex,int partyIndex,int quantity=1)
 {
  var target=PartyAt(partyIndex);if(target==null)return false;if(target==actor)return PickupGroundItem(actor,groundIndex,quantity);var pile=GroundPileAt(actor.Pos);if(pile==null||groundIndex<0||groundIndex>=pile.Inventory.Items.Count)return false;var source=pile.Inventory.Items[groundIndex];int move=Math.Clamp(quantity,1,source.Quantity);var probe=source.Portion(move);if(move<source.Quantity)probe.InstanceId="";if(!CanCarry(target,probe,out var reason)){LogSystem($"{target.Name} cannot receive {source.Definition.Name}: {reason}",true);return false;}var moved=pile.Inventory.TakeAt(groundIndex,move);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();target.Inventory.Add(moved);AssignMissingItemIds();RemoveEmptyGroundPiles();Message(MessageCategory.System,$"{target.Name} picks up {moved.Definition.Name} from the ground.");return true;
 }

 public void Save(string path)
 {
  if(Mode!=Mode.Explore&&!CampaignEnded)throw new InvalidOperationException("Save when exploring outside combat or after a campaign ending.");
  BindCampaignContent();SyncCampaignObjectives();NormalizePartyOrder();if(PartyCount<1||PartyCount>MaxPartySize)throw new InvalidOperationException($"Party must contain 1-{MaxPartySize} members.");
  foreach(var actor in Actors.Concat(ShelterResidents.Select(r=>r.Actor)))actor.Character.ValidateOrThrow(actor.Id);NormalizeMapState();ValidateMapState();NormalizeTravelState();ValidateTravelState();NormalizeJournalState();ValidateJournalState();NormalizeCompanionConversationState();ValidateCompanionConversationState();NormalizeStoreState();ValidateStoreState();NormalizeServiceState();ValidateServiceState();NormalizeRestState();ValidateRestState();ValidateInventoryState();
  ValidateResourceState();ValidateExpeditionState();ValidateFirearmState();ValidateLocalAwareness();ValidateHordeState();ValidateSurvivalState();ValidateShelterState();ValidateInfrastructure();ValidateTension();ValidateSurvivorClasses();ValidateEquipmentState();ValidateWorkshopState();ValidateStoryState();ValidateSocialState();ValidateLowerMall();ValidateFinale();Version=41;Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);string temp=path+".tmp";
  File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,path,true);Message(MessageCategory.System,"Game saved.");
 }
 static JsonObject MigrateLegacySave(JsonObject root,int version)
 {
  int level=root["Level"]?.GetValue<int>()??1;
  if(root["Actors"] is not JsonArray actors)throw new InvalidDataException("Legacy save has no actors.");
  foreach(var node in actors)
  {
   if(node is not JsonObject actor)continue;
   string id=actor["Id"]?.GetValue<string>()??"";
   string legacyClass=actor["Class"]?.GetValue<string>()??(actor["CreatureKind"]?.GetValue<string>()??"Adventurer");
   bool enemy=actor["Enemy"]?.GetValue<bool>()??false;
   int hp=actor["Hp"]?.GetValue<int>()??1,maxHp=actor["MaxHp"]?.GetValue<int>()??Math.Max(1,hp);
   int baseAc=actor["BaseAc"]?.GetValue<int>()??10,bonus=actor["Bonus"]?.GetValue<int>()??0,die=actor["Die"]?.GetValue<int>()??6;
   int range=actor["Range"]?.GetValue<int>()??1,mana=actor["Mana"]?.GetValue<int>()??0,maxMana=actor["MaxMana"]?.GetValue<int>()??mana;
   var sheet=CharacterTemplates.FromLegacy(id,legacyClass,enemy,level,hp,maxHp,baseAc,bonus,die,range,mana,maxMana);
   actor["Character"]=JsonSerializer.SerializeToNode(sheet);
   actor["CreatureKind"]=enemy?legacyClass:"";
   foreach(var key in new[]{"Class","Hp","MaxHp","BaseAc","Dex","Bonus","Die","Range","Mana","MaxMana","Guarding"})actor.Remove(key);
  }
  root["Version"]=4;return root;
 }
 static JsonObject MigrateV4Inventory(JsonObject root)
 {
  if(root["Actors"] is not JsonArray actors)throw new InvalidDataException("V4 save has no actors.");
  int serial=1;string Next()=> $"itm-{serial++:D6}";
  ItemStack Stack(string id,int quantity=1){var def=Items.Get(id);return new(){InstanceId=Next(),ItemId=id,Quantity=Math.Clamp(quantity,1,Math.Max(1,def.MaxStack)),Charges=def.MaxCharges>0?def.MaxCharges:-1,Identified=def.IdentifiedByDefault};}
  var actorObjects=actors.OfType<JsonObject>().ToList();var party=actorObjects.Where(a=>!(a["Enemy"]?.GetValue<bool>()??false)).ToList();
  foreach(var actor in actorObjects)
  {
   string weapon=actor["Weapon"]?.GetValue<string>()??"sword",armor=actor["Armor"]?.GetValue<string>()??"mail",charm=actor["Charm"]?.GetValue<string>()??"";
   var equipment=new EquipmentState{Weapon=Stack(weapon),Armor=Stack(armor),Charm=string.IsNullOrWhiteSpace(charm)?null:Stack(charm)};
   actor["Equipment"]=JsonSerializer.SerializeToNode(equipment);actor["Inventory"]=JsonSerializer.SerializeToNode(new InventoryState());
   actor.Remove("Weapon");actor.Remove("Armor");actor.Remove("Charm");
  }
  string? recipientId=null;int selected=root["Selected"]?.GetValue<int>()??0;
  if(root["PartyOrder"] is JsonArray order&&order.Count>0){selected=Math.Clamp(selected,0,order.Count-1);recipientId=order[selected]?.GetValue<string>();}
  var recipient=party.FirstOrDefault(a=>(a["Id"]?.GetValue<string>()??"")==recipientId)??party.FirstOrDefault();
  InventoryState Inventory(JsonObject actor)=>actor["Inventory"]?.Deserialize<InventoryState>()??new();
  void StoreInventory(JsonObject actor,InventoryState inventory)=>actor["Inventory"]=JsonSerializer.SerializeToNode(inventory);
  if(root["Bag"] is JsonArray bag&&recipient!=null)
  {
   var inventory=Inventory(recipient);foreach(var node in bag){var id=node?.GetValue<string>();if(!string.IsNullOrWhiteSpace(id))inventory.Add(Stack(id));}StoreInventory(recipient,inventory);
  }
  int potions=root["Potions"]?.GetValue<int>()??0;for(int i=0;i<potions&&party.Count>0;i++)
  {
   var owner=party[i%party.Count];var inventory=Inventory(owner);var existing=inventory.Items.FirstOrDefault(x=>x.ItemId=="healing-potion"&&x.Quantity<Items.Get("healing-potion").MaxStack);if(existing!=null)existing.Quantity++;else inventory.Add(Stack("healing-potion"));StoreInventory(owner,inventory);
  }
  if(root["Features"] is JsonArray features)foreach(var node in features)if(node is JsonObject feature)
  {
   bool used=feature["Used"]?.GetValue<bool>()??false;string kind=feature["Kind"]?.GetValue<string>()??"";string loot=feature["Loot"]?.GetValue<string>()??"";var contents=new InventoryState{MaxSlots=32};int gold=0;
   if(!used&&!string.IsNullOrWhiteSpace(loot)){contents.Add(Stack(loot));if(kind=="chest")contents.Add(Stack("healing-potion",2));gold=20;}
   feature["Contents"]=JsonSerializer.SerializeToNode(contents);feature["Gold"]=gold;
  }
  root.Remove("Bag");root.Remove("Potions");root["SharedStorage"]=JsonSerializer.SerializeToNode(new InventoryState{MaxSlots=120});root["NextItemInstanceNumber"]=serial;root["Version"]=5;return root;
 }
 static JsonObject MigrateV5Equipment(JsonObject root)
 {
  if(root["Actors"] is not JsonArray actors)throw new InvalidDataException("V5 save has no actors.");
  int serial=root["NextItemInstanceNumber"]?.GetValue<int>()??1;string Next()=> $"itm-{serial++:D6}";
  foreach(var node in actors)if(node is JsonObject actor&&!(actor["Enemy"]?.GetValue<bool>()??false))
  {
   var equipment=actor["Equipment"]?.Deserialize<EquipmentState>()??new EquipmentState();
   var weapon=equipment.Weapon?.Definition;if(weapon?.RequiresAmmunition==true&&equipment.AmmunitionFor(weapon.AmmunitionType)==null)
   {
    var ammo=Items.All.FirstOrDefault(i=>i.Kind==ItemKind.Ammunition&&i.AmmoType.Equals(weapon.AmmunitionType,StringComparison.OrdinalIgnoreCase));
    if(ammo!=null)equipment.Set(EquipmentSlot.Ammo1,new(){InstanceId=Next(),ItemId=ammo.Id,Quantity=Math.Min(40,ammo.MaxStack),Charges=-1,Identified=true});
   }
   actor["Equipment"]=JsonSerializer.SerializeToNode(equipment);
  }
  root["NextItemInstanceNumber"]=serial;root["Version"]=6;return root;
 }

 static JsonObject MigrateV6Spellcasting(JsonObject root)
 {
  // IIP30's v7 schema adds typed spellcasting-source metadata and a durable single-concentration field.
  // Existing v6 spell IDs/slots already deserialize safely; runtime normalization supplies the class tradition defaults.
  root["Version"]=7;return root;
 }
 static JsonObject MigrateV7AreaMap(JsonObject root)
 {
  // IIP35's v8 schema adds durable player-authored Area Map notes. Authored location/quest markers
  // are content definitions and therefore do not need to be serialized into each save.
  root["MapNotes"]??=new JsonArray();root["NextMapNoteNumber"]??=1;root["Version"]=8;return root;
 }
 static JsonObject MigrateV8WorldTravel(JsonObject root)
 {
  // IIP36's v9 schema adds durable world-map discovery/unlock state, a campaign travel clock,
  // and snapshots for non-current playable regions. V8 saves are always in Saint Orlan's Crypt.
  root["CurrentTravelDestinationId"]??="saint-orlans-crypt";root["TravelDestinations"]??=new JsonArray();root["StoredTravelAreas"]??=new JsonArray();
  root["CampaignMinutes"]??=480;root["TravelCount"]??=0;root["LastTravelEventId"]??="";root["Version"]=9;return root;
 }
 static JsonObject MigrateV9Journal(JsonObject root)
 {
  // IIP37's v10 schema adds durable quest lifecycle, independent story entries, and user journal notes.
  // Runtime normalization reconstructs the main quest from authoritative v9 world state.
  root["Quests"]??=new JsonArray();root["JournalEntries"]??=new JsonArray();root["UserJournalNotes"]??=new JsonArray();
  root["NextJournalSequence"]??=1;root["NextUserJournalNoteNumber"]??=1;root["Version"]=10;return root;
 }
 static JsonObject MigrateV10CompanionConversations(JsonObject root)
 {
  // IIP40's v11 schema adds durable companion approval, narrative flags, one-shot
  // interjection history, and queued/seen companion conversation state.
  root["CompanionApproval"]??=new JsonObject();root["DialogueFlags"]??=new JsonArray();root["SeenCompanionInterjections"]??=new JsonArray();
  root["SeenCompanionConversations"]??=new JsonArray();root["PendingCompanionConversations"]??=new JsonArray();root["Version"]=11;return root;
 }
 static JsonObject MigrateV11Stores(JsonObject root)
 {
  // IIP41's v12 schema adds durable merchant stock/cash. Runtime normalization seeds
  // authored stores because v11 journeys could not have made any merchant transactions.
  root["Stores"]??=new JsonArray();root["Version"]=12;return root;
 }
 static JsonObject MigrateV12Services(JsonObject root)
 {
  // IIP42's v13 schema adds durable non-trade service usage/revenue. Runtime normalization
  // also injects the shrine/inn bindings into Saint Orlan snapshots created by older builds.
  root["Services"]??=new JsonArray();root["Version"]=13;return root;
 }
 static JsonObject MigrateV13Rest(JsonObject root)
 {
  // IIP43's v14 schema adds durable rest-attempt count and last camp-event identity.
  // Older journeys had only the direct long-rest button, so no historical event is invented.
  root["RestCount"]??=0;root["LastRestEventId"]??="";root["Version"]=14;return root;
 }
 static JsonObject MigrateV14CharacterCreation(JsonObject root)
 {
  // IIP47's v15 schema adds durable identity fields to actors. Older journeys retain their authored companions
  // and receive neutral identity defaults; migration never invents a player-created protagonist.
  if(root["Actors"] is JsonArray actors)foreach(var node in actors)if(node is JsonObject actor)
  {actor["PlayerCreated"]??=false;actor["Pronouns"]??="they/them";actor["VoiceSet"]??="";}
  root["Version"]=15;return root;
 }
 public static World Load(string path)
 {
  var root=JsonNode.Parse(File.ReadAllText(path)) as JsonObject??throw new InvalidDataException("Empty save.");
  int sourceVersion=root["Version"]?.GetValue<int>()??1;
  if(sourceVersion<1||sourceVersion>41)throw new InvalidDataException("Invalid or unsupported save.");
  if(sourceVersion<4)root=MigrateLegacySave(root,sourceVersion);
  if(sourceVersion<5)root=MigrateV4Inventory(root);
  if(sourceVersion<6)root=MigrateV5Equipment(root);
  if(sourceVersion<7)root=MigrateV6Spellcasting(root);
  if(sourceVersion<8)root=MigrateV7AreaMap(root);
  if(sourceVersion<9)root=MigrateV8WorldTravel(root);
  if(sourceVersion<10)root=MigrateV9Journal(root);
  if(sourceVersion<11)root=MigrateV10CompanionConversations(root);
  if(sourceVersion<12)root=MigrateV11Stores(root);
  if(sourceVersion<13)root=MigrateV12Services(root);
  if(sourceVersion<14)root=MigrateV13Rest(root);
  if(sourceVersion<15)root=MigrateV14CharacterCreation(root);
  if(sourceVersion<41)root["Version"]=41;
  var w=root.Deserialize<World>()??throw new InvalidDataException("Empty save.");
  if(w.Version!=41||w.Floors.Count==0||(w.Mode!=Mode.Explore&&!w.CampaignEnded)||w.RandomState==0||w.Actors.Select(a=>a.Id).Distinct().Count()!=w.Actors.Count)throw new InvalidDataException("Invalid or unsupported save.");
  // V1/V2 saves did not persist party order. Normalize preserves their actor order without inventing missing companions.
  w.BindCampaignContent();w.NormalizePartyOrder();var party=w.Party.ToList();
  if(party.Count<1||party.Count>MaxPartySize||w.PartyOrder.Count!=party.Count)throw new InvalidDataException($"Party must contain 1-{MaxPartySize} members.");
  w.NormalizeTravelState();w.ValidateTravelState();
  foreach(var a in w.Actors)
  {
   a.Statistics??=new();a.Pronouns=string.IsNullOrWhiteSpace(a.Pronouns)?"they/them":a.Pronouns;a.VoiceSet??="";if(!a.Enemy){if(w.Campaign==null)CharacterTemplates.EnsureNarrative(a.Id,a.Character);LevelingRules.NormalizeSubclassDecisionState(a.Character);}
   SpellcastingRules.NormalizeCharacter(a.Character);a.Character.ValidateOrThrow(a.Id);foreach(var equipped in a.Equipment.All())_=equipped.Definition;
   if(!float.IsFinite(a.Position.X)||!float.IsFinite(a.Position.Y)||!w.SavedActorCanStand(a))throw new InvalidDataException("Invalid character position.");
  }
  w.NormalizeDialogueContent();w.NormalizeMapState();w.ValidateMapState();w.ValidateTravelState();w.NormalizeJournalState();w.ValidateJournalState();w.NormalizeCompanionConversationState();w.ValidateCompanionConversationState();w.NormalizeStoreState();w.ValidateStoreState();w.NormalizeServiceState();w.ValidateServiceState();w.NormalizeRestState();w.ValidateRestState();w.ValidateInventoryState();w.Version=41;w.Selected=Math.Clamp(w.Selected,0,party.Count-1);w.Reveal();w.NormalizeMessageHistory();
  string resume=sourceVersion<41?$"Journey resumed. Save upgraded from v{sourceVersion} through the current v41 mall schema.":"Journey resumed.";w.Message(MessageCategory.System,resume);return w;
 }
}
