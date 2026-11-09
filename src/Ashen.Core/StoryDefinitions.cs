namespace Ashen.Core;
public sealed class CampaignStory
{
 public string AreaId {get;set;}="";
 public string BoardFeatureId {get;set;}="";
 public List<CampaignContract> Contracts {get;set;}=[];
 public List<CampaignEncounterPlan> EncounterPlans {get;set;}=[];
}
public sealed class CampaignContract
{
 public string Id {get;set;}="";
 public string Title {get;set;}="";
 public string Description {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public List<string> Prerequisites {get;set;}=[];
 public List<string> RequiredFlags {get;set;}=[];
 public string BranchGroup {get;set;}="";
 public bool RequiresExpedition {get;set;}
 public int DeadlineMinutes {get;set;}
 public string FailureActorId {get;set;}="";
 public string FailureFlag {get;set;}="";
 public List<CampaignContractGoal> Goals {get;set;}=[];
 public int RewardXp {get;set;}
 public List<CampaignStack> Rewards {get;set;}=[];
}
public sealed class CampaignContractGoal
{
 public string Id {get;set;}="";
 public string Text {get;set;}="";
 public string Kind {get;set;}="flag";
 public string TargetId {get;set;}="";
 public int Quantity {get;set;}=1;
}
public sealed class CampaignEncounterPlan
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public string AreaId {get;set;}="";
 public string TriggerFeatureId {get;set;}="";
 public string RequiredContractId {get;set;}="";
 public List<string> Waves {get;set;}=[];
}
public sealed class ContractState
{
 public string Id {get;set;}="";
 public string Status {get;set;}="active";
 public int AcceptedMinute {get;set;}
 public int ResolvedMinute {get;set;}=-1;
 public int BaselineSv {get;set;}
 public List<int> Progress {get;set;}=[];
}
public sealed class EncounterPlanState
{
 public string Id {get;set;}="";
 public int NextWave {get;set;}
 public string Status {get;set;}="active";
}
public static class StoryRules
{
 public static void Validate(CampaignDefinition c,Action<string,string,string> error)
 {
  var d=c.Story;if(d==null){if(c.Areas.SelectMany(a=>a.Features).Any(f=>f.Kind is "quest-board" or "encounter"))error("story","features","Story features require story definitions.");return;}
  bool Id(string s)=>!string.IsNullOrWhiteSpace(s)&&System.Text.RegularExpressions.Regex.IsMatch(s,"^[a-z0-9][a-z0-9._-]*$");
  bool Source(SourceReference? s)=>s!=null&&s.AuditId!=""&&s.PageStart>=1&&s.PageEnd>=s.PageStart&&s.PageEnd<=513;
  if(c.SchemaVersion<18||!c.SurvivorClasses||c.Shelter==null||d.AreaId!=c.Shelter.AreaId||!c.Areas.Any(a=>a.Id==d.AreaId&&a.Features.Any(f=>f.Id==d.BoardFeatureId&&f.Kind=="quest-board")))error("story","story","Schema 18 and a refuge contract board are required.");
  if(d.Contracts==null||d.EncounterPlans==null||d.Contracts.Any(q=>q==null)||d.EncounterPlans.Any(p=>p==null)){error("story","story","Null story collections/entries.");return;}
  var flags=c.Events.SelectMany(e=>e.Effects).Where(e=>e.Handler=="set-flag").Select(e=>e.Flag).Concat(c.Dialogues.Select(x=>x.CompletionFlag)).Concat(c.Dialogues.SelectMany(x=>x.Nodes).SelectMany(n=>n.Choices).SelectMany(ch=>ch.Consequence?.SetFlags??[])).ToHashSet();
  var global=c.Areas.Select(a=>a.Id).Concat(c.Items.Select(i=>i.Id)).Concat(c.Actors.Select(a=>a.Id)).Concat(c.Events.Select(e=>e.Id)).Concat(c.Encounters.Select(e=>e.Id)).Concat(c.Areas.SelectMany(a=>a.Features.Select(f=>f.Id))).ToHashSet();
  global.Add(c.Id+".objectives");foreach(var id in c.Dialogues.Select(d=>d.Id).Concat(c.Objectives.Select(o=>o.Id)).Concat(c.Connections.Select(r=>r.Id)))global.Add(id);
  var ids=d.Contracts.Select(q=>q.Id).Concat(d.EncounterPlans.Select(p=>p.Id)).ToArray();if(ids.Length>48||d.Contracts.Count>32||d.EncounterPlans.Count>16||ids.Any(i=>!Id(i)||global.Contains(i))||ids.Distinct().Count()!=ids.Length)error("story","ids","Story IDs must be distinct and bounded.");
  var reserved=new HashSet<string>();var usedEncounters=new HashSet<string>();
  foreach(var p in d.EncounterPlans)
  {
   var area=c.Areas.FirstOrDefault(a=>a.Id==p.AreaId);
   if(!Source(p.Source)||p.Name==""||area==null||!area.Features.Any(f=>f.Id==p.TriggerFeatureId&&f.Kind=="encounter")||d.EncounterPlans.Count(x=>x.TriggerFeatureId==p.TriggerFeatureId)!=1||p.RequiredContractId!=""&&!d.Contracts.Any(q=>q.Id==p.RequiredContractId)||p.Waves==null||p.Waves.Count is <1 or >8){error("plan",p.Id,"Invalid gated encounter plan.");continue;}
   foreach(var wave in p.Waves)
   {
    var e=c.Encounters.FirstOrDefault(e=>e.Id==wave);if(e==null||e.AreaId!=p.AreaId||e.RewardXp!=0||e.RewardCurrency!=0||e.ActorIds.Count>12||!usedEncounters.Add(wave)){error("plan",p.Id,"Each finite wave must reference its own unrewarded encounter in this area.");continue;}
    foreach(var actor in e.ActorIds)if(!reserved.Add(actor)||c.Hordes.Any(h=>h.MemberActorIds.Contains(actor)))error("plan",p.Id,"A reserved actor cannot belong to another wave or horde.");
   }
  }
  foreach(var f in c.Areas.SelectMany(a=>a.Features))if(f.Kind=="quest-board"&&f.Id!=d.BoardFeatureId||f.Kind=="encounter"&&!d.EncounterPlans.Any(p=>p.TriggerFeatureId==f.Id))error("story",f.Id,"Unbound story feature.");
  foreach(var q in d.Contracts)
  {
   if(!Source(q.Source)||string.IsNullOrWhiteSpace(q.Title)||q.Description==null||q.Description.Length>3000||q.BranchGroup!=""&&!Id(q.BranchGroup)||q.DeadlineMinutes is <0 or >10080||q.RewardXp is <0 or >10000||q.Goals==null||q.Prerequisites==null||q.RequiredFlags==null||q.Rewards==null){error("contract",q.Id,"Invalid contract fields.");continue;}
   if(q.Goals.Count is <1 or >12||q.Goals.Any(g=>g==null)||q.Rewards.Count>8||q.Prerequisites.Any(id=>id==q.Id||!d.Contracts.Any(x=>x.Id==id))||q.Prerequisites.Distinct().Count()!=q.Prerequisites.Count||q.RequiredFlags.Any(f=>!flags.Contains(f))||q.FailureFlag!=""&&!flags.Contains(q.FailureFlag)||q.FailureActorId!=""&&!c.Actors.Any(a=>a.Id==q.FailureActorId&&!a.Enemy))error("contract",q.Id,"Invalid goals, prerequisites or failure conditions.");
   var goals=q.Goals.Where(g=>g!=null).ToArray();if(goals.Select(g=>g.Id).Distinct().Count()!=goals.Length)error("contract",q.Id,"Goal IDs must be distinct.");
   foreach(var g in goals)
   {
    bool valid=g.Kind switch
    {"feature-used"=>c.Areas.Any(a=>a.Features.Any(f=>f.Id==g.TargetId&&f.Kind is "event" or "door")),"encounter-cleared"=>c.Encounters.Any(e=>e.Id==g.TargetId),"plan-cleared"=>d.EncounterPlans.Any(p=>p.Id==g.TargetId),"area-visited"=>c.Areas.Any(a=>a.Id==g.TargetId),"flag"=>flags.Contains(g.TargetId),"rescued"=>c.Actors.Any(a=>a.Id==g.TargetId&&!a.Enemy),"item-held" or "deliver-items"=>c.Items.Any(i=>i.Id==g.TargetId&&i.Equipment?.Status is not ("scripted" or "reference")&&i.Kind!=ItemKind.Quest),"secured-sv"=>g.TargetId=="",_=>false};
    if(!Id(g.Id)||string.IsNullOrWhiteSpace(g.Text)||g.Quantity is <1 or >100000||g.Kind is not ("item-held" or "deliver-items" or "secured-sv")&&g.Quantity!=1||!valid)error("goal",q.Id,"Invalid typed objective or reference.");
   }
   foreach(var reward in q.Rewards)if(reward==null||reward.Wear!=0||reward.Quantity<1||!c.Items.Any(i=>i.Id==reward.ItemId&&reward.Quantity<=i.MaxStack&&i.Equipment?.Status is not ("scripted" or "reference")&&i.Kind!=ItemKind.Quest))error("reward",q.Id,"Reward must be a bounded ordinary physical stack.");
  }
  var visiting=new HashSet<string>();var done=new HashSet<string>();bool Cycle(string id){if(visiting.Contains(id))return true;if(done.Contains(id))return false;var q=d.Contracts.FirstOrDefault(q=>q.Id==id);if(q?.Prerequisites==null)return false;visiting.Add(id);bool cycle=q.Prerequisites.Any(Cycle);visiting.Remove(id);done.Add(id);return cycle;}if(d.Contracts.Any(q=>Cycle(q.Id)))error("contract","prerequisites","Contract prerequisites must be acyclic.");
 }
}
