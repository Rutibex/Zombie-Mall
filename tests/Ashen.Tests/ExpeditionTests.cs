using Ashen.Core;
using System.Text.Json.Nodes;
static class ExpeditionTests
{
 public static bool SkipRoutes {get;set;}
 public static void Run(Action<bool,string> check)
 {
  CampaignDefinition Definition()=>CampaignLoader.Load(CampaignLoader.BundledExpeditionPath);
  World Fresh()=>World.New(Definition());
  check(new Cell(1,2).ToString()=="(1, 2)"&&new WorldPoint(1,2).ToString()=="(1, 2)","Phase 8 coordinate traces cannot recurse through Cell/WorldPoint adapters");
  var w=Fresh();var definition=w.Campaign!.Expedition!;
  check(w.Campaign.SchemaVersion==6&&definition.Stages.Count==4&&w.CurrentExpeditionStage?.Id=="stage.expedition.entry","Phase 8 starts with a four-stage expedition and entry guidance");
  check(w.StoryEntries().Any(e=>e.Id=="expedition-briefing")&&w.CompletedExpedition==null,"Phase 8 briefing is recorded without inventing a completion");
  var board=w.Features.Single(f=>f.Kind=="debrief");check(!w.ReportExpedition(board)&&w.CompletedExpedition==null,"Phase 8 early remote report cannot complete expedition");
  void Ready()
  {
   w=Fresh();foreach(var f in w.Features.Where(f=>f.Kind=="door"))f.Used=true;foreach(var e in w.Actors.Where(a=>a.Enemy))e.Hp=0;
   foreach(var f in w.Features.Where(f=>f.Kind=="campaign-event")){w.PartyLeader.Pos=f.Pos;w.Reveal();w.Interact(f);}
   foreach(var source in w.Features.Where(f=>w.CurrentCampaignArea!.Features.Any(d=>d.Id==f.Id&&d.Scavengable)))
   {w.PartyLeader.Pos=source.Pos.Neighbors().First(p=>!w.Blocked(p)&&!w.Actors.Any(a=>a!=w.PartyLeader&&a.Alive&&a.Pos==p));w.Reveal();w.TakeAllContainer(w.PartyLeader,source);}
   w.PartyLeader.Pos=new(14,27);w.Reveal();w.SecureCarriedResources(w.PartyLeader,w.Features.Single(f=>f.Id==definition.StockpileFeatureId));
   Cell[] cells=[new(11,27),new(12,29),new(13,28),new(14,27)];int i=0;foreach(var a in w.Party)a.Pos=cells[i++];w.Reveal();w.SyncCampaignObjectives();board=w.Features.Single(f=>f.Kind=="debrief");
  }
  Ready();var testParty=w.Party.ToArray();Cell[] cafePositions=[new(20,6),new(20,7),new(19,7),new(19,8)];for(int i=0;i<testParty.Length;i++)testParty[i].Pos=cafePositions[i];w.Reveal();
  check(w.MoveSelectionFormation(new Cell(21,3).Center,w.Party.Select(a=>a.Id),FormationPreset.Column),"Phase 8 four survivors can approach shallow cafe stockroom as a group");
  for(int i=0;i<1800&&w.FormationTravelOrderCount>0;i++)w.UpdateMovement(1f/60);
  check(w.FormationTravelOrderCount==0&&!w.Party.Any(w.IsMoving)&&w.Party.All(a=>w.CanStand(a.Position,a)),"Phase 8 tight stockroom arrival settles without endless follower repathing or overlap");
  Ready();check(w.CurrentExpeditionStage==null&&w.Quest(w.Campaign!.Id+".objectives")!.Status==QuestStatus.Active,"Phase 8 completed tasks wait for a deliberate report rather than auto-finishing");
  var distant=w.Party.Last();var original=distant.Position;distant.Pos=new(13,20);check(!w.ReportExpedition(board)&&w.CompletedExpedition==null,"Phase 8 party left outside shelter blocks return");distant.Position=original;distant.Hp=0;check(!w.ReportExpedition(board),"Phase 8 unconscious party member blocks report");distant.Hp=distant.MaxHp;
  var site=w.Features.Single(f=>f.Id==definition.StockpileFeatureId);int earned=w.SecuredSurvivalValue;
  w.PartyLeader.Pos=new(14,27);w.Reveal();w.TakeAllContainer(w.PartyLeader,site);w.PartyLeader.Pos=new(11,27);w.Reveal();check(w.SecuredSurvivalValue==earned&&!w.ReportExpedition(board)&&w.ExpeditionReturnReason(board).Contains("Return at least"),"Phase 8 earned XP cannot substitute for actual returned stock");
  w.PartyLeader.Pos=new(14,27);w.Reveal();for(int i=w.PartyLeader.Inventory.Items.Count-1;i>=0;i--)w.StoreContainerItem(w.PartyLeader,site,i);w.PartyLeader.Pos=new(11,27);w.Reveal();int xp=w.ResourceXpAwarded;
  check(w.Interact(board)&&w.CompletedExpedition is {PartyIds.Count:4,StoredSv:37}&&w.Mode==Mode.Explore,"Phase 8 valid return records four survivors and physical supplies while keeping exploration available");
  check(w.Quest(w.Campaign!.Id+".objectives")!.Status==QuestStatus.Completed&&w.StoryEntries().Count(e=>e.Id=="expedition-completed")==1,"Phase 8 success resolves Journal once");
  var report=w.CompletedExpedition;check(w.Interact(board)&&ReferenceEquals(report,w.CompletedExpedition)&&w.ResourceXpAwarded==xp&&w.StoryEntries().Count(e=>e.Id=="expedition-completed")==1,"Phase 8 report review cannot pay XP again or replace its snapshot");
  string file=Path.Combine(Path.GetTempPath(),"zombie-expedition-"+Guid.NewGuid()+".json");
  try
  {
   w.Save(file);var loaded=World.Load(file);check(loaded.Version==World.CurrentSaveVersion&&loaded.CompletedExpedition?.StoredSv==37&&loaded.Quest(loaded.Campaign!.Id+".objectives")!.Status==QuestStatus.Completed,"Phase 8 completed expedition survives save/load");
   var root=JsonNode.Parse(File.ReadAllText(file))!.AsObject();root["CompletedExpedition"]!["StoredSv"]=1000;File.WriteAllText(file,root.ToJsonString());bool rejected=false;try{World.Load(file);}catch(InvalidDataException){rejected=true;}check(rejected,"Phase 8 impossible saved completion totals are rejected");
   var old=World.New(CampaignLoader.Load(CampaignLoader.BundledScavengingPath));old.Save(file);root=JsonNode.Parse(File.ReadAllText(file))!.AsObject();root["Version"]=20;root.Remove("CompletedExpedition");File.WriteAllText(file,root.ToJsonString());loaded=World.Load(file);check(loaded.Version==World.CurrentSaveVersion&&loaded.Campaign!.SchemaVersion==5&&loaded.Campaign.Expedition==null&&loaded.CompletedExpedition==null,"Phase 8 v20 migration preserves original campaign without inventing an expedition report");
  }
  finally{if(File.Exists(file))File.Delete(file);}
  void Reject(Action<CampaignDefinition> change,string label){var d=Definition();change(d);check(CampaignLoader.Validate(d).Count>0,"Phase 8 rejects "+label);}
  Reject(c=>c.SchemaVersion=5,"expedition metadata in old schema");Reject(c=>c.Expedition!.DebriefFeatureId="missing","unknown report point");Reject(c=>c.Expedition!.StockpileFeatureId="missing","unknown stockpile");Reject(c=>c.Expedition!.Stages[0].ObjectiveIds.Add(c.Expedition.Stages[0].ObjectiveIds[0]),"duplicated stage objective");Reject(c=>c.Expedition!.Stages.RemoveAt(0),"unassigned objectives");Reject(c=>c.Expedition!.Stages=null!,"null stages");
  RunFullRoute(check);
 }
 public static void RunFullRoute(Action<bool,string> check,string? campaignPath=null,bool hordeChallenge=false,bool survivalRest=false,bool shelterPopulation=false,bool infrastructureWorks=false,bool tensionEvents=false,bool survivorClasses=false,bool equipmentCatalogue=false,bool workshop=false,bool story=false,bool social=false,bool lower=false)
 {
  if(SkipRoutes)return;
  var w=World.New(CampaignLoader.Load(campaignPath??CampaignLoader.BundledExpeditionPath));w.ReactionPolicyResolver=(_,_)=>ReactionPolicy.Auto;
  if(campaignPath!=null){var leader=w.PartyLeader;w.AssignQuickWeapon(leader,leader.Equipment.Weapon!.InstanceId,1);w.Equip(leader,leader.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));w.ReloadFirearm(leader);}
  if(survivorClasses){if(!w.ChooseSurvivorClass(w.PartyLeader,"tough")||!w.ChooseSurvivorClass(w.Party.Skip(1).First(),"agile"))throw new Exception("Class route starting choice failed.");}
  int combats=0,steps=0;string logPath=Path.Combine(Path.GetTempPath(),"zombie-expedition-route.log");var trace=new List<string>();
  void Tick(){w.AdvanceCampaignSeconds(.1);w.UpdateMovement(.1f);steps++;if(w.Mode==Mode.Defeat)throw new Exception("Expedition route defeated: "+string.Join(" | ",w.Log.TakeLast(5)));}
  void Battle()
  {
   if(w.Mode!=Mode.Combat)return;combats++;int guard=0;
   while(w.Mode==Mode.Combat&&guard++<12000)
   {
    var a=w.Active??throw new Exception("No active combat actor.");
    if(w.IsMoving(a)){Tick();continue;}
    if(a.Enemy){w.EnemyTurn();Tick();continue;}
    var target=w.Actors.Where(e=>e.Enemy&&e.Alive&&(w.LocalAwarenessEnabled?w.Order.Contains(e.Id):e.Group==w.CombatGroup)).OrderBy(e=>e.Position.Distance(a.Position)).FirstOrDefault();if(target==null){w.CheckEnd();continue;}
    if(!a.Economy.CanAttack){w.EndTurn();continue;}
    if(campaignPath!=null&&a.RequiresAmmunition&&!w.HasRequiredAmmunition(a)){if(w.ReloadFirearm(a)){Tick();continue;}w.ActivateQuickWeapon(a,1);}
    if(w.RequestAttack(a,target,AttackKind.Basic)){Tick();continue;}
    // Close distance with a legal partial move when a complete approach exceeds this turn's budget.
    var options=w.Explored.Select(p=>p.Center).Where(p=>w.CanStand(p,a)&&p.Distance(target.Position)<=1.15f&&w.Line(p,target.Position)).Select(p=>(Point:p,Path:w.Path(a,p))).Where(x=>x.Path.Count>0).OrderBy(x=>World.PathLength(a.Position,x.Path)).ToArray();
    if(options.Length>0&&a.Move>.05f)
    {
     WorldPoint origin=a.Position,end=origin;float budget=a.Move-.01f;foreach(var point in options[0].Path){float length=origin.Distance(point);if(length<=budget){end=point;budget-=length;origin=point;}else{end=origin+(point-origin)*(budget/length);break;}}
     if(end.Distance(a.Position)>.02f&&w.MoveTo(a,end)){Tick();continue;}
    }
    w.EndTurn();
   }
   if(w.Mode!=Mode.Explore)throw new Exception("Combat stalled: "+string.Join(" | ",w.Log.TakeLast(5)));
  }
  void Move(Actor leader,Cell goal,bool group=true)
  {
   trace.Add("Move "+leader.Name+" to "+goal);File.WriteAllLines(logPath,trace);for(int guard=0;guard<250;guard++)
   {
    Battle();if(leader.Position.Distance(goal.Center)<.12f){w.StopAll();return;}
    var path=w.Path(leader,goal.Center,false);if(path.Count==0)throw new Exception("No route to "+goal+" from "+leader.Pos);
    WorldPoint point=leader.Position,visiblePoint=point;
    foreach(var end in path)
    {int count=Math.Max(1,(int)Math.Ceiling(point.Distance(end)/.25f));bool stop=false;for(int i=1;i<=count;i++){var candidate=point+(end-point)*(i/(float)count);if(!w.Explored.Contains(candidate.Cell)){stop=true;break;}visiblePoint=candidate;}if(stop)break;point=end;}
    if(visiblePoint.Distance(leader.Position)<.05f)throw new Exception("No discovered advance to "+goal);
    bool moved=group?w.MoveSelectionFormation(visiblePoint,w.Party.Select(a=>a.Id),FormationPreset.Column):w.MoveTo(leader,visiblePoint);
    if(!moved)throw new Exception("Move denied toward "+goal+": "+w.Log.LastOrDefault()+" positions "+string.Join(";",w.Party.Select(a=>a.Name+" "+a.Position)));
    int frames=0;while(w.Actors.Any(w.IsMoving)&&w.Mode==Mode.Explore&&frames++<1600)Tick();Battle();
    if(frames>=1600)throw new Exception("Movement stalled toward "+goal);
   }
   throw new Exception("Route limit toward "+goal+" party "+string.Join("; ",w.Party.Select(a=>a.Name+" "+a.Position)));
  }
  void Go(Cell goal)=>Move(w.PartyLeader,goal);
  void Use(string id){Battle();var f=w.Features.Single(f=>f.Id==id);if(!w.Interact(f))throw new Exception("Interaction failed "+id+": "+w.Log.LastOrDefault());Battle();trace.Add("Used "+id);}
  void Door(string suffix)=>Use("feature.mall.door."+suffix);
  string checkpoint=Path.Combine(Path.GetTempPath(),"zombie-expedition-checkpoint-"+Guid.NewGuid()+".json");
  try
  {
   Go(new(4,16));Door("entrance");Go(new(7,16));Use("feature.mall.directory");Go(new(13,17));Door("travel");Go(new(13,20));Go(new(11,21));Use("feature.mall.travel");Go(new(12,24));Door("office");Go(new(12,26));Go(new(11,27));Use("feature.mall.shelter");Go(new(12,27));Use("feature.mall.recruit-casey");
   Go(new(13,24));Go(new(20,24));Door("closet");Go(new(20,26));Use("feature.mall.recruit-robin");check(w.PartyCount==4,"Phase 8 real route recruits both survivors through movement and invitations");
   if(survivorClasses)check(w.ChooseSurvivorClass(w.Party.Single(a=>a.Id=="actor.mall.casey"),"smart")&&w.ChooseSurvivorClass(w.Party.Single(a=>a.Id=="actor.mall.robin"),"wise"),"Phase 17 actual route assigns permanent classes to both recruited survivors without granting equipment");
   Go(new(21,28));var stash=w.Features.Single(f=>f.Id=="feature.mall.closet-box");check(w.TakeAllContainer(w.PartyLeader,stash)>0,"Phase 8 real route scavenges first finite stash");
   Go(new(20,24));Go(new(13,20));Go(new(13,16));Go(new(13,14));Door("cafe");Go(new(13,11));Go(new(11,11));Use("feature.mall.cafe");Go(new(17,12));Go(new(22,12));Battle();Go(new(20,6));Door("cafe-stock");Go(new(21,3));check(w.TakeAllContainer(w.PartyLeader,w.Features.Single(f=>f.Id=="feature.mall.cafe-box"))>0,"Phase 8 real route clears cafe and loots stockroom");
   w.StopAll();w.Save(checkpoint);w=World.Load(checkpoint);w.ReactionPolicyResolver=(_,_)=>ReactionPolicy.Auto;check(w.PartyCount==4&&w.Actors.Single(a=>a.Id=="actor.mall.cafe-shambler").Hp==0&&w.CompletedExpedition==null,"Phase 8 mid-expedition reload retains recruits, defeated enemy, loot and incomplete expedition");
   Go(new(23,4));Door("cafe-service");Go(new(23,6));Go(new(13,12));Go(new(13,16));Go(new(21,16));Go(new(28,16));Go(new(32,17));Door("paper");Go(new(32,20));Go(new(29,21));Use("feature.mall.paper");Go(new(36,23));Battle();Go(new(38,24));Door("paper-stock");Go(new(40,27));check(w.TakeAllContainer(w.PartyLeader,w.Features.Single(f=>f.Id=="feature.mall.paper-box"))>0,"Phase 8 real route clears runner encounter and scavenges stationery stockroom");
   Go(new(38,24));Go(new(32,20));Go(new(32,16));Go(new(32,14));Door("gourmet");Go(new(32,11));Go(new(29,11));Use("feature.mall.gourmet");Go(new(36,12));Go(new(40,12));Battle();
   Go(new(32,12));Go(new(32,16));Go(new(24,16));Go(new(16,16));Go(new(13,20));Go(new(13,24));Go(new(12,26));Go(new(12,27));
   var remaining=w.Party.ToList();foreach(var goal in new[]{new Cell(14,27),new Cell(13,28),new Cell(12,29),new Cell(11,27)}){var actor=remaining.OrderBy(a=>a.Position.Distance(goal.Center)).First();Move(actor,goal,false);remaining.Remove(actor);}
   var site=w.Features.Single(f=>f.Id==w.Campaign!.Expedition!.StockpileFeatureId);var carrier=w.Party.Where(a=>a.Position.Distance(site.Pos.Center)<=1.25f).OrderBy(a=>a==w.PartyLeader?0:1).First();
   if(carrier!=w.PartyLeader)while(w.PartyLeader.Inventory.Items.Count>0){if(!w.TransferItem(w.PartyLeader,carrier,0,int.MaxValue))throw new Exception("Return handoff failed.");}
   check(w.SecureCarriedResources(carrier,site)>0&&w.StoredCreditedSv(site)>=10,"Phase 8 real route returns and secures supplies through ordinary transfer/deposit actions");
   var board=w.Features.Single(f=>f.Kind=="debrief");check(w.ExpeditionReturnReason(board)==""&&w.Interact(board)&&w.CompletedExpedition!=null,"Phase 8 first expedition completes through actual movement, combat and reporting");
   check(combats>=(w.LocalAwarenessEnabled?1:3)&&w.ClearedCampaignEncounters.Count==3&&w.Party.All(a=>a.Alive)&&w.Actors.Where(a=>a.Enemy).All(a=>a.Hp==0)&&w.Mode==Mode.Explore,"Phase 8 route defeats three real encounters and returns all four survivors without setting enemy HP");
   if(campaignPath!=null)check(w.Party.SelectMany(a=>a.Inventory.Items.Concat(a.Equipment.All())).Sum(s=>s.ItemId=="item.mall.ammo-9mm"?s.Quantity:s.LoadedAmmunition?.ItemId=="item.mall.ammo-9mm"?s.LoadedAmmunition.Quantity:0)<12,"Phase 9 full route fires finite starter rounds and still completes the expedition");
   if(hordeChallenge)
   {
    var leader=w.PartyLeader;
    void Give(string item,Actor recipient,int quantity=1){foreach(var owner in w.Party){int index=owner.Inventory.Items.FindIndex(s=>s.ItemId==item);if(index<0)continue;if(owner!=recipient&&!w.TransferItem(owner,recipient,index,quantity))throw new Exception("Horde preparation transfer failed.");return;}throw new Exception("Missing finite starter item "+item);}
    Give("item.melee.006",leader);var melee=leader.Inventory.Items.First(s=>s.ItemId=="item.melee.006");w.AssignQuickWeapon(leader,melee.InstanceId,1);
    var gunTeam=new[]{(Actor:w.Party.First(a=>a.Id=="actor.mall.sam"),Gun:"item.mall.revolver",Ammo:"item.mall.ammo-38",Rounds:6),(Actor:w.Party.First(a=>a.Id=="actor.mall.robin"),Gun:"item.mall.shotgun",Ammo:"item.mall.ammo-shells",Rounds:2)};
    foreach(var kit in gunTeam){w.AssignQuickWeapon(kit.Actor,kit.Actor.Equipment.Weapon!.InstanceId,1);Give(kit.Gun,kit.Actor);Give(kit.Ammo,kit.Actor,kit.Rounds);if(!w.Equip(kit.Actor,kit.Actor.Inventory.Items.FindIndex(s=>s.ItemId==kit.Gun))||!w.ReloadFirearm(kit.Actor))throw new Exception("Could not prepare horde firearm.");}
    Go(new(13,24));Go(new(13,20));Go(new(24,16));Go(new(32,16));Go(new(40,16));Go(new(42,16));
    var breach=w.Features.Single(f=>f.Id=="feature.mall.shutter");check(w.Interact(breach),"Phase 12 real route deliberately provokes the optional shutter breach");Battle();
    var horde=w.Campaign!.Hordes.First(h=>h.Id=="horde.mall.east-shutter");check(w.HordeStatus(horde.Id)=="Cleared"&&horde.MemberActorIds.All(id=>w.Actors.Single(a=>a.Id==id).Hp==0)&&w.Party.All(a=>a.Alive),"Phase 12 real six-shambler horde is defeated with finite starter weapons and normal turns");
    check(w.ReleasedHordes.Count==1&&w.ClearedCampaignEncounters.Count==4&&w.ResourceXpAwarded==24,"Phase 12 real horde completion retains encounter identity and grants no extra resource XP");
   }
   if(survivalRest||shelterPopulation)
   {
    Go(new(32,16));Go(new(24,16));Go(new(16,16));Go(new(13,20));Go(new(13,24));Go(new(12,26));Go(new(12,27));
    var crew=w.Party.ToList();foreach(var goal in new[]{new Cell(14,27),new Cell(13,28),new Cell(12,29),new Cell(11,27)}){var member=crew.OrderBy(a=>a.Position.Distance(goal.Center)).First();Move(member,goal,false);crew.Remove(member);}
    if(shelterPopulation)
    {
     var casey=w.Party.Single(a=>a.Id=="actor.mall.casey");var robin=w.Party.Single(a=>a.Id=="actor.mall.robin");int injured=casey.Hp;
     check(w.HouseSurvivor(casey.Id)&&w.HouseSurvivor(robin.Id)&&w.PartyCount==2&&w.ShelterResidents.Count==2,"Phase 14 actual route leaves two rescued survivors in the refuge using roster transfers");
     check(w.AssignShelterDuty(casey.Id,ShelterDuty.Care)&&w.AssignShelterDuty(robin.Id,ShelterDuty.Steward),"Phase 14 actual route assigns care and stewardship");
     check(w.SupplyParty()==4&&w.ProvisionShelter()==4,"Phase 14 actual route provisions field crew and residents once each from eight finite supplies");
     int xp=w.ResourceXpAwarded;int bandages=w.ShelterSupplyCount("bandage");
     check(w.PerformRest(RestKind.LongRest).Completed&&w.Party.All(a=>a.Hp==a.MaxHp)&&casey.Hp>injured&&w.ShelterSupplyCount("bandage")==bandages-1,"Phase 14 actual eight-hour rest recovers field crew while resident care spends a bandage on post-horde wounds");
     int due=robin.ShelterWork.NextWorkMinute;check(w.RecallResident(robin.Id)&&w.HouseSurvivor(robin.Id)&&robin.ShelterWork.NextWorkMinute==due&&robin.ShelterWork.Duty==ShelterDuty.Steward,"Phase 14 actual recall/rehousing retains the same resident and duty deadline");
     check(w.ResourceXpAwarded==xp&&w.ReleasedHordes.Count==1&&w.HordeStatus(w.Campaign!.Hordes.First(h=>h.Id=="horde.mall.east-shutter").Id)=="Cleared"&&w.CompletedExpedition!=null,"Phase 14 actual shelter operation retains earned XP, expedition history and all horde casualties");
     trace.Add("Returned after horde; housed Casey and Robin; assigned Care/Steward; spent eight meal/water units and one bandage; tested recall/rehousing.");
    }
    else
    {
    int fatigueBefore=w.ResourceXpAwarded,minuteBefore=w.CampaignMinutes;
    check(w.UseSurvivalSupply(w.PartyLeader,w.PartyLeader,"first-aid")&&w.PartyLeader.Needs.Treated,"Phase 13 actual route spends one refuge first-aid kit to treat post-horde wounds");
    check(w.SupplyParty()==8,"Phase 13 actual route provisions four survivors with eight finite returned supplies");
    check(w.PerformRest(RestKind.LongRest).Completed&&w.CampaignMinutes==minuteBefore+480&&w.Party.All(a=>a.Hp==a.MaxHp)&&w.ResourceXpAwarded==fatigueBefore,"Phase 13 actual post-horde refuge rest restores all four survivors, advances eight hours and retains secured XP");
    check(w.CompletedExpedition!=null&&w.ReleasedHordes.Count==1&&w.HordeStatus(w.Campaign!.Hordes.First(h=>h.Id=="horde.mall.east-shutter").Id)=="Cleared","Phase 13 daily care preserves expedition history and persistent horde casualties");
    trace.Add("Returned after horde, provisioned four survivors and completed an eight-hour refuge rest.");
    }
   }
   if(infrastructureWorks)
   {
    int before=w.CampaignMinutes;var works=w.InfrastructureState;
    check(w.PerformWorks(WorksAction.BuildBarricade)&&w.PerformWorks(WorksAction.InstallGenerator)&&w.PerformWorks(WorksAction.InstallWaterStation),"Phase 15 actual route constructs the office barricade, generator and water station from finite refuge materials");
    check(w.PerformWorks(WorksAction.Refuel)&&w.PerformWorks(WorksAction.TogglePower)&&w.PerformWorks(WorksAction.PurifyWater),"Phase 15 actual route fuels and starts power, then purifies one physical water unit");
    check(w.CampaignMinutes==before+130&&works.BarricadeHp==24&&works.BarricadeEngaged&&w.ShelterPowered&&works.FuelMinutes==50&&works.PurifiedUnits==1,"Phase 15 actual route persists construction time, secured durability and exact operating fuel");
    check(works.XpAwarded==60&&works.XpRemainder==0&&w.ResourceXpAwarded==24&&w.CommunityMembers.Sum(a=>a.Character.Experience)==84,"Phase 15 actual one-time 60 construction XP adds to the unchanged 24 secured-resource XP without duplicating resident shares");
    trace.Add("Built office barricade, generator and water station; spent one fuel unit, untreated water and tablet; 60 construction XP once; generator has 50 minutes remaining.");
   }
   if(tensionEvents)
   {
    check(w.TensionEnabled&&w.RecallResident("actor.mall.casey")&&w.RecallResident("actor.mall.robin"),"Phase 16 actual route recalls both original residents with their existing equipment and needs");
    int rests=0;while(w.TensionState.History.Count==0&&rests++<12){var rest=w.PerformRest(RestKind.ShortRest,[]);if(!rest.Completed)throw new Exception("Daily wait rest failed: "+rest.Summary);}
    check(w.TensionState.History.Count==1&&w.TensionState.PendingAttacks.Count==1,"Phase 16 actual expedition reaches the next day through ordinary refuge rests and records one pending attack");
    string attack=w.TensionState.PendingAttacks[0];check(w.DispatchDailyAttack(),"Phase 16 actual route deploys the pending finite attack at the refuge");
    Move(w.PartyLeader,new(12,26),false);Use("feature.mall.door.office");Go(new(12,24));Battle();
    for(int i=0;i<500&&w.HordeStatus(attack)!="Cleared";i++){Tick();Battle();}
    check(w.HordeStatus(attack)=="Cleared"&&w.Party.All(a=>a.Alive)&&w.TensionState.CreditedHordes.Contains(attack),"Phase 16 actual perimeter attack is defeated through normal movement and combat without assigning enemy HP");
    Go(new(14,28));check(w.PerformWorks(WorksAction.Secure),"Phase 16 actual route returns and secures the surviving office barricade");
    check(w.ResourceXpAwarded==24&&w.InfrastructureState.XpAwarded==60&&w.TensionState.History.Count==1,"Phase 16 daily combat preserves both earned XP ledgers and the immutable daily report");
    trace.Add("Reached Day 2 through normal rests; recalled original residents; fought and cleared the two-member perimeter attack; returned and secured the refuge. No HP, clock, Tension or daily roll was forced.");
   }
   if(survivorClasses){check(w.Party.Select(a=>a.Training.ClassId).ToHashSet().SetEquals(new[]{"tough","agile","smart","wise"})&&w.Party.All(a=>a.TotalLevel==1&&a.Alive),"Phase 17 actual expedition retains all four trained survivors at honestly earned level one");trace.Add("All four selected source classes, hit dice, proficiencies and class features persisted through actual recruitment, travel, shelter roles, combat and daily events. No XP or class levels were forced.");}
   if(equipmentCatalogue)
   {
    Move(w.PartyLeader,new(12,26),false);Use("feature.mall.door.office");Go(new(13,24));Go(new(20,24));Go(new(20,26));Go(new(19,28));
    var cache=w.Features.Single(f=>f.Id=="feature.mall.equipment-cache");int before=w.ResourceXpAwarded;check(w.TakeAllContainer(w.PartyLeader,cache)>0&&cache.Contents.Items.Count==0,"Phase 18 actual route retrieves the finite equipment recovery crate without grants");
    Go(new(20,24));Go(new(13,24));Go(new(12,26));Go(new(12,27));
    var team=w.Party.ToList();foreach(var goal in new[]{new Cell(14,27),new Cell(13,28),new Cell(12,29),new Cell(11,27)}){var member=team.OrderBy(a=>a.Position.Distance(goal.Center)).First();Move(member,goal,false);team.Remove(member);}
    var leader=w.PartyLeader;var receiver=w.Party.First(a=>a.Position.Distance(site.Pos.Center)<=1.25f);if(receiver!=leader)while(leader.Inventory.Items.Count>0)if(!w.TransferItem(leader,receiver,0,int.MaxValue))throw new Exception("Equipment handoff failed");
    check(w.SecureCarriedResources(receiver,site)>0&&w.ResourceXpAwarded==228&&w.ResourceXpRemainder==2,"Phase 18 actual route banks 203 new SV once, reaching 228 awarded XP and remainder two");
    if(leader.Position.Distance(site.Pos.Center)>1.25f){Move(receiver,new(11,29),false);Move(leader,new(13,28),false);}
    foreach(var id in new[]{"item.armour.002","item.ranged.006","item.ammo.catalogue-bolt"})if(!w.TakeContainerItem(leader,site,site.Contents.Items.FindIndex(s=>s.ItemId==id),id=="item.ammo.catalogue-bolt"?4:1))throw new Exception("Banked gear withdrawal failed "+id);
    check(w.Equip(leader,leader.Inventory.Items.FindIndex(s=>s.ItemId=="item.armour.002"))&&w.Equip(leader,leader.Inventory.Items.FindIndex(s=>s.ItemId=="item.ranged.006"))&&w.ReloadFirearm(leader),"Phase 18 actual survivor wears recovered armour and loads a banked physical bolt");
    int xp=w.ResourceXpAwarded;check(w.UnloadFirearm(leader)&&w.ReloadFirearm(leader)&&leader.Equipment.Weapon!.LoadedAmmunition!.Quantity==1&&leader.Inventory.Count("item.ammo.catalogue-bolt")==3&&w.ResourceXpAwarded==xp,"Phase 18 actual load/unload cycle preserves rounds and cannot re-award secured XP");
    check(w.PerformWorks(WorksAction.Secure)&&w.InfrastructureState.XpAwarded==60&&w.Party.All(a=>a.Alive)&&w.CompletedExpedition!=null,"Phase 18 equipment recovery preserves the completed expedition and refuge history");
    trace.Add("Recovered the physical outdoor equipment crate on Day 2, returned and banked its 203 SV, withdrew hoodie/crossbow/four bolts, equipped and reloaded normally. No item, XP, HP, time or encounter grant was used.");
   }
   if(workshop)
   {
    var leader=w.PartyLeader;Move(leader,new(12,28),false);var bench=w.Features.Single(f=>f.Id==w.Campaign!.Workshop!.BenchFeatureId);string damagedId=bench.Contents.Items.Single(s=>s.Wear==65).InstanceId;
    check(w.TakeAllContainer(leader,bench)>0&&bench.Contents.Items.Count==0,"Phase 19 actual route retrieves the finite toolkit/material crate and authored damaged crowbar");var accessBlocker=w.Party.FirstOrDefault(a=>a!=leader&&a.Pos==new Cell(13,28));if(accessBlocker!=null)Move(accessBlocker,new(14,29),false);Move(leader,new(13,28),false);
    check(w.SecureCarriedResources(leader,site)>0&&w.ResourceXpAwarded==324&&w.ResourceXpRemainder==3,"Phase 19 actual route banks 97 new SV exactly once");var damaged=leader.Inventory.FindInstance(damagedId)!;int minute=w.CampaignMinutes,xp=w.ResourceXpAwarded;
    check(damaged.Wear==65&&w.RepairEquipment(leader,damagedId)&&damaged.Wear==40&&w.CampaignMinutes==minute+30,"Phase 19 actual repair restores 25 condition using banked scrap and real time");
    check(w.Craft(leader,"recipe.mall.bolts")&&site.Contents.Count("item.ammo.catalogue-bolt")==4&&site.Contents.Items.Where(s=>s.ItemId=="item.ammo.catalogue-bolt").All(s=>s.ResourceUnits.Count==0),"Phase 19 actual crafting creates four physical bolts without new XP provenance");
    check(w.TakeContainerItem(leader,site,site.Contents.Items.FindIndex(s=>s.ItemId=="item.ammo.catalogue-bolt"),4)&&leader.Inventory.Count("item.ammo.catalogue-bolt")==7,"Phase 19 actual survivor withdraws four crafted bolts");
    check(w.SalvageEquipment(leader,damagedId)&&leader.Inventory.FindInstance(damagedId)==null&&w.ResourceXpAwarded==xp,"Phase 19 actual dismantling consumes the spare without XP");
    if(!w.TakeContainerItem(leader,site,site.Contents.Items.FindIndex(s=>s.ItemId==w.Campaign!.Workshop!.WoodItemId),1))throw new Exception("Trade withdrawal failed");Move(leader,new(12,28),false);
    var offer=leader.Inventory.Items.First(s=>s.ItemId==w.Campaign!.Workshop!.WoodItemId);var want=w.TraderStock.Items.First(s=>s.ItemId=="item.commodity.037");check(w.Barter(leader,offer.InstanceId,1,want.InstanceId,1)&&w.TraderStock.Count(want.ItemId)==3&&leader.Inventory.Count(want.ItemId)==1,"Phase 19 actual barter swaps one owned plank for one finite battery");
    check(w.BarterRewardUsed==2&&w.BarterXpAwarded==0&&w.BarterXpRemainder==2&&w.ResourceXpAwarded==324&&w.InfrastructureState.XpAwarded==60,"Phase 19 commission is a separate bounded pool; prior XP unchanged");
    w.Save(checkpoint);w=World.Load(checkpoint);w.ReactionPolicyResolver=(_,_)=>ReactionPolicy.Auto;check(w.TraderStock.Count("item.commodity.037")==3&&w.WorkshopHistory.Count==4&&w.PartyLeader.Inventory.Count("item.ammo.catalogue-bolt")==7&&w.Party.All(a=>a.Alive),"Phase 19 actual reload retains stock, work history, crafted ammunition and four survivors");trace.Add("Looted and banked the finite 97-SV bench cache; repaired its damaged crowbar, crafted four bolts, dismantled the spare and bartered one plank for a battery. Saved/reloaded without production item, HP, XP, clock or restock grants.");
   }
   if(story)
   {
    site=w.Features.Single(f=>f.Id==site.Id);
    var leader=w.PartyLeader;check(w.AcceptContract(leader,"contract.mall.service-sweep"),"Phase 20 actual route accepts the gated sweep after the honest first expedition and workshop recovery");
    var proof=Environment.GetEnvironmentVariable("ZOMBIE_MALL_PLAYTHROUGH_SAVE");if(!string.IsNullOrWhiteSpace(proof))w.Save(proof+".accepted.json");
    Move(leader,new(12,26),false);Use("feature.mall.door.office");Go(new(13,24));Go(new(20,24));Go(new(20,26));Go(new(20,28));Use("feature.mall.maintenance-release");
    if(w.Mode==Mode.Explore&&!string.IsNullOrWhiteSpace(proof))w.Save(proof+".wave.json");
    Go(new(21,27));Battle();Go(new(21,28));for(int i=0;i<600&&w.EncounterPlanStatus("plan.mall.maintenance")!="cleared";i++){Tick();Battle();}
    check(w.EncounterPlanStatus("plan.mall.maintenance")=="cleared"&&w.Actors.Count(a=>a.Id.StartsWith("actor.mall.maintenance-"))==2&&w.Actors.Where(a=>a.Id.StartsWith("actor.mall.maintenance-")).All(a=>!a.Alive),"Phase 20 actual two-wave response is cleared using normal movement, finite weapons and combat");
    Go(new(22,28));Use("feature.mall.maintenance-manifest");check(w.ContractStatus("contract.mall.service-sweep")=="Ready","Phase 20 actual investigation event completes the live contract objectives");
    Go(new(20,24));Go(new(13,24));Go(new(12,26));Go(new(12,27));var crew=w.Party.ToList();foreach(var goal in new[]{new Cell(14,27),new Cell(13,28),new Cell(12,29),new Cell(11,27)}){var member=crew.OrderBy(a=>a.Position.Distance(goal.Center)).First();Move(member,goal,false);crew.Remove(member);}
    var blocker=w.Party.FirstOrDefault(a=>a!=w.PartyLeader&&a.Pos==new Cell(12,28));if(blocker!=null)Move(blocker,new(14,29),false);Move(w.PartyLeader,new(12,28),false);
    check(w.PerformWorks(WorksAction.Secure)&&w.ClaimContract(w.PartyLeader,"contract.mall.service-sweep")&&w.ContractXpAwarded==8,"Phase 20 actual refuge report secures the door and credits the sweep once");
    int wire=site.Contents.Count("item.commodity.084");check(w.AcceptContract(w.PartyLeader,"contract.mall.radio-wire")&&w.ContractStatus("contract.mall.radio-batteries")=="Excluded"&&w.ClaimContract(w.PartyLeader,"contract.mall.radio-wire")&&site.Contents.Count("item.commodity.084")==wire-1,"Phase 20 actual wiring choice consumes one banked physical spool and excludes the alternative");
    int serial=w.NextItemInstanceNumber;check(!w.ClaimContract(w.PartyLeader,"contract.mall.radio-wire")&&w.NextItemInstanceNumber==serial&&w.ContractXpAwarded==12&&w.ContractXpRemainder==0&&w.ResourceXpAwarded==324&&w.BarterRewardUsed==2&&w.InfrastructureState.XpAwarded==60,"Phase 20 repeated reports preserve finite rewards and all four independent ledgers");
    w.Save(checkpoint);w=World.Load(checkpoint);check(w.ContractStatus("contract.mall.radio-wire")=="Completed"&&w.ContractStatus("contract.mall.radio-batteries")=="Excluded"&&w.EncounterPlanStatus("plan.mall.maintenance")=="cleared"&&w.Party.All(a=>a.Alive),"Phase 20 actual reload preserves completed contracts, excluded branch, dead waves and four survivors");
    trace.Add("Accepted the sweep at the refuge, activated the closet release, fought both finite waves, read the manifest and reported. Chose wiring and consumed one banked spool; repeated report denied. Saved/reloaded without production HP, item, XP, clock or enemy grants.");
   }
   if(social)
   {
    const string guild="faction.exemplar.smart.13";var leader=w.PartyLeader;int serial=w.NextItemInstanceNumber,minute=w.CampaignMinutes;
    check(w.SocialHistory.Count==2&&w.SurvivorTrust("actor.mall.robin")==2&&w.FactionStanding(guild)==1,"Phase 21 completed contracts apply their social outcomes exactly once");
    check(w.ResolveSocialDecision(leader,"social.sam.watch","listen")&&w.ResolveSocialDecision(leader,"social.casey.care","support")&&w.ResolveSocialDecision(leader,"social.robin.stock","share"),"Phase 21 actual survivors discuss scouting, care and shared supplies at the refuge");
    check(w.ResolveSocialDecision(leader,"social.guild.contact","share")&&w.ResolveSocialDecision(leader,"social.runners.contact","warn")&&w.ResolveSocialDecision(leader,"social.remnants.contact","cooperate"),"Phase 21 actual route establishes all three authored faction contacts through permanent choices");
    check(w.ResolveSocialDecision(leader,"social.sam.followup","listen")&&w.ResolveSocialDecision(leader,"social.casey.followup","listen")&&w.ResolveSocialDecision(leader,"social.robin.followup","listen"),"Phase 21 earned trust unlocks all three personal follow-ups");
    check(w.CampaignMinutes==minute+45&&w.NextItemInstanceNumber==serial&&w.SocialTradeMarkup==-5&&w.FactionStanding(guild)==6&&w.SocialHistory.Count==11&&w.ContractXpAwarded==12&&w.ResourceXpAwarded==324&&w.InfrastructureState.XpAwarded==60&&w.BarterRewardUsed==2,"Phase 21 conversations consume real time without item or XP grants and preserve finite trade limits");
    int n=w.SocialHistory.Count;check(!w.ResolveSocialDecision(leader,"social.guild.contact","withhold")&&w.SocialHistory.Count==n&&w.FactionStanding(guild)==6,"Phase 21 resolved faction choices cannot be reversed or replayed");
    w.Save(checkpoint);w=World.Load(checkpoint);check(w.SocialHistory.Count==11&&w.SurvivorTrust("actor.mall.sam")==7&&w.SurvivorTrust("actor.mall.casey")==7&&w.SurvivorTrust("actor.mall.robin")==9&&w.SurvivorBond("actor.mall.sam","actor.mall.casey")==6&&w.Party.All(a=>a.Alive),"Phase 21 actual save/load retains faction standing, individual trust, symmetric bonds and original living survivors");
    trace.Add("At the safe refuge, resolved nine finite conversations and three local faction contacts with visible consequences. Earned trust unlocked personal follow-ups. Saved/reloaded the same survivors, finite stock and unchanged XP ledgers. No HP, item, XP or clock grants were used.");
   }
   if(lower)
   {
    Move(w.PartyLeader,new(12,26),false);Use("feature.mall.door.office");int slot=0;foreach(var member in w.Party.ToArray())Move(member,new(13+slot++,24),false);Go(new(13,18));Go(new(7,16));Go(new(4,16));var exitSlots=new[]{new Cell(2,14),new Cell(3,15),new Cell(3,17),new Cell(4,17)};slot=0;foreach(var member in w.Party.ToArray())Move(member,exitSlots[slot++],false);
    check(w.TravelCampaignTo("area.lower.concourse"),"Phase 22 actual party walks from the established refuge and travels into the lower concourse");
    if(w.Campaign!.SchemaVersion>=29)
    {
     var slots=new[]{new Cell(22,19),new Cell(23,19),new Cell(24,19),new Cell(23,20)};slot=0;foreach(var member in w.Party.ToArray())Move(member,slots[slot++],false);
     check(w.TravelCampaignTo("area.exterior.perimeter"),"Phase 31 original four survivors use actual mall exit");
     slots=[new(41,19),new(40,20),new(41,21),new(42,20)];slot=0;foreach(var member in w.Party.ToArray())Move(member,slots[slot++],false);
     check(w.TravelCampaignTo("area.exterior.outer-road"),"Phase 31 original expedition walks to the connected outer service road");
     slots=[new(4,24),new(5,24),new(6,24),new(5,25)];slot=0;foreach(var member in w.Party.ToArray())Move(member,slots[slot++],false);
     check(w.TravelCampaignTo("area.exterior.14")&&w.LowerMallProgress(314)!=null,"Phase 31 original expedition reaches Tire World and rolls its actual source encounter");
     check(w.PartyCount==4&&w.ResourceXpAwarded==324&&w.ContractXpAwarded==12&&w.SocialHistory.Count==11,"Phase 31 exterior arrival preserves four survivors and honestly earned reward and social ledgers");
     w.Save(checkpoint);w=World.Load(checkpoint);check(w.CurrentTravelDestinationId=="area.exterior.14"&&w.Party.All(a=>a.Alive),"Phase 31 actual expedition saves and reloads with all four survivors alive");
     trace.Add("Original four survivors walked from refuge through the mall exit, perimeter and outer service road to Tire World. Isolated fixtures separately exercise all thirteen new sites.");
    }
    else if(w.Campaign!.SchemaVersion>=28)
    {
     var mainSlots=new[]{new Cell(22,19),new Cell(23,19),new Cell(24,19),new Cell(23,20)};slot=0;foreach(var member in w.Party.ToArray())Move(member,mainSlots[slot++],false);
     check(w.TravelCampaignTo("area.exterior.perimeter"),"Phase 30 original four-survivor expedition walks through the actual mall main exit into the exterior perimeter");
     var lotSlots=new[]{new Cell(4,20),new Cell(5,19),new Cell(6,20),new Cell(5,21)};slot=0;foreach(var member in w.Party.ToArray())Move(member,lotSlots[slot++],false);
     check(w.TravelCampaignTo("area.exterior.01")&&w.LowerMallProgress(301)!=null,"Phase 30 original expedition walks into the North Lot and rolls its saved source encounter");
     check(w.Campaign.LowerMall!.Sites.Count==(w.Campaign.SchemaVersion>=29?159:146)&&w.PartyCount==4&&w.ResourceXpAwarded==324&&w.ContractXpAwarded==12&&w.SocialHistory.Count==11,"Phase 30 exterior arrival preserves original survivors, faction progress and both earned reward ledgers");
     w.Save(checkpoint);w=World.Load(checkpoint);check(w.CurrentTravelDestinationId=="area.exterior.01"&&w.Party.All(a=>a.Alive),"Phase 30 normal exterior expedition saves/reloads with all four survivors alive");
     trace.Add("Walked normally from the established refuge through the mall main exit and perimeter road into exterior location 1. Separate fixtures verify each new location.");
    }
    else
    {
    if(w.Campaign!.SchemaVersion is >=21 and <23){var northSlots=new[]{new Cell(41,14),new Cell(40,15),new Cell(41,16),new Cell(42,15)};slot=0;foreach(var member in w.Party.ToArray())Move(member,northSlots[slot++],false);check(w.TravelCampaignTo("area.lower.north"),"Phase 23 actual party walks across the original concourse into the east and north storefronts");}
    if(w.Campaign!.SchemaVersion==22){var arcadeSlots=new[]{new Cell(41,14),new Cell(40,15),new Cell(41,16),new Cell(42,15)};slot=0;foreach(var member in w.Party.ToArray())Move(member,arcadeSlots[slot++],false);check(w.TravelCampaignTo("area.lower.east"),"Phase 24 actual party walks through the final arcade passage without replacing the prior concourses");}
    if(w.Campaign!.SchemaVersion>=23){var escalatorSlots=new[]{new Cell(39,17),new Cell(38,18),new Cell(39,19),new Cell(40,18)};slot=0;foreach(var member in w.Party.ToArray())Move(member,escalatorSlots[slot++],false);check(w.TravelCampaignTo("area.upper.concourse"),"Phase 25 actual original party walks to the central escalator and arrives in the upper gallery");}
    if(w.Campaign!.SchemaVersion>=24){var gallerySlots=new[]{new Cell(41,14),new Cell(40,15),new Cell(41,16),new Cell(42,15)};slot=0;foreach(var member in w.Party.ToArray())Move(member,gallerySlots[slot++],false);check(w.TravelCampaignTo("area.upper.north"),"Phase 26 original party walks through the upper gallery passage into locations 19–36");}
    if(w.Campaign!.SchemaVersion>=25){var finalSlots=new[]{new Cell(41,14),new Cell(40,15),new Cell(41,16),new Cell(42,15)};slot=0;foreach(var member in w.Party.ToArray())Move(member,finalSlots[slot++],false);check(w.TravelCampaignTo("area.upper.final"),"Phase 27 original expedition walks into the final upper gallery");}
    if(w.Campaign!.SchemaVersion>=26){var roofSlots=new[]{new Cell(41,14),new Cell(40,15),new Cell(41,16),new Cell(42,15)};slot=0;foreach(var member in w.Party.ToArray())Move(member,roofSlots[slot++],false);check(w.TravelCampaignTo("area.roof.walkway"),"Phase 28 original expedition climbs from final upper gallery to the roof walkway");}
    if(w.Campaign!.SchemaVersion>=27){var eastSlots=new[]{new Cell(41,17),new Cell(42,16),new Cell(42,18),new Cell(40,17)};slot=0;foreach(var member in w.Party.ToArray())Move(member,eastSlots[slot++],false);check(w.TravelCampaignTo("area.roof.east"),"Phase 29 original expedition walks across the roof into the eastern service walkway");}
    string firstShop=w.Campaign!.SchemaVersion>=27?"area.roof.14":w.Campaign.SchemaVersion>=26?"area.roof.01":w.Campaign.SchemaVersion>=25?"area.upper.37":w.Campaign.SchemaVersion>=24?"area.upper.19":w.Campaign.SchemaVersion>=23?"area.upper.01":w.Campaign.SchemaVersion>=22?"area.lower.37":w.Campaign.SchemaVersion>=21?"area.lower.19":"area.lower.01";int firstNumber=w.Campaign.SchemaVersion>=27?214:w.Campaign.SchemaVersion>=26?201:w.Campaign.SchemaVersion>=25?137:w.Campaign.SchemaVersion>=24?119:w.Campaign.SchemaVersion>=23?101:w.Campaign.SchemaVersion>=22?37:w.Campaign.SchemaVersion>=21?19:1;
    var shopSlots=w.Campaign.SchemaVersion>=27?new[]{new Cell(6,20),new Cell(7,19),new Cell(8,20),new Cell(7,21)}:new[]{new Cell(4,20),new Cell(5,19),new Cell(6,20),new Cell(5,21)};slot=0;foreach(var member in w.Party.ToArray())Move(member,shopSlots[slot++],false);
    check(w.TravelCampaignTo(firstShop)&&w.LowerMallProgress(firstNumber)!=null,"Lower mall actual party enters its chapter storefront and rolls one saved entry encounter");
    check(w.Campaign!.LowerMall!.Sites.Count==(w.Campaign.SchemaVersion>=27?133:w.Campaign.SchemaVersion>=26?121:w.Campaign.SchemaVersion>=25?108:w.Campaign.SchemaVersion>=24?90:w.Campaign.SchemaVersion>=23?72:w.Campaign.SchemaVersion>=22?54:w.Campaign.SchemaVersion>=21?36:18)&&w.StoredTravelAreas.Any(a=>a.DestinationId==w.Campaign.Shelter!.AreaId)&&w.SocialHistory.Count==11&&w.ResourceXpAwarded==324&&w.ContractXpAwarded==12&&w.Party.All(a=>a.Alive),"Phase 22 actual arrival retains four survivors, original refuge, relationships and XP ownership");
    w.Save(checkpoint);w=World.Load(checkpoint);check(w.CurrentTravelDestinationId==firstShop&&w.LowerMallProgress(firstNumber)!.Releases.Select(r=>r.EncounterId).Distinct().Count()==w.LowerMallProgress(firstNumber)!.Releases.Count,"Phase 22 actual entry saves/reloads without respawning reserved threats");
    trace.Add("Walked from the office refuge to the west service access, travelled into the source-numbered concourse and entered the chapter storefront. Existing survivors, equipment, finite stock and XP persist. This chapter-entry proof uses normal movement/travel; separate isolated fixtures verify all eighteen locations.");
    }
   }
   var output=Environment.GetEnvironmentVariable("ZOMBIE_MALL_PLAYTHROUGH_SAVE");if(!string.IsNullOrWhiteSpace(output)){w.Save(output);File.WriteAllLines(output+".log",trace.Append($"Completed in {steps} simulation steps; {combats} encounters; {w.StoredCreditedSv(site)} SV returned; XP {w.ResourceXpAwarded}, pool {w.ResourceXpRemainder}."));}
  }
  catch{File.WriteAllLines(logPath,trace.Concat(w.Log.TakeLast(20)));throw;}
  finally{if(File.Exists(checkpoint))File.Delete(checkpoint);}
 }
}
