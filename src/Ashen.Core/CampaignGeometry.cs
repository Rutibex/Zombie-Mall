using System.Text.Json.Serialization;
namespace Ashen.Core;

// Static, save-owned spatial content. Rectangles use tile coordinates (five feet per tile).
public sealed class CampaignZone
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public int X {get;set;} public int Y {get;set;} public int Width {get;set;} public int Height {get;set;}
 public string FloorStyle {get;set;}="concourse";
 public bool Contains(Cell c)=>c.X>=X&&c.X<X+Width&&c.Y>=Y&&c.Y<Y+Height;
}
public sealed class CampaignProp
{
 public string Id {get;set;}="";
 public string Kind {get;set;}="desk";
 public Cell Position {get;set;}
 public int Width {get;set;}=1;
 public int Height {get;set;}=1;
 public bool BlocksMovement {get;set;}=true;
 public int CoverBonus {get;set;}
 public bool BlocksSight {get;set;}=true;
 public IEnumerable<Cell> Cells(){for(int y=Position.Y;y<Position.Y+Height;y++)for(int x=Position.X;x<Position.X+Width;x++)yield return new(x,y);}
}
public static class CampaignGeometry
{
 public static readonly IReadOnlySet<string> Styles=new HashSet<string>{"asphalt","concrete","concourse","vestibule","carpet","paper","cafe","gourmet","shelter"};
 public static readonly IReadOnlySet<string> PropKinds=new HashSet<string>{"tire-rack","bowling-lane","railcar","sewer-channel","skate-ramp","desk","shelf","counter","table","chair","planter","cot","crate","glass","spill","poster","rubble","helicopter","water-tank","generator","control-panel","billboard","duct","hydroponics","catwalk","scaffold","locker","satellite","elevator","barricade","pool","car","truck","dumpster","fuel-pump","bench","fryer"};
 public static bool ValidRect(int x,int y,int w,int h)=>x>=0&&y>=0&&w>0&&h>0&&w<=World.Width&&h<=World.Height&&x<=World.Width-w&&y<=World.Height-h;
 public static IEnumerable<Cell> SolidCells(CampaignArea area)=>area.Props.Where(p=>p.BlocksMovement).SelectMany(p=>p.Cells());
 // Clamp each axis independently; a viewport wider than the section is centred on it.
 public static WorldPoint ClampCamera(IEnumerable<Cell> floors,WorldPoint camera,float viewportWidth,float viewportHeight,float padding=1.5f)
 {
  if(!float.IsFinite(camera.X)||!float.IsFinite(camera.Y)||!float.IsFinite(viewportWidth)||!float.IsFinite(viewportHeight)||viewportWidth<=0||viewportHeight<=0||!float.IsFinite(padding)||padding<0)throw new ArgumentOutOfRangeException(nameof(camera));
  var cells=floors.ToArray();if(cells.Length==0)return camera;
  float Axis(float value,float min,float max,float size){float lo=min-padding+size/2,hi=max+padding-size/2;return lo>hi?(min+max)/2:Math.Clamp(value,lo,hi);}
  return new(Axis(camera.X,cells.Min(c=>c.X),cells.Max(c=>c.X)+1,viewportWidth),Axis(camera.Y,cells.Min(c=>c.Y),cells.Max(c=>c.Y)+1,viewportHeight));
 }
}
public sealed partial class World
{
 [JsonIgnore] public CampaignArea? CurrentCampaignArea=>Campaign?.Areas.FirstOrDefault(a=>a.Id==CurrentTravelDestinationId);
 [JsonIgnore] public bool HasMallGeometry=>CurrentCampaignArea?.Zones.Count>0;
 // Cache keyed by definition and area; definitions are immutable for a running journey.
 CampaignArea? solidArea;HashSet<Cell>? solidCells;HashSet<Cell>? sightCells;
 bool CampaignPropBlocks(Cell p)
 {
  var area=CurrentCampaignArea;if(area==null)return false;
  if(!ReferenceEquals(area,solidArea)){solidArea=area;solidCells=CampaignGeometry.SolidCells(area).ToHashSet();sightCells=area.Props.Where(p=>p.BlocksMovement&&p.BlocksSight&&p.CoverBonus==0).SelectMany(p=>p.Cells()).ToHashSet();}
  return solidCells!.Contains(p);
 }
 bool CampaignSightPropBlocks(Cell p){CampaignPropBlocks(p);return sightCells?.Contains(p)==true;}
 public CampaignZone? CampaignZoneAt(Cell p)=>CurrentCampaignArea?.Zones.LastOrDefault(z=>z.Contains(p));
 IReadOnlyList<MapMarkerDefinition> CampaignMapMarkers()
 {
  var area=CurrentCampaignArea;if(area==null)return [];
  if(area.Zones.Count==0)return area.Features.Select(f=>new MapMarkerDefinition{Id=f.Id,Label=f.Name,Position=f.Position.Center,FeatureName=f.Name,Kind=f.Kind=="transition"?MapMarkerKind.Exit:MapMarkerKind.Location}).ToArray();
  return area.Zones.Select(z=>new MapMarkerDefinition{Id=z.Id,Label=z.Name,Description=z.Source.Note,Position=area.Floors.Where(z.Contains).OrderBy(p=>Math.Abs(p.X-(z.X+z.Width/2))+Math.Abs(p.Y-(z.Y+z.Height/2))).First().Center,Kind=z.FloorStyle=="shelter"?MapMarkerKind.Service:MapMarkerKind.Location}).ToArray();
 }
}
