using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed class SurvivorTraining
{
 public string ClassId {get;set;}="";
 public string Path {get;set;}="";
 public int StartingHp {get;set;}
 public List<Ability> Improvements {get;set;}=[];
 public List<Skill> InitialSkills {get;set;}=[];
 public List<Ability?> SecondaryImprovements {get;set;}=[];
 public List<Skill> SkillChoices {get;set;}=[];
 public int LastMealMinute {get;set;}=-1440;
 public double EffectEndSeconds {get;set;}
 public float EffectSeconds {get;set;}
 public string TimedEffect {get;set;}="";
}
public sealed record SurvivorClassDefinition(string Id,string Name,int HitDie,Ability Primary,int Page,Ability[] Saves,Skill[] Skills,string[] Paths,string[] Features);
public static class SurvivorClassesRules
{
 public static readonly SurvivorClassDefinition[] All=
 [
  new("agile","Agile Survivor",6,Ability.Dexterity,99,[Ability.Dexterity,Ability.Intelligence],[Skill.Acrobatics,Skill.Stealth,Skill.Perception],["infiltrator","shadow-sniper"],
   ["Ghost Walker: Stealth proficiency; advantage while hiding; half-speed hidden movement.","Parkour Master: vault low furniture or an enemy without opportunity attacks; half fall damage.","Specialization: Infiltrator (Lockpicking expertise) or Shadow Sniper (+2 ranged attacks from cover/hidden; +1d6 against unaware targets).","Ability improvement: +2 to one score, maximum 20.","Evasion: no damage on successful Dexterity saves for half; half on failure.","Silent Killer: hidden attacks retain concealment (gunshots still propagate sound); +1d6 against unaware targets.","Adrenaline Rush: Bonus Action Dash plus level temporary HP, once per long rest.","Ability improvement: +2 to one score, maximum 20.","Vanish: improved Hide; no tracking signature in supported checks.","Phantom Survivor: no opportunity attacks; undetectable for 60 simulation seconds, once per long rest."]),
  new("charismatic","Charismatic Survivor",6,Ability.Charisma,103,[Ability.Charisma,Ability.Wisdom],[Skill.Persuasion,Skill.Deception,Skill.Insight],["diplomat","manipulator","warlord"],
   ["Smooth Talker: social advantage; reroll a failed Charisma check once per long rest.","Rallying Cry: Action; allies within 30 feet gain CHA + half level temporary HP; once per short rest.","Path: Diplomat (Persuasion/Insight expertise), Manipulator (Deception/Intimidation expertise), or Warlord (Leadership expertise).","Ability improvement: +2 to one score, maximum 20.","Unshakable Resolve: allies within 10 feet have advantage on fear saves.","Quick Thinking: Help as a Bonus Action.","Faction Influence: permanent contact flag; a one-time finite supply donation can be requested at the refuge. Faction stories follow in Phase 21.","Ability improvement: +2 to one score, maximum 20.","Master Negotiator: automatically pass one ordinary social check per long rest.","Indomitable Presence: fear immunity within 20 feet; inspire an ally's next attack using a Reaction, twice per long rest."]),
  new("smart","Smart Survivor",4,Ability.Intelligence,107,[Ability.Intelligence,Ability.Dexterity],[Skill.Investigation,Skill.Technology,Skill.Mechanics],["field-medic","demolitions","master-mechanic"],
   ["Tinkerer's Instinct: half-time refuge construction; material Investigation advantage; paid quick repairs.","Hacker's Edge: Technology/Mechanics proficiency; disable the local shutter alarm using physical parts.","Path: Field Medic (2d6+INT kit treatment, once per short rest), Demolitions (finite incendiaries), or Master Mechanic (2d6 paid barrier repair, once per short rest).","Ability improvement: +2 to one score, maximum 20.","Trapper's Mastery: upgraded traps and advantage on technical checks.","Engineer's Resilience: half environmental fire, poison and lightning damage.","Combat Engineer: fit a +1 accuracy modification to a physical firearm using finite parts.","Ability improvement: +2 to one score, maximum 20.","Master Improviser: electrical parts may replace mechanical trap parts.","Post-Apocalyptic Genius: upgraded 4d6 proximity devices. Broader invention recipes follow in Phase 19."]),
  new("strong","Strong Survivor",10,Ability.Strength,112,[Ability.Strength,Ability.Constitution],[Skill.Athletics,Skill.Intimidation,Skill.Survival],["brawler","juggernaut"],
   ["Power Strike: an extra weapon die once on your own turn if you have not moved; double carrying capacity.","Breaker: force a nearby ordinary door using an Action and six seconds of time.","Path: Brawler (1d6 unarmed strikes, Bonus Action attack) or Juggernaut (+1 armored AC; physical resistance at half HP).","Ability improvement: +2 to one score, maximum 20.","Critical Strength: one extra die on a melee critical; Brawler gains a free shove once per turn.","Unstoppable: one extra Action and no fatigue movement penalties for 60 seconds; once per long rest.","Last Stand: CON save DC 10 + half damage to remain at 1 HP; once per long rest.","Ability improvement: +2 to one score, maximum 20.","Smash Through: shove enemies up to 10 feet, with legal collision checks.","Indomitable Survivor: physical resistance, grapple/restraint immunity and two Last Stand uses. Brawler makes two bonus strikes; Juggernaut may halve one hit using a Reaction per short rest."]),
  new("tough","Tough Survivor",12,Ability.Constitution,118,[Ability.Constitution,Ability.Strength],[Skill.Athletics,Skill.Survival,Skill.Medicine],[],
   ["Iron Constitution: poison resistance; advantage against poison, disease and infection.","Pain Tolerance: subtract 2 from non-critical damage (4 at level 6, 6 at level 10); halve critical damage.","Unyielding Survivor: CON save DC 10 + damage to remain at 1 HP, once per long rest.","Ability improvement: +2 to one score, maximum 20.","Second Wind: Bonus Action; heal 1d10+CON, once per short rest.","Unshakable Resolve: advantage against fear, exhaustion and mental strain.","Survivor's Fortitude: ignore one fatigue level or supported injury condition, once per long rest; no food/water debt is removed.","Ability improvement: +2 to one score, maximum 20.","Walking Tank: +2 AC while armored; double carrying capacity.","Indomitable Survivor: reroll one failed save per long rest; bludgeoning resistance."]),
  new("wise","Wise Survivor",8,Ability.Wisdom,122,[Ability.Wisdom,Ability.Intelligence],[Skill.Perception,Skill.Insight,Skill.Survival],[],
   ["Survivor's Instinct: Perception and Insight advantage against threats.","Eagle-Eyed Scavenger: scout existing finite supplies; one failed Investigation/Survival reroll per long rest.","Forager's Efficiency: one physical food unit feeds two nearby survivors once per 24 hours; halve daily spoilage losses when present at home.","Keen Awareness: +1 Wisdom, maximum 20.","Predictive Evasion: disadvantage on the first visible enemy attack against you between your turns.","Silent Stalker: full-speed hidden movement.","Master Forager: automatic Survival checks for food, water and shelter; no infinite item creation.","Unshakable Nerves: +1 Wisdom; advantage on fear/mental saves.","Danger Sense: initiative advantage and immunity to surprise in supported checks.","Omniscient Awareness: count threats within 100 feet through walls; Insight DC15 predicts the saved next-day roll, once per long rest."])
 ];
 public static SurvivorClassDefinition? Find(string id)=>All.FirstOrDefault(c=>c.Id==id);
 public static Skill[] AllowedSkills(string id)=>id switch
 {
  "agile"=>[Skill.Acrobatics,Skill.Athletics,Skill.Stealth,Skill.Driving,Skill.Perception,Skill.Investigation,Skill.Technology,Skill.Deception],
  "charismatic"=>[Skill.Persuasion,Skill.Intimidation,Skill.Deception,Skill.Insight,Skill.Perception,Skill.Leadership,Skill.Resourcefulness],
  "smart"=>[Skill.Investigation,Skill.Technology,Skill.Mechanics,Skill.Medicine,Skill.Resourcefulness,Skill.Perception,Skill.Occultism],
  "strong"=>[Skill.Athletics,Skill.Intimidation,Skill.Survival,Skill.Mechanics,Skill.Driving,Skill.Medicine,Skill.Resourcefulness],
  "tough"=>[Skill.Athletics,Skill.Survival,Skill.Medicine,Skill.Intimidation,Skill.Resourcefulness,Skill.Mechanics],
  "wise"=>[Skill.Perception,Skill.Survival,Skill.AnimalHandling,Skill.Medicine,Skill.Insight,Skill.Investigation,Skill.Resourcefulness],_=>[]
 };
 public static string[] Tools(string id)=>id switch
 {
  "agile"=>["Lockpicking tools","Mechanical tools","Climbing gear"],"charismatic"=>["Communication devices","Lockpicking tools","Forgery kits"],"smart"=>["Mechanical tools","Electronic tools","Medical kits","Crafting kits"],"strong"=>["Mechanical tools","Barricade-building kits","Climbing gear"],"tough"=>["First aid kits","Mechanical tools","Survival gear"],"wise"=>["Scavenger tools","Survival kits","Hunting gear"],_=>[]
 };
 public static bool WeaponProficient(Actor a)
 {
  if(!a.UsesSurvivorClasses||a.Training.ClassId==""||a.Equipment.Weapon?.Definition.MagazineSize is not >0)return true;
  if(a.Equipment.Weapon.Definition.Equipment is {} p)return p.Family switch
  {
   "melee"=>true,"heavy-melee"=>a.Training.ClassId is "strong" or "smart" or "tough" or "wise",
   "bow"=>a.Training.ClassId is "agile" or "smart" or "strong" or "wise","crossbow"=>a.Training.ClassId=="agile",
   "shotgun"=>a.Training.ClassId is "strong" or "tough","handgun"=>a.Training.ClassId!="strong","smg"=>a.Training.ClassId is "charismatic" or "smart" or "tough" or "wise",_=>a.Character.Proficiencies.Weapons.Contains(p.Family,StringComparer.OrdinalIgnoreCase)
  };
  bool shotgun=a.Equipment.Weapon.Definition.AmmunitionType=="12-gauge";
  return a.Training.ClassId=="tough"||a.Training.ClassId=="strong"&&shotgun||a.Training.ClassId!="strong"&&!shotgun;
 }
 public static int ArmorBonus(Actor a)=>a.Equipment.Armor==null?0:(a.ClassIs("tough",9)?2:0)+(a.ClassIs("strong",3)&&a.Training.Path=="juggernaut"?1:0);
 public static IEnumerable<(string Id,int Level,int Uses,ResourceRecharge Recharge)> Resources(Actor a)
 {
  var recharge=ResourceRecharge.LongRest;
  switch(a.Training.ClassId)
  {
   case "agile":yield return("rush",7,1,recharge);yield return("phantom",10,1,recharge);break;
   case "charismatic":yield return("social-reroll",1,1,recharge);yield return("rally",2,1,ResourceRecharge.ShortRest);yield return("negotiator",9,1,recharge);yield return("inspire",10,2,recharge);break;
   case "smart":if(a.Training.Path=="field-medic")yield return("medic",3,1,ResourceRecharge.ShortRest);if(a.Training.Path=="master-mechanic")yield return("mechanic",3,1,ResourceRecharge.ShortRest);break;
   case "strong":yield return("unstoppable",6,1,recharge);yield return("last-stand",7,a.TotalLevel>=10?2:1,recharge);if(a.Training.Path=="juggernaut")yield return("halve-hit",10,1,ResourceRecharge.ShortRest);break;
   case "tough":yield return("last-stand",3,1,recharge);yield return("second-wind",5,1,ResourceRecharge.ShortRest);yield return("fortitude",7,1,recharge);yield return("save-reroll",10,1,recharge);break;
   case "wise":yield return("scout-reroll",2,1,recharge);yield return("foresee",10,1,recharge);break;
  }
 }
}
public sealed partial class World
{
 [JsonIgnore] public bool SurvivorClassesEnabled=>Campaign?.SurvivorClasses==true;
 public string ChooseSurvivorClassReason(Actor a,string id,Skill[]? skills=null)
 {
  if(!SurvivorClassesEnabled||!Party.Contains(a)||!a.Alive||SurvivorClassesRules.Find(id)==null)return "Choose a conscious active survivor and a valid class.";
  if(Mode!=Mode.Explore||PendingReaction!=null||ReactionInterruptActive||IsMoving(a)||HostilesTooCloseToRest(out _))return "Finish combat/movement and find a safe place to choose training.";
  if(skills!=null&&(skills.Length!=3||skills.Distinct().Count()!=3||skills.Any(s=>!SurvivorClassesRules.AllowedSkills(id).Contains(s))))return "Choose three distinct class skills.";
  return a.Training.ClassId!=""?"Class choice is permanent for this journey.":"";
 }
 public bool ChooseSurvivorClass(Actor a,string id,Skill[]? skills=null)
 {
  string reason=ChooseSurvivorClassReason(a,id,skills);if(reason!=""){LogSystem(reason,true);return false;}
  var d=SurvivorClassesRules.Find(id)!;a.Training.ClassId=id;a.Training.StartingHp=a.MaxHp;a.Character.Classes[0].HitDieSize=d.HitDie;foreach(var die in a.Character.HitDice)die.DieSize=d.HitDie;
  foreach(var tool in SurvivorClassesRules.Tools(id))if(!a.Character.Proficiencies.Tools.Contains(tool))a.Character.Proficiencies.Tools.Add(tool);
  foreach(var armor in new[]{"Light armor","Makeshift armor"})if(!a.Character.Proficiencies.Armor.Contains(armor))a.Character.Proficiencies.Armor.Add(armor);
  if(id is "strong" or "tough"&&!a.Character.Proficiencies.Armor.Contains("Riot gear"))a.Character.Proficiencies.Armor.Add("Riot gear");
  foreach(var weapon in id=="strong"?new[]{"Melee weapons","Shotguns","Bows","Improvised weapons"}:id=="tough"?new[]{"Melee weapons","Small firearms","Shotguns","Improvised weapons"}:new[]{"Melee weapons","Small firearms","Bows","Improvised weapons"})if(!a.Character.Proficiencies.Weapons.Contains(weapon))a.Character.Proficiencies.Weapons.Add(weapon);
  foreach(var save in d.Saves)if(!a.Character.Proficiencies.SavingThrows.Contains(save))a.Character.Proficiencies.SavingThrows.Add(save);
  a.Training.InitialSkills=(skills??d.Skills).ToList();
  foreach(var skill in a.Training.InitialSkills)TrainSkill(a,skill,ProficiencyRank.Proficient);
  if(a.Training.ClassId=="agile")TrainSkill(a,Skill.Stealth,ProficiencyRank.Proficient);
  if(a.Training.ClassId=="charismatic")foreach(var s in new[]{Skill.Persuasion,Skill.Deception,Skill.Insight})TrainSkill(a,s,ProficiencyRank.Proficient);
  SyncSurvivorFeatures(a);Message(MessageCategory.Quest,$"{a.Name} chooses {d.Name}. Existing HP, gear and earned XP are retained.");return true;
 }
 void TrainSkill(Actor a,Skill skill,ProficiencyRank rank)
 {
  var t=a.Character.Proficiencies.Skills.FirstOrDefault(s=>s.Skill==skill);if(t==null)a.Character.Proficiencies.Skills.Add(new(){Skill=skill,Rank=rank});else if(t.Rank<rank)t.Rank=rank;
 }
 void SyncSurvivorFeatures(Actor a)
 {
  var d=SurvivorClassesRules.Find(a.Training.ClassId)!;
  for(int i=1;i<=a.TotalLevel;i++)if(a.Character.Feature($"survivor.{d.Id}.{i}")==null)a.Character.Features.Add(new(){Id=$"survivor.{d.Id}.{i}",Name=d.Features[i-1].Split(':')[0],Description=d.Features[i-1],Source=$"Zombie Mall class section p.{d.Page}; Phase 17 computer adaptation",Kind=FeatureKind.ClassFeature});
  foreach(var r in SurvivorClassesRules.Resources(a).Where(r=>r.Level<=a.TotalLevel))
  {var p=a.Character.Resource("survivor."+r.Id);if(p==null)a.Character.Resources.Add(new(){Id="survivor."+r.Id,Name=r.Id,Maximum=r.Uses,Current=r.Uses,Recharge=r.Recharge});else{p.Maximum=r.Uses;p.Current=Math.Min(p.Current,p.Maximum);}}
  if(a.ClassIs("smart",2)){TrainSkill(a,Skill.Technology,ProficiencyRank.Proficient);TrainSkill(a,Skill.Mechanics,ProficiencyRank.Proficient);}
  if(a.ClassIs("strong",10))foreach(var condition in new[]{"Grappled","Restrained"}){if(!a.Character.Defenses.ConditionImmunities.Contains(condition))a.Character.Defenses.ConditionImmunities.Add(condition);a.Character.SetCondition(condition,condition,false);}
  var expertise=a.Training.Path switch{"infiltrator"=>new[]{Skill.Lockpicking},"diplomat"=>[Skill.Persuasion,Skill.Insight],"manipulator"=>[Skill.Deception,Skill.Intimidation],"warlord"=>[Skill.Leadership],_=>[]};foreach(var skill in expertise)TrainSkill(a,skill,ProficiencyRank.Expertise);
  if(a.Training.Path=="field-medic")TrainSkill(a,Skill.Medicine,ProficiencyRank.Proficient);
 }
 public string AdvanceSurvivorReason(Actor a,string path,Ability ability,Skill skill,Ability? second=null)
 {
  if(!SurvivorClassesEnabled||!Party.Contains(a)||!a.Alive||a.Training.ClassId=="")return "Choose a class for an active conscious survivor first.";
  if(Mode!=Mode.Explore||PendingReaction!=null||IsMoving(a)||HostilesTooCloseToRest(out _))return "Find a safe place outside combat to advance.";
  int level=a.TotalLevel+1;if(level>10||a.Character.Experience<SurvivalProgression.RequiredForLevel(level))return "Not enough earned XP for the next survivor level.";
  var d=SurvivorClassesRules.Find(a.Training.ClassId)!;
  if(level==3&&d.Paths.Length>0&&!d.Paths.Contains(path))return "Select a valid specialization.";
  if(!Enum.IsDefined(ability)||!Enum.IsDefined(skill)||second.HasValue&&(!Enum.IsDefined(second.Value)||second.Value==ability))return "Choose valid advancement options.";
  if(level is 4 or 8&&d.Id!="wise"&&(a.Character.Abilities.Score(ability)>(second.HasValue?19:18)||second.HasValue&&a.Character.Abilities.Score(second.Value)>19))return "Choose an ability that can increase by two without exceeding 20.";
  if(level is 2 or 4 or 6 or 8&&a.Character.Proficiencies.SkillRank(skill)==ProficiencyRank.Expertise)return "Choose a skill below expertise.";
  return "";
 }
 public bool AdvanceSurvivor(Actor a,string path,Ability ability,Skill skill,Ability? second=null)
 {
  string reason=AdvanceSurvivorReason(a,path,ability,skill,second);if(reason!=""){LogSystem(reason,true);return false;}
  var d=SurvivorClassesRules.Find(a.Training.ClassId)!;int level=a.TotalLevel+1;
  if(level==3&&d.Paths.Length>0)a.Training.Path=path;
  int hp=Math.Max(1,d.HitDie/2+1+a.AbilityModifier(Ability.Constitution));a.Character.AdvancePrimaryClassTo(level);a.MaxHp+=hp;a.Hp+=hp;
  if(level is 4 or 8)
  {
   Ability chosen=d.Id=="wise"?Ability.Wisdom:ability;int delta=d.Id=="wise"||second.HasValue?1:2,old=a.Character.Abilities.Score(chosen);SetSurvivorAbility(a,chosen,Math.Min(20,old+delta));a.Training.Improvements.Add(chosen);a.Training.SecondaryImprovements.Add(d.Id=="wise"?null:second);
   if(d.Id!="wise"&&second.HasValue){int before=a.AbilityModifier(Ability.Constitution);SetSurvivorAbility(a,second.Value,a.Character.Abilities.Score(second.Value)+1);if(second.Value==Ability.Constitution){int extra=(a.AbilityModifier(Ability.Constitution)-before)*level;a.MaxHp+=extra;a.Hp+=extra;}}
   if(chosen==Ability.Constitution){int growth=a.AbilityModifier(chosen)-AbilityScores.ModifierFor(old);a.MaxHp+=growth*level;a.Hp+=growth*level;}
  }
  if(level is 2 or 4 or 6 or 8){var rank=a.Character.Proficiencies.SkillRank(skill);TrainSkill(a,skill,rank>=ProficiencyRank.Proficient?ProficiencyRank.Expertise:ProficiencyRank.Proficient);a.Training.SkillChoices.Add(skill);}
  if(a.Training.ClassId=="agile")TrainSkill(a,Skill.Stealth,ProficiencyRank.Proficient);
  if(a.Training.ClassId=="charismatic")foreach(var s in new[]{Skill.Persuasion,Skill.Deception,Skill.Insight})TrainSkill(a,s,ProficiencyRank.Proficient);
  SyncSurvivorFeatures(a);Message(MessageCategory.Quest,$"{a.Name} advances to level {level} {d.Name}. Earned XP is retained.");return true;
 }
 static void SetSurvivorAbility(Actor a,Ability ability,int value)
 {var s=a.Character.Abilities;switch(ability){case Ability.Strength:s.Strength=value;break;case Ability.Dexterity:s.Dexterity=value;break;case Ability.Constitution:s.Constitution=value;break;case Ability.Intelligence:s.Intelligence=value;break;case Ability.Wisdom:s.Wisdom=value;break;default:s.Charisma=value;break;}}
 ResourcePool? SurvivorResource(Actor a,string id)=>a.Character.Resource("survivor."+id);
 bool SpendSurvivorResource(Actor a,string id){var p=SurvivorResource(a,id);if(p==null||p.Current<1)return false;p.Current--;return true;}
 void UpdateSurvivorEffects(float seconds)
 {
  if(!SurvivorClassesEnabled)return;
  foreach(var a in CommunityMembers)if(a.Training.EffectSeconds>0){a.Training.EffectSeconds=Math.Max(0,(float)(a.Training.EffectEndSeconds-(CampaignMinutes*60d+CampaignSeconds)));if(a.Training.EffectSeconds==0){a.Character.SetCondition(a.Training.TimedEffect,a.Training.TimedEffect,false);a.Training.TimedEffect="";a.Training.EffectEndSeconds=0;}}
  TriggerSurvivorTraps();
 }
 void ValidateSurvivorClasses()
 {
  if(SurvivorClassesEnabled&&CommunityMembers.Any(a=>a.Character.HasCondition("class-phantom")&&a.Training.TimedEffect!="class-phantom"||a.Character.HasCondition("class-unstoppable")&&a.Training.TimedEffect!="class-unstoppable"))throw new InvalidDataException("Unowned timed class condition.");
  var actors=Actors.Concat(ShelterResidents.Select(r=>r.Actor)).Concat(StoredTravelAreas.SelectMany(s=>s.Actors));
  foreach(var a in actors)
  {
   var t=a.Training;if(t==null||t.ClassId==null||t.Path==null||t.Improvements==null||t.SkillChoices==null||t.InitialSkills==null||t.SecondaryImprovements==null||!float.IsFinite(t.EffectSeconds)||t.EffectSeconds is <0 or >60||!double.IsFinite(t.EffectEndSeconds)||t.EffectEndSeconds<0||t.LastMealMinute< -1440||t.LastMealMinute>CampaignMinutes||t.TimedEffect is not ("" or "class-phantom" or "class-unstoppable"))throw new InvalidDataException("Invalid survivor training state.");
   var features=a.Character.Features.Where(f=>f.Id.StartsWith("survivor.")).ToArray();var resources=a.Character.Resources.Where(p=>p.Id.StartsWith("survivor.")).ToArray();
   if(!SurvivorClassesEnabled||a.Enemy||t.ClassId=="")
   {if(t.ClassId!=""||t.Path!=""||t.StartingHp!=0||t.Improvements.Count!=0||t.SkillChoices.Count!=0||t.InitialSkills.Count!=0||t.SecondaryImprovements.Count!=0||t.EffectSeconds!=0||t.TimedEffect!=""||features.Length!=0||resources.Length!=0)throw new InvalidDataException("Unsupported survivor training on legacy or untrained actor.");continue;}
   var d=SurvivorClassesRules.Find(t.ClassId);int level=a.TotalLevel;
   if(d==null||level is <1 or >10||a.Character.Classes.Count!=1||a.Character.PrimaryClassName!="Survivor"||a.Character.Classes[0].HitDieSize!=d.HitDie||t.StartingHp<1||a.Character.Experience<SurvivalProgression.RequiredForLevel(level)||t.InitialSkills.Count!=3||t.InitialSkills.Distinct().Count()!=3||t.InitialSkills.Any(s=>!SurvivorClassesRules.AllowedSkills(t.ClassId).Contains(s))||t.SecondaryImprovements.Count!=t.Improvements.Count||t.SecondaryImprovements.Any(s=>s.HasValue&&!Enum.IsDefined(s.Value))||level>=3&&d.Paths.Length>0&&!d.Paths.Contains(t.Path)||level<3&&t.Path!=""||d.Paths.Length==0&&t.Path!=""||t.Improvements.Count!=(level>=8?2:level>=4?1:0)||t.SkillChoices.Count!=new[]{2,4,6,8}.Count(n=>n<=level)||t.Improvements.Any(n=>!Enum.IsDefined(n))||t.SkillChoices.Any(n=>!Enum.IsDefined(n)))throw new InvalidDataException("Invalid survivor class progression.");
   if(features.Length!=level||Enumerable.Range(1,level).Any(i=>features.Count(f=>f.Id==$"survivor.{d.Id}.{i}")!=1))throw new InvalidDataException("Class features disagree with level.");
   var expected=SurvivorClassesRules.Resources(a).Where(r=>r.Level<=level).ToArray();if(resources.Length!=expected.Length||expected.Any(r=>!resources.Any(p=>p.Id=="survivor."+r.Id&&p.Maximum==r.Uses&&p.Recharge==r.Recharge&&p.Current>=0&&p.Current<=p.Maximum)))throw new InvalidDataException("Class resource ledger disagrees with level.");
   if(t.TimedEffect==""&&t.EffectEndSeconds!=0||t.TimedEffect!=""&&(t.EffectEndSeconds-(CampaignMinutes*60d+CampaignSeconds)>60.001||t.EffectEndSeconds<CampaignMinutes*60d+CampaignSeconds)|| (t.EffectSeconds>0)!=(t.TimedEffect!="")||t.TimedEffect!=""&&!a.Character.HasCondition(t.TimedEffect))throw new InvalidDataException("Class duration and condition disagree.");
  }
  ValidateSurvivorTraps();
 }
}
