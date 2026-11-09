using System.Text.Json.Serialization;
namespace Ashen.Core;

public sealed class CampaignShelter
{
 public string AreaId {get;set;}="";
 public string StockpileFeatureId {get;set;}="";
 public string LeaderActorId {get;set;}="";
 public int Capacity {get;set;}=6;
 public SourceReference Source {get;set;}=new();
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShelterDuty { Recuperate, Steward, Care }
public sealed class ShelterDutyState
{
 public ShelterDuty Duty {get;set;}=ShelterDuty.Recuperate;
 public int NextWorkMinute {get;set;}
 public int CompletedShifts {get;set;}
 public int SuppliesSpent {get;set;}
 public string LastResult {get;set;}="Awaiting first duty period.";
}
public sealed class ShelterResident
{
 public Actor Actor {get;set;}=new();
 [JsonIgnore] public ShelterDuty Duty {get=>Actor.ShelterWork.Duty;set=>Actor.ShelterWork.Duty=value;}
 [JsonIgnore] public int NextWorkMinute {get=>Actor.ShelterWork.NextWorkMinute;set=>Actor.ShelterWork.NextWorkMinute=value;}
 [JsonIgnore] public int CompletedShifts {get=>Actor.ShelterWork.CompletedShifts;set=>Actor.ShelterWork.CompletedShifts=value;}
 [JsonIgnore] public int SuppliesSpent {get=>Actor.ShelterWork.SuppliesSpent;set=>Actor.ShelterWork.SuppliesSpent=value;}
 [JsonIgnore] public string LastResult {get=>Actor.ShelterWork.LastResult;set=>Actor.ShelterWork.LastResult=value;}
}
public static class ShelterRules
{
 public static void Validate(CampaignDefinition c,Action<string,string,string> error)
 {
  if(c.Shelter is not {} s)return;
  if(c.SchemaVersion<12||!c.DailySurvival)error("schema","shelter","Shelter population requires schema 12 and daily survival.");
  var area=c.Areas.FirstOrDefault(a=>a.Id==s.AreaId);
  if(area==null||!area.Features.Any(f=>f.Id==s.StockpileFeatureId&&f.SecuringSite))error("shelter","shelter.stockpileFeatureId","Shelter must name a securing stockpile in its home area.");
  if(!c.Actors.Any(a=>a.Id==s.LeaderActorId&&!a.Enemy&&!a.Recruitable)||!c.Areas.Any(a=>a.Id==c.StartAreaId&&a.Actors.Any(p=>p.ActorId==s.LeaderActorId)))error("shelter","shelter.leaderActorId","A starting survivor must remain the expedition leader.");
  if(s.Capacity is <1 or >12)error("shelter","shelter.capacity","Shelter capacity must be 1–12.");
  if(s.Source==null||string.IsNullOrWhiteSpace(s.Source.AuditId)||s.Source.PageStart<1||s.Source.PageEnd<s.Source.PageStart||s.Source.PageEnd>513)error("source","shelter.source","Shelter requires a valid source/adaptation reference.");
 }
}
public sealed partial class World
{
 public List<ShelterResident> ShelterResidents {get;set;}=[];
 [JsonIgnore] public bool ShelterEnabled=>Campaign?.Shelter!=null;
 [JsonIgnore] public IEnumerable<Actor> CommunityMembers=>Party.Concat(ShelterResidents.Select(r=>r.Actor));
 [JsonIgnore] public int ShelterCapacity=>Campaign?.Shelter?.Capacity??0;
 public string ShelterManagementReason()
 {
  if(!ShelterEnabled)return "This campaign does not enable shelter population.";
  if(Mode!=Mode.Explore||PendingReaction!=null)return "Finish combat before managing shelter residents.";
  if(CurrentTravelDestinationId!=Campaign!.Shelter!.AreaId)return "Return to the office refuge to change the roster or assignments.";
  if(Party.Any(a=>!a.Alive))return "Every expedition member must be conscious for a roster change.";
  if(SurvivalStockpile()?.Id!=Campaign.Shelter.StockpileFeatureId)return "Establish the refuge, gather the party beside its stockpile and make the area safe.";
  return "";
 }
 public string HouseSurvivorReason(string actorId)
 {
  string reason=ShelterManagementReason();if(reason!="")return reason;
  var a=Party.FirstOrDefault(a=>a.Id==actorId);
  if(a==null||!a.Alive)return "Choose a conscious expedition member.";
  if(a.Id==Campaign!.Shelter!.LeaderActorId||PartyCount<=1)return "The starting leader must stay with the expedition.";
  if(ShelterResidents.Count>=ShelterCapacity)return "The refuge has no free resident spaces.";
  if((long)CampaignMinutes+1920>int.MaxValue)return "The campaign clock limit has been reached.";
  return "";
 }
 public bool HouseSurvivor(string actorId)
 {
  string reason=HouseSurvivorReason(actorId);if(reason!=""){LogSystem(reason,true);return false;}
  var a=Party.First(a=>a.Id==actorId);StopAll();ClearReactionInterrupts();Actors.Remove(a);PartyOrder.Remove(a.Id);Order.Remove(a.Id);
  a.ShelterWork.NextWorkMinute=Math.Max(checked(CampaignMinutes+480),a.ShelterWork.NextWorkMinute);
  if(a.ShelterWork.Duty==ShelterDuty.Steward&&ShelterResidents.Any(r=>!r.Actor.Dead&&r.Duty==ShelterDuty.Steward))a.ShelterWork.Duty=ShelterDuty.Recuperate;
  ShelterResidents.Add(new(){Actor=a});NormalizePartyOrder();Selected=0;
  Message(MessageCategory.Rest,$"{a.Name} stays at the refuge. Equipment, HP and daily needs are unchanged.");Reveal();return true;
 }
 Cell? ResidentArrival()
 {
  var site=ShelterHomeStockpile();if(site==null)return null;
  foreach(var cell in CampaignTravelRules.ArrivalCells(Floors,Floors.Where(Blocked),Actors.Where(a=>a.Alive).Select(a=>a.Pos),site.Pos))
   if(cell.Distance(site.Pos)<=3&&Features.All(f=>f.Pos!=cell)&&CanStand(cell.Center,null,true))return cell;
  return null;
 }
 public string RecallResidentReason(string actorId)
 {
  string reason=ShelterManagementReason();if(reason!="")return reason;
  var r=ShelterResidents.FirstOrDefault(r=>r.Actor.Id==actorId);
  if(r==null||!r.Actor.Alive)return "Only a conscious living resident can join the expedition.";
  if(PartyCount>=MaxPartySize)return "The expedition already has six members.";
  if(ResidentArrival()==null)return "Clear a legal arrival space beside the stockpile.";
  return "";
 }
 public bool RecallResident(string actorId)
 {
  string reason=RecallResidentReason(actorId);if(reason!=""){LogSystem(reason,true);return false;}
  var r=ShelterResidents.First(r=>r.Actor.Id==actorId);var cell=ResidentArrival()!.Value;StopAll();ClearReactionInterrupts();ShelterResidents.Remove(r);
  r.Actor.Pos=cell;r.Actor.Motion=0;Actors.Add(r.Actor);PartyOrder.Add(r.Actor.Id);NormalizePartyOrder();AssignMissingItemIds();Reveal();
  Message(MessageCategory.Rest,$"{r.Actor.Name} rejoins the expedition with their existing equipment and needs.");return true;
 }
 public string AssignShelterDutyReason(string actorId,ShelterDuty duty)
 {
  string reason=ShelterManagementReason();if(reason!="")return reason;
  if(!Enum.IsDefined(duty))return "Unknown shelter duty.";
  var r=ShelterResidents.FirstOrDefault(r=>r.Actor.Id==actorId);
  if(r==null||!r.Actor.Alive)return "Choose a conscious living resident.";
  if(duty==ShelterDuty.Steward&&ShelterResidents.Any(x=>x!=r&&!x.Actor.Dead&&x.Duty==duty))return "The refuge already has a supply steward.";
  return "";
 }
 public bool AssignShelterDuty(string actorId,ShelterDuty duty)
 {
  string reason=AssignShelterDutyReason(actorId,duty);if(reason!=""){LogSystem(reason,true);return false;}
  var r=ShelterResidents.First(r=>r.Actor.Id==actorId);if(r.Duty==duty)return true;
  r.Duty=duty;r.LastResult="Assignment changed; existing duty deadline retained.";
  Message(MessageCategory.Rest,$"{r.Actor.Name} assigned to {duty}. Next duty review at campaign minute {r.NextWorkMinute}.");return true;
 }
 Feature? ShelterHomeStockpile()
 {
  if(Campaign?.Shelter is not {} s)return null;
  return (s.AreaId==CurrentTravelDestinationId?Features:StoredTravelAreas.FirstOrDefault(a=>a.DestinationId==s.AreaId)?.Features)?.FirstOrDefault(f=>f.Id==s.StockpileFeatureId);
 }
 bool ShelterWorkSafe()
 {
  if(Campaign?.Shelter is not {} s||ShelterHomeStockpile() is not {} site)return false;
  var d=Campaign.Areas.First(a=>a.Id==s.AreaId).Features.First(f=>f.Id==site.Id);
  if(d.SecuringFlag!=""&&!HasDialogueFlag(d.SecuringFlag))return false;
  if(s.AreaId==CurrentTravelDestinationId)return !Actors.Any(a=>a.Enemy&&a.Alive&&a.Position.Distance(site.Pos.Center)<10&&Line(a.Position,site.Pos.Center));
  var home=StoredTravelAreas.FirstOrDefault(a=>a.DestinationId==s.AreaId);if(home==null)return false;
  var geometry=new World{Campaign=Campaign,CurrentTravelDestinationId=s.AreaId,Floors=home.Floors,Features=home.Features};
  return !home.Actors.Any(a=>a.Enemy&&a.Alive&&a.Position.Distance(site.Pos.Center)<10&&geometry.Line(a.Position,site.Pos.Center));
 }
 public int ShelterSupplyCount(string use)=>ShelterHomeStockpile()?.Contents.Items.Where(s=>SupplyUse(s)==use).Sum(SupplyUnits)??0;
 bool ConsumeResidentSupply(Actor a,string use,bool stockpile)
 {
  var inventory=a.Inventory;
  var stack=inventory.Items.FirstOrDefault(s=>SupplyUse(s)==use);
  if(stack==null&&stockpile&&ShelterHomeStockpile() is {} site){inventory=site.Contents;stack=inventory.Items.FirstOrDefault(s=>SupplyUse(s)==use);}
  if(stack==null)return false;
  if(!GreenhouseSubstitutesFood(use))ConsumeSupplyUnit(inventory,stack);a.QuickAccess.Normalize(a);a.Statistics.ItemsUsed++;return true;
 }
 int ProvisionResident(Actor a,int minute,bool stockpile)
 {
  if(!a.Alive)return 0;int used=0;
  if(!a.Needs.FoodTaken&&ConsumeResidentSupply(a,"food",stockpile)){a.Needs.FoodTaken=true;a.Needs.LastFoodMinute=minute;a.Needs.DaysWithoutFood=0;used++;}
  if(!a.Needs.WaterTaken&&ConsumeResidentSupply(a,"water",stockpile)){a.Needs.WaterTaken=true;a.Needs.LastWaterMinute=minute;a.Needs.DaysWithoutWater=0;used++;}
  return used;
 }
 int ProvisionResidentsAt(int minute,bool stockpile)
 {
  int spent=0;foreach(var r in ShelterResidents.OrderBy(r=>r.Actor.Id,StringComparer.Ordinal))spent+=ProvisionResident(r.Actor,minute,stockpile);return spent;
 }
 public int ProvisionShelter()
 {
  string reason=ShelterManagementReason();if(reason!=""){LogSystem(reason,true);return 0;}
  int n=ProvisionResidentsAt(CampaignMinutes,true);Message(MessageCategory.Rest,$"Resident provisioning spent {n} physical supplies. Already met daily requirements were not charged again.");return n;
 }
 public string TreatResidentReason(string actorId)
 {
  string reason=ShelterManagementReason();if(reason!="")return reason;
  var patient=ShelterResidents.FirstOrDefault(r=>r.Actor.Id==actorId)?.Actor;
  if(patient==null||patient.Dead)return "Choose a living resident; treatment cannot resurrect the dead.";
  if(patient.Hp>=patient.MaxHp)return "This resident is already at full HP.";
  if(patient.Needs.Treated)return "This resident has already received first aid since their last recovered long rest.";
  if(!CanResidentSupply(PartyLeader,"bandage")&&!CanResidentSupply(PartyLeader,"first-aid"))return "The expedition leader and refuge have no bandage or first-aid kit.";
  return "";
 }
 public bool TreatResident(string actorId)
 {
  string reason=TreatResidentReason(actorId);if(reason!=""){LogSystem(reason,true);return false;}
  var patient=ShelterResidents.First(r=>r.Actor.Id==actorId).Actor;var carer=PartyLeader;
  string use=CanResidentSupply(carer,"bandage")?"bandage":"first-aid";
  if(!ConsumeResidentSupply(carer,use,true))return false;
  int before=patient.Hp;ApplyResidentTreatment(carer,patient,use);
  Message(MessageCategory.Healing,$"{carer.Name} treats resident {patient.Name} with one {use}: +{patient.Hp-before} HP.");return true;
 }
 void ApplyResidentTreatment(Actor carer,Actor patient,string use)
 {
  int before=patient.Hp;int heal=Math.Max(1,(use=="bandage"?Roll(4):Roll(6)+Roll(6))+carer.AbilityModifier(Ability.Wisdom));patient.Hp=Math.Min(patient.MaxHp,patient.Hp+heal);patient.Character.SetCondition("unconscious","Unconscious",false);patient.Needs.Treated=true;
  carer.Statistics.HealingDone+=patient.Hp-before;patient.Statistics.HealingReceived+=patient.Hp-before;
 }
 static long DeprivationDue(Actor a)=>Math.Max(Math.Min((long)a.Needs.LastWaterMinute+2880,(long)a.Needs.LastFoodMinute+4320),(long)a.Needs.LastFatigueMinute+1440);
 // One chronological scheduler for field and shelter survivors. Shared stockpiles cannot be spent by two timelines.
 void ProcessCommunityTime()
 {
  while(true)
  {
   var living=CommunityMembers.Where(a=>!a.Dead).ToArray();
   long next=living.Select(a=>Math.Min((long)a.Needs.NextDailyMinute,DeprivationDue(a))).Concat(ShelterResidents.Where(r=>!r.Actor.Dead).Select(r=>(long)r.NextWorkMinute)).DefaultIfEmpty(long.MaxValue).Min();
   if(next>CampaignMinutes)break;int minute=(int)next;var resetting=living.Where(a=>a.Needs.NextDailyMinute==minute).ToArray();
   foreach(var a in resetting)
   {
    Message(MessageCategory.Rest,$"Daily needs — {a.Name}: food {(a.Needs.FoodTaken?"met":"missed")}, water {(a.Needs.WaterTaken?"met":"missed")}.");
    a.Needs.FoodTaken=false;a.Needs.WaterTaken=false;a.Needs.NextDailyMinute=checked(minute+1440);
   }
   bool safe=ShelterWorkSafe();
   foreach(var r in ShelterResidents.Where(r=>resetting.Contains(r.Actor)))ProvisionResident(r.Actor,minute,false);
   var steward=ShelterResidents.FirstOrDefault(r=>r.Actor.Alive&&r.Duty==ShelterDuty.Steward);
   if(resetting.Length>0&&safe&&steward!=null)
   {
    int used=ProvisionResident(steward.Actor,minute,true);
    if(steward.Actor.Needs.FoodTaken&&steward.Actor.Needs.WaterTaken)used+=ProvisionResidentsAt(minute,true);
    steward.SuppliesSpent=checked(steward.SuppliesSpent+used);
   }
   foreach(var r in ShelterResidents.Where(r=>!r.Actor.Dead&&r.NextWorkMinute==minute).OrderBy(r=>r.Actor.Id,StringComparer.Ordinal).ToArray())
   {
    r.NextWorkMinute=checked(minute+1440);RunShelterDuty(r,minute,safe);
   }
   foreach(var a in living.Where(a=>!a.Dead&&DeprivationDue(a)<=minute))
   {
    a.Needs.LastFatigueMinute=minute;a.Needs.Exhaustion=Math.Min(6,a.Needs.Exhaustion+1);
    Message(MessageCategory.Rest,$"{a.Name} gains one survival fatigue level ({a.Needs.Exhaustion}/6).");
    if(a.Needs.Exhaustion==6){a.Hp=0;a.Character.SetCondition("dead","Dead",true,"Survival exhaustion");Message(MessageCategory.Damage,$"{a.Name} dies from survival exhaustion.");}
   }
  }
  foreach(var a in CommunityMembers.Where(a=>!a.Dead)){a.Needs.DaysWithoutFood=Math.Max(0,(CampaignMinutes-a.Needs.LastFoodMinute)/1440);a.Needs.DaysWithoutWater=Math.Max(0,(CampaignMinutes-a.Needs.LastWaterMinute)/1440);}
  if(!Party.Any(a=>!a.Dead))Mode=Mode.Defeat;
 }
 void RunShelterDuty(ShelterResident r,int minute,bool safe)
 {
  var a=r.Actor;string result;
  if(!safe)result="Duty paused: the refuge is not safe.";
  else if(!a.Alive&&r.Duty!=ShelterDuty.Recuperate)result="Duty paused: survivor is unconscious.";
  else if(!a.Needs.FoodTaken||!a.Needs.WaterTaken)result="Duty paused: daily food or water is missing.";
  else
  {
   switch(r.Duty)
   {
    case ShelterDuty.Recuperate:
     if(a.Needs.NextLongRestMinute>minute){result="Recuperation cooldown is still active.";break;}
     a.Character.RestoreLongRest();RecoverLongRestHitDice(a);a.Needs.Exhaustion=Math.Max(0,a.Needs.Exhaustion-1);a.Needs.Treated=false;a.Needs.NextLongRestMinute=checked(minute+960);a.Statistics.RestsTaken++;r.CompletedShifts++;result="Completed fed, hydrated recuperation.";break;
    case ShelterDuty.Steward:
     int supplied=ProvisionResidentsAt(minute,true);r.SuppliesSpent=checked(r.SuppliesSpent+supplied);r.CompletedShifts++;result=$"Supply review complete; {supplied} food/water units used.";break;
    default:
     var patient=ShelterResidents.Select(x=>x.Actor).Where(p=>!p.Dead&&p.Hp<p.MaxHp&&!p.Needs.Treated).OrderBy(p=>p.Hp/(double)p.MaxHp).ThenBy(p=>p.Id,StringComparer.Ordinal).FirstOrDefault();
     if(patient==null){r.CompletedShifts++;result="Care review complete; no eligible injured resident.";break;}
     string use=CanResidentSupply(a,"bandage")?"bandage":"first-aid";
     if(!ConsumeResidentSupply(a,use,true)){result="Care paused: no bandage or first-aid kit.";break;}
     int before=patient.Hp;ApplyResidentTreatment(a,patient,use);r.SuppliesSpent++;r.CompletedShifts++;result=$"Used {use} for {patient.Name}: +{patient.Hp-before} HP.";break;
   }
  }
  r.LastResult=result;Message(MessageCategory.Rest,$"Shelter duty — {a.Name}: {result}");
  AddStoryJournalEntry($"shelter-duty-{a.Id}-{minute}","Shelter Duty",$"{a.Name} ({r.Duty}): {result}","Shelter",CurrentJournalChapter,minute);
 }
 bool CanResidentSupply(Actor a,string use)=>a.Inventory.Items.Any(s=>SupplyUse(s)==use)||ShelterSupplyCount(use)>0;
 void ValidateShelterShape()
 {
  if(ShelterResidents==null||ShelterResidents.Any(r=>r?.Actor==null||r.Actor.ShelterWork==null))throw new InvalidDataException("Null shelter roster.");
 }
 void ValidateShelterState()
 {
  ValidateShelterShape();
  foreach(var a in Actors.Concat(ShelterResidents.Select(r=>r.Actor)))
  {
   var d=a.ShelterWork;if(d==null||!Enum.IsDefined(d.Duty)||d.NextWorkMinute<0||d.CompletedShifts<0||d.SuppliesSpent<0||d.LastResult==null||d.LastResult.Length>300)throw new InvalidDataException("Invalid survivor duty history.");
  }
  if(!ShelterEnabled){if(ShelterResidents.Count>0)throw new InvalidDataException("Residents in a campaign without shelter population.");return;}
  var s=Campaign!.Shelter!;
  if(ShelterResidents.Count>s.Capacity||!Actors.Any(a=>!a.Enemy&&a.Id==s.LeaderActorId))throw new InvalidDataException("Invalid shelter capacity or missing expedition leader.");
  var all=Actors.Concat(ShelterResidents.Select(r=>r.Actor)).Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).ToArray();
  if(all.Select(a=>a.Id).Distinct(StringComparer.Ordinal).Count()!=all.Length)throw new InvalidDataException("Duplicate community actor identity.");
  if(ShelterResidents.Count>0&&ShelterHomeStockpile()==null)throw new InvalidDataException("Resident home is not materialized.");
  var home=Campaign.Areas.First(a=>a.Id==s.AreaId);
  foreach(var r in ShelterResidents)
  {
   var a=r.Actor;
   if(a.Enemy||a.Id==s.LeaderActorId||Campaign.Actors.All(d=>d.Id!=a.Id||d.Enemy)||PartyOrder.Contains(a.Id)||!Enum.IsDefined(r.Duty)||r.CompletedShifts<0||r.SuppliesSpent<0||r.LastResult==null||r.LastResult.Length>300||r.NextWorkMinute<0||!a.Dead&&(r.NextWorkMinute<=CampaignMinutes||(long)r.NextWorkMinute>(long)CampaignMinutes+1440)||!float.IsFinite(a.Position.X)||!float.IsFinite(a.Position.Y)||!home.Floors.Contains(a.Pos))throw new InvalidDataException("Invalid resident identity, assignment or duty clock.");
   a.Character.ValidateOrThrow(a.Id);a.Inventory.ValidateOrThrow(a.Id);a.Equipment.ValidateOrThrow(a.Id);
  }
  if(ShelterResidents.Count(r=>!r.Actor.Dead&&r.Duty==ShelterDuty.Steward)>1)throw new InvalidDataException("More than one active supply steward.");
 }
}
