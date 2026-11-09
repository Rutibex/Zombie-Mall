using System.Text.Json.Serialization;
namespace Ashen.Core;
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwarenessKind { Idle, Investigating, Searching, Alerted }
public sealed class LocalAwareness
{
 public AwarenessKind State {get;set;}
 public Cell? LastKnown {get;set;}
 public float MemorySeconds {get;set;}
 public long LastNoise {get;set;}
 public float BarricadeAttackSeconds {get;set;}
}
public sealed class LocalNoise
{
 public long Sequence {get;set;}
 public string AreaId {get;set;}="";
 public string SourceId {get;set;}="";
 public Cell Position {get;set;}
 public string Kind {get;set;}="";
 public int RangeFeet {get;set;}
}
public sealed partial class World
{
 public List<LocalNoise> NoiseHistory {get;set;}=[];
 public long NextNoiseSequence {get;set;}=1;
 [JsonIgnore] public bool LocalAwarenessEnabled=>Campaign?.NoiseAwareness==true;
 float awarenessClock;
 public bool CanEnemySee(Actor enemy,Actor survivor)=>enemy.Enemy&&enemy.Alive&&!enemy.Unconscious&&!survivor.Enemy&&survivor.Alive&&!survivor.Character.HasCondition("hidden")&&!survivor.Character.HasCondition("class-phantom")&&!(survivor.Character.HasCondition("upper26-vampire")&&!LowerTraits(enemy).Contains("human"))&&enemy.Position.Distance(survivor.Position)<=CampaignAwareness(enemy)&&Line(enemy.Position,survivor.Position);
 // A bounded four-neighbour sound flood. Walls stop sound; closed doors add 30 feet of acoustic cost.
 public Dictionary<Cell,int> LocalSoundField(Cell origin,int rangeFeet)
 {
  var result=new Dictionary<Cell,int>();if(!IsFloor(origin)||rangeFeet is <5 or >200)return result;
  var queue=new PriorityQueue<Cell,int>();result[origin]=0;queue.Enqueue(origin,0);
  while(queue.TryDequeue(out var cell,out int cost))
  {
   if(result[cell]!=cost)continue;
   foreach(var next in cell.Neighbors())
   {
    if(!IsFloor(next))continue;
    int extra=Features.Any(f=>f.Kind=="door"&&!f.Used&&f.Pos==next)?30:0;
    int total=cost+5+extra;if(total>rangeFeet||result.TryGetValue(next,out int previous)&&previous<=total)continue;
    result[next]=total;queue.Enqueue(next,total);
   }
  }
  return result;
 }
 public int EmitLocalNoise(Actor source,string kind,int rangeFeet,Cell? position=null)
 {
  if(source.Character.HasCondition("chapter-silence"))return 0;
  if(kind is "weapon" or "explosion"&&!EquipmentEnabled||kind=="generator"&&!InfrastructureEnabled||!LocalAwarenessEnabled||!Actors.Contains(source)||!source.Alive||kind is not ("gunshot" or "door" or "shout" or "generator" or "weapon" or "explosion")||rangeFeet is <5 or >200||Mode is Mode.Defeat or Mode.Victory)return 0;
  Cell origin=position??source.Pos;if(!IsFloor(origin))return 0;
  var sound=LocalSoundField(origin,rangeFeet);var noise=new LocalNoise{Sequence=NextNoiseSequence++,AreaId=CurrentTravelDestinationId,SourceId=source.Id,Position=origin,Kind=kind,RangeFeet=rangeFeet};NoiseHistory.Add(noise);if(NoiseHistory.Count>32)NoiseHistory.RemoveAt(0);
  Upper26Noise(source,kind);Upper27Noise(source,kind);Roof28Noise(source,kind);Roof29Noise(source,kind);Exterior30Noise(source,kind);Exterior31Noise(source,kind);Lower23Noise(source,kind);if(kind=="gunshot")LowerGunshot(source);TensionNoise(source,kind);HearHordeTriggers(noise,sound);
  int heard=0;
  foreach(var enemy in Actors.Where(e=>e!=source&&e.Enemy&&e.Alive&&!e.Unconscious&&sound.ContainsKey(e.Pos)))
  {
   heard++;Stop(enemy);enemy.Awareness.LastNoise=noise.Sequence;
   // A visible target takes priority over a distraction. Hearing never discloses a survivor's current location.
   var seen=Party.FirstOrDefault(p=>CanEnemySee(enemy,p));
   enemy.Awareness.LastKnown=seen?.Pos??origin;enemy.Awareness.State=seen==null?AwarenessKind.Investigating:AwarenessKind.Alerted;enemy.Awareness.MemorySeconds=18;
   if(Mode==Mode.Combat&&!Order.Contains(enemy.Id)&&InvestigationPath(enemy).Count>0)AddInitiativeParticipant(enemy,reason:"responds to a local disturbance");
  }
  Message(MessageCategory.Combat,$"{source.Name}: {kind} ({rangeFeet} ft sound budget).","Walls stop propagation; closed doors muffle it. Nearby listeners may investigate.");return heard;
 }
 public bool CanShout(Actor a)=>LocalAwarenessEnabled&&Actors.Contains(a)&&!a.Enemy&&a.Alive&&!IsMoving(a)&&!ReactionInterruptActive&&(Mode==Mode.Explore||CanAct(a));
 public bool Shout(Actor a)
 {
  if(!CanShout(a))return false;if(Mode==Mode.Combat&&!SpendAction(a,"Shout"))return false;
  a.Character.SetCondition("hidden","Hidden",false);EmitLocalNoise(a,"shout",60);return true;
 }
 void RefreshLocalSight()
 {
  if(!LocalAwarenessEnabled||Mode is Mode.Defeat or Mode.Victory)return;
  foreach(var enemy in Actors.Where(e=>e.Enemy&&e.Alive&&!e.Unconscious))
  {
   var target=Party.Where(p=>CanEnemySee(enemy,p)).OrderBy(p=>p.Position.Distance(enemy.Position)).FirstOrDefault();if(target==null)continue;
   enemy.Awareness.State=AwarenessKind.Alerted;enemy.Awareness.LastKnown=target.Pos;enemy.Awareness.MemorySeconds=18;
   if(Mode==Mode.Explore)StartCombat(enemy.Group);else if(!Order.Contains(enemy.Id))AddInitiativeParticipant(enemy,reason:"spots the expedition");
  }
 }
 List<WorldPoint> InvestigationPath(Actor enemy)
 {
  if(enemy.Awareness.LastKnown is not {} target)return [];
  if(BarricadeApproach(enemy) is {} approach)return approach;
  // Find the connected physical region first, so a muffled sound cannot pull an enemy through a locked door.
  var reached=new HashSet<Cell>{enemy.Pos};var queue=new Queue<Cell>();queue.Enqueue(enemy.Pos);
  while(queue.TryDequeue(out var cell))foreach(var next in cell.Neighbors())if(!Blocked(next)&&reached.Add(next))queue.Enqueue(next);
  foreach(var goal in reached.Where(c=>c.Distance(target)<enemy.Pos.Distance(target)-.01f&&CanStand(c.Center,enemy)).OrderBy(c=>c.Distance(target)).ThenBy(c=>c.Distance(enemy.Pos)).Take(12))
  {var path=Path(enemy,goal.Center);if(path.Count>0)return path;}
  return [];
 }
 void ForgetLocalTarget(Actor enemy)
 {
  Stop(enemy);enemy.Awareness.State=AwarenessKind.Idle;enemy.Awareness.LastKnown=null;enemy.Awareness.MemorySeconds=0;enemy.Awareness.BarricadeAttackSeconds=0;
 }
 void UpdateLocalAwareness(float seconds)
 {
  if(!LocalAwarenessEnabled||Mode!=Mode.Explore)return;
  awarenessClock+=seconds;if(awarenessClock<.25f)return;float delta=awarenessClock;awarenessClock=0;RefreshLocalSight();if(Mode!=Mode.Explore)return;
  foreach(var enemy in Actors.Where(e=>e.Enemy&&e.Alive&&!e.Unconscious&&e.Awareness.State!=AwarenessKind.Idle))
  {
   var state=enemy.Awareness;state.MemorySeconds=Math.Max(0,state.MemorySeconds-delta);
   if(state.MemorySeconds<=0){ForgetLocalTarget(enemy);continue;}
   if(state.State==AwarenessKind.Alerted)state.State=AwarenessKind.Investigating;
   if(UpdateExplorationBarricadeAttack(enemy,delta)){if(Mode!=Mode.Explore)return;continue;}
   if(state.State==AwarenessKind.Searching||IsMoving(enemy))continue;
   if(state.LastKnown is {} goal&&enemy.Pos.Distance(goal)<=1){state.State=AwarenessKind.Searching;state.MemorySeconds=Math.Min(6,state.MemorySeconds);continue;}
   var path=InvestigationPath(enemy);if(path.Count>0)Routes[enemy.Id]=new(path);else{state.State=AwarenessKind.Searching;state.MemorySeconds=Math.Min(6,state.MemorySeconds);}
  }
 }
 bool RunNoiseEnemyTurn(Actor enemy)
 {
  if(Party.Any(p=>CanEnemySee(enemy,p))){var target=Party.First(p=>CanEnemySee(enemy,p));enemy.Awareness.State=AwarenessKind.Alerted;enemy.Awareness.LastKnown=target.Pos;enemy.Awareness.MemorySeconds=18;return false;}
  if(!enemyPlans.Add(enemy.Id)){enemyPlans.Remove(enemy.Id);EndTurn();return true;}
  enemy.Awareness.MemorySeconds=Math.Max(0,enemy.Awareness.MemorySeconds-6);
  if(enemy.Awareness.MemorySeconds<=0||enemy.Awareness.State==AwarenessKind.Idle)
  {ForgetLocalTarget(enemy);enemyPlans.Remove(enemy.Id);RemoveInitiativeParticipant(enemy.Id,"loses the expedition's trail");CheckEnd();return true;}
  if(CanBashBarricade(enemy)){AttackBarricade(enemy);enemyPlans.Remove(enemy.Id);EndTurn();return true;}
  var path=InvestigationPath(enemy);var trimmed=new List<WorldPoint>();var from=enemy.Position;float budget=enemy.Move;
  foreach(var point in path){float length=from.Distance(point);if(length<=budget){trimmed.Add(point);budget-=length;from=point;}else{if(budget>.01f)trimmed.Add(from+(point-from)*(budget/length));break;}}
  if(trimmed.Count>0){Routes[enemy.Id]=new(trimmed);return true;}
  enemy.Awareness.State=AwarenessKind.Searching;enemyPlans.Remove(enemy.Id);EndTurn();return true;
 }
 void ValidateLocalAwareness()
 {
  if(NoiseHistory==null||NoiseHistory.Count>32||NextNoiseSequence<1)throw new InvalidDataException("Invalid noise history.");
  long previous=0;foreach(var n in NoiseHistory)
  {
   var area=Campaign?.Areas.FirstOrDefault(a=>a.Id==n.AreaId);
   if(n.Kind is "weapon" or "explosion"&&!EquipmentEnabled||n.Kind=="generator"&&!InfrastructureEnabled||n.Sequence<=previous||n.Sequence>=NextNoiseSequence||n.RangeFeet is <5 or >200||n.Kind is not ("gunshot" or "door" or "shout" or "generator" or "weapon" or "explosion")||area==null||!area.Floors.Contains(n.Position)||Campaign!.Actors.All(a=>a.Id!=n.SourceId))throw new InvalidDataException("Invalid local noise record.");previous=n.Sequence;
  }
  var areas=StoredTravelAreas.Select(a=>(Id:a.DestinationId,Actors:a.Actors.AsEnumerable())).Prepend((CurrentTravelDestinationId,Actors.AsEnumerable()));
  foreach(var area in areas)foreach(var actor in area.Item2)
  {
   var state=actor.Awareness;var definition=Campaign?.Areas.FirstOrDefault(a=>a.Id==area.Item1);
   if(state==null||!InfrastructureEnabled&&state.BarricadeAttackSeconds!=0||!float.IsFinite(state.BarricadeAttackSeconds)||state.BarricadeAttackSeconds is <0 or >6||!Enum.IsDefined(state.State)||!float.IsFinite(state.MemorySeconds)||state.MemorySeconds is <0 or >18||state.LastNoise<0||state.LastNoise>=NextNoiseSequence||state.LastKnown is {} cell&&(definition==null||!definition.Floors.Contains(cell))||state.State==AwarenessKind.Idle&&(state.LastKnown!=null||state.MemorySeconds!=0)||state.State!=AwarenessKind.Idle&&(state.LastKnown==null||state.MemorySeconds<=0)||(!LocalAwarenessEnabled||!actor.Enemy)&&state.State!=AwarenessKind.Idle)throw new InvalidDataException("Invalid enemy awareness state.");
  }
  if(!LocalAwarenessEnabled&&NoiseHistory.Count>0)throw new InvalidDataException("Awareness data on a legacy campaign.");
 }
}
