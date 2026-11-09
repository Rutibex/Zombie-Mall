using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 public enum MessageVerbosity { Concise=0, Detailed=1 }
 public enum MessageLogHeight { Collapsed=0, Normal=1, Expanded=2 }

 sealed class MessageLog
 {
  readonly CovenantGame g;
  MessageLogHeight height=MessageLogHeight.Normal;
  int scrollOffset;
  int chronicleOffset;
  int lastPersistentLineCount;
  float animatedThumbY=float.NaN,lastThumbDrawAt;
  Rectangle currentBounds;
  public MessageLog(CovenantGame game)=>g=game;
  public MessageLogHeight HeightMode=>height;
  public bool Expanded=>height==MessageLogHeight.Expanded;
  public bool Contains(Point p)=>!g.title&&!g.ui.Screens.IsOpen&&currentBounds.Contains(p);
  bool Detailed=>g.preferences.LogVerbosity==MessageVerbosity.Detailed;

  readonly record struct RenderedLine(MessageEntry Entry,string Text,bool First);

  public void RestoreHeight(int raw)=>height=(MessageLogHeight)Math.Clamp(raw,0,2);
  public void CycleHeight()=>SetHeight((MessageLogHeight)(((int)height+1)%3));
  void SetHeight(MessageLogHeight value)
  {
   height=value;scrollOffset=0;g.preferences.MessageLogHeight=(int)value;g.SavePreferences();
  }
  Rectangle Bounds(Rectangle normal)
  {
   int collapsed=Math.Max(50,(int)Math.Round(58*g.ui.Layout.Scale));
   if(height==MessageLogHeight.Collapsed)return new(normal.X,normal.Y,normal.Width,collapsed);
   if(height==MessageLogHeight.Normal)return normal;
   int expanded=Math.Min((int)Math.Round(310*g.ui.Layout.Scale),normal.Height+Math.Max(120,g.ui.Layout.WorldViewport.Height/2));
   expanded=Math.Max(normal.Height,expanded);
   return new(normal.X,normal.Bottom-expanded,normal.Width,expanded);
  }
  Color CategoryColor(MessageCategory category)=>category switch
  {
   MessageCategory.Combat=>Gold,
   MessageCategory.Roll=>Bright,
   MessageCategory.Damage=>Red,
   MessageCategory.Healing=>Teal,
   MessageCategory.Condition=>new Color(205,175,112),
   MessageCategory.Reaction=>new Color(140,198,220),
   MessageCategory.Quest=>Bright,
   MessageCategory.Loot=>Gold,
   MessageCategory.Rest=>Teal,
   MessageCategory.Travel=>Muted,
   MessageCategory.System=>Muted,
   MessageCategory.Error=>Red,
   _=>Muted
  };
  List<RenderedLine> Flatten(int width,float scale)
  {
   var result=new List<RenderedLine>();
   foreach(var entry in g.world.MessageHistory)
   {
    string prefix=$"[{entry.CategoryLabel}] ";
    string text=entry.DisplayText(Detailed);
    var wrapped=g.Wrap(prefix+text,width,scale).ToArray();
    for(int i=0;i<wrapped.Length;i++)result.Add(new(entry,wrapped[i],i==0));
   }
   return result;
  }
  void DrawHeader(Rectangle r)
  {
   int header=Math.Max(30,(int)Math.Round(31*g.ui.Layout.Scale));
   string mode=g.world.Mode==Mode.Combat?$"ROUND {g.world.Round}":"EXPLORATION";
   g.Text("CHRONICLE",r.X+10,r.Y+6,Gold,.55f);
   var titleHit=new Rectangle(r.X+7,r.Y+3,112,header-5);g.buttons.Add((titleHit,()=>{chronicleOffset=scrollOffset;g.ui.Screens.NavigateRoot(UiScreenId.Chronicle);}));
   if(titleHit.Contains(g.cursor))g.tooltip="Open the full Chronicle history.";
   string verbosity=Detailed?"DETAIL":"CONCISE";
   string status=$"{mode}  {verbosity}  GOLD {g.world.Gold}  SUP {g.world.Rations}  POT {g.world.TotalItemCount("healing-potion")}";
   float statusScale=.47f;while(g.Measure(status,statusScale)>r.Width-315&&statusScale>.35f)statusScale-=.02f;
   g.Text(status,r.X+125,r.Y+7,g.world.Mode==Mode.Combat?Bright:Teal,statusScale);
   int bw=Math.Max(30,(int)Math.Round(31*g.ui.Layout.Scale)),gap=4,x=r.Right-3*(bw+gap)-8;
   void SizeButton(MessageLogHeight mode,string label,string tip)
   {
    var b=new Rectangle(x,r.Y+3,bw,header-6);g.Button(b,label,()=>SetHeight(mode),height==mode,.48f);if(b.Contains(g.cursor))g.tooltip=tip;x+=bw+gap;
   }
   SizeButton(MessageLogHeight.Collapsed,"-","Collapse the message window to a compact strip.");
   SizeButton(MessageLogHeight.Normal,"=","Use the normal message-window height.");
   SizeButton(MessageLogHeight.Expanded,"+","Expand the message window upward for more scrollback.");
  }
  public void Draw(Rectangle normal)
  {
   currentBounds=Bounds(normal);var r=currentBounds;
   g.Box(r,new Color(13,18,21)*.98f,Gold*.55f);g.buttons.Add((r,()=>{}));DrawHeader(r);
   int header=Math.Max(30,(int)Math.Round(31*g.ui.Layout.Scale));
   if(height==MessageLogHeight.Collapsed)
   {
    var latest=g.world.MessageHistory.LastOrDefault();if(latest!=null)
    {
     string one=$"[{latest.CategoryLabel}] {latest.DisplayText(Detailed)}";while(g.Measure(one,.50f)>r.Width-220&&one.Length>10)one=one[..^2]+".";
     g.Text(one,r.X+10,r.Y+33,CategoryColor(latest.Category),.50f);
    }
    return;
   }
   var content=new Rectangle(r.X+9,r.Y+header+2,r.Width-31,r.Height-header-8);int lineH=Math.Max(15,(int)Math.Round(17*g.preferences.TextScale));int capacity=Math.Max(1,content.Height/lineH);
   var lines=Flatten(content.Width-8,.52f);
   if(scrollOffset>0&&lastPersistentLineCount>0&&lines.Count>lastPersistentLineCount)scrollOffset+=lines.Count-lastPersistentLineCount;
   lastPersistentLineCount=lines.Count;int maxOffset=Math.Max(0,lines.Count-capacity);scrollOffset=Math.Clamp(scrollOffset,0,maxOffset);
   int start=Math.Max(0,lines.Count-capacity-scrollOffset);var visible=lines.Skip(start).Take(capacity).ToArray();int y=content.Y;
   foreach(var line in visible){g.Text(line.Text,content.X,y,CategoryColor(line.Entry.Category),.52f);y+=lineH;}
   DrawScrollbar(new(r.Right-17,content.Y,8,content.Height),lines.Count,capacity,scrollOffset);
   g.buttons.Add((content,()=>{chronicleOffset=scrollOffset;g.ui.Screens.NavigateRoot(UiScreenId.Chronicle);}));
   if(content.Contains(g.cursor))g.tooltip="Mouse wheel scrolls history. Click to open the full Chronicle.";
  }
  void DrawScrollbar(Rectangle track,int total,int capacity,int offset)
  {
   g.Fill(track,new Color(8,11,13));g.Border(track,Gold*.28f);
   if(total<=capacity){g.Fill(new(track.X+2,track.Y+2,track.Width-4,track.Height-4),Gold*.25f);return;}
   int thumbH=Math.Max(14,(int)(track.Height*(capacity/(float)total)));int range=Math.Max(1,track.Height-thumbH-4);float olderFraction=offset/(float)Math.Max(1,total-capacity);
   float targetY=track.Y+2+range*(1-olderFraction);
   float step=Math.Clamp((g.elapsed-lastThumbDrawAt)*14,0,1);lastThumbDrawAt=g.elapsed;
   animatedThumbY=g.preferences.ReducedMotion||float.IsNaN(animatedThumbY)||Math.Abs(animatedThumbY-targetY)>track.Height?targetY:MathHelper.Lerp(animatedThumbY,targetY,step);
   int thumbY=(int)animatedThumbY;
   g.Fill(new(track.X+2,thumbY,track.Width-4,thumbH),Gold*.75f);
   g.Fill(new(track.X+3,thumbY+3,Math.Max(1,track.Width-6),Math.Max(1,thumbH-6)),new Color(73,49,27));
  }
  public bool HandleWheel(int delta)
  {
   if(delta==0||g.title)return false;
   int steps=delta/120;if(steps==0)steps=Math.Sign(delta);
   if(g.ui.Screens.Active==UiScreenId.Chronicle){chronicleOffset=Math.Max(0,chronicleOffset+steps*4);return true;}
   if(g.ui.Screens.IsOpen||!currentBounds.Contains(g.cursor))return false;
   scrollOffset=Math.Max(0,scrollOffset+steps*4);return true;
  }
  public void DrawChronicle(Rectangle r)
  {
   int x=r.X+35,y=r.Y+88,width=r.Width-70;float scale=.72f;int lineH=25;
   g.Text(Detailed?"DETAILED RULE FEEDBACK":"CONCISE FEEDBACK",x,y-35,Detailed?Bright:Teal,.66f);
   var lines=Flatten(width-18,scale);int capacity=Math.Max(1,(r.Bottom-y-78)/lineH);int maxOffset=Math.Max(0,lines.Count-capacity);chronicleOffset=Math.Clamp(chronicleOffset,0,maxOffset);
   int start=Math.Max(0,lines.Count-capacity-chronicleOffset);foreach(var line in lines.Skip(start).Take(capacity)){g.Text(line.Text,x,y,CategoryColor(line.Entry.Category),scale);y+=lineH;}
   int by=r.Bottom-61;g.Button(new(x,by,150,38),"Older",()=>chronicleOffset=Math.Min(maxOffset,chronicleOffset+12));g.Button(new(x+164,by,150,38),"Newer",()=>chronicleOffset=Math.Max(0,chronicleOffset-12));g.Button(new(x+328,by,150,38),"Latest",()=>chronicleOffset=0);
   g.Button(new(x+510,by,225,38),Detailed?"Feedback: Detailed":"Feedback: Concise",()=>{g.preferences.LogVerbosity=Detailed?MessageVerbosity.Concise:MessageVerbosity.Detailed;g.SavePreferences();chronicleOffset=0;},false,.63f);
   g.Text("Categories are always named in brackets; color is supplemental only.",x+752,by+8,Muted,.52f);
  }
 }
}
