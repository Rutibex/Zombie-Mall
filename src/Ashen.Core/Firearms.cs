namespace Ashen.Core;

public sealed partial class World
{
 public string FirearmActionReason(Actor a)
 {
  if(!Actors.Contains(a)||!a.Alive||a.Enemy)return "Choose a conscious survivor.";
  if(a.Equipment.Weapon?.Definition.MagazineSize is not >0)return "Equip a firearm first.";
  if(ReactionInterruptActive||IsMoving(a)||Mode!=Mode.Explore&&!(Mode==Mode.Combat&&CanAct(a)))return "Stop moving; reloading or unloading requires the active Action in combat.";
  return "";
 }
 public bool ReloadFirearm(Actor a)
 {
  string reason=FirearmActionReason(a);if(reason!=""){LogSystem(reason,true);return false;}
  var gun=a.Equipment.Weapon!;int room=gun.Definition.MagazineSize-(gun.LoadedAmmunition?.Quantity??0);
  int index=a.Inventory.Items.FindIndex(s=>s.Definition.Kind==ItemKind.Ammunition&&s.Definition.AmmoType==gun.Definition.AmmunitionType&&(gun.LoadedAmmunition==null||s.ItemId==gun.LoadedAmmunition.ItemId));
  if(room<1||index<0){LogSystem(room<1?"The firearm is already full.":"No compatible loose ammunition in this survivor's inventory.",true);return false;}
  if(Mode==Mode.Combat&&!SpendAction(a,"Reload firearm"))return false;
  // One source stack per action; exact unit identities follow the rounds into the magazine.
  var rounds=a.Inventory.TakeAt(index,Math.Min(room,a.Inventory.Items[index].Quantity))!;
  if(gun.LoadedAmmunition is {} loaded){loaded.Quantity+=rounds.Quantity;loaded.ResourceUnits.AddRange(rounds.ResourceUnits);}else gun.LoadedAmmunition=rounds;
  AssignMissingItemIds();Message(MessageCategory.Combat,$"{a.Name} reloads {gun.Definition.Name}: {gun.LoadedAmmunition.Quantity}/{gun.Definition.MagazineSize}.");return true;
 }
 public bool UnloadFirearm(Actor a)
 {
  string reason=FirearmActionReason(a);if(reason!=""){LogSystem(reason,true);return false;}
  var gun=a.Equipment.Weapon!;if(gun.LoadedAmmunition is not {} rounds||!a.Inventory.CanAdd(rounds)){LogSystem("The firearm is empty or there is no inventory space.",true);return false;}
  if(Mode==Mode.Combat&&!SpendAction(a,"Unload firearm"))return false;
  a.Inventory.Add(rounds);gun.LoadedAmmunition=null;AssignMissingItemIds();Message(MessageCategory.Combat,$"{a.Name} unloads {gun.Definition.Name}.");return true;
 }
 void ValidateFirearmState()
 {
  if(Campaign==null)return;
  foreach(var stack in AllCampaignStacks())stack.ValidateOrThrow();
 }
}
