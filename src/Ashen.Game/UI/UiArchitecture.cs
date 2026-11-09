using Microsoft.Xna.Framework;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 sealed class UiArchitecture
 {
  readonly CovenantGame g;
  readonly Dictionary<UiScreenId,(string Title,Action<Rectangle> Draw)> screens=[];
  UiScreenId? lastDrawnScreen;
  float screenEnteredAt;
  public ScreenStack Screens {get;}=new();
  public UiLayout Layout {get;}=new();
  public TooltipManager Tooltips {get;}
  public CursorManager Cursors {get;}
  public HighlightManager Highlights {get;}
  public InformationManager Information {get;}
  public ModalManager Modals {get;}=new();
  public HudShell Hud {get;}
  public PortraitRail Portraits {get;}
  public NavigationRail Navigation {get;}
  public ActionBar Actions {get;}
  public TurnControl TurnControl {get;}
  public InitiativeStrip Initiative {get;}
  public MessageLog Messages {get;}
  public ReactionPrompt Reactions {get;}
  public LootPanel Loot {get;}
  public DialoguePanel Dialogue {get;}
  public StorePanel Store {get;}
  public ServicePanel Services {get;}
  public RestPanel Rest {get;}
  public bool BlocksWorldInput=>Screens.IsOpen||Modals.IsOpen||Loot.IsOpen||Dialogue.IsOpen||Store.IsOpen||Services.IsOpen||Rest.IsOpen||g.world.ReactionInterruptActive;

  public UiArchitecture(CovenantGame game)
  {
   g=game;Tooltips=new(game);Cursors=new(game);Highlights=new(game);Information=new(game);
   Portraits=new(game);Navigation=new(game);TurnControl=new(game);Initiative=new(game);Actions=new(game);Messages=new(game);Reactions=new(game);Loot=new(game);Dialogue=new(game);Store=new(game);Services=new(game);Rest=new(game);Dialogue.BindWorld();Store.BindWorld();Services.BindWorld();Hud=new(game);
   Register(UiScreenId.Expedition,"FIRST EXPEDITION",g.DrawExpeditionScreen);
   Register(UiScreenId.Map,"AREA MAP",g.DrawAreaMapScreen);
   Register(UiScreenId.WorldMap,"WORLD MAP",g.DrawWorldMapScreen);
   Register(UiScreenId.Journal,"JOURNAL",g.DrawJournal);
   Register(UiScreenId.Inventory,"INVENTORY",g.DrawInventory);
   Register(UiScreenId.Character,"CHARACTER RECORD",g.DrawCharacter);
   Register(UiScreenId.Biography,"CHARACTER BIOGRAPHY",g.DrawCharacterBiographyScreen);
   Register(UiScreenId.CharacterInformation,"CHARACTER INFORMATION",g.DrawCharacterInformationScreen);
   Register(UiScreenId.LevelUp,"LEVEL UP",g.DrawLevelUpScreen);
   Register(UiScreenId.Spellbook,"SPELLBOOK",g.DrawSpellbook);
   Register(UiScreenId.Abilities,"ABILITIES",g.DrawAbilities);
   Register(UiScreenId.Help,"HOW TO PLAY",g.DrawHelp);
   Register(UiScreenId.Settings,"SETTINGS",g.DrawSettings);
   Register(UiScreenId.Chronicle,"CHRONICLE",g.DrawChronicle);
   Register(UiScreenId.Menu,"OPTIONS",g.DrawMenu);
   Register(UiScreenId.SaveGame,"SAVE GAME",g.DrawSaveGameScreen);
   Register(UiScreenId.LoadGame,"LOAD GAME",g.DrawLoadGameScreen);
  }
  void Register(UiScreenId id,string title,Action<Rectangle> draw)=>screens[id]=(title,draw);
  public void BeginFrame(){Tooltips.BeginFrame();Cursors.BeginFrame();Information.BeginFrame();}
  public void DrawActiveScreen()
  {
   var active=Screens.Active;if(active is null||!screens.TryGetValue(active.Value,out var entry))return;
   if(active!=lastDrawnScreen){lastDrawnScreen=active;screenEnteredAt=g.elapsed;}
   var shade=Layout.CenterBounds;g.Fill(shade,Color.Black*.72f);g.buttons.Add((shade,()=>{}));var r=Layout.ManagementRect;g.Panel(r);
   string heading=entry.Title;
   bool saveFromTitle=g.saveBrowserFromTitle&&(active is UiScreenId.SaveGame or UiScreenId.LoadGame);
   bool auxFromTitle=g.titleAuxFromTitle;
   if(!saveFromTitle&&!auxFromTitle&&Screens.Parent is { } parent&&screens.TryGetValue(parent,out var parentEntry))heading=$"{parentEntry.Title}  >  {entry.Title}";
   float headingScale=.8f;int headingWidth=Math.Max(200,r.Width-350);while(headingScale>.25f&&g.Measure(heading,headingScale,true)>headingWidth)headingScale-=.025f;
   g.Center(heading,new(r.X+240,r.Y+21,headingWidth,50),Bright,headingScale,true);
   float reveal=g.preferences.ReducedMotion?1:Math.Clamp((g.elapsed-screenEnteredAt)/.22f,0,1);
   g.Fill(new Rectangle(r.X+24,r.Y+72,Math.Max(1,(int)((r.Width-48)*reveal)),2),Gold*.55f);
   var close=new Rectangle(r.Right-95,r.Y+20,73,36);
   if(saveFromTitle)g.Button(close,"< TITLE",g.CloseSaveBrowserToTitle,false,.34f);
   else if(auxFromTitle)g.Button(close,"< TITLE",g.CloseTitleAuxScreen,false,.34f);
   else g.Button(close,Screens.HasChild?"< BACK":"X",()=>Screens.Back(),false,Screens.HasChild ? .34f : .72f);
   if(close.Contains(g.cursor))g.tooltip=(saveFromTitle||auxFromTitle)?"Return to the title screen.":Screens.HasChild?"Return to the parent screen.":"Return to the game world.";
   entry.Draw(r);
  }
 }

 string tooltip {get=>ui.Tooltips.Text;set=>ui.Tooltips.Text=value;}
 bool contextOpen
 {
  get=>ui.Modals.ContextOpen;
  set{if(value)ui.Modals.Open(UiModalId.Context);else if(ui.Modals.ContextOpen)ui.Modals.Close();}
 }
 bool UiBlocksWorld=>ui.BlocksWorldInput;
}
