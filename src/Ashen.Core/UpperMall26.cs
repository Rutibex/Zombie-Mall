namespace Ashen.Core;
public sealed partial class Actor
{
 int Upper26GearSkill(Skill skill)=>(SkillRules.AbilityFor(skill)==Ability.Charisma&&Character.HasCondition("upper26-confidence")?2:0)-(Character.HasCondition("upper26-mold")?1:0)+(skill==Skill.Mechanics&&HasUpper25Gear("Cobbler Tool Kit")?2:0)+(skill==Skill.Survival&&Character.HasCondition("upper26-grid-study")?1:0);
}
public sealed partial class World
{
 static bool Upper26Site(LowerMallSite? d)=>d?.Level=="upper"&&d.DisplayNumber is >=19 and <=36;
 void EnterUpperMall26(LowerMallSite d,LowerMallState s)
 {
  if(!Upper26Site(d))return;int n=d.DisplayNumber;s.BranchRoll=Roll(6);s.BonusRoll=Roll(4);s.NextEventMinute=CampaignMinutes+10;
  void Trim(int slot,int count){var f=Features.Single(f=>f.Id==$"feature.{d.Prefix}.loot.{slot}");var stack=f.Contents.Items.Single();stack.Quantity=count;stack.ResourceUnits=stack.ResourceUnits.Take(count).ToList();}
  if(n==19&&s.BonusRoll!=2)Features.Single(f=>f.Id=="feature.upper.19.loot.7").Contents.Items.Clear();
  if(n==20){ChangeTension(2,"Entering Melody Makers");if(s.EncounterRoll==1)ChangeTension(2,"Phantom notes");if(s.EncounterRoll==2)ChangeTension(5,"Silent observer");if(s.EncounterRoll==3&&s.BranchRoll>=4)SpawnChapterNamed(d,s,"Crazed Composer");if(s.EncounterRoll==4)UpperAlarm(d,s,5,Roll(4));if(s.EncounterRoll==5)s.Events.Add("upper26-survivor");}
  if(n==19&&s.EncounterRoll==6)ChangeTension(5,"Tiny Threads silence");
  if(n==21){var keep=s.BranchRoll switch{1=>12,3=>13,4=>14,5=>15,_=>-1};foreach(var f in Features.Where(f=>f.Id.StartsWith("feature.upper.21.loot.")&&int.Parse(f.Id.Split('.').Last())>=12))if(int.Parse(f.Id.Split('.').Last())!=keep)f.Contents.Items.Clear();s.Events.Add("surprise:"+s.BranchRoll);if(s.BranchRoll==2)s.Events.Add("upper26-survivor");}
  if(n==24){foreach(var key in new[]{"storage","register","freezer"})s.LootRolls[key]=Roll(6);Trim(3,RollDamage(2,6));KeepSugarStock(s);}
  if(n==25){var keep=s.BranchRoll+4;foreach(var f in Features.Where(f=>f.Id.StartsWith("feature.upper.25.loot.")&&int.Parse(f.Id.Split('.').Last())>=5))if(int.Parse(f.Id.Split('.').Last())!=keep)f.Contents.Items.Clear();}
  if(n==26)Trim(4,Roll(6));if(n==27)Trim(4,Roll(6));if(n==31&&s.BranchRoll==4||n==32&&s.EncounterRoll==1)s.Events.Add("upper26-survivor");
  if(n==31)ChangeTension(1,"Searching Panel Mania");if(n==32&&s.EncounterRoll==2)UpperAlarm(d,s,5,Roll(4));
  if(n is 33 or 35)s.Events.Add("gas-active");
  if(n==36)Trim(6,Roll(4));
 }
 void KeepSugarStock(LowerMallState s)
 {
  int storage=s.LootRolls["storage"],register=s.LootRolls["register"],freezer=s.LootRolls["freezer"];var keep=new HashSet<int>{0,1,2};
  if(storage==3)keep.Add(3);if(storage==4)keep.Add(4);if(storage==5)keep.Add(5);if(storage==6){keep.Add(6);keep.Add(7);}keep.Add(register<=3?8:register==4?9:register==5?10:11);keep.Add(freezer<=2?12:freezer==3?13:freezer==4?14:freezer==5?15:16);
  foreach(var f in Features.Where(f=>f.Id.StartsWith("feature.upper.24.loot.")))if(!keep.Contains(int.Parse(f.Id.Split('.').Last())))f.Contents.Items.Clear();
 }
 void ConfigureUpper26Actors(IEnumerable<string> ids)
 {
  foreach(var id in ids){var a=Actors.Single(a=>a.Id==id);var tags=LowerTraits(a);if(tags.Contains("gallery-caffeine")||tags.Contains("gallery-baker")||tags.Contains("gallery-cobbler")||tags.Contains("gallery-porcelain"))LowerEffect(a,"chapter-asleep",0);if(tags.Contains("gallery-ink"))a.Character.MainAttack.RangeFeet=10;}
 }
 int Upper26SpawnCount(LowerMallSite d,LowerMallState s,LowerMallAction a)=>Upper26Site(d)&&d.DisplayNumber==31&&a.Effect=="upper26-back"?s.BonusRoll:99;
 string Upper26Reason(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Upper26Site(d))return "";int n=d.DisplayNumber;string effect=a.Effect;
  if(n==34&&a.Kind=="loot"&&s.SearchAttempts.GetValueOrDefault(actor.Id)>=3)return "Gourmet Bites allows at most three search attempts per survivor.";
  if(effect=="upper26-observe"&&!Actors.Any(t=>t.Enemy&&t.Alive&&LowerTraits(t).Contains("gallery-glitcher")&&t.Position.Distance(actor.Position)<=8&&Line(t.Position,actor.Position)))return "Observe a visible Glitcher before recording its pattern.";
  if(a.NpcActorId!=""&&!s.Events.Contains("upper26-survivor"))return "No living survivor was found on this saved source branch.";
  if(n==19&&effect=="upper26-cry"&&s.EncounterRoll!=3)return "The crying-child mechanism was absent on this entry branch.";
  if(n==19&&effect=="upper26-cat"&&s.EncounterRoll!=5)return "The store cat was absent on this entry branch.";
  if(n==20&&effect=="upper26-piano"&&s.EncounterRoll!=6)return "This journey did not reveal a cache beneath the piano.";
  if(effect=="upper26-negotiate"&&!Actors.Any(t=>t.Alive&&t.Enemy&&LowerTraits(t).Contains(n==23?"gallery-collector":"gallery-feral")))return "Find the living hostile survivor before negotiating.";
  if(effect=="upper26-barter"&&!Actors.Any(t=>t.Alive&&t.Enemy&&t.Name=="Paranoid Ink Survivor"))return "Meet the paranoid survivor before offering supplies.";
  if(effect=="upper26-escape"&&!actor.Character.HasCondition("upper26-freezer-trapped"))return "No survivor is currently trapped by the freezer door.";
  if(effect=="upper26-tribute"&&!Actors.Any(t=>t.Alive&&t.Name.StartsWith("Cultist Cosplayer")))return "No surviving cultist cosplayers remain to accept tribute.";
  if(effect is "upper26-tribute" or "upper26-negotiate" or "upper26-barter"&&!actor.Inventory.Items.Any(i=>Campaign!.Items.Any(d=>d.Id==i.ItemId&&d.SurvivalUse is "food" or "water")))return "Carry a real ration or water bottle to offer.";
  return "";
 }
 void Upper26Attempt(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(Upper26Site(d)&&d.DisplayNumber==34&&a.Kind=="loot")s.SearchAttempts[actor.Id]=s.SearchAttempts.GetValueOrDefault(actor.Id)+1;
 }
 void Upper26Failure(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Upper26Site(d))return;if(d.DisplayNumber==24&&a.Effect=="upper26-register")UpperAlarm(d,s,3,0);if(d.DisplayNumber==27){WakeUpper26("gallery-caffeine");ChangeTension(15,"Disturbed caffeine crawlers");}
 }
 void Upper26Hazard(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Upper26Site(d))return;int n=d.DisplayNumber,j=int.Parse(a.Id.Split('.').Last());int[] values=n switch{19=>[0,5],20=>[1,5,0],21=>[5],22=>[0,0,5,5,5],23=>[0,5,5,5,0],24=>[5,0],25=>[5,10],26=>[5,0],27=>[0,15],28=>[10,0,5],29=>[10],30=>[5,5,5],31=>[5,5],32=>[5,0],33=>[5,0],34=>[0,0,0],35=>[0,0,0],36=>[0,0,0],_=>[0]};int tension=values[j];if(n==23&&j==0){tension=Roll(4);if(Roll(4)==1)UpperReinforce(d,s,Roll(4));}if(n==23&&j==4)tension=RollDamage(2,6);if(n==36)tension=j==1?Roll(6):Roll(4);if(tension>0)UpperAlarm(d,s,tension,n==28&&j==0?Roll(4):n==23&&j==2?Roll(6):0);
  if(n==22&&j==3)SpawnChapterNamed(d,s,"Pet Swarm");if(n==27){WakeUpper26("gallery-caffeine");}if(n==25)WakeUpper26("gallery-cobbler");
 }
 void WakeUpper26(string trait)
 {
  foreach(var enemy in Actors.Where(e=>e.Alive&&LowerTraits(e).Contains(trait))){enemy.Character.SetCondition("chapter-asleep","Sleeping",false);LowerMallEffects.RemoveAll(e=>e.ActorId==enemy.Id&&e.Condition=="chapter-asleep");}
 }
 void OfferUpper26Ration(Actor actor)
 {
  var item=actor.Inventory.Items.First(i=>Campaign!.Items.Any(d=>d.Id==i.ItemId&&d.SurvivalUse is "food" or "water"));item.ConsumeResourceUnits(1);if(--item.Quantity<=0)actor.Inventory.Items.Remove(item);
 }
 void PacifyUpper26(string role)
 {
  foreach(var enemy in Actors.Where(e=>e.Alive&&e.Enemy&&e.CreatureKind==role||e.Alive&&e.Enemy&&e.Name.StartsWith(role))){enemy.Hp=0;enemy.Character.SetCondition("unconscious","Peacefully withdrew",true);}CheckEnd();
 }
 void Upper26Device(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Upper26Site(d))return;int n=d.DisplayNumber;string effect=a.Effect;
  if(a.Kind=="loot")
  {
   if(n==25&&(a.Id.EndsWith("loot.1")||a.Id.EndsWith("loot.3"))){SpawnChapterNamed(d,s,"Lurker Zombie Cobbler");WakeUpper26("gallery-cobbler");}
   if(n==26&&a.Id.EndsWith("loot.1"))ChangeTension(5,"Jewelry case opened");if(n==34&&s.EncounterRoll==6&&!s.Events.Contains("noise-wave")){s.Events.Add("noise-wave");UpperReinforce(d,s,Roll(4));}
  }
  switch(effect)
  {
   case "upper26-cry":s.Events.Add("cry-silenced");break;
   case "upper26-mannequin":s.Events.Add("mannequin:"+s.BonusRoll);if(s.BonusRoll==3)SpawnChapterNamed(d,s,"Tiny Mannequin",2);if(s.BonusRoll==4){var clue=d.Actions.Single(a=>a.Effect=="upper26-cult-clue");s.Resolved.Add(clue.Id);CreditLower(s,clue.Id,250);}break;
   case "upper26-cat":SetDialogueFlag("flag.upper.19.cat-calmed");Message(MessageCategory.Narrative,"The feral cat accepts your approach. It is recorded as a surviving animal, without inventing a combat pet class.");break;
   case "upper26-tape":if(n==19&&s.BonusRoll!=2)Features.Single(f=>f.Id=="feature.upper.19.loot.7").Contents.Items.Clear();
  if(n==20){ChangeTension(Roll(6),"Final Composition");if(!SurvivorSave(actor,Ability.Wisdom,13,"mental recording"))LowerEffect(actor,"book-disoriented",3600);}if(n==28){ChangeTension(5,"The island is real");SetDialogueFlag("flag.upper.28.mixtape-decoded");}break;
   case "upper26-teen":case "upper26-charlie":case "upper26-rescue":case "upper26-fan":LowerRecruit(a,s);break;
   case "upper26-tribute":OfferUpper26Ration(actor);PacifyUpper26("Cultist Cosplayer");break;
   case "upper26-surprise":s.Events.Add("surprise-opened");Message(MessageCategory.Narrative,"Costume surprise: "+new[]{"A finite snack stash.","A terrified survivor in a fitting room.","Medical supplies hidden behind the counter.","A Security Access keyring.","A portable battery charger.","A frightened pet cat."}[s.BranchRoll-1]);break;
   case "upper26-cages":SpawnChapterNamed(d,s,"Pet Swarm");break;
   case "upper26-negotiate":OfferUpper26Ration(actor);PacifyUpper26(n==23?"Doll Collector":"Feral Survivor");break;
   case "upper26-register":if(n==24){s.Events.Add("code:0714");SetDialogueFlag("flag.upper.24.freezer-key");}if(n==25){SpawnChapterNamed(d,s,"Lurker Zombie Cobbler");WakeUpper26("gallery-cobbler");UpperAlarm(d,s,5,0);}break;
   case "upper26-tools":if(n==25){SpawnChapterNamed(d,s,"Lurker Zombie Cobbler");WakeUpper26("gallery-cobbler");}break;
   case "upper26-freezer":if(n==24){SpawnChapterNamed(d,s,"Frozen Bakery Zombie");ChangeTension(2,"Frozen baker lunges");}if(n==35){if(s.EncounterRoll==4)SpawnChapterNamed(d,s,"Frozen Horror");LowerEffect(actor,"upper26-freezer-trapped",0);LowerEffect(actor,"restrained",0);foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=false;ChangeTension(RollDamage(2,6),"Freezer door slams");}break;
   case "upper26-escape":actor.Character.SetCondition("restrained","Restrained",false);LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition=="restrained");actor.Character.SetCondition("upper26-freezer-trapped","Freezer trap",false);LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition=="upper26-freezer-trapped");foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=true;break;
   case "upper26-repairboots":s.Events.Add("boots-repaired");SetDialogueFlag("flag.upper.25.boots-repaired");break;
   case "upper26-window":UpperAlarm(d,s,10,0);foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=true;break;
   case "upper26-musicbox":ChangeTension(2,"BioDyne music box");if(!SurvivorSave(actor,Ability.Wisdom,12,"mental music box"))LowerEffect(actor,"upper-paranoia",3600);break;
   case "upper26-radio":GalleryRadio(d,s,actor);break;
   case "upper26-quiet":s.Events.Add("quiet-approach");break;
   case "upper26-turntable":UpperAlarm(d,s,10,1);foreach(var groover in Actors.Where(e=>e.Alive&&LowerTraits(e).Contains("gallery-groover")))LowerEffect(groover,"upper26-groove",60);LowerEffect(actor,"upper-morale",3600);break;
   case "upper26-observe":LowerEffect(actor,"upper26-observed",3600);break;
   case "upper26-power":s.Events.Add("power-cut");ChangeTension(-5,"Pixel Paradise power isolated");break;
   case "upper26-exit":UpperAlarm(d,s,15,RollDamage(2,4));foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=true;break;
   case "upper26-clay":SpawnChapterNamed(d,s,"Porcelain Stalker");WakeUpper26("gallery-porcelain");ChangeTension(5,"Porcelain Stalker disturbed");break;
   case "upper26-glue":s.Events.Add("glue-trap");break;
   case "upper26-gas":s.Events.Remove("gas-active");s.Events.Add("gas-disabled");break;
   case "upper26-study":LowerEffect(actor,"upper26-grid-study",0);break;
   case "upper26-letter":if(n==36)ChangeTension(Roll(4),"Unsealed farewell letter");break;
   case "upper26-headset":s.Events.Add("headset:"+s.BranchRoll);Message(MessageCategory.Narrative,"The drive-thru signal repeats fragments of distant survivors, followed by: Thank you for choosing Big Bite Express. Please drive through for your order.");break;
   case "upper26-oil":ConsumeUpper25(actor,"Deep Fryer Oil");s.Events.Add("oil-trap");break;
   case "upper26-barter":OfferUpper26Ration(actor);PacifyUpper26("Paranoid Ink Survivor");break;
  }
 }
 void GalleryRadio(LowerMallSite d,LowerMallState s,Actor actor)
 {
  int broadcast=s.BranchRoll;string text=new[]{"Help… I’m trapped… I’m at the—static—Oh God, it’s here!","The coffee machine still works! Just kidding. You’re all dead anyway.","17… 42… 6… 99.","A child hums a lullaby; the ending stretches unnaturally.","Don’t listen. Don’t trust the—loud static and whispering.","An unearthly voice sighs: I see you."}[broadcast-1];s.Events.Add("broadcast:"+broadcast);Message(MessageCategory.Narrative,text);AddStoryJournalEntry("upper26.radio."+d.Number,"Mystery radio",text,"Upper Mall");UpperAlarm(d,s,10,0);WakeUpper26("gallery-caffeine");GalleryEvent(d,s);
 }
 void GalleryEvent(LowerMallSite d,LowerMallState s)
 {
  int roll=Roll(6);string key="gallery-event:"+roll;if(s.Events.Contains(key))return;s.Events.Add(key);if(d.DisplayNumber==27){if(roll==1)foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=false;if(roll==3)ChangeTension(5,"All the mugs shatter");if(roll==6)s.Events.Add("upper26-survivor");Message(MessageCategory.Narrative,new[]{"The café door shuts itself.","Something skitters in the air ducts.","All coffee mugs shatter at once.","The radio seems to answer your last words.","The floor turns warm, then ice-cold.","A panicked, bleeding survivor runs into the café."}[roll-1]);}
 }
 int Upper26Damage(Actor target,int amount,DamageType type,Actor? source,bool critical)
 {
  var tags=LowerTraits(target);if(tags.Contains("gallery-costume")){if(type is DamageType.Bludgeoning or DamageType.Piercing or DamageType.Slashing)return 0;if(type==DamageType.Fire)amount*=2;}
  if(tags.Contains("gallery-panel")){if(type==DamageType.Slashing)return 0;if(type==DamageType.Bludgeoning)amount*=2;}
  if((tags.Contains("gallery-parrot")||tags.Contains("gallery-bluntresist"))&&type==DamageType.Bludgeoning)amount/=2;if(tags.Contains("gallery-parrot")&&type==DamageType.Fire)amount*=2;
  if(tags.Contains("gallery-frozen")&&type is DamageType.Bludgeoning or DamageType.Piercing)amount/=2;
  if(amount>0&&source?.Enemy==true&&target.Character.HasCondition("upper26-cloak-ready")){target.Character.SetCondition("upper26-cloak-ready","Hero cloak",false);LowerMallEffects.RemoveAll(e=>e.ActorId==target.Id&&e.Condition=="upper26-cloak-ready");return 0;}return amount;
 }
 void Upper26DamageResolved(Actor target,int damage,DamageType type,Actor? source,bool wasAlive,bool critical)
 {
  if(damage>0&&target.Equipment.Armor is {} cosplay&&cosplay.ItemId=="item.upper.21.loot.0"&&--cosplay.Charges<=0){cosplay.ConsumeResourceUnits(1);target.Equipment.Armor=null;}
  if(type==DamageType.Fire&&source!=null)IgniteUpper26(source);var d=CurrentLowerMallSite;if(!Upper26Site(d))return;var s=LowerMallProgress(d!.Number)!;var tags=LowerTraits(target);
  if(damage>0&&(tags.Contains("gallery-baker")||tags.Contains("gallery-caffeine")||tags.Contains("gallery-porcelain")||tags.Contains("gallery-cobbler")))WakeUpper26(tags.Contains("gallery-baker")?"gallery-baker":tags.Contains("gallery-caffeine")?"gallery-caffeine":tags.Contains("gallery-cobbler")?"gallery-cobbler":"gallery-porcelain");
  if(tags.Contains("gallery-boss")&&wasAlive&&target.Hp<=target.MaxHp/2&&!s.Events.Contains("boss-roar")){s.Events.Add("boss-roar");var e=Campaign!.Encounters.First(e=>e.AreaId==d.AreaId&&e.ActorIds.Any(id=>Campaign.Actors.Single(a=>a.Id==id).Role=="Glitched Zombie"));int had=s.Releases.FirstOrDefault(r=>r.EncounterId==e.Id)?.Count??0;SpawnLower(d,s,e.Id,had+Roll(4),"reaction");ChangeTension(10,"Respawn Roar");}
  if(damage>0&&tags.Contains("gallery-spores"))foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(target.Position)<=2))if(!SurvivorSave(p,Ability.Constitution,12,"infection spores"))LowerEffect(p,"upper26-spores",Roll(6)*6);
  if(source!=null&&damage>0&&target.Alive){var st=LowerTraits(source);if(st.Contains("gallery-python")&&!SurvivorSave(target,Ability.Constitution,13,"mutation poison"))LowerEffect(target,"upper26-python-poison",18);if(st.Contains("gallery-charge")&&!SurvivorSave(target,Ability.Strength,12,"mosh charge"))LowerEffect(target,"prone",6);if(st.Contains("gallery-frozen")&&!SurvivorSave(target,Ability.Constitution,13,"freezing grasp"))LowerEffect(target,"final-slow",6);if(st.Contains("gallery-baker")&&Roll(5)==1)LowerEffect(source,"upper26-teeth-broken",0);if(st.Contains("gallery-comic-collector")&&critical){LowerEffect(target,"restrained",6);Hurt(target,Roll(4),DamageType.Piercing,source);}}
  if(damage>0&&tags.Contains("gallery-grease")&&source!=null&&!SurvivorSave(source,Ability.Dexterity,12,"grease impact"))LowerEffect(source,"prone",6);
  if(wasAlive&&!target.Alive)
  {
   var definition=Campaign!.LowerMall!.Monsters.FirstOrDefault(m=>m.ActorId==target.Id);if(definition?.RewardXp>0)CreditLower(s,"defeat:"+target.Id,definition.RewardXp);
   if(tags.Contains("gallery-porcelain"))foreach(var p in Actors.Where(p=>p!=target&&p.Alive&&p.Position.Distance(target.Position)<=2).ToArray())if(!SurvivorSave(p,Ability.Dexterity,12,"glass bones"))Hurt(p,Roll(6),DamageType.Slashing,target);
   if(tags.Contains("gallery-glitcher"))foreach(var p in Actors.Where(p=>p!=target&&p.Alive&&p.Position.Distance(target.Position)<=2))if(!SurvivorSave(p,Ability.Wisdom,12,"mental reset"))LowerEffect(p,"stunned",6);
   if(tags.Contains("gallery-gasbag")){foreach(var p in Actors.Where(p=>p!=target&&p.Alive&&p.Position.Distance(target.Position)<=2).ToArray())Hurt(p,SurvivorSaveDamage(p,RollDamage(2,6),Ability.Constitution,SurvivorSave(p,Ability.Constitution,12,"gasbag poison"),true),DamageType.Poison,target);UpperAlarm(d,s,RollDamage(2,6),Roll(6));}
   if(tags.Contains("gallery-cobbler")&&TensionState.Score>=50)UpperReinforce(d,s,Roll(4));
  }
 }
 void Upper26Turn(Actor actor,LowerMallState s,IReadOnlyList<string> tags)
 {
  var d=CurrentLowerMallSite;if(!Upper26Site(d))return;
  if(tags.Contains("gallery-composer")&&!s.Events.Contains("composer-scream")){s.Events.Add("composer-scream");UpperReinforce(d!,s,Roll(4));}
  if(tags.Contains("gallery-caffeine")){int roll=Roll(4);if(roll==1)actor.Economy.Reset(actor.EffectiveSpeedFeet,2);if(roll==2)LowerEffect(actor,"upper26-jump",6);if(roll==3)UpperReinforce(d!,s,Roll(2));if(roll==4){LowerEffect(actor,"prone",6);actor.Economy.Spend(ActionResourceKind.Action);}}
  if(tags.Contains("gallery-glitched"))actor.Economy.AddMovement(10);
  if(tags.Contains("gallery-screamer")&&Roll(6)>=5&&!s.Events.Contains("power-cut")){UpperAlarm(d!,s,10,Roll(4));}
  if(tags.Contains("gallery-art")&&Roll(4)==1)actor.Economy.Spend(ActionResourceKind.Action);
  if(tags.Contains("gallery-baker")&&actor.Character.HasCondition("upper26-teeth-broken")){actor.Character.MainAttack.DamageDieSize=6;actor.Character.MainAttack.DamageType=DamageType.Slashing;}
  if(tags.Contains("gallery-ink")&&Party.Any(p=>p.Alive&&p.Position.Distance(actor.Position)<=2&&Line(actor.Position,p.Position))){foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=2&&Line(actor.Position,p.Position)))if(!SurvivorSave(p,Ability.Dexterity,13,"ink vomit"))LowerEffect(p,"blinded",6);actor.Economy.Spend(ActionResourceKind.Action);}
  if(s.Events.Contains("glue-trap")&&!s.Events.Contains("glue-triggered")&&d!.Actions.FirstOrDefault(a=>a.Effect=="upper26-glue") is {} glue&&actor.Position.Distance(Features.Single(f=>f.Id==glue.FeatureId).Pos.Center)<=1.25f){s.Events.Add("glue-triggered");LowerEffect(actor,"restrained",Roll(3)*6);}
  if(s.Events.Contains("oil-trap")&&d!.Actions.FirstOrDefault(a=>a.Effect=="upper26-oil") is {} oil&&actor.Position.Distance(Features.Single(f=>f.Id==oil.FeatureId).Pos.Center)<=1.25f)Hurt(actor,Roll(6),DamageType.Fire);
 }
 void Upper26PartyTurn(Actor actor)
 {
  if(actor.Character.HasCondition("upper26-python-poison"))Hurt(actor,Roll(6),DamageType.Poison);if(actor.Character.HasCondition("upper26-spores"))Hurt(actor,Roll(4),DamageType.Poison);if(actor.Character.HasCondition("upper26-freezer-trapped")&&!SurvivorSave(actor,Ability.Constitution,14,"freezer cold"))Hurt(actor,Roll(6),DamageType.Cold);
 }
 void Upper26Noise(Actor source,string kind)
 {
  var d=CurrentLowerMallSite;if(!Upper26Site(d))return;var s=LowerMallProgress(d!.Number);if(s==null)return;s.Quiet=false;if(d.DisplayNumber==28)foreach(var e in Actors.Where(e=>e.Alive&&LowerTraits(e).Contains("gallery-groover")))LowerEffect(e,"upper26-groove",60);if(d.DisplayNumber==27)WakeUpper26("gallery-caffeine");
 }
 void Upper26Gunshot(Actor actor)
 {
  var d=CurrentLowerMallSite;if(!Upper26Site(d))return;var s=LowerMallProgress(d!.Number)!;ChangeTension(5,"Upper north-gallery gunfire");if(d.DisplayNumber==32)UpperReinforce(d,s,Roll(4));IgniteUpper26(actor);
 }
 void IgniteUpper26(Actor source)
 {
  var d=CurrentLowerMallSite;if(!Upper26Site(d))return;var s=LowerMallProgress(d!.Number)!;if(!s.Events.Contains("gas-active")||s.Events.Contains("gas-exploded"))return;s.Events.Add("gas-exploded");s.Events.Remove("gas-active");foreach(var a in Actors.Where(a=>a.Alive&&a.Position.Distance(source.Position)<=2).ToArray())Hurt(a,RollDamage(3,6),DamageType.Fire);ChangeTension(RollDamage(2,6),"Leaking propane ignites");
 }
 void TickUpperMall26()
 {
  var d=CurrentLowerMallSite;if(!Upper26Site(d))return;var s=LowerMallProgress(d!.Number);if(s==null)return;
  if(CampaignMinutes>=s.NextEventMinute&&!s.Secured){s.NextEventMinute=CampaignMinutes+10;if(d.DisplayNumber==27)GalleryEvent(d,s);if(d.DisplayNumber==35)ChangeTension(Roll(6),"Drive-thru whispers after ten minutes");}
 }
 void SecureUpper26(LowerMallSite d,LowerMallState s)
 {
  if(!Upper26Site(d))return;if(d.DisplayNumber==23){var clear=d.Actions.Single(a=>a.Effect=="upper26-clear");s.Resolved.Add(clear.Id);CreditLower(s,clear.Id,50);}
  if(LowerMallStates.Count(s=>s.Number is >=119 and <=127&&s.Secured)==9)RunCampaignEvent("event.upper.nineteen-twentyseven");if(LowerMallStates.Count(s=>s.Number is >=128 and <=136&&s.Secured)==9)RunCampaignEvent("event.upper.twentyeight-thirtysix");if(LowerMallStates.Count(s=>s.Number is >=101 and <=136&&s.Secured)==36)RunCampaignEvent("event.upper.all-thirtysix");
 }
 void ValidateUpper26State(LowerMallSite d,LowerMallState s)
 {
  if(s.LootRolls==null||s.SearchAttempts==null||s.LootRolls.Count>3||s.SearchAttempts.Count>64||s.SearchAttempts.Any(p=>p.Value is <0 or >3||!Campaign!.Actors.Any(a=>a.Id==p.Key&&!a.Enemy)))throw new InvalidDataException("Invalid chapter roll/search map.");
  if(Upper27Site(d))return;
  if(!Upper26Site(d)){if(s.LootRolls.Count+s.SearchAttempts.Count>0)throw new InvalidDataException("Foreign chapter progress maps.");return;}
  if(d.DisplayNumber==24){if(s.LootRolls.Count!=3||new[]{"storage","register","freezer"}.Any(k=>!s.LootRolls.TryGetValue(k,out int value)||value is <1 or >6))throw new InvalidDataException("Invalid Sugar and Crumbs saved rolls.");}else if(s.LootRolls.Count>0)throw new InvalidDataException("Foreign pantry rolls.");
  if(d.DisplayNumber!=34&&s.SearchAttempts.Count>0)throw new InvalidDataException("Foreign gourmet search attempts.");if(d.DisplayNumber==23&&!s.Secured&&s.Resolved.Contains("action.upper.23.clear"))throw new InvalidDataException("Unearned antique clear milestone.");
 }
 int ValidateUpper26Credit(LowerMallSite d,LowerMallState s,string credit,Actor[] all)
 {
  string id=credit[7..];var def=Campaign!.LowerMall!.Monsters.FirstOrDefault(m=>m.ActorId==id);if(!(Upper26Site(d)||Upper27Site(d)||Roof28Site(d))||def==null||def.RewardXp<=0||!s.Releases.Any(r=>Campaign.Encounters.Single(e=>e.Id==r.EncounterId).ActorIds.Take(r.Count).Contains(id))||!all.Any(a=>a.Id==id&&!a.Alive))throw new InvalidDataException("Unearned printed monster reward.");return def.RewardXp;
 }
 void Upper26AttackRoll(Actor actor,int roll)
 {
  if(actor.Equipment.Weapon is not {} weapon)return;bool fragile=weapon.ItemId is "item.upper.21.loot.1" or "item.upper.29.loot.0" or "item.upper.30.loot.2" or "item.upper.31.loot.0";if(fragile&&roll<=(weapon.ItemId=="item.upper.30.loot.2"?2:1)){weapon.ConsumeResourceUnits(1);actor.Equipment.Weapon=null;Message(MessageCategory.Combat,weapon.Definition.Name+" breaks.");}
 }
 string Upper26ItemReason(Actor actor,ItemStack stack)
 {
  string family=stack.Definition.Equipment?.Family??"";if(family=="upper26-cloak"&&actor.Character.HasCondition("upper26-cloak-used"))return "The hero cloak has already been prepared today.";
  if(family=="upper26-inkbottle"&&!Actors.Any(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=3&&Line(t.Position,actor.Position)))return "Find a visible hostile target within fifteen feet.";return "";
 }
 void UseUpper26Item(Actor actor,ItemStack stack,string effect)
 {
  switch(effect)
  {
   case "upper26-confidence":LowerEffect(actor,"upper26-confidence",86400);break;
   case "upper26-elf":LowerEffect(actor,"book-stealth",600);break;
   case "upper26-bravery":case "upper26-morale":LowerEffect(actor,"upper-morale",3600);break;
   case "upper26-cloak":LowerEffect(actor,"upper26-cloak-used",86400);LowerEffect(actor,"upper26-cloak-ready",86400);break;
   case "upper26-vampire":LowerEffect(actor,"upper26-vampire",60);break;
   case "upper26-constitution":LowerEffect(actor,"upper26-constitution",86400);break;
   case "upper26-dexdrink":LowerEffect(actor,"upper26-dexdrink",3600);actor.Needs.WaterTaken=false;actor.Needs.LastWaterMinute=Math.Max(Campaign!.StartMinute,actor.Needs.LastWaterMinute-60);break;
   case "upper26-stamina":actor.Character.HitPoints.Temporary=Math.Max(actor.Character.HitPoints.Temporary,Roll(6));break;
   case "upper26-scent":LowerEffect(actor,"book-stealth",600);break;
   case "upper26-bowl":EmitLocalNoise(actor,"door",60);break;
   case "upper26-inkbottle":foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=3&&Line(t.Position,actor.Position)).Take(1))if(!SurvivorSave(t,Ability.Dexterity,12,"thrown ink"))LowerEffect(t,"blinded",6);break;
   case "upper26-patties":if(!SurvivorSave(actor,Ability.Constitution,13,"food poisoning"))LowerEffect(actor,"chapter-sick",3600);else{actor.Needs.FoodTaken=true;actor.Needs.LastFoodMinute=CampaignMinutes;}break;
  }
 }
}
