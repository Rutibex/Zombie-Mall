using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 void Shade(){Fill(new(0,0,W,H),Color.Black*.72f);buttons.Add((new(0,0,W,H),()=>{}));}
 void DrawTitle()
 {
  int offset=(W-1440)/2;
  Shade();Panel(new(330+offset,112,780,664));
  Center("ZOMBIE MALL",new(330+offset,140,780,55),Bright,1.05f,true);
  Center(GameIdentity.WindowTitle.Replace("Zombie Mall - ","").ToUpperInvariant(),new(330+offset,201,780,30),Gold,.85f);
  var party=world.Party.ToList();int portraitW=86,portraitH=104,gap=11,total=party.Count*portraitW+Math.Max(0,party.Count-1)*gap;int x=720+offset-total/2;
  foreach(var a in party){var pr=new Rectangle(x,249,portraitW,portraitH);Portrait(a,pr);Border(pr,Gold);x+=portraitW+gap;}
  Center("Find a foothold. Bring everyone and your supplies home.",new(330+offset,376,780,31),Muted,.74f);
  Button(new(425+offset,425,590,48),"START ZOMBIE MALL",NewGame,true,.86f);
  Button(new(425+offset,484,590,48),"CONTINUE",ContinueLatest,false,.86f);
  Button(new(425+offset,543,590,48),"LOAD GAME",()=>OpenSaveBrowser(false,true),false,.86f);
  Button(new(425+offset,602,185,41),"How to play",()=>OpenTitleAuxScreen(UiScreenId.Help));
  Button(new(628+offset,602,185,41),"OPTIONS",()=>OpenTitleAuxScreen(UiScreenId.Settings));
  Button(new(830+offset,602,185,41),"QUIT",Exit);
  Button(new(425+offset,655,590,32),"ORIGINAL ENGINE TEST",BeginCharacterCreation,false,.60f);
  Center($"{BindingLabel(InputAction.QuickSave,false)} Quick Save  /  {BindingLabel(InputAction.QuickLoad,false)} Quick Load  /  Rotating Autosaves",new(330+offset,713,780,23),Gold,.63f);
  Center($"By Michael Robinson   |   {GameIdentity.Name} {GameIdentity.Version}",new(330+offset,742,780,20),Muted,.57f);
 }
 
void DrawEnding()
 {
  if(world.CampaignEnded){DrawCampaignFinaleEnding();return;}
  int offset=(W-1440)/2;
  Shade();Panel(new(362+offset,246,716,390));bool win=world.Mode==Mode.Victory;
  Center(world.Campaign!=null?(win?"EXPEDITION COMPLETE":"EXPEDITION LOST"):win?"DAWN RETURNS":"THE OATH IS BROKEN",new(362+offset,281,716,60),win?Gold:Red,.85f,true);
  Paragraph(world.Campaign!=null?"The expedition has ended. Load a saved game or start a new expedition.":win?"The Ember Heart warms in your hands. Above, the bells ring once, and the dead of Orlan are silent. The covenant steps into the first light of morning.":"Beneath Saint Orlan, the last torch fades. Your journey can begin again, or resume from your last saved game.",407+offset,366,618,Muted,.94f,31);
  Button(new(407+offset,516,280,51),win?"Another journey":"Try again",()=>{if(world.Campaign==null)BeginCharacterCreation();else NewGame();},true,.9f);Button(new(715+offset,516,280,51),"Load saved journey",()=>OpenSaveBrowser(false),false,.9f);
 }
}
