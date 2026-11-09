using Ashen.Core;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 // Disposable screenshot staging only; production new games retain authored positions and empty guns.
 void StageFirearmCapture()
 {
  title=false;SelectCharacter(0);var a=world.Chosen;world.Equip(a,a.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));world.ReloadFirearm(a);
  if(scene=="firearms-record")ui.Screens.Navigate(UiScreenId.Character);
  else if(scene=="firearms-context"){contextScreen=new(800,300);contextPoint=a.Position;contextOpen=true;BuildContextCommands();}
  else ui.Screens.Navigate(UiScreenId.Inventory);
 }
}
