using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 /// <summary>IIP43 lower-HUD rest interface. Core owns legality, time, recovery,
 /// hit-die rolls, supplies, and camp events; this panel only builds a Rest plan.</summary>
 sealed partial class RestPanel
 {
  readonly CovenantGame g;
  bool open;
  RestKind kind=RestKind.ShortRest;
  readonly Dictionary<string,int> plannedDice=new(StringComparer.Ordinal);
  int selectedPool;
  Rectangle bounds,typeFrame,recoveryFrame,siteFrame;
  public RestPanel(CovenantGame game)=>g=game;
  public bool IsOpen=>open;
  public Rectangle Bounds=>bounds;
  Actor Actor=>g.PrimaryPartyActor;
  RestPreview Preview=>g.world.PreviewRest(kind);

  static string PlanKey(string actorId,string sourceClass)=>actorId+"|"+sourceClass.ToLowerInvariant();
  int Planned(Actor actor,HitDiePool pool)=>plannedDice.TryGetValue(PlanKey(actor.Id,pool.SourceClass),out var value)?Math.Clamp(value,0,pool.Current):0;
  void SetPlanned(Actor actor,HitDiePool pool,int value)=>plannedDice[PlanKey(actor.Id,pool.SourceClass)]=Math.Clamp(value,0,pool.Current);

  public void Open()
  {
   if(g.ui.Dialogue.IsOpen){g.Notify("Finish the conversation before resting.");return;}
   if(g.world.Mode!=Mode.Explore){g.Notify("Rest is only available while exploring outside combat.");return;}
   PrepareOpen();open=true;equipmentView=false;classView=false;tensionView=false;worksView=false;shelterView=false;residentPage=0;residentSelection="";kind=RestKind.ShortRest;selectedPool=0;plannedDice.Clear();
  }

  void PrepareOpen()
  {
   finaleView=false;lowerView=false;socialView=false;contractsView=false;workshopView=false;
   g.CancelInventoryDrag();g.CancelFormationRightPointer();g.ClearArmedSpell();g.ClearArmedFeatureAbility();g.pendingSpellPlan=null;g.quickSpellAssignmentSlot=-1;
   g.contextOpen=false;g.ui.Information.Close();g.ui.Modals.Clear();g.ui.Loot.Close();g.ui.Store.Close();g.ui.Services.Close();g.ui.Screens.ReturnToGame();g.leftWorldPress=g.boxDragging=false;g.world.StopAll();
  }

  public bool Close(){if(!open)return false;open=false;plannedDice.Clear();selectedPool=0;return true;}
  public bool HandleRightPress(Point p)=>IsOpen&&bounds.Contains(p);

  public void HandleKeyboard()
  {
   // Historical IIP43 structural markers after party-slot rebinding: Keys.Tab Keys.D1 Keys.D6 Keys.Enter Keys.Escape
   if(!IsOpen)return;
   if(g.Pressed(Keys.Escape)){Close();return;}
   if(equipmentView){EquipmentKeys();return;}
   if(finaleView||shelterView||worksView||tensionView||classView||equipmentView||workshopView||contractsView||socialView||lowerView)return;
   if(g.Pressed(Keys.Tab)){kind=kind==RestKind.ShortRest?RestKind.LongRest:RestKind.ShortRest;selectedPool=0;return;}
   if(g.BindingPressed(InputAction.SelectParty1))g.Select(0);if(g.BindingPressed(InputAction.SelectParty2))g.Select(1);if(g.BindingPressed(InputAction.SelectParty3))g.Select(2);if(g.BindingPressed(InputAction.SelectParty4))g.Select(3);if(g.BindingPressed(InputAction.SelectParty5))g.Select(4);if(g.BindingPressed(InputAction.SelectParty6))g.Select(5);
   if(kind==RestKind.ShortRest)
   {
    var pools=Actor.Character.HitDice;
    if(pools.Count>0)
    {
     selectedPool=Math.Clamp(selectedPool,0,pools.Count-1);
     if(g.Pressed(Keys.Up))selectedPool=Math.Max(0,selectedPool-1);
     if(g.Pressed(Keys.Down))selectedPool=Math.Min(pools.Count-1,selectedPool+1);
     var pool=pools[selectedPool];if(g.Pressed(Keys.Left)||g.Pressed(Keys.OemMinus)||g.Pressed(Keys.Subtract))SetPlanned(Actor,pool,Planned(Actor,pool)-1);
     if(g.Pressed(Keys.Right)||g.Pressed(Keys.OemPlus)||g.Pressed(Keys.Add))SetPlanned(Actor,pool,Planned(Actor,pool)+1);
    }
   }
   if(g.Pressed(Keys.Enter))Perform();
  }

  IEnumerable<HitDieSpendRequest> Requests()
  {
   foreach(var actor in g.world.Party)
    foreach(var pool in actor.Character.HitDice)
    {
     int count=Planned(actor,pool);if(count>0)yield return new(actor.Id,pool.SourceClass,count);
    }
  }

  void Perform()
  {
   var preview=Preview;if(!preview.Available){g.Notify(preview.Reason);return;}
   var result=g.world.PerformRest(kind,Requests().ToArray());
   if(result.Completed){g.Notify(kind==RestKind.ShortRest?"Short rest complete.":"Long rest complete.");Close();return;}
   if(result.Interrupted){g.Notify("Rest interrupted — prepare for trouble.");Close();return;}
   g.Notify(result.Summary);
  }

  void Reflow()
  {
   var center=g.ui.Layout.CenterBounds;int h=Math.Clamp((int)Math.Round(center.Height*.61),450,550);if(finaleView||shelterView||worksView||tensionView||classView||equipmentView||workshopView||contractsView||socialView||lowerView)h=550;bounds=new(center.X,center.Bottom-h,center.Width,h);
   int pad=15,gap=10,top=bounds.Y+62,bottom=18,available=bounds.Width-pad*2-gap*2;int left=(int)(available*.25f),right=(int)(available*.29f),mid=available-left-right;
   typeFrame=new(bounds.X+pad,top,left,bounds.Height-(top-bounds.Y)-bottom);recoveryFrame=new(typeFrame.Right+gap,top,mid,bounds.Height-(top-bounds.Y)-bottom);siteFrame=new(recoveryFrame.Right+gap,top,right,bounds.Height-(top-bounds.Y)-bottom);
  }

  public void Draw()
  {
   if(!IsOpen)return;Reflow();if(finaleView&&g.world.FinaleEnabled){DrawFinale();return;}if(lowerView&&g.world.LowerMallEnabled){DrawLowerMall();return;}if(socialView&&g.world.SocialEnabled){DrawSocial();return;}if(contractsView&&g.world.StoryEnabled){DrawContracts();return;}if(workshopView&&g.world.WorkshopEnabled){DrawWorkshop();return;}if(equipmentView&&g.world.EquipmentEnabled){DrawEquipment();return;}if(classView&&g.world.SurvivorClassesEnabled){DrawClasses();return;}if(tensionView&&g.world.TensionEnabled){DrawTension();return;}if(worksView&&g.world.InfrastructureEnabled){DrawWorks();return;}if(shelterView&&g.world.ShelterEnabled){DrawShelter();return;}if(g.world.DailySurvivalEnabled){DrawSurvival();return;}var preview=Preview;
   g.Panel(bounds);g.buttons.Add((bounds,()=>{}));g.Fill(new(bounds.X+9,bounds.Y+9,bounds.Width-18,45),new Color(11,14,15)*.97f);g.Border(new(bounds.X+9,bounds.Y+9,bounds.Width-18,45),Gold*.44f);
   g.Text("REST",bounds.X+22,bounds.Y+17,Bright,.62f,true);g.Text($"{g.world.CurrentAreaName} · {g.world.CampaignTimeText} · Supplies {g.world.Rations}",bounds.X+132,bounds.Y+21,Gold,.42f);
   var close=new Rectangle(bounds.Right-92,bounds.Y+15,68,30);g.Button(close,"CLOSE",()=>Close(),false,.46f);
   DrawTypeChoices(typeFrame);DrawRecovery(recoveryFrame);DrawSite(siteFrame,preview);
  }

  void DrawSurvival()
  {
   g.Panel(bounds);g.buttons.Add((bounds,()=>{}));int x=bounds.X+20,y=bounds.Y+15;
   g.Text("SURVIVAL & FIRST AID",x,y,Bright,.57f,true);
   g.Text(g.world.CampaignTimeText+" · Daily supplies are physical items",x+310,y+5,Gold,.44f);
   if(g.world.InfrastructureEnabled)g.Button(new(bounds.Right-440,y,160,28),"WORKS / UTILITIES",()=>{worksView=true;shelterView=false;},false,.43f);
   if(g.world.ShelterEnabled)g.Button(new(bounds.Right-270,y,165,28),"SHELTER ROSTER",()=>{shelterView=true;},false,.44f);
   g.Button(new(bounds.Right-90,y,65,28),"CLOSE",()=>Close(),false,.4f);
   int leftW=(bounds.Width-65)/2,rightX=x+leftW+25,rightW=leftW;
   y+=48;g.Text("PARTY NEEDS - SELECT A SURVIVOR",x,y,Gold,.38f,true);y+=27;
   int index=0;
   foreach(var member in g.world.Party)
   {
    var a=member;int slot=index++;var row=new Rectangle(x,y,leftW,48);
    g.Box(row,a==Actor?new Color(49,47,32):new Color(13,19,19),a==Actor?Gold:Gold*.25f);
    g.Text($"{a.Name}   HP {a.Hp}/{a.MaxHp}"+(a.Dead?"  DEAD":""),x+9,y+5,Bright,.48f,true);
    g.Text(g.world.SurvivalSummary(a),x+9,y+27,a.Needs.Exhaustion>0?Red:Muted,.49f);
    g.buttons.Add((row,()=>g.Select(slot)));y+=53;
   }
   g.Text("At 48h without water: +1 fatigue each day.",x,bounds.Bottom-100,Muted,.44f);
   g.Text("At 72h without food: +1 fatigue each day.",x,bounds.Bottom-82,Muted,.44f);
   g.Text("Fatigue: -1 attack, -5 ft speed per level; death at 6.",x,bounds.Bottom-64,Muted,.44f);
   g.Text("Clock stops in menus. Combat: 6 sec per full round.",x,bounds.Bottom-46,Muted,.44f);
   var patient=Actor;var carer=g.world.Party.Where(a=>a.Alive).OrderBy(a=>a.Position.Distance(patient.Position)).FirstOrDefault()??patient;
   int yy=bounds.Y+64;g.Text(patient.Name.ToUpperInvariant()+" - DAILY CARE",rightX,yy,Gold,.39f,true);yy+=29;
   foreach(var use in new[]{"food","water","bandage","first-aid"})
   {
    string u=use;var helper=u is "food" or "water"?patient:carer;string reason=g.world.SurvivalSupplyReason(helper,patient,u);
    string label=u switch{"food"=>"EAT", "water"=>"DRINK", "bandage"=>"BANDAGE: 1d4 + WIS",_=>"FIRST AID: 2d6 + WIS"};
    g.CommandButton(new(rightX,yy,rightW,30),label+$"   [{g.world.AvailableSurvivalSupply(helper,u)} available]",()=>{g.world.UseSurvivalSupply(helper,patient,u);},reason=="",reason==""?$"Consumes one physical {u} unit. Treatment: once per patient between recovered long rests.":reason,false,.48f);yy+=36;
   }
   g.CommandButton(new(rightX,yy,rightW,30),"PROVISION PARTY",()=>{int n=g.world.SupplyParty();g.Notify($"Used {n} daily supplies.");},g.world.Mode==Mode.Explore,"Meet unmet daily needs from each backpack and a nearby refuge. Existing daily intake is never charged twice.",false,.48f);yy+=39;
   g.Text("REST AT THE ESTABLISHED OFFICE REFUGE",rightX,yy,Gold,.32f,true);yy+=23;
   if(patient.Character.HitDice.FirstOrDefault() is {} pool)
   {
    int count=Planned(patient,pool);g.Text($"Short rest: {count}/{pool.Current} Hit Dice planned",rightX,yy+6,Muted,.49f);
    g.Button(new(rightX+rightW-64,yy,28,27),"-",()=>SetPlanned(patient,pool,count-1),false,.38f);
    g.Button(new(rightX+rightW-30,yy,28,27),"+",()=>SetPlanned(patient,pool,count+1),false,.38f);yy+=36;
   }
   foreach(var rest in new[]{RestKind.ShortRest,RestKind.LongRest})
   {
    var k=rest;var preview=g.world.PreviewRest(k);
    g.CommandButton(new(rightX,yy,rightW,31),k==RestKind.ShortRest?"SHORT REST - 1 HOUR":"LONG REST - 8 HOURS",()=>{kind=k;Perform();},preview.Available,preview.Available?preview.OutcomeText:preview.Reason,false,.48f);yy+=37;
   }
   g.Text("Long rest auto-provisions; missing food/water blocks healing.",rightX,yy+2,Muted,.42f);
   g.Text("One long rest per 24h. First aid never cures infection.",rightX,yy+19,Muted,.42f);
  }

  void DrawTypeChoices(Rectangle frame)
  {
   g.Box(frame,new Color(16,19,19)*.96f,Gold*.40f);g.Text("REST TYPE",frame.X+12,frame.Y+10,Gold,.41f,true);
   DrawRestType(new(frame.X+9,frame.Y+45,frame.Width-18,118),RestKind.ShortRest,"SHORT REST","1 hour · 0 supplies","Spend chosen Hit Dice; restore short-rest resources and pact slots.");
   DrawRestType(new(frame.X+9,frame.Y+171,frame.Width-18,136),RestKind.LongRest,"LONG REST","8 hours · 1 supply","Full HP; short/long-rest resources; spell slots; up to half spent Hit Dice.");
   int yy=frame.Bottom-70;g.Text("KEYBOARD",frame.X+11,yy,Gold,.30f,true);g.Text($"Tab type · party {g.PartySlotBindingSummary()}",frame.X+11,yy+17,Muted,.28f);g.Text("↑/↓ die pool · ←/→ amount",frame.X+11,yy+33,Muted,.28f);g.Text("Enter rest · Esc close",frame.X+11,yy+49,Muted,.28f);
  }

  void DrawRestType(Rectangle r,RestKind option,string title,string cost,string text)
  {
   bool selected=kind==option,hover=r.Contains(g.cursor);g.Box(r,selected?new Color(69,57,36):new Color(10,13,14),selected?Bright:hover?Bright:Gold*.26f);
   g.Text(title,r.X+10,r.Y+9,Bright,.42f,true);g.Text(cost,r.X+10,r.Y+33,Gold,.32f);int y=r.Y+55;foreach(var line in g.Wrap(text,r.Width-20,.31f).Take(4)){g.Text(line,r.X+10,y,Muted,.31f);y+=16;}
   g.buttons.Add((r,()=>{kind=option;selectedPool=0;}));if(hover)g.tooltip=option==RestKind.ShortRest?"Choose a 5e-style short rest. Hit Dice are selected per companion before confirmation.":"Choose a long rest. Camping consumes one shared supply unless lodging is purchased from an inn.";
  }

  void DrawRecovery(Rectangle frame)
  {
   g.Box(frame,new Color(13,16,17)*.98f,Gold*.50f);var a=Actor;int x=frame.X+14,y=frame.Y+11,w=frame.Width-28;
   g.Text("RECOVERY PLAN",x,y,Gold,.40f,true);y+=26;g.Text(a.Name.ToUpperInvariant(),x,y,Bright,.45f,true);y+=23;g.Text($"{a.Class} · Level {a.TotalLevel} · HP {a.Hp}/{a.MaxHp}",x,y,Muted,.32f);y+=28;
   if(kind==RestKind.ShortRest)DrawShortRestPlan(frame,x,ref y,w,a);else DrawLongRestPlan(frame,x,ref y,w,a);
   g.Text($"Use the right portrait rail or party keys {g.PartySlotBindingSummary()}",x,frame.Bottom-40,Muted,.28f);g.Text("to inspect and plan each companion.",x,frame.Bottom-24,Muted,.28f);
  }

  void DrawShortRestPlan(Rectangle frame,int x,ref int y,int w,Actor a)
  {
   g.Text("HIT DICE TO SPEND",x,y,Gold,.31f,true);y+=20;
   var pools=a.Character.HitDice;selectedPool=Math.Clamp(selectedPool,0,Math.Max(0,pools.Count-1));
   if(pools.Count==0){g.Text("No Hit Dice available.",x,y,Muted,.32f);y+=24;}
   for(int i=0;i<pools.Count&&i<5;i++)
   {
    var pool=pools[i];var row=new Rectangle(x,y,w,49);bool selected=i==selectedPool;g.Box(row,selected?new Color(55,50,37):new Color(10,13,14),selected?Gold*.75f:Gold*.20f);
    g.Text($"{pool.SourceClass} d{pool.DieSize}",row.X+8,row.Y+6,Bright,.33f,true);g.Text($"Available {pool.Current}/{pool.Maximum}",row.X+8,row.Y+26,Muted,.28f);
    int amount=Planned(a,pool);Rectangle minus=new Rectangle(row.Right-96,row.Y+9,25,28),plus=new Rectangle(row.Right-29,row.Y+9,25,28);g.CommandButton(minus,"-",()=>SetPlanned(a,pool,amount-1),amount>0,"Remove one planned Hit Die.",false,.45f);g.Center(amount.ToString(),new(row.Right-68,row.Y+9,36,28),Bright,.44f,true);g.CommandButton(plus,"+",()=>SetPlanned(a,pool,amount+1),amount<pool.Current,"Plan one more Hit Die.",false,.45f);string classId=pool.SourceClass;g.buttons.Add((new Rectangle(row.X,row.Y,row.Width-102,row.Height),()=>{selectedPool=Math.Max(0,a.Character.HitDice.FindIndex(h=>h.SourceClass==classId));}));
    y+=55;
   }
   y+=4;var shortResources=a.Character.Resources.Where(r=>r.Recharge==ResourceRecharge.ShortRest).ToArray();g.Text("SHORT-REST RESOURCES",x,y,Gold,.31f,true);y+=19;
   if(shortResources.Length==0)g.Text("None on this character.",x,y,Muted,.30f);else foreach(var resource in shortResources.Take(4)){g.Text($"{resource.Name}: {resource.Current}/{resource.Maximum} → {resource.Maximum}",x,y,Muted,.29f);y+=17;}
   if(a.Character.Spellcasting.Any(s=>s.PreparationMode==SpellPreparationMode.Pact)){g.Text("Pact spell slots restore.",x,y,Teal,.29f);y+=17;}
  }

  void DrawLongRestPlan(Rectangle frame,int x,ref int y,int w,Actor a)
  {
   string life=a.Dead?"DEAD — long rest will not resurrect":a.Unconscious?"UNCONSCIOUS — long rest restores to full HP":"Living — long rest restores to full HP";g.Text(life,x,y,a.Dead?Red:Teal,.31f,true);y+=27;
   g.Text("RESOURCES",x,y,Gold,.31f,true);y+=19;var resources=a.Character.Resources.Where(r=>r.Recharge is ResourceRecharge.ShortRest or ResourceRecharge.LongRest).ToArray();
   if(resources.Length==0)g.Text("No rest-recharging class resources.",x,y,Muted,.29f);else foreach(var resource in resources.Take(5)){g.Text($"{resource.Name}: {resource.Current}/{resource.Maximum} → {resource.Maximum}",x,y,Muted,.29f);y+=17;}y+=8;
   g.Text("SPELL SLOTS",x,y,Gold,.31f,true);y+=19;int slots=a.Character.Spellcasting.SelectMany(s=>s.Slots).Sum(s=>s.Current),max=a.Character.Spellcasting.SelectMany(s=>s.Slots).Sum(s=>s.Maximum);g.Text(max>0?$"{slots}/{max} total → {max}/{max}":"No spell-slot pools.",x,y,Muted,.30f);y+=26;
   g.Text("HIT DICE",x,y,Gold,.31f,true);y+=19;int current=a.Character.HitDice.Sum(h=>h.Current),total=a.Character.HitDice.Sum(h=>h.Maximum),recover=total<=0?0:Math.Min(total-current,Math.Max(1,total/2));g.Text(total>0?$"{current}/{total}; recover up to {recover}":"No Hit Dice pool.",x,y,Muted,.30f);
  }

  void DrawSite(Rectangle frame,RestPreview preview)
  {
   g.Box(frame,new Color(16,19,19)*.96f,Gold*.40f);int x=frame.X+13,y=frame.Y+11,w=frame.Width-26;g.Text("CAMP CONDITIONS",x,y,Gold,.40f,true);y+=28;
   g.Text(preview.SiteName.ToUpperInvariant(),x,y,Bright,.37f,true);y+=23;foreach(var line in g.Wrap(preview.SiteDescription,w,.30f).Take(5)){g.Text(line,x,y,Muted,.30f);y+=16;}y+=9;
   g.Text($"Duration: {(preview.Minutes>=60?$"{preview.Minutes/60} hr{(preview.Minutes==60?"":"s")}":$"{preview.Minutes} min")}",x,y,Bright,.31f);y+=19;g.Text($"Camping supplies: {g.world.Rations}  ·  Cost: {preview.RationCost}",x,y,Bright,.31f);y+=19;g.Text($"Interruption/event chance: {preview.EncounterChancePercent}%",x,y,preview.EncounterChancePercent>0?Gold:Teal,.31f);y+=22;
   string arrival=FutureTime(preview.Minutes);g.Text($"If completed: {arrival}",x,y,Muted,.30f);y+=27;
   g.Text("RECOVERY",x,y,Gold,.31f,true);y+=19;foreach(var line in g.Wrap(preview.OutcomeText,w,.29f).Take(5)){g.Text(line,x,y,Muted,.29f);y+=16;}y+=7;
   if(preview.Available)g.Text("REST CAN BEGIN",x,y,Teal,.32f,true);else foreach(var line in g.Wrap(preview.Reason,w,.31f).Take(4)){g.Text(line,x,y,Red,.31f);y+=17;}
   var confirm=new Rectangle(x,frame.Bottom-45,w,33);string label=kind==RestKind.ShortRest?"BEGIN SHORT REST":"MAKE CAMP — LONG REST";g.CommandButton(confirm,label,Perform,preview.Available,preview.Available?"Commit the displayed rest plan. Time, supplies, recovery, and events are resolved by Core.":preview.Reason,false,.34f);
  }

  string FutureTime(int minutes)
  {
   int total=Math.Max(0,g.world.CampaignMinutes+Math.Max(0,minutes)),day=1+total/1440,hour=(total%1440)/60,minute=total%60;return $"Day {day}, {hour:00}:{minute:00}";
  }
 }
}
