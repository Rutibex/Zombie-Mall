using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 sealed partial class RestPanel
 {
  bool finaleView;
  int finaleTab,finalePage;
  string finaleSelected="",finalePending="";
  public void ShowFinale(int tab=0){if(!g.world.FinaleEnabled||g.world.ReactionInterruptActive||g.ui.Dialogue.IsOpen)return;PrepareOpen();open=true;finaleView=true;finaleTab=tab;finalePage=0;finalePending="";}
  void DrawFinale()
  {
   var w=g.world;var a=g.PrimaryPartyActor;g.Panel(bounds);g.buttons.Add((bounds,()=>{}));int x=bounds.X+20,y=bounds.Y+16,width=bounds.Width-40,left=width/2-15,right=x+left+25;
   g.Text("MYSTERIES & FINAL ESCAPE",x,y,Gold,.53f,true);g.Button(new(bounds.Right-270,y,170,28),"NEXT SURVIVOR",()=>{var crew=w.Party.ToArray();g.SelectCharacter((Array.IndexOf(crew,a)+1)%crew.Length);},false,.39f);g.Button(new(bounds.Right-85,y,65,28),"CLOSE",()=>Close(),false,.39f);y+=42;
   string[] tabs=["EVIDENCE","HELICOPTER","WINTER WATCH","TERMINAL CRISIS"];for(int i=0;i<tabs.Length;i++){int tab=i;g.Button(new(x+i*(width/4),y,width/4-8,29),tabs[i],()=>{finaleTab=tab;finalePage=0;finaleSelected=finalePending="";},tab==finaleTab,.39f);}y+=42;
   g.Text(w.FinaleStatus,x,y,Teal,.39f);y+=29;int top=y;
   if(finaleTab==0)
   {
    var all=w.DiscoveredEvidence;int pages=Math.Max(1,(all.Count+5)/6);finalePage=Math.Clamp(finalePage,0,pages-1);var chosen=all.FirstOrDefault(e=>e.Id==finaleSelected)??all.Skip(finalePage*6).FirstOrDefault();
    foreach(var e in all.Skip(finalePage*6).Take(6)){var entry=e;g.Button(new(x,y,left,31),$"{e.Thread.ToUpperInvariant()} | {e.Title}",()=>finaleSelected=entry.Id,chosen==e,.35f);y+=37;}
    g.Text($"{all.Count} RECOVERED CLUES | THEORY: {w.Finale.Theory.ToUpperInvariant()}",right,top,Gold,.4f);int yy=top+30;
    if(chosen!=null){g.Paragraph(chosen.Text,right,yy,width-left-40,Muted,.43f,20);yy=top+175;}else{g.Paragraph("Investigate the source clues in the mall, roof and exterior. Each new location keeps one saved variant. Catalogue browsing and unvisited source text do not become recovered evidence.",right,yy,width-left-40,Muted,.43f,22);yy=top+175;}
    g.Paragraph("A working theory records the expedition's belief. Contradictory evidence remains visible. It does not certify one true cause or grant XP.",right,yy,width-left-40,Muted,.4f,20);yy+=75;
    string[] theories=["undetermined","biodyne","cult","military","anomaly"];for(int i=0;i<theories.Length;i++){string t=theories[i];string why=w.SetMysteryTheoryReason(t);g.CommandButton(new(right+(i%3)*(width-left-35)/3,yy+(i/3)*30,(width-left-45)/3-5,26),t.ToUpperInvariant(),()=>w.SetMysteryTheory(t),why=="",why,false,.32f);}
    g.Button(new(x,bounds.Bottom-55,95,27),"PREVIOUS",()=>finalePage=Math.Max(0,finalePage-1),false,.36f);g.Text($"PAGE {finalePage+1}/{pages}",x+110,bounds.Bottom-47,Muted,.38f);g.Button(new(x+left-95,bounds.Bottom-55,95,27),"NEXT",()=>finalePage=Math.Min(pages-1,finalePage+1),false,.36f);
   }
   else
   {
    (string Id,string Label)[] actions=finaleTab==1?[("readiness","Base readiness (at refuge)"),("heli-inspect","Inspect SkyEye 7: DC 12"),("heli-repair-rotor","Repair rotor: DC 14 + material"),("heli-repair-pump","Repair fuel pump: DC 14 + material"),("heli-repair-electrics","Repair electrics: DC 14 + material"),("heli-salvage","Resolve catastrophic salvage: DC 18"),("heli-wait","Spend one real hour on repair delay"),("heli-refuel","Transfer one physical fuel unit"),("heli-pilot","Train Smart pilot: two hours"),("heli-airworthy","Final airworthiness: DC 15"),("heli-launch","DEPART FOR THE ISLAND")]:finaleTab==2?[("readiness","Base readiness"),("winter-start","Begin the 150-day watch"),("winter-day","Survive one day (pauses for trouble)"),("freeze-final","Attempt the Day 150 crossing"),("freeze-emergency","Risk an emergency crossing"),("freeze-wait","Choose another seven days")]:[("crisis-nuclear","Accept nuclear warning: 2d6 hours"),("crisis-ragnarok","Accept Ragnarok warning: 1d12 hours"),("crisis-escape","Leave the exclusion zone"),("cosmic-core","ACTIVATE OWNED COSMIC CORE")];
    int pages=Math.Max(1,(actions.Length+5)/6);finalePage=Math.Clamp(finalePage,0,pages-1);var selected=actions.FirstOrDefault(p=>p.Id==finaleSelected);if(selected.Id==null)selected=actions[finalePage*6];finaleSelected=selected.Id;
    foreach(var p in actions.Skip(finalePage*6).Take(6)){var pair=p;g.Button(new(x,y,left,32),p.Label,()=>{finaleSelected=pair.Id;finalePending="";},p.Id==selected.Id,.38f);y+=39;}
    string summary=finaleTab==1?w.HelicopterStatus+". Aircraft actions require the secured roof #11 console and recovered aviation tools. Six seats include the pilot; 600 kg total payload. Two or three abstract fuel units power a 45-minute crossing. Refuge residents not on the expedition stay behind.":finaleTab==2?$"{w.Finale.Weeks.Count} saved weekly records. Supply DC 12 (+2 above Tension 70), upkeep DC 13. Real food/water, repairs and daily attacks still apply. No advance skips to Day 150. The final roll is DC 15; a failed check opens emergency DC 18 or another week.":w.Finale.CrisisDeadline>=0?$"ACTIVE {w.Finale.Crisis.ToUpperInvariant()} DEADLINE: {World.JournalTimeText(w.Finale.CrisisDeadline)}. The deadline is saved and cannot be reset. Use a genuinely established escape route before it expires.":"Optional terminal scenarios require extreme Tension and recovered evidence at the communications tower. Accepting a warning starts a real countdown. Cosmic erasure requires owning the actual core; the catalogue grants nothing.";
    g.Paragraph(summary,right,top,width-left-40,Muted,.42f,21);int yy=top+170;g.Text(selected.Label.ToUpperInvariant(),right,yy,Gold,.41f);yy+=33;string reason=w.FinaleActionReason(a,selected.Id);g.Paragraph(reason==""?"Requirements met. Checks, physical costs, time and failed outcomes apply. No preview grants progress.":reason,right,yy,width-left-40,reason==""?Teal:Muted,.42f,21);
    bool final=selected.Id is "heli-launch" or "freeze-final" or "freeze-emergency" or "crisis-escape" or "cosmic-core" or "crisis-nuclear" or "crisis-ragnarok";g.CommandButton(new(right,bounds.Bottom-94,width-left-25,32),final?"REVIEW CONSEQUENCES":"PERFORM THIS ACTION",()=>{if(final)finalePending=selected.Id;else w.PerformFinaleAction(a,selected.Id);},reason=="",reason,false,.39f);
    if(finalePending==selected.Id){g.Paragraph("CONFIRM: this can end the campaign, start an irreversible deadline or risk a failed escape. Save beforehand. Heli evacuees: expedition only; winter: surviving community.",x,bounds.Bottom-129,left,Gold,.35f,16);g.CommandButton(new(right,bounds.Bottom-54,width-left-25,30),"CONFIRM CHOSEN CONSEQUENCES",()=>{w.PerformFinaleAction(a,selected.Id);finalePending="";if(w.CampaignEnded)Close();},reason=="",reason,false,.37f);}
    g.Button(new(x,bounds.Bottom-55,95,27),"PREVIOUS",()=>finalePage=Math.Max(0,finalePage-1),false,.36f);g.Text($"PAGE {finalePage+1}/{pages}",x+110,bounds.Bottom-47,Muted,.38f);g.Button(new(x+left-95,bounds.Bottom-55,95,27),"NEXT",()=>finalePage=Math.Min(pages-1,finalePage+1),false,.36f);
   }
  }
 }
 void DrawCampaignFinaleEnding()
 {
  var end=world.Finale.Ending!;Shade();var r=new Rectangle(W/2-485,H/2-290,970,580);Panel(r);Text(end.Title.ToUpperInvariant(),r.X+30,r.Y+25,world.Mode==Mode.Victory?Gold:Red,.67f,true);Paragraph(end.Text,r.X+30,r.Y+87,r.Width-60,Muted,.58f,28);
  int y=r.Y+235;string Names(IEnumerable<string> ids)=>string.Join(", ",ids.Select(id=>world.Campaign!.Actors.FirstOrDefault(a=>a.Id==id)?.Name??id));
  Paragraph("EVACUATED: "+(end.Evacuated.Count>0?Names(end.Evacuated):"none"),r.X+30,y,r.Width-60,Teal,.45f,23);y+=58;Paragraph("LEFT AT THE MALL: "+(end.LeftBehind.Count>0?Names(end.LeftBehind):"none"),r.X+30,y,r.Width-60,Muted,.45f,23);y+=58;Text($"{end.SecuredLocations} SECURED LOCATIONS | {end.EvidenceIds.Count} CLUES | {end.Dead.Count} PRIOR DEATHS | THEORY: {end.Theory.ToUpperInvariant()}",r.X+30,y,Gold,.41f);y+=32;
  Paragraph("Your interpretation remains a belief, not a revealed canonical cause. Save this final record to keep it; this completed world cannot continue spending supplies or collect another ending.",r.X+30,y,r.Width-60,Muted,.44f,22);
  Text("FACTION STANDING: "+string.Join(" | ",end.Factions.Where(f=>f.Value!=0).OrderByDescending(f=>Math.Abs(f.Value)).Take(3).Select(f=>(world.Campaign!.Social!.Factions.First(d=>d.Id==f.Key).Name+": "+World.SocialLabel(f.Value))).DefaultIfEmpty("No reputation changes recorded")),r.X+30,r.Bottom-110,Teal,.36f);
  Button(new(r.X+30,r.Bottom-70,280,43),"Save completed campaign",()=>OpenSaveBrowser(true),false,.55f);Button(new(r.X+325,r.Bottom-70,280,43),"Load earlier journey",()=>OpenSaveBrowser(false),false,.55f);Button(new(r.X+620,r.Bottom-70,320,43),"Review recovered evidence",()=>ui.Rest.ShowFinale(),false,.53f);
 }
}
