using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReactionKind { OpportunityAttack, Shield, Counterspell, ReadiedAction, CuttingWords }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReactionPolicy { Ask, Auto, Never }

/// <summary>
/// UI-safe description of an interrupt. It carries no executable delegates and therefore
/// cannot accidentally grant the reactor a full initiative turn.
/// </summary>
public sealed record ReactionOffer(
 int Sequence,
 ReactionKind Kind,
 string ReactorId,
 string ReactorName,
 string TriggerActorId,
 string TriggerActorName,
 string TargetId,
 string TargetName,
 string Title,
 string Description,
 string AcceptLabel,
 string DeclineLabel);

public sealed partial class World
{
 sealed class ReactionRequest
 {
  public required ReactionOffer Offer {get;init;}
  public required Action<bool> Resolve {get;init;}
 }

 readonly Queue<ReactionRequest> reactionQueue=[];
 readonly Dictionary<string,HashSet<string>> opportunityOffered=[];
 ReactionRequest? pendingReaction;
 bool pumpingReactions;
 int nextReactionSequence=1;

 /// <summary>
 /// Optional presentation/user-preference policy provider. Enemy reactions remain automatic;
 /// player-controlled reactors default to Ask if no provider is bound.
 /// </summary>
 [JsonIgnore] public Func<Actor,ReactionKind,ReactionPolicy>? ReactionPolicyResolver {get;set;}
 [JsonIgnore] public ReactionOffer? PendingReaction=>pendingReaction?.Offer;
 [JsonIgnore] public bool ReactionInterruptActive=>pendingReaction!=null;
 [JsonIgnore] public int QueuedReactionCount=>reactionQueue.Count;

 public ReactionPolicy PolicyFor(Actor reactor,ReactionKind kind)
 {
  if(reactor.Enemy)return ReactionPolicy.Auto;
  var policy=ReactionPolicyResolver?.Invoke(reactor,kind)??ReactionPolicy.Ask;
  return Enum.IsDefined(typeof(ReactionPolicy),policy)?policy:ReactionPolicy.Ask;
 }

 ReactionOffer BuildOffer(ReactionKind kind,Actor reactor,Actor trigger,Actor target,string title,string description,string accept,string decline)=>
  new(nextReactionSequence++,kind,reactor.Id,reactor.Name,trigger.Id,trigger.Name,target.Id,target.Name,title,description,accept,decline);

 /// <summary>
 /// Adds a reaction interrupt without changing Turn/Round. Ask pauses at PendingReaction;
 /// Auto/Never resolve synchronously and then continue the interrupted operation.
 /// </summary>
 bool OfferReaction(ReactionOffer offer,Action<bool> resolve)
 {
  var reactor=Actors.FirstOrDefault(a=>a.Id==offer.ReactorId);
  if(Mode!=Mode.Combat||reactor?.Alive!=true||!CanReact(reactor)){resolve(false);return false;}
  reactionQueue.Enqueue(new(){Offer=offer,Resolve=resolve});PumpReactions();return true;
 }

 void PumpReactions()
 {
  if(pumpingReactions||pendingReaction!=null)return;
  pumpingReactions=true;
  try
  {
   while(pendingReaction==null&&reactionQueue.Count>0)
   {
    var request=reactionQueue.Dequeue();var reactor=Actors.FirstOrDefault(a=>a.Id==request.Offer.ReactorId);
    if(Mode!=Mode.Combat||reactor?.Alive!=true||!CanReact(reactor)){request.Resolve(false);continue;}
    var policy=PolicyFor(reactor,request.Offer.Kind);
    if(policy==ReactionPolicy.Never)
    {
     Message(MessageCategory.Reaction,$"{reactor.Name} declines {ReactionName(request.Offer.Kind)} by policy.");request.Resolve(false);continue;
    }
    if(policy==ReactionPolicy.Auto)
    {
     ResolveRequest(request,true,true);continue;
    }
    pendingReaction=request;
    Message(MessageCategory.Reaction,$"Reaction available: {request.Offer.Title}.",request.Offer.Description);
   }
  }
  finally{pumpingReactions=false;}
 }

 static string ReactionName(ReactionKind kind)=>kind switch
 {
  ReactionKind.OpportunityAttack=>"Opportunity Attack",
  ReactionKind.Shield=>"Shield",
  ReactionKind.Counterspell=>"Counterspell",
  ReactionKind.ReadiedAction=>"Readied Action",
  ReactionKind.CuttingWords=>"Cutting Words",
  _=>"Reaction"
 };

 void ResolveRequest(ReactionRequest request,bool accept,bool automatic=false)
 {
  var reactor=Actors.FirstOrDefault(a=>a.Id==request.Offer.ReactorId);bool accepted=accept&&reactor?.Alive==true&&CanReact(reactor!);
  if(accepted)
  {
   if(!SpendReaction(reactor!,ReactionName(request.Offer.Kind)))accepted=false;
   else Message(MessageCategory.Reaction,$"{reactor!.Name} uses {ReactionName(request.Offer.Kind)}{(automatic?" automatically":"")}.");
  }
  else if(reactor!=null&&accept)Message(MessageCategory.Reaction,$"{reactor.Name} can no longer take that reaction.");
  else if(reactor!=null)Message(MessageCategory.Reaction,$"{reactor.Name} declines {ReactionName(request.Offer.Kind)}.");
  request.Resolve(accepted);
 }

 public bool ResolvePendingReaction(bool accept)
 {
  var request=pendingReaction;if(request==null)return false;pendingReaction=null;ResolveRequest(request,accept);PumpReactions();return true;
 }

 /// <summary>Clears transient interrupt state on combat teardown/load/new game.</summary>
 public void ClearReactionInterrupts()
 {
  pendingReaction=null;reactionQueue.Clear();opportunityOffered.Clear();pumpingReactions=false;
 }

 public bool CanCastReactionSpell(Actor actor,string spellId,int minimumLevel)
 {
  if(!actor.Alive||!CanReact(actor)||!SpellCatalog.TryGet(spellId,out var spell)||spell.CastingTime!=SpellCastingTime.Reaction)return false;
  return actor.Character.Spellcasting.Any(source=>source.SpellAvailable(spellId,actor.Character)&&source.Slots.Any(s=>s.Level>=Math.Max(minimumLevel,spell.Level)&&s.Current>0));
 }

 bool SpendReactionSpellSlot(Actor actor,string spellId,int minimumLevel,out int slotLevel)
 {
  foreach(var source in actor.Character.Spellcasting)if(SpellcastingRules.TrySpendSlot(actor.Character,source,spellId,minimumLevel,out slotLevel))return true;
  slotLevel=0;return false;
 }

 bool CanShield(Actor target,int attackRoll,int attackTotal)
 {
  if(attackRoll==20||target.Character.HasCondition("shielded")||!CanCastReactionSpell(target,"shield",1))return false;
  // Auto/Ask only when +5 AC can actually change this attack from hit to miss.
  return attackTotal>=target.Ac&&attackTotal<target.Ac+5;
 }

 /// <summary>
 /// Resolves a normal or reaction weapon attack. Shield may suspend the resolution after
 /// the attack roll and resume it after the player accepts/declines the interrupt.
 /// </summary>
 void ResolveAttackWithReactions(Actor attacker,Actor target,bool special,FeatureEntry? signature,string sourceLabel="Attack")
 {
  if(!attacker.Alive||!target.Alive)return;
  int cover=CoverAgainst(attacker,target);
  if(ModernTacticalCombat)Message(MessageCategory.Combat,DescribeAttackContext(attacker,target));
  var inspiration=attacker.Character.Conditions.FirstOrDefault(c=>c.Id=="bardic-inspiration");var inspirationSource=inspiration==null?null:Actors.FirstOrDefault(a=>a.Name.Equals(inspiration.Source,StringComparison.OrdinalIgnoreCase));attacker.Face(target.Position);int first,second;string rollMode;int roll=AttackRoll(attacker,target,out first,out second,out rollMode),blessBonus=attacker.Character.HasCondition("blessed")?Roll(4):0,inspirationBonus=inspiration!=null?Roll(BardicInspirationDie(inspirationSource?.Character??attacker.Character)):0,bonus=attacker.AttackBonus+SurvivorAttackBonus(attacker,target)+blessBonus+inspirationBonus,rolledTotal=roll+bonus;if(inspirationBonus>0){attacker.Character.SetCondition("bardic-inspiration","Bardic Inspiration",false);Message(MessageCategory.Roll,$"{attacker.Name} spends Bardic Inspiration (+{inspirationBonus}).");}

  void ContinueAfterCuttingWords(int total,int cuttingPenalty=0)
  {
   bool initialHit=roll==20||(roll!=1&&total>=(target.Ac+cover));
   string cutting=cuttingPenalty>0?$" - {cuttingPenalty} Cutting Words":"";
   if(rollMode=="")
   {
    if(cuttingPenalty==0&&cover==0)LogAttackRoll(attacker,target,roll,bonus,total,initialHit);
    else Message(MessageCategory.Roll,$"{attacker.Name} attacks {target.Name}: {(initialHit?"HIT":"MISS")}.",$"{attacker.Name}: d20 {roll} + {bonus}{cutting} = {total} vs AC {(target.Ac+cover)}: {(initialHit?"HIT":"MISS")}.");
   }
   else Message(MessageCategory.Roll,$"{attacker.Name} {(initialHit?"hits":"misses")} {target.Name}.",$"{sourceLabel} ({rollMode}): d20 {first} / {second} -> {roll} + {bonus}{cutting} = {total} vs AC {(target.Ac+cover)}: {(initialHit?"HIT":"MISS")}.");

   void ContinueShield(bool shieldAccepted)
   {
    if(shieldAccepted)
    {
     if(SpendReactionSpellSlot(target,"shield",1,out int slot))
     {
      target.Character.SetCondition("shielded","Shield",true,$"Shield, level {slot} slot",tags:["reaction","spell"]);
      LogCondition(target,"Shield",true,$"AC +5 until the start of {target.Name}'s next turn");
     }
     else Message(MessageCategory.Reaction,$"{target.Name}'s Shield fails because no spell slot remains.");
    }
    bool hit=roll==20||(roll!=1&&total>=(target.Ac+cover));
    if(initialHit&&!hit)Message(MessageCategory.Reaction,$"Shield turns the attack aside: {total} no longer reaches {target.Name}'s AC {(target.Ac+cover)}.");
    ResolveAttackOutcome(attacker,target,roll,hit,special,signature);
   }

   if(initialHit&&CanShield(target,roll,total-cover))
   {
    int baseAc=(target.Ac+cover),shieldAc=baseAc+5;
    var offer=BuildOffer(ReactionKind.Shield,target,attacker,target,"SHIELD",
     $"{attacker.Name}'s attack total is {total} against AC {baseAc}. Spend your Reaction and a 1st-level-or-higher spell slot to raise AC to {shieldAc} until the start of your next turn.",
     "CAST SHIELD","TAKE THE HIT");
    OfferReaction(offer,ContinueShield);return;
   }
   ContinueShield(false);
  }

  if(roll is not (1 or 20)&&rolledTotal>=(target.Ac+cover))
  {
   var cutter=FindCuttingWordsReactor(attacker,target,rolledTotal-cover);
   if(cutter!=null)
   {
    int die=BardicInspirationDie(cutter.Character);
    var offer=BuildOffer(ReactionKind.CuttingWords,cutter,attacker,target,"CUTTING WORDS",
     $"{attacker.Name}'s attack total is {rolledTotal} against {target.Name}'s AC {(target.Ac+cover)}. Spend {cutter.Name}'s Reaction and one Bardic Inspiration die (d{die}) to subtract the roll before the attack resolves.",
     "CUTTING WORDS","ALLOW ATTACK");
    OfferReaction(offer,accepted=>
    {
     int penalty=0;
     if(accepted)
     {
      var resource=cutter.Character.Resource("bardic-inspiration");
      if(resource?.Current>0)
      {
       resource.Current--;penalty=Roll(die);Message(MessageCategory.Reaction,$"{cutter.Name}'s Cutting Words reduces the attack by {penalty}.",$"Bardic Inspiration: d{die} = {penalty}; attack total {rolledTotal} -> {rolledTotal-penalty}.");
      }
      else Message(MessageCategory.Reaction,$"{cutter.Name} has no Bardic Inspiration remaining; Cutting Words has no effect.");
     }
     ContinueAfterCuttingWords(rolledTotal-penalty,penalty);
    });return;
   }
  }
  ContinueAfterCuttingWords(rolledTotal);
 }

 Actor? FindCuttingWordsReactor(Actor attacker,Actor target,int attackTotal)
 {
  return Actors.Where(a=>a.Alive&&a.Enemy!=attacker.Enemy&&CanReact(a)&&a.Position.Distance(attacker.Position)<=12.001f&&Line(a.Position,attacker.Position)
    &&a.Character.Features.Any(f=>f.Id.Equals("cutting-words",StringComparison.OrdinalIgnoreCase))
    &&a.Character.Resource("bardic-inspiration")?.Current>0)
   .Where(a=>PolicyFor(a,ReactionKind.CuttingWords)!=ReactionPolicy.Auto||attackTotal-target.Ac<BardicInspirationDie(a.Character))
   .OrderBy(a=>a.Enemy?1:0).ThenBy(a=>PartyIndex(a)<0?99:PartyIndex(a)).FirstOrDefault();
 }

 static int BardicInspirationDie(CharacterSheet sheet)
 {
  int level=sheet.Classes.FirstOrDefault(c=>c.ClassId.Equals("Bard",StringComparison.OrdinalIgnoreCase))?.Level??1;
  return level>=15?12:level>=10?10:level>=5?8:6;
 }

 void ResolveAttackOutcome(Actor attacker,Actor target,int roll,bool hit,bool special,FeatureEntry? signature)
 {
  bool equipmentCritical=roll>=(attacker.Equipment.Weapon?.Definition.Equipment?.Critical??20)&&!LowerTraits(target).Contains("gallery-nocrit");
  attacker.Statistics.AttacksMade++;if(hit){attacker.Statistics.AttacksHit++;if(equipmentCritical)attacker.Statistics.CriticalHits++;}
  if(target.ClassIs("wise",5))target.Character.SetCondition("class-evasion-used","Predictive Evasion used",true);
  bool removeHidden=attacker.Character.HasCondition("hidden"),removeHelped=attacker.Character.HasCondition("helped"),removeMockery=attacker.Character.HasCondition("vicious-mockery"),removeGuided=target.Character.HasCondition("guided");
  Upper26AttackRoll(attacker,roll);
  if(removeHidden&&!attacker.ClassIs("agile",6)){attacker.Character.SetCondition("hidden","Hidden",false);LogCondition(attacker,"Hidden",false,"attack made");}
  if(removeHelped){attacker.Character.SetCondition("helped","Helped",false);LogCondition(attacker,"Helped",false,"attack made");}
  if(removeMockery){attacker.Character.SetCondition("vicious-mockery","Vicious Mockery",false);LogCondition(attacker,"Vicious Mockery",false,"attack made");}
  if(removeGuided){target.Character.SetCondition("guided","Guided",false);LogCondition(target,"Guided",false,"attack received");}
  if(hit&&target.Alive)
  {
   int damage=RollDamage(attacker.DamageDice,attacker.Die)+(equipmentCritical?RollDamage(attacker.DamageDice,attacker.Die):0)+attacker.DamageBonus;
   var mark=target.Character.Conditions.FirstOrDefault(c=>c.Id=="hunters-mark"&&c.Source.Equals($"spell:hunters-mark:{attacker.Id}",StringComparison.OrdinalIgnoreCase));if(mark!=null)damage+=RollDamage(1,6);
   // IIP34 Sneak Attack setting: automatic by default, once per turn, when the rogue has advantage or an ally threatens the target.
   if(!special&&attacker.Character.Feature("sneak-attack")!=null&&!attacker.Character.HasCondition("sneak-attack-disabled")&&!attacker.Character.HasCondition("sneak-attack-used"))
   {
    bool finesseOrRanged=attacker.Character.MainAttack.Ability==Ability.Dexterity;bool advantage=attacker.Character.HasCondition("hidden")||attacker.Character.HasCondition("helped")||target.Character.HasCondition("faerie-fire");
    bool allyThreatens=Actors.Any(x=>x!=attacker&&!x.Enemy&&x.Alive&&x.Position.Distance(target.Position)<=1.25f);
    if(finesseOrRanged&&(advantage||allyThreatens)){int rogueLevel=LevelingRules.ClassLevel(attacker.Character,"Rogue"),dice=Math.Max(1,(rogueLevel+1)/2);int sneak=RollDamage(dice,6);damage+=sneak;attacker.Character.SetCondition("sneak-attack-used","Sneak Attack Used",true,"IIP34");Message(MessageCategory.Combat,$"{attacker.Name} adds Sneak Attack for {sneak} damage.");}
   }
   if(special&&signature!=null){var m=signature.Mechanics;damage+=RollDamage(m.ExtraDamageDice,m.ExtraDamageDieSize)+m.ExtraDamageBonus;}
   damage+=SurvivorExtraDamage(attacker,target,equipmentCritical,removeHidden);
   if(target.ClassIs("wise",5))target.Character.SetCondition("class-evasion-used","Predictive Evasion used",true);
   Hurt(target,damage,attacker.AttackDamageType,attacker,equipmentCritical);
   if(special&&attacker.SpecialSplashes)foreach(var other in Actors.Where(x=>x!=target&&x.Enemy!=attacker.Enemy&&x.Alive&&x.Position.Distance(target.Position)<=1.25f&&Line(target.Position,x.Position)).ToList())Hurt(other,Math.Max(1,damage/2),attacker.AttackDamageType,attacker);
  }
  WearWeapon(attacker,hit);Sound?.Invoke(special?"spell":"hit");CheckEnd();
 }

 public bool ReactionAttack(Actor reactor,Actor target,string source="Reaction attack")
 {
  if(WorkshopEnabled&&reactor.Equipment.Weapon?.Broken==true)return false;
  if(Mode!=Mode.Combat||!reactor.Alive||!target.Alive||reactor.Enemy==target.Enemy||!HasRequiredAmmunition(reactor))return false;
  int range=reactor.Range;if(reactor.Position.Distance(target.Position)>range+.001f||!WeaponLine(reactor,target))return false;
  if(!ConsumeRequiredAmmunition(reactor))return false;ResolveAttackWithReactions(reactor,target,false,null,source);return true;
 }

 internal void BeginMovementReactionWindow(Actor mover)
 {
  opportunityOffered.Remove(mover.Id);
 }

 internal void EndMovementReactionWindow(Actor mover)
 {
  opportunityOffered.Remove(mover.Id);
 }

 /// <summary>Called before a movement substep. Opportunity attacks occur before reach is left.</summary>
 internal bool TriggerOpportunityBeforeStep(Actor mover,WorldPoint next)
 {
  if(Mode!=Mode.Combat||!mover.Alive||mover.Character.HasCondition("disengaged")||mover.ClassIs("agile",10))return false;
  var offered=opportunityOffered.GetValueOrDefault(mover.Id);if(offered==null){offered=[];opportunityOffered[mover.Id]=offered;}
  foreach(var reactor in Actors.Where(a=>a.Alive&&a.Enemy!=mover.Enemy&&a.Range<=1&&CanReact(a)&&HasRequiredAmmunition(a)).OrderBy(a=>a.Enemy?1:0).ThenBy(a=>PartyIndex(a)<0?99:PartyIndex(a)))
  {
   if(offered.Contains(reactor.Id)||!WeaponLine(reactor,mover))continue;
   float reach=Math.Max(1f,reactor.Range);
   float before=reactor.Position.Distance(mover.Position),after=reactor.Position.Distance(next);
   if(before>reach+.001f||after<=reach+.001f)continue;
   offered.Add(reactor.Id);
   var offer=BuildOffer(ReactionKind.OpportunityAttack,reactor,mover,mover,"OPPORTUNITY ATTACK",
    $"{mover.Name} is leaving {reactor.Name}'s 5-foot reach. Make one melee weapon attack before {mover.Name} leaves it. This spends {reactor.Name}'s Reaction, not an Action or a turn.",
    "ATTACK","LET THEM GO");
   OfferReaction(offer,accepted=>{if(accepted&&mover.Alive)ReactionAttack(reactor,mover,"Opportunity Attack");});
   if(ReactionInterruptActive)return true;
   if(!mover.Alive)return true;
  }
  return false;
 }

 /// <summary>Readied Attack default trigger: a hostile enters weapon range.</summary>
 internal bool TriggerReadiedAfterStep(Actor mover,WorldPoint before)
 {
  if(Mode!=Mode.Combat||!mover.Alive)return false;
  foreach(var reactor in Actors.Where(a=>a.Alive&&a.Enemy!=mover.Enemy&&a.Character.HasCondition("readying")&&CanReact(a)&&HasRequiredAmmunition(a)).ToArray())
  {
   float range=Math.Max(1,reactor.Range);float oldDistance=reactor.Position.Distance(before),newDistance=reactor.Position.Distance(mover.Position);
   if(oldDistance<=range+.001f||newDistance>range+.001f||!WeaponLine(reactor,mover))continue;
   if(OfferReadiedAttack(reactor,mover,"enters weapon range")&&ReactionInterruptActive)return true;
   if(!mover.Alive)return true;
  }
  return false;
 }

 internal void TriggerReadiedAtTurnStart(Actor active)
 {
  if(Mode!=Mode.Combat||!active.Alive)return;
  foreach(var reactor in Actors.Where(a=>a.Alive&&a.Enemy!=active.Enemy&&a.Character.HasCondition("readying")&&CanReact(a)&&HasRequiredAmmunition(a)).ToArray())
  {
   if(reactor.Position.Distance(active.Position)>reactor.Range+.001f||!WeaponLine(reactor,active))continue;
   if(OfferReadiedAttack(reactor,active,"begins a turn in weapon range")&&ReactionInterruptActive)return;
   if(!active.Alive)return;
  }
 }

 bool OfferReadiedAttack(Actor reactor,Actor target,string trigger)
 {
  var offer=BuildOffer(ReactionKind.ReadiedAction,reactor,target,target,"READIED ATTACK",
   $"{target.Name} {trigger}. {reactor.Name} readied a basic weapon attack. Use the Reaction now or keep the readied response available for a later valid trigger before {reactor.Name}'s next turn.",
   "RELEASE ATTACK","WAIT");
  return OfferReaction(offer,accepted=>
  {
   if(!accepted)return;
   reactor.Character.SetCondition("readying","Readied Action",false);LogCondition(reactor,"Readied Action",false,"reaction released");
   if(target.Alive)ReactionAttack(reactor,target,"Readied Attack");
  });
 }

 /// <summary>
 /// Hook for IIP30+ spell execution. Returns true if a Counterspell reaction window was
 /// offered or resolved. completion receives true when the spell is countered.
 /// </summary>
 public bool TryCounterspellWindow(Actor caster,string spellId,int spellLevel,Action<bool>? completion=null)
 {
  if(Mode!=Mode.Combat||!caster.Alive){completion?.Invoke(false);return false;}
  spellLevel=Math.Max(0,spellLevel);
  var reactor=Actors.Where(a=>a.Alive&&a.Enemy!=caster.Enemy&&a.Position.Distance(caster.Position)<=12.001f&&Line(a.Position,caster.Position)&&CanCastReactionSpell(a,"counterspell",3))
   .OrderBy(a=>a.Enemy?1:0).ThenBy(a=>a.Position.Distance(caster.Position)).FirstOrDefault();
  if(reactor==null){completion?.Invoke(false);return false;}
  var offer=BuildOffer(ReactionKind.Counterspell,reactor,caster,caster,"COUNTERSPELL",
   $"{caster.Name} is casting {DisplaySpell(spellId)} at level {Math.Max(1,spellLevel)} within 60 feet. Spend your Reaction and a 3rd-level-or-higher slot to attempt to interrupt it.",
   "COUNTERSPELL","ALLOW SPELL");
  OfferReaction(offer,accepted=>
  {
   if(!accepted){completion?.Invoke(false);return;}
   if(!SpendReactionSpellSlot(reactor,"counterspell",3,out int slot)){completion?.Invoke(false);return;}
   bool countered=slot>=spellLevel||spellLevel<=3;
   if(!countered)
   {
    int roll=Roll(20),bonus=reactor.Character.Spellcasting.First(s=>s.SpellAvailable("counterspell",reactor.Character)).SpellAttackBonus(reactor.Character)-reactor.ProficiencyBonus;
    int dc=10+spellLevel,total=roll+bonus;countered=total>=dc;
    Message(MessageCategory.Roll,$"{reactor.Name}'s Counterspell {(countered?"succeeds":"fails")}.",$"Counterspell ability check: d20 {roll} + {bonus} = {total} vs DC {dc}: {(countered?"SUCCESS":"FAILURE")}.");
   }
   Message(MessageCategory.Reaction,countered?$"{reactor.Name} counters {DisplaySpell(spellId)}.":$"{reactor.Name}'s Counterspell does not stop {DisplaySpell(spellId)}.");completion?.Invoke(countered);
  });
  return true;
 }

 static string DisplaySpell(string id)=>string.Join(" ",id.Split('-',StringSplitOptions.RemoveEmptyEntries).Select(p=>$"{char.ToUpperInvariant(p[0])}{p[1..]}"));
}
