using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum HighlightKind { Actor,Corpse,Door,Container,Switch,Transition,Pickup,Trap,Dialogue,Merchant,Service,Interact }

 /// <summary>IIP17 discovered-only interaction/highlight layer. It owns the Tab reveal filter
 /// and mouseover emphasis, but never changes gameplay state. Hidden secrets stay absent until
 /// the Core world marks them discovered.</summary>
 sealed class HighlightManager
 {
  readonly CovenantGame g;
  readonly List<Rectangle> labels=[];
  public HighlightManager(CovenantGame game)=>g=game;
  public bool RevealHeld=>g.BindingHeld(InputAction.HighlightInteractables)&&!g.title&&!g.UiBlocksWorld; // historical IIP17 marker: Keys.Tab

  public bool IsTargetable(Feature feature)=>g.world.IsFeatureInteractable(feature);
  public bool IsCorpseHighlightEligible(Actor actor)=>!actor.Alive&&(!actor.Enemy||g.world.Visible.Contains(actor.Pos));
  public Feature? FeatureAt(Cell cell)=>g.world.Features.FirstOrDefault(f=>f.Pos==cell&&IsTargetable(f));

  public void DrawWorld()
  {
   if(g.title||g.ui.Screens.IsOpen)return;
   labels.Clear();

   // Living actors are highlighted only by direct mouseover. Tab is for world interactions,
   // so it never turns every creature in a fight into visual noise.
   if(g.WorldHover&&g.hoverActor is {} actor)DrawActorHover(actor);
   if(g.WorldHover&&g.hoverCorpse is {} hoveredCorpse)DrawCorpse(hoveredCorpse);
   if(g.WorldHover&&g.hoverGround is {} hoveredGround)DrawGround(hoveredGround,true);

   foreach(var feature in g.world.Features.Where(IsTargetable))
   {
    bool hovered=g.WorldHover&&ReferenceEquals(g.hoverFeature,feature);
    if(!hovered&&!RevealHeld)continue;
    DrawFeature(feature,hovered);
   }

   // Corpses remain discoverable world information after battle. IIP26 makes these
   // highlights actionable loot sources through the shared interaction authority.
   if(RevealHeld)
   {
    foreach(var corpse in g.world.Actors.Where(IsCorpseHighlightEligible))if(!ReferenceEquals(corpse,g.hoverCorpse))DrawCorpse(corpse);
    foreach(var pile in g.world.GroundPiles.Where(p=>p.Inventory.Items.Count>0&&g.world.Explored.Contains(p.Pos)))if(!ReferenceEquals(pile,g.hoverGround))DrawGround(pile,false);
   }
  }

  void DrawActorHover(Actor actor)
  {
   var color=actor.Enemy?Red:Teal;var bounds=g.ActorBounds(actor);var foot=g.Screen(actor.Position);
   g.GroundRing(foot,29*g.zoom,color);Bracket(Inflate(bounds,5),color);
   DrawLabel($"[{(actor.Enemy?"HOSTILE":"ALLY")}] {actor.Name}",new((int)foot.X,(int)(bounds.Y-7)),color);
  }

  void DrawCorpse(Actor actor)
  {
   var foot=g.Screen(actor.Position);var bounds=g.ActorBounds(actor);if(!bounds.Intersects(g.MapRect))return;
   Color color=Muted*.82f;g.GroundRing(foot,23*g.zoom,color);Bracket(Inflate(bounds,3),color);
   DrawLabel($"[CORPSE] {actor.Name}",new((int)foot.X,bounds.Y-4),color);
  }

  void DrawGround(GroundPile pile,bool hovered)
  {
   var center=g.Screen(pile.Pos);int size=(int)(42*g.zoom);var bounds=new Rectangle((int)center.X-size/2,(int)center.Y-size/2,size,size);if(!bounds.Intersects(g.MapRect))return;Color color=Gold*(hovered?1f:.82f);g.GroundRing(center,hovered?27*g.zoom:23*g.zoom,color);Bracket(Inflate(bounds,hovered?5:3),color);DrawLabel($"[GROUND] {pile.Inventory.Items.Count} stack{(pile.Inventory.Items.Count==1?"":"s")}",new((int)center.X,bounds.Y-6),color);
  }

  void DrawFeature(Feature feature,bool hovered)
  {
   var kind=Kind(feature);var color=ColorFor(feature,kind);var center=g.Screen(feature.Pos);int size=(int)(48*g.zoom);
   var bounds=new Rectangle((int)center.X-size/2,(int)center.Y-size/2,size,size);if(!bounds.Intersects(g.MapRect))return;
   float pulse=g.preferences.ReducedMotion?1f:hovered?1f:.78f+.12f*MathF.Sin(g.elapsed*4f+feature.Pos.X*.7f+feature.Pos.Y*.4f);
   g.GroundRing(center,hovered?29*g.zoom:25*g.zoom,color*pulse);Bracket(Inflate(bounds,hovered?6:3),color*pulse);
   DrawLabel($"[{Tag(feature,kind)}] {feature.Name}",new((int)center.X,bounds.Y-7),color);
  }

  HighlightKind Kind(Feature f)=>f.Kind.ToLowerInvariant() switch
  {
   "door"=>HighlightKind.Door,
   "chest" or "container"=>HighlightKind.Container,
   "switch" or "lever" or "button"=>HighlightKind.Switch,
   "exit" or "stairs" or "travel" or "transition"=>HighlightKind.Transition,
   "relic" or "pickup" or "ground" or "loot"=>HighlightKind.Pickup,
   "trap"=>HighlightKind.Trap,
   "npc"=>HighlightKind.Dialogue,
   "service"=>g.world.IsStoreFeature(f)?HighlightKind.Merchant:HighlightKind.Service,
   _=>HighlightKind.Interact
  };
  static string Tag(Feature f,HighlightKind kind)
  {
   if(f.Locked)return "LOCKED";
   return kind switch
   {
    HighlightKind.Door=>"DOOR",HighlightKind.Container=>"CONTAINER",HighlightKind.Switch=>"SWITCH",
    HighlightKind.Transition=>"EXIT",HighlightKind.Pickup=>"PICKUP",HighlightKind.Trap=>"TRAP",HighlightKind.Dialogue=>"TALK",HighlightKind.Merchant=>"STORE",HighlightKind.Service=>"SERVICE",_=>"INTERACT"
   };
  }
  static Color ColorFor(Feature f,HighlightKind kind)
  {
   if(f.Locked||kind==HighlightKind.Trap)return Red;
   if(kind is HighlightKind.Transition or HighlightKind.Dialogue or HighlightKind.Merchant or HighlightKind.Service)return Teal;
   return Gold;
  }

  void DrawLabel(string text,Point anchor,Color color)
  {
   int width=Math.Min(360,(int)g.Measure(text,.62f)+20),height=25;
   var r=new Rectangle(Math.Clamp(anchor.X-width/2,g.MapRect.Left+4,g.MapRect.Right-width-4),anchor.Y-height,width,height);
   for(int i=0;i<8&&labels.Any(x=>x.Intersects(r));i++)r.Y-=height+2;
   if(r.Y<g.MapRect.Top+4)
   {
    r.Y=Math.Clamp(anchor.Y+12,g.MapRect.Top+4,g.MapRect.Bottom-height-4);
    for(int i=0;i<8&&labels.Any(x=>x.Intersects(r));i++)r.Y+=height+2;
   }
   r.Y=Math.Clamp(r.Y,g.MapRect.Top+4,g.MapRect.Bottom-height-4);labels.Add(r);
   g.Box(r,new Color(11,16,19)*.95f,color*.88f);g.Text(text,r.X+9,r.Y+4,Bright,.62f);
  }

  void Bracket(Rectangle r,Color c)
  {
   const int n=9,t=2;
   g.Fill(new(r.Left,r.Top,n,t),c);g.Fill(new(r.Left,r.Top,t,n),c);
   g.Fill(new(r.Right-n,r.Top,n,t),c);g.Fill(new(r.Right-t,r.Top,t,n),c);
   g.Fill(new(r.Left,r.Bottom-t,n,t),c);g.Fill(new(r.Left,r.Bottom-n,t,n),c);
   g.Fill(new(r.Right-n,r.Bottom-t,n,t),c);g.Fill(new(r.Right-t,r.Bottom-n,t,n),c);
  }
  static Rectangle Inflate(Rectangle r,int amount)=>new(r.X-amount,r.Y-amount,r.Width+amount*2,r.Height+amount*2);
 }
}
