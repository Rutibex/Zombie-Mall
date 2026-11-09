namespace Ashen.Core;

public static class CharacterTemplates
{
 static SkillTraining Train(Skill skill,ProficiencyRank rank=ProficiencyRank.Proficient)=>new(){Skill=skill,Rank=rank};
 static FeatureEntry Feature(string id,string name,string source,string description,FeatureKind kind=FeatureKind.ClassFeature,string[]? tags=null,FeatureMechanics? mechanics=null)=>new()
 {
  Id=id,Name=name,Source=source,Description=description,Kind=kind,Tags=tags?.ToList()??[],Mechanics=mechanics??new()
 };
 static ResourcePool Resource(string id,string name,int maximum,ResourceRecharge recharge=ResourceRecharge.LongRest)=>new(){Id=id,Name=name,Current=maximum,Maximum=maximum,Recharge=recharge};
 static SpellSlotPool Slots(int level,int amount)=>new(){Level=level,Current=amount,Maximum=amount};
 static CharacterSheet Base(
  string species,string background,string alignment,int speed,AbilityScores abilities,
  string classId,string subclass,int level,int hitDie,int hp,int ac,ProficiencyData proficiencies,
  AttackProfile attack)
 {
  return new()
  {
   Species=species,Background=background,Alignment=alignment,SpeedFeet=speed,Abilities=abilities,
   Classes=[new(){ClassId=classId,SubclassId=subclass,Level=level,HitDieSize=hitDie}],
   HitPoints=new(){Current=hp,Maximum=hp},HitDice=[new(){SourceClass=classId,DieSize=hitDie,Current=level,Maximum=level}],
   Proficiencies=proficiencies,Defenses=new(){ArmorClassBase=ac},MainAttack=attack
  };
 }
 static CharacterNarrative Narrative(string id)=>id switch
 {
  "knight"=>new()
  {
   Biography=@"Garrick served for twelve years in the watch of Orlan, first on the river gate and later as a sergeant responsible for the shrine road. He learned to distrust glorious plans and to value the quiet work that keeps ordinary people alive: checking hinges, counting rations, walking the same wall twice, and putting himself between danger and whoever cannot stand there alone. When the Ember Heart vanished and the dead began stirring beneath Saint Orlan, Garrick resigned his post rather than obey an order to seal the district and abandon those still inside. The Ashen Covenant began as his promise that nobody would be written off as an acceptable loss.

He does not speak often about the night the bells rang below the crypt. Garrick was on duty and still considers every death that followed a personal failure, even though no sentry could have stopped what came through the old reliquary. Recovering the Heart is therefore more than a mission to him. It is a chance to prove that duty means returning for the people an institution has decided are too expensive to save.",
   Personality="Steady, protective, practical, and almost impossible to hurry. Garrick prefers a defensible plan to a clever one, but once he gives his word he will walk into unreasonable danger to keep it.",
   Affiliations=["Former Orlan Watch sergeant","Ashen Covenant","Friends among the shrine-road wardens"],
   Notes=["Treats party formation as a promise that nobody fights alone.","Distrusts officials who speak about necessary losses.","Keeps a private list of the dead from the night the Ember Heart was stolen."]
  },
  "ranger"=>new()
  {
   Biography=@"Sylva grew up beyond Orlan's cultivated fields where the old road disappears into pine and blackthorn. Her family guided pilgrims through the Briarwood and taught her that ruins, weather, spoor, and frightened animals all tell the truth more readily than people do. In the weeks before the Ember Heart was stolen, she found deer abandoning the valleys around Saint Orlan and crows gathering over sealed burial mounds. The watch dismissed the signs as superstition. Sylva did not.

When the crypt opened, she tracked the first shambling dead back through tunnels that had not appeared on any living map for generations. She joined the Covenant because the others were willing to follow evidence into places respectable authorities preferred not to admit existed. Sylva considers the current descent unfinished business: whatever disturbed the dead left a trail, and she intends to follow it until the trail ends.",
   Personality="Observant, dryly funny, patient outdoors, and restless in crowded rooms. Sylva rarely argues loudly; she simply remembers who ignored a warning and adjusts her trust accordingly.",
   Affiliations=["Briarwood guides","Ashen Covenant","Informal network of hunters and charcoal burners around Orlan"],
   Notes=["Reads disturbed wildlife as carefully as tracks.","Sleeps lightly and volunteers for first watch.","Has no affection for ceremonial titles but respects demonstrated competence."]
  },
  "mage"=>new()
  {
   Biography=@"Mira came to Orlan as a scholar of warding traditions, not as an adventurer. Her particular obsession is the way old protective rites survive by being copied imperfectly from one generation to the next. The carvings beneath Saint Orlan were supposed to be a minor provincial example. Instead she discovered a layered magical architecture in which the Ember Heart was only the visible focus of a much older compact. Several symbols in the crypt predate the saint, the town, and possibly the kingdom that claims both.

On the night of the theft, Mira was the first to understand that simply replacing the relic would not be enough if the underlying covenant had been rewritten. She joined Garrick's expedition partly to recover the Heart and partly because the scholar in her cannot bear an unanswered question. She is increasingly uncomfortable with how much the buried script seems to recognize living observers.",
   Personality="Intensely curious, articulate, absent-minded about ordinary comforts, and stubborn when a mystery resists her. Mira can sound detached when frightened because analysis is how she keeps fear from choosing for her.",
   Affiliations=["Corresponding member of the Collegium of Letters","Ashen Covenant","Former assistant to Master Vael's warding archive"],
   Notes=["Collects rubbings of unusual sigils whenever circumstances permit.","Believes the crypt's oldest wards were negotiated rather than imposed.","Pretends not to notice when Rook steals her chalk, then invoices him in increasingly absurd units."]
  },
  "cleric"=>new()
  {
   Biography=@"Orren was raised in a dwarven hospice where faith was measured in meals served, wounds cleaned, and names remembered. He came to Saint Orlan years ago as an itinerant priest and stayed because the shrine kept its doors open during a winter fever that wealthier temples treated as someone else's problem. Over time he became one of the few clergy permitted to tend the old reliquary, though even he was never shown every seal beneath it.

The theft of the Ember Heart wounded Orren's confidence more deeply than he admits. He had blessed the reliquary that same evening and felt nothing amiss. Rather than interpret that failure as proof his faith was misplaced, he treats it as a demand for greater humility. Orren joined the Covenant to restore the relic, protect the living, and discover why something sacred could be removed from under the eyes of those charged with guarding it.",
   Personality="Warm, stubbornly compassionate, fond of practical jokes, and severe only when someone uses faith as an excuse for cruelty. Orren believes doubt is a tool: dangerous when hidden, useful when examined.",
   Affiliations=["Hospitallers of the Last Dawn","Caretakers of Saint Orlan","Ashen Covenant"],
   Notes=["Knows the public rites of the Ember Heart better than anyone in the party.","Carries a brewer's kit and judges camps by the quality of their morning tea.","Will argue doctrine for hours but ends arguments immediately when somebody needs help."]
  },
  "rogue"=>new()
  {
   Biography=@"Rook grew up in alleys where every locked door had two meanings: someone wanted privacy, or someone had something worth taking. He survived by learning the difference. Before joining the Covenant he worked as a courier, burglar, and occasional finder of objects whose owners had become inconveniently interested in getting them back. His first connection to Saint Orlan was less noble than the others': years ago he stole a pilgrim token from a collector and discovered that its markings matched maintenance signs used in the crypt's oldest service passages.

When the Ember Heart disappeared, Rook recognized one of those same signs scratched near a sealed access stair. He could have sold the information. Instead he brought it to Garrick, partly because animated corpses are bad for business and partly because somebody had used a thieves' language older than any guild he knew. Rook now wants to know who taught the dead to leave burglar's marks.",
   Personality="Quick, irreverent, generous when nobody is watching, and suspicious of anyone who claims never to have compromised. Rook jokes when cornered and becomes unnervingly quiet when he is genuinely angry.",
   Affiliations=["Ashen Covenant","Former Southbank courier circles","Loose contacts among Orlan's fences and locksmiths"],
   Notes=["Recognizes several crypt maintenance marks that predate the current shrine.","Insists he is borrowing Mira's chalk, never stealing it.","Keeps track of favors more carefully than coins."]
  },
  "bard"=>new()
  {
   Biography=@"Elara arrived in Orlan with a touring company and stayed when she discovered that the town's oldest funeral songs contained verses nobody remembered singing. She collects fragments of oral history that scholars dismiss because the singers cannot cite a manuscript. In Orlan that habit paid off: three unrelated families knew different endings to the same cradle-song, and when assembled the verses described a light carried below the earth, a promise made in ash, and bells that must never answer themselves.

After the Ember Heart vanished, Elara heard those forbidden answering bells from beneath the town. She joined the Covenant because the crypt is full of stories that have survived precisely by disguising themselves as superstition. She jokes that every expedition needs someone capable of writing the correct version afterward. More privately, she fears the old songs are not records of what happened but instructions for what is supposed to happen again.",
   Personality="Sociable, perceptive, theatrical when it helps others relax, and far more serious about memory than her manner suggests. Elara dislikes silence imposed by authority and delights in silence freely chosen between friends.",
   Affiliations=["Ashen Covenant","The Lantern Players","Correspondents among roadside singers, actors, and storytellers"],
   Notes=["Can identify several local legends from only a line or melody.","Writes down companion sayings without asking permission.","Suspects the oldest version of the Covenant was preserved as a song rather than a legal text."]
  },
  _=>new(){Biography="No biography has been authored for this character yet.",Personality="Unknown",Affiliations=[],Notes=[]}
 };
 public static void EnsureNarrative(string actorId,CharacterSheet sheet)
 {
  if(sheet.Narrative==null||string.IsNullOrWhiteSpace(sheet.Narrative.Biography))sheet.Narrative=Narrative(actorId);
 }

 static void AddSignature(CharacterSheet c,string id,string name,string description,int charges,int rangeFeet=0,bool splash=false,int extraDice=1,int extraDie=6,int extraBonus=2)
 {
  c.Resources.Add(Resource("signature","Signature Ability",charges));
  c.Features.Add(Feature(id,name,c.PrimaryClassName,description,tags:["actionbar","signature","special"],mechanics:new(){ResourceId="signature",RangeFeet=rangeFeet,ExtraDamageDice=extraDice,ExtraDamageDieSize=extraDie,ExtraDamageBonus=extraBonus,Splash=splash}));
 }
 public static CharacterSheet Companion(string id,int level=1)
 {
  level=Math.Clamp(level,1,20);
  CharacterSheet c=id switch
  {
   "knight"=>Base("Human","Soldier","Lawful Good",30,new(){Strength=16,Dexterity=12,Constitution=16,Intelligence=10,Wisdom=12,Charisma=10},"Fighter",level>=3?"Champion":"",level,10,36+(level-1)*5,16,new(){SavingThrows=[Ability.Strength,Ability.Constitution],Skills=[Train(Skill.Athletics),Train(Skill.Perception)],Weapons=["Simple weapons","Martial weapons"],Armor=["Light armor","Medium armor","Heavy armor","Shields"],Tools=["Gaming set"],Languages=["Common"]},new(){Id="longsword",Name="Longsword",Ability=Ability.Strength,DamageDieSize=8,RangeFeet=5,DamageType=DamageType.Slashing}),
   "ranger"=>Base("Wood Elf","Outlander","Neutral Good",35,new(){Strength=10,Dexterity=18,Constitution=14,Intelligence=12,Wisdom=16,Charisma=10},"Ranger",level>=3?"Hunter":"",level,10,27+(level-1)*5,14,new(){SavingThrows=[Ability.Strength,Ability.Dexterity],Skills=[Train(Skill.Perception),Train(Skill.Stealth),Train(Skill.Survival)],Weapons=["Simple weapons","Martial weapons"],Armor=["Light armor","Medium armor","Shields"],Tools=["Herbalism kit"],Languages=["Common","Elvish"]},new(){Id="longbow",Name="Longbow",Ability=Ability.Dexterity,DamageDieSize=8,RangeFeet=35,DamageType=DamageType.Piercing}),
   "mage"=>Base("Human","Sage","Neutral Good",30,new(){Strength=8,Dexterity=14,Constitution=12,Intelligence=18,Wisdom=14,Charisma=10},"Wizard",level>=2?"School of Evocation":"",level,6,22+(level-1)*5,12,new(){SavingThrows=[Ability.Intelligence,Ability.Wisdom],Skills=[Train(Skill.Arcana,ProficiencyRank.Expertise),Train(Skill.History),Train(Skill.Investigation)],Weapons=["Daggers","Darts","Slings","Quarterstaffs","Light crossbows"],Languages=["Common","Draconic"]},new(){Id="arcane-bolt",Name="Arcane Bolt",Ability=Ability.Intelligence,DamageDieSize=6,RangeFeet=30,DamageType=DamageType.Force}),
   "cleric"=>Base("Dwarf","Acolyte","Lawful Good",25,new(){Strength=14,Dexterity=10,Constitution=14,Intelligence=10,Wisdom=18,Charisma=12},"Cleric","Life Domain",level,8,30+(level-1)*5,15,new(){SavingThrows=[Ability.Wisdom,Ability.Charisma],Skills=[Train(Skill.Insight),Train(Skill.Medicine),Train(Skill.Religion)],Weapons=["Simple weapons"],Armor=["Light armor","Medium armor","Shields"],Tools=["Brewer's supplies"],Languages=["Common","Dwarvish","Celestial"]},new(){Id="mace",Name="Mace",Ability=Ability.Strength,DamageDieSize=6,RangeFeet=5,DamageType=DamageType.Bludgeoning}),
   "rogue"=>Base("Lightfoot Halfling","Urchin","Chaotic Good",25,new(){Strength=10,Dexterity=18,Constitution=14,Intelligence=14,Wisdom=12,Charisma=12},"Rogue",level>=3?"Thief":"",level,8,25+(level-1)*5,15,new(){SavingThrows=[Ability.Dexterity,Ability.Intelligence],Skills=[Train(Skill.Acrobatics),Train(Skill.Investigation),Train(Skill.Perception),Train(Skill.SleightOfHand,ProficiencyRank.Expertise),Train(Skill.Stealth,ProficiencyRank.Expertise)],Weapons=["Simple weapons","Hand crossbows","Longswords","Rapiers","Shortswords"],Armor=["Light armor"],Tools=["Thieves' tools"],Languages=["Common","Halfling","Thieves' Cant"]},new(){Id="shortsword",Name="Shortsword",Ability=Ability.Dexterity,DamageDieSize=6,RangeFeet=5,DamageType=DamageType.Piercing}),
   "bard"=>Base("Half-Elf","Entertainer","Chaotic Good",30,new(){Strength=8,Dexterity=16,Constitution=14,Intelligence=12,Wisdom=12,Charisma=18},"Bard",level>=3?"College of Lore":"",level,8,26+(level-1)*5,14,new(){SavingThrows=[Ability.Dexterity,Ability.Charisma],Skills=[Train(Skill.Deception),Train(Skill.Insight),Train(Skill.Performance,ProficiencyRank.Expertise),Train(Skill.Persuasion),Train(Skill.SleightOfHand)],Weapons=["Simple weapons","Hand crossbows","Longswords","Rapiers","Shortswords"],Armor=["Light armor"],Tools=["Lute","Flute"],Languages=["Common","Elvish","Sylvan"]},new(){Id="shortbow",Name="Shortbow",Ability=Ability.Dexterity,DamageDieSize=6,RangeFeet=30,DamageType=DamageType.Piercing}),
   _=>Base("Human","Adventurer","Unaligned",30,new(),"Adventurer","",level,8,20+(level-1)*5,12,new(),new())
  };
  switch(id)
  {
   case "knight":
    AddSignature(c,"cleave","Cleave","An empowered weapon sweep. The prototype runtime splashes half damage to nearby enemies.",3,splash:true);
    c.Features.Add(Feature("fighting-style-defense","Defense Fighting Style","Fighter","Training with armor and defensive positioning."));
    c.Features.Add(Feature("second-wind","Second Wind","Fighter","A reserve of self-recovery, represented for later action-bar implementation."));
    c.Resources.Add(Resource("second-wind","Second Wind",1,ResourceRecharge.ShortRest));
    break;
   case "ranger":
    AddSignature(c,"power-shot","Power Shot","A focused ranged attack used by the first playable.",3);
    c.Features.Add(Feature("favored-foe","Favored Foe","Ranger","Marks practiced quarry knowledge for later rules implementation."));
    c.Features.Add(Feature("deft-explorer","Deft Explorer","Ranger","Exploration expertise represented as character data."));
    if(level>=2)
    {
     var rangerMagic=SpellcastingRules.CreateSource("Ranger");rangerMagic.KnownSpells.AddRange(["hunters-mark","goodberry"]);if(level>=5)rangerMagic.KnownSpells.Add("pass-without-trace");if(level>=9)rangerMagic.KnownSpells.Add("lightning-arrow");SpellcastingRules.RebuildSlots(rangerMagic,level);c.Spellcasting.Add(rangerMagic);
    }
    break;
   case "mage":
    AddSignature(c,"ember-burst","Ember Burst","A ranged arcane strike that splashes nearby enemies.",5,30,true);
    c.Features.Add(Feature("arcane-recovery","Arcane Recovery","Wizard","Recovers spellcasting capacity during rest, represented for IIP30/IIP43."));
    var wizardKnown=new List<string>{"burning-hands","magic-missile","shield","sleep","detect-magic","identify"};
    var wizardPrepared=new List<string>{"burning-hands","magic-missile","shield","sleep"};
    if(level>=5){wizardKnown.Add("counterspell");wizardPrepared.Add("counterspell");}
    var wizardMagic=SpellcastingRules.CreateSource("Wizard");wizardMagic.KnownSpells=wizardKnown;wizardMagic.PreparedSpells=wizardPrepared;SpellcastingRules.RebuildSlots(wizardMagic,level);c.Spellcasting.Add(wizardMagic);
    break;
   case "cleric":
    AddSignature(c,"sacred-flame","Sacred Flame","A ranged radiant invocation used by the first playable.",5,30);
    c.Features.Add(Feature("healing-prayer","Healing Prayer","Cleric","Restores a wounded ally in the first playable.",tags:["actionbar","healing"],mechanics:new(){ResourceId="signature",RangeFeet=30,HealingDice=1,HealingDieSize=8,HealingBonus=6}));
    c.Features.Add(Feature("divine-domain","Life Domain","Cleric","Domain identity and future Channel Divinity hooks."));
    var clericMagic=SpellcastingRules.CreateSource("Cleric");clericMagic.PreparedSpells=["bless","cure-wounds","guiding-bolt","healing-word"];SpellcastingRules.RebuildSlots(clericMagic,level);c.Spellcasting.Add(clericMagic);
    break;
   case "rogue":
    AddSignature(c,"cunning-strike","Cunning Strike","A prototype empowered finesse attack pending the complete action system.",3);
    c.Features.Add(Feature("expertise","Expertise","Rogue","Double proficiency is represented directly in trained skills."));
    c.Features.Add(Feature("sneak-attack","Sneak Attack","Rogue","Conditional precision damage hook for later action/rule implementation."));
    c.Features.Add(Feature("thieves-cant","Thieves' Cant","Rogue","Secret language and underworld signs.",FeatureKind.ClassFeature));
    break;
   case "bard":
    AddSignature(c,"discordant-shot","Discordant Shot","A prototype ranged flourish pending the complete action system.",4);
    c.Features.Add(Feature("bardic-inspiration","Bardic Inspiration","Bard","Inspirational dice resource represented independently of the prototype signature charge."));
    c.Resources.Add(Resource("bardic-inspiration","Bardic Inspiration",Math.Max(1,c.Abilities.Modifier(Ability.Charisma)),ResourceRecharge.LongRest));
    if(level>=3)c.Features.Add(Feature("cutting-words","Cutting Words","Bard","College of Lore reaction: expend Bardic Inspiration to reduce a nearby creature's attack roll.",tags:["reaction","cutting-words"]));
    c.Features.Add(Feature("jack-of-all-trades","Jack of All Trades","Bard","Half proficiency on otherwise untrained ability checks is represented as a future rule hook."));
    var bardMagic=SpellcastingRules.CreateSource("Bard");bardMagic.KnownSpells=["dissonant-whispers","faerie-fire","healing-word","thunderwave"];if(level>=3)bardMagic.KnownSpells.Add("shatter");if(level>=5)bardMagic.KnownSpells.Add("hypnotic-pattern");SpellcastingRules.RebuildSlots(bardMagic,level);c.Spellcasting.Add(bardMagic);
    break;
  }
  if(level>=5&&(id is "knight" or "ranger"))
   c.Features.Add(Feature("extra-attack","Extra Attack",c.PrimaryClassName,"When taking the Attack action, make two attacks instead of one.",tags:["extra-attack"],mechanics:new(){AttacksPerAction=2}));
  SpellcastingRules.NormalizeCharacter(c);
  EnsureNarrative(id,c);
  return c;
 }

 static List<SpellSlotPool> FullCasterSlots(int level)
 {
  if(level<=0)return[];
  // 5e full-caster slot progression, levels 1-20. Runtime casting is owned by IIP30;
  // keeping the complete progression here makes the character model valid at every level now.
  int[] maxima=Math.Clamp(level,1,20) switch
  {
   1=>[2],2=>[3],3=>[4,2],4=>[4,3],5=>[4,3,2],6=>[4,3,3],
   7=>[4,3,3,1],8=>[4,3,3,2],9=>[4,3,3,3,1],10=>[4,3,3,3,2],
   11=>[4,3,3,3,2,1],12=>[4,3,3,3,2,1],13=>[4,3,3,3,2,1,1],14=>[4,3,3,3,2,1,1],
   15=>[4,3,3,3,2,1,1,1],16=>[4,3,3,3,2,1,1,1],17=>[4,3,3,3,2,1,1,1,1],
   18=>[4,3,3,3,3,1,1,1,1],19=>[4,3,3,3,3,2,1,1,1],20=>[4,3,3,3,3,2,2,1,1],_=>[]
  };
  return maxima.Select((n,i)=>Slots(i+1,n)).ToList();
 }
 static List<SpellSlotPool> WizardSlots(int level)=>FullCasterSlots(level);

 public static CharacterSheet Enemy(string role,int hp,int ac,int rangeTiles=1)
 {
  role=string.IsNullOrWhiteSpace(role)?"Creature":role;
  var c=role switch
  {
   "Archer"=>Base("Undead","Crypt Archer","Neutral Evil",30,new(){Strength=10,Dexterity=14,Constitution=12,Intelligence=6,Wisdom=8,Charisma=5},"Monster","Archer",1,8,hp,ac,new(){SavingThrows=[Ability.Dexterity],Skills=[Train(Skill.Perception)]},new(){Id="shortbow",Name="Bone Bow",Ability=Ability.Dexterity,DamageDieSize=6,RangeFeet=Math.Max(5,rangeTiles*5),DamageType=DamageType.Piercing,AttackBonusOverride=3,DamageBonusOverride=2}),
   "Wraith"=>Base("Undead","Hollow Acolyte","Neutral Evil",30,new(){Strength=8,Dexterity=14,Constitution=12,Intelligence=10,Wisdom=12,Charisma=14},"Monster","Wraith",1,8,hp,ac,new(){SavingThrows=[Ability.Wisdom,Ability.Charisma],Skills=[Train(Skill.Perception)]},new(){Id="grave-bolt",Name="Grave Bolt",Ability=Ability.Charisma,DamageDieSize=6,RangeFeet=Math.Max(5,rangeTiles*5),DamageType=DamageType.Necrotic,AttackBonusOverride=3,DamageBonusOverride=2}),
   "Boss"=>Base("Undead","Fallen Prior","Lawful Evil",30,new(){Strength=14,Dexterity=12,Constitution=16,Intelligence=12,Wisdom=16,Charisma=16},"Monster","Ashen Prior",3,10,hp,ac,new(){SavingThrows=[Ability.Constitution,Ability.Wisdom,Ability.Charisma],Skills=[Train(Skill.Religion),Train(Skill.Intimidation)]},new(){Id="ashen-flame",Name="Ashen Flame",Ability=Ability.Wisdom,DamageDieSize=6,RangeFeet=Math.Max(5,rangeTiles*5),DamageType=DamageType.Necrotic,AttackBonusOverride=5,DamageBonusOverride=3}),
   _=>Base("Undead","Crypt Sentinel","Neutral Evil",30,new(){Strength=12,Dexterity=12,Constitution=14,Intelligence=6,Wisdom=8,Charisma=5},"Monster","Skeleton",1,8,hp,ac,new(){SavingThrows=[Ability.Constitution]},new(){Id="rusted-blade",Name="Rusted Blade",Ability=Ability.Strength,DamageDieSize=6,RangeFeet=Math.Max(5,rangeTiles*5),DamageType=DamageType.Slashing,AttackBonusOverride=3,DamageBonusOverride=2})
  };
  return c;
 }

 public static CharacterSheet FromLegacy(string actorId,string legacyClass,bool enemy,int level,int hp,int maxHp,int baseAc,int bonus,int die,int range,int mana,int maxMana)
 {
  var sheet=enemy?Enemy(legacyClass,Math.Max(1,maxHp),baseAc,range):Companion(actorId,Math.Max(1,level));
  sheet.HitPoints.Maximum=Math.Max(1,maxHp);sheet.HitPoints.Current=Math.Clamp(hp,0,sheet.HitPoints.Maximum);sheet.Defenses.ArmorClassBase=baseAc;
  if(die>=2)sheet.MainAttack.DamageDieSize=die;if(range>0)sheet.MainAttack.RangeFeet=range*5;
  // Legacy attack bonuses could include progression not derivable from a pre-IIP4 sheet. Preserve it as an explicit override.
  if(bonus!=0)sheet.MainAttack.AttackBonusOverride=bonus;
  var signature=sheet.Resource("signature");if(signature!=null){signature.Maximum=Math.Max(0,maxMana);signature.Current=Math.Clamp(mana,0,signature.Maximum);}
  return sheet;
 }
}
