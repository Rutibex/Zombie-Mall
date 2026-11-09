namespace Ashen.Core;
public sealed class CampaignSocial
{
 public List<CampaignFaction> Factions {get;set;}=[];
 public List<CampaignRelationship> Survivors {get;set;}=[];
 public List<CampaignSocialDecision> Decisions {get;set;}=[];
 public List<CampaignSocialOutcome> ContractOutcomes {get;set;}=[];
 public string TraderFactionId {get;set;}="";
}
public sealed class CampaignFaction
{
 public string Id {get;set;}="";
 public string Name {get;set;}="";
 public string Description {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public bool LocalContact {get;set;}
}
public sealed class CampaignRelationship
{
 public string ActorId {get;set;}="";
 public string Values {get;set;}="";
}
public sealed class SocialShift
{
 public string Kind {get;set;}="trust";
 public string TargetId {get;set;}="";
 public string OtherActorId {get;set;}="";
 public int Delta {get;set;}
}
public sealed class CampaignSocialChoice
{
 public string Id {get;set;}="";
 public string Text {get;set;}="";
 public string Response {get;set;}="";
 public List<SocialShift> Changes {get;set;}=[];
}
public sealed class CampaignSocialDecision
{
 public string Id {get;set;}="";
 public string Title {get;set;}="";
 public string Text {get;set;}="";
 public string ActorId {get;set;}="";
 public string FactionId {get;set;}="";
 public string RequiredDecisionId {get;set;}="";
 public string RequiredContractId {get;set;}="";
 public int MinimumTrust {get;set;}=-100;
 public List<CampaignSocialChoice> Choices {get;set;}=[];
}
public sealed class CampaignSocialOutcome
{
 public string ContractId {get;set;}="";
 public List<SocialShift> Changes {get;set;}=[];
}
public sealed class SocialRecord
{
 public string Id {get;set;}="";
 public string ChoiceId {get;set;}="";
 public int Minute {get;set;}
}
public static class SocialRules
{
 public static void Validate(CampaignDefinition c,Action<string,string,string> error)
 {
  var d=c.Social;if(d==null)return;
  bool Id(string s)=>!string.IsNullOrWhiteSpace(s)&&System.Text.RegularExpressions.Regex.IsMatch(s,"^[a-z0-9][a-z0-9._-]*$");
  bool Source(SourceReference? s)=>s!=null&&!string.IsNullOrWhiteSpace(s.AuditId)&&s.PageStart>=1&&s.PageEnd>=s.PageStart&&s.PageEnd<=513;
  if(c.SchemaVersion<19||c.Story==null||c.Workshop==null)error("social","social","Social rules require schema 19, refuge contracts and workshop.");
  if(d.Factions==null||d.Survivors==null||d.Decisions==null||d.ContractOutcomes==null||d.Factions.Any(x=>x==null)||d.Survivors.Any(x=>x==null)||d.Decisions.Any(x=>x==null)||d.ContractOutcomes.Any(x=>x==null)){error("social","collections","Null social collections or entries.");return;}
  if(d.Factions.Count is <1 or >32||d.Survivors.Count is <1 or >12||d.Decisions.Count>32||d.ContractOutcomes.Count>32)error("social","counts","Social definitions exceed bounded counts.");
  if(d.Factions.Select(f=>f.Id).Distinct().Count()!=d.Factions.Count||d.Survivors.Select(s=>s.ActorId).Distinct().Count()!=d.Survivors.Count||d.Decisions.Select(q=>q.Id).Distinct().Count()!=d.Decisions.Count||d.ContractOutcomes.Select(q=>q.ContractId).Distinct().Count()!=d.ContractOutcomes.Count)error("social","ids","Duplicate social IDs.");
  var common=c.Actors.Select(a=>a.Id).Concat(c.Items.Select(i=>i.Id)).Concat(c.Events.Select(e=>e.Id)).Concat(c.Areas.Select(a=>a.Id)).Concat(c.Areas.SelectMany(a=>a.Features).Select(f=>f.Id)).Concat(c.Story?.Contracts.Select(q=>q.Id)??[]).Concat(c.Story?.EncounterPlans.Select(p=>p.Id)??[]).ToHashSet();
  var socialIds=d.Factions.Select(f=>f.Id).Concat(d.Decisions.Select(q=>q.Id)).ToArray();if(socialIds.Distinct().Count()!=socialIds.Length||socialIds.Any(common.Contains))error("social","ids","Social IDs must not collide with authored world IDs.");
  foreach(var f in d.Factions)if(!Id(f.Id)||string.IsNullOrWhiteSpace(f.Name)||string.IsNullOrWhiteSpace(f.Description)||f.Description.Length>2000||!Source(f.Source))error("faction",f.Id,"Invalid sourced faction.");
  if(!d.Factions.Any(f=>f.Id==d.TraderFactionId&&f.LocalContact))error("social","trader","Trader faction must be a local contact.");
  foreach(var s in d.Survivors)if(!c.Actors.Any(a=>a.Id==s.ActorId&&!a.Enemy)||string.IsNullOrWhiteSpace(s.Values)||s.Values.Length>1500)error("social",s.ActorId,"Invalid survivor profile.");
  void Changes(List<SocialShift>? shifts,string owner)
  {
   if(shifts==null||shifts.Count>12||shifts.Any(s=>s==null)){error("social",owner,"Invalid consequence list.");return;}
   foreach(var s in shifts)if(s.Delta is <-10 or >10||s.Delta==0||s.Kind switch{"faction"=>!d.Factions.Any(f=>f.Id==s.TargetId&&f.LocalContact)||s.OtherActorId!="","trust"=>!d.Survivors.Any(p=>p.ActorId==s.TargetId)||s.OtherActorId!="","bond"=>s.TargetId==s.OtherActorId||!d.Survivors.Any(p=>p.ActorId==s.TargetId)||!d.Survivors.Any(p=>p.ActorId==s.OtherActorId),_=>true})error("social",owner,"Invalid bounded faction, trust or bond shift.");
  }
  foreach(var q in d.Decisions)
  {
   if(!Id(q.Id)||string.IsNullOrWhiteSpace(q.Title)||string.IsNullOrWhiteSpace(q.Text)||q.Text.Length>2000||q.ActorId!=""&&!d.Survivors.Any(s=>s.ActorId==q.ActorId)||q.FactionId!=""&&!d.Factions.Any(f=>f.Id==q.FactionId&&f.LocalContact)||q.RequiredDecisionId!=""&&!d.Decisions.Any(p=>p.Id==q.RequiredDecisionId)||q.RequiredContractId!=""&&!(c.Story?.Contracts.Any(p=>p.Id==q.RequiredContractId)??false)||q.MinimumTrust is <-100 or >100||q.ActorId==""&&q.MinimumTrust!=-100||q.Choices==null||q.Choices.Count is <2 or >3){error("social",q.Id,"Invalid conversation gates or choices.");continue;}
   if(q.Choices.Any(x=>x==null)){error("social",q.Id,"Null choice.");continue;}
   if(q.Choices.Select(x=>x.Id).Distinct().Count()!=q.Choices.Count)error("social",q.Id,"Duplicate choice IDs.");
   foreach(var choice in q.Choices){if(!Id(choice.Id)||string.IsNullOrWhiteSpace(choice.Text)||choice.Text.Length>400||string.IsNullOrWhiteSpace(choice.Response)||choice.Response.Length>1500)error("social",q.Id,"Invalid dialogue text.");Changes(choice.Changes,q.Id);}
  }
  foreach(var o in d.ContractOutcomes){if(!(c.Story?.Contracts.Any(q=>q.Id==o.ContractId)??false))error("social",o.ContractId,"Unknown contract outcome.");Changes(o.Changes,o.ContractId);}
  var visiting=new HashSet<string>();var done=new HashSet<string>();bool Cycle(string id){if(visiting.Contains(id))return true;if(done.Contains(id))return false;var q=d.Decisions.FirstOrDefault(q=>q.Id==id);if(q==null||q.RequiredDecisionId=="")return false;visiting.Add(id);bool r=Cycle(q.RequiredDecisionId);visiting.Remove(id);done.Add(id);return r;}if(d.Decisions.Any(q=>Cycle(q.Id)))error("social","prerequisites","Conversation prerequisites must be acyclic.");
 }
}
