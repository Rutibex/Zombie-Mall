using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed partial class World
{
 public List<SocialRecord> SocialHistory {get;set;}=[];
 [JsonIgnore] public bool SocialEnabled=>Campaign?.Social!=null;
 IEnumerable<SocialShift> SocialChanges(SocialRecord r)=>r.ChoiceId==""?Campaign!.Social!.ContractOutcomes.Single(o=>o.ContractId==r.Id).Changes:Campaign!.Social!.Decisions.Single(q=>q.Id==r.Id).Choices.Single(c=>c.Id==r.ChoiceId).Changes;
 int SocialScore(string kind,string id,string other="")
 {
  if(!SocialEnabled)return 0;int n=0;foreach(var record in SocialHistory)foreach(var s in SocialChanges(record))if(s.Kind==kind&&(s.TargetId==id&&s.OtherActorId==other||kind=="bond"&&s.TargetId==other&&s.OtherActorId==id))n=Math.Clamp(n+s.Delta,-100,100);return n;
 }
 public int FactionStanding(string id)=>SocialScore("faction",id);
 public int SurvivorTrust(string id)=>SocialScore("trust",id);
 public int SurvivorBond(string a,string b)=>SocialScore("bond",a,b);
 public static string SocialLabel(int n)=>n switch{>=10=>"Trusted",>=3=>"Friendly",<=-10=>"Hostile",<=-3=>"Wary",_=>"Neutral"};
 public bool FactionContacted(string id)=>SocialEnabled&&Campaign!.Social!.Factions.Any(f=>f.Id==id&&f.LocalContact)&&SocialHistory.Any(r=>r.ChoiceId!=""&&Campaign.Social.Decisions.Any(q=>q.Id==r.Id&&q.FactionId==id));
 [JsonIgnore] public int SocialTradeMarkup=>!SocialEnabled||!FactionContacted(Campaign!.Social!.TraderFactionId)?0:FactionStanding(Campaign.Social.TraderFactionId)>=5?-5:FactionStanding(Campaign.Social.TraderFactionId)<=-5?5:0;
 public string SocialDecisionGate(string id)
 {
  if(!SocialEnabled)return "This campaign has no faction and relationship rules.";var q=Campaign!.Social!.Decisions.FirstOrDefault(q=>q.Id==id);if(q==null)return "Unknown conversation.";
  if(SocialHistory.Any(r=>r.Id==id))return "This conversation is already resolved.";
  if(q.RequiredDecisionId!=""&&!SocialHistory.Any(r=>r.Id==q.RequiredDecisionId))return "Resolve the earlier conversation first.";
  if(q.RequiredContractId!=""&&!ContractStates.Any(s=>s.Id==q.RequiredContractId&&s.Status=="completed"))return "Complete and report the prerequisite contract first.";
  var required=q.Choices.SelectMany(c=>c.Changes).Where(s=>s.Kind is "trust" or "bond").SelectMany(s=>s.OtherActorId==""?new[]{s.TargetId}:new[]{s.TargetId,s.OtherActorId}).Append(q.ActorId).Where(s=>s!="").Distinct();
  if(required.Any(id=>!CommunityMembers.Any(a=>a.Id==id&&a.Alive)))return "Recruit the affected survivors and keep them alive in the community.";
  if(q.ActorId!=""&&SurvivorTrust(q.ActorId)<q.MinimumTrust)return $"Requires {q.MinimumTrust} trust with {Campaign.Actors.Single(a=>a.Id==q.ActorId).Name}.";return "";
 }
 public string SocialDecisionReason(Actor a,string id)
 {
  string r=SocialDecisionGate(id);if(r!="")return r;r=ContractBoardReason(a);if(r!="")return r;var q=Campaign!.Social!.Decisions.Single(q=>q.Id==id);
  // All affected survivors must be physically at home; distant allies cannot answer a refuge conversation.
  var required=q.Choices.SelectMany(c=>c.Changes).Where(s=>s.Kind is "trust" or "bond").SelectMany(s=>s.OtherActorId==""?new[]{s.TargetId}:new[]{s.TargetId,s.OtherActorId}).Append(q.ActorId).Where(s=>s!="").Distinct();
  if(required.Any(id=>!CommunityMembers.Any(member=>member.Id==id&&member.Alive&&(Party.Contains(member)||ShelterResidents.Any(res=>res.Actor.Id==id)))))return "Bring the affected survivors home.";
  if((long)CampaignMinutes+5>int.MaxValue)return "The campaign clock limit has been reached.";return "";
 }
 public bool ResolveSocialDecision(Actor a,string id,string choiceId)
 {
  string reason=SocialDecisionReason(a,id);if(reason!=""){LogSystem(reason,true);return false;}var q=Campaign!.Social!.Decisions.Single(q=>q.Id==id);var choice=q.Choices.FirstOrDefault(c=>c.Id==choiceId);if(choice==null)return false;
  SocialHistory.Add(new(){Id=id,ChoiceId=choiceId,Minute=CampaignMinutes});AddStoryJournalEntry("social."+id,q.Title,choice.Text+"\n"+choice.Response+"\n"+DescribeSocialChanges(choice.Changes),"Factions and Relationships");Message(MessageCategory.Narrative,choice.Response);CampaignMinutes+=5;ProcessSurvivalTime();SyncCampaignObjectives();Reveal();return true;
 }
 public string DescribeSocialChanges(IEnumerable<SocialShift> changes)=>string.Join("; ",changes.Select(s=>$"{(s.Kind=="faction"?Campaign!.Social!.Factions.Single(f=>f.Id==s.TargetId).Name:Campaign!.Actors.Single(a=>a.Id==s.TargetId).Name)} {(s.Kind=="bond"?"/ "+Campaign!.Actors.Single(a=>a.Id==s.OtherActorId).Name+" bond":s.Kind)} {s.Delta:+0;-0}"));
 void SyncSocial()
 {
  if(!SocialEnabled)return;foreach(var o in Campaign!.Social!.ContractOutcomes)if(ContractStates.Any(s=>s.Id==o.ContractId&&s.Status=="completed")&&!SocialHistory.Any(r=>r.Id==o.ContractId))
  {SocialHistory.Add(new(){Id=o.ContractId,Minute=CampaignMinutes});AddStoryJournalEntry("social."+o.ContractId,"Word reaches the refuge",DescribeSocialChanges(o.Changes),"Factions and Relationships");}
 }
 void ValidateSocialState()
 {
  if(SocialHistory==null||SocialHistory.Count>64||SocialHistory.Any(r=>r==null)||SocialHistory.Select(r=>r.Id).Distinct().Count()!=SocialHistory.Count)throw new InvalidDataException("Invalid social history.");
  if(!SocialEnabled){if(SocialHistory.Count>0)throw new InvalidDataException("Social history requires opt-in.");return;}
  int minute=Campaign!.StartMinute;var earlier=new HashSet<string>();var prior=new List<SocialRecord>();
  foreach(var r in SocialHistory)
  {
   if(r.Minute<minute||r.Minute>CampaignMinutes)throw new InvalidDataException("Invalid social chronology.");minute=r.Minute;
   if(r.ChoiceId==""){if(!Campaign.Social!.ContractOutcomes.Any(o=>o.ContractId==r.Id)||!ContractStates.Any(s=>s.Id==r.Id&&s.Status=="completed"&&s.ResolvedMinute<=r.Minute))throw new InvalidDataException("Unearned social contract outcome.");}
   else
   {
    var q=Campaign.Social!.Decisions.FirstOrDefault(q=>q.Id==r.Id);if(q==null||!q.Choices.Any(c=>c.Id==r.ChoiceId)||q.RequiredDecisionId!=""&&!earlier.Contains(q.RequiredDecisionId)||q.RequiredContractId!=""&&!ContractStates.Any(s=>s.Id==q.RequiredContractId&&s.Status=="completed"&&s.ResolvedMinute<=r.Minute))throw new InvalidDataException("Invalid social choice or prerequisite.");
    int trust=0;foreach(var p in prior)foreach(var s in SocialChanges(p))if(s.Kind=="trust"&&s.TargetId==q.ActorId)trust=Math.Clamp(trust+s.Delta,-100,100);if(q.ActorId!=""&&trust<q.MinimumTrust)throw new InvalidDataException("Conversation trust gate was not met.");
   }
   if(!JournalEntries.Any(j=>j.Id=="social."+r.Id))throw new InvalidDataException("Social history is missing its journal entry.");earlier.Add(r.Id);prior.Add(r);
  }
 }
}
