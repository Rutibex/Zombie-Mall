using Microsoft.Xna.Framework;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 /// <summary>
 /// Resolution-independent geometry for the permanent Infinity-style shell.
 /// The game renders to a 900-unit logical canvas; only the center world viewport
 /// grows with aspect ratio. InterfaceScale changes shell geometry, never the
 /// backbuffer aspect ratio or texture sampling mode.
 /// </summary>
 sealed class UiLayout
 {
  public const int LogicalHeight=900;
  public const int MinimumLogicalWidth=1440; // 16:10 remains a first-class supported layout.
  public const float MinimumScale=.85f;
  public const float MaximumScale=1.30f;

  public int Width {get;private set;}=1600;
  public int Height {get;private set;}=LogicalHeight;
  public float RequestedScale {get;private set;}=1;
  public float Scale {get;private set;}=1;
  public int OuterMargin {get;private set;}=10;
  public int RailWidth {get;private set;}=130;
  public int Gap {get;private set;}=8;
  public int BottomHeight {get;private set;}=190;

  public Rectangle Screen {get;private set;}=new(0,0,1600,LogicalHeight);
  public Rectangle LeftRail {get;private set;}
  public Rectangle RightRail {get;private set;}
  public Rectangle CenterBounds {get;private set;}
  public Rectangle WorldViewport {get;private set;}
  public Rectangle BottomShell {get;private set;}
  public Rectangle MessageRegion {get;private set;}
  public Rectangle TurnControlRegion {get;private set;}
  public Rectangle ActionRegion {get;private set;}
  public Rectangle ManagementRect {get;private set;}

  public static int LogicalWidthFor(int backBufferWidth,int backBufferHeight)
  {
   if(backBufferWidth<=0||backBufferHeight<=0)return 1600;
   return Math.Max(MinimumLogicalWidth,(int)Math.Round(LogicalHeight*backBufferWidth/(double)backBufferHeight));
  }

  public void Reflow(int width,int height,float requestedScale)
  {
   Width=Math.Max(MinimumLogicalWidth,width);Height=Math.Max(720,height);
   RequestedScale=float.IsFinite(requestedScale)?Math.Clamp(requestedScale,MinimumScale,MaximumScale):1;
   Scale=RequestedScale;

   int S(float value)=>Math.Max(1,(int)Math.Round(value*Scale));
   OuterMargin=S(10);Gap=S(8);RailWidth=S(130);BottomHeight=S(190);

   // Guarantee a useful central viewport even if a future port requests an extreme size.
   const int minWorldWidth=760,minWorldHeight=500;
   float widthBudget=(Width-S(20)-S(16)-minWorldWidth)/(260f);
   float heightBudget=(Height-S(20)-S(8)-minWorldHeight)/190f;
   float fit=Math.Min(widthBudget,heightBudget);
   if(float.IsFinite(fit)&&fit<Scale)
   {
    Scale=Math.Clamp(fit,MinimumScale,RequestedScale);
    OuterMargin=S(10);Gap=S(8);RailWidth=S(130);BottomHeight=S(190);
   }

   Screen=new(0,0,Width,Height);
   LeftRail=new(OuterMargin,OuterMargin,RailWidth,Height-OuterMargin*2);
   RightRail=new(Width-OuterMargin-RailWidth,OuterMargin,RailWidth,Height-OuterMargin*2);
   int centerX=LeftRail.Right+Gap;
   int centerRight=RightRail.Left-Gap;
   CenterBounds=new(centerX,OuterMargin,Math.Max(1,centerRight-centerX),Height-OuterMargin*2);
   BottomShell=new(CenterBounds.X,CenterBounds.Bottom-BottomHeight,CenterBounds.Width,BottomHeight);
   WorldViewport=new(CenterBounds.X,CenterBounds.Y,CenterBounds.Width,Math.Max(1,BottomShell.Top-Gap-CenterBounds.Y));

   int pad=S(10),messageH=Math.Min(S(78),Math.Max(S(62),BottomShell.Height/2));
   MessageRegion=new(BottomShell.X+pad,BottomShell.Y+pad,BottomShell.Width-pad*2,messageH);
   int rowY=MessageRegion.Bottom+S(8),rowBottom=BottomShell.Bottom-pad;
   int turnW=Math.Min(S(206),Math.Max(S(174),BottomShell.Width/5));
   TurnControlRegion=new(BottomShell.X+pad,rowY,turnW,Math.Max(S(48),rowBottom-rowY));
   ActionRegion=new(TurnControlRegion.Right+S(9),rowY,Math.Max(1,BottomShell.Right-pad-(TurnControlRegion.Right+S(9))),TurnControlRegion.Height);

   int managementW=Math.Min(1040,Math.Max(860,CenterBounds.Width-S(28)));
   int managementH=Math.Min(654,Math.Max(600,CenterBounds.Height-S(28)));
   ManagementRect=new(CenterBounds.Center.X-managementW/2,CenterBounds.Center.Y-managementH/2,managementW,managementH);
  }

  public bool IsPermanentUi(Point p)=>LeftRail.Contains(p)||RightRail.Contains(p)||BottomShell.Contains(p);
  public bool IsWorld(Point p)=>WorldViewport.Contains(p);
  public bool IsOnCanvas(Point p)=>Screen.Contains(p);
 }
}
