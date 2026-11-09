namespace Ashen.Core;

/// <summary>IIP47 transient choices made before a new journey begins.</summary>
public sealed class CharacterCreationDraft
{
 public string Name {get;set;}="";
 public string Pronouns {get;set;}="they/them";
 public string VoiceSet {get;set;}="Stoic";
 public int Portrait {get;set;}
 public string SpeciesId {get;set;}="Human";
 public string ClassId {get;set;}="Fighter";
 public string BackgroundId {get;set;}="Soldier";
 public string Alignment {get;set;}="Neutral Good";
 public string SubclassId {get;set;}="";
 public Dictionary<Ability,int> AbilityAssignments {get;set;}=[];
 public List<Skill> ClassSkillChoices {get;set;}=[];
 public List<Skill> SpeciesSkillChoices {get;set;}=[];
 public string EquipmentPackageId {get;set;}="fighter-sword";
 public List<string> CantripChoices {get;set;}=[];
 public List<string> SpellChoices {get;set;}=[];
 public List<string> PreparedSpellChoices {get;set;}=[];
}

public sealed record CreationSpecies(
 string Id,string Name,int SpeedFeet,IReadOnlyDictionary<Ability,int> AbilityBonuses,int BonusSkillChoices,string Description,string[] Languages,string[] Traits);
public sealed record CreationBackground(string Id,string Name,Skill[] Skills,string[] Tools,string[] Languages,string Description);
public sealed record CreationEquipmentPackage(string Id,string Name,string Description,string[] ItemIds);
public sealed record CreationClass(
 string Id,int HitDie,Ability[] Saves,int SkillChoiceCount,Skill[] SkillOptions,string[] Weapons,string[] Armor,string[] Tools,
 int BaseArmorClass,Ability AttackAbility,int DamageDieSize,int RangeFeet,DamageType DamageType,int CantripChoices,int SpellChoices,
 CreationEquipmentPackage[] EquipmentPackages,string Description);

/// <summary>
/// IIP47 rules authority. Character creation is data-driven and validated atomically before a protagonist is built.
/// The campaign uses the same six supported 5e classes, spell catalogue, inventory and character sheet as runtime play.
/// </summary>
public static class CharacterCreationRules
{
 public static readonly int[] StandardArray=[15,14,13,12,10,8];
 public static readonly string[] PronounOptions=["they/them","she/her","he/him"];
 public static readonly string[] VoiceOptions=["Stoic","Warm","Scholarly","Wry","Bold","Quiet"];
 public static readonly string[] AlignmentOptions=["Lawful Good","Neutral Good","Chaotic Good","Lawful Neutral","True Neutral","Chaotic Neutral"];

 static readonly CreationSpecies[] species=
 [
  new("Human","Human",30,new Dictionary<Ability,int>{{Ability.Strength,1},{Ability.Dexterity,1},{Ability.Constitution,1},{Ability.Intelligence,1},{Ability.Wisdom,1},{Ability.Charisma,1}},0,"Adaptable people found throughout the covenant roads.",["Common"],["Versatile Heritage"]),
  new("Wood Elf","Wood Elf",35,new Dictionary<Ability,int>{{Ability.Dexterity,2},{Ability.Wisdom,1}},0,"Swift woodland folk with keen senses and fey ancestry.",["Common","Elvish"],["Darkvision","Fey Ancestry","Trance","Mask of the Wild"]),
  new("Hill Dwarf","Hill Dwarf",25,new Dictionary<Ability,int>{{Ability.Constitution,2},{Ability.Wisdom,1}},0,"Stout folk whose endurance is famous among shrine-road pilgrims.",["Common","Dwarvish"],["Darkvision","Dwarven Resilience","Dwarven Toughness"]),
  new("Lightfoot Halfling","Lightfoot Halfling",25,new Dictionary<Ability,int>{{Ability.Dexterity,2},{Ability.Charisma,1}},0,"Small, nimble travelers with remarkable nerve and luck.",["Common","Halfling"],["Lucky","Brave","Halfling Nimbleness"]),
  new("Half-Elf","Half-Elf",30,new Dictionary<Ability,int>{{Ability.Charisma,2},{Ability.Dexterity,1},{Ability.Wisdom,1}},2,"People of mixed human and elven heritage who move easily between communities.",["Common","Elvish"],["Darkvision","Fey Ancestry","Skill Versatility"])
 ];

 static readonly CreationBackground[] backgrounds=
 [
  new("Soldier","Soldier",[Skill.Athletics,Skill.Intimidation],["Gaming set"],[],"You served in an organized fighting force and learned discipline under pressure."),
  new("Outlander","Outlander",[Skill.Athletics,Skill.Survival],["Herbalism kit"],[],"You learned to read weather, tracks and danger far from settled roads."),
  new("Sage","Sage",[Skill.Arcana,Skill.History],[],["One scholarly language"],"You spent years studying old records, symbols and forgotten arguments."),
  new("Acolyte","Acolyte",[Skill.Insight,Skill.Religion],[],["One liturgical language"],"You served a shrine or religious community and know its rites and obligations."),
  new("Urchin","Urchin",[Skill.SleightOfHand,Skill.Stealth],["Thieves' tools"],[],"You survived by noticing openings, exits and the things respectable people overlook."),
  new("Entertainer","Entertainer",[Skill.Acrobatics,Skill.Performance],["Lute"],[],"You made a living before audiences and learned how to hold a room's attention.")
 ];

 static readonly CreationClass[] classes=
 [
  new("Fighter",10,[Ability.Strength,Ability.Constitution],2,[Skill.Acrobatics,Skill.AnimalHandling,Skill.Athletics,Skill.History,Skill.Insight,Skill.Intimidation,Skill.Perception,Skill.Survival],["Simple weapons","Martial weapons","Longswords","Quarterstaffs"],["Light armor","Medium armor","Heavy armor","Shields"],[],14,Ability.Strength,8,5,DamageType.Slashing,0,0,
   [new("fighter-sword","Sword & Shield","Longsword, shield, mail and a healing potion.",["sword","shield","mail","healing-potion"]),new("fighter-bow","Longbow Kit","Longbow, arrows, mail and a healing potion.",["bow","arrows","mail","healing-potion"])],"A durable martial adventurer with Second Wind and strong equipment access."),
  new("Ranger",10,[Ability.Strength,Ability.Dexterity],3,[Skill.AnimalHandling,Skill.Athletics,Skill.Insight,Skill.Investigation,Skill.Nature,Skill.Perception,Skill.Stealth,Skill.Survival],["Simple weapons","Martial weapons","Longswords"],["Light armor","Medium armor","Shields"],["Herbalism kit"],14,Ability.Dexterity,8,35,DamageType.Piercing,0,0,
   [new("ranger-bow","Longbow Hunter","Longbow, arrows, mail and a healing potion.",["bow","arrows","mail","healing-potion"]),new("ranger-blade","Blade & Shield","Longsword, shield, mail and trail boots.",["sword","shield","mail","trail-boots"])],"A wilderness martial specialist. Spellcasting begins at Ranger level 2."),
  new("Wizard",6,[Ability.Intelligence,Ability.Wisdom],2,[Skill.Arcana,Skill.History,Skill.Insight,Skill.Investigation,Skill.Medicine,Skill.Religion],["Quarterstaffs"],[],[],12,Ability.Intelligence,6,30,DamageType.Force,3,6,
   [new("wizard-road","Road Scholar","Ashwood staff, trail boots and a healing potion.",["staff","trail-boots","healing-potion"]),new("wizard-kit","Field Scholar","Ashwood staff and two healing potions.",["staff","healing-potion","healing-potion"])],"A spellbook caster. Choose three cantrips and six 1st-level spells, then prepare a legal subset."),
  new("Cleric",8,[Ability.Wisdom,Ability.Charisma],2,[Skill.History,Skill.Insight,Skill.Medicine,Skill.Persuasion,Skill.Religion],["Simple weapons"],["Light armor","Medium armor","Shields"],[],15,Ability.Strength,6,5,DamageType.Bludgeoning,3,0,
   [new("cleric-shield","Pilgrim Guardian","Mace, shield, mail and a healing potion.",["mace","shield","mail","healing-potion"]),new("cleric-healer","Hospitaller","Mace, mail and two healing potions.",["mace","mail","healing-potion","healing-potion"])],"A prepared divine caster. Clerics choose their domain at level 1."),
  new("Rogue",8,[Ability.Dexterity,Ability.Intelligence],4,[Skill.Acrobatics,Skill.Athletics,Skill.Deception,Skill.Insight,Skill.Intimidation,Skill.Investigation,Skill.Perception,Skill.Performance,Skill.Persuasion,Skill.SleightOfHand,Skill.Stealth],["Simple weapons","Longswords"],["Light armor"],["Thieves' tools"],14,Ability.Dexterity,6,5,DamageType.Piercing,0,0,
   [new("rogue-blade","Street Blade","Longsword, mail, trail boots and a healing potion.",["sword","mail","trail-boots","healing-potion"]),new("rogue-light","Light Kit","Longsword, trail boots and two healing potions.",["sword","trail-boots","healing-potion","healing-potion"])],"A skill-heavy expert with Sneak Attack and Expertise."),
  new("Bard",8,[Ability.Dexterity,Ability.Charisma],3,Enum.GetValues<Skill>(),["Simple weapons","Longswords"],["Light armor"],["Lute"],14,Ability.Dexterity,6,30,DamageType.Psychic,2,4,
   [new("bard-bow","Traveling Performer","Longbow, arrows, mail and a healing potion.",["bow","arrows","mail","healing-potion"]),new("bard-blade","Road Minstrel","Longsword, mail, trail boots and a healing potion.",["sword","mail","trail-boots","healing-potion"])],"A versatile known-spell caster and skill expert with Bardic Inspiration.")
 ];

 public static IReadOnlyList<CreationSpecies> Species=>species;
 public static IReadOnlyList<CreationBackground> Backgrounds=>backgrounds;
 public static IReadOnlyList<CreationClass> Classes=>classes;
 public static CreationSpecies SpeciesById(string id)=>species.First(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
 public static CreationBackground BackgroundById(string id)=>backgrounds.First(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
 public static CreationClass ClassById(string id)=>classes.First(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
 public static CreationEquipmentPackage EquipmentById(string id)=>classes.SelectMany(x=>x.EquipmentPackages).First(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
 public static IReadOnlyList<SpellDefinition> CantripOptions(string classId)=>classId is "Wizard" or "Cleric" or "Bard"?SpellCatalog.ForClass(classId,0).Where(s=>s.Level==0).ToArray():[];
 public static IReadOnlyList<SpellDefinition> FirstLevelSpellOptions(string classId)=>classId is "Wizard" or "Cleric" or "Bard"?SpellCatalog.ForClass(classId,1).Where(s=>s.Level==1).ToArray():[];
 public static int PreparedSpellCount(CharacterCreationDraft d)
 {
  if(d.ClassId is not ("Wizard" or "Cleric"))return 0;var s=FinalAbilities(d);var ability=d.ClassId=="Wizard"?Ability.Intelligence:Ability.Wisdom;return Math.Max(1,1+s.Modifier(ability));
 }
 public static CharacterCreationDraft NewDraft()
 {
  var d=new CharacterCreationDraft();ApplyRecommendedAbilities(d);ResetClassDependentChoices(d);return d;
 }
 public static void ApplyRecommendedAbilities(CharacterCreationDraft d)
 {
  Ability[] order=d.ClassId switch
  {
   "Fighter"=>[Ability.Strength,Ability.Constitution,Ability.Dexterity,Ability.Wisdom,Ability.Charisma,Ability.Intelligence],
   "Ranger"=>[Ability.Dexterity,Ability.Wisdom,Ability.Constitution,Ability.Strength,Ability.Intelligence,Ability.Charisma],
   "Wizard"=>[Ability.Intelligence,Ability.Constitution,Ability.Dexterity,Ability.Wisdom,Ability.Charisma,Ability.Strength],
   "Cleric"=>[Ability.Wisdom,Ability.Constitution,Ability.Strength,Ability.Charisma,Ability.Dexterity,Ability.Intelligence],
   "Rogue"=>[Ability.Dexterity,Ability.Constitution,Ability.Intelligence,Ability.Charisma,Ability.Wisdom,Ability.Strength],
   _=>[Ability.Charisma,Ability.Dexterity,Ability.Constitution,Ability.Wisdom,Ability.Intelligence,Ability.Strength]
  };
  d.AbilityAssignments.Clear();for(int i=0;i<order.Length;i++)d.AbilityAssignments[order[i]]=StandardArray[i];
 }
 public static void ResetClassDependentChoices(CharacterCreationDraft d)
 {
  var c=ClassById(d.ClassId);d.ClassSkillChoices.Clear();d.SpeciesSkillChoices.Clear();d.SubclassId=c.Id=="Cleric"?LevelingRules.SubclassOptions("Cleric")[0]:"";d.EquipmentPackageId=c.EquipmentPackages[0].Id;
  d.CantripChoices=CantripOptions(c.Id).Take(c.CantripChoices).Select(s=>s.Id).ToList();d.SpellChoices=FirstLevelSpellOptions(c.Id).Take(c.SpellChoices).Select(s=>s.Id).ToList();d.PreparedSpellChoices=[];
  if(c.Id=="Wizard")d.PreparedSpellChoices=d.SpellChoices.Take(PreparedSpellCount(d)).ToList();
  else if(c.Id=="Cleric")d.PreparedSpellChoices=FirstLevelSpellOptions(c.Id).Take(PreparedSpellCount(d)).Select(s=>s.Id).ToList();
 }
 public static AbilityScores FinalAbilities(CharacterCreationDraft d)
 {
  var ancestry=species.FirstOrDefault(x=>x.Id.Equals(d.SpeciesId,StringComparison.OrdinalIgnoreCase));
  int Score(Ability a){int value=d.AbilityAssignments.TryGetValue(a,out var n)?n:10;if(ancestry!=null&&ancestry.AbilityBonuses.TryGetValue(a,out var bonus))value+=bonus;return value;}
  return new(){Strength=Score(Ability.Strength),Dexterity=Score(Ability.Dexterity),Constitution=Score(Ability.Constitution),Intelligence=Score(Ability.Intelligence),Wisdom=Score(Ability.Wisdom),Charisma=Score(Ability.Charisma)};
 }
 public static bool Validate(CharacterCreationDraft d,out IReadOnlyList<string> issues)
 {
  var errors=new List<string>();
  if(string.IsNullOrWhiteSpace(d.Name)||d.Name.Trim().Length<2||d.Name.Trim().Length>24||d.Name.Any(char.IsControl))errors.Add("Enter a name between 2 and 24 characters.");
  if(!PronounOptions.Contains(d.Pronouns,StringComparer.OrdinalIgnoreCase))errors.Add("Choose a supported pronoun set.");
  if(!VoiceOptions.Contains(d.VoiceSet,StringComparer.OrdinalIgnoreCase))errors.Add("Choose a voice set.");
  if(d.Portrait<0||d.Portrait>5)errors.Add("Choose a portrait.");
  var sp=species.FirstOrDefault(x=>x.Id.Equals(d.SpeciesId,StringComparison.OrdinalIgnoreCase));if(sp==null)errors.Add("Choose a species.");
  var cl=classes.FirstOrDefault(x=>x.Id.Equals(d.ClassId,StringComparison.OrdinalIgnoreCase));if(cl==null)errors.Add("Choose a supported class.");
  var bg=backgrounds.FirstOrDefault(x=>x.Id.Equals(d.BackgroundId,StringComparison.OrdinalIgnoreCase));if(bg==null)errors.Add("Choose a background.");
  if(!AlignmentOptions.Contains(d.Alignment,StringComparer.OrdinalIgnoreCase))errors.Add("Choose an alignment.");
  if(d.AbilityAssignments.Count!=6||Enum.GetValues<Ability>().Any(a=>!d.AbilityAssignments.ContainsKey(a))||!d.AbilityAssignments.Values.Order().SequenceEqual(StandardArray.Order()))errors.Add("Assign each standard-array score (15, 14, 13, 12, 10, 8) exactly once.");
  if(cl!=null&&bg!=null)
  {
   var bgSkills=bg.Skills.ToHashSet();
   if(d.ClassSkillChoices.Distinct().Count()!=cl.SkillChoiceCount||d.ClassSkillChoices.Any(s=>!cl.SkillOptions.Contains(s)||bgSkills.Contains(s)))errors.Add($"Choose exactly {cl.SkillChoiceCount} legal {cl.Id} skills that do not duplicate background training.");
   int extra=sp?.BonusSkillChoices??0;if(d.SpeciesSkillChoices.Distinct().Count()!=extra||d.SpeciesSkillChoices.Any(s=>bgSkills.Contains(s)||d.ClassSkillChoices.Contains(s)))errors.Add(extra==0?"This species grants no additional skill choices.":$"Choose exactly {extra} additional non-duplicate species skills.");
   bool needsSubclass=LevelingRules.Class(cl.Id).SubclassLevel==1;if(needsSubclass&&!LevelingRules.SubclassOptions(cl.Id).Contains(d.SubclassId,StringComparer.OrdinalIgnoreCase))errors.Add("Choose the class's level-1 subclass.");
   if(!needsSubclass&&!string.IsNullOrWhiteSpace(d.SubclassId))errors.Add("This class does not choose a subclass at level 1.");
   if(!cl.EquipmentPackages.Any(p=>p.Id.Equals(d.EquipmentPackageId,StringComparison.OrdinalIgnoreCase)))errors.Add("Choose a starting equipment package for the selected class.");
   var cantripOptions=CantripOptions(cl.Id).Select(s=>s.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);if(d.CantripChoices.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=cl.CantripChoices||d.CantripChoices.Any(x=>!cantripOptions.Contains(x)))errors.Add($"Choose exactly {cl.CantripChoices} legal cantrip{(cl.CantripChoices==1?"":"s")}.");
   var spellOptions=FirstLevelSpellOptions(cl.Id).Select(s=>s.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);if(d.SpellChoices.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=cl.SpellChoices||d.SpellChoices.Any(x=>!spellOptions.Contains(x)))errors.Add($"Choose exactly {cl.SpellChoices} legal 1st-level spell{(cl.SpellChoices==1?"":"s")}.");
   int prepared=PreparedSpellCount(d);
   if(cl.Id=="Wizard"&&(d.PreparedSpellChoices.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=prepared||d.PreparedSpellChoices.Any(x=>!d.SpellChoices.Contains(x,StringComparer.OrdinalIgnoreCase))))errors.Add($"Prepare exactly {prepared} spells from the Wizard spellbook.");
   if(cl.Id=="Cleric"&&(d.PreparedSpellChoices.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=prepared||d.PreparedSpellChoices.Any(x=>!spellOptions.Contains(x))))errors.Add($"Prepare exactly {prepared} Cleric spells.");
   if(cl.Id is not ("Wizard" or "Cleric")&&d.PreparedSpellChoices.Count>0)errors.Add("This class does not prepare spells at level 1.");
  }
  issues=errors;return errors.Count==0;
 }

 static void AddFeature(CharacterSheet s,string id,string name,string source,string description,FeatureKind kind=FeatureKind.ClassFeature,string[]? tags=null,FeatureMechanics? mechanics=null)
 {if(s.Feature(id)==null)s.Features.Add(new(){Id=id,Name=name,Source=source,Description=description,Kind=kind,Tags=tags?.ToList()??[],Mechanics=mechanics??new()});}
 static void AddResource(CharacterSheet s,string id,string name,int maximum,ResourceRecharge recharge=ResourceRecharge.LongRest)=>s.Resources.Add(new(){Id=id,Name=name,Current=maximum,Maximum=maximum,Recharge=recharge});
 static ItemStack Stack(string id,int quantity=1)=>new(){ItemId=id,Quantity=quantity,Charges=Items.Get(id).MaxCharges>0?Items.Get(id).MaxCharges:-1,Identified=true};
 static void AddUnique(List<string> target,IEnumerable<string> values){foreach(var v in values)if(!target.Contains(v,StringComparer.OrdinalIgnoreCase))target.Add(v);}

 public static Actor BuildActor(CharacterCreationDraft d,string actorId="protagonist",Cell? position=null)
 {
  if(!Validate(d,out var issues))throw new InvalidOperationException("Illegal character: "+string.Join(" ",issues));
  var sp=SpeciesById(d.SpeciesId);var cl=ClassById(d.ClassId);var bg=BackgroundById(d.BackgroundId);var abilities=FinalAbilities(d);int hp=Math.Max(1,cl.HitDie+abilities.Modifier(Ability.Constitution)+(sp.Id=="Hill Dwarf"?1:0));
  var prof=new ProficiencyData{SavingThrows=cl.Saves.ToList()};AddUnique(prof.Weapons,cl.Weapons);AddUnique(prof.Armor,cl.Armor);AddUnique(prof.Tools,cl.Tools);AddUnique(prof.Tools,bg.Tools);AddUnique(prof.Languages,sp.Languages);AddUnique(prof.Languages,bg.Languages);
  foreach(var skill in bg.Skills.Concat(d.ClassSkillChoices).Concat(d.SpeciesSkillChoices).Distinct())prof.Skills.Add(new(){Skill=skill,Rank=ProficiencyRank.Proficient});
  var sheet=new CharacterSheet{Species=sp.Name,Background=bg.Name,Alignment=d.Alignment,SpeedFeet=sp.SpeedFeet,Abilities=abilities,Classes=[new(){ClassId=cl.Id,SubclassId=d.SubclassId,Level=1,HitDieSize=cl.HitDie,SpellcastingAbility=cl.Id switch{"Wizard"=>Ability.Intelligence,"Cleric"=>Ability.Wisdom,"Bard"=>Ability.Charisma,_=>(Ability?)null}}],HitPoints=new(){Current=hp,Maximum=hp},HitDice=[new(){SourceClass=cl.Id,DieSize=cl.HitDie,Current=1,Maximum=1}],Proficiencies=prof,Defenses=new(){ArmorClassBase=cl.BaseArmorClass},MainAttack=new(){Name=cl.Id switch{"Wizard"=>"Arcane Bolt","Cleric"=>"Pilgrim's Mace","Bard"=>"Discordant Shot","Ranger"=>"Longbow","Rogue"=>"Finesse Strike",_=>"Longsword"},Ability=cl.AttackAbility,Proficient=true,DamageDice=1,DamageDieSize=cl.DamageDieSize,RangeFeet=cl.RangeFeet,DamageType=cl.DamageType}};
  sheet.Narrative=new(){Biography=$"{d.Name.Trim()} enters the Ashen Covenant as a {sp.Name.ToLowerInvariant()} {cl.Id.ToLowerInvariant()} with the {bg.Name.ToLowerInvariant()} background. This history is intentionally concise: later dialogue and player choices write the protagonist's actual story into the journey.",Personality="Player-defined protagonist.",Affiliations=["Ashen Covenant"],Notes=[$"Voice: {d.VoiceSet}",$"Pronouns: {d.Pronouns}"]};
  foreach(var trait in sp.Traits)AddFeature(sheet,"species-"+trait.ToLowerInvariant().Replace(' ','-'),trait,sp.Name,$"A {sp.Name} ancestry trait.",FeatureKind.SpeciesTrait);
  AddFeature(sheet,"background-"+bg.Id.ToLowerInvariant(),bg.Name,bg.Name,bg.Description,FeatureKind.BackgroundFeature);
  switch(cl.Id)
  {
   case "Fighter":AddFeature(sheet,"fighting-style-defense","Defense Fighting Style","Fighter","Training with armor and defensive positioning.");AddFeature(sheet,"second-wind","Second Wind","Fighter","Recover during battle.",tags:["actionbar","healing"],mechanics:new(){ResourceId="second-wind",HealingDice=1,HealingDieSize=10,HealingBonus=1});AddResource(sheet,"second-wind","Second Wind",1,ResourceRecharge.ShortRest);AddFeature(sheet,"fighter-signature","Cleave","Fighter","A forceful martial attack.",tags:["signature"],mechanics:new(){ResourceId="signature",RangeFeet=5,ExtraDamageDice=1,ExtraDamageDieSize=4});AddResource(sheet,"signature","Cleave",3);break;
   case "Ranger":AddFeature(sheet,"favored-explorer","Favored Explorer","Ranger","Training for pursuit and travel.");AddFeature(sheet,"ranger-signature","Power Shot","Ranger","Commit to a powerful ranged shot.",tags:["signature"],mechanics:new(){ResourceId="signature",RangeFeet=35,ExtraDamageDice=1,ExtraDamageDieSize=6});AddResource(sheet,"signature","Power Shot",3);break;
   case "Wizard":AddFeature(sheet,"arcane-recovery","Arcane Recovery","Wizard","Recover magical endurance during a short rest.");AddFeature(sheet,"wizard-signature","Ember Burst","Wizard","Release a compact burst of force.",tags:["signature"],mechanics:new(){ResourceId="signature",RangeFeet=30,ExtraDamageDice=1,ExtraDamageDieSize=6,Splash=true});AddResource(sheet,"signature","Ember Burst",3);break;
   case "Cleric":AddFeature(sheet,"cleric-domain","Divine Domain",d.SubclassId,$"Your calling follows the {d.SubclassId}.",FeatureKind.SubclassFeature);AddFeature(sheet,"cleric-signature","Sacred Flame","Cleric","Radiant divine attack.",tags:["signature"],mechanics:new(){ResourceId="signature",RangeFeet=30,ExtraDamageDice=1,ExtraDamageDieSize=4});AddResource(sheet,"signature","Sacred Flame",3);AddFeature(sheet,"healing-prayer","Healing Prayer","Cleric","Restore an ally.",tags:["healing"],mechanics:new(){ResourceId="healing",RangeFeet=30,HealingDice=1,HealingDieSize=8,HealingBonus=abilities.Modifier(Ability.Wisdom)});AddResource(sheet,"healing","Healing Prayer",3);break;
   case "Rogue":AddFeature(sheet,"sneak-attack","Sneak Attack","Rogue","Deal extra damage when an opening permits it.",mechanics:new(){ExtraDamageDice=1,ExtraDamageDieSize=6});AddFeature(sheet,"expertise","Expertise","Rogue","Two trained skills become areas of exceptional expertise.");foreach(var skill in d.ClassSkillChoices.Take(2)){var row=sheet.Proficiencies.Skills.First(x=>x.Skill==skill);row.Rank=ProficiencyRank.Expertise;}AddFeature(sheet,"rogue-signature","Cunning Strike","Rogue","Exploit an opening with precision.",tags:["signature"],mechanics:new(){ResourceId="signature",RangeFeet=5,ExtraDamageDice=1,ExtraDamageDieSize=6});AddResource(sheet,"signature","Cunning Strike",3);break;
   case "Bard":AddFeature(sheet,"bardic-inspiration","Bardic Inspiration","Bard","Inspire an ally with words or music.",tags:["actionbar"]);AddResource(sheet,"bardic-inspiration","Bardic Inspiration",Math.Max(1,abilities.Modifier(Ability.Charisma)),ResourceRecharge.LongRest);AddFeature(sheet,"bard-signature","Discordant Shot","Bard","A sharp magical attack carried on sound.",tags:["signature"],mechanics:new(){ResourceId="signature",RangeFeet=30,ExtraDamageDice=1,ExtraDamageDieSize=4});AddResource(sheet,"signature","Discordant Shot",3);break;
  }
  if(cl.Id is "Wizard" or "Cleric" or "Bard")
  {
   var source=SpellcastingRules.CreateSource(cl.Id);source.Cantrips=d.CantripChoices.ToList();if(cl.Id is "Wizard" or "Bard")source.KnownSpells=d.SpellChoices.ToList();source.PreparedSpells=d.PreparedSpellChoices.ToList();SpellcastingRules.RebuildSlots(source,1);sheet.Spellcasting.Add(source);SpellcastingRules.NormalizeCharacter(sheet);
  }
  var actor=new Actor{Id=actorId,Name=d.Name.Trim(),PlayerCreated=true,Pronouns=d.Pronouns,VoiceSet=d.VoiceSet,Portrait=d.Portrait,Pos=position??new Cell(6,15),Tactics=cl.Id switch{"Ranger" or "Wizard"=>TacticsProfile.Ranged,"Cleric" or "Bard"=>TacticsProfile.Support,"Rogue"=>TacticsProfile.Aggressive,_=>TacticsProfile.Defensive},Character=sheet,Inventory=new(){MaxSlots=18},Equipment=new(),QuickAccess=new()};
  var package=EquipmentById(d.EquipmentPackageId);foreach(var id in package.ItemIds)
  {
   var item=Items.Get(id);var stack=Stack(id,id=="arrows"?40:1);EquipmentSlot? slot=id switch{"sword" or "bow" or "staff" or "mace"=>EquipmentSlot.MainHand,"shield"=>EquipmentSlot.OffHand,"mail"=>EquipmentSlot.Armor,"arrows"=>EquipmentSlot.Ammo1,"trail-boots"=>EquipmentSlot.Boots,"iron-helm"=>EquipmentSlot.Head,_=>(EquipmentSlot?)null};
   if(slot!=null&&actor.Equipment.Get(slot.Value)==null)actor.Equipment.Set(slot.Value,stack);else actor.Inventory.Add(stack);
  }
  actor.Character.ValidateOrThrow(actor.Id);return actor;
 }
}
