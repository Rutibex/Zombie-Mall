using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum InventorySourceKind { Backpack, Equipment, Ground }
 readonly record struct InventoryDragSource(InventorySourceKind Kind,string ActorId,string InstanceId,int Index,EquipmentSlot Slot);
 sealed class InventoryGeometry
 {
  public Rectangle Content,Doll,BackpackFrame,GroundFrame;
  public Rectangle[] Backpack=[],Ground=[],QuickWeapons=[],QuickItems=[],Quiver=[];
  public Dictionary<EquipmentSlot,Rectangle> Equipment=[];
 }
 InventoryDragSource? inventoryPressSource,inventoryDragSource;
 Point inventoryPressPoint;
 bool inventoryDragging;

 InventoryGeometry BuildInventoryGeometry(Rectangle r)
 {
  int x=r.X+22,y=r.Y+78,w=r.Width-44;
  var g=new InventoryGeometry{Content=new(x,y,w,r.Height-96)};
  const int leftW=230,groundW=118,gap=14;
  int midX=x+leftW+gap,groundX=r.Right-22-groundW,midW=groundX-gap-midX;
  g.Doll=new(midX+178,y+76,214,250);
  // IIP24: every visible body slot is now authoritative. Weapon is the serialized compatibility name for Main Hand.
  g.Equipment[EquipmentSlot.Head]=new(g.Doll.X+77,y+1,60,60);
  g.Equipment[EquipmentSlot.Neck]=new(g.Doll.X+7,y+1,56,56);
  g.Equipment[EquipmentSlot.Charm]=new(g.Doll.Right-63,y+1,56,56);
  g.Equipment[EquipmentSlot.Cloak]=new(g.Doll.X-69,g.Doll.Y+18,56,56);
  g.Equipment[EquipmentSlot.OffHand]=new(g.Doll.Right+13,g.Doll.Y+18,56,56);
  g.Equipment[EquipmentSlot.Armor]=new(g.Doll.X-69,g.Doll.Y+86,60,60);
  g.Equipment[EquipmentSlot.MainHand]=new(g.Doll.Right+9,g.Doll.Y+86,60,60);
  g.Equipment[EquipmentSlot.RingLeft]=new(g.Doll.X-69,g.Doll.Bottom-78,56,56);
  g.Equipment[EquipmentSlot.RingRight]=new(g.Doll.Right+13,g.Doll.Bottom-78,56,56);
  g.Equipment[EquipmentSlot.Gloves]=new(g.Doll.X+7,g.Doll.Bottom-14,56,56);
  g.Equipment[EquipmentSlot.Boots]=new(g.Doll.Right-63,g.Doll.Bottom-14,56,56);
  int lx=x+13;
  g.Quiver=Enumerable.Range(0,3).Select(i=>new Rectangle(lx+i*67,y+52,59,59)).ToArray();
  g.Equipment[EquipmentSlot.Ammo1]=g.Quiver[0];g.Equipment[EquipmentSlot.Ammo2]=g.Quiver[1];g.Equipment[EquipmentSlot.Ammo3]=g.Quiver[2];
  g.QuickWeapons=Enumerable.Range(0,4).Select(i=>new Rectangle(lx+i*52,y+157,46,54)).ToArray();
  g.QuickItems=Enumerable.Range(0,3).Select(i=>new Rectangle(lx+i*67,y+261,59,59)).ToArray();
  int backpackY=y+382,slot=58,slotGap=5;
  g.BackpackFrame=new(midX-3,backpackY-35,midW+6,164);
  g.Backpack=Enumerable.Range(0,18).Select(i=>new Rectangle(midX+(i%9)*(slot+slotGap),backpackY+(i/9)*(slot+slotGap),slot,slot)).ToArray();
  int groundSlot=50,groundGap=5;g.GroundFrame=new(groundX,y+38,groundW,320);
  g.Ground=Enumerable.Range(0,10).Select(i=>new Rectangle(groundX+7+(i%2)*(groundSlot+groundGap),y+75+(i/2)*(groundSlot+groundGap),groundSlot,groundSlot)).ToArray();
  return g;
 }

 ItemStack? InventorySourceStack(InventoryDragSource source)
 {
  var actor=world.Actors.FirstOrDefault(a=>a.Id==source.ActorId);if(actor==null)return null;
  return source.Kind switch
  {
   InventorySourceKind.Backpack=>actor.Inventory.Items.FirstOrDefault(x=>x.InstanceId==source.InstanceId),
   InventorySourceKind.Equipment=>actor.Equipment.Get(source.Slot) is {} eq&&eq.InstanceId==source.InstanceId?eq:null,
   InventorySourceKind.Ground=>world.GroundPileAt(actor.Pos)?.Inventory.Items.FirstOrDefault(x=>x.InstanceId==source.InstanceId),
   _=>null
  };
 }
 InventoryDragSource? InventorySourceAt(Point p)
 {
  if(ui.Screens.Active!=UiScreenId.Inventory)return null;var a=world.Chosen;var g=BuildInventoryGeometry(ui.Layout.ManagementRect);
  for(int i=0;i<g.Backpack.Length&&i<a.Inventory.Items.Count;i++)if(g.Backpack[i].Contains(p)){var stack=a.Inventory.Items[i];return new(InventorySourceKind.Backpack,a.Id,stack.InstanceId,i,default);}
  foreach(var pair in g.Equipment)if(pair.Value.Contains(p)&&a.Equipment.Get(pair.Key) is {} eq)return new(InventorySourceKind.Equipment,a.Id,eq.InstanceId,-1,pair.Key);
  var ground=world.GroundPileAt(a.Pos)?.Inventory;
  if(ground!=null)for(int i=0;i<g.Ground.Length&&i<ground.Items.Count;i++)if(g.Ground[i].Contains(p)){var stack=ground.Items[i];return new(InventorySourceKind.Ground,a.Id,stack.InstanceId,i,default);}
  return null;
 }
 bool InventorySplitModifier=>keys.IsKeyDown(Keys.LeftShift)||keys.IsKeyDown(Keys.RightShift)||keys.IsKeyDown(Keys.LeftControl)||keys.IsKeyDown(Keys.RightControl);
 int SourceIndex(Actor a,string instanceId)=>a.Inventory.Items.FindIndex(x=>x.InstanceId==instanceId);
 int GroundIndex(Actor a,string instanceId)=>world.GroundPileAt(a.Pos)?.Inventory.Items.FindIndex(x=>x.InstanceId==instanceId)??-1;

 bool HandleInventoryPointer(bool down,bool held,bool up)
 {
  if(ui.Screens.Active!=UiScreenId.Inventory||ui.Modals.IsOpen){CancelInventoryDrag();return false;}
  if(down)
  {
   var source=InventorySourceAt(cursor);if(source==null)return false;
   inventoryPressSource=source;inventoryDragSource=null;inventoryPressPoint=cursor;inventoryDragging=false;return true;
  }
  if(inventoryPressSource!=null&&held)
  {
   if(!inventoryDragging&&V.Distance(new(inventoryPressPoint.X,inventoryPressPoint.Y),new(cursor.X,cursor.Y))>6){inventoryDragging=true;inventoryDragSource=inventoryPressSource;}
   return true;
  }
  if(up&&inventoryPressSource is {} pressed)
  {
   if(inventoryDragging&&inventoryDragSource is {} drag)CompleteInventoryDrop(drag,cursor);
   else ActivateInventorySource(pressed);
   CancelInventoryDrag();return true;
  }
  return inventoryPressSource!=null;
 }
 void CancelInventoryDrag(){inventoryPressSource=null;inventoryDragSource=null;inventoryDragging=false;}
 void ActivateInventorySource(InventoryDragSource source)
 {
  var a=world.Actors.FirstOrDefault(x=>x.Id==source.ActorId);if(a==null)return;
  if(source.Kind==InventorySourceKind.Backpack)
  {
   int index=SourceIndex(a,source.InstanceId);if(index<0)return;var stack=a.Inventory.Items[index];var item=stack.Definition;
   if(item.EquipSlot!=null){if(!world.Equip(a,index))Notify("That item cannot be equipped right now.");}
   else if(item.Id=="healing-potion")world.Potion(a);
   else Notify($"{item.Name}: drag it to a compatible destination or right-click for details.");
  }
  else if(source.Kind==InventorySourceKind.Equipment)
  {
   if(!world.Unequip(a,source.Slot))Notify(world.Mode==Mode.Combat?"Equipment changes are locked during combat.":"No backpack slot is available.");
  }
  else
  {
   int index=GroundIndex(a,source.InstanceId);if(index>=0&&!world.PickupGroundItem(a,index,InventorySplitModifier?1:int.MaxValue))Notify("That item cannot be picked up right now.");
  }
 }
 void CompleteInventoryDrop(InventoryDragSource source,Point drop)
 {
  var from=world.Actors.FirstOrDefault(x=>x.Id==source.ActorId);var stack=from==null?null:InventorySourceStack(source);if(from==null||stack==null)return;
  var g=BuildInventoryGeometry(ui.Layout.ManagementRect);int amount=InventorySplitModifier?1:stack.Quantity;

  // Permanent portrait rail is a direct transfer destination, matching classic Infinity inventory behavior.
  for(int i=0;i<world.PartyCount;i++)if(ui.Portraits.SlotRectangle(i).Contains(drop))
  {
   var to=world.PartyAt(i);if(to==null)return;
   bool ok=source.Kind switch
   {
    InventorySourceKind.Backpack=>to==from||world.TransferItem(from,to,SourceIndex(from,source.InstanceId),amount),
    InventorySourceKind.Equipment=>to==from||world.TransferEquippedItem(from,to,source.Slot),
    InventorySourceKind.Ground=>world.TransferGroundItem(from,GroundIndex(from,source.InstanceId),i,amount),
    _=>false
   };
   if(!ok&&to!=from)Notify("That transfer is not legal right now.");return;
  }

  for(int i=0;i<g.QuickWeapons.Length;i++)if(g.QuickWeapons[i].Contains(drop))
  {
   if(source.Kind==InventorySourceKind.Ground){Notify("Pick the weapon up before assigning a quick weapon slot.");return;}
   if(!world.AssignQuickWeapon(from,source.InstanceId,i))Notify("Quick weapon slots accept weapons carried or equipped by this character.");return;
  }
  for(int i=0;i<g.QuickItems.Length;i++)if(g.QuickItems[i].Contains(drop))
  {
   if(source.Kind==InventorySourceKind.Ground){Notify("Pick the item up before assigning a quick item slot.");return;}
   if(!world.AssignQuickItem(from,source.InstanceId,i))Notify("Quick item slots accept consumables and usable tools.");return;
  }

  foreach(var pair in g.Equipment)if(pair.Value.Contains(drop))
  {
   if(!stack.Definition.CanEquipIn(pair.Key)){Notify($"{stack.Definition.Name} does not fit the {EquipmentSlots.Display(pair.Key)} slot.");return;}
   if(source.Kind==InventorySourceKind.Equipment)
   {
    if(source.Slot!=pair.Key&&!world.MoveEquippedItem(from,source.Slot,pair.Key))Notify("That equipment cannot be moved to that slot right now.");return;
   }
   string itemId=stack.ItemId;
   if(source.Kind==InventorySourceKind.Ground)
   {
    int gi=GroundIndex(from,source.InstanceId);int pickup=stack.Definition.Kind==ItemKind.Ammunition?amount:1;if(gi<0||!world.PickupGroundItem(from,gi,pickup)){Notify("That item cannot be picked up.");return;}
   }
   int bi=SourceIndex(from,source.InstanceId);if(bi<0)bi=from.Inventory.Items.FindLastIndex(x=>x.ItemId==itemId&&x.Definition.CanEquipIn(pair.Key));
   if(bi<0||!world.Equip(from,bi,pair.Key))Notify("That item cannot be equipped right now.");return;
  }

  for(int i=0;i<g.Backpack.Length;i++)if(g.Backpack[i].Contains(drop))
  {
   if(source.Kind==InventorySourceKind.Backpack){int fromIndex=SourceIndex(from,source.InstanceId);if(fromIndex>=0)world.ReorderInventory(from,fromIndex,i);}
   else if(source.Kind==InventorySourceKind.Equipment){if(!world.Unequip(from,source.Slot))Notify("That item cannot be moved to the backpack.");}
   else{int gi=GroundIndex(from,source.InstanceId);if(gi>=0&&!world.PickupGroundItem(from,gi,amount))Notify("That item cannot be picked up.");}
   return;
  }

  if(g.GroundFrame.Contains(drop))
  {
   bool ok=source.Kind switch
   {
    InventorySourceKind.Backpack=>world.DropInventoryItem(from,SourceIndex(from,source.InstanceId),amount),
    InventorySourceKind.Equipment=>world.DropEquippedItem(from,source.Slot),
    InventorySourceKind.Ground=>true,
    _=>false
   };
   if(!ok)Notify("That item cannot be dropped here.");return;
  }
  Notify("Drop the item on a backpack slot, compatible equipment slot, quick slot, party portrait, or the Ground pane.");
 }

 void DrawInventoryPaperDoll(Actor a,Rectangle r)
 {
  batch.Draw(paper,r,Color.White);Border(r,Gold*.7f,2);var inner=new Rectangle(r.X+7,r.Y+7,r.Width-14,r.Height-14);Fill(inner,new Color(34,31,26)*.24f);
  if(SurvivorProfile(a) is {} modern){DrawModernFigure(new(inner.Center.X,inner.Bottom-10),Math.Min(inner.Height-20,inner.Width*1.8f),a.Portrait,a.Enemy,modern.Archetype=="runner",firearm:a.Equipment.Weapon?.Definition.MagazineSize>0);return;}
  int art=PortraitArtIndex(a);
  if(art<4)
  {
   var t=tex["party_bodies"];int sw=t.Width/2,sh=t.Height/2;var src=new Rectangle(art%2*sw,art/2*sh,sw,sh);
   float scale=Math.Min(inner.Width/(float)sw,inner.Height/(float)sh);int dw=(int)(sw*scale),dh=(int)(sh*scale);var dest=new Rectangle(inner.Center.X-dw/2,inner.Bottom-dh,dw,dh);batch.Draw(t,dest,src,Color.White);
  }
  else Portrait(a,new(inner.X+32,inner.Y+18,inner.Width-64,inner.Height-36));
 }
 void DrawInventorySlot(Rectangle q,ItemStack? stack,string emptyLabel,bool registered=true,string? context=null)
 {
  bool hover=q.Contains(cursor);Box(q,new Color(16,19,19),hover?Bright:Gold*.36f);Border(new(q.X+3,q.Y+3,q.Width-6,q.Height-6),Gold*.12f);
  if(stack==null){Center(emptyLabel,new(q.X,q.Y+q.Height/2-8,q.Width,18),Muted*.45f,.38f);return;}
  var item=stack.Definition;bool known=stack.Identified;if(!known||!DrawScavengingIcon(item.Id,new(q.X+8,q.Y+5,q.Width-16,q.Height-16)))Icon(item.EquipSlot?.ToString()??item.Kind.ToString(),new(q.X+8,q.Y+5,q.Width-16,q.Height-16),!known?Muted: item.Magical?Teal:item.Bonus>0?Teal:Gold);
  string badge=stack.Quantity>1?$"x{stack.Quantity}":known&&item.MaxCharges>0?$"{stack.Charges}":known&&item.Bonus!=0?$"{(item.Bonus>0?"+":"")}{item.Bonus}":"";if(badge!="")Text(badge,q.Right-Measure(badge,.43f)-4,q.Bottom-16,Bright,.43f);
  if(known&&item.Magical)Text("*",q.X+4,q.Y+1,Teal,.48f,true);
  if(!known)Text("?",q.X+4,q.Y+2,Red,.55f,true);
  if(registered)
  {
   string visibleName=stack.Identified?item.Name:$"Unidentified {item.Kind.ToString().ToLowerInvariant()}";
   string visibleDescription=stack.Identified?item.Description:"Properties unknown until identified.";
   ui.Information.Register(q,GearInformation(item,context??$"Owned by {world.Chosen.Name}.",stack,world.Chosen),$"{visibleName} — {visibleDescription} Drag to move/assign; right-click for item information.");
  }
 }
 void DrawQuickAssignment(Rectangle q,string instanceId,string emptyLabel,bool weapon,int slot,Actor a)
 {
  var stack=string.IsNullOrWhiteSpace(instanceId)?null:world.OwnedItem(a,instanceId);DrawInventorySlot(q,stack,emptyLabel,stack!=null,$"Assigned by {a.Name} to quick {(weapon?"weapon":"item")} {slot+1}.");
  if(stack==null){if(q.Contains(cursor))ui.Tooltips.Offer($"quick-empty:{weapon}:{slot}",$"Drag a compatible {(weapon?"weapon":"consumable/tool")} here to assign quick slot {slot+1}.");return;}
  buttons.Add((q,()=>{if(weapon)a.QuickAccess.WeaponInstanceIds[slot]="";else a.QuickAccess.ItemInstanceIds[slot]="";world.LogSystem($"{a.Name} clears quick {(weapon?"weapon":"item")} {slot+1}.");}));
  if(q.Contains(cursor))ui.Tooltips.Offer($"quick:{a.Id}:{weapon}:{slot}",$"{stack.Definition.Name} assigned to quick {(weapon?"weapon":"item")} {slot+1}. Click to clear, drag another compatible item to replace, or use the matching action-bar slot for one-click activation.");
 }

 void DrawInventoryScreen(Rectangle r)
 {
  PartySwitch(r);var a=world.Chosen;a.QuickAccess.Normalize(a);var g=BuildInventoryGeometry(r);int x=g.Content.X,y=g.Content.Y;
  Text(a.Name,x+252,y-2,Bright,.76f,true);Text($"LEVEL {a.TotalLevel} {a.Class.ToUpperInvariant()}",x+252,y+30,Gold,.51f);Text($"GOLD  {world.Gold}",r.Right-198,y+4,Gold,.62f,true);

  // Left: IIP24 quiver slots plus IIP33 persistent quick weapon/item assignment surfaces.
  Text(world.Campaign==null?"QUIVER":"AMMO",x+13,y+25,Gold,.55f,true);var ammoSlots=EquipmentSlots.Ammunition;for(int i=0;i<g.Quiver.Length;i++){var stack=a.Equipment.Get(ammoSlots[i]);DrawInventorySlot(g.Quiver[i],stack,"AMMO",true,$"Equipped by {a.Name} in {EquipmentSlots.Display(ammoSlots[i])}.");if(stack==null&&g.Quiver[i].Contains(cursor))ui.Tooltips.Offer("quiver:"+i,"Drag compatible ammunition here. Bows consume one equipped arrow per attack.");}
  Text("QUICK WEAPONS",x+13,y+128,Gold,.50f,true);for(int i=0;i<4;i++)DrawQuickAssignment(g.QuickWeapons[i],a.QuickAccess.WeaponInstanceIds[i],(i+1).ToString(),true,i,a);
  Text("QUICK ITEMS",x+13,y+233,Gold,.50f,true);for(int i=0;i<3;i++)DrawQuickAssignment(g.QuickItems[i],a.QuickAccess.ItemInstanceIds[i],(i+1).ToString(),false,i,a);
  Text($"WEIGHT  {a.CarriedWeight:0.#} / {a.CarryingCapacity:0.#} lb   {a.EncumbranceState}",x+13,y+340,a.CarriedWeight>a.CarryingCapacity?Red:a.EncumbranceState=="HEAVY"?Gold:Muted,.49f);
  Bar(new(x+13,y+364,198,9),a.CarriedWeight/Math.Max(1,a.CarryingCapacity),a.CarriedWeight>a.CarryingCapacity?Red:Teal);
  Paragraph(world.Campaign?.SchemaVersion>=7?$"Loaded: {a.EquippedAmmunitionCount}/{a.Equipment.Weapon?.Definition.MagazineSize??0}. Keep matching rounds in the backpack. Right-click ground to reload or unload. Combat quick-switch: Bonus Action.":"Magic items use ordinary physical equipment slots. Every legally equipped magic item is active; there is no magic-item cap.",x+13,y+383,205,Muted,.43f,16);

  DrawInventoryPaperDoll(a,g.Doll);
  foreach(var slot in EquipmentSlots.Body)if(g.Equipment.TryGetValue(slot,out var rect))
  {
   var stack=a.Equipment.Get(slot);string label=EquipmentSlots.Display(slot).ToUpperInvariant();DrawInventorySlot(rect,stack,label,true,$"Equipped by {a.Name} in the {EquipmentSlots.Display(slot)} slot.");
   Text(label,rect.X,rect.Bottom+3,Muted,.30f);
   if(stack==null&&rect.Contains(cursor))ui.Tooltips.Offer("equip-empty:"+slot,$"{EquipmentSlots.Display(slot)}. Drag a compatible item here. Magic items work while equipped with no global magic-item limit.");
  }
  Text($"MAGIC ITEMS  {a.EquippedMagicItems} ACTIVE",g.Doll.X+12,g.Doll.Y-26,Teal,.40f);
  Box(new(g.Doll.X+5,g.Doll.Bottom-34,g.Doll.Width-10,28),new Color(19,22,21)*.94f,Gold*.38f);
  Text($"AC {a.Ac}",g.Doll.X+13,g.Doll.Bottom-30,Bright,.46f);Text($"HP {a.Hp}/{a.MaxHp}",g.Doll.Right-106,g.Doll.Bottom-30,Bright,.46f);

  // Backpack: authoritative IIP22 personal inventory, now direct-manipulation IIP23 UI.
  Box(g.BackpackFrame,new Color(18,20,19)*.75f,Gold*.35f);Text("BACKPACK",g.BackpackFrame.X+9,g.BackpackFrame.Y+8,Gold,.48f,true);Text($"{a.Inventory.UsedSlots}/{a.Inventory.MaxSlots} slots / {a.Inventory.TotalWeight:0.#} lb",g.BackpackFrame.Right-190,g.BackpackFrame.Y+10,Muted,.39f);
  for(int i=0;i<g.Backpack.Length;i++){var stack=i<a.Inventory.Items.Count?a.Inventory.Items[i]:null;DrawInventorySlot(g.Backpack[i],stack,(i+1).ToString(),true,$"Carried by {a.Name} in backpack slot {i+1}.");}

  // Ground is spatial: only the pile in the inspected character's current cell appears.
  Box(g.GroundFrame,new Color(18,20,19)*.75f,Gold*.35f);Center("GROUND",new(g.GroundFrame.X,g.GroundFrame.Y+8,g.GroundFrame.Width,23),Gold,.47f,true);
  var ground=world.GroundPileAt(a.Pos)?.Inventory;for(int i=0;i<g.Ground.Length;i++){var stack=ground!=null&&i<ground.Items.Count?ground.Items[i]:null;DrawInventorySlot(g.Ground[i],stack,"",stack!=null,stack==null?null:$"On the ground at cell {a.Pos.X},{a.Pos.Y} beside {a.Name}.");if(stack==null&&g.Ground[i].Contains(cursor))ui.Tooltips.Offer($"ground-empty:{i}","Empty ground slot. Drag a carried or equipped item here to drop it at this map position.");}
  Center($"CELL {a.Pos.X},{a.Pos.Y}",new(g.GroundFrame.X,g.GroundFrame.Bottom-24,g.GroundFrame.Width,18),Muted,.37f);
  if(g.GroundFrame.Contains(cursor)&&!g.Ground.Any(slot=>slot.Contains(cursor)))ui.Tooltips.Offer("ground-pane",$"Items lying at {a.Name}'s current position. Drag between Ground, Backpack, equipment, or a party portrait.");

  if(world.Mode==Mode.Combat){var lockbar=new Rectangle(x+241,r.Bottom-43,r.Width-285,27);Box(lockbar,new Color(39,25,23),Red*.65f);Center("COMBAT: INVENTORY TRANSFERS AND EQUIPMENT CHANGES ARE LOCKED",lockbar,Bright,.43f,true);}
  else Text("Drag-and-drop: backpack <-> equipment <-> quiver <-> portraits <-> ground. Shift or Ctrl-drag a stack to move one. Firearms: equip, then right-click ground to reload from backpack.",x+241,r.Bottom-38,Muted,.43f);

  if(inventoryDragging&&inventoryDragSource is {} source&&InventorySourceStack(source) is {} dragged)
  {
   var ghost=new Rectangle(cursor.X-27,cursor.Y-27,54,54);Fill(new(ghost.X-3,ghost.Y-3,ghost.Width+6,ghost.Height+6),new Color(0,0,0)*.55f);DrawInventorySlot(ghost,dragged,"",false);
   float gleam=preferences.ReducedMotion?1:.65f+.35f*MathF.Sin(elapsed*9);
   Border(ghost,Bright*gleam,2);Fill(new(ghost.X+4,ghost.Y+4,Math.Max(1,(int)((ghost.Width-8)*gleam)),2),Gold);
   string qty=InventorySplitModifier&&dragged.Quantity>1?"1":dragged.Quantity.ToString();Text("x"+qty,ghost.Right-19,ghost.Bottom-15,Bright,.43f);
  }
 }
}
