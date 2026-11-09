using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 void ShowExpeditionReport()
 {
  ui.Loot.Close();ui.Dialogue.Close(false);contextOpen=false;world.StopAll();SelectAll();ui.Screens.Navigate(UiScreenId.Expedition);
 }
 void DrawExpeditionTracker()
 {
  if(title||world.Campaign?.Expedition is not {} expedition||ui.Screens.IsOpen||ui.Loot.IsOpen||ui.Dialogue.IsOpen||InterfaceViewportExpanded)return;
  int x=MapRect.X+18,y=MapRect.Y+(world.Mode==Mode.Combat?155:84);var stage=world.CurrentExpeditionStage;var r=new Rectangle(x,y,430,89);Box(r,new Color(17,32,35)*.94f,Teal*.5f);
  string heading=world.CompletedExpedition!=null?"EXPEDITION COMPLETE":stage?.Title.ToUpperInvariant()??"RETURN AND REPORT";
  Text(heading,x+12,y+9,Bright,.44f,true);Button(new(r.Right-67,y+6,57,24),"PLAN",()=>ui.Screens.Navigate(UiScreenId.Expedition),false,.37f);
  string hint=world.CompletedExpedition!=null?"Your foothold is secured. You can keep exploring.":stage?.Hint??"Gather everyone in Wanderlust's back office and use the expedition report board.";
  Paragraph(hint,x+12,y+36,r.Width-24,Muted,.42f,17);
 }
 void DrawExpeditionScreen(Rectangle r)
 {
  if(world.Campaign?.Expedition is not {} expedition)return;
  int x=r.X+35,y=r.Y+92,w=r.Width-70;var report=world.CompletedExpedition;
  Text(report==null?expedition.Title.ToUpperInvariant():"FIRST EXPEDITION COMPLETE",x,y,report==null?Bright:Teal,.64f,true);y+=43;
  if(report==null)
  {
   Paragraph(expedition.Briefing,x,y,w,Muted,.47f,20);y+=85;
   foreach(var stage in expedition.Stages)
   {
    bool complete=world.ExpeditionStageComplete(stage);var card=new Rectangle(x,y,w,64);Box(card,new Color(16,25,27),complete?Teal*.5f:Gold*.3f);
    Text((complete?"[DONE] ":"[ ] ")+stage.Title,x+13,y+8,complete?Teal:Bright,.44f,true);Paragraph(stage.Hint,x+13,y+31,w-26,Muted,.39f,15);y+=73;
   }
   var point=world.Features.FirstOrDefault(f=>f.Id==expedition.DebriefFeatureId);
   string state=point==null?"Find the office report board.":world.ExpeditionReturnReason(point);
   if(state=="")state="Everyone is ready. Use the report board beside the office door.";
   Paragraph(state,x,Math.Min(y+2,r.Bottom-98),w,Gold,.42f,18);
  }
  else
  {
   Paragraph(expedition.SuccessText,x,y,w,Bright,.52f,23);y+=110;
   string[] labels=["SURVIVORS RETURNED","SUPPLIES IN STOCK","LIFETIME EARNED SV","RESOURCE XP SHARED","XP STILL POOLED","EXPEDITION TIME"];
   string[] values=[report.PartyIds.Count.ToString(),report.StoredSv+" SV",report.EarnedSv+" SV",report.ResourceXp+" XP",report.PooledXp+" XP",report.ElapsedMinutes+(report.ElapsedMinutes==1?" minute":" minutes")];
   for(int i=0;i<labels.Length;i++){var box=new Rectangle(x+(i%2)*(w/2+5),y+(i/2)*70,w/2-5,61);Box(box,new Color(17,29,29),Teal*.4f);Text(labels[i],box.X+12,box.Y+9,Muted,.36f,true);Text(values[i],box.X+12,box.Y+31,Bright,.57f);}
   Paragraph("This report records your return. You can save the journey or continue exploring the southwest section.",x,y+233,w,Muted,.45f,20);
  }
  int by=r.Bottom-52;Button(new(x,by,240,34),report==null?"RETURN TO EXPEDITION":"CONTINUE EXPLORING",()=>ui.Screens.ReturnToGame(),true,.43f);
  Button(new(x+255,by,170,34),"SAVE GAME",()=>OpenSaveBrowser(true),false,.43f);
  if(report==null){var board=world.Features.FirstOrDefault(f=>f.Id==expedition.DebriefFeatureId);string reason=board==null?"Find the office report board.":world.ExpeditionReturnReason(board);bool near=board!=null&&world.Party.Any(a=>a.Alive&&a.Position.Distance(board.Pos.Center)<=1.25f);CommandButton(new(x+440,by,180,34),"REPORT BACK",()=>{if(board!=null)world.Interact(board);},reason==""&&near,reason!=""?reason:near?"Finish the expedition at this report board.":"Move a survivor beside the office report board.",false,.43f);}
 }
 void StageExpeditionCapture()
 {
  world.ExpeditionReported=ShowExpeditionReport;
  if(scene=="expedition-title"){title=true;return;}title=false;
  if(scene=="expedition-playthrough"){camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);ui.Screens.Navigate(UiScreenId.Expedition);return;}
  if(scene=="expedition-briefing"){ui.Screens.Navigate(UiScreenId.Expedition);return;}
  if(scene=="expedition-entry")return;
  foreach(var door in world.Features.Where(f=>f.Kind=="door"))door.Used=true;
  foreach(var enemy in world.Actors.Where(a=>a.Enemy))enemy.Hp=0;
  foreach(var f in world.Features.Where(f=>f.Kind=="campaign-event"&&f.Id!="feature.mall.stockpile-note"&&f.Id!="feature.mall.shutter"))
  {world.Chosen.Pos=f.Pos;world.Reveal();world.Interact(f);}
  var hero=world.Chosen;foreach(var source in world.Features.Where(f=>world.CurrentCampaignArea!.Features.Any(d=>d.Id==f.Id&&d.Scavengable)))
  {hero.Pos=source.Pos.Neighbors().First(p=>!world.Blocked(p)&&!world.Actors.Any(a=>a!=hero&&a.Alive&&a.Pos==p));world.Reveal();world.TakeAllContainer(hero,source);}
  hero.Pos=new(14,27);world.Reveal();world.SecureCarriedResources(hero,world.Features.First(f=>f.Id=="feature.mall.supplies"));
  Cell[] positions=[new(12,27),new(11,27),new(13,27),new(14,27)];int index=0;foreach(var a in world.Party)a.Pos=positions[index++];world.Reveal();world.SyncCampaignObjectives();camera=new(12,27);SelectAll();
  if(scene=="expedition-ready"){ui.Screens.Navigate(UiScreenId.Expedition);return;}
  if(!world.Interact(world.Features.First(f=>f.Kind=="debrief")))throw new InvalidOperationException("Expedition capture report failed.");
  if(scene=="expedition-after")ui.Screens.ReturnToGame();
 }
}
