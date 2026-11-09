using System.Text.Json.Serialization;
namespace Ashen.Core;

/// <summary>Continuous floor coordinates: one unit is the width of a stone floor tile.</summary>
public readonly record struct WorldPoint(float X,float Y)
{
 public override string ToString()=>$"({X:0.##}, {Y:0.##})";
 [JsonIgnore] public Cell Cell=>new((int)MathF.Floor(X),(int)MathF.Floor(Y));
 public float Distance(WorldPoint b)=>MathF.Sqrt((X-b.X)*(X-b.X)+(Y-b.Y)*(Y-b.Y));
 public static WorldPoint operator +(WorldPoint a,WorldPoint b)=>new(a.X+b.X,a.Y+b.Y);
 public static WorldPoint operator -(WorldPoint a,WorldPoint b)=>new(a.X-b.X,a.Y-b.Y);
 public static WorldPoint operator *(WorldPoint a,float s)=>new(a.X*s,a.Y*s);
 public static implicit operator WorldPoint(Cell c)=>c.Center;
}

public sealed partial class World
{
 public const float BodyRadius=.22f, WalkSpeed=3.4f;
 float visionClock;
 readonly Dictionary<string,float> blockedTime=[];
 readonly HashSet<string> enemyPlans=[];
 public bool IsMoving(Actor actor)=>Routes.TryGetValue(actor.Id,out var route)&&route.Count>0;
 public void Stop(Actor actor){Routes.Remove(actor.Id);pendingAttacks.Remove(actor.Id);pendingInteractions.Remove(actor.Id);ClearPendingLoot(actor.Id);blockedTime.Remove(actor.Id);EndMovementReactionWindow(actor);actor.Motion=0;}
 public void StopAll(){CancelAllFormationTravel();Routes.Clear();pendingAttacks.Clear();pendingInteractions.Clear();ClearAllPendingLoot();blockedTime.Clear();enemyPlans.Clear();opportunityOffered.Clear();foreach(var a in Actors)a.Motion=0;}
 public Actor? At(WorldPoint point,float radius=.4f)=>Actors.Where(a=>a.Alive&&a.Position.Distance(point)<=radius).OrderBy(a=>a.Position.Distance(point)).FirstOrDefault();

 // Fallen bodies persist on floor even when a door is subsequently secured over their position.
 bool SavedActorCanStand(Actor actor)
 {
  if(actor.Alive)return CanStand(actor.Position,actor,false);
  var geometry=new World{Campaign=Campaign,CurrentTravelDestinationId=CurrentTravelDestinationId,Floors=Floors,Features=Features.Where(f=>f.Kind!="door").ToList()};
  return geometry.CanStand(actor.Position,actor,false);
 }
 public bool CanStand(WorldPoint p,Actor? actor=null,bool occupied=true)
 {
  if(!float.IsFinite(p.X)||!float.IsFinite(p.Y)||Roof29CrossingBlocked(p))return false;
  for(int y=(int)MathF.Floor(p.Y-BodyRadius);y<=(int)MathF.Floor(p.Y+BodyRadius);y++)
  for(int x=(int)MathF.Floor(p.X-BodyRadius);x<=(int)MathF.Floor(p.X+BodyRadius);x++)
  {
   if(!Blocked(new(x,y)))continue;
   float dx=p.X-Math.Clamp(p.X,x,x+1),dy=p.Y-Math.Clamp(p.Y,y,y+1);
   if(dx*dx+dy*dy<BodyRadius*BodyRadius-.000001f)return false;
  }
  return !occupied||!Actors.Any(a=>a!=actor&&a.Alive&&a.Position.Distance(p)<BodyRadius*2-.001f);
 }
 public bool Line(WorldPoint from,WorldPoint to)
 {
  int steps=Math.Max(1,(int)MathF.Ceiling(from.Distance(to)/.06f));
  for(int i=0;i<=steps;i++)if(SightBlocked((from+(to-from)*(i/(float)steps)).Cell))return false;
  return true;
 }
 public bool SegmentClear(WorldPoint from,WorldPoint to,Actor? actor=null,bool occupied=true)
 {
  int steps=Math.Max(1,(int)MathF.Ceiling(from.Distance(to)/.06f));
  for(int i=1;i<=steps;i++)
  {
   var point=from+(to-from)*(i/(float)steps);if(CanStand(point,actor,occupied))continue;
   // A recovered casualty may stand up under an ally who crossed the fallen body.
   // Permit only separation from existing overlaps; retain scenery and new-body collision.
   if(!EquipmentEnabled||!occupied||actor==null||!CanStand(point,actor,false))return false;
   foreach(var other in Actors.Where(a=>a!=actor&&a.Alive))
   {float distance=point.Distance(other.Position),original=from.Distance(other.Position);if(distance<BodyRadius*2-.001f&&(original>=BodyRadius*2-.001f||distance<original-.000001f))return false;}
  }
  return true;
 }
 static IEnumerable<Cell> Neighbors8(Cell p)
 {for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)if(x!=0||y!=0)yield return new(p.X+x,p.Y+y);}

 /// <summary>Eight-way A* is only a navigation aid. Visibility smoothing removes cell-center waypoints;
 /// the first and last point are exact, unsnapped world coordinates.</summary>
 public List<WorldPoint> Path(Actor a,WorldPoint goal,bool occupied=true)
 {
  if(a.Position.Distance(goal)<.002f||!CanStand(goal,a,occupied))return [];
  if(SegmentClear(a.Position,goal,a,occupied))return [goal];
  Cell start=a.Pos,end=goal.Cell;var queue=new PriorityQueue<Cell,float>();
  var cost=new Dictionary<Cell,float>{{start,0}};var came=new Dictionary<Cell,Cell>();queue.Enqueue(start,0);
  bool found=false;
  while(queue.TryDequeue(out var cell,out _))
  {
   WorldPoint current=cell==start?a.Position:cell==end?goal:cell.Center;
   if(cell==end&&SegmentClear(current,goal,a,occupied)){found=true;break;}
   foreach(var next in Neighbors8(cell))
   {
    WorldPoint nextPoint=next==end?goal:next.Center;
    if(!CanStand(nextPoint,a,occupied)||!SegmentClear(current,nextPoint,a,occupied))continue;
    float candidate=cost[cell]+current.Distance(nextPoint);
    if(cost.TryGetValue(next,out float existing)&&existing<=candidate+.0001f)continue;
    cost[next]=candidate;came[next]=cell;queue.Enqueue(next,candidate+nextPoint.Distance(goal));
   }
  }
  if(!found)return [];
  var raw=new List<WorldPoint>{goal};for(var p=end;p!=start;){p=came[p];if(p!=start)raw.Add(p.Center);}raw.Reverse();
  var smooth=new List<WorldPoint>();var origin=a.Position;int index=0;
  while(index<raw.Count){int far=raw.Count-1;while(far>index&&!SegmentClear(origin,raw[far],a,occupied))far--;smooth.Add(raw[far]);origin=raw[far];index=far+1;}
  return smooth;
 }
 public static float PathLength(WorldPoint start,IEnumerable<WorldPoint> path)
 {float length=0;foreach(var point in path){length+=start.Distance(point);start=point;}return length;}
 public bool MoveTo(Actor a,WorldPoint goal)
 {
  if(!a.Alive||ReactionInterruptActive||Mode is Mode.Defeat or Mode.Victory||Mode==Mode.Combat&&Active!=a)return false;
  if(!a.Enemy&&!Explored.Contains(goal.Cell))return false;
  var path=Path(a,goal);if(path.Count==0)return false;
  if(Mode==Mode.Combat&&PathLength(a.Position,path)>a.Move+.001f){LogSystem("Not enough movement distance remaining.",true);return false;}
  pendingAttacks.Remove(a.Id);pendingInteractions.Remove(a.Id);ClearPendingLoot(a.Id);BeginMovementReactionWindow(a);Routes[a.Id]=new(path);blockedTime.Remove(a.Id);return true;
 }
 public void MoveParty(WorldPoint goal,bool together)=>MoveSelection(goal,together?Party.Select(a=>a.Id):[Chosen.Id]);

 /// <summary>Fixed maximum substeps keep collision sweeps and travelled distance frame-rate independent.</summary>
 public void UpdateMovement(float dt)
 {
  if(!float.IsFinite(dt)||dt<=0||Mode is Mode.Defeat or Mode.Victory||ReactionInterruptActive)return;
  TickLowerMall();AdvanceEncounterPlans();SyncContracts();UpdateEquipmentTime();TriggerEquipmentMines();
  UpdateSurvivorEffects(Math.Min(dt,.25f));
  ObserveTensionOutcomes();DispatchDailyAttack();MaintainDailyAttackOrders();
  float remaining=Math.Min(dt,.25f);
  while(remaining>.00001f)
  {
   float step=Math.Min(remaining,1f/60);remaining-=step;
   UpdateFormationTravel(step);
   foreach(var a in Actors.ToArray())
   {
    a.Motion=0;
    if(!a.Alive||LowerMallEnabled&&(a.Character.HasCondition("stunned")||a.Character.HasCondition("book-grappled")||LowerTraits(a).Contains("rooted"))||Mode==Mode.Combat&&Active!=a||!Routes.TryGetValue(a.Id,out var route)||route.Count==0)continue;
    float travel=WalkSpeed*step*(a.UsesDailySurvival?Math.Min(1,a.EffectiveSpeedFeet/(float)Math.Max(1,a.Character.SpeedFeet)):1);if(Mode==Mode.Combat)travel=Math.Min(travel,a.Move);
    while(travel>.00001f&&route.Count>0)
    {
     var target=route.Peek();float length=a.Position.Distance(target);
     if(length<.00001f){route.Dequeue();continue;}
     float distance=Math.Min(travel,length);var before=a.Position;var next=a.Position+(target-a.Position)*(distance/length);
     // Opportunity attacks interrupt before the mover leaves reach. Ask-mode reactions
     // suspend this exact substep; resolving the prompt resumes the unchanged route.
     if(Mode==Mode.Combat&&TriggerOpportunityBeforeStep(a,next)){a.Motion=0;return;}
     if(!a.Alive){Stop(a);return;}
     if(!SegmentClear(a.Position,next,a))
     {
      blockedTime[a.Id]=blockedTime.GetValueOrDefault(a.Id)+step;
      if(blockedTime[a.Id]>.3f)
      {
       var replacement=Path(a,route.Last());
       if(replacement.Count>0&&(Mode!=Mode.Combat||PathLength(a.Position,replacement)<=a.Move+.001f)){Routes[a.Id]=new(replacement);blockedTime[a.Id]=0;}
       else if(blockedTime[a.Id]>1.5f){Stop(a);LogSystem($"{a.Name}'s path is blocked.",true);}
      }
      break;
     }
     a.Face(next);a.Position=next;a.Motion=1;a.WalkDistance+=distance;a.Statistics.DistanceTravelledFeet+=distance*5f;travel-=distance;
     if(Mode==Mode.Combat)a.Economy.SpendMovement(distance*5f);
     blockedTime.Remove(a.Id);if(distance>=length-.00001f)route.Dequeue();
     // A readied attack fires after the triggering hostile has actually entered range.
     if(Mode==Mode.Combat&&TriggerReadiedAfterStep(a,before)){a.Motion=0;return;}
     if(!a.Alive){Stop(a);return;}
    }
    if(route.Count==0||Mode==Mode.Combat&&a.Move<.00001f){Routes.Remove(a.Id);EndMovementReactionWindow(a);}
   }
   CompleteInteractions();CompleteLootInteractions();CompleteAttacks();UpdateLocalAwareness(step);
   visionClock+=step;
   if(visionClock>=.08f){visionClock=0;Reveal();CheckEncounter();}
  }
 }
 // Kept as a deterministic compatibility step for tools/tests; the game uses UpdateMovement(dt).
 public void ExploreTick()=>UpdateMovement(.16f);
 public void EnemyTurn()
 {
  if(Active is not {Enemy:true} e||IsMoving(e))return;
  if(LowerMallEnabled&&e.Character.HasCondition("stunned")){EndTurn();return;}
  if(e.Character.HasCondition("turned-undead")){e.Character.SetCondition("turned-undead","Turned",false);Message(MessageCategory.Combat,$"{e.Name} recoils from Channel Divinity and loses its turn.");EndTurn();return;}
  if(LocalAwarenessEnabled&&RunNoiseEnemyTurn(e))return;
  var visibleTargets=Party.Where(a=>a.Alive&&!a.Character.HasCondition("hidden")&&(!LocalAwarenessEnabled||CanEnemySee(e,a))).ToList();if(visibleTargets.Count==0&&!LocalAwarenessEnabled)visibleTargets=Party.Where(a=>a.Alive).ToList();
  var target=visibleTargets.Where(a=>e.Position.Distance(a.Position)<=e.Range+.001f&&Line(e.Position,a.Position)).OrderBy(a=>a.Hp).FirstOrDefault();
  if(target!=null){Attack(e,target,AttackKind.Basic);enemyPlans.Remove(e.Id);if(ReactionInterruptActive)return;if(Mode==Mode.Combat&&!e.Economy.CanAttack)EndTurn();return;}
  if(!enemyPlans.Add(e.Id)){enemyPlans.Remove(e.Id);EndTurn();return;}
  var candidates=new List<WorldPoint>();
  foreach(var p in visibleTargets)
  {
   for(int i=0;i<16;i++){float angle=i*MathF.Tau/16;candidates.Add(p.Position+new WorldPoint(MathF.Cos(angle),MathF.Sin(angle))*(e.Range-.12f));}
   candidates.AddRange(Floors.Select(c=>c.Center).Where(c=>c.Distance(p.Position)<=e.Range&&c.Distance(p.Position)>.5f));
  }
  foreach(var goal in candidates.Distinct().Where(c=>CanStand(c,e)&&Party.Any(p=>p.Alive&&p.Position.Distance(c)<=e.Range&&Line(c,p.Position))).OrderBy(c=>c.Distance(e.Position)))
  {
   var path=Path(e,goal);if(path.Count==0)continue;
   var trimmed=new List<WorldPoint>();var origin=e.Position;float budget=e.Move;
   foreach(var waypoint in path){float distance=origin.Distance(waypoint);if(distance<=budget){trimmed.Add(waypoint);budget-=distance;origin=waypoint;}else{if(budget>.001f)trimmed.Add(origin+(waypoint-origin)*(budget/distance));break;}}
   if(trimmed.Count>0){Routes[e.Id]=new(trimmed);return;}
  }
  enemyPlans.Remove(e.Id);EndTurn();
 }
}
