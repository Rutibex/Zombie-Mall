namespace Ashen.Core;
public sealed partial class World
{
 int LowerDamage(Actor target,int amount,DamageType type,Actor? source,bool critical=false)
 {
  if(!LowerMallEnabled)return amount;var t=LowerTraits(target);if(t.Contains("layered")&&type is DamageType.Bludgeoning or DamageType.Piercing&&source?.Range<=1)amount=Math.Max(0,amount-3);if(t.Contains("ceramic")&&type==DamageType.Bludgeoning&&(source?.Die??0)<8)return 0;if(t.Contains("bloom")&&type==DamageType.Fire)amount*=2;return Exterior31Damage(target,Exterior30Damage(target,Roof29Damage(target,Roof28Damage(target,Upper27Damage(target,Upper26Damage(target,Upper25Damage(target,Lower24Damage(target,Lower23Damage(target,amount,type,source),type,source,critical),type,source),type,source,critical),type,source),type,source),type,critical,source),type),type);
 }
 void LowerDamageResolved(Actor target,int damage,DamageType type,Actor? source,bool wasAlive,bool critical=false)
 {
  if(!LowerMallEnabled)return;var t=LowerTraits(target);var s=LowerMonsterState(target);Lower23DamageResolved(target,damage,type,source,wasAlive);LowerDamageResolved24(target,damage,type,source,wasAlive);Upper25DamageResolved(target,damage,type,source,wasAlive,critical);Upper26DamageResolved(target,damage,type,source,wasAlive,critical);Upper27DamageResolved(target,damage,type,source,wasAlive,critical);Roof28DamageResolved(target,damage,type,source,wasAlive,critical);Roof29DamageResolved(target,damage,type,source,wasAlive,critical);Exterior30DamageResolved(target,damage,type,source,wasAlive);Exterior31DamageResolved(target,damage,type,source,wasAlive);
  if(type==DamageType.Fire&&s!=null&&!s.Events.Contains("burn:"+target.Id))s.Events.Add("burn:"+target.Id);
  if(wasAlive&&target.Hp<=0&&s!=null)
  {
   if(t.Contains("butcher")&&!s.Events.Contains("stand:"+target.Id)){s.Events.Add("stand:"+target.Id);if(SurvivorSave(target,Ability.Constitution,15,"Unrelenting Rage")){target.Hp=1;target.Character.SetCondition("unconscious","Unconscious",false);}}
   if(t.Contains("finish")&&type!=DamageType.Fire&&!(source?.AttackDamageType==DamageType.Slashing&&source.Range<=1)){target.Hp=1;target.Character.SetCondition("unconscious","Unconscious",false);Message(MessageCategory.Combat,"The Freezer Horror needs fire or a cutting finishing blow.");}
  }
  if(wasAlive&&target.Hp<=0&&s!=null&&!s.Events.Contains("burst:"+target.Id))
  {
   s.Events.Add("burst:"+target.Id);if(t.Contains("acid")||t.Contains("bile")||t.Contains("ceramic"))foreach(var a in Actors.Where(a=>a!=target&&a.Alive&&a.Position.Distance(target.Position)<=(t.Contains("acid")?1:2)&&Line(target.Position,a.Position)).ToArray()){bool save=SurvivorSave(a,Ability.Dexterity,13,"death burst");Hurt(a,SurvivorSaveDamage(a,RollDamage(t.Contains("bile")?2:1,t.Contains("ceramic")?8:6),Ability.Dexterity,save,t.Contains("acid")),t.Contains("ceramic")?DamageType.Piercing:DamageType.Acid,target);}
   if(t.Contains("maggots")){var d=CurrentLowerMallSite!;var e=Campaign!.Encounters.FirstOrDefault(e=>e.AreaId==d.AreaId&&e.ActorIds.Any(id=>Campaign.Actors.Single(a=>a.Id==id).Name=="Swarm of Maggots"));if(e!=null)SpawnLower(d,s,e.Id,1,"reaction");}
  }
  if(source is {Enemy:true}&&!target.Enemy&&damage>0&&target.Alive)
  {
   var st=LowerTraits(source);if(st.Contains("grapple")&&!SurvivorSave(target,Ability.Strength,14,"grapple")){LowerEffect(target,"restrained",0);LowerEffect(target,"book-grappled",0);}if(st.Contains("infection")||st.Contains("scratch")){bool pass=SurvivorSave(target,Ability.Constitution,12,"infection");if(st.Contains("scratch"))pass&=SurvivorSave(target,Ability.Constitution,12,"infected scratch");if(!pass)LowerEffect(target,"book-infected",86400);}if(st.Contains("stun")&&!SurvivorSave(target,Ability.Constitution,12,"taser"))LowerEffect(target,"stunned",6);if(st.Contains("cold"))Hurt(target,Roll(6),DamageType.Cold);if(st.Contains("lifesteal"))source.Hp=Math.Min(source.MaxHp,source.Hp+Roll(8));if(st.Contains("charge"))target.Character.SetCondition("prone","Prone",true);
  }
  if(t.Contains("spores")&&damage>0)foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=1))if(!SurvivorSave(p,Ability.Constitution,13,"poison spores"))LowerEffect(p,"book-disoriented",3600);
  if(t.Contains("ceramic")&&target.Hp is >0 and <=5&&s!=null&&!s.Events.Contains("ceramic:"+target.Id)){s.Events.Add("ceramic:"+target.Id);foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=2).ToArray())Hurt(p,Roll(8),DamageType.Piercing,target);}
 }
 void LowerTurn(Actor actor)
 {
  Exterior30PartyTurn(actor);Roof29PartyTurn(actor);Roof28PartyTurn(actor);Upper27PartyTurn(actor);Upper26PartyTurn(actor);LowerPartyTurn24(actor);LowerPartyTurn23(actor);var s=LowerMonsterState(actor);if(s==null)return;var t=LowerTraits(actor);LowerTurn23(actor,s,t);LowerTurn24(actor,s,t);Upper25Turn(actor,s,t);Upper26Turn(actor,s,t);Upper27Turn(actor,s,t);Roof28Turn(actor,s,t);Roof29Turn(actor,s,t);Exterior30Turn(actor,s,t);Exterior31Turn(actor,s,t);if(actor.Name=="The Speed Freak"&&actor.Equipment.Weapon?.Definition.MagazineSize>0&&actor.Equipment.Weapon.LoadedAmmunition==null){var iron=actor.Inventory.Items.FirstOrDefault(i=>i.ItemId=="item.lower.14.tireiron");if(iron!=null){actor.Inventory.Items.Remove(iron);actor.Inventory.Add(actor.Equipment.Weapon);actor.Equipment.Weapon=iron;}}bool burned=s.Events.Remove("burn:"+actor.Id);if(t.Contains("regenerate")&&!burned&&actor.Alive)actor.Hp=Math.Min(actor.MaxHp,actor.Hp+(t.Contains("bloom")?5:Roll(4)));if(t.Contains("flurry"))actor.Economy.Reset(actor.EffectiveSpeedFeet,2);if(t.Contains("jitter"))actor.Economy.AddMovement(actor.EffectiveSpeedFeet);if(t.Contains("rooted")||t.Contains("mannequin")&&Party.Any(p=>p.Alive&&Line(p.Position,actor.Position)))actor.Economy.MovementRemainingFeet=0;
  if(t.Contains("sonic")){ChangeTension(actor.Name=="The Scribbler"?RollDamage(2,6):Roll(4),"Amplified undead sound");if(Roll(6)>=5)foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=3))if(!SurvivorSave(p,Ability.Constitution,14,"sonic burst"))LowerEffect(p,"book-deafened",Roll(4)*6);}
  if((t.Contains("psychic")||t.Contains("fear"))&&!s.Events.Contains("fear:"+actor.Id)){s.Events.Add("fear:"+actor.Id);foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=4).ToArray())if(!SurvivorSave(p,Ability.Wisdom,actor.Name=="The Optometrist"?14:12,"mental fear")){if(t.Contains("psychic"))Hurt(p,Roll(4),DamageType.Psychic,actor);LowerEffect(p,"book-disoriented",6);}}
 }
 void LowerGunshot(Actor actor)
 {
  var d=CurrentLowerMallSite;var s=d==null?null:LowerMallProgress(d.Number);if(d?.Level=="exterior")return;if(d?.Level=="roof"){Roof28Gunshot(actor);Roof29Gunshot(actor);return;}if(d?.Level=="upper"){Upper25Gunshot(actor);Upper26Gunshot(actor);Upper27Gunshot(actor);return;}if(d?.Number>=37){IgniteLower24(actor,true);return;}if(d?.Number==34)return;if(s==null||!s.Events.Contains("gas-active")||s.Events.Contains("gas-exploded"))return;if(d!.Number==4&&Roll(2)!=1)return;s.Events.Add("gas-exploded");foreach(var a in Actors.Where(a=>a.Alive&&a.Position.Distance(actor.Position)<=(d.Number==5?3:2)).ToArray())Hurt(a,RollDamage(d.Number==9?4:3,6),DamageType.Fire);ChangeTension(RollDamage(2,6),"Shop gas explosion");
 }
 public bool EscapeLowerMallGrapple(Actor actor)
 {
  if(!actor.Alive||!Party.Contains(actor)||!actor.Character.HasCondition("book-grappled")||Mode==Mode.Combat&&!CanAct(actor))return false;if(Mode==Mode.Combat)SpendAction(actor,"Escape grapple");if(SurvivorSkillCheck(actor,Skill.Athletics,14,"Escape grapple")){actor.Character.SetCondition("restrained","Restrained",false);actor.Character.SetCondition("book-grappled","Grappled",false);LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition is "restrained" or "book-grappled");}return true;
 }
}
