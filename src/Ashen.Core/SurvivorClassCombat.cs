namespace Ashen.Core;
public sealed partial class World
{
 public int SurvivorAttackBonus(Actor a,Actor target,WorldPoint? from=null)
 {
  if(!SurvivorClassesEnabled)return 0;int bonus=(LowerTraits(a).Contains("final-eyeless")?target.EquipmentArmorBonus:0)+(a.Character.HasCondition("chapter-scan")&&!LowerTraits(target).Contains("human")?1:0)+(LowerTraits(a).Count>0&&Actors.Any(e=>e.Alive&&LowerTraits(e).Contains("coach")&&e.Position.Distance(a.Position)<=6)?1:0);bonus+=a.Equipment.Weapon?.SurvivorAccuracy==true?1:0;
  if(CurrentLowerMallSite?.Number==224&&a.Range>1&&!ShooterHasCover(a,target,from))bonus-=2;
  if(a.ClassIs("agile",3)&&a.Training.Path=="shadow-sniper"&&a.Range>1&&(a.Character.HasCondition("hidden")||ShooterHasCover(a,target,from)))bonus+=2;
  return bonus;
 }
 int SurvivorExtraDamage(Actor a,Actor target,bool critical,bool wasHidden)
 {
  int extra=0;if(!SurvivorClassesEnabled)return extra;
  if(a.ClassIs("strong")&&a.Range<=1&&Mode==Mode.Combat&&Active==a&&!a.Character.HasCondition("class-power-used")&&a.Economy.MovementMaximumFeet-a.Economy.MovementRemainingFeet<.01f)
  {extra+=Roll(a.Die);a.Character.SetCondition("class-power-used","Power Strike used",true);}
  if(a.ClassIs("strong",5)&&a.Range<=1&&critical)extra+=Roll(a.Die);
  if(wasHidden&&target.Awareness.State!=AwarenessKind.Alerted&&(a.ClassIs("agile",6)||a.ClassIs("agile",3)&&a.Training.Path=="shadow-sniper"))extra+=Roll(6);
  return extra;
 }
 int SurvivorReduceDamage(Actor a,int amount,DamageType type,Actor? source,bool critical)
 {
  if(!a.UsesSurvivorClasses)return amount;int n=Math.Max(0,amount);
  if(a.ClassIs("tough",2))n=critical?n/2:Math.Max(0,n-(a.TotalLevel>=10?6:a.TotalLevel>=6?4:2));
  bool physical=type is DamageType.Bludgeoning or DamageType.Piercing or DamageType.Slashing;
  bool resistance=a.ClassIs("tough")&&type==DamageType.Poison||a.ClassIs("tough",10)&&type==DamageType.Bludgeoning||a.ClassIs("strong",10)&&physical||a.ClassIs("strong",3)&&a.Training.Path=="juggernaut"&&a.Hp*2<=a.MaxHp&&physical||a.ClassIs("smart",6)&&source==null&&type is DamageType.Fire or DamageType.Poison or DamageType.Lightning;
  if(resistance&&!a.Character.Defenses.Resistances.Contains(type))n/=2;
  if(a.ClassIs("strong",3)&&a.Training.Path=="juggernaut"&&a.Character.HasCondition("dodging"))n=Math.Max(0,n-3);
  if(a.ClassIs("strong",10)&&a.Training.Path=="juggernaut"&&Mode==Mode.Combat&&n>0&&CanReact(a)&&SurvivorResource(a,"halve-hit")?.Current>0){SpendReaction(a,"Unstoppable Force");SpendSurvivorResource(a,"halve-hit");n/=2;}
  return n;
 }
 void SurvivorLastStand(Actor a,int damage)
 {
  if(a.Hp>0||a.Dead||!a.ClassIs("tough",3)&&!a.ClassIs("strong",7)||!SpendSurvivorResource(a,"last-stand"))return;
  int dc=10+(a.ClassIs("strong",7)?damage/2:damage);if(SurvivorSave(a,Ability.Constitution,dc,"last-stand")){a.Hp=1;a.Character.SetCondition("unconscious","Unconscious",false);}
 }
 public bool SurvivorSave(Actor a,Ability ability,int dc,string context="")
 {
  bool fear=context.Contains("fear"),mental=fear||context.Contains("mental"),poison=context.Contains("poison")||context.Contains("disease")||context.Contains("infection"),fatigue=context.Contains("exhaustion");
  if(context.Contains("infection")&&a.Character.HasCondition("final-antibiotic"))return true;
  var leaders=Party.Where(p=>p.Alive&&p.ClassIs("charismatic",5)&&p.Position.Distance(a.Position)<= (p.TotalLevel>=10?4:2)&&Line(p.Position,a.Position)).ToArray();
  if(fear&&leaders.Any(p=>p.TotalLevel>=10))return true;
  bool vitamin=poison&&a.Character.HasCondition("upper-vitamin");bool training=(a.Character.HasCondition("upper26-constitution")&&ability==Ability.Constitution)||a.Character.HasCondition("final-trained")&&ability is Ability.Strength or Ability.Constitution;bool advantage=a.Character.HasCondition("ext31-adrenaline")&&ability is Ability.Strength or Ability.Dexterity||vitamin||fatigue&&a.HasUpper25Gear("Compression Clothing")||training||(fatigue&&a.Character.HasCondition("chapter-stimulant"))||a.ClassIs("tough")&&poison||a.ClassIs("tough",6)&&(mental||fatigue)||a.ClassIs("wise",8)&&mental||fear&&leaders.Length>0||a.ClassIs("strong",7)&&a.Training.Path=="juggernaut"&&ability==Ability.Constitution;
  if(vitamin){a.Character.SetCondition("upper-vitamin","Vitamins",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="upper-vitamin");}
  int roll=Roll(20);bool disadvantage=a.Character.HasCondition("chapter-sick")||ability==Ability.Dexterity&&(a.Character.HasCondition("roof-dex-shock")||a.Character.HasCondition("roof29-jitters"));if(advantage!=disadvantage)roll=advantage?Math.Max(roll,Roll(20)):Math.Min(roll,Roll(20));int bonus=a.SaveModifier(ability)+(ability==Ability.Constitution&&(poison||context.Contains("hazard"))&&a.WearsUpper25Gear("Work Boots")?1:0)+(context.Contains("infection")&&a.Character.HasCondition("final-herbal-protected")?2:0)+(ability==Ability.Wisdom&&a.Character.HasCondition("chapter-brew")?1:0);
  if(roll+bonus<dc&&a.ClassIs("tough",10)&&SpendSurvivorResource(a,"save-reroll"))roll=Roll(20);
  if(training){a.Character.SetCondition("upper26-constitution","Prepared Constitution",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="upper26-constitution");a.Character.SetCondition("final-trained","Training",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="final-trained");}bool success=roll+bonus>=dc;LogSavingThrow(a,ability,roll,bonus,dc,success);return success;
 }
 public int SurvivorSaveDamage(Actor a,int amount,Ability ability,bool success,bool halfOnSave)
 {
  if(a.ClassIs("agile",5)&&ability==Ability.Dexterity&&halfOnSave)return success?0:amount/2;
  return success?(halfOnSave?amount/2:0):amount;
 }
 public bool SurvivorSkillAdvantage(Actor a,Skill? skill,Ability? ability=null)
 {
  if(a.Character.HasCondition("final-trained")&&skill.HasValue&&SkillRules.AbilityFor(skill.Value) is Ability.Strength or Ability.Constitution)return true;
  if(a.Character.HasCondition("final-lucky")||a.Character.HasCondition("upper-morale")||skill==Skill.Stealth&&a.Character.HasCondition("upper-concealer"))return true;
  if(skill==Skill.Perception&&a.Character.HasCondition("chapter-lastorder"))return true;
  if(skill==Skill.Athletics&&a.HasLowerGear("boxing wraps"))return true;
  if(skill==Skill.Stealth&&a.Character.HasCondition("book-stealth"))return true;
  if(!a.UsesSurvivorClasses)return false;var stat=ability??(skill.HasValue?SkillRules.AbilityFor(skill.Value):Ability.Strength);
  return a.ClassIs("charismatic")&&stat==Ability.Charisma||a.ClassIs("strong")&&stat==Ability.Strength||a.ClassIs("wise")&&skill is Skill.Perception or Skill.Insight||a.ClassIs("agile")&&skill==Skill.Stealth||a.ClassIs("smart")&&skill==Skill.Investigation||a.ClassIs("smart",5)&&skill is Skill.Technology or Skill.Mechanics;
 }
 public bool SurvivorSkillCheck(Actor a,Skill skill,int dc,string context="")
 {
  if(!Actors.Contains(a)||a.Enemy||!a.Alive||dc is <1 or >40)return false;
  if(a.ClassIs("wise",7)&&skill==Skill.Survival&&context is "food" or "water" or "shelter"){LogAbilityCheck(a,skill,20,a.SkillModifier(skill),dc,true);return true;}
  bool advantage=SurvivorSkillAdvantage(a,skill),disadvantage=a.Character.HasCondition("book-disoriented")||a.Character.HasCondition("chapter-sick")||skill==Skill.Stealth&&(a.Character.HasCondition("final-cough")||a.Character.HasCondition("upper-glass"))||skill==Skill.Perception&&a.Character.HasCondition("book-deafened")||skill==Skill.Stealth&&(a.HasLowerGear("roller skates")||a.Equipment.Armor?.ItemId is "item.lower.19.loot.5.0" or "item.lower.21.loot.2.0" or "item.lower.36.loot.1.0");int roll=Roll(20);if(advantage!=disadvantage)roll=advantage?Math.Max(roll,Roll(20)):Math.Min(roll,Roll(20));int bonus=a.SkillModifier(skill);if(a.Character.HasCondition("final-trained")&&SkillRules.AbilityFor(skill) is Ability.Strength or Ability.Constitution){a.Character.SetCondition("final-trained","Training",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="final-trained");}if(a.Character.HasCondition("final-lucky")){a.Character.SetCondition("final-lucky","Lucky",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="final-lucky");}
  if(roll+bonus<dc&&a.ClassIs("charismatic")&&SkillRules.AbilityFor(skill)==Ability.Charisma&&SpendSurvivorResource(a,"social-reroll"))roll=Roll(20);
  if(roll+bonus<dc&&a.ClassIs("wise",2)&&skill is Skill.Survival or Skill.Investigation&&SpendSurvivorResource(a,"scout-reroll"))roll=Roll(20);
  if(a.Character.HasCondition("upper-morale")){a.Character.SetCondition("upper-morale","Morale",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="upper-morale");}
  if(SkillRules.AbilityFor(skill)==Ability.Charisma&&a.Character.HasCondition("upper26-confidence")){a.Character.SetCondition("upper26-confidence","Confidence",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="upper26-confidence");}
  bool result=roll+bonus>=dc;LogAbilityCheck(a,skill,roll,bonus,dc,result);return result;
 }
 public int SurvivorThreatCount(Actor a)=>a.ClassIs("wise",10)?Actors.Count(e=>e.Enemy&&e.Alive&&e.Position.Distance(a.Position)<=20):0;
}
public sealed partial class World
{
 public bool ResolveCampaignHazard(Actor actor,int damage,DamageType type,Ability? save=null,int dc=12,string context="",bool critical=false)
 {
  if(!SurvivorClassesEnabled||!Actors.Contains(actor)||actor.Dead||damage is <0 or >1000||dc is <1 or >40||!Enum.IsDefined(type)||save.HasValue&&!Enum.IsDefined(save.Value))return false;
  int amount=damage;if(save.HasValue)amount=SurvivorSaveDamage(actor,amount,save.Value,SurvivorSave(actor,save.Value,dc,context),true);
  if(actor.ClassIs("agile",2)&&context=="fall")amount/=2;Hurt(actor,amount,type,null,critical);return true;
 }
 bool ShooterHasCover(Actor a,Actor target,WorldPoint? from=null)
 {
  if(!ModernTacticalCombat)return false;var start=from??a.Position;float length=start.Distance(target.Position);if(length<.01f)return false;
  var end=start+(target.Position-start)*(Math.Min(2,length)/length);int steps=Math.Max(1,(int)(start.Distance(end)/.06f));
  for(int i=1;i<steps;i++){var cell=(start+(end-start)*(i/(float)steps)).Cell;if(CurrentCampaignArea!.Props.Any(p=>p.CoverBonus>0&&p.Cells().Contains(cell)))return true;}return false;
 }
 (int Min,int Max,int Crit) SurvivorDamagePreview(Actor a,Actor target)
 {
  int min=0,max=0,crit=0;if(!a.UsesSurvivorClasses)return(0,0,0);
  bool power=a.ClassIs("strong")&&a.Range<=1&&Mode==Mode.Combat&&Active==a&&!a.Character.HasCondition("class-power-used")&&a.Economy.MovementMaximumFeet-a.Economy.MovementRemainingFeet<.01f;
  if(power){min++;max+=a.Die;}
  if(a.ClassIs("strong",5)&&a.Range<=1)crit+=a.Die;
  if(a.Character.HasCondition("hidden")&&target.Awareness.State!=AwarenessKind.Alerted&&(a.ClassIs("agile",6)||a.ClassIs("agile",3)&&a.Training.Path=="shadow-sniper")){min++;max+=6;}
  return(min,max,crit);
 }
}
