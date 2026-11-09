using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Text.Json;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 enum DisplayModePreference { Windowed, Fullscreen }
 enum SettingsCategory { Graphics, Audio, Gameplay, Accessibility, Controls }
 static readonly (int Width,int Height,string Label)[] DisplayResolutions=
 [
  (1280,720,"1280 x 720"),(1600,900,"1600 x 900"),(1920,1080,"1920 x 1080"),(2560,1440,"2560 x 1440")
 ];
 static readonly float[] DialogueSpeeds=[0,35,70,110];

 sealed class Preferences
 {
  // Graphics/display
  public DisplayModePreference DisplayMode {get;set;}=DisplayModePreference.Windowed;
  public int ResolutionIndex {get;set;}=0;
  public bool VSync {get;set;}=true;
  // Audio categories
  public bool AudioMuted {get;set;}=false;
  public float MasterVolume {get;set;}=.80f;
  public float MusicVolume {get;set;}=.70f;
  public float EffectsVolume {get;set;}=.80f;
  public float UiVolume {get;set;}=.65f;
  public float VoiceVolume {get;set;}=.85f;
  // Camera/gameplay/readability
  public bool EdgeScroll {get;set;}=true;
  public float ScrollSpeed {get;set;}=11;
  public float KeyboardScrollSpeed {get;set;}=10;
  public float TextScale {get;set;}=1;
  public float InterfaceScale {get;set;}=1;
  public float Zoom {get;set;}=1;
  public bool AutoFocus {get;set;}=false;
  public bool ConfirmEndTurn {get;set;}=true;
  public DifficultyPreset Difficulty {get;set;}=DifficultyPreset.Balanced;
  public float DialogueTextSpeed {get;set;}=70;
  public bool TacticsEnabled {get;set;}=false;
  public MessageVerbosity LogVerbosity {get;set;}=MessageVerbosity.Detailed;
  public int MessageLogHeight {get;set;}=1;
  public int TooltipDelayMs {get;set;}=550;
  public bool AutoSaveEnabled {get;set;}=true;
  public int AutoSaveSlots {get;set;}=3;
  // Accessibility
  public bool HighContrast {get;set;}=false;
  public bool ReducedMotion {get;set;}=false;
  public bool PersistentTooltips {get;set;}=false;
  public bool LargeCursor {get;set;}=false;
  public ReactionPolicy OpportunityAttackPolicy {get;set;}=ReactionPolicy.Ask;
  public ReactionPolicy ShieldPolicy {get;set;}=ReactionPolicy.Ask;
  public ReactionPolicy CounterspellPolicy {get;set;}=ReactionPolicy.Ask;
  public ReactionPolicy ReadiedActionPolicy {get;set;}=ReactionPolicy.Ask;
  public ReactionPolicy CuttingWordsPolicy {get;set;}=ReactionPolicy.Ask;
  // IIP46: keyboard bindings are profile state, not journey state. Empty string means explicitly unbound.
  public Dictionary<string,string> KeyBindings {get;set;}=[];
 }
 Preferences preferences=new();
 SettingsCategory settingsCategory=SettingsCategory.Graphics;
 string PreferencesPath=>Path.Combine(Path.GetDirectoryName(savePath)!,"preferences.json");
 bool following;string followLeader="";float followClock;
 float noticeUntil,bannerUntil,focusUntil;string notice="",banner="";V focusTarget;
 string previousTurn="";Mode previousMode;
 float turnBeganAt;
 string lastPortraitId="";float lastPortraitTime=-1;
 readonly HashSet<Cell> reachable=[];float reachableAt;
 readonly List<(WorldPoint Position,string Text,Color Color,float Until)> floats=[];
 readonly Dictionary<string,int> lastHp=[];
 string lastLog="";
 List<WorldPoint> hoverRoute=[];string moveReason="";float hoverCost,previewAt=-1;WorldPoint previewPoint;string previewActor="";
 ActionPreview? attackPreview;Actor? hoverActor,hoverCorpse;Feature? hoverFeature;GroundPile? hoverGround;
 Rectangle MiniRect=>new(MapRect.Right-208,MapRect.Bottom-152,196,140);
 Rectangle MiniInner=>new(MiniRect.X+9,MiniRect.Y+27,178,104);
 bool WorldHover=>MapRect.Contains(cursor)&&(!preferences.EdgeScroll||EdgeDirection(cursor)==V.Zero)&&!MiniRect.Contains(cursor)&&!ui.Messages.Contains(cursor)&&!title&&!UiBlocksWorld&&!boxDragging&&!ui.Portraits.IsDragging&&!FormationDragActive;
 string SelectionLabel=>Selection.Count()==1?Selection.First().Name+" selected":$"{Selection.Count()} characters selected";

 void SanitizePreferences()
 {
  if(!Enum.IsDefined(typeof(DisplayModePreference),preferences.DisplayMode))preferences.DisplayMode=DisplayModePreference.Windowed;
  preferences.ResolutionIndex=Math.Clamp(preferences.ResolutionIndex,0,DisplayResolutions.Length-1);
  preferences.MasterVolume=float.IsFinite(preferences.MasterVolume)?Math.Clamp(preferences.MasterVolume,0,1):.8f;
  preferences.MusicVolume=float.IsFinite(preferences.MusicVolume)?Math.Clamp(preferences.MusicVolume,0,1):.7f;
  preferences.EffectsVolume=float.IsFinite(preferences.EffectsVolume)?Math.Clamp(preferences.EffectsVolume,0,1):.8f;
  preferences.UiVolume=float.IsFinite(preferences.UiVolume)?Math.Clamp(preferences.UiVolume,0,1):.65f;
  preferences.VoiceVolume=float.IsFinite(preferences.VoiceVolume)?Math.Clamp(preferences.VoiceVolume,0,1):.85f;
  preferences.ScrollSpeed=float.IsFinite(preferences.ScrollSpeed)?Math.Clamp(preferences.ScrollSpeed,4,22):11;
  preferences.KeyboardScrollSpeed=float.IsFinite(preferences.KeyboardScrollSpeed)?Math.Clamp(preferences.KeyboardScrollSpeed,4,24):10;
  preferences.TextScale=float.IsFinite(preferences.TextScale)?Math.Clamp(preferences.TextScale,1,1.3f):1;
  preferences.InterfaceScale=float.IsFinite(preferences.InterfaceScale)?Math.Clamp(preferences.InterfaceScale,UiLayout.MinimumScale,UiLayout.MaximumScale):1;
  if(!Enum.IsDefined(typeof(DifficultyPreset),preferences.Difficulty))preferences.Difficulty=DifficultyPreset.Balanced;
  if(!float.IsFinite(preferences.DialogueTextSpeed))preferences.DialogueTextSpeed=70;
  preferences.DialogueTextSpeed=DialogueSpeeds.OrderBy(v=>Math.Abs(v-preferences.DialogueTextSpeed)).First();
  if(!Enum.IsDefined(typeof(MessageVerbosity),preferences.LogVerbosity))preferences.LogVerbosity=MessageVerbosity.Detailed;
  preferences.MessageLogHeight=Math.Clamp(preferences.MessageLogHeight,0,2);
  preferences.TooltipDelayMs=Math.Clamp(preferences.TooltipDelayMs,0,1500);
  preferences.AutoSaveSlots=Math.Clamp(preferences.AutoSaveSlots,1,5);
  SanitizeKeyBindings();
  ui.Messages.RestoreHeight(preferences.MessageLogHeight);
  foreach(var kind in Enum.GetValues<ReactionKind>())if(!Enum.IsDefined(typeof(ReactionPolicy),ReactionPolicyFor(kind)))SetReactionPolicy(kind,ReactionPolicy.Ask);
  zoom=float.IsFinite(preferences.Zoom)?Math.Clamp(preferences.Zoom,.65f,1.65f):1;
 }
 void LoadPreferences()
 {
  if(screenshot!=null||uiCheck){SanitizePreferences();return;}
  try{if(File.Exists(PreferencesPath))preferences=JsonSerializer.Deserialize<Preferences>(File.ReadAllText(PreferencesPath))??new();}catch{preferences=new();}
  SanitizePreferences();
 }
 void ApplyRuntimePreferences()
 {
  world.Difficulty=preferences.Difficulty;
  ui.Layout.Reflow(W,H,preferences.InterfaceScale);
  if(audio!=null)
  {
   audio.Muted=preferences.AudioMuted;audio.MasterVolume=preferences.MasterVolume;audio.MusicVolume=preferences.MusicVolume;audio.EffectsVolume=preferences.EffectsVolume;audio.UiVolume=preferences.UiVolume;audio.VoiceVolume=preferences.VoiceVolume;
  }
 }
 void ApplyDisplayPreferences(bool announce=true)
 {
  if(screenshot!=null||uiCheck)return;
  var selected=DisplayResolutions[Math.Clamp(preferences.ResolutionIndex,0,DisplayResolutions.Length-1)];
  graphics.PreferredBackBufferWidth=selected.Width;graphics.PreferredBackBufferHeight=selected.Height;graphics.SynchronizeWithVerticalRetrace=preferences.VSync;
  bool wantFullscreen=preferences.DisplayMode==DisplayModePreference.Fullscreen;
  if(graphics.IsFullScreen!=wantFullscreen)graphics.ToggleFullScreen();else graphics.ApplyChanges();
  resizePending=true;if(announce)Notify($"Display: {preferences.DisplayMode}, {selected.Label}, VSync {(preferences.VSync?"on":"off")}.");
 }
 void CycleDisplayMode(){preferences.DisplayMode=preferences.DisplayMode==DisplayModePreference.Windowed?DisplayModePreference.Fullscreen:DisplayModePreference.Windowed;SavePreferences();ApplyDisplayPreferences();}
 void ToggleFullscreenShortcut(){preferences.DisplayMode=graphics.IsFullScreen?DisplayModePreference.Windowed:DisplayModePreference.Fullscreen;SavePreferences();ApplyDisplayPreferences(false);Notify(preferences.DisplayMode==DisplayModePreference.Fullscreen?"Fullscreen enabled.":"Windowed mode enabled.");}
 void CycleResolution(){preferences.ResolutionIndex=(preferences.ResolutionIndex+1)%DisplayResolutions.Length;SavePreferences();ApplyDisplayPreferences();}
 void ToggleVSync(){preferences.VSync=!preferences.VSync;SavePreferences();ApplyDisplayPreferences();}
 void CycleInterfaceScale()
 {
  float s=preferences.InterfaceScale;
  preferences.InterfaceScale=s<.93f?1:s<1.08f?1.15f:s<1.23f?1.30f:.85f;
  ui.Layout.Reflow(W,H,preferences.InterfaceScale);SavePreferences();
 }
 void CycleTextScale(){preferences.TextScale=preferences.TextScale<1.09f?1.1f:preferences.TextScale<1.19f?1.2f:preferences.TextScale<1.29f?1.3f:1;SavePreferences();}
 void CycleTooltipDelay()
 {
  int[] values=[0,250,550,850,1200];int index=Array.IndexOf(values,preferences.TooltipDelayMs);
  preferences.TooltipDelayMs=values[(index+1+values.Length)%values.Length];SavePreferences();
  Notify($"Tooltip delay set to {preferences.TooltipDelayMs} ms.");
 }
 void CycleDialogueTextSpeed()
 {
  int index=Array.IndexOf(DialogueSpeeds,preferences.DialogueTextSpeed);preferences.DialogueTextSpeed=DialogueSpeeds[(index+1+DialogueSpeeds.Length)%DialogueSpeeds.Length];SavePreferences();
 }
 string DialogueTextSpeedName()=>preferences.DialogueTextSpeed<=0?"Instant":preferences.DialogueTextSpeed<50?"Slow":preferences.DialogueTextSpeed<90?"Normal":"Fast";
 void CycleDifficulty()
 {
  var values=Enum.GetValues<DifficultyPreset>();int index=Array.IndexOf(values,preferences.Difficulty);preferences.Difficulty=values[(index+1+values.Length)%values.Length];ApplyRuntimePreferences();SavePreferences();
 }
 void AdjustVolume(string channel,float delta)
 {
  switch(channel)
  {
   case "master":preferences.MasterVolume=Math.Clamp(preferences.MasterVolume+delta,0,1);break;
   case "music":preferences.MusicVolume=Math.Clamp(preferences.MusicVolume+delta,0,1);break;
   case "effects":preferences.EffectsVolume=Math.Clamp(preferences.EffectsVolume+delta,0,1);break;
   case "ui":preferences.UiVolume=Math.Clamp(preferences.UiVolume+delta,0,1);break;
   case "voices":preferences.VoiceVolume=Math.Clamp(preferences.VoiceVolume+delta,0,1);break;
  }
  ApplyRuntimePreferences();SavePreferences();
 }
 void ToggleAudioMute(){preferences.AudioMuted=!preferences.AudioMuted;ApplyRuntimePreferences();SavePreferences();Notify(preferences.AudioMuted?"Audio muted.":"Audio enabled.");}
 void RestorePreferenceDefaults()
 {
  preferences=new();SanitizePreferences();ApplyRuntimePreferences();SavePreferences();ApplyDisplayPreferences(false);Notify("Options restored to defaults.");
 }
 void SavePreferences()
 {
  if(screenshot!=null||uiCheck)return;
  try
  {
   preferences.Zoom=zoom;SanitizePreferences();Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);string temp=PreferencesPath+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(preferences,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,PreferencesPath,true);
  }
  catch(Exception){Notify("Settings could not be saved to your profile.");}
 }
 void Notify(string message){notice=message;noticeUntil=elapsed+3.5f;}
 void FocusActor(Actor actor)
 {
  focusTarget=new(actor.Position.X,actor.Position.Y);focusUntil=elapsed+.6f;
  // Major management screens visually cover the world. Center immediately behind the
  // screen so a portrait double-click still has deterministic camera semantics.
  if(UiBlocksWorld)camera=focusTarget;
 }
 void PortraitClick(int index,bool extend=false)
 {
  var target=world.PartyAt(index);if(target==null)return;
  if(action==TargetIntent.Heal&&!UiBlocksWorld&&!paused){if(!world.Heal(Controller,target))Notify("Heal needs a healing feature, a resource charge, and a wounded ally in range.");return;}
  if(action==TargetIntent.Help&&!UiBlocksWorld&&!paused&&world.Mode==Mode.Combat){if(!world.Help(Controller,target))Notify("Help needs a nearby living ally and an unused Action.");else{action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;}return;}
  SelectCharacter(index,extend);
  audio?.PlayAcknowledgment(target.VoiceSet);
  if(!extend&&lastPortraitId==target.Id&&elapsed-lastPortraitTime<.35f)FocusActor(target);
  lastPortraitId=target.Id;lastPortraitTime=elapsed;
 }
 void OpenPortraitInventory(int index)
 {
  var target=world.PartyAt(index);if(target==null)return;
  SelectCharacter(index);ui.Modals.Clear();ui.Screens.NavigateRoot(UiScreenId.Inventory);
  leftWorldPress=boxDragging=false;
 }
 void FollowParty()
 {
  if(world.Mode!=Mode.Explore){Notify("Follow is available during exploration.");return;}
  if(following){following=false;followLeader="";Notify("Follow disabled.");return;}
  var leader=world.AvailablePartyLeader;if(leader==null){Notify("No conscious party leader is available.");return;}
  following=true;followLeader=leader.Id;followClock=0;
  Notify($"Party follows leader {leader.Name}. Reorder portraits to change the leader.");
  SelectCharacter(Math.Max(0,world.PartyIndex(leader)));
 }
 void HoldParty(){following=false;StopSelection();Notify("Selected characters hold position.");}
 void UpdateUsability(float dt)
 {
  foreach(var a in world.Actors)
  {
   if(!preferences.ReducedMotion&&lastHp.TryGetValue(a.Id,out int hp)&&hp!=a.Hp&&(!a.Enemy||world.Visible.Contains(a.Pos)))floats.Add((a.Position,a.Hp<hp?$"{hp-a.Hp} damage":$"+{a.Hp-hp} HP",a.Hp<hp?Bright:Teal,elapsed+1.8f));
   lastHp[a.Id]=a.Hp;
  }
  floats.RemoveAll(x=>x.Until<elapsed);
  string latest=world.Log.LastOrDefault()??"";
  if(latest!=lastLog)
  {
   if(!preferences.ReducedMotion&&latest.Contains("MISS"))floats.Add((world.Active?.Position??world.Chosen.Position,"Miss",Muted,elapsed+1.8f));
   if(latest.Contains("blocked")||latest.Contains("Not enough")||latest.Contains("No clear")||latest.Contains("requires")||latest.Contains("too close"))Notify(latest);
   lastLog=latest;
  }
  if(previousMode!=world.Mode)
  {
   ClearEndTurnConfirmation();
   if(world.Mode==Mode.Combat){paused=false;banner="COMBAT BEGINS - INITIATIVE ROLLED";bannerUntil=elapsed+3;following=false;}
   else if(previousMode==Mode.Combat&&world.Mode==Mode.Explore){banner="VICTORY - EXPLORATION RESUMES";bannerUntil=elapsed+3;SelectAll();}
   previousMode=world.Mode;
  }
  if(previousTurn!=world.Active?.Id)
  {
   if(previousTurn.Length>0)ClearEndTurnConfirmation();previousTurn=world.Active?.Id??"";turnBeganAt=elapsed;
   if(preferences.AutoFocus&&world.Active is {} active){focusTarget=new(active.Position.X,active.Position.Y);focusUntil=elapsed+.6f;}
  }
  if(elapsed<focusUntil&&!UiBlocksWorld)camera=preferences.ReducedMotion?focusTarget:V.Lerp(camera,focusTarget,Math.Min(1,dt*9));
  if(following&&world.Mode==Mode.Explore&&!paused&&!title&&!UiBlocksWorld)
  {
   followClock-=dt;if(followClock<=0)
   {
    followClock=.6f;var leader=world.AvailablePartyLeader;
    if(leader==null){following=false;followLeader="";}
    else
    {
     followLeader=leader.Id;
     foreach(var a in world.Party.Where(a=>a.Alive&&a!=leader&&!world.IsMoving(a)&&a.Position.Distance(leader.Position)>2.1f))
     {
      for(int i=0;i<12;i++){float angle=(i+Math.Max(0,world.PartyIndex(a))*3)*MathF.Tau/12;var p=leader.Position+new WorldPoint(MathF.Cos(angle),MathF.Sin(angle))*1.2f;if(world.Explored.Contains(p.Cell)&&world.MoveTo(a,p))break;}
     }
    }
   }
  }
  if(elapsed>=reachableAt)
  {
   reachableAt=elapsed+.35f;reachable.Clear();
   if(world.Mode==Mode.Combat&&world.Active is {Enemy:false} current&&!world.IsMoving(current))
    RebuildReachable(current);
  }
  UpdateHoverPreview();
 }
 void RebuildReachable(Actor actor)
 {
  // One bounded search replaces a separate A* and smoothing pass for every cell.
  // This preview stays conservative; the authoritative MoveTo path still decides moves.
  var origin=actor.Position;float limit=actor.Move;
  var costs=new Dictionary<Cell,float>{{origin.Cell,0}};
  var frontier=new PriorityQueue<Cell,float>();frontier.Enqueue(origin.Cell,0);
  var visible=world.Visible.ToHashSet();
  while(frontier.TryDequeue(out var cell,out float queued))
  {
   if(queued>costs[cell]+.001f)continue;
   var from=cell==origin.Cell?origin:cell.Center;
   for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
   {
    if(dx==0&&dy==0)continue;
    var next=new Cell(cell.X+dx,cell.Y+dy);if(!visible.Contains(next))continue;
    var point=next.Center;float cost=queued+from.Distance(point);
    if(cost>limit+.001f||costs.TryGetValue(next,out float old)&&old<=cost+.001f)continue;
    if(!world.CanStand(point,actor)||!world.SegmentClear(from,point,actor))continue;
    costs[next]=cost;frontier.Enqueue(next,cost);reachable.Add(next);
   }
  }
 }
 void UpdateHoverPreview()
 {
  if(!WorldHover){hoverActor=null;hoverCorpse=null;hoverFeature=null;hoverGround=null;attackPreview=null;return;}
  hoverActor=PickActor();hoverCorpse=hoverActor==null?PickCorpse():null;var point=MouseWorld();hoverFeature=ui.Highlights.FeatureAt(point.Cell);hoverGround=hoverFeature==null&&hoverCorpse==null?world.GroundPileAt(point.Cell):null;
  var movementActor=world.Mode==Mode.Explore?SelectionLeader??Controller:Controller;
  if(elapsed<previewAt&&point.Distance(previewPoint)<.12f&&previewActor==movementActor.Id)return;
  previewAt=elapsed+.13f;previewPoint=point;previewActor=movementActor.Id;attackPreview=null;hoverRoute=[];hoverCost=0;moveReason="";
  if(SpellTargetingActive){UpdateSpellTargetPreview(point);return;}
  spellTargetPreview=null;
  if(hoverActor is {Enemy:true} target&&world.Mode==Mode.Combat&&(action is TargetIntent.Attack or TargetIntent.Special)){attackPreview=world.PreviewAttack(Controller,target,action==TargetIntent.Special?AttackKind.Special:AttackKind.Basic);return;}
  if(hoverActor!=null||hoverCorpse!=null||hoverFeature!=null||hoverGround!=null)return;
  if(!Selection.Any()){moveReason="Select a companion first";return;}
  var movement=world.Mode==Mode.Explore&&Selection.Count()>1?world.PreviewFormationMovement(movementActor,point):world.PreviewMovement(movementActor,point);moveReason=movement.Reason;hoverRoute=movement.Route;hoverCost=movement.CostFeet;
 }
 void DrawRoute(Actor a,IEnumerable<WorldPoint> route,Color color)
 {
  var origin=Screen(a.Position);foreach(var p in route){var next=Screen(p);Line(origin,next,color,2);GroundRing(next,4*zoom,color);origin=next;}
 }
 void DrawWorldGuidance()
 {
  if(title||ui.Screens.IsOpen||ui.Loot.IsOpen)return;
  if(SpellTargetingActive)
  {
   ui.Highlights.DrawWorld();DrawSpellTargetingGuidance();
   foreach(var f in floats){var v=Screen(f.Position);Text(f.Text,v.X-30,v.Y-125-(1.8f-(f.Until-elapsed))*22,f.Color,.8f);}
   return;
  }
  // Reachable cells are a conservative distance preview; movement itself remains continuous.
  if(world.Mode==Mode.Combat&&world.Active is {Enemy:false} active&&!world.IsMoving(active))
  {
   foreach(var cell in reachable)
   {var v=Screen(cell);int s=(int)(54*zoom);var q=new Rectangle((int)v.X-s/2+2,(int)v.Y-s/2+2,s-4,s-4);if(q.Intersects(MapRect))Fill(q,Teal*.095f);}
  }
  foreach(var a in Selection)if(world.Routes.TryGetValue(a.Id,out var route)){DrawRoute(a,route,Teal*.65f);var end=Screen(route.Last());GroundRing(end,14*zoom,Teal);}
  DrawFormationDragPreview();
  if(SelectionLeader is {} travellingLeader&&world.FormationTravelCompressed(travellingLeader.Id))
  {var v=Screen(travellingLeader.Position);var r=new Rectangle((int)v.X-58,(int)v.Y-61,116,24);Box(r,new Color(12,17,20)*.90f,Gold*.5f);Center("COMPRESSING",r,Gold,.48f);}
  if(WorldHover&&hoverActor==null&&hoverFeature==null)
  {
   var movementLeader=world.Mode==Mode.Explore?SelectionLeader??Controller:Controller;
   DrawRoute(movementLeader,hoverRoute,moveReason==""?Teal:Red);
   if(Selection.Count()>1&&world.Mode==Mode.Explore&&moveReason=="")
   {
    var goal=MouseWorld();var d=goal-movementLeader.Position;float len=d.Distance(new(0,0));d=len>.001f?d*(1/len):new(1,0);
    foreach(var entry in FormationPreview(goal,d))
    {var p=Screen(entry.Target);GroundRing(p,18*zoom,entry.Compressed?Gold*.58f:Teal*.65f);Text(entry.Actor.Name,p.X-20,p.Y+8,Muted,.55f);}
   }
  }
  ui.Highlights.DrawWorld();
  if(WorldHover&&hoverActor is {} target)
  {
   if(attackPreview is {} preview)
   {
    DrawRoute(Controller,preview.Route,preview.Allowed?Teal:Red);
    if(action==TargetIntent.Special&&Controller.SpecialSplashes)
    {
     var center=Screen(target.Position);for(int i=0;i<48;i++){float t=i*MathF.Tau/48,u=(i+1)*MathF.Tau/48;Line(center+new V(MathF.Cos(t),MathF.Sin(t))*67.5f*zoom,center+new V(MathF.Cos(u),MathF.Sin(u))*67.5f*zoom,Gold*.8f,2);}
     foreach(var id in preview.Splash)GroundRing(Screen(world.Actors.First(a=>a.Id==id).Position),28*zoom,Gold);
    }
   }
  }
  foreach(var f in floats){var v=Screen(f.Position);Text(f.Text,v.X-30,v.Y-125-(1.8f-(f.Until-elapsed))*22,f.Color,.8f);}
 }
 void DrawMinimap()
 {
  Panel(MiniRect);Text(world.Campaign==null?"CRYPT MAP":"AREA MAP",MiniRect.X+10,MiniRect.Y+6,Gold,.59f);var r=MiniInner;
  float sx=r.Width/(float)World.Width,sy=r.Height/(float)World.Height;
  foreach(var c in world.Explored)Fill(new(r.X+(int)(c.X*sx),r.Y+(int)(c.Y*sy),(int)Math.Ceiling(sx),(int)Math.Ceiling(sy)),world.Visible.Contains(c)?new Color(118,117,95):new Color(55,61,62));
  foreach(var f in world.Features.Where(ui.Highlights.IsTargetable))
  {int x=r.X+(int)(f.Pos.Center.X*sx),y=r.Y+(int)(f.Pos.Center.Y*sy);if(f.Kind=="exit"){Line(new(x-3,y+3),new(x,y-3),Bright,2);Line(new(x,y-3),new(x+3,y+3),Bright,2);}else Fill(new(x-2,y-2,4,4),Gold);}
  foreach(var a in world.Actors.Where(a=>a.Alive&&(!a.Enemy||world.Visible.Contains(a.Pos)))){var q=new Rectangle(r.X+(int)(a.Position.X*sx)-2,r.Y+(int)(a.Position.Y*sy)-2,5,5);Fill(q,a.Enemy?Red:IsSelected(a)?Bright:Teal);if(a.Enemy)Border(new(q.X-1,q.Y-1,7,7),Red);}
  float ww=MapRect.Width/(54*zoom),hh=MapRect.Height/(54*zoom);
  var view=Rectangle.Intersect(r,new(r.X+(int)((camera.X-ww/2)*sx),r.Y+(int)((camera.Y-hh/2)*sy),(int)(ww*sx),(int)(hh*sy)));if(view.Width>0&&view.Height>0)Border(view,Teal);
  buttons.Add((MiniRect,()=>{if(r.Contains(cursor)){camera=new((cursor.X-r.X)/sx,(cursor.Y-r.Y)/sy);focusUntil=0;}}));
 }
 void DrawFeedback()
 {
  if(title||characterCreationOpen||ui.Screens.IsOpen||ui.Loot.IsOpen||InterfaceViewportExpanded)return;
  // Conversations, services and other foreground panels own the lower viewport.
  // Never draw or register the map's click target behind them.
  if(!ui.Messages.Expanded&&!ui.Dialogue.IsOpen&&!ui.Store.IsOpen&&!ui.Services.IsOpen&&!ui.Rest.IsOpen&&!ui.Information.IsOpen&&world.PendingReaction==null&&!ui.Modals.IsOpen&&world.Mode is not (Mode.Defeat or Mode.Victory))DrawMinimap();
  if(elapsed<bannerUntil){var r=new Rectangle(MapRect.Center.X-270,MapRect.Top+86,540,42);Box(r,new Color(13,18,22),Gold);Center(banner,new(r.X,r.Y+9,r.Width,25),Bright,.78f);}
  if(elapsed<noticeUntil){int noticeWidth=Math.Min(660,(int)Measure(notice,.78f)+28);var r=new Rectangle(Math.Clamp(cursor.X-noticeWidth/2,MapRect.Left+5,MapRect.Right-noticeWidth-5),Math.Clamp(cursor.Y+34,MapRect.Top+8,MapRect.Bottom-45),noticeWidth,36);Box(r,new Color(27,20,20),Red);Center(notice,new(r.X,r.Y+7,r.Width,24),Bright,.78f);}
  if(!WorldHover)return;
  string label;string detail="";Color color=Teal;string glyph=">";
  if(SpellTargetingActive&&ArmedSpell is {} armed&&armedSpellPlan is {} plan)
  {
   var preview=spellTargetPreview??world.PreviewSpell(Controller,plan,SpellHoverTarget(armed,hoverActor),hoverActor?.Position??MouseWorld());
   label=$"Cast {armed.Name}";glyph=preview.Allowed?"*":"!";color=preview.Allowed?(preview.FriendlyTarget?Teal:Gold):Red;
   string amount=preview.MaxAmount>0?$" | {(armed.Effect.Kind==SpellEffectKind.Healing?"healing":"effect")} {preview.MinAmount}-{preview.MaxAmount}":"";
   string roll=armed.SavingThrow is {} ability?$" | {ability} save DC {preview.SaveDc}":armed.SpellAttack?$" | spell attack +{preview.SpellAttackBonus}":"";
   string targets=preview.Area?$" | {preview.AffectedActorIds.Count} creature(s) in area":"";
   detail=$"{(armed.Level==0?"Cantrip":$"Level {armed.Level} spell / slot {plan.SlotLevel}")} | {armed.RangeText()}{roll}{amount}{targets}\n{(preview.Allowed?"Left-click to cast. Right-click or Esc cancels.":preview.Reason)}";
  }
  else if(hoverCorpse is {} dead)
  {
   label="Loot "+dead.Name+"'s remains";glyph="+";color=Gold;detail=$"{world.CorpseLoot(dead).Count()} recoverable item(s) | Click to approach and open the loot panel";
  }
  else if(hoverActor is {} a)
  {
   if(a.Enemy){label=world.Mode==Mode.Explore?"Engage "+a.Name:(action==TargetIntent.Special?Controller.Special:"Attack")+" - "+a.Name;glyph="X";color=Red;
    if(attackPreview is {} p){string attackState=action==TargetIntent.Special?"Action + 1 charge":Controller.Economy.AttackActionStarted?$"Attack {Math.Max(0,Controller.Economy.AttacksRemaining)} remaining in current Attack action":"Attack action";detail=$"{p.HitChance}% hit | {p.MinDamage}-{p.MaxDamage} damage (crit up to {p.CriticalMax})\nApproach {p.MovementFeet:0}/{Controller.Economy.MovementRemainingFeet:0} ft | {attackState}";if(world.ModernTacticalCombat){WorldPoint? tacticalFrom=p.Route.Count>0?p.Route[^1]:null;var modifiers=world.AttackModifiers(Controller,a,tacticalFrom);string roll=modifiers.Advantage==modifiers.Disadvantage?"normal roll":modifiers.Advantage?"advantage":"disadvantage";detail+=$"\nCover +{world.CoverAgainst(Controller,a,tacticalFrom)} AC | {roll}";if(Controller.Range>1&&world.Threatened(Controller,tacticalFrom))detail+=" | close threat";}if(action==TargetIntent.Special&&Controller.SpecialSplashes)detail+=$"\nSplash: {p.Splash.Count} enemies. Allies are safe.";if(!p.Allowed){detail+="\n"+p.Reason;glyph="!";}}
   }
   else{bool helping=action==TargetIntent.Help&&world.Mode==Mode.Combat;bool healing=action==TargetIntent.Heal;label=healing?"Heal "+a.Name:helping?"Help "+a.Name:"Select "+a.Name;glyph=healing?"+":helping?"+":"o";detail=$"HP {a.Hp}/{a.MaxHp} | Armor class {a.Ac}"+(healing?"\nHealing feature | costs Action in combat":helping?"\nHelp: ally within 5 ft gains advantage on next attack | costs Action":"");}
  }
  else if(hoverFeature is {} f){label=f.Name;glyph=f.Kind=="door"?"[]":f.Kind=="npc"?"T":"+";string serviceDetail=f.Kind=="service"?(world.IsStoreFeature(f)?"Merchant service | click to approach and trade":world.IsServiceFeature(f)?$"{ServiceCatalog.Get(f.ServiceId).Category} service | click to approach and request services":"Unavailable service"):"";detail=world.Mode==Mode.Combat?(action==TargetIntent.UseObject?"Use Object: adjacent door | costs Action":"Select USE OBJECT from Standard Actions to manipulate a combat object"):f.Kind=="npc"?$"Dialogue target | {(action==TargetIntent.Talk?"Talk intent armed · ":"")}click to approach and speak":f.Kind=="service"?serviceDetail:f.Locked?"Locked container/object | click to approach and inspect":f.DetectedTrap?"Detected trap | click to approach and inspect":"Click to approach and "+FeatureVerb(f).ToLowerInvariant();}
  else if(hoverGround is { } ground){label="Ground loot";glyph="+";color=Gold;detail=$"{ground.Inventory.Items.Count} stack(s) | Click to approach and open the loot panel";}
  else{label=moveReason==""?"Move selected":"Unavailable";glyph=moveReason==""?">":"!";color=moveReason==""?Teal:Red;detail=moveReason==""?(world.Mode==Mode.Combat?$"Movement {hoverCost:0}/{Controller.Economy.MovementRemainingFeet:0} ft":"Click to move | Drag to select | Right-click for orders"):moveReason;}
  // IIP16: the software cursor is resolved from the same authoritative world/target
  // state that left click uses. Hover prose remains descriptive; cursor art is semantic.
  ui.Cursors.Set(ResolveWorldCursor());
  var lines=detail.Split('\n').SelectMany(line=>Wrap(line,430,.65f)).ToArray();int width=450,height=39+lines.Length*24;var box=new Rectangle(MapRect.Left+13,MapRect.Bottom-height-12,width,height);Box(box,new Color(12,17,20)*.96f,Gold*.55f);Text(label,box.X+10,box.Y+7,Bright,.7f);for(int i=0;i<lines.Length;i++)Text(lines[i],box.X+10,box.Y+34+i*24,Muted,.65f);
 }
 string FeatureVerb(Feature f)=>f.Kind switch{"location"=>"Investigate location","quest-board"=>"Review contracts","encounter"=>"Activate encounter","door"=>"Open door","chest" or "container"=>"Loot container","exit" or "stairs"=>"Use stairs","travel" or "transition"=>"Travel","switch" or "lever" or "button"=>"Use switch","campaign-event"=>"Use object","horde"=>f.Used?"Inspect breach":"Provoke horde","debrief"=>"Report back","shrine"=>"Use shrine","npc"=>"Talk to","service"=>world.IsStoreFeature(f)?"Trade with":"Request service from","trap"=>f.Disarmable?"Disarm trap":"Inspect trap",_=>"Take relic"};

}
