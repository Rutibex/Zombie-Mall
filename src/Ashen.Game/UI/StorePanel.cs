using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum StoreMode { Buy,Sell }

 /// <summary>
 /// IIP41 lower-HUD merchant workflow. The world remains visible above the panel while
 /// merchant stock, the selected companion's backpack, quantity, price, affordability,
 /// usability and equipment comparison are visible in one interaction surface.
 /// </summary>
 sealed class StorePanel
 {
  readonly CovenantGame g;
  Feature? source;
  string storeId="";
  StoreMode mode=StoreMode.Buy;
  string selectedStoreInstanceId="";
  string selectedPackInstanceId="";
  int quantity=1,storePage,packPage;
  Rectangle bounds,storeFrame,packFrame,detailFrame;
  const int PageSize=5;
  public StorePanel(CovenantGame game)=>g=game;
  public bool IsOpen=>source!=null&&!string.IsNullOrWhiteSpace(storeId);
  public Rectangle Bounds=>bounds;

  StoreDefinition Definition=>StoreCatalog.Get(storeId);
  StoreState State=>g.world.StoreState(storeId);
  Actor Actor=>g.PrimaryPartyActor;
  ItemStack? SelectedStoreStack=>State.Stock.FindInstance(selectedStoreInstanceId);
  ItemStack? SelectedPackStack=>Actor.Inventory.FindInstance(selectedPackInstanceId);
  ItemStack? SelectedStack=>mode==StoreMode.Buy?SelectedStoreStack:SelectedPackStack;

  public void BindWorld(){Close();g.world.StoreRequested=Open;}

  public void Open(Feature feature)
  {
   if(!g.world.IsStoreFeature(feature)){g.Notify("There is no merchant available here.");return;}
   if(g.ui.Dialogue.IsOpen){g.Notify("Finish the conversation before trading.");return;}
   if(g.world.Mode!=Mode.Explore){g.Notify("Trading is only available while exploring outside combat.");return;}
   PrepareOpen();source=feature;storeId=feature.StoreId;mode=StoreMode.Buy;quantity=1;storePage=packPage=0;
   selectedStoreInstanceId=State.Stock.Items.FirstOrDefault()?.InstanceId??"";selectedPackInstanceId=Actor.Inventory.Items.FirstOrDefault()?.InstanceId??"";
   g.world.Message(MessageCategory.Narrative,$"{Definition.Proprietor}: Welcome to {Definition.Name}.");
  }

  void PrepareOpen()
  {
   g.CancelInventoryDrag();g.CancelFormationRightPointer();g.ClearArmedSpell();g.ClearArmedFeatureAbility();g.pendingSpellPlan=null;g.quickSpellAssignmentSlot=-1;
   g.contextOpen=false;g.ui.Information.Close();g.ui.Modals.Clear();g.ui.Loot.Close();g.ui.Services.Close();g.ui.Rest.Close();g.ui.Screens.ReturnToGame();g.leftWorldPress=g.boxDragging=false;g.world.StopAll();
  }

  public bool Close()
  {
   if(!IsOpen)return false;source=null;storeId="";selectedStoreInstanceId=selectedPackInstanceId="";quantity=1;storePage=packPage=0;return true;
  }

  void EnsureSelection()
  {
   storePage=Math.Clamp(storePage,0,Math.Max(0,(State.Stock.Items.Count-1)/PageSize));
   packPage=Math.Clamp(packPage,0,Math.Max(0,(Actor.Inventory.Items.Count-1)/PageSize));
   if(SelectedStoreStack==null)selectedStoreInstanceId=State.Stock.Items.Skip(storePage*PageSize).FirstOrDefault()?.InstanceId??State.Stock.Items.FirstOrDefault()?.InstanceId??"";
   if(SelectedPackStack==null)selectedPackInstanceId=Actor.Inventory.Items.Skip(packPage*PageSize).FirstOrDefault()?.InstanceId??Actor.Inventory.Items.FirstOrDefault()?.InstanceId??"";
   int max=Math.Max(1,SelectedStack?.Quantity??1);quantity=Math.Clamp(quantity,1,max);
  }

  public void HandleKeyboard()
  {
   // IIP41 historical validator marker: Keys.D1 Keys.D6 (party selection is now routed through IIP46 bindings).
   if(!IsOpen)return;
   if(g.Pressed(Keys.Escape)){Close();return;}
   if(g.Pressed(Keys.B)){mode=StoreMode.Buy;quantity=1;EnsureSelection();}
   if(g.Pressed(Keys.S)){mode=StoreMode.Sell;quantity=1;EnsureSelection();}
   if(g.Pressed(Keys.OemMinus)||g.Pressed(Keys.Subtract))quantity=Math.Max(1,quantity-1);
   if(g.Pressed(Keys.OemPlus)||g.Pressed(Keys.Add))quantity=Math.Min(Math.Max(1,SelectedStack?.Quantity??1),quantity+1);
   if(g.Pressed(Keys.Enter))Transact();
   if(g.BindingPressed(InputAction.SelectParty1))g.Select(0);if(g.BindingPressed(InputAction.SelectParty2))g.Select(1);if(g.BindingPressed(InputAction.SelectParty3))g.Select(2);if(g.BindingPressed(InputAction.SelectParty4))g.Select(3);if(g.BindingPressed(InputAction.SelectParty5))g.Select(4);if(g.BindingPressed(InputAction.SelectParty6))g.Select(5);
   EnsureSelection();
  }

  public bool HandleWheel(int delta)
  {
   if(!IsOpen||delta==0||!bounds.Contains(g.cursor))return false;int steps=Math.Sign(delta);
   if(storeFrame.Contains(g.cursor)){storePage=Math.Clamp(storePage-steps,0,Math.Max(0,(State.Stock.Items.Count-1)/PageSize));return true;}
   if(packFrame.Contains(g.cursor)){packPage=Math.Clamp(packPage-steps,0,Math.Max(0,(Actor.Inventory.Items.Count-1)/PageSize));return true;}
   return true;
  }

  public bool HandleRightPress(Point p)=>IsOpen&&bounds.Contains(p);

  void SelectStore(string id){selectedStoreInstanceId=id;mode=StoreMode.Buy;quantity=1;}
  void SelectPack(string id){selectedPackInstanceId=id;mode=StoreMode.Sell;quantity=1;}

  StorePricePreview Preview()=>mode==StoreMode.Buy
   ?(SelectedStoreStack==null?new(0,0,0,false,"Select an item from the merchant's stock."):g.world.PreviewStoreBuy(storeId,Actor,SelectedStoreStack.InstanceId,quantity))
   :(SelectedPackStack==null?new(0,0,0,false,"Select an item from the companion backpack."):g.world.PreviewStoreSell(storeId,Actor,SelectedPackStack.InstanceId,quantity));

  void Transact()
  {
   EnsureSelection();var stack=SelectedStack;if(stack==null){g.Notify(mode==StoreMode.Buy?"Select something to buy.":"Select something to sell.");return;}
   string id=stack.InstanceId;bool ok=mode==StoreMode.Buy?g.world.BuyFromStore(storeId,Actor,id,quantity):g.world.SellToStore(storeId,Actor,id,quantity);
   if(!ok){g.Notify(g.world.Log.LastOrDefault()??"That trade could not be completed.");return;}
   quantity=1;EnsureSelection();
  }

  static string Delta(int value)=>value==0?"—":value>0?$"+{value}":value.ToString();
  static string WeightDelta(float value)=>Math.Abs(value)<.001f?"—":value>0?$"+{value:0.#}":$"{value:0.#}";

  void Reflow()
  {
   var center=g.ui.Layout.CenterBounds;int h=Math.Clamp((int)Math.Round(center.Height*.48),350,430);bounds=new(center.X,center.Bottom-h,center.Width,h);
   int pad=15,gap=10,top=bounds.Y+62,bottom=20;int available=bounds.Width-pad*2-gap*2;int side=(int)(available*.34f),mid=available-side*2;
   storeFrame=new(bounds.X+pad,top,side,bounds.Height-(top-bounds.Y)-bottom);detailFrame=new(storeFrame.Right+gap,top,mid,bounds.Height-(top-bounds.Y)-bottom);packFrame=new(detailFrame.Right+gap,top,side,bounds.Height-(top-bounds.Y)-bottom);
  }

  public void Draw()
  {
   if(!IsOpen)return;Reflow();EnsureSelection();var def=Definition;var state=State;var actor=Actor;
   g.Panel(bounds);g.buttons.Add((bounds,()=>{}));g.Fill(new(bounds.X+9,bounds.Y+9,bounds.Width-18,45),new Color(11,14,15)*.97f);g.Border(new(bounds.X+9,bounds.Y+9,bounds.Width-18,45),Gold*.44f);
   g.Text(def.Name.ToUpperInvariant(),bounds.X+22,bounds.Y+18,Bright,.59f,true);g.Text($"{def.Proprietor} · Party gold {g.world.Gold} gp · Merchant purse {state.Gold} gp",bounds.X+275,bounds.Y+21,Gold,.44f);
   var close=new Rectangle(bounds.Right-92,bounds.Y+15,68,30);g.Button(close,"CLOSE",()=>Close(),false,.46f);

   DrawInventoryPane(storeFrame,"MERCHANT STOCK",state.Stock.Items,storePage,true);
   DrawDetails(detailFrame);
   DrawInventoryPane(packFrame,$"{actor.Name.ToUpperInvariant()} — BACKPACK",actor.Inventory.Items,packPage,false);
  }

  void DrawInventoryPane(Rectangle frame,string title,IReadOnlyList<ItemStack> items,int page,bool merchant)
  {
   g.Box(frame,new Color(16,19,19)*.96f,Gold*.40f);g.Text(title,frame.X+12,frame.Y+10,Gold,.43f,true);
   string meta=merchant?$"{items.Sum(x=>x.Quantity)} items · sell price {Definition.BuyPercent}% base":$"{Actor.Inventory.UsedSlots}/{Actor.Inventory.MaxSlots} slots · {Actor.CarriedWeight:0.#}/{Actor.CarryingCapacity:0.#} lb";
   g.Text(meta,frame.X+12,frame.Y+32,Muted,.34f);
   int y=frame.Y+57,rowH=48,start=page*PageSize;
   for(int i=0;i<PageSize;i++)
   {
    int index=start+i;var row=new Rectangle(frame.X+8,y+i*(rowH+4),frame.Width-16,rowH);g.Box(row,new Color(10,13,14),Gold*.20f);
    if(index>=items.Count)continue;var stack=items[index];bool selected=(merchant?selectedStoreInstanceId:selectedPackInstanceId)==stack.InstanceId&&mode==(merchant?StoreMode.Buy:StoreMode.Sell);if(selected){g.Fill(new(row.X+2,row.Y+2,row.Width-4,row.Height-4),new Color(73,59,34)*.48f);g.Border(row,Bright,2);}
    var icon=new Rectangle(row.X+4,row.Y+4,40,40);g.DrawInventorySlot(icon,stack,"",false);
    string name=stack.Identified?stack.Definition.Name:$"Unidentified {stack.Definition.Kind.ToString().ToLowerInvariant()}";if(name.Length>25)name=name[..24]+"…";
    g.Text(name,row.X+50,row.Y+7,Bright,.39f);g.Text($"Qty {stack.Quantity}",row.X+50,row.Y+26,Muted,.33f);
    int unit=merchant?g.world.StoreBuyUnitPrice(storeId,stack.Definition):g.world.StoreSellUnitPrice(storeId,stack.Definition);string price=unit<=0?"NO TRADE":$"{unit} gp ea";g.Text(price,row.Right-90,row.Y+16,unit>0?Gold:Red,.34f,true);
    string id=stack.InstanceId;g.buttons.Add((row,()=>{if(merchant)SelectStore(id);else SelectPack(id);}));
    var info=merchant?g.GearInformation(stack.Definition,$"Offered by {Definition.Name} for {Math.Max(0,unit)} gp each.",null,Actor):g.GearInformation(stack.Definition,$"Carried by {Actor.Name}; {Math.Max(0,unit)} gp sale value each.",stack,Actor);
    g.ui.Information.Register(row,info,$"{name}. Left-click selects it for {(merchant?"purchase":"sale")}; right-click opens Item Information.");
   }
   int max=Math.Max(0,(items.Count-1)/PageSize);Rectangle prev=new Rectangle(frame.X+9,frame.Bottom-29,32,21),next=new Rectangle(frame.Right-41,frame.Bottom-29,32,21);
   g.CommandButton(prev,"<",()=>{if(merchant)storePage=Math.Max(0,storePage-1);else packPage=Math.Max(0,packPage-1);},page>0,"Previous store page.",false,.39f);
   g.Center($"PAGE {page+1}/{max+1}",new(frame.Center.X-48,frame.Bottom-27,96,18),Muted,.32f);
   g.CommandButton(next,">",()=>{if(merchant)storePage=Math.Min(max,storePage+1);else packPage=Math.Min(max,packPage+1);},page<max,"Next store page.",false,.39f);
  }

  void DrawDetails(Rectangle frame)
  {
   g.Box(frame,new Color(13,16,17)*.98f,Gold*.50f);int x=frame.X+12,y=frame.Y+10,w=frame.Width-24;
   Rectangle buy=new Rectangle(x,y,(w-6)/2,28),sell=new Rectangle(x+(w-6)/2+6,y,(w-6)/2,28);g.Button(buy,"BUY [B]",()=>{mode=StoreMode.Buy;quantity=1;EnsureSelection();},mode==StoreMode.Buy,.42f);g.Button(sell,"SELL [S]",()=>{mode=StoreMode.Sell;quantity=1;EnsureSelection();},mode==StoreMode.Sell,.42f);y+=41;
   var stack=SelectedStack;if(stack==null){g.Center(mode==StoreMode.Buy?"Select merchant stock":"Select backpack item",new(x,y,w,30),Muted,.40f);return;}
   var item=stack.Definition;g.Center(item.Name.ToUpperInvariant(),new(x,y,w,24),Bright,.45f,true);y+=29;
   var comparison=g.world.CompareStoreGear(Actor,item);Color usability=comparison.Usable?Teal:Red;g.Center(comparison.Usable?$"{Actor.Name.ToUpperInvariant()}: CAN USE":$"{Actor.Name.ToUpperInvariant()}: CANNOT USE",new(x,y,w,22),usability,.34f,true);y+=23;
   string reason=comparison.Usable?(comparison.IsEquipment?$"Compares against {comparison.EquippedName} in {comparison.Slot}.":"No equipment proficiency is required."):comparison.Reason;
   foreach(var line in g.Wrap(reason,w,.33f).Take(2)){g.Text(line,x,y,comparison.Usable?Muted:Red*.88f,.33f);y+=16;}y+=4;
   if(comparison.IsEquipment)
   {
    g.Text("EQUIPMENT COMPARISON",x,y,Gold,.33f,true);y+=18;g.Text($"Attack {Delta(comparison.AttackDelta)}   Damage {Delta(comparison.DamageDelta)}",x,y,Bright,.32f);y+=17;g.Text($"AC {Delta(comparison.ArmorClassDelta)}   Weight {WeightDelta(comparison.WeightDelta)} lb",x,y,Bright,.32f);y+=21;
   }
   else {g.Text($"{item.Kind} · {item.Weight:0.#} lb each",x,y,Muted,.32f);y+=21;}

   int max=Math.Max(1,stack.Quantity);quantity=Math.Clamp(quantity,1,max);g.Text("QUANTITY",x,y,Gold,.32f,true);y+=19;
   Rectangle minus=new Rectangle(x,y,31,26),qty=new Rectangle(minus.Right+5,y,w-72,26),plus=new Rectangle(qty.Right+5,y,31,26);g.CommandButton(minus,"-",()=>quantity=Math.Max(1,quantity-1),quantity>1,"Reduce trade quantity.",false,.40f);g.Box(qty,new Color(8,11,12),Gold*.35f);g.Center(quantity.ToString(),qty,Bright,.46f,true);g.CommandButton(plus,"+",()=>quantity=Math.Min(max,quantity+1),quantity<max,"Increase trade quantity.",false,.40f);y+=32;
   var maxButton=new Rectangle(x,y,w,24);g.CommandButton(maxButton,"MAX STACK",()=>quantity=max,max>1,"Select the entire available stack.",false,.34f);y+=31;

   var preview=Preview();g.Text($"UNIT {preview.UnitPrice} gp",x,y,Muted,.32f);g.Text($"TOTAL {preview.TotalPrice} gp",x+w/2,y,Gold,.36f,true);y+=22;
   string funds=mode==StoreMode.Buy?$"Party funds: {g.world.Gold} gp":$"Merchant funds: {State.Gold} gp";g.Text(funds,x,y,Muted,.31f);y+=20;
   if(!preview.Affordable)
   {
    foreach(var line in g.Wrap(preview.Reason,w,.32f).Take(2)){g.Text(line,x,y,Red,.32f);y+=16;}
   }
   else {g.Text(mode==StoreMode.Buy?$"{Actor.Name} has room for this purchase.":"Merchant can complete this purchase.",x,y,Teal,.31f);y+=17;}
   y=Math.Max(y,frame.Bottom-45);var trade=new Rectangle(x,frame.Bottom-39,w,30);g.CommandButton(trade,mode==StoreMode.Buy?$"BUY FOR {preview.TotalPrice} GP":$"SELL FOR {preview.TotalPrice} GP",Transact,preview.Affordable,preview.Reason,false,.39f);
  }
 }
}
