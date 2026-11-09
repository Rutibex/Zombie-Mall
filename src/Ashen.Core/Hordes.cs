namespace Ashen.Core;
public sealed class CampaignHorde
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public string AreaId {get;set;}="";
 public string TriggerFeatureId {get;set;}="";
 public List<string> MemberActorIds {get;set;}=[];
 public int NoiseThreshold {get;set;}=3;
 public string RequiredFlag {get;set;}="";
}
public static class HordeRules
{
 public static IEnumerable<CampaignValidationIssue> Validate(CampaignDefinition c)
 {
  var members=new HashSet<string>();var triggers=new HashSet<string>();
  foreach(var h in c.Hordes)
  {
   var area=c.Areas.FirstOrDefault(a=>a.Id==h.AreaId);var trigger=area?.Features.FirstOrDefault(f=>f.Id==h.TriggerFeatureId&&f.Kind=="horde");
   if(string.IsNullOrWhiteSpace(h.Name)||h.Source==null||string.IsNullOrWhiteSpace(h.Source.AuditId)||h.Source.PageStart<1||h.Source.PageEnd<h.Source.PageStart||h.Source.PageEnd>513||h.NoiseThreshold is <1 or >10||area==null||trigger==null||!triggers.Add(h.TriggerFeatureId)||h.MemberActorIds==null||h.MemberActorIds.Count is <2 or >12)
   {yield return new("horde",h.Id,"Invalid horde definition, source, trigger or bounded member list.");continue;}
   var encounter=c.Encounters.FirstOrDefault(e=>e.AreaId==h.AreaId&&e.ActorIds.ToHashSet().SetEquals(h.MemberActorIds));
   if(encounter==null)yield return new("horde",h.Id,"Horde must bind exactly one complete encounter group.");
   if(h.RequiredFlag!=""&&!c.Events.Any(e=>e.Effects.Any(x=>x.Handler=="set-flag"&&x.Flag==h.RequiredFlag)))yield return new("horde",h.Id,"Required flag has no producer.");
   foreach(var id in h.MemberActorIds)
   {
    var actor=c.Actors.FirstOrDefault(a=>a.Id==id);var placement=area.Actors.FirstOrDefault(p=>p.ActorId==id);
    if(actor==null||!actor.Enemy||actor.Recruitable||placement==null||placement.Position.Distance(trigger!.Position)>3.1f||!members.Add(id))yield return new("horde",h.Id,"Members must be distinct hostile actors near their area's breach, owned by one horde.");
   }
  }
  foreach(var feature in c.Areas.SelectMany(a=>a.Features).Where(f=>f.Kind=="horde"))if(c.SchemaVersion<10||!c.Hordes.Any(h=>h.TriggerFeatureId==feature.Id))yield return new("horde",feature.Id,"Horde feature requires a schema-10 horde binding.");
 }
}
public sealed partial class World
{
 public List<string> ReleasedHordes {get;set;}=[];
 public Dictionary<string,int> HordeNoiseCounts {get;set;}=[];
 public string HordeStatus(string id)
 {
  var h=Campaign?.Hordes.FirstOrDefault(h=>h.Id==id);if(h==null)return "Unavailable";
  if(!ReleasedHordes.Contains(id))return "Dormant";
  int alive=AllHordeActors(h).Count(a=>a.Alive);return alive==0?"Cleared":$"{alive}/{h.MemberActorIds.Count} remaining";
 }
 IEnumerable<Actor> AllHordeActors(CampaignHorde h)=>Actors.Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).Where(a=>h.MemberActorIds.Contains(a.Id));
 List<Cell> HordeArrival(CampaignHorde h)
 {
  var feature=Features.FirstOrDefault(f=>f.Id==h.TriggerFeatureId);if(feature==null||Blocked(feature.Pos))return [];
  var seen=new HashSet<Cell>{feature.Pos};var queue=new Queue<Cell>();queue.Enqueue(feature.Pos);
  while(queue.TryDequeue(out var cell))foreach(var next in cell.Neighbors())if(next.Distance(feature.Pos)<=3&&!Blocked(next)&&seen.Add(next))queue.Enqueue(next);
  return seen.Where(c=>!Features.Any(f=>f.Pos==c)&&CanStand(c.Center,null,true)).OrderBy(c=>c.Distance(feature.Pos)).ThenBy(c=>c.Y).ThenBy(c=>c.X).Take(h.MemberActorIds.Count).ToList();
 }
 public string HordeReleaseReason(string id)
 {
  if(IsDailyAttack(id)&&!dispatchingDailyAttack)return "This attack is controlled by the daily event ledger.";
  var h=Campaign?.Hordes.FirstOrDefault(h=>h.Id==id);if(h==null||h.AreaId!=CurrentTravelDestinationId)return "This horde belongs to another area.";
  if(ReleasedHordes.Contains(id))return "This horde has already breached; it cannot spawn again.";
  if(!Party.Any(a=>a.Alive)||Mode is Mode.Defeat or Mode.Victory||PendingReaction!=null)return "Finish the current interrupt first.";
  if(h.RequiredFlag!=""&&!HasDialogueFlag(h.RequiredFlag))return "Establish the office refuge before provoking the shutter.";
  var feature=Features.FirstOrDefault(f=>f.Id==h.TriggerFeatureId);if(feature==null||!IsDailyAttack(id)&&!IsFeatureDiscovered(feature))return "Find the pounding shutter first.";
  if(HordeArrival(h).Count<h.MemberActorIds.Count)return "The breach is blocked. Clear space beside the shutter.";
  return "";
 }
 public bool ReleaseHorde(string id,Cell? heardPosition=null)
 {
  if(heardPosition is {} heard&&!IsFloor(heard))return false;
  string reason=HordeReleaseReason(id);if(reason!=""){LogSystem(reason,true);return false;}
  var h=Campaign!.Hordes.First(h=>h.Id==id);var slots=HordeArrival(h);var feature=Features.Single(f=>f.Id==h.TriggerFeatureId);
  // All slots are preflighted before actor/item identities or the one-shot ledger change.
  ReleasedHordes.Add(id);feature.Used=true;HordeNoiseCounts[id]=h.NoiseThreshold;
  for(int i=0;i<h.MemberActorIds.Count;i++)
  {
   var actor=AddCampaignActor(Campaign.Actors.First(a=>a.Id==h.MemberActorIds[i]),slots[i]);actor.Awareness.State=AwarenessKind.Investigating;actor.Awareness.LastKnown=heardPosition??Party.Where(p=>p.Alive).OrderBy(p=>p.Position.Distance(feature.Pos.Center)).First().Pos;actor.Awareness.MemorySeconds=18;
   if(Mode==Mode.Combat)AddInitiativeParticipant(actor,reason:"surges through the breached shutter");
  }
  AssignMissingItemIds();AddStoryJournalEntry("released-"+id,h.Name,$"{h.MemberActorIds.Count} undead breach the shutter. This is a finite group; retreat does not reset it.","Hordes");
  Message(MessageCategory.Combat,$"{h.Name}: {h.MemberActorIds.Count} undead breach the shutter.");Reveal();return true;
 }
 bool InteractHorde(Feature feature)
 {
  var h=Campaign!.Hordes.FirstOrDefault(h=>h.TriggerFeatureId==feature.Id);if(h==null)return false;
  if(ReleasedHordes.Contains(h.Id)){Say(h.Name+": "+HordeStatus(h.Id)+". The wave will not return.");return true;}
  bool released=ReleaseHorde(h.Id);if(released)RefreshLocalSight();return released;
 }
 void HearHordeTriggers(LocalNoise noise,Dictionary<Cell,int> field)
 {
  if(noise.Kind is not ("gunshot" or "shout" or "explosion")||Campaign==null||SurvivorClassesEnabled&&HasDialogueFlag("flag.class.alarm-disabled"))return;
  foreach(var h in Campaign.Hordes.Where(h=>h.AreaId==CurrentTravelDestinationId&&!ReleasedHordes.Contains(h.Id)&&!IsDailyAttack(h.Id)))
  {
   var feature=Features.FirstOrDefault(f=>f.Id==h.TriggerFeatureId);
   if(feature==null||!IsFeatureDiscovered(feature)||!field.ContainsKey(feature.Pos)||h.RequiredFlag!=""&&!HasDialogueFlag(h.RequiredFlag))continue;
   int count=Math.Min(h.NoiseThreshold,HordeNoiseCounts.GetValueOrDefault(h.Id)+1);HordeNoiseCounts[h.Id]=count;
   if(count==h.NoiseThreshold)ReleaseHorde(h.Id,noise.Position);
  }
 }
 void ValidateHordeState()
 {
  if(ReleasedHordes==null||HordeNoiseCounts==null||ReleasedHordes.Distinct().Count()!=ReleasedHordes.Count)throw new InvalidDataException("Invalid horde ledger.");
  foreach(string id in ReleasedHordes)if(Campaign==null||!Campaign.Hordes.Any(h=>h.Id==id))throw new InvalidDataException("Unknown released horde.");
  foreach(var pair in HordeNoiseCounts){var h=Campaign?.Hordes.FirstOrDefault(h=>h.Id==pair.Key);if(h==null||pair.Value<0||pair.Value>h.NoiseThreshold)throw new InvalidDataException("Invalid horde noise count.");}
  foreach(var h in Campaign?.Hordes??[])
  {
   var actors=AllHordeActors(h).ToArray();bool released=ReleasedHordes.Contains(h.Id);
   if(released&&HordeNoiseCounts.GetValueOrDefault(h.Id)!=h.NoiseThreshold)throw new InvalidDataException("Released horde lacks its activation state.");
   if(!released&&actors.Length>0||released&&(actors.Length!=h.MemberActorIds.Count||!actors.Select(a=>a.Id).ToHashSet().SetEquals(h.MemberActorIds)||actors.Any(a=>!a.Enemy||a.Group!=Campaign!.Encounters.Single(e=>e.ActorIds.Contains(a.Id)).Group)))throw new InvalidDataException("Horde membership and release ledger disagree.");
   var feature=h.AreaId==CurrentTravelDestinationId?Features.FirstOrDefault(f=>f.Id==h.TriggerFeatureId):StoredTravelAreas.FirstOrDefault(a=>a.DestinationId==h.AreaId)?.Features.FirstOrDefault(f=>f.Id==h.TriggerFeatureId);
   if(feature!=null&&feature.Used!=released||released&&feature==null)throw new InvalidDataException("Horde trigger and release ledger disagree.");
  }
 }
}
