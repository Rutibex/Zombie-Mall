using Ashen.Core;
using Microsoft.Xna.Framework;

namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum LevelUpStage { Class, HitPoints, Subclass, Advancement, Training, Spells, Review }
 LevelUpPlan? levelUpPlan;
 LevelUpStage levelUpStage;
 string levelUpNotice="";

 void OpenLevelUp(Actor actor)
 {
  if(actor.Character.PrimaryClassName=="Survivor"){Notify("Survivor advancement is not available yet.");return;}
  levelUpPlan=new(){ActorId=actor.Id,ClassId=actor.Character.PrimaryClassName};
  var entry=actor.Character.Classes.FirstOrDefault(c=>c.ClassId.Equals(levelUpPlan.ClassId,StringComparison.OrdinalIgnoreCase));
  if(entry!=null)levelUpPlan.SubclassId=entry.SubclassId;
  levelUpStage=LevelUpStage.Class;levelUpNotice="";ui.Screens.OpenChild(UiScreenId.LevelUp);
 }
 Actor? LevelUpActor=>levelUpPlan==null?null:world.Party.FirstOrDefault(a=>a.Id==levelUpPlan.ActorId);

 void LevelChoice(Rectangle r,string label,Action click,bool selected=false,bool enabled=true,string hint="",float scale=.43f)
 {
  if(enabled){Button(r,label,click,selected,scale);if(r.Contains(cursor)&&hint.Length>0)ui.Tooltips.Offer("levelup:"+label,hint);return;}
  Box(r,new Color(20,22,22),Gold*.13f);Center(label,new(r.X,r.Center.Y-9,r.Width,22),Muted*.42f,scale);buttons.Add((r,()=>{if(hint.Length>0)Notify(hint);}));if(r.Contains(cursor)&&hint.Length>0)ui.Tooltips.Offer("levelup-disabled:"+label,hint);
 }
 void ResetLevelUpChoicesForClass(Actor actor,string classId)
 {
  if(levelUpPlan==null)return;levelUpPlan.ClassId=classId;levelUpPlan.RolledHitDie=0;levelUpPlan.AbilityIncreases.Clear();levelUpPlan.FeatId="";levelUpPlan.SkillChoices.Clear();levelUpPlan.ExpertiseChoices.Clear();levelUpPlan.SpellChoices.Clear();levelUpPlan.AdvancementMode=AdvancementChoiceMode.AbilityScoreIncrease;
  levelUpPlan.SubclassId=actor.Character.Classes.FirstOrDefault(c=>c.ClassId.Equals(classId,StringComparison.OrdinalIgnoreCase))?.SubclassId??"";levelUpNotice="";
 }

 void DrawLevelUpScreen(Rectangle r)
 {
  var a=LevelUpActor;if(a==null||levelUpPlan==null){Center("NO LEVEL-UP SESSION",new(r.X,r.Center.Y-18,r.Width,30),Muted,.58f,true);return;}
  var c=a.Character;int x=r.X+28,y=r.Y+75,w=r.Width-56;
  var banner=new Rectangle(x,y,w,54);Box(banner,new Color(17,20,20),Gold*.42f);Text(a.Name.ToUpperInvariant(),banner.X+16,banner.Y+10,Bright,.58f,true);
  Text($"LEVEL {c.TotalLevel}  →  {Math.Min(20,c.TotalLevel+1)}",banner.X+255,banner.Y+12,Gold,.48f,true);Text($"XP {c.Experience:N0} / {c.NextLevelExperience:N0}",banner.Right-235,banner.Y+15,c.CanLevelUp?Teal:Red,.40f,true);
  if(!c.CanLevelUp){Center("NOT ENOUGH EXPERIENCE TO ADVANCE",new(x,y+92,w,28),Red,.55f,true);Paragraph("Level Up never occurs automatically. Earn enough cumulative XP, then return here to make the advancement choices yourself.",x+90,y+135,w-180,Muted,.45f,22);return;}

  int tabY=banner.Bottom+10;var stages=Enum.GetValues<LevelUpStage>();int gap=5,tabW=(w-gap*(stages.Length-1))/stages.Length;
  for(int i=0;i<stages.Length;i++)
  {
   var st=stages[i];var q=new Rectangle(x+i*(tabW+gap),tabY,tabW,29);Button(q,st switch{LevelUpStage.HitPoints=>"HP",LevelUpStage.Advancement=>"ASI / FEAT",LevelUpStage.Training=>"TRAINING",LevelUpStage.Subclass=>"SUBCLASS",LevelUpStage.Spells=>"SPELLS",LevelUpStage.Review=>"REVIEW",_=>"CLASS"},()=>levelUpStage=st,levelUpStage==st,.32f);
  }
  var body=new Rectangle(x,tabY+39,w,r.Bottom-(tabY+39)-53);Box(body,new Color(12,15,16)*.97f,Gold*.35f);Border(new(body.X+4,body.Y+4,body.Width-8,body.Height-8),Gold*.12f);
  switch(levelUpStage)
  {
   case LevelUpStage.Class:DrawLevelClass(a,body);break;
   case LevelUpStage.HitPoints:DrawLevelHitPoints(a,body);break;
   case LevelUpStage.Subclass:DrawLevelSubclass(a,body);break;
   case LevelUpStage.Advancement:DrawLevelAdvancement(a,body);break;
   case LevelUpStage.Training:DrawLevelTraining(a,body);break;
   case LevelUpStage.Spells:DrawLevelSpells(a,body);break;
   case LevelUpStage.Review:DrawLevelReview(a,body);break;
  }
  if(levelUpNotice.Length>0)Center(levelUpNotice,new(x,r.Bottom-47,w,20),levelUpNotice.StartsWith("LEVEL UP COMPLETE")?Teal:Gold,.36f,true);
  if(levelUpStage!=LevelUpStage.Class)Button(new(x,r.Bottom-42,116,29),"< PREVIOUS",()=>levelUpStage=(LevelUpStage)Math.Max(0,(int)levelUpStage-1),false,.37f);
  if(levelUpStage!=LevelUpStage.Review)Button(new(x+w-116,r.Bottom-42,116,29),"NEXT >",()=>levelUpStage=(LevelUpStage)Math.Min((int)LevelUpStage.Review,(int)levelUpStage+1),false,.37f);
 }

 void LevelHeading(Rectangle r,string title,string subtitle)
 {
  Text(title,r.X+18,r.Y+14,Gold,.58f,true);Text(subtitle,r.X+18,r.Y+44,Muted,.40f);Fill(new(r.X+17,r.Y+68,r.Width-34,1),Gold*.25f);
 }
 void DrawLevelClass(Actor a,Rectangle r)
 {
  if(levelUpPlan==null)return;LevelHeading(r,"CHOOSE CLASS","Advance an existing class or enter another class if all multiclass prerequisites are met.");int y=r.Y+83;int gap=10,colW=(r.Width-56-gap)/2,rowH=82;
  var infos=LevelingRules.Classes;
  for(int i=0;i<infos.Count;i++)
  {
   var info=infos[i];bool allowed=LevelingRules.CanTakeClass(a.Character,info.Id,out var why);int current=LevelingRules.ClassLevel(a.Character,info.Id),target=current+1;var q=new Rectangle(r.X+23+(i%2)*(colW+gap),y+(i/2)*(rowH+9),colW,rowH);
   LevelChoice(q,$"{info.Id}  {current} → {target}",()=>ResetLevelUpChoicesForClass(a,info.Id),levelUpPlan.ClassId.Equals(info.Id,StringComparison.OrdinalIgnoreCase),allowed,allowed?$"d{info.HitDie} hit die. {info.RequirementText}." : why,.46f);
   Text(info.RequirementText,q.X+10,q.Bottom-24,allowed?Muted:Red*.72f,.32f);
  }
  var chosen=LevelingRules.Class(levelUpPlan.ClassId);Text($"Selected: {chosen.Id}  •  d{chosen.HitDie} hit die  •  {(LevelingRules.IsMulticlass(a.Character,chosen.Id)?"MULTICLASS":"CLASS ADVANCEMENT")}",r.X+23,r.Bottom-34,Teal,.39f,true);
 }

 void DrawLevelHitPoints(Actor a,Rectangle r)
 {
  if(levelUpPlan==null)return;var info=LevelingRules.Class(levelUpPlan.ClassId);int con=a.AbilityModifier(Ability.Constitution);int avg=LevelingRules.AverageHitDie(info.HitDie);LevelHeading(r,"HIT POINTS",$"{info.Id} uses d{info.HitDie}. Constitution modifier {Signed(con)}. Choose the deterministic average or one permanent die roll.");int y=r.Y+104;
  Rectangle avgR=new Rectangle(r.X+70,y,(r.Width-160)/2,145),rollR=new Rectangle(r.Center.X+10,y,(r.Width-160)/2,145);
  LevelChoice(avgR,$"AVERAGE  {avg}",()=>{levelUpPlan.HitPointMethod=HitPointAdvancementMethod.Average;levelUpNotice="Average HP selected.";},levelUpPlan.HitPointMethod==HitPointAdvancementMethod.Average,true,"Use the fixed average result. Constitution and Tough are applied afterward.",.54f);
  string rollLabel=levelUpPlan.RolledHitDie>0?$"ROLLED  {levelUpPlan.RolledHitDie}":$"ROLL 1d{info.HitDie}";
  LevelChoice(rollR,rollLabel,()=>{levelUpPlan.HitPointMethod=HitPointAdvancementMethod.Roll;if(levelUpPlan.RolledHitDie==0)levelUpPlan.RolledHitDie=world.Roll(info.HitDie);levelUpNotice=$"Hit die result: {levelUpPlan.RolledHitDie}. This roll is locked for this session.";},levelUpPlan.HitPointMethod==HitPointAdvancementMethod.Roll,true,"Roll once. Leaving this screen does not reroll the result.",.54f);
  int raw=levelUpPlan.HitPointMethod==HitPointAdvancementMethod.Average?avg:levelUpPlan.RolledHitDie;int gain=raw>0?Math.Max(1,raw+con)+(a.Character.Feature("feat-tough")!=null?2:0):0;
  Center(raw>0?$"CURRENT HP GAIN PREVIEW: +{gain} MAXIMUM HP":"ROLL THE HIT DIE TO PREVIEW HP GAIN",new(r.X+30,y+185,r.Width-60,28),raw>0?Teal:Muted,.48f,true);
  Paragraph("The gain is applied only after final confirmation. Choosing a different class resets the roll because the hit die may change.",r.X+95,y+233,r.Width-190,Muted,.39f,20);
 }

 void DrawLevelSubclass(Actor a,Rectangle r)
 {
  if(levelUpPlan==null)return;var c=a.Character;var entry=c.Classes.FirstOrDefault(x=>x.ClassId.Equals(levelUpPlan.ClassId,StringComparison.OrdinalIgnoreCase));bool needs=LevelingRules.NeedsSubclass(c,levelUpPlan.ClassId);LevelHeading(r,"SUBCLASS",needs?$"{levelUpPlan.ClassId} reaches its subclass decision point on this level.":"No subclass decision is required for this level.");
  if(!needs)
  {
   string retained=entry?.SubclassId??"None yet";Center($"RETAINED: {retained.ToUpperInvariant()}",new(r.X+40,r.Y+135,r.Width-80,35),Bright,.56f,true);Paragraph("Existing subclass identity is never replaced during ordinary advancement. Future subclass features are granted from the retained choice.",r.X+130,r.Y+190,r.Width-260,Muted,.41f,21);return;
  }
  var options=LevelingRules.SubclassOptions(levelUpPlan.ClassId);int y=r.Y+110,gap=12,bw=(r.Width-60-gap*(options.Length-1))/options.Length;
  for(int i=0;i<options.Length;i++){string option=options[i];LevelChoice(new(r.X+30+i*(bw+gap),y,bw,125),option,()=>levelUpPlan.SubclassId=option,levelUpPlan.SubclassId.Equals(option,StringComparison.OrdinalIgnoreCase),true,"Choose this subclass. The selection becomes permanent when Level Up is confirmed.",.44f);}
  Center(string.IsNullOrWhiteSpace(levelUpPlan.SubclassId)?"CHOOSE ONE SUBCLASS":$"SELECTED: {levelUpPlan.SubclassId.ToUpperInvariant()}",new(r.X+30,y+170,r.Width-60,30),string.IsNullOrWhiteSpace(levelUpPlan.SubclassId)?Gold:Teal,.46f,true);
 }

 void DrawLevelAdvancement(Actor a,Rectangle r)
 {
  if(levelUpPlan==null)return;int target=LevelingRules.TargetClassLevel(a.Character,levelUpPlan.ClassId);bool gains=LevelingRules.GainsAbilityScoreChoice(levelUpPlan.ClassId,target);LevelHeading(r,"ABILITY SCORE / FEAT",gains?$"{levelUpPlan.ClassId} {target} grants an Ability Score Improvement or feat choice.":"This class level does not grant an Ability Score Improvement or feat.");
  if(!gains){Center("NO ADVANCEMENT CHOICE THIS LEVEL",new(r.X+30,r.Y+150,r.Width-60,35),Muted,.52f,true);return;}
  int y=r.Y+88;LevelChoice(new(r.X+25,y,210,34),"ABILITY SCORES",()=>{levelUpPlan.AdvancementMode=AdvancementChoiceMode.AbilityScoreIncrease;levelUpPlan.FeatId="";levelUpPlan.SkillChoices.Clear();},levelUpPlan.AdvancementMode==AdvancementChoiceMode.AbilityScoreIncrease,true,"Assign two points: +2 to one score or +1 to two scores.",.40f);
  LevelChoice(new(r.X+245,y,150,34),"FEAT",()=>{levelUpPlan.AdvancementMode=AdvancementChoiceMode.Feat;levelUpPlan.AbilityIncreases.Clear();},levelUpPlan.AdvancementMode==AdvancementChoiceMode.Feat,true,"Choose one available feat instead of ability-score points.",.40f);
  y+=49;
  if(levelUpPlan.AdvancementMode==AdvancementChoiceMode.AbilityScoreIncrease)
  {
   int bw=(r.Width-70)/3;var abilities=Enum.GetValues<Ability>();
   for(int i=0;i<abilities.Length;i++)
   {
    var ability=abilities[i];int amount=levelUpPlan.AbilityIncreases.TryGetValue(ability,out var n)?n:0;int score=a.Character.Abilities.Score(ability);bool canAdd=levelUpPlan.AbilityIncreases.Values.Sum()<2&&score+amount<20;var q=new Rectangle(r.X+25+(i%3)*(bw+10),y+(i/3)*70,bw,59);
    LevelChoice(q,$"{AbilityShort(ability)}  {score}  +{amount}",()=>{int current=levelUpPlan.AbilityIncreases.TryGetValue(ability,out var v)?v:0,total=levelUpPlan.AbilityIncreases.Values.Sum();if(current==0&&total<2&&score<20)levelUpPlan.AbilityIncreases[ability]=1;else if(current==1&&total<2&&score+1<20)levelUpPlan.AbilityIncreases[ability]=2;else levelUpPlan.AbilityIncreases.Remove(ability);},amount>0,canAdd||amount>0,score>=20&&amount==0?"This score is already 20.":"Click to add points; when both points are assigned, click a selected score to clear it.",.43f);
   }
   int assigned=levelUpPlan.AbilityIncreases.Values.Sum();Center($"ASSIGNED {assigned}/2",new(r.X+25,y+150,r.Width-50,25),assigned==2?Teal:Gold,.43f,true);
  }
  else
  {
   var feats=LevelingRules.AvailableFeats(a.Character);int bw=(r.Width-70)/3;
   for(int i=0;i<feats.Count;i++){var feat=feats[i];var q=new Rectangle(r.X+25+i*(bw+10),y,bw,118);LevelChoice(q,feat.Name,()=>{levelUpPlan.FeatId=feat.Id;levelUpPlan.SkillChoices.Clear();},levelUpPlan.FeatId.Equals(feat.Id,StringComparison.OrdinalIgnoreCase),true,feat.Description,.47f);Paragraph(feat.Description,q.X+8,q.Y+55,q.Width-16,Muted,.31f,16);}
  }
 }

 void ToggleSkillChoice(List<Skill> list,Skill skill,int max)
 {
  if(list.Remove(skill))return;if(list.Count<max)list.Add(skill);
 }
 void DrawLevelTraining(Actor a,Rectangle r)
 {
  if(levelUpPlan==null)return;int skillCount=LevelingRules.SkillChoiceCount(a.Character,levelUpPlan),expertiseCount=LevelingRules.ExpertiseChoiceCount(a.Character,levelUpPlan);LevelHeading(r,"PROFICIENCY & EXPERTISE",skillCount+expertiseCount==0?"This level grants no proficiency decisions.":$"Choose {skillCount} new skill proficienc{(skillCount==1?"y":"ies")} and {expertiseCount} Expertise skill{(expertiseCount==1?"":"s")} where required.");
  if(skillCount+expertiseCount==0){Center("NO TRAINING CHOICE THIS LEVEL",new(r.X+30,r.Y+150,r.Width-60,35),Muted,.52f,true);return;}
  int x=r.X+20,y=r.Y+82,colW=(r.Width-50)/2;var eligible=LevelingRules.EligibleSkillChoices(a.Character,levelUpPlan).ToArray();
  if(skillCount>0)
  {
   Text($"NEW PROFICIENCIES  {levelUpPlan.SkillChoices.Count}/{skillCount}",x,y,Gold,.40f,true);y+=27;
   for(int i=0;i<eligible.Length;i++){var skill=eligible[i];int col=i/9,row=i%9;var q=new Rectangle(x+col*(colW+10),y+row*27,colW,24);LevelChoice(q,SkillRules.DisplayName(skill),()=>ToggleSkillChoice(levelUpPlan.SkillChoices,skill,skillCount),levelUpPlan.SkillChoices.Contains(skill),true,"Add or remove this skill from the level-up training choices.",.31f);}y+=Math.Min(9,eligible.Length)*27+13;
  }
  if(expertiseCount>0)
  {
   var expert=LevelingRules.EligibleExpertiseChoices(a.Character).ToArray();Text($"EXPERTISE  {levelUpPlan.ExpertiseChoices.Count}/{expertiseCount}",x,Math.Min(r.Bottom-170,y),Teal,.40f,true);int ey=Math.Min(r.Bottom-140,y+27);
   for(int i=0;i<expert.Length&&i<10;i++){var skill=expert[i];var q=new Rectangle(x+(i%2)*(colW+10),ey+(i/2)*27,colW,24);LevelChoice(q,SkillRules.DisplayName(skill),()=>ToggleSkillChoice(levelUpPlan.ExpertiseChoices,skill,expertiseCount),levelUpPlan.ExpertiseChoices.Contains(skill),true,"Upgrade this proficient skill to Expertise.",.31f);}
  }
 }

 void DrawLevelSpells(Actor a,Rectangle r)
 {
  if(levelUpPlan==null)return;int needed=LevelingRules.SpellChoiceCount(a.Character,levelUpPlan.ClassId);LevelHeading(r,"SPELL SELECTION",needed==0?"This class level grants no spell-list selection.":$"Choose {needed} new {levelUpPlan.ClassId} spell{(needed==1?"":"s")}. IIP30 will deepen spell definitions and casting rules.");
  if(needed==0){Center("NO SPELL CHOICE THIS LEVEL",new(r.X+30,r.Y+150,r.Width-60,35),Muted,.52f,true);return;}
  var spells=LevelingRules.EligibleSpellChoices(a.Character,levelUpPlan.ClassId);int x=r.X+20,y=r.Y+86,colW=(r.Width-50)/2;
  for(int i=0;i<spells.Count;i++)
  {
   string spell=spells[i];int col=i/10,row=i%10;var q=new Rectangle(x+col*(colW+10),y+row*30,colW,27);LevelChoice(q,DisplayId(spell),()=>{if(levelUpPlan.SpellChoices.Remove(spell))return;if(levelUpPlan.SpellChoices.Count<needed)levelUpPlan.SpellChoices.Add(spell);},levelUpPlan.SpellChoices.Contains(spell,StringComparer.OrdinalIgnoreCase),true,"Add or remove this spell from the level-up selection.",.34f);
  }
  Center($"SELECTED {levelUpPlan.SpellChoices.Count}/{needed}",new(r.X+20,r.Bottom-39,r.Width-40,24),levelUpPlan.SpellChoices.Count==needed?Teal:Gold,.40f,true);
 }

 string LevelUpReviewLine(Actor a)
 {
  if(levelUpPlan==null)return "";int target=LevelingRules.TargetClassLevel(a.Character,levelUpPlan.ClassId);var info=LevelingRules.Class(levelUpPlan.ClassId);int hpRaw=levelUpPlan.HitPointMethod==HitPointAdvancementMethod.Average?LevelingRules.AverageHitDie(info.HitDie):levelUpPlan.RolledHitDie;
  return $"{levelUpPlan.ClassId} {target}  •  HP {(levelUpPlan.HitPointMethod==HitPointAdvancementMethod.Average?"average":$"roll {hpRaw}")}  •  Total level {a.TotalLevel+1}";
 }
 void DrawLevelReview(Actor a,Rectangle r)
 {
  if(levelUpPlan==null)return;bool valid=LevelingRules.Validate(a.Character,levelUpPlan,out var why);LevelHeading(r,"REVIEW & CONFIRM","Nothing changes until you press CONFIRM LEVEL UP. Closing or backing out discards the pending plan.");int x=r.X+28,y=r.Y+91,w=r.Width-56;
  var summary=new Rectangle(x,y,w,208);Box(summary,new Color(20,23,23),Gold*.25f);Text(LevelUpReviewLine(a),summary.X+12,summary.Y+12,Bright,.48f,true);int sy=summary.Y+47;
  string subclass=string.IsNullOrWhiteSpace(levelUpPlan.SubclassId)?a.Character.Classes.FirstOrDefault(c=>c.ClassId.Equals(levelUpPlan.ClassId,StringComparison.OrdinalIgnoreCase))?.SubclassId??"—":levelUpPlan.SubclassId;
  string advancement=LevelingRules.GainsAbilityScoreChoice(levelUpPlan.ClassId,LevelingRules.TargetClassLevel(a.Character,levelUpPlan.ClassId))?(levelUpPlan.AdvancementMode==AdvancementChoiceMode.AbilityScoreIncrease?"ASI: "+string.Join(", ",levelUpPlan.AbilityIncreases.Select(p=>$"{AbilityShort(p.Key)} +{p.Value}")):"Feat: "+(LevelingRules.Feats.FirstOrDefault(f=>f.Id==levelUpPlan.FeatId)?.Name??"not chosen")):"No ASI/feat this level";
  foreach(var line in new[]{"Subclass: "+subclass,advancement,"Training: "+(levelUpPlan.SkillChoices.Count==0?"none":string.Join(", ",levelUpPlan.SkillChoices.Select(SkillRules.DisplayName))),"Expertise: "+(levelUpPlan.ExpertiseChoices.Count==0?"none":string.Join(", ",levelUpPlan.ExpertiseChoices.Select(SkillRules.DisplayName))),"Spells: "+(levelUpPlan.SpellChoices.Count==0?"none":string.Join(", ",levelUpPlan.SpellChoices.Select(DisplayId)))}){Text(line,summary.X+14,sy,Muted,.38f);sy+=28;}
  if(valid)Center("PLAN COMPLETE — READY TO ADVANCE",new(x,y+229,w,28),Teal,.47f,true);else{Center("INCOMPLETE",new(x,y+229,w,25),Gold,.44f,true);Center(why,new(x+20,y+258,w-40,45),Muted,.35f);}
  var confirm=new Rectangle(r.Center.X-150,r.Bottom-77,300,42);LevelChoice(confirm,"CONFIRM LEVEL UP",()=>ConfirmLevelUp(a),false,valid,valid?"Apply every reviewed choice atomically.":why,.50f);
 }
 void ConfirmLevelUp(Actor a)
 {
  if(levelUpPlan==null)return;if(!LevelingRules.Apply(world,a,levelUpPlan,out var summary)){levelUpNotice=summary;Notify(summary);return;}levelUpNotice="LEVEL UP COMPLETE — "+summary;Notify(summary);levelUpPlan=null;characterRecordPage=CharacterRecordPage.Overview;ui.Screens.Back();
 }

 void DrawLevelUpLaunchButton(Rectangle r,Actor a)
 {
  if(a.CanLevelUp)LevelChoice(r,"LEVEL UP",()=>OpenLevelUp(a),false,true,$"Spend no XP; choose how {a.Name} advances to level {a.TotalLevel+1}.",.43f);
  else LevelChoice(r,"LEVEL UP",()=>{},false,false,a.TotalLevel>=20?"Maximum level reached.":$"Requires {a.Character.NextLevelExperience:N0} cumulative XP; current XP {a.Character.Experience:N0}.",.43f);
 }
}
