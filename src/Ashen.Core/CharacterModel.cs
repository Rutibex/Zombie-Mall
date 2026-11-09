using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Ability { Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Skill
{
 Acrobatics, AnimalHandling, Arcana, Athletics, Deception, History, Insight, Intimidation,
 Investigation, Medicine, Nature, Perception, Performance, Persuasion, Religion,
 SleightOfHand, Stealth, Survival, Technology, Mechanics, Resourcefulness, Driving, Leadership, Occultism, Lockpicking
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProficiencyRank { None, Half, Proficient, Expertise }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DamageType { Acid, Bludgeoning, Cold, Fire, Force, Lightning, Necrotic, Piercing, Poison, Psychic, Radiant, Slashing, Thunder }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResourceRecharge { None, Turn, ShortRest, LongRest }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureKind { ClassFeature, SubclassFeature, SpeciesTrait, BackgroundFeature, Feat, EquipmentFeature, Other }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellPreparationMode { None, Known, Prepared, Spellbook, Pact }


public sealed class CharacterNarrative
{
 public string Biography {get;set;}="";
 public string Personality {get;set;}="";
 public List<string> Affiliations {get;set;}=[];
 public List<string> Notes {get;set;}=[];
}

/// <summary>Persistent journey history for the IIP28 Information screen.</summary>
public sealed class CharacterStatistics
{
 public int AttacksMade {get;set;}
 public int AttacksHit {get;set;}
 public int CriticalHits {get;set;}
 public int DamageDealt {get;set;}
 public int DamageTaken {get;set;}
 public int HealingDone {get;set;}
 public int HealingReceived {get;set;}
 public int EnemiesDefeated {get;set;}
 public int TimesDowned {get;set;}
 public int ReactionsUsed {get;set;}
 public int ItemsUsed {get;set;}
 public int RestsTaken {get;set;}
 public int EncountersWon {get;set;}
 public int SuccessfulSkillChecks {get;set;}
 public int FailedSkillChecks {get;set;}
 public int SuccessfulSavingThrows {get;set;}
 public int FailedSavingThrows {get;set;}
 public float DistanceTravelledFeet {get;set;}
 [JsonIgnore] public int Misses=>Math.Max(0,AttacksMade-AttacksHit);
 [JsonIgnore] public float HitRate=>AttacksMade<=0?0:AttacksHit*100f/AttacksMade;
}

public sealed class AbilityScores
{
 public int Strength {get;set;}=10;
 public int Dexterity {get;set;}=10;
 public int Constitution {get;set;}=10;
 public int Intelligence {get;set;}=10;
 public int Wisdom {get;set;}=10;
 public int Charisma {get;set;}=10;
 public int Score(Ability ability)=>ability switch
 {
  Ability.Strength=>Strength,Ability.Dexterity=>Dexterity,Ability.Constitution=>Constitution,
  Ability.Intelligence=>Intelligence,Ability.Wisdom=>Wisdom,_=>Charisma
 };
 public int Modifier(Ability ability)=>ModifierFor(Score(ability));
 public static int ModifierFor(int score)=>(int)Math.Floor((score-10)/2d);
 public IEnumerable<(Ability Ability,int Score,int Modifier)> All()
 {
  foreach(var ability in Enum.GetValues<Ability>())yield return(ability,Score(ability),Modifier(ability));
 }
}

public static class SkillRules
{
 public static Ability AbilityFor(Skill skill)=>skill switch
 {
  Skill.Driving or Skill.Lockpicking or Skill.Acrobatics or Skill.SleightOfHand or Skill.Stealth=>Ability.Dexterity,
  Skill.Technology or Skill.Mechanics or Skill.Resourcefulness or Skill.Occultism or Skill.Arcana or Skill.History or Skill.Investigation or Skill.Nature or Skill.Religion=>Ability.Intelligence,
  Skill.AnimalHandling or Skill.Insight or Skill.Medicine or Skill.Perception or Skill.Survival=>Ability.Wisdom,
  Skill.Leadership or Skill.Deception or Skill.Intimidation or Skill.Performance or Skill.Persuasion=>Ability.Charisma,
  _=>Ability.Strength
 };
 public static string DisplayName(Skill skill)=>skill switch
 {
  Skill.AnimalHandling=>"Animal Handling",Skill.SleightOfHand=>"Sleight of Hand",_=>SplitPascal(skill.ToString())
 };
 static string SplitPascal(string value)
 {
  var chars=new List<char>(value.Length+5);
  for(int i=0;i<value.Length;i++){if(i>0&&char.IsUpper(value[i])&&char.IsLower(value[i-1]))chars.Add(' ');chars.Add(value[i]);}
  return new string(chars.ToArray());
 }
}

public static class ExperienceRules
{
 // 5e cumulative XP thresholds. IIP29 owns the actual level-up workflow; IIP5 only needs a reliable eligibility signal.
 static readonly int[] Thresholds=[0,0,300,900,2700,6500,14000,23000,34000,48000,64000,85000,100000,120000,140000,165000,195000,225000,265000,305000,355000];
 public static int ForLevel(int level)=>Thresholds[Math.Clamp(level,1,20)];
}

public sealed class SkillTraining
{
 public Skill Skill {get;set;}
 public ProficiencyRank Rank {get;set;}=ProficiencyRank.Proficient;
}

public sealed class ProficiencyData
{
 public List<Ability> SavingThrows {get;set;}=[];
 public List<SkillTraining> Skills {get;set;}=[];
 public List<string> Weapons {get;set;}=[];
 public List<string> Armor {get;set;}=[];
 public List<string> Tools {get;set;}=[];
 public List<string> Languages {get;set;}=[];
 public ProficiencyRank SkillRank(Skill skill)=>Skills.FirstOrDefault(s=>s.Skill==skill)?.Rank??ProficiencyRank.None;
 public bool SaveProficient(Ability ability)=>SavingThrows.Contains(ability);
}

public sealed class ClassLevelEntry
{
 public string ClassId {get;set;}="Adventurer";
 public string SubclassId {get;set;}="";
 public int Level {get;set;}=1;
 public int HitDieSize {get;set;}=8;
 public Ability? SpellcastingAbility {get;set;}
}

public sealed class HitPointPool
{
 public int Current {get;set;}=1;
 public int Maximum {get;set;}=1;
 public int Temporary {get;set;}
}

public sealed class HitDiePool
{
 public string SourceClass {get;set;}="Adventurer";
 public int DieSize {get;set;}=8;
 public int Current {get;set;}=1;
 public int Maximum {get;set;}=1;
}

public sealed class DefenseData
{
 // Base defense supplied by the character rules; IIP24 equipment then contributes legal shield/armor/accessory modifiers through EquipmentState.
 public int ArmorClassBase {get;set;}=10;
 public List<DamageType> Resistances {get;set;}=[];
 public List<DamageType> Immunities {get;set;}=[];
 public List<DamageType> Vulnerabilities {get;set;}=[];
 public List<string> ConditionImmunities {get;set;}=[];
 public int AdjustDamage(int amount,DamageType type)
 {
  if(Immunities.Contains(type))return 0;
  if(Resistances.Contains(type))amount/=2;
  if(Vulnerabilities.Contains(type))amount*=2;
  return Math.Max(0,amount);
 }
}

public sealed class ResourcePool
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public int Current {get;set;}
 public int Maximum {get;set;}
 public ResourceRecharge Recharge {get;set;}=ResourceRecharge.LongRest;
 public void Restore()=>Current=Maximum;
}

public sealed class FeatureMechanics
{
 public string ResourceId {get;set;}="";
 public int RangeFeet {get;set;}
 public int ExtraDamageDice {get;set;}
 public int ExtraDamageDieSize {get;set;}
 public int ExtraDamageBonus {get;set;}
 public bool Splash {get;set;}
 public int HealingDice {get;set;}
 public int HealingDieSize {get;set;}
 public int HealingBonus {get;set;}
 public int AttacksPerAction {get;set;}
}

public sealed class FeatureEntry
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public FeatureKind Kind {get;set;}=FeatureKind.Other;
 public string Source {get;set;}="";
 public string Description {get;set;}="";
 public List<string> Tags {get;set;}=[];
 public FeatureMechanics Mechanics {get;set;}=new();
 public bool HasTag(string tag)=>Tags.Contains(tag,StringComparer.OrdinalIgnoreCase);
}

public sealed class ConditionState
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public string Source {get;set;}="";
 public int RoundsRemaining {get;set;}=-1;
 public List<string> Tags {get;set;}=[];
}

public sealed class SpellSlotPool
{
 public int Level {get;set;}
 public int Current {get;set;}
 public int Maximum {get;set;}
 public void Restore()=>Current=Maximum;
}

public sealed class SpellcastingSource
{
 public string SourceId {get;set;}="";
 /// <summary>Class/subclass level used for slot/preparation progression. Empty values migrate to SourceId.</summary>
 public string SourceClassId {get;set;}="";
 /// <summary>Class spell list used by prepared-list casters. Known/spellbook casters may still know off-list spells through explicit features.</summary>
 public string SpellListId {get;set;}="";
 public Ability CastingAbility {get;set;}=Ability.Intelligence;
 public SpellPreparationMode PreparationMode {get;set;}=SpellPreparationMode.None;
 public SpellSlotProgression SlotProgression {get;set;}=SpellSlotProgression.None;
 public bool CanRitualCast {get;set;}
 public List<string> Cantrips {get;set;}=[];
 /// <summary>Known spells for known/pact casters; spellbook contents for Spellbook casters.</summary>
 public List<string> KnownSpells {get;set;}=[];
 public List<string> PreparedSpells {get;set;}=[];
 public List<SpellSlotPool> Slots {get;set;}=[];
 public int SpellSaveDc(CharacterSheet character)=>8+character.ProficiencyBonus+character.Abilities.Modifier(CastingAbility);
 public int SpellAttackBonus(CharacterSheet character)=>character.ProficiencyBonus+character.Abilities.Modifier(CastingAbility);
 /// <summary>Legacy convenience used by older UI/tests. IIP30 authoritative checks should use the character-aware overload.</summary>
 public bool SpellAvailable(string spellId)
 {
  bool cantrip=Cantrips.Contains(spellId,StringComparer.OrdinalIgnoreCase);if(cantrip)return true;
  return PreparationMode switch
  {
   SpellPreparationMode.Prepared or SpellPreparationMode.Spellbook=>PreparedSpells.Contains(spellId,StringComparer.OrdinalIgnoreCase),
   SpellPreparationMode.Known or SpellPreparationMode.Pact=>KnownSpells.Contains(spellId,StringComparer.OrdinalIgnoreCase),
   _=>false
  };
 }
 public bool SpellAvailable(string spellId,CharacterSheet character)=>SpellcastingRules.SpellAvailable(character,this,spellId);
 public bool SpendSlot(int minimumLevel,out int slotLevel)
 {
  var slot=Slots.Where(s=>s.Level>=minimumLevel&&s.Current>0).OrderBy(s=>s.Level).FirstOrDefault();
  if(slot==null){slotLevel=0;return false;}
  slot.Current--;slotLevel=slot.Level;return true;
 }
 public void RestoreSlots(){foreach(var slot in Slots)slot.Restore();}
}

public sealed class AttackProfile
{
 public string Id {get;set;}="basic";
 public string Name {get;set;}="Strike";
 public Ability Ability {get;set;}=Ability.Strength;
 public bool Proficient {get;set;}=true;
 public int DamageDice {get;set;}=1;
 public int DamageDieSize {get;set;}=6;
 public int FlatDamageBonus {get;set;}
 public int RangeFeet {get;set;}=5;
 public DamageType DamageType {get;set;}=DamageType.Slashing;
 public int? AttackBonusOverride {get;set;}
 public int? DamageBonusOverride {get;set;}
 public int AttackBonus(CharacterSheet character)=>AttackBonusOverride??character.Abilities.Modifier(Ability)+(Proficient?character.ProficiencyBonus:0);
 public int DamageBonus(CharacterSheet character)=>DamageBonusOverride??character.Abilities.Modifier(Ability)+FlatDamageBonus;
 public int RangeTiles=>Math.Max(1,(int)MathF.Ceiling(RangeFeet/5f));
}

public sealed class CharacterSheet
{
 public string Species {get;set;}="Human";
 public string Background {get;set;}="Adventurer";
 public string Alignment {get;set;}="Unaligned";
 public CharacterNarrative Narrative {get;set;}=new();
 public int Experience {get;set;}
 public int SpeedFeet {get;set;}=30;
 public AbilityScores Abilities {get;set;}=new();
 public List<ClassLevelEntry> Classes {get;set;}=[];
 public HitPointPool HitPoints {get;set;}=new();
 public List<HitDiePool> HitDice {get;set;}=[];
 public ProficiencyData Proficiencies {get;set;}=new();
 public DefenseData Defenses {get;set;}=new();
 public List<FeatureEntry> Features {get;set;}=[];
 public List<ResourcePool> Resources {get;set;}=[];
 public List<ConditionState> Conditions {get;set;}=[];
 public List<SpellcastingSource> Spellcasting {get;set;}=[];
 /// <summary>IIP30 durable single-concentration state. A mirrored condition keeps portraits/Record compatible with the pre-IIP30 effect UI.</summary>
 public SpellConcentrationState? Concentration {get;set;}
 public AttackProfile MainAttack {get;set;}=new();
 [JsonIgnore] public int TotalLevel=>Math.Max(1,Classes.Sum(c=>Math.Max(0,c.Level)));
 [JsonIgnore] public int ProficiencyBonus=>Math.Clamp(2+(TotalLevel-1)/4,2,6);
 [JsonIgnore] public int AttacksPerAction=>Math.Max(1,Features.Where(f=>f.HasTag("extra-attack")).Select(f=>f.Mechanics.AttacksPerAction).DefaultIfEmpty(1).Max());
 [JsonIgnore] public ClassLevelEntry? PrimaryClass=>Classes.OrderByDescending(c=>c.Level).ThenBy(c=>Classes.IndexOf(c)).FirstOrDefault();
 [JsonIgnore] public string PrimaryClassName=>PrimaryClass?.ClassId??"Adventurer";
 [JsonIgnore] public string SubclassName=>PrimaryClass?.SubclassId??"";
 public int AbilityModifier(Ability ability)=>Abilities.Modifier(ability);
 public int SaveModifier(Ability ability)=>AbilityModifier(ability)+(Proficiencies.SaveProficient(ability)?ProficiencyBonus:0);
 public int SkillModifier(Skill skill)=>SkillModifier(skill,null);
 public int SkillModifier(Skill skill,Ability? abilityOverride)
 {
  // IIP39: 5e skill checks are ability checks that may use an authored alternate ability
  // (for example Strength (Intimidation)) while retaining the skill's proficiency rank.
  int bonus=AbilityModifier(abilityOverride??SkillRules.AbilityFor(skill));
  return Proficiencies.SkillRank(skill) switch
  {
   ProficiencyRank.Half=>bonus+ProficiencyBonus/2,
   ProficiencyRank.Proficient=>bonus+ProficiencyBonus,
   ProficiencyRank.Expertise=>bonus+ProficiencyBonus*2,
   _=>bonus
  };
 }
 public ResourcePool? Resource(string id)=>Resources.FirstOrDefault(r=>r.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
 public FeatureEntry? Feature(string id)=>Features.FirstOrDefault(f=>f.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
 public FeatureEntry? FeatureWithTag(string tag)=>Features.FirstOrDefault(f=>f.HasTag(tag));
 public bool HasCondition(string id)=>Conditions.Any(c=>c.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
 public bool HasConditionTag(string tag)=>Conditions.Any(c=>c.Tags.Any(t=>t.Equals(tag,StringComparison.OrdinalIgnoreCase)));
 public ConditionState? FirstConditionWithTag(string tag)=>Conditions.FirstOrDefault(c=>c.Tags.Any(t=>t.Equals(tag,StringComparison.OrdinalIgnoreCase)));
 [JsonIgnore] public int NextLevelExperience=>PrimaryClassName=="Survivor"?SurvivalProgression.RequiredForLevel(TotalLevel+1):TotalLevel>=20?int.MaxValue:ExperienceRules.ForLevel(TotalLevel+1);
 [JsonIgnore] public bool CanLevelUp=>PrimaryClassName!="Survivor"&&TotalLevel<20&&Experience>=NextLevelExperience;
 public void SetCondition(string id,string name,bool active,string source="",IEnumerable<string>? tags=null)
 {
  Conditions.RemoveAll(c=>c.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
  if(active&&Defenses.ConditionImmunities.Any(x=>x.Equals(id,StringComparison.OrdinalIgnoreCase)||x.Equals(name,StringComparison.OrdinalIgnoreCase)))return;
  if(active)Conditions.Add(new(){Id=id,Name=name,Source=source,Tags=tags?.ToList()??[]});
 }
 public void RestoreShortRest()
 {
  // IIP43: short rests refresh explicitly short-rest resources and pact slots only.
  foreach(var resource in Resources.Where(r=>r.Recharge==ResourceRecharge.ShortRest))resource.Restore();
  foreach(var source in Spellcasting.Where(s=>s.PreparationMode==SpellPreparationMode.Pact))source.RestoreSlots();
 }
 public void RestoreLongRest()
 {
  // Ordinary rest restores living/downed characters but never clears an explicit death state.
  if(!HasCondition("dead")){HitPoints.Current=HitPoints.Maximum;SetCondition("unconscious","Unconscious",false);}
  foreach(var resource in Resources.Where(r=>r.Recharge is ResourceRecharge.ShortRest or ResourceRecharge.LongRest))resource.Restore();
  foreach(var source in Spellcasting)source.RestoreSlots();
  SpellcastingRules.BreakConcentration(this);
 }
 public void AdvancePrimaryClassTo(int level)
 {
  if(Classes.Count==0)Classes.Add(new(){Level=Math.Max(1,level)});
  var primary=Classes[0];int previous=Math.Max(1,primary.Level);primary.Level=Math.Max(previous,level);
  var pool=HitDice.FirstOrDefault(h=>h.SourceClass.Equals(primary.ClassId,StringComparison.OrdinalIgnoreCase));
  if(pool==null){pool=new(){SourceClass=primary.ClassId,DieSize=primary.HitDieSize};HitDice.Add(pool);}
  int gained=Math.Max(0,primary.Level-pool.Maximum);pool.Maximum=primary.Level;pool.Current=Math.Min(pool.Maximum,pool.Current+gained);
 }
 public void ValidateOrThrow(string actorId)
 {
  foreach(var (_,score,_) in Abilities.All())if(score<1||score>30)throw new InvalidDataException($"Invalid ability score on {actorId}.");
  if(Classes.Count==0||Classes.Any(c=>string.IsNullOrWhiteSpace(c.ClassId)||c.Level<1||c.Level>20||c.HitDieSize<4))throw new InvalidDataException($"Invalid class progression on {actorId}.");
  if(HitPoints.Maximum<1||HitPoints.Current<0||HitPoints.Current>HitPoints.Maximum||HitPoints.Temporary<0)throw new InvalidDataException($"Invalid hit points on {actorId}.");
  if(SpeedFeet<0||SpeedFeet>240||Defenses.ArmorClassBase<0||Defenses.ArmorClassBase>40)throw new InvalidDataException($"Invalid movement/defense data on {actorId}.");
  if(MainAttack.DamageDice<1||MainAttack.DamageDieSize<2||MainAttack.RangeFeet<0)throw new InvalidDataException($"Invalid attack profile on {actorId}.");
  if(Resources.Any(r=>string.IsNullOrWhiteSpace(r.Id)||r.Maximum<0||r.Current<0||r.Current>r.Maximum)||Resources.Select(r=>r.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Resources.Count)throw new InvalidDataException($"Invalid resources on {actorId}.");
  if(Proficiencies.Skills.GroupBy(s=>s.Skill).Any(g=>g.Count()>1))throw new InvalidDataException($"Duplicate skill training on {actorId}.");
  if(Spellcasting.SelectMany(s=>s.Slots).Any(s=>s.Level<1||s.Level>9||s.Maximum<0||s.Current<0||s.Current>s.Maximum))throw new InvalidDataException($"Invalid spell slots on {actorId}.");
  if(Spellcasting.Select(s=>s.SourceId).Where(id=>!string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Spellcasting.Count)throw new InvalidDataException($"Duplicate/invalid spellcasting sources on {actorId}.");
  foreach(var source in Spellcasting)
  {
   foreach(var id in source.Cantrips.Concat(source.KnownSpells).Concat(source.PreparedSpells))if(!SpellCatalog.TryGet(id,out _))throw new InvalidDataException($"Unknown spell '{id}' on {actorId}.");
   if(source.Cantrips.Any(id=>SpellCatalog.Get(id).Level!=0))throw new InvalidDataException($"Non-cantrip stored in cantrip list on {actorId}.");
  }
  if(Concentration!=null&&(!SpellCatalog.TryGet(Concentration.SpellId,out var concentrationSpell)||!concentrationSpell.Concentration))throw new InvalidDataException($"Invalid concentration spell on {actorId}.");
 }
}
