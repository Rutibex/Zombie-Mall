using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 sealed partial class RestPanel
 {
  bool tensionView;
  public void ShowTension(){Open();if(IsOpen){tensionView=true;worksView=false;shelterView=false;}}
  void DrawTension()
  {
   var w=g.world;var s=w.TensionState;g.Panel(bounds);g.buttons.Add((bounds,()=>{}));int x=bounds.X+20,y=bounds.Y+18;
   g.Text("TENSION & DAILY EVENTS",x,y,Bright,.58f,true);
   g.Button(new(bounds.Right-270,y,165,28),"WORKS / UTILITIES",()=>{tensionView=false;worksView=true;},false,.43f);
   g.Button(new(bounds.Right-90,y,65,28),"CLOSE",()=>Close(),false,.43f);
   y+=48;g.Text($"TENSION {s.Score}/100 - {w.TensionBand}",x,y,s.Score>60?Red:Gold,.64f,true);
   g.Text(w.CampaignTimeText+" | Next check: Day "+(s.NextDayMinute/1440+1),x+450,y+5,Muted,.47f);y+=35;
   g.Text(s.LastReason==""?"No Tension adjustments yet.":$"Last change: {s.LastAdjustment:+0;-0;0} | {s.LastReason}",x,y,Muted,.47f);y+=29;
   g.Paragraph("Gunfire +2; combat +1 per round; shouts and generator startup +1d6. Moving barricades +1d4. First reinforcement, paid repairs, quiet secured days and defeating whole hordes reduce Tension.",x,y,bounds.Width-40,Muted,.45f,19);y+=61;
   g.Text($"PERIMETER: {s.PendingAttacks.Count} queued | {w.Campaign!.Tension!.AttackHordeIds.Count(id=>w.ReleasedHordes.Contains(id))}/3 groups deployed",x,y,Gold,.48f,true);y+=27;
   g.Paragraph(s.PendingAttacks.Count==0?"No attack is waiting. Each authored group can appear once. Corpses, wounds and retreat persist.":w.DailyAttackReason()==""?"Attack ready at the refuge. Closing this panel resumes gameplay and deploys it. Prepare finite supplies and defenses first.":w.DailyAttackReason(),x,y,bounds.Width-40,Muted,.46f,20);y+=49;
   g.Text("RECENT DAILY REPORTS (D100 + TENSION)",x,y,Gold,.46f,true);y+=27;
   if(s.History.Count==0)g.Paragraph("Establish the office refuge. The first daily check occurs at the next midnight. Menus and offline time do not advance the clock.",x,y,bounds.Width-40,Muted,.47f,21);
   foreach(var e in s.History.TakeLast(3).Reverse())
   {
    g.Text($"DAY {e.Day} | {e.Kind.ToUpperInvariant()} | {e.Roll} + {e.Tension} = {e.Roll+e.Tension}",x,y,Teal,.46f,true);y+=23;
    foreach(var line in g.Wrap(e.Result,bounds.Width-45,.43f).Take(2)){g.Text(line,x,y,Muted,.43f);y+=18;}y+=12;
   }
   g.Text("Full reports remain in the journal. This phase adapts local refuge incidents; endgame disasters are deferred.",x,bounds.Bottom-30,Muted,.42f);
  }
 }
 void StageTensionCapture()
 {
  title=false;
  if(scene is not ("tension-playthrough" or "tension-field"))
  {
   foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;world.SetDialogueFlag("flag.mall.shelter");int i=0;foreach(var a in world.Party)a.Pos=new(12+i++,27);world.Reveal();world.PerformWorks(WorksAction.BuildBarricade);world.CampaignMinutes=1440;world.ProcessSurvivalTime();
  }
  camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();if(scene!="tension-field")ui.Rest.ShowTension();
 }
}
