using Microsoft.Xna.Framework;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 // Original Ashen Covenant visual language: forged iron, aged brass, and a
 // four-point covenant seal. Geometry derives from each live layout rectangle.
 void CovenantFrame(Rectangle r,bool heavy=true)
 {
  if(r.Width<24||r.Height<24)return;
  Color iron=new(9,13,17),brass=AccentGold,shadow=new(76,48,27);
  Border(r,iron,heavy?4:2);
  Border(new(r.X+3,r.Y+3,r.Width-6,r.Height-6),brass*(heavy?.78f:.48f));
  Border(new(r.X+6,r.Y+6,r.Width-12,r.Height-12),shadow*.62f);
  if(!heavy)return;
  // Corner plates are all inside the panel's existing safe area.
  foreach(int sx in new[]{1,-1})foreach(int sy in new[]{1,-1})
  {
   int x=sx>0?r.X+9:r.Right-10,y=sy>0?r.Y+9:r.Bottom-10;
   Line(new V(x-sx*5,y),new V(x+sx*5,y),brass*.72f,2);
   Line(new V(x,y-sy*5),new V(x,y+sy*5),brass*.72f,2);
   batch.Draw(circle,new Rectangle(x-2,y-2,4,4),brass);
  }
  // Hand-authored forged rivets break up broad horizontal and vertical rails.
  if(r.Width>180){Rivet(r.Center.X,r.Y+4);Rivet(r.Center.X,r.Bottom-5);}
  if(r.Height>180){Rivet(r.X+4,r.Center.Y);Rivet(r.Right-5,r.Center.Y);}
 }
 void Rivet(int x,int y)
 {
  batch.Draw(circle,new Rectangle(x-3,y-3,6,6),new Color(138,103,55));
  Fill(new Rectangle(x-1,y-2,2,2),new Color(233,199,128));
 }
 void CovenantButtonFrame(Rectangle r,bool active,bool hover,bool enabled=true)
 {
  if(r.Width<14||r.Height<14)return;
  var brass=AccentGold;Color stroke=enabled?(active?Bright:hover?brass:brass*.59f):brass*.22f;
  Border(r,new Color(8,10,13),2);
  Border(new(r.X+2,r.Y+2,r.Width-4,r.Height-4),stroke);
  if(r.Width<39||r.Height<27)return;
  Fill(new Rectangle(r.X+7,r.Y+5,Math.Max(1,r.Width-14),1),stroke*.38f);
  foreach(int sx in new[]{1,-1})
  {
   int x=sx>0?r.X+7:r.Right-8;
   Line(new V(x-sx*3,r.Y+8),new V(x,r.Y+5),stroke);
   Line(new V(x,r.Bottom-6),new V(x-sx*3,r.Bottom-9),stroke);
  }
 }
 void CovenantSeal(Rectangle r,Color color)
 {
  if(r.Width<22||r.Height<22)return;
  var p=new V(r.Center.X,r.Center.Y);int z=Math.Min(13,Math.Min(r.Width,r.Height)/3);
  for(int i=0;i<4;i++)
  {
   float t=i*MathF.PI/2;var end=p+new V(MathF.Cos(t),MathF.Sin(t))*z;
   Line(p,end,color,2);
   batch.Draw(circle,new Rectangle((int)end.X-2,(int)end.Y-2,4,4),color);
  }
  batch.Draw(circle,new Rectangle((int)p.X-3,(int)p.Y-3,6,6),color);
 }
}
