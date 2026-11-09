using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;
namespace Ashen.Game;

// Presentation is deliberately external to save-owned campaign geometry. Older
// embedded campaigns get the same materials without any migration or new loot.
public sealed partial class CovenantGame
{
 sealed class EnvironmentArt
 {
  public int Version {get;set;}
  public Dictionary<string,string[]> Floors {get;set;}=[];
  public Dictionary<string,string> Props {get;set;}=[];
  public Dictionary<string,string> PropOverrides {get;set;}=[];
  public Dictionary<string,string> Features {get;set;}=[];
  public Dictionary<string,EnvironmentArea> Areas {get;set;}=[];
 }
 sealed class EnvironmentArea
 {
  public Dictionary<string,string[]> Floors {get;set;}=[];
  public Dictionary<string,string> Features {get;set;}=[];
  public List<EnvironmentDecal> Decals {get;set;}=[];
  public List<EnvironmentSign> Signs {get;set;}=[];
 }
 sealed class EnvironmentDecal
 {
  public string Texture {get;set;}="";
  public int X {get;set;} public int Y {get;set;}
  public int Width {get;set;}=1;public int Height {get;set;}=1;
 }
 sealed class EnvironmentSign
 {
  public string Label {get;set;}="";public string Theme {get;set;}="";
  public int X {get;set;} public int Y {get;set;}
  public int[]? Color {get;set;}
  public float MaxWidthTiles {get;set;}
 }
 EnvironmentArt environmentArt=new();
 void StageEnvironmentCapture()
 {
  title=false;
  if(scene.StartsWith("env33b-",StringComparison.Ordinal)){StageEnvironmentCaptureB();return;}
  if(captureSave!=null){camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);return;}
  if(scene=="env33-entrance")return;
  foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;
  Cell focus=scene switch{"env33-states"=>new(21,27),"env33-cafe"=>new(14,9),"env33-paper"=>new(32,22),"env33-gourmet"=>new(35,9),"env33-refuge"=>new(12,27),"env33-travel"=>new(14,22),_=>new(23,16)};
  int i=0;foreach(var a in world.Party)a.Pos=new(focus.X+i++,focus.Y);
  world.Reveal();camera=new(focus.X+.5f,focus.Y+.5f);
  if(scene is "env33-overview" or "env33-zoom"){world.Explored=world.Floors.ToList();zoom=scene=="env33-overview"?.56f:1.37f;camera=new(23,16);}
  if(scene=="env33-states")
  {
   var containers=world.Features.Where(f=>f.Id is "feature.mall.closet-box" or "feature.mall.equipment-cache").ToArray();
   if(containers.Length>0){containers[0].Used=true;containers[0].Contents.Items.Clear();containers[0].Gold=0;}
   if(containers.Length>1){containers[1].Used=true;containers[1].Locked=true;}
   world.Explored=world.Floors.ToList();zoom=.8f;camera=new(17,26);
  }
 }
 // Graphics-only fixtures. No staging path is called during normal play or save loading.
 void StageEnvironmentCaptureB()
 {
  if(captureSave!=null){camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);return;}
  string suffix=scene["env33b-".Length..];bool fog=suffix.StartsWith("fog",StringComparison.Ordinal);if(fog)suffix=suffix[3..];
  bool back=suffix.StartsWith("back",StringComparison.Ordinal);if(back)suffix=suffix[4..];
  bool zoomed=suffix.StartsWith("zoom",StringComparison.Ordinal);if(zoomed)suffix=suffix[4..];
  string target="area.lower."+suffix;
  if(!world.Campaign!.Areas.Any(a=>a.Id==target))throw new InvalidDataException("Unknown environment capture area: "+target);
  void ClearCaptureThreats(){foreach(var a in world.Actors.Where(a=>a.Enemy))a.Hp=0;world.StopAll();world.Order.Clear();world.Mode=Mode.Explore;world.ClearReactionInterrupts();}
  while(world.CurrentTravelDestinationId!=target)
  {
   var queue=new Queue<string>();var previous=new Dictionary<string,string>{{world.CurrentTravelDestinationId,""}};queue.Enqueue(world.CurrentTravelDestinationId);
   while(queue.Count>0&&!previous.ContainsKey(target))
   {
    var from=queue.Dequeue();foreach(var route in world.Campaign.Connections.Where(r=>r.FromAreaId==from&&r.RequiredFlag==""))if(previous.TryAdd(route.ToAreaId,from))queue.Enqueue(route.ToAreaId);
   }
   if(!previous.ContainsKey(target))throw new InvalidDataException("Capture area has no unlocked path");
   string next=target;while(previous[next]!=world.CurrentTravelDestinationId)next=previous[next];
   ClearCaptureThreats();foreach(var door in world.Features.Where(f=>f.Kind=="door"))door.Used=true;
   var connection=world.Campaign.Connections.First(r=>r.FromAreaId==world.CurrentTravelDestinationId&&r.ToAreaId==next);var exit=world.Features.Single(f=>f.Id==connection.ExitFeatureId);
   var cells=world.Floors.Where(c=>!world.Blocked(c)&&c.Distance(exit.Pos)<=2).OrderBy(c=>c.Distance(exit.Pos)).ToArray();int i=0;foreach(var actor in world.Party)actor.Pos=cells[i++];world.Reveal();
   if(!world.TravelCampaignTo(next))throw new InvalidOperationException(world.PreviewCampaignTravel(next).Reason);
  }
  ClearCaptureThreats();
  if(!fog)foreach(var door in world.Features.Where(f=>f.Kind=="door"))door.Used=true;
  var focus=back?new Cell(18,9):world.CurrentLowerMallSite!=null?new Cell(22,23):new Cell(21,16);
  var positions=world.Floors.Where(c=>!world.Blocked(c)&&c.Distance(focus)<=4).OrderBy(c=>c.Distance(focus)).ToArray();int n=0;foreach(var actor in world.Party)actor.Pos=positions[n++];
  world.Explored.Clear();world.Reveal();if(!fog)world.Explored=world.Floors.ToList();
  zoom=zoomed?1.37f:back?1:.8f;camera=new(focus.X+.5f,focus.Y+.5f);SelectAll();
 }
 bool DrawEnvironmentFeatureOverride(Feature feature,Rectangle bounds,float light)
 {
  if(!environmentArt.Areas.TryGetValue(world.CurrentTravelDestinationId,out var area)||area?.Features==null||!area.Features.TryGetValue(feature.Id,out var key)||!tex.TryGetValue(key,out var texture))return false;
  batch.Draw(texture,bounds,Color.White*light);return true;
 }

 void LoadEnvironmentArt()
 {
  var path=Path.Combine(AppContext.BaseDirectory,"Assets","environment33.json");
  if(!File.Exists(path))return; // Source and old asset packs remain usable.
  try
  {
   var candidate=JsonSerializer.Deserialize<EnvironmentArt>(File.ReadAllText(path),new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
   if(candidate?.Version==1&&candidate.Floors!=null&&candidate.Props!=null&&candidate.PropOverrides!=null&&candidate.Features!=null&&candidate.Areas!=null)environmentArt=candidate;
  }
  catch(Exception e) when(e is IOException or JsonException or NotSupportedException)
  {Console.Error.WriteLine("Environment art fallback: "+e.Message);}
 }
 Texture2D EnvironmentFloor(string style,Cell cell)
 {
  string[]? variants=null;
  if(environmentArt.Areas.TryGetValue(world.CurrentTravelDestinationId,out var area)&&area?.Floors!=null&&world.CampaignZoneAt(cell) is {} zone)area.Floors.TryGetValue(zone.Id,out variants);
  if(variants is not {Length:>0})environmentArt.Floors.TryGetValue(style,out variants);
  if(variants is {Length:>0})
  {
   // Stable across machines, reloads and camera movement; no per-frame RNG.
   uint hash=unchecked((uint)(cell.X*73856093)^(uint)(cell.Y*19349663));
   if(variants[hash%(uint)variants.Length] is {} key&&tex.TryGetValue(key,out var texture))return texture;
  }
  return tex["mall-"+(CampaignGeometry.Styles.Contains(style)?style:"concourse")];
 }
 Rectangle EnvironmentTileRect(Cell cell)
 {
  var a=Screen(new WorldPoint(cell.X,cell.Y));var b=Screen(new WorldPoint(cell.X+1,cell.Y+1));
  // Shared rounded edges avoid cracks and overlapping fog seams at fractional zoom.
  int x=(int)MathF.Round(a.X),y=(int)MathF.Round(a.Y);
  return new(x,y,Math.Max(1,(int)MathF.Round(b.X)-x),Math.Max(1,(int)MathF.Round(b.Y)-y));
 }
 bool DrawEnvironmentProp(CampaignProp prop,HashSet<Cell> explored,HashSet<Cell> visible)
 {
  if(!environmentArt.PropOverrides.TryGetValue(prop.Id,out var key)&&!environmentArt.Props.TryGetValue(prop.Kind,out key))return false;
  if(!tex.TryGetValue(key,out var texture))return false;
  DrawEnvironmentFootprint(texture,prop.Position.X,prop.Position.Y,prop.Width,prop.Height,explored,visible);
  if(world.ModernTacticalCombat&&prop.CoverBonus>0)
  {
   var first=prop.Cells().Where(explored.Contains).Take(1).ToArray();
   if(first.Length>0){var r=EnvironmentTileRect(first[0]);Text("C+"+prop.CoverBonus,r.X+3,r.Y+3,Teal*(visible.Contains(first[0])?1:.3f),.31f*zoom,true);}
  }
  return true;
 }
 void DrawEnvironmentFootprint(Texture2D texture,int x,int y,int width,int height,HashSet<Cell> explored,HashSet<Cell> visible)
 {
  if(width<=0||height<=0||width>World.Width||height>World.Height)return;
  for(int dy=0;dy<height;dy++)for(int dx=0;dx<width;dx++)
  {
   var cell=new Cell(x+dx,y+dy);
   if(!explored.Contains(cell)||!world.IsFloor(cell))continue;
   var dest=EnvironmentTileRect(cell);if(!dest.Intersects(MapRect))continue;
   int sx=dx*texture.Width/width,sy=dy*texture.Height/height;
   var source=new Rectangle(sx,sy,(dx+1)*texture.Width/width-sx,(dy+1)*texture.Height/height-sy);
   if(source.Width==0||source.Height==0)continue;
   batch.Draw(texture,dest,source,Color.White*(visible.Contains(cell)?1:.3f));
  }
 }
 void DrawEnvironmentDetails(HashSet<Cell> explored,HashSet<Cell> visible)
 {
  if(!environmentArt.Areas.TryGetValue(world.CurrentTravelDestinationId,out var area)||area?.Decals==null)return;
  foreach(var decal in area.Decals)
   if(decal!=null&&tex.TryGetValue(decal.Texture,out var texture))DrawEnvironmentFootprint(texture,decal.X,decal.Y,decal.Width,decal.Height,explored,visible);
 }
 void DrawEnvironmentWall(Rectangle tile,Cell cell,Cell neighbor,float light)
 {
  bool horizontal=neighbor.Y!=cell.Y;bool north=neighbor.Y<cell.Y,west=neighbor.X<cell.X;
  int thick=Math.Max(3,(int)(9*zoom));
  var r=horizontal?new Rectangle(tile.X,north?tile.Y:tile.Bottom-thick,tile.Width,thick):new Rectangle(west?tile.X:tile.Right-thick,tile.Y,thick,tile.Height);
  var style=world.CampaignZoneAt(cell)?.FloorStyle??"concourse";
  Color face=style switch{"cafe"=>new(117,91,65),"carpet"=>new(87,117,120),"shelter"=>new(89,117,98),"paper"=>new(150,135,104),"gourmet"=>new(119,137,114),_=>new(134,145,135)};
  Fill(r,new Color(28,40,41)*light);var inset=new Rectangle(r.X+1,r.Y+1,Math.Max(1,r.Width-2),Math.Max(1,r.Height-2));Fill(inset,face*light);
  int trim=Math.Max(1,(int)(2*zoom));
  Fill(horizontal?new(r.X+1,r.Y+1,Math.Max(1,r.Width-2),trim):new(r.X+1,r.Y+1,trim,Math.Max(1,r.Height-2)),new Color(194,197,170)*light);
  // Interior baseboard/shadow. All pixels stay in this discovered floor tile.
  if(horizontal)Fill(new(r.X,north?r.Bottom:r.Y-trim,r.Width,trim),new Color(28,41,39)*(.45f*light));
  else Fill(new(west?r.Right:r.X-trim,r.Y,trim,r.Height),new Color(28,41,39)*(.45f*light));
 }
 bool DrawEnvironmentFeature(string key,Rectangle r,float light)
 {
  if(!environmentArt.Features.TryGetValue(key,out var asset)||!tex.TryGetValue(asset,out var texture))return false;
  batch.Draw(texture,r,Color.White*light);return true;
 }
 void DrawEnvironmentSigns(HashSet<Cell> explored,HashSet<Cell> visible)
 {
  if(!environmentArt.Areas.TryGetValue(world.CurrentTravelDestinationId,out var area)||area?.Signs==null)return;
  foreach(var sign in area.Signs)
  {
   var cell=new Cell(sign.X,sign.Y);
   if(!explored.Contains(cell)||!world.IsFloor(cell)||string.IsNullOrWhiteSpace(sign.Label))continue;
   var tile=EnvironmentTileRect(cell);if(!tile.Intersects(MapRect)||zoom<.55f)continue;
   float light=visible.Contains(cell)?1:.3f;
   Color tone=sign.Theme switch{"cafe"=>new(77,59,44),"gourmet"=>new(61,80,59),"paper"=>new(109,86,59),"shelter"=>new(47,78,64),_=>new(44,74,81)};
   if(sign.Color is {Length:3} c)tone=new Color(Math.Clamp(c[0],0,255),Math.Clamp(c[1],0,255),Math.Clamp(c[2],0,255));
   // Labels sit above the doorway, never over a survivor's feet or the doorway click target.
   float scale=.5f*zoom;
   if(sign.MaxWidthTiles>0)scale*=Math.Min(1,(sign.MaxWidthTiles*54*zoom-18*zoom)/Math.Max(1,Measure(sign.Label,scale)));
   int w=(int)(Measure(sign.Label,scale)+18*zoom),h=Math.Max(12,(int)(24*zoom));
   var r=new Rectangle(tile.Center.X-w/2,tile.Y-h-3,w,h);
   Box(r,tone*light,new Color(158,173,144)*light);Text(sign.Label,r.X+9*zoom,r.Y+4*zoom,new Color(234,226,188)*light,scale);
  }
 }
}
