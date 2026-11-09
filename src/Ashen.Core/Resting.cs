using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RestKind { ShortRest, LongRest }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RestEventKind { Narrative, Encounter }

/// <summary>Authored rest policy for a playable travel region. It is deliberately
/// data-driven so future areas can forbid camping or carry different watch risk
/// without adding UI conditionals.</summary>
public sealed record RestSiteDefinition
{
 public string DestinationId {get;init;}="";
 public string Name {get;init;}="Campsite";
 public string Description {get;init;}="";
 public bool ShortRestAllowed {get;init;}=true;
 public bool LongRestAllowed {get;init;}=true;
 public int ShortRestEncounterChancePercent {get;init;}
 public int LongRestEncounterChancePercent {get;init;}
}

public sealed record RestEventDefinition
{
 public string Id {get;init;}="";
 public string Name {get;init;}="";
 public RestEventKind Kind {get;init;}=RestEventKind.Narrative;
 public bool InterruptsRest {get;init;}
 public string Text {get;init;}="";
}

public readonly record struct HitDieSpendRequest(string ActorId,string SourceClass,int Count);
public readonly record struct RestPreview(RestKind Kind,bool Available,string Reason,int Minutes,int RationCost,int EncounterChancePercent,string SiteName,string SiteDescription,string OutcomeText);
public readonly record struct RestResult(bool Completed,bool Interrupted,int MinutesElapsed,int RationsSpent,string EventId,string Summary);

public static class RestCatalog
{
 static readonly RestSiteDefinition[] sites=
 [
  new(){DestinationId="saint-orlans-crypt",Name="Saint Orlan's Crypt",Description="Stone chambers offer cover but the disturbed dead make every watch uncertain.",ShortRestEncounterChancePercent=10,LongRestEncounterChancePercent=25},
  new(){DestinationId="saint-orlan",Name="Saint Orlan",Description="The town watch and shrine wardens make ordinary street-side rest comparatively secure.",ShortRestEncounterChancePercent=0,LongRestEncounterChancePercent=0},
  new(){DestinationId="briarwood-road",Name="Briarwood Road",Description="A sheltered roadside camp is possible, though beasts and opportunists move after dark.",ShortRestEncounterChancePercent=10,LongRestEncounterChancePercent=25},
  new(){DestinationId="ashen-ford",Name="Ashen Ford",Description="Open riverbanks offer water and visibility but little protection from night movement.",ShortRestEncounterChancePercent=15,LongRestEncounterChancePercent=30},
  new(){DestinationId="old-watchtower",Name="Old Watchtower",Description="The exposed ruin never becomes quiet enough for eight uninterrupted hours. A brief halt is possible, but a long camp is not.",LongRestAllowed=false,ShortRestEncounterChancePercent=20,LongRestEncounterChancePercent=40}
 ];
 static readonly RestEventDefinition[] events=
 [
  new(){Id="uneasy-watch",Name="Uneasy Watch",Kind=RestEventKind.Narrative,InterruptsRest=false,Text="The watch hears movement beyond the firelight, but nothing approaches. The party completes the rest under a tense silence."},
  new(){Id="ashen-dream",Name="Ashen Dream",Kind=RestEventKind.Narrative,InterruptsRest=false,Text="Several companions wake from the same half-remembered dream of bells below the earth. Nothing attacks, and the rest continues."},
  new(){Id="night-ambush",Name="Night Ambush",Kind=RestEventKind.Encounter,InterruptsRest=true,Text="Shapes rush the camp before the watch can settle. The rest is interrupted and initiative is rolled."}
 ];
 public static RestSiteDefinition Site(string destinationId)=>sites.FirstOrDefault(s=>s.DestinationId.Equals(destinationId,StringComparison.OrdinalIgnoreCase))??new(){DestinationId=destinationId,Name="Wilderness camp",Description="No special shelter is available here.",ShortRestEncounterChancePercent=15,LongRestEncounterChancePercent=30};
 public static RestEventDefinition Event(string id)=>events.FirstOrDefault(e=>e.Id.Equals(id,StringComparison.OrdinalIgnoreCase))??throw new KeyNotFoundException($"Unknown rest event '{id}'.");
 public static bool TryGetEvent(string id,out RestEventDefinition evt){evt=events.FirstOrDefault(e=>e.Id.Equals(id,StringComparison.OrdinalIgnoreCase))!;return evt!=null;}
 public static IReadOnlyList<RestEventDefinition> Events=>events;
}

public sealed partial class World
{
 // IIP43 durable campaign rest state. UI selection and planned hit-die spending are transient.
 public int RestCount {get;set;}
 public string LastRestEventId {get;set;}="";
 [JsonIgnore] public RestSiteDefinition CurrentRestSite=>RestCatalog.Site(CurrentTravelDestinationId);

 public void InitializeRestState(){RestCount=0;LastRestEventId="";}
 public void NormalizeRestState()
 {
  RestCount=Math.Max(0,RestCount);
  if(!string.IsNullOrWhiteSpace(LastRestEventId)&&!RestCatalog.TryGetEvent(LastRestEventId,out _))LastRestEventId="";
 }
 public void ValidateRestState()
 {
  if(RestCount<0)throw new InvalidDataException("Invalid rest count.");
  if(!string.IsNullOrWhiteSpace(LastRestEventId)&&!RestCatalog.TryGetEvent(LastRestEventId,out _))throw new InvalidDataException("Unknown persisted rest event.");
 }

 bool HostilesTooCloseToRest(out Actor? hostile)
 {
  hostile=Actors.Where(a=>a.Enemy&&a.Alive)
   .OrderBy(a=>Party.Where(p=>!p.Dead).Select(p=>p.Position.Distance(a.Position)).DefaultIfEmpty(float.MaxValue).Min())
   .FirstOrDefault(a=>Party.Any(p=>!p.Dead&&p.Position.Distance(a.Position)<10));
  return hostile!=null;
 }

 public RestPreview PreviewRest(RestKind kind)
 {
  if(DailySurvivalEnabled)return PreviewSurvivalRest(kind);
  if(Campaign!=null)return new(kind,false,"This saved campaign does not enable daily survival/rest. Start a new game for current rules.",0,0,0,CurrentAreaName,"Campaign fixture","");
  var site=CurrentRestSite;int minutes=kind==RestKind.ShortRest?60:480,rations=kind==RestKind.LongRest?1:0;
  int risk=kind==RestKind.ShortRest?site.ShortRestEncounterChancePercent:site.LongRestEncounterChancePercent;
  string outcome=kind==RestKind.ShortRest
   ?"Spend chosen Hit Dice for healing, restore short-rest resources, restore pact slots, and advance one hour."
   :"Restore living companions to full HP, restore short/long-rest resources and spell slots, recover up to half spent Hit Dice, consume one camping supply, and advance eight hours.";
  if(Mode!=Mode.Explore)return new(kind,false,"Rest is only available while exploring outside combat.",minutes,rations,risk,site.Name,site.Description,outcome);
  if(!Party.Any(a=>!a.Dead))return new(kind,false,"No living or stable companion can benefit from rest.",minutes,rations,risk,site.Name,site.Description,outcome);
  if(kind==RestKind.ShortRest&&!site.ShortRestAllowed)return new(kind,false,$"A short rest is not possible at {site.Name}.",minutes,rations,risk,site.Name,site.Description,outcome);
  if(kind==RestKind.LongRest&&!site.LongRestAllowed)return new(kind,false,$"This location does not permit a long rest: {site.Description}",minutes,rations,risk,site.Name,site.Description,outcome);
  if(kind==RestKind.LongRest&&Rations<1)return new(kind,false,"A long rest requires 1 camping supply. Short rest remains available.",minutes,rations,risk,site.Name,site.Description,outcome);
  if(HostilesTooCloseToRest(out var hostile))return new(kind,false,$"{hostile!.Name} is too close to make camp safely.",minutes,rations,risk,site.Name,site.Description,outcome);
  return new(kind,true,"",minutes,rations,risk,site.Name,site.Description,outcome);
 }

 public int PlannedHitDiceAvailable(Actor actor,string sourceClass)=>actor.Character.HitDice.FirstOrDefault(h=>h.SourceClass.Equals(sourceClass,StringComparison.OrdinalIgnoreCase))?.Current??0;

 List<HitDieSpendRequest> NormalizeHitDieRequests(IEnumerable<HitDieSpendRequest>? requests)
 {
  if(requests==null)return [];
  var normalized=new List<HitDieSpendRequest>();
  foreach(var group in requests.Where(r=>r.Count>0).GroupBy(r=>(r.ActorId,r.SourceClass),StringTupleComparer.Instance))
  {
   var actor=Party.FirstOrDefault(a=>a.Id.Equals(group.Key.ActorId,StringComparison.Ordinal));if(actor==null||actor.Dead)continue;
   var pool=actor.Character.HitDice.FirstOrDefault(h=>h.SourceClass.Equals(group.Key.SourceClass,StringComparison.OrdinalIgnoreCase));if(pool==null||pool.Current<=0)continue;
   int wanted=group.Sum(x=>Math.Max(0,x.Count));normalized.Add(new(actor.Id,pool.SourceClass,Math.Clamp(wanted,0,pool.Current)));
  }
  return normalized;
 }

 sealed class StringTupleComparer:IEqualityComparer<(string ActorId,string SourceClass)>
 {
  public static readonly StringTupleComparer Instance=new();
  public bool Equals((string ActorId,string SourceClass) x,(string ActorId,string SourceClass) y)=>x.ActorId.Equals(y.ActorId,StringComparison.Ordinal)&&x.SourceClass.Equals(y.SourceClass,StringComparison.OrdinalIgnoreCase);
  public int GetHashCode((string ActorId,string SourceClass) obj)=>HashCode.Combine(StringComparer.Ordinal.GetHashCode(obj.ActorId),StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SourceClass));
 }

 int SpendHitDice(Actor actor,HitDiePool pool,int count)
 {
  int healed=0,con=actor.AbilityModifier(Ability.Constitution);count=Math.Clamp(count,0,pool.Current);
  for(int i=0;i<count&&actor.Hp<actor.MaxHp;i++)
  {
   int roll=Roll(Math.Max(2,pool.DieSize)),gain=Math.Max(0,roll+con),before=actor.Hp;pool.Current--;actor.Hp=Math.Min(actor.MaxHp,actor.Hp+gain);int actual=actor.Hp-before;healed+=actual;
   Message(MessageCategory.Rest,$"{actor.Name} spends a {pool.SourceClass} Hit Die and regains {actual} HP.",$"Hit Die: d{pool.DieSize} {roll} {(con>=0?"+":"-")} {Math.Abs(con)} Constitution = {gain}; HP {before} -> {actor.Hp}. {pool.Current}/{pool.Maximum} {pool.SourceClass} Hit Dice remain.");
  }
  if(actor.Hp>0)actor.Character.SetCondition("unconscious","Unconscious",false);
  return healed;
 }

 void ApplyShortRestRecovery(IEnumerable<HitDieSpendRequest>? requests)
 {
  foreach(var actor in Party.Where(a=>!a.Dead))actor.Character.RestoreShortRest();
  foreach(var request in NormalizeHitDieRequests(requests))
  {
   var actor=Party.First(a=>a.Id==request.ActorId);var pool=actor.Character.HitDice.First(h=>h.SourceClass.Equals(request.SourceClass,StringComparison.OrdinalIgnoreCase));SpendHitDice(actor,pool,request.Count);
  }
 }

 int RecoverLongRestHitDice(Actor actor)
 {
  int totalMax=actor.Character.HitDice.Sum(h=>Math.Max(0,h.Maximum));if(totalMax<=0)return 0;int allowance=Math.Max(1,totalMax/2),recovered=0;
  foreach(var pool in actor.Character.HitDice.OrderBy(h=>actor.Character.Classes.FindIndex(c=>c.ClassId.Equals(h.SourceClass,StringComparison.OrdinalIgnoreCase))))
  {
   int missing=Math.Max(0,pool.Maximum-pool.Current),take=Math.Min(missing,allowance-recovered);if(take<=0)continue;pool.Current+=take;recovered+=take;if(recovered>=allowance)break;
  }
  return recovered;
 }

 public void ApplyLongRestRecovery()
 {
  foreach(var actor in Party.Where(a=>!a.Dead))
  {
   actor.Character.RestoreLongRest();int dice=RecoverLongRestHitDice(actor);if(dice>0)Message(MessageCategory.Rest,$"{actor.Name} recovers {dice} Hit Die{(dice==1?"":"s")}.");
  }
 }

 public void CompleteSafeLodging(int minutes)
 {
  if(Campaign!=null)throw new InvalidOperationException("Campaign rest must use the campaign rest procedure.");
  StopAll();CampaignMinutes=Math.Max(0,CampaignMinutes+Math.Max(0,minutes));RestCount++;LastRestEventId="";
  foreach(var actor in Party.Where(a=>!a.Dead))actor.Statistics.RestsTaken++;
  ApplyLongRestRecovery();
 }

 public RestResult PerformRest(RestKind kind,IEnumerable<HitDieSpendRequest>? hitDice=null)
 {
  if(DailySurvivalEnabled)return PerformSurvivalRest(kind,hitDice);
  var preview=PreviewRest(kind);if(!preview.Available){LogSystem(preview.Reason,true);return new(false,false,0,0,"",preview.Reason);}
  var requests=kind==RestKind.ShortRest?NormalizeHitDieRequests(hitDice):[];StopAll();RestCount++;LastRestEventId="";
  if(preview.RationCost>0)Rations=Math.Max(0,Rations-preview.RationCost);

  RestEventDefinition? evt=null;
  if(preview.EncounterChancePercent>0&&Roll(100)<=preview.EncounterChancePercent)
  {
   evt=RestCatalog.Events[(Roll(RestCatalog.Events.Count)-1)%RestCatalog.Events.Count];LastRestEventId=evt.Id;
  }
  if(evt?.InterruptsRest==true)
  {
   int interruptedMinutes=kind==RestKind.ShortRest?30:240;CampaignMinutes=Math.Max(0,CampaignMinutes+interruptedMinutes);
   ResolveRestEventDefinition(evt,kind);Message(MessageCategory.Rest,$"{(kind==RestKind.ShortRest?"Short":"Long")} rest interrupted after {interruptedMinutes} minutes. No rest recovery is gained.");
   return new(false,true,interruptedMinutes,preview.RationCost,evt.Id,"Rest interrupted.");
  }

  CampaignMinutes=Math.Max(0,CampaignMinutes+preview.Minutes);
  if(kind==RestKind.ShortRest)ApplyShortRestRecovery(requests);else ApplyLongRestRecovery();
  foreach(var actor in Party.Where(a=>!a.Dead))actor.Statistics.RestsTaken++;
  if(evt!=null)ResolveRestEventDefinition(evt,kind);
  string summary=kind==RestKind.ShortRest?$"The party completes a short rest. One hour passes; short-rest resources recover and selected Hit Dice are spent.":$"The party completes a long rest. Eight hours pass; living companions recover and {Rations} camping suppl{(Rations==1?"y":"ies")} remain.";
  Message(MessageCategory.Rest,summary,$"Completion time: {CampaignTimeText}. Rest site: {preview.SiteName}. Encounter/event chance was {preview.EncounterChancePercent}%.");Sound?.Invoke("heal");
  QueueRestCompanionConversation(kind);
  return new(true,false,preview.Minutes,preview.RationCost,evt?.Id??"",summary);
 }

 public bool ResolveRestEvent(string eventId,RestKind kind=RestKind.LongRest)
 {
  if(!RestCatalog.TryGetEvent(eventId,out var evt))return false;
  LastRestEventId=evt.Id;ResolveRestEventDefinition(evt,kind);return true;
 }

 void ResolveRestEventDefinition(RestEventDefinition evt,RestKind kind)
 {
  Message(MessageCategory.Rest,$"Rest event — {evt.Name}: {evt.Text}");AddStoryJournalEntry($"rest-event-{RestCount:D4}-{evt.Id}",evt.Name,evt.Text,"Camp",CurrentJournalChapter,CampaignMinutes);
  if(evt.Kind==RestEventKind.Encounter)SpawnRestAmbush();
 }

 void SpawnRestAmbush()
 {
  var leader=AvailablePartyLeader;if(leader==null)return;int group=9000+RestCount;
  var candidates=Floors.Where(c=>!Blocked(c)&&Actors.All(a=>a.Pos!=c)&&Party.All(p=>p.Position.Distance(c.Center)>=4f)&&leader.Position.Distance(c.Center)<=8f).OrderBy(c=>leader.Position.Distance(c.Center)).Take(2).ToList();
  if(candidates.Count==0){Message(MessageCategory.Rest,"The alarm passes without a visible attacker, but the rest has already been broken.");return;}
  string role=CurrentTravelDestinationId switch{"briarwood-road"=>"Archer","ashen-ford"=>"Skeleton","old-watchtower"=>"Wraith",_=>"Skeleton"};
  for(int i=0;i<candidates.Count;i++)
  {
   var actor=new Actor{Id=$"rest-ambush-{RestCount}-{i+1}",Name=role=="Wraith"?"Watchfire shade":role=="Archer"?"Briarwood prowler":"Night raider",CreatureKind=role,Pos=candidates[i],Enemy=true,Group=group,Character=CharacterTemplates.Enemy(role,role=="Wraith"?20:16,13,role=="Archer"||role=="Wraith"?6:1)};Actors.Add(actor);
  }
  Reveal();StartCombat(group);
 }

 void QueueRestCompanionConversation(RestKind kind)
 {
  // IIP43 deliberately reuses IIP40's durable queue instead of introducing a second
  // camp-dialogue system. The first completed long rest can surface a campfire scene.
  if(kind!=RestKind.LongRest||DialogueFlags.Contains("rest-first-long",StringComparer.OrdinalIgnoreCase))return;
  DialogueFlags.Add("rest-first-long");
  if(!SeenCompanionConversations.Contains("campfire-first-long",StringComparer.OrdinalIgnoreCase)&&!PendingCompanionConversations.Contains("campfire-first-long",StringComparer.OrdinalIgnoreCase))PendingCompanionConversations.Add("campfire-first-long");
 }

 /// <summary>IIP8 compatibility wrapper retained for old tests/tools. The production
 /// Rest button opens the IIP43 interface; this convenience performs a safe long rest
 /// without rolling a camp event.</summary>
 public bool Rest()
 {
  if(DailySurvivalEnabled)return PerformSurvivalRest(RestKind.LongRest,null).Completed;
  var preview=PreviewRest(RestKind.LongRest);if(!preview.Available){LogSystem(preview.Reason,true);return false;}
  Rations=Math.Max(0,Rations-1);StopAll();RestCount++;LastRestEventId="";CampaignMinutes=Math.Max(0,CampaignMinutes+480);foreach(var actor in Party.Where(a=>!a.Dead))actor.Statistics.RestsTaken++;ApplyLongRestRecovery();Message(MessageCategory.Rest,$"The party makes camp. Eight hours pass; health, spell slots, and long-rest resources are restored. {Rations} camping suppl{(Rations==1?"y":"ies")} remain.");Sound?.Invoke("heal");return true;
 }
}
