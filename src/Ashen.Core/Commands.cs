namespace Ashen.Core;

public sealed partial class World
{
 readonly Dictionary<string,Feature> pendingInteractions=[];
 // A solid surface is itself visible, but still hides the floor behind it.
 public bool CanSee(WorldPoint from,WorldPoint to)
 {
  int steps=Math.Max(1,(int)MathF.Ceiling(from.Distance(to)/.06f));
  for(int i=0;i<=steps;i++){var cell=(from+(to-from)*(i/(float)steps)).Cell;if(cell==to.Cell)return true;if(SightBlocked(cell))return false;}
  return true;
 }
 public bool RequestInteraction(Actor actor,Feature feature)
 {
  if(LowerMallEnabled&&Mode==Mode.Combat&&feature.Kind=="location"&&CurrentLowerMallSite?.Actions.Any(a=>a.FeatureId==feature.Id&&ChapterCombatAction(a))==true){if(actor.Position.Distance(feature.Pos.Center)<=1.25f){LowerMallOpened?.Invoke(feature.Id);return true;}return MoveTo(actor,feature.Pos.Center);}
  if(Mode!=Mode.Explore||actor.Enemy||!actor.Alive||!IsFeatureInteractable(feature)){LogSystem("Interaction requires a living companion and a discovered usable object outside combat.",true);return false;}
  if(!IsLootContainer(feature)&&feature.Locked){LogSystem($"{feature.Name} is locked.",true);return false;}
  if(!IsLootContainer(feature)&&feature.DetectedTrap){LogSystem($"{feature.Name} is trapped. Disarm it before interacting.",true);return false;}
  if(actor.Position.Distance(feature.Pos.Center)<=1.25f){Stop(actor);return Interact(feature);}
  var candidates=new List<WorldPoint>();
  for(int i=0;i<16;i++){float angle=i*MathF.Tau/16;candidates.Add(feature.Pos.Center+new WorldPoint(MathF.Cos(angle),MathF.Sin(angle))*1.05f);}
  candidates.AddRange(feature.Pos.Neighbors().Select(c=>c.Center));
  foreach(var point in candidates.OrderBy(p=>p.Distance(actor.Position)))
  {
   if(!CanStand(point,actor)||!Explored.Contains(point.Cell))continue;
   if(MoveTo(actor,point)){pendingInteractions[actor.Id]=feature;Message(MessageCategory.Travel,$"{actor.Name} approaches {feature.Name}.");return true;}
  }
  LogSystem("No clear approach. Move a blocking companion aside.",true);return false;
 }
 void CompleteInteractions()
 {
  if(Mode!=Mode.Explore)return;
  foreach(var (id,feature) in pendingInteractions.ToArray())
  {
   var actor=Actors.First(a=>a.Id==id);
   if(!actor.Alive){pendingInteractions.Remove(id);continue;}
   if(actor.Position.Distance(feature.Pos.Center)<=1.25f){Stop(actor);Interact(feature);if(Mode!=Mode.Explore)break;}
   else if(!IsMoving(actor))pendingInteractions.Remove(id);
  }
 }
 public void MoveSelection(WorldPoint goal,IEnumerable<string> ids,FormationPreset formation=FormationPreset.Wedge)
  =>MoveSelectionFormation(goal,ids,formation);
 public void MoveSelection(WorldPoint goal,IEnumerable<string> ids,FormationPreset formation,WorldPoint facing)
  =>MoveSelectionFormation(goal,ids,formation,facing);
}
