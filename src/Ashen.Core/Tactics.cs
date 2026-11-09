namespace Ashen.Core;

/// <summary>
/// IIP21 turn-based party automation. This is intentionally a rules-layer decision engine:
/// it requests the same attacks, movement, healing and standard actions available to the player.
/// It never owns initiative and never grants resources outside ActionEconomyState.
/// </summary>
public enum TacticsStepResult { Waiting, Moving, Acted, EndedTurn }

public sealed partial class World
{
 public static string TacticsProfileName(TacticsProfile profile)=>profile switch
 {
  TacticsProfile.Aggressive=>"Aggressive",
  TacticsProfile.Defensive=>"Defensive",
  TacticsProfile.Support=>"Support",
  TacticsProfile.Ranged=>"Ranged",
  _=>"Balanced"
 };
 public static string TacticsProfileHint(TacticsProfile profile)=>profile switch
 {
  TacticsProfile.Aggressive=>"Press the attack, spend signature abilities readily, and prefer wounded enemies.",
  TacticsProfile.Defensive=>"Protect low HP, use potions/Dodge before committing, then attack cautiously.",
  TacticsProfile.Support=>"Prioritize healing and Help when useful, then make a normal attack.",
  TacticsProfile.Ranged=>"Prefer normal ranged attacks and approach only far enough to obtain a clear shot.",
  _=>"Use healing when urgent, otherwise attack efficiently and preserve limited abilities unless they have strong value."
 };

 public TacticsStepResult ExecuteTacticsStep(Actor actor)
 {
  if(Mode!=Mode.Combat||Active!=actor||actor.Enemy||!actor.Alive)return TacticsStepResult.Waiting;
  if(ReactionInterruptActive)return TacticsStepResult.Waiting;
  if(IsMoving(actor))return TacticsStepResult.Moving;
  var enemies=Actors.Where(x=>x.Enemy&&x.Alive&&Order.Contains(x.Id)).ToList();
  if(enemies.Count==0){if(CheckEnd())return TacticsStepResult.EndedTurn;EndTurn();return TacticsStepResult.EndedTurn;}

  if(ModernTacticalCombat&&actor.Equipment.Weapon?.Definition.MagazineSize>0&&!HasRequiredAmmunition(actor))
  {
   if(actor.Economy.ActionAvailable&&ReloadFirearm(actor))return TacticsStepResult.Acted;
   for(int slot=0;slot<4;slot++)if(QuickWeapon(actor,slot) is {} weapon&&weapon.Definition.Kind==ItemKind.Weapon&&weapon.Definition.MagazineSize==0&&ActivateQuickWeapon(actor,slot))return TacticsStepResult.Acted;
   if(actor.Economy.ActionAvailable&&ExecuteStandardAction(actor,StandardActionKind.Dodge))return TacticsStepResult.Acted;
   EndTurn();return TacticsStepResult.EndedTurn;
  }

  // Support and Balanced profiles may rescue a wounded ally before attacking.
  if(actor.Economy.ActionAvailable&&actor.CanHeal&&(actor.Tactics is TacticsProfile.Support or TacticsProfile.Balanced))
  {
   float threshold=actor.Tactics==TacticsProfile.Support ? .72f : .42f;
   var ally=Party.Where(x=>x.Alive&&x.Hp<x.MaxHp&&x.Hp/(float)Math.Max(1,x.MaxHp)<=threshold)
    .Where(x=>actor.Position.Distance(x.Position)<=Math.Max(1,actor.HealingFeature!.Mechanics.RangeFeet/5f)&&Line(actor.Position,x.Position))
    .OrderBy(x=>x.Hp/(float)Math.Max(1,x.MaxHp)).ThenBy(x=>PartyIndex(x)).FirstOrDefault();
   if(ally!=null&&Heal(actor,ally))return TacticsStepResult.Acted;
  }

  // Defensive characters stabilize themselves before spending the turn offensively.
  if(actor.Tactics==TacticsProfile.Defensive&&actor.Economy.ActionAvailable&&actor.Hp<=Math.Max(1,(int)MathF.Ceiling(actor.MaxHp*.38f)))
  {
   if(ItemCount(actor,"healing-potion")>0&&actor.Hp<actor.MaxHp&&Potion(actor))return TacticsStepResult.Acted;
   if(ExecuteStandardAction(actor,StandardActionKind.Dodge))return TacticsStepResult.Acted;
  }

  // Support characters without a healing target can grant advantage to an adjacent ally.
  if(actor.Tactics==TacticsProfile.Support&&actor.Economy.ActionAvailable)
  {
   var ally=Party.Where(x=>x!=actor&&x.Alive&&actor.Position.Distance(x.Position)<=1.25f)
    .OrderByDescending(x=>x.AttackBonus).ThenBy(x=>PartyIndex(x)).FirstOrDefault();
   if(ally!=null&&Help(actor,ally))return TacticsStepResult.Acted;
  }

  // Aggressive automation spends signature charges readily. Balanced spends them only when
  // they splash multiple enemies or can finish an already wounded target. Ranged preserves them.
  if(actor.Economy.ActionAvailable&&actor.SignatureFeature!=null&&(actor.SignatureResource?.Current??0)>0&&actor.Tactics!=TacticsProfile.Ranged)
  {
   var special=BestTacticsTarget(actor,enemies,AttackKind.Special);
   if(special.Target!=null)
   {
    bool worthwhile=actor.Tactics==TacticsProfile.Aggressive||actor.Tactics==TacticsProfile.Support||
     special.Preview.Splash.Count>=2||special.Target.Hp<=special.Preview.MaxDamage;
    if(worthwhile&&RequestAttack(actor,special.Target,AttackKind.Special))return IsMoving(actor)?TacticsStepResult.Moving:TacticsStepResult.Acted;
   }
  }

  if(actor.Economy.CanAttack)
  {
   var basic=BestTacticsTarget(actor,enemies,AttackKind.Basic);
   if(basic.Target!=null&&RequestAttack(actor,basic.Target,AttackKind.Basic))return IsMoving(actor)?TacticsStepResult.Moving:TacticsStepResult.Acted;
  }

  // If no attack can be reached this turn, advance toward the nearest hostile while respecting
  // the remaining movement budget. The next tactics pulse may attack after the route completes.
  if(actor.Economy.CanAttack&&actor.Move>.01f&&TryTacticsAdvance(actor,enemies))return TacticsStepResult.Moving;

  // Do not waste an unused Action simply because no target can be reached.
  if(actor.Economy.ActionAvailable)
  {
   var fallback=actor.Tactics==TacticsProfile.Defensive?StandardActionKind.Dodge:StandardActionKind.Ready;
   if(ExecuteStandardAction(actor,fallback))return TacticsStepResult.Acted;
  }

  EndTurn();return TacticsStepResult.EndedTurn;
 }

 (Actor? Target,ActionPreview Preview) BestTacticsTarget(Actor actor,List<Actor> enemies,AttackKind kind)
 {
  var candidates=enemies.Select(target=>(Target:target,Preview:PreviewAttack(actor,target,kind)))
   .Where(x=>x.Preview.Allowed).ToList();
  if(candidates.Count==0)return(null,new(false,"No legal target",0,0,0,0,0,0,[],[]));
  var chosen=(actor.Tactics switch
  {
   TacticsProfile.Aggressive=>candidates.OrderBy(x=>x.Target.Hp).ThenByDescending(x=>x.Preview.HitChance),
   TacticsProfile.Defensive=>candidates.OrderBy(x=>x.Preview.MovementFeet).ThenBy(x=>x.Target.Hp),
   TacticsProfile.Ranged=>candidates.OrderBy(x=>x.Preview.MovementFeet).ThenByDescending(x=>x.Preview.HitChance),
   TacticsProfile.Support=>candidates.OrderBy(x=>x.Target.Hp).ThenBy(x=>x.Preview.MovementFeet),
   _=>candidates.OrderByDescending(x=>x.Preview.HitChance).ThenBy(x=>x.Preview.MovementFeet).ThenBy(x=>x.Target.Hp)
  }).First();
  return chosen;
 }

 bool TryTacticsAdvance(Actor actor,List<Actor> enemies)
 {
  var target=enemies.OrderBy(x=>actor.Position.Distance(x.Position)).FirstOrDefault();if(target==null)return false;
  float preferred=Math.Max(.8f,actor.Range-.10f);var candidates=new List<WorldPoint>();
  for(int i=0;i<24;i++)
  {
   float angle=i*MathF.Tau/24;var point=target.Position+new WorldPoint(MathF.Cos(angle),MathF.Sin(angle))*preferred;
   if(Explored.Contains(point.Cell)&&CanStand(point,actor)&&Line(point,target.Position))candidates.Add(point);
  }
  foreach(var goal in candidates.OrderBy(p=>p.Distance(actor.Position)))
  {
   var path=Path(actor,goal);if(path.Count==0)continue;float budget=actor.Move;var origin=actor.Position;WorldPoint? last=null;
   foreach(var waypoint in path)
   {
    float distance=origin.Distance(waypoint);
    if(distance<=budget+.0001f){last=waypoint;budget-=distance;origin=waypoint;continue;}
    if(budget>.05f)last=origin+(waypoint-origin)*(budget/distance);
    break;
   }
   if(last is {} destination&&destination.Distance(actor.Position)>.05f&&MoveTo(actor,destination))return true;
  }
  return false;
 }
}
