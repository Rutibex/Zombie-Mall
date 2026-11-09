using Ashen.Core;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 ReactionPolicy ReactionPolicyFor(ReactionKind kind)=>kind switch
 {
  ReactionKind.OpportunityAttack=>preferences.OpportunityAttackPolicy,
  ReactionKind.Shield=>preferences.ShieldPolicy,
  ReactionKind.Counterspell=>preferences.CounterspellPolicy,
  ReactionKind.ReadiedAction=>preferences.ReadiedActionPolicy,
  ReactionKind.CuttingWords=>preferences.CuttingWordsPolicy,
  _=>ReactionPolicy.Ask
 };

 void BindReactionPolicies()=>world.ReactionPolicyResolver=(actor,kind)=>actor.Enemy?ReactionPolicy.Auto:ReactionPolicyFor(kind);

 static string ReactionPolicyText(ReactionPolicy policy)=>policy switch
 {
  ReactionPolicy.Auto=>"AUTO",
  ReactionPolicy.Never=>"NEVER",
  _=>"ASK"
 };

 void SetReactionPolicy(ReactionKind kind,ReactionPolicy policy)
 {
  switch(kind)
  {
   case ReactionKind.OpportunityAttack:preferences.OpportunityAttackPolicy=policy;break;
   case ReactionKind.Shield:preferences.ShieldPolicy=policy;break;
   case ReactionKind.Counterspell:preferences.CounterspellPolicy=policy;break;
   case ReactionKind.ReadiedAction:preferences.ReadiedActionPolicy=policy;break;
   case ReactionKind.CuttingWords:preferences.CuttingWordsPolicy=policy;break;
  }
  SavePreferences();
 }

 void CycleReactionPolicy(ReactionKind kind)
 {
  var current=ReactionPolicyFor(kind);var next=current switch{ReactionPolicy.Ask=>ReactionPolicy.Auto,ReactionPolicy.Auto=>ReactionPolicy.Never,_=>ReactionPolicy.Ask};
  SetReactionPolicy(kind,next);Notify($"{ReactionLabel(kind)} reactions: {ReactionPolicyText(next)}.");
 }

 static string ReactionLabel(ReactionKind kind)=>kind switch
 {
  ReactionKind.OpportunityAttack=>"Opportunity Attack",
  ReactionKind.Shield=>"Shield",
  ReactionKind.Counterspell=>"Counterspell",
  ReactionKind.ReadiedAction=>"Readied Action",
  ReactionKind.CuttingWords=>"Cutting Words",
  _=>"Reaction"
 };

 void SyncReactionModal()
 {
  if(world.PendingReaction!=null)
  {
   ui.Portraits.CancelInteraction();contextOpen=false;leftWorldPress=boxDragging=false;ui.Modals.Open(UiModalId.Reaction);
  }
  else if(ui.Modals.ReactionOpen)ui.Modals.Close();
 }

 void ResolveReaction(bool accept)
 {
  var offer=world.PendingReaction;if(offer==null){SyncReactionModal();return;}
  string name=offer.Title;
  if(world.ResolvePendingReaction(accept))Notify(accept?$"{name}: reaction used.":$"{name}: declined.");
  SyncReactionModal();
 }
}
