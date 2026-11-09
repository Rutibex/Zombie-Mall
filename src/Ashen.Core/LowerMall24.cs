namespace Ashen.Core;
public sealed partial class World
{
 void EnterLowerMall24(LowerMallSite d,LowerMallState s)
 {
  if((d.Number<37||d.Number>54))return;s.BranchRoll=Roll(6);if(d.Number is 48 or 51)s.BonusRoll=Roll(6);s.NextHazardMinute=CampaignMinutes+10;s.NextEventMinute=CampaignMinutes+10;
  if(d.Number is 44 or 52)s.Events.Add("gas-active");
  void Trim(string feature,int quantity){var f=Features.Single(f=>f.Id==feature);var stack=f.Contents.Items.First();stack.Quantity=quantity;stack.ResourceUnits=stack.ResourceUnits.Take(quantity).ToList();s.StockRoll=quantity;}
  if(d.Number==42)Trim("feature.lower.42.loot.0",2+Roll(4));if(d.Number==43)Trim("feature.lower.43.loot.2",RollDamage(3,6));if(d.Number==45)Trim("feature.lower.45.loot.5",RollDamage(3,6));if(d.Number==47)Trim("feature.lower.47.loot.1",Roll(6));if(d.Number==52)Trim("feature.lower.52.loot.3",Roll(20));
  if(d.Number is 48 or 51){var f=Features.Single(f=>f.Id==$"feature.lower.{d.Number}.extra.freezer");var keep=d.Number==48?s.BonusRoll switch{1=>new[]{0},2=>new[]{1},3=>new[]{2},4=>new[]{3},5=>new[]{4,5},_=>new[]{6}}:s.BonusRoll<=2?new[]{0}:s.BonusRoll==5?new[]{1}:Array.Empty<int>();f.Contents.Items.RemoveAll(i=>!keep.Any(k=>i.ItemId.EndsWith("."+k)));}
  if(d.Number==48&&s.EncounterRoll==6)ChangeTension(Roll(4),"Something is wrong with the snacks");
 }
 void ConfigureLower24Actors(IEnumerable<string> ids)
 {
  foreach(var id in ids){var actor=Actors.Single(a=>a.Id==id);var t=LowerTraits(actor);if(t.Contains("final-immovable")){actor.Character.Defenses.ConditionImmunities.AddRange(["grappled","restrained"]);}if(t.Contains("final-eyeless"))actor.Character.Defenses.ConditionImmunities.AddRange(["blinded","blind"]);
   if(t.Contains("final-feast"))actor.Character.MainAttack.RangeFeet=10;if(t.Contains("final-spitter")||t.Contains("final-acid"))actor.Character.MainAttack.RangeFeet=20;if(t.Contains("final-manager"))actor.Character.MainAttack.RangeFeet=30;
   if(t.Contains("final-reflection")){var p=PartyLeader;actor.Character.MainAttack.DamageDice=p.DamageDice;actor.Character.MainAttack.DamageDieSize=p.Die;actor.Character.MainAttack.RangeFeet=p.Range*5;actor.Character.MainAttack.DamageType=p.AttackDamageType;actor.Character.MainAttack.AttackBonusOverride=p.AttackBonus;actor.Character.MainAttack.DamageBonusOverride=p.DamageBonus;}
   if(actor.Name=="Big Bite Mascot"&&LowerMallProgress(52) is {} mascot&&!mascot.Resolved.Contains("action.lower.52.challenge.mascot"))mascot.Resolved.Add("action.lower.52.challenge.mascot");
   if(t.Contains("final-still"))LowerEffect(actor,"chapter-asleep",0);if(t.Contains("final-fresh"))ChangeTension(Roll(4),"They should not be this fresh");}
 }
 bool Lower24HasKey(Actor a,int n)=>n switch{38=>a.HasLowerGear("whispering doll storage key"),39=>false,47=>a.HasLowerGear("spice vault key"),45=>a.HasLowerGear("trendsetters storage key"),_=>false};
 string Lower24Reason(Actor a,LowerMallSite d,LowerMallState s,LowerMallAction action)
 {
  if((d.Number<37||d.Number>54))return "";string e=action.Effect;
  if(e=="final-locker47"&&!LowerCleared(s,d.BackEncounterId))return "Defeat Broseidon before using his locker key.";
  if(e=="final-decapitate"&&!Actors.Any(a=>LowerTraits(a).Contains("final-manager")&&!a.Alive))return "The manager must be down before you can decapitate the body.";
  if(e=="final-rescue"&&d.Number==51&&s.BonusRoll!=6)return "This journey's freezer does not contain a living survivor.";
  if(e=="final-rescue"&&d.Number==54&&s.EncounterRoll!=4)return "No bound Hollow One was found on the saved branch.";
  if(e=="final-survivor"&&d.Number==44&&s.EncounterRoll!=6)return "No trapped survivor was found on the saved branch.";
  if(e=="final-survivor"&&d.Number==46&&!s.Events.Contains("vault-survivor"))return "The barely conscious survivor has not appeared on a return event.";
  if(e=="final-cook"&&(s.BonusRoll!=6||!s.Resolved.Contains($"action.lower.{d.Number:00}.device.freezer")))return "Find the preserved steak in this journey's freezer first.";
  if(e=="final-train"&&!s.Secured)return "Secure the gym before beginning a full day of training.";
  if(e=="final-defib"&&(a.Character.HasCondition("final-defib-cooldown")||!Actors.Any(t=>t.Enemy&&t.Alive&&t.Position.Distance(a.Position)<=1.25f)))return "Stand beside a zombie with a charged defibrillator.";
  if(e=="final-negotiate"&&LowerCleared(s,d.BackEncounterId))return "The survivor has already been dealt with.";
  return "";
 }
 string Lower24SecureReason(LowerMallSite d,LowerMallState s)=>d.Number==47&&LowerMallEffects.Any(e=>e.Condition=="final-manager-revive"&&Campaign!.Encounters.Where(x=>x.AreaId==d.AreaId).SelectMany(x=>x.ActorIds).Contains(e.ActorId))?"Decapitate the fallen manager or resolve its revival before securing.":"";
 void LowerDevice24(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if((d.Number<37||d.Number>54))return;string e=a.Effect;
  switch(e){
   case "final-study":LowerEffect(actor,d.Number==37?"final-herbal-study":"final-prepper-study",0);break;
   case "final-kombucha":SetDialogueFlag("flag.lower.37.kombucha-inspected");break;
   case "final-clown":SpawnChapterNamed(d,s,"Shamblers",Roll(6));ChangeTension(Roll(6),"Animatronic final greeting");break;
   case "final-ballpit":SpawnChapterNamed(d,s,"Ball Pit Crawler",Roll(3));ChangeTension(RollDamage(2,6),"Ball-pit ambush");break;
   case "final-rc":EmitLocalNoise(actor,"weapon",80,Features.Single(f=>f.Id==a.FeatureId).Pos);LowerEffect(actor,"final-rc-running",Roll(4)*6);break;
   case "final-negotiate":PacifyChapter(d,s,"final-collector");break;
   case "final-music":ChangeTension(RollDamage(2,6),"Music box plays its hidden message");break;
   case "final-gas":case "final-dust":s.Events.Remove("gas-active");s.Events.Add("gas-disabled");s.HazardResolved=true;break;
   case "final-piano":ChangeTension(Roll(6),"Phantom piano");EmitLocalNoise(actor,"weapon",100);break;
   case "final-cellar":if(s.HazardId.EndsWith("hazard.3"))SpawnChapterNamed(d,s,"Wine Cellar Stalker",6);break;
   case "final-kettle":{var id=Campaign!.Items.First(i=>i.SurvivalUse=="water").Id;var water=CreateItemStack(id);if(actor.Inventory.CanAdd(water))actor.Inventory.Add(water);break;}
   case "final-scald":foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=2).Take(1).ToArray())Hurt(t,Roll(6),DamageType.Fire,actor);ChangeTension(Roll(4),"Scalding kettle");break;
   case "final-mummy":ChangeTension(5,"Dry whisper from the tea master");break;
   case "final-vip":LowerEffect(actor,"book-disoriented",60);break;
   case "final-train":s.HarvestMinute=CampaignMinutes+1440;s.Events.Add("training-started");break;
   case "final-meat":s.Events.Add("meat-destroyed");ChangeTension(Roll(6),"Destroyed raw meat piles");break;
   case "final-freezer":OpenFinalFreezer(actor,d,s);break;
   case "final-display":s.Events.Add("display:"+s.BonusRoll);Message(MessageCategory.Narrative,new[]{"A mannequin is suddenly standing behind you.","Distant laughter echoes without a source.","The light stabilizes; one mannequin is missing.","The jawless mannequin lunges from the display."}[s.BonusRoll-1]);if(s.BonusRoll==4)SpawnChapterNamed(d,s,"Trendsetters Mannequin",5);ChangeTension(Roll(4),"The Living Display");break;
   case "final-escape":foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=true;SetDialogueFlag("flag.lower.45.service-access");break;
   case "final-trade":ConsumeChapterItem(actor,a);break;
   case "final-survivor":case "final-rescue":if(LowerRecruit(a,s)&&d.Number==46&&CommunityMembers.FirstOrDefault(p=>p.Id==a.NpcActorId) is {} survivor)survivor.Hp=1;break;
   case "final-decapitate":s.Events.Add("manager-decapitated");foreach(var t in Actors.Where(t=>LowerTraits(t).Contains("final-manager"))){t.Hp=0;LowerMallEffects.RemoveAll(e=>e.ActorId==t.Id&&e.Condition=="final-manager-revive");t.Character.SetCondition("final-manager-revive","Revival",false);}break;
   case "final-can":ConsumeChapterItem(actor,a);int roll=Roll(4);s.Events.Add("can:"+roll);if(roll==3)Hurt(actor,Roll(6),DamageType.Poison);else if(roll==4&&!SurvivorSave(actor,Ability.Constitution,14,"infection parasite"))LowerEffect(actor,"final-parasite",Roll(4)*86400);else LowerEffect(actor,"chapter-morale",3600);break;
   case "final-inspect":s.Events.Add("snacks-inspected");break;
   case "final-cook":SetDialogueFlag("flag.lower.48.steak-cooked");actor.Needs.FoodTaken=true;actor.Needs.LastFoodMinute=CampaignMinutes;break;
   case "final-barricade":foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=false;SetDialogueFlag("flag.lower.49.book-barricade");break;
   case "final-book":EmitLocalNoise(actor,"weapon",20,Features.Single(f=>f.Id==a.FeatureId).Pos);foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=4))LowerEffect(t,"stunned",6);break;
   case "final-share":ChangeTension(-Roll(6),"Shared conspiracy");LowerEffect(actor,"chapter-paranoia",3600);break;
   case "final-read":ChangeTension(-Roll(4),"Reading nook");break;
   case "final-defib":LowerEffect(actor,"final-defib-cooldown",6);foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=1.25f).Take(1).ToArray())Hurt(t,Roll(12),DamageType.Lightning,actor);break;
   case "final-mascot":s.Events.Add("mascot:"+s.BranchRoll);if(s.BranchRoll==6){SpawnChapterNamed(d,s,"Big Bite Mascot");s.Resolved.Add("action.lower.52.challenge.mascot");}ChangeTension(Roll(6),"Big Bite mascot turns");break;
   case "final-radio":ChangeTension(Roll(4),"Drive-thru static");SetDialogueFlag($"flag.lower.{d.Number}.radio-evidence");break;
   case "final-feast":int feast=Roll(20)+TensionState.Score;s.Events.Add("feast:"+feast);if(feast>=20)SpawnChapterNamed(d,s,"Big Bite Mascot");else if(feast>=16)SpawnChapterNamed(d,s,"Shamblers",RollDamage(2,6));else if(feast>=11)SpawnChapterNamed(d,s,"Cursed Burger Employee",1);break;
   case "final-sculpture":SpawnChapterNamed(d,s,"Sculpture Stalker");ChangeTension(Roll(4),"Hollow sculpture ambush");break;
   case "final-tapestry":foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=true;SetDialogueFlag("flag.lower.53.tapestry-access");break;
   case "final-mirror":SpawnChapterNamed(d,s,"Cursed Showroom Reflection");ChangeTension(RollDamage(2,6),"Broken showroom mirror");break;
   case "final-glass":ConsumeChapterItem(actor,a);s.Events.Add("glass-trap");ChangeTension(Roll(6),"Shattered glass coffee table");break;
   case "final-recliner":SetDialogueFlag("flag.lower.54.recliner-disarmed");break;
  }
  if(d.Number==51&&a.Kind=="loot"&&a.Id.EndsWith("loot.0"))CreditLower(s,"food",50);
 }
 void OpenFinalFreezer(Actor actor,LowerMallSite d,LowerMallState s)
 {
  if(d.Number==43){ChangeTension(Roll(6),"Meat locker secured");return;}if(d.Number is not (48 or 51))return;ChangeTension(RollDamage(d.Number==51?2:1,6),"Locked freezer opened");s.Events.Add("freezer:"+s.BonusRoll);
  if(d.Number==48){if(s.BonusRoll==2&&s.BranchRoll>=4)SpawnChapterNamed(d,s,"Frozen Head");if(s.BonusRoll==4)ChangeTension(Roll(6),"Moving black goo");}
  else if(s.BonusRoll is 3 or 4)SpawnChapterNamed(d,s,"Frozen Revenant");
 }
 void LowerHazard24(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if((d.Number<37||d.Number>54))return;if(d.Number==37&&a.Id.EndsWith("hazard.0"))LowerEffect(actor,"final-mold",28800);
  if(d.Number==41&&a.Id.EndsWith("hazard.3")&&Roll(2)==1)SpawnChapterNamed(d,s,"Shamblers",Roll(4));if(d.Number==44&&a.Id.EndsWith("hazard.3"))Hurt(actor,RollDamage(2,6),DamageType.Slashing);
 }
 int Lower24Damage(Actor target,int amount,DamageType type,Actor? source,bool critical)
 {
  if(critical&&source?.Equipment.Weapon?.ItemId=="item.lower.43.loot.0.0")amount++;if(LowerTraits(target).Contains("final-bloated")&&type==DamageType.Bludgeoning)amount/=2;return amount;
 }
 void LowerDamageResolved24(Actor target,int damage,DamageType type,Actor? source,bool wasAlive)
 {
  if(!LowerMallEnabled)return;if(damage>0&&target.Equipment.Armor is {} shield&&shield.ItemId=="item.lower.54.loot.2.0"&&--shield.Charges<=0){shield.ConsumeResourceUnits(1);target.Equipment.Armor=null;}var d=CurrentLowerMallSite;var state=d==null?null:LowerMallProgress(d.Number);if(d==null||state==null||(d.Number<37||d.Number>54))return;var t=LowerTraits(target);
  if(source!=null&&type==DamageType.Fire)IgniteLower24(source,false);
  if(damage>0&&t.Contains("final-still")){target.Character.SetCondition("chapter-asleep","Sleeping",false);LowerMallEffects.RemoveAll(e=>e.ActorId==target.Id&&e.Condition=="chapter-asleep");}
  if(damage>0&&target.Alive&&t.Contains("final-meatheal")&&!state.Events.Contains("meat-destroyed"))target.Hp=Math.Min(target.MaxHp,target.Hp+5);
  if((type==DamageType.Fire||type==DamageType.Acid)&&t.Contains("final-patient"))state.Events.Add("suppress:"+target.Id+":"+Round);
  if(wasAlive&&!target.Alive){
   if(t.Contains("final-manager")&&!state.Events.Contains("manager-roll")){state.Events.Add("manager-roll");if(source?.AttackDamageType==DamageType.Slashing)state.Events.Add("manager-decapitated");else if(Roll(4)>=3)LowerEffect(target,"final-manager-revive",Roll(6)*6);}
   if(!state.Events.Contains("final-death:"+target.Id)){state.Events.Add("final-death:"+target.Id);if(t.Contains("final-mold"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=2).ToArray())if(!SurvivorSave(p,Ability.Constitution,14,"poison mold burst"))Hurt(p,Roll(6),DamageType.Poison,target);if(t.Contains("final-doll"))ChangeTension(Roll(6),"Hollow doll scream");if(t.Contains("final-wax"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=2).ToArray())if(!SurvivorSave(p,Ability.Dexterity,12,"burning resin"))Hurt(p,Roll(6),DamageType.Fire,target);}
  }
  if(target.Alive&&t.Contains("final-disperse")&&target.Hp<=10){target.Hp=0;target.Character.SetCondition("unconscious","Swarm dispersed",true);}
  if(source!=null&&source.Enemy&&damage>0&&!target.Enemy){var st=LowerTraits(source);if(st.Contains("final-feast")&&!target.Alive)source.Hp=Math.Min(source.MaxHp,source.Hp+10);if(st.Contains("final-bleed"))LowerEffect(target,"final-bleeding",0);if(st.Contains("final-slow"))LowerEffect(target,"final-slow",60);if(st.Contains("poison")&&!SurvivorSave(target,Ability.Constitution,13,"poison rotten meat"))LowerEffect(target,"poisoned",60);if(st.Contains("final-disease")&&!SurvivorSave(target,Ability.Constitution,13,"infection tainted meat"))LowerEffect(target,"book-infected",86400);if(st.Contains("final-archivist")&&!SurvivorSave(target,Ability.Dexterity,14,"ink vomit"))LowerEffect(target,"blinded",6);if(st.Contains("final-spitter")&&!SurvivorSave(target,Ability.Dexterity,14,"acid bile"))LowerEffect(target,"final-acid-burn",18);}
  if(source?.Equipment.Weapon?.ItemId=="item.lower.50.loot.4.0"&&damage>0&&target.Enemy&&LowerMallProgress(50) is {} med){var a=Campaign!.LowerMall!.Sites.Single(s=>s.Number==50).Actions.Single(a=>a.Effect=="final-defib");if(!med.Resolved.Contains(a.Id))med.Resolved.Add(a.Id);CreditLower(med,a.Id,10);}
  if(damage>0&&t.Contains("final-archivist")&&!state.Events.Contains("archivist-call")){state.Events.Add("archivist-call");SpawnChapterNamed(d,state,"Shamblers",Roll(6));ChangeTension(RollDamage(2,6),"Archivist screech");}
 }
 void LowerTurn24(Actor actor,LowerMallState s,IReadOnlyList<string> t)
 {
  if((s.Number<37||s.Number>54))return;var d=CurrentLowerMallSite!;
  if(t.Contains("final-patient")&&!s.Events.Contains("suppress:"+actor.Id+":"+(Round-1))&&!s.Events.Contains("suppress:"+actor.Id+":"+Round))actor.Hp=Math.Min(actor.MaxHp,actor.Hp+1);
  if(t.Contains("final-still")&&actor.Character.HasCondition("chapter-asleep")){if(Party.Any(p=>p.Alive&&p.Position.Distance(actor.Position)<=1)){actor.Character.SetCondition("chapter-asleep","Sleeping",false);LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition=="chapter-asleep");}else{actor.Economy.Spend(ActionResourceKind.Action);actor.Economy.MovementRemainingFeet=0;}}
  if(t.Contains("final-chant")||t.Contains("final-herbalist"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=4))if(!SurvivorSave(p,Ability.Wisdom,t.Contains("final-chant")?13:12,"mental chanting"))LowerEffect(p,"stunned",6);
  if(t.Contains("final-smile")&&!s.Events.Contains("smile:"+actor.Id)){s.Events.Add("smile:"+actor.Id);foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=3))if(!SurvivorSave(p,Ability.Wisdom,13,"mental unsettling smile"))LowerEffect(p,"stunned",6);}
  if(t.Contains("final-feast"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=6))if(!SurvivorSave(p,Ability.Wisdom,15,"mental horrific wailing"))LowerEffect(p,"frightened",60);
  if((t.Contains("final-screamer")||t.Contains("final-managercall"))&&!s.Events.Contains("final-call:"+actor.Id)){s.Events.Add("final-call:"+actor.Id);SpawnChapterNamed(d,s,"Shamblers",Roll(t.Contains("final-screamer")?6:4));ChangeTension(RollDamage(t.Contains("final-screamer")?3:1,6),"Local undead scream");}
  if(t.Contains("final-weaver"))foreach(var corpse in Actors.Where(a=>a.Enemy&&!a.Alive&&a!=actor&&a.Position.Distance(actor.Position)<=4).ToArray())if(!s.Events.Contains("absorbed:"+corpse.Id)){s.Events.Add("absorbed:"+corpse.Id);actor.Hp=Math.Min(actor.MaxHp,actor.Hp+Roll(8));}
 }
 void LowerPartyTurn24(Actor actor)
 {
  if(actor.Character.HasCondition("final-bleeding"))Hurt(actor,1,DamageType.Slashing);if(actor.Character.HasCondition("final-acid-burn"))Hurt(actor,Roll(6),DamageType.Acid);var d=CurrentLowerMallSite;var state=d==null?null:LowerMallProgress(d.Number);if(state?.Events.Contains("glass-trap")==true&&d!.Actions.FirstOrDefault(a=>a.Effect=="final-glass") is {} trap&&actor.Position.Distance(Features.Single(f=>f.Id==trap.FeatureId).Pos.Center)<=1.25f)Hurt(actor,Roll(4),DamageType.Piercing);
 }
 void ExpireLower24(Actor actor,LowerMallEffect e)
 {
  if(e.Condition.StartsWith("final-steroid-crash:"))actor.Needs.Exhaustion=Math.Min(6,actor.Needs.Exhaustion+1);
  if(e.Condition=="final-manager-revive"&&LowerMonsterState(actor) is {} s&&!s.Events.Contains("manager-decapitated")){actor.Hp=actor.MaxHp;actor.Character.SetCondition("unconscious","Unconscious",false);if(Mode==Mode.Combat)AddInitiativeParticipant(actor,reason:"saved manager revival");}
  if(e.Condition.StartsWith("final-food-poison-")){Hurt(actor,Roll(4),DamageType.Poison);int count=int.Parse(e.Condition[^1..]);if(count>1)LowerEffect(actor,"final-food-poison-"+(count-1),3600);}
  if(e.Condition=="final-parasite")LowerEffect(actor,"book-infected",86400);
 }
 void TickLowerMall24()
 {
  var d=CurrentLowerMallSite;var s=d==null?null:LowerMallProgress(d.Number);if(d==null||s==null||(d.Number<37||d.Number>54))return;
  if(s.NextHazardMinute>=0&&CampaignMinutes>=s.NextHazardMinute){s.NextHazardMinute=-1;if(d.Number==37&&s.HazardId.EndsWith("hazard.0"))foreach(var p in Party.Where(p=>p.Alive))if(!SurvivorSave(p,Ability.Constitution,13,"mold air"))LowerEffect(p,"final-mold",28800);if(d.Number==51)foreach(var p in Party.Where(p=>p.Alive))if(!SurvivorSave(p,Ability.Constitution,13,"mold cough"))LowerEffect(p,"final-cough",3600);if(d.Number is 46 or 47)ChangeTension(Roll(d.Number==46?6:4),"Lingering among the shelves");}
  if(d.Number is 50 or 52&&CampaignMinutes>=s.NextEventMinute){s.NextEventMinute=CampaignMinutes+10;FinalEvent(actor:PartyLeader,d,s);}
  if(d.Number==42&&s.Events.Contains("training-started")&&s.HarvestMinute<=CampaignMinutes&&!s.Events.Contains("trained")){s.Events.Add("trained");foreach(var p in Party)LowerEffect(p,"final-trained",86400);}
 }
 void FinalEvent(Actor actor,LowerMallSite d,LowerMallState s)
 {
  var table=d.Actions.Where(a=>a.Kind=="event").ToArray();if(table.Length==0)return;var a=table[Roll(table.Length)-1];if(s.Events.Contains("final-event:"+a.Id))return;s.Events.Add("final-event:"+a.Id);Message(MessageCategory.Narrative,a.Text);AddStoryJournalEntry("final.event."+a.Id,a.Name,a.Text,"Lower Mall");
  if(d.Number==46&&a.Id.EndsWith(".2"))s.Events.Add("vault-survivor");if(d.Number==50){if(a.Id.EndsWith(".1"))SpawnChapterNamed(d,s,"MediMart Lurker",5);if(a.Id.EndsWith(".4"))SpawnChapterNamed(d,s,"Mutant Patient");if(a.Id.EndsWith(".5")&&!SurvivorSave(actor,Ability.Dexterity,14,"chemical explosion"))Hurt(actor,RollDamage(2,8),DamageType.Fire);}
  if(d.Number==52&&a.Id.EndsWith(".2"))SpawnChapterNamed(d,s,"Cursed Burger Employee",2);
 }
 void ReturnLowerMall24(LowerMallSite d,LowerMallState s)
 {
  if(d.Number is 46 or 53)FinalEvent(PartyLeader,d,s);if(d.Number==54&&!s.Secured&&!s.Events.Contains("repopulated")){s.Events.Add("repopulated");SpawnChapterNamed(d,s,"Repopulation Shambler",Roll(4)+1);ChangeTension(1,"Returned to unsecured showroom");}
 }
 void SecureLower24(Actor actor,LowerMallSite d,LowerMallState s)
 {
  if((d.Number<37||d.Number>54))return;if(d.Number==51&&!s.Events.Contains("gas-exploded"))CreditLower(s,"quiet",25);
  if(LowerMallStates.Count(s=>s.Number>=37&&s.Number<=45&&s.Secured)==9)RunCampaignEvent("event.lower.thirtyseven-fortyfive");if(LowerMallStates.Count(s=>s.Number>=46&&s.Number<=54&&s.Secured)==9)RunCampaignEvent("event.lower.fortysix-fiftyfour");if(LowerMallStates.Count(s=>s.Number<=54&&s.Secured)==54)RunCampaignEvent("event.lower.all-secured");
 }
 void IgniteLower24(Actor source,bool gunshot)
 {
  var d=CurrentLowerMallSite;var s=d==null?null:LowerMallProgress(d.Number);if(d==null||s==null||(d.Number<37||d.Number>54)||!s.Events.Contains("gas-active")||s.Events.Contains("gas-exploded"))return;if(d.Number==52&&(gunshot||Roll(4)!=1))return;
  s.Events.Add("gas-exploded");s.Quiet=false;int dice=d.Number==52?6:d.Number==44?2:3,die=d.Number==44?10:6,radius=d.Number==52?6:d.Number==51?3:2;
  foreach(var p in Actors.Where(a=>a.Alive&&a.Position.Distance(source.Position)<=radius).ToArray()){int amount=RollDamage(dice,die);if(d.Number==51&&SurvivorSave(p,Ability.Dexterity,14,"flour ignition"))amount=SurvivorSaveDamage(p,amount,Ability.Dexterity,true,true);Hurt(p,amount,DamageType.Fire);}ChangeTension(RollDamage(2,6),"Ignited lower-mall hazard");
 }
 string Lower24ItemEffect(ItemStack item)
 {
  if(!System.Text.RegularExpressions.Regex.IsMatch(item.ItemId,@"^item\.lower\.(3[7-9]|4[0-9]|5[0-4])\."))return "";string n=item.Definition.Name.ToLowerInvariant();foreach(var (key,value) in new[]{("lucky rabbit","lucky"),("pre-workout","preworkout"),("steroid vial","steroid"),("permanent enhancer","enhancer"),("prescription optical stimulant","optical"),("two-dose antibiotics","antibiotic"),("recliner antibiotics","antibiotic"),("expired painkiller","painkiller"),("exhaustion stimulant","stimulant"),("defibrillator","defib"),("eye wash","eyewash"),("atomic chew","gum"),("dragon’s breath","chips"),("fugu","fugu"),("obscure canned meat","snack"),("collector energy drink","morale"),("moving kombucha","kombucha"),("herbal medical kit","herbal"),("trauma kits","medical"),("transfusion kit","medical")})if(n.Contains(key))return "final-"+value;return "";
 }
 string Lower24ItemReason(Actor a,ItemStack item)
 {
  string e=Lower24ItemEffect(item);if(e=="final-lucky"&&LowerMallProgress(39) is {} lucky&&lucky.DailyUses.GetValueOrDefault(item.InstanceId,-1)==CampaignMinutes/1440)return "The rabbit foot has been used by this survivor today.";
  if(e=="final-defib"&&(a.Character.HasCondition("final-defib-cooldown")||!Actors.Any(t=>t.Enemy&&t.Alive&&t.Position.Distance(a.Position)<=1.25f)))return "The defibrillator needs a round to recharge and a zombie within reach.";return "";
 }
 void UseLower24Item(Actor a,ItemStack item,string e)
 {
  switch(e){case "final-lucky":if(LowerMallProgress(39) is {} lucky){lucky.DailyUses[item.InstanceId]=CampaignMinutes/1440;LowerEffect(a,"final-lucky",0);}break;
   case "final-preworkout":LowerEffect(a,"final-preworkout",3600);ChangeTension(Roll(4),"Pre-workout jitters");break;
   case "final-steroid":a.Hp=Math.Min(a.MaxHp,a.Hp+RollDamage(2,8));LowerEffect(a,"final-steroid-crash:"+item.InstanceId+":"+item.Quantity,3600);break;
   case "final-enhancer":a.Character.Abilities.Strength=Math.Min(30,a.Character.Abilities.Strength+2);ChangeTension(RollDamage(2,6),"Experimental performance enhancer");break;
   case "final-optical":LowerEffect(a,"final-optical",3600);break;
   case "final-antibiotic":a.Character.SetCondition("book-infected","Infection",false);a.Character.SetCondition("poisoned","Poison",false);LowerEffect(a,"final-antibiotic",3600);break;
   case "final-painkiller":LowerEffect(a,"final-pain-relief",3600);break;
   case "final-stimulant":LowerEffect(a,"chapter-stimulant",3600);break;
   case "final-eyewash":a.Character.SetCondition("blinded","Blind",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="blinded");break;
   case "final-gum":a.Character.HitPoints.Temporary=Math.Max(a.Character.HitPoints.Temporary,Roll(6));break;
   case "final-chips":LowerEffect(a,"final-cough",Roll(4)*6);break;
   case "final-fugu":if(!SurvivorSave(a,Ability.Wisdom,15,"recognize fugu")){Hurt(a,Roll(6),DamageType.Poison);LowerEffect(a,"poisoned",3600);}break;
   case "final-snack":if(!SurvivorSkillCheck(a,Skill.Medicine,14,"inspect expired snack")){int result=Roll(4);if(result==1)LowerEffect(a,"final-food-poison-3",3600);if(result==2)LowerEffect(a,"book-caffeine",3600);if(result==3)LowerEffect(a,"book-disoriented",60);}break;
   case "final-kombucha":LowerEffect(a,"book-disoriented",60);break;
   case "final-morale":LowerEffect(a,"chapter-morale",3600);break;
   case "final-herbal":a.Hp=Math.Min(a.MaxHp,a.Hp+Roll(6));LowerEffect(a,"final-herbal-protected",3600);break;
   case "final-medical":a.Hp=Math.Min(a.MaxHp,a.Hp+5);a.Character.SetCondition("book-infected","Infection",false);break;
   case "final-defib":LowerEffect(a,"final-defib-cooldown",6);foreach(var target in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(a.Position)<=1.25f).Take(1).ToArray())Hurt(target,Roll(12),DamageType.Lightning,a);break;
  }
 }
 void Lower24Failure(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(d.Number==54&&a.Effect=="final-recliner"&&!SurvivorSave(actor,Ability.Dexterity,14,"spring knife"))Hurt(actor,Roll(8),DamageType.Slashing);
 }

}
