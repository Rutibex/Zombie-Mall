namespace Ashen.Core;
public sealed partial class Actor
{
 int LowerGearSkill(Skill skill)=>Exterior30Skill(skill)+Roof29GearSkill(skill)+Roof28GearSkill(skill)+Upper27GearSkill(skill)+Upper26GearSkill(skill)+Upper25GearSkill(skill)+(SkillRules.AbilityFor(skill)==Ability.Dexterity?OpticalDexBonus:0)+(skill==Skill.Medicine&&Character.HasCondition("final-herbal-study")?1:0)+(skill==Skill.Mechanics&&HasLowerGear("optometrist toolkit")?1:0)+(skill==Skill.Survival&&Character.HasCondition("final-prepper-study")?1:0)+(SkillRules.AbilityFor(skill)==Ability.Strength&&Character.HasCondition("final-preworkout")?1:0)+(skill==Skill.Intimidation&&HasLowerGear("big bite serrated knife")?1:0)-(skill==Skill.Perception&&Character.HasCondition("final-mold")?1:0)+(skill==Skill.Mechanics&&HasLowerGear("multi-tool")?2:0)+(skill==Skill.SleightOfHand&&HasLowerGear("nail file shiv")?2:0)+(skill==Skill.Survival&&HasLowerGear("hunting knife")?1:0)+(skill==Skill.Intimidation&&Equipment.Armor?.ItemId=="item.lower.21.loot.1.0"?1:0)-(skill==Skill.Perception&&Character.HasCondition("chapter-perfume-cloud")?2:0)-(skill==Skill.Perception&&Character.HasCondition("chapter-paranoia")?2:0)-(SkillRules.AbilityFor(skill)==Ability.Intelligence&&Character.HasCondition("chapter-brew")?1:0);
}
public sealed partial class World
{
 static bool ChapterCombatAction(LowerMallAction a)=>a.Effect is "ext31-ride" or "ext30-fryer" or "ext30-patrol" or "roof29-food" or "roof29-release" or "roof-drone" or "roof-negotiate" or "roof-talk" or "roof-light" or "roof-gate" or "upper27-predict" or "upper27-reveal" or "upper27-tame" or "upper27-light" or "upper27-fire" or "upper26-observe" or "upper26-tribute" or "upper26-negotiate" or "upper26-escape" or "upper26-oil"||a.Effect is "upper-shiny" or "upper-hack" or "upper-negotiate" or "upper-foam"||a.Effect is "final-meat" or "final-scald" or "final-defib" or "final-negotiate" or "final-decapitate" or "final-glass" or "final-mirror" or "final-book"||a.Effect is "chapter-mask" or "chapter-hatchet" or "chapter-mirror-break" or "chapter-mirror-turn" or "chapter-speakers-break" or "chapter-shelve" or "chapter-scan" or "chapter-bluff" or "chapter-convert" or "chapter-deal";
 void EnterLowerMall23(LowerMallSite d,LowerMallState s)
 {
  if((d.Number<19||d.Number>36))return;s.BonusRoll=Roll(d.Number is 25 or 32?6:4);s.CarLimit=d.Number==34?Roll(4):0;s.NextEventMinute=CampaignMinutes+(d.Number==24?10:60);
  if(d.Number==25){var choices=d.Actions.Where(a=>a.Effect=="chapter-freezer-result").ToArray();var chosen=choices[s.BonusRoll<=2?0:s.BonusRoll-2];d.BackEncounterId=chosen.EncounterId;d.Actions.Single(a=>a.Id.EndsWith(".back")).EncounterId=chosen.EncounterId;}
  if(d.Number==22&&s.EncounterRoll==6)foreach(var p in Party)if(!SurvivorSave(p,Ability.Wisdom,14,"mental pantry ghost"))LowerEffect(p,"chapter-paranoia",3600);
  if(d.Number==34)s.Events.Add("gas-active");if(d.Number==22)s.Events.Add("toxic-active");
  if(d.Number==19){var f=Features.Single(f=>f.Id=="feature.lower.19.loot.4");var stack=f.Contents.Items.Single();int count=Roll(6);stack.Quantity=count;stack.ResourceUnits=stack.ResourceUnits.Take(count).ToList();}
  if(d.Number==30){var f=Features.Single(f=>f.Id=="feature.lower.30.loot.4");f.Contents.Items.RemoveAll(i=>i.ItemId!=$"item.lower.30.loot.4.{s.BonusRoll-1}");if(s.BonusRoll==2){var stack=f.Contents.Items.Single();stack.Quantity=1;stack.CampaignGear=ResolveItemDefinition(stack.ItemId);s.Events.Add("safe-bullets:"+RollDamage(3,6));}}
  if(d.Number==24)ChapterEvent(PartyLeader,d,s,"entry");
  if(d.Number==31){foreach(var a in Actors.Where(a=>a.Enemy))LowerEffect(a,"chapter-asleep",0);}
 }
 void ConfigureLower23Actors(IEnumerable<string> ids)
 {
  foreach(var id in ids){var a=Actors.Single(a=>a.Id==id);var t=LowerTraits(a);if(t.Contains("camper")){a.Equipment.Weapon=CreateItemStack("item.lower.32.camper-rifle");a.Equipment.Weapon.LoadedAmmunition=CreateItemStack("item.lower.native-rifle",5);a.Inventory.Add(CreateItemStack(Campaign!.Items.First(i=>i.SurvivalUse=="food").Id,3));}if(t.Contains("two-dice"))a.Character.MainAttack.DamageDice=2;if(t.Contains("reflection")){var p=PartyLeader;a.MaxHp=p.MaxHp;a.Hp=p.Hp;a.BaseAc=p.Ac;a.Character.MainAttack.DamageDice=p.DamageDice;a.Character.MainAttack.DamageDieSize=p.Die;a.Character.MainAttack.DamageType=p.AttackDamageType;a.Character.MainAttack.AttackBonusOverride=p.AttackBonus;a.Character.MainAttack.DamageBonusOverride=p.DamageBonus;a.Character.MainAttack.RangeFeet=p.Range*5;}
   if(t.Contains("dormant")||t.Contains("silent"))LowerEffect(a,"chapter-asleep",0);}
 }
 string Lower23Reason(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if((d.Number<19||d.Number>36))return "";
  if(a.Effect=="chapter-deal"&&LowerCleared(s,d.BackEncounterId)&&!s.Events.Contains("peaceful:"+d.BackEncounterId))return "The hoarder is dead; a nonviolent deal is no longer possible.";
  if(a.Effect is "chapter-hush"&&s.Events.Contains("hush-day:"+(CampaignMinutes/1440)))return "The Baton of Silence has been used today.";
  if(a.Effect=="chapter-sedan"&&(s.CarAttempts>=s.CarLimit||s.Events.Contains("sedan-broken")))return "The source's finite jumpstart attempts are exhausted.";
  if(a.Effect is "chapter-greenhouse"&&(!s.Secured||Actors.Any(e=>e.Enemy&&e.Alive)))return "Secure the greenhouse before establishing food production.";
  if(a.Effect=="chapter-radio30"&&s.BonusRoll!=4)return "This journey's safe did not contain the military radio.";
  if(a.Effect=="chapter-speakers-break"&&!Actors.Any(e=>LowerTraits(e).Contains("conductor")))return "Play the recovered ECHO USB before disrupting the Conductor’s speakers.";
  if(a.Effect=="chapter-hatchet"&&!Actors.Any(e=>LowerTraits(e).Contains("mountain")))return "Investigate the Mountain Man first.";
  if(a.Effect is "chapter-mask" or "chapter-mirror-turn"&&!s.Releases.Any(r=>r.EncounterId==d.BackEncounterId))return "Investigate the back room first.";
  if(a.Effect=="chapter-scan"&&actor.Character.HasCondition("chapter-emp"))return "Powered gear is disabled by the EMP.";
  if(a.Effect=="chapter-shelve"&&!Actors.Any(e=>e.Enemy&&e.Alive&&e.Position.Distance(actor.Position)<=2))return "Bring a hostile within reach of the shelving.";
  if(a.Effect=="chapter-survivor"&&s.EncounterRoll!=6)return "No survivor was found on this saved encounter branch.";
  if(a.Effect=="chapter-calm"&&s.EncounterRoll!=5)return "No horrified survivor was found on this saved encounter branch.";
  if(a.Effect=="chapter-scavenger"&&!s.Events.Contains("return-hidden"))return "The hidden scavenger has not appeared in a return event.";
  return "";
 }
 void Lower23Failure(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if((d.Number<19||d.Number>36))return;s.Quiet=false;
  if(a.Effect=="chapter-sedan"){s.CarAttempts++;if(s.CarAttempts>=s.CarLimit){s.Events.Add("sedan-broken");Hurt(actor,RollDamage(2,6),DamageType.Lightning);IgniteLowerGarage(actor);}}
 }
 void ConsumeChapterItem(Actor actor,LowerMallAction a)
 {
  if(a.CostItemId=="")return;var item=actor.Inventory.Items.FirstOrDefault(i=>i.ItemId==a.CostItemId);if(item==null)return;item.ConsumeResourceUnits(1);if(--item.Quantity==0)actor.Inventory.Items.Remove(item);
 }
 void PacifyChapter(LowerMallSite d,LowerMallState s,string trait)
 {
  var enc=Campaign!.Encounters.FirstOrDefault(e=>e.AreaId==d.AreaId&&e.ActorIds.Any(id=>Campaign.LowerMall!.Monsters.Any(m=>m.ActorId==id&&m.Traits.Contains(trait))));if(enc==null)return;SpawnLower(d,s,enc.Id,1,"reaction");foreach(var a in Actors.Where(a=>enc.ActorIds.Contains(a.Id))){a.Hp=0;a.Character.SetCondition("unconscious","Peacefully withdrew",true);}s.Events.Add("peaceful:"+enc.Id);CheckEnd();
 }
 void SpawnChapterNamed(LowerMallSite d,LowerMallState s,string name,int count=1,string trigger="reaction")
 {
  var e=Campaign!.Encounters.FirstOrDefault(e=>e.AreaId==d.AreaId&&e.ActorIds.Any(id=>Campaign.Actors.Single(a=>a.Id==id).Role==name));if(e!=null)SpawnLower(d,s,e.Id,count,trigger);
 }
 void LowerDevice23(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if((d.Number<19||d.Number>36))return;string e=a.Effect;
  switch(e)
  {
   case "chapter-mask":s.Events.Add("mask-broken");ChangeTension(Roll(6),"Screaming empty mask");break;
   case "chapter-hatchet":s.Events.Add("hatchet-removed");break;
   case "chapter-speakers-break":s.Events.Add("speakers-broken");foreach(var target in Actors.Where(e=>e.Alive&&LowerTraits(e).Contains("conductor")).ToArray())Hurt(target,999,DamageType.Thunder,actor);ChangeTension(RollDamage(2,6),"Breaking the Conductor's speakers");break;
   case "chapter-mirror-turn":LowerEffect(actor,"chapter-looking-away",0);foreach(var target in Actors.Where(e=>e.Alive&&LowerTraits(e).Contains("reflection")))actor.FacingLeft=target.Position.X>actor.Position.X;break;
   case "chapter-mirror-break":s.Events.Add("mirror-broken");SpawnChapterNamed(d,s,"Cursed Mirror Horror");foreach(var target in Actors.Where(e=>e.Alive&&LowerTraits(e).Contains("reflection")).ToArray())Hurt(target,999,DamageType.Psychic,actor);CreditLower(s,a.Id,0);ChangeTension(Roll(6),"Shattered wall mirror");break;
   case "chapter-shelve":foreach(var target in Actors.Where(e=>e.Enemy&&e.Alive&&e.Position.Distance(actor.Position)<=2).Take(1).ToArray())Hurt(target,Roll(6),DamageType.Bludgeoning,actor);ChangeTension(Roll(6),"Shelving shove");break;
   case "chapter-deal":PacifyChapter(d,s,"hoarder");break;
   case "chapter-convert":PacifyChapter(d,s,"cultist");break;
   case "chapter-bluff":foreach(var target in Actors.Where(e=>e.Enemy&&e.Alive&&e.Position.Distance(actor.Position)<=6))LowerEffect(target,"stunned",6);break;
   case "chapter-dressing":ChangeTension(RollDamage(2,6),"Barricaded dressing room");break;
   case "chapter-dressing-branch":s.Events.Add("dressing:"+s.BonusRoll);if(s.BonusRoll==1){LowerRecruit(a,s);if(CommunityMembers.FirstOrDefault(p=>p.Id==a.NpcActorId) is {} infected)LowerEffect(infected,"book-infected",86400);}else if(s.BonusRoll==3)SpawnChapterNamed(d,s,"Dressing Crawler");break;
   case "chapter-vines":s.Events.Remove("vines-blocked");foreach(var exit in Features.Where(f=>f.Kind=="transition"))exit.Locked=false;break;
   case "chapter-training":LowerEffect(actor,"chapter-morale",3600);break;
   case "chapter-vhs":foreach(var p in Party.Where(p=>p.Alive))if(!SurvivorSave(p,Ability.Wisdom,14,"mental VHS"))p.Needs.Exhaustion=Math.Min(6,p.Needs.Exhaustion+1);else LowerEffect(p,"chapter-watched",86400);break;
   case "chapter-ventilate":s.Events.Remove("toxic-active");s.Events.Add("toxic-cleared");break;
   case "chapter-brew":ConsumeChapterItem(actor,a);LowerEffect(actor,"chapter-brew",3600);break;
   case "chapter-nest":SpawnChapterNamed(d,s,"Suitcase Stalker",Roll(3)+3);ChangeTension(RollDamage(2,6),"Disturbed luggage nest");s.Quiet=false;break;
   case "chapter-drone-repair":case "chapter-radio-repair":case "chapter-radio30":case "chapter-medicine":ConsumeChapterItem(actor,a);break;
   case "chapter-power":ChangeTension(RollDamage(2,6),"Gadget Galaxy power restored");break;
   case "chapter-tv":case "chapter-broadcast":EmitLocalNoise(actor,"door",120);ChangeTension(RollDamage(2,6),"Broadcast lure");s.Quiet=false;break;
   case "chapter-scan":LowerEffect(actor,"chapter-scan",6);break;
   case "chapter-slicer":s.Events.Add("slicer-isolated");s.HazardResolved=true;break;
   case "chapter-freezer-close":s.Events.Add("freezer-closed");foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=false;break;
   case "chapter-blackcup":if(!SurvivorSave(actor,Ability.Wisdom,15,"mental Black Cup"))LowerEffect(actor,"book-disoriented",60);LowerEffect(actor,"chapter-radio-haunted",86400);break;
   case "chapter-lastorder":actor.Hp=Math.Min(actor.MaxHp,actor.Hp+Roll(10));LowerEffect(actor,"chapter-lastorder",3600);break;
   case "chapter-greenhouse":ConsumeChapterItem(actor,a);s.Events.Add("greenhouse-active");break;
   case "chapter-arcade":ChangeTension(-Roll(4),"Arcade distraction");EmitLocalNoise(actor,"weapon",20);break;
   case "chapter-safe30":ChangeTension(Roll(6),"Rattling safe opened");if(s.BonusRoll==1)SpawnChapterNamed(d,s,"Severed Safe Hand");break;
   case "chapter-echo":case "chapter-score":RunChapterEcho(actor,d,s);break;
   case "chapter-hush":s.Events.Add("hush-day:"+(CampaignMinutes/1440));LowerEffect(actor,"chapter-silence",60);s.Resolved.Remove(a.Id);break;
   case "chapter-sneak":foreach(var target in Actors.Where(a=>a.Enemy&&a.Alive))LowerEffect(target,"chapter-asleep",60);break;
   case "chapter-camper":if(s.BonusRoll<=2){LowerEffect(actor,"chapter-camper-hostile",60);SpawnChapterNamed(d,s,"The Last Camper Hostile");Message(MessageCategory.Narrative,"The Last Camper fires a warning shot and refuses to join. The source's wider ranger-station route is recorded as a future hook.");}else {LowerRecruit(a,s);var camper=CommunityMembers.FirstOrDefault(p=>p.Id==a.NpcActorId);if(camper!=null){camper.Inventory.Add(CreateItemStack("item.lower.32.camper-rifle"));camper.Inventory.Add(CreateItemStack("item.lower.native-rifle",5));}}break;
   case "chapter-survivor":case "chapter-calm":case "chapter-scavenger":LowerRecruit(a,s);break;
   case "chapter-spill":s.Events.Remove("gas-active");s.Events.Add("gas-disabled");break;
   case "chapter-battery":s.Events.Add("battery-isolated");break;
   case "chapter-sedan":s.CarAttempts++;s.Events.Add("sedan-restored");ChangeTension(-5,"Sedan restored");break;
   case "chapter-fuel":s.Events.Add("fuel-recovered");break;
   case "chapter-tea":ConsumeChapterItem(actor,a);LowerEffect(actor,"chapter-morale",3600);break;
   case "chapter-filter":case "chapter-stove":if(e=="chapter-filter"){string water=Campaign!.Items.First(i=>i.SurvivalUse=="water").Id;var stack=CreateItemStack(water);if(actor.Inventory.CanAdd(stack))actor.Inventory.Add(stack);}LowerEffect(actor,"chapter-morale",3600);break;
   case "chapter-trade":ConsumeChapterItem(actor,a);break;
  }
  if(a.Kind=="loot"&&d.Number==20&&Roll(4)==1){s.Quiet=false;ChangeTension(Roll(6),"Junk avalanche");}
  if(a.Kind=="loot"&&d.Number==30&&e=="chapter-safe-cache"&&s.BonusRoll==2){var f=Features.Single(f=>f.Id==a.FeatureId);f.Contents.Items.Clear();var count=int.Parse(s.Events.Single(x=>x.StartsWith("safe-bullets:")).Split(':')[1]);f.Contents.Add(CreateItemStack("item.mall.ammo-9mm",count));}
  if(a.Kind=="backroom"&&d.Number==27){var release=s.Releases.Single(r=>r.EncounterId==a.EncounterId);int count=Roll(6);foreach(var enemy in Actors.Where(e=>Campaign!.Encounters.Single(c=>c.Id==a.EncounterId).ActorIds.Skip(count).Contains(e.Id)).ToArray())Actors.Remove(enemy);release.Count=count;ChangeTension(Roll(6),"Storage-room horde");}
  if(a.Kind=="backroom"&&d.Number==25)OpenDeliFreezer(actor,d,s,a);
 }
 void OpenDeliFreezer(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  int roll=s.BonusRoll;s.Events.Add("freezer:"+roll);var choices=d.Actions.Where(a=>a.Effect=="chapter-freezer-result").ToArray();var table=choices[roll<=2?0:roll-2];if(table.DamageDice>0){var release=s.Releases.Single(r=>r.EncounterId==table.EncounterId);int count=Roll(2)+1;foreach(var enemy in Actors.Where(e=>Campaign!.Encounters.Single(c=>c.Id==table.EncounterId).ActorIds.Skip(count).Contains(e.Id)).ToArray())Actors.Remove(enemy);release.Count=count;}ChangeTension(RollDamage(2,6),"Walk-in freezer opened");s.NextEventMinute=CampaignMinutes+10;
 }
 void RunChapterEcho(Actor actor,LowerMallSite d,LowerMallState s)
 {
  if(s.EchoRoll>0)return;s.EchoRoll=Roll(6);s.Events.Add("echo:"+s.EchoRoll);Message(MessageCategory.Narrative,"Project ECHO result "+s.EchoRoll+". The Conductor conducts from the speakers: break the music.");SpawnChapterNamed(d,s,"The Conductor");
  if(s.EchoRoll==1)foreach(var corpse in Actors.Where(a=>a.Enemy&&!a.Alive&&!LowerTraits(a).Contains("conductor"))){corpse.Hp=corpse.MaxHp;corpse.Character.SetCondition("unconscious","Unconscious",false);corpse.Character.SetCondition("dead","Dead",false);}
  if(s.EchoRoll==2)foreach(var p in Party.Where(p=>p.Alive))if(!SurvivorSave(p,Ability.Wisdom,15,"mental ECHO"))LowerEffect(p,"book-disoriented",3600);
  if(s.EchoRoll==3)foreach(var p in CommunityMembers)LowerEffect(p,"chapter-emp",60);
  if(s.EchoRoll==4)foreach(var target in Actors.Where(a=>a.Enemy&&a.Alive))LowerEffect(target,"chapter-berserk",3600);
  if(s.EchoRoll==5)foreach(var p in Party)LowerEffect(p,"book-disoriented",10);
  if(s.EchoRoll==6)SpawnChapterNamed(d,s,"The Stagehand");ChangeTension(RollDamage(2,6),"Project ECHO playback");
 }
 void Lower23Hazard(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if((d.Number<19||d.Number>36))return;s.Quiet=false;
  if(d.Number==22&&a.Id.EndsWith("hazard.1"))LowerEffect(actor,"chapter-sick",60);
  if(d.Number==23&&a.Id.EndsWith("hazard.0")&&Roll(4)==1)SpawnChapterNamed(d,s,"Suitcase Stalker",3);
  if(d.Number==26&&a.Id.EndsWith("hazard.2")&&Roll(6)==6)SpawnChapterNamed(d,s,"shamblers",4);
  if(d.Number==30&&a.Id.EndsWith("hazard.0")&&Roll(2)==1)SpawnChapterNamed(d,s,"shamblers",Roll(4));
 }
 void SecureLower23(Actor actor,LowerMallSite d,LowerMallState s)
 {
  if((d.Number<19||d.Number>36))return;if(s.Quiet&&d.Number is 23 or 34)CreditLower(s,"quiet",50);if(d.Number==26&&s.Quiet)ChangeTension(-Roll(4),"Quiet optometry clear");
 }
 int Lower23Damage(Actor target,int amount,DamageType type,Actor? source)
 {
  var t=LowerTraits(target);var s=LowerMonsterState(target);if(s==null)return amount;
  if(t.Contains("mask")&&!s.Events.Contains("mask-broken")||t.Contains("conductor")&&!s.Events.Contains("speakers-broken"))return 0;
  if(t.Contains("reflection")&&!s.Events.Contains("mirror-broken")&&(source==null||!source.Character.HasCondition("chapter-looking-away")&&source.FacingLeft!=(target.Position.X>source.Position.X)))return 0;
  if(t.Contains("dormant")&&target.Character.HasCondition("chapter-asleep"))return 0;
  if(t.Contains("slashresist")&&type==DamageType.Slashing||t.Contains("frozen")&&type==DamageType.Slashing||t.Contains("suitcase")&&type==DamageType.Bludgeoning)amount/=2;
  if((t.Contains("fireweak")||t.Contains("frozen")||t.Contains("suitcase"))&&type==DamageType.Fire)amount*=2;
  if(t.Contains("knight")&&source?.Range<=1){amount=Math.Max(0,amount-3);if(type!=DamageType.Bludgeoning)amount/=2;}
  if(t.Contains("model")&&!s.Events.Contains("mirror-broken"))amount/=2;return amount;
 }
 void Lower23DamageResolved(Actor target,int damage,DamageType type,Actor? source,bool wasAlive)
 {
  var t=LowerTraits(target);var s=LowerMonsterState(target);if(s==null)return;var d=Campaign!.LowerMall!.Sites.Single(d=>d.Number==s.Number);if((d.Number<19||d.Number>36))return;
  if(type==DamageType.Fire&&source!=null)IgniteLowerGarage(source);
  if(damage>0&&t.Contains("shortcircuit")&&type==DamageType.Bludgeoning&&target.Alive)LowerEffect(target,"stunned",6);
  if(damage>0&&t.Contains("gasbody")&&type==DamageType.Fire&&!s.Events.Contains("gas-body:"+target.Id)){s.Events.Add("gas-body:"+target.Id);foreach(var p in Actors.Where(a=>a.Alive&&a!=target&&a.Position.Distance(target.Position)<=2).ToArray())Hurt(p,RollDamage(3,6),DamageType.Fire);}
  if(wasAlive&&!target.Alive)
  {
   if(t.Contains("mountain")&&!s.Events.Contains("hatchet-removed")){target.Hp=1;target.Character.SetCondition("unconscious","Unconscious",false);LowerEffect(target,"stunned",6);Message(MessageCategory.Combat,"The Mountain Man rises: pull the hatchet from his skull.");}
   if(t.Contains("minute-revive")&&type!=DamageType.Fire&&!s.Events.Contains("burn:"+target.Id))LowerEffect(target,"chapter-revive",60);
   if(!s.Events.Contains("death23:"+target.Id)){
    s.Events.Add("death23:"+target.Id);
    if(t.Contains("deathwail")){SpawnChapterNamed(d,s,"shamblers",Roll(4),"reaction");ChangeTension(RollDamage(2,6),"The show must go on");}
    if(t.Contains("sporebrute"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=2).ToArray())if(!SurvivorSave(p,Ability.Constitution,15,"poison spores"))Hurt(p,RollDamage(2,6),DamageType.Poison,target);
    if(t.Contains("drummer"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=2).ToArray())if(!SurvivorSave(p,Ability.Dexterity,14,"exploding drummer"))Hurt(p,RollDamage(2,6),DamageType.Necrotic,target);
    if(t.Contains("vineboss"))ChangeTension(-Roll(6),"Vine-Twisted destroyed");
    if(t.Contains("model"))CreditLower(s,"model",150);if(t.Contains("reflection")){CreditLower(s,"reflection",200);foreach(var member in Party){member.Character.SetCondition("chapter-looking-away","Looking away",false);LowerMallEffects.RemoveAll(e=>e.ActorId==member.Id&&e.Condition=="chapter-looking-away");}}
   }
  }
  if(source?.Equipment.Weapon?.Definition.Name.Contains("taser",StringComparison.OrdinalIgnoreCase)==true&&damage>0&&Roll(2)==1)LowerEffect(target,"stunned",6);
  if(source?.Enemy==true&&damage>0&&!target.Enemy&&target.Alive){var st=LowerTraits(source);if(st.Contains("bleed"))LowerEffect(target,"chapter-bleeding",60);if(st.Contains("visions")&&!SurvivorSave(target,Ability.Wisdom,14,"mental island vision"))LowerEffect(target,"book-disoriented",6);if(st.Contains("poison")&&!SurvivorSave(target,Ability.Constitution,13,"poison"))LowerEffect(target,"poisoned",60);}
  if(source!=null&&!source.Enemy&&source.Range<=1&&t.Contains("vineboss")&&!SurvivorSave(source,Ability.Constitution,15,"mental spores"))LowerEffect(source,"book-disoriented",24);
 }
 void LowerTurn23(Actor actor,LowerMallState s,IReadOnlyList<string> t)
 {
  if((s.Number<19||s.Number>36))return;
  if(t.Contains("vineboss")&&!s.Events.Contains("burn:"+actor.Id))actor.Hp=Math.Min(actor.MaxHp,actor.Hp+5);
  if(t.Contains("dormant")&&Party.Any(p=>p.Alive&&p.Position.Distance(actor.Position)<=1)){actor.Character.SetCondition("chapter-asleep","Sleeping",false);LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition=="chapter-asleep");}
  if(actor.Character.HasCondition("chapter-asleep"))actor.Economy.Spend(ActionResourceKind.Action);
  if(t.Contains("cultist")&&Roll(6)<=3)actor.Economy.Spend(ActionResourceKind.Action);
  if(t.Contains("mindterror"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=6).ToArray())if(!SurvivorSave(p,Ability.Wisdom,14,"mental terror"))Hurt(p,Roll(6),DamageType.Psychic,actor);
  if(t.Contains("shortcircuit")&&Roll(6)>=4)foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=4))LowerEffect(p,"chapter-emp",60);
  if(t.Contains("sporebrute"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=2))if(!SurvivorSave(p,Ability.Constitution,14,"poison spores"))LowerEffect(p,"poisoned",60);
  if(CurrentLowerMallSite?.Number is 29 or 32 or 33)ChangeTension(Roll(6),"Loud local combat round");
 }
 void TickLowerMall23()
 {
  var d=CurrentLowerMallSite;var s=d==null?null:LowerMallProgress(d.Number);if(s==null||d!.Number is <19 or >36)return;
  foreach(var a in Actors.Where(a=>a.Enemy&&a.Character.HasCondition("chapter-asleep")&&LowerTraits(a).Contains("dormant")&&Party.Any(p=>p.Alive&&p.Position.Distance(a.Position)<=1)).ToArray()){a.Character.SetCondition("chapter-asleep","Sleeping",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="chapter-asleep");}
  if(Mode==Mode.Explore&&s.NextEventMinute>=0&&CampaignMinutes>=s.NextEventMinute){s.NextEventMinute=CampaignMinutes+(d.Number==24?10:60);if(d.Number is 24 or 36)ChapterEvent(PartyLeader,d,s,"timed");if(d.Number==22&&s.Events.Contains("toxic-active"))foreach(var p in Party.Where(p=>p.Alive))if(!SurvivorSave(p,Ability.Constitution,13,"poison fridge"))LowerEffect(p,"chapter-sick",60);if(d.Number==25&&s.Events.Any(x=>x.StartsWith("freezer:"))&&!s.Events.Contains("freezer-closed")&&!s.Events.Contains("freezer-escape")){s.Events.Add("freezer-escape");SpawnChapterNamed(d,s,"Meat Locker Shambler",3);}}
 }
 void ChapterEvent(Actor actor,LowerMallSite d,LowerMallState s,string trigger)
 {
  var events=d.Actions.Where(a=>a.Kind=="event").ToArray();if(events.Length==0)return;var e=events[Roll(events.Length)-1];string key="chapter-event:"+e.Id;if(s.Events.Contains(key))return;s.Events.Add(key);AddStoryJournalEntry(key,e.Name,e.Text,"Lower Mall");Message(MessageCategory.Narrative,e.Text);
  if(d.Number==24){if(e.Id.EndsWith(".1"))Hurt(actor,Roll(6),DamageType.Fire);if(e.Id.EndsWith(".4"))ChangeTension(Roll(6),"Doomed survivor video");}
  if(d.Number==28&&e.Id.EndsWith(".2")){s.Events.Add("vines-blocked");foreach(var exit in Features.Where(f=>f.Kind=="transition"))exit.Locked=true;}
  if(d.Number==36&&e.Id.EndsWith(".1"))s.Events.Add("return-hidden");if(d.Number==36&&e.Id.EndsWith(".3")){ChangeTension(RollDamage(2,6),"Comic soundtrack trap");SpawnChapterNamed(d,s,"shamblers",8);}
 }
 void IgniteLowerGarage(Actor source)
 {
  var d=CurrentLowerMallSite;var s=d==null?null:LowerMallProgress(d.Number);if(d?.Number!=34||s==null||!s.Events.Contains("gas-active")||s.Events.Contains("gas-exploded"))return;s.Events.Add("gas-exploded");s.Quiet=false;foreach(var a in Actors.Where(a=>a.Alive&&a.Position.Distance(source.Position)<=2).ToArray())Hurt(a,RollDamage(3,6),DamageType.Fire);ChangeTension(RollDamage(2,6),"Garage gasoline fireball");
 }
 string Lower23ItemEffect(ItemStack item)
 {
  if(!System.Text.RegularExpressions.Regex.IsMatch(item.ItemId,@"^item\.lower\.(19|2[0-9]|3[0-6])\."))return "";var n=item.Definition.Name.ToLowerInvariant();
  foreach(var (key,effect) in new[]{("spice bomb","spice"),("grandma","molotov"),("ghost peppers","pepper"),("not food anymore","spoiled"),("energy drink can","energy"),("espresso stimulants","stimulant"),("sugary fuel","sugar"),("luxury perfume","scent"),("smart glasses","smart"),("medical eye drops","eyes"),("mre meal","meal"),("cheese wheel","meal"),("protein bar","meal"),("freeze-dried","meal"),("ration pack","meal"),("sealed snack-day","meal"),("processed antibiotic","antibiotic")})if(n.Contains(key))return "chapter-"+effect;return "";
 }
 void UseLower23Item(Actor actor,ItemStack item,string effect)
 {
  switch(effect){
   case "chapter-spice":foreach(var e in Actors.Where(e=>e.Enemy&&e.Alive&&e.Position.Distance(actor.Position)<=2))if(!SurvivorSave(e,Ability.Constitution,13,"spice powder"))LowerEffect(e,"blinded",60);ChangeTension(2,"Spice bomb");break;
   case "chapter-molotov":case "chapter-pepper":foreach(var e in Actors.Where(e=>e.Enemy&&e.Alive&&e.Position.Distance(actor.Position)<=2).ToArray())Hurt(e,Roll(6),DamageType.Fire,actor);IgniteLowerGarage(actor);break;
   case "chapter-spoiled":if(!SurvivorSave(actor,Ability.Constitution,15,"poison spoiled surprise")){Hurt(actor,Roll(4),DamageType.Poison);LowerEffect(actor,"chapter-sick",3600);}break;
   case "chapter-energy":LowerEffect(actor,"book-bike",3600);break;
   case "chapter-stimulant":LowerEffect(actor,"chapter-stimulant",28800);break;
   case "chapter-sugar":actor.Hp=Math.Min(actor.MaxHp,actor.Hp+Roll(6));break;
   case "chapter-scent":LowerEffect(actor,"book-stealth",3600);break;
   case "chapter-smart":if(!actor.Character.HasCondition("chapter-emp"))LowerEffect(actor,"chapter-scan",6);break;
   case "chapter-eyes":actor.Character.SetCondition("blinded","Blinded",false);LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition=="blinded");break;
   case "chapter-meal":actor.Needs.LastFoodMinute=CampaignMinutes;actor.Needs.FoodTaken=true;break;
   case "chapter-antibiotic":actor.Character.SetCondition("book-infected","Infection",false);actor.Character.SetCondition("poisoned","Poisoned",false);break;
  }
 }
 void Lower23Noise(Actor source,string kind)
 {
  var d=CurrentLowerMallSite;var s=d==null?null:LowerMallProgress(d.Number);if(s==null||Exterior31Site(d)||Exterior30Site(d)||Roof29Site(d)||d!.Number<19||source.Character.HasCondition("chapter-silence"))return;
  if(kind is "gunshot" or "explosion" or "shout"){s.Quiet=false;int dice=d.Number is 19 or 23 or 25 or 36?3:d.Number is 20 or 30?2:1;ChangeTension(RollDamage(dice,6),"Echoes in "+d.Name);}
  if(d.Number==31&&kind is "gunshot" or "explosion" or "shout" or "door")foreach(var a in Actors.Where(a=>a.Enemy)){a.Character.SetCondition("chapter-asleep","Sleeping",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="chapter-asleep");}
 }

 void LowerPartyTurn23(Actor actor)
 {
  var d=CurrentLowerMallSite;var s=d==null?null:LowerMallProgress(d.Number);if(s==null||d!.Number<19||!Party.Contains(actor))return;
  if(d.Number==22&&s.Events.Contains("toxic-active")&&!SurvivorSave(actor,Ability.Constitution,13,"poison fridge gas"))LowerEffect(actor,"chapter-sick",60);
  if(actor.Character.HasCondition("chapter-bleeding"))Hurt(actor,1,DamageType.Slashing);
 }

 bool GreenhouseSubstitutesFood(string use)
 {
  var s=LowerMallProgress(28);if(use!="food"||s?.Secured!=true||!s.Events.Contains("greenhouse-active"))return false;s.FoodCycle=(s.FoodCycle+1)%5;return s.FoodCycle==0;
 }

}
