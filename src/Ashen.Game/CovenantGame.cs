using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Text.Json;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame : Microsoft.Xna.Framework.Game
{
 enum TargetIntent { Attack, Special, Heal, Help, UseObject, Spell, Ability, Talk }
 enum ActionBarPage { Primary, StandardActions, Abilities, Spells, SpellSlots, SpellConcentration, QuickSpellAssign }
 int W=1600; const int H=900;
 readonly GraphicsDeviceManager graphics;SpriteBatch batch=null!;RenderTarget2D canvas=null!;
 readonly Dictionary<string,Texture2D> tex=[];Texture2D pixel=null!,circle=null!,stone=null!,paper=null!;
 readonly List<(Rectangle Rect,System.Action Click)> buttons=[];
 readonly RasterizerState clip=new(){ScissorTestEnable=true};
 World world=World.New();
 readonly string campaignPath;Audio? audio;MouseState mouse,oldMouse;KeyboardState keys,oldKeys;Point cursor;
 float[] fontWidths=[],titleWidths=[];float zoom=1,enemyTimer,elapsed;V camera;
 bool paused,grid=false,title=true;TargetIntent action=TargetIntent.Attack;ActionBarPage actionBarPage=ActionBarPage.Primary;
 string endTurnConfirmActorId="";float endTurnConfirmUntil;
 readonly UiArchitecture ui;
 readonly bool uiCheck;
 readonly string? screenshot;readonly string? captureSave;readonly string scene;int frames;Rectangle destination;
 Rectangle MapRect=>InterfaceViewportExpanded?HiddenWorldViewport:ui.Layout.WorldViewport; // historical IIP2 marker: Rectangle MapRect=>ui.Layout.WorldViewport;
 static readonly Color Gold=new(185,150,87),Bright=new(238,215,169),Muted=new(192,190,176),Ink=new(37,30,25),Teal=new(103,185,163),Red=new(193,91,72);
 readonly string profilePath=GameIdentity.ProfilePath;
 readonly string savePath=Path.Combine(GameIdentity.ProfilePath,"journey.json"); // pre-IIP44 legacy slot
 readonly SaveGameRepository saveGames;
 public CovenantGame(string[] args)
 {
  int campaignArg=Array.IndexOf(args,"--campaign");
  campaignPath=campaignArg>=0&&campaignArg+1<args.Length?Path.GetFullPath(args[campaignArg+1]):CampaignLoader.BundledCampaignPath;
  uiCheck=args.Contains("--ui-check");saveGames=new(Path.Combine(profilePath,"Saves"),savePath);ui=new(this);
  graphics=new(this){PreferredBackBufferWidth=1280,PreferredBackBufferHeight=720,SynchronizeWithVerticalRetrace=true};
  Window.Title=GameIdentity.WindowTitle;Window.AllowUserResizing=true;IsMouseVisible=false;Window.ClientSizeChanged+=(_,_)=>resizePending=true;Window.TextInput+=AreaMapTextInput;Window.TextInput+=EquipmentTextInput;
  int p=Array.IndexOf(args,"--screenshot");if(p>=0&&p+1<args.Length)screenshot=args[p+1];
  int captureArg=Array.IndexOf(args,"--capture-save");captureSave=captureArg>=0&&captureArg+1<args.Length?Path.GetFullPath(args[captureArg+1]):null;
  int s=Array.IndexOf(args,"--scene");scene=s>=0&&s+1<args.Length?args[s+1]:"exploration";
 }
 protected override void LoadContent()
 {
  if(scene.StartsWith("fixture",StringComparison.Ordinal))world=World.New(CampaignLoader.Load(CampaignLoader.BundledTravelPath));
  if(scene.StartsWith("env33",StringComparison.Ordinal)||scene.StartsWith("lower-",StringComparison.Ordinal)||scene.StartsWith("finale-",StringComparison.Ordinal)||scene.StartsWith("social-",StringComparison.Ordinal)||scene.StartsWith("quest-",StringComparison.Ordinal)||scene.StartsWith("workshop",StringComparison.Ordinal)||scene.StartsWith("equipment",StringComparison.Ordinal)||scene.StartsWith("classes",StringComparison.Ordinal)||scene.StartsWith("tension",StringComparison.Ordinal)||scene.StartsWith("works-",StringComparison.Ordinal)||scene.StartsWith("shelter",StringComparison.Ordinal)||scene.StartsWith("survival",StringComparison.Ordinal)||scene.StartsWith("horde",StringComparison.Ordinal)||scene.StartsWith("awareness",StringComparison.Ordinal)||scene.StartsWith("tactical",StringComparison.Ordinal)||scene.StartsWith("firearms",StringComparison.Ordinal)||scene.StartsWith("mall",StringComparison.Ordinal)||scene.StartsWith("survivors",StringComparison.Ordinal)||scene.StartsWith("scavenge",StringComparison.Ordinal)||scene.StartsWith("expedition",StringComparison.Ordinal)||(screenshot==null&&!uiCheck))world=World.New(CampaignLoader.Load(campaignPath));
  if((scene.StartsWith("env33",StringComparison.Ordinal)||scene=="expedition-playthrough"||scene=="horde-playthrough"||scene=="survival-playthrough"||scene=="shelter-playthrough"||scene=="works-playthrough"||scene=="tension-playthrough"||scene=="tension-field"||scene=="classes-playthrough"||scene=="equipment-playthrough"||scene=="workshop-playthrough"||scene=="quest-playthrough"||scene=="social-playthrough"||scene.StartsWith("lower-",StringComparison.Ordinal)||scene.StartsWith("finale-",StringComparison.Ordinal))&&captureSave!=null)world=World.Load(captureSave);
  batch=new(GraphicsDevice);canvas=new(GraphicsDevice,W,H);pixel=new(GraphicsDevice,1,1);pixel.SetData([Color.White]);
  foreach(var f in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"Assets"),"*.png")){using var stream=File.OpenRead(f);var texture=Texture2D.FromStream(GraphicsDevice,stream);var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=Color.FromNonPremultiplied(c.R,c.G,c.B,c.A);}texture.SetData(pixels);tex[Path.GetFileNameWithoutExtension(f)]=texture;}
  LoadEnvironmentArt();
  fontWidths=JsonSerializer.Deserialize<float[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Assets/font.json")))!;
  titleWidths=JsonSerializer.Deserialize<float[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Assets/title.json")))!;
  stone=Noise(128,128,false);paper=Noise(128,128,true);circle=Disc(96);for(int i=0;i<4;i++)tex["floor"+i]=FloorTexture(i);foreach(var style in CampaignGeometry.Styles)tex["mall-"+style]=MallFloorTexture(style);
  LoadPreferences();BindReactionPolicies();ui.Loot.BindWorld();ui.Layout.Reflow(W,H,preferences.InterfaceScale);SelectAll();BuildBodyBounds("party_bodies");BuildBodyBounds("enemy_bodies");camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);InitializeAutosaveTracking();
  if(screenshot==null&&!uiCheck){audio=new();ApplyRuntimePreferences();ApplyDisplayPreferences(false);world.Sound=audio.Play;}
  else
  {
   ApplyRuntimePreferences();
   title=scene=="title";if(scene=="creation")BeginCharacterCreation();
   if(scene is "combat" or "inventory"){world.Features.First(f=>f.Kind=="door").Used=true;var party=world.Party.ToArray();Cell[] pos=[new(20,16),new(20,14),new(19,16),new(19,18),new(18,15),new(18,17)];for(int i=0;i<Math.Min(party.Length,pos.Length);i++)party[i].Pos=pos[i];world.Reveal();camera=new(23,15);}
   if(scene=="combat")world.StartCombat(0);
   if(scene=="inventory"){ui.Screens.Navigate(UiScreenId.Inventory);world.AddItem(world.Chosen,"silver");world.AddItem(world.Chosen,"ward");world.AddItem(world.Chosen,"seal");world.AddItem(world.Chosen,"wand-sparks");}
   if(scene=="door"){world.Chosen.Position=new(14.2f,16.5f);world.Reveal();camera=new(13.5f,16.5f);SelectCharacter(0);}
   if(scene=="context"){title=false;camera=new(7.5f,16.5f);contextScreen=new(1020,310);contextPoint=new(9.3f,17.2f);contextOpen=true;BuildContextCommands();}
   if(scene=="settings"){title=false;ui.Screens.Navigate(UiScreenId.Settings);}
   if(scene=="map"){title=false;ui.Screens.Navigate(UiScreenId.Map);}
   if(scene=="worldmap"){title=false;ui.Screens.Navigate(UiScreenId.WorldMap);}
   if(scene=="spellbook"){title=false;SelectCharacter(Math.Min(2,world.PartyCount-1));ui.Screens.Navigate(UiScreenId.Spellbook);}
   if(scene=="abilities"){title=false;ui.Screens.Navigate(UiScreenId.Abilities);}
   if(scene=="biography"){title=false;ui.Screens.Navigate(UiScreenId.Biography);}
   if(scene=="information"){title=false;ui.Screens.Navigate(UiScreenId.CharacterInformation);}
   if(scene=="levelup"){title=false;world.Chosen.Character.Experience=Math.Max(world.Chosen.Character.Experience,300);OpenLevelUp(world.Chosen);}
   if(scene=="options"){title=false;ui.Screens.Navigate(UiScreenId.Menu);}
   if(scene=="savegame"){title=false;OpenSaveBrowser(true);}
   if(scene=="loadgame"){title=false;OpenSaveBrowser(false);}
   if(scene=="large"){title=false;preferences.TextScale=1.2f;preferences.InterfaceScale=1.15f;}
   if(scene=="selection"){title=false;SelectCharacter(0);SelectCharacter(1,true);camera=new(7.5f,16.5f);}
   if(scene=="fixture-service")StageCampaignTravelCapture("service");
   if(scene=="fixture-roof")StageCampaignTravelCapture("roof");
   if(scene=="fixture-return"||scene=="fixture-worldmap")StageCampaignTravelCapture("return");
   if(scene=="fixture-inventory")ui.Screens.Navigate(UiScreenId.Inventory);
   if(scene=="fixture-journal")ui.Screens.Navigate(UiScreenId.Journal);
   if(scene=="fixture-worldmap")ui.Screens.Navigate(UiScreenId.WorldMap);
   if(scene=="fixture-dialogue"){world.Chosen.Pos=new(8,15);world.Reveal();ui.Dialogue.Open(world.Features.First(f=>f.Kind=="npc"));}
   if(scene=="dialogue"){title=false;var npc=world.Features.First(f=>f.Kind=="npc");world.Chosen.Position=new(7.2f,14.8f);world.Reveal();camera=new(8,15);ui.Dialogue.Open(npc);}

  }
  if(scene.StartsWith("equipment",StringComparison.Ordinal))StageEquipmentCapture();
  if(scene.StartsWith("workshop",StringComparison.Ordinal))StageWorkshopCapture();
  if(scene.StartsWith("quest-",StringComparison.Ordinal))StageContractCapture();
  if(scene.StartsWith("social-",StringComparison.Ordinal))StageSocialCapture();
  if(scene.StartsWith("lower-",StringComparison.Ordinal))StageLowerCapture();
  if(scene.StartsWith("finale-",StringComparison.Ordinal)){title=false;camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();if(!world.CampaignEnded)ui.Rest.ShowFinale(scene=="finale-heli"?1:scene=="finale-winter"?2:scene=="finale-crisis"?3:0);}
  if(scene.StartsWith("classes",StringComparison.Ordinal))StageClassesCapture();
  if(scene.StartsWith("tension",StringComparison.Ordinal))StageTensionCapture();
  if(scene.StartsWith("works-",StringComparison.Ordinal))StageWorksCapture();
  if(scene.StartsWith("shelter",StringComparison.Ordinal))StageShelterCapture();
  if(scene.StartsWith("survival",StringComparison.Ordinal))StageSurvivalCapture();
  if(scene.StartsWith("horde",StringComparison.Ordinal)&&scene!="horde-playthrough")StageHordeCapture();
  if(scene=="horde-playthrough"){title=false;camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();}
  if(scene.StartsWith("awareness",StringComparison.Ordinal))StageAwarenessCapture();
  if(scene.StartsWith("tactical",StringComparison.Ordinal))StageTacticalCapture();
  if(scene.StartsWith("firearms",StringComparison.Ordinal))StageFirearmCapture();
  if(scene.StartsWith("mall",StringComparison.Ordinal))StageMallCapture();
  if(scene.StartsWith("survivors",StringComparison.Ordinal))StageSurvivorCapture();
  if(scene.StartsWith("scavenge",StringComparison.Ordinal))StageScavengingCapture();
  if(scene.StartsWith("expedition",StringComparison.Ordinal))StageExpeditionCapture();
  if(screenshot!=null&&scene.StartsWith("env33",StringComparison.Ordinal))StageEnvironmentCapture();
  if(uiCheck){RunControlChecks();Exit();}
 }
 Texture2D Noise(int w,int h,bool parchment)
 {
  var data=new Color[w*h];var rng=new Random(813);for(int y=0;y<h;y++)for(int x=0;x<w;x++)
  {int n=rng.Next(-9,10)+(int)(Math.Sin(x*.14+y*.09)*4);data[y*w+x]=parchment?new Color(181+n,162+n,124+n):new Color(35+n,38+n,37+n);}
  var t=new Texture2D(GraphicsDevice,w,h);t.SetData(data);return t;
 }
 Texture2D Disc(int size){var data=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){float d=V.Distance(new(x,y),new(size/2f,size/2f));data[y*size+x]=Color.White*Math.Clamp(size/2f-d,0,1);}var t=new Texture2D(GraphicsDevice,size,size);t.SetData(data);return t;}
 void BuildTokens(string atlasName,string prefix)
 {
  var atlas=tex[atlasName];var data=new Color[atlas.Width*atlas.Height];atlas.GetData(data);int sw=atlas.Width/2,sh=atlas.Height/2;
  for(int p=0;p<4;p++){const int size=128;var dest=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){var c=data[(p/2*sh+y*sh/size)*atlas.Width+p%2*sw+x*sw/size];float alpha=Math.Clamp(63-V.Distance(new(x,y),new(63.5f)),0,1);dest[y*size+x]=c*alpha;}var t=new Texture2D(GraphicsDevice,size,size);t.SetData(dest);tex[prefix+p]=t;}
 }
 protected override void Update(GameTime gameTime)
 {
  float dt=Math.Min(.1f,(float)gameTime.ElapsedGameTime.TotalSeconds);elapsed+=dt;
  oldMouse=mouse;mouse=Mouse.GetState();oldKeys=keys;keys=Keyboard.GetState();
  UpdateLayout();
  SyncReactionModal();
  bool reactionPrompt=world.PendingReaction!=null;
  float scale=destination.Width/(float)W;
  audio?.Update(dt,title,world.Mode==Mode.Combat);
  if(screenshot!=null){base.Update(gameTime);return;}
  if(KeyBindingCaptureActive){HandleKeyBindingCapture();base.Update(gameTime);return;}
  if(BindingPressed(InputAction.ToggleFullscreen))ToggleFullscreenShortcut();
  if(characterCreationOpen)HandleCharacterCreationKeyboard();
  // Historical IIP45 structural marker retained after IIP46 routing: if(Pressed(Keys.F11))ToggleFullscreenShortcut();
  if(!title&&!reactionPrompt&&!ui.Dialogue.IsOpen&&!ui.Store.IsOpen&&!ui.Services.IsOpen&&!ui.Rest.IsOpen&&!MapNoteEditorOpen&&!JournalNoteEditorOpen)ui.Dialogue.TryOpenQueuedCompanionConversation();
  if(reactionPrompt)
  {
   if(Pressed(Keys.Escape))ResolveReaction(false);
   else if(Pressed(Keys.Enter)||Pressed(Keys.R))ResolveReaction(true);
  }
  else if(ui.Dialogue.IsOpen)ui.Dialogue.HandleKeyboard();
  else if(ui.Store.IsOpen&&ui.Information.IsOpen){if(Pressed(Keys.Escape))ui.Information.Close();}
  else if(ui.Store.IsOpen)ui.Store.HandleKeyboard();
  else if(ui.Services.IsOpen&&ui.Information.IsOpen){if(Pressed(Keys.Escape))ui.Information.Close();}
  else if(ui.Services.IsOpen)ui.Services.HandleKeyboard();
  else if(ui.Rest.IsOpen)ui.Rest.HandleKeyboard();
  else if(SaveNameEditorOpen)HandleSaveNameKeyboard();
  else if(SaveBrowserActive)HandleSaveBrowserKeyboard();
  else if(JournalNoteEditorOpen)HandleJournalNoteKeyboard();
  else if(MapNoteEditorOpen)HandleAreaMapNoteKeyboard();
  else if(characterCreationOpen){}
  else if(Pressed(Keys.Escape))HandleEscapeNavigation();
  HandleManagementFocus();
  int frameWheel=mouse.ScrollWheelValue-oldMouse.ScrollWheelValue;if(ui.Dialogue.IsOpen&&frameWheel!=0)ui.Dialogue.HandleWheel(frameWheel);else if(ui.Store.IsOpen&&frameWheel!=0)ui.Store.HandleWheel(frameWheel);else if(ui.Services.IsOpen&&frameWheel!=0)ui.Services.HandleWheel(frameWheel);else if(SaveBrowserActive&&frameWheel!=0)HandleSaveBrowserWheel(frameWheel);
  if(!reactionPrompt&&!ui.Dialogue.IsOpen&&!ui.Store.IsOpen&&!ui.Services.IsOpen&&!ui.Rest.IsOpen&&!SaveBrowserActive&&!MapNoteEditorOpen&&!JournalNoteEditorOpen&&BindingPressed(InputAction.ToggleMute)&&audio!=null)ToggleAudioMute();
  // Historical IIP45 structural marker retained after IIP46 routing: Pressed(Keys.M)&&ControlHeld ... ToggleAudioMute();
  if(!title&&!reactionPrompt&&!ui.Dialogue.IsOpen&&!ui.Store.IsOpen&&!ui.Services.IsOpen&&!ui.Rest.IsOpen&&!SaveBrowserActive&&!MapNoteEditorOpen&&!JournalNoteEditorOpen)
  {
   int wheel=frameWheel;bool areaMapWheelHandled=wheel!=0&&HandleAreaMapWheel(wheel);bool journalWheelHandled=wheel!=0&&HandleJournalWheel(wheel);bool logWheelHandled=!ui.Screens.IsOpen&&wheel!=0&&ui.Messages.HandleWheel(wheel);
   UpdateAreaMapKeyboard(dt);UpdateJournalKeyboard();
   if(BindingPressed(InputAction.OpenMap))ToggleMap();
   if(BindingPressed(InputAction.OpenInventory))Toggle(UiScreenId.Inventory);if(BindingPressed(InputAction.OpenJournal))Toggle(UiScreenId.Journal);if(BindingPressed(InputAction.OpenCharacter))Toggle(UiScreenId.Character);
   if(BindingPressed(InputAction.OpenSpellbook))Toggle(UiScreenId.Spellbook);if(BindingPressed(InputAction.OpenAbilities))Toggle(UiScreenId.Abilities);if(BindingPressed(InputAction.OpenHelp))Toggle(UiScreenId.Help);
   if(BindingPressed(InputAction.OpenSurvivorClasses)&&world.SurvivorClassesEnabled)ui.Rest.ShowClasses();
   if(BindingPressed(InputAction.OpenContracts)&&world.StoryEnabled)ui.Rest.ShowContracts();
   if(BindingPressed(InputAction.OpenWorkshop)&&world.WorkshopEnabled&&ui.Screens.Active==null)ui.Rest.ShowWorkshop();
   if(BindingPressed(InputAction.OpenEquipmentCatalogue)&&world.EquipmentEnabled)ui.Rest.ShowEquipment();
   if(BindingPressed(InputAction.QuickSave))QuickSave();if(BindingPressed(InputAction.QuickLoad))QuickLoad();
   if(BindingPressed(InputAction.ToggleTactics))ToggleTactics();if(BindingPressed(InputAction.ToggleInterface))ToggleInterfaceVisibility();
   // Party-slot selection remains live on management screens, matching the permanent
   // portrait rail. It changes inspection/selection only and never changes initiative.
   // Historical key-routing markers retained for structural regressions after IIP46 centralizes dispatch:
   // if(Pressed(Keys.M)&&!ControlHeld)ToggleMap();  Pressed(Keys.M)&&ControlHeld
   // IIP8 historical major-screen defaults after IIP46 rebinding: Keys.I Keys.J Keys.C Keys.R Keys.B Keys.K
   // if(Pressed(Keys.F5))QuickSave();if(Pressed(Keys.F9))QuickLoad();
   // if(Pressed(Keys.A)&&ControlHeld)ToggleTactics();  if(Pressed(Keys.A)&&!ControlHeld)action=TargetIntent.Attack;
   // if(Pressed(Keys.X))StopSelection();
   // if(Pressed(Keys.D1))Select(0); if(Pressed(Keys.D2))Select(1); if(Pressed(Keys.D3))Select(2);
   // if(Pressed(Keys.D4))Select(3); if(Pressed(Keys.D5))Select(4); if(Pressed(Keys.D6))Select(5);
   // keys.IsKeyDown(Keys.Left) / keys.IsKeyDown(Keys.Right) / keys.IsKeyDown(Keys.Up) / keys.IsKeyDown(Keys.Down)
   if(!ui.Modals.IsOpen)
   {
    if(BindingPressed(InputAction.SelectParty1))Select(0);if(BindingPressed(InputAction.SelectParty2))Select(1);if(BindingPressed(InputAction.SelectParty3))Select(2);if(BindingPressed(InputAction.SelectParty4))Select(3);if(BindingPressed(InputAction.SelectParty5))Select(4);if(BindingPressed(InputAction.SelectParty6))Select(5);
    if(BindingPressed(InputAction.SelectAllParty))SelectAll();
   }
   if(!UiBlocksWorld)
   {
    if(BindingPressed(InputAction.Pause))
    {
     if(world.Mode==Mode.Combat)Notify($"Combat is turn-based. Use End Turn [{BindingLabel(InputAction.EndTurn,false)}] to advance initiative.");
     // Historical IIP13 validation marker: if(world.Mode==Mode.Combat)Notify("Combat is turn-based. Use End Turn [Enter] to advance initiative.");
     else paused=!paused;
    }
    if(BindingPressed(InputAction.ToggleGrid))grid=!grid;
    if(BindingPressed(InputAction.Attack))action=TargetIntent.Attack;if(BindingPressed(InputAction.OpenBattleAbilities))OpenAbilityActionBar(Controller);if(BindingPressed(InputAction.Heal))action=TargetIntent.Heal;
    if(BindingPressed(InputAction.Potion)&&!paused)world.Potion(Controller);if(BindingPressed(InputAction.Dodge)&&!paused&&world.Active is {Enemy:false} a)world.ExecuteStandardAction(a,StandardActionKind.Dodge);
    if(BindingPressed(InputAction.EndTurn))RequestEndTurn();
    // Historical IIP13 validation marker after IIP46 rebinding: if(Pressed(Keys.Enter))RequestEndTurn();
    if(BindingPressed(InputAction.FocusActor))Focus();if(BindingPressed(InputAction.FollowLeader))FollowParty();if(BindingPressed(InputAction.StopSelection))StopSelection();
    if(BindingHeld(InputAction.PanLeft)||BindingHeld(InputAction.PanRight)||BindingHeld(InputAction.PanUp)||BindingHeld(InputAction.PanDown)||mouse.MiddleButton==ButtonState.Pressed)focusUntil=0;
    if(BindingHeld(InputAction.PanLeft))camera.X-=dt*preferences.KeyboardScrollSpeed;if(BindingHeld(InputAction.PanRight))camera.X+=dt*preferences.KeyboardScrollSpeed;if(BindingHeld(InputAction.PanUp))camera.Y-=dt*preferences.KeyboardScrollSpeed;if(BindingHeld(InputAction.PanDown))camera.Y+=dt*preferences.KeyboardScrollSpeed;
    if(wheel!=0&&!areaMapWheelHandled&&!journalWheelHandled&&!logWheelHandled&&MapRect.Contains(cursor)&&!ui.Messages.Contains(cursor)&&!ui.Initiative.Contains(cursor)){zoom=Math.Clamp(zoom+wheel/120f*.1f,.65f,1.65f);SavePreferences();}
    if(mouse.MiddleButton==ButtonState.Pressed&&oldMouse.MiddleButton==ButtonState.Pressed&&!ui.Messages.Contains(cursor)&&!ui.Initiative.Contains(cursor))camera-=new V(mouse.X-oldMouse.X,mouse.Y-oldMouse.Y)/(54*zoom*scale);
   }
  }
  bool rightDown=mouse.RightButton==ButtonState.Pressed&&oldMouse.RightButton==ButtonState.Released;
  bool rightHeld=mouse.RightButton==ButtonState.Pressed;
  bool rightUp=mouse.RightButton==ButtonState.Released&&oldMouse.RightButton==ButtonState.Pressed;
  if(rightDown&&!reactionPrompt&&!characterCreationOpen)
  {
   if(SaveNameEditorOpen){}
   else if(JournalNoteEditorOpen){}
   else if(MapNoteEditorOpen){}
   else if(ui.Portraits.IsDragging)ui.Portraits.CancelInteraction();
   else if(ui.Information.IsOpen)ui.Information.Close();
   else if(HandleAreaMapRightPress()){}
   else if(CancelTargetingWithRightClick()){}
   else
   {
    bool handled=ui.Portraits.HandleRightPress()||ui.Actions.HandleRightPress(cursor)||ui.Information.HandleRightPress(cursor)||ui.Loot.HandleRightPress(cursor)||ui.Store.HandleRightPress(cursor)||ui.Services.HandleRightPress(cursor)||ui.Rest.HandleRightPress(cursor);
    if(!handled&&!BeginFormationRightPress())OpenContext();
   }
  }
  if(!reactionPrompt)UpdateFormationRightPointer(rightHeld,rightUp);else CancelFormationRightPointer();
  bool leftDown=mouse.LeftButton==ButtonState.Pressed&&oldMouse.LeftButton==ButtonState.Released;
  bool leftHeld=mouse.LeftButton==ButtonState.Pressed;
  bool leftUp=mouse.LeftButton==ButtonState.Released&&oldMouse.LeftButton==ButtonState.Pressed;
  bool lootPointer=ui.Loot.HandlePointer(leftDown,leftHeld,leftUp);
  bool inventoryPointer=!lootPointer&&HandleInventoryPointer(leftDown,leftHeld,leftUp);
  bool portraitPointer=!lootPointer&&!inventoryPointer&&ui.Portraits.HandleLeftPointer(leftDown,leftHeld,leftUp,ExtendSelection);
  bool areaMapPointer=!lootPointer&&!inventoryPointer&&!portraitPointer&&HandleAreaMapLeftPointer(leftDown,leftHeld,leftUp);
  if(!lootPointer&&!inventoryPointer&&!portraitPointer&&!areaMapPointer)HandleLeftPointer(leftDown,leftHeld,leftUp);
  UpdateEdgeScroll(dt);
  if(!title&&!UiBlocksWorld&&!paused)
  {
   if(IsActive&&world.Mode==Mode.Explore)world.AdvanceCampaignSeconds(dt);
   world.UpdateMovement(dt);
   if(world.Mode==Mode.Combat&&world.Active is {Enemy:true} enemy&&!world.IsMoving(enemy)){enemyTimer+=dt;if(enemyTimer>.8f){world.EnemyTurn();enemyTimer=0;action=TargetIntent.Attack;}}
   else enemyTimer=0;
   UpdatePlayerTactics(dt);
  }
  if(!title){world.SyncJournalProgress();UpdateAutosaveTriggers();}
  SyncCombatSelection();UpdateUsability(dt);ClampMallCamera();
  base.Update(gameTime);
 }
 Actor Controller=>world.Mode==Mode.Combat&&world.Active is {Enemy:false} a?a:world.Chosen;
 bool Pressed(Keys key)=>keys.IsKeyDown(key)&&!oldKeys.IsKeyDown(key);
 bool ControlHeld=>keys.IsKeyDown(Keys.LeftControl)||keys.IsKeyDown(Keys.RightControl);
 bool dispatchingUiButton;
 void Toggle(UiScreenId screen){if(ui.Dialogue.IsOpen){Notify("Finish the conversation before opening another screen.");audio?.PlayUi("error");return;}CancelInventoryDrag();CancelFormationRightPointer();ui.Portraits.CancelInteraction();ui.Modals.Clear();ui.Loot.Close();ui.Store.Close();ui.Services.Close();ui.Rest.Close();ui.Screens.ToggleRoot(screen);if(!dispatchingUiButton)audio?.PlayUi(ui.Screens.IsOpen?"page":"back");leftWorldPress=boxDragging=false;}
 void ToggleMap(){if(ui.Dialogue.IsOpen){Notify("Finish the conversation before opening the map.");return;}bool opening=!ui.Screens.IsFamily(UiScreenId.Map);CancelInventoryDrag();CancelFormationRightPointer();ui.Portraits.CancelInteraction();ui.Modals.Clear();ui.Loot.Close();ui.Store.Close();ui.Services.Close();ui.Rest.Close();ui.Screens.ToggleRoot(UiScreenId.Map);if(opening&&ui.Screens.Active==UiScreenId.Map)CenterAreaMapOnParty();leftWorldPress=boxDragging=false;}
 void HandleEscapeNavigation()
 {
  CancelInventoryDrag();CancelFormationRightPointer();ui.Portraits.CancelInteraction();
  if(characterCreationOpen){CreationBack();}
  else if(SaveNameEditorOpen){CancelSaveNameEditor();}
  else if(titleAuxFromTitle){CloseTitleAuxScreen();}
  else if(SaveBrowserActive&&saveBrowserFromTitle){CloseSaveBrowserToTitle();}
  else if(ui.Modals.ReactionOpen||world.PendingReaction!=null)ResolveReaction(false);
  else if(ui.Dialogue.Close()){}
  else if(ui.Information.Close()){}
  else if(ui.Modals.Close()){}
  else if(ui.Loot.Close()){}
  else if(ui.Store.Close()){}
  else if(ui.Services.Close()){}
  else if(ui.Rest.Close()){}
  else if(ui.Screens.IsOpen){ui.Screens.Back();audio?.PlayUi("back");}
  else if(SpellTargetingActive||actionBarPage!=ActionBarPage.Primary||action!=TargetIntent.Attack)
  {ClearArmedSpell();ClearArmedFeatureAbility();pendingSpellPlan=null;quickSpellAssignmentSlot=-1;action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;Notify("Pending targeting/action menu cancelled.");}
  else if(!title)ui.Screens.NavigateRoot(UiScreenId.Menu);
  // The title screen owns its own New/Continue/Quit flow; Escape never starts a game implicitly.
  leftWorldPress=boxDragging=false;
 }
 void Select(int i)=>SelectCharacter(i,ExtendSelection);
 void Focus(){var a=world.Mode==Mode.Combat?world.Active??world.Chosen:world.Chosen;FocusActor(a);}
 void Bind(){world.FinaleOpened=()=>ui.Rest.ShowFinale();world.CampaignAreaChanged=HandleCampaignAreaChanged;world.ExpeditionReported=ShowExpeditionReport;CancelInventoryDrag();CancelFormationRightPointer();ClearArmedSpell();ClearArmedFeatureAbility();pendingSpellPlan=null;quickSpellAssignmentSlot=-1;ui.Loot.BindWorld();ui.Dialogue.BindWorld();ui.Store.BindWorld();ui.Services.BindWorld();ui.Rest.Close();following=false;ResetTacticsPulse();lastHp.Clear();floats.Clear();previousTurn="";levelUpPlan=null;levelUpNotice="";world.ClearReactionInterrupts();world.NormalizeMessageHistory();ApplyRuntimePreferences();world.Sound=audio is null?null:audio.Play;BindReactionPolicies();paused=false;ClearEndTurnConfirmation();ui.Screens.ReturnToGame();title=false;ui.Modals.Clear();ui.Portraits.CancelInteraction();leftWorldPress=boxDragging=false;lastActive="";primaryPartyActorId=world.Chosen.Id;SelectAll();action=TargetIntent.Attack;actionBarPage=ActionBarPage.Primary;activeFormationSlot=0;groupFormation=FormationAtQuickSlot(0);camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);ResetAreaMapState();ResetJournalView(false);worldMapSelectionId=world.CurrentTravelDestinationId;ResetSaveBrowserAfterWorldBind();}
 bool CanSaveJourney(out string reason)
 {
  if(title){reason="A journey must be active before it can be saved.";return false;}
  if(world.Mode!=Mode.Explore&&!world.CampaignEnded){reason="Save while exploring outside combat.";return false;}
  if(world.PendingReaction!=null){reason="Resolve the reaction before saving.";return false;}
  if(ui.Dialogue.IsOpen){reason="Finish the conversation before saving.";return false;}
  if(ui.Store.IsOpen||ui.Services.IsOpen||ui.Rest.IsOpen||ui.Loot.IsOpen){reason="Close the active lower interface before saving.";return false;}
  if(MapNoteEditorOpen||JournalNoteEditorOpen){reason="Finish the active text editor before saving.";return false;}
  reason="";return true;
 }
 // Compatibility aliases retained for older UI call sites while IIP44 routes F5/F9 to dedicated quick slots.
 void Save(){if(ui.Dialogue.IsOpen){Notify("Finish the conversation before saving.");return;}QuickSave();}
 void Load()=>QuickLoad();
 void NewGame()
 {
  try{var next=World.New(CampaignLoader.Load(campaignPath));world=next;Bind();InitializeAutosaveTracking();Notify(world.HasMallGeometry?"West entrance reached.":"Campaign fixture loaded from JSON.");if(world.Campaign?.Expedition!=null)ui.Screens.Navigate(UiScreenId.Expedition);}
  catch(Exception ex) when(ex is IOException or InvalidDataException or System.Text.Json.JsonException or ArgumentException){Notify("Campaign load failed: "+ex.Message);}
 }
 WorldPoint MouseWorld(){var v=(new V(cursor.X,cursor.Y)-new V(MapRect.Center.X,MapRect.Center.Y))/(54*zoom)+camera;return new(v.X,v.Y);}
 Cell MouseCell()=>MouseWorld().Cell;
 V Screen(Cell c)=>Screen(c.Center);
 V Screen(WorldPoint p)=>new V(MapRect.Center.X,MapRect.Center.Y)+(new V(p.X,p.Y)-camera)*(54*zoom);
 protected override void Draw(GameTime gameTime)
 {
  ClampMallCamera();GraphicsDevice.SetRenderTarget(canvas);GraphicsDevice.Clear(new Color(15,18,22));buttons.Clear();ui.BeginFrame();
  batch.Begin(samplerState:SamplerState.LinearClamp);DrawMapBase();batch.End();
  GraphicsDevice.ScissorRectangle=MapRect;batch.Begin(samplerState:SamplerState.LinearClamp,rasterizerState:clip);DrawWorld();DrawWorldGuidance();batch.End();
  if(screenshot!=null&&scene.StartsWith("tactical",StringComparison.Ordinal)&&hoverActor!=null&&!ui.Screens.IsOpen&&!contextOpen){var hover=Screen(hoverActor.Position);cursor=new((int)hover.X,(int)hover.Y);}
  batch.Begin(samplerState:SamplerState.LinearClamp);if(!InterfaceViewportExpanded)DrawHud();DrawExpeditionTracker();DrawLocalAwarenessHud();DrawHordeHud();DrawSurvivalHud();DrawFeedback();if(!title&&!ui.Screens.IsOpen&&!ui.Loot.IsOpen&&!ui.Dialogue.IsOpen&&!ui.Store.IsOpen&&!ui.Services.IsOpen&&!ui.Rest.IsOpen&&!InterfaceViewportExpanded)DrawControlOverlays();if(characterCreationOpen)DrawCharacterCreation();else if(title)DrawTitle();else if(ui.Screens.IsOpen)ui.DrawActiveScreen();else if(world.Mode is Mode.Defeat or Mode.Victory)DrawEnding();ui.Information.Draw();ui.Reactions.Draw();DrawManagementFocus();ui.Cursors.Draw();ui.Tooltips.Draw();
  batch.End();GraphicsDevice.SetRenderTarget(null);GraphicsDevice.Clear(Color.Black);batch.Begin(samplerState:SamplerState.LinearClamp);batch.Draw(canvas,destination,Color.White);batch.End();
  if(screenshot!=null&&++frames>=3){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(screenshot))!);using var f=File.Create(screenshot);canvas.SaveAsPng(f,W,H);Exit();}
  base.Draw(gameTime);
 }
 void DrawMapBase()
 {
  Fill(MapRect,new Color(20,24,29));for(int y=MapRect.Y;y<MapRect.Bottom;y+=128)for(int x=MapRect.X;x<MapRect.Right;x+=128)batch.Draw(stone,new Rectangle(x,y,128,128),new Color(94,104,125));
 }
 void DrawWorld()
 {
  int size=(int)Math.Ceiling(54*zoom);var explored=world.Explored.ToHashSet();var visible=world.Visible.ToHashSet();
  foreach(var p in world.Floors)
  {
   var pos=Screen(p);var r=new Rectangle((int)pos.X-size/2,(int)pos.Y-size/2,size+1,size+1);if(!r.Intersects(MapRect)||!explored.Contains(p))continue;
   bool litNow=visible.Contains(p);float light=litNow?1:(world.PoweredShelterCell(p)||world.EquipmentLitCell(p) ? .55f : .28f);var key=$"floor{Math.Abs(p.X*7+p.Y*11)%4}";
   if(world.HasMallGeometry){r=EnvironmentTileRect(p);batch.Draw(EnvironmentFloor(world.CampaignZoneAt(p)?.FloorStyle??"concourse",p),r,Color.White*light);}
   else {batch.Draw(tex[key],r,Color.White*light);Fill(r,new Color(104,85,56)*(.16f*light));}
   if(grid)Border(r,new Color(11,17,19)*(.4f*light),1);
   foreach(var n in p.Neighbors().Where(n=>!world.IsFloor(n)))
   {
    var edge=n.X<p.X?new Rectangle(r.X,r.Y,7,size):n.X>p.X?new Rectangle(r.Right-7,r.Y,7,size):n.Y<p.Y?new Rectangle(r.X,r.Y,size,9):new Rectangle(r.X,r.Bottom-7,size,7);
    if(world.HasMallGeometry)DrawEnvironmentWall(r,p,n,light);
    else {Fill(edge,new Color(10,14,18));Border(edge,new Color(73,69,60)*light,1);}
   }
   if(!world.HasMallGeometry&&(p.X*11+p.Y*3)%29==0){var q=new Rectangle(r.X+size/4,r.Y+size/4,size/2,size/2);batch.Draw(tex["atlas_crypt_prop1"],q,Color.White*(.7f*light));}
  }
  if(world.EquipmentEnabled)foreach(var p in world.Explored.Where(world.EquipmentSmoke)){var smoke=Screen(p);Fill(new((int)smoke.X-size/2,(int)smoke.Y-size/2,size,size),new Color(111,122,119)*.7f);}
  if(world.HasMallGeometry){DrawEnvironmentDetails(explored,visible);DrawMallProps(explored,visible);}
  foreach(var actor in world.Actors.Where(a=>a.Alive&&world.IsMoving(a)&&(!a.Enemy||visible.Contains(a.Pos))))
  {
   if(world.Routes.TryGetValue(actor.Id,out var route)&&route.Count>0)GroundRing(Screen(route.Last()),9*zoom,actor.Enemy?Red*.5f:Teal*.6f);
  }
  foreach(var f in world.Features.Where(f=>world.IsFeatureDiscovered(f)))
  {
   var v=Screen(f.Pos);float lit=visible.Contains(f.Pos)?1:.3f;int ss=(int)(45*zoom);var r=new Rectangle((int)v.X-ss/2,(int)v.Y-ss/2,ss,ss);if(!r.Intersects(MapRect))continue;
   if(world.HasMallGeometry&&DrawMallFeature(f,v,lit))continue;
   if(f.Kind=="door"){if(!f.Used){Fill(new((int)v.X-8,r.Y,16,r.Height),new Color(121,76,32)*lit);Border(new((int)v.X-8,r.Y,16,r.Height),Gold*lit,2);Text("DOOR",v.X-18,v.Y-46,Gold*lit,.55f);}}
   else if(f.Kind=="exit"){for(int i=0;i<5;i++){Fill(new(r.X+i*3,r.Y+i*7,r.Width-i*6,5),new Color(147,143,128)*lit);}}
   else if(f.Kind=="relic"){if(!f.Used){float glow=preferences.ReducedMotion?0.17f:0.15f+0.04f*MathF.Sin(elapsed*3);batch.Draw(circle,new Rectangle(r.X-14,r.Y-14,r.Width+28,r.Height+28),AccentGold*glow);Icon("Relic",r,AccentGold*lit);}}
   else if(f.Kind=="npc")DrawDialogueNpcWorld(f,v,lit);
   else {batch.Draw(tex[f.Kind=="shrine"?"shrine":"chest"],r,Color.White*(f.Used?.35f:lit));}
  }
  foreach(var pile in world.GroundPiles.Where(p=>p.Inventory.Items.Count>0&&world.Explored.Contains(p.Pos)))
  {
   var v=Screen(pile.Pos);if(!MapRect.Contains(v.ToPoint()))continue;float lit=visible.Contains(pile.Pos)?1:.35f;int s=(int)(28*zoom);
   batch.Draw(circle,new Rectangle((int)v.X-s/2,(int)v.Y-s/4,s,s/2),new Color(122,88,47)*(.72f*lit));
   batch.Draw(circle,new Rectangle((int)v.X-s/5,(int)v.Y-s/3,s/3,s/3),Gold*(.62f*lit));
  }
  if(world.HasMallGeometry)DrawEnvironmentSigns(explored,visible);
  foreach(var a in world.Actors.Where(a=>(!a.Enemy||visible.Contains(a.Pos))&&new Rectangle((int)Screen(a.Position).X-80,(int)Screen(a.Position).Y-170,160,190).Intersects(MapRect)).OrderBy(a=>a.Position.Y))DrawActor(a);
  if(world.HasMallGeometry)DrawMallSigns(explored,visible);
  // A small context label stays attached to the viewport, independent of camera pan.
  string areaTitle=world.CurrentAreaName.ToUpperInvariant();int areaLabelWidth=Math.Clamp((int)Math.Ceiling(Measure(areaTitle,.85f))+26,238,620);float areaTitleScale=.85f*Math.Min(1,(areaLabelWidth-26)/Math.Max(1,Measure(areaTitle,.85f)));
  Box(new(MapRect.X+18,MapRect.Y+(world.Mode==Mode.Combat?84:18),areaLabelWidth,57),new Color(18,22,24)*.92f,Gold*.5f);
  Text(areaTitle,MapRect.X+31,MapRect.Y+(world.Mode==Mode.Combat?90:24),Bright,areaTitleScale);
  Text(world.Campaign==null?"The stolen Ember Heart":world.CurrentLowerMallSite is {} lower?$"{lower.Level.ToUpperInvariant()} LEVEL - LOCATION {lower.DisplayNumber}":world.HasMallGeometry?"LOWER LEVEL - WEST WING":"CONNECTED AREA FIXTURE",MapRect.X+31,MapRect.Y+(world.Mode==Mode.Combat?114:48),Muted,.68f);
  if(paused){Box(new(MapRect.Center.X-100,MapRect.Center.Y-27,200,54),new Color(16,19,20),Gold);Center("PAUSED",new(MapRect.Center.X-100,MapRect.Center.Y-18,200,36),Bright,.8f,true);}
 }
 int PortraitArtIndex(Actor a)
 {
  // IIP5 upgraded the atlas to six frames (3x2). These two fallbacks transparently
  // upgrade pre-IIP5 saves that stored the reused prototype art indices.
  if(a.Id=="rogue"&&a.Portrait==0)return 4;if(a.Id=="bard"&&a.Portrait==1)return 5;
  return ((a.Portrait%6)+6)%6;
 }
 void Portrait(Actor a,Rectangle r)
 {
  if(SurvivorProfile(a)!=null){DrawModernPortrait(a,r);return;}
  var t=tex["portraits"];int art=PortraitArtIndex(a),sw=t.Width/3,sh=t.Height/2;
  Color tint=a.Dead?new Color(62,55,55):a.Unconscious?new Color(100,100,100):Color.White;
  batch.Draw(t,r,new Rectangle(art%3*sw,art/3*sh,sw,sh),tint);
 }
 void Tile(Texture2D texture,Rectangle r,Color tint){for(int y=r.Y;y<r.Bottom;y+=128)for(int x=r.X;x<r.Right;x+=128){int width=Math.Min(128,r.Right-x),height=Math.Min(128,r.Bottom-y);batch.Draw(texture,new Rectangle(x,y,width,height),new Rectangle(0,0,width,height),tint);}}
 Texture2D FloorTexture(int variant)
 {
  const int size=96;var data=new Color[size*size];var rng=new Random(231+variant);for(int y=0;y<size;y++)for(int x=0;x<size;x++)
  {int shade=rng.Next(-6,7)+(int)(5*Math.Sin(x*.04+y*.065))+variant*3;int edge=Math.Min(Math.Min(x,y),Math.Min(size-1-x,size-1-y));if(edge<2)shade-=45;else if(edge<4)shade+=12;data[y*size+x]=new Color(103+shade,99+shade,82+shade);}
  for(int n=0;n<2;n++){int x=rng.Next(12,84),y=rng.Next(12,84);for(int j=0;j<rng.Next(7,27);j++){if(x<2||y<2||x>93||y>93)break;data[y*size+x]=new Color(58,57,49);x+=rng.Next(-1,2);y++;}}
  var t=new Texture2D(GraphicsDevice,size,size);t.SetData(data);return t;
 }
 Color AccentGold=>preferences.HighContrast?new Color(255,226,130):Gold;
 Color AccessibleColor(Color c)
 {
  if(!preferences.HighContrast)return c;if(c==Gold)return AccentGold;if(c==Muted)return new Color(235,235,228);if(c==Teal)return new Color(117,235,211);if(c==Red)return new Color(255,114,92);return c;
 }
 void Fill(Rectangle r,Color c)=>batch.Draw(pixel,r,c);
 void Border(Rectangle r,Color c,int t=1){Fill(new(r.X,r.Y,r.Width,t),c);Fill(new(r.X,r.Bottom-t,r.Width,t),c);Fill(new(r.X,r.Y,t,r.Height),c);Fill(new(r.Right-t,r.Y,t,r.Height),c);}
 void Box(Rectangle r,Color fill,Color border){Fill(r,fill);Border(r,border);}
 void Panel(Rectangle r){Tile(stone,r,Color.White);CovenantFrame(r);}
 void Bar(Rectangle r,float fraction,Color c){Fill(r,new Color(11,14,16));Fill(new(r.X,r.Y,(int)(r.Width*Math.Clamp(fraction,0,1)),r.Height),c);}
 void Line(V a,V b,Color c,int thickness=1){V delta=b-a;batch.Draw(pixel,a,null,c,MathF.Atan2(delta.Y,delta.X),V.Zero,new V(delta.Length(),thickness),SpriteEffects.None,0);}
 static string DisplayText(string text)
 {
  if(text.All(c=>c<128))return text;
  text=text.Replace("’","'").Replace("‘","'").Replace("“","\"").Replace("”","\"").Replace("–","-").Replace("—","-").Replace("…","...").Replace("•","-").Replace("\uf0b7","-").Replace("×","x").Replace("\u00a0"," ");
  return new string(text.Normalize(System.Text.NormalizationForm.FormD).Where(c=>char.GetUnicodeCategory(c)!=System.Globalization.UnicodeCategory.NonSpacingMark).Select(c=>c<128?c:' ').ToArray());
 }
 float Measure(string s,float scale=1,bool heading=false)=>DisplayText(s).Sum(c=>(heading?titleWidths:fontWidths)[Math.Clamp(c-32,0,95)])*scale*(heading?1:preferences.TextScale);
 void Text(string text,float x,float y,Color color,float scale=1,bool heading=false)
 {
  color=AccessibleColor(color);if(!heading)scale*=preferences.TextScale;
  int size=heading?64:32;var widths=heading?titleWidths:fontWidths;float start=x;
  foreach(char c in DisplayText(text)){if(c=='\n'){x=start;y+=(heading?56:28)*scale;continue;}int i=Math.Clamp(c-32,0,95);batch.Draw(tex[heading?"title":"font"],new V(x,y),new Rectangle(i%16*size,i/16*size,size-1,size-1),color,0,V.Zero,scale,SpriteEffects.None,0);x+=widths[i]*scale;}
 }
 void Center(string s,Rectangle r,Color color,float scale=.8f,bool heading=false)=>Text(s,r.Center.X-Measure(s,scale,heading)/2,r.Y,color,scale,heading);
 IEnumerable<string> Wrap(string s,float width,float scale){string line="";foreach(var word in s.Split(' ')){if(Measure(line+word,scale)>width&&line!=""){yield return line.TrimEnd();line="";}line+=word+" ";}if(line!="")yield return line.TrimEnd();}
 void Paragraph(string s,int x,int y,int width,Color c,float scale=.83f,int lineHeight=26){foreach(var line in Wrap(s,width,scale)){Text(line,x,y,c,scale);y+=lineHeight;}}
 void Button(Rectangle r,string label,System.Action click,bool active=false,float scale=.72f)
 {
  while(Measure(label,scale)>r.Width-12&&scale>.45f)scale-=.02f;
  bool hover=r.Contains(cursor);Box(r,active?new Color(57,66,58):hover?new Color(59,54,42):new Color(24,28,28),AccentGold*.35f);CovenantButtonFrame(r,active,hover);DrawButtonGlow(r,label,hover);DrawButtonPress(r);Center(label,new(r.X,r.Center.Y-10,r.Width,24),active||hover?Bright:Muted,scale);buttons.Add((r,click));
 }
 void Icon(string slot,Rectangle r,Color color)
 {
  var c=new V(r.Center.X,r.Center.Y);float z=r.Width/45f;
  if(slot=="Weapon"){Line(c+new V(-13,13)*z,c+new V(13,-13)*z,color,(int)(4*z));Line(c+new V(-12,1)*z,c+new V(-1,12)*z,Gold,(int)(3*z));}
  else if(slot=="Armor"){Line(c+new V(-13,-13)*z,c+new V(13,-13)*z,color,3);Line(c+new V(-13,-13)*z,c+new V(-10,10)*z,color,3);Line(c+new V(13,-13)*z,c+new V(10,10)*z,color,3);Line(c+new V(-10,10)*z,c+new V(0,17)*z,color,3);Line(c+new V(10,10)*z,c+new V(0,17)*z,color,3);}
  else if(slot is "Shield" or "OffHand")
  {Line(c+new V(-13,-13)*z,c+new V(13,-13)*z,color,2);Line(c+new V(-13,-13)*z,c+new V(-11,7)*z,color,2);Line(c+new V(13,-13)*z,c+new V(11,7)*z,color,2);Line(c+new V(-11,7)*z,c+new V(0,17)*z,color,2);Line(c+new V(11,7)*z,c+new V(0,17)*z,color,2);}
  else if(slot is "Consumable" or "Tool")
  {Border(new((int)(c.X-10*z),(int)(c.Y-5*z),(int)(20*z),(int)(20*z)),color,2);Fill(new((int)(c.X-5*z),(int)(c.Y-15*z),(int)(10*z),(int)(9*z)),color);Line(c+new V(-5,5)*z,c+new V(5,5)*z,color,2);}
  else if(slot is "Ammunition" or "Ammo1" or "Ammo2" or "Ammo3")
  {Line(c+new V(-12,13)*z,c+new V(11,-11)*z,color,3);Line(c+new V(4,-11)*z,c+new V(11,-11)*z,color,2);Line(c+new V(11,-11)*z,c+new V(11,-4)*z,color,2);}
  else if(slot is "RingLeft" or "RingRight" or "Accessory" or "Charm")
  {batch.Draw(circle,new Rectangle((int)(c.X-12*z),(int)(c.Y-5*z),(int)(24*z),(int)(22*z)),color);batch.Draw(circle,new Rectangle((int)(c.X-7*z),(int)(c.Y),(int)(14*z),(int)(12*z)),new Color(28,32,32));Fill(new((int)(c.X-4*z),(int)(c.Y-13*z),(int)(8*z),(int)(9*z)),color);}
  else{batch.Draw(circle,new Rectangle((int)c.X-10,(int)c.Y-10,20,20),color);batch.Draw(circle,new Rectangle((int)c.X-5,(int)c.Y-5,10,10),new Color(28,32,32));for(int i=0;i<8;i++){float t=i*MathF.PI/4;Line(c+new V(MathF.Cos(t),MathF.Sin(t))*14,c+new V(MathF.Cos(t),MathF.Sin(t))*20,color,2);}}
 }
 protected override void UnloadContent(){SavePreferences();DisposeSaveThumbnails();audio?.Dispose();foreach(var t in tex.Values)t.Dispose();pixel.Dispose();stone.Dispose();paper.Dispose();circle.Dispose();canvas.Dispose();batch.Dispose();clip.Dispose();base.UnloadContent();}
}
