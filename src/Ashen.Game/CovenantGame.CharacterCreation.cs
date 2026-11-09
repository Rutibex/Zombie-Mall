using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 bool characterCreationOpen,creationNameFocused;
 CharacterCreationDraft creationDraft=CharacterCreationRules.NewDraft();
 int creationStep,creationSpellPane;
 string creationNotice="";
 static readonly string[] CreationSteps=["IDENTITY","SPECIES","CLASS & BACKGROUND","ABILITIES","SKILLS","EQUIPMENT","SPELLS","REVIEW"];

 void BeginCharacterCreation()
 {
  creationDraft=CharacterCreationRules.NewDraft();creationStep=0;creationSpellPane=0;creationNameFocused=true;creationNotice="";characterCreationOpen=true;title=true;ui.Screens.ReturnToGame();ui.Modals.Clear();
 }
 void CancelCharacterCreation(){characterCreationOpen=false;creationNameFocused=false;creationNotice="";title=true;}
 void CommitCharacterCreation()
 {
  if(!CharacterCreationRules.Validate(creationDraft,out var issues)){creationNotice=issues.FirstOrDefault()??"Finish character creation first.";return;}
  world=World.New(creationDraft);characterCreationOpen=false;creationNameFocused=false;Bind();InitializeAutosaveTracking();Notify($"{world.PartyLeader.Name}'s journey begins.");
 }
 bool ValidateCreationStep(int step,out string reason)
 {
  reason="";try
  {
   var sp=CharacterCreationRules.SpeciesById(creationDraft.SpeciesId);var cl=CharacterCreationRules.ClassById(creationDraft.ClassId);var bg=CharacterCreationRules.BackgroundById(creationDraft.BackgroundId);
   switch(step)
   {
    case 0:
     if(string.IsNullOrWhiteSpace(creationDraft.Name)||creationDraft.Name.Trim().Length is <2 or >24){reason="Enter a name between 2 and 24 characters.";return false;}break;
    case 3:
     if(creationDraft.AbilityAssignments.Count!=6||!creationDraft.AbilityAssignments.Values.Order().SequenceEqual(CharacterCreationRules.StandardArray.Order())){reason="Assign every standard-array score exactly once.";return false;}break;
    case 4:
     if(creationDraft.ClassSkillChoices.Distinct().Count()!=cl.SkillChoiceCount){reason=$"Choose {cl.SkillChoiceCount} class skills.";return false;}
     if(creationDraft.SpeciesSkillChoices.Distinct().Count()!=sp.BonusSkillChoices){reason=$"Choose {sp.BonusSkillChoices} species bonus skills.";return false;}break;
    case 5:
     if(!cl.EquipmentPackages.Any(p=>p.Id==creationDraft.EquipmentPackageId)){reason="Choose a starting equipment package.";return false;}break;
    case 6:
     if(creationDraft.CantripChoices.Count!=cl.CantripChoices){reason=$"Choose {cl.CantripChoices} cantrips.";return false;}
     if(creationDraft.SpellChoices.Count!=cl.SpellChoices){reason=$"Choose {cl.SpellChoices} 1st-level spells.";return false;}
     int prepared=CharacterCreationRules.PreparedSpellCount(creationDraft);if(creationDraft.PreparedSpellChoices.Count!=prepared){reason=$"Prepare {prepared} spells.";return false;}break;
    case 7:
     if(!CharacterCreationRules.Validate(creationDraft,out var issues)){reason=issues.FirstOrDefault()??"Character is incomplete.";return false;}break;
   }
  }catch{reason="Complete the required choices on this page.";return false;}return true;
 }
 void CreationNext()
 {
  if(!ValidateCreationStep(creationStep,out var reason)){creationNotice=reason;return;}creationNotice="";creationNameFocused=false;if(creationStep<CreationSteps.Length-1)creationStep++;else CommitCharacterCreation();
 }
 void CreationBack(){creationNotice="";creationNameFocused=false;if(creationStep>0)creationStep--;else CancelCharacterCreation();}
 void SelectCreationSpecies(string id){creationDraft.SpeciesId=id;creationDraft.SpeciesSkillChoices.Clear();NormalizeCreationPrepared();creationNotice="";}
 void SelectCreationClass(string id){creationDraft.ClassId=id;CharacterCreationRules.ApplyRecommendedAbilities(creationDraft);CharacterCreationRules.ResetClassDependentChoices(creationDraft);creationNotice="";creationSpellPane=0;}
 void SelectCreationBackground(string id){creationDraft.BackgroundId=id;var fixedSkills=CharacterCreationRules.BackgroundById(id).Skills.ToHashSet();creationDraft.ClassSkillChoices.RemoveAll(fixedSkills.Contains);creationDraft.SpeciesSkillChoices.RemoveAll(fixedSkills.Contains);creationNotice="";}
 void CycleCreationAbility(Ability ability,int direction)
 {
  int current=creationDraft.AbilityAssignments[ability],index=Array.IndexOf(CharacterCreationRules.StandardArray,current);int next=CharacterCreationRules.StandardArray[(index+direction+CharacterCreationRules.StandardArray.Length)%CharacterCreationRules.StandardArray.Length];var swap=creationDraft.AbilityAssignments.First(kv=>kv.Value==next).Key;creationDraft.AbilityAssignments[swap]=current;creationDraft.AbilityAssignments[ability]=next;NormalizeCreationPrepared();
 }
 void ToggleCreationClassSkill(Skill skill)
 {
  var cl=CharacterCreationRules.ClassById(creationDraft.ClassId);var bg=CharacterCreationRules.BackgroundById(creationDraft.BackgroundId);if(bg.Skills.Contains(skill))return;
  if(creationDraft.ClassSkillChoices.Remove(skill))return;if(!cl.SkillOptions.Contains(skill)){creationNotice="That skill is not available to this class.";return;}if(creationDraft.ClassSkillChoices.Count>=cl.SkillChoiceCount){creationNotice=$"Choose exactly {cl.SkillChoiceCount} class skills; deselect one first.";return;}creationDraft.ClassSkillChoices.Add(skill);creationNotice="";
 }
 void ToggleCreationSpeciesSkill(Skill skill)
 {
  var sp=CharacterCreationRules.SpeciesById(creationDraft.SpeciesId);if(sp.BonusSkillChoices<=0)return;var bg=CharacterCreationRules.BackgroundById(creationDraft.BackgroundId);if(bg.Skills.Contains(skill)||creationDraft.ClassSkillChoices.Contains(skill))return;
  if(creationDraft.SpeciesSkillChoices.Remove(skill))return;if(creationDraft.SpeciesSkillChoices.Count>=sp.BonusSkillChoices){creationNotice=$"Choose exactly {sp.BonusSkillChoices} ancestry bonus skills; deselect one first.";return;}creationDraft.SpeciesSkillChoices.Add(skill);creationNotice="";
 }
 void ToggleCreationSpell(string id)
 {
  var cl=CharacterCreationRules.ClassById(creationDraft.ClassId);List<string> list;int limit;
  if(creationSpellPane==0){list=creationDraft.CantripChoices;limit=cl.CantripChoices;}
  else if(creationSpellPane==1){list=creationDraft.SpellChoices;limit=cl.SpellChoices;}
  else {list=creationDraft.PreparedSpellChoices;limit=CharacterCreationRules.PreparedSpellCount(creationDraft);}
  if(list.Remove(id)){if(creationSpellPane==1&&cl.Id=="Wizard")creationDraft.PreparedSpellChoices.Remove(id);return;}
  if(limit<=0)return;if(list.Count>=limit){creationNotice=$"This page allows {limit} selection{(limit==1?"":"s")}; deselect one first.";return;}list.Add(id);creationNotice="";
 }
 void NormalizeCreationPrepared()
 {
  int count=CharacterCreationRules.PreparedSpellCount(creationDraft);var allowed=creationDraft.ClassId=="Wizard"?creationDraft.SpellChoices:CharacterCreationRules.FirstLevelSpellOptions(creationDraft.ClassId).Select(s=>s.Id).ToList();creationDraft.PreparedSpellChoices=creationDraft.PreparedSpellChoices.Where(x=>allowed.Contains(x,StringComparer.OrdinalIgnoreCase)).Take(count).ToList();foreach(var id in allowed)if(creationDraft.PreparedSpellChoices.Count<count&&!creationDraft.PreparedSpellChoices.Contains(id,StringComparer.OrdinalIgnoreCase))creationDraft.PreparedSpellChoices.Add(id);
 }
 void CharacterCreationTextInput(TextInputEventArgs e)
 {
  if(!characterCreationOpen||!creationNameFocused||creationStep!=0)return;char c=e.Character;if(c<' '||c=='\u007f'||creationDraft.Name.Length>=24)return;creationDraft.Name+=c;creationNotice="";
 }
 void HandleCharacterCreationKeyboard()
 {
  if(!characterCreationOpen)return;if(Pressed(Keys.Escape)){CreationBack();return;}
  if(creationNameFocused&&creationStep==0)
  {
   if(Pressed(Keys.Back)&&creationDraft.Name.Length>0)creationDraft.Name=creationDraft.Name[..^1];
   if(Pressed(Keys.Enter)){creationNameFocused=false;CreationNext();}return;
  }
  if(Pressed(Keys.Enter)){CreationNext();return;}if(Pressed(Keys.Left)&&creationStep>0){creationStep--;creationNotice="";}if(Pressed(Keys.Right)&&creationStep<CreationSteps.Length-1&&ValidateCreationStep(creationStep,out _)){creationStep++;creationNotice="";}
 }

 void DrawCharacterCreation()
 {
  Shade();var panel=new Rectangle(85,35,W-170,H-70);Panel(panel);Center("CREATE YOUR PROTAGONIST",new(panel.X,panel.Y+18,panel.Width,44),Bright,.76f,true);
  int tabX=panel.X+36,tabY=panel.Y+70,tabW=(panel.Width-72)/CreationSteps.Length;for(int i=0;i<CreationSteps.Length;i++){int step=i;Button(new(tabX+i*tabW,tabY,tabW-4,33),$"{i+1} {CreationSteps[i]}",()=>{if(step<=creationStep||ValidateCreationStep(creationStep,out _)){creationStep=step;creationNotice="";creationNameFocused=false;}},i==creationStep,.39f);}
  var body=new Rectangle(panel.X+38,panel.Y+116,panel.Width-76,panel.Height-205);Box(body,new Color(15,18,18),Gold*.35f);switch(creationStep){case 0:DrawCreationIdentity(body);break;case 1:DrawCreationSpecies(body);break;case 2:DrawCreationClassBackground(body);break;case 3:DrawCreationAbilities(body);break;case 4:DrawCreationSkills(body);break;case 5:DrawCreationEquipment(body);break;case 6:DrawCreationSpells(body);break;default:DrawCreationReview(body);break;}
  Button(new(panel.X+42,panel.Bottom-69,180,39),creationStep==0?"CANCEL":"< BACK",CreationBack,false,.58f);Button(new(panel.Right-222,panel.Bottom-69,180,39),creationStep==CreationSteps.Length-1?"BEGIN JOURNEY":"NEXT >",CreationNext,true,.58f);
  if(!string.IsNullOrWhiteSpace(creationNotice))Center(creationNotice,new(panel.X+245,panel.Bottom-64,panel.Width-490,28),Red,.49f);
  else Center("All choices are validated before the journey can begin.",new(panel.X+245,panel.Bottom-64,panel.Width-490,28),Muted,.47f);
 }
 void DrawCreationIdentity(Rectangle r)
 {
  Text("IDENTITY",r.X+28,r.Y+24,Gold,.66f,true);Text("Name",r.X+32,r.Y+89,Muted,.52f);var nameRect=new Rectangle(r.X+32,r.Y+116,500,44);Box(nameRect,creationNameFocused?new Color(52,50,39):new Color(25,28,27),creationNameFocused?Bright:Gold*.55f);Text(string.IsNullOrEmpty(creationDraft.Name)?"Click and type a name":creationDraft.Name+(creationNameFocused?" |":""),nameRect.X+14,nameRect.Y+11,string.IsNullOrEmpty(creationDraft.Name)?Muted:Bright,.6f);buttons.Add((nameRect,()=>creationNameFocused=true));
  Text("Pronouns",r.X+32,r.Y+184,Muted,.52f);int px=r.X+32;foreach(var p in CharacterCreationRules.PronounOptions){string value=p;Button(new(px,r.Y+211,145,36),p,()=>creationDraft.Pronouns=value,creationDraft.Pronouns==p,.49f);px+=154;}
  Text("Voice",r.X+32,r.Y+274,Muted,.52f);px=r.X+32;for(int i=0;i<CharacterCreationRules.VoiceOptions.Length;i++){string voice=CharacterCreationRules.VoiceOptions[i];Button(new(px+(i%3)*155,r.Y+302+(i/3)*45,145,35),voice,()=>creationDraft.VoiceSet=voice,creationDraft.VoiceSet==voice,.48f);}
  Text("Portrait",r.X+590,r.Y+89,Muted,.52f);for(int i=0;i<6;i++){int portrait=i;var pr=new Rectangle(r.X+590+(i%3)*164,r.Y+122+(i/3)*188,138,154);var actor=world.Party.FirstOrDefault(a=>a.Portrait==portrait)??world.Chosen;Portrait(actor,pr);Border(pr,creationDraft.Portrait==i?Bright:Gold*.55f);buttons.Add((pr,()=>creationDraft.Portrait=portrait));Center($"PORTRAIT {i+1}",new(pr.X,pr.Bottom+5,pr.Width,22),creationDraft.Portrait==i?Bright:Muted,.39f);}
 }
 void DrawCreationSpecies(Rectangle r)
 {
  Text("SPECIES",r.X+28,r.Y+24,Gold,.66f,true);int y=r.Y+82;foreach(var sp in CharacterCreationRules.Species){string id=sp.Id;Button(new(r.X+32,y,310,42),sp.Name,()=>SelectCreationSpecies(id),creationDraft.SpeciesId==id,.56f);y+=52;}
  var chosen=CharacterCreationRules.SpeciesById(creationDraft.SpeciesId);Text(chosen.Name.ToUpperInvariant(),r.X+390,r.Y+86,Bright,.69f,true);Paragraph(chosen.Description,r.X+390,r.Y+132,r.Width-430,Muted,.58f,27);Text($"Speed {chosen.SpeedFeet} ft",r.X+390,r.Y+210,Gold,.53f);Text("Ability bonuses",r.X+390,r.Y+252,Gold,.52f);int yy=r.Y+284;foreach(var b in chosen.AbilityBonuses){Text($"{b.Key}: +{b.Value}",r.X+405,yy,Bright,.51f);yy+=25;}Text("Traits",r.X+700,r.Y+252,Gold,.52f);yy=r.Y+284;foreach(var t in chosen.Traits){Text("• "+t,r.X+715,yy,Muted,.5f);yy+=25;}if(chosen.BonusSkillChoices>0)Text($"Skill Versatility: choose {chosen.BonusSkillChoices} extra skills on the Skills page.",r.X+390,r.Bottom-55,Teal,.49f);
 }
 void DrawCreationClassBackground(Rectangle r)
 {
  Text("CLASS",r.X+28,r.Y+24,Gold,.62f,true);int y=r.Y+76;foreach(var cl in CharacterCreationRules.Classes){string id=cl.Id;Button(new(r.X+30,y,260,39),cl.Id,()=>SelectCreationClass(id),creationDraft.ClassId==id,.52f);y+=46;}
  var chosen=CharacterCreationRules.ClassById(creationDraft.ClassId);Text(chosen.Id.ToUpperInvariant(),r.X+320,r.Y+76,Bright,.58f,true);Paragraph(chosen.Description,r.X+320,r.Y+114,450,Muted,.53f,24);Text($"Hit Die d{chosen.HitDie}  ·  Saves {string.Join(" / ",chosen.Saves)}  ·  Choose {chosen.SkillChoiceCount} skills",r.X+320,r.Y+184,Gold,.47f);
  if(LevelingRules.Class(chosen.Id).SubclassLevel==1){Text("Level-1 subclass",r.X+320,r.Y+226,Gold,.5f);int sx=r.X+320;foreach(var sub in LevelingRules.SubclassOptions(chosen.Id)){string value=sub;Button(new(sx,r.Y+255,205,37),sub,()=>creationDraft.SubclassId=value,creationDraft.SubclassId==sub,.43f);sx+=215;}}
  Text("BACKGROUND",r.X+820,r.Y+24,Gold,.62f,true);y=r.Y+76;foreach(var bg in CharacterCreationRules.Backgrounds){string id=bg.Id;Button(new(r.X+820,y,260,39),bg.Name,()=>SelectCreationBackground(id),creationDraft.BackgroundId==id,.5f);y+=46;}
  var b=CharacterCreationRules.BackgroundById(creationDraft.BackgroundId);Text("Training",r.X+1110,r.Y+83,Gold,.5f);Text(string.Join(" · ",b.Skills.Select(SkillRules.DisplayName)),r.X+1110,r.Y+112,Bright,.46f);Paragraph(b.Description,r.X+1110,r.Y+153,260,Muted,.47f,23);
 }
 void DrawCreationAbilities(Rectangle r)
 {
  Text("ABILITY SCORES — STANDARD ARRAY",r.X+28,r.Y+24,Gold,.62f,true);Text("Assign 15, 14, 13, 12, 10 and 8 exactly once. Species bonuses are shown in the final column.",r.X+32,r.Y+66,Muted,.5f);var final=CharacterCreationRules.FinalAbilities(creationDraft);int y=r.Y+115;foreach(var ability in Enum.GetValues<Ability>()){int baseScore=creationDraft.AbilityAssignments[ability],bonus=final.Score(ability)-baseScore;Text(ability.ToString(),r.X+55,y+8,Bright,.55f);Button(new(r.X+360,y,38,36),"<",()=>CycleCreationAbility(ability,-1));Center(baseScore.ToString(),new(r.X+405,y+5,65,30),Gold,.67f);Button(new(r.X+475,y,38,36),">",()=>CycleCreationAbility(ability,1));Text($"+ {bonus} species",r.X+560,y+8,Muted,.5f);Text($"FINAL {final.Score(ability)}  ({final.Modifier(ability):+0;-0;0})",r.X+780,y+7,Teal,.55f,true);y+=66;}Button(new(r.X+55,r.Bottom-70,270,39),"RECOMMENDED FOR CLASS",()=>{CharacterCreationRules.ApplyRecommendedAbilities(creationDraft);NormalizeCreationPrepared();},false,.47f);
 }
 void DrawCreationSkills(Rectangle r)
 {
  var cl=CharacterCreationRules.ClassById(creationDraft.ClassId);var bg=CharacterCreationRules.BackgroundById(creationDraft.BackgroundId);var sp=CharacterCreationRules.SpeciesById(creationDraft.SpeciesId);Text("SKILLS",r.X+28,r.Y+24,Gold,.62f,true);Text($"{bg.Name} already grants: {string.Join(", ",bg.Skills.Select(SkillRules.DisplayName))}",r.X+32,r.Y+66,Muted,.49f);Text($"Choose {cl.SkillChoiceCount} {cl.Id} skills",r.X+32,r.Y+102,Gold,.53f);DrawSkillGrid(r,new Rectangle(r.X+30,r.Y+135,650,390),cl.SkillOptions.Where(s=>!bg.Skills.Contains(s)).ToArray(),creationDraft.ClassSkillChoices,ToggleCreationClassSkill);
  Text(sp.BonusSkillChoices>0?$"{sp.Name}: choose {sp.BonusSkillChoices} additional skills":$"{sp.Name}: no additional ancestry skill choices",r.X+720,r.Y+102,Gold,.53f);if(sp.BonusSkillChoices>0)DrawSkillGrid(r,new Rectangle(r.X+720,r.Y+135,650,390),Enum.GetValues<Skill>().Where(s=>!bg.Skills.Contains(s)&&!creationDraft.ClassSkillChoices.Contains(s)).ToArray(),creationDraft.SpeciesSkillChoices,ToggleCreationSpeciesSkill);
  Text($"Class choices {creationDraft.ClassSkillChoices.Count}/{cl.SkillChoiceCount}",r.X+32,r.Bottom-58,creationDraft.ClassSkillChoices.Count==cl.SkillChoiceCount?Teal:Red,.5f);if(sp.BonusSkillChoices>0)Text($"Species choices {creationDraft.SpeciesSkillChoices.Count}/{sp.BonusSkillChoices}",r.X+720,r.Bottom-58,creationDraft.SpeciesSkillChoices.Count==sp.BonusSkillChoices?Teal:Red,.5f);
 }
 void DrawSkillGrid(Rectangle parent,Rectangle box,Skill[] options,List<Skill> selected,Action<Skill> toggle)
 {
  int cols=3,w=205,h=35;for(int i=0;i<options.Length;i++){var skill=options[i];int row=i/cols,col=i%cols;Button(new(box.X+col*(w+8),box.Y+row*(h+7),w,h),SkillRules.DisplayName(skill),()=>toggle(skill),selected.Contains(skill),.42f);}
 }
 void DrawCreationEquipment(Rectangle r)
 {
  var cl=CharacterCreationRules.ClassById(creationDraft.ClassId);Text("STARTING EQUIPMENT",r.X+28,r.Y+24,Gold,.62f,true);Text("Choose one package. Equipment uses the same IIP22–IIP25 inventory, proficiency and quick-slot systems as the campaign.",r.X+32,r.Y+68,Muted,.49f);int y=r.Y+126;foreach(var p in cl.EquipmentPackages){var value=p;var pr=new Rectangle(r.X+60,y,r.Width-120,160);Box(pr,creationDraft.EquipmentPackageId==p.Id?new Color(50,57,48):new Color(23,27,27),creationDraft.EquipmentPackageId==p.Id?Bright:Gold*.45f);Text(p.Name.ToUpperInvariant(),pr.X+24,pr.Y+20,creationDraft.EquipmentPackageId==p.Id?Bright:Gold,.57f,true);Paragraph(p.Description,pr.X+24,pr.Y+61,pr.Width-48,Muted,.52f,25);Text("Items: "+string.Join(" · ",p.ItemIds.GroupBy(x=>x).Select(g=>$"{Items.Get(g.Key).Name}{(g.Count()>1?$" x{g.Count()}":"")}")),pr.X+24,pr.Bottom-42,Teal,.47f);buttons.Add((pr,()=>creationDraft.EquipmentPackageId=value.Id));y+=185;}
 }
 void DrawCreationSpells(Rectangle r)
 {
  var cl=CharacterCreationRules.ClassById(creationDraft.ClassId);Text("SPELLCASTING",r.X+28,r.Y+24,Gold,.62f,true);if(cl.Id is not ("Wizard" or "Cleric" or "Bard")){Center(cl.Id=="Ranger"?"Rangers begin spellcasting at level 2.":"This class does not cast spells at level 1.",new(r.X+30,r.Y+180,r.Width-60,50),Muted,.68f);return;}
  Button(new(r.X+30,r.Y+70,180,35),$"CANTRIPS {creationDraft.CantripChoices.Count}/{cl.CantripChoices}",()=>creationSpellPane=0,creationSpellPane==0,.43f);Button(new(r.X+218,r.Y+70,220,35),cl.Id=="Cleric"?"CLASS SPELL LIST":$"KNOWN/SPELLBOOK {creationDraft.SpellChoices.Count}/{cl.SpellChoices}",()=>creationSpellPane=1,creationSpellPane==1,.43f);int prepared=CharacterCreationRules.PreparedSpellCount(creationDraft);if(cl.Id is "Wizard" or "Cleric")Button(new(r.X+446,r.Y+70,190,35),$"PREPARED {creationDraft.PreparedSpellChoices.Count}/{prepared}",()=>creationSpellPane=2,creationSpellPane==2,.43f);
  IReadOnlyList<SpellDefinition> options;List<string> selected;if(creationSpellPane==0){options=CharacterCreationRules.CantripOptions(cl.Id);selected=creationDraft.CantripChoices;}else if(creationSpellPane==2){options=cl.Id=="Wizard"?creationDraft.SpellChoices.Select(SpellCatalog.Get).ToArray():CharacterCreationRules.FirstLevelSpellOptions(cl.Id);selected=creationDraft.PreparedSpellChoices;}else{options=CharacterCreationRules.FirstLevelSpellOptions(cl.Id);selected=creationDraft.SpellChoices;}
  int x=r.X+34,y=r.Y+132,w=310,h=42;for(int i=0;i<options.Count;i++){var spell=options[i];int col=i%4,row=i/4;Button(new(x+col*(w+9),y+row*(h+8),w,h),spell.Name,()=>ToggleCreationSpell(spell.Id),selected.Contains(spell.Id,StringComparer.OrdinalIgnoreCase),.43f);}
  string hint=creationSpellPane switch{0=>$"Select exactly {cl.CantripChoices} cantrips.",1=>cl.Id=="Cleric"?"Clerics know their class spell list; choose prepared spells on the Prepared tab.":$"Select exactly {cl.SpellChoices} 1st-level spells.",_=>$"Prepare exactly {prepared} spell{(prepared==1?"":"s")} based on level + casting modifier."};Text(hint,r.X+35,r.Bottom-55,Muted,.49f);
 }
 void DrawCreationReview(Rectangle r)
 {
  Text("REVIEW",r.X+28,r.Y+24,Gold,.62f,true);var final=CharacterCreationRules.FinalAbilities(creationDraft);var cl=CharacterCreationRules.ClassById(creationDraft.ClassId);Text(creationDraft.Name.ToUpperInvariant(),r.X+40,r.Y+82,Bright,.79f,true);Text($"{creationDraft.SpeciesId} {creationDraft.ClassId}{(string.IsNullOrWhiteSpace(creationDraft.SubclassId)?"":$" — {creationDraft.SubclassId}")}",r.X+40,r.Y+130,Gold,.54f);Text($"{creationDraft.BackgroundId} · {creationDraft.Alignment} · {creationDraft.Pronouns} · Voice: {creationDraft.VoiceSet}",r.X+40,r.Y+164,Muted,.49f);
  int y=r.Y+220;foreach(var a in Enum.GetValues<Ability>()){Text($"{a,-12} {final.Score(a),2}  {final.Modifier(a):+0;-0;0}",r.X+55,y,Bright,.53f);y+=31;}Text("Skills",r.X+410,r.Y+220,Gold,.52f);Paragraph(string.Join(" · ",CharacterCreationRules.BackgroundById(creationDraft.BackgroundId).Skills.Concat(creationDraft.ClassSkillChoices).Concat(creationDraft.SpeciesSkillChoices).Distinct().Select(SkillRules.DisplayName)),r.X+410,r.Y+255,430,Muted,.5f,24);var eq=CharacterCreationRules.EquipmentById(creationDraft.EquipmentPackageId);Text("Equipment",r.X+410,r.Y+350,Gold,.52f);Paragraph(eq.Name+": "+string.Join(", ",eq.ItemIds.Select(id=>Items.Get(id).Name)),r.X+410,r.Y+385,430,Muted,.5f,24);Text("Spells",r.X+890,r.Y+220,Gold,.52f);Paragraph(cl.Id is "Wizard" or "Cleric" or "Bard"?$"Cantrips: {SpellNames(creationDraft.CantripChoices)}\n1st level: {SpellNames(cl.Id=="Cleric"?creationDraft.PreparedSpellChoices:creationDraft.SpellChoices)}":"No level-1 spellcasting.",r.X+890,r.Y+255,430,Muted,.49f,24);
  bool legal=CharacterCreationRules.Validate(creationDraft,out var issues);Box(new(r.X+885,r.Y+440,440,105),legal?new Color(36,55,44):new Color(58,35,33),legal?Teal:Red);Text(legal?"LEGAL LEVEL-1 PROTAGONIST":"INCOMPLETE CHARACTER",r.X+905,r.Y+458,legal?Teal:Red,.52f,true);Paragraph(legal?"Ready to begin. All creation choices use campaign rules and will be persisted in the journey save.":issues.FirstOrDefault()??"Complete the missing choices.",r.X+905,r.Y+493,400,Muted,.43f,20);
 }
 string SpellNames(IEnumerable<string> ids)=>string.Join(", ",ids.Select(id=>SpellCatalog.TryGet(id,out var spell)?spell.Name:id));
}
