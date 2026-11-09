using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 readonly Dictionary<string,Rectangle[]> bodyBounds=[];
 readonly Dictionary<string,Color[]> bodyPixels=[];
 void BuildBodyBounds(string name)
 {
  var atlas=tex[name];int w=atlas.Width/2,h=atlas.Height/2;var colors=new Color[atlas.Width*atlas.Height];atlas.GetData(colors);bodyPixels[name]=colors;
  var bounds=new Rectangle[4];
  for(int i=0;i<4;i++)
  {
   int x0=i%2*w,y0=i/2*h,minX=x0+w,minY=y0+h,maxX=x0,maxY=y0;
   for(int y=y0;y<y0+h;y++)for(int x=x0;x<x0+w;x++)if(colors[y*atlas.Width+x].A>40){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
   bounds[i]=new(minX,minY,Math.Max(1,maxX-minX+1),Math.Max(1,maxY-minY+1));
  }
  bodyBounds[name]=bounds;
 }
 // The portrait atlas has six choices, while the world body atlas has four frames.
 // Keep every portrait choice playable by mapping it to an existing body frame.
 (string Atlas,int Index) Body(Actor a)=>a.Enemy?("enemy_bodies",a.CreatureKind switch{"Archer"=>1,"Wraith"=>2,"Boss"=>3,_=>0}):("party_bodies",((a.Portrait%4)+4)%4);
 Rectangle ActorBounds(Actor a)
 {
  if(SurvivorProfile(a)!=null){var p=Screen(a.Position);return new((int)(p.X-24*zoom),(int)(p.Y-88*zoom),(int)(48*zoom),(int)(88*zoom));}
  var (atlas,index)=Body(a);var source=bodyBounds[atlas][index];float height=108*zoom,width=height*source.Width/source.Height;var foot=Screen(a.Position);
  return new((int)(foot.X-width/2),(int)(foot.Y-height),(int)width,(int)height);
 }
 Actor? PickCorpse()
 {
  // Corpses use a generous body/foot hit area because their rendered sprite lies on its side.
  var point=MouseWorld();
  foreach(var a in world.Actors.Where(a=>!a.Alive&&a.Enemy&&world.Visible.Contains(a.Pos)).OrderByDescending(a=>a.Position.Y))
  {
   var rect=ActorBounds(a);var fallen=new Rectangle(rect.X-18,rect.Bottom-58,rect.Width+36,64);
   if(fallen.Contains(cursor)||a.Position.Distance(point)<=.62f)return a;
  }
  return null;
 }
 Actor? PickActor()
 {
  // Frontmost visible figure wins; transparent pixels do not steal ground clicks.
  foreach(var a in world.Actors.Where(a=>a.Alive&&(!a.Enemy||world.Visible.Contains(a.Pos))).OrderByDescending(a=>a.Position.Y))
  {
   var rect=ActorBounds(a);if(!rect.Contains(cursor))continue;
   if(SurvivorProfile(a)!=null)return a;
   var (atlas,index)=Body(a);var source=bodyBounds[atlas][index];float u=(cursor.X-rect.X)/(float)rect.Width;if(a.FacingLeft)u=1-u;
   int x=source.X+Math.Clamp((int)(u*source.Width),0,source.Width-1);
   int y=source.Y+Math.Clamp((int)((cursor.Y-rect.Y)/(float)rect.Height*source.Height),0,source.Height-1);
   if(bodyPixels[atlas][y*tex[atlas].Width+x].A>50)return a;
  }
  var footActor=world.At(MouseWorld());return footActor!=null&&(!footActor.Enemy||world.Visible.Contains(footActor.Pos))?footActor:null;
 }
 void GroundRing(V foot,float radius,Color color)
 {
  const int segments=36;V previous=foot+new V(radius,0);
  for(int i=1;i<=segments;i++){float angle=i*MathF.Tau/segments;var next=foot+new V(MathF.Cos(angle)*radius,MathF.Sin(angle)*radius*.4f);Line(previous,next,color,2);previous=next;}
 }
 void DrawActor(Actor a)
 {
  if(SurvivorProfile(a) is {} profile){DrawModernActor(a,profile);return;}
  var foot=Screen(a.Position);var (atlas,index)=Body(a);var source=bodyBounds[atlas][index];float scale=108*zoom/source.Height;
  batch.Draw(circle,new Rectangle((int)(foot.X-21*zoom),(int)(foot.Y-6*zoom),(int)(42*zoom),(int)(13*zoom)),Color.Black*.48f);
  if(!a.Alive)
  {
   batch.Draw(tex[atlas],foot,source,new Color(119,108,101)*.55f,MathF.PI/2,new V(source.Width/2f,source.Height),scale*.65f,a.FacingLeft?SpriteEffects.FlipHorizontally:SpriteEffects.None,0);return;
  }
  Color ring=a.Enemy?Red:IsSelected(a)?Teal:Gold;
  GroundRing(foot,21*zoom,ring*.85f);
  if(!a.Enemy&&a==world.Chosen&&IsSelected(a)){GroundRing(foot,25*zoom,Bright);Text("*",foot.X-5,foot.Y+6,Bright,.7f);}
  if(a.Character.HasCondition("dodging"))Text("[DODGE]",foot.X-24,foot.Y-145*zoom,Teal,.52f);
  else if(a.Character.HasCondition("hidden"))Text("[HIDDEN]",foot.X-26,foot.Y-145*zoom,Gold,.50f);
  bool moving=world.IsMoving(a)&&a.Motion>0;
  float step=moving?MathF.Sin(a.WalkDistance*12):0;
  float bob=moving?MathF.Abs(step)*1.7f*zoom:0;
  // A subtle gait sway is driven by distance travelled; facing is retained at rest.
  float sway=step*.018f;
  batch.Draw(tex[atlas],foot-new V(0,bob),source,Color.White,sway,new V(source.Width/2f,source.Height),scale,a.FacingLeft?SpriteEffects.FlipHorizontally:SpriteEffects.None,0);
  if(world.Active==a){GroundRing(foot,25*zoom,Bright);Text("TURN",foot.X-17*zoom,foot.Y-130*zoom,Bright,.58f*zoom);}
  Bar(new((int)(foot.X-20*zoom),(int)(foot.Y-116*zoom),(int)(40*zoom),4),a.Hp/(float)a.MaxHp,a.Enemy?Red:Teal);
 }
}
