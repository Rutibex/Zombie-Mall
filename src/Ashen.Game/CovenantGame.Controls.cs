using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 readonly HashSet<string> selectedParty=[];
 FormationPreset groupFormation=FormationPreset.Wedge;
 string primaryPartyActorId="";
 bool resizePending;
 bool leftWorldPress,boxDragging;
 WorldPoint selectionAnchor,pressedPoint,contextPoint;
 Actor? pressedActor,pressedCorpse,contextActor,contextCorpse;Feature? contextFeature;GroundPile? pressedGround,contextGround;
 Point pressScreen,contextScreen;
 string lastActive="";
 readonly List<(string Label,System.Action Run,bool Enabled,string Hint)> contextCommands=[];
 Rectangle contextRect;
 bool ExtendSelection=>keys.IsKeyDown(Keys.LeftShift)||keys.IsKeyDown(Keys.RightShift)||keys.IsKeyDown(Keys.LeftControl)||keys.IsKeyDown(Keys.RightControl);
 bool IsSelected(Actor a)=>selectedParty.Contains(a.Id);
 IEnumerable<Actor> Selection=>world.Party.Where(a=>a.Alive&&IsSelected(a));
 Actor PrimaryPartyActor=>world.Party.FirstOrDefault(a=>a.Id==primaryPartyActorId)??world.Chosen;
 Actor? SelectionLeader=>world.FormationLeader(selectedParty);
 void SetPrimaryPartyActor(Actor a){primaryPartyActorId=a.Id;world.Selected=Math.Max(0,world.PartyIndex(a));}
 void SelectAll(){selectedParty.Clear();foreach(var a in world.Party.Where(a=>a.Alive))selectedParty.Add(a.Id);if(string.IsNullOrEmpty(primaryPartyActorId)||world.PartyIndex(primaryPartyActorId)<0)SetPrimaryPartyActor(world.Chosen);}
 void SelectCharacter(int index,bool extend=false)
 {
  var a=world.PartyAt(index);if(a==null)return;
  if(SpellTargetingActive&&a.Id!=armedSpellCasterId){ClearArmedSpell();pendingSpellPlan=null;actionBarPage=ActionBarPage.Primary;Notify("Spell targeting cancelled because the active character changed.");}
  if(FeatureAbilityTargetingActive&&a.Id!=armedFeatureCasterId){ClearArmedFeatureAbility();actionBarPage=ActionBarPage.Primary;Notify("Ability targeting cancelled because the active character changed.");}
  SetPrimaryPartyActor(a);
  if(!extend)selectedParty.Clear();
  // Fallen companions remain the primary inspection actor but never enter the executable selection set.
  if(extend&&selectedParty.Contains(a.Id))selectedParty.Remove(a.Id);else if(a.Alive)selectedParty.Add(a.Id);
 }
 void SyncCombatSelection()
 {
  if(world.Mode!=Mode.Combat){lastActive="";return;}
  if(world.Active is {Enemy:false} a&&lastActive!=a.Id){selectedParty.Clear();selectedParty.Add(a.Id);SetPrimaryPartyActor(a);}
  lastActive=world.Active?.Id??"";
 }
 void IssueMove(WorldPoint point)=>IssueMove(point,null);
 void IssueMove(WorldPoint point,WorldPoint? formationFacing)
 {
  if(paused){world.LogSystem("Resume time to issue a movement command.",true);return;}
  if(selectedParty.Count==0){world.LogSystem("Select a companion or drag a selection box first.",true);return;}
  var leader=SelectionLeader;if(leader==null){world.LogSystem("Select a living companion first.",true);return;}
  bool formationOrder=world.Mode==Mode.Explore&&Selection.Count()>1;
  var reason=formationOrder?world.FormationMovementReason(leader,point):world.MovementReason(leader,point);if(reason!=""){Notify(reason);return;}
  if(following&&world.AvailablePartyLeader is {} partyLeader&&!selectedParty.Contains(partyLeader.Id))following=false;
  if(formationOrder&&formationFacing is {} facing)world.MoveSelection(point,selectedParty,groupFormation,facing);
  else world.MoveSelection(point,selectedParty,groupFormation);
  previewAt=-1;
 }
 void IssueInteraction(Feature f)
 {
  if(paused)return;
  var a=SelectionLeader;
  if(a==null){world.LogSystem("Select a companion first.",true);return;}
  if(!world.RequestInteraction(a,f))Notify(world.Log.Last());
 }
 void StopSelection(){var ids=Selection.Select(a=>a.Id).ToArray();world.CancelFormationTravel(ids);foreach(var a in Selection)if(world.Mode==Mode.Explore||world.Active==a)world.Stop(a);}
 V EdgeDirection(Point p)
 {
  var bounds=MapRect;
  // The pointer cannot leave a normal window far enough to reach the old outer
  // scroll region. A narrow strip inside the unobstructed world restores panning.
  if(bounds.Contains(p))
  {
   if(ui.Initiative.Contains(p)||ui.Messages.Contains(p)||MiniRect.Contains(p))return V.Zero;
   const int edge=24;
   float x=p.X<bounds.Left+edge?-Math.Clamp((bounds.Left+edge-p.X)/(float)edge,0,1):p.X>=bounds.Right-edge?Math.Clamp((p.X-(bounds.Right-edge))/(float)edge,0,1):0;
   float y=p.Y<bounds.Top+edge?-Math.Clamp((bounds.Top+edge-p.Y)/(float)edge,0,1):p.Y>=bounds.Bottom-edge?Math.Clamp((p.Y-(bounds.Bottom-edge))/(float)edge,0,1):0;
   return new V(x,y);
  }
  if(ui.Layout.IsPermanentUi(p)||ui.Layout.IsOnCanvas(p))return V.Zero;
  float referenceDistance=Math.Max(1,Math.Min(bounds.Width,bounds.Height)*.5f);
  return new V(p.X-bounds.Center.X,p.Y-bounds.Center.Y)/referenceDistance;
 }
 void UpdateEdgeScroll(float dt)
 {
  if(!preferences.EdgeScroll||!IsActive||title||UiBlocksWorld||ui.Portraits.IsDragging||mouse.MiddleButton==ButtonState.Pressed)return;
  var direction=EdgeDirection(cursor);
  if(direction!=V.Zero)focusUntil=0;
  camera+=direction*(dt*preferences.ScrollSpeed/zoom);
  camera.X=Math.Clamp(camera.X,-3,World.Width+3);camera.Y=Math.Clamp(camera.Y,-3,World.Height+3);
 }
 Rectangle SelectionRectangle()
 {
  var start=Screen(selectionAnchor);
  return new((int)Math.Min(start.X,cursor.X),(int)Math.Min(start.Y,cursor.Y),(int)Math.Abs(start.X-cursor.X),(int)Math.Abs(start.Y-cursor.Y));
 }
 void ApplyBoxSelection(Rectangle rect,bool additive)
 {
  if(!additive)selectedParty.Clear();
  foreach(var a in world.Party.Where(a=>a.Alive))if(rect.Intersects(ActorBounds(a)))selectedParty.Add(a.Id);
  var first=Selection.FirstOrDefault();if(first!=null)SetPrimaryPartyActor(first);
 }
 void HandleLeftPointer(bool down,bool held,bool up)
 {
  if(down)
  {
   bool used=false;foreach(var b in buttons.ToArray().Reverse())if(b.Rect.Contains(cursor))
   {
    keyboardFocus=-1;lastPressedButton=b.Rect;lastPressedAt=elapsed;var before=ui.Screens.Active;dispatchingUiButton=true;
    try{b.Click();}finally{dispatchingUiButton=false;}
    audio?.PlayUi(before!=ui.Screens.Active?"page":"confirm");used=true;break;
   }
   if(!used&&!title&&!UiBlocksWorld&&MapRect.Contains(cursor)&&world.Mode is Mode.Explore or Mode.Combat)
   {leftWorldPress=true;boxDragging=false;pressScreen=cursor;selectionAnchor=pressedPoint=MouseWorld();pressedActor=PickActor();pressedCorpse=pressedActor==null?PickCorpse():null;pressedGround=world.GroundPileAt(pressedPoint.Cell);}
  }
  if(held&&leftWorldPress&&V.Distance(new(pressScreen.X,pressScreen.Y),new(cursor.X,cursor.Y))>6)boxDragging=true;
  if(up&&leftWorldPress)
  {
   leftWorldPress=false;
   if(title||UiBlocksWorld){boxDragging=false;return;}
   if(boxDragging){ApplyBoxSelection(SelectionRectangle(),ExtendSelection);boxDragging=false;return;}
   if(!MapRect.Contains(cursor))return;
   if(SpellTargetingActive)
   {
    if(paused){Notify("Resume time before casting.");return;}
    var spell=ArmedSpell;Actor? spellTarget=pressedActor??(spell?.Id=="revivify"?pressedCorpse:null);
    bool pointOnly=spell!=null&&(spell.Target==SpellTargetKind.Point||IsDirectionalSpell(spell));
    if(pointOnly)spellTarget=null;
    TryCastArmedSpell(spellTarget,spellTarget?.Position??pressedPoint);return;
   }
   if(pressedCorpse is {} corpseTarget)
   {
    var looter=SelectionLeader;if(looter==null){Notify("Select a living companion first.");return;}if(!world.RequestCorpseLoot(looter,corpseTarget))Notify(world.Log.LastOrDefault()??"That corpse cannot be reached.");return;
   }
   if(pressedActor is {} target)
   {
    if(paused)return;
    if(action==TargetIntent.Ability){TryUseArmedFeatureAbility(target);return;}
    if(!target.Enemy)
    {
     if(action==TargetIntent.Talk){Notify(target.DialogueTarget?$"{target.Name} is ready to speak, but actor-dialogue binding is reserved for companion conversations.":"Talk requires a dialogue target.");return;}
     if(action==TargetIntent.Heal){if(!world.Heal(Controller,target))world.LogSystem("Select a healer and a wounded living ally within the feature range.",true);return;}
     if(action==TargetIntent.Help&&world.Mode==Mode.Combat){if(!world.Help(Controller,target))Notify("Help requires a nearby ally and an unused Action.");else{action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;}return;}
     SelectCharacter(world.PartyIndex(target),ExtendSelection);return;
    }
    if(target.Enemy)
    {
     if(action is TargetIntent.Heal or TargetIntent.Help or TargetIntent.UseObject or TargetIntent.Ability or TargetIntent.Talk){Notify("That action requires a legal target.");return;}
     if(world.Mode==Mode.Explore)
     {
      if(Selection.Count()>1&&action==TargetIntent.Attack)
      {
       var leader=SelectionLeader;world.Message(MessageCategory.Combat,$"{leader?.Name??"The party"} orders {Selection.Count()} selected companions to attack {target.Name}.");
      }
      world.StartCombat(target.Group);
     }
     else if(world.Active is {Enemy:false} a&&(action is TargetIntent.Attack or TargetIntent.Special))world.RequestAttack(a,target,action==TargetIntent.Special?AttackKind.Special:AttackKind.Basic);
     return;
    }
   }
   if(!world.Explored.Contains(pressedPoint.Cell)){Notify("Explore closer to reveal this area.");return;}
   var f=ui.Highlights.FeatureAt(pressedPoint.Cell);
   if(f!=null)
   {
    if(action==TargetIntent.Talk&&f.Kind!="npc"){Notify("Talk requires a dialogue target.");return;}
    if(world.Mode==Mode.Combat&&action==TargetIntent.UseObject){if(world.UseObject(Controller,f)){action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;}return;}
    IssueInteraction(f);
   }
   else if(pressedGround!=null&&world.Mode==Mode.Explore)
   {
    var looter=SelectionLeader;if(looter==null)Notify("Select a living companion first.");else if(!world.RequestGroundLoot(looter,pressedPoint.Cell))Notify(world.Log.LastOrDefault()??"That ground pile cannot be reached.");
   }
   else if(action==TargetIntent.Talk)Notify("Talk requires a dialogue target.");
   else if(world.Mode==Mode.Combat&&(action is TargetIntent.Help or TargetIntent.UseObject or TargetIntent.Heal or TargetIntent.Ability))Notify(action==TargetIntent.Help?"Help needs a nearby ally.":action==TargetIntent.Heal?"Heal needs a wounded living ally.":action==TargetIntent.Ability?"That ability needs a legal creature target.":"Use Object needs an adjacent usable object.");
   else IssueMove(pressedPoint);
  }
 }

 bool CancelTargetingWithRightClick()
 {
  if(title||ui.Screens.IsOpen||ui.Modals.IsOpen)return false;
  if(action==TargetIntent.Attack&&actionBarPage==ActionBarPage.Primary)return false;
  ClearArmedSpell();ClearArmedFeatureAbility();pendingSpellPlan=null;action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;
  Notify("Targeting cancelled.");return true;
 }
 void OpenContext()
 {
  if(title||ui.Screens.IsOpen||ui.Messages.Contains(cursor)||ui.Initiative.Contains(cursor)||!MapRect.Contains(cursor)||world.Mode is Mode.Defeat or Mode.Victory)return;
  leftWorldPress=boxDragging=false;contextPoint=MouseWorld();contextActor=PickActor();contextCorpse=contextActor==null?PickCorpse():null;
  contextFeature=ui.Highlights.FeatureAt(contextPoint.Cell);contextGround=world.GroundPileAt(contextPoint.Cell);
  contextScreen=cursor;contextOpen=true;BuildContextCommands();
 }
 void BuildContextCommands()
 {
  ResetFormationContext();
  contextCommands.Clear();var controller=Controller;var target=contextActor;var feature=contextFeature;
  bool canOrder=!paused&&(world.Mode==Mode.Explore||world.Active is {Enemy:false});
  bool canAct=canOrder&&world.CanAct(controller);
  void Add(string label,System.Action run,bool enabled=true,string hint="")=>contextCommands.Add((label,run,enabled,hint));
  if(contextCorpse is {} dead)
  {
   Add("Loot remains",()=>{var looter=SelectionLeader??controller;if(!world.RequestCorpseLoot(looter,dead))Notify(world.Log.LastOrDefault()??"Cannot reach the remains.");},canOrder&&Selection.Any(),"Approach the remains and open the lower loot panel.");
   Add("Inspect remains",()=>Notify($"{dead.Name}: fallen. {world.CorpseLoot(dead).Count()} recoverable item(s)."));
  }
  else if(target is {Enemy:true})
  {
   var basic=world.PreviewAttack(controller,target,AttackKind.Basic);var special=world.PreviewAttack(controller,target,AttackKind.Special);
   Add(world.Mode==Mode.Explore?"Engage enemy":"Attack",()=>{action=TargetIntent.Attack;if(world.Mode==Mode.Explore)world.StartCombat(target.Group);else world.RequestAttack(controller,target,AttackKind.Basic);},canOrder&&(world.Mode==Mode.Explore||basic.Allowed),paused?"Resume time first":basic.Reason);
   if(world.Mode==Mode.Combat)Add(controller.Special+" [S]",()=>world.RequestAttack(controller,target,AttackKind.Special),canOrder&&special.Allowed,paused?"Resume time first":special.Reason);
   Add("Inspect enemy",()=>Notify($"{target.Name}: HP {target.Hp}/{target.MaxHp}, armor {target.Ac}, range {target.Range}."));
  }
  else if(target is {Enemy:false})
  {
   Add("Select "+target.Name,()=>SelectCharacter(world.PartyIndex(target)));
   Add("Heal "+target.Name,()=>world.Heal(controller,target),canOrder&&controller.CanHeal&&controller.HealingFeature is {} healing&&(controller.Character.Resource(healing.Mechanics.ResourceId)?.Current??0)>0&&target.Hp<target.MaxHp&&controller.Position.Distance(target.Position)<=Math.Max(1,healing.Mechanics.RangeFeet/5f)&&world.Line(controller.Position,target.Position)&&(world.Mode==Mode.Explore||canAct),"Needs a healing feature, its resource, a wounded ally in range, and an available action.");
   if(world.Mode==Mode.Combat&&target!=controller)Add("Help "+target.Name,()=>world.Help(controller,target),world.CanAct(controller)&&controller.Position.Distance(target.Position)<=1.25f,"Help costs the current Action and requires an ally within 5 feet.");
   Add("Inspect character [C]",()=>{SetPrimaryPartyActor(target);ui.Screens.NavigateRoot(UiScreenId.Character);});
   if(target==controller)Add("Drink potion [P]",()=>world.Potion(controller),canOrder&&controller.Hp<controller.MaxHp&&world.ItemCount(controller,"healing-potion")>0&&(world.Mode==Mode.Explore||canAct),"Needs a potion, missing HP and an available action.");
  }
  else if(feature!=null)
  {
   if(world.Mode==Mode.Combat&&world.IsEngagedBarricade(feature))Add("Strike barricade",()=>world.AttackBarricade(controller),world.CanBashBarricade(controller),"A melee attack against AC 12; spends an Attack allocation and damages the barricade.");
   if(world.Mode==Mode.Combat)Add("Use Object",()=>world.UseObject(controller,feature),canAct&&feature.Kind=="door"&&!feature.Used&&controller.Position.Distance(feature.Pos.Center)<=1.25f,"Use Object costs the Action and currently supports adjacent doors in combat.");
   else Add(FeatureVerb(feature),()=>IssueInteraction(feature),canOrder&&Selection.Any()&&world.IsFeatureInteractable(feature),"Select a companion first or discover the object before interacting.");
   Add("Inspect object",()=>Notify(feature.Name+(feature.Locked?" - locked.":feature.DetectedTrap?" - trapped.":" - "+FeatureVerb(feature)+" by clicking it.")));
  }
  else if(contextGround is {} pile&&pile.Inventory.Items.Count>0)
  {
   Add("Loot ground",()=>{var looter=SelectionLeader??controller;if(!world.RequestGroundLoot(looter,contextPoint.Cell))Notify(world.Log.LastOrDefault()??"Cannot reach the ground loot.");},canOrder&&Selection.Any(),"Approach this ground pile and open the lower loot panel.");
   Add("Inspect ground",()=>Notify($"{pile.Inventory.Items.Count} item stack(s) lie here."));
  }
  else
  {
   var movementLeader=SelectionLeader??controller;string reason=world.MovementReason(movementLeader,contextPoint);
   Add("Move selected here",()=>IssueMove(contextPoint),canOrder&&Selection.Any()&&reason=="",paused?"Resume time first":!Selection.Any()?"Select a companion first":reason);
   Add("Stop selected [X]",StopSelection,Selection.Any(),"Select a companion first.");
   Add("Hold / Guard",HoldParty,Selection.Any(),"Stop selected companions and disable Follow Leader.");
   Add("Select all [G]",SelectAll);
   if(world.Mode==Mode.Explore)Add(following?"Disable leader follow":"Follow leader [O]",FollowParty);
  }
  if(world.SurvivorClassesEnabled)Add($"Class abilities [{BindingLabel(InputAction.OpenSurvivorClasses,false)}]",()=>ui.Rest.ShowClasses(controller,target,contextPoint.Cell),true,"Choose class abilities for this survivor using the clicked target or location.");
  if(world.LowerMallEnabled&&controller.Character.HasCondition("book-grappled"))Add("Escape shop grapple (Action)",()=>world.EscapeLowerMallGrapple(controller),controller.Alive,"Athletics DC 14; uses your Action in combat.");
  if(world.SocialEnabled)Add("Factions and survivor relationships",()=>ui.Rest.ShowSocial(),true,"Review standings, trust, bonds and refuge conversations.");
  if(world.StoryEnabled)Add($"Refuge contracts [{BindingLabel(InputAction.OpenContracts,false)}]",()=>ui.Rest.ShowContracts(controller),true,"Review requirements, progress and one-time turn-ins.");
  if(world.WorkshopEnabled)Add($"Workshop / Barter [{BindingLabel(InputAction.OpenWorkshop,false)}]",()=>ui.Rest.ShowWorkshop(controller),true,"Manage physical gear or trade at the refuge.");
  if(world.EquipmentEnabled)Add($"Equipment [{BindingLabel(InputAction.OpenEquipmentCatalogue,false)}]",()=>ui.Rest.ShowEquipment(controller,target,contextPoint.Cell),true,"Browse the catalogue and use owned equipment on this target or location.");
  if(world.LocalAwarenessEnabled)Add("Shout / distract",()=>world.Shout(controller),world.CanShout(controller),"A 60-foot sound budget. Costs an Action in combat, free in exploration. Breaks hiding and may attract enemies.");
  if(controller.Equipment.Weapon?.Definition.MagazineSize>0){Add("Reload weapon",()=>world.ReloadFirearm(controller),world.FirearmActionReason(controller)=="",world.FirearmActionReason(controller));Add("Unload weapon",()=>world.UnloadFirearm(controller),world.FirearmActionReason(controller)==""&&controller.Equipment.Weapon.LoadedAmmunition!=null,"Requires a loaded firearm and an available Action in combat.");}
  if(world.Mode==Mode.Combat)
  {
   if(world.ModernTacticalCombat){Add("Disengage",()=>world.ExecuteStandardAction(controller,StandardActionKind.Disengage),canAct,"Use your Action to move without opportunity attacks this turn.");Add("Ready weapon attack",()=>world.ExecuteStandardAction(controller,StandardActionKind.Ready),canAct&&world.HasRequiredAmmunition(controller),"Use your Action to ready one attack, then your Reaction to fire when a hostile enters weapon range.");}
   Add("Dodge [D]",()=>world.ExecuteStandardAction(controller,StandardActionKind.Dodge),canAct,"Dodge costs the current Action.");
   Add("Dash",()=>world.ExecuteStandardAction(controller,StandardActionKind.Dash),canAct,"Dash costs the current Action and adds movement equal to speed.");
   Add("End turn",RequestEndTurn,CanEndTurnFromControl,"End Turn is available only for the active player-controlled combatant.");
  }
  int height=39+contextCommands.Count*33;
  contextRect=new(Math.Clamp(contextScreen.X,MapRect.Left,MapRect.Right-238),Math.Clamp(contextScreen.Y,MapRect.Top,MapRect.Bottom-height),238,height);
 }
 void DrawControlOverlays()
 {
  if(boxDragging&&leftWorldPress){var r=Rectangle.Intersect(SelectionRectangle(),MapRect);Fill(r,Teal*.13f);Border(r,Teal,2);}
  if(!contextOpen)return;
  buttons.Add((new(0,0,W,H),()=>contextOpen=false));Panel(contextRect);
  string heading=contextHeadingOverride.Length>0?contextHeadingOverride:contextActor?.Name??contextFeature?.Name??$"{Selection.Count()} companions selected";
  if(heading.Length>25)heading=heading[..24]+".";
  Center(heading,new(contextRect.X+7,contextRect.Y+8,224,22),Gold,.65f);
  for(int i=0;i<contextCommands.Count;i++)
  {
   var entry=contextCommands[i];var row=new Rectangle(contextRect.X+8,contextRect.Y+33+i*33,222,30);
   if(entry.Enabled)Button(row,entry.Label,()=>{contextOpen=false;entry.Run();},false,.68f);
   else{Box(row,new Color(23,25,25),Gold*.15f);Text(entry.Label,row.X+11,row.Y+5,Muted*.4f,.68f);buttons.Add((row,()=>Notify(entry.Hint)));}
   if(row.Contains(cursor)&&entry.Hint!=""){var hint=new Rectangle(contextRect.X,contextRect.Bottom+4,contextRect.Width,60);if(hint.Bottom>MapRect.Bottom)hint.Y=contextRect.Y-64;Box(hint,new Color(14,18,22),Gold*.5f);Paragraph(entry.Hint,hint.X+8,hint.Y+5,hint.Width-16,Muted,.62f,18);}
  }
 }
 void UpdateLayout()
 {
  if(resizePending)
  {
   resizePending=false;var bounds=Window.ClientBounds;
   if(bounds.Width>0&&bounds.Height>0&&(bounds.Width!=GraphicsDevice.PresentationParameters.BackBufferWidth||bounds.Height!=GraphicsDevice.PresentationParameters.BackBufferHeight))
   {graphics.PreferredBackBufferWidth=bounds.Width;graphics.PreferredBackBufferHeight=bounds.Height;graphics.ApplyChanges();}
  }
  int bw=Math.Max(1,GraphicsDevice.PresentationParameters.BackBufferWidth),bh=Math.Max(1,GraphicsDevice.PresentationParameters.BackBufferHeight);
  int width=UiLayout.LogicalWidthFor(bw,bh);
  if(width!=W){W=width;canvas.Dispose();canvas=new(GraphicsDevice,W,H);contextOpen=false;ui.Portraits.CancelInteraction();leftWorldPress=boxDragging=false;}
  float scale=Math.Min(bw/(float)W,bh/(float)H);int dw=(int)Math.Round(W*scale),dh=(int)Math.Round(H*scale);
  destination=new((bw-dw)/2,(bh-dh)/2,dw,dh);
  cursor=new((int)((mouse.X-destination.X)/scale),(int)((mouse.Y-destination.Y)/scale));
  ui.Layout.Reflow(W,H,preferences.InterfaceScale);
 }
}
