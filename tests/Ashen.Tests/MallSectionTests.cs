using Ashen.Core;
using System.Text.Json.Nodes;
static class MallSectionTests
{
 public static void Run(Action<bool,string> check)
 {
  CampaignDefinition Definition()=>CampaignLoader.Load(CampaignLoader.BundledMallPath);
  var c=Definition();var area=c.Areas.Single();var w=World.New(c);
  check(c.SchemaVersion==3&&area.Id=="area.mall.southwest"&&area.Zones.Count==7,"Phase 5 default starts in the real southwest section with seven named zones");
  var shops=area.Zones.Where(z=>z.Name.StartsWith("#")).ToArray();
  check(shops.Length==4&&shops.Select(z=>z.Source.AuditId).Order().SequenceEqual(new[]{"loc.lower.01","loc.lower.02","loc.lower.03","loc.lower.04"}),"Phase 5 four shops retain their audit identities and PDF references");
  var travel=shops.Single(z=>z.Source.AuditId=="loc.lower.01");var cafe=shops.Single(z=>z.Source.AuditId=="loc.lower.03");var paper=shops.Single(z=>z.Source.AuditId=="loc.lower.02");var gourmet=shops.Single(z=>z.Source.AuditId=="loc.lower.04");
  check(cafe.Y<travel.Y&&gourmet.Y<paper.Y&&travel.X<paper.X&&cafe.X<gourmet.X,"Phase 5 north/south and east/west shop arrangement matches the lower map");
  check(w.CampaignZoneAt(new(10,27))?.FloorStyle=="shelter"&&w.CampaignZoneAt(new(10,21))?.FloorStyle=="carpet","Phase 5 back office overrides travel-agency floor style");
  check(w.Party.All(a=>w.CanStand(a.Position,a))&&w.DiscoveredMapMarkers().All(m=>!m.Label.StartsWith("#")),"Phase 5 starts legally in vestibule without revealing shop markers");
  check(w.Blocked(new(10,22))&&!w.CanStand(new Cell(10,22).Center,w.Chosen,false)&&!w.Line(new Cell(9,22).Center,new Cell(14,22).Center),"Phase 5 desks block collision and line of sight");
  check(!w.Blocked(new(13,19))&&w.CanStand(new Cell(13,19).Center,w.Chosen,false),"Phase 5 decorative broken glass remains walkable");
  var arrivals=CampaignTravelRules.ArrivalCells(area.Floors,area.Features.Where(f=>f.Kind=="door").Select(f=>f.Position).Concat(CampaignGeometry.SolidCells(area)),[],area.EntryPoints[0].Position);
  check(arrivals.Take(6).Distinct().Count()==6&&arrivals.All(p=>!w.Blocked(p)),"Phase 5 west entry has six distinct legal cells despite closed doors and furniture");
  check(!w.SegmentClear(new Cell(9,22).Center,new Cell(14,22).Center,w.Chosen,false),"Phase 5 continuous movement cannot cross desk footprints");
  check(w.Path(w.Chosen,new Cell(14,16).Center,false).Count==0,"Phase 5 closed west doors stop entrance path");
  w.Explored=w.Floors.ToList();var entrance=w.Features.Single(f=>f.Id=="feature.mall.door.entrance");w.Chosen.Pos=new(4,16);
  check(w.Interact(entrance)&&!w.Blocked(entrance.Pos)&&w.Path(w.Chosen,new Cell(14,16).Center,false).Count>0,"Phase 5 real interaction opens vestibule and permits corridor path");
  foreach(var f in w.Features.Where(f=>f.Kind=="door"))f.Used=true;
  var walkable=w.Floors.Where(p=>!w.Blocked(p)).ToHashSet();var seen=new HashSet<Cell>{w.Chosen.Pos};var q=new Queue<Cell>();q.Enqueue(w.Chosen.Pos);
  while(q.TryDequeue(out var p))foreach(var n in p.Neighbors())if(walkable.Contains(n)&&seen.Add(n))q.Enqueue(n);
  check(seen.SetEquals(walkable),"Phase 5 all non-furniture floor cells belong to one reachable section with doors open");
  foreach(var z in shops.Append(area.Zones.Single(z=>z.FloorStyle=="shelter")))
  {
   var target=area.Floors.First(p=>z.Contains(p)&&!w.Blocked(p));
   check(w.Path(w.Chosen,target.Center,false).Count>0,"Phase 5 engine path reaches "+z.Name);
  }
  check(w.Features.All(f=>f.Pos.Neighbors().Any(p=>seen.Contains(p)&&p.Center.Distance(f.Pos.Center)<=1.25f)),"Phase 5 every interaction has an accessible adjacent standing cell");
  check(w.Features.Where(f=>f.Kind=="door").All(f=>{var p=f.Pos;return seen.Contains(new(p.X-1,p.Y))&&seen.Contains(new(p.X+1,p.Y))||seen.Contains(new(p.X,p.Y-1))&&seen.Contains(new(p.X,p.Y+1));}),"Phase 5 every doorway connects legal floor on both sides");
  var route=w.Path(w.Chosen,new Cell(14,28).Center,false);var origin=w.Chosen.Position;
  check(route.Count>0&&route.All(p=>{bool legal=w.SegmentClear(origin,p,w.Chosen,false);origin=p;return legal;}),"Phase 5 smoothed office route never cuts through walls or furniture");
  // Run the real movement loop through both storefront and back-office chokepoints.
  check(w.MoveTo(w.Chosen,new Cell(12,27).Center),"Phase 5 office move can be queued");
  for(int i=0;i<1800&&w.IsMoving(w.Chosen);i++)w.UpdateMovement(1f/60);
  check(!w.IsMoving(w.Chosen)&&w.Chosen.Position.Distance(new Cell(12,27).Center)<.05,"Phase 5 movement reaches the shelter through two doorways");
  w.Chosen.Pos=new(10,28);w.Reveal();var refuge=w.Features.Single(f=>f.Id=="feature.mall.shelter");
  check(w.Interact(refuge)&&w.HasDialogueFlag("flag.mall.shelter"),"Phase 5 shelter inspection sets its objective flag through the real event handler");
  w.Chosen.Pos=new(14,27);w.Reveal();var box=w.Features.Single(f=>f.Id=="feature.mall.supplies");
  check(w.TakeContainerItem(w.Chosen,box,0,1)&&box.Contents.Count("item.commodity.001")==2,"Phase 5 finite shelter supplies use ordinary container transfer");
  check(w.DiscoveredMapMarkers().Any(m=>m.Label=="Temporary Shelter")&&w.AreaMapMarkers.All(m=>!m.Id.StartsWith("crypt")),"Phase 5 room markers use campaign definitions and discovery");
  var lo=CampaignGeometry.ClampCamera(w.Floors,new(-100,-100),20,12);var hi=CampaignGeometry.ClampCamera(w.Floors,new(100,100),20,12);
  check(Math.Abs(lo.X-9.5)<.001&&Math.Abs(lo.Y-6.5)<.001&&Math.Abs(hi.X-35.5)<.001&&Math.Abs(hi.Y-25.5)<.001,"Phase 5 camera clamps extreme pans to padded section bounds");
  var small=CampaignGeometry.ClampCamera(new[]{new Cell(2,3),new Cell(3,4)},new(100,-100),30,20);
  check(small==new WorldPoint(3,4),"Phase 5 camera centres geometry smaller than viewport");
  bool bad=false;try{CampaignGeometry.ClampCamera(w.Floors,new(float.NaN,0),20,12);}catch(ArgumentOutOfRangeException){bad=true;}check(bad,"Phase 5 camera rejects non-finite input");
  void Reject(Action<CampaignDefinition> mutate,string label){var x=Definition();mutate(x);check(CampaignLoader.Validate(x).Count>0,"Phase 5 rejects "+label);}
  Reject(x=>x.SchemaVersion=2,"new geometry in an older schema");
  Reject(x=>x.Areas[0].Props[0].Width=int.MaxValue,"overflowing furniture bounds");
  Reject(x=>x.Areas[0].Props[0].Position=new(0,0),"furniture outside authored floor");
  Reject(x=>x.Areas[0].Props[0].Kind="execute-code","unknown furniture kind");
  Reject(x=>x.Areas[0].Props[0].Position=x.Areas[0].Actors[0].Position,"solid prop on party spawn");
  Reject(x=>x.Areas[0].Props[0].Position=x.Areas[0].Features[0].Position,"solid prop on doorway");
  Reject(x=>x.Areas[0].Props[0].Id=x.Areas[0].Zones[0].Id,"duplicate prop/zone IDs");
  Reject(x=>x.Areas[0].Zones[0].Source.PageStart=0,"missing zone provenance");
  Reject(x=>x.Areas[0].Zones[0].FloorStyle="crypt","unknown floor style");
  Reject(x=>x.Areas[0].Props=null!,"null props");
  var file=Path.Combine(Path.GetTempPath(),"zombie-mall-section-"+Guid.NewGuid()+".json");
  try
  {
   var note=w.AddMapNote(w.Chosen.Position,"Shelter supplies");w.Save(file);var loaded=World.Load(file);
   check(loaded.Version==World.CurrentSaveVersion&&loaded.CurrentCampaignArea!.Props.Count==31&&loaded.Blocked(new(10,22)),"Phase 5 save snapshot preserves static mall geometry and collision");
   check(loaded.Features.Single(f=>f.Id==entrance.Id).Used&&loaded.HasDialogueFlag("flag.mall.shelter")&&loaded.Features.Single(f=>f.Id==box.Id).Contents.Count("item.commodity.001")==2&&loaded.MapNotes.Any(n=>n.Id==note!.Id),"Phase 5 save/load retains doors, finite supplies, shelter inspection and map notes");
   var old=World.New(CampaignLoader.Load(CampaignLoader.BundledTravelPath));old.Save(file);var root=JsonNode.Parse(File.ReadAllText(file))!.AsObject();root["Version"]=17;File.WriteAllText(file,root.ToJsonString());var migrated=World.Load(file);
   check(migrated.Version==World.CurrentSaveVersion&&migrated.Campaign!.SchemaVersion==2&&!migrated.HasMallGeometry&&migrated.Campaign.Areas.Count==3,"Phase 5 v17 travel save migrates without replacing its content with the mall");
  }
  finally{if(File.Exists(file))File.Delete(file);}
 }
}
