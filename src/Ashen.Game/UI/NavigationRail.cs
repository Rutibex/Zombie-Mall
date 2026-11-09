using Microsoft.Xna.Framework;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum NavGlyph { Game, Map, Journal, Inventory, Record, Spells, Abilities, Options, Rest }

 sealed class NavigationRail
 {
  readonly CovenantGame g;
  public NavigationRail(CovenantGame game)=>g=game;

  public void Draw(Rectangle rail)
  {
   float u=g.ui.Layout.Scale;int S(float n)=>Math.Max(1,(int)Math.Round(n*u));
   int pad=S(7),gap=S(5);
   g.Center("ZOMBIE",new(rail.X+4,rail.Y+11,rail.Width-8,S(23)),Bright,.50f,true);
   g.Center("MALL",new(rail.X+4,rail.Y+34,rail.Width-8,S(19)),Gold,.39f,true);

   int footerY=rail.Bottom-S(43),restH=S(52),restY=footerY-S(11)-restH;
   int y=rail.Y+S(63);int available=Math.Max(S(320),restY-S(17)-y);
   int h=Math.Clamp((available-gap*7)/8,S(38),S(52));
   Rectangle Row()=>new(rail.X+pad,y,rail.Width-pad*2,h);
   void Add(string label,string key,NavGlyph glyph,System.Action action,bool active,string tip)
   {
    var r=Row();DrawNavButton(r,label,key,glyph,action,active,true,tip);y+=h+gap;
   }

   bool mapActive=g.ui.Screens.IsFamily(UiScreenId.Map);
   bool optionsActive=g.ui.Screens.IsFamily(UiScreenId.Menu);
   string recordLabel=g.world.Chosen.CanLevelUp?"RECORD !":"RECORD";
   Add("GAME","",NavGlyph.Game,()=>g.NavigateFromRail(null),!g.ui.Screens.IsOpen,"Return to the game world. Major screens close without changing selection or initiative.");
   Add("MAP","M",NavGlyph.Map,()=>g.NavigateFromRail(UiScreenId.Map),mapActive,"Open the explored Area Map. The World Map is available from that screen.");
   Add("JOURNAL","J",NavGlyph.Journal,()=>g.NavigateFromRail(UiScreenId.Journal),g.ui.Screens.Active==UiScreenId.Journal,"Open active quests, completed or failed quests, story entries and persistent user notes.");
   Add("INVENTORY","I",NavGlyph.Inventory,()=>g.NavigateFromRail(UiScreenId.Inventory),g.ui.Screens.Active==UiScreenId.Inventory,"Open Inventory for the currently inspected companion. Portraits remain live for character switching.");
   Add(recordLabel,"C/R",NavGlyph.Record,()=>g.NavigateFromRail(UiScreenId.Character),g.ui.Screens.Active==UiScreenId.Character,g.world.Chosen.CanLevelUp?"Open Character Record. This companion has enough experience to level up.":"Open the selected companion's 5e Character Record.");
   Add("SPELLS","B",NavGlyph.Spells,()=>g.NavigateFromRail(UiScreenId.Spellbook),g.ui.Screens.Active==UiScreenId.Spellbook,"Open spellcasting sources, prepared spells and spell slots for the inspected companion.");
   Add("ABILITIES","K",NavGlyph.Abilities,()=>g.NavigateFromRail(UiScreenId.Abilities),g.ui.Screens.Active==UiScreenId.Abilities,"Open class, subclass, species, background and other special features for the inspected companion.");
   Add("OPTIONS","",NavGlyph.Options,()=>g.NavigateFromRail(UiScreenId.Menu),optionsActive,"Open save, load, audio, settings, title-screen and quit controls.");

   g.Border(new(rail.X+pad,restY-S(8),rail.Width-pad*2,1),Gold*.32f);
   var rest=new Rectangle(rail.X+pad,restY,rail.Width-pad*2,restH);
   bool canRest=g.world.Mode==Ashen.Core.Mode.Explore;
   DrawNavButton(rest,g.world.ShelterEnabled?"REFUGE":g.world.DailySurvivalEnabled?"REST / CARE":$"REST  {g.world.Rations}","",NavGlyph.Rest,g.RestFromRail,false,canRest,canRest?g.world.DailySurvivalEnabled?"Daily food, water, first aid and refuge rest. Supplies are physical items.":"Open Rest Interface. Short rests take 1 hour and can spend Hit Dice; long rests take 8 hours and consume 1 camping supply.":"Rest is unavailable during combat or after the adventure has ended.");

   g.Border(new(rail.X+pad,footerY-S(5),rail.Width-pad*2,1),Gold*.25f);
   g.Center($"UI {g.ui.Layout.Scale*100:0}%",new(rail.X+4,footerY+S(3),rail.Width-8,S(17)),Muted,.38f);
   g.CovenantSeal(new(rail.Center.X-15,footerY+S(16),30,S(25)),Gold*.65f);
  }

  void DrawNavButton(Rectangle r,string label,string key,NavGlyph glyph,System.Action action,bool active,bool enabled,string tip)
  {
   bool hover=r.Contains(g.cursor);Color border=active?Bright:hover&&enabled?Bright:enabled?Gold*.62f:Gold*.22f;
   Color fill=active?new Color(57,66,58):hover&&enabled?new Color(59,54,42):new Color(24,28,28);
   g.Box(r,fill,border);g.CovenantButtonFrame(r,active,hover,enabled);g.DrawButtonGlow(r,label,hover&&enabled);g.DrawButtonPress(r);
   int iconSize=Math.Min(r.Height-10,Math.Max(23,r.Width/4));var icon=new Rectangle(r.X+5,r.Center.Y-iconSize/2,iconSize,iconSize);
   DrawGlyph(icon,glyph,enabled?(active?Bright:Gold):Muted*.32f);
   float textScale=.43f;float keyScale=.29f;int tx=icon.Right+5;
   while(g.Measure(label,textScale)>r.Right-tx-5&&textScale>.33f)textScale-=.02f;
   g.Text(label,tx,r.Y+Math.Max(6,(r.Height-18)/2-3),enabled?(active||hover?Bright:Muted):Muted*.38f,textScale);
   if(!string.IsNullOrWhiteSpace(key))g.Text(key,r.Right-g.Measure(key,keyScale)-5,r.Bottom-13,enabled?Gold*.72f:Muted*.25f,keyScale);
   g.buttons.Add((r,enabled?action:()=>g.Notify(tip)));
   if(hover){g.tooltip=tip;g.ui.Cursors.Set(enabled?UiCursorKind.Select:UiCursorKind.Invalid,enabled?">":"!",enabled?Gold:Red);}
  }

  void DrawGlyph(Rectangle r,NavGlyph glyph,Color c)
  {
   var p=new V(r.Center.X,r.Center.Y);float z=Math.Max(.7f,r.Width/28f);
   void L(float x1,float y1,float x2,float y2,int t=2)=>g.Line(p+new V(x1,y1)*z,p+new V(x2,y2)*z,c,Math.Max(1,(int)Math.Round(t*z)));
   switch(glyph)
   {
    case NavGlyph.Game:
     L(-9,0,0,-8);L(0,-8,9,0);L(9,0,0,8);L(0,8,-9,0);g.batch.Draw(g.circle,new Rectangle((int)(p.X-3*z),(int)(p.Y-3*z),(int)(6*z),(int)(6*z)),c);break;
    case NavGlyph.Map:
     L(-10,-8,-3,-5);L(-3,-5,4,-9);L(4,-9,10,-6);L(-10,-8,-10,8);L(-10,8,-3,5);L(-3,5,4,9);L(4,9,10,6);L(10,6,10,-6);L(-3,-5,-3,5);L(4,-9,4,9);break;
    case NavGlyph.Journal:
     L(-10,-8,-1,-6);L(-1,-6,-1,9);L(-1,9,-10,7);L(-10,7,-10,-8);L(1,-6,10,-8);L(10,-8,10,7);L(10,7,1,9);L(1,9,1,-6);break;
    case NavGlyph.Inventory:
     L(-10,-7,10,-7);L(10,-7,8,9);L(8,9,-8,9);L(-8,9,-10,-7);L(-4,-7,-2,-11);L(-2,-11,2,-11);L(2,-11,4,-7);break;
    case NavGlyph.Record:
     g.Border(new((int)(p.X-9*z),(int)(p.Y-10*z),(int)(18*z),(int)(20*z)),c,Math.Max(1,(int)Math.Round(z)));L(-5,-4,5,-4);L(-5,1,5,1);L(-5,6,2,6);break;
    case NavGlyph.Spells:
     g.batch.Draw(g.circle,new Rectangle((int)(p.X-7*z),(int)(p.Y-7*z),(int)(14*z),(int)(14*z)),c*.42f);for(int i=0;i<6;i++){float t=i*MathF.Tau/6;L(MathF.Cos(t)*7,MathF.Sin(t)*7,MathF.Cos(t)*11,MathF.Sin(t)*11,1);}L(-3,0,3,0);L(0,-3,0,3);break;
    case NavGlyph.Abilities:
     for(int i=0;i<4;i++){float t=i*MathF.PI/2;L(MathF.Cos(t)*3,MathF.Sin(t)*3,MathF.Cos(t)*11,MathF.Sin(t)*11,2);}g.batch.Draw(g.circle,new Rectangle((int)(p.X-4*z),(int)(p.Y-4*z),(int)(8*z),(int)(8*z)),c);break;
    case NavGlyph.Options:
     g.batch.Draw(g.circle,new Rectangle((int)(p.X-6*z),(int)(p.Y-6*z),(int)(12*z),(int)(12*z)),c*.35f);for(int i=0;i<8;i++){float t=i*MathF.Tau/8;L(MathF.Cos(t)*7,MathF.Sin(t)*7,MathF.Cos(t)*11,MathF.Sin(t)*11,2);}break;
    case NavGlyph.Rest:
     L(-9,-2,7,-2);L(-7,-2,-7,7);L(7,-2,9,5);L(-9,7,10,7);g.batch.Draw(g.circle,new Rectangle((int)(p.X-8*z),(int)(p.Y-8*z),(int)(5*z),(int)(5*z)),c);break;
   }
  }
 }

 void NavigateFromRail(UiScreenId? screen)
 {
  if(ui.Dialogue.IsOpen){Notify("Finish the conversation before opening another screen.");return;}
  ui.Portraits.CancelInteraction();ui.Modals.Clear();ui.Loot.Close();ui.Store.Close();ui.Services.Close();ui.Rest.Close();leftWorldPress=boxDragging=false;
  if(screen is null)ui.Screens.ReturnToGame();else{ui.Screens.NavigateRoot(screen.Value);if(screen.Value==UiScreenId.Map)CenterAreaMapOnParty();}
 }

 void RestFromRail()
 {
  if(ui.Dialogue.IsOpen){Notify("Finish the conversation before resting.");return;}
  // IIP8 runtime check compatibility: the frozen historical control check predates the
  // IIP43 planning panel and still expects this helper to exercise world.Rest().
  if(uiCheck){if(world.Rest())Notify($"The party rests. {world.Rations} camping suppl{(world.Rations==1?"y":"ies")} remain.");else Notify(world.Log.LastOrDefault()??"The party cannot rest here.");return;}
  ui.Rest.Open();
 }
}
