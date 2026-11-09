using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed class CampaignInfrastructure
{
 public SourceReference Source {get;set;}=new();
 public string DoorFeatureId {get;set;}="";
 public List<CampaignStack> BarricadeCost {get;set;}=[];
 public List<CampaignStack> GeneratorCost {get;set;}=[];
 public List<CampaignStack> WaterStationCost {get;set;}=[];
 public List<CampaignStack> RepairCost {get;set;}=[];
 public string FuelItemId {get;set;}="";
 public string DirtyWaterItemId {get;set;}="";
 public string FilterItemId {get;set;}="";
 public string CleanWaterItemId {get;set;}="";
 public int BarricadeMaxHp {get;set;}=24;
 public int FuelMinutesPerUnit {get;set;}=60;
 public int FuelCapacityMinutes {get;set;}=240;
 public int BarricadeXp {get;set;}=10;
 public int GeneratorXp {get;set;}=50;
}
public sealed class InfrastructureState
{
 public int LastMinute {get;set;}
 public int BarricadeHp {get;set;}
 public bool BarricadeEngaged {get;set;}
 public bool GeneratorInstalled {get;set;}
 public bool WaterStationInstalled {get;set;}
 public bool Running {get;set;}
 public int FuelMinutes {get;set;}
 public int PurifiedUnits {get;set;}
 public List<string> ConstructionCredits {get;set;}=[];
 public int XpAwarded {get;set;}
 public int XpRemainder {get;set;}
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorksAction { BuildBarricade,RepairBarricade,Unbar,Secure,InstallGenerator,Refuel,TogglePower,InstallWaterStation,PurifyWater }
public static class InfrastructureRules
{
 public static void Validate(CampaignDefinition c,Action<string,string,string> error)
 {
  if(c.Infrastructure is not {} d)return;
  if(c.SchemaVersion<13||c.Shelter==null||!c.DailySurvival||!c.NoiseAwareness)error("schema","infrastructure","Infrastructure requires schema 13, shelter, daily survival and local awareness.");
  if(c.Shelter==null)return;
  var area=c.Areas.FirstOrDefault(a=>a.Id==c.Shelter.AreaId);
  if(area==null||!area.Features.Any(f=>f.Id==d.DoorFeatureId&&f.Kind=="door"))error("infrastructure","doorFeatureId","Barricade must name a door in the shelter area.");
  foreach(var recipe in new[]{d.BarricadeCost,d.GeneratorCost,d.WaterStationCost,d.RepairCost})
   if(recipe==null||recipe.Count is <1 or >4||recipe.Any(s=>s==null||s.Quantity is <1 or >20||!c.Items.Any(i=>i.Id==s.ItemId&&i.Kind==ItemKind.Misc&&i.SurvivalUse==""))||recipe.Select(s=>s.ItemId).Distinct().Count()!=recipe.Count)error("infrastructure","costs","Recipes require 1–4 distinct miscellaneous material inputs, quantities 1–20.");
  var inputs=new[]{d.FuelItemId,d.DirtyWaterItemId,d.FilterItemId};
  if(inputs.Distinct().Count()!=3||inputs.Any(id=>!c.Items.Any(i=>i.Id==id&&i.Kind==ItemKind.Misc&&i.SurvivalUse==""))||!c.Items.Any(i=>i.Id==d.CleanWaterItemId&&i.SurvivalUse=="water"))error("infrastructure","inputs","Distinct fuel, untreated water and filter inputs plus clean drinking-water output are required.");
  if(d.BarricadeMaxHp is <1 or >200||d.FuelMinutesPerUnit is <10 or >240||d.FuelCapacityMinutes<d.FuelMinutesPerUnit||d.FuelCapacityMinutes>1440||d.BarricadeXp is <0 or >10000||d.GeneratorXp is <0 or >10000)error("infrastructure","rates","Invalid durability, fuel capacity or one-time XP values.");
  if(d.Source==null||string.IsNullOrWhiteSpace(d.Source.AuditId)||d.Source.PageStart<1||d.Source.PageEnd<d.Source.PageStart||d.Source.PageEnd>513)error("source","infrastructure.source","A source/adaptation reference is required.");
 }
}
public sealed partial class World
{
 public InfrastructureState InfrastructureState {get;set;}=new();
 [JsonIgnore] public bool InfrastructureEnabled=>Campaign?.Infrastructure!=null;
 [JsonIgnore] public bool ShelterPowered=>InfrastructureEnabled&&InfrastructureState.Running&&InfrastructureState.FuelMinutes>0;
 Feature? InfrastructureDoor()=>!InfrastructureEnabled?null:(CurrentTravelDestinationId==Campaign!.Shelter!.AreaId?Features:StoredTravelAreas.FirstOrDefault(a=>a.DestinationId==Campaign.Shelter.AreaId)?.Features)?.FirstOrDefault(f=>f.Id==Campaign.Infrastructure!.DoorFeatureId);
 public bool IsEngagedBarricade(Feature f)=>InfrastructureEnabled&&ReferenceEquals(f,InfrastructureDoor())&&InfrastructureState.BarricadeHp>0&&InfrastructureState.BarricadeEngaged;
 public bool PoweredShelterCell(Cell c)=>(ShelterPowered||RoofGridOnline&&LowerMallProgress(204)?.Events.Contains("relay-restored")==true)&&CurrentTravelDestinationId==Campaign!.Shelter!.AreaId&&CampaignZoneAt(c)?.FloorStyle=="shelter";
 public int WorksMaterialCount(string id)=>!InfrastructureEnabled?0:PartyLeader.Inventory.Items.Where(s=>EquipmentRules.MaterialMatches(s,id)).Sum(s=>s.Quantity)+(ShelterHomeStockpile()?.Contents.Items.Where(s=>EquipmentRules.MaterialMatches(s,id)).Sum(s=>s.Quantity)??0);
 public int WorksMinutes(WorksAction a)=>Math.Max(0,(a switch{WorksAction.BuildBarricade=>30,WorksAction.RepairBarricade=>15,WorksAction.InstallGenerator=>60,WorksAction.InstallWaterStation=>30,WorksAction.PurifyWater=>10,_=>0})/(PartyLeader.ClassIs("smart")&&a is WorksAction.BuildBarricade or WorksAction.InstallGenerator or WorksAction.InstallWaterStation?2:1));
 List<CampaignStack> WorksCost(WorksAction a)
 {
  var d=Campaign!.Infrastructure!;
  return a switch{WorksAction.BuildBarricade=>d.BarricadeCost,WorksAction.RepairBarricade=>d.RepairCost,WorksAction.InstallGenerator=>d.GeneratorCost,WorksAction.InstallWaterStation=>d.WaterStationCost,WorksAction.Refuel=>[new(){ItemId=d.FuelItemId}],WorksAction.PurifyWater=>[new(){ItemId=d.DirtyWaterItemId},new(){ItemId=d.FilterItemId}],_=>[]};
 }
 public string WorksCostText(WorksAction a)=>!InfrastructureEnabled?"Unavailable":string.Join(", ",WorksCost(a).Select(s=>$"{s.Quantity} {ResolveItemDefinition(s.ItemId).Name}"));
 (string Key,int Xp) WorksCredit(WorksAction a)=>a switch{WorksAction.BuildBarricade=>("barricade",Campaign!.Infrastructure!.BarricadeXp),WorksAction.InstallGenerator=>("generator",Campaign!.Infrastructure!.GeneratorXp),_=>("",0)};
 static void ConsumeWorksCost(IEnumerable<InventoryState> inventories,IEnumerable<CampaignStack> cost)
 {
  foreach(var ingredient in cost)
  {
   int remaining=ingredient.Quantity;
   foreach(var inventory in inventories)foreach(var stack in inventory.Items.Where(s=>EquipmentRules.MaterialMatches(s,ingredient.ItemId)).ToArray())
   {int n=Math.Min(remaining,stack.Quantity);stack.ConsumeResourceUnits(n);stack.Quantity-=n;remaining-=n;if(stack.Quantity==0)inventory.Items.Remove(stack);if(remaining==0)break;}
   if(remaining!=0)throw new InvalidOperationException("Material preflight disagreed with payment.");
  }
 }
 public string WorksReason(WorksAction a)
 {
  if(!InfrastructureEnabled)return "This campaign does not enable refuge works.";
  if(!Enum.IsDefined(a))return "Unknown works action.";
  string safety=ShelterManagementReason();if(safety!="")return safety;
  var s=InfrastructureState;var d=Campaign!.Infrastructure!;var door=InfrastructureDoor()!;
  if((long)CampaignMinutes+WorksMinutes(a)+1440>int.MaxValue)return "The campaign clock limit has been reached.";
  if(a==WorksAction.BuildBarricade&&s.BarricadeHp>0)return "A barricade already exists. Repair or unbar it.";
  if(a==WorksAction.RepairBarricade&&(s.BarricadeHp<=0||s.BarricadeHp>=d.BarricadeMaxHp))return "Repair requires a damaged standing barricade; a destroyed one must be rebuilt.";
  if(a==WorksAction.Unbar&&(s.BarricadeHp<=0||!s.BarricadeEngaged))return "There is no secured barricade to unbar.";
  if(a==WorksAction.Secure&&(s.BarricadeHp<=0||s.BarricadeEngaged))return "A standing, unbarred barricade is required.";
  if(a is WorksAction.BuildBarricade or WorksAction.Secure&&Actors.Any(actor=>actor.Alive&&actor.Position.Distance(door.Pos.Center)<.8f))return "Move everyone clear of the office doorway before securing it.";
  if(a==WorksAction.InstallGenerator&&s.GeneratorInstalled)return "The generator is already installed.";
  if(a is WorksAction.Refuel or WorksAction.TogglePower&&!s.GeneratorInstalled)return "Install the generator first.";
  if(a==WorksAction.Refuel&&s.FuelMinutes>d.FuelCapacityMinutes-d.FuelMinutesPerUnit)return "The generator fuel capacity is full.";
  if(a==WorksAction.TogglePower&&!s.Running&&s.FuelMinutes<1)return "Add a physical fuel unit first.";
  if(a==WorksAction.InstallWaterStation&&s.WaterStationInstalled)return "The water station is already installed.";
  if(a==WorksAction.PurifyWater&&(!s.WaterStationInstalled||!ShelterPowered||s.FuelMinutes<10))return "Install the water station and run the generator with at least ten fuel-minutes remaining.";
  foreach(var cost in WorksCost(a))if(WorksMaterialCount(cost.ItemId)<cost.Quantity)return $"Requires {cost.Quantity} {ResolveItemDefinition(cost.ItemId).Name} in the leader's backpack or refuge stockpile.";
  if(a==WorksAction.PurifyWater)
  {
   var leader=new InventoryState{MaxSlots=PartyLeader.Inventory.MaxSlots,Items=PartyLeader.Inventory.Items.Select(s=>s.Clone()).ToList()};var site=ShelterHomeStockpile()!.Contents;var copy=new InventoryState{MaxSlots=site.MaxSlots,Items=site.Items.Select(s=>s.Clone()).ToList()};ConsumeWorksCost([leader,copy],WorksCost(a));
   if(!copy.CanAdd(new(){ItemId=d.CleanWaterItemId,CampaignGear=ResolveItemDefinition(d.CleanWaterItemId)}))return "Make stockpile space for the purified water.";
  }
  var credit=WorksCredit(a);if(credit.Key!=""&&!s.ConstructionCredits.Contains(credit.Key))
  {var party=Party.Where(p=>!p.Dead).ToArray();int each=(s.XpRemainder+credit.Xp)/party.Length;if(party.Any(p=>(long)p.Character.Experience+each>int.MaxValue))return "Construction XP would exceed an experience limit.";}
  return "";
 }
 public bool PerformWorks(WorksAction a)
 {
  string reason=WorksReason(a);if(reason!=""){LogSystem(reason,true);return false;}
  var s=InfrastructureState;var d=Campaign!.Infrastructure!;StopAll();ConsumeWorksCost([PartyLeader.Inventory,ShelterHomeStockpile()!.Contents],WorksCost(a));PartyLeader.QuickAccess.Normalize(PartyLeader);
  switch(a)
  {
   case WorksAction.BuildBarricade:s.BarricadeHp=d.BarricadeMaxHp;s.BarricadeEngaged=true;InfrastructureDoor()!.Used=false;break;
   case WorksAction.RepairBarricade:s.BarricadeHp=Math.Min(d.BarricadeMaxHp,s.BarricadeHp+8);break;
   case WorksAction.Unbar:s.BarricadeEngaged=false;InfrastructureDoor()!.Used=true;break;
   case WorksAction.Secure:s.BarricadeEngaged=true;InfrastructureDoor()!.Used=false;break;
   case WorksAction.InstallGenerator:s.GeneratorInstalled=true;break;
   case WorksAction.Refuel:s.FuelMinutes+=d.FuelMinutesPerUnit;break;
   case WorksAction.TogglePower:s.Running=!s.Running;if(s.Running)EmitLocalNoise(PartyLeader,"generator",60,ShelterHomeStockpile()!.Pos);break;
   case WorksAction.InstallWaterStation:s.WaterStationInstalled=true;break;
   case WorksAction.PurifyWater:ShelterHomeStockpile()!.Contents.Add(CreateItemStack(d.CleanWaterItemId));s.PurifiedUnits=checked(s.PurifiedUnits+1);AssignMissingItemIds();break;
  }
  var credit=WorksCredit(a);
  if(credit.Key!=""&&!s.ConstructionCredits.Contains(credit.Key))
  {
   var party=Party.Where(p=>!p.Dead).ToArray();int pool=s.XpRemainder+credit.Xp,each=pool/party.Length;
   foreach(var p in party)p.Character.Experience+=each;s.XpAwarded+=each*party.Length;s.XpRemainder=pool%party.Length;s.ConstructionCredits.Add(credit.Key);
   Message(MessageCategory.Quest,$"Construction secured: {credit.Xp} SV, {each} XP per expedition member; {s.XpRemainder} pooled. This upgrade is credited once.");
  }
  TensionWorks(a);
  CampaignMinutes=checked(CampaignMinutes+WorksMinutes(a));ProcessSurvivalTime();Reveal();SyncCampaignObjectives();
  Message(MessageCategory.Rest,$"Refuge works: {a} complete. {WorksMinutes(a)} minutes elapsed; materials spent permanently.");return true;
 }
 void ProcessInfrastructureTime()
 {
  if(!InfrastructureEnabled)return;
  var s=InfrastructureState;int elapsed=Math.Max(0,CampaignMinutes-s.LastMinute);s.LastMinute=CampaignMinutes;
  if(!s.Running)return;s.FuelMinutes=Math.Max(0,s.FuelMinutes-elapsed);
  if(s.FuelMinutes==0){s.Running=false;Message(MessageCategory.Rest,"The refuge generator runs out of fuel. Lighting and powered purification stop.");}
 }
 bool OpenOwnedBarricade(Actor a,Feature door,bool combat)
 {
  if(!IsEngagedBarricade(door)||!Party.Contains(a)||!a.Alive||a.Position.Distance(door.Pos.Center)>1.25f)return false;
  if(combat&&!SpendAction(a,"Unbar office door"))return false;
  Stop(a);TensionWorks(WorksAction.Unbar);InfrastructureState.BarricadeEngaged=false;door.Used=true;EmitLocalNoise(a,"door",20,door.Pos);Message(MessageCategory.Combat,$"{a.Name} unbars the office door. Existing barricade HP is retained.");Reveal();if(!combat)CheckEncounter();return true;
 }
 bool HeardThroughBarricade(Actor enemy)
 {
  if(!InfrastructureEnabled||CurrentTravelDestinationId!=Campaign!.Shelter!.AreaId||!enemy.Enemy||enemy.Awareness.State==AwarenessKind.Idle||enemy.Awareness.MemorySeconds<=0||enemy.Awareness.LastKnown is not {} known||Party.Any(p=>CanEnemySee(enemy,p)))return false;
  var door=InfrastructureDoor()!;if(!IsEngagedBarricade(door))return false;
  var from=enemy.Position;var to=known.Center;int steps=Math.Max(1,(int)MathF.Ceiling(from.Distance(to)/.08f));bool crosses=false;
  for(int i=1;i<=steps;i++)if((from+(to-from)*(i/(float)steps)).Cell==door.Pos){crosses=true;break;}
  return crosses&&CanSee(from,door.Pos.Center);
 }
 public bool CanBashBarricade(Actor a)
 {
  var door=InfrastructureDoor();return door!=null&&IsEngagedBarricade(door)&&Actors.Contains(a)&&a.Alive&&!IsMoving(a)&&a.Range<=1&&a.Position.Distance(door.Pos.Center)<=1.25f&&CanSee(a.Position,door.Pos.Center)&&(Mode==Mode.Combat?CanAttack(a):Mode==Mode.Explore&&a.Enemy&&HeardThroughBarricade(a));
 }
 public bool AttackBarricade(Actor a)
 {
  if(Mode!=Mode.Combat||!CanBashBarricade(a))return false;
  if(!a.Economy.SpendAttack())return false;
  a.Awareness.BarricadeAttackSeconds=0;
  ResolveBarricadeStrike(a);return true;
 }
 void ResolveBarricadeStrike(Actor a)
 {
  int roll=Roll(20);bool hit=roll==20||roll!=1&&roll+a.AttackBonus>=12;
  if(!hit){Message(MessageCategory.Combat,$"{a.Name} strikes the office barricade and misses (AC 12).");return;}
  int damage=Math.Max(1,RollDamage(a.DamageDice*(roll==20?2:1),a.Die)+a.DamageBonus);InfrastructureState.BarricadeHp=Math.Max(0,InfrastructureState.BarricadeHp-damage);
  Message(MessageCategory.Damage,$"{a.Name} hits the office barricade for {damage}. {InfrastructureState.BarricadeHp} HP remain.");
  if(InfrastructureState.BarricadeHp==0){ChangeTension(4,"Barricade breached");InfrastructureState.BarricadeEngaged=false;InfrastructureDoor()!.Used=true;Message(MessageCategory.Combat,"The office barricade breaks. The doorway is open.");Reveal();if(Mode==Mode.Explore)RefreshLocalSight();}
 }
 List<WorldPoint>? BarricadeApproach(Actor enemy)
 {
  if(!HeardThroughBarricade(enemy))return null;var door=InfrastructureDoor()!;
  if(enemy.Position.Distance(door.Pos.Center)<=1.25f)return [];
  foreach(var cell in door.Pos.Neighbors().Where(c=>CanStand(c.Center,enemy)).OrderBy(c=>c.Distance(enemy.Pos)))
  {var path=Path(enemy,cell.Center);if(path.Count>0)return path;}
  return null;
 }
 bool UpdateExplorationBarricadeAttack(Actor enemy,float delta)
 {
  if(!CanBashBarricade(enemy)){enemy.Awareness.BarricadeAttackSeconds=0;return false;}
  Stop(enemy);enemy.Awareness.State=AwarenessKind.Investigating;
  enemy.Awareness.BarricadeAttackSeconds=Math.Min(6,enemy.Awareness.BarricadeAttackSeconds+delta);
  if(enemy.Awareness.BarricadeAttackSeconds>=6){enemy.Awareness.BarricadeAttackSeconds=0;ResolveBarricadeStrike(enemy);}return true;
 }
 void ValidateInfrastructure()
 {
  var s=InfrastructureState;if(s==null||s.ConstructionCredits==null||s.ConstructionCredits.Distinct().Count()!=s.ConstructionCredits.Count||s.ConstructionCredits.Any(id=>id is not ("barricade" or "generator"))||s.LastMinute<0||s.XpAwarded<0||s.XpRemainder is <0 or >5||s.PurifiedUnits<0)throw new InvalidDataException("Invalid infrastructure state or construction ledger.");
  if(!InfrastructureEnabled)
  {if(s.BarricadeHp!=0||s.BarricadeEngaged||s.GeneratorInstalled||s.WaterStationInstalled||s.Running||s.FuelMinutes!=0||s.ConstructionCredits.Count>0||s.XpAwarded!=0||s.XpRemainder!=0||s.PurifiedUnits!=0)throw new InvalidDataException("Infrastructure on a legacy campaign.");return;}
  var d=Campaign!.Infrastructure!;long credit=s.ConstructionCredits.Sum(id=>id=="barricade"?d.BarricadeXp:d.GeneratorXp);
  if(s.LastMinute!=CampaignMinutes||s.BarricadeHp<0||s.BarricadeHp>d.BarricadeMaxHp||s.FuelMinutes<0||s.FuelMinutes>d.FuelCapacityMinutes||s.Running&&(!s.GeneratorInstalled||s.FuelMinutes==0)||!s.GeneratorInstalled&&s.FuelMinutes!=0||s.GeneratorInstalled!=s.ConstructionCredits.Contains("generator")||s.BarricadeHp>0&&!s.ConstructionCredits.Contains("barricade")||s.BarricadeEngaged&&(s.BarricadeHp==0||InfrastructureDoor()==null||InfrastructureDoor()!.Used)||(long)s.XpAwarded+s.XpRemainder!=credit||s.PurifiedUnits>0&&!s.WaterStationInstalled)throw new InvalidDataException("Infrastructure fuel, door, upgrade or XP state disagrees.");
 }
}
