using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 bool DrawScavengingIcon(string id,Rectangle r)
 {
  if(world.Campaign?.SchemaVersion<5||world.Campaign==null)return false;
  if(id.StartsWith("item.mall.ammo-")){for(int i=0;i<3;i++){Box(new(r.X+5+i*r.Width/4,r.Y+r.Height/3,Math.Max(3,r.Width/6),r.Height/2),new Color(180,143,62),new Color(224,201,130));}return true;}
  if(id is "item.mall.pistol" or "item.mall.revolver" or "item.mall.shotgun"){Fill(new(r.X+3,r.Y+r.Height/3,r.Width-6,r.Height/5),new Color(113,126,132));Fill(new(r.X+r.Width/3,r.Y+r.Height/2,r.Width/5,r.Height/3),new Color(85,62,44));return true;}
  if(id=="item.commodity.004")
  {
   int w=r.Width/2;var bottle=new Rectangle(r.Center.X-w/2,r.Y+7,w,r.Height-10);Box(bottle,new Color(91,164,189),new Color(159,206,207));Fill(new(bottle.X+2,bottle.Y+bottle.Height/3,bottle.Width-4,bottle.Height/3),new Color(218,220,190));Fill(new(bottle.X+bottle.Width/4,r.Y+2,bottle.Width/2,6),new Color(214,218,212));return true;
  }
  if(id=="item.commodity.001")
  {
   var can=new Rectangle(r.X+5,r.Y+5,r.Width-10,r.Height-8);Box(can,new Color(83,120,68),new Color(170,179,155));Fill(new(can.X+2,can.Y+3,can.Width-4,3),new Color(205,207,184));Fill(new(can.X+2,can.Bottom-5,can.Width-4,3),new Color(205,207,184));Text("B",can.Center.X-6,can.Center.Y-7,new Color(227,218,169),.43f);return true;
  }
  if(id=="item.melee.002")
  {Fill(new(r.Center.X-3,r.Y+3,7,r.Height*2/3),new Color(196,207,208));Fill(new(r.Center.X-4,r.Y+r.Height*2/3,9,r.Height/3),new Color(108,75,52));return true;}
  return false;
 }
 void StageScavengingCapture()
 {
  if(scene=="scavenge-title"){title=true;return;}title=false;
  foreach(var e in world.Actors.Where(a=>a.Enemy))e.Hp=0;
  foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;
  var hero=world.Chosen;var cache=world.Features.First(f=>f.Id=="feature.mall.cafe-box");hero.Pos=new(21,3);world.Reveal();
  if(scene=="scavenge-find"){camera=new(21,4);ui.Loot.Open(cache);return;}
  world.TakeAllContainer(hero,cache);
  var shelter=world.Features.First(f=>f.Id=="feature.mall.shelter");hero.Pos=new(11,27);world.Reveal();world.Interact(shelter);
  var stockpile=world.Features.First(f=>f.Id=="feature.mall.supplies");hero.Pos=new(14,27);world.Party.Skip(1).First().Pos=new(12,28);world.Reveal();camera=new(13,27);
  if(scene!="scavenge-return")world.SecureCarriedResources(hero,stockpile);
  if(scene=="scavenge-record")ui.Screens.Navigate(UiScreenId.Character);
  else if(scene=="scavenge-journal")ui.Screens.Navigate(UiScreenId.Journal);
  else ui.Loot.Open(stockpile);
 }
}
