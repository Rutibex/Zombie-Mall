using Ashen.Core;
using Microsoft.Xna.Framework;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 sealed record ItemInspection(string ActorId,string InstanceId);
 enum ItemInformationLocation { Backpack, Equipment, Ground, Container, Corpse, Unknown }
 readonly record struct ResolvedItemInspection(Actor Actor,ItemStack Stack,ItemInformationLocation Location,EquipmentSlot? Slot,int Index,Feature? Container=null,Actor? Corpse=null,Cell? GroundCell=null);

 bool TryResolveItemInspection(ItemInspection inspection,out ResolvedItemInspection resolved)
 {
  resolved=default;var actor=world.Actors.FirstOrDefault(a=>a.Id==inspection.ActorId);if(actor==null)return false;
  int backpack=actor.Inventory.Items.FindIndex(x=>x.InstanceId==inspection.InstanceId);
  if(backpack>=0){resolved=new(actor,actor.Inventory.Items[backpack],ItemInformationLocation.Backpack,null,backpack);return true;}
  var slot=actor.Equipment.SlotOf(inspection.InstanceId);if(slot is {} equipped&&actor.Equipment.Get(equipped) is {} eq){resolved=new(actor,eq,ItemInformationLocation.Equipment,equipped,-1);return true;}
  var ground=world.GroundPileAt(actor.Pos)?.Inventory;int groundIndex=ground?.Items.FindIndex(x=>x.InstanceId==inspection.InstanceId)??-1;
  if(ground!=null&&groundIndex>=0){resolved=new(actor,ground.Items[groundIndex],ItemInformationLocation.Ground,null,groundIndex,null,null,actor.Pos);return true;}
  foreach(var pile in world.GroundPiles){int i=pile.Inventory.Items.FindIndex(x=>x.InstanceId==inspection.InstanceId);if(i>=0){resolved=new(actor,pile.Inventory.Items[i],ItemInformationLocation.Ground,null,i,null,null,pile.Pos);return true;}}
  foreach(var feature in world.Features){int i=feature.Contents.Items.FindIndex(x=>x.InstanceId==inspection.InstanceId);if(i>=0){resolved=new(actor,feature.Contents.Items[i],ItemInformationLocation.Container,null,i,feature);return true;}}
  foreach(var dead in world.Actors.Where(a=>!a.Alive))
  {
   int i=dead.Inventory.Items.FindIndex(x=>x.InstanceId==inspection.InstanceId);if(i>=0){resolved=new(actor,dead.Inventory.Items[i],ItemInformationLocation.Corpse,null,i,null,dead);return true;}
   var corpseSlot=dead.Equipment.SlotOf(inspection.InstanceId);if(corpseSlot is {} cs&&dead.Equipment.Get(cs) is {} corpseGear){resolved=new(actor,corpseGear,ItemInformationLocation.Corpse,cs,-1,null,dead);return true;}
  }
  return false;
 }

 string ItemLocationText(ResolvedItemInspection item)=>item.Location switch
 {
  ItemInformationLocation.Backpack=>$"Carried by {item.Actor.Name}",
  ItemInformationLocation.Equipment=>$"Equipped by {item.Actor.Name}: {EquipmentSlots.Display(item.Slot!.Value)}",
  ItemInformationLocation.Ground=>$"On the ground at cell {(item.GroundCell??item.Actor.Pos).X},{(item.GroundCell??item.Actor.Pos).Y}",
  ItemInformationLocation.Container=>$"Inside {item.Container?.Name??"a container"}",
  ItemInformationLocation.Corpse=>$"Among {item.Corpse?.Name??"a corpse"}'s remains",
  _=>"Location unknown"
 };

 void DrawItemIllustration(Gear item,Rectangle r,bool identified)
 {
  batch.Draw(paper,r,Color.White);Border(r,Gold*.65f,2);var inner=new Rectangle(r.X+8,r.Y+8,r.Width-16,r.Height-16);Fill(inner,new Color(50,42,31)*.14f);
  if(identified&&DrawScavengingIcon(item.Id,inner))return;
  Color ink=identified?(item.Magical?Teal:new Color(91,67,39)):Muted;
  var c=new V(inner.Center.X,inner.Center.Y);float z=Math.Min(inner.Width,inner.Height)/180f;
  void L(V a,V b,int t=4)=>Line(c+a*z,c+b*z,ink,Math.Max(1,(int)(t*z)));
  void Ring(float radius,int segments=40,int t=3)
  {
   for(int i=0;i<segments;i++){float a=i*MathF.Tau/segments,b=(i+1)*MathF.Tau/segments;Line(c+new V(MathF.Cos(a),MathF.Sin(a))*radius*z,c+new V(MathF.Cos(b),MathF.Sin(b))*radius*z,ink,Math.Max(1,(int)(t*z)));}
  }
  if(!identified){Ring(48,40,3);Text("?",c.X-Measure("?",2.2f)/2,c.Y-42,ink,2.2f,true);return;}
  switch(item.Kind)
  {
   case ItemKind.Consumable:
    L(new(-25,-58),new(25,-58));L(new(-18,-58),new(-18,-40));L(new(18,-58),new(18,-40));L(new(-18,-40),new(-37,45));L(new(18,-40),new(37,45));L(new(-37,45),new(37,45));L(new(-31,20),new(31,20),2);Ring(9,24,2);break;
   case ItemKind.Weapon:
    if(item.Id=="bow")
    {L(new(-34,-62),new(-52,0),3);L(new(-52,0),new(-34,62),3);L(new(-34,-62),new(30,0),2);L(new(-34,62),new(30,0),2);L(new(30,0),new(57,0),3);L(new(47,-8),new(57,0),2);L(new(47,8),new(57,0),2);}
    else if(item.Id=="staff")L(new(-48,65),new(44,-65),8);
    else if(item.Id=="mace"){L(new(-40,60),new(28,-34),8);Ring(23,24,6);}
    else{L(new(-52,58),new(45,-48),10);L(new(-30,20),new(-7,43),6);L(new(35,-58),new(49,-44),4);}break;
   case ItemKind.Armor:
    L(new(-43,-50),new(43,-50),5);L(new(-43,-50),new(-58,24),5);L(new(43,-50),new(58,24),5);L(new(-58,24),new(-24,61),5);L(new(58,24),new(24,61),5);L(new(-24,61),new(24,61),5);L(new(-18,-50),new(0,-22),2);L(new(18,-50),new(0,-22),2);break;
   case ItemKind.Shield:
    L(new(-45,-50),new(45,-50),5);L(new(-45,-50),new(-37,28),5);L(new(45,-50),new(37,28),5);L(new(-37,28),new(0,65),5);L(new(37,28),new(0,65),5);Ring(18,24,3);break;
   case ItemKind.Ammunition:
    for(int i=-1;i<=1;i++){float x=i*18;L(new(x-25,55),new(x+24,-50),4);L(new(x+16,-45),new(x+24,-50),2);L(new(x+22,-39),new(x+24,-50),2);}break;
   case ItemKind.Accessory:
    if(item.EquipSlots().Any(s=>s is EquipmentSlot.RingLeft or EquipmentSlot.RingRight)){Ring(42,44,7);Ring(16,30,4);}
    else{Ring(40,44,5);L(new(0,-65),new(0,-40),4);Ring(12,24,3);}break;
   case ItemKind.Charm:
    Ring(43,40,4);for(int i=0;i<8;i++){float a=i*MathF.Tau/8;Line(c+new V(MathF.Cos(a),MathF.Sin(a))*18*z,c+new V(MathF.Cos(a),MathF.Sin(a))*50*z,ink,Math.Max(1,(int)(3*z)));}break;
   case ItemKind.Tool:
    L(new(-42,58),new(30,-50),8);Ring(18,28,4);L(new(30,-50),new(52,-65),4);break;
   case ItemKind.Quest:
    Ring(49,44,5);Ring(24,36,3);for(int i=0;i<6;i++){float a=i*MathF.Tau/6;Line(c,c+new V(MathF.Cos(a),MathF.Sin(a))*42*z,ink,Math.Max(1,(int)(3*z)));}break;
   default: Ring(45,40,4);break;
  }
  if(item.Magical){for(int i=0;i<6;i++){float a=i*MathF.Tau/6;var p=c+new V(MathF.Cos(a),MathF.Sin(a))*72*z;batch.Draw(circle,new Rectangle((int)p.X-3,(int)p.Y-3,6,6),Teal*.7f);}}
 }

 List<string> ItemRuleLines(ResolvedItemInspection resolved)
 {
  var item=resolved.Stack.Definition;var stack=resolved.Stack;var lines=new List<string>
  {
   $"Category: {item.Kind}",ItemLocationText(resolved),$"Weight: {item.Weight:0.##} lb each"+(stack.Quantity>1?$" / {stack.Weight:0.##} lb stack":""),$"Quantity: {stack.Quantity}"
  };
  if(!stack.Identified){lines.Add("Identification: Unidentified");return lines;}
  if(item.Value>0)lines.Add($"Base value: {item.Value} gp"+(stack.Quantity>1?$" each / {item.Value*stack.Quantity} gp stack":""));
  lines.Add("Identification: Identified");
  if(item.MagazineSize>0){lines.Add($"Loaded: {stack.LoadedAmmunition?.Quantity??0}/{item.MagazineSize}");lines.Add($"Attack: {item.WeaponDamageDice}d{item.WeaponDamageDieSize} piercing / {item.WeaponRangeFeet} ft");}
  if(item.MaxCharges>0)lines.Add($"Charges: {stack.Charges}/{item.MaxCharges}");
  if(item.EquipSlot!=null)lines.Add($"Equipment slot: {item.SlotSummary()}");
  if(item.TwoHanded)lines.Add("Hands: Two-handed");
  if(item.RequiresAmmunition)lines.Add($"Requires ammunition: {item.AmmunitionType}");
  if(item.IsAmmunition)lines.Add($"Ammunition type: {item.AmmoType}");
  if(item.EffectiveAttackModifier!=0)lines.Add($"Attack: {(item.EffectiveAttackModifier>=0?"+":"")}{item.EffectiveAttackModifier}");
  if(item.EffectiveDamageModifier!=0)lines.Add($"Damage: {(item.EffectiveDamageModifier>=0?"+":"")}{item.EffectiveDamageModifier}");
  if(item.EffectiveArmorModifier!=0)lines.Add($"Armor Class: {(item.EffectiveArmorModifier>=0?"+":"")}{item.EffectiveArmorModifier}");
  lines.Add($"Requirements: {item.EffectiveRequirements.Summary()}");
  if(!item.EffectiveRequirements.Allows(resolved.Actor,out var reason))lines.Add($"{resolved.Actor.Name}: CANNOT USE — {reason}");
  if(item.Magical)lines.Add("Magic: active while legally equipped; no attunement and no global magic-item cap.");
  return lines;
 }

 void DrawItemInformation(InformationModel model)
 {
  if(model.Item==null||!TryResolveItemInspection(model.Item,out var resolved)){ui.Information.Close();return;}
  var stack=resolved.Stack;var item=stack.Definition;bool identified=stack.Identified;
  string title=identified?item.Name:$"Unidentified {item.Kind.ToString().ToLowerInvariant()}";
  string lore=identified?(string.IsNullOrWhiteSpace(item.Lore)?item.Description:item.Lore):"The item's exact nature is hidden. Its weight, apparent form and location are known, but magical properties, command words and precise rules remain concealed until it is identified.";
  string mechanics=identified?(world.WorkshopEnabled&&WorkshopRules.Durable(item)?$"Condition {100-stack.Wear}/100{(stack.Broken?" (BROKEN)":"")}. Repair at the refuge workbench. "+item.Description:item.Description):"Identify the item to reveal its complete mechanical description.";

  Fill(ui.Layout.CenterBounds,Color.Black*.68f);buttons.Add((ui.Layout.CenterBounds,()=>ui.Information.Close()));
  int width=Math.Min(860,ui.Layout.CenterBounds.Width-64),height=Math.Min(640,ui.Layout.CenterBounds.Height-48);
  var r=new Rectangle(ui.Layout.CenterBounds.Center.X-width/2,ui.Layout.CenterBounds.Center.Y-height/2,width,height);Panel(r);
  var titleBar=new Rectangle(r.X+20,r.Y+18,r.Width-40,55);Box(titleBar,new Color(13,17,18)*.96f,Gold*.55f);Center("ITEM INFORMATION",new(titleBar.X,titleBar.Y+10,titleBar.Width,30),Bright,.72f,true);
  var artBand=new Rectangle(r.X+132,r.Y+82,r.Width-264,72);Box(artBand,new Color(11,14,16)*.94f,Gold*.38f);DrawItemIllustration(item,new(artBand.Center.X-34,artBand.Y+5,68,62),identified);

  var body=new Rectangle(r.X+25,r.Y+166,r.Width-50,r.Height-245);batch.Draw(paper,body,Color.White);Border(body,new Color(89,65,37),3);Border(new(body.X+6,body.Y+6,body.Width-12,body.Height-12),new Color(130,98,55)*.55f,1);
  Text(title,body.X+22,body.Y+16,Ink,.75f,true);
  Text(identified?item.Kind.ToString().ToUpperInvariant():"UNIDENTIFIED ITEM",body.X+23,body.Y+50,new Color(105,72,38),.47f,true);

  var art=new Rectangle(body.X+24,body.Y+82,224,185);DrawItemIllustration(item,art,identified);
  string quantity=stack.Quantity>1?$" x{stack.Quantity}":"";Center(title+quantity,new(art.X,art.Bottom+7,art.Width,21),new Color(92,65,36),.42f,true);
  int ly=art.Bottom+34;Text("LOCATION",art.X,ly,new Color(104,70,36),.43f,true);ly+=22;
  foreach(var line in Wrap(ItemLocationText(resolved),art.Width,.42f)){Text(line,art.X,ly,Ink,.42f);ly+=17;}
  Text($"WEIGHT  {stack.Weight:0.##} lb",art.X,ly+3,Ink,.42f);Text($"QUANTITY  {stack.Quantity}",art.X,ly+22,Ink,.42f);
  if(identified&&item.MagazineSize>0)Text($"LOADED  {stack.LoadedAmmunition?.Quantity??0}/{item.MagazineSize}",art.X,ly+41,Ink,.42f);
  if(identified&&item.MaxCharges>0)Text($"CHARGES  {stack.Charges}/{item.MaxCharges}",art.X,ly+41,Ink,.42f);

  int tx=body.X+274,tw=body.Right-tx-24,y=body.Y+80;
  Text("LORE",tx,y,new Color(104,70,36),.48f,true);y+=25;
  int loreLines=0;foreach(var line in Wrap(lore,tw,.53f)){Text(line,tx,y,Ink,.53f);y+=18;if(++loreLines>=5)break;}
  y=Math.Max(y+7,body.Y+190);Text("RULES",tx,y,new Color(104,70,36),.48f,true);y+=25;
  int ruleLines=0;foreach(var line in Wrap(mechanics,tw,.51f)){Text(line,tx,y,Ink,.51f);y+=18;if(++ruleLines>=3)break;}

  var facts=ItemRuleLines(resolved);int fy=Math.Max(y+7,body.Y+264);Text("PROPERTIES",tx,fy,new Color(104,70,36),.46f,true);fy+=23;
  int gap=14,columnWidth=(tw-gap)/2;for(int i=0;i<facts.Count;i++)
  {
   int col=i%2,row=i/2;int px=tx+col*(columnWidth+gap),py=fy+row*17;if(py>body.Bottom-20)break;
   string line=facts[i];while(Measure(line,.39f)>columnWidth-6&&line.Length>8)line=line[..^1];if(line!=facts[i])line=line.TrimEnd()+"…";Text("• "+line,px,py,Ink,.39f);
  }

  DrawItemInformationActions(r,resolved);
 }

 void DrawItemInformationActions(Rectangle r,ResolvedItemInspection resolved)
 {
  var stack=resolved.Stack;var item=stack.Definition;var actor=resolved.Actor;
  var actions=new List<(string Label,System.Action Run,bool Enabled,string Hint)>();
  void Add(string label,System.Action run,bool enabled=true,string hint="")=>actions.Add((label,run,enabled,hint));

  if(!stack.Identified)Add("IDENTIFY",()=>{world.IdentifyItem(stack.InstanceId);world.LogSystem($"{actor.Name} identifies {item.Name}.");},true,"Reveal the item's full name, lore and mechanical properties.");
  if(resolved.Location==ItemInformationLocation.Ground)
  {
   string why="";bool can=world.Mode==Mode.Explore&&actor.Alive&&world.CanCarry(actor,stack,out why);if(world.Mode==Mode.Explore&&!actor.Alive)why="This character cannot carry items while incapacitated.";var cell=resolved.GroundCell??actor.Pos;Add("PICK UP",()=>world.PickupGroundItemAt(actor,cell,resolved.Index,stack.Quantity),can,world.Mode!=Mode.Explore?"Ground transfers are locked during combat.":why);
  }
  else if(resolved.Location==ItemInformationLocation.Container&&resolved.Container is {} container)
  {
   bool can=world.Mode==Mode.Explore&&actor.Alive&&!container.Locked&&!container.DetectedTrap&&actor.Position.Distance(container.Pos.Center)<=1.25f;Add("TAKE",()=>world.TakeContainerItem(actor,container,resolved.Index,stack.Quantity),can,container.Locked?"The container is locked.":container.DetectedTrap?"The container is trapped.":"Move beside the container to take this item.");
  }
  else if(resolved.Location==ItemInformationLocation.Corpse&&resolved.Corpse is {} corpse)
  {
   bool can=world.Mode==Mode.Explore&&actor.Alive&&actor.Position.Distance(corpse.Position)<=1.25f;Add("TAKE",()=>world.TakeCorpseItem(actor,corpse,stack.InstanceId,stack.Quantity),can,"Move beside the corpse to take this item.");
  }
  else if(resolved.Location==ItemInformationLocation.Backpack)
  {
   if(item.EquipSlots().Any())
   {
    var slot=actor.Equipment.PreferredSlot(item);string why="";bool can=world.Mode==Mode.Explore&&actor.Alive&&world.CanEquipInSlot(actor,stack,slot,out why);if(world.Mode==Mode.Explore&&!actor.Alive)why="This character cannot change equipment while incapacitated.";Add("EQUIP",()=>{int index=actor.Inventory.Items.FindIndex(x=>x.InstanceId==stack.InstanceId);if(index>=0)world.Equip(actor,index);},can,world.Mode!=Mode.Explore?"Equipment changes are locked during combat.":why);
   }
   if(item.Id=="healing-potion")
   {
    bool can=world.CanUseOwnedItem(actor,stack.InstanceId,out var why);Add("DRINK POTION",()=>{if(world.UseOwnedItem(actor,stack.InstanceId)&&actor.Inventory.FindInstance(stack.InstanceId)==null)ui.Information.Close();},can,why);
   }
   bool canDrop=world.Mode==Mode.Explore&&actor.Alive;Add("DROP",()=>{int index=actor.Inventory.Items.FindIndex(x=>x.InstanceId==stack.InstanceId);if(index>=0)world.DropInventoryItem(actor,index,stack.Quantity);},canDrop,"Items can only be dropped during exploration.");
  }
  else if(resolved.Location==ItemInformationLocation.Equipment&&resolved.Slot is {} slot)
  {
   bool can=world.Mode==Mode.Explore&&actor.Alive&&actor.Inventory.CanAdd(stack);Add("UNEQUIP",()=>world.Unequip(actor,slot),can,world.Mode!=Mode.Explore?"Equipment changes are locked during combat.":"A free backpack slot is required.");
  }
  Add("DONE",()=>ui.Information.Close());

  int gap=8,max=5,count=Math.Min(max,actions.Count);int totalWidth=r.Width-50;int bw=(totalWidth-gap*(count-1))/count,bh=40,x=r.X+25,y=r.Bottom-58;
  for(int i=0;i<count;i++)
  {
   var action=actions[i];var q=new Rectangle(x+i*(bw+gap),y,bw,bh);CommandButton(q,action.Label,action.Run,action.Enabled,string.IsNullOrWhiteSpace(action.Hint)?action.Label:action.Hint,false,.55f);
  }
 }
}
