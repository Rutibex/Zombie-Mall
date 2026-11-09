using System.Text.Json.Serialization;
namespace Ashen.Core;

/// <summary>IIP26 container/corpse/ground-loot authority. UI code never mutates inventories directly.</summary>
public sealed partial class World
{
 readonly Dictionary<string,string> pendingCorpseLoot=[];
 readonly Dictionary<string,Cell> pendingGroundLoot=[];
 [JsonIgnore] public Action<Feature>? ContainerLootRequested {get;set;}
 [JsonIgnore] public Action<Actor>? CorpseLootRequested {get;set;}
 [JsonIgnore] public Action<Cell>? GroundLootRequested {get;set;}

 public static bool IsLootContainer(Feature feature)=>feature.Kind is "chest" or "container" or "relic";
 bool LootReachable(Actor actor,WorldPoint source)=>Mode==Mode.Explore&&!actor.Enemy&&actor.Alive&&actor.Position.Distance(source)<=1.25f;
 bool ContainerAccessible(Actor actor,Feature feature,out string reason)
 {
  if(Campaign!=null&&(!Features.Contains(feature)||!Actors.Contains(actor))){reason="That container or actor belongs to another area.";return false;}
  if(Mode!=Mode.Explore){reason="Finish combat before looting.";return false;}
  if(actor.Enemy||!actor.Alive){reason="A living companion must handle the container.";return false;}
  if(!IsFeatureDiscovered(feature)||!IsLootContainer(feature)){reason="That is not an accessible loot container.";return false;}
  if(actor.Position.Distance(feature.Pos.Center)>1.25f){reason="Move beside the container first.";return false;}
  if(!LowerMallContainerReady(feature)){reason="Search or unlock this finite cache through the location interaction first.";return false;}
  if(feature.Locked){reason=$"{feature.Name} is locked.";return false;}
  if(feature.DetectedTrap){reason=$"{feature.Name} is trapped. Disarm it first.";return false;}
  reason="";return true;
 }
 bool GroundAccessible(Actor actor,Cell cell,out string reason)
 {
  if(Mode!=Mode.Explore){reason="Finish combat before looting.";return false;}
  if(actor.Enemy||!actor.Alive){reason="A living companion must handle the ground pile.";return false;}
  if(actor.Position.Distance(cell.Center)>1.25f){reason="Move beside the ground pile first.";return false;}
  if(GroundPileAt(cell)==null){reason="There is no ground pile there.";return false;}
  reason="";return true;
 }
 bool CorpseAccessible(Actor actor,Actor corpse,out string reason)
 {
  if(Mode!=Mode.Explore){reason="Finish combat before looting.";return false;}
  if(actor.Enemy||!actor.Alive){reason="A living companion must handle the corpse.";return false;}
  if(corpse.Alive){reason="That creature is still alive.";return false;}
  if(actor.Position.Distance(corpse.Position)>1.25f){reason="Move beside the corpse first.";return false;}
  reason="";return true;
 }

 public bool RequestCorpseLoot(Actor actor,Actor corpse)
 {
  if(Mode!=Mode.Explore||actor.Enemy||!actor.Alive||corpse.Alive){LogSystem("Corpse looting requires a living companion outside combat.",true);return false;}
  if(actor.Position.Distance(corpse.Position)<=1.25f){Stop(actor);CorpseLootRequested?.Invoke(corpse);return true;}
  foreach(var point in ApproachPoints(corpse.Position).OrderBy(p=>p.Distance(actor.Position)))if(CanStand(point,actor)&&Explored.Contains(point.Cell)&&MoveTo(actor,point)){pendingCorpseLoot[actor.Id]=corpse.Id;Message(MessageCategory.Travel,$"{actor.Name} approaches {corpse.Name}'s remains.");return true;}
  LogSystem("No clear approach to the corpse.",true);return false;
 }
 public bool RequestGroundLoot(Actor actor,Cell cell)
 {
  if(Mode!=Mode.Explore||actor.Enemy||!actor.Alive||GroundPileAt(cell)==null){LogSystem("There is no accessible ground pile there.",true);return false;}
  if(actor.Position.Distance(cell.Center)<=1.25f){Stop(actor);GroundLootRequested?.Invoke(cell);return true;}
  foreach(var point in ApproachPoints(cell.Center).OrderBy(p=>p.Distance(actor.Position)))if(CanStand(point,actor)&&Explored.Contains(point.Cell)&&MoveTo(actor,point)){pendingGroundLoot[actor.Id]=cell;Message(MessageCategory.Travel,$"{actor.Name} approaches the ground loot.");return true;}
  LogSystem("No clear approach to the ground loot.",true);return false;
 }
 IEnumerable<WorldPoint> ApproachPoints(WorldPoint center)
 {
  for(int i=0;i<16;i++){float angle=i*MathF.Tau/16;yield return center+new WorldPoint(MathF.Cos(angle),MathF.Sin(angle))*1.05f;}
  foreach(var c in center.Cell.Neighbors())yield return c.Center;
 }
 void CompleteLootInteractions()
 {
  if(Mode!=Mode.Explore)return;
  foreach(var (actorId,corpseId) in pendingCorpseLoot.ToArray())
  {
   var actor=Actors.FirstOrDefault(a=>a.Id==actorId);var corpse=Actors.FirstOrDefault(a=>a.Id==corpseId);
   if(actor==null||corpse==null||!actor.Alive||corpse.Alive){pendingCorpseLoot.Remove(actorId);continue;}
   if(actor.Position.Distance(corpse.Position)<=1.25f){Stop(actor);pendingCorpseLoot.Remove(actorId);CorpseLootRequested?.Invoke(corpse);}
   else if(!IsMoving(actor))pendingCorpseLoot.Remove(actorId);
  }
  foreach(var (actorId,cell) in pendingGroundLoot.ToArray())
  {
   var actor=Actors.FirstOrDefault(a=>a.Id==actorId);if(actor==null||!actor.Alive||GroundPileAt(cell)==null){pendingGroundLoot.Remove(actorId);continue;}
   if(actor.Position.Distance(cell.Center)<=1.25f){Stop(actor);pendingGroundLoot.Remove(actorId);GroundLootRequested?.Invoke(cell);}
   else if(!IsMoving(actor))pendingGroundLoot.Remove(actorId);
  }
 }
 public void ClearPendingLoot(string actorId){pendingCorpseLoot.Remove(actorId);pendingGroundLoot.Remove(actorId);}
 public void ClearAllPendingLoot(){pendingCorpseLoot.Clear();pendingGroundLoot.Clear();}

 public bool TakeContainerItem(Actor actor,Feature feature,int index,int quantity=int.MaxValue)
 {
  if(!ContainerAccessible(actor,feature,out var reason)){LogSystem(reason,true);return false;}
  if(index<0||index>=feature.Contents.Items.Count)return false;var source=feature.Contents.Items[index];int move=Math.Clamp(quantity,1,source.Quantity);var probe=source.Portion(move);if(move<source.Quantity)probe.InstanceId="";
  if(!CanCarry(actor,probe,out reason)){LogSystem($"{actor.Name} cannot carry {source.Definition.Name}: {reason}",true);return false;}
  var moved=feature.Contents.TakeAt(index,move);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();if(!actor.Inventory.Add(moved)){feature.Contents.Add(moved);AssignMissingItemIds();return false;}
  AssignMissingItemIds();actor.QuickAccess.Normalize(actor);Message(MessageCategory.Loot,$"{actor.Name} takes {moved.Definition.Name}{(moved.Quantity>1?$" x{moved.Quantity}":"")} from {feature.Name}.");Sound?.Invoke("loot");SyncCampaignObjectives();return true;
 }
 public bool StoreContainerItem(Actor actor,Feature feature,int inventoryIndex,int quantity=int.MaxValue)
 {
  if(!ContainerAccessible(actor,feature,out var reason)){LogSystem(reason,true);return false;}
  if(inventoryIndex<0||inventoryIndex>=actor.Inventory.Items.Count)return false;var source=actor.Inventory.Items[inventoryIndex];int move=Math.Clamp(quantity,1,source.Quantity);var probe=source.Portion(move);if(move<source.Quantity)probe.InstanceId="";if(IsSecuringSite(feature)&&!PreviewSecuring(actor,feature,probe).Allowed){LogSystem(PreviewSecuring(actor,feature,probe).Reason,true);return false;}if(!feature.Contents.CanAdd(probe)){LogSystem($"{feature.Name} has no room for that item.",true);return false;}
  var moved=actor.Inventory.TakeAt(inventoryIndex,move);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();feature.Contents.Add(moved);actor.QuickAccess.Normalize(actor);AssignMissingItemIds();Message(MessageCategory.Loot,$"{actor.Name} places {moved.Definition.Name}{(moved.Quantity>1?$" x{moved.Quantity}":"")} in {feature.Name}.");CreditSecuredResources(actor,feature,moved);return true;
 }
 public bool TakeContainerGold(Actor actor,Feature feature)
 {
  if(!ContainerAccessible(actor,feature,out var reason)){LogSystem(reason,true);return false;}if(feature.Gold<=0)return false;int amount=feature.Gold;feature.Gold=0;Gold+=amount;Message(MessageCategory.Loot,$"{actor.Name} takes {amount} gold from {feature.Name}.");Sound?.Invoke("loot");SyncCampaignObjectives();return true;
 }
 public int TakeAllContainer(Actor actor,Feature feature)
 {
  if(!ContainerAccessible(actor,feature,out var reason)){LogSystem(reason,true);return 0;}int moved=0,index=0;
  while(index<feature.Contents.Items.Count){if(TakeContainerItem(actor,feature,index,int.MaxValue))moved++;else index++;}
  if(feature.Gold>0&&TakeContainerGold(actor,feature))moved++;
  if(moved==0)LogSystem($"Nothing from {feature.Name} could be taken.");return moved;
 }

 public bool StoreGroundItem(Actor actor,Cell cell,int inventoryIndex,int quantity=int.MaxValue)
 {
  // An opened ground-loot panel remains a valid drop target after its last item is taken.
  // Therefore storing validates the spatial location, not the continued existence of a pile;
  // GroundInventoryAt recreates the pile if the player puts something back.
  if(Mode!=Mode.Explore){LogSystem("Finish combat before changing ground loot.",true);return false;}
  if(actor.Enemy||!actor.Alive){LogSystem("A living companion must handle the ground pile.",true);return false;}
  if(actor.Position.Distance(cell.Center)>1.25f){LogSystem("Move beside the ground pile first.",true);return false;}
  if(inventoryIndex<0||inventoryIndex>=actor.Inventory.Items.Count)return false;if(actor.Pos==cell)return DropInventoryItem(actor,inventoryIndex,quantity);
  var source=actor.Inventory.Items[inventoryIndex];int move=Math.Clamp(quantity,1,source.Quantity);var moved=actor.Inventory.TakeAt(inventoryIndex,move);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();var ground=GroundInventoryAt(cell,true);if(!ground.Add(moved)){actor.Inventory.Add(moved);AssignMissingItemIds();return false;}actor.QuickAccess.Normalize(actor);AssignMissingItemIds();Message(MessageCategory.Loot,$"{actor.Name} places {moved.Definition.Name} with the ground loot.");return true;
 }
 public int TakeAllGround(Actor actor,Cell cell)
 {
  if(!GroundAccessible(actor,cell,out var reason)){LogSystem(reason,true);return 0;}int moved=0,index=0;while(GroundPileAt(cell) is { } pile&&index<pile.Inventory.Items.Count){if(PickupGroundItemAt(actor,cell,index,int.MaxValue))moved++;else index++;}return moved;
 }
 public bool PickupGroundItemAt(Actor actor,Cell cell,int index,int quantity=int.MaxValue)
 {
  if(!GroundAccessible(actor,cell,out var reason)){LogSystem(reason,true);return false;}var pile=GroundPileAt(cell)!;if(index<0||index>=pile.Inventory.Items.Count)return false;var source=pile.Inventory.Items[index];int move=Math.Clamp(quantity,1,source.Quantity);var probe=source.Portion(move);if(move<source.Quantity)probe.InstanceId="";if(!CanCarry(actor,probe,out reason)){LogSystem($"{actor.Name} cannot pick up {source.Definition.Name}: {reason}",true);return false;}var moved=pile.Inventory.TakeAt(index,move);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();if(!actor.Inventory.Add(moved)){pile.Inventory.Add(moved);AssignMissingItemIds();return false;}actor.QuickAccess.Normalize(actor);AssignMissingItemIds();RemoveEmptyGroundPiles();Message(MessageCategory.Loot,$"{actor.Name} picks up {moved.Definition.Name}{(moved.Quantity>1?$" x{moved.Quantity}":"")}.");return true;
 }

 public IEnumerable<ItemStack> CorpseLoot(Actor corpse)=>corpse.Inventory.Items.Concat(corpse.Equipment.All());
 public bool TakeCorpseItem(Actor actor,Actor corpse,string instanceId,int quantity=int.MaxValue)
 {
  if(!CorpseAccessible(actor,corpse,out var reason)){LogSystem(reason,true);return false;}int index=corpse.Inventory.Items.FindIndex(x=>x.InstanceId==instanceId);
  if(index>=0)
  {
   var source=corpse.Inventory.Items[index];int move=Math.Clamp(quantity,1,source.Quantity);var probe=source.Portion(move);if(move<source.Quantity)probe.InstanceId="";if(!CanCarry(actor,probe,out reason)){LogSystem($"{actor.Name} cannot carry {source.Definition.Name}: {reason}",true);return false;}var moved=corpse.Inventory.TakeAt(index,move);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();actor.Inventory.Add(moved);AssignMissingItemIds();actor.QuickAccess.Normalize(actor);Message(MessageCategory.Loot,$"{actor.Name} takes {moved.Definition.Name} from {corpse.Name}.");return true;
  }
  var slot=corpse.Equipment.SlotOf(instanceId);if(slot is null||corpse.Equipment.Get(slot.Value) is not {} equipped)return false;if(!CanCarry(actor,equipped,out reason)){LogSystem($"{actor.Name} cannot carry {equipped.Definition.Name}: {reason}",true);return false;}corpse.Equipment.Set(slot.Value,null);actor.Inventory.Add(equipped);AssignMissingItemIds();Message(MessageCategory.Loot,$"{actor.Name} takes {equipped.Definition.Name} from {corpse.Name}.");return true;
 }
 public bool StoreCorpseItem(Actor actor,Actor corpse,int inventoryIndex,int quantity=int.MaxValue)
 {
  if(!CorpseAccessible(actor,corpse,out var reason)){LogSystem(reason,true);return false;}if(inventoryIndex<0||inventoryIndex>=actor.Inventory.Items.Count)return false;var source=actor.Inventory.Items[inventoryIndex];int move=Math.Clamp(quantity,1,source.Quantity);var probe=source.Portion(move);if(move<source.Quantity)probe.InstanceId="";if(!corpse.Inventory.CanAdd(probe)){LogSystem("The corpse cannot hold another item.",true);return false;}var moved=actor.Inventory.TakeAt(inventoryIndex,move);if(moved==null)return false;if(string.IsNullOrWhiteSpace(moved.InstanceId))moved.InstanceId=NextItemId();corpse.Inventory.Add(moved);actor.QuickAccess.Normalize(actor);AssignMissingItemIds();Message(MessageCategory.Loot,$"{actor.Name} leaves {moved.Definition.Name} with {corpse.Name}'s remains.");return true;
 }
 public int TakeAllCorpse(Actor actor,Actor corpse)
 {
  if(!CorpseAccessible(actor,corpse,out var reason)){LogSystem(reason,true);return 0;}int moved=0;
  foreach(var instance in CorpseLoot(corpse).Select(x=>x.InstanceId).Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray())if(TakeCorpseItem(actor,corpse,instance,int.MaxValue))moved++;
  return moved;
 }

 public bool TryUnlockContainer(Actor actor,Feature feature)
 {
  if(Mode!=Mode.Explore||!actor.Alive||!IsLootContainer(feature)||actor.Position.Distance(feature.Pos.Center)>1.25f)return false;if(!feature.Locked)return true;
  if(!actor.Character.Proficiencies.Tools.Contains("Thieves' tools",StringComparer.OrdinalIgnoreCase)){LogSystem($"{actor.Name} lacks thieves' tools proficiency.",true);return false;}
  int roll=Roll(20),bonus=actor.SkillModifier(Skill.SleightOfHand),dc=12;bool success=roll+bonus>=dc;LogAbilityCheck(actor,Skill.SleightOfHand,roll,bonus,dc,success);if(success){feature.Locked=false;Message(MessageCategory.System,$"{actor.Name} opens the lock on {feature.Name}.");Sound?.Invoke("click");}return success;
 }
 public bool TryDisarmContainer(Actor actor,Feature feature)
 {
  if(Mode!=Mode.Explore||!actor.Alive||!IsLootContainer(feature)||actor.Position.Distance(feature.Pos.Center)>1.25f)return false;if(!feature.DetectedTrap)return true;if(!feature.Disarmable){LogSystem($"The trap on {feature.Name} cannot be disarmed with the current tools.",true);return false;}
  if(!actor.Character.Proficiencies.Tools.Contains("Thieves' tools",StringComparer.OrdinalIgnoreCase)){LogSystem($"{actor.Name} lacks thieves' tools proficiency.",true);return false;}
  int roll=Roll(20),bonus=actor.SkillModifier(Skill.SleightOfHand),dc=12;bool success=roll+bonus>=dc;LogAbilityCheck(actor,Skill.SleightOfHand,roll,bonus,dc,success);if(success){feature.DetectedTrap=false;Message(MessageCategory.System,$"{actor.Name} disarms the trap on {feature.Name}.");Sound?.Invoke("click");}return success;
 }
}
