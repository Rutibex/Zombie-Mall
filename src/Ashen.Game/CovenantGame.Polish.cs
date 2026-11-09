using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 readonly Dictionary<string,float> buttonHoverStarted=[];
 string lastHoveredButton="";
 Rectangle lastPressedButton;
 float lastPressedAt=-10;
 void DrawButtonPress(Rectangle r)
 {
  if(preferences.ReducedMotion||!r.Equals(lastPressedButton)||elapsed-lastPressedAt>.13f)return;
  float fade=1-(elapsed-lastPressedAt)/.13f;
  Fill(new Rectangle(r.X+4,r.Bottom-6,Math.Max(1,r.Width-8),3),AccentGold*(fade*.8f));
 }
 float FeedbackStrength(Rectangle r,string id,bool hovered)
 {
  if(!hovered)return 0;
  if(preferences.ReducedMotion)return 1;
  string key=$"{id}:{r.X}:{r.Y}:{r.Width}:{r.Height}";
  if(lastHoveredButton!=key){buttonHoverStarted.Clear();lastHoveredButton=key;}
  if(!buttonHoverStarted.TryGetValue(key,out float start))
  {
   if(buttonHoverStarted.Count>256)buttonHoverStarted.Clear();
   buttonHoverStarted[key]=elapsed;start=elapsed;
  }
  return Math.Clamp((elapsed-start)/.12f,0,1);
 }
 void DrawButtonGlow(Rectangle r,string id,bool hovered)
 {
  float strength=FeedbackStrength(r,id,hovered);
  if(strength<=0||r.Width<18||r.Height<18)return;
  Fill(new Rectangle(r.X+5,r.Bottom-4,Math.Max(1,(int)((r.Width-10)*strength)),2),AccentGold*.64f);
 }

 // F6 and Shift+F6 cycle visible management buttons. Enter activates the focused
 // button through its existing action; no second navigation or rules route exists.
 int keyboardFocus=-1;
 UiScreenId? keyboardFocusScreen;
 void HandleManagementFocus()
 {
  var active=ui.Screens.Active;
  if(active!=keyboardFocusScreen){keyboardFocus=-1;keyboardFocusScreen=active;return;}
  if(active==null||title||characterCreationOpen||ui.Modals.IsOpen||ui.Information.IsOpen||
     SaveBrowserActive||SaveNameEditorOpen||MapNoteEditorOpen||JournalNoteEditorOpen)return;
  if(Pressed(Keys.F6))
  {
   var eligible=Enumerable.Range(0,buttons.Count).Where(i=>
    buttons[i].Rect is {Width:>=24 and <=900,Height:>=20 and <=110}&&
    (ui.Layout.ManagementRect.Intersects(buttons[i].Rect)||ui.Layout.LeftRail.Intersects(buttons[i].Rect)||ui.Layout.RightRail.Intersects(buttons[i].Rect))).ToArray();
   if(eligible.Length==0)return;
   bool backwards=keys.IsKeyDown(Keys.LeftShift)||keys.IsKeyDown(Keys.RightShift);
   int position=Array.IndexOf(eligible,keyboardFocus);
   keyboardFocus=eligible[position<0?(backwards?eligible.Length-1:0):
    (position+(backwards?-1:1)+eligible.Length)%eligible.Length];
   audio?.PlayUi("hover");
  }
  if(keyboardFocus>=0&&keyboardFocus<buttons.Count&&Pressed(Keys.Enter))
  {
   var control=buttons[keyboardFocus];keyboardFocus=-1;lastPressedButton=control.Rect;lastPressedAt=elapsed;
   control.Click();audio?.PlayUi("confirm");
  }
 }
 void DrawManagementFocus()
 {
  if(keyboardFocus<0||!ui.Screens.IsOpen||ui.Modals.IsOpen||ui.Information.IsOpen||keyboardFocus>=buttons.Count)return;
  var r=buttons[keyboardFocus].Rect;
  if(r.Width<8||r.Height<8)return;
  Border(new Rectangle(r.X-2,r.Y-2,r.Width+4,r.Height+4),Bright,2);
  Fill(new Rectangle(r.X+3,r.Bottom-5,Math.Min(12,r.Width-6),2),Bright);
 }
}
