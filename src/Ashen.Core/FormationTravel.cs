namespace Ashen.Core;

/// <summary>
/// IIP20 formation travel keeps a selected party operating as one order rather than six
/// unrelated destinations. The leader owns the centerline. Followers continuously seek
/// portrait-order slots, collapse onto the leader's breadcrumb trail when a full footprint
/// cannot fit, and expand again once terrain clearance returns.
/// </summary>
public sealed partial class World
{
 sealed class FormationTravelOrder
 {
  public readonly List<string> Members=[];
  public FormationPreset Formation;
  public WorldPoint Goal;
  public WorldPoint FinalFacing=new(1,0);
  public WorldPoint LastTravelFacing=new(1,0);
  public readonly List<WorldPoint> Trail=[];
  public bool Compressed;
  public float OpenClearanceFor;
  public float LeaderArrivedFor;
  public float RefreshIn;
 }

 readonly List<FormationTravelOrder> formationTravelOrders=[];
 public int FormationTravelOrderCount=>formationTravelOrders.Count;

 static WorldPoint Unit(WorldPoint vector,WorldPoint fallback)
 {
  float length=vector.Distance(new(0,0));
  if(length>.0001f)return vector*(1/length);
  float fallbackLength=fallback.Distance(new(0,0));
  return fallbackLength>.0001f?fallback*(1/fallbackLength):new(1,0);
 }
 static WorldPoint Side(WorldPoint forward)=>new(-forward.Y,forward.X);
 static WorldPoint SlotPoint(WorldPoint anchor,WorldPoint forward,FormationOffset slot)
  =>anchor-forward*slot.Back+Side(forward)*slot.Side;

 public bool FormationTravelCompressed(string actorId)
  =>formationTravelOrders.FirstOrDefault(o=>o.Members.Contains(actorId,StringComparer.Ordinal))?.Compressed??false;

 public string FormationMovementReason(Actor leader,WorldPoint goal)
 {
  if(!leader.Alive)return "Select a living companion";
  if(Mode!=Mode.Explore)return MovementReason(leader,goal);
  if(!Explored.Contains(goal.Cell))return "Explore closer to reveal this area";
  if(!CanStand(goal,leader,false))return "Destination blocked";
  var path=Path(leader,goal,false);if(path.Count==0)return leader.Position.Distance(goal)<.025f?"Already here":"Path blocked";
  return "";
 }
 public MovementPreview PreviewFormationMovement(Actor leader,WorldPoint goal)
 {
  string reason=FormationMovementReason(leader,goal);var route=new List<WorldPoint>();float cost=0;
  if(reason==""&&leader.Position.Distance(goal)>=.025f){route=Path(leader,goal,false);cost=PathLength(leader.Position,route)*5f;}
  return new(reason=="",reason,cost,leader.Economy.MovementRemainingFeet,route);
 }

 public void CancelFormationTravel(IEnumerable<string> actorIds)
 {
  var ids=actorIds.ToHashSet(StringComparer.Ordinal);
  formationTravelOrders.RemoveAll(o=>o.Members.Any(ids.Contains));
 }
 public void CancelAllFormationTravel()=>formationTravelOrders.Clear();

 public IReadOnlyList<(Actor Actor,WorldPoint Target,bool Compressed)> PreviewFormation(
  WorldPoint anchor,IEnumerable<string> actorIds,FormationPreset formation,WorldPoint facing)
 {
  var members=FormationMembers(actorIds);var result=new List<(Actor,WorldPoint,bool)>();
  if(members.Length==0)return result;
  var forward=Unit(facing,new(1,0));var slots=FormationPatterns.Slots(formation);
  bool full=FormationFootprintFits(members,anchor,forward,formation);
  for(int i=0;i<members.Length;i++)
  {
   var slot=slots[Math.Min(i,slots.Count-1)];
   var target=full?SlotPoint(anchor,forward,slot):anchor-forward*(.68f*i);
   result.Add((members[i],target,!full));
  }
  return result;
 }

 bool FormationFootprintFits(IReadOnlyList<Actor> members,WorldPoint anchor,WorldPoint forward,FormationPreset formation)
 {
  var slots=FormationPatterns.Slots(formation);forward=Unit(forward,new(1,0));
  for(int i=0;i<members.Count;i++)
  {
   var target=SlotPoint(anchor,forward,slots[Math.Min(i,slots.Count-1)]);
   if(!Explored.Contains(target.Cell)||!CanStand(target,members[i],false))return false;
   // Slot centers cannot hide behind a wall relative to the formation center. This
   // catches door jambs and inside corners even when each individual point is floor.
   if(i>0&&!SegmentClear(anchor,target,members[i],false))return false;
  }
  return true;
 }

 bool QueueFormationRoute(Actor actor,WorldPoint target,bool force=false)
 {
  if(actor.Position.Distance(target)<.10f){if(Routes.TryGetValue(actor.Id,out var tiny)&&tiny.Count>0&&tiny.Last().Distance(target)<.16f)return true;Routes.Remove(actor.Id);return true;}
  if(!force&&Routes.TryGetValue(actor.Id,out var existing)&&existing.Count>0&&existing.Last().Distance(target)<.26f)return true;
  var path=Path(actor,target,false);if(path.Count==0)return false;
  pendingAttacks.Remove(actor.Id);pendingInteractions.Remove(actor.Id);BeginMovementReactionWindow(actor);
  Routes[actor.Id]=new(path);blockedTime.Remove(actor.Id);return true;
 }

 public bool MoveSelectionFormation(WorldPoint goal,IEnumerable<string> actorIds,FormationPreset formation,WorldPoint? requestedFacing=null)
 {
  var selected=actorIds.ToHashSet(StringComparer.Ordinal);var members=FormationMembers(selected);
  CancelFormationTravel(selected);
  if(Mode==Mode.Combat)
  {
   if(Active is {Enemy:false} current&&selected.Contains(current.Id))return MoveTo(current,goal);
   LogSystem("Only the character whose turn it is can move in combat.",true);return false;
  }
  if(Mode!=Mode.Explore||members.Length==0)return false;
  if(members.Length==1)
  {
   bool moved=MoveTo(members[0],goal);if(!moved)LogSystem("No clear path to that point.",true);return moved;
  }
  var leader=members[0];
  if(!CanStand(goal,leader,false)||!Explored.Contains(goal.Cell)||Path(leader,goal,false).Count==0)
  {LogSystem("No clear path to that point.",true);return false;}

  foreach(var a in members)Stop(a);
  var travelFacing=Unit(goal-leader.Position,new(1,0));
  var finalFacing=Unit(requestedFacing??travelFacing,travelFacing);
  var order=new FormationTravelOrder{Formation=formation,Goal=goal,FinalFacing=finalFacing,LastTravelFacing=travelFacing};
  order.Members.AddRange(members.Select(a=>a.Id));order.Trail.Add(leader.Position);formationTravelOrders.Add(order);
  if(!QueueFormationRoute(leader,goal,true)){formationTravelOrders.Remove(order);LogSystem("No clear path to that point.",true);return false;}
  UpdateFormationTravelOrder(order,0,true);
  return true;
 }

 WorldPoint TrailPointBehind(FormationTravelOrder order,float distance)
 {
  if(order.Trail.Count==0)return order.Goal;
  var current=order.Trail[^1];float remaining=Math.Max(0,distance);
  for(int i=order.Trail.Count-2;i>=0;i--)
  {
   var previous=order.Trail[i];float segment=current.Distance(previous);
   if(segment>=remaining&&segment>.0001f)return current+(previous-current)*(remaining/segment);
   remaining-=segment;current=previous;
  }
  return order.Trail[0];
 }

 void RememberLeaderTrail(FormationTravelOrder order,Actor leader)
 {
  if(order.Trail.Count==0||order.Trail[^1].Distance(leader.Position)>.12f)order.Trail.Add(leader.Position);
  // Only a few tiles of history are needed for a six-person compressed column.
  while(order.Trail.Count>96)order.Trail.RemoveAt(0);
 }

 void UpdateFormationTravelOrder(FormationTravelOrder order,float dt,bool force=false)
 {
  var members=order.Members.Select(id=>Actors.FirstOrDefault(a=>a.Id==id)).Where(a=>a?.Alive==true).Cast<Actor>().ToArray();
  if(members.Length<2){formationTravelOrders.Remove(order);return;}
  var leader=members[0];RememberLeaderTrail(order,leader);
  bool leaderArrived=leader.Position.Distance(order.Goal)<.12f&&!IsMoving(leader);
  order.LeaderArrivedFor=leaderArrived?order.LeaderArrivedFor+dt:0;

  WorldPoint forward;
  if(leaderArrived)forward=order.FinalFacing;
  else if(Routes.TryGetValue(leader.Id,out var route)&&route.Count>0)forward=Unit(route.Peek()-leader.Position,order.LastTravelFacing);
  else forward=Unit(order.Goal-leader.Position,order.LastTravelFacing);
  order.LastTravelFacing=forward;

  // Probe slightly ahead so the party compresses before the leader reaches a doorway.
  var probe=leaderArrived?order.Goal:leader.Position+forward*.72f;
  bool hasClearance=FormationFootprintFits(members,leaderArrived?order.Goal:leader.Position,forward,order.Formation)
                    &&FormationFootprintFits(members,probe,forward,order.Formation);
  if(!hasClearance){order.Compressed=true;order.OpenClearanceFor=0;}
  else if(order.Compressed)
  {
   order.OpenClearanceFor+=dt;
   if(order.OpenClearanceFor>=.34f){order.Compressed=false;order.OpenClearanceFor=0;}
  }

  order.RefreshIn-=dt;if(!force&&order.RefreshIn>0)return;order.RefreshIn=.16f;
  if(!leaderArrived&&!IsMoving(leader))QueueFormationRoute(leader,order.Goal,true);

  var slots=FormationPatterns.Slots(order.Formation);bool allSettled=leaderArrived;
  for(int i=1;i<members.Length;i++)
  {
   var actor=members[i];WorldPoint target;
   if(order.Compressed)
   {
    target=TrailPointBehind(order,.68f*i);
    // At a tight final destination the tail may need to extend beyond remembered travel.
    if(leaderArrived&&target.Distance(leader.Position)<.45f*i)target=leader.Position-forward*(.68f*i);
   }
   else target=SlotPoint(leaderArrived?order.Goal:leader.Position,forward,slots[Math.Min(i,slots.Count-1)]);

   if(!Explored.Contains(target.Cell)||!CanStand(target,actor,false))
   {
    // Never wedge a follower into scenery: fall back to the breadcrumb column for
    // this actor even during the open-formation state.
    target=TrailPointBehind(order,.68f*i);
   }
   float distance=actor.Position.Distance(target);
   if(distance>.18f)
   {
    allSettled=false;QueueFormationRoute(actor,target,force||distance>.65f&&!IsMoving(actor));
   }
   else if(IsMoving(actor)&&Routes.TryGetValue(actor.Id,out var actorRoute)&&actorRoute.Count>0&&actorRoute.Last().Distance(target)>.24f)
   {
    QueueFormationRoute(actor,target,true);allSettled=false;
   }
  }

  if(leaderArrived&&allSettled)
  {
   // In open terrain this is the re-formed requested shape. In a genuinely tight
   // endpoint the party is allowed to finish compressed rather than retry forever.
   formationTravelOrders.Remove(order);return;
  }
  // A shallow furnished shop may have no legal tail footprint. Once everyone is
  // nearby, finish the order without repeatedly repathing followers into each other.
  if(HasMallGeometry&&leaderArrived&&order.Compressed&&order.LeaderArrivedFor>3&&members.Skip(1).All(a=>a.Position.Distance(leader.Position)<=4))
  {
   foreach(var follower in members.Skip(1))Stop(follower);
   formationTravelOrders.Remove(order);LogSystem("The party holds near the tight doorway. Move individual survivors to adjust positions.");return;
  }
  if(leaderArrived&&order.Compressed&&order.LeaderArrivedFor>1.5f)
  {
   bool tailDone=members.Skip(1).All(a=>!IsMoving(a));
   if(tailDone)formationTravelOrders.Remove(order);
  }
 }

 internal void UpdateFormationTravel(float dt)
 {
  if(Mode!=Mode.Explore){if(formationTravelOrders.Count>0)formationTravelOrders.Clear();return;}
  foreach(var order in formationTravelOrders.ToArray())UpdateFormationTravelOrder(order,dt);
 }
}
