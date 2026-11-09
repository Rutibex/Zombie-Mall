using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 sealed partial class RestPanel
 {
  bool equipmentView,equipmentSearchFocus;
  string equipmentCategory="all",equipmentQuery="",equipmentSelection="",equipmentActor="";
  int equipmentPage,equipmentRawPage;
  Actor? equipmentTarget;
  Cell? equipmentCell;
  static readonly string[] EquipmentCategories=["all","commodity","melee","ranged","armour","explosive","ammunition","campaign"];
  public void ShowEquipment(Actor? actor=null,Actor? target=null,Cell? cell=null)
  {
   if(!g.world.EquipmentEnabled||g.world.ReactionInterruptActive||g.ui.Dialogue.IsOpen)return;
   PrepareOpen();open=true;equipmentView=true;classView=worksView=shelterView=tensionView=false;equipmentActor=(actor??g.PrimaryPartyActor).Id;equipmentTarget=target;equipmentCell=cell;equipmentSearchFocus=false;
  }
  public void PreviewEquipment(string category,string query=""){equipmentCategory=category;equipmentQuery=query;equipmentPage=0;equipmentSelection="";equipmentRawPage=0;}
  public void EquipmentText(char ch)
  {if(!open||!equipmentView||!equipmentSearchFocus||char.IsControl(ch)||equipmentQuery.Length>=60)return;equipmentQuery+=ch;equipmentPage=0;}
  void EquipmentKeys()
  {
   if(equipmentSearchFocus&&g.Pressed(Keys.Back)&&equipmentQuery.Length>0){equipmentQuery=equipmentQuery[..^1];equipmentPage=0;}
   if(g.Pressed(Keys.PageDown))equipmentPage++;if(g.Pressed(Keys.PageUp))equipmentPage=Math.Max(0,equipmentPage-1);
  }
  void DrawEquipment()
  {
   var w=g.world;var actor=w.Party.FirstOrDefault(a=>a.Id==equipmentActor)??w.PartyLeader;g.Panel(bounds);g.buttons.Add((bounds,()=>{}));int x=bounds.X+20,y=bounds.Y+18;
   g.Text("EQUIPMENT CATALOGUE",x,y,Bright,.56f,true);g.Text($"{w.Campaign!.Items.Count} definitions | 496 source rows",x+395,y+5,Gold,.42f);
   if(w.WorkshopEnabled)g.Button(new(bounds.Right-460,y,170,28),"WORKSHOP / BARTER",()=>ShowWorkshop(actor),false,.37f);
   g.Button(new(bounds.Right-280,y,175,28),"NEXT SURVIVOR",()=>{var crew=w.Party.ToArray();int i=Array.IndexOf(crew,actor);equipmentActor=crew[(i+1)%crew.Length].Id;},false,.4f);g.Button(new(bounds.Right-90,y,65,28),"CLOSE",()=>Close(),false,.4f);
   int tabWidth=(bounds.Width-40)/8;int ty=bounds.Y+59;for(int i=0;i<EquipmentCategories.Length;i++){string cat=EquipmentCategories[i];g.Button(new(x+i*tabWidth,ty,tabWidth-6,27),cat.ToUpperInvariant(),()=>{equipmentCategory=cat;equipmentPage=0;equipmentSearchFocus=false;},equipmentCategory==cat,.37f);}
   int listWidth=(bounds.Width-65)/2,right=x+listWidth+20,searchY=ty+38;
   g.Button(new(x,searchY,listWidth-110,28),equipmentQuery==""?"SEARCH NAME / ID...":equipmentQuery,()=>equipmentSearchFocus=true,equipmentSearchFocus,.4f);
   g.Button(new(x+listWidth-102,searchY,102,28),"CLEAR",()=>{equipmentQuery="";equipmentPage=0;equipmentSearchFocus=false;},false,.38f);
   var entries=w.EquipmentEntries(equipmentCategory,equipmentQuery).ToArray();int pages=Math.Max(1,(entries.Length+9)/10);equipmentPage=Math.Clamp(equipmentPage,0,pages-1);
   if(entries.All(i=>i.Id!=equipmentSelection))equipmentSelection=entries.FirstOrDefault()?.Id??"";
   int rowY=searchY+39;
   foreach(var item in entries.Skip(equipmentPage*10).Take(10))
   {
    string label=item.Name;int owned=actor.Inventory.Count(item.Id)+actor.Equipment.All().Where(s=>s.ItemId==item.Id).Sum(s=>s.Quantity);if(owned>0)label+=$" [{owned} OWNED]";
    var chosen=item;g.Button(new(x,rowY,listWidth,28),label,()=>{equipmentSelection=chosen.Id;equipmentRawPage=0;equipmentSearchFocus=false;},equipmentSelection==item.Id,.4f);rowY+=32;
   }
   if(entries.Length==0)g.Text("No matching equipment.",x,rowY,Muted,.45f);
   int footer=bounds.Bottom-77;g.Button(new(x,footer,90,27),"PREVIOUS",()=>equipmentPage=Math.Max(0,equipmentPage-1),false,.36f);g.Text($"PAGE {equipmentPage+1}/{pages} | {entries.Length} MATCHES",x+104,footer+8,Gold,.38f);g.Button(new(x+listWidth-90,footer,90,27),"NEXT",()=>equipmentPage=Math.Min(pages-1,equipmentPage+1),false,.36f);
   var selected=entries.FirstOrDefault(i=>i.Id==equipmentSelection);if(selected!=null)
   {
    var p=selected.Equipment;int yy=searchY;g.Text(selected.Name.ToUpperInvariant(),right,yy,Gold,Math.Min(.48f,(bounds.Right-right-20)/Math.Max(1,g.Measure(selected.Name.ToUpperInvariant(),1f))),true);yy+=28;
    g.Text($"{selected.Id} | MODULE P.{selected.Source.PageStart}",right,yy,Muted,.35f);yy+=23;
    g.Text($"{p?.Status.ToUpperInvariant()??"CAMPAIGN"} | {actor.Name} | {actor.CarriedWeight:0.#}/{actor.CarryingCapacity:0.#} LB",right,yy,p?.Status=="standard"?Teal:Gold,.39f);yy+=27;
    foreach(var line in g.Wrap(w.EquipmentSummary(selected),bounds.Right-right-25,.41f).Take(4)){g.Text(line,right,yy,Muted,.41f);yy+=19;}yy+=9;
    var cells=p?.SourceCells.ToArray()??[];int rawPages=Math.Max(1,(cells.Length+4)/5);equipmentRawPage=Math.Clamp(equipmentRawPage,0,rawPages-1);
    g.Button(new(right,yy,bounds.Right-right-20,25),$"SOURCE CELLS {equipmentRawPage+1}/{rawPages}",()=>equipmentRawPage=(equipmentRawPage+1)%rawPages,false,.36f);yy+=32;
    foreach(var pair in cells.Skip(equipmentRawPage*5).Take(5)){foreach(var line in g.Wrap(pair.Key.Replace('_',' ')+": "+pair.Value,bounds.Right-right-25,.38f).Take(2)){g.Text(line,right,yy,Muted,.38f);yy+=17;}}
    yy+=12;foreach(var line in g.Wrap(p?.Note??selected.Description,bounds.Right-right-25,.37f).Take(5)){g.Text(line,right,yy,Muted,.37f);yy+=17;}
    var ownedStack=actor.Inventory.Items.Concat(actor.Equipment.All()).FirstOrDefault(s=>s.ItemId==selected.Id);
    if(ownedStack!=null)
    {
     bool equip=actor.Inventory.Items.Contains(ownedStack)&&ownedStack.Definition.EquipSlot!=null;string reason=equip?w.CanEquipInSlot(actor,ownedStack,ownedStack.Definition.EquipSlot!.Value,out var why)?"":why:w.EquipmentUseReason(actor,ownedStack.InstanceId,equipmentTarget,equipmentCell);
     string action=equip?"EQUIP OWNED ITEM":"USE OWNED ITEM";g.CommandButton(new(right,footer,bounds.Right-right-20,28),action,()=>{bool used=equip?w.Equip(actor,actor.Inventory.Items.IndexOf(ownedStack)):w.UseEquipment(actor,ownedStack.InstanceId,equipmentTarget,equipmentCell);if(used){g.Notify(action+" complete.");Close();}else g.Notify(reason);},reason=="",reason==""?"Uses this real item. Charges, ammo and provenance persist.":reason,false,.4f);
    }
    else g.Text("Reference only: this survivor does not own this item.",right,footer+7,Muted,.39f);
   }
   g.Text("Browsing grants no equipment. Right-click a target/location, then Equipment, to use an owned item there. Page Up/Down changes pages.",x,bounds.Bottom-32,Muted,.39f);
  }
 }
 void EquipmentTextInput(object? sender,TextInputEventArgs e)=>ui?.Rest.EquipmentText(e.Character);
 void StageEquipmentCapture()
 {
  title=false;if(scene=="equipment-title"){title=true;return;}
  if(scene!="equipment-playthrough")
  {foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;world.SetDialogueFlag("flag.mall.shelter");int i=0;foreach(var a in world.Party)a.Pos=new(12+i++,27);world.Reveal();}
  camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();ui.Rest.ShowEquipment();if(scene.StartsWith("equipment-",StringComparison.Ordinal)&&scene is not ("equipment-playthrough" or "equipment-title")){string cat=scene[10..];ui.Rest.PreviewEquipment(cat=="scripted"?"all":cat=="owned"?"ranged":cat,cat=="scripted"?"Dimensional Eraser":"");}
 }
}
