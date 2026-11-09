using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 sealed partial class RestPanel
 {
  bool classView;
  string classActor="",classCandidate="strong";
  int classFeaturePage,classActionPage,classTargetIndex;
  Ability classAbility=Ability.Strength;
  Ability? classSecond;
  Skill classSkill=Skill.Perception;
  string classPath="";
  Skill[] classInitial=[Skill.Athletics,Skill.Intimidation,Skill.Survival];
  Cell? classCell;
  public void ShowClasses(Actor? actor=null,Actor? target=null,Cell? cell=null,bool advanced=false)
  {
   if(!g.world.SurvivorClassesEnabled||g.world.ReactionInterruptActive||g.ui.Dialogue.IsOpen)return;
   PrepareOpen();open=true;equipmentView=false;classView=true;worksView=shelterView=tensionView=false;
   actor??=g.world.Mode==Mode.Combat&&g.world.Active is {Enemy:false} active?active:g.PrimaryPartyActor;classActor=actor.Id;classCandidate=actor.Training.ClassId==""?"strong":actor.Training.ClassId;classAbility=SurvivorClassesRules.Find(classCandidate)!.Primary;classSecond=null;classPath="";classFeaturePage=advanced?1:0;classActionPage=0;classCell=cell;
   classInitial=SurvivorClassesRules.Find(classCandidate)!.Skills.ToArray();var targets=ClassTargets();classTargetIndex=target==null?0:Math.Max(0,Array.FindIndex(targets,a=>a.Id==target.Id));
  }
  Actor[] ClassTargets()=>g.world.Actors.Where(a=>!a.Dead&&(!a.Enemy||g.world.Visible.Contains(a.Pos))).OrderBy(a=>a.Enemy).ToArray();
  void DrawClasses()
  {
   var w=g.world;var members=w.CommunityMembers.ToArray();var actor=members.FirstOrDefault(a=>a.Id==classActor)??w.PartyLeader;var selected=SurvivorClassesRules.Find(classCandidate)!;bool chosen=actor.Training.ClassId!="";
   g.Panel(bounds);g.buttons.Add((bounds,()=>{}));int x=bounds.X+20,top=bounds.Y+70,y=bounds.Y+18;int width=(bounds.Width-80)/3,mid=x+width+20,right=mid+width+20;
   g.Text("SURVIVOR CLASSES",x,y,Bright,.58f,true);
   g.Button(new(bounds.Right-300,y,195,28),"NEXT SURVIVOR",()=>{int i=Array.FindIndex(members,a=>a.Id==actor.Id);ShowClasses(members[(i+1)%members.Length]);},false,.43f);
   g.Button(new(bounds.Right-90,y,65,28),"CLOSE",()=>Close(),false,.43f);
   for(int i=0;i<3;i++)g.Box(new(x+i*(width+20),top,width,bounds.Height-122),new Color(13,20,20),Gold*.3f);
   int yy=top+12;g.Text(actor.Name.ToUpperInvariant()+$" | LEVEL {actor.TotalLevel}",x+12,yy,Gold,.48f,true);yy+=26;
   g.Text($"XP {actor.Character.Experience} / {(actor.TotalLevel>=10?"MAX":SurvivalProgression.RequiredForLevel(actor.TotalLevel+1).ToString())} | HP {actor.Hp}/{actor.MaxHp}",x+12,yy,Muted,.42f);yy+=27;
   foreach(var c in SurvivorClassesRules.All)
   {
    g.Button(new(x+12,yy,width-24,31),c.Name.ToUpperInvariant(),()=>{classCandidate=c.Id;classInitial=c.Skills.ToArray();classAbility=c.Primary;classSecond=null;classFeaturePage=0;},classCandidate==c.Id,.43f);yy+=37;
   }
   yy+=4;g.Paragraph(chosen?$"Training: {SurvivorClassesRules.Find(actor.Training.ClassId)!.Name}. Path: {(SurvivorClassesRules.Find(actor.Training.ClassId)!.Paths.Length==0?"none":actor.Training.Path==""?"not yet chosen":actor.Training.Path)}. Class choice is permanent.":"Choose a class and three skills. Authored starting HP, gear and all earned XP remain. No extra equipment is created.",x+12,yy,width-24,Muted,.43f,19);
   int my=top+12;
   if(!chosen)
   {
    g.Text($"d{selected.HitDie} | {selected.Primary}",mid+12,my,Gold,.48f,true);my+=31;g.Paragraph("Saves: "+string.Join(", ",selected.Saves),mid+12,my,width-24,Muted,.43f,19);my+=45;
    var allowed=SurvivorClassesRules.AllowedSkills(selected.Id);for(int i=0;i<3;i++){int n=i;g.Button(new(mid+12,my,width-24,33),$"SKILL {i+1}: {SkillRules.DisplayName(classInitial[i])}",()=>classInitial[n]=allowed[(Array.IndexOf(allowed,classInitial[n])+1)%allowed.Length],false,.43f);my+=42;}
    string reason=w.ChooseSurvivorClassReason(actor,selected.Id,classInitial);g.CommandButton(new(mid+12,my+8,width-24,37),"CHOOSE THIS CLASS",()=>{if(w.ChooseSurvivorClass(actor,selected.Id,classInitial)){g.Notify("Class chosen.");ShowClasses(actor);}else g.Notify(reason);},reason=="",reason==""?"Permanent class choice. No HP reset or equipment grant.":reason,false,.45f);
    g.Paragraph("Click a class to browse its ten-level progression. First Aid uses the Medicine skill. Advancement uses the module's survivor XP thresholds.",mid+12,my+65,width-24,Muted,.43f,20);
   }
   else
   {
    var actual=SurvivorClassesRules.Find(actor.Training.ClassId)!;if(actual.Id=="wise"){classAbility=Ability.Wisdom;classSecond=null;}g.Text("ADVANCEMENT",mid+12,my,Gold,.47f,true);my+=29;
    if(actual.Paths.Length>0&&actor.TotalLevel==2){if(!actual.Paths.Contains(classPath))classPath=actual.Paths[0];g.Button(new(mid+12,my,width-24,28),"PATH: "+classPath,()=>classPath=actual.Paths[(Array.IndexOf(actual.Paths,classPath)+1)%actual.Paths.Length],false,.4f);my+=35;}
    g.Button(new(mid+12,my,width-24,28),"ABILITY: "+classAbility,()=>classAbility=(Ability)(((int)classAbility+1)%6),false,.4f);my+=34;
    g.Button(new(mid+12,my,width-24,28),actual.Id=="wise"?"WISE IMPROVEMENT: +1 WIS":classSecond.HasValue?"SPLIT +1: "+classSecond:"ABILITY MODE: +2 TO ONE",()=>{classSecond=classSecond==null?Ability.Strength:classSecond==Ability.Charisma?null:(Ability)((int)classSecond+1);},false,.4f);my+=34;
    g.Button(new(mid+12,my,width-24,28),"SKILL: "+SkillRules.DisplayName(classSkill),()=>classSkill=(Skill)(((int)classSkill+1)%Enum.GetValues<Skill>().Length),false,.4f);my+=34;
    string reason=w.AdvanceSurvivorReason(actor,classPath,classAbility,classSkill,classSecond);g.CommandButton(new(mid+12,my,width-24,31),"ADVANCE ONE LEVEL",()=>{if(w.AdvanceSurvivor(actor,classPath,classAbility,classSkill,classSecond))g.Notify("Level gained.");else g.Notify(reason);},reason=="",reason==""?"Adds one earned survivor level; XP is retained.":reason,false,.42f);my+=44;
    var targets=ClassTargets();classTargetIndex=Math.Clamp(classTargetIndex,0,Math.Max(0,targets.Length-1));var target=targets.Length>0?targets[classTargetIndex]:null;
    g.Button(new(mid+12,my,width-24,28),"TARGET: "+(target?.Name??"none"),()=>classTargetIndex=targets.Length==0?0:(classTargetIndex+1)%targets.Length,false,.4f);my+=35;
    var actions=w.AvailableSurvivorActions(actor).ToArray();if(actions.Length==0)g.Paragraph("This level grants passive class features. Active abilities unlock at the levels shown.",mid+12,my,width-24,Muted,.42f,20);int pages=Math.Max(1,(actions.Length+2)/3);classActionPage=Math.Clamp(classActionPage,0,pages-1);
    foreach(var d in actions.Skip(classActionPage*3).Take(3))
    {string why=w.SurvivorActionReason(actor,d.Action,target,classCell);g.CommandButton(new(mid+12,my,width-24,29),d.Name,()=>{if(w.UseSurvivorAction(actor,d.Action,target,classCell)){g.Notify(d.Name+" used.");Close();}else g.Notify(why);},why=="",d.Cost+(d.Resource!=""?$" | Uses {actor.Character.Resource("survivor."+d.Resource)?.Current??0}":"")+(why==""?"":" | "+why),false,.42f);my+=35;}
    if(pages>1)g.Button(new(mid+12,bounds.Bottom-89,width-24,25),$"MORE ABILITIES {classActionPage+1}/{pages}",()=>classActionPage=(classActionPage+1)%pages,false,.38f);
   }
   int ry=top+12;g.Text(selected.Name.ToUpperInvariant(),right+12,ry,Gold,.46f,true);ry+=28;
   g.Text($"MODULE P.{selected.Page} | LEVELS {classFeaturePage*5+1}-{classFeaturePage*5+5}",right+12,ry,Muted,.39f);ry+=28;
   for(int i=classFeaturePage*5;i<Math.Min(10,classFeaturePage*5+5);i++)
   {g.Text($"LEVEL {i+1}",right+12,ry,chosen&&actor.Training.ClassId==selected.Id&&actor.TotalLevel>i?Teal:Gold,.41f,true);ry+=20;foreach(var line in g.Wrap(selected.Features[i],width-26,.4f).Take(2)){g.Text(line,right+12,ry,Muted,.4f);ry+=17;}ry+=9;}
   g.Button(new(right+12,bounds.Bottom-89,width-24,25),classFeaturePage==0?"NEXT: LEVELS 6-10":"BACK: LEVELS 1-5",()=>classFeaturePage=1-classFeaturePage,false,.4f);
   g.Paragraph("Right-click a map location or character, then Class abilities, to supply a target or landing point. Menus pause simulation; charges and physical costs persist.",x,bounds.Bottom-42,bounds.Width-40,Muted,.4f,18);
  }
 }
 void StageClassesCapture()
 {
  title=false;if(scene=="classes-title"){title=true;return;}
  if(scene!="classes-playthrough")
  {
   foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;world.SetDialogueFlag("flag.mall.shelter");int i=0;foreach(var a in world.Party)a.Pos=new(12+i++,27);world.Reveal();
   string name=scene.StartsWith("classes-",StringComparison.Ordinal)?scene[8..].Split('-')[0]:"strong";var d=SurvivorClassesRules.Find(name);
   if(d!=null){var a=world.PartyLeader;world.ChooseSurvivorClass(a,d.Id);a.Character.Experience=12000;for(int level=2;level<=10;level++){Ability ability=a.Character.Abilities.Score(d.Primary)<19?d.Primary:Ability.Wisdom;Skill skill=Enum.GetValues<Skill>().First(s=>a.Character.Proficiencies.SkillRank(s)!=ProficiencyRank.Expertise);world.AdvanceSurvivor(a,d.Paths.FirstOrDefault()??"",ability,skill);}}
  }
  camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();if(scene.EndsWith("-record",StringComparison.Ordinal)){characterRecordPage=CharacterRecordPage.Skills;ui.Screens.NavigateRoot(UiScreenId.Character);return;}ui.Rest.ShowClasses(advanced:scene.EndsWith("-advanced",StringComparison.Ordinal));
 }
}
