using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 sealed partial class RestPanel
 {
  bool lowerView,lowerBook;
  int lowerPage,lowerTextPage;
  string lowerSelected="",lowerActor="";
  public void ShowLowerMall(string feature="")
  {if(!g.world.LowerMallEnabled||g.world.ReactionInterruptActive||g.ui.Dialogue.IsOpen)return;PrepareOpen();open=true;lowerView=true;contractsView=socialView=workshopView=equipmentView=classView=tensionView=worksView=shelterView=false;lowerActor=g.PrimaryPartyActor.Id;lowerSelected=g.world.CurrentLowerMallSite?.Actions.FirstOrDefault(a=>a.FeatureId==feature)?.Id??"";lowerBook=false;lowerPage=lowerTextPage=0;}
  void DrawLowerMall()
  {
   var w=g.world;var d=w.CurrentLowerMallSite;var actor=w.Party.FirstOrDefault(a=>a.Id==lowerActor)??w.PartyLeader;g.Panel(bounds);g.buttons.Add((bounds,()=>{}));int x=bounds.X+20,y=bounds.Y+16,width=bounds.Width-40,left=(width-24)/2,right=x+left+24;
   g.Text(d==null?"MALL LOCATION DIRECTORY":$"{d.Level.ToUpperInvariant()} #{d.DisplayNumber} {d.Name.ToUpperInvariant()}",x,y,Gold,.51f,true);g.Button(new(bounds.Right-280,y,175,28),"NEXT SURVIVOR",()=>{var crew=w.Party.ToArray();lowerActor=crew[(Array.IndexOf(crew,actor)+1)%crew.Length].Id;},false,.4f);g.Button(new(bounds.Right-90,y,65,28),"CLOSE",()=>Close(),false,.4f);y+=44;
   g.Button(new(x,y,190,28),"OBJECTS / PROGRESS",()=>{lowerBook=false;lowerTextPage=0;},!lowerBook,.4f);g.Button(new(x+200,y,240,28),"BOOK REFERENCE (SPOILERS)",()=>{lowerBook=true;lowerTextPage=0;},lowerBook,.39f);g.Text($"{actor.Name} | {World.JournalTimeText(w.CampaignMinutes)} | LOCATION XP {w.LowerMallXpAwarded}, POOL {w.LowerMallXpRemainder}",x+460,y+9,Teal,.37f);y+=43;
   if(d==null)
   {
    var all=w.Campaign!.LowerMall!.Sites;int pages=(all.Count+8)/9;lowerPage=Math.Clamp(lowerPage,0,pages-1);foreach(var site in all.Skip(lowerPage*9).Take(9)){var p=w.LowerMallProgress(site.Number);g.Text($"{site.Level.ToUpperInvariant()} #{site.DisplayNumber} {site.Name} | {(p?.Secured==true?"SECURED":p!=null?"EXPLORED":"UNVISITED")}",x,y,p?.Secured==true?Teal:Muted,.43f);y+=31;}
    g.Paragraph("Walk the south concourse to the numbered storefronts. Each shop has a complete interior, source-specific caches and clues, an entry hazard, and its own back-room threat. The office refuge is reached through the west service access. East passages connect the south concourse, north concourse and final arcade. The central escalator on the lower south concourse leads to upper locations 1–18. The east passages continue to upper 19–54, then the final gallery stair reaches roof areas 1–25 through the eastern service walkway. The main exit on the lower south concourse reaches the perimeter road and exterior 1–13. Its eastern exit reaches Outer Service Road for exterior 14–26. Roof access also connects the scaffold, loading stairs, skybridge and camp fire escape to their exterior destinations.",right,bounds.Y+140,width-left-30,Muted,.44f,23);g.Button(new(x,bounds.Bottom-55,150,28),"PREVIOUS",()=>lowerPage=Math.Max(0,lowerPage-1),false,.4f);g.Button(new(x+170,bounds.Bottom-55,150,28),"NEXT",()=>lowerPage=Math.Min(pages-1,lowerPage+1),false,.4f);return;
   }
   var progress=w.LowerMallProgress(d.Number);g.Text($"BOOK pp. {d.Source.PageStart}–{d.Source.PageEnd} | {(progress?.Secured==true?"SECURED":progress?.HazardResolved==true?"ENTRANCE HAZARD RESOLVED":"ENTRANCE HAZARD ACTIVE")}",x,y,Teal,.41f);y+=29;
   if(d.Level=="exterior"){g.Text(w.Exterior31Status+w.Exterior30Status,x,y,Teal,.36f);y+=24;}
   if(d.Level=="roof"&&d.DisplayNumber>=14){g.Text(w.Roof29Status,x,y,Teal,.36f);y+=24;}
   if(d.Level=="roof"&&d.DisplayNumber<14){int fuel=Math.Max(0,(w.LowerMallProgress(206)?.RoofTimers.GetValueOrDefault("fuelUntil")??w.CampaignMinutes)-w.CampaignMinutes);g.Text($"GRID {(w.RoofGridOnline?"ON":"OFF")} | FUEL {fuel} MIN | WATER {(w.RoofWaterPurified?"PURIFIED":"UNREPAIRED")}",x,y,Teal,.4f);y+=21;}
   if(lowerBook)
   {
    var lines=d.BookText.Replace('\uf0b7','-').Replace("\r","").Split('\n').SelectMany(line=>g.Wrap(line,width-20,.43f).DefaultIfEmpty("")).ToArray();int count=14,pages=Math.Max(1,(lines.Length+count-1)/count);lowerTextPage=Math.Clamp(lowerTextPage,0,pages-1);foreach(var line in lines.Skip(lowerTextPage*count).Take(count)){g.Text(line,x,y,Muted,.43f);y+=21;}g.Button(new(x,bounds.Bottom-58,130,28),"PREVIOUS TEXT",()=>lowerTextPage=Math.Max(0,lowerTextPage-1),false,.37f);g.Text($"PAGE {lowerTextPage+1}/{pages}",x+145,bounds.Bottom-50,Muted,.39f);g.Button(new(x+285,bounds.Bottom-58,130,28),"NEXT TEXT",()=>lowerTextPage=Math.Min(pages-1,lowerTextPage+1),false,.37f);return;
   }
   var actions=d.Actions.Where(a=>a.Kind is not ("reference" or "event")&&(a.Kind!="hazard"||a.Id==progress?.HazardId)).ToArray();int listPages=Math.Max(1,(actions.Length+6)/7);lowerPage=Math.Clamp(lowerPage,0,listPages-1);var chosen=actions.FirstOrDefault(a=>a.Id==lowerSelected)??actions.Skip(lowerPage*7).First();lowerSelected=chosen.Id;int yy=y;
   foreach(var action in actions.Skip(lowerPage*7).Take(7)){var a=action;g.Button(new(x,y,left,30),$"{(progress?.Resolved.Contains(a.Id)==true?"DONE | ":"")}{a.Name}",()=>{lowerSelected=a.Id;lowerTextPage=0;},chosen==a,.39f);y+=36;}
   var text=g.Wrap(System.Text.RegularExpressions.Regex.Replace(chosen.Text,@"\s+"," "),width-left-40,.43f).ToArray();int tp=Math.Max(1,(text.Length+7)/8);lowerTextPage=Math.Clamp(lowerTextPage,0,tp-1);foreach(var line in text.Skip(lowerTextPage*8).Take(8)){g.Text(line,right,yy,Muted,.43f);yy+=21;}yy+=12;g.Button(new(right,yy,190,25),$"DESCRIPTION {lowerTextPage+1}/{tp}",()=>lowerTextPage=(lowerTextPage+1)%tp,false,.36f);yy+=35;g.Text($"{chosen.Skill} DC {chosen.Dc} | {chosen.Minutes} MIN | MILESTONE {chosen.Xp} XP",right,yy,Teal,.39f);yy+=28;
   var feature=w.Features.Single(f=>f.Id==chosen.FeatureId);bool found=progress?.Resolved.Contains(chosen.Id)==true&&chosen.Kind=="loot";string reason=w.LowerMallActionReason(actor,chosen.Id);g.CommandButton(new(right,yy,width-left-24,29),found?"OPEN FOUND CACHE":"RESOLVE / SEARCH CAREFULLY",()=>{if(found){Close();w.RequestInteraction(actor,feature);}else w.PerformLowerMallAction(actor,chosen.Id);},found||reason=="",reason==""?"Uses real checks and campaign time; rewards cannot repeat.":reason,false,.39f);yy+=35;
   if(chosen.ForceDc>0)g.CommandButton(new(right,yy,width-left-24,28),$"FORCE LOCK (ATHLETICS DC {chosen.ForceDc})",()=>w.PerformLowerMallAction(actor,chosen.Id,true),reason=="",reason,false,.37f);
   g.Button(new(right,bounds.Bottom-55,190,28),"APPROACH OBJECT",()=>{Close();w.RequestInteraction(actor,feature);},false,.4f);string secure=w.SecureLowerMallReason(actor);g.CommandButton(new(right+205,bounds.Bottom-55,width-left-229,28),"SECURE LOCATION",()=>w.SecureLowerMall(actor),secure=="",secure==""?"Clear threats and secure access; source milestone pays once.":secure,false,.37f);g.Button(new(x,bounds.Bottom-55,100,28),"PREVIOUS",()=>{lowerPage=Math.Max(0,lowerPage-1);lowerSelected="";},false,.36f);g.Text($"OBJECTS {lowerPage+1}/{listPages}",x+115,bounds.Bottom-47,Muted,.37f);g.Button(new(x+left-100,bounds.Bottom-55,100,28),"NEXT",()=>{lowerPage=Math.Min(listPages-1,lowerPage+1);lowerSelected="";},false,.36f);g.Text("Search physical objects. Doors block access. Found goods earn scavenging XP only when secured at a real stockpile.",x,bounds.Bottom-18,Muted,.34f);
  }
 }
 void StageLowerCapture()
 {
  title=scene=="lower-title";if(title)return;camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();if(scene!="lower-field")ui.Rest.ShowLowerMall();
 }
}
