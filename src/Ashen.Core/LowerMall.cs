using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed partial class World
{
 public List<LowerMallState> LowerMallStates {get;set;}=[];
 public List<LowerMallEffect> LowerMallEffects {get;set;}=[];
 public int LowerMallXpAwarded {get;set;}
 public int LowerMallXpRemainder {get;set;}
 [JsonIgnore] public bool LowerMallEnabled=>Campaign?.LowerMall!=null;
 [JsonIgnore] public LowerMallSite? CurrentLowerMallSite=>Campaign?.LowerMall?.Sites.FirstOrDefault(s=>s.AreaId==CurrentTravelDestinationId);
 [JsonIgnore] public Action<string>? LowerMallOpened {get;set;}
 public LowerMallState? LowerMallProgress(int n)=>LowerMallStates.FirstOrDefault(s=>s.Number==n);
 public bool LowerMallReservedActor(string id)=>Campaign?.LowerMall?.Monsters.Any(m=>m.ActorId==id)==true;
 IReadOnlyList<string> LowerTraits(Actor a)=>Campaign?.LowerMall?.Monsters.FirstOrDefault(m=>m.ActorId==a.Id)?.Traits??(IReadOnlyList<string>)Array.Empty<string>();
 LowerMallState? LowerMonsterState(Actor a)=>LowerMallStates.FirstOrDefault(s=>s.Releases.Any(r=>Campaign!.Encounters.Single(e=>e.Id==r.EncounterId).ActorIds.Take(r.Count).Contains(a.Id)));
 public bool LowerMallContainerReady(Feature f)=>f.Id=="feature.exterior.13.pump-reserve"?false:CurrentLowerMallSite is not {} d||d.Actions.FirstOrDefault(a=>a.FeatureId==f.Id&&a.Kind=="loot") is not {} a||LowerMallProgress(d.Number)?.Resolved.Contains(a.Id)==true;
 void EnterLowerMall(bool arrival=false)
 {
  var d=CurrentLowerMallSite;if(d==null)return;if(LowerMallProgress(d.Number) is {} existing){if(arrival)ReturnUpper27(d,existing);if(arrival)ReturnRoof28(d,existing);if(arrival&&Roof29Site(d))TickRoof29();if(arrival&&Exterior30Site(d))TickExterior30();if(arrival&&d.Number is >=37 and <=54)ReturnLowerMall24(d,existing);if(arrival&&d.Number is 28 or 36)ChapterEvent(PartyLeader,d,existing,"return");return;}var hazards=d.Actions.Where(a=>a.Kind=="hazard").ToArray();var s=new LowerMallState{Number=d.Number,EnteredMinute=CampaignMinutes,EncounterRoll=Roll(d.EncounterSides),HazardId=hazards[Roll(hazards.Length)-1].Id};LowerMallStates.Add(s);SelectFinaleClue(d);
  if(hazards.Single(a=>a.Id==s.HazardId).Effect=="gas")s.Events.Add("gas-active");
  var e=d.Encounters.FirstOrDefault(e=>s.EncounterRoll>=e.Low&&s.EncounterRoll<=e.High);if(e!=null){Message(MessageCategory.Narrative,d.Name+": "+e.Text);if(e.EncounterId!="")SpawnLower(d,s,e.EncounterId,Math.Max(1,RollDamage(e.Dice,e.Die)+e.Bonus),"entry");}
  EnterLowerMall23(d,s);EnterLowerMall24(d,s);EnterUpperMall25(d,s);EnterUpperMall26(d,s);EnterUpperMall27(d,s);EnterRoof28(d,s);EnterRoof29(d,s);EnterExterior30(d,s);EnterExterior31(d,s);
  if(d.Number is 3 or 8 or 13 or 15 or 17&&Roll(2)==1)s.Events.Add("npc-present");
  AddStoryJournalEntry("lower.arrival."+d.Number,d.Name,$"Book pp. {d.Source.PageStart}–{d.Source.PageEnd}. "+d.BookText[..Math.Min(1200,d.BookText.Length)],d.Level=="roof"?"Roof":d.Level=="upper"?"Upper Mall":"Lower Mall");Reveal();
 }
 bool SpawnLower(LowerMallSite d,LowerMallState s,string id,int count,string trigger)
 {
  var enc=Campaign!.Encounters.Single(e=>e.Id==id);if(enc.ActorIds.Count==0)return true;count=Math.Min(count,enc.ActorIds.Count);var record=s.Releases.FirstOrDefault(r=>r.EncounterId==id);int had=record?.Count??0;if(count<=had)return true;
  var ids=enc.ActorIds.Skip(had).Take(count-had).ToArray();var positions=new List<Cell>();foreach(var actor in ids){var home=CurrentCampaignArea!.Actors.Single(p=>p.ActorId==actor).Position;var point=Floors.Where(p=>p.Distance(home)<=6&&!positions.Contains(p)&&CanStand(p.Center,null,true)).OrderBy(p=>p.Distance(home)).ThenBy(p=>p.Y).ThenBy(p=>p.X).Cast<Cell?>().FirstOrDefault();if(!point.HasValue)return false;positions.Add(point.Value);}
  if(record==null){record=new(){EncounterId=id,Trigger=trigger};s.Releases.Add(record);}record.Count=count;
  for(int i=0;i<ids.Length;i++){var a=AddCampaignActor(Campaign.Actors.Single(a=>a.Id==ids[i]),positions[i]);if(a.Name=="The Speed Freak"){a.Equipment.Weapon=CreateItemStack("item.lower.14.raider-gun");a.Equipment.Weapon.LoadedAmmunition=CreateItemStack("item.mall.ammo-9mm",6);a.Inventory.Add(CreateItemStack("item.lower.14.tireiron"));}
   var tags=LowerTraits(a);if(tags.Contains("swarm")||tags.Contains("lifesteal"))a.Character.MainAttack.DamageDice=2;if(tags.Contains("rooted")||tags.Contains("bloom")&&a.Name=="The Blooming One"||a.Name=="Cyber-Zombie")a.Character.MainAttack.RangeFeet=10;if(Mode==Mode.Combat)AddInitiativeParticipant(a,reason:"finite shop reinforcement");}AssignMissingItemIds();ConfigureLower23Actors(ids);ConfigureLower24Actors(ids);ConfigureUpper25Actors(ids);ConfigureUpper26Actors(ids);ConfigureUpper27Actors(ids);ConfigureRoof28Actors(ids);ConfigureExterior30Actors(ids);return true;
 }
 bool LowerCleared(LowerMallState s,string id)
 {
  if(Campaign!.Encounters.Single(e=>e.Id==id).ActorIds.Count==0)return true;var r=s.Releases.FirstOrDefault(r=>r.EncounterId==id);if(r==null)return false;var all=Actors.Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).ToArray();return Campaign!.Encounters.Single(e=>e.Id==id).ActorIds.Take(r.Count).All(id=>all.Any(a=>a.Id==id&&!a.Alive));
 }
 public string LowerMallActionReason(Actor actor,string id,bool force=false)
 {
  var d=CurrentLowerMallSite;var s=d==null?null:LowerMallProgress(d.Number);var a=d?.Actions.FirstOrDefault(a=>a.Id==id);if(a==null||s==null)return "Find this object in its numbered shop.";
  if(!Party.Contains(actor)||!actor.Alive||(Mode!=Mode.Explore&&!(ChapterCombatAction(a)&&Mode==Mode.Combat&&CanAct(actor)))||PendingReaction!=null||ReactionInterruptActive||Party.Any(IsMoving))return "Choose a conscious survivor and finish movement, combat and reactions.";
  var f=Features.Single(f=>f.Id==a.FeatureId);if(!IsFeatureDiscovered(f)||actor.Position.Distance(f.Pos.Center)>1.25f||!Line(actor.Position,f.Pos.Center))return "Stand beside the discovered object; walls and closed doors block interaction.";
  if(a.Kind is "reference" or "event")return "This is a table result, resolved only by its actual rolled event or fruit interaction.";
  if(s.Resolved.Contains(id)&&a.Effect is not ("gate" or "roof-refuel" or "roof-pump" or "roof29-refuel" or "roof29-release" or "roof29-flare" or "roof29-food" or "ext30-pump" or "ext30-close" or "ext31-ride"))return "This finite interaction is already resolved.";
  if(a.Kind=="hazard"&&(s.HazardResolved||s.HazardId!=id))return "Only this journey's rolled entrance hazard is active.";
  if(a.RequiredActionId!=""&&!s.Resolved.Contains(a.RequiredActionId))return "Resolve the prerequisite interaction first.";
  if(a.CostItemId!=""&&!actor.Inventory.Contains(a.CostItemId))return "Bring the recovered "+ResolveItemDefinition(a.CostItemId).Name+" in this backpack.";
  if(a.Effect=="repair"&&!actor.Inventory.Items.Concat(actor.Equipment.All()).Any(i=>i.ItemId==Campaign!.Workshop!.ToolItemId&&i.Wear<100))return "Carry the refuge toolkit to repair this laptop.";
  if(a.Effect=="eddie"&&!s.Events.Contains("npc-present"))return "Eddie was absent on this journey's rolled branch.";
  if(a.NpcActorId!=""&&PartyCount>=MaxPartySize&&!CommunityMembers.Any(p=>p.Id==a.NpcActorId))return "House a survivor at the refuge first; the expedition already has six members.";
  if(a.Effect=="roadwarriors"&&TensionState.Score<50)return "The Road Warriors appear at Tension 50 or higher.";
  if(a.Effect=="ticket"&&s.Events.Contains("alarm"))return "The Last Ticket faded when the alarm sounded.";
  if(a.Effect=="fruit-day"&&(s.FruitMinute<0||CampaignMinutes-s.FruitMinute<1440))return "Survive a full campaign day after accepting the fruit.";
  if(a.Effect=="vent"&&d!.Number==8&&!LowerCleared(s,d.BackEncounterId))return "Clear the Ceiling Creeper before using its vent.";
  var mysteryReason=FinaleClueReason(d!,a);if(mysteryReason!="")return mysteryReason;
  var chapterReason=Lower23Reason(actor,d!,s,a);if(chapterReason!="")return chapterReason;
  var finalReason=Lower24Reason(actor,d!,s,a);if(finalReason!="")return finalReason;
  var upperReason=Upper25Reason(actor,d!,s,a);if(upperReason!="")return upperReason;
  var outerReason=Exterior31Reason(actor,d!,s,a);if(outerReason!="")return outerReason;
  var extReason=Exterior30Reason(actor,d!,s,a);if(extReason!="")return extReason;
  var roof29Reason=Roof29Reason(actor,d!,s,a);if(roof29Reason!="")return roof29Reason;
  var roofReason=Roof28Reason(actor,d!,s,a);if(roofReason!="")return roofReason;
  var upper27Reason=Upper27Reason(actor,d!,s,a);if(upper27Reason!="")return upper27Reason;
  var upper26Reason=Upper26Reason(actor,d!,s,a);if(upper26Reason!="")return upper26Reason;
  if((long)CampaignMinutes+a.Minutes>int.MaxValue||Party.Any(p=>(long)p.Character.Experience+a.Xp+d!.SecureXp>int.MaxValue))return "The campaign time or experience limit would be exceeded.";return "";
 }
 public bool PerformLowerMallAction(Actor actor,string id,bool force=false)
 {
  string reason=LowerMallActionReason(actor,id,force);if(reason!=""){LogSystem(reason,true);return false;}var d=CurrentLowerMallSite!;var s=LowerMallProgress(d.Number)!;var a=d.Actions.Single(a=>a.Id==id);Upper26Attempt(actor,d,s,a);Upper27Attempt(actor,d,s,a);Roof29Attempt(actor,d,s,a);Exterior30Attempt(d,s,a);int dc=force&&a.ForceDc>0?a.ForceDc:a.Dc;if(a.Effect=="ext31-locked"&&actor.Inventory.Items.Any(i=>i.Definition.Name.Contains("Bolt Cutters")))dc=0;if(a.Effect=="final-storage"&&Lower24HasKey(actor,d.Number))dc=0;bool pass=dc==0||SurvivorSkillCheck(actor,force&&a.ForceDc>0?Skill.Athletics:Enum.Parse<Skill>(a.Skill),dc,a.Name);
  if(Mode==Mode.Combat){if(!SpendAction(actor,a.Name))return false;}else {CampaignMinutes+=a.Minutes;ProcessSurvivalTime();}if(force){EmitLocalNoise(actor,"door",60);ChangeTension(Roll(4),"Forced shop lock");}
  if(a.Kind=="hazard"){s.HazardResolved=true;s.Resolved.Add(id);Roof28HazardResult(actor,d,s,a,pass);Roof29HazardResult(actor,d,s,a,pass);Exterior30Hazard(actor,d,s,a,pass);Exterior31Hazard(actor,d,s,a,pass);if(!pass)LowerHazard(actor,d,s,a);else Message(MessageCategory.Narrative,"Avoided: "+a.Name);SyncLowerMall();return true;}
  if(!pass){Exterior31Failure(actor,d,s,a);Roof29Failure(actor,d,s,a);Upper26Failure(actor,d,s,a);Upper25Failure(actor,d,s,a);Lower24Failure(actor,d,s,a);Lower23Failure(actor,d,s,a);if(a.Effect=="speedfreak"){var raider=Campaign!.Encounters.Single(e=>e.AreaId==d.AreaId&&e.ActorIds.Any(id=>Campaign.Actors.Single(a=>a.Id==id).Name=="The Speed Freak"));SpawnLower(d,s,raider.Id,1,"reaction");}
   ChangeTension(Exterior30Site(d)&&d.DisplayNumber==2?1:a.Effect=="quiet"?RollDamage(2,6):Roll(4),"Failed shop interaction");if(a.Effect=="quiet")SpawnLower(d,s,d.BackEncounterId,99,id);return false;}
  if(a.EncounterId!=""&&!SpawnLower(d,s,a.EncounterId,Upper26SpawnCount(d,s,a),id)){LogSystem("Clear the arrival region first.",true);return false;}
  if(!s.Resolved.Contains(id))s.Resolved.Add(id);
  if(a.CostItemId!=""&&a.Effect is "repair" or "flour" or "hermit" or "candles" or "plant" or "emp" or "perfume" or "spray" or "roadwarriors"){var stack=actor.Inventory.Items.First(i=>i.ItemId==a.CostItemId);stack.ConsumeResourceUnits(1);if(--stack.Quantity==0)actor.Inventory.Items.Remove(stack);}
  LowerDevice(actor,d,s,a);LowerDevice23(actor,d,s,a);LowerDevice24(actor,d,s,a);Upper25Device(actor,d,s,a);Upper26Device(actor,d,s,a);Upper27Device(actor,d,s,a);Roof28Device(actor,d,s,a);Roof29Device(actor,d,s,a);Exterior30Device(actor,d,s,a);Exterior31Device(actor,d,s,a);if(a.Kind=="loot"&&d.Number is 16 or 18&&Roll(4)==1)LowerEvent(actor,d,s);AddStoryJournalEntry("lower.action."+id,a.Name,a.Text,"Lower Mall");Message(MessageCategory.Narrative,a.Text);AssignMissingItemIds();SyncLowerMall();Reveal();return true;
 }
 void LowerHazard(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  if(actor.WearsUpper25Gear("Glassproof Steel-Toed Boots")&&(a.Name.Contains("glass",StringComparison.OrdinalIgnoreCase)||a.Name.Contains("shard",StringComparison.OrdinalIgnoreCase))){Message(MessageCategory.Narrative,"Steel-toed boots protect against the broken glass.");return;}Upper26Hazard(actor,d,s,a);Upper27Hazard(actor,d,s,a);Upper25Hazard(actor,d,s,a);Lower23Hazard(actor,d,s,a);LowerHazard24(actor,d,s,a);if(a.DamageDice>0)Hurt(actor,RollDamage(a.DamageDice,a.DamageDie),a.DamageType);if(a.Condition!="")LowerEffect(actor,a.Condition,a.DurationSeconds);if(a.Effect=="exhaustion")actor.Needs.Exhaustion=Math.Min(6,actor.Needs.Exhaustion+1);
  int noise=RollDamage(a.TensionDice,a.TensionDie);ChangeTension(noise,a.Name);if(noise>0){if(!s.Events.Contains("alarm"))s.Events.Add("alarm");EmitLocalNoise(actor,"door",100);}if(a.EncounterId!="")SpawnLower(d,s,a.EncounterId,99,a.Id);Message(MessageCategory.Narrative,"Triggered: "+a.Text);
 }
 bool LowerRecruit(LowerMallAction a,LowerMallState s)
 {
  if(a.NpcActorId==""||CommunityMembers.Any(p=>p.Id==a.NpcActorId))return true;var cells=CampaignTravelRules.ArrivalCells(Floors,Features.Where(f=>f.Kind=="door"&&!f.Used).Select(f=>f.Pos).Concat(CampaignGeometry.SolidCells(CurrentCampaignArea!)),Actors.Where(a=>a.Alive&&a!=PartyLeader).Select(a=>a.Pos),PartyLeader.Pos).Where(p=>p!=PartyLeader.Pos).ToList();if(cells.Count==0||PartyCount>=MaxPartySize)return false;AddCampaignActor(Campaign!.Actors.Single(n=>n.Id==a.NpcActorId),cells[0]);s.Events.Add("rescued:"+a.NpcActorId);AssignMissingItemIds();return true;
 }
 void LowerDevice(Actor actor,LowerMallSite d,LowerMallState s,LowerMallAction a)
 {
  switch(a.Effect)
  {
   case "alarm":s.HazardResolved=true;if(!s.Events.Contains("alarm-disabled"))s.Events.Add("alarm-disabled");break;
   case "gas":s.HazardResolved=true;s.Events.Remove("gas-active");if(!s.Events.Contains("gas-disabled"))s.Events.Add("gas-disabled");break;
   case "gate":foreach(var exit in Features.Where(f=>f.Kind=="transition"))exit.Locked=!exit.Locked;break;
   case "espresso":actor.Character.HitPoints.Temporary=Math.Max(1,actor.Character.HitPoints.Temporary);ChangeTension(1,"Espresso machine");break;
   case "steam":foreach(var e in Actors.Where(e=>e.Enemy&&e.Alive&&e.Position.Distance(actor.Position)<=3).ToArray())Hurt(e,RollDamage(2,6),DamageType.Fire,actor);EmitLocalNoise(actor,"weapon",100);break;
   case "emp":case "perfume":case "light":case "flour":foreach(var e in Actors.Where(e=>e.Enemy&&e.Alive&&e.Position.Distance(actor.Position)<=(a.Effect=="emp"?6:3)&&Line(actor.Position,e.Position)).ToArray()){if(a.Effect=="flour")LowerEffect(e,"blinded",6);else if(a.Effect=="emp"&&LowerTraits(e).Contains("cyber")||a.Effect=="perfume"&&e.Name=="The Last Employee"||a.Effect=="light"&&e.Name=="The Optometrist")LowerEffect(e,"stunned",6);}break;
   case "spray":LowerEffect(actor,"book-stealth",Roll(6)*3600);break;
   case "chime":LowerEffect(actor,"book-warning",86400);break;
   case "candles":s.Events.Add("scent-protected");ChangeTension(-Roll(6),"Scent protection");break;
   case "bike":case "roadwarriors":LowerEffect(actor,"book-bike",3600);break;
   case "study":actor.Character.SetCondition("book-studied","Studied Backyard Apocalypse",true);break;
   case "speakers":case "drone":EmitLocalNoise(actor,"door",120,Features.Single(f=>f.Id==a.FeatureId).Pos);ChangeTension(2,"Audio distraction");break;
   case "water":{string id=Campaign!.Items.First(i=>i.SurvivalUse=="water").Id;var probe=new ItemStack{ItemId=id,CampaignGear=ResolveItemDefinition(id),Quantity=1};if(actor.Inventory.CanAdd(probe))actor.Inventory.Add(CreateItemStack(id));break;}
   case "fruit":s.FruitMinute=CampaignMinutes;int roll=Roll(4);s.Events.Add("fruit:"+roll);if(roll==1){actor.Character.Abilities.Strength+=2;actor.Character.HitPoints.Maximum=Math.Max(1,actor.MaxHp-1);actor.Hp=Math.Min(actor.Hp,actor.MaxHp);}else if(roll==2)actor.Character.SetCondition("book-darkvision","Vision of the Bloom",true);else if(roll==3){actor.Hp=actor.MaxHp;actor.Character.Abilities.Charisma=Math.Max(1,actor.Character.Abilities.Charisma-1);}else LowerEffect(actor,"book-seeded",Roll(6)*86400);break;
   case "plant":case "scout":s.HarvestMinute=CampaignMinutes+1440;break;
   case "freezer":SpawnLower(d,s,d.BackEncounterId,99,a.Id);ChangeTension(Roll(6),"Freezer opened");break;
   case "eddie":if(Roll(2)==1){s.Events.Add("eddie-infected");SpawnLower(d,s,d.BackEncounterId,99,a.Id);Message(MessageCategory.Narrative,"Eddie was restrained for a reason: the infection is advancing.");}else LowerRecruit(a,s);break;
   case "npc":case "entity":LowerRecruit(a,s);if(d.Number==17)SetDialogueFlag("flag.lower.security-codes");break;
   case "speedfreak":case "looter":case "hoarder":case "hermit":case "gardener":SetDialogueFlag($"flag.lower.{d.Number}.negotiated");Message(MessageCategory.Narrative,a.Effect=="gardener"?"The Last Gardener hides bite marks and shares botanical knowledge before withdrawing.":"The survivor accepts a cautious exchange; their information is recorded with the shop entry.");break;
   case "turnoff":s.Events.Remove("haunted-active");break;
   case "mirror":if(Roll(6)==6)LowerEffect(actor,"book-disoriented",60);break;
   case "vent":foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=true;break;
   case "camera":case "usb":case "record":SetDialogueFlag($"flag.lower.{d.Number}.evidence");break;
  }
 }
 void LowerEvent(Actor actor,LowerMallSite d,LowerMallState s)
 {
  var choices=d.Actions.Where(a=>a.Kind=="event").ToArray();if(choices.Length==0)return;var e=choices[Roll(choices.Length)-1];string key="event:"+e.Id;if(s.Events.Contains(key))return;s.Events.Add(key);Message(MessageCategory.Narrative,e.Text);AddStoryJournalEntry("lower.random."+e.Id,e.Name,e.Text,"Lower Mall");if(e.Text.Contains("PA System")){ChangeTension(RollDamage(2,4),"Automated PA");EmitLocalNoise(actor,"door",150);}if(e.Text.Contains("Alarm")){ChangeTension(Roll(6),e.Name);EmitLocalNoise(actor,"door",120);var pool=d.Encounters.FirstOrDefault(e=>e.EncounterId!="");if(pool!=null)SpawnLower(d,s,pool.EncounterId,Roll(6),"event");}
 }
 void CreditLower(LowerMallState s,string key,int amount)
 {
  if(s.Credits.Contains(key))return;int count=Party.Count(p=>!p.Dead);if(count==0)return;int pool=LowerMallXpRemainder+amount,each=pool/count;if(Party.Any(p=>(long)p.Character.Experience+each>int.MaxValue))throw new InvalidOperationException("Location XP overflow.");s.Credits.Add(key);foreach(var p in Party.Where(p=>!p.Dead))p.Character.Experience+=each;LowerMallXpAwarded+=each*count;LowerMallXpRemainder=pool%count;Message(MessageCategory.Loot,$"Location milestone: {amount} XP; {each} each, pool {LowerMallXpRemainder}.");
 }
 void SyncLowerMall()
 {
  if(!LowerMallEnabled)return;foreach(var s in LowerMallStates){var d=Campaign!.LowerMall!.Sites.Single(d=>d.Number==s.Number);foreach(var a in d.Actions.Where(a=>a.Xp>0&&s.Resolved.Contains(a.Id))){if(a.EncounterId!=""&&(!LowerCleared(s,a.EncounterId)||LowerMallEffects.Any(e=>e.Condition=="final-manager-revive"&&Campaign!.Encounters.Single(x=>x.Id==a.EncounterId).ActorIds.Contains(e.ActorId)))||a.Effect=="eddie"&&s.Events.Contains("eddie-infected")||a.Effect is "plant" or "scout"&&CampaignMinutes<s.HarvestMinute)continue;if(d.Number==50&&a.Kind=="lore"&&d.Actions.Where(a=>a.Kind=="lore").Any(a=>!s.Resolved.Contains(a.Id)))continue;CreditLower(s,a.Id,a.Xp);}
   if(s.Number<37&&s.HarvestMinute>=0&&CampaignMinutes>=s.HarvestMinute&&!s.Events.Contains("harvested")){s.Events.Add("harvested");var stock=ShelterHomeStockpile();if(stock!=null){string food=Campaign.Items.First(i=>i.SurvivalUse=="food").Id;var probe=new ItemStack{ItemId=food,CampaignGear=ResolveItemDefinition(food),Quantity=1};if(stock.Contents.CanAdd(probe))stock.Contents.Add(CreateItemStack(food));}AddStoryJournalEntry("lower.harvest."+s.Number,"A full day of work","A first finite crop/scout shift completes after 24 campaign hours, without new scavenging provenance.","Lower Mall");}}
 }
 public string SecureLowerMallReason(Actor actor)
 {
  var d=CurrentLowerMallSite;var s=d==null?null:LowerMallProgress(d.Number);if(d==null||s==null)return "Enter a numbered shop first.";if(s.Secured)return "Already secured; this reward cannot repeat.";if(Mode!=Mode.Explore||PendingReaction!=null||Party.Any(a=>!a.Alive||IsMoving(a)))return "Recover the crew and finish movement/combat.";var f=Features.Single(f=>f.Id==$"feature.{d.Prefix}.secure");if(actor.Position.Distance(f.Pos.Center)>1.25f||!Line(actor.Position,f.Pos.Center))return "Return to the securing point beside the entrance.";var outerReason=Exterior31SecureReason(d,s);if(outerReason!="")return outerReason;var extReason=Exterior30SecureReason(d,s);if(extReason!="")return extReason;var roof29Reason=Roof29SecureReason(d,s);if(roof29Reason!="")return roof29Reason;var roofReason=Roof28SecureReason(d,s);if(roofReason!="")return roofReason;var finalReason=Lower24SecureReason(d,s);if(finalReason!="")return finalReason;if(!s.HazardResolved)return "Resolve the rolled entrance hazard.";if(s.Events.Contains("gas-active"))return "Isolate the active gas leak first.";if(d.Number==22&&s.Events.Contains("toxic-active"))return "Ventilate the rotting fridge first.";if(Actors.Any(a=>!a.Alive&&LowerTraits(a).Contains("minute-revive")&&!s.Events.Contains("burn:"+a.Id)))return "Destroy the garage mannequin with fire before securing.";if(!s.Resolved.Contains($"action.{d.Prefix}.back")||!LowerCleared(s,d.BackEncounterId)||Actors.Any(a=>a.Enemy&&a.Alive))return "Investigate and clear the shop’s remaining threats, including the back room.";if((long)CampaignMinutes+10>int.MaxValue||Party.Any(p=>(long)p.Character.Experience+d.SecureXp>int.MaxValue))return "Campaign time or XP limit reached.";return "";
 }
 public bool SecureLowerMall(Actor actor)
 {
  string reason=SecureLowerMallReason(actor);if(reason!=""){LogSystem(reason,true);return false;}var d=CurrentLowerMallSite!;var s=LowerMallProgress(d.Number)!;s.Secured=true;CreditLower(s,"secure",d.SecureXp);CampaignMinutes+=10;foreach(var door in Features.Where(f=>f.Kind=="door"))door.Used=false;RunCampaignEvent($"event.{d.Prefix}.secured");ChangeTension(-Roll(6),"Shop secured");ProcessSurvivalTime();AddStoryJournalEntry("lower.secured."+d.Number,d.Name+" secured","Threats, hazards and access points secured; milestone credited once.","Lower Mall");if(LowerMallStates.Count(s=>s.Number<=9&&s.Secured)==9)RunCampaignEvent("event.lower.first-nine");if(LowerMallStates.Count(s=>s.Number>=10&&s.Number<=18&&s.Secured)==9)RunCampaignEvent("event.lower.last-nine");if(Campaign!.SchemaVersion>=21){if(LowerMallStates.Count(s=>s.Number>=19&&s.Number<=27&&s.Secured)==9)RunCampaignEvent("event.lower.nineteen-twentyseven");if(LowerMallStates.Count(s=>s.Number>=28&&s.Number<=36&&s.Secured)==9)RunCampaignEvent("event.lower.twentyeight-thirtysix");}SecureLower23(actor,d,s);SecureLower24(actor,d,s);SecureUpper25(d,s);SecureUpper26(d,s);SecureUpper27(d,s);SecureRoof28(d,s);SecureRoof29(d,s);SecureExterior30(d,s);SecureExterior31(d,s);SyncCampaignObjectives();return true;
 }
 void TickLowerMall()
 {
  if(!LowerMallEnabled)return;EnterLowerMall();UpdateLowerEffects();TickLowerMall23();TickLowerMall24();TickUpperMall25();TickUpperMall26();TickUpperMall27();TickRoof28();TickRoof29();TickExterior30();var d=CurrentLowerMallSite;var s=d==null?null:LowerMallProgress(d.Number);if(s!=null&&!s.HazardResolved&&Mode==Mode.Explore&&Party.FirstOrDefault(a=>a.Alive&&a.Pos.Y<23) is {} actor){var h=d!.Actions.Single(a=>a.Id==s.HazardId);s.HazardResolved=true;s.Resolved.Add(h.Id);bool pass=SurvivorSave(actor,Enum.Parse<Ability>(h.Save),Math.Max(12,h.Dc),h.Name);Roof29HazardResult(actor,d,s,h,pass);Exterior30Hazard(actor,d,s,h,pass);if(!pass)LowerHazard(actor,d,s,h);}SyncLowerMall();
 }
 void LowerEffect(Actor actor,string condition,int seconds)
 {
  if(LowerTraits(actor).Contains("final-immovable")&&condition is "restrained" or "book-grappled"||LowerTraits(actor).Contains("final-eyeless")&&condition=="blinded")return;
  actor.Character.SetCondition(condition,condition,true,"Lower mall");LowerMallEffects.RemoveAll(e=>e.ActorId==actor.Id&&e.Condition==condition);LowerMallEffects.Add(new(){ActorId=actor.Id,Condition=condition,EndsAt=seconds==0?0:CampaignMinutes*60d+CampaignSeconds+seconds});
 }
 void UpdateLowerEffects()
 {
  if(!LowerMallEnabled)return;var all=CommunityMembers.Concat(Actors.Where(a=>a.Enemy)).Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).ToArray();foreach(var e in LowerMallEffects.Where(e=>e.EndsAt>0&&e.EndsAt<=CampaignMinutes*60d+CampaignSeconds).ToArray()){var a=all.FirstOrDefault(a=>a.Id==e.ActorId);if(a!=null){ExpireLower24(a,e);if(e.Condition=="ext30-energy")a.Needs.Exhaustion=Math.Min(6,a.Needs.Exhaustion+1);if(e.Condition=="chapter-revive"&&LowerMonsterState(a) is {} state&&!state.Events.Contains("burn:"+a.Id)){a.Hp=a.MaxHp;a.Character.SetCondition("unconscious","Unconscious",false);if(Mode==Mode.Combat)AddInitiativeParticipant(a,reason:"source revival");}a.Character.SetCondition(e.Condition,e.Condition,false);if(e.Condition=="book-seeded"&&!SurvivorSave(a,Ability.Constitution,15,"infection"))a.Character.SetCondition("book-vines","Seeded flesh sprouts vines",true);}LowerMallEffects.Remove(e);}
 }
 void ValidateLowerMall()
 {
  if(LowerMallStates==null||LowerMallEffects==null||LowerMallStates.Any(s=>s==null)||LowerMallEffects.Any(e=>e==null)||LowerMallStates.Count>(Campaign?.LowerMall?.Sites.Count??0)||LowerMallStates.Select(s=>s.Number).Distinct().Count()!=LowerMallStates.Count||LowerMallXpAwarded<0||LowerMallXpRemainder is <0 or >5)throw new InvalidDataException("Invalid lower-mall state.");
  if(!LowerMallEnabled){if(LowerMallStates.Count+LowerMallEffects.Count+LowerMallXpAwarded+LowerMallXpRemainder>0)throw new InvalidDataException("Lower-mall state requires opt-in.");return;}
  var all=Actors.Concat(StoredTravelAreas.SelectMany(a=>a.Actors)).ToArray();var expected=new HashSet<string>();long xp=0;
  foreach(var s in LowerMallStates)
  {
   var d=Campaign!.LowerMall!.Sites.FirstOrDefault(d=>d.Number==s.Number);if(d==null||s.EnteredMinute<Campaign.StartMinute||s.EnteredMinute>CampaignMinutes||(s.EncounterRoll<1||s.EncounterRoll>d.EncounterSides)||!d.Actions.Any(a=>a.Id==s.HazardId&&a.Kind=="hazard")||s.Resolved==null||s.Credits==null||s.Releases==null||s.Events==null||s.Resolved.Distinct().Count()!=s.Resolved.Count||s.Credits.Distinct().Count()!=s.Credits.Count||s.Events.Distinct().Count()!=s.Events.Count||s.Events.Count>512||s.BonusRoll is <0 or >6||s.EchoRoll is <0 or >6||s.CarLimit is <0 or >4||s.CarAttempts<0||s.CarAttempts>s.CarLimit||s.FoodCycle is <0 or >4||s.Number!=28&&s.FoodCycle!=0||s.Number!=34&&(s.CarLimit!=0||s.CarAttempts!=0)||s.Number!=31&&s.EchoRoll!=0||s.NextEventMinute< -1||s.StockRoll is <0 or >100||s.BranchRoll is <0 or >6||s.NextHazardMinute< -1||s.DailyUses==null||s.DailyUses.Count>24||s.DailyUses.Any(k=>k.Value<0||k.Value>CampaignMinutes/1440||k.Key.Length>160)||s.Resolved.Any(id=>!d.Actions.Any(a=>a.Id==id)))throw new InvalidDataException("Invalid location progress, actions or rolled branch.");
   ValidateUpper26State(d,s);ValidateUpper27State(d,s);ValidateRoof28State(d,s);ValidateExterior30(d,s);ValidateExterior31(d,s);
   if(s.Releases.Select(r=>r.EncounterId).Distinct().Count()!=s.Releases.Count)throw new InvalidDataException("Duplicate encounter release.");
   foreach(var r in s.Releases){var e=Campaign.Encounters.FirstOrDefault(e=>e.Id==r.EncounterId&&e.AreaId==d.AreaId);if(e==null||r.Count<1||r.Count>e.ActorIds.Count||r.Trigger!="entry"&&r.Trigger!="event"&&r.Trigger!="reaction"&&!d.Actions.Any(a=>a.Id==r.Trigger&&s.Resolved.Contains(a.Id)))throw new InvalidDataException("Invalid encounter release ownership.");if(r.Trigger=="entry"&&!d.Encounters.Any(x=>x.EncounterId==e.Id&&s.EncounterRoll>=x.Low&&s.EncounterRoll<=x.High))throw new InvalidDataException("Encounter does not match the saved entry roll.");foreach(var id in e.ActorIds.Take(r.Count))if(!expected.Add(id)||!all.Any(a=>a.Id==id))throw new InvalidDataException("Missing or duplicate released actor.");}
   foreach(var credit in s.Credits){if(credit=="outer-boxcar"){xp+=Exterior31BoxcarCredit(d,s);}else if(credit=="outer-signal"){xp+=Exterior31SignalCredit(d,s);}else if(credit=="roof-signal"){xp+=Roof28SignalCredit(d,s);}else if(credit.StartsWith("defeat:")){xp+=ValidateUpper26Credit(d,s,credit,all);}else if(credit=="food"){if(s.Number!=51||!s.Resolved.Contains("action.lower.51.loot.0"))throw new InvalidDataException("Unearned bakery food milestone.");xp+=50;}else if(credit is "quiet" or "model" or "reflection"){if(credit=="quiet"&&(!s.Secured||s.Number is not (23 or 34 or 51)||(s.Number==51?s.Events.Contains("gas-exploded"):!s.Quiet)))throw new InvalidDataException("Unearned quiet clear.");if(credit is "model" or "reflection"){string trait=credit=="model"?"model":"reflection";if(!s.Releases.Any(r=>Campaign!.Encounters.Single(e=>e.Id==r.EncounterId).ActorIds.Any(id=>Campaign.LowerMall!.Monsters.Any(m=>m.ActorId==id&&m.Traits.Contains(trait)))&&LowerCleared(s,r.EncounterId)))throw new InvalidDataException("Unearned monster milestone.");}xp+=credit=="model"?150:credit=="reflection"?200:s.Number==51?25:50;}else if(credit=="secure"){if(!s.Secured||!HasDialogueFlag($"flag.{d.Prefix}.secured")||!LowerCleared(s,d.BackEncounterId))throw new InvalidDataException("Unearned securing milestone.");xp+=d.SecureXp;}else{var a=d.Actions.FirstOrDefault(a=>a.Id==credit);if(a==null||!s.Resolved.Contains(a.Id)||a.EncounterId!=""&&!LowerCleared(s,a.EncounterId))throw new InvalidDataException("Unearned chapter milestone.");xp+=a.Xp;}}
  }
  if(xp!=(long)LowerMallXpAwarded+LowerMallXpRemainder||all.Any(a=>LowerMallReservedActor(a.Id)&&!expected.Contains(a.Id)))throw new InvalidDataException("Invalid chapter XP or dormant actor ownership.");
  foreach(var e in LowerMallEffects)if(!all.Concat(ShelterResidents.Select(r=>r.Actor)).Any(a=>a.Id==e.ActorId)||!double.IsFinite(e.EndsAt)||e.EndsAt<0||string.IsNullOrWhiteSpace(e.Condition))throw new InvalidDataException("Invalid timed location effect.");
 }
}
