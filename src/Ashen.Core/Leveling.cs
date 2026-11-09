using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HitPointAdvancementMethod { Average, Roll }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AdvancementChoiceMode { AbilityScoreIncrease, Feat }

/// <summary>IIP29 transient player choices. The plan is validated before CharacterSheet state is mutated.</summary>
public sealed class LevelUpPlan
{
 public string ActorId {get;set;}="";
 public string ClassId {get;set;}="";
 public HitPointAdvancementMethod HitPointMethod {get;set;}=HitPointAdvancementMethod.Average;
 public int RolledHitDie {get;set;}
 public string SubclassId {get;set;}="";
 public AdvancementChoiceMode AdvancementMode {get;set;}=AdvancementChoiceMode.AbilityScoreIncrease;
 public Dictionary<Ability,int> AbilityIncreases {get;set;}=[];
 public string FeatId {get;set;}="";
 public List<Skill> SkillChoices {get;set;}=[];
 public List<Skill> ExpertiseChoices {get;set;}=[];
 public List<string> SpellChoices {get;set;}=[];
}

public sealed record LevelUpFeat(string Id,string Name,string Description);
public sealed record LevelUpClassInfo(string Id,int HitDie,int SubclassLevel,string[] Subclasses,string RequirementText);

/// <summary>
/// IIP29 rules authority. It owns eligibility, advancement choices, and atomic application.
/// IIP30 supplies the authoritative spell catalogue/casting metadata; IIP29 owns only the advancement choice workflow.
/// </summary>
public static class LevelingRules
{
 static readonly LevelUpClassInfo[] classes=
 [
  new("Fighter",10,3,["Champion","Battle Master","Eldritch Knight"],"STR 13 or DEX 13"),
  new("Ranger",10,3,["Hunter","Beast Master","Gloom Stalker"],"DEX 13 and WIS 13"),
  new("Wizard",6,2,["School of Evocation","School of Abjuration","School of Divination"],"INT 13"),
  new("Cleric",8,1,["Life Domain","Light Domain","Knowledge Domain"],"WIS 13"),
  new("Rogue",8,3,["Thief","Assassin","Arcane Trickster"],"DEX 13"),
  new("Bard",8,3,["College of Lore","College of Valor","College of Glamour"],"CHA 13")
 ];
 static readonly LevelUpFeat[] feats=
 [
  new("tough","Tough","Gain 2 maximum HP for every character level you possess; future levels also gain +2 HP."),
  new("fleet","Fleet","Increase base walking speed by 5 feet."),
  new("skilled","Skilled","Gain proficiency in three skills in which you are not already proficient.")
 ];
 public static IReadOnlyList<LevelUpClassInfo> Classes=>classes;
 public static IReadOnlyList<LevelUpFeat> Feats=>feats;
 public static LevelUpClassInfo Class(string id)=>classes.First(c=>c.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
 public static int ClassLevel(CharacterSheet c,string classId)=>c.Classes.FirstOrDefault(x=>x.ClassId.Equals(classId,StringComparison.OrdinalIgnoreCase))?.Level??0;
 public static int TargetClassLevel(CharacterSheet c,string classId)=>ClassLevel(c,classId)+1;
 public static bool IsMulticlass(CharacterSheet c,string classId)=>ClassLevel(c,classId)==0&&c.Classes.Count>0;

 public static bool MeetsPrerequisite(CharacterSheet c,string classId)
 {
  return classId.ToLowerInvariant() switch
  {
   "fighter"=>c.Abilities.Strength>=13||c.Abilities.Dexterity>=13,
   "ranger"=>c.Abilities.Dexterity>=13&&c.Abilities.Wisdom>=13,
   "wizard"=>c.Abilities.Intelligence>=13,
   "cleric"=>c.Abilities.Wisdom>=13,
   "rogue"=>c.Abilities.Dexterity>=13,
   "bard"=>c.Abilities.Charisma>=13,
   _=>false
  };
 }
 public static bool CanTakeClass(CharacterSheet c,string classId,out string reason)
 {
  reason="";
  if(c.PrimaryClassName=="Survivor"){reason="Survivor advancement is not available yet.";return false;}
  if(c.TotalLevel>=20){reason="Maximum character level reached.";return false;}
  LevelUpClassInfo info;try{info=Class(classId);}catch{reason="Unsupported class.";return false;}
  int existing=ClassLevel(c,info.Id);if(existing>=20){reason=$"{info.Id} is already level 20.";return false;}
  if(existing>0)return true;
  // 5e multiclassing checks both the class being entered and all classes already held.
  if(!MeetsPrerequisite(c,info.Id)){reason=$"Requires {info.RequirementText}.";return false;}
  foreach(var current in c.Classes)if(!MeetsPrerequisite(c,current.ClassId))
  {var currentInfo=classes.FirstOrDefault(x=>x.Id.Equals(current.ClassId,StringComparison.OrdinalIgnoreCase));reason=currentInfo==null?$"Current class {current.ClassId} does not support multiclassing.":$"Current {current.ClassId} multiclass prerequisite is not met ({currentInfo.RequirementText}).";return false;}
  return true;
 }
 public static bool NeedsSubclass(CharacterSheet c,string classId)
 {
  var entry=c.Classes.FirstOrDefault(x=>x.ClassId.Equals(classId,StringComparison.OrdinalIgnoreCase));
  if(entry!=null&&!string.IsNullOrWhiteSpace(entry.SubclassId))return false;
  var info=Class(classId);return TargetClassLevel(c,classId)>=info.SubclassLevel;
 }
 public static string[] SubclassOptions(string classId)=>Class(classId).Subclasses;
 public static void NormalizeSubclassDecisionState(CharacterSheet c)
 {
  // Pre-IIP29 templates/saves stored the companion's eventual subclass before the class had reached its decision level.
  // Clear only those premature values so the real Level-Up workflow can present the choice at the proper class level.
  foreach(var entry in c.Classes)
  {
   var info=classes.FirstOrDefault(x=>x.Id.Equals(entry.ClassId,StringComparison.OrdinalIgnoreCase));
   if(info!=null&&entry.Level<info.SubclassLevel)entry.SubclassId="";
  }
 }
 public static bool GainsAbilityScoreChoice(string classId,int classLevel)
 {
  if(classLevel is 4 or 8 or 12 or 16 or 19)return true;
  if(classId.Equals("Fighter",StringComparison.OrdinalIgnoreCase)&&classLevel is 6 or 14)return true;
  return classId.Equals("Rogue",StringComparison.OrdinalIgnoreCase)&&classLevel==10;
 }
 public static IReadOnlyList<LevelUpFeat> AvailableFeats(CharacterSheet c)=>feats.Where(f=>c.Feature("feat-"+f.Id)==null).ToArray();

 public static int SkillChoiceCount(CharacterSheet c,LevelUpPlan plan)
 {
  int count=0;if(IsMulticlass(c,plan.ClassId)&&plan.ClassId is "Bard" or "Ranger" or "Rogue")count++;
  if(plan.AdvancementMode==AdvancementChoiceMode.Feat&&plan.FeatId.Equals("skilled",StringComparison.OrdinalIgnoreCase))count+=3;
  return count;
 }
 public static int ExpertiseChoiceCount(CharacterSheet c,LevelUpPlan plan)
 {
  int target=TargetClassLevel(c,plan.ClassId);
  if(plan.ClassId.Equals("Bard",StringComparison.OrdinalIgnoreCase)&&(target==3||target==10))return 2;
  if(plan.ClassId.Equals("Rogue",StringComparison.OrdinalIgnoreCase)&&(target==1||target==6))return 2;
  return 0;
 }
 public static IEnumerable<Skill> EligibleSkillChoices(CharacterSheet c,LevelUpPlan plan)
 {
  IEnumerable<Skill> candidates=Enum.GetValues<Skill>();
  if(IsMulticlass(c,plan.ClassId)&&!plan.FeatId.Equals("skilled",StringComparison.OrdinalIgnoreCase))
  {
   candidates=plan.ClassId switch
   {
    "Ranger"=>[Skill.AnimalHandling,Skill.Athletics,Skill.Insight,Skill.Investigation,Skill.Nature,Skill.Perception,Skill.Stealth,Skill.Survival],
    "Rogue"=>[Skill.Acrobatics,Skill.Athletics,Skill.Deception,Skill.Insight,Skill.Intimidation,Skill.Investigation,Skill.Perception,Skill.Performance,Skill.Persuasion,Skill.SleightOfHand,Skill.Stealth],
    _=>Enum.GetValues<Skill>()
   };
  }
  return candidates.Where(s=>c.Proficiencies.SkillRank(s)==ProficiencyRank.None).Distinct();
 }
 public static IEnumerable<Skill> EligibleExpertiseChoices(CharacterSheet c)=>Enum.GetValues<Skill>().Where(s=>c.Proficiencies.SkillRank(s)==ProficiencyRank.Proficient);

 public static int SpellChoiceCount(CharacterSheet c,string classId)
 {
  int target=TargetClassLevel(c,classId);
  return classId switch
  {
   "Wizard"=>target==1?6:2,
   "Bard"=>target switch{1=>4,10=>2,14=>2,18=>2,2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 11 or 13 or 15 or 17=>1,_=>0},
   "Ranger"=>target==2?2:target is 3 or 5 or 7 or 9 or 11 or 13 or 15 or 17 or 19?1:0,
   _=>0
  };
 }
 public static IReadOnlyList<string> EligibleSpellChoices(CharacterSheet c,string classId)
 {
  if(classId is not ("Wizard" or "Bard" or "Ranger"))return [];
  int target=TargetClassLevel(c,classId);var tradition=SpellcastingRules.Tradition(classId);int maxLevel=SpellcastingRules.MaximumSpellLevel(tradition.Progression,target);
  var known=c.Spellcasting.FirstOrDefault(s=>s.SourceId.Equals(classId,StringComparison.OrdinalIgnoreCase))?.KnownSpells??[];
  return SpellCatalog.ForClass(tradition.SpellListId,maxLevel).Where(spell=>spell.Level>0&&!known.Contains(spell.Id,StringComparer.OrdinalIgnoreCase)).Select(spell=>spell.Id).ToArray();
 }
 public static int AverageHitDie(int hitDie)=>hitDie/2+1;
 public static int HitPointGain(CharacterSheet c,LevelUpPlan plan)
 {
  int die=Class(plan.ClassId).HitDie;int raw=plan.HitPointMethod==HitPointAdvancementMethod.Average?AverageHitDie(die):plan.RolledHitDie;
  int gain=Math.Max(1,raw+c.Abilities.Modifier(Ability.Constitution));
  if(c.Feature("feat-tough")!=null)gain+=2;
  return gain;
 }

 static int AbilityIncreaseTotal(LevelUpPlan plan)=>plan.AbilityIncreases.Values.Sum();
 static int ScoreAfter(CharacterSheet c,Ability ability,LevelUpPlan plan)=>c.Abilities.Score(ability)+(plan.AbilityIncreases.TryGetValue(ability,out var n)?n:0);
 public static bool Validate(CharacterSheet c,LevelUpPlan plan,out string reason)
 {
  reason="";if(!c.CanLevelUp){reason="Not enough experience to level up.";return false;}
  if(!CanTakeClass(c,plan.ClassId,out reason))return false;
  var info=Class(plan.ClassId);int target=TargetClassLevel(c,plan.ClassId);
  if(plan.HitPointMethod==HitPointAdvancementMethod.Roll&&(plan.RolledHitDie<1||plan.RolledHitDie>info.HitDie)){reason=$"Roll 1d{info.HitDie} for hit points first.";return false;}
  if(NeedsSubclass(c,plan.ClassId)&&!SubclassOptions(plan.ClassId).Contains(plan.SubclassId,StringComparer.OrdinalIgnoreCase)){reason="Choose a subclass.";return false;}
  if(GainsAbilityScoreChoice(plan.ClassId,target))
  {
   if(plan.AdvancementMode==AdvancementChoiceMode.AbilityScoreIncrease)
   {
    if(AbilityIncreaseTotal(plan)!=2||plan.AbilityIncreases.Any(x=>x.Value<0||x.Value>2)||Enum.GetValues<Ability>().Any(a=>ScoreAfter(c,a,plan)>20)){reason="Assign exactly two ability-score points without raising a score above 20.";return false;}
   }
   else
   {
    if(!AvailableFeats(c).Any(f=>f.Id.Equals(plan.FeatId,StringComparison.OrdinalIgnoreCase))){reason="Choose an available feat.";return false;}
   }
  }
  int skills=SkillChoiceCount(c,plan);var eligibleSkills=EligibleSkillChoices(c,plan).ToHashSet();
  if(plan.SkillChoices.Distinct().Count()!=skills||plan.SkillChoices.Any(s=>!eligibleSkills.Contains(s))){reason=$"Choose {skills} valid new skill proficienc{(skills==1?"y":"ies")}.";return false;}
  int expertise=ExpertiseChoiceCount(c,plan);var eligibleExpertise=EligibleExpertiseChoices(c).ToHashSet();
  if(plan.ExpertiseChoices.Distinct().Count()!=expertise||plan.ExpertiseChoices.Any(s=>!eligibleExpertise.Contains(s))){reason=$"Choose {expertise} valid Expertise skill{(expertise==1?"":"s")}.";return false;}
  int spells=SpellChoiceCount(c,plan.ClassId);var eligibleSpells=EligibleSpellChoices(c,plan.ClassId).ToHashSet(StringComparer.OrdinalIgnoreCase);
  if(plan.SpellChoices.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=spells||plan.SpellChoices.Any(s=>!eligibleSpells.Contains(s))){reason=$"Choose {spells} new spell{(spells==1?"":"s")}.";return false;}
  return true;
 }

 static void AddUnique(List<string> list,params string[] values){foreach(var value in values)if(!list.Contains(value,StringComparer.OrdinalIgnoreCase))list.Add(value);}
 static void AddFeature(CharacterSheet c,string id,string name,string source,string description,FeatureKind kind=FeatureKind.ClassFeature,string[]? tags=null,FeatureMechanics? mechanics=null)
 {
  if(c.Feature(id)!=null)return;c.Features.Add(new(){Id=id,Name=name,Source=source,Description=description,Kind=kind,Tags=tags?.ToList()??[],Mechanics=mechanics??new()});
 }
 static void AddResource(CharacterSheet c,string id,string name,int maximum,ResourceRecharge recharge)
 {
  var r=c.Resource(id);if(r==null)c.Resources.Add(new(){Id=id,Name=name,Current=maximum,Maximum=maximum,Recharge=recharge});else if(maximum>r.Maximum){r.Current+=maximum-r.Maximum;r.Maximum=maximum;}
 }
 static void SetAbility(AbilityScores a,Ability ability,int value)
 {
  switch(ability){case Ability.Strength:a.Strength=value;break;case Ability.Dexterity:a.Dexterity=value;break;case Ability.Constitution:a.Constitution=value;break;case Ability.Intelligence:a.Intelligence=value;break;case Ability.Wisdom:a.Wisdom=value;break;case Ability.Charisma:a.Charisma=value;break;}
 }
 static void AddSkill(CharacterSheet c,Skill skill,ProficiencyRank rank)
 {
  var row=c.Proficiencies.Skills.FirstOrDefault(x=>x.Skill==skill);if(row==null)c.Proficiencies.Skills.Add(new(){Skill=skill,Rank=rank});else if((int)rank>(int)row.Rank)row.Rank=rank;
 }
 static void ApplyMulticlassGrants(CharacterSheet c,string classId)
 {
  switch(classId)
  {
   case "Fighter":AddUnique(c.Proficiencies.Weapons,"Simple weapons","Martial weapons");AddUnique(c.Proficiencies.Armor,"Light armor","Medium armor","Shields");break;
   case "Ranger":AddUnique(c.Proficiencies.Weapons,"Simple weapons","Martial weapons");AddUnique(c.Proficiencies.Armor,"Light armor","Medium armor","Shields");break;
   case "Rogue":AddUnique(c.Proficiencies.Armor,"Light armor");AddUnique(c.Proficiencies.Tools,"Thieves' tools");break;
   case "Bard":AddUnique(c.Proficiencies.Armor,"Light armor");AddUnique(c.Proficiencies.Tools,"One musical instrument");break;
   case "Cleric":AddUnique(c.Proficiencies.Armor,"Light armor","Medium armor","Shields");AddUnique(c.Proficiencies.Weapons,"Simple weapons");break;
  }
 }
 static void SetExtraAttacks(CharacterSheet c,int attacks,string source)
 {
  var f=c.Feature("extra-attack");
  if(f==null)AddFeature(c,"extra-attack","Extra Attack",source,$"When taking the Attack action, make {attacks} attacks instead of one.",tags:["extra-attack"],mechanics:new(){AttacksPerAction=attacks});
  else {f.Mechanics.AttacksPerAction=Math.Max(f.Mechanics.AttacksPerAction,attacks);f.Description=$"When taking the Attack action, make {f.Mechanics.AttacksPerAction} attacks instead of one.";}
 }
 static void AddSubclassMilestone(CharacterSheet c,string classId,string subclass,int level)
 {
  if(string.IsNullOrWhiteSpace(subclass))return;
  AddFeature(c,$"{classId.ToLowerInvariant()}-subclass-{level}",$"{subclass} Feature ({level})",subclass,$"{subclass} grants its class-level {level} specialization feature.",FeatureKind.SubclassFeature);
 }
 static void ApplyClassFeatures(CharacterSheet c,string classId,int level,string subclass)
 {
  switch(classId)
  {
   case "Fighter":
    if(level==1){AddFeature(c,"fighting-style-defense","Defense Fighting Style","Fighter","Training with armor and defensive positioning.");AddFeature(c,"second-wind","Second Wind","Fighter","A reserve of self-recovery.");AddResource(c,"second-wind","Second Wind",1,ResourceRecharge.ShortRest);}
    if(level==2){AddFeature(c,"action-surge","Action Surge","Fighter","Push beyond normal limits for an additional burst of action.",tags:["actionbar"]);AddResource(c,"action-surge","Action Surge",1,ResourceRecharge.ShortRest);}
    if(level is 3 or 7 or 10 or 15 or 18)AddSubclassMilestone(c,"Fighter",subclass,level);
    if(level==5)SetExtraAttacks(c,2,"Fighter");
    if(level==9){AddFeature(c,"indomitable","Indomitable","Fighter","Reroll a failed saving throw.",tags:["reaction"]);AddResource(c,"indomitable","Indomitable",1,ResourceRecharge.LongRest);}
    if(level==11)SetExtraAttacks(c,3,"Fighter");
    if(level==13)AddResource(c,"indomitable","Indomitable",2,ResourceRecharge.LongRest);
    if(level==17){AddResource(c,"action-surge","Action Surge",2,ResourceRecharge.ShortRest);AddResource(c,"indomitable","Indomitable",3,ResourceRecharge.LongRest);}
    if(level==20)SetExtraAttacks(c,4,"Fighter");
    break;
   case "Ranger":
    if(level==1){AddFeature(c,"favored-foe","Favored Foe","Ranger","Marks practiced quarry knowledge.");AddFeature(c,"deft-explorer","Deft Explorer","Ranger","Exploration expertise represented as character data.");}
    if(level==2)AddFeature(c,"ranger-fighting-style","Ranger Fighting Style","Ranger","Specialized martial training gained at second level.");
    if(level is 3 or 7 or 11 or 15)AddSubclassMilestone(c,"Ranger",subclass,level);
    if(level==5)SetExtraAttacks(c,2,"Ranger");
    if(level==6)AddFeature(c,"deft-explorer-improvement","Deft Explorer Improvement","Ranger","Your exploration training improves.");
    if(level==8)AddFeature(c,"lands-stride","Land's Stride","Ranger","Traverse difficult natural terrain with practiced ease.");
    if(level==10)AddFeature(c,"hide-in-plain-sight","Hide in Plain Sight","Ranger","Prepare exceptional concealment when circumstances permit.");
    if(level==14)AddFeature(c,"vanish","Vanish","Ranger","Hide quickly and leave fewer tracks.");
    if(level==18)AddFeature(c,"feral-senses","Feral Senses","Ranger","Your senses reveal threats others miss.");
    if(level==20)AddFeature(c,"foe-slayer","Foe Slayer","Ranger","Your mastery of chosen foes reaches its peak.");
    break;
   case "Wizard":
    if(level==1)AddFeature(c,"arcane-recovery","Arcane Recovery","Wizard","Recover a measure of spellcasting capacity during rest.");
    if(level is 2 or 6 or 10 or 14)AddSubclassMilestone(c,"Wizard",subclass,level);
    if(level==18)AddFeature(c,"spell-mastery","Spell Mastery","Wizard","Master selected low-level spells for exceptional repeat use.");
    if(level==20)AddFeature(c,"signature-spells","Signature Spells","Wizard","Designate signature spells as the culmination of wizard training.");
    break;
   case "Cleric":
    if(level==1)AddSubclassMilestone(c,"Cleric",subclass,1);
    if(level==2){AddFeature(c,"channel-divinity","Channel Divinity","Cleric","Channel divine power through a domain feature.",tags:["actionbar"]);AddResource(c,"channel-divinity","Channel Divinity",1,ResourceRecharge.ShortRest);}
    if(level is 6 or 8 or 17)AddSubclassMilestone(c,"Cleric",subclass,level);
    if(level==5)AddFeature(c,"destroy-undead-5","Destroy Undead","Cleric","Turned lesser undead may be destroyed outright.");
    if(level==6)AddResource(c,"channel-divinity","Channel Divinity",2,ResourceRecharge.ShortRest);
    if(level==10)AddFeature(c,"divine-intervention","Divine Intervention","Cleric","Call upon divine aid under extraordinary circumstances.");
    if(level==14)AddFeature(c,"destroy-undead-14","Destroy Undead Improvement","Cleric","Destroy Undead affects more powerful creatures.");
    if(level==18)AddResource(c,"channel-divinity","Channel Divinity",3,ResourceRecharge.ShortRest);
    if(level==20)AddFeature(c,"improved-divine-intervention","Improved Divine Intervention","Cleric","Divine intervention reaches its highest reliability.");
    break;
   case "Rogue":
    if(level==1){AddFeature(c,"expertise","Expertise","Rogue","Selected trained skills use double proficiency.");AddFeature(c,"sneak-attack","Sneak Attack","Rogue","Conditional precision damage hook for the combat rules.");AddFeature(c,"thieves-cant","Thieves' Cant","Rogue","Secret language and underworld signs.");}
    if(level==2)AddFeature(c,"cunning-action","Cunning Action","Rogue","Your training lets you reposition and act with exceptional speed.",tags:["actionbar"]);
    if(level is 3 or 9 or 13 or 17)AddSubclassMilestone(c,"Rogue",subclass,level);
    if(level==5)AddFeature(c,"uncanny-dodge","Uncanny Dodge","Rogue","React quickly to blunt the force of a dangerous hit.",tags:["reaction"]);
    if(level==7)AddFeature(c,"evasion","Evasion","Rogue","Avoid the worst effects of many Dexterity-based hazards.");
    if(level==11)AddFeature(c,"reliable-talent","Reliable Talent","Rogue","Exceptional training makes proficient skill use remarkably dependable.");
    if(level==14)AddFeature(c,"blindsense","Blindsense","Rogue","Notice nearby hidden creatures through sharpened awareness.");
    if(level==15){AddFeature(c,"slippery-mind","Slippery Mind","Rogue","Mental discipline grants proficiency in Wisdom saving throws.");if(!c.Proficiencies.SavingThrows.Contains(Ability.Wisdom))c.Proficiencies.SavingThrows.Add(Ability.Wisdom);}
    if(level==18)AddFeature(c,"elusive","Elusive","Rogue","Enemies struggle to exploit openings against you.");
    if(level==20){AddFeature(c,"stroke-of-luck","Stroke of Luck","Rogue","Turn a crucial failure into success on rare occasions.",tags:["actionbar"]);AddResource(c,"stroke-of-luck","Stroke of Luck",1,ResourceRecharge.ShortRest);}
    break;
   case "Bard":
    if(level==1){AddFeature(c,"bardic-inspiration","Bardic Inspiration","Bard","Grant an ally an inspirational die.");AddResource(c,"bardic-inspiration","Bardic Inspiration",Math.Max(1,c.Abilities.Modifier(Ability.Charisma)),ResourceRecharge.LongRest);}
    if(level==2){AddFeature(c,"song-of-rest","Song of Rest","Bard","Your performance improves recovery during a rest.");AddFeature(c,"jack-of-all-trades","Jack of All Trades","Bard","Apply half proficiency to otherwise untrained ability checks.");}
    if(level is 3 or 6 or 14)AddSubclassMilestone(c,"Bard",subclass,level);
    if(level==5)AddFeature(c,"font-of-inspiration","Font of Inspiration","Bard","Bardic Inspiration now refreshes on a short rest.");
    if(level==6)AddFeature(c,"countercharm","Countercharm","Bard","Use performance to bolster allies against fear and charm.");
    if(level is 10 or 14 or 18)AddFeature(c,$"magical-secrets-{level}","Magical Secrets","Bard","Learn magic beyond the ordinary bard list.");
    if(level==20)AddFeature(c,"superior-inspiration","Superior Inspiration","Bard","Begin desperate encounters with renewed inspiration.");
    if(level==3&&subclass.Equals("College of Lore",StringComparison.OrdinalIgnoreCase))AddFeature(c,"cutting-words","Cutting Words","Bard","Expend Bardic Inspiration as a reaction to undermine a nearby creature's attack.",tags:["reaction","cutting-words"]);
    break;
  }
 }
 static int[] FullCasterMaxima(int level)=>Math.Clamp(level,1,20) switch
 {
  1=>[2],2=>[3],3=>[4,2],4=>[4,3],5=>[4,3,2],6=>[4,3,3],7=>[4,3,3,1],8=>[4,3,3,2],9=>[4,3,3,3,1],10=>[4,3,3,3,2],11=>[4,3,3,3,2,1],12=>[4,3,3,3,2,1],13=>[4,3,3,3,2,1,1],14=>[4,3,3,3,2,1,1],15=>[4,3,3,3,2,1,1,1],16=>[4,3,3,3,2,1,1,1],17=>[4,3,3,3,2,1,1,1,1],18=>[4,3,3,3,3,1,1,1,1],19=>[4,3,3,3,3,2,1,1,1],20=>[4,3,3,3,3,2,2,1,1],_=>[]
 };
 static int[] HalfCasterMaxima(int level)=>level switch
 {
  <2=>[],2=>[2],3=>[3],4=>[3],5=>[4,2],6=>[4,2],7=>[4,3],8=>[4,3],9=>[4,3,2],10=>[4,3,2],11=>[4,3,3],12=>[4,3,3],13=>[4,3,3,1],14=>[4,3,3,1],15=>[4,3,3,2],16=>[4,3,3,2],17=>[4,3,3,3,1],18=>[4,3,3,3,1],19=>[4,3,3,3,2],20=>[4,3,3,3,2],_=>[]
 };
 static void UpdateSlots(SpellcastingSource source,int[] maxima)
 {
  for(int i=0;i<maxima.Length;i++)
  {
   int level=i+1,max=maxima[i];var slot=source.Slots.FirstOrDefault(s=>s.Level==level);
   if(slot==null)source.Slots.Add(new(){Level=level,Current=max,Maximum=max});else if(max>slot.Maximum){slot.Current+=max-slot.Maximum;slot.Maximum=max;}
  }
 }
 static SpellcastingSource EnsureSpellcasting(CharacterSheet c,string classId,int classLevel)
 {
  var source=c.Spellcasting.FirstOrDefault(s=>s.SourceId.Equals(classId,StringComparison.OrdinalIgnoreCase));if(source!=null){SpellcastingRules.NormalizeSource(c,source);return source;}
  source=SpellcastingRules.CreateSource(classId);
  if(classId=="Cleric")source.PreparedSpells=["bless","cure-wounds","healing-word","guiding-bolt"];
  c.Spellcasting.Add(source);return source;
 }
 static void UpdateSpellcasting(CharacterSheet c,string classId,int classLevel,IEnumerable<string> chosen)
 {
  if(classId is not ("Wizard" or "Bard" or "Ranger" or "Cleric"))return;if(classId=="Ranger"&&classLevel<2)return;
  var source=EnsureSpellcasting(c,classId,classLevel);foreach(var spell in chosen)if(!source.KnownSpells.Contains(spell,StringComparer.OrdinalIgnoreCase))source.KnownSpells.Add(spell);
  SpellcastingRules.RebuildSlots(source,classLevel);SpellcastingRules.NormalizeSource(c,source);
 }

 public static bool Apply(World world,Actor actor,LevelUpPlan plan,out string summary)
 {
  summary="";var c=actor.Character;
  if(!actor.Id.Equals(plan.ActorId,StringComparison.Ordinal)){summary="This level-up plan belongs to another character.";return false;}
  if(!Validate(c,plan,out var reason)){summary=reason.Length==0?"Invalid level-up plan.":reason;return false;}
  bool multiclass=IsMulticlass(c,plan.ClassId);int classLevel=TargetClassLevel(c,plan.ClassId);var info=Class(plan.ClassId);int oldTotal=c.TotalLevel;int oldConMod=c.Abilities.Modifier(Ability.Constitution);
  var entry=c.Classes.FirstOrDefault(x=>x.ClassId.Equals(plan.ClassId,StringComparison.OrdinalIgnoreCase));
  if(entry==null){entry=new(){ClassId=info.Id,Level=0,HitDieSize=info.HitDie};c.Classes.Add(entry);ApplyMulticlassGrants(c,info.Id);}entry.Level=classLevel;entry.HitDieSize=info.HitDie;
  if(!string.IsNullOrWhiteSpace(plan.SubclassId))entry.SubclassId=plan.SubclassId;
  var hitPool=c.HitDice.FirstOrDefault(h=>h.SourceClass.Equals(info.Id,StringComparison.OrdinalIgnoreCase));if(hitPool==null){hitPool=new(){SourceClass=info.Id,DieSize=info.HitDie};c.HitDice.Add(hitPool);}hitPool.Maximum++;hitPool.Current++;
  int hpGain=HitPointGain(c,plan);c.HitPoints.Maximum+=hpGain;c.HitPoints.Current=Math.Min(c.HitPoints.Maximum,c.HitPoints.Current+hpGain);
  if(GainsAbilityScoreChoice(info.Id,classLevel))
  {
   if(plan.AdvancementMode==AdvancementChoiceMode.AbilityScoreIncrease)
   {
    foreach(var pair in plan.AbilityIncreases)SetAbility(c.Abilities,pair.Key,c.Abilities.Score(pair.Key)+pair.Value);
    int conDelta=c.Abilities.Modifier(Ability.Constitution)-oldConMod;if(conDelta!=0){int retro=conDelta*(oldTotal+1);c.HitPoints.Maximum+=retro;c.HitPoints.Current=Math.Max(0,Math.Min(c.HitPoints.Maximum,c.HitPoints.Current+retro));hpGain+=retro;}
   }
   else
   {
    var feat=feats.First(f=>f.Id.Equals(plan.FeatId,StringComparison.OrdinalIgnoreCase));AddFeature(c,"feat-"+feat.Id,feat.Name,"Feat",feat.Description,FeatureKind.Feat);
    if(feat.Id=="tough"){int bonus=2*(oldTotal+1);c.HitPoints.Maximum+=bonus;c.HitPoints.Current+=bonus;hpGain+=bonus;}
    else if(feat.Id=="fleet")c.SpeedFeet+=5;
   }
  }
  foreach(var skill in plan.SkillChoices)AddSkill(c,skill,ProficiencyRank.Proficient);
  foreach(var skill in plan.ExpertiseChoices)AddSkill(c,skill,ProficiencyRank.Expertise);
  ApplyClassFeatures(c,info.Id,classLevel,entry.SubclassId);
  UpdateSpellcasting(c,info.Id,classLevel,plan.SpellChoices);
  // Some resource maxima depend on the character's new ability modifier.
  var inspiration=c.Resource("bardic-inspiration");if(inspiration!=null){int max=Math.Max(1,c.Abilities.Modifier(Ability.Charisma));if(max>inspiration.Maximum){inspiration.Current+=max-inspiration.Maximum;inspiration.Maximum=max;}if(c.Feature("font-of-inspiration")!=null)inspiration.Recharge=ResourceRecharge.ShortRest;}
  world.Level=Math.Max(world.Level,c.TotalLevel);c.ValidateOrThrow(actor.Id);
  summary=$"{actor.Name} advances to level {c.TotalLevel}: {info.Id} {classLevel}, +{hpGain} maximum HP"+(multiclass?" (multiclass).":".");
  world.Message(MessageCategory.System,summary,$"Level Up confirmed. XP remains {c.Experience:N0}. Proficiency bonus is now +{c.ProficiencyBonus}. No advancement was applied before confirmation.");world.Sound?.Invoke("level");return true;
 }
}
