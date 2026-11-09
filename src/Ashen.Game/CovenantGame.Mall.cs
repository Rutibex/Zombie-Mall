using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 void ClampMallCamera()
 {
  if(!world.HasMallGeometry)return;
  var p=CampaignGeometry.ClampCamera(world.Floors,new(camera.X,camera.Y),MapRect.Width/(54*zoom),MapRect.Height/(54*zoom));camera=new(p.X,p.Y);
 }
 Texture2D MallFloorTexture(string style)
 {
  Color baseColor=style switch{"asphalt"=>new(55,62,65),"concrete"=>new(111,117,114),"carpet"=>new(83,110,119),"paper"=>new(178,170,142),"cafe"=>new(134,102,73),"gourmet"=>new(149,160,141),"shelter"=>new(89,117,105),"vestibule"=>new(89,107,114),_=>new(178,184,181)};
  const int size=96;var pixels=new Color[size*size];var rng=new Random(831);
  for(int y=0;y<size;y++)for(int x=0;x<size;x++)
  {
   int n=rng.Next(-3,4);bool carpet=style is "carpet" or "shelter";
   if(carpet){if((x+y)%4==0)n-=5;}
   else if(style=="cafe"){if(y%24<2||x%48<2)n-=22;}
   else if(x<2||y<2)n-=23;
   pixels[y*size+x]=new Color(Math.Clamp(baseColor.R+n,0,255),Math.Clamp(baseColor.G+n,0,255),Math.Clamp(baseColor.B+n,0,255));
  }
  var t=new Texture2D(GraphicsDevice,size,size);t.SetData(pixels);return t;
 }
 void DrawMallProps(HashSet<Cell> explored,HashSet<Cell> visible)
 {
  foreach(var p in world.CurrentCampaignArea!.Props)
  {
   if(DrawEnvironmentProp(p,explored,visible))continue;
   // Clip each tile to discovery; a long counter must not reveal its undiscovered end.
   foreach(var cell in p.Cells().Where(explored.Contains))
   {
    var origin=Screen(new WorldPoint(cell.X,cell.Y));int size=(int)(54*zoom);var r=new Rectangle((int)origin.X+4,(int)origin.Y+4,size-8,size-8);if(!r.Intersects(MapRect))continue;
    float light=visible.Contains(cell)?1:.3f;Color tone=p.Kind switch{"shelf"=>new(111,80,52),"counter"=>new(64,91,99),"cot"=>new(61,113,106),"desk"=>new(160,115,65),"table"=>new(151,108,63),"chair"=>new(104,74,54),"planter"=>new(75,119,78),"crate"=>new(142,125,88),_=>new(95,117,119)};
    if(p.Kind is "tire-rack" or "bowling-lane" or "railcar" or "sewer-channel" or "skate-ramp" or "helicopter" or "water-tank" or "generator" or "control-panel" or "billboard" or "duct" or "hydroponics" or "catwalk" or "scaffold" or "locker" or "satellite" or "elevator" or "barricade" or "pool" or "car" or "truck" or "dumpster" or "fuel-pump" or "bench" or "fryer")
    {
     var top=Screen(new WorldPoint(p.Position.X,p.Position.Y));var full=new Rectangle((int)top.X,(int)top.Y,(int)(54*zoom*p.Width),(int)(54*zoom*p.Height));var clipTile=new Rectangle((int)origin.X,(int)origin.Y,size,size);
     void Paint(Rectangle box,Color color){var cut=Rectangle.Intersect(box,clipTile);if(cut.Width>0&&cut.Height>0)Fill(cut,color*light);}
     Rectangle Part(float x,float y,float w,float h)=>new(full.X+(int)(full.Width*x),full.Y+(int)(full.Height*y),Math.Max(1,(int)(full.Width*w)),Math.Max(1,(int)(full.Height*h)));
     if(p.Kind=="helicopter")
     {
      Paint(Part(.38f,.15f,.25f,.65f),new Color(51,69,91));Paint(Part(.4f,.2f,.21f,.3f),new Color(99,174,193));Paint(Part(.45f,.66f,.1f,.29f),new Color(58,76,98));Paint(Part(.35f,.87f,.3f,.035f),new Color(222,186,76));Paint(Part(.02f,.48f,.96f,.025f),new Color(40,43,42));Paint(Part(.49f,.025f,.02f,.95f),new Color(40,43,42));Paint(Part(.3f,.19f,.02f,.65f),new Color(151,153,143));Paint(Part(.68f,.19f,.02f,.65f),new Color(151,153,143));Paint(Part(.39f,.59f,.23f,.05f),new Color(215,180,58));
     }
     else if(p.Kind is "car" or "truck")
     {Paint(Part(.04f,.2f,.92f,.62f),p.Kind=="truck"?new Color(146,143,124):new Color(98,116,129));Paint(Part(.24f,.23f,.46f,.55f),new Color(37,62,76));Paint(Part(.07f,.15f,.14f,.12f),new Color(20,23,24));Paint(Part(.77f,.15f,.14f,.12f),new Color(20,23,24));Paint(Part(.07f,.76f,.14f,.12f),new Color(20,23,24));Paint(Part(.77f,.76f,.14f,.12f),new Color(20,23,24));Paint(Part(.92f,.28f,.04f,.13f),new Color(211,201,141));}
     else if(p.Kind=="dumpster")
     {Paint(Part(.03f,.07f,.94f,.86f),new Color(47,82,67));Paint(Part(.06f,.12f,.88f,.35f),new Color(69,104,83));Paint(Part(.1f,.48f,.8f,.06f),new Color(25,40,33));}
     else if(p.Kind=="fuel-pump")
     {Paint(Part(.16f,.08f,.64f,.84f),new Color(148,59,47));Paint(Part(.23f,.16f,.5f,.32f),new Color(31,47,49));Paint(Part(.3f,.25f,.35f,.08f),new Color(161,189,146));Paint(Part(.82f,.23f,.06f,.64f),new Color(24,29,29));}
     else if(p.Kind=="fryer")
     {Paint(Part(.04f,.08f,.92f,.84f),new Color(128,135,132));Paint(Part(.15f,.2f,.7f,.58f),new Color(56,46,30));for(int k=0;k<4;k++)Paint(Part(.18f+k*.17f,.22f,.025f,.52f),new Color(163,143,88));}
     else if(p.Kind=="bench")
     {for(int k=0;k<3;k++)Paint(Part(.04f,.18f+k*.21f,.92f,.12f),new Color(116,91,62));}
     else if(p.Kind=="pool")
     {Paint(Part(.03f,.04f,.94f,.92f),new Color(173,181,173));Paint(Part(.1f,.13f,.8f,.74f),new Color(39,58,63));Paint(Part(.16f,.2f,.68f,.6f),new Color(61,72,65));for(int k=0;k<9;k++)Paint(Part(.2f+(k%3)*.2f,.25f+(k/3)*.18f,.12f,.08f),new Color(105,87,76));}
     else if(p.Kind=="satellite")
     {Paint(Part(.43f,.47f,.12f,.5f),new Color(63,69,72));Paint(Part(.08f,.08f,.84f,.62f),new Color(163,177,180));Paint(Part(.2f,.18f,.6f,.35f),new Color(114,137,144));Paint(Part(.46f,.02f,.05f,.6f),new Color(50,68,76));}
     else if(p.Kind=="elevator")
     {Paint(Part(.03f,.03f,.94f,.94f),new Color(43,47,47));Paint(Part(.12f,.12f,.76f,.76f),new Color(111,117,114));Paint(Part(.47f,.12f,.04f,.76f),new Color(41,49,49));Paint(Part(.87f,.32f,.09f,.25f),new Color(201,163,62));}
     else if(p.Kind=="locker")
     {Paint(Part(.05f,.05f,.9f,.9f),new Color(76,101,105));for(int k=0;k<4;k++){Paint(Part(.07f+k*.22f,.08f,.02f,.84f),new Color(36,56,59));Paint(Part(.17f+k*.22f,.4f,.035f,.12f),new Color(190,187,157));}}
     else if(p.Kind is "scaffold" or "barricade")
     {Paint(Part(.04f,.1f,.92f,.13f),new Color(128,104,72));Paint(Part(.04f,.76f,.92f,.13f),new Color(128,104,72));for(int k=0;k<4;k++)Paint(Part(.08f+k*.27f,.03f,.04f,.94f),new Color(100,114,116));}
     else if(p.Kind=="tire-rack")
     {Paint(Part(.03f,.05f,.94f,.9f),new Color(51,58,59));for(int k=0;k<4;k++){Paint(Part(.08f+k*.23f,.16f,.18f,.68f),new Color(20,25,26));Paint(Part(.12f+k*.23f,.32f,.1f,.36f),new Color(104,108,105));}}
     else if(p.Kind=="bowling-lane")
     {Paint(Part(0,0,1,1),new Color(91,76,49));Paint(Part(.1f,0,.8f,1),new Color(185,148,88));for(int k=0;k<4;k++)Paint(Part(.15f+k*.2f,0,.015f,1),new Color(130,102,61));Paint(Part(.43f,.05f,.14f,.08f),new Color(221,218,193));}
     else if(p.Kind=="railcar")
     {Paint(Part(0,.05f,1,.9f),new Color(65,57,48));Paint(Part(.04f,.12f,.92f,.76f),new Color(114,70,50));for(int k=0;k<5;k++)Paint(Part(.1f+k*.18f,.16f,.025f,.68f),new Color(73,47,36));Paint(Part(.4f,.2f,.2f,.6f),new Color(49,54,50));}
     else if(p.Kind=="sewer-channel")
     {Paint(Part(0,0,1,1),new Color(50,71,55));for(int k=0;k<4;k++)Paint(Part(.1f+k*.25f,.2f,.14f,.05f),new Color(89,112,77));}
     else if(p.Kind=="skate-ramp")
     {Paint(Part(0,0,1,1),new Color(105,112,110));for(int k=0;k<5;k++)Paint(Part(.1f+k*.18f,.12f,.025f,.76f),new Color(71,80,80));Paint(Part(.03f,.03f,.94f,.06f),new Color(161,171,166));}
     else if(p.Kind=="water-tank")
     {Paint(Part(.08f,.08f,.84f,.84f),new Color(89,116,126));Paint(Part(.15f,.17f,.7f,.6f),new Color(133,164,170));Paint(Part(.12f,.24f,.76f,.025f),new Color(52,76,86));Paint(Part(.12f,.65f,.76f,.025f),new Color(52,76,86));Paint(Part(.42f,.02f,.16f,.16f),new Color(54,74,82));}
     else if(p.Kind=="catwalk")
     {Paint(new(r.X,r.Y,r.Width,r.Height),new Color(63,72,74));for(int k=0;k<6;k++)Paint(new(r.X+3,r.Y+k*Math.Max(1,r.Height/6),r.Width-6,2),new Color(136,145,139));}
     else
     {
      Paint(new(r.X,r.Y,r.Width,r.Height),p.Kind=="hydroponics"?new Color(50,88,69):p.Kind=="billboard"?new Color(111,61,72):new Color(57,72,80));
      for(int k=0;k<4;k++)Paint(new(r.X+4+k*Math.Max(1,r.Width/4),r.Y+7,Math.Max(2,r.Width/8),Math.Max(3,r.Height-14)),p.Kind=="hydroponics"?new Color(92,144,72):p.Kind=="control-panel"?new Color(100,196,174):p.Kind=="billboard"?new Color(220,196,116):new Color(132,139,128));
     }
     continue;
    }
    if(p.Kind is "glass" or "spill" or "rubble")
    {
     for(int i=0;i<5;i++){var fragment=new Rectangle(r.X+((i*13+3)%Math.Max(1,r.Width-6)),r.Y+((i*9+7)%Math.Max(1,r.Height-6)),Math.Max(3,size/9),Math.Max(2,size/15));Fill(fragment,(p.Kind=="glass"?new Color(176,221,222):new Color(79,73,54))*light);}continue;
    }
    if(p.Kind=="poster"){Box(new(r.X,r.Y,r.Width,Math.Max(8,size/3)),new Color(203,178,112)*light,new Color(230,223,200)*light);Fill(new(r.X+4,r.Y+3,r.Width/3,Math.Max(3,size/5)),Teal*light);continue;}
    if(p.Kind=="chair"){r=new(r.X+size/5,r.Y+size/5,r.Width-size/3,r.Height-size/3);}
    Fill(new(r.X+3,r.Y+5,r.Width,r.Height),Color.Black*(.35f*light));
    if(p.Kind is "table" or "planter"){batch.Draw(circle,r,tone*light);batch.Draw(circle,new Rectangle(r.X+4,r.Y+4,r.Width-8,r.Height-8),(p.Kind=="planter"?new Color(65,99,52):new Color(182,137,83))*light);}
    else
    {
     Box(r,tone*light,new Color(46,55,57)*light);Fill(new(r.X+2,r.Y+2,r.Width-4,3),new Color(210,208,187)*(.65f*light));
     if(p.Kind=="desk"){Box(new(r.X+size/5,r.Y+size/5,size/3,size/4),new Color(216,219,198)*light,new Color(51,68,69)*light);}
     if(p.Kind=="shelf")for(int i=0;i<4;i++){int x=r.X+4+i*Math.Max(1,(r.Width-8)/4);Fill(new(x,r.Y+6,Math.Max(3,size/8),Math.Max(4,r.Height-13)),(i%2==0?new Color(161,169,129):new Color(193,138,97))*light);}
     if(p.Kind=="cot"){Fill(new(r.X+4,r.Y+4,r.Width-8,r.Height-8),new Color(92,142,128)*light);if(cell.X==p.Position.X)Fill(new(r.X+5,r.Y+6,r.Width/2,r.Height-12),new Color(197,207,181)*light);}
     if(p.Kind=="crate"){Line(new(r.X+3,r.Y+4),new(r.Right-3,r.Bottom-4),new Color(99,85,61)*light,2);Line(new(r.Right-3,r.Y+4),new(r.X+3,r.Bottom-4),new Color(99,85,61)*light,2);}
    }
    if(world.ModernTacticalCombat&&p.CoverBonus>0&&cell==p.Cells().First(explored.Contains)){Text("C+"+p.CoverBonus,r.X+3,r.Y+3,Teal*light,.31f*zoom,true);}
   }
  }
 }
 bool DrawMallFeature(Feature f,V v,float light)
 {
  if(DrawRecruitFeature(f,v,light))return true;
  int s=(int)(44*zoom);var r=new Rectangle((int)v.X-s/2,(int)v.Y-s/2,s,s);
  if(f.Kind=="door")
  {
   bool horizontal=world.IsFloor(new(f.Pos.X,f.Pos.Y-1))&&world.IsFloor(new(f.Pos.X,f.Pos.Y+1));
   var bar=horizontal?new Rectangle(r.X,(int)v.Y-5,r.Width,10):new Rectangle((int)v.X-5,r.Y,10,r.Height);
   if(!f.Used)
   {
    Box(bar,new Color(63,105,112)*light,new Color(184,202,187)*light);
    if(horizontal){Fill(new(bar.X+5,bar.Y+2,Math.Max(1,bar.Width/3),2),new Color(160,205,196)*light);Fill(new(bar.Center.X,bar.Y,3,bar.Height),new Color(210,211,177)*light);}
    else {Fill(new(bar.X+2,bar.Y+5,2,Math.Max(1,bar.Height/3)),new Color(160,205,196)*light);Fill(new(bar.X,bar.Center.Y,bar.Width,3),new Color(210,211,177)*light);}
   }
   else {Fill(horizontal?new(r.X,r.Y,6,r.Height):new(r.X,r.Y,r.Width,6),new Color(65,109,113)*light);}
   if(world.IsEngagedBarricade(f))
   {
    for(int i=0;i<3;i++)Box(new(r.X,r.Y+6+i*Math.Max(4,r.Height/4),r.Width,5),new Color(157,111,62)*light,Gold*light);
    Text("BARRIER "+world.InfrastructureState.BarricadeHp+" HP",v.X-38*zoom,v.Y-43*zoom,Gold*light,.35f*zoom,true);
   }
   return true;
  }
  if(DrawEnvironmentFeatureOverride(f,r,light))return true;
  if(f.Kind=="container")
  {
   if(world.IsSecuringSite(f)){Box(new(r.X-4,r.Y-4,r.Width+8,r.Height+8),new Color(36,74,65)*light,Teal*light);Text("STOCKPILE",v.X-33*zoom,v.Y-42*zoom,Teal*light,.42f*zoom);}
   if(world.IsSecuringSite(f)&&world.InfrastructureEnabled&&world.InfrastructureState.GeneratorInstalled)
   {
    var lamp=new Rectangle(r.Right+4,r.Y+3,13,20);Box(lamp,world.ShelterPowered?new Color(210,192,100):new Color(54,62,56),Gold*light);Text(world.ShelterPowered?"POWER ON":"POWER OFF",v.X+25*zoom,v.Y+30*zoom,world.ShelterPowered?Teal:Muted,.31f*zoom);
   }
   if(!DrawEnvironmentFeature(world.IsSecuringSite(f)?"stockpile":f.Id=="feature.mall.workbench"?"workbench":f.Used&&!f.Locked&&!f.DetectedTrap&&f.Contents.Items.Count==0&&f.Gold==0?"empty":"container",r,light)){Box(new(r.X+5,r.Y+8,r.Width-10,r.Height-16),(f.Used?new Color(88,88,70):new Color(176,152,93))*light,new Color(60,72,68)*light);Fill(new((int)v.X-3,r.Y+8,6,r.Height-16),new Color(212,200,158)*light);}
   if(!world.IsSecuringSite(f)){var status=f.Locked?"LOCKED":f.DetectedTrap?"TRAPPED":f.Used?(f.Contents.Items.Count==0&&f.Gold==0?"EMPTY":"OPENED"):"";if(status!="")Text(status,r.X,r.Bottom,(f.Locked||f.DetectedTrap?Gold:Muted)*light,.3f*zoom);}return true;
  }
  if(f.Kind=="campaign-event"&&DrawEnvironmentFeature(f.Id=="feature.mall.directory"?"directory":"notice",r,light))return true;
  if(f.Kind=="horde")
  {
   if(world.Campaign!.Hordes.Any(h=>h.TriggerFeatureId==f.Id&&world.IsDailyAttack(h.Id)))return true;
   var gate=new Rectangle(r.X+8,r.Y-3,r.Width-16,r.Height+6);Box(gate,new Color(91,78,65)*light,(f.Used?Red:Gold)*light);for(int i=0;i<5;i++)Fill(new(gate.X+2,gate.Y+4+i*Math.Max(3,gate.Height/6),gate.Width-4,2),new Color(43,46,45)*light);Text("HORDE",v.X-20*zoom,v.Y-39*zoom,Gold*light,.4f*zoom,true);return true;
  }
  if(f.Kind is "campaign-event" or "debrief" or "quest-board" or "encounter" or "location")
  {
   if(DrawEnvironmentFeature("notice",r,light))return true;
   Box(new(r.X+9,r.Y+4,r.Width-18,r.Height-8),new Color(201,213,196)*light,new Color(57,83,87)*light);for(int i=0;i<3;i++)Fill(new(r.X+13,r.Y+11+i*6,r.Width-26,2),new Color(64,102,109)*light);return true;
  }
  return false;
 }
 void DrawMallSigns(HashSet<Cell> explored,HashSet<Cell> visible)
 {
  if(environmentArt.Areas.TryGetValue(world.CurrentTravelDestinationId,out var art)&&art?.Signs is {Count:>0})return;
  foreach(var z in world.CurrentCampaignArea!.Zones.Where(z=>z.Name.StartsWith("#")))
  {
   int doorX=z.X==8?13:32;int doorY=z.Y<14?13:18;var door=new Cell(doorX,doorY);if(!explored.Contains(door))continue;
   var p=Screen(new WorldPoint(z.X+z.Width/2f,z.Y<14?12.65f:19.15f));float light=visible.Contains(door)?1:.35f;float font=.47f*zoom;float w=Measure(z.Name,font)+20;
   Box(new((int)(p.X-w/2),(int)p.Y-10,(int)w,23),new Color(25,51,59)*light,new Color(110,146,148)*light);Text(z.Name,p.X-w/2+10,p.Y-5,new Color(220,222,191)*light,font);
  }
 }
 void StageMallCapture()
 {
  if(scene=="mall-title"){title=true;return;}
  title=false;
  if(scene=="mall-entrance")return;
  foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;
  // Screenshot staging changes only this disposable world, never authored starts or saved journeys.
  Cell focus=scene switch{"mall-travel"=>new(14,22),"mall-paper"=>new(32,21),"mall-cafe"=>new(14,10),"mall-gourmet"=>new(34,11),"mall-shelter"=>new(12,27),_=>new(23,16)};
  int i=0;foreach(var a in world.Party)a.Pos=new(focus.X+i++,focus.Y);world.Reveal();
  if(scene=="mall-map"||scene=="mall-overview")world.Explored=world.Floors.ToList();
  camera=new(focus.X+.5f,focus.Y+.5f);if(scene=="mall-overview"){zoom=.38f;camera=new(23,16);}
  if(scene=="mall-map")ui.Screens.Navigate(UiScreenId.Map);
 }
}
