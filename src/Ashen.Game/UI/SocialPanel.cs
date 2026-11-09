using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 sealed partial class RestPanel
 {
  bool socialView;
  int socialTab,socialPage;
  string socialSelected="",socialActor="";
  public void ShowSocial(int tab=0)
  {if(!g.world.SocialEnabled||g.world.ReactionInterruptActive||g.ui.Dialogue.IsOpen)return;PrepareOpen();open=true;socialView=true;contractsView=workshopView=equipmentView=classView=tensionView=worksView=shelterView=false;socialTab=tab;socialSelected="";socialPage=0;socialActor=g.PrimaryPartyActor.Id;}
  public void PreviewSocial(string id){socialTab=2;socialSelected=id;socialPage=0;}
  void DrawSocial()
  {
   var w=g.world;var d=w.Campaign!.Social!;var actor=w.Party.FirstOrDefault(a=>a.Id==socialActor)??w.PartyLeader;g.Panel(bounds);g.buttons.Add((bounds,()=>{}));int x=bounds.X+20,y=bounds.Y+18,width=bounds.Width-40,left=(width-25)/2,right=x+left+25,detailWidth=width-left-25;
   g.Text("FACTIONS & SURVIVOR RELATIONSHIPS",x,y,Gold,.51f,true);g.Button(new(bounds.Right-280,y,175,28),"NEXT SURVIVOR",()=>{var crew=w.Party.ToArray();socialActor=crew[(Array.IndexOf(crew,actor)+1)%crew.Length].Id;},false,.4f);g.Button(new(bounds.Right-90,y,65,28),"CLOSE",()=>Close(),false,.4f);y+=44;
   string[] tabs=["FACTIONS","SURVIVORS","CONVERSATIONS"];for(int i=0;i<tabs.Length;i++){int t=i;g.Button(new(x+i*200,y,190,28),tabs[i],()=>{socialTab=t;socialPage=0;socialSelected="";},socialTab==t,.42f);}g.Button(new(bounds.Right-210,y,185,28),"CONTRACTS",()=>ShowContracts(),false,.42f);y+=44;
   g.Text($"{actor.Name} | {World.JournalTimeText(w.CampaignMinutes)} | MARA TRADE MARKUP ADJUSTMENT: {w.SocialTradeMarkup:+0;-0;0} POINTS",x,y,Teal,.41f);y+=30;int detailY=y,footer=bounds.Bottom-68;
   string[] ids=socialTab==0?d.Factions.Select(f=>f.Id).ToArray():socialTab==1?d.Survivors.Select(s=>s.ActorId).ToArray():d.Decisions.Select(q=>q.Id).ToArray();
   int pages=Math.Max(1,(ids.Length+7)/8);socialPage=Math.Clamp(socialPage,0,pages-1);if(!ids.Contains(socialSelected))socialSelected=ids.Skip(socialPage*8).FirstOrDefault()??"";
   foreach(string id in ids.Skip(socialPage*8).Take(8))
   {
    string selected=id;string name=socialTab==0?d.Factions.Single(f=>f.Id==id).Name:socialTab==1?w.Campaign.Actors.Single(a=>a.Id==id).Name:d.Decisions.Single(q=>q.Id==id).Title;
    string status=socialTab==0?(w.FactionContacted(id)?$"{w.FactionStanding(id):+0;-0;0}":d.Factions.Single(f=>f.Id==id).LocalContact?"CONTACT PENDING":"BACKGROUND"):socialTab==1?$"TRUST {w.SurvivorTrust(id):+0;-0;0}":w.SocialHistory.Any(r=>r.Id==id)?"RESOLVED":w.SocialDecisionGate(id)==""?"AVAILABLE":"LOCKED";
    g.Button(new(x,y,left,30),name+" | "+status,()=>socialSelected=selected,socialSelected==id,.39f);y+=36;
   }
   int yy=detailY;
   void Para(string text,Color color,int lines=5){foreach(var line in g.Wrap(text,detailWidth-12,.42f).Take(lines)){g.Text(line,right,yy,color,.42f);yy+=20;}yy+=10;}
   if(socialSelected!="")
   {
    if(socialTab==0)
    {
     var f=d.Factions.Single(f=>f.Id==socialSelected);Para(f.Name.ToUpperInvariant(),Gold,2);Para(f.Description,Muted,7);Para($"SOURCE: pp. {f.Source.PageStart}-{f.Source.PageEnd}. "+(f.LocalContact?"Authored local contact through refuge conversations.":"Background example only; no local outpost or actors are spawned."),Muted,4);
     Para(w.FactionContacted(f.Id)?$"STANDING {w.FactionStanding(f.Id):+0;-0;0} | {World.SocialLabel(w.FactionStanding(f.Id))}":"No relationship established.",Teal,2);
     if(f.Id==d.TraderFactionId)Para("Mara relays Guild trade terms. Contact plus standing at least +5 reduces asking markup by 5 percentage points; -5 or below adds 5. Finite stock and trade XP limits still apply.",Muted,4);
    }
    else if(socialTab==1)
    {
     var p=d.Survivors.Single(s=>s.ActorId==socialSelected);var member=w.CommunityMembers.FirstOrDefault(a=>a.Id==p.ActorId);Para(w.Campaign.Actors.Single(a=>a.Id==p.ActorId).Name.ToUpperInvariant(),Gold,2);Para(p.Values,Muted,4);Para(member==null?"Not recruited.":member.Dead?"Deceased; relationship history retained.":w.ShelterResidents.Any(r=>r.Actor.Id==p.ActorId)?"Refuge resident; relationships retained.":"Active expedition survivor.",Muted,2);Para($"TRUST {w.SurvivorTrust(p.ActorId):+0;-0;0} | {World.SocialLabel(w.SurvivorTrust(p.ActorId))}",Teal,2);
     foreach(var other in d.Survivors.Where(s=>s.ActorId!=p.ActorId)){int n=w.SurvivorBond(p.ActorId,other.ActorId);Para($"With {w.Campaign.Actors.Single(a=>a.Id==other.ActorId).Name}: bond {n:+0;-0;0} | {World.SocialLabel(n)}",Muted,2);}Para("Trust unlocks personal follow-ups at +3. Bonds record cooperation and disagreements; they do not add combat bonuses or force departures.",Muted,4);
    }
    else
    {
     var q=d.Decisions.Single(q=>q.Id==socialSelected);var record=w.SocialHistory.FirstOrDefault(r=>r.Id==q.Id);Para(q.Title.ToUpperInvariant(),Gold,2);Para(q.Text,Muted,4);
     if(record!=null){var choice=q.Choices.Single(c=>c.Id==record.ChoiceId);Para("RESOLVED: "+choice.Text,Teal,3);Para(choice.Response,Muted,5);Para(w.DescribeSocialChanges(choice.Changes),Teal,3);}
     else
     {
      string reason=w.SocialDecisionReason(actor,q.Id);foreach(var choice in q.Choices){var c=choice;g.CommandButton(new(right,yy,detailWidth,37),c.Text,()=>w.ResolveSocialDecision(actor,q.Id,c.Id),reason=="",reason==""?"Commits once; five campaign minutes. Consequences are shown below.":reason,false,.4f);yy+=43;Para(c.Changes.Count==0?"No score changes.":w.DescribeSocialChanges(c.Changes),Teal,2);}if(reason!="")Para(reason,Gold,3);
     }
    }
   }
   g.Button(new(x,footer,95,27),"PREVIOUS",()=>{socialPage=Math.Max(0,socialPage-1);socialSelected="";},false,.35f);g.Text($"PAGE {socialPage+1}/{pages}",x+110,footer+7,Muted,.4f);g.Button(new(x+left-95,footer,95,27),"NEXT",()=>{socialPage=Math.Min(pages-1,socialPage+1);socialSelected="";},false,.35f);
   g.Paragraph("Browse freely. Conversations require the safe refuge board and affected living survivors at home. Choices are permanent, take five campaign minutes and grant no items or XP. Journal records each response.",x,bounds.Bottom-32,width,Muted,.36f,17);
  }
 }
 void StageSocialCapture()
 {
  title=false;if(scene=="social-title"){title=true;return;}
  if(scene!="social-playthrough"){foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;world.SetDialogueFlag("flag.mall.shelter");int i=0;foreach(var a in world.Party)a.Pos=new(12+i++,28);world.Reveal();}
  camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();ui.Rest.ShowSocial(scene=="social-survivors"?1:scene=="social-conversations"?2:0);if(scene=="social-conversations")ui.Rest.PreviewSocial("social.sam.watch");
 }
}
