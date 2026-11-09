using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed class CampaignExpedition
{
 public string Title {get;set;}="";
 public string Briefing {get;set;}="";
 public string DebriefFeatureId {get;set;}="";
 public string StockpileFeatureId {get;set;}="";
 public int MinimumStoredSv {get;set;}=10;
 public string SuccessText {get;set;}="";
 public List<CampaignExpeditionStage> Stages {get;set;}=[];
}
public sealed class CampaignExpeditionStage
{
 public string Id {get;set;}="";
 public string Title {get;set;}="";
 public string Hint {get;set;}="";
 public List<string> ObjectiveIds {get;set;}=[];
}
public sealed class ExpeditionReport
{
 public int CompletedMinute {get;set;}
 public int ElapsedMinutes {get;set;}
 public int EarnedSv {get;set;}
 public int StoredSv {get;set;}
 public int ResourceXp {get;set;}
 public int PooledXp {get;set;}
 public List<string> PartyIds {get;set;}=[];
}
public sealed partial class World
{
 public ExpeditionReport? CompletedExpedition {get;set;}
 [JsonIgnore] public Action? ExpeditionReported {get;set;}
 [JsonIgnore] public CampaignExpeditionStage? CurrentExpeditionStage=>Campaign?.Expedition?.Stages.FirstOrDefault(s=>!ExpeditionStageComplete(s));
 public bool ExpeditionStageComplete(CampaignExpeditionStage stage)=>stage.ObjectiveIds.All(id=>Quest(Campaign!.Id+".objectives")?.Objectives.Any(o=>o.Id==id&&o.Completed)==true);
 public int StoredCreditedSv(Feature site)
 {
  var catalogue=ResourceCatalogue();return site.Contents.Items.SelectMany(StackResourceUnits).Where(SecuredResourceUnits.Contains).Sum(id=>catalogue[id].Sv);
 }
 void SyncExpeditionStageJournal()
 {
  if(Campaign?.Expedition is not {} expedition)return;
  foreach(var stage in expedition.Stages.Where(ExpeditionStageComplete))AddStoryJournalEntry(stage.Id+".complete",stage.Title+" Complete","Completed: "+string.Join(" ",stage.ObjectiveIds.Select(id=>Campaign.Objectives.First(o=>o.Id==id).Text)),"Expedition");
 }
 public string ExpeditionReturnReason(Feature report)
 {
  if(Campaign?.Expedition is not {} expedition||!Features.Contains(report)||report.Id!=expedition.DebriefFeatureId||report.Kind!="debrief")return "That is not this expedition's report point.";
  if(Mode!=Mode.Explore||PendingReaction!=null)return "Finish combat before reporting back.";
  if(!IsFeatureDiscovered(report))return "Find the report point in Wanderlust's back office.";
  if(CompletedExpedition!=null)return "";
  if(CurrentExpeditionStage is {} stage)return "Still to do: "+stage.Hint;
  if(Party.Any(a=>!a.Alive))return "Every expedition member must be conscious before reporting back.";
  if(Party.Any(a=>a.Position.Distance(report.Pos.Center)>4||!Line(a.Position,report.Pos.Center)))return "Gather everyone inside the back office, within four tiles of the report point.";
  var site=Features.FirstOrDefault(f=>f.Id==expedition.StockpileFeatureId);
  if(site==null||StoredCreditedSv(site)<expedition.MinimumStoredSv)return $"Return at least {expedition.MinimumStoredSv} SV of credited supplies to the shelter stockpile.";
  var siteDefinition=CurrentCampaignArea!.Features.First(f=>f.Id==site.Id);
  if(siteDefinition.SecuringFlag!=""&&!HasDialogueFlag(siteDefinition.SecuringFlag))return "Establish this shelter before reporting back.";
  if(Actors.Any(e=>e.Enemy&&e.Alive&&e.Position.Distance(site.Pos.Center)<=7&&Line(e.Position,site.Pos.Center)))return "Make the shelter safe before reporting back.";
  return "";
 }
 public bool ReportExpedition(Feature feature)
 {
  SyncCampaignObjectives();string reason=ExpeditionReturnReason(feature);if(reason!=""){LogSystem(reason,true);return false;}
  if(CompletedExpedition!=null){ExpeditionReported?.Invoke();return true;}
  var expedition=Campaign!.Expedition!;var site=Features.First(f=>f.Id==expedition.StockpileFeatureId);
  CompletedExpedition=new(){CompletedMinute=CampaignMinutes,ElapsedMinutes=CampaignMinutes-Campaign.StartMinute,EarnedSv=SecuredSurvivalValue,StoredSv=StoredCreditedSv(site),ResourceXp=ResourceXpAwarded,PooledXp=ResourceXpRemainder,PartyIds=Party.Select(a=>a.Id).ToList()};
  feature.Used=true;CompleteQuest(Campaign.Id+".objectives",expedition.SuccessText);
  AddStoryJournalEntry("expedition-completed",expedition.Title+" Complete",expedition.SuccessText+$" Returned: {CompletedExpedition.PartyIds.Count} survivors. Supplies: {CompletedExpedition.StoredSv} SV in stock. Earned: {CompletedExpedition.EarnedSv} SV; shared XP distributed: {CompletedExpedition.ResourceXp}.","Expedition");
  StopAll();Message(MessageCategory.Quest,expedition.Title+" complete.",expedition.SuccessText);ExpeditionReported?.Invoke();return true;
 }
 void ValidateExpeditionState()
 {
  if(CompletedExpedition is not {} report){if(Campaign?.Expedition!=null&&Quest(Campaign.Id+".objectives")?.Status==QuestStatus.Completed)throw new InvalidDataException("Completed expedition quest has no report.");return;}
  var expedition=Campaign?.Expedition??throw new InvalidDataException("Report has no expedition definition.");
  if(report.PartyIds==null||report.PartyIds.Count is <1 or >MaxPartySize||report.PartyIds.Distinct().Count()!=report.PartyIds.Count||report.PartyIds.Any(id=>Campaign!.Actors.All(a=>a.Id!=id||a.Enemy)))throw new InvalidDataException("Invalid expedition roster.");
  if(report.CompletedMinute<Campaign!.StartMinute||report.CompletedMinute>CampaignMinutes||report.ElapsedMinutes!=report.CompletedMinute-Campaign.StartMinute||report.StoredSv<expedition.MinimumStoredSv||report.StoredSv>report.EarnedSv||report.EarnedSv>SecuredSurvivalValue||report.ResourceXp<0||report.PooledXp is <0 or >5||(long)report.ResourceXp+report.PooledXp!=report.EarnedSv)throw new InvalidDataException("Invalid expedition totals or completion time.");
  var quest=Quest(Campaign.Id+".objectives");if(quest?.Status!=QuestStatus.Completed||expedition.Stages.Any(s=>!ExpeditionStageComplete(s)))throw new InvalidDataException("Expedition report disagrees with objective state.");
  var features=Features.Concat(StoredTravelAreas.SelectMany(a=>a.Features));if(!features.Any(f=>f.Id==expedition.DebriefFeatureId&&f.Used))throw new InvalidDataException("Expedition report has no completed report point.");
 }
}
