namespace Ashen.Core;

public sealed record MovementPreview(bool Allowed,string Reason,float CostFeet,float RemainingFeet,List<WorldPoint> Route);
public sealed record ActionPreview(bool Allowed,string Reason,int Range,int HitChance,int MinDamage,int MaxDamage,int CriticalMax,float MovementFeet,List<WorldPoint> Route,List<string> Splash);
public sealed partial class World
{
 readonly Dictionary<string,(string Target,AttackKind Kind)> pendingAttacks=[];
 int HitChanceAgainst(Actor attacker,Actor target,WorldPoint? fromPosition=null)
 {
  int bonus=attacker.AttackBonus+SurvivorAttackBonus(attacker,target,fromPosition);
  var modifiers=AttackModifiers(attacker,target,fromPosition);bool advantage=modifiers.Advantage,disadvantage=modifiers.Disadvantage;int armor=target.Ac+CoverAgainst(attacker,target,fromPosition);
  if(advantage==disadvantage)return Enumerable.Range(1,20).Count(r=>r==20||r!=1&&r+bonus>=armor)*5;
  int hits=0;
  for(int a=1;a<=20;a++)for(int b=1;b<=20;b++)
  {
   int roll=advantage?Math.Max(a,b):Math.Min(a,b);
   if(roll==20||roll!=1&&roll+bonus>=armor)hits++;
  }
  return (int)Math.Round(hits/4d); // 400 equally likely pairs => percentage.
 }
 public MovementPreview PreviewMovement(Actor a,WorldPoint goal)
 {
  string reason=MovementReason(a,goal);var route=new List<WorldPoint>();float cost=0;
  if(reason==""&&a.Position.Distance(goal)>=.025f){route=Path(a,goal);cost=PathLength(a.Position,route)*5f;}
  return new(reason=="",reason,cost,a.Economy.MovementRemainingFeet,route);
 }
 public ActionPreview PreviewAttack(Actor a,Actor target,AttackKind kind)
 {
  bool special=kind==AttackKind.Special;int range=special?a.SpecialRange:a.Range;
  int chance=HitChanceAgainst(a,target);
  var attack=a.Character.MainAttack;var mechanics=special?a.SignatureFeature?.Mechanics:null;
  int min=Math.Max(0,a.DamageDice+a.DamageBonus),max=Math.Max(0,a.DamageDice*a.Die+a.DamageBonus);
  if(mechanics!=null){min+=mechanics.ExtraDamageDice+mechanics.ExtraDamageBonus;max+=mechanics.ExtraDamageDice*mechanics.ExtraDamageDieSize+mechanics.ExtraDamageBonus;}
  var route=new List<WorldPoint>();float cost=0;string reason="";
  if(Mode!=Mode.Combat||Active!=a)reason="Wait for this character's turn";
  else if(!a.Alive||!target.Alive||target.Enemy==a.Enemy)reason="Choose a living enemy";
  else if(special&&!a.Economy.ActionAvailable)reason="Action already used";
  else if(!special&&!a.Economy.CanAttack)reason=a.Economy.AttackActionStarted?"No attacks remain in this Attack action":"Action already used";
  else if(IsMoving(a))reason="Finish moving first";
  else if(special&&(a.SignatureFeature==null||(a.SignatureResource?.Current??0)<1))reason="No ability charges remaining";
  else if(WorkshopEnabled&&a.Equipment.Weapon?.Broken==true)reason="Repair or replace the broken weapon";
  else if(!HasRequiredAmmunition(a))reason=a.Equipment.Weapon?.Definition.MagazineSize>0?$"Empty firearm: reload {a.RequiredAmmunitionType}":$"No {a.RequiredAmmunitionType} ammunition equipped";
  else if(!Visible.Contains(target.Pos)&&!a.Enemy)reason="Target is not visible";
  else if(a.Position.Distance(target.Position)>range+.001f||!WeaponLine(a,target))
  {
   List<WorldPoint>? best=null;float length=float.MaxValue;
   for(int i=0;i<24;i++)
   {
    float angle=i*MathF.Tau/24;var point=target.Position+new WorldPoint(MathF.Cos(angle),MathF.Sin(angle))*(range-.08f);
    if(!Explored.Contains(point.Cell)||!WeaponLine(a,target,point)||!CanStand(point,a))continue;
    var path=Path(a,point);if(path.Count==0)continue;float distance=PathLength(a.Position,path);
    if(distance<length){best=path;length=distance;}
   }
   if(best==null)reason="No clear route into attack range";
   else{route=best;cost=length*5f;if(length>a.Move+.001f)reason="Not enough movement to reach attack range";}
  }
  var splash=special&&a.SpecialSplashes?Actors.Where(x=>x!=target&&x.Alive&&x.Enemy!=a.Enemy&&x.Position.Distance(target.Position)<=1.25f&&Line(target.Position,x.Position)).Select(x=>x.Id).ToList():[];
  if(ModernTacticalCombat&&route.Count>0)chance=HitChanceAgainst(a,target,route[^1]);
  var classDamage=SurvivorDamagePreview(a,target);min+=classDamage.Min;max+=classDamage.Max;
  int criticalMax=max+a.DamageDice*a.Die+classDamage.Crit;
  return new(reason=="",reason,range,chance,min,max,criticalMax,cost,route,splash);
 }
 public bool RequestAttack(Actor a,Actor target,AttackKind kind)
 {
  var p=PreviewAttack(a,target,kind);if(!p.Allowed){LogSystem(p.Reason+".",true);return false;}
  if(p.Route.Count==0)return Attack(a,target,kind);
  if(!MoveTo(a,p.Route.Last()))return false;
  pendingAttacks[a.Id]=(target.Id,kind);return true;
 }
 void CompleteAttacks()
 {
  foreach(var (id,order) in pendingAttacks.ToArray())
  {
   var actor=Actors.First(a=>a.Id==id);if(IsMoving(actor))continue;pendingAttacks.Remove(id);
   var target=Actors.First(a=>a.Id==order.Target);if(Mode==Mode.Combat&&Active==actor)Attack(actor,target,order.Kind);
  }
 }
 public string MovementReason(Actor a,WorldPoint goal)
 {
  if(!a.Alive)return "Select a living companion";
  if(Mode==Mode.Combat&&Active!=a)return "Wait for this character's turn";
  if(!Explored.Contains(goal.Cell))return "Explore closer to reveal this area";
  if(!CanStand(goal,a))return "Destination blocked";
  var path=Path(a,goal);if(path.Count==0)return a.Position.Distance(goal)<.025f?"Already here":"Path blocked";
  if(Mode==Mode.Combat&&PathLength(a.Position,path)>a.Move+.001f)return "Not enough movement remaining";
  return "";
 }
}
