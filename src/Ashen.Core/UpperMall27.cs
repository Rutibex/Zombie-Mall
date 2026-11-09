namespace Ashen.Core;
public sealed partial class Actor
{
 int Upper27GearSkill(Skill skill)=>(skill==Skill.Lockpicking&&HasUpper25Gear("Master Lockpick Set")?2:0)+(skill==Skill.Intimidation&&Equipment.Weapon?.ItemId=="item.upper.40.loot.0"?1:0)+(skill==Skill.Mechanics&&Character.HasCondition("upper27-fortification")?2:0);
}
public sealed partial class World
{
 static bool Upper27Site(LowerMallSite? d)=>d?.Level=="upper"&&d.DisplayNumber is >=37 and <=54;
 void EnterUpperMall27(LowerMallSite d,LowerMallState s)
 {
  if(!Upper27Site(d))return;int n=d.DisplayNumber;s.BranchRoll=Roll(6);s.BonusRoll=Roll(4);s.NextEventMinute=CampaignMinutes+10;
  void Trim(int slot,int quantity){var stack=Features.Single(f=>f.Id==$"feature.{d.Prefix}.loot.{slot}").Contents.Items.Single();stack.Quantity=quantity;stack.ResourceUnits=stack.ResourceUnits.Take(quantity).ToList();}
  if(n==43){Trim(1,Roll(6));s.Events.Add("gas-active");}if(n==44){s.BranchRoll=Roll(6);s.BonusRoll=Roll(6);Trim(7,RollDamage(2,6));foreach(var f in Features.Where(f=>f.Id.StartsWith("feature.upper.44.loot.")&&int.Parse(f.Id.Split('.').Last())<6))if(int.Parse(f.Id.Split('.').Last())!=s.BranchRoll-1)f.Contents.Items.Clear();}
  if(n==45)Trim(4,Roll(4));if(n==48)Trim(5,Roll(4));
  if(n==51){int count=Roll(3);var keep=new HashSet<int>();while(keep.Count<count)keep.Add(Roll(6)+5);foreach(var f in Features.Where(f=>f.Id.StartsWith("feature.upper.51.loot.")&&int.Parse(f.Id.Split('.').Last())>=6))if(!keep.Contains(int.Parse(f.Id.Split('.').Last())))f.Contents.Items.Clear();s.Events.Add("flour-active");}
  if(n==37&&s.EncounterRoll==3)s.Events.Add("cyclist-present");if(n==43&&s.EncounterRoll==6||n==47&&s.EncounterRoll==3||n==49&&s.EncounterRoll==5||n==50&&s.EncounterRoll==4||n==52&&s.EncounterRoll==5)s.Events.Add("upper27-survivor");
  if(n==46&&s.EncounterRoll==6)Message(MessageCategory.Narrative,"An ancient root hints at something below the mall. The printed GM-choice result has no defined monster block; it remains an unresolved lead.");
  if(n==53&&s.EncounterRoll>=5)ChangeTension(Roll(6),"Unsettling artisan-store silence");
 }
 void ConfigureUpper27Actors(IEnumerable<string> ids)
 {
  foreach(string id in ids){var a=Actors.Single(a=>a.Id==id);var t=LowerTraits(a);if(t.Contains("final27-lurker")&&LowerMallProgress(154)?.Events.Contains("lurker-revealed")==true){a.Character.SetCondition("hidden","Hidden",false);LowerMallEffects.RemoveAll(e=>e.ActorId==a.Id&&e.Condition=="hidden");}if(t.Contains("final27-puppeteer")||t.Contains("final27-inkspawn"))a.Character.MainAttack.RangeFeet=10;}
 }
 void ReturnUpper27(LowerMallSite d,LowerMallState s){if(Upper27Site(d)&&d.DisplayNumber==37)UpdateUpper27Time();}
 void UpdateUpper27Time()
 {
  var s=LowerMallProgress(137);if(s==null||!s.Events.Contains("cyclist-quarantined")||s.Events.Contains("cyclist-ended"))return;
  if(CampaignMinutes>=s.HarvestMinute&&!s.Events.Contains("cyclist-turned")){s.Events.Add("cyclist-turned");AddStoryJournalEntry("upper27.cyclist.turned","Quarantine failed","The rescued cyclist turned after 24 campaign hours. The storeroom remains sealed until your return.","Upper Mall");}
  if(s.Events.Contains("cyclist-turned")&&CurrentLowerMallSite is {Number:137} d&&!s.Events.Contains("cyclist-released")){SpawnChapterNamed(d,s,"Turned Cyclist");s.Events.Add("cyclist-released");}
 }
 string Upper27Reason(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Upper27Site(d))return "";int n=d.DisplayNumber;string e=a.Effect;
  if(n==44&&a.Kind=="loot"&&a.Id.Split('.').Last() is "0" or "1" or "2" or "3" or "4" or "5"&&s.SearchAttempts.GetValueOrDefault(actor.Id)>=3)return "Each survivor has at most three grocery-aisle search attempts.";
  if(n==37&&e is "upper27-rescue" or "upper27-mercy"&&(!s.Events.Contains("cyclist-present")||s.Events.Contains("cyclist-ended")||s.Events.Contains("cyclist-quarantined")))return "This saved cyclist choice is absent or already resolved.";
  if(n==41&&e is "upper27-answer" or "upper27-hangup"&&(CampaignMinutes<s.EnteredMinute+10||s.Events.Contains("power-cut")))return "The Whispering Line is silent; it rings after ten minutes unless power is isolated.";
  if(a.NpcActorId!=""&&e!="upper27-tame"&&!s.Events.Contains("upper27-survivor"))return "No living survivor appeared on this saved entry branch.";
  if(e=="upper27-tame"&&(!Actors.Any(a=>a.Alive&&a.Enemy&&LowerTraits(a).Contains("final27-alpha"))||!actor.Inventory.Items.Any(i=>Campaign!.Items.Any(d=>d.Id==i.ItemId&&d.SurvivalUse=="food"))))return "Find the living Alpha Stray and carry real food to offer.";
  if(n==49&&e=="upper27-befriend"&&!actor.Inventory.Items.Any(i=>i.Definition.Name.Contains("Battery",StringComparison.OrdinalIgnoreCase)||i.Definition.Name.Contains("Power Bank",StringComparison.OrdinalIgnoreCase)))return "Carry a battery or power bank for the technician.";
  if(e=="upper27-refuel"&&!actor.Inventory.Items.Concat(actor.Equipment.All()).Any(i=>i.ItemId=="item.upper.46.loot.2"))return "Carry the recovered hedge trimmer before refuelling.";
  if(e=="upper27-reveal"&&s.Resolved.Contains($"action.{d.Prefix}.back"))return "The hidden lurker has already been disturbed.";
  return "";
 }
 void Upper27Attempt(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(Upper27Site(d)&&d.DisplayNumber==44&&a.Kind=="loot"&&int.Parse(a.Id.Split('.').Last())<6)s.SearchAttempts[actor.Id]=s.SearchAttempts.GetValueOrDefault(actor.Id)+1;
 }
 void Upper27Hazard(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Upper27Site(d))return;int n=d.DisplayNumber,j=int.Parse(a.Id.Split('.').Last());
  if(n==37&&j==3)SpawnChapterNamed(d,s,"Ride Stalker");
  if(n==38&&j==0&&Roll(2)==1)UpperReinforce(d,s,Roll(6));if(n==38&&j==3||n==45&&j==1||n==52&&j==2){s.Events.Add("alarm-pending");s.NextHazardMinute=CampaignMinutes+1;}
  if(n==39&&j==0&&Roll(2)==1){s.Events.Add("alarm-pending");s.NextHazardMinute=CampaignMinutes+1;}
  if(n==46&&j==2)SpawnChapterNamed(d,s,"Child Crawler",2);
  if(n==50&&j==2&&Roll(4)==1)UpperReinforce(d,s,Roll(4));
  if(n==51&&j==0)s.Events.Add("flour-active");if(n==54&&j==0)SpawnChapterNamed(d,s,"Showroom Lurker");
  if(a.TensionDice>0)s.Quiet=false;
 }
 void Upper27Milestone(LowerMallSite d,LowerMallState s,string key)
 {
  var a=d.Actions.FirstOrDefault(a=>a.Id.EndsWith("milestone."+key));if(a!=null&&!s.Resolved.Contains(a.Id)){s.Resolved.Add(a.Id);CreditLower(s,a.Id,a.Xp);}
 }
 void Upper27Device(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(!Upper27Site(d))return;int n=d.DisplayNumber;string e=a.Effect;
  if(a.Kind=="loot")
  {
   if(n==43)ChangeTension(Roll(4),"Searching Fine Cuts");
   if(n==49&&s.EncounterRoll==3&&!s.Events.Contains("outside-wave")){s.Events.Add("outside-wave");UpperReinforce(d,s,Roll(6));}
   if(n==51&&d.Actions.Where(a=>a.Kind=="loot"&&a.Id.Split('.').Last() is "1" or "2" or "7").All(a=>s.Resolved.Contains(a.Id)||Features.Single(f=>f.Id==a.FeatureId).Contents.Items.Count==0))Upper27Milestone(d,s,"food");
  }
  switch(e)
  {
   case "upper27-rescue":if(n==37){s.Events.Add("cyclist-quarantined");s.HarvestMinute=CampaignMinutes+1440;Message(MessageCategory.Narrative,"You free the cyclist from the chains, recover the maintenance-map lead, and establish a sealed storeroom quarantine. The infection will turn them within 24 hours.");}else {LowerRecruit(a,s);if(n==43)ChangeTension(-Roll(4),"Freezer survivor rescued");}break;
   case "upper27-mercy":s.Events.Add("cyclist-ended");break;
   case "upper27-freezer":if(n==39)UpperReinforce(d,s,1);if(n==44&&s.EncounterRoll==5)SpawnChapterNamed(d,s,"Freezer Lurker",Roll(3)+1);break;
   case "upper27-arcade":LowerEffect(actor,"upper-morale",86400);break;
   case "upper27-answer":s.Events.Add("line-answered");AddStoryJournalEntry("upper27.whispering-line","The Whispering Line","A whisper repeats a familiar voice. The telephone connection remains unexplained.","Upper Mall");break;
   case "upper27-hangup":ChangeTension(RollDamage(2,6),"Whispering Line disconnected");break;
   case "upper27-power":s.Events.Add("power-cut");ChangeTension(-Roll(4),"Cameras and phone line isolated");break;
   case "upper27-usb":SetDialogueFlag(n==54?"flag.upper.54.project-z-decoded":"flag.upper.41.frequency-decoded");break;
   case "upper27-gas":s.Events.Remove("gas-active");s.Events.Add("gas-disabled");break;
   case "upper27-trap":ConsumeUpper25(actor,"Slicer Trap Parts");s.Events.Add("slicer-trap");break;
   case "upper27-refuel":var trimmer=actor.Inventory.Items.Concat(actor.Equipment.All()).First(i=>i.ItemId=="item.upper.46.loot.2");ConsumeUpper25(actor,"Gasoline (per gallon)");trimmer.Charges=5;break;
   case "upper27-predict":LowerEffect(actor,"upper27-predicted",3600);break;
   case "upper27-fire":s.Events.Add("infestation-burned");s.Quiet=false;ChangeTension(RollDamage(2,6),"Runaway plant-store blaze");foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive).ToArray())Hurt(t,RollDamage(3,6),DamageType.Fire,actor);break;
   case "upper27-light":s.Events.Add("direct-light");foreach(var t in Actors.Where(t=>t.Alive&&LowerTraits(t).Contains("final27-inkspawn")))t.BaseAc=12;break;
   case "upper27-clean":s.Events.Add("ink-cleaned");break;
   case "upper27-manual":LowerEffect(actor,"upper27-fortification",0);break;
   case "upper27-befriend":var battery=actor.Inventory.Items.First(i=>i.Definition.Name.Contains("Battery",StringComparison.OrdinalIgnoreCase)||i.Definition.Name.Contains("Power Bank",StringComparison.OrdinalIgnoreCase));battery.ConsumeResourceUnits(1);if(--battery.Quantity==0)actor.Inventory.Items.Remove(battery);LowerRecruit(a,s);break;
   case "upper27-trainer":LowerRecruit(a,s);break;
   case "upper27-tame":OfferUpper26Ration(actor);PacifyUpper26("Alpha Stray");LowerRecruit(a,s);s.Events.Add("alpha-tamed");break;
   case "upper27-upgrade":ConsumeUpper25(actor,"Kevlar Fabric");LowerEffect(actor,"upper-reinforced",0);break;
   case "upper27-reveal":s.Events.Add("lurker-revealed");break;
   case "upper27-barricade":s.Events.Add("barricade-built");ChangeTension(-Roll(6),"Furniture barricade");break;
   case "upper27-quiet":ChangeTension(-2,"Quiet showroom sweep");break;
  }
 }
 bool Upper27Advantage(Actor attacker,Actor target)=>LowerTraits(attacker).Contains("final27-pack")&&Actors.Any(a=>a!=attacker&&a.Enemy&&a.Alive&&LowerTraits(a).Contains("final27-pack")&&a.Position.Distance(target.Position)<=1.5f);
 bool Upper27Disadvantage(Actor attacker,Actor target)=>LowerTraits(target).Contains("final27-child")&&!attacker.Character.HasCondition("upper27-predicted")||LowerTraits(target).Contains("final27-collector")&&attacker.Range>1&&attacker.Position.Distance(target.Position)>2||LowerTraits(target).Contains("final27-demon")&&attacker.Character.Abilities.Dexterity<16;
 int Upper27Damage(Actor target,int amount,DamageType type,Actor? source)
 {
  var tags=LowerTraits(target);if(tags.Contains("final27-cyber")&&source?.Equipment.Weapon?.Definition.MagazineSize>0)amount/=2;
  if(source?.Enemy==true&&source.Range<=1&&target.Equipment.All().Any(i=>i.ItemId=="item.upper.48.loot.2"))amount=Math.Max(0,amount-1);
  if(source?.Equipment.Weapon?.ItemId=="item.upper.43.loot.0"&&target.Enemy&&!tags.Contains("human"))amount++;
  return amount;
 }
 void Upper27DamageResolved(Actor target,int damage,DamageType type,Actor? source,bool wasAlive,bool critical)
 {
  var d=CurrentLowerMallSite;if(!Upper27Site(d))return;var s=LowerMallProgress(d!.Number)!;var tags=LowerTraits(target);
  if(damage>0&&type==DamageType.Fire)IgniteUpper27(source??target);
  if(damage>0&&type==DamageType.Lightning&&tags.Contains("final27-sales")&&!s.Events.Contains("sales-feedback")){s.Events.Add("sales-feedback");foreach(var p in Actors.Where(a=>a!=target&&a.Alive&&a.Position.Distance(target.Position)<=2).ToArray())Hurt(p,Roll(8),DamageType.Lightning,target);}
  if(damage>0&&type==DamageType.Fire&&tags.Contains("final27-wax")&&!target.Character.HasCondition("upper27-burning"))LowerEffect(target,"upper27-burning",60);
  if(damage>0&&source!=null&&target.Alive)
  {
   var st=LowerTraits(source);if(st.Contains("final27-paper")&&!SurvivorSave(target,Ability.Wisdom,13,"mental paranoia"))LowerEffect(target,"upper-paranoia",3600);
   if(st.Contains("final27-inkspawn"))LowerEffect(target,"book-disoriented",6);
   if(st.Contains("final27-alpha")&&!SurvivorSave(target,Ability.Strength,15,"alpha knockdown"))LowerEffect(target,"prone",6);
   if(st.Contains("final27-revenant")&&critical)LowerEffect(target,"upper27-limb-wound",0);
   if(st.Contains("final27-demon")&&critical)source.Hp=Math.Min(source.MaxHp,source.Hp+5);
   if(source.Equipment.Weapon?.ItemId=="item.upper.42.loot.5"&&!SurvivorSave(target,Ability.Constitution,13,"stun gun"))LowerEffect(target,"stunned",6);
  }
  if(wasAlive&&!target.Alive)
  {
   var definition=Campaign!.LowerMall!.Monsters.FirstOrDefault(m=>m.ActorId==target.Id);if(definition?.RewardXp>0)CreditLower(s,"defeat:"+target.Id,definition.RewardXp);
   if(tags.Contains("final27-gourmand")||tags.Contains("final27-fungal"))foreach(var p in Actors.Where(p=>p!=target&&p.Alive&&p.Position.Distance(target.Position)<=2).ToArray()){int hit=tags.Contains("final27-gourmand")?Roll(8):Roll(6);bool pass=SurvivorSave(p,tags.Contains("final27-gourmand")?Ability.Dexterity:Ability.Constitution,14,"death cloud");if(!pass){Hurt(p,hit,DamageType.Poison,target);if(tags.Contains("final27-fungal"))LowerEffect(p,"chapter-sick",60);}else if(tags.Contains("final27-gourmand"))Hurt(p,hit/2,DamageType.Poison,target);}
   if(tags.Contains("final27-revenant")||tags.Contains("final27-collector")||tags.Contains("final27-grocery"))ChangeTension(RollDamage(2,6),"Dying shop horror");
   if(tags.Contains("final27-plush")){ChangeTension(1,"Plushling death cry");foreach(var z in Actors.Where(a=>a.Alive&&a.Enemy))LowerEffect(z,"upper27-rush",Roll(4)*6);}
   if(d.DisplayNumber==45&&!Actors.Any(a=>a.Alive&&LowerTraits(a).Contains("final27-child")))Upper27Milestone(d,s,"pack");
   if(source!=null&&LowerTraits(source).Contains("final27-cleave"))source.Hp=Math.Min(source.MaxHp,source.Hp+10);
  }
 }
 void Upper27Turn(Actor actor,LowerMallState s,IReadOnlyList<string> tags)
 {
  var d=CurrentLowerMallSite;if(!Upper27Site(d))return;
  if(tags.Contains("final27-dough"))actor.Hp=Math.Min(actor.MaxHp,actor.Hp+5);
  if(actor.Character.HasCondition("upper27-burning"))Hurt(actor,Roll(6),DamageType.Fire);
  if(tags.Contains("final27-demon"))actor.Economy.AddMovement(actor.EffectiveSpeedFeet);if(actor.Character.HasCondition("upper27-rush"))actor.Economy.AddMovement(10);
  if(tags.Contains("final27-cape")&&Roll(10)==1)LowerEffect(actor,"prone",6);
  if(tags.Contains("final27-monitor")&&!s.Events.Contains("monitor-shriek")){s.Events.Add("monitor-shriek");ChangeTension(RollDamage(2,6),"Class Monitor shriek");s.Events.Add("alarm-pending");s.NextHazardMinute=CampaignMinutes+1;}
  if(tags.Contains("final27-child")&&!s.Events.Contains("children-giggle")){s.Events.Add("children-giggle");foreach(var p in Party.Where(p=>p.Alive))if(!SurvivorSave(p,Ability.Wisdom,13,"fear giggle"))LowerEffect(p,"frightened",6);}
  if(tags.Contains("final27-puppeteer")&&Round%3==0){var corpse=Actors.FirstOrDefault(a=>!a.Alive&&a.Enemy&&a!=actor&&LowerTraits(a).Contains("infection")&&!s.Events.Contains("puppet:"+a.Id));if(corpse!=null){s.Events.Add("puppet:"+corpse.Id);corpse.Hp=corpse.MaxHp;corpse.Character.SetCondition("unconscious","Unconscious",false);AddInitiativeParticipant(corpse,reason:"corpse puppetry");}}
  if(tags.Contains("final27-alpha")&&!Party.Any(p=>p.Alive&&p.Hp<p.MaxHp)){actor.Economy.Spend(ActionResourceKind.Action);actor.Economy.MovementRemainingFeet=0;}
  if(tags.Contains("final27-receiver")&&!s.Events.Contains("receiver-static")){s.Events.Add("receiver-static");ChangeTension(RollDamage(2,6),"Receiver static");foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=2))Hurt(p,Roll(4),DamageType.Lightning,actor);}
  if((tags.Contains("final27-gourmand")||tags.Contains("final27-scorched")||tags.Contains("final27-dough")||tags.Contains("final27-inkspawn"))&&Roll(6)>=5)
  {
   foreach(var p in Party.Where(p=>p.Alive&&p.Position.Distance(actor.Position)<=2&&Line(p.Position,actor.Position)).ToArray())
   {if(tags.Contains("final27-inkspawn")){if(!SurvivorSave(p,Ability.Dexterity,14,"ink burst"))LowerEffect(p,"blinded",Roll(4)*6);}else{int hit=tags.Contains("final27-dough")?RollDamage(3,6):tags.Contains("final27-scorched")?RollDamage(2,6):Roll(8);Hurt(p,hit,tags.Contains("final27-scorched")?DamageType.Fire:DamageType.Acid,actor);if(tags.Contains("final27-gourmand")&&!SurvivorSave(p,Ability.Constitution,13,"gourmand vomit"))LowerEffect(p,"blinded",6);}}actor.Economy.Spend(ActionResourceKind.Action);
  }
  if(s.Events.Contains("slicer-trap")&&!s.Events.Contains("slicer-used")&&actor.Position.Distance(Features.Single(f=>f.Id=="feature.upper.43.device.trap").Pos.Center)<=1.5f){s.Events.Add("slicer-used");Hurt(actor,Roll(10),DamageType.Slashing);}
 }
 void Upper27PartyTurn(Actor actor){if(actor.Character.HasCondition("upper27-limb-wound"))actor.Economy.MovementRemainingFeet=Math.Max(0,actor.Economy.MovementRemainingFeet-10);}
 void Upper27Noise(Actor source,string kind)
 {
  var d=CurrentLowerMallSite;if(!Upper27Site(d))return;var s=LowerMallProgress(d!.Number);if(s==null)return;s.Quiet=false;
  if(d.DisplayNumber==49&&s.EncounterRoll==3&&!s.Events.Contains("outside-wave")){s.Events.Add("outside-wave");UpperReinforce(d,s,Roll(6));}
 }
 void Upper27Gunshot(Actor source){if(Upper27Site(CurrentLowerMallSite)){ChangeTension(5,"Final-gallery gunfire");IgniteUpper27(source);}}
 void IgniteUpper27(Actor source)
 {
  var d=CurrentLowerMallSite;if(!Upper27Site(d))return;var s=LowerMallProgress(d!.Number)!;
  bool gas=s.Events.Contains("gas-active"),flour=s.Events.Contains("flour-active");if((gas||flour)&&!s.Events.Contains("ignition")){s.Events.Add("ignition");s.Events.Remove("gas-active");s.Events.Remove("flour-active");foreach(var p in Actors.Where(p=>p.Alive&&p.Position.Distance(source.Position)<=2).ToArray())Hurt(p,RollDamage(3,6),DamageType.Fire);ChangeTension(RollDamage(gas?3:2,6),"Shop ignition");}
  if(d.DisplayNumber==53&&!s.Events.Contains("store-fire")){s.Events.Add("store-fire");ChangeTension(RollDamage(2,6),"Craft-store fire");}
 }
 void TickUpperMall27()
 {
  UpdateUpper27Time();var d=CurrentLowerMallSite;if(!Upper27Site(d))return;var s=LowerMallProgress(d!.Number);if(s==null)return;
  if(s.Events.Contains("alarm-pending")&&CampaignMinutes>=s.NextHazardMinute){s.Events.Remove("alarm-pending");UpperReinforce(d,s,d.DisplayNumber==39?RollDamage(2,6):d.DisplayNumber==38?Roll(4):Roll(6));}
  if(!s.Secured&&CampaignMinutes>=s.NextEventMinute){s.NextEventMinute=CampaignMinutes+10;if(d.DisplayNumber==44)ChangeTension(Roll(4),"Grocery-store delay");if(d.DisplayNumber==47&&!s.Events.Contains("ink-cleaned")&&Actors.Any(a=>a.Alive&&LowerTraits(a).Contains("final27-paper")))ChangeTension(1,"Uncleaned ink trail");if(d.DisplayNumber==41&&!s.Events.Contains("power-cut")&&!s.Events.Contains("phone-ringing")){s.Events.Add("phone-ringing");Message(MessageCategory.Narrative,"The Whispering Line begins to ring.");}}
 }
 void SecureUpper27(LowerMallSite d,LowerMallState s)
 {
  if(!Upper27Site(d))return;if(d.DisplayNumber is 40 or 49)Upper27Milestone(d,s,"clear");if(d.DisplayNumber is 45 or 49&&s.Quiet)Upper27Milestone(d,s,"quiet");if(d.DisplayNumber is 38 or 44&&s.Quiet)ChangeTension(-Roll(4),"Quiet shop clearance");
  if(LowerMallStates.Count(s=>s.Number is >=137 and <=145&&s.Secured)==9)RunCampaignEvent("event.upper.thirtyseven-fortyfive");if(LowerMallStates.Count(s=>s.Number is >=146 and <=154&&s.Secured)==9)RunCampaignEvent("event.upper.fortysix-fiftyfour");if(LowerMallStates.Count(s=>s.Number is >=101 and <=154&&s.Secured)==54)RunCampaignEvent("event.upper.all-fiftyfour");
 }
 void ValidateUpper27State(LowerMallSite d,LowerMallState s)
 {
  if(!Upper27Site(d))return;if(s.BranchRoll is <1 or >6||s.BonusRoll is <1 or >6||s.LootRolls.Count>0||d.DisplayNumber!=44&&s.SearchAttempts.Count>0)throw new InvalidDataException("Invalid final-gallery rolls or searches.");
  if(d.DisplayNumber==37){bool rescued=s.Resolved.Contains("action.upper.37.device.rescue"),mercy=s.Resolved.Contains("action.upper.37.device.mercy");if(rescued&&mercy||rescued!=s.Events.Contains("cyclist-quarantined")||mercy!=s.Events.Contains("cyclist-ended")||rescued&&(s.HarvestMinute<s.EnteredMinute+1440||s.HarvestMinute>CampaignMinutes+1440)||s.Events.Contains("cyclist-turned")&&(!rescued||CampaignMinutes<s.HarvestMinute))throw new InvalidDataException("Invalid cyclist quarantine ownership.");}
  foreach(var a in d.Actions.Where(a=>a.Kind=="reference"&&a.Xp>0&&s.Resolved.Contains(a.Id))){string key=a.Id.Split('.').Last();if(key=="quiet"&&(!s.Secured||!s.Quiet)||key=="clear"&&!s.Secured||key=="pack"&&allLivingChildren())throw new InvalidDataException("Unearned final-gallery milestone.");}
  bool allLivingChildren()=>Actors.Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).Any(a=>a.Alive&&a.Id.StartsWith("actor.upper.45.lost-child"));
 }
 string Upper27ItemReason(Actor actor,ItemStack stack)
 {
  string family=stack.Definition.Equipment?.Family??"";
  if(family is "upper27-molotov" or "upper27-emp"&&!Actors.Any(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=3&&Line(actor.Position,t.Position)))return "Find a visible hostile within fifteen feet.";
  return "";
 }
 void UseUpper27Item(Actor actor,ItemStack stack,string effect)
 {
  switch(effect)
  {
   case "upper27-heal":actor.Hp=Math.Min(actor.MaxHp,actor.Hp+Roll(4));break;
   case "upper27-firstaid":actor.Hp=Math.Min(actor.MaxHp,actor.Hp+5);break;
   case "upper27-antibiotic":actor.Character.SetCondition("book-infected","Infection",false);LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition=="book-infected");break;
   case "upper27-morale":LowerEffect(actor,"upper-morale",3600);break;
   case "upper27-energy":actor.Hp=Math.Min(actor.MaxHp,actor.Hp+Roll(4));LowerEffect(actor,"upper27-energy",3600);break;
   case "upper27-audio":EmitLocalNoise(actor,"door",100);break;
   case "upper27-noise":ChangeTension(Roll(6),"Recovered whistle or horn");EmitLocalNoise(actor,"door",100);break;
   case "upper27-diversion":foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=3))LowerEffect(t,"stunned",6);break;
   case "upper27-emp":foreach(var t in Actors.Where(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=3&&Line(actor.Position,t.Position)).ToArray()){if(LowerTraits(t).Contains("final27-receiver"))Hurt(t,t.MaxHp,DamageType.Lightning,actor);else if(LowerTraits(t).Contains("cyber"))LowerEffect(t,"stunned",18);}break;
   case "upper27-molotov":var target=Actors.First(t=>t.Enemy&&t.Alive&&t.Position.Distance(actor.Position)<=3&&Line(actor.Position,t.Position));foreach(var t in Actors.Where(t=>t.Alive&&t.Position.Distance(target.Position)<=1).ToArray())Hurt(t,RollDamage(3,6),DamageType.Fire,actor);EmitLocalNoise(actor,"explosion",150);break;
  }
 }
}
