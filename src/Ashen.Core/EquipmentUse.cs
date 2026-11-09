using System.Text.Json.Serialization;
namespace Ashen.Core;
public sealed class EquipmentEffect
{
 public string Kind {get;set;}="";
 public string ActorId {get;set;}="";
 public string AreaId {get;set;}="";
 public string ItemId {get;set;}="";
 public Cell Cell {get;set;}
 public int RadiusFeet {get;set;}
 public double Expires {get;set;}
}
public sealed class EquipmentMine
{
 public string OwnerId {get;set;}="";
 public string AreaId {get;set;}="";
 public string ItemId {get;set;}="";
 public Cell Cell {get;set;}
 public bool Spent {get;set;}
}
public sealed partial class World
{
 public List<EquipmentEffect> EquipmentEffects {get;set;}=[];
 public List<EquipmentMine> EquipmentMines {get;set;}=[];
 double EquipmentClock=>CampaignMinutes*60d+CampaignSeconds;
 public bool EquipmentBlind(Actor a)=>!LowerTraits(a).Contains("final-eyeless")&&(a.Character.HasCondition("blinded")||EquipmentEnabled&&EquipmentEffects.Any(e=>e.Kind=="blind"&&e.ActorId==a.Id&&e.Expires>EquipmentClock));
 public bool EquipmentSmoke(Cell cell)=>EquipmentEnabled&&EquipmentEffects.Any(e=>e.Kind=="smoke"&&e.AreaId==CurrentTravelDestinationId&&e.Expires>EquipmentClock&&e.Cell.Distance(cell)*5<=e.RadiusFeet);
 public bool EquipmentLitCell(Cell cell)=>EquipmentEnabled&&Explored.Contains(cell)&&(Exterior31Power||EquipmentEffects.Any(e=>e.Kind=="light"&&e.AreaId==CurrentTravelDestinationId&&e.Expires>EquipmentClock&&Actors.Any(a=>a.Id==e.ActorId&&a.Alive&&a.Pos.Distance(cell)<=3&&Line(a.Position,cell.Center))));
 void UpdateEquipmentTime()
 {
  if(!EquipmentEnabled)return;EquipmentEffects.RemoveAll(e=>e.Expires<=EquipmentClock);
  foreach(var light in EquipmentEffects.Where(e=>e.Kind=="light")){var owner=Actors.FirstOrDefault(a=>a.Id==light.ActorId);if(owner!=null){light.AreaId=CurrentTravelDestinationId;light.Cell=owner.Pos;}}
 }
 ItemStack? EquipmentOwned(Actor a,string instance)=>a.Inventory.Items.Concat(a.Equipment.All()).FirstOrDefault(s=>s.InstanceId==instance);
 static void ConsumeEquipmentStack(InventoryState inventory,ItemStack stack)
 {stack.ConsumeResourceUnits(1);if(stack.Definition.MaxCharges>0){stack.Charges--;if(stack.Charges>0)return;}stack.Quantity--;if(stack.Quantity==0)inventory.Items.Remove(stack);}
 public string EquipmentUseReason(Actor a,string instance,Actor? target=null,Cell? cell=null)
 {
  if(!EquipmentEnabled||!Party.Contains(a)||!a.Alive)return "Choose a conscious active survivor in an equipment-enabled campaign.";
  var stack=EquipmentOwned(a,instance);if(stack==null)return "This survivor does not own the selected item.";
  if(stack.Definition.Equipment?.Family=="ext30-flashlight")return LowerItemUseReason(a,stack);
  var p=stack.Definition.Equipment;if(p?.Status is "scripted" or "reference")return p.Note;
  if(Mode is not (Mode.Explore or Mode.Combat)||ReactionInterruptActive||PendingReaction!=null||IsMoving(a))return "Finish movement and interrupts first.";
  if(p?.Use=="attack")
  {if(a.Equipment.Weapon!=stack)return "Equip this physical weapon first.";if(Mode!=Mode.Combat||target==null)return "Choose a visible enemy during this survivor's combat turn.";return PreviewAttack(a,target,AttackKind.Basic).Reason;}
  if(Mode==Mode.Combat&&!CanAct(a))return "Use this item on its owner's turn with an available Action.";
  if(!a.Inventory.Items.Contains(stack))return "Stow the item in this survivor's backpack before using it.";
  var survival=SupplyUse(stack);if(survival!="")return SurvivalSupplyReason(a,target??a,survival);
  if(p==null||p.Use=="supply")return "Carry, secure or trade this supply; no direct tactical power is defined. Scenario/crafting use must be authored.";
  if(stack.Definition.MaxCharges>0&&stack.Charges<1)return "This item has no remaining charges.";
  if(p.Use is "throw" or "blast" or "mine" or "flash" or "smoke")
  {
   var point=cell??target?.Pos;if(point==null||!Explored.Contains(point.Value)||!IsFloor(point.Value)||a.Pos.Distance(point.Value)*5>stack.Definition.WeaponRangeFeet+.01f||!Line(a.Position,point.Value.Center))return "Choose a discovered floor position in the source range with clear sight.";
   if(p.Use=="mine"&&(!CanStand(point.Value.Center,a)||EquipmentMines.Count(m=>!m.Spent)>=16||EquipmentMines.Any(m=>!m.Spent&&m.AreaId==CurrentTravelDestinationId&&m.Cell==point)))return "A device needs an empty legal floor cell; at most sixteen may be live.";
   if(p.Use=="throw"&&(target==null||!Actors.Contains(target)||!target.Enemy||!target.Alive||!Visible.Contains(target.Pos)))return "Choose a visible living enemy for this thrown projectile.";
   int effects=p.Use=="flash"?Actors.Count(t=>t.Alive&&t.Pos.Distance(point.Value)*5<=p.BurstFeet):p.Use=="smoke"?1:0;
   if(EquipmentEffects.Count+effects>128)return "Too many active equipment effects; wait for them to expire.";
  }
  if(p.Use=="light")
  {
   if(EquipmentEffects.Count>=128)return "Wait for existing equipment effects to expire.";
   if(EquipmentEffects.Any(e=>e.Kind=="light"&&e.ActorId==a.Id&&e.Expires>EquipmentClock))return "This survivor's light is already operating.";
   if(stack.ItemId!="item.commodity.049"&&!a.Inventory.Items.Any(s=>s.ItemId=="item.commodity.037"))return "Carry a physical battery unit in this backpack.";
   if(stack.ItemId=="item.commodity.049"&&Mode!=Mode.Explore)return "Crank the hand-powered light while exploring.";
  }
  if(p.Use=="lockpick")
  {if(Mode!=Mode.Explore||cell==null||!Features.Any(f=>f.Pos==cell&&f.Locked&&a.Position.Distance(f.Pos.Center)<=1.25f&&CanSee(a.Position,f.Pos.Center)))return "Choose an adjacent locked object while exploring.";}
  if(p.Use=="purify")
  {
   if(Mode!=Mode.Explore)return "Purify drinking water outside combat.";
   var sources=SurvivalSources(a).ToArray();if(!sources.SelectMany(i=>i.Items).Any(s=>s.ItemId==Campaign!.Infrastructure!.DirtyWaterItemId))return "Carry untreated water or stand at the safe refuge stockpile.";
   var copy=new InventoryState{MaxSlots=a.Inventory.MaxSlots,Items=a.Inventory.Items.Select(s=>s.Clone()).ToList()};var copied=copy.Items.Single(s=>s.InstanceId==instance);ConsumeEquipmentStack(copy,copied);
   var dirty=copy.Items.FirstOrDefault(s=>s.ItemId==Campaign!.Infrastructure!.DirtyWaterItemId);float removed=0;if(dirty!=null){removed=dirty.Definition.Weight;ConsumeEquipmentStack(copy,dirty);}
   var water=new ItemStack{ItemId=Campaign!.Infrastructure!.CleanWaterItemId,CampaignGear=ResolveItemDefinition(Campaign.Infrastructure.CleanWaterItemId)};
   if(!copy.CanAdd(water)||a.CarriedWeight-removed-(stack.Charges==1?stack.Weight:0)+water.Weight>a.CarryingCapacity+.01f)return "Make backpack space and carrying capacity for the purified water.";
  }
  if(p.Use=="ammo-pack")
  {var ammo=new ItemStack{ItemId=p.PackAmmoId,Quantity=p.PackQuantity,CampaignGear=ResolveItemDefinition(p.PackAmmoId)};if(!a.Inventory.CanAdd(ammo))return "Make space for the physical loose rounds.";}
  return "";
 }
 public bool UseEquipment(Actor a,string instance,Actor? target=null,Cell? cell=null)
 {
  string reason=EquipmentUseReason(a,instance,target,cell);if(reason!=""){LogSystem(reason,true);return false;}
  var stack=EquipmentOwned(a,instance)!;if(stack.Definition.Equipment?.Family=="ext30-flashlight")return UseLowerMallItem(a,instance);var p=stack.Definition.Equipment;
  if(p?.Use=="attack")return RequestAttack(a,target!,AttackKind.Basic);
  string survival=SupplyUse(stack);if(survival!="")return UseSurvivalSupply(a,target??a,survival,instance);
  if(Mode==Mode.Combat&&!SpendAction(a,"Use "+stack.Definition.Name))return false;
  Stop(a);string id=stack.ItemId;var d=Campaign!.Items.Single(i=>i.Id==id);Cell point=cell??target?.Pos??a.Pos;
  if(p!.Use=="mine")
  {ConsumeEquipmentStack(a.Inventory,stack);EquipmentMines.RemoveAll(m=>m.Spent);EquipmentMines.Add(new(){OwnerId=a.Id,AreaId=CurrentTravelDestinationId,ItemId=id,Cell=point});}
  else if(p.Use is "blast" or "flash" or "smoke")
  {ConsumeEquipmentStack(a.Inventory,stack);ResolveEquipmentBurst(a,d,point);}
  else if(p.Use=="throw")
  {
   ConsumeEquipmentStack(a.Inventory,stack);int roll=Roll(20);int bonus=a.Dex+a.ProficiencyBonus-(a.UsesDailySurvival?a.Needs.Exhaustion:0);
   if(roll==20||roll!=1&&roll+bonus>=target!.Ac+CoverAgainst(a,target)){Hurt(target!,Math.Max(0,RollDamage(d.DamageDice*(roll==20?2:1),d.DamageDie)+p.DamageBonus+a.Dex),p.DamageType,a,roll==20);}EmitLocalNoise(a,"weapon",20);ResolveClassCasualties();
  }
  else if(p.Use=="light")
  {
   if(id!="item.commodity.049"){var battery=a.Inventory.Items.First(s=>s.ItemId=="item.commodity.037");ConsumeEquipmentStack(a.Inventory,battery);}else AdvanceCampaignSeconds(60);
   EquipmentEffects.Add(new(){Kind="light",ActorId=a.Id,AreaId=CurrentTravelDestinationId,ItemId=id,Cell=a.Pos,RadiusFeet=15,Expires=EquipmentClock+1800});
  }
  else if(p.Use=="lockpick")
  {bool success=SurvivorSkillCheck(a,Skill.Lockpicking,12);if(success)Features.First(f=>f.Pos==point&&f.Locked).Locked=false;AdvanceCampaignSeconds(a.ClassIs("agile",3)&&a.Training.Path=="infiltrator"?30:60);}
  else if(p.Use=="purify")
  {
   var source=SurvivalSources(a).First(i=>i.Items.Any(s=>s.ItemId==Campaign.Infrastructure!.DirtyWaterItemId));var dirty=source.Items.First(s=>s.ItemId==Campaign.Infrastructure!.DirtyWaterItemId);ConsumeEquipmentStack(source,dirty);ConsumeEquipmentStack(a.Inventory,stack);a.Inventory.Add(CreateItemStack(Campaign.Infrastructure!.CleanWaterItemId));AdvanceCampaignSeconds(600);
  }
  else if(p.Use=="ammo-pack")
  {ConsumeEquipmentStack(a.Inventory,stack);a.Inventory.Add(CreateItemStack(p.PackAmmoId,p.PackQuantity));}
  AssignMissingItemIds();a.QuickAccess.Normalize(a);Message(MessageCategory.Combat,$"{a.Name} uses {d.Name}. Physical costs and charges are retained.");SyncCampaignObjectives();Reveal();return true;
 }
 void ResolveEquipmentBurst(Actor? owner,CampaignItem d,Cell point)
 {
  var p=d.Equipment!;
  if(p.Use=="smoke")EquipmentEffects.Add(new(){Kind="smoke",ActorId=owner?.Id??"",AreaId=CurrentTravelDestinationId,ItemId=d.Id,Cell=point,RadiusFeet=p.BurstFeet,Expires=EquipmentClock+12});
  else foreach(var victim in Actors.Where(t=>t.Alive&&t.Pos.Distance(point)*5<=p.BurstFeet&&Line(point.Center,t.Position)).ToArray())
  {
   bool saved=SurvivorSave(victim,Ability.Dexterity,p.SaveDc,"equipment-blast");
   if(p.Use=="flash"){if(!saved)EquipmentEffects.Add(new(){Kind="blind",ActorId=victim.Id,AreaId=CurrentTravelDestinationId,ItemId=d.Id,Cell=victim.Pos,RadiusFeet=0,Expires=EquipmentClock+12});}
   else Hurt(victim,SurvivorSaveDamage(victim,Math.Max(0,RollDamage(d.DamageDice,d.DamageDie)+p.DamageBonus),Ability.Dexterity,saved,true),p.DamageType,owner);
  }
  var emitter=owner is {Alive:true}&&Actors.Contains(owner)?owner:Party.FirstOrDefault(a=>a.Alive);if(emitter!=null)EmitLocalNoise(emitter,"explosion",p.NoiseFeet,point);
  ResolveClassCasualties();Reveal();
 }
 void TriggerEquipmentMines()
 {
  if(!EquipmentEnabled)return;
  foreach(var mine in EquipmentMines.Where(m=>!m.Spent&&m.AreaId==CurrentTravelDestinationId).ToArray())if(Actors.Any(a=>a.Enemy&&a.Alive&&a.Pos==mine.Cell))
  {mine.Spent=true;var d=Campaign!.Items.Single(i=>i.Id==mine.ItemId);ResolveEquipmentBurst(Actors.FirstOrDefault(a=>a.Id==mine.OwnerId),d,mine.Cell);}
 }
 void ValidateEquipmentState()
 {
  if(EquipmentEffects==null||EquipmentMines==null||EquipmentEffects.Count>128||EquipmentMines.Count>16||!EquipmentEnabled&&(EquipmentEffects.Count>0||EquipmentMines.Count>0))throw new InvalidDataException("Invalid equipment effect/device collections.");
  var all=Actors.Concat(ShelterResidents.Select(r=>r.Actor)).Concat(StoredTravelAreas.SelectMany(s=>s.Actors)).ToArray();
  foreach(var e in EquipmentEffects)
  {var area=Campaign?.Areas.FirstOrDefault(a=>a.Id==e.AreaId);if(e==null||e.Kind is not ("light" or "smoke" or "blind")||!double.IsFinite(e.Expires)||e.Expires<=EquipmentClock||e.Expires>EquipmentClock+1800.001||area==null||!area.Floors.Contains(e.Cell)||!all.Any(a=>a.Id==e.ActorId)||!Campaign!.Items.Any(i=>i.Id==e.ItemId&&i.Equipment?.Use==(e.Kind=="blind"?"flash":e.Kind))||e.RadiusFeet is <0 or >1000)throw new InvalidDataException("Invalid saved equipment effect.");}
  var cells=new HashSet<(string,Cell)>();foreach(var m in EquipmentMines)
  {var area=Campaign?.Areas.FirstOrDefault(a=>a.Id==m.AreaId);if(m==null||area==null||!area.Floors.Contains(m.Cell)||!all.Any(a=>a.Id==m.OwnerId&&!a.Enemy)||!Campaign!.Items.Any(i=>i.Id==m.ItemId&&i.Equipment?.Use=="mine")||!m.Spent&&!cells.Add((m.AreaId,m.Cell)))throw new InvalidDataException("Invalid saved equipment device.");}
 }
}
