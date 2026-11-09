using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 sealed partial class RestPanel
 {
  bool contractsView,encounterView;
  string contractActor="",contractSelected="";
  int contractPage,goalPage;
  public void ShowContracts(Actor? actor=null,bool encounters=false)
  {if(!g.world.StoryEnabled||g.world.ReactionInterruptActive||g.ui.Dialogue.IsOpen)return;PrepareOpen();open=true;contractsView=true;workshopView=equipmentView=classView=tensionView=worksView=shelterView=false;contractActor=(actor??g.PrimaryPartyActor).Id;encounterView=encounters;}
  public void PreviewContract(string id){contractSelected=id;goalPage=0;}
  void DrawContracts()
  {
   var w=g.world;var d=w.Campaign!.Story!;var a=w.Party.FirstOrDefault(a=>a.Id==contractActor)??w.PartyLeader;g.Panel(bounds);g.buttons.Add((bounds,()=>{}));int x=bounds.X+20,y=bounds.Y+18,width=bounds.Width-40,left=(width-25)/2,right=x+left+25;
   g.Text("REFUGE CONTRACTS & ENCOUNTERS",x,y,Gold,.52f,true);g.Button(new(bounds.Right-280,y,175,28),"NEXT SURVIVOR",()=>{var crew=w.Party.ToArray();contractActor=crew[(Array.IndexOf(crew,a)+1)%crew.Length].Id;},false,.4f);g.Button(new(bounds.Right-90,y,65,28),"CLOSE",()=>Close(),false,.4f);y+=43;
   g.Button(new(x,y,190,28),"CONTRACTS",()=>{encounterView=false;contractSelected="";contractPage=0;},!encounterView,.43f);g.Button(new(x+200,y,190,28),"ENCOUNTER PLANS",()=>{encounterView=true;contractSelected="";contractPage=0;},encounterView,.43f);if(w.SocialEnabled)g.Button(new(x+400,y,190,28),"FACTIONS / RELATIONS",()=>ShowSocial(),false,.4f);y+=42;
   g.Text($"{a.Name} | {World.JournalTimeText(w.CampaignMinutes)} | CONTRACT XP {w.ContractXpAwarded}, POOL {w.ContractXpRemainder}",x,y,Teal,.43f);y+=30;
   int detailY=y,footer=bounds.Bottom-77;
   if(encounterView)
   {
    var plans=d.EncounterPlans.ToArray();var chosen=plans.FirstOrDefault(p=>p.Id==contractSelected)??plans.FirstOrDefault();contractSelected=chosen?.Id??"";int pages=Math.Max(1,(plans.Length+7)/8);contractPage=Math.Clamp(contractPage,0,pages-1);
    foreach(var plan in plans.Skip(contractPage*8).Take(8)){var p=plan;g.Button(new(x,y,left,30),$"{p.Name} | {w.EncounterPlanStatus(p.Id).ToUpperInvariant()}",()=>contractSelected=p.Id,chosen==p,.4f);y+=36;}
    if(chosen!=null){var state=w.EncounterPlanStates.FirstOrDefault(p=>p.Id==chosen.Id);g.Text(chosen.Name.ToUpperInvariant(),right,detailY,Gold,.46f,true);int yy=detailY+35;g.Text($"WAVES RELEASED: {state?.NextWave??0}/{chosen.Waves.Count} | {w.EncounterPlanStatus(chosen.Id).ToUpperInvariant()}",right,yy,Teal,.43f);yy+=28;
     g.Paragraph($"Area: {w.Campaign.Areas.Single(ar=>ar.Id==chosen.AreaId).Name}. Trigger: {w.Campaign.Areas.SelectMany(ar=>ar.Features).Single(f=>f.Id==chosen.TriggerFeatureId).Name}. Required contract: {d.Contracts.FirstOrDefault(q=>q.Id==chosen.RequiredContractId)?.Title??"none"}. Each wave has reserved unique actors. Blocked arrivals wait; travel and loading do not replay waves.",right,yy,width-left-40,Muted,.43f,22);
     string reason=w.EncounterPlanReason(chosen.Id);g.CommandButton(new(right,footer,width-left-25,31),"ACTIVATE NEARBY TRIGGER",()=>{if(w.StartEncounterPlan(chosen.Id))Close();},reason=="",reason==""?"Activates this finite plan at the nearby physical trigger.":reason,false,.42f);
    }
    g.Button(new(x,footer,95,27),"PREVIOUS",()=>contractPage=Math.Max(0,contractPage-1),false,.35f);g.Text($"PAGE {contractPage+1}/{pages}",x+110,footer+7,Muted,.4f);g.Button(new(x+left-95,footer,95,27),"NEXT",()=>contractPage=Math.Min(pages-1,contractPage+1),false,.35f);
   }
   else
   {
    var quests=d.Contracts.ToArray();var chosen=quests.FirstOrDefault(q=>q.Id==contractSelected)??quests.FirstOrDefault();contractSelected=chosen?.Id??"";int pages=Math.Max(1,(quests.Length+7)/8);contractPage=Math.Clamp(contractPage,0,pages-1);
    foreach(var quest in quests.Skip(contractPage*8).Take(8)){var q=quest;g.Button(new(x,y,left,30),$"{q.Title} | {w.ContractStatus(q.Id).ToUpperInvariant()}",()=>{contractSelected=q.Id;goalPage=0;},chosen==q,.4f);y+=36;}
    if(chosen!=null)
    {
     var state=w.ContractStates.FirstOrDefault(s=>s.Id==chosen.Id);int yy=detailY;g.Text(chosen.Title.ToUpperInvariant(),right,yy,Gold,.46f,true);yy+=30;g.Text(w.ContractStatus(chosen.Id).ToUpperInvariant()+$" | REWARD {chosen.RewardXp} CONTRACT XP",right,yy,Teal,.43f);yy+=28;
     foreach(var line in g.Wrap(chosen.Description,width-left-40,.42f).Take(4)){g.Text(line,right,yy,Muted,.42f);yy+=20;}
     if(chosen.DeadlineMinutes>0){g.Text(state==null?$"DEADLINE: {chosen.DeadlineMinutes} MINUTES AFTER ACCEPTANCE":$"DUE: {World.JournalTimeText(state.AcceptedMinute+chosen.DeadlineMinutes)}",right,yy,Gold,.4f);yy+=25;}
     if(chosen.BranchGroup!=""){g.Text("MUTUALLY EXCLUSIVE CHOICE: "+chosen.BranchGroup,right,yy,Gold,.39f);yy+=25;}
     int goalPages=Math.Max(1,(chosen.Goals.Count+2)/3);goalPage=Math.Clamp(goalPage,0,goalPages-1);g.Button(new(right,yy,width-left-25,25),$"OBJECTIVES {goalPage+1}/{goalPages}",()=>goalPage=(goalPage+1)%goalPages,false,.37f);yy+=31;
     foreach(var goal in chosen.Goals.Skip(goalPage*3).Take(3)){int n=w.ContractGoalProgress(chosen,state,goal);foreach(var line in g.Wrap($"{n}/{goal.Quantity}  {goal.Text}",width-left-40,.42f).Take(2)){g.Text(line,right,yy,n>=goal.Quantity?Teal:Muted,.42f);yy+=20;}}
     yy+=10;foreach(var line in g.Wrap("PHYSICAL REWARDS: "+(chosen.Rewards.Count==0?"none":string.Join(", ",chosen.Rewards.Select(s=>$"{s.Quantity} {w.ResolveItemDefinition(s.ItemId).Name}"))),width-left-40,.4f).Take(2)){g.Text(line,right,yy,Muted,.4f);yy+=19;}
     bool accept=state==null;string reason=accept?w.AcceptContractReason(a,chosen.Id):w.ClaimContractReason(a,chosen.Id);g.CommandButton(new(right,footer,width-left-25,31),accept?"ACCEPT THIS CONTRACT":"REPORT / CLAIM ONCE",()=>{if(accept)w.AcceptContract(a,chosen.Id);else w.ClaimContract(a,chosen.Id);},reason=="",reason==""?"Confirms the selected contract action. Delivery consumes the stated goods; rewards enter the refuge stockpile.":reason,false,.42f);
    }
    g.Button(new(x,footer,95,27),"PREVIOUS",()=>contractPage=Math.Max(0,contractPage-1),false,.35f);g.Text($"PAGE {contractPage+1}/{pages}",x+110,footer+7,Muted,.4f);g.Button(new(x+left-95,footer,95,27),"NEXT",()=>contractPage=Math.Min(pages-1,contractPage+1),false,.35f);
   }
   g.Paragraph("Accept and report beside the safe refuge board. Delivery requires intact, identified, unloaded goods with full charges in this backpack or stockpile. J opens the journal. Browsing grants nothing.",x,bounds.Bottom-38,width,Muted,.36f,17);
  }
 }
 void StageContractCapture()
 {
  title=false;if(scene=="quest-title"){title=true;return;}
  if(scene!="quest-playthrough"){foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;world.SetDialogueFlag("flag.mall.shelter");int i=0;foreach(var a in world.Party)a.Pos=new(12+i++,28);world.Reveal();}
  camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();ui.Rest.ShowContracts(encounters:scene=="quest-plans");
 }
}
