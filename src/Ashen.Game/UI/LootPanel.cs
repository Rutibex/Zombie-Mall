using Ashen.Core;
using Microsoft.Xna.Framework;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum LootPanelSourceKind { None,Container,Corpse,Ground }
 readonly record struct LootPanelDrag(bool FromSource,string InstanceId);

 sealed class LootPanel
 {
  readonly CovenantGame g;
  LootPanelSourceKind kind;
  Feature? feature;Actor? corpse;Cell groundCell;
  int sourcePage,backpackPage;
  Rectangle bounds,sourceFrame,backpackFrame;
  Rectangle[] sourceSlots=[],backpackSlots=[];
  LootPanelDrag? press,drag;Point pressPoint;bool dragging;
  const int PageSize=10;
  public LootPanel(CovenantGame game)=>g=game;
  public bool IsOpen=>kind!=LootPanelSourceKind.None;
  public Rectangle Bounds=>bounds;

  public void Open(Feature source)
  {
   if(!World.IsLootContainer(source))return;feature=source;corpse=null;kind=LootPanelSourceKind.Container;sourcePage=backpackPage=0;CancelDrag();PrepareOpen();
  }
  public void Open(Actor source)
  {
   if(source.Alive)return;corpse=source;feature=null;kind=LootPanelSourceKind.Corpse;sourcePage=backpackPage=0;CancelDrag();PrepareOpen();
  }
  public void Open(Cell source)
  {
   if(g.world.GroundPileAt(source)==null)return;groundCell=source;feature=null;corpse=null;kind=LootPanelSourceKind.Ground;sourcePage=backpackPage=0;CancelDrag();PrepareOpen();
  }
  void PrepareOpen()
  {
   g.CancelInventoryDrag();g.CancelFormationRightPointer();g.contextOpen=false;g.ui.Screens.ReturnToGame();g.ui.Modals.Clear();g.leftWorldPress=g.boxDragging=false;
  }
  public bool Close()
  {
   if(!IsOpen)return false;kind=LootPanelSourceKind.None;feature=null;corpse=null;CancelDrag();return true;
  }
  public void BindWorld()
  {
   g.world.LowerMallOpened=id=>g.ui.Rest.ShowLowerMall(id);
   g.world.ContractsOpened=()=>g.ui.Rest.ShowContracts();
   Close();g.world.ContainerLootRequested=Open;g.world.CorpseLootRequested=Open;g.world.GroundLootRequested=Open;
  }

  Actor Actor=>g.PrimaryPartyActor;
  string SourceTitle=>kind switch
  {
   LootPanelSourceKind.Container=>feature?.Name??"CONTAINER",
   LootPanelSourceKind.Corpse=>$"{corpse?.Name??"Unknown"} — REMAINS",
   LootPanelSourceKind.Ground=>$"GROUND — CELL {groundCell.X},{groundCell.Y}",
   _=>"LOOT"
  };
  List<ItemStack> SourceItems()=>kind switch
  {
   LootPanelSourceKind.Container=>feature?.Contents.Items.ToList()??[],
   LootPanelSourceKind.Corpse=>corpse==null?[]:g.world.CorpseLoot(corpse).ToList(),
   LootPanelSourceKind.Ground=>g.world.GroundPileAt(groundCell)?.Inventory.Items.ToList()??[],
   _=>[]
  };
  ItemStack? FindSource(string instanceId)=>SourceItems().FirstOrDefault(x=>x.InstanceId==instanceId);
  ItemStack? FindBackpack(string instanceId)=>Actor.Inventory.Items.FirstOrDefault(x=>x.InstanceId==instanceId);
  bool Split=>g.InventorySplitModifier;

  void Reflow()
  {
   var center=g.ui.Layout.CenterBounds;int h=Math.Min(330,Math.Max(284,g.ui.Layout.BottomShell.Height+132));bounds=new(center.X,center.Bottom-h,center.Width,h);
   int pad=16,centerW=160,gap=12,top=bounds.Y+54,frameH=bounds.Height-72;int sideW=(bounds.Width-pad*2-centerW-gap*2)/2;
   backpackFrame=new(bounds.X+pad,top,sideW,frameH);sourceFrame=new(backpackFrame.Right+gap+centerW+gap,top,sideW,frameH);
   backpackSlots=BuildSlots(backpackFrame);sourceSlots=BuildSlots(sourceFrame);
  }
  static Rectangle[] BuildSlots(Rectangle frame)
  {
   int slot=Math.Clamp((frame.Width-42)/5,47,61),gap=6,total=5*slot+4*gap,x=frame.Center.X-total/2,y=frame.Y+61;
   return Enumerable.Range(0,PageSize).Select(i=>new Rectangle(x+(i%5)*(slot+gap),y+(i/5)*(slot+gap),slot,slot)).ToArray();
  }
  int BackpackMaxPage=>Math.Max(0,(Actor.Inventory.Items.Count-1)/PageSize);
  int SourceMaxPage=>Math.Max(0,(SourceItems().Count-1)/PageSize);
  int BackpackIndex(string instanceId)=>Actor.Inventory.Items.FindIndex(x=>x.InstanceId==instanceId);
  int SourceIndex(string instanceId)=>kind switch
  {
   LootPanelSourceKind.Container=>feature?.Contents.Items.FindIndex(x=>x.InstanceId==instanceId)??-1,
   LootPanelSourceKind.Ground=>g.world.GroundPileAt(groundCell)?.Inventory.Items.FindIndex(x=>x.InstanceId==instanceId)??-1,
   _=>-1
  };

  LootPanelDrag? At(Point p)
  {
   var actor=Actor;backpackPage=Math.Clamp(backpackPage,0,BackpackMaxPage);sourcePage=Math.Clamp(sourcePage,0,SourceMaxPage);
   for(int i=0;i<backpackSlots.Length;i++){int index=backpackPage*PageSize+i;if(index<actor.Inventory.Items.Count&&backpackSlots[i].Contains(p))return new(false,actor.Inventory.Items[index].InstanceId);}
   var source=SourceItems();for(int i=0;i<sourceSlots.Length;i++){int index=sourcePage*PageSize+i;if(index<source.Count&&sourceSlots[i].Contains(p))return new(true,source[index].InstanceId);}
   return null;
  }
  bool InBackpack(Point p)=>backpackFrame.Contains(p);
  bool InSource(Point p)=>sourceFrame.Contains(p);

  public bool HandlePointer(bool down,bool held,bool up)
  {
   if(!IsOpen||g.ui.Modals.IsOpen){CancelDrag();return false;}
   if(down)
   {
    var hit=At(g.cursor);if(hit==null)return false;
    press=hit;drag=null;pressPoint=g.cursor;dragging=false;return true;
   }
   if(press!=null&&held)
   {
    if(!dragging&&V.Distance(new(pressPoint.X,pressPoint.Y),new(g.cursor.X,g.cursor.Y))>6){dragging=true;drag=press;}
    return true;
   }
   if(up&&press is {} clicked)
   {
    if(dragging&&drag is {} moving)Drop(moving,g.cursor);else Activate(clicked);CancelDrag();return true;
   }
   return press!=null;
  }
  public bool HandleRightPress(Point p)
  {
   if(!IsOpen)return false;return bounds.Contains(p);
  }
  void CancelDrag(){press=null;drag=null;dragging=false;}

  void Activate(LootPanelDrag source)
  {
   if(source.FromSource)Take(source.InstanceId);else Store(source.InstanceId);
  }
  void Drop(LootPanelDrag source,Point point)
  {
   if(source.FromSource&&InBackpack(point))Take(source.InstanceId);
   else if(!source.FromSource&&InSource(point))Store(source.InstanceId);
   else g.Notify(source.FromSource?"Drop loot on the companion backpack to take it.":"Drop a carried item on the loot source to store it there.");
  }
  void Take(string instanceId)
  {
   var stack=FindSource(instanceId);if(stack==null)return;int amount=Split?1:stack.Quantity;bool ok=kind switch
   {
    LootPanelSourceKind.Container=>feature!=null&&g.world.TakeContainerItem(Actor,feature,SourceIndex(instanceId),amount),
    LootPanelSourceKind.Ground=>g.world.PickupGroundItemAt(Actor,groundCell,SourceIndex(instanceId),amount),
    LootPanelSourceKind.Corpse=>corpse!=null&&g.world.TakeCorpseItem(Actor,corpse,instanceId,amount),
    _=>false
   };
   if(!ok)g.Notify(g.world.Log.LastOrDefault()??"That item cannot be taken.");
  }
  void Store(string instanceId)
  {
   int index=BackpackIndex(instanceId);if(index<0)return;var stack=Actor.Inventory.Items[index];int amount=Split?1:stack.Quantity;bool ok=kind switch
   {
    LootPanelSourceKind.Container=>feature!=null&&g.world.StoreContainerItem(Actor,feature,index,amount),
    LootPanelSourceKind.Ground=>g.world.StoreGroundItem(Actor,groundCell,index,amount),
    LootPanelSourceKind.Corpse=>corpse!=null&&g.world.StoreCorpseItem(Actor,corpse,index,amount),
    _=>false
   };
   if(!ok)g.Notify(g.world.Log.LastOrDefault()??"That item cannot be stored there.");
  }
  void TakeAll()
  {
   int moved=kind switch
   {
    LootPanelSourceKind.Container=>feature==null?0:g.world.TakeAllContainer(Actor,feature),
    LootPanelSourceKind.Ground=>g.world.TakeAllGround(Actor,groundCell),
    LootPanelSourceKind.Corpse=>corpse==null?0:g.world.TakeAllCorpse(Actor,corpse),
    _=>0
   };
   if(moved==0)g.Notify(g.world.Log.LastOrDefault()??"Nothing could be taken.");
  }

  public void Draw()
  {
   if(!IsOpen)return;Reflow();var actor=Actor;var source=SourceItems();backpackPage=Math.Clamp(backpackPage,0,BackpackMaxPage);sourcePage=Math.Clamp(sourcePage,0,SourceMaxPage);
   g.Panel(bounds);g.Fill(new(bounds.X+9,bounds.Y+9,bounds.Width-18,40),new Color(12,16,18)*.96f);g.Border(new(bounds.X+9,bounds.Y+9,bounds.Width-18,40),Gold*.45f);
   g.Text("LOOT",bounds.X+22,bounds.Y+18,Bright,.44f,true);g.Text(SourceTitle,bounds.X+112,bounds.Y+20,Gold,.50f);
   var close=new Rectangle(bounds.Right-100,bounds.Y+14,76,30);g.Button(close,"CLOSE",()=>Close(),false,.48f);

   DrawSide(backpackFrame,$"{actor.Name.ToUpperInvariant()} — BACKPACK",actor.Inventory.Items,backpackPage,backpackSlots,false,$"{actor.Inventory.UsedSlots}/{actor.Inventory.MaxSlots} slots   {actor.CarriedWeight:0.#}/{actor.CarryingCapacity:0.#} lb");
   bool shelter=kind==LootPanelSourceKind.Container&&feature!=null&&g.world.IsSecuringSite(feature);
   string sourceMeta=kind switch
   {
    LootPanelSourceKind.Container=>$"{source.Count} stack{(source.Count==1?"":"s")}   GOLD {feature?.Gold??0}",
    LootPanelSourceKind.Corpse=>$"{source.Count} recoverable item{(source.Count==1?"":"s")}",
    LootPanelSourceKind.Ground=>$"{source.Count} stack{(source.Count==1?"":"s")} at {groundCell.X},{groundCell.Y}",
    _=>""
   };
   if(g.world.Campaign?.SchemaVersion>=5)sourceMeta=shelter?$"EARNED {g.world.SecuredSurvivalValue} SV   SHARED POOL {g.world.ResourceXpRemainder} XP":$"{source.Count} stacks   UNSECURED {source.Sum(s=>g.world.UnsecuredSv(s))} SV";
   DrawSide(sourceFrame,kind==LootPanelSourceKind.Corpse?"CORPSE":"CONTENTS",source,sourcePage,sourceSlots,true,sourceMeta);

   int midX=backpackFrame.Right+18,midW=sourceFrame.X-backpackFrame.Right-36,y=bounds.Y+78;var mid=new Rectangle(midX,y,midW,bounds.Height-99);
   g.Center("TRANSFER",new(mid.X,mid.Y,mid.Width,22),Gold,.40f,true);y+=34;
   string state=AccessState(actor);Color stateColor=state=="ACCESSIBLE"?Teal:Red;g.Center(state,new(mid.X,mid.Y+33,mid.Width,42),stateColor,.40f,true);y+=86;
   var takeAll=new Rectangle(mid.X+4,y,mid.Width-8,36);bool any=source.Count>0||(kind==LootPanelSourceKind.Container&&(feature?.Gold??0)>0);g.CommandButton(takeAll,shelter?"STORE FINDS":"TAKE ALL",()=>{if(shelter){if(g.world.SecureCarriedResources(actor,feature!)==0)g.Notify("No new supplies could be secured. Inspect the shelter, move beside it, and carry scavenged finds.");}else TakeAll();},(shelter?actor.Inventory.Items.Any(s=>g.world.UnsecuredSv(s)>0):any)&&state=="ACCESSIBLE",shelter?"Store this survivor's scavenged supplies. Only uncredited units earn shared XP.":"Take every item that fits. Items that exceed slots or weight remain in the source.",false,.44f);y+=44;
   if(kind==LootPanelSourceKind.Container&&feature!=null&&feature.Gold>0)
   {var gold=new Rectangle(mid.X+4,y,mid.Width-8,34);g.CommandButton(gold,$"TAKE {feature.Gold} GOLD",()=>g.world.TakeContainerGold(actor,feature),state=="ACCESSIBLE","Move the container's coin directly into shared party gold.",false,.42f);y+=42;}
   if(kind==LootPanelSourceKind.Container&&feature!=null)
   {
    if(feature.DetectedTrap){var disarm=new Rectangle(mid.X+4,y,mid.Width-8,34);bool tools=HasTools(actor);g.CommandButton(disarm,"DISARM TRAP",()=>g.world.TryDisarmContainer(actor,feature),tools&&Near(actor,feature.Pos.Center),tools?"Attempt to disarm the detected trap with Sleight of Hand.":"Requires thieves' tools proficiency.",false,.40f);y+=42;}
    if(feature.Locked){var unlock=new Rectangle(mid.X+4,y,mid.Width-8,34);bool tools=HasTools(actor);g.CommandButton(unlock,"PICK LOCK",()=>g.world.TryUnlockContainer(actor,feature),tools&&Near(actor,feature.Pos.Center),tools?"Attempt to pick the lock with Sleight of Hand.":"Requires thieves' tools proficiency.",false,.40f);y+=42;}
   }
   g.Center("Click transfers a stack",new(mid.X,mid.Bottom-48,mid.Width,16),Muted,.32f);g.Center("Shift/Ctrl = one",new(mid.X,mid.Bottom-29,mid.Width,16),Muted,.32f);

   if(BlockedSource())
   {
    g.Fill(new(sourceFrame.X+5,sourceFrame.Y+48,sourceFrame.Width-10,sourceFrame.Height-55),new Color(25,12,13)*.82f);string label=feature?.DetectedTrap==true?"TRAPPED":"LOCKED";g.Center(label,new(sourceFrame.X,sourceFrame.Center.Y-23,sourceFrame.Width,40),Red,.76f,true);g.Center("Contents remain inaccessible",new(sourceFrame.X,sourceFrame.Center.Y+18,sourceFrame.Width,22),Bright,.45f);
   }

   if(dragging&&drag is {} moving)
   {
    var stack=moving.FromSource?FindSource(moving.InstanceId):FindBackpack(moving.InstanceId);if(stack!=null){var ghost=new Rectangle(g.cursor.X-28,g.cursor.Y-28,56,56);g.Fill(new(ghost.X-3,ghost.Y-3,ghost.Width+6,ghost.Height+6),Color.Black*.6f);g.DrawInventorySlot(ghost,stack,"",false);g.Border(ghost,Bright,2);}
   }
  }
  bool Near(Actor actor,WorldPoint point)=>actor.Position.Distance(point)<=1.25f;
  bool HasTools(Actor actor)=>actor.Character.Proficiencies.Tools.Contains("Thieves' tools",StringComparer.OrdinalIgnoreCase);
  bool BlockedSource()=>kind==LootPanelSourceKind.Container&&feature!=null&&(feature.Locked||feature.DetectedTrap);
  string AccessState(Actor actor)
  {
   if(g.world.Mode!=Mode.Explore)return "COMBAT LOCK";
   WorldPoint point=kind switch{LootPanelSourceKind.Container=>feature?.Pos.Center??actor.Position,LootPanelSourceKind.Corpse=>corpse?.Position??actor.Position,LootPanelSourceKind.Ground=>groundCell.Center,_=>actor.Position};
   if(!Near(actor,point))return "MOVE CLOSER";
   if(kind==LootPanelSourceKind.Container&&feature?.DetectedTrap==true)return "TRAPPED";
   if(kind==LootPanelSourceKind.Container&&feature?.Locked==true)return "LOCKED";
   if(kind==LootPanelSourceKind.Container&&feature!=null&&g.world.IsSecuringSite(feature)&&!g.world.PreviewSecuring(actor,feature).Allowed)return "NOT SECURE";
   return "ACCESSIBLE";
  }
  void DrawSide(Rectangle frame,string title,IReadOnlyList<ItemStack> items,int page,Rectangle[] slots,bool source,string meta)
  {
   g.Box(frame,new Color(17,20,20)*.96f,Gold*.42f);g.Text(title,frame.X+12,frame.Y+10,Gold,.43f,true);g.Text(meta,frame.X+12,frame.Y+34,Muted,.37f);
   int start=page*PageSize;for(int i=0;i<slots.Length;i++)
   {
    int index=start+i;var stack=index<items.Count?items[index]:null;g.DrawInventorySlot(slots[i],stack,"",false);
    if(stack!=null)
    {
     string visible=stack.Identified?stack.Definition.Name:$"Unidentified {stack.Definition.Kind.ToString().ToLowerInvariant()}";string side=source?SourceTitle:$"Carried by {Actor.Name}";if(g.world.Campaign?.SchemaVersion>=5)side+=$" | New secured value: {g.world.UnsecuredSv(stack)} SV";
     g.ui.Information.Register(slots[i],g.GearInformation(stack.Definition,side,stack,Actor),$"{visible}. Left-click transfers {(Split?"one item":"the stack")}; drag to the other side; right-click for Item Information.");
    }
   }
   int max=Math.Max(0,(items.Count-1)/PageSize);Rectangle prev=new Rectangle(frame.X+9,frame.Bottom-32,34,23),next=new Rectangle(frame.Right-43,frame.Bottom-32,34,23);
   g.CommandButton(prev,"<",()=>{if(source)sourcePage=Math.Max(0,sourcePage-1);else backpackPage=Math.Max(0,backpackPage-1);},page>0,"Previous loot page.",false,.42f);
   g.Center($"PAGE {page+1}/{max+1}",new(frame.Center.X-55,frame.Bottom-29,110,18),Muted,.35f);
   g.CommandButton(next,">",()=>{if(source)sourcePage=Math.Min(SourceMaxPage,sourcePage+1);else backpackPage=Math.Min(BackpackMaxPage,backpackPage+1);},page<max,"Next loot page.",false,.42f);
  }
 }
}
