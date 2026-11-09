using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 sealed partial class RestPanel
 {
  bool workshopView;
  string workshopTab="craft",workshopActor="",workshopSelection="",barterOffer="",barterWant="";
  int workshopPage,barterOfferPage,barterWantPage,barterGive=1,barterTake=1;
  public void ShowWorkshop(Actor? actor=null,string tab="craft")
  {if(!g.world.WorkshopEnabled||g.world.ReactionInterruptActive||g.ui.Dialogue.IsOpen)return;PrepareOpen();open=true;workshopView=true;equipmentView=classView=tensionView=worksView=shelterView=false;workshopActor=(actor??g.PrimaryPartyActor).Id;workshopTab=tab;}
  public void PreviewWorkshopItem(string id)=>workshopSelection=id;
  public void PreviewWorkshopBarter(string give,string take,int quantity=1){barterOffer=give;barterWant=take;barterGive=quantity;barterTake=1;}
  string ConditionText(ItemStack s)=>WorkshopRules.Durable(s.Definition)?$"{100-s.Wear}/100{(s.Broken?" BROKEN":"")}":s.Definition.MaxCharges>0?$"{s.Charges}/{s.Definition.MaxCharges} charges":"";
  void WorkshopAction(Rectangle r,string text,string reason,Action action)=>g.CommandButton(r,text,action,reason=="",reason==""?"Commits the displayed physical costs and campaign time.":reason,false,.43f);
  void DrawWorkshop()
  {
   var w=g.world;var d=w.Campaign!.Workshop!;var a=w.Party.FirstOrDefault(a=>a.Id==workshopActor)??w.PartyLeader;int x=bounds.X+20,y=bounds.Y+18,width=bounds.Width-40;
   g.Panel(bounds);g.buttons.Add((bounds,()=>{}));g.Text("REPAIRS, CRAFTING & BARTER",x,y,Gold,.54f,true);
   g.Button(new(bounds.Right-280,y,175,28),"NEXT SURVIVOR",()=>{var crew=w.Party.ToArray();workshopActor=crew[(Array.IndexOf(crew,a)+1)%crew.Length].Id;workshopSelection=barterOffer="";},false,.4f);g.Button(new(bounds.Right-90,y,65,28),"CLOSE",()=>Close(),false,.4f);y+=43;
   foreach(var tab in new[]{"craft","repair","salvage","barter"}){string t=tab;g.Button(new(x,y,145,28),tab.ToUpperInvariant(),()=>{workshopTab=t;workshopSelection="";workshopPage=0;},workshopTab==tab,.43f);x+=155;}x=bounds.X+20;y+=42;
   g.Text($"{a.Name} | {a.CarriedWeight:0.#}/{a.CarryingCapacity:0.#} LB | DAY {w.CampaignMinutes/1440+1} | TRADE REWARD {w.BarterRewardUsed}/{d.TradeXpBudget}",x,y,Teal,.43f);y+=28;
   string access=w.WorkshopAccessReason(a,workshopTab=="barter");g.Text(access!=""?access:workshopTab=="barter"?d.TraderName+": finite stock; no currency and no change.":"Costs use this backpack and the refuge stockpile. Toolkit retained; outputs go to the stockpile.",x,y,access!=""?Gold:Muted,.41f);y+=34;
   if(workshopTab=="barter"){DrawBarter(a,x,y,width);return;}
   int left=width/2-15,right=x+left+25,footer=bounds.Bottom-90;
   if(workshopTab=="craft")
   {
    var chosen=d.Recipes.FirstOrDefault(r=>r.Id==workshopSelection)??d.Recipes[0];workshopSelection=chosen.Id;
    foreach(var recipe in d.Recipes){var r=recipe;g.Button(new(x,y,left,29),r.Name,()=>workshopSelection=r.Id,chosen==r,.43f);y+=34;}
    int yy=bounds.Y+190;g.Text(chosen.Name.ToUpperInvariant(),right,yy,Gold,.46f,true);yy+=34;
    foreach(var c in chosen.Ingredients)foreach(var line in g.Wrap($"{c.Quantity} {w.ResolveItemDefinition(c.ItemId).Name} | available {w.WorkshopMaterialCount(a,c.ItemId)}",width-left-40,.43f)){g.Text(line,right,yy,Muted,.43f);yy+=22;}
    yy+=16;g.Text($"OUTPUT: {chosen.OutputQuantity} {w.ResolveItemDefinition(chosen.OutputItemId).Name}",right,yy,Teal,.43f);yy+=29;g.Text($"TIME: {w.CraftMinutes(a,chosen)} MINUTES | TOOLKIT REUSABLE",right,yy,Muted,.41f);yy+=31;
    g.Paragraph("Recipes are authored adaptations. Crafted items carry no new scavenging XP. Browsing grants no items.",right,yy,width-left-40,Muted,.42f,21);
    WorkshopAction(new(right,footer,width-left-25,32),"CRAFT INTO STOCKPILE",w.CraftReason(a,chosen.Id),()=>{if(w.Craft(a,chosen.Id))g.Notify("Crafting complete. Output stored at refuge.");});
   }
   else
   {
    var entries=(workshopTab=="repair"?w.WorkshopOwned(a):a.Inventory.Items).Where(s=>WorkshopRules.Durable(s.Definition)).ToArray();int pages=Math.Max(1,(entries.Length+6)/7);workshopPage=Math.Clamp(workshopPage,0,pages-1);var chosen=entries.FirstOrDefault(s=>s.InstanceId==workshopSelection)??entries.FirstOrDefault();workshopSelection=chosen?.InstanceId??"";
    foreach(var item in entries.Skip(workshopPage*7).Take(7)){var s=item;g.Button(new(x,y,left,30),s.Definition.Name+" | "+ConditionText(s),()=>workshopSelection=s.InstanceId,s==chosen,.42f);y+=36;}
    g.Button(new(x,footer,90,27),"PREVIOUS",()=>workshopPage=Math.Max(0,workshopPage-1),false,.36f);g.Text($"{workshopPage+1}/{pages}",x+110,footer+8,Gold,.4f);g.Button(new(x+left-90,footer,90,27),"NEXT",()=>workshopPage=Math.Min(pages-1,workshopPage+1),false,.36f);
    if(chosen!=null){int yy=bounds.Y+190;g.Text(chosen.Definition.Name.ToUpperInvariant(),right,yy,Gold,.46f,true);yy+=34;g.Text("CONDITION: "+ConditionText(chosen),right,yy,Teal,.46f);yy+=32;
     if(workshopTab=="repair")
     {g.Paragraph("Standard repair: 2 scrap, 30 minutes, up to 25 condition. Smart Quick Repairs: 1 scrap, 10 minutes, 1d6 condition. Item, loaded rounds and existing upgrades stay intact.",right,yy,width-left-40,Muted,.44f,23);WorkshopAction(new(right,footer-42,width-left-25,32),"STANDARD REPAIR",w.RepairReason(a,chosen.InstanceId),()=>w.RepairEquipment(a,chosen.InstanceId));WorkshopAction(new(right,footer,width-left-25,32),"SMART QUICK REPAIR",w.RepairReason(a,chosen.InstanceId,true),()=>w.RepairEquipment(a,chosen.InstanceId,true));}
     else
     {g.Paragraph($"DESTROYS this physical item permanently. Recover one {w.ResolveItemDefinition(w.SalvageOutput(chosen)).Name} into the stockpile. No XP; unload first. Time: {(a.ClassIs("smart")?5:10)} minutes.",right,yy,width-left-40,Gold,.44f,23);WorkshopAction(new(right,footer,width-left-25,32),"DISMANTLE SELECTED ITEM",w.SalvageReason(a,chosen.InstanceId),()=>{if(w.SalvageEquipment(a,chosen.InstanceId))workshopSelection="";});}
    }else g.Paragraph("No eligible owned equipment. Withdraw gear from the stockpile first.",right,bounds.Y+200,width-left-40,Muted,.43f,21);
   }
   g.Paragraph(w.WorkshopHistory.LastOrDefault()?.Text??"Equipment wears during use. Broken weapons cannot attack; broken armour loses its AC bonus.",bounds.X+20,bounds.Bottom-43,width,Muted,.36f,17);
  }
  void DrawBarter(Actor a,int x,int y,int width)
  {
   var w=g.world;int half=(width-25)/2,right=x+half+25;
   var offers=a.Inventory.Items.Where(s=>w.BarterValue(s,1)>0).ToArray();var wants=w.TraderStock.Items.Where(s=>w.BarterValue(s,1)>0).ToArray();var offer=offers.FirstOrDefault(s=>s.InstanceId==barterOffer)??offers.FirstOrDefault();var want=wants.FirstOrDefault(s=>s.InstanceId==barterWant)??wants.FirstOrDefault();barterOffer=offer?.InstanceId??"";barterWant=want?.InstanceId??"";
   int givePages=Math.Max(1,(offers.Length+4)/5),takePages=Math.Max(1,(wants.Length+4)/5);barterOfferPage=Math.Clamp(barterOfferPage,0,givePages-1);barterWantPage=Math.Clamp(barterWantPage,0,takePages-1);
   g.Text("YOU OFFER (BACKPACK ONLY)",x,y,Gold,.44f,true);g.Text("YOU REQUEST (TRADER STOCK)",right,y,Gold,.44f,true);y+=28;int ry=y;
   foreach(var s in offers.Skip(barterOfferPage*5).Take(5)){var item=s;g.Button(new(x,y,half,29),s.Definition.Name+$" x{s.Quantity} | {ConditionText(s)}",()=>{barterOffer=item.InstanceId;barterGive=1;},offer==s,.4f);y+=34;}
   foreach(var s in wants.Skip(barterWantPage*5).Take(5)){var item=s;g.Button(new(right,ry,half,29),s.Definition.Name+$" x{s.Quantity} | {ConditionText(s)}",()=>{barterWant=item.InstanceId;barterTake=1;},want==s,.4f);ry+=34;}
   int qy=bounds.Bottom-171;g.Button(new(x,qy,80,25),"PAGE <",()=>barterOfferPage=Math.Max(0,barterOfferPage-1),false,.35f);g.Button(new(x+90,qy,80,25),"PAGE >",()=>barterOfferPage=Math.Min(givePages-1,barterOfferPage+1),false,.35f);g.Text($"{barterOfferPage+1}/{givePages}",x+185,qy+6,Muted,.4f);
   g.Button(new(right,qy,80,25),"PAGE <",()=>barterWantPage=Math.Max(0,barterWantPage-1),false,.35f);g.Button(new(right+90,qy,80,25),"PAGE >",()=>barterWantPage=Math.Min(takePages-1,barterWantPage+1),false,.35f);g.Text($"{barterWantPage+1}/{takePages}",right+185,qy+6,Muted,.4f);qy+=32;
   barterGive=Math.Clamp(barterGive,1,offer?.Quantity??1);barterTake=Math.Clamp(barterTake,1,want?.Quantity??1);
   g.Button(new(x,qy,40,27),"-",()=>barterGive=Math.Max(1,barterGive-1),false,.43f);g.Text($"OFFER {barterGive}",x+55,qy+8,Teal,.43f);g.Button(new(x+190,qy,40,27),"+",()=>barterGive++,false,.43f);
   g.Button(new(right,qy,40,27),"-",()=>barterTake=Math.Max(1,barterTake-1),false,.43f);g.Text($"REQUEST {barterTake}",right+55,qy+8,Teal,.43f);g.Button(new(right+190,qy,40,27),"+",()=>barterTake++,false,.43f);
   var quote=w.PreviewBarter(a,barterOffer,barterGive,barterWant,barterTake);qy+=33;g.Text($"GIVE {barterGive} {offer?.Definition.Name??"—"}",x,qy,Teal,.4f);g.Text($"RECEIVE {barterTake} {want?.Definition.Name??"—"}",right,qy,Teal,.4f);qy+=22;
   g.Text($"OFFER {quote.OfferSv} SV | ASK {quote.AskSv} SV | BONUS {quote.BonusSv} SV | 5 MINUTES | NO CHANGE",x,qy,Gold,.4f);qy+=22;
   WorkshopAction(new(right,bounds.Bottom-44,half,29),"CONFIRM EXACT EXCHANGE",quote.Reason,()=>{if(w.Barter(a,barterOffer,barterGive,barterWant,barterTake))g.Notify("Physical goods exchanged; persistent stock updated.");});g.Paragraph(quote.Allowed?"This transfers the displayed quantities. Stock never restocks; the reward cap persists.":quote.Reason,x,qy,half,Muted,.39f,18);
  }
 }
 void StageWorkshopCapture()
 {
  title=false;if(scene=="workshop-title"){title=true;return;}
  if(scene!="workshop-playthrough"){foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;world.SetDialogueFlag("flag.mall.shelter");int i=0;foreach(var a in world.Party)a.Pos=new(12+i++,28);world.Reveal();world.TakeAllContainer(world.PartyLeader,world.Features.Single(f=>f.Id==world.Campaign!.Workshop!.BenchFeatureId));}
  camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();ui.Rest.ShowWorkshop(tab:scene is "workshop-barter" or "workshop-playthrough"?"barter":scene=="workshop-repair"?"repair":scene=="workshop-salvage"?"salvage":"craft");
  if(scene is "workshop-repair" or "workshop-salvage")ui.Rest.PreviewWorkshopItem(world.PartyLeader.Inventory.Items.FirstOrDefault(s=>s.Wear>0)?.InstanceId??"");
  if(scene is "workshop-barter" or "workshop-playthrough"){var give=world.PartyLeader.Inventory.Items.FirstOrDefault(s=>s.ItemId==(scene=="workshop-playthrough"?"item.ammo.catalogue-bolt":world.Campaign!.Workshop!.WoodItemId));var take=world.TraderStock.Items.FirstOrDefault(s=>s.ItemId=="item.commodity.037");if(give!=null&&take!=null)ui.Rest.PreviewWorkshopBarter(give.InstanceId,take.InstanceId,scene=="workshop-playthrough"?4:1);}
 }
}
