using System.Text.Json.Serialization;
namespace Ashen.Core;

public sealed record AttackRollModifiers(bool Advantage,bool Disadvantage);
public sealed partial class World
{
 [JsonIgnore] public bool ModernTacticalCombat=>Campaign?.TacticalCombat==true;
 // Cover furniture blocks feet, but not sight or projectiles. Walls, closed doors and tall furniture still stop both.
 public bool SightBlocked(Cell cell)
 {
  if(!ModernTacticalCombat)return Blocked(cell);
  return EquipmentSmoke(cell)||!IsFloor(cell)||Features.Any(f=>f.Kind=="door"&&!f.Used&&f.Pos==cell)||CampaignSightPropBlocks(cell);
 }
 public bool WeaponLine(Actor attacker,Actor target,WorldPoint? fromPosition=null)
 {
  var from=fromPosition??attacker.Position;var to=target.Position;
  if(!ModernTacticalCombat||attacker.Range>1)return Line(from,to);
  return PhysicalAttackLine(from,to);
 }
 bool PhysicalAttackLine(WorldPoint from,WorldPoint to)
 {
  int steps=Math.Max(1,(int)MathF.Ceiling(from.Distance(to)/.06f));
  for(int i=0;i<=steps;i++)if(Blocked((from+(to-from)*(i/(float)steps)).Cell))return false;
  return true;
 }
 public bool Threatened(Actor a,WorldPoint? fromPosition=null)=>Actors.Any(e=>e!=a&&e.Alive&&!e.Unconscious&&e.Enemy!=a.Enemy&&e.Range<=1&&e.Position.Distance(fromPosition??a.Position)<=1.25f&&PhysicalAttackLine(fromPosition??a.Position,e.Position)&&(Mode!=Mode.Combat||Order.Contains(e.Id)));
 public int CoverAgainst(Actor attacker,Actor target,WorldPoint? fromPosition=null)
 {
  if(!ModernTacticalCombat||attacker.Range<=1||!Actors.Contains(attacker)||!Actors.Contains(target))return 0;
  var from=fromPosition??attacker.Position;var to=target.Position;int cover=0,steps=Math.Max(1,(int)MathF.Ceiling(from.Distance(to)/.06f));
  // Only furniture close to the defender, on the incoming line, gives cover. Shooting over one's own desk does not protect a distant enemy.
  var props=CurrentCampaignArea!.Props.Where(p=>p.CoverBonus>0).ToArray();
  for(int i=1;i<steps;i++)
  {
   var point=from+(to-from)*(i/(float)steps);if(point.Distance(to)>2||point.Distance(to)>=point.Distance(from))continue;
   foreach(var prop in props)if(prop.Cells().Contains(point.Cell))cover=Math.Max(cover,prop.CoverBonus);
  }
  return cover;
 }
 public AttackRollModifiers AttackModifiers(Actor attacker,Actor target,WorldPoint? fromPosition=null)
 {
  bool advantage=Upper27Advantage(attacker,target)||attacker.Character.HasCondition("chapter-berserk")||EquipmentBlind(target)||attacker.Character.HasCondition("hidden")||attacker.Character.HasCondition("helped")||target.Character.HasCondition("guided")||target.Character.HasCondition("faerie-fire");
  bool disadvantage=LowerTraits(target).Contains("ext31-cop")&&attacker.Range>1||Roof28Disadvantage(attacker,target)||Upper27Disadvantage(attacker,target)||LowerTraits(target).Contains("gallery-glitcher")&&!attacker.Character.HasCondition("upper26-observed")||LowerTraits(target).Contains("gallery-small")&&attacker.Range>1||LowerTraits(target).Contains("final-small")&&!attacker.Character.HasCondition("prone")||attacker.Character.HasCondition("chapter-looking-away")||attacker.Character.HasCondition("book-disoriented")||attacker.Character.HasCondition("chapter-sick")||attacker.Character.HasCondition("book-grappled")||EquipmentBlind(attacker)||target.Character.HasCondition("dodging")||attacker.Character.HasCondition("vicious-mockery");
  if(LowerTraits(attacker).Contains("ink")&&Round%2==0)advantage=true;
  if(ModernTacticalCombat)
  {
   if(attacker.Range>1&&Threatened(attacker,fromPosition))disadvantage=true;
   if(attacker.Character.HasCondition("prone")&&!LowerTraits(target).Contains("final-small"))disadvantage=true;
   if(target.Character.HasCondition("prone")){if((fromPosition??attacker.Position).Distance(target.Position)<=1.25f)advantage=true;else disadvantage=true;}
  }
  if(target.ClassIs("wise",5)&&!target.Character.HasCondition("class-evasion-used")&&Visible.Contains(attacker.Pos)&&Line(target.Position,fromPosition??attacker.Position))disadvantage=true;
  return new(advantage,disadvantage);
 }
 public string DescribeAttackContext(Actor attacker,Actor target,WorldPoint? fromPosition=null)
 {
  int cover=CoverAgainst(attacker,target,fromPosition);var modifiers=AttackModifiers(attacker,target,fromPosition);
  string roll=modifiers.Advantage==modifiers.Disadvantage?"normal roll":modifiers.Advantage?"advantage":"disadvantage";
  string threat=attacker.Range>1&&Threatened(attacker,fromPosition)?"; hostile in close reach":"";
  return $"{attacker.Equipment.Weapon?.Definition.Name??attacker.Character.MainAttack.Name}: {attacker.AttackBonus+SurvivorAttackBonus(attacker,target,fromPosition):+0;-0;0} to hit; {attacker.DamageDice}d{attacker.Die}{attacker.DamageBonus:+0;-0;+0} {attacker.AttackDamageType.ToString().ToLowerInvariant()}; {roll}{threat}; cover +{cover} AC (effective AC {target.Ac+cover}).";
 }
}
