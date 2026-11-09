using Ashen.Core;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 // Disposable render fixtures. No staging affects normal play or authored campaign positions.
 void StageTacticalCapture()
 {
  if(scene=="tactical-title"){title=true;return;}
  title=false;foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;
  var a=world.PartyLeader;var target=world.Actors.First(e=>e.Enemy);a.Pos=new(11,24);target.Pos=scene=="tactical-close"?new(12,24):new(11,21);
  world.AssignQuickWeapon(a,a.Equipment.Weapon!.InstanceId,1);world.Equip(a,a.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));world.ReloadFirearm(a);world.Reveal();
  world.StartCombat(target.Group);world.Order=[a.Id,target.Id];world.Turn=0;SelectCharacter(0);camera=new(11,23);action=TargetIntent.Attack;
  if(scene=="tactical-record"){ui.Screens.Navigate(UiScreenId.Character);return;}
  hoverActor=target;attackPreview=world.PreviewAttack(a,target,AttackKind.Basic);var point=Screen(target.Position);cursor=new((int)point.X,(int)point.Y);
  if(scene=="tactical-context"){contextScreen=new(800,270);contextPoint=a.Position;contextOpen=true;BuildContextCommands();}
 }
}
