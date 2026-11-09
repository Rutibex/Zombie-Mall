using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 /// <summary>IIP42 common lower-HUD shell for temple, inn and tavern services.
 /// Service availability and mutation stay in Core; this class only presents the authored provider.</summary>
 sealed class ServicePanel
 {
  readonly CovenantGame g;
  Feature? source;
  string serviceId="",selectedOfferingId="",selectedItemInstanceId="";
  int itemPage;
  Rectangle bounds,serviceFrame,detailFrame,targetFrame;
  const int ItemPageSize=5;
  public ServicePanel(CovenantGame game)=>g=game;
  public bool IsOpen=>source!=null&&!string.IsNullOrWhiteSpace(serviceId);
  public Rectangle Bounds=>bounds;
  ServiceDefinition Definition=>ServiceCatalog.Get(serviceId);
  ServiceProviderState State=>g.world.ServiceState(serviceId);
  Actor Actor=>g.PrimaryPartyActor;
  ServiceOfferingDefinition Offering=>Definition.Offering(selectedOfferingId);
  IReadOnlyList<ItemStack> Identifiable=>g.world.ServiceIdentifiableItems(Actor).ToList();

  public void BindWorld(){Close();g.world.ServiceRequested=Open;}

  public void Open(Feature feature)
  {
   if(!g.world.IsServiceFeature(feature)){g.Notify("There is no service provider available here.");return;}
   if(g.ui.Dialogue.IsOpen){g.Notify("Finish the conversation before requesting services.");return;}
   if(g.world.Mode!=Mode.Explore){g.Notify("Services are only available while exploring outside combat.");return;}
   PrepareOpen();source=feature;serviceId=feature.ServiceId;selectedOfferingId=Definition.Offerings.FirstOrDefault()?.Id??"";selectedItemInstanceId="";itemPage=0;EnsureSelection();
   g.world.Message(MessageCategory.Narrative,$"{Definition.Proprietor}: Welcome to {Definition.Name}.");
  }

  void PrepareOpen()
  {
   g.CancelInventoryDrag();g.CancelFormationRightPointer();g.ClearArmedSpell();g.ClearArmedFeatureAbility();g.pendingSpellPlan=null;g.quickSpellAssignmentSlot=-1;
   g.contextOpen=false;g.ui.Information.Close();g.ui.Modals.Clear();g.ui.Loot.Close();g.ui.Store.Close();g.ui.Rest.Close();g.ui.Screens.ReturnToGame();g.leftWorldPress=g.boxDragging=false;g.world.StopAll();
  }

  public bool Close()
  {
   if(!IsOpen)return false;source=null;serviceId=selectedOfferingId=selectedItemInstanceId="";itemPage=0;return true;
  }

  void EnsureSelection()
  {
   if(!Definition.Offerings.Any(o=>o.Id.Equals(selectedOfferingId,StringComparison.OrdinalIgnoreCase)))selectedOfferingId=Definition.Offerings.FirstOrDefault()?.Id??"";
   var items=Identifiable;itemPage=Math.Clamp(itemPage,0,Math.Max(0,(items.Count-1)/ItemPageSize));
   if(Offering.Kind==ServiceOfferingKind.Identification&&!items.Any(i=>i.InstanceId==selectedItemInstanceId))selectedItemInstanceId=items.Skip(itemPage*ItemPageSize).FirstOrDefault()?.InstanceId??items.FirstOrDefault()?.InstanceId??"";
  }

  void SelectOffering(string id){selectedOfferingId=id;selectedItemInstanceId="";itemPage=0;EnsureSelection();}

  public void HandleKeyboard()
  {
   // IIP42 historical validator marker: Keys.D1 Keys.D6 Keys.Up Keys.Down Keys.Enter (party selection is now routed through IIP46 bindings).
   if(!IsOpen)return;
   if(g.Pressed(Keys.Escape)){Close();return;}
   if(g.BindingPressed(InputAction.SelectParty1))g.Select(0);if(g.BindingPressed(InputAction.SelectParty2))g.Select(1);if(g.BindingPressed(InputAction.SelectParty3))g.Select(2);if(g.BindingPressed(InputAction.SelectParty4))g.Select(3);if(g.BindingPressed(InputAction.SelectParty5))g.Select(4);if(g.BindingPressed(InputAction.SelectParty6))g.Select(5);
   int index=Math.Max(0,Array.FindIndex(Definition.Offerings,o=>o.Id.Equals(selectedOfferingId,StringComparison.OrdinalIgnoreCase)));
   if(g.Pressed(Keys.Up)){index=Math.Max(0,index-1);SelectOffering(Definition.Offerings[index].Id);}
   if(g.Pressed(Keys.Down)){index=Math.Min(Definition.Offerings.Length-1,index+1);SelectOffering(Definition.Offerings[index].Id);}
   if(g.Pressed(Keys.Enter))Perform();EnsureSelection();
  }

  public bool HandleWheel(int delta)
  {
   if(!IsOpen||delta==0||!bounds.Contains(g.cursor))return false;
   if(Offering.Kind==ServiceOfferingKind.Identification&&targetFrame.Contains(g.cursor))
   {int steps=Math.Sign(delta);itemPage=Math.Clamp(itemPage-steps,0,Math.Max(0,(Identifiable.Count-1)/ItemPageSize));EnsureSelection();}
   return true;
  }

  public bool HandleRightPress(Point p)=>IsOpen&&bounds.Contains(p);

  ServicePreview Preview()=>g.world.PreviewService(serviceId,selectedOfferingId,Actor,selectedItemInstanceId);

  void Perform()
  {
   EnsureSelection();var preview=Preview();if(!preview.Available){g.Notify(preview.Reason);return;}
   if(!g.world.PerformService(serviceId,selectedOfferingId,Actor,selectedItemInstanceId)){g.Notify(g.world.Log.LastOrDefault()??"That service could not be completed.");return;}
   if(Offering.Kind==ServiceOfferingKind.Identification)selectedItemInstanceId="";EnsureSelection();
  }

  void Reflow()
  {
   var center=g.ui.Layout.CenterBounds;int h=Math.Clamp((int)Math.Round(center.Height*.58),430,520);bounds=new(center.X,center.Bottom-h,center.Width,h);
   int pad=15,gap=10,top=bounds.Y+62,bottom=20;int available=bounds.Width-pad*2-gap*2;int left=(int)(available*.28f),right=(int)(available*.31f),mid=available-left-right;
   serviceFrame=new(bounds.X+pad,top,left,bounds.Height-(top-bounds.Y)-bottom);detailFrame=new(serviceFrame.Right+gap,top,mid,bounds.Height-(top-bounds.Y)-bottom);targetFrame=new(detailFrame.Right+gap,top,right,bounds.Height-(top-bounds.Y)-bottom);
  }

  public void Draw()
  {
   if(!IsOpen)return;Reflow();EnsureSelection();var def=Definition;
   g.Panel(bounds);g.buttons.Add((bounds,()=>{}));g.Fill(new(bounds.X+9,bounds.Y+9,bounds.Width-18,45),new Color(11,14,15)*.97f);g.Border(new(bounds.X+9,bounds.Y+9,bounds.Width-18,45),Gold*.44f);
   g.Text(def.Name.ToUpperInvariant(),bounds.X+22,bounds.Y+18,Bright,.59f,true);g.Text($"{def.Proprietor} · {def.Category} · Party gold {g.world.Gold} gp · {g.world.CampaignTimeText}",bounds.X+285,bounds.Y+21,Gold,.42f);
   var close=new Rectangle(bounds.Right-92,bounds.Y+15,68,30);g.Button(close,"CLOSE",()=>Close(),false,.46f);
   DrawServiceList(serviceFrame);DrawDetails(detailFrame);DrawTarget(targetFrame);
  }

  void DrawServiceList(Rectangle frame)
  {
   g.Box(frame,new Color(16,19,19)*.96f,Gold*.40f);g.Text("AVAILABLE SERVICES",frame.X+12,frame.Y+10,Gold,.41f,true);string providerState=Definition.Category.Contains("TEMPLE",StringComparison.OrdinalIgnoreCase)?$"Receipts {State.Revenue} gp · Favor {State.Favor}/10":$"Provider receipts: {State.Revenue} gp";g.Text(providerState,frame.X+12,frame.Y+32,Muted,.32f);
   int y=frame.Y+59,rowH=48,gap=4;
   for(int i=0;i<Definition.Offerings.Length;i++)
   {
    var offering=Definition.Offerings[i];var row=new Rectangle(frame.X+8,y+i*(rowH+gap),frame.Width-16,rowH);bool selected=offering.Id.Equals(selectedOfferingId,StringComparison.OrdinalIgnoreCase);
    g.Box(row,selected?new Color(70,57,35):new Color(10,13,14),selected?Bright:Gold*.23f);g.Text(offering.Name,row.X+10,row.Y+6,Bright,.36f,true);g.Text($"{offering.Price} gp",row.X+10,row.Y+26,Gold,.32f);g.Text(offering.Kind.ToString().ToUpperInvariant(),row.Right-g.Measure(offering.Kind.ToString().ToUpperInvariant(),.25f)-9,row.Y+29,Muted,.25f);
    string id=offering.Id;g.buttons.Add((row,()=>SelectOffering(id)));if(row.Contains(g.cursor))g.tooltip=$"{offering.Description} Left-click to select.";
   }
   int yy=frame.Bottom-53;g.Text("KEYBOARD",frame.X+11,yy,Gold,.30f,true);g.Text($"↑/↓ service · party {g.PartySlotBindingSummary()}",frame.X+11,yy+17,Muted,.29f);g.Text("Enter confirm · Esc close",frame.X+11,yy+33,Muted,.29f);
  }

  void DrawDetails(Rectangle frame)
  {
   g.Box(frame,new Color(13,16,17)*.98f,Gold*.50f);int x=frame.X+14,y=frame.Y+12,w=frame.Width-28;var offering=Offering;var preview=Preview();
   g.Center(offering.Name.ToUpperInvariant(),new(x,y,w,24),Bright,.47f,true);y+=31;g.Center($"{offering.Price} GP",new(x,y,w,20),Gold,.39f,true);y+=29;
   g.Text("SERVICE",x,y,Gold,.32f,true);y+=19;foreach(var line in g.Wrap(offering.Description,w,.34f).Take(5)){g.Text(line,x,y,Muted,.34f);y+=17;}y+=6;
   g.Text("TARGET",x,y,Gold,.32f,true);y+=19;g.Text(preview.TargetLabel,x,y,Bright,.38f,true);y+=22;
   foreach(var line in g.Wrap(preview.OutcomeText,w,.33f).Take(4)){g.Text(line,x,y,Muted,.33f);y+=17;}y+=7;
   g.Text($"Party funds: {g.world.Gold} gp",x,y,Muted,.31f);y+=19;
   if(!preview.Available)foreach(var line in g.Wrap(preview.Reason,w,.32f).Take(3)){g.Text(line,x,y,Red,.32f);y+=16;}
   else g.Text("Service can be completed now.",x,y,Teal,.31f);
   var use=new Rectangle(x,frame.Bottom-42,w,31);g.CommandButton(use,$"PAY {preview.Price} GP — {offering.Name.ToUpperInvariant()}",Perform,preview.Available,preview.Available?"Purchase the selected service.":preview.Reason,false,.35f);
  }

  void DrawTarget(Rectangle frame)
  {
   g.Box(frame,new Color(16,19,19)*.96f,Gold*.40f);var offering=Offering;g.Text("SERVICE TARGET",frame.X+12,frame.Y+10,Gold,.41f,true);
   if(offering.Kind==ServiceOfferingKind.Identification){DrawIdentificationTargets(frame);return;}
   if(offering.Target is ServiceTargetKind.Companion or ServiceTargetKind.FallenCompanion)
   {
    var a=Actor;string life=a.Dead?"DEAD":a.Unconscious?"UNCONSCIOUS":a.Alive?"LIVING":"INCAPACITATED";Color lifeColor=a.Dead?Red:a.Hp<a.MaxHp?Gold:Teal;
    g.Center(a.Name.ToUpperInvariant(),new(frame.X+12,frame.Y+62,frame.Width-24,28),Bright,.49f,true);g.Center($"{a.Class} · Level {a.TotalLevel}",new(frame.X+12,frame.Y+91,frame.Width-24,20),Muted,.33f);g.Center(life,new(frame.X+12,frame.Y+120,frame.Width-24,21),lifeColor,.36f,true);
    g.Center($"HP {a.Hp}/{a.MaxHp}",new(frame.X+12,frame.Y+151,frame.Width-24,24),Bright,.43f,true);
    int barW=frame.Width-52;var bar=new Rectangle(frame.X+26,frame.Y+183,barW,12);g.Fill(bar,new Color(52,43,36));g.Fill(new(bar.X,bar.Y,(int)(bar.Width*Math.Clamp(a.Hp/(float)Math.Max(1,a.MaxHp),0,1)),bar.Height),lifeColor);g.Border(bar,Gold*.36f);
    g.Text("Choose another companion with",frame.X+14,frame.Y+218,Muted,.31f);g.Text($"the right portrait rail or party keys {g.PartySlotBindingSummary()}.",frame.X+14,frame.Y+235,Muted,.31f);
    if(offering.Kind==ServiceOfferingKind.Resurrection)g.Text("Only an explicit DEAD condition qualifies.",frame.X+14,frame.Y+270,Gold,.30f);
    return;
   }
   g.Center("ENTIRE PARTY",new(frame.X+12,frame.Y+70,frame.Width-24,28),Bright,.48f,true);g.Center(g.world.CampaignTimeText,new(frame.X+12,frame.Y+105,frame.Width-24,22),Gold,.38f,true);
   string text=offering.Kind switch{ServiceOfferingKind.Room=>"Safe lodging applies the current party long-rest recovery routine.",ServiceOfferingKind.Drink=>"A short common-room visit advances campaign time.",ServiceOfferingKind.Rumor=>"A purchased rumor is recorded in the Story Journal.",ServiceOfferingKind.Donation=>$"Shrine favor is currently {State.Favor}/10; donations raise it by one.",_=>"No individual target is required."};
   int y=frame.Y+148;foreach(var line in g.Wrap(text,frame.Width-28,.34f)){g.Text(line,frame.X+14,y,Muted,.34f);y+=18;}
  }

  void DrawIdentificationTargets(Rectangle frame)
  {
   var items=Identifiable;g.Text($"{Actor.Name.ToUpperInvariant()} — UNIDENTIFIED",frame.X+12,frame.Y+34,Bright,.34f,true);g.Text($"Portraits / party keys {g.PartySlotBindingSummary()} change owner",frame.X+12,frame.Y+51,Muted,.28f);
   int start=itemPage*ItemPageSize,y=frame.Y+75,rowH=42,gap=4;
   for(int i=0;i<ItemPageSize;i++)
   {
    int index=start+i;Rectangle row=new Rectangle(frame.X+8,y+i*(rowH+gap),frame.Width-16,rowH);g.Box(row,new Color(10,13,14),Gold*.20f);if(index>=items.Count)continue;var stack=items[index];bool selected=stack.InstanceId==selectedItemInstanceId;if(selected){g.Fill(new(row.X+2,row.Y+2,row.Width-4,row.Height-4),new Color(73,59,34)*.48f);g.Border(row,Bright,2);}var icon=new Rectangle(row.X+4,row.Y+4,34,34);g.DrawInventorySlot(icon,stack,"",false);g.Text($"Unidentified {stack.Definition.Kind.ToString().ToLowerInvariant()}",row.X+44,row.Y+5,Bright,.32f);bool equipped=Actor.Equipment.All().Any(x=>x.InstanceId==stack.InstanceId);g.Text(equipped?"Equipped":"Backpack",row.X+44,row.Y+23,Muted,.27f);string id=stack.InstanceId;g.buttons.Add((row,()=>selectedItemInstanceId=id));var info=g.GearInformation(stack.Definition,$"Unidentified item carried by {Actor.Name}.",stack,Actor);g.ui.Information.Register(row,info,"Left-click selects this item for identification; right-click opens Item Information.");
   }
   if(items.Count==0)g.Center("No unidentified items",new(frame.X+10,frame.Y+115,frame.Width-20,30),Muted,.37f);
   int max=Math.Max(0,(items.Count-1)/ItemPageSize);Rectangle prev=new Rectangle(frame.X+9,frame.Bottom-29,32,21),next=new Rectangle(frame.Right-41,frame.Bottom-29,32,21);g.CommandButton(prev,"<",()=>{itemPage=Math.Max(0,itemPage-1);EnsureSelection();},itemPage>0,"Previous item page.",false,.39f);g.Center($"PAGE {itemPage+1}/{max+1}",new(frame.Center.X-48,frame.Bottom-27,96,18),Muted,.32f);g.CommandButton(next,">",()=>{itemPage=Math.Min(max,itemPage+1);EnsureSelection();},itemPage<max,"Next item page.",false,.39f);
  }
 }
}
