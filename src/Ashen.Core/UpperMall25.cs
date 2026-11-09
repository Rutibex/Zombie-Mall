namespace Ashen.Core;
public sealed partial class Actor
{
 public bool HasUpper25Gear(string name)=>Inventory.Items.Concat(Equipment.All()).Any(s=>s.ItemId.StartsWith("item.upper.")&&s.Definition.Name==name&&s.Wear<100);
 public bool WearsUpper25Gear(string name)=>Equipment.All().Any(s=>s.ItemId.StartsWith("item.upper.")&&s.Definition.Name==name&&s.Wear<100);
 int Upper25GearSkill(Skill skill)=>(skill==Skill.Perception&&HasUpper25Gear("Prescription Glasses")?1:0)+(skill==Skill.Perception&&HasUpper25Gear("Compact Mirror")?1:0)+(skill==Skill.Lockpicking&&HasUpper25Gear("Manicure Kit")?1:0)+(skill==Skill.Lockpicking&&HasUpper25Gear("Musician Toolkit")?1:0)+(skill==Skill.Mechanics&&(HasUpper25Gear("Mechanic Toolkit")||HasUpper25Gear("Optician Toolkit")||HasUpper25Gear("Eyeglass Repair Kit")||HasUpper25Gear("Magnifying Lens"))?2:0)+(SkillRules.AbilityFor(skill)==Ability.Charisma&&Character.HasCondition("upper-makeup")?1:0)-(skill==Skill.Perception&&Character.HasCondition("upper-paranoia")?1:0)+(skill==Skill.Perception&&Character.HasCondition("upper-senses")?1:0);
}
public sealed partial class World
{
 void EnterUpperMall25(LowerMallSite d,LowerMallState s)
 {
  if(d.Level!="upper"||d.DisplayNumber>18)return;int n=d.DisplayNumber;s.BranchRoll=Roll(6);s.BonusRoll=Roll(4);s.NextEventMinute=CampaignMinutes+10;s.NextHazardMinute=CampaignMinutes+15;
  void Trim(int slot,int quantity){var f=Features.Single(f=>f.Id==$"feature.{d.Prefix}.loot.{slot}");var stack=f.Contents.Items.Single();stack.Quantity=quantity;stack.ResourceUnits=stack.ResourceUnits.Take(quantity).ToList();}
  if(n==5){Trim(2,Roll(6));Trim(5,Roll(6));}
  if(n==8)Trim(6,Roll(4));if(n==9)Trim(2,RollDamage(2,8));if(n==13)Trim(0,RollDamage(2,6));if(n==14)Trim(2,Roll(6));if(n==17){Trim(0,Roll(4));Features.Single(f=>f.Id=="feature.upper.17.loot.3").Contents.Items.Single().Charges=Roll(4);}
  if(n==18)Trim(0,RollDamage(2,4));
  if(n==15){s.StockRoll=Roll(10);if(s.StockRoll!=1)Features.Single(f=>f.Id=="feature.upper.15.loot.7").Contents.Items.Clear();}
  if(n==12&&s.HazardId.EndsWith(".4"))s.Events.Add("gas-active");
  if(n==10)ChangeTension(2,"Entering Dough & Co.");
  if(n==3){if(s.EncounterRoll==6)s.Events.Add("upper-survivor");else if(s.EncounterRoll==7)UpperAlarm(d,s,10,Roll(4));if(s.EncounterRoll==4)Message(MessageCategory.Narrative,"A faint emergency transmission repeats cryptic references to the island.");}
  if(n==6&&s.EncounterRoll==5||n==9&&s.EncounterRoll==4||n==10&&s.EncounterRoll==4||n==13&&s.EncounterRoll==2||n==18&&s.EncounterRoll==6)s.Events.Add("upper-survivor");
  if(n==9&&s.EncounterRoll==3)ChangeTension(5,"Mysterious melody");if(n==9&&s.EncounterRoll==6)UpperAlarm(d,s,5,Roll(8));if(n==11&&s.EncounterRoll==6)ChangeTension(2,"Eerie silence");
  if(n==13&&s.EncounterRoll==4&&s.BranchRoll>=4)foreach(var p in Party)LowerEffect(p,"upper-slippery",60);
  if(n==14&&s.EncounterRoll==3&&s.BranchRoll>=4)SpawnChapterNamed(d,s,"Fitness Cultist");if(n==14&&s.EncounterRoll>=5)UpperAlarm(d,s,5,s.EncounterRoll==6?Roll(4):0);
  if(n==16){if(s.EncounterRoll==2){s.HazardResolved=true;s.Resolved.Add(s.HazardId);if(!SurvivorSave(PartyLeader,Ability.Dexterity,12,"shoe caltrops"))Hurt(PartyLeader,Roll(4),DamageType.Piercing);}else{s.HazardResolved=true;s.Resolved.Add(s.HazardId);}if(s.EncounterRoll==4)Message(MessageCategory.Narrative,"A stray cat hisses among the shoes; it is alive.");}
  if(n==17){SpawnChapterNamed(d,s,"Stalking Waitstaff",2);if(s.EncounterRoll==3)UpperAlarm(d,s,5,Roll(4));}
 }
 void ConfigureUpper25Actors(IEnumerable<string> ids)
 {
  foreach(var id in ids){var a=Actors.Single(a=>a.Id==id);var t=LowerTraits(a);if(t.Contains("ambush")&&a.Id.StartsWith("actor.upper."))LowerEffect(a,"hidden",0);if(t.Contains("upper-cobbler"))LowerEffect(a,"chapter-asleep",0);if(t.Contains("upper-eye"))a.Character.Defenses.ConditionImmunities.AddRange(["blinded","blind"]);if(t.Contains("upper-drone"))a.Character.MainAttack.RangeFeet=30;}
 }
 string Upper25Reason(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(d.Level!="upper"||d.DisplayNumber>18)return "";string e=a.Effect;
  if(a.NpcActorId!=""&&!s.Events.Contains("upper-survivor"))return "No living survivor is present on this location’s saved encounter branch.";
  if(e=="upper-leather"&&(actor.Equipment.Armor==null||actor.Character.HasCondition("upper-reinforced")))return "Wear armor; each survivor may reinforce it once.";
  if(e=="upper-hack"&&!Actors.Any(t=>t.Alive&&LowerTraits(t).Contains("upper-drone")))return "The malfunctioning security drone is not active.";
  if(e=="upper-negotiate"&&!Actors.Any(t=>t.Alive&&t.Enemy&&t.Name.StartsWith("Tech Cultist")))return "No active tech cultist remains to negotiate with.";
  if(e=="upper-feast"&&!Actors.Any(t=>t.Alive&&t.Name.StartsWith("Fine-Dining Cultist")))return "There is no surviving fine-dining cultist to address.";
  if(e=="upper-shiny"&&!Actors.Any(t=>t.Alive&&LowerTraits(t).Contains("upper-hartley")))return "Investigate the exam room and find Hartley first.";
  if(e=="upper-poison"&&!actor.HasUpper25Gear("Expired Supplements"))return "Recover the expired supplements first.";
  if(e=="upper-briefcase"&&!s.Releases.Any(r=>Campaign!.Encounters.Single(e=>e.Id==r.EncounterId).ActorIds.Any(id=>Campaign.Actors.Single(a=>a.Id==id).Role=="Corporate Courier")))return "The corporate courier and its briefcase were absent on this entry branch.";
  return "";
 }
 void UpperAlarm(LowerMallSite d,LowerMallState s,int tension,int count)
 {
  ChangeTension(tension,d.Name+" alarm / noise");s.Quiet=false;EmitLocalNoise(PartyLeader,"door",100);if(count>0)UpperReinforce(d,s,count);
 }
 void UpperReinforce(LowerMallSite d,LowerMallState s,int count)
 {
  var e=Campaign!.Encounters.First(e=>e.AreaId==d.AreaId&&e.ActorIds.Any(id=>Campaign.Actors.Single(a=>a.Id==id).Role=="Shambler"));int used=s.Releases.FirstOrDefault(r=>r.EncounterId==e.Id)?.Count??0;SpawnLower(d,s,e.Id,Math.Min(e.ActorIds.Count,used+count),"reaction");
 }
 void Upper25Hazard(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(d.Level!="upper"||d.DisplayNumber>18)return;int n=d.DisplayNumber;int index=int.Parse(a.Id.Split('.').Last());
  int[] tension=n switch{1=>[5,15],2=>[5,3],3=>[5,5,10],4=>[Roll(4),0,0,5,10,5],5=>[0,0,5],6=>[5,10,5,0],7=>[5,5,5,10,5,5],8=>[0,5,5],9=>[5,5,0,5],10=>[0,1,2],11=>[0,0,5,0],12=>[5,0,0,5,0,0],13=>[0,0,5,5,5,0],14=>[0,Roll(4),5],15=>[2,0,0,10],16=>[5],17=>[10,5],18=>[5,0,0],_=>[0]};
  if(tension[index]>0)UpperAlarm(d,s,tension[index],n==7&&index==3?Roll(4):n==4&&index==3?Roll(4):0);
  if(n==1&&index==1){if(!SurvivorSave(actor,Ability.Dexterity,13,"weak floor")){Hurt(actor,Roll(6),DamageType.Bludgeoning);UpperReinforce(d,s,Roll(4));}Message(MessageCategory.Narrative,"The weakened floor gives way into a service void. Its vertical fall is resolved here; no unprinted basement map is invented.");}
  if(n==5&&index==0)LowerEffect(actor,"chapter-sick",Roll(4)*86400);
  if(n==13&&index==4)SpawnChapterNamed(d,s,"Vitality Rats");
  if(n==12&&index==4)s.Events.Add("gas-active");
 }
 void Upper25Failure(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(d.Level!="upper"||d.DisplayNumber>18)return;if(d.DisplayNumber==5&&a.Effect=="upper-freezer")UpperAlarm(d,s,5,0);
  if(d.DisplayNumber==5&&a.Effect=="upper-power"&&!SurvivorSave(actor,Ability.Dexterity,13,"electrical repair hazard"))Hurt(actor,Roll(6),DamageType.Lightning);
  if(a.Effect=="upper-hack")UpperAlarm(d,s,2,0);
 }
 void Upper25Device(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(d.Level!="upper"||d.DisplayNumber>18)return;int n=d.DisplayNumber;switch(a.Effect)
  {
   case "upper-back":if(n==2&&!s.Events.Contains("alarm-disabled"))UpperAlarm(d,s,5,2);break;
   case "upper-alarm":case "upper-pa":s.Events.Add("alarm-disabled");break;
   case "upper-carousel":s.Events.Add("carousel-disabled");break;
   case "upper-bike":LowerEffect(actor,"book-bike",3600);break;
   case "upper-floor":s.HazardResolved=true;if(!s.Resolved.Contains(s.HazardId))s.Resolved.Add(s.HazardId);break;
   case "upper-office":s.Events.Add("office:"+s.BranchRoll);if(s.BranchRoll==3)UpperAlarm(d,s,5,0);else if(s.BranchRoll==4)SpawnChapterNamed(d,s,"Runner");else {string id=s.BranchRoll==2?"item.mall.ammo-9mm":s.BranchRoll==5?"item.upper.02.loot.4":s.BranchRoll==6?"item.upper.10.loot.8":"item.upper.02.loot.0";var stack=CreateItemStack(id,s.BranchRoll==2?10:s.BranchRoll==5?2:s.BranchRoll==1?3:1);if(actor.Inventory.CanAdd(stack))actor.Inventory.Add(stack);}break;
   case "upper-shiny":foreach(var t in Actors.Where(t=>t.Alive&&LowerTraits(t).Contains("upper-hartley")))LowerEffect(t,"stunned",6);break;
   case "upper-power":ChangeTension(-5,"Freezer power restored");if(!SurvivorSave(actor,Ability.Dexterity,13,"electrical repair hazard"))Hurt(actor,Roll(6),DamageType.Lightning);break;
   case "upper-freezer":s.Events.Add("freezer-powered");s.HarvestMinute=CampaignMinutes+4320;break;
   case "upper-bait":ConsumeUpper25(actor,"Spoiled Bait Meat");EmitLocalNoise(actor,"door",60);break;
   case "upper-chart":SetDialogueFlag("flag.upper.06.vision-chart");break;
   case "upper-laser":s.Events.Add("laser-disabled");break;
   case "upper-laserrepair":SetDialogueFlag("flag.upper.06.laser-repaired");break;
   case "upper-child":case "upper-optician":case "upper-musician":case "upper-mia":case "upper-shopper":case "upper-clara":LowerRecruit(a,s);if(n==6)SetDialogueFlag("flag.upper.06.optometry-key");if(n==18)SetDialogueFlag("flag.upper.18.clara-parley");break;
   case "upper-drone":s.Events.Add("drone-repaired");Reveal();foreach(var t in Actors.Where(t=>t.Alive&&t.Enemy))Explored.Add(t.Pos);ChangeTension(2,"Repaired drone scouts this shop");break;
   case "upper-terminal":case "upper-tracker":case "upper-handbook":SetDialogueFlag($"flag.{d.Prefix}.evidence");break;
   case "upper-cart":case "upper-barricade":case "upper-gate":foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=false;s.Events.Add("barricade-braced");break;
   case "upper-music":ChangeTension(-10,"Controlled musical diversion");EmitLocalNoise(actor,"door",100);LowerEffect(actor,"upper-morale",3600);break;
   case "upper-display":UpperAlarm(d,s,5,0);if(Roll(3)==1){var stack=CreateItemStack("item.upper.10.loot.0");if(actor.Inventory.CanAdd(stack))actor.Inventory.Add(stack);}break;
   case "upper-foam":foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=3))LowerEffect(t,"blinded",6);break;
   case "upper-machine":s.Events.Add("machine:"+s.BonusRoll);if(s.BonusRoll==1)Hurt(actor,Roll(4),DamageType.Lightning);else if(s.BonusRoll==2)UpperAlarm(d,s,5,Roll(4));else if(s.BonusRoll==3){LowerEffect(actor,"upper-morale",3600);UpperAlarm(d,s,5,Roll(4));}else if(!SurvivorSave(actor,Ability.Wisdom,13,"mental spore visions"))LowerEffect(actor,"book-disoriented",3600);break;
   case "upper-fuel":s.Events.Remove("gas-active");s.Events.Add("gas-disabled");break;
   case "upper-compressor":UpperAlarm(d,s,5,0);break;
   case "upper-cabinet":SpawnChapterNamed(d,s,"Vitality Rats");break;
   case "upper-poison":ConsumeUpper25(actor,"Expired Supplements");var poison=Campaign!.Items.FirstOrDefault(i=>i.Name.Contains("Poison")&&i.Equipment!=null);if(poison!=null){var stack=CreateItemStack(poison.Id);if(actor.Inventory.CanAdd(stack))actor.Inventory.Add(stack);}s.Events.Add("supplements-identified");break;
   case "upper-hack":foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&LowerTraits(t).Contains("upper-drone"))){t.Hp=0;t.Character.SetCondition("unconscious","Hacked and powered down",true);}CheckEnd();break;
   case "upper-negotiate":case "upper-feast":foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Name.StartsWith(a.Effect=="upper-negotiate"?"Tech Cultist":"Fine-Dining Cultist"))){t.Hp=0;t.Character.SetCondition("unconscious","Peacefully withdrew",true);}CheckEnd();break;
   case "upper-tablet":s.Events.Add("camera-feed");Reveal();ChangeTension(2,"Local camera feed");break;
   case "upper-solar":s.Events.Add("solar-tested");Message(MessageCategory.Narrative,"The portable charger works. It is recovered for later powered-device use; no unlimited ammunition or fuel is generated.");break;
   case "upper-radio":ChangeTension(-5,"Fragmented military transmission");SetDialogueFlag("flag.upper.16.radio-evidence");break;
   case "upper-leather":LowerEffect(actor,"upper-reinforced",0);break;
   case "upper-glue":ConsumeUpper25(actor,"Industrial Glue");s.Events.Add("glue-trap");break;
   case "upper-mirror":s.Events.Add("mirror:"+s.BranchRoll);if(s.BranchRoll==6)UpperAlarm(d,s,5,0);if(s.BranchRoll==3&&!SurvivorSave(actor,Ability.Wisdom,13,"mental mirror"))LowerEffect(actor,"upper-paranoia",3600);break;
  }
 }
 void ConsumeUpper25(Actor actor,string name)
 {
  var stack=actor.Inventory.Items.FirstOrDefault(i=>i.ItemId.StartsWith("item.upper.")&&i.Definition.Name==name);if(stack==null)return;stack.ConsumeResourceUnits(1);if(--stack.Quantity<=0)actor.Inventory.Items.Remove(stack);
 }
 int Upper25Damage(Actor target,int damage,DamageType type,Actor? source)
 {
  var t=LowerTraits(target);if(t.Contains("upper-static")&&type==DamageType.Lightning)return 0;if(t.Contains("upper-electricresist")&&type==DamageType.Lightning)damage/=2;if(t.Contains("upper-areaonly")&&source!=null&&source.Range<=1&&type!=DamageType.Fire)return 0;return damage;
 }
 void Upper25DamageResolved(Actor target,int damage,DamageType type,Actor? source,bool wasAlive,bool critical)
 {
  var d=CurrentLowerMallSite;if(d?.Level!="upper"||d.DisplayNumber>18)return;var s=LowerMallProgress(d.Number)!;var t=LowerTraits(target);
  if(damage>0&&t.Contains("upper-cobbler")){target.Character.SetCondition("chapter-asleep","Sleeping",false);LowerMallEffects.RemoveAll(e=>e.ActorId==target.Id&&e.Condition=="chapter-asleep");}
  if(damage>0&&source!=null&&LowerTraits(source).Contains("upper-static")&&target.Alive)Hurt(target,Roll(4),DamageType.Lightning);
  if(damage>0&&t.Contains("upper-static")&&source!=null&&source.Range<=1&&source.Alive)Hurt(source,Roll(4),DamageType.Lightning,target);
  if(wasAlive&&!target.Alive&&t.Any(x=>x is "upper-diseaseburst" or "upper-burst8" or "upper-burst6" or "upper-fireburst"))foreach(var p in Actors.Where(p=>p!=target&&p.Alive&&p.Position.Distance(target.Position)<=2).ToArray())
  {bool fire=t.Contains("upper-fireburst"),disease=t.Contains("upper-diseaseburst");int value=disease?0:t.Contains("upper-burst8")?Roll(8):RollDamage(2,fire?8:6);if(value>0)Hurt(p,SurvivorSaveDamage(p,value,Ability.Dexterity,SurvivorSave(p,Ability.Dexterity,fire?13:14,"death burst"),true),fire?DamageType.Fire:DamageType.Poison,target);if(!fire&&!SurvivorSave(p,Ability.Constitution,13,"infection burst"))LowerEffect(p,"book-infected",86400);}
  if(damage>0&&source?.Equipment.Weapon?.ItemId=="item.upper.09.loot.0"&&target.Alive&&critical)LowerEffect(target,"stunned",6);
 }
 void Upper25Turn(Actor actor,LowerMallState s,IReadOnlyList<string> t)
 {
  var d=CurrentLowerMallSite;if(d?.Level!="upper"||d.DisplayNumber>18)return;
  if(s.Events.Contains("glue-trap")&&!s.Events.Contains("glue-triggered")&&actor.Enemy&&d.Actions.FirstOrDefault(a=>a.Effect=="upper-glue") is {} glue&&actor.Position.Distance(Features.Single(f=>f.Id==glue.FeatureId).Pos.Center)<=1.25f){s.Events.Add("glue-triggered");LowerEffect(actor,"restrained",12);}
  if(t.Contains("upper-cobbler")&&Party.Any(p=>p.Alive&&p.Position.Distance(actor.Position)<=1)){actor.Character.SetCondition("chapter-asleep","Sleeping",false);LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition=="chapter-asleep");}
  if(t.Contains("upper-hartley")&&!s.Events.Contains("lamp:"+actor.Id)&&Party.Any(p=>p.Alive&&p.Position.Distance(actor.Position)<=3)){s.Events.Add("lamp:"+actor.Id);foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=3))if(!SurvivorSave(p,Ability.Constitution,13,"blinding lamp"))LowerEffect(p,"blinded",Roll(4)*6);}
  if(t.Contains("upper-eye")&&Roll(6)>=5&&Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=6&&Line(actor.Position,p.Position)).OrderBy(p=>p.Position.Distance(actor.Position)).FirstOrDefault() is {} victim){Hurt(victim,SurvivorSaveDamage(victim,RollDamage(2,6),Ability.Dexterity,SurvivorSave(victim,Ability.Dexterity,13,"eye beam"),true),DamageType.Fire,actor);actor.Economy.Spend(ActionResourceKind.Action);}
  if(t.Contains("upper-screamer")&&!s.Events.Contains("scream:"+actor.Id)){s.Events.Add("scream:"+actor.Id);UpperAlarm(d,s,d.DisplayNumber==3?15:10,Roll(6));}
  if(t.Contains("upper-chant")){int turns=s.Events.Count(e=>e.StartsWith("chant:"+actor.Id+":"));if(turns<9)s.Events.Add("chant:"+actor.Id+":"+turns);if(turns%3==2)UpperReinforce(d,s,Roll(4));}
  if(t.Contains("upper-groan")&&actor.Hp<actor.MaxHp)UpperReinforce(d,s,Roll(4));if(t.Contains("upper-slogan"))ChangeTension(1,"Fitness cultist slogans");
  if(t.Contains("upper-cobbler")&&!s.Events.Contains("cobbler-call")){s.Events.Add("cobbler-call");s.NextHazardMinute=CampaignMinutes+1;}
  if(t.Contains("upper-drone")){var nearest=Actors.Where(a=>a!=actor&&a.Alive&&a.Position.Distance(actor.Position)<=6&&Line(a.Position,actor.Position)).OrderBy(a=>a.Position.Distance(actor.Position)).FirstOrDefault();if(nearest!=null&&nearest.Enemy){Hurt(nearest,Roll(4),DamageType.Lightning,actor);actor.Economy.Spend(ActionResourceKind.Action);}}
 }
 void TickUpperMall25()
 {
  var d=CurrentLowerMallSite;if(d?.Level!="upper"||d.DisplayNumber>18)return;var s=LowerMallProgress(d.Number);if(s==null)return;int n=d.DisplayNumber;
  if(n==8&&!s.Secured&&CampaignMinutes>=s.NextEventMinute){s.NextEventMinute=CampaignMinutes+10;if(!s.Events.Contains("alarm-disabled")&&Roll(5)==1)UpperAlarm(d,s,Roll(6),Roll(6));}
  if(n==8&&!s.Secured&&CampaignMinutes>=s.NextHazardMinute){s.NextHazardMinute=CampaignMinutes+15;int roll=Roll(6);Message(MessageCategory.Narrative,"SuperSave fifteen-minute patrol result: "+roll);if(roll<=3)UpperReinforce(d,s,roll==1?Roll(8):roll==3?RollDamage(2,4):1);else if(roll==4)UpperAlarm(d,s,5,Roll(4));else if(roll==5)SpawnChapterNamed(d,s,"Mutant Rat Swarm");}
  if(n==3&&!s.Events.Contains("carousel-disabled")&&!s.Secured&&CampaignMinutes>=s.NextEventMinute){s.NextEventMinute=CampaignMinutes+10;UpperAlarm(d,s,5,0);}
  if(n==16&&s.Events.Contains("cobbler-call")&&!s.Events.Contains("cobbler-arrived")&&CampaignMinutes>=s.NextHazardMinute){s.Events.Add("cobbler-arrived");UpperReinforce(d,s,Roll(4));}
  if(n==5&&s.HarvestMinute>=0&&CampaignMinutes>=s.HarvestMinute&&!s.Events.Contains("meat-expired")){s.Events.Add("meat-expired");foreach(var stack in Actors.Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).SelectMany(a=>a.Inventory.Items).Concat(Features.SelectMany(f=>f.Contents.Items)).Concat(StoredTravelAreas.SelectMany(a=>a.Features).SelectMany(f=>f.Contents.Items)).Where(i=>i.ItemId=="item.upper.05.loot.2"))stack.Wear=100;}
 }
 void Upper25Gunshot(Actor actor)
 {
  var d=CurrentLowerMallSite;if(d?.Level!="upper"||d.DisplayNumber>18)return;var s=LowerMallProgress(d.Number)!;s.Quiet=false;
  if(d.DisplayNumber==2){SpawnChapterNamed(d,s,"Shambler",2);SpawnChapterNamed(d,s,"Runner");}
  if(d.DisplayNumber==12&&s.Events.Contains("gas-active")&&!s.Events.Contains("gas-exploded")){s.Events.Add("gas-exploded");s.Events.Remove("gas-active");foreach(var p in Actors.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=2).ToArray())Hurt(p,SurvivorSaveDamage(p,RollDamage(2,8),Ability.Dexterity,SurvivorSave(p,Ability.Dexterity,13,"fuel blast"),true),DamageType.Fire);ChangeTension(15,"Fuel canister explosion");}
  else ChangeTension(d.DisplayNumber==5?10:5,"Upper-mall gunfire");
 }
 void SecureUpper25(LowerMallSite d,LowerMallState s)
 {
  if(d.Level!="upper"||d.DisplayNumber>18)return;if(LowerMallStates.Count(s=>s.Number is >=101 and <=109&&s.Secured)==9)RunCampaignEvent("event.upper.first-nine");if(LowerMallStates.Count(s=>s.Number is >=110 and <=118&&s.Secured)==9)RunCampaignEvent("event.upper.last-nine");if(LowerMallStates.Count(s=>s.Number>=101&&s.Secured)==18)RunCampaignEvent("event.upper.all-eighteen");
 }
 string Upper25ItemReason(Actor a,ItemStack stack)
 {
  string effect=stack.Definition.Equipment?.Family??"";if(!effect.StartsWith("upper-"))return "";
  if(effect is "upper-optolaser" or "upper-torch"&&!Actors.Any(t=>t.Enemy&&t.Alive&&t.Position.Distance(a.Position)<=(effect=="upper-optolaser"?6:2)&&Line(a.Position,t.Position)))return "Bring a visible hostile target within the device’s range.";
  if(effect=="upper-meat"&&LowerMallProgress(105) is {} meat&&meat.HarvestMinute>=0&&CampaignMinutes>=meat.HarvestMinute)return "This deli meat has spoiled after three campaign days.";
  if(effect=="upper-energy"&&a.Character.HasCondition("upper-energy-used"))return "One energy drink benefit per survivor per day.";
  if(effect=="upper-laserpointer"&&a.Character.HasCondition("upper-pointer-used"))return "The laser pointer has already been used today.";return "";
 }
 void UseUpper25Item(Actor actor,ItemStack stack,string effect)
 {
  switch(effect)
  {
   case "upper-meat":actor.Needs.FoodTaken=true;actor.Needs.LastFoodMinute=CampaignMinutes;break;
   case "upper-makeup":LowerEffect(actor,"upper-makeup",86400);break;
   case "upper-concealer":LowerEffect(actor,"upper-concealer",3600);break;
   case "upper-serum":actor.Character.HitPoints.Temporary=Math.Max(5,actor.Character.HitPoints.Temporary);break;
   case "upper-vitamin":LowerEffect(actor,"upper-vitamin",86400);break;
   case "upper-cbd":foreach(var c in new[]{"book-disoriented","chapter-paranoia","upper-paranoia"}){actor.Character.SetCondition(c,c,false);LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition==c);}break;
   case "upper-firstaid":actor.Hp=Math.Min(actor.MaxHp,actor.Hp+Roll(8));break;
   case "upper-heal":actor.Hp=Math.Min(actor.MaxHp,actor.Hp+Roll(6));break;
   case "upper-antibiotic":actor.Character.SetCondition("book-infected","Infection",false);LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition=="book-infected");LowerEffect(actor,"final-antibiotic",86400);break;
   case "upper-energy":actor.Character.HitPoints.Temporary=Math.Max(actor.Character.HitPoints.Temporary,Roll(6));LowerEffect(actor,"upper-energy-used",86400);LowerEffect(actor,"upper-energy",3600);break;
   case "upper-emp":case "upper-empwide":foreach(var target in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=(effect=="upper-empwide"?12:6)&&LowerTraits(t).Contains("cyber")))LowerEffect(target,"stunned",(effect=="upper-empwide"?RollDamage(2,6):Roll(4))*6);break;
   case "upper-optolaser":case "upper-torch":case "upper-aerosol":case "upper-cleaner":foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=(effect=="upper-optolaser"?6:2)).Take(1).ToArray())Hurt(t,Roll(effect=="upper-optolaser"||effect=="upper-aerosol"?4:effect=="upper-torch"?8:6),DamageType.Fire,actor);ChangeTension(5,"Improvised incendiary");break;
   case "upper-powder":foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=2))LowerEffect(t,"blinded",6);break;
   case "upper-audio":EmitLocalNoise(actor,"door",100);ChangeTension(2,"Portable upper-mall audio distraction");break;
   case "upper-laserpointer":LowerEffect(actor,"upper-pointer-used",86400);foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=6))if(!SurvivorSave(t,Ability.Intelligence,13,"laser dot"))LowerEffect(t,"stunned",6);break;
   case "upper-wine":LowerEffect(actor,"upper-morale",3600);break;
   case "upper-candle":LowerEffect(actor,"chapter-morale",3600);break;
  }
 }
}
