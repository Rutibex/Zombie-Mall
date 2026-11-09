using Ashen.Core;
using System.Text.Json.Nodes;
static class DailySurvivalTests
{
 public static void Run(Action<bool,string> check)
 {
  var definition=CampaignLoader.Load(CampaignLoader.BundledSurvivalPath);
  World Fresh(bool refuge=true)
  {
   var w=World.New(definition);if(refuge){w.SetDialogueFlag("flag.mall.shelter");foreach(var d in w.Features.Where(f=>f.Kind=="door"&&f.Id.Contains("office")))d.Used=true;w.PartyLeader.Pos=new(13,28);w.Party.Skip(1).First().Pos=new(13,27);w.Reveal();}return w;
  }
  void Hours(World w,int hours){for(int i=0;i<hours;i++)w.AdvanceCampaignSeconds(3600);}
  string path=Path.Combine(Path.GetTempPath(),"zombie-survival-"+Guid.NewGuid()+".json");
  var w=Fresh();var a=w.PartyLeader;var sam=w.Party.Skip(1).First();var site=w.Features.Single(f=>w.IsSecuringSite(f));
  check(w.DailySurvivalEnabled&&definition.SchemaVersion==11&&a.Needs.NextDailyMinute==1920,"Phase 13 daily survival begins with a full 24-hour intake window");
  check(!Fresh(false).PreviewRest(RestKind.LongRest).Available,"Phase 13 refuge rest requires established shelter and gathered party");
  int food=site.Contents.Items.Where(s=>s.ItemId=="item.commodity.001").Sum(s=>s.Quantity);int water=site.Contents.Items.Where(s=>s.ItemId=="item.commodity.004").Sum(s=>s.Quantity);
  check(w.SupplyParty()==4&&a.Needs.FoodTaken&&a.Needs.WaterTaken,"Phase 13 party provisioning consumes individual daily food and water from the nearby physical stockpile");
  check(site.Contents.Items.Where(s=>s.ItemId=="item.commodity.001").Sum(s=>s.Quantity)==food-2&&site.Contents.Items.Where(s=>s.ItemId=="item.commodity.004").Sum(s=>s.Quantity)==water-2&&w.SupplyParty()==0,"Phase 13 repeated provisioning cannot consume twice in one daily window");
  Hours(w,23);check(a.Needs.DaysWithoutWater==0&&a.Needs.FoodTaken,"Phase 13 daily intake remains met before its boundary");w.Save(path);w=World.Load(path);a=w.PartyLeader;Hours(w,1);
  check(!a.Needs.FoodTaken&&a.Needs.DaysWithoutWater==1&&a.Needs.Exhaustion==0,"Phase 13 exact paid daily boundary resets intake without shortage or load replay");
  Hours(w,23);check(a.Needs.DaysWithoutWater==1&&a.Needs.Exhaustion==0,"Phase 13 before 48 dry hours there is no premature exhaustion");Hours(w,1);
  check(a.Needs.DaysWithoutWater==2&&a.Needs.Exhaustion==1,"Phase 13 48 hours without water adds one exhaustion");
  w.Save(path);var loaded=World.Load(path);check(loaded.PartyLeader.Needs.Exhaustion==1&&loaded.PartyLeader.Needs.NextDailyMinute==a.Needs.NextDailyMinute,"Phase 13 shortage and personal daily deadlines persist exactly");Hours(w,24);
  check(a.Needs.Exhaustion==2&&a.EffectiveSpeedFeet==a.Character.SpeedFeet-10,"Phase 13 fatigue increases once per day and reduces combat movement");
  w=Fresh();a=w.PartyLeader;sam=w.Party.Skip(1).First();int before=a.AttackBonus;a.Needs.Exhaustion=2;check(a.AttackBonus==before-2,"Phase 13 fatigue attack penalty uses the common attack preview/roll stat");
  int hp=a.Hp;a.Hp=1;int bandages=w.AvailableSurvivalSupply(a,"bandage");check(w.UseSurvivalSupply(a,a,"bandage")&&a.Hp>1&&a.Needs.Treated&&w.AvailableSurvivalSupply(a,"bandage")==bandages-1,"Phase 13 first aid consumes one unit and heals using the normal HP authority");
  int healed=a.Hp;check(!w.UseSurvivalSupply(a,a,"first-aid")&&a.Hp==healed,"Phase 13 switching supply type cannot bypass the per-patient first-aid limit");
  w.Save(path);w=World.Load(path);a=w.PartyLeader;check(a.Needs.Treated&&!w.UseSurvivalSupply(a,a,"bandage"),"Phase 13 first-aid lock survives saving");
  check(w.PerformRest(RestKind.ShortRest).Completed&&a.Needs.Treated,"Phase 13 short rest advances one hour without resetting treatment");
  int xp=w.ResourceXpAwarded;check(w.PerformRest(RestKind.LongRest).Completed&&a.Hp==a.MaxHp&&a.Needs.Exhaustion==1&&!a.Needs.Treated&&w.ResourceXpAwarded==xp,"Phase 13 fed hydrated long rest heals, removes one fatigue and resets care without XP changes");
  check(!w.PreviewRest(RestKind.LongRest).Available,"Phase 13 repeated long rests cannot farm recovery");
  Hours(w,16);check(w.PreviewRest(RestKind.LongRest).Available,"Phase 13 recovery becomes available 24 hours after the previous rest started");
  w=Fresh();a=w.PartyLeader;site=w.Features.Single(f=>w.IsSecuringSite(f));site.Contents.Items.Clear();a.Hp=2;check(w.PerformRest(RestKind.LongRest).Completed&&a.Hp==2,"Phase 13 resting without physical food/water grants no long-rest healing");
  w=Fresh();a=w.PartyLeader;sam=w.Party.Skip(1).First();sam.Hp=0;sam.Character.SetCondition("unconscious","Unconscious",true);check(w.UseSurvivalSupply(a,sam,"first-aid")&&sam.Alive&&!sam.Unconscious,"Phase 13 nearby conscious carer can treat an unconscious survivor");
  sam.Character.SetCondition("dead","Dead",true);check(!w.UseSurvivalSupply(a,sam,"first-aid"),"Phase 13 medical supplies cannot resurrect a dead survivor");
  w=Fresh(false);a=w.PartyLeader;var box=w.Features.Single(f=>f.Id=="feature.mall.cafe-box");var eligible=box.Contents.Items.First(s=>s.ItemId=="item.commodity.004");box.Contents.Items.Remove(eligible);a.Inventory.Items.Add(eligible);int sv=w.UnsecuredSv(eligible);check(sv>0&&w.UseSurvivalSupply(a,a,"water")&&w.SecuredResourceUnits.Count==0&&w.ResourceXpAwarded==0,"Phase 13 consuming a found resource before securing destroys its provenance without XP");
  w=Fresh();a=w.PartyLeader;site=w.Features.Single(f=>w.IsSecuringSite(f));box=w.Features.Single(f=>f.Id=="feature.mall.cafe-box");eligible=box.Contents.Items.First(s=>s.ItemId=="item.commodity.004");box.Contents.Items.Remove(eligible);a.Inventory.Items.Add(eligible);check(w.SecureCarriedResources(a,site)>0,"Phase 13 test physically banks a recovered supply through normal deposit");xp=w.ResourceXpAwarded;int secured=w.SecuredSurvivalValue;check(w.UseSurvivalSupply(a,a,"water")&&w.ResourceXpAwarded==xp&&w.SecuredSurvivalValue==secured,"Phase 13 consuming stockpiled resources preserves earned permanent XP");w.Save(path);check(World.Load(path).ResourceXpAwarded==xp,"Phase 13 consumed banked resource ledger remains load-valid");
  var invalid=CampaignLoader.Clone(definition);invalid.SchemaVersion=10;check(CampaignLoader.Validate(invalid).Count>0,"Phase 13 survival metadata requires schema 11");invalid=CampaignLoader.Clone(definition);invalid.Items[0].SurvivalUse="antibiotic-magic";check(CampaignLoader.Validate(invalid).Count>0,"Phase 13 unknown medical supply behavior is rejected");
  w=Fresh();w.Save(path);var root=JsonNode.Parse(File.ReadAllText(path))!;root["Actors"]![0]!["Needs"]!["Exhaustion"]=7;File.WriteAllText(path,root.ToJsonString());bool rejected=false;try{World.Load(path);}catch(InvalidDataException){rejected=true;}check(rejected,"Phase 13 invalid persisted needs are rejected");
  var old=World.New(CampaignLoader.Load(CampaignLoader.BundledHordePath));old.Save(path);root=JsonNode.Parse(File.ReadAllText(path))!;root["Version"]=25;foreach(var actor in root["Actors"]!.AsArray())actor!.AsObject().Remove("Needs");File.WriteAllText(path,root.ToJsonString());var legacy=World.Load(path);check(legacy.Version==World.CurrentSaveVersion&&!legacy.DailySurvivalEnabled&&legacy.Campaign!.SchemaVersion==10,"Phase 13 v25 saves retain their original campaign with no retroactive hunger");
  w=Fresh();Hours(w,24*7);check(w.Mode==Mode.Defeat&&w.Party.All(a=>a.Dead),"Phase 13 prolonged untreated shortage reaches terminal exhaustion and defeat");
  w=Fresh();a=w.PartyLeader;Hours(w,23);check(w.UseSurvivalSupply(a,a,"water"),"Phase 13 a late-window drink records its actual simulation timestamp");Hours(w,47);check(a.Needs.Exhaustion==0,"Phase 13 daily window rollover does not cause dehydration before 48 hours since drinking");Hours(w,1);check(a.Needs.Exhaustion==1,"Phase 13 dehydration resolves exactly 48 hours after an off-boundary drink");
  w=Fresh(false);a=w.PartyLeader;var enemy=w.Actors.First(e=>e.Enemy);enemy.Pos=new(6,16);a.Pos=new(5,16);w.StartCombat(enemy.Group);w.Order=[a.Id,enemy.Id];w.Turn=0;int startMinute=w.CampaignMinutes;double seconds=w.CampaignSeconds;w.AdvanceCampaignSeconds(60);check(w.CampaignMinutes==startMinute,"Phase 13 exploration clock cannot double count combat time");for(int i=0;i<10;i++){w.EndTurn();w.EndTurn();}check(w.CampaignMinutes==startMinute+1&&Math.Abs(w.CampaignSeconds-seconds)<.001,"Phase 13 ten full combat rounds advance one minute, independent of actor count");
  var travel=CampaignLoader.Clone(definition);var home=travel.Areas[0];home.EntryPoints.Add(new(){Id="entry.survival.home",Position=new(39,16)});home.Features.Add(new(){Id="feature.survival.exit",Name="Test service exit",Kind="transition",Position=new(39,17)});
  var annex=new CampaignArea{Id="area.survival.annex",Name="Test annex",Source=new(){AuditId="test.survival.travel",PageStart=57,PageEnd=57},EntryPoints=[new(){Id="entry.survival.annex",Position=new(5,16)}],Features=[new(){Id="feature.survival.return",Name="Return",Kind="transition",Position=new(5,17)}],Floors=(from xx in Enumerable.Range(3,6) from yy in Enumerable.Range(14,6) select new Cell(xx,yy)).ToList()};
  travel.Areas.Add(annex);travel.Connections.Add(new(){Id="route.survival.out",FromAreaId=home.Id,ToAreaId=annex.Id,ExitFeatureId="feature.survival.exit",EntryPointId="entry.survival.annex",Minutes=1});travel.Connections.Add(new(){Id="route.survival.back",FromAreaId=annex.Id,ToAreaId=home.Id,ExitFeatureId="feature.survival.return",EntryPointId="entry.survival.home",Minutes=1});
  w=World.New(travel);a=w.PartyLeader;a.Pos=new(38,17);w.Party.Skip(1).First().Pos=new(39,16);w.Reveal();Hours(w,47);w.AdvanceCampaignSeconds(3540);check(a.Needs.Exhaustion==0&&w.TravelTo(annex.Id)&&a.Needs.Exhaustion==1,"Phase 13 legal travel resolves a dehydration boundary without offscreen resource access");w.Save(path);w=World.Load(path);a=w.PartyLeader;a.Pos=new(5,16);w.Party.Skip(1).First().Pos=new(6,17);w.Reveal();check(w.TravelTo(home.Id)&&a.Needs.Exhaustion==1,"Phase 13 save/load and return travel preserve needs without replaying the previous penalty");
  File.Delete(path);
  ExpeditionTests.RunFullRoute((ok,name)=>check(ok,name.Replace("Phase 8","Phase 13 survival expedition")),CampaignLoader.BundledSurvivalPath,true,true);
 }
}
