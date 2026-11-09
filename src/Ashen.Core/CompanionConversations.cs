namespace Ashen.Core;

/// <summary>
/// IIP40 companion narrative rules. The Core owns approval, gating, one-shot interjections,
/// persistent narrative flags and the queue for companion-initiated conversations. UI code
/// asks these APIs what is available and never infers relationship state from rendered text.
/// </summary>
public static class CompanionConversationRules
{
 public static void ValidateConditions(DialogueConditionDefinition[]? conditions)
 {
  foreach(var condition in conditions??[])
  {
   if(condition.MinimumApproval<-100||condition.MinimumApproval>100||condition.MaximumApproval<-100||condition.MaximumApproval>100||condition.MinimumApproval>condition.MaximumApproval)
    throw new InvalidDataException("Dialogue companion approval condition must remain within -100..100.");
   if(string.IsNullOrWhiteSpace(condition.CompanionId)&&(condition.MinimumApproval!=-100||condition.MaximumApproval!=100))
    throw new InvalidDataException("Dialogue approval/alive conditions require a CompanionId.");
  }
 }
 public static void ValidateConsequence(DialogueConsequenceDefinition? consequence)
 {
  if(consequence==null)return;
  foreach(var change in consequence.ApprovalChanges??[])
  {
   if(string.IsNullOrWhiteSpace(change.ActorId)||change.Delta is <-100 or >100)throw new InvalidDataException("Dialogue approval changes require an actor and a bounded delta.");
  }
 }
}

public sealed partial class World
{
 // IIP40 durable relationship/narrative state. Scores are the companion's approval of the
 // player's leadership/party decisions, independent from portrait order or combat selection.
 public Dictionary<string,int> CompanionApproval {get;set;}=[];
 public List<string> DialogueFlags {get;set;}=[];
 public List<string> SeenCompanionInterjections {get;set;}=[];
 public List<string> SeenCompanionConversations {get;set;}=[];
 public List<string> PendingCompanionConversations {get;set;}=[];

 public void InitializeCompanionConversationState()
 {
  CompanionApproval=[];foreach(var actor in Party)CompanionApproval[actor.Id]=0;
  DialogueFlags=[];SeenCompanionInterjections=[];SeenCompanionConversations=[];PendingCompanionConversations=[];
 }

 public void NormalizeCompanionConversationState()
 {
  CompanionApproval??=[];DialogueFlags??=[];SeenCompanionInterjections??=[];SeenCompanionConversations??=[];PendingCompanionConversations??=[];
  var normalized=new Dictionary<string,int>(StringComparer.Ordinal);
  foreach(var actor in Party)normalized[actor.Id]=Math.Clamp(CompanionApproval.TryGetValue(actor.Id,out var score)?score:0,-100,100);
  CompanionApproval=normalized;
  static List<string> Clean(IEnumerable<string> source)=>source.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList();
  DialogueFlags=Clean(DialogueFlags);SeenCompanionInterjections=Clean(SeenCompanionInterjections);SeenCompanionConversations=Clean(SeenCompanionConversations);
  PendingCompanionConversations=Clean(PendingCompanionConversations).Where(id=>DialogueCatalog.TryGet(id,out var d)&&d.CompanionConversation&&!SeenCompanionConversations.Contains(id,StringComparer.Ordinal)).ToList();
 }

 public void ValidateCompanionConversationState()
 {
  if(CompanionApproval.Keys.Any(id=>Party.All(a=>a.Id!=id))||Party.Any(a=>!CompanionApproval.ContainsKey(a.Id)))throw new InvalidDataException("Companion approval state does not match the active party.");
  if(CompanionApproval.Values.Any(v=>v is <-100 or >100))throw new InvalidDataException("Companion approval is outside -100..100.");
  if(PendingCompanionConversations.Any(id=>!DialogueCatalog.TryGet(id,out var d)||!d.CompanionConversation))throw new InvalidDataException("Unknown pending companion conversation.");
  if(PendingCompanionConversations.Distinct(StringComparer.Ordinal).Count()!=PendingCompanionConversations.Count)throw new InvalidDataException("Duplicate pending companion conversation.");
 }

 public int Approval(string actorId)=>CompanionApproval.TryGetValue(actorId,out var value)?value:0;
 public string ApprovalLabel(string actorId)=>Approval(actorId) switch
 {
  >=50=>"Devoted",
  >=25=>"Loyal",
  >=10=>"Warm",
  <=-50=>"Hostile",
  <=-25=>"Resentful",
  <=-10=>"Wary",
  _=>"Neutral"
 };

 public int ChangeCompanionApproval(string actorId,int delta,string reason="")
 {
  var actor=Party.FirstOrDefault(a=>a.Id==actorId);if(actor==null)return 0;
  int before=Approval(actorId),after=Math.Clamp(before+delta,-100,100);CompanionApproval[actorId]=after;
  if(after!=before)
  {
   int actual=after-before;string sign=actual>=0?"+":"";
   Message(MessageCategory.Narrative,$"{actor.Name} approval {sign}{actual} — {ApprovalLabel(actorId)}.",string.IsNullOrWhiteSpace(reason)?$"{actor.Name}'s approval changed from {before} to {after}.":reason);
  }
  return after;
 }

 public bool HasDialogueFlag(string flag)=>!string.IsNullOrWhiteSpace(flag)&&DialogueFlags.Contains(flag,StringComparer.Ordinal);
 public void SetDialogueFlag(string flag,bool value=true)
 {
  if(string.IsNullOrWhiteSpace(flag))return;if(value){if(!DialogueFlags.Contains(flag,StringComparer.Ordinal))DialogueFlags.Add(flag);}else DialogueFlags.RemoveAll(x=>x==flag);
  if(Campaign!=null)DiscoverCampaignNeighbours();
 }

 public bool DialogueConditionMet(DialogueConditionDefinition condition)
 {
  if(!string.IsNullOrWhiteSpace(condition.RequiredFlag)&&!HasDialogueFlag(condition.RequiredFlag))return false;
  if(!string.IsNullOrWhiteSpace(condition.ForbiddenFlag)&&HasDialogueFlag(condition.ForbiddenFlag))return false;
  if(string.IsNullOrWhiteSpace(condition.CompanionId))return true;
  var actor=Party.FirstOrDefault(a=>a.Id==condition.CompanionId);if(actor==null||condition.RequireAlive&&!actor.Alive)return false;
  int approval=Approval(condition.CompanionId);return approval>=condition.MinimumApproval&&approval<=condition.MaximumApproval;
 }
 public bool DialogueConditionsMet(DialogueConditionDefinition[]? conditions)=>conditions==null||conditions.All(DialogueConditionMet);

 public Actor? ResolveDialogueChoiceSpeaker(DialogueChoiceDefinition choice,Actor? fallback)
 {
  if(string.IsNullOrWhiteSpace(choice.SpeakerActorId))return fallback;
  return Party.FirstOrDefault(a=>a.Id==choice.SpeakerActorId&&a.Alive);
 }
 public bool DialogueChoiceAvailable(DialogueChoiceDefinition choice,Actor? fallback)
  =>DialogueConditionsMet(choice.Conditions)&&ResolveDialogueChoiceSpeaker(choice,fallback)!=null;

 public void ApplyDialogueConsequence(DialogueConsequenceDefinition? consequence)
 {
  if(consequence==null)return;CompanionConversationRules.ValidateConsequence(consequence);
  foreach(var change in consequence.ApprovalChanges??[])ChangeCompanionApproval(change.ActorId,change.Delta,change.Reason);
  foreach(var flag in consequence.SetFlags??[])SetDialogueFlag(flag,true);
  foreach(var flag in consequence.ClearFlags??[])SetDialogueFlag(flag,false);
  foreach(var conversation in consequence.QueueCompanionConversations??[])QueueCompanionConversation(conversation);
  SyncCampaignObjectives();
 }

 public IReadOnlyList<CompanionInterjectionDefinition> ResolveCompanionInterjections(DialogueDefinition dialogue,DialogueNodeDefinition node)
 {
  var result=new List<CompanionInterjectionDefinition>();
  foreach(var interjection in node.Interjections??[])
  {
   string key=$"{dialogue.Id}:{node.Id}:{interjection.Id}";
   if(interjection.Once&&SeenCompanionInterjections.Contains(key,StringComparer.Ordinal))continue;
   if(Party.All(a=>a.Id!=interjection.ActorId||!a.Alive)||!DialogueConditionsMet(interjection.Conditions))continue;
   if(interjection.Once)SeenCompanionInterjections.Add(key);ApplyDialogueConsequence(interjection.Consequence);result.Add(interjection);
  }
  return result;
 }

 public bool QueueCompanionConversation(string dialogueId)
 {
  if(!DialogueCatalog.TryGet(dialogueId,out var definition)||!definition.CompanionConversation)return false;
  if(SeenCompanionConversations.Contains(dialogueId,StringComparer.Ordinal)||PendingCompanionConversations.Contains(dialogueId,StringComparer.Ordinal))return false;
  PendingCompanionConversations.Add(dialogueId);return true;
 }

 public bool TryTakeQueuedCompanionConversation(out DialogueDefinition definition)
 {
  NormalizeCompanionConversationState();
  while(PendingCompanionConversations.Count>0)
  {
   string id=PendingCompanionConversations[0];PendingCompanionConversations.RemoveAt(0);
   if(!DialogueCatalog.TryGet(id,out var found)||!found.CompanionConversation)continue;
   var start=found.Node(found.StartNodeId);if(!string.IsNullOrWhiteSpace(start.SpeakerActorId)&&Party.All(a=>a.Id!=start.SpeakerActorId||!a.Alive))continue;
   var participants=found.Nodes.Select(n=>n.SpeakerActorId).Concat(found.Nodes.SelectMany(n=>n.Choices.Select(c=>c.SpeakerActorId))).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
   if(participants.Length>0&&participants.All(id=>Party.All(a=>a.Id!=id||!a.Alive)))continue;
   if(!SeenCompanionConversations.Contains(id,StringComparer.Ordinal))SeenCompanionConversations.Add(id);definition=found;return true;
  }
  definition=null!;return false;
 }

 public void CompleteCompanionConversation(DialogueDefinition definition)
 {
  if(!definition.CompanionConversation)return;
  if(!string.IsNullOrWhiteSpace(definition.CompletionFlag))SetDialogueFlag(definition.CompletionFlag,true);
 }

 public void QueueTravelCompanionConversations(string destinationId)
 {
  if(destinationId=="briarwood-road")QueueCompanionConversation("banter-briarwood-signs");
  if(destinationId=="saint-orlan"&&HasRelic)QueueCompanionConversation("banter-heart-home");
 }
}
