using Ashen.Core;
using Microsoft.Xna.Framework;

namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum CharacterRecordPage { Overview, Skills, Proficiencies, Features }
 CharacterRecordPage characterRecordPage=CharacterRecordPage.Overview;
 int characterFeaturePage;

 static string Signed(int value)=>value>=0?$"+{value}":value.ToString();
 static string AbilityShort(Ability ability)=>ability switch
 {
  Ability.Strength=>"STR",Ability.Dexterity=>"DEX",Ability.Constitution=>"CON",
  Ability.Intelligence=>"INT",Ability.Wisdom=>"WIS",_=>"CHA"
 };
 static string ProficiencyMark(ProficiencyRank rank)=>rank switch
 {
  ProficiencyRank.Expertise=>"**",ProficiencyRank.Proficient=>"*",ProficiencyRank.Half=>"~",_=>""
 };
 static string JoinOrNone(IEnumerable<string> values)=>values.Any()?string.Join(", ",values):"None";
 static string DamageList(IEnumerable<DamageType> values)=>JoinOrNone(values.Select(x=>x.ToString()));

 void DrawCharacterRecordScreen(Rectangle r)
 {
  PartySwitch(r);var a=world.Chosen;
  int x=r.X+24,y=r.Y+76,w=r.Width-48,h=r.Height-98;

  var left=new Rectangle(x,y,220,h-4);
  var middle=new Rectangle(left.Right+10,y,248,h-4);
  int rightX=middle.Right+10,rightW=w-left.Width-middle.Width-20;
  var right=new Rectangle(rightX,y,rightW,h-48);

  DrawRecordAbilityPanel(a,left);
  DrawRecordIdentityPanel(a,middle);
  DrawRecordDetailPanel(a,right);

  int by=right.Bottom+8,gap=7,bw=(right.Width-gap*2)/3;
  Button(new(right.X,by,bw,32),"BIOGRAPHY",()=>ui.Screens.OpenChild(UiScreenId.Biography),false,.41f);
  Button(new(right.X+bw+gap,by,bw,32),"INFORMATION",()=>ui.Screens.OpenChild(UiScreenId.CharacterInformation),false,.39f);
  DrawLevelUpLaunchButton(new(right.X+(bw+gap)*2,by,bw,32),a);
 }

 void DrawRecordAbilityPanel(Actor a,Rectangle r)
 {
  var c=a.Character;Box(r,new Color(14,17,18)*.88f,Gold*.42f);Border(new(r.X+4,r.Y+4,r.Width-8,r.Height-8),Gold*.12f);
  Center("ABILITIES",new(r.X,r.Y+11,r.Width,24),Gold,.56f,true);
  int rowY=r.Y+45;
  foreach(var ability in Enum.GetValues<Ability>())
  {
   int score=c.Abilities.Score(ability),mod=c.AbilityModifier(ability),save=c.SaveModifier(ability);bool saveProf=c.Proficiencies.SaveProficient(ability);
   var q=new Rectangle(r.X+10,rowY,r.Width-20,65);bool hover=q.Contains(cursor);
   Box(q,hover?new Color(42,39,31):new Color(20,23,23),hover?Bright:Gold*.22f);
   Text(AbilityShort(ability),q.X+10,q.Y+8,Gold,.62f,true);
   Text(score.ToString(),q.X+72,q.Y+4,Bright,.86f,true);
   Text(Signed(mod),q.X+126,q.Y+10,Teal,.61f,true);
   Text($"SAVE {Signed(save)}{(saveProf?" *":"")}",q.X+10,q.Y+39,saveProf?Bright:Muted,.43f);
   ui.Information.Register(q,$"record:ability:{a.Id}:{ability}",ability.ToString(),"Ability score",
    $"{ability}: {score} ({Signed(mod)}). {(saveProf?"Proficient":"Not proficient")} saving throw {Signed(save)}.",
    $"{a.Name}'s {ability} ability score and derived modifier.",
    $"Score: {score}",$"Modifier: {Signed(mod)}",$"Saving throw: {Signed(save)}",$"Save proficiency: {(saveProf?"Yes":"No")}");
   rowY+=70;
  }
  Text("* proficient save",r.X+11,r.Bottom-23,Muted,.38f);
 }

 void DrawRecordIdentityPanel(Actor a,Rectangle r)
 {
  var c=a.Character;Box(r,new Color(18,20,20)*.82f,Gold*.34f);Border(new(r.X+4,r.Y+4,r.Width-8,r.Height-8),Gold*.12f);
  Center(a.Class.ToUpperInvariant(),new(r.X,r.Y+9,r.Width,25),Gold,.56f,true);
  var portrait=new Rectangle(r.X+32,r.Y+43,r.Width-64,190);Portrait(a,portrait);Border(portrait,Gold,2);Border(new(portrait.X+5,portrait.Y+5,portrait.Width-10,portrait.Height-10),Gold*.28f);
  Center(a.Name.ToUpperInvariant(),new(r.X,r.Y+242,r.Width,25),Bright,.60f,true);
  string subclass=string.IsNullOrWhiteSpace(c.SubclassName)?"":$" / {c.SubclassName}";
  Center($"LEVEL {a.TotalLevel}  {a.Class}{subclass}",new(r.X+6,r.Y+270,r.Width-12,22),Gold,.40f);

  int statY=r.Y+303;int bw=(r.Width-30)/2;
  RecordStatBox(new(r.X+10,statY,bw,43),"AC",a.Ac.ToString(),$"Base {a.BaseAc}; equipment {Signed(a.EquipmentArmorBonus)}{(c.HasCondition("shielded")?"; Shield +5":"")}");
  RecordStatBox(new(r.X+20+bw,statY,bw,43),"HP",$"{a.Hp}/{a.MaxHp}",c.HitPoints.Temporary>0?$"Temporary HP {c.HitPoints.Temporary}":"Current / maximum hit points");
  RecordStatBox(new(r.X+10,statY+50,bw,43),"INIT",Signed(a.Dex),"Dexterity modifier");
  RecordStatBox(new(r.X+20+bw,statY+50,bw,43),"SPEED",$"{c.SpeedFeet} ft",$"{a.SpeedTiles:0.#} world units");
  RecordStatBox(new(r.X+10,statY+100,bw,43),"PROF",$"+{a.ProficiencyBonus}","Level-based proficiency bonus");
  RecordStatBox(new(r.X+20+bw,statY+100,bw,43),"ATTACKS",c.AttacksPerAction.ToString(),"Attacks available from the Attack action");

  int idY=r.Bottom-82;
  Center(c.Species.ToUpperInvariant(),new(r.X+6,idY,r.Width-12,20),Muted,.42f,true);
  Center(c.Background.ToUpperInvariant(),new(r.X+6,idY+24,r.Width-12,20),Muted,.40f);
  Center(c.Alignment.ToUpperInvariant(),new(r.X+6,idY+48,r.Width-12,20),Muted,.40f);
 }

 void RecordStatBox(Rectangle r,string label,string value,string help)
 {
  bool hover=r.Contains(cursor);Box(r,hover?new Color(44,40,30):new Color(14,17,18),hover?Bright:Gold*.28f);
  Text(label,r.X+7,r.Y+3,Gold,.40f);Center(value,new(r.X+3,r.Y+18,r.Width-6,23),Bright,.43f);
  if(hover)ui.Tooltips.Offer("record:stat:"+label,help);
 }

 void DrawRecordDetailPanel(Actor a,Rectangle r)
 {
  var c=a.Character;Box(r,new Color(14,17,18)*.90f,Gold*.42f);Border(new(r.X+4,r.Y+4,r.Width-8,r.Height-8),Gold*.12f);
  var tabs=Enum.GetValues<CharacterRecordPage>();int gap=5,tabW=(r.Width-20-gap*(tabs.Length-1))/tabs.Length;
  for(int i=0;i<tabs.Length;i++)
  {
   var page=tabs[i];var q=new Rectangle(r.X+10+i*(tabW+gap),r.Y+10,tabW,31);
   Button(q,page switch{CharacterRecordPage.Overview=>"OVERVIEW",CharacterRecordPage.Skills=>"SKILLS",CharacterRecordPage.Proficiencies=>"PROFS",_=>"FEATURES"},()=>characterRecordPage=page,characterRecordPage==page,.43f);
  }
  var body=new Rectangle(r.X+12,r.Y+51,r.Width-24,r.Height-63);
  switch(characterRecordPage)
  {
   case CharacterRecordPage.Overview:DrawRecordOverview(a,body);break;
   case CharacterRecordPage.Skills:DrawRecordSkills(a,body);break;
   case CharacterRecordPage.Proficiencies:DrawRecordProficiencies(a,body);break;
   case CharacterRecordPage.Features:DrawRecordFeatures(a,body);break;
  }
 }

 void RecordHeading(string text,int x,int y,int width)
 {
  Text(text,x,y,Gold,.50f,true);Fill(new(x,y+22,width,1),Gold*.38f);
 }

 void DrawRecordOverview(Actor a,Rectangle r)
 {
  var c=a.Character;int y=r.Y+2;
  RecordHeading("PROGRESSION",r.X,y,r.Width);y+=30;
  string classes=string.Join(" / ",c.Classes.Select(cl=>$"{cl.ClassId} {cl.Level}{(string.IsNullOrWhiteSpace(cl.SubclassId)?"":$" ({cl.SubclassId})")}"));
  Text(classes,r.X,y,Bright,.51f);y+=24;
  string next=c.TotalLevel>=20?"MAXIMUM LEVEL":$"NEXT LEVEL {c.NextLevelExperience:N0} XP";
  if(c.PrimaryClassName=="Survivor"){int rank=SurvivalProgression.EarnedRank(c.Experience);next=rank>=10?"RANK 10":$"NEXT RANK {SurvivalProgression.RequiredForLevel(rank+1):N0} XP";}
  Text($"XP {c.Experience:N0}    {next}",r.X,y,Muted,.48f);y+=23;
  if(c.CanLevelUp){Text("LEVEL UP AVAILABLE",r.X,y,Bright,.49f,true);Text("Choose LEVEL UP below to advance.",r.X+177,y+2,Gold,.39f);y+=25;}
  else y+=6;

  RecordHeading("COMBAT",r.X,y,r.Width);y+=29;var atk=c.MainAttack;
  int displayAttack=a.AttackBonus+(a.Equipment.Weapon?.SurvivorAccuracy==true?1:0);
  string damage=$"{a.DamageDice}d{a.Die}{Signed(a.DamageBonus)} {a.AttackDamageType.ToString().ToLowerInvariant()}";
  var attackRect=new Rectangle(r.X,y,r.Width,48);Box(attackRect,new Color(20,23,23),Gold*.20f);
  Text(a.Equipment.Weapon?.Definition.Name??atk.Name,attackRect.X+8,attackRect.Y+5,Bright,.51f,true);
  Text($"{Signed(displayAttack)} to hit   {damage}   Range {a.Range*5} ft",attackRect.X+8,attackRect.Y+27,Muted,.43f);
  ui.Information.Register(attackRect,$"record:attack:{a.Id}:{atk.Id}",atk.Name,"Attack summary",$"{atk.Name}: {Signed(displayAttack)} to hit, {damage}, range {a.Range*5} ft.","Record combat summary",$"Attack bonus: {Signed(displayAttack)}",$"Damage: {damage}",$"Range: {a.Range*5} ft",$"Attacks per Attack action: {c.AttacksPerAction}");
  y+=56;
  Text($"Hit dice  {JoinOrNone(c.HitDice.Select(h=>$"{h.Current}/{h.Maximum}d{h.DieSize} {h.SourceClass}"))}",r.X,y,Muted,.43f);y+=23;
  if(a.RequiresAmmunition){Text($"Ammunition  {a.RequiredAmmunitionType}: {a.EquippedAmmunitionCount}",r.X,y,a.EquippedAmmunitionCount>0?Muted:Red,.43f);y+=23;}

  RecordHeading("SPELLCASTING",r.X,y,r.Width);y+=28;
  if(c.Spellcasting.Count==0){Text("No spellcasting source.",r.X,y,Muted,.43f);y+=24;}
  else foreach(var source in c.Spellcasting.Take(2))
  {
   var q=new Rectangle(r.X,y,r.Width,53);Box(q,new Color(20,23,23),Gold*.18f);
   Text($"{source.SourceId} / {source.PreparationMode}",q.X+8,q.Y+5,Bright,.46f,true);
   string slots=source.Slots.Count==0?"No slots":string.Join("  ",source.Slots.Select(s=>$"L{s.Level} {s.Current}/{s.Maximum}"));
   Text($"{AbilityShort(source.CastingAbility)}  DC {source.SpellSaveDc(c)}  Attack {Signed(source.SpellAttackBonus(c))}",q.X+8,q.Y+26,Gold,.39f);
   Text(slots,q.Right-MathF.Min(q.Width-12,Measure(slots,.34f))-7,q.Y+29,Muted,.34f);
   int prepared=source.PreparedSpells.Count>0?source.PreparedSpells.Count:source.KnownSpells.Count;
   ui.Information.Register(q,$"record:spellcasting:{a.Id}:{source.SourceId}",$"{source.SourceId} Spellcasting","Spellcasting summary",$"{source.SourceId} uses {source.CastingAbility} with the {source.PreparationMode} preparation model.","Record spellcasting summary",$"Spell save DC: {source.SpellSaveDc(c)}",$"Spell attack: {Signed(source.SpellAttackBonus(c))}",$"Cantrips: {source.Cantrips.Count}",$"Prepared/known: {prepared}",slots);
   y+=59;
  }

  RecordHeading("ACTIVE EFFECTS",r.X,y,r.Width);y+=27;
  var active=c.Conditions.ToArray();
  if(active.Length==0)Text("None",r.X,y,Muted,.43f);
  else
  {
   foreach(var condition in active.Take(4))
   {
    string duration=condition.RoundsRemaining<0?"ongoing":$"{condition.RoundsRemaining} rounds";
    string concentration=condition.Tags.Any(t=>t.Equals("concentration",StringComparison.OrdinalIgnoreCase))?"  [CONCENTRATION]":"";
    var q=new Rectangle(r.X,y,r.Width,22);Text($"• {condition.Name} — {duration}{concentration}",q.X,q.Y,concentration.Length>0?Teal:Bright,.40f);
    ui.Information.Register(q,$"record:condition:{a.Id}:{condition.Id}",condition.Name,"Active effect",$"{condition.Name} is currently affecting {a.Name}.","Active Character Record effect",$"Source: {(string.IsNullOrWhiteSpace(condition.Source)?"Unknown":condition.Source)}",$"Duration: {duration}",$"Tags: {JoinOrNone(condition.Tags)}");
    y+=22;
   }
  }
 }

 void DrawRecordSkills(Actor a,Rectangle r)
 {
  var c=a.Character;RecordHeading($"SKILLS   PROFICIENCY +{a.ProficiencyBonus}",r.X,r.Y+2,r.Width);
  Text("* proficient   ** expertise   ~ half proficiency",r.X,r.Y+29,Muted,.36f);
  var skills=Enum.GetValues<Skill>();int top=r.Y+57,colGap=12,columns=(skills.Length+8)/9,colW=(r.Width-colGap*(columns-1))/columns;
  for(int i=0;i<skills.Length;i++)
  {
   var skill=skills[i];int col=i/9,row=i%9;var ability=SkillRules.AbilityFor(skill);var rank=c.Proficiencies.SkillRank(skill);int mod=a.SkillModifier(skill);
   var q=new Rectangle(r.X+col*(colW+colGap),top+row*32,colW,27);bool hover=q.Contains(cursor);Box(q,hover?new Color(42,39,31):new Color(20,23,23),hover?Bright:Gold*.18f);
   string skillLabel=SkillRules.DisplayName(skill);float labelScale=Math.Min(.36f,Math.Max(30,q.Width-90)/Math.Max(1,Measure(skillLabel,1f)));Text(skillLabel,q.X+7,q.Y+5,Bright,labelScale);
   Text(AbilityShort(ability),q.Right-78,q.Y+6,Muted,.29f,true);Text(Signed(mod)+ProficiencyMark(rank),q.Right-43,q.Y+4,rank==ProficiencyRank.None?Muted:Teal,.39f,true);
   ui.Information.Register(q,$"record:skill:{a.Id}:{skill}",SkillRules.DisplayName(skill),"Skill",$"{SkillRules.DisplayName(skill)} uses {ability}: {Signed(mod)}.","Complete Character Record skill entry",$"Ability: {ability}",$"Modifier: {Signed(mod)}",$"Training: {rank}",$"Proficiency bonus: +{a.ProficiencyBonus}");
  }
  int saveY=top+9*32+10;RecordHeading("SAVING THROWS",r.X,saveY,r.Width);saveY+=28;
  int saveW=(r.Width-10)/3;
  for(int i=0;i<6;i++)
  {
   var ability=(Ability)i;bool proficient=c.Proficiencies.SaveProficient(ability);var q=new Rectangle(r.X+(i%3)*(saveW+5),saveY+(i/3)*34,saveW,28);
   Box(q,new Color(20,23,23),Gold*.18f);Text($"{AbilityShort(ability)} {Signed(a.SaveModifier(ability))}{(proficient?" *":"")}",q.X+8,q.Y+6,proficient?Bright:Muted,.39f);
  }
 }

 void DrawRecordProficiencies(Actor a,Rectangle r)
 {
  var c=a.Character;int gap=12,colW=(r.Width-gap)/2;Rectangle left=new Rectangle(r.X,r.Y,colW,r.Height),right=new Rectangle(r.X+colW+gap,r.Y,colW,r.Height);
  int ly=left.Y+2;
  void LeftBlock(string heading,string value,int height)
  {
   RecordHeading(heading,left.X,ly,left.Width);ly+=27;var q=new Rectangle(left.X,ly,left.Width,height);Box(q,new Color(20,23,23),Gold*.16f);Paragraph(value,q.X+7,q.Y+6,q.Width-14,Muted,.36f,18);ly=q.Bottom+10;
  }
  LeftBlock("WEAPONS",JoinOrNone(c.Proficiencies.Weapons),74);
  LeftBlock("ARMOR & SHIELDS",JoinOrNone(c.Proficiencies.Armor),64);
  LeftBlock("TOOLS",JoinOrNone(c.Proficiencies.Tools),58);
  LeftBlock("LANGUAGES",JoinOrNone(c.Proficiencies.Languages),58);

  int ry=right.Y+2;RecordHeading("DEFENSES",right.X,ry,right.Width);ry+=29;var d=c.Defenses;
  string[] defenseLines=
  [
   $"Resistances: {DamageList(d.Resistances)}",
   $"Immunities: {DamageList(d.Immunities)}",
   $"Vulnerabilities: {DamageList(d.Vulnerabilities)}",
   $"Condition immunities: {JoinOrNone(d.ConditionImmunities)}"
  ];
  foreach(var line in defenseLines){foreach(var wrapped in Wrap(line,right.Width,.36f)){Text(wrapped,right.X,ry,Muted,.36f);ry+=19;}ry+=2;}

  ry+=9;RecordHeading("EQUIPMENT STATE",right.X,ry,right.Width);ry+=29;
  Text($"Carried {a.CarriedWeight:0.#}/{a.CarryingCapacity:0.#} lb",right.X,ry,Muted,.39f);ry+=22;
  Text($"Encumbrance: {a.EncumbranceState}",right.X,ry,a.EncumbranceState=="OVERLOADED"?Red:a.EncumbranceState=="HEAVY"?Gold:Muted,.39f);ry+=22;
  Text($"MAGIC ITEMS  {a.EquippedMagicItems} / NO CAP",right.X,ry,Teal,.39f,true);ry+=25;
  Paragraph("Hexmaster rule: legally equipped magic items never require attunement. Physical equipment slots are the limit.",right.X,ry,right.Width,Muted,.36f,18);ry+=58;

  RecordHeading("SAVE PROFICIENCIES",right.X,ry,right.Width);ry+=29;
  var saveNames=c.Proficiencies.SavingThrows.Select(AbilityShort).ToArray();
  Paragraph(saveNames.Length==0?"None":string.Join("  •  ",saveNames),right.X,ry,right.Width,Bright,.39f,19);
 }

 void DrawRecordFeatures(Actor a,Rectangle r)
 {
  var c=a.Character;int y=r.Y+2;
  RecordHeading("RESOURCES",r.X,y,r.Width);y+=29;
  if(c.Resources.Count==0){Text("None",r.X,y,Muted,.42f);y+=27;}
  else
  {
   int colW=(r.Width-8)/2;
   for(int i=0;i<c.Resources.Count;i++)
   {
    var resource=c.Resources[i];int col=i%2,row=i/2;var q=new Rectangle(r.X+col*(colW+8),y+row*47,colW,40);Box(q,new Color(20,23,23),Gold*.18f);
    Text(resource.Name,q.X+7,q.Y+5,Bright,.39f);Text($"{resource.Current}/{resource.Maximum}  {resource.Recharge}",q.X+7,q.Y+22,Teal,.33f);
    ui.Information.Register(q,ResourceInformation(a,resource),$"{resource.Name}: {resource.Current}/{resource.Maximum}. Right-click for details.");
   }
   y+=((c.Resources.Count+1)/2)*47+8;
  }

  RecordHeading("FEATURES & TRAITS",r.X,y,r.Width);y+=28;
  const int pageSize=6;int pages=Math.Max(1,(c.Features.Count+pageSize-1)/pageSize);characterFeaturePage=Math.Clamp(characterFeaturePage,0,pages-1);
  foreach(var feature in c.Features.Skip(characterFeaturePage*pageSize).Take(pageSize))
  {
   var q=new Rectangle(r.X,y,r.Width,44);Box(q,new Color(20,23,23),Gold*.16f);Text(feature.Name,q.X+8,q.Y+5,Bright,.43f,true);Text($"{feature.Kind} / {feature.Source}",q.X+8,q.Y+24,Muted,.33f);
   ui.Information.Register(q,FeatureInformation(a,feature),(string.IsNullOrWhiteSpace(feature.Description)?feature.Name:feature.Description)+" Right-click for details.");
   y+=49;
  }
  if(c.Features.Count==0)Text("None",r.X,y,Muted,.42f);
  if(pages>1)
  {
   int buttonY=Math.Min(r.Bottom-32,y+4);Rectangle prev=new Rectangle(r.X,buttonY,82,27);var next=new Rectangle(r.Right-82,buttonY,82,27);
   Button(prev,"< PREV",()=>characterFeaturePage=(characterFeaturePage+pages-1)%pages,false,.38f);
   Center($"PAGE {characterFeaturePage+1}/{pages}",new(r.X+90,buttonY+6,r.Width-180,20),Muted,.36f);
   Button(next,"NEXT >",()=>characterFeaturePage=(characterFeaturePage+1)%pages,false,.38f);
  }
 }

}
