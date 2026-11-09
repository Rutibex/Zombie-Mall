using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 sealed partial class RestPanel
 {
  bool worksView;
  public void ShowWorks(){Open();if(IsOpen){worksView=true;shelterView=false;}}
  void DrawWorks()
  {
   var w=g.world;var s=w.InfrastructureState;var d=w.Campaign!.Infrastructure!;g.Panel(bounds);g.buttons.Add((bounds,()=>{}));
   int x=bounds.X+20,y=bounds.Y+18;g.Text("BARRICADES & UTILITIES",x,y,Bright,.58f,true);
   g.Text(w.CampaignTimeText,x+390,y+7,Gold,.46f);
   g.Button(new(bounds.Right-280,y,80,28),"ROSTER",()=>{worksView=false;shelterView=true;},false,.43f);
   g.Button(new(bounds.Right-185,y,80,28),"CARE",()=>{worksView=false;shelterView=false;},false,.43f);
   g.Button(new(bounds.Right-90,y,65,28),"CLOSE",()=>Close(),false,.43f);
   if(w.TensionEnabled)g.Button(new(bounds.Right-380,y,90,28),"TENSION",()=>{tensionView=true;worksView=false;},false,.43f);
   if(w.SurvivorClassesEnabled)g.Button(new(bounds.Right-480,y,90,28),"CLASSES",()=>ShowClasses(),false,.4f);
   int width=(bounds.Width-80)/3,top=bounds.Y+73,height=bounds.Height-166;
   void TextLines(string text,int xx,ref int yy,int max=3){foreach(var line in g.Wrap(text,width-28,.46f).Take(max)){g.Text(line,xx,yy,Muted,.46f);yy+=19;}yy+=7;}
   void Action(WorksAction action,string label,int xx,ref int yy)
   {
    string reason=w.WorksReason(action);g.CommandButton(new(xx,yy,width-28,32),label,()=>{if(w.PerformWorks(action))g.Notify(label+" complete.");else g.Notify(w.WorksReason(action));},reason=="",reason==""?$"{w.WorksMinutes(action)} minutes. {w.WorksCostText(action)}. Costs are physical and permanent.":reason,false,.46f);yy+=39;
   }
   for(int i=0;i<3;i++)g.Box(new(x+i*(width+20),top,width,height),new Color(13,20,20),Gold*.35f);
   int xx=x+14,yy=top+13;g.Text("OFFICE BARRICADE",xx,yy,Gold,.47f,true);yy+=31;
   g.Text($"HP {s.BarricadeHp}/{d.BarricadeMaxHp} | "+(s.BarricadeHp==0?"ABSENT / BROKEN":s.BarricadeEngaged?"SECURED":"UNBARRED"),xx,yy,s.BarricadeHp>0?Teal:Muted,.47f);yy+=27;
   TextLines("Build: "+w.WorksCostText(WorksAction.BuildBarricade),xx,ref yy);
   Action(WorksAction.BuildBarricade,"BUILD / REBUILD - 30 MIN",xx,ref yy);
   Action(WorksAction.RepairBarricade,"REPAIR +8 HP - 15 MIN",xx,ref yy);
   Action(WorksAction.Unbar,"UNBAR DOOR - RETAIN HP",xx,ref yy);
   Action(WorksAction.Secure,"RE-SECURE DOOR - RETAIN HP",xx,ref yy);
   TextLines("Repair costs one plank. A broken barrier requires full rebuild materials. Friendly door interaction can unbar during danger; combat costs an Action.",xx,ref yy,5);
   xx=x+width+34;yy=top+13;g.Text("GENERATOR & LIGHTING",xx,yy,Gold,.47f,true);yy+=31;
   g.Text(!s.GeneratorInstalled?"NOT INSTALLED":w.ShelterPowered?"RUNNING":"STOPPED",xx,yy,w.ShelterPowered?Teal:Muted,.47f);yy+=25;
   g.Text($"Fuel {s.FuelMinutes}/{d.FuelCapacityMinutes} minutes",xx,yy,Muted,.47f);yy+=25;
   TextLines("Install: "+w.WorksCostText(WorksAction.InstallGenerator),xx,ref yy);
   Action(WorksAction.InstallGenerator,"INSTALL GENERATOR - 60 MIN",xx,ref yy);
   Action(WorksAction.Refuel,$"ADD ONE FUEL UNIT (+{d.FuelMinutesPerUnit} MIN)",xx,ref yy);
   Action(WorksAction.TogglePower,s.Running?"STOP GENERATOR":"START GENERATOR",xx,ref yy);
   TextLines("Starting it creates a 60-foot local sound. Fuel burns during exploration, combat rounds, travel and rest. No offline/menu drain. Power lights the known refuge and runs purification.",xx,ref yy,6);
   xx=x+(width+20)*2+14;yy=top+13;g.Text("WATER STATION",xx,yy,Gold,.47f,true);yy+=31;
   g.Text(s.WaterStationInstalled?$"INSTALLED | {s.PurifiedUnits} units purified":"NOT INSTALLED",xx,yy,s.WaterStationInstalled?Teal:Muted,.47f);yy+=27;
   TextLines("Install: "+w.WorksCostText(WorksAction.InstallWaterStation),xx,ref yy);
   Action(WorksAction.InstallWaterStation,"INSTALL STATION - 30 MIN",xx,ref yy);
   Action(WorksAction.PurifyWater,"PURIFY ONE WATER - 10 MIN",xx,ref yy);
   TextLines($"Inputs: {w.WorksMaterialCount(d.DirtyWaterItemId)} untreated water, {w.WorksMaterialCount(d.FilterItemId)} tablets. Requires running power and ten fuel-minutes. Output goes to the refuge stockpile; it grants no scavenging XP.",xx,ref yy,6);
   string reason=w.ShelterManagementReason();int footer=bounds.Bottom-79;
   g.Text($"One-time construction XP: {s.XpAwarded} distributed, {s.XpRemainder} pooled. Source values: barricade {d.BarricadeXp} SV, generator {d.GeneratorXp} SV.",x,footer,Gold,.45f);
   g.Paragraph(reason==""?"Use materials in the expedition leader's backpack or refuge stockpile. Rebuilds and repairs never re-award construction XP.":reason,x,footer+25,bounds.Width-40,reason==""?Muted:Red,.45f,19);
  }
 }
 void StageWorksCapture()
 {
  title=false;if(scene=="works-title"){title=true;return;}
  if(scene!="works-playthrough")
  {
   foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;world.SetDialogueFlag("flag.mall.shelter");int i=0;foreach(var a in world.Party)a.Pos=new(12+i++,27);world.Reveal();
   if(scene=="works-powered"){world.PerformWorks(WorksAction.BuildBarricade);world.PerformWorks(WorksAction.InstallGenerator);world.PerformWorks(WorksAction.InstallWaterStation);world.PerformWorks(WorksAction.Refuel);world.PerformWorks(WorksAction.TogglePower);world.PerformWorks(WorksAction.PurifyWater);}
  }
  camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();ui.Rest.ShowWorks();
 }
}
