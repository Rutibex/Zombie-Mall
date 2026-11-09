namespace Ashen.Core;
public sealed class CampaignFinale
{
 public string RefugeFeatureId {get;set;}="";
 public string HelicopterFeatureId {get;set;}="";
 public string CrisisFeatureId {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public string SourceText {get;set;}="";
 public List<MysteryEvidence> Evidence {get;set;}=[];
}
public sealed class MysteryEvidence
{
 public string Id {get;set;}="";
 public int Location {get;set;}
 public string AreaId {get;set;}="";
 public string Title {get;set;}="";
 public string Text {get;set;}="";
 public string Thread {get;set;}="";
 public SourceReference Source {get;set;}=new();
}
public sealed class FinaleRoll
{
 public string Action {get;set;}="";
 public int Minute {get;set;}
 public int Roll {get;set;}
 public int Bonus {get;set;}
 public int Dc {get;set;}
 public bool Success {get;set;}
 public int TableRoll {get;set;}
 public string ActorId {get;set;}="";
 public int FuelSpent {get;set;}
 public int MaterialSpent {get;set;}
}
public sealed class WinterWeek
{
 public int Week {get;set;}
 public int Minute {get;set;}
 public int EventRoll {get;set;}
 public int SupplyRoll {get;set;}
 public int SupplyBonus {get;set;}
 public int SupplyDc {get;set;}
 public int FortRoll {get;set;}
 public int FortBonus {get;set;}
 public int FoodLost {get;set;}
 public int WaterLost {get;set;}
 public int FoodAdded {get;set;}
 public int WaterAdded {get;set;}
 public string Result {get;set;}="";
}
public sealed class EndingRecord
{
 public string Id {get;set;}="";
 public string Title {get;set;}="";
 public string Text {get;set;}="";
 public int Minute {get;set;}
 public string Theory {get;set;}="";
 public List<string> Evacuated {get;set;}=[];
 public List<string> LeftBehind {get;set;}=[];
 public List<string> Dead {get;set;}=[];
 public List<string> EvidenceIds {get;set;}=[];
 public Dictionary<string,int> Factions {get;set;}=[];
 public Dictionary<string,int> Trust {get;set;}=[];
 public int SecuredLocations {get;set;}
 public int ResourceXp {get;set;}
 public int FuelConsumed {get;set;}
}
public sealed class FinaleState
{
 public uint Seed {get;set;}
 public Dictionary<int,string> Clues {get;set;}=[];
 public string Theory {get;set;}="undetermined";
 public int TheoryMinute {get;set;}=-1;
 public int Morale {get;set;}=50;
 public int ReadyUntil {get;set;}=-1;
 public bool Inspected {get;set;}
 public bool HiddenFlaw {get;set;}
 public List<string> Repairs {get;set;}=[];
 public bool RarePartNeeded {get;set;}
 public bool Followup {get;set;}
 public int DelayUntil {get;set;}=-1;
 public int FlightBonus {get;set;}
 public bool Efficient {get;set;}
 public int FuelSpent {get;set;}
 public int Fuel {get;set;}
 public string PilotId {get;set;}="";
 public bool Airworthy {get;set;}
 public List<FinaleRoll> Rolls {get;set;}=[];
 public int WinterStart {get;set;}=-1;
 public int NextFreezeAttempt {get;set;}=-1;
 public int FinalWinterBonus {get;set;}
 public bool Emergency {get;set;}
 public List<WinterWeek> Weeks {get;set;}=[];
 public List<string> Volunteers {get;set;}=[];
 public string Crisis {get;set;}="";
 public int CrisisStart {get;set;}=-1;
 public int CrisisDeadline {get;set;}=-1;
 public EndingRecord? Ending {get;set;}
}
public static class FinaleRules
{
 public static void Validate(CampaignDefinition c,Action<string,string,string> error)
 {
  var d=c.Finale;if(d==null)return;if(c.SchemaVersion<30||c.LowerMall?.Sites.Count!=159||c.Shelter==null||c.Tension==null||d.SourceText.Length<5000||d.Evidence==null||d.Evidence.Count>1000){error("finale","definition","Complete location campaign and bounded ending source required.");return;}
  foreach(var id in new[]{d.RefugeFeatureId,d.HelicopterFeatureId,d.CrisisFeatureId})if(!c.Areas.Any(a=>a.Features.Any(f=>f.Id==id&&f.Kind=="location")))error("finale",id,"Physical finale console missing.");
  if(d.Evidence.Select(e=>e.Id).Distinct().Count()!=d.Evidence.Count)error("finale","evidence","Duplicate evidence identity.");
  foreach(var e in d.Evidence)if(e.Thread is not ("biodyne" or "cult" or "military" or "anomaly")||!c.LowerMall!.Sites.Any(s=>s.Number==e.Location&&s.AreaId==e.AreaId&&s.Actions.Any(a=>a.Id==e.Id&&a.Kind=="lore"))||e.Text.Length==0)error("finale",e.Id,"Evidence must reference an actual source lore action.");
 }
}
