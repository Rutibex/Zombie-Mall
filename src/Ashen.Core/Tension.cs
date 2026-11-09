using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed class CampaignTension
{
 public SourceReference Source {get;set;}=new();
 public string RequiredFlag {get;set;}="";
 public List<string> AttackHordeIds {get;set;}=[];
}
public sealed class DailyIncident
{
 public int Day {get;set;}
 public int Minute {get;set;}
 public int Roll {get;set;}
 public int Tension {get;set;}
 public int DetailRoll {get;set;}
 public string Kind {get;set;}="";
 public string Result {get;set;}="";
}
public sealed class TensionState
{
 public uint Seed {get;set;}
 public int Score {get;set;}
 public int NextDayMinute {get;set;}
 public int DaysChecked {get;set;}
 public int LastNoiseMinute {get;set;}=-1;
 public int LastAdjustment {get;set;}
 public string LastReason {get;set;}="";
 public List<DailyIncident> History {get;set;}=[];
 public List<string> PendingAttacks {get;set;}=[];
 public List<string> CreditedHordes {get;set;}=[];
 public List<string> RecordedDeaths {get;set;}=[];
 // Credit defensive reductions only once per distinct construction / damage expenditure.
 public int RepairsCredited {get;set;}
 public int LastStopDay {get;set;}=-1;
 public bool BuildCredited {get;set;}
}
public static class TensionRules
{
 public static void Validate(CampaignDefinition c,Action<string,string,string> error)
 {
  if(c.Tension is not {} d)return;
  if(c.SchemaVersion<14||c.Infrastructure==null||c.Shelter==null||!c.NoiseAwareness||!c.DailySurvival)error("schema","tension","Tension requires schema 14, infrastructure, shelter, awareness and daily survival.");
  if(d.Source==null||string.IsNullOrWhiteSpace(d.Source.AuditId)||d.Source.PageStart<1||d.Source.PageEnd<d.Source.PageStart||d.Source.PageEnd>513)error("source","tension.source","Source/adaptation reference required.");
  if(d.RequiredFlag==""||!c.Events.Any(e=>e.Effects.Any(x=>x.Handler=="set-flag"&&x.Flag==d.RequiredFlag)))error("tension","requiredFlag","Tension must be gated by an authored refuge flag.");
  if(d.AttackHordeIds==null||d.AttackHordeIds.Count is <1 or >3||d.AttackHordeIds.Distinct().Count()!=d.AttackHordeIds.Count||d.AttackHordeIds.Any(id=>!c.Hordes.Any(h=>h.Id==id&&h.AreaId==c.Shelter?.AreaId&&h.RequiredFlag==d.RequiredFlag)))error("tension","attackHordeIds","One to three distinct finite home-area hordes required.");
 }
}
public sealed partial class World
{
 public TensionState TensionState {get;set;}=new();
 [JsonIgnore] public bool TensionEnabled=>Campaign?.Tension!=null;
 [JsonIgnore] public string TensionBand=>TensionState.Score switch{<=30=>"LOW",<=60=>"MEDIUM",<=90=>"HIGH",_=>"EXTREME"};
 bool dispatchingDailyAttack;
 public bool IsDailyAttack(string id)=>Campaign?.Tension?.AttackHordeIds.Contains(id)==true;
 void InitializeTension()
 {
  if(!TensionEnabled)return;
  TensionState.Seed=(uint)Random.Shared.Next(1,int.MaxValue);
  TensionState.NextDayMinute=checked((CampaignMinutes/1440+1)*1440);
 }
 // An independent saved seed keeps event outcomes stable without changing combat RNG.
 static int DailyDie(uint seed,int day,int salt,int sides)
 {
  uint n=unchecked(seed+(uint)day*2654435761u+(uint)salt*2246822519u);n^=n>>16;n=unchecked(n*2146121005u);n^=n>>15;n=unchecked(n*2221713035u);n^=n>>16;return (int)(n%(uint)sides)+1;
 }
 void ChangeTension(int amount,string reason)
 {
  if(!TensionEnabled||amount==0)return;var s=TensionState;int before=s.Score;s.Score=Math.Clamp(s.Score+amount,0,100);s.LastAdjustment=s.Score-before;s.LastReason=reason;
 }
 void TensionNoise(Actor source,string kind)
 {
  if(!TensionEnabled||source.Enemy)return;if((Exterior31Site(CurrentLowerMallSite)||Roof29Site(CurrentLowerMallSite)||Exterior30Site(CurrentLowerMallSite))&&kind=="gunshot"){TensionState.LastNoiseMinute=CampaignMinutes;return;}var s=TensionState;s.LastNoiseMinute=CampaignMinutes;
  int amount=kind switch{"explosion"=>DailyDie(s.Seed,(int)Math.Min(int.MaxValue,NextNoiseSequence),21,6)+DailyDie(s.Seed,(int)Math.Min(int.MaxValue,NextNoiseSequence),22,6),"gunshot"=>2,"shout" or "generator"=>DailyDie(s.Seed,(int)Math.Min(int.MaxValue,NextNoiseSequence),1,6),_=>0};
  ChangeTension(amount,kind=="generator"?"Generator startup":kind=="gunshot"?"Gunfire":"Loud noise");
 }
 void TensionWorks(WorksAction action)
 {
  if(!TensionEnabled)return;var s=TensionState;
  if(action==WorksAction.BuildBarricade&&!s.BuildCredited){s.BuildCredited=true;ChangeTension(-DailyDie(s.Seed,CampaignMinutes,3,4),"Reinforced defenses");}
  if(action==WorksAction.RepairBarricade){s.RepairsCredited++;ChangeTension(-DailyDie(s.Seed,s.RepairsCredited,4,4),"Repaired defenses");}
  if(action==WorksAction.Unbar)ChangeTension(DailyDie(s.Seed,CampaignMinutes,5,4),"Moving barricades");
  if(action==WorksAction.TogglePower&&InfrastructureState.Running==false&&s.LastStopDay!=CampaignMinutes/1440){s.LastStopDay=CampaignMinutes/1440;ChangeTension(-2,"Disabled generator noise source");}
 }
 void ObserveTensionOutcomes()
 {
  if(!TensionEnabled)return;var s=TensionState;
  foreach(var actor in CommunityMembers.Where(a=>a.Dead))if(!s.RecordedDeaths.Contains(actor.Id)){s.RecordedDeaths.Add(actor.Id);int dice=Enumerable.Range(0,4).Sum(i=>DailyDie(s.Seed,s.RecordedDeaths.Count,10+i,6));ChangeTension(dice,"Survivor death");}
  foreach(var h in Campaign!.Hordes.Where(h=>ReleasedHordes.Contains(h.Id)&&!s.CreditedHordes.Contains(h.Id)))
   if(AllHordeActors(h).Any()&&AllHordeActors(h).All(a=>!a.Alive)){s.CreditedHordes.Add(h.Id);ChangeTension(-DailyDie(s.Seed,s.CreditedHordes.Count,20,6),"Entire horde defeated");}
 }
 void ProcessTensionTime()
 {
  if(!TensionEnabled)return;ObserveTensionOutcomes();var s=TensionState;
  while(s.NextDayMinute<=CampaignMinutes)
  {
   int due=s.NextDayMinute;s.NextDayMinute=checked(s.NextDayMinute+1440);s.DaysChecked++;
   if(!HasDialogueFlag(Campaign!.Tension!.RequiredFlag))continue;
   // Time-consuming commands finish atomically. Incidents resolve after upkeep at the next control boundary;
   // they never interrupt a partially paid recipe, a rest recovery, or an initiative turn.
   if(s.LastNoiseMinute<due-1440&&InfrastructureState.BarricadeEngaged&&!InfrastructureState.Running&&ShelterWorkSafe())ChangeTension(-DailyDie(s.Seed,due/1440,2,6),"Quiet secured day");
   var e=new DailyIncident{Day=due/1440+1,Minute=due,Roll=DailyDie(s.Seed,due/1440,0,100),DetailRoll=DailyDie(s.Seed,due/1440,1,20),Tension=s.Score};
   int severity=e.Roll+e.Tension;
   if(severity<=10){e.Kind="quiet";e.Result="Distant movement passes without an incident.";}
   else if(severity<=30)
   {
    switch(e.DetailRoll%3)
    {
     case 0:
      e.Kind="spoilage";int lost=SpoilRefugeFood(1+(e.DetailRoll%4));e.Result=$"Pests spoil {lost} physical food unit(s) in the refuge stockpile.";break;
     case 1:
      e.Kind="outage";bool running=InfrastructureState.Running;InfrastructureState.Running=false;e.Result=running?"The generator trips off. Remaining fuel is retained; restart it at the refuge.":"An electrical flicker passes; the generator was already stopped.";break;
     default:
      e.Kind="weakness";int damage=Math.Min(InfrastructureState.BarricadeHp,4);InfrastructureState.BarricadeHp-=damage;
      if(InfrastructureState.BarricadeHp==0&&damage>0){InfrastructureState.BarricadeEngaged=false;InfrastructureDoor()!.Used=true;ChangeTension(4,"Barricade failure");}
      e.Result=$"A weak fixing costs the office barricade {damage} HP. Repair with physical materials.";break;
    }
   }
   else
   {
    var ids=Campaign.Tension.AttackHordeIds;string? id=ids.FirstOrDefault(id=>!ReleasedHordes.Contains(id)&&!s.PendingAttacks.Contains(id));
    e.Kind="attack";
    if(id==null)e.Result="Perimeter activity reported. All three authored attack groups are already committed or cleared; no replacement enemies appear.";
    else{s.PendingAttacks.Add(id);e.Result=$"{Campaign.Hordes.First(h=>h.Id==id).Name} approaches the refuge. Deployment waits for exploration at home; no offscreen resident damage.";}
   }
   s.History.Add(e);if(s.History.Count>64)s.History.RemoveAt(0);
   Message(MessageCategory.Rest,$"Day {e.Day} incident: {e.Kind}. D100 {e.Roll} + Tension {e.Tension} = {severity}. {e.Result}");
   AddStoryJournalEntry("daily-incident-"+e.Day,$"Day {e.Day}: {e.Kind}",$"D100 {e.Roll} + Tension {e.Tension}. {e.Result}","Refuge events");
  }
 }
 int SpoilRefugeFood(int requested)
 {
  if(SurvivorClassesEnabled&&CommunityMembers.Any(a=>a.Alive&&a.ClassIs("wise",3))&&CurrentTravelDestinationId==Campaign!.Shelter!.AreaId)requested=(requested+1)/2;
  var stock=ShelterHomeStockpile();if(stock==null)return 0;int left=requested;
  foreach(var stack in stock.Contents.Items.Where(s=>SupplyUse(s)=="food").ToArray())
  {int n=Math.Min(left,stack.Quantity);stack.ConsumeResourceUnits(n);stack.Quantity-=n;left-=n;if(stack.Quantity==0)stock.Contents.Items.Remove(stack);if(left==0)break;}
  return requested-left;
 }
 public string DailyAttackReason()
 {
  if(!TensionEnabled||TensionState.PendingAttacks.Count==0)return "No pending perimeter attack.";
  if(Mode!=Mode.Explore||PendingReaction!=null||ReactionInterruptActive)return "Finish the current combat or interrupt first.";
  if(CurrentTravelDestinationId!=Campaign!.Shelter!.AreaId)return "The attack waits at the refuge while the expedition is away.";
  if(Actors.Any(a=>a.Enemy&&a.Alive&&IsDailyAttackActor(a.Id)))return "The current perimeter attack must be defeated or evaded first.";
  if(!Party.Any(a=>a.Alive))return "No conscious expedition member.";
  dispatchingDailyAttack=true;try{return HordeReleaseReason(TensionState.PendingAttacks[0]);}finally{dispatchingDailyAttack=false;}
 }
 bool IsDailyAttackActor(string actorId)=>Campaign?.Hordes.Any(h=>IsDailyAttack(h.Id)&&h.MemberActorIds.Contains(actorId))==true;
 public bool DispatchDailyAttack()
 {
  if(DailyAttackReason()!="")return false;
  string id=TensionState.PendingAttacks[0];dispatchingDailyAttack=true;
  try{if(!ReleaseHorde(id,ShelterHomeStockpile()!.Pos))return false;TensionState.PendingAttacks.RemoveAt(0);StopAll();RefreshLocalSight();return true;}
  finally{dispatchingDailyAttack=false;}
 }
 void MaintainDailyAttackOrders()
 {
  if(!TensionEnabled||CurrentTravelDestinationId!=Campaign!.Shelter!.AreaId||ShelterHomeStockpile() is not {} site)return;
  foreach(var enemy in Actors.Where(a=>a.Enemy&&a.Alive&&IsDailyAttackActor(a.Id)))
  {
   if(Party.Any(p=>CanEnemySee(enemy,p)))continue;
   enemy.Awareness.State=AwarenessKind.Investigating;enemy.Awareness.LastKnown=site.Pos;enemy.Awareness.MemorySeconds=18;
  }
 }
 void ValidateTension()
 {
  var s=TensionState;if(s==null||s.History==null||s.PendingAttacks==null||s.RecordedDeaths==null||s.CreditedHordes==null)throw new InvalidDataException("Null tension state.");
  if(!TensionEnabled){if(s.Seed!=0||s.Score!=0||s.NextDayMinute!=0||s.DaysChecked!=0||s.LastNoiseMinute!=-1||s.History.Count!=0||s.PendingAttacks.Count!=0||s.CreditedHordes.Count!=0||s.RecordedDeaths.Count!=0||s.LastStopDay!=-1||s.BuildCredited||s.RepairsCredited!=0||s.LastAdjustment!=0||s.LastReason!="")throw new InvalidDataException("Legacy campaign contains unsupported tension.");return;}
  int expected=checked((CampaignMinutes/1440+1)*1440),start=Campaign!.StartMinute/1440;
  if(s.Seed==0||s.Score is <0 or >100||s.NextDayMinute!=expected||s.DaysChecked!=CampaignMinutes/1440-start||s.LastNoiseMinute< -1||s.LastNoiseMinute>CampaignMinutes||s.LastAdjustment is < -100 or >100||s.LastReason==null||s.LastReason.Length>100||s.LastStopDay< -1||s.LastStopDay>CampaignMinutes/1440||s.RepairsCredited<0||s.History.Count>64)throw new InvalidDataException("Invalid tension clock or score.");
  foreach(var list in new[]{s.PendingAttacks,s.RecordedDeaths,s.CreditedHordes})if(list.Distinct().Count()!=list.Count)throw new InvalidDataException("Duplicate tension ledger identity.");
  if(s.PendingAttacks.Any(id=>!IsDailyAttack(id)||ReleasedHordes.Contains(id))||s.PendingAttacks.Count>3||s.RecordedDeaths.Any(id=>!CommunityMembers.Any(a=>a.Id==id&&a.Dead))||s.CreditedHordes.Any(id=>!ReleasedHordes.Contains(id)||!Campaign.Hordes.Any(h=>h.Id==id)||AllHordeActors(Campaign.Hordes.First(h=>h.Id==id)).Any(a=>a.Alive)))throw new InvalidDataException("Tension ledger disagrees with actors or attack ownership.");
  int previous=0;foreach(var e in s.History)
  {
   if(e==null||e.Day<=previous||e.Minute<1440||e.Day>CampaignMinutes/1440+1||e.Minute!=(e.Day-1)*1440||e.Minute>CampaignMinutes||e.Roll is <1 or >100||e.DetailRoll is <1 or >20||e.Tension is <0 or >100||e.Kind is not ("quiet" or "spoilage" or "outage" or "weakness" or "attack")||string.IsNullOrWhiteSpace(e.Result)||e.Result.Length>1000||e.Roll!=DailyDie(s.Seed,e.Minute/1440,0,100)||e.DetailRoll!=DailyDie(s.Seed,e.Minute/1440,1,20))throw new InvalidDataException("Invalid or rerolled daily incident.");previous=e.Day;
  }
 }
}
