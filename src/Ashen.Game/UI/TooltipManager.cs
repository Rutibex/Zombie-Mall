using Microsoft.Xna.Framework;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 sealed class TooltipManager
 {
  readonly CovenantGame g;
  string text="",key="",stableKey="";
  float stableSince;
  string lastVisible="";float lastVisibleAt;
  public TooltipManager(CovenantGame game)=>g=game;
  public string Text {get=>text;set=>Offer(value,value);}
  public int DelayMs=>Math.Clamp(g.preferences.TooltipDelayMs,0,1500);

  public void BeginFrame(){text="";key="";}
  public void Offer(string id,string value)
  {
   if(string.IsNullOrWhiteSpace(value))return;text=value;key=string.IsNullOrWhiteSpace(id)?value:id;
  }
  public void Draw()
  {
   if(g.title||g.ui.Modals.IsOpen||g.boxDragging){stableKey="";lastVisible="";return;}
   if(text=="")
   {
    stableKey="";
    if(!g.preferences.PersistentTooltips||lastVisible==""||g.elapsed-lastVisibleAt>4)return;
    text=lastVisible;
   }
   if(key!=""&&stableKey!=key){stableKey=key;stableSince=g.elapsed;return;}
   bool force=g.BindingHeld(InputAction.HighlightInteractables); // historical IIP18 marker: Keys.Tab
   if(key!=""&&!force&&(g.elapsed-stableSince)*1000f<DelayMs)return;
   if(key!=""){lastVisible=text;lastVisibleAt=g.elapsed;}
   int width=Math.Min(590,(int)g.Measure(text,.72f)+24);var lines=g.Wrap(text,width-24,.72f).ToArray();int height=14+lines.Length*27;
   var r=new Rectangle(Math.Clamp(g.cursor.X+16,18,g.W-width-18),Math.Clamp(g.cursor.Y-height-10,85,CovenantGame.H-height-18),width,height);
   float alpha=g.preferences.ReducedMotion||key==""?1:Math.Clamp((g.elapsed-stableSince-DelayMs/1000f)*8,0,1);
   g.Box(r,new Color(13,18,22)*alpha,Gold*alpha);if(alpha>=.95f)g.CovenantFrame(r,false);
   for(int i=0;i<lines.Length;i++)g.Text(lines[i],r.X+12,r.Y+6+i*27,Bright*alpha,.72f);
  }
 }
}
