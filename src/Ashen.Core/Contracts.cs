using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed partial class World
{
 public List<ContractState> ContractStates {get;set;}=[];
 public List<EncounterPlanState> EncounterPlanStates {get;set;}=[];
 public int ContractXpAwarded {get;set;}
 public int ContractXpRemainder {get;set;}
 [JsonIgnore] public bool StoryEnabled=>Campaign?.Story!=null;
 [JsonIgnore] public Action? ContractsOpened {get;set;}
 public string ContractStatus(string id)
 {
  if(!StoryEnabled)return "Unavailable";var q=Campaign!.Story!.Contracts.FirstOrDefault(q=>q.Id==id);if(q==null)return "Unknown";var s=ContractStates.FirstOrDefault(s=>s.Id==id);
  if(s!=null)return s.Status=="active"?(q.Goals.All(g=>ContractGoalProgress(q,s,g)>=g.Quantity)?"Ready":"Active"):s.Status=="completed"?"Completed":"Failed";
  if(q.BranchGroup!=""&&ContractStates.Any(s=>s.Status is "active" or "completed"&&Campaign.Story.Contracts.Any(other=>other.Id==s.Id&&other.BranchGroup==q.BranchGroup)))return "Excluded";
  return q.Prerequisites.All(id=>ContractStates.Any(s=>s.Id==id&&s.Status=="completed"))&&q.RequiredFlags.All(HasDialogueFlag)&&(!q.RequiresExpedition||CompletedExpedition!=null)?"Available":"Locked";
 }
 public int ContractGoalProgress(CampaignContract q,ContractState? s,CampaignContractGoal g)
 {
  if(s?.Status=="completed")return g.Quantity;
  if(s?.Status=="failed"){int i=q.Goals.IndexOf(g);return i>=0&&i<s.Progress.Count?s.Progress[i]:0;}
  int n=g.Kind switch
  {"feature-used"=>Features.Concat(StoredTravelAreas.SelectMany(a=>a.Features)).Any(f=>f.Id==g.TargetId&&f.Used)?1:0,
   "encounter-cleared"=>ClearedCampaignEncounters.Contains(g.TargetId)?1:0,
   "plan-cleared"=>EncounterPlanStates.Any(p=>p.Id==g.TargetId&&p.Status=="cleared")?1:0,
   "area-visited"=>TravelDestinations.Any(a=>a.Id==g.TargetId&&a.Visits>0)?1:0,
   "flag"=>HasDialogueFlag(g.TargetId)?1:0,
   "rescued"=>CommunityMembers.Any(a=>a.Id==g.TargetId&&a.Alive)?1:0,
   "item-held"=>Party.Sum(a=>a.Inventory.Count(g.TargetId)+a.Equipment.All().Where(i=>i.ItemId==g.TargetId).Sum(i=>i.Quantity)),
   "deliver-items"=>Party.Sum(a=>a.Inventory.Items.Where(i=>i.ItemId==g.TargetId&&ContractDeliverable(i)).Sum(i=>i.Quantity))+(ShelterHomeStockpile()?.Contents.Items.Where(i=>i.ItemId==g.TargetId&&ContractDeliverable(i)).Sum(i=>i.Quantity)??0),
   "secured-sv"=>Math.Max(0,SecuredSurvivalValue-(s?.BaselineSv??SecuredSurvivalValue)),_=>0};return Math.Clamp(n,0,g.Quantity);
 }
 public string ContractBoardReason(Actor actor)
 {
  if(!StoryEnabled)return "This saved campaign has no reusable contracts.";
  if(!Party.Contains(actor)||!actor.Alive||Mode!=Mode.Explore||ReactionInterruptActive||PendingReaction!=null||Party.Any(IsMoving))return "Choose a conscious survivor and finish combat, movement and reactions.";
  string safety=ShelterManagementReason();if(safety!="")return safety;var board=Features.FirstOrDefault(f=>f.Id==Campaign!.Story!.BoardFeatureId);
  if(board==null||!IsFeatureDiscovered(board)||actor.Position.Distance(board.Pos.Center)>2.25f||!Line(actor.Position,board.Pos.Center))return "Stand beside the refuge contract board.";
  if((long)CampaignMinutes+10080+1440>int.MaxValue)return "The campaign clock limit has been reached.";return "";
 }
 public string AcceptContractReason(Actor a,string id)
 {
  string r=ContractBoardReason(a);if(r!="")return r;string status=ContractStatus(id);return status=="Available"?"":status switch{"Locked"=>"Complete the prerequisite contracts, expedition or required discoveries first.","Excluded"=>"Another contract in this mutually exclusive branch is active or completed.",_=>"This contract has already been accepted, resolved or is unknown."};
 }
 public bool AcceptContract(Actor a,string id)
 {
  string reason=AcceptContractReason(a,id);if(reason!=""){LogSystem(reason,true);return false;}var q=Campaign!.Story!.Contracts.Single(q=>q.Id==id);
  ContractStates.Add(new(){Id=id,AcceptedMinute=CampaignMinutes,BaselineSv=SecuredSurvivalValue,Progress=q.Goals.Select(_=>0).ToList()});
  StartQuest(new(){Id=q.Id,Title=q.Title,Summary=q.Description,Category="Refuge Contracts",Objectives=q.Goals.Select(g=>new QuestObjectiveState{Id=g.Id,Text=g.Text}).ToList()},"Contract accepted. Rewards require reporting at the refuge board.");SyncContracts();return true;
 }
 static bool ContractDeliverable(ItemStack s)=>s.Identified&&s.Wear==0&&s.LoadedAmmunition==null&&(s.Definition.MaxCharges<=0||s.Charges==s.Definition.MaxCharges)&&s.Definition.Equipment?.Status is not ("scripted" or "reference");
 IEnumerable<InventoryState> ContractSources(Actor a)=>new[]{a.Inventory,ShelterHomeStockpile()!.Contents};
 static void PayContract(IEnumerable<InventoryState> sources,IEnumerable<CampaignStack> costs)
 {
  foreach(var c in costs){int left=c.Quantity;foreach(var i in sources){foreach(var s in i.Items.Where(s=>s.ItemId==c.ItemId&&ContractDeliverable(s)).ToArray()){int n=Math.Min(left,s.Quantity);s.ConsumeResourceUnits(n);s.Quantity-=n;left-=n;if(s.Quantity==0)i.Items.Remove(s);if(left==0)break;}if(left==0)break;}if(left!=0)throw new InvalidOperationException("Contract preflight disagreed with payment.");}
 }
 static CampaignStack[] ContractCosts(CampaignContract q)=>q.Goals.Where(g=>g.Kind=="deliver-items").GroupBy(g=>g.TargetId).Select(g=>new CampaignStack{ItemId=g.Key,Quantity=g.Sum(x=>x.Quantity)}).ToArray();
 public string ClaimContractReason(Actor a,string id)
 {
  string r=ContractBoardReason(a);if(r!="")return r;var q=Campaign!.Story!.Contracts.FirstOrDefault(q=>q.Id==id);var s=ContractStates.FirstOrDefault(s=>s.Id==id);
  if(q==null||s?.Status!="active")return "Only an active, unclaimed contract can be reported.";
  if(ContractFailure(q,s)!="")return "This contract has failed its deadline or survival condition.";
  if(!q.Goals.All(g=>ContractGoalProgress(q,s,g)>=g.Quantity))return "Finish the current objectives before reporting.";
  var costs=ContractCosts(q);foreach(var c in costs)if(ContractSources(a).Sum(i=>i.Items.Where(s=>s.ItemId==c.ItemId&&ContractDeliverable(s)).Sum(s=>s.Quantity))<c.Quantity)return $"Bring {c.Quantity} intact, identified, unloaded {ResolveItemDefinition(c.ItemId).Name} with full charges in this backpack or the refuge stockpile.";
  var copies=ContractSources(a).Select(i=>new InventoryState{MaxSlots=i.MaxSlots,Items=i.Items.Select(s=>s.Clone()).ToList()}).ToArray();PayContract(copies,costs);
  foreach(var reward in q.Rewards){var gear=ResolveItemDefinition(reward.ItemId);var probe=new ItemStack{ItemId=reward.ItemId,CampaignGear=gear,Quantity=reward.Quantity,Charges=gear.MaxCharges>0?gear.MaxCharges:-1};if(!copies[1].CanAdd(probe))return "Make refuge stockpile space for all contract rewards.";copies[1].Add(probe);}
  int each=(ContractXpRemainder+q.RewardXp)/Party.Count();if(Party.Any(a=>(long)a.Character.Experience+each>int.MaxValue))return "The contract XP share would exceed a survivor's experience limit.";return "";
 }
 public bool ClaimContract(Actor a,string id)
 {
  string reason=ClaimContractReason(a,id);if(reason!=""){LogSystem(reason,true);return false;}var q=Campaign!.Story!.Contracts.Single(q=>q.Id==id);var s=ContractStates.Single(s=>s.Id==id);PayContract(ContractSources(a),ContractCosts(q));
  s.Status="completed";s.ResolvedMinute=CampaignMinutes;s.Progress=q.Goals.Select(g=>g.Quantity).ToList();int pool=ContractXpRemainder+q.RewardXp,each=pool/Party.Count();foreach(var actor in Party)actor.Character.Experience+=each;ContractXpAwarded+=each*Party.Count();ContractXpRemainder=pool%Party.Count();
  foreach(var reward in q.Rewards)ShelterHomeStockpile()!.Contents.Add(CreateItemStack(reward.ItemId,reward.Quantity));foreach(var g in q.Goals)SetQuestObjective(q.Id,g.Id,true,g.Text);CompleteQuest(q.Id,$"Reported once: {q.RewardXp} contract XP, {each} per active survivor, {ContractXpRemainder} pooled. Physical rewards are stored at the refuge without scavenging credit.");
  AssignMissingItemIds();foreach(var actor in Party)actor.QuickAccess.Normalize(actor);CampaignMinutes+=5;ProcessSurvivalTime();SyncCampaignObjectives();Reveal();return true;
 }
 string ContractFailure(CampaignContract q,ContractState s)
 {
  if(q.DeadlineMinutes>0&&(long)CampaignMinutes>=((long)s.AcceptedMinute+q.DeadlineMinutes))return "The accepted contract's campaign-time deadline expired.";
  if(q.FailureFlag!=""&&HasDialogueFlag(q.FailureFlag))return "The contract's authored failure condition occurred.";
  if(q.FailureActorId!=""&&CommunityMembers.Any(a=>a.Id==q.FailureActorId&&a.Dead))return "The protected survivor died.";return "";
 }
 void SyncContracts()
 {
  if(!StoryEnabled)return;
  foreach(var s in ContractStates.Where(s=>s.Status=="active"))
  {
   var q=Campaign!.Story!.Contracts.Single(q=>q.Id==s.Id);string failure=ContractFailure(q,s);if(failure!=""){s.Status="failed";s.ResolvedMinute=CampaignMinutes;FailQuest(s.Id,failure);continue;}
   for(int i=0;i<q.Goals.Count;i++){var g=q.Goals[i];int progress=ContractGoalProgress(q,s,g);s.Progress[i]=progress;SetQuestObjective(q.Id,g.Id,progress>=g.Quantity,$"{g.Text} ({progress}/{g.Quantity}).");}
  }
 }
 public bool StoryReservedActor(string id)=>StoryEnabled&&Campaign!.Story!.EncounterPlans.SelectMany(p=>p.Waves).Any(e=>Campaign.Encounters.Any(x=>x.Id==e&&x.ActorIds.Contains(id)));
 public string EncounterPlanStatus(string id)=>EncounterPlanStates.FirstOrDefault(p=>p.Id==id)?.Status??"dormant";
 bool CanPlacePlanWave(CampaignEncounterPlan p,int wave)
 {
  var e=Campaign!.Encounters.Single(e=>e.Id==p.Waves[wave]);var area=CurrentCampaignArea!;
  return e.ActorIds.All(id=>!Actors.Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).Any(a=>a.Id==id)&&area.Actors.Any(a=>a.ActorId==id&&CanStand(a.Position.Center,null,true)));
 }
 public string EncounterPlanReason(string id)
 {
  if(!StoryEnabled)return "This campaign has no encounter plans.";var p=Campaign!.Story!.EncounterPlans.FirstOrDefault(p=>p.Id==id);if(p==null||p.AreaId!=CurrentTravelDestinationId)return "Find this encounter in its authored area.";
  if(EncounterPlanStates.Any(s=>s.Id==id))return "This finite encounter has already been activated.";
  if(Mode!=Mode.Explore||PendingReaction!=null||ReactionInterruptActive||Party.Any(IsMoving)||!Party.Any(a=>a.Alive))return "Finish movement, combat and reactions first.";
  var f=Features.Single(f=>f.Id==p.TriggerFeatureId);if(!IsFeatureDiscovered(f)||!Party.Any(a=>a.Alive&&a.Position.Distance(f.Pos.Center)<=1.25f&&Line(a.Position,f.Pos.Center)))return "Stand beside the discovered encounter trigger.";
  if(p.RequiredContractId!=""&&!ContractStates.Any(s=>s.Id==p.RequiredContractId&&s.Status=="active"))return "Accept the required contract first.";
  return CanPlacePlanWave(p,0)?"":"Clear the authored arrival cells before activating the encounter.";
 }
 void SpawnPlanWave(CampaignEncounterPlan p,EncounterPlanState s)
 {
  var e=Campaign!.Encounters.Single(e=>e.Id==p.Waves[s.NextWave]);foreach(var id in e.ActorIds){var actor=AddCampaignActor(Campaign.Actors.Single(a=>a.Id==id),CurrentCampaignArea!.Actors.Single(a=>a.ActorId==id).Position);actor.Awareness.State=AwarenessKind.Investigating;actor.Awareness.LastKnown=PartyLeader.Pos;actor.Awareness.MemorySeconds=18;}
  s.NextWave++;AssignMissingItemIds();Message(MessageCategory.Combat,$"{p.Name}: wave {s.NextWave}/{p.Waves.Count} arrives. This wave is finite.");Reveal();
 }
 public bool StartEncounterPlan(string id)
 {
  string r=EncounterPlanReason(id);if(r!=""){LogSystem(r,true);return false;}var p=Campaign!.Story!.EncounterPlans.Single(p=>p.Id==id);var s=new EncounterPlanState{Id=id};EncounterPlanStates.Add(s);Features.Single(f=>f.Id==p.TriggerFeatureId).Used=true;SpawnPlanWave(p,s);EmitLocalNoise(PartyLeader,"door",20,Features.Single(f=>f.Id==p.TriggerFeatureId).Pos);return true;
 }
 void AdvanceEncounterPlans()
 {
  if(!StoryEnabled||Mode!=Mode.Explore||PendingReaction!=null||ReactionInterruptActive)return;
  foreach(var s in EncounterPlanStates.Where(s=>s.Status=="active"))
  {
   var p=Campaign!.Story!.EncounterPlans.Single(p=>p.Id==s.Id);if(p.AreaId!=CurrentTravelDestinationId||!ClearedCampaignEncounters.Contains(p.Waves[s.NextWave-1]))continue;
   if(s.NextWave==p.Waves.Count){s.Status="cleared";AddStoryJournalEntry("encounter-plan."+p.Id,p.Name,"Every authored wave is cleared. This encounter will not respawn.","Encounters");SyncContracts();}
   else if(CanPlacePlanWave(p,s.NextWave))SpawnPlanWave(p,s);
  }
 }
 void ValidateStoryState()
 {
  if(ContractStates==null||EncounterPlanStates==null||ContractStates.Any(s=>s==null)||EncounterPlanStates.Any(s=>s==null)||ContractXpAwarded<0||ContractXpRemainder is <0 or >5||ContractStates.Select(s=>s.Id).Distinct().Count()!=ContractStates.Count||EncounterPlanStates.Select(s=>s.Id).Distinct().Count()!=EncounterPlanStates.Count)throw new InvalidDataException("Invalid story state collections or rewards.");
  if(!StoryEnabled){if(ContractStates.Count>0||EncounterPlanStates.Count>0||ContractXpAwarded+ContractXpRemainder>0)throw new InvalidDataException("Story state requires opt-in.");return;}
  long earned=0;var d=Campaign!.Story!;
  foreach(var s in ContractStates)
  {
   var q=d.Contracts.FirstOrDefault(q=>q.Id==s.Id);var journal=Quest(s.Id);if(q==null||s.Status is not ("active" or "completed" or "failed")||s.AcceptedMinute<Campaign.StartMinute||s.AcceptedMinute>CampaignMinutes||s.ResolvedMinute< -1||s.ResolvedMinute>CampaignMinutes||s.Status=="active"&&s.ResolvedMinute!=-1||s.Status!="active"&&s.ResolvedMinute<s.AcceptedMinute||s.BaselineSv<0||s.BaselineSv>SecuredSurvivalValue||s.Progress==null||s.Progress.Count!=q.Goals.Count||s.Progress.Where((v,i)=>v<0||v>q.Goals[i].Quantity).Any()||s.Status=="completed"&&s.Progress.Where((v,i)=>v!=q.Goals[i].Quantity).Any()||journal==null||journal.Status!=(s.Status=="completed"?QuestStatus.Completed:s.Status=="failed"?QuestStatus.Failed:QuestStatus.Active))throw new InvalidDataException("Invalid contract progress, journal or resolution.");
   if(q.Prerequisites.Any(id=>!ContractStates.Any(p=>p.Id==id&&p.Status=="completed"&&p.ResolvedMinute<=s.AcceptedMinute)))throw new InvalidDataException("Contract prerequisite not completed before acceptance.");if(s.Status=="completed")earned+=q.RewardXp;
  }
  if(earned!=(long)ContractXpAwarded+ContractXpRemainder||d.Contracts.Where(q=>q.BranchGroup!="").GroupBy(q=>q.BranchGroup).Any(group=>ContractStates.Count(s=>s.Status is "active" or "completed"&&group.Any(q=>q.Id==s.Id))>1))throw new InvalidDataException("Invalid contract reward/branch ledger.");
  var features=Features.Concat(StoredTravelAreas.SelectMany(a=>a.Features)).ToArray();foreach(var plan in d.EncounterPlans){var feature=features.FirstOrDefault(f=>f.Id==plan.TriggerFeatureId);if(feature!=null&&feature.Used!=EncounterPlanStates.Any(s=>s.Id==plan.Id)||EncounterPlanStates.Any(s=>s.Id==plan.Id)&&feature==null)throw new InvalidDataException("Encounter trigger and activation ledger disagree.");}
  var all=Actors.Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).ToArray();var expected=new HashSet<string>();
  foreach(var s in EncounterPlanStates)
  {
   var p=d.EncounterPlans.FirstOrDefault(p=>p.Id==s.Id);if(p==null||s.Status is not ("active" or "cleared")||s.NextWave<1||s.NextWave>p.Waves.Count||s.Status=="cleared"&&s.NextWave!=p.Waves.Count)throw new InvalidDataException("Invalid encounter wave cursor.");
   if(p.RequiredContractId!=""&&!ContractStates.Any(q=>q.Id==p.RequiredContractId))throw new InvalidDataException("Encounter activated without accepting its contract.");
   for(int i=0;i<s.NextWave;i++){var e=Campaign.Encounters.Single(e=>e.Id==p.Waves[i]);foreach(var id in e.ActorIds){expected.Add(id);if(!all.Any(a=>a.Id==id&&a.Enemy))throw new InvalidDataException("Released wave actor is missing.");}if((i<s.NextWave-1||s.Status=="cleared")&&(!ClearedCampaignEncounters.Contains(e.Id)||e.ActorIds.Any(id=>all.Any(a=>a.Id==id&&a.Alive))))throw new InvalidDataException("Earlier wave not cleared.");}
  }
  if(all.Any(a=>StoryReservedActor(a.Id)&&!expected.Contains(a.Id))||d.EncounterPlans.SelectMany(p=>p.Waves).Any(e=>ClearedCampaignEncounters.Contains(e)&&Campaign.Encounters.Single(x=>x.Id==e).ActorIds.Any(id=>!expected.Contains(id))))throw new InvalidDataException("Dormant actors or future wave credit injected.");
 }
}
