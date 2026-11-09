using System.Text.Json.Serialization;
namespace Ashen.Core;

public sealed class SurvivalNeeds
{
 public int LastFoodMinute {get;set;}
 public int LastWaterMinute {get;set;}
 public int LastFatigueMinute {get;set;}=-1440;
 public int NextDailyMinute {get;set;}
 public bool FoodTaken {get;set;}
 public bool WaterTaken {get;set;}
 public int DaysWithoutFood {get;set;}
 public int DaysWithoutWater {get;set;}
 public int Exhaustion {get;set;}
 public bool Treated {get;set;}
 public int NextLongRestMinute {get;set;}
}
public sealed partial class World
{
 [JsonIgnore] public bool DailySurvivalEnabled=>Campaign?.DailySurvival==true;
 public void ProcessSurvivalTime()
 {
  if(CampaignEnded)return;
  TickExterior30();TickRoof28();TickUpperMall27();UpdateLowerEffects();ProcessInfrastructureTime();UpdateEquipmentTime();UpdateSurvivorEffects(0);
  if(!DailySurvivalEnabled){ProcessTensionTime();SyncContracts();ProcessFinaleTime();return;}
  if(ShelterEnabled){ProcessCommunityTime();ProcessTensionTime();SyncContracts();ProcessFinaleTime();return;}
  foreach(var a in Party.Where(a=>!a.Dead))
  {
   var n=a.Needs;
   while(!a.Dead)
   {
    long fatigueDue=Math.Max(Math.Min((long)n.LastWaterMinute+2880,(long)n.LastFoodMinute+4320),(long)n.LastFatigueMinute+1440);
    long next=Math.Min(n.NextDailyMinute,fatigueDue);if(next>CampaignMinutes)break;
    if(next==n.NextDailyMinute)
    {
     Message(MessageCategory.Rest,$"Daily needs — {a.Name}: food {(n.FoodTaken?"met":"missed")}, water {(n.WaterTaken?"met":"missed")}.");
     n.FoodTaken=false;n.WaterTaken=false;n.NextDailyMinute=checked(n.NextDailyMinute+1440);
    }
    if(next==fatigueDue)
    {
     n.LastFatigueMinute=(int)fatigueDue;n.Exhaustion=Math.Min(6,n.Exhaustion+1);
     Message(MessageCategory.Rest,$"{a.Name} gains one survival fatigue level ({n.Exhaustion}/6). Food or water deprivation persists.");
     if(n.Exhaustion==6){a.Hp=0;a.Character.SetCondition("dead","Dead",true,"Survival exhaustion");Message(MessageCategory.Damage,$"{a.Name} dies from survival exhaustion.");}
    }
   }
   n.DaysWithoutFood=Math.Max(0,(CampaignMinutes-n.LastFoodMinute)/1440);n.DaysWithoutWater=Math.Max(0,(CampaignMinutes-n.LastWaterMinute)/1440);
  }
  ProcessTensionTime();SyncContracts();ProcessFinaleTime();
  if(!Party.Any(a=>!a.Dead))Mode=Mode.Defeat;
 }
 void AdvanceSurvivalCombatRound()
 {
  UpdateSurvivorEffects(6);
  if(TensionEnabled&&Actors.Any(a=>a.Enemy&&a.Alive&&Order.Contains(a.Id)))ChangeTension(1,"Combat round");
  TickRoof28();TickUpperMall27();UpdateLowerEffects();ProcessInfrastructureTime();UpdateEquipmentTime();UpdateSurvivorEffects(0);
  ProcessTensionTime();SyncContracts();
  if(!DailySurvivalEnabled)return;
  CampaignSeconds+=6;TickExterior30();if(CampaignSeconds>=60){CampaignMinutes=checked(CampaignMinutes+1);CampaignSeconds-=60;ProcessSurvivalTime();}
 }
 public string SurvivalSummary(Actor a)=>!DailySurvivalEnabled?"Daily survival inactive":$"Food {(a.Needs.FoodTaken?"met":"due")} / water {(a.Needs.WaterTaken?"met":"due")} | fatigue {a.Needs.Exhaustion}/6";
 Feature? SurvivalStockpile()
 {
  if(!DailySurvivalEnabled||Mode!=Mode.Explore||PendingReaction!=null||!Party.Any(a=>a.Alive)||HostilesTooCloseToRest(out _))return null;
  return Features.FirstOrDefault(f=>IsSecuringSite(f)&&CurrentCampaignArea!.Features.Any(d=>d.Id==f.Id&&(d.SecuringFlag==""||HasDialogueFlag(d.SecuringFlag)))&&Party.Where(a=>!a.Dead).All(a=>a.Position.Distance(f.Pos.Center)<=5&&Line(a.Position,f.Pos.Center)));
 }
 int SupplyUnits(ItemStack s)=>s.Definition.MaxCharges>0?Math.Max(0,s.Charges):s.Quantity;
 void ConsumeSupplyUnit(InventoryState inventory,ItemStack stack)
 {
  stack.ConsumeResourceUnits(1);if(stack.Definition.MaxCharges>0){stack.Charges--;if(stack.Charges>0)return;}stack.Quantity--;if(stack.Quantity==0)inventory.Items.Remove(stack);
 }
 string SupplyUse(ItemStack s)=>Campaign?.Items.FirstOrDefault(i=>i.Id==s.ItemId)?.SurvivalUse??"";
 IEnumerable<InventoryState> SurvivalSources(Actor a)
 {
  yield return a.Inventory;if(SurvivalStockpile() is {} f)yield return f.Contents;
 }
 public int AvailableSurvivalSupply(Actor a,string use)=>DailySurvivalEnabled&&Actors.Contains(a)&&!a.Enemy?SurvivalSources(a).SelectMany(i=>i.Items).Where(s=>SupplyUse(s)==use).Sum(SupplyUnits):0;
 public string SurvivalSupplyReason(Actor carer,Actor patient,string use)
 {
  if(!DailySurvivalEnabled||Mode!=Mode.Explore||PendingReaction!=null)return "Supplies can be used during exploration outside combat.";
  if(!Party.Contains(carer)||!Party.Contains(patient)||!carer.Alive||patient.Dead)return "Choose a conscious party member and a living patient.";
  if(Actors.Any(e=>e.Enemy&&e.Alive&&e.Position.Distance(carer.Position)<10))return "Make the immediate area safe first.";
  if(use is "food" or "water")
  {
   if(carer!=patient)return "Each survivor uses their own carried supplies or the nearby refuge stockpile.";
   if(use=="food"?patient.Needs.FoodTaken:patient.Needs.WaterTaken)return "This daily requirement is already met.";
  }
  else if(use is "bandage" or "first-aid")
  {
   if(carer.Position.Distance(patient.Position)>1.5f||!PhysicalAttackLine(carer.Position,patient.Position))return "Stand beside the patient with a clear path.";
   if(patient.Hp>=patient.MaxHp)return "The patient is already at full HP.";
   if(patient.Needs.Treated)return "This patient has already received first aid since their last long rest.";
  }
  else return "Unknown survival supply.";
  if(AvailableSurvivalSupply(carer,use)<1)return "No suitable physical supply is available in this backpack or nearby established refuge.";
  return "";
 }
 public bool UseSurvivalSupply(Actor carer,Actor patient,string use,string? instanceId=null)
 {
  string reason=SurvivalSupplyReason(carer,patient,use);if(reason!=""){LogSystem(reason,true);return false;}
  if(instanceId!=null&&!SurvivalSources(carer).SelectMany(i=>i.Items).Any(s=>s.InstanceId==instanceId&&SupplyUse(s)==use))return false;
  var inventory=SurvivalSources(carer).First(i=>i.Items.Any(s=>SupplyUse(s)==use&&(instanceId==null||s.InstanceId==instanceId)));var stack=inventory.Items.First(s=>SupplyUse(s)==use&&(instanceId==null||s.InstanceId==instanceId));
  Stop(carer);if(!GreenhouseSubstitutesFood(use))ConsumeSupplyUnit(inventory,stack);carer.QuickAccess.Normalize(carer);carer.Statistics.ItemsUsed++;
  if(use=="food"){patient.Needs.FoodTaken=true;patient.Needs.DaysWithoutFood=0;patient.Needs.LastFoodMinute=CampaignMinutes;}
  else if(use=="water"){patient.Needs.WaterTaken=true;patient.Needs.DaysWithoutWater=0;patient.Needs.LastWaterMinute=CampaignMinutes;}
  else
  {
   patient.Character.SetCondition("final-bleeding","Bleeding",false);LowerMallEffects.RemoveAll(e=>e.ActorId==patient.Id&&e.Condition=="final-bleeding");
   int healing=Math.Max(1,(use=="first-aid"?Roll(6)+Roll(6):Roll(4))+carer.AbilityModifier(Ability.Wisdom)),before=patient.Hp;
   patient.Hp=Math.Min(patient.MaxHp,patient.Hp+healing);patient.Character.SetCondition("unconscious","Unconscious",false);patient.Needs.Treated=true;
   carer.Statistics.HealingDone+=patient.Hp-before;patient.Statistics.HealingReceived+=patient.Hp-before;LogHealing(patient,patient.Hp-before,use);Sound?.Invoke("heal");
  }
  Message(MessageCategory.Rest,$"{carer.Name} uses one {use} supply for {patient.Name}. Secured XP is unchanged.");return true;
 }
 public int SupplyParty()
 {
  int spent=0;foreach(var a in Party.Where(a=>a.Alive))foreach(var use in new[]{"food","water"})if(SurvivalSupplyReason(a,a,use)==""&&UseSurvivalSupply(a,a,use))spent++;return spent;
 }
 RestPreview PreviewSurvivalRest(RestKind kind)
 {
  int minutes=kind==RestKind.ShortRest?60:480;
  string reason=Mode!=Mode.Explore||PendingReaction!=null?"Finish combat before resting.":SurvivalStockpile()==null?"Gather all living survivors within 25 feet of the established refuge stockpile, with clear sight and no nearby hostile.":kind==RestKind.LongRest&&Party.Any(a=>!a.Dead&&a.Needs.NextLongRestMinute>CampaignMinutes)?"Long rest recovery is available once per 24 hours. Take a short rest or continue exploring.":(long)CampaignMinutes+minutes+1440>int.MaxValue?"The campaign clock limit has been reached.":"";
  return new(kind,reason=="",reason,minutes,0,0,CurrentLowerMallSite?.Level=="roof"?CurrentLowerMallSite.Name:"Office refuge","Party backpacks and this stockpile supply daily needs. Supplies are finite; enabled campaigns check daily incidents at midnight. Queued attacks deploy when exploration resumes.",kind==RestKind.ShortRest?"One hour. Spend selected Hit Dice; first aid does not reset.":"Eight hours. First use available daily food/water. Fed and hydrated survivors regain full HP and lose one fatigue level. Missing supplies prevent recovery. First aid resets for rested survivors. Once per 24 hours.");
 }
 RestResult PerformSurvivalRest(RestKind kind,IEnumerable<HitDieSpendRequest>? requests)
 {
  var preview=PreviewSurvivalRest(kind);if(!preview.Available)return new(false,false,0,0,"",preview.Reason);
  StopAll();RestCount++;LastRestEventId="";
  // Record provisioning before time crosses a personal daily boundary.
  var provisioned=new HashSet<string>();if(kind==RestKind.LongRest){SupplyParty();foreach(var a in Party.Where(a=>!a.Dead&&a.Needs.FoodTaken&&a.Needs.WaterTaken))provisioned.Add(a.Id);}
  CampaignMinutes=checked(CampaignMinutes+preview.Minutes);ProcessSurvivalTime();
  if(kind==RestKind.ShortRest)ApplyShortRestRecovery(requests);
  else foreach(var a in Party.Where(a=>!a.Dead))
  {
   Roof28LongRest(a);a.Needs.NextLongRestMinute=checked(CampaignMinutes+960);
   if(!provisioned.Contains(a.Id)){Message(MessageCategory.Rest,$"{a.Name} rests but lacks daily food/water; no long-rest recovery.");continue;}
   a.Character.RestoreLongRest();RecoverLongRestHitDice(a);a.Needs.Exhaustion=Math.Max(0,a.Needs.Exhaustion-1);a.Needs.Treated=false;
  }
  foreach(var a in Party.Where(a=>!a.Dead))a.Statistics.RestsTaken++;
  Message(MessageCategory.Rest,$"Refuge {(kind==RestKind.ShortRest?"short":"long")} rest complete. {CampaignTimeText}. Physical supplies consumed; secured XP retained.");return new(true,false,preview.Minutes,0,"","Refuge rest complete.");
 }
 void ValidateSurvivalState()
 {
  foreach(var a in Actors.Concat(ShelterResidents.Select(r=>r.Actor)).Concat(StoredTravelAreas.SelectMany(s=>s.Actors)))
  {
   var n=a.Needs;if(n==null||n.LastFoodMinute<0||n.LastFoodMinute>CampaignMinutes||n.LastWaterMinute<0||n.LastWaterMinute>CampaignMinutes||n.LastFatigueMinute< -1440||n.LastFatigueMinute>CampaignMinutes||n.DaysWithoutFood is <0 or >1000000||n.DaysWithoutWater is <0 or >1000000||n.Exhaustion is <0 or >6||n.NextLongRestMinute<0||n.NextDailyMinute<0)throw new InvalidDataException("Invalid survival needs.");
   if(DailySurvivalEnabled&&!a.Enemy&&(n.NextDailyMinute<=CampaignMinutes||(long)n.NextDailyMinute> (long)CampaignMinutes+1440||(long)n.NextLongRestMinute>(long)CampaignMinutes+1440||n.Exhaustion==6&&!a.Dead))
   {
    // Dead survivors stop their clock; living actors must have no unsettled daily boundary.
    if(!a.Dead)throw new InvalidDataException("Invalid survivor daily clock.");
   }
  }
 }
}
