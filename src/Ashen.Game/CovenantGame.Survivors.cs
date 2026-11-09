using Ashen.Core;
using Microsoft.Xna.Framework;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 CampaignActor? SurvivorProfile(Actor a)=>world.Campaign?.Actors.FirstOrDefault(d=>d.Id==a.Id&&d.Archetype!="");
 static Color SurvivorCoat(int index)=>(index%4) switch{0=>new(63,133,151),1=>new(179,128,70),2=>new(163,73,64),_=>new(91,131,77)};
 // Code-native placeholder figures keep modern silhouettes distinct until the final character-art pass.
 void DrawModernFigure(V foot,float height,int index,bool zombie,bool runner,bool fallen=false,float step=0,bool firearm=false)
 {
  float u=height/100;Color coat=zombie?(runner?new(136,66,59):new(89,99,76)):SurvivorCoat(index);Color skin=zombie?new(143,155,112):index%4==1?new(151,103,76):new(204,166,125);Color dark=new(29,36,42);
  if(fallen){Box(new((int)(foot.X-27*u),(int)(foot.Y-10*u),(int)(55*u),(int)(13*u)),coat*.65f,dark);return;}
  void R(float x,float y,float w,float h,Color c)=>Fill(new((int)(foot.X+x*u),(int)(foot.Y+y*u),Math.Max(1,(int)(w*u)),Math.Max(1,(int)(h*u))),c);
  void Limb(float x,float y,float ex,float ey,Color c,int width)=>Line(foot+new V(x,y)*u,foot+new V(ex,ey)*u,c,Math.Max(1,(int)(width*u)));
  Limb(-9,-34,-10+step*3,-4,dark,11);Limb(9,-34,10-step*3,-4,dark,11);
  R(-17+step*3,-6,17,6,new Color(19,23,25));R(3-step*3,-6,17,6,new Color(19,23,25));
  R(-18,-72,36,40,dark);R(-15,-69,30,35,coat);R(-2,-67,3,31,new Color(186,185,159));R(-16,-40,31,6,new Color(54,52,45));
  Limb(-18,-65,-23,-37,coat,9);Limb(18,-65,zombie?34:24,zombie?-57:-36,coat,9);
  R(-27,-40,8,9,skin);R(zombie?31:20,zombie?-61:-39,8,9,skin);
  if(!zombie&&firearm){R(24,-48,24,6,new Color(80,90,94));R(26,-44,6,12,new Color(66,54,42));}
  else if(!zombie){Limb(27,-39,30,-65,new Color(163,171,166),3);Limb(30,-65,36,-68,new Color(163,171,166),3);}
  batch.Draw(circle,new Rectangle((int)(foot.X-14*u),(int)(foot.Y-98*u),(int)(28*u),(int)(29*u)),dark);
  batch.Draw(circle,new Rectangle((int)(foot.X-11*u),(int)(foot.Y-94*u),(int)(22*u),(int)(23*u)),skin);
  R(-11,-96,22,6,zombie?new Color(67,75,59):new Color(57,43,33));R(-7,-84,4,3,zombie?new Color(183,53,43):dark);R(4,-84,4,3,zombie?new Color(183,53,43):dark);
  if(index%4==2&&!zombie){R(-4,-60,8,3,new Color(229,220,191));R(-1,-63,3,9,new Color(229,220,191));}
 }
 void DrawModernActor(Actor a,CampaignActor d)
 {
  var foot=Screen(a.Position);bool zombie=a.Enemy;float step=world.IsMoving(a)?MathF.Sin(a.WalkDistance*12):0;
  batch.Draw(circle,new Rectangle((int)(foot.X-18*zoom),(int)(foot.Y-5*zoom),(int)(36*zoom),(int)(12*zoom)),Color.Black*.4f);
  if(!a.Alive){DrawModernFigure(foot,88*zoom,a.Portrait,zombie,d.Archetype=="runner",true);return;}
  GroundRing(foot,18*zoom,(zombie?Red:IsSelected(a)?Teal:Gold)*.8f);DrawModernFigure(foot,88*zoom,a.Portrait,zombie,d.Archetype=="runner",step:step,firearm:a.Equipment.Weapon?.Definition.MagazineSize>0);
  if(a==world.Chosen&&!zombie)GroundRing(foot,22*zoom,Bright);
  if(world.LocalAwarenessEnabled&&a.Enemy&&world.Visible.Contains(a.Pos)&&a.Awareness.State!=AwarenessKind.Idle)Text(a.Awareness.State.ToString().ToUpperInvariant(),foot.X-25*zoom,foot.Y-117*zoom,Gold,.35f*zoom);
  if(world.Active==a)Text("TURN",foot.X-14,foot.Y-105*zoom,Bright,.5f*zoom);
  Bar(new((int)(foot.X-18*zoom),(int)(foot.Y-96*zoom),(int)(36*zoom),4),a.Hp/(float)a.MaxHp,zombie?Red:Teal);
 }
 void DrawModernPortrait(Actor a,Rectangle r)
 {
  var d=SurvivorProfile(a)!;float light=a.Alive?1:.4f;Color coat=SurvivorCoat(a.Portrait);Color skin=a.Enemy?new(143,155,112):a.Portrait%4==1?new(151,103,76):new(204,166,125);
  Fill(r,new Color(29,44,49));Fill(new(r.X+r.Width/8,r.Y+r.Height*3/5,r.Width*3/4,r.Height*2/5),coat*light);
  var head=new Rectangle(r.X+r.Width/4,r.Y+r.Height/9,r.Width/2,r.Height*3/5);batch.Draw(circle,head,new Color(42,36,31)*light);
  var face=new Rectangle(head.X+3,head.Y+head.Height/6,head.Width-6,head.Height*4/5);batch.Draw(circle,face,skin*light);
  int eye=Math.Max(2,r.Width/18);Fill(new(face.X+face.Width/4,face.Y+face.Height/2,eye,eye),new Color(35,36,34)*light);Fill(new(face.X+face.Width*2/3,face.Y+face.Height/2,eye,eye),new Color(35,36,34)*light);
  if(a.Portrait==2){Fill(new(r.Center.X-3,r.Bottom-r.Height/4,6,r.Height/6),Bright*light);Fill(new(r.Center.X-r.Width/9,r.Bottom-r.Height/5,r.Width*2/9,5),Bright*light);}
  Border(r,coat*.7f,2);
 }
 bool DrawRecruitFeature(Feature f,V v,float light)
 {
  if(f.Kind!="campaign-event")return false;
  var effect=world.Campaign?.Events.FirstOrDefault(e=>e.Id==f.CampaignEventId)?.Effects.FirstOrDefault(e=>e.Handler=="recruit");if(effect==null)return false;
  if(f.Used||world.Actors.Any(a=>a.Id==effect.ActorId))return true;
  var d=world.Campaign!.Actors.First(a=>a.Id==effect.ActorId);
  DrawModernFigure(v,72*zoom,d.Portrait,false,false);GroundRing(v,19*zoom,Teal*light);Text(d.Name+" (invite)",v.X-35*zoom,v.Y-92*zoom,Bright*light,.48f*zoom);return true;
 }
 void StageSurvivorCapture()
 {
  if(scene=="survivors-title"){title=true;return;}
  title=false;foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;
  if(scene is "survivors-party" or "survivors-record" or "survivors-inventory")
  {
   foreach(var id in new[]{"casey","robin"}){var f=world.Features.First(f=>f.Id=="feature.mall.recruit-"+id);world.Chosen.Pos=f.Pos;world.Reveal();if(!world.Interact(f))throw new InvalidOperationException("Recruit capture setup failed.");}
   int i=0;foreach(var actor in world.Party)actor.Pos=new(10+i++*2,21);world.Reveal();camera=new(14,22);SelectAll();
   if(scene=="survivors-record")ui.Screens.Navigate(UiScreenId.CharacterInformation);
   if(scene=="survivors-inventory")ui.Screens.Navigate(UiScreenId.Inventory);
  }
  else if(scene=="survivors-recruit"){world.Chosen.Pos=new(12,27);world.Party.Skip(1).First().Pos=new(12,28);world.Reveal();camera=new(13,27);}
  else {world.Chosen.Pos=scene=="survivors-runner"?new(36,23):new(36,11);world.Party.Skip(1).First().Pos=scene=="survivors-runner"?new(36,24):new(36,12);world.Reveal();camera=new(38,world.Chosen.Pos.Y);world.CheckEncounter();}
 }
}
