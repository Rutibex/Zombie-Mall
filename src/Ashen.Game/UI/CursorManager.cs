using Microsoft.Xna.Framework;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 /// <summary>IIP16 semantic cursor vocabulary. The enum is deliberately broader than the
 /// first playable's current content so later dialogue, thievery, traps, and spell targeting
 /// can request a stable semantic cursor without extending the renderer again.</summary>
 enum UiCursorKind
 {
  Default,Select,Move,Blocked,Attack,Talk,Door,Locked,Container,Pickup,Travel,Stairs,
  Interact,Trap,Disarm,Pickpocket,SpellTarget,InvalidTarget,Friendly,AoE,Invalid
 }

 sealed class CursorManager
 {
  readonly CovenantGame g;
  UiCursorKind kind;
  string? glyphOverride;
  Color? colorOverride;
  public CursorManager(CovenantGame game)=>g=game;
  public UiCursorKind Current=>kind;
  public string SemanticName=>Name(kind);

  public void BeginFrame(){kind=UiCursorKind.Default;glyphOverride=null;colorOverride=null;}
  public void Set(UiCursorKind value,string? glyph=null,Color? color=null){kind=value;glyphOverride=glyph;colorOverride=color;}

  static string Name(UiCursorKind value)=>value switch
  {
   UiCursorKind.Default=>"Pointer",UiCursorKind.Select=>"Select",UiCursorKind.Move=>"Move",UiCursorKind.Blocked=>"Blocked",
   UiCursorKind.Attack=>"Attack",UiCursorKind.Talk=>"Talk",UiCursorKind.Door=>"Open",UiCursorKind.Locked=>"Locked",
   UiCursorKind.Container=>"Container",UiCursorKind.Pickup=>"Pick up",UiCursorKind.Travel=>"Travel",UiCursorKind.Stairs=>"Stairs",
   UiCursorKind.Interact=>"Interact",UiCursorKind.Trap=>"Trap",UiCursorKind.Disarm=>"Disarm",UiCursorKind.Pickpocket=>"Pickpocket",
   UiCursorKind.SpellTarget=>"Target",UiCursorKind.InvalidTarget=>"Invalid target",UiCursorKind.Friendly=>"Friendly target",
   UiCursorKind.AoE=>"Area placement",_=>"Unavailable"
  };

  Color DefaultColor()=>kind switch
  {
   UiCursorKind.Attack or UiCursorKind.Blocked or UiCursorKind.InvalidTarget or UiCursorKind.Invalid=>Red,
   UiCursorKind.Friendly or UiCursorKind.Select or UiCursorKind.Move or UiCursorKind.SpellTarget or UiCursorKind.AoE=>Teal,
   _=>Gold
  };

  public void Draw()
  {
   // The OS cursor is hidden by CovenantGame; every state, including the ordinary UI pointer,
   // is drawn here so the click hotspot and semantic art cannot drift apart after scaling.
   var p=new V(g.cursor.X,g.cursor.Y);Color color=colorOverride??DefaultColor();
   DrawPointer(p,kind==UiCursorKind.Default?Bright:color);
   if(kind==UiCursorKind.Default)return;
   int size=g.preferences.LargeCursor?39:30;
   var r=new Rectangle(g.cursor.X+12,g.cursor.Y+12,size,size);
   g.Box(r,new Color(10,14,17)*.96f,color);g.CovenantFrame(r,false);
   g.Border(new(r.X+3,r.Y+3,r.Width-6,r.Height-6),color*.28f);
   if(!string.IsNullOrWhiteSpace(glyphOverride)){g.Center(glyphOverride,new(r.X,r.Y+7,r.Width,r.Height-7),color,.48f);return;}
   DrawSemantic(kind,r,color);
  }

  void DrawPointer(V p,Color color)
  {
   // Original primitive arrow: exact click hotspot is the tip at p.
   float s=g.preferences.LargeCursor?1.4f:1;
   V a=p,b=p+new V(0,18)*s,c=p+new V(5,13)*s,d=p+new V(10,23)*s,e=p+new V(14,21)*s,f=p+new V(9,11)*s,h=p+new V(15,10)*s;
   g.Line(a,b,color,2);g.Line(b,c,color,2);g.Line(c,d,color,2);g.Line(d,e,color,2);g.Line(e,f,color,2);g.Line(f,h,color,2);g.Line(h,a,color,2);
  }

  void DrawSemantic(UiCursorKind value,Rectangle r,Color c)
  {
   var m=new V(r.Center.X,r.Center.Y);float pulse=g.preferences.ReducedMotion?1f:.82f+.18f*MathF.Sin(g.elapsed*5f);
   switch(value)
   {
    case UiCursorKind.Select:
     g.batch.Draw(g.circle,new Rectangle(r.Center.X-6,r.Center.Y-6,12,12),c*.20f);Ring(m,9,c);break;
    case UiCursorKind.Move:
     Arrow(m+new V(-8,0),m+new V(8,0),c);break;
    case UiCursorKind.Blocked:
     Ring(m,9,c);g.Line(m+new V(-6,6),m+new V(6,-6),c,3);break;
    case UiCursorKind.Attack:
     g.Line(m+new V(-8,8),m+new V(8,-8),c,3);g.Line(m+new V(-7,-4),m+new V(4,7),c,2);g.Line(m+new V(-3,8),m+new V(8,-3),c,2);break;
    case UiCursorKind.Talk:
     g.Line(m+new V(-8,-6),m+new V(8,-6),c,2);g.Line(m+new V(-8,-6),m+new V(-8,5),c,2);g.Line(m+new V(8,-6),m+new V(8,5),c,2);g.Line(m+new V(-8,5),m+new V(2,5),c,2);g.Line(m+new V(2,5),m+new V(-2,9),c,2);break;
    case UiCursorKind.Door:
     g.Border(new(r.X+8,r.Y+5,14,21),c,2);g.batch.Draw(g.circle,new Rectangle(r.X+17,r.Y+14,3,3),c);break;
    case UiCursorKind.Locked:
     g.Border(new(r.X+8,r.Y+13,14,11),c,2);Arc(m+new V(0,-1),7,c);break;
    case UiCursorKind.Container:
     g.Border(new(r.X+6,r.Y+11,18,13),c,2);g.Line(new V(r.X+5,r.Y+11),new V(r.Right-5,r.Y+11),c,2);g.Line(m+new V(0,-3),m+new V(0,7),c,2);break;
    case UiCursorKind.Pickup:
     g.Line(m+new V(-7,5),m+new V(0,-7),c,2);g.Line(m+new V(0,-7),m+new V(7,5),c,2);g.Line(m+new V(-7,5),m+new V(7,5),c,2);g.batch.Draw(g.circle,new Rectangle(r.Center.X-3,r.Center.Y+5,6,6),c);break;
    case UiCursorKind.Travel:
     Arrow(m+new V(-9,-4),m+new V(7,-4),c);Arrow(m+new V(-7,5),m+new V(9,5),c);break;
    case UiCursorKind.Stairs:
     for(int i=0;i<4;i++){int x=r.X+5+i*5,y=r.Bottom-6-i*5;g.Line(new V(x,y),new V(x+6,y),c,2);if(i<3)g.Line(new V(x+6,y),new V(x+6,y-5),c,2);}break;
    case UiCursorKind.Interact:
     g.batch.Draw(g.circle,new Rectangle(r.Center.X-5,r.Center.Y-5,10,10),c*.28f);for(int i=0;i<8;i++){float t=i*MathF.Tau/8;g.Line(m+new V(MathF.Cos(t),MathF.Sin(t))*7,m+new V(MathF.Cos(t),MathF.Sin(t))*11,c,2);}break;
    case UiCursorKind.Trap:
     g.Line(m+new V(-9,7),m+new V(0,-8),c,2);g.Line(m+new V(0,-8),m+new V(9,7),c,2);g.Line(m+new V(-9,7),m+new V(9,7),c,2);g.Line(m+new V(0,-3),m+new V(0,2),c,2);g.batch.Draw(g.circle,new Rectangle(r.Center.X-1,r.Center.Y+5,3,3),c);break;
    case UiCursorKind.Disarm:
     g.Line(m+new V(-8,8),m+new V(8,-8),c,2);g.Line(m+new V(-7,-7),m+new V(7,7),c,2);g.batch.Draw(g.circle,new Rectangle(r.Center.X-3,r.Center.Y-3,6,6),new Color(10,14,17));break;
    case UiCursorKind.Pickpocket:
     g.Border(new(r.X+8,r.Y+11,14,12),c,2);g.Line(new V(r.X+10,r.Y+10),new V(r.X+13,r.Y+6),c,2);g.Line(new V(r.X+13,r.Y+6),new V(r.X+18,r.Y+8),c,2);break;
    case UiCursorKind.SpellTarget:
     Ring(m,9,c);Cross(m,12,c);g.batch.Draw(g.circle,new Rectangle(r.Center.X-3,r.Center.Y-3,6,6),c*pulse);break;
    case UiCursorKind.InvalidTarget:
     Ring(m,9,c);Cross(m,7,c);break;
    case UiCursorKind.Friendly:
     g.Line(m+new V(-8,0),m+new V(8,0),c,3);g.Line(m+new V(0,-8),m+new V(0,8),c,3);break;
    case UiCursorKind.AoE:
     Ring(m,10,c);Ring(m,5,c*.75f);Cross(m,12,c*.8f);break;
    default:
     g.Line(m+new V(-7,-7),m+new V(7,7),c,3);g.Line(m+new V(7,-7),m+new V(-7,7),c,3);break;
   }
  }

  void Ring(V m,float radius,Color c)
  {
   const int steps=20;for(int i=0;i<steps;i++){float a=i*MathF.Tau/steps,b=(i+1)*MathF.Tau/steps;g.Line(m+new V(MathF.Cos(a),MathF.Sin(a))*radius,m+new V(MathF.Cos(b),MathF.Sin(b))*radius,c,2);}
  }
  void Cross(V m,float radius,Color c){g.Line(m+new V(-radius,0),m+new V(radius,0),c,1);g.Line(m+new V(0,-radius),m+new V(0,radius),c,1);}
  void Arrow(V from,V to,Color c)
  {
   g.Line(from,to,c,2);var d=to-from;float len=Math.Max(.001f,d.Length());d/=len;var side=new V(-d.Y,d.X);g.Line(to,to-d*6+side*4,c,2);g.Line(to,to-d*6-side*4,c,2);
  }
  void Arc(V center,float radius,Color c)
  {
   const int steps=10;for(int i=0;i<steps;i++){float a=MathF.PI+i*MathF.PI/steps,b=MathF.PI+(i+1)*MathF.PI/steps;g.Line(center+new V(MathF.Cos(a),MathF.Sin(a))*radius,center+new V(MathF.Cos(b),MathF.Sin(b))*radius,c,2);}
  }
 }
}
