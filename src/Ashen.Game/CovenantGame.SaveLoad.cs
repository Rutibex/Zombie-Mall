using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ashen.Game;

public sealed partial class CovenantGame
{
 List<SaveGameEntry> saveEntries=[];
 string saveSelectedId="";
 int saveListOffset;
 bool saveBrowserFromTitle;
 bool titleAuxFromTitle;
 string saveNameBuffer="",saveOverwriteId="",saveDeleteArmedId="";
 int autosaveObservedTravelCount;
 string autosaveObservedAreaId="";
 bool autosaveObservedEmberHeart;
 readonly Dictionary<string,(Texture2D Texture,DateTime Stamp)> saveThumbnailCache=[];
 const int SaveNameMaximumLength=48;
 bool SaveBrowserActive=>ui.Screens.Active is UiScreenId.SaveGame or UiScreenId.LoadGame;
 bool SaveNameEditorOpen=>ui.Modals.SaveNameOpen;
 bool SaveBrowserSaving=>ui.Screens.Active==UiScreenId.SaveGame;

 void ResetSaveBrowserAfterWorldBind()
 {
  saveBrowserFromTitle=false;titleAuxFromTitle=false;saveNameBuffer=saveOverwriteId=saveDeleteArmedId="";saveEntries=[];saveListOffset=0;InitializeAutosaveTracking();
 }
 void InitializeAutosaveTracking()
 {
  autosaveObservedTravelCount=world.TravelCount;autosaveObservedAreaId=world.CurrentTravelDestinationId;
  autosaveObservedEmberHeart=world.Features.Any(f=>f.Name=="The Ember Heart"&&f.Used);
 }

 void OpenTitleAuxScreen(UiScreenId screen)
 {
  saveBrowserFromTitle=false;titleAuxFromTitle=true;title=false;ui.Screens.Navigate(screen);
 }
 void CloseTitleAuxScreen()
 {
  ui.Screens.ReturnToGame();titleAuxFromTitle=false;title=true;
 }
 void OpenSaveBrowser(bool saving,bool fromTitle=false)
 {
  if(saving&&fromTitle)return;
  CancelInventoryDrag();CancelFormationRightPointer();ui.Portraits.CancelInteraction();ui.Modals.Clear();ui.Loot.Close();ui.Dialogue.Close();ui.Store.Close();ui.Services.Close();ui.Rest.Close();
  saveBrowserFromTitle=fromTitle;if(fromTitle)title=false;saveNameBuffer=saveOverwriteId=saveDeleteArmedId="";RefreshSaveEntries();
  ui.Screens.OpenChild(saving?UiScreenId.SaveGame:UiScreenId.LoadGame);
 }
 void CloseSaveBrowserToTitle()
 {
  CancelSaveNameEditor();ui.Screens.ReturnToGame();saveBrowserFromTitle=false;title=true;saveDeleteArmedId="";
 }
 void RefreshSaveEntries(string preferId="")
 {
  try{saveEntries=saveGames.List().ToList();}
  catch(Exception e){saveEntries=[];Notify("Save browser could not be refreshed: "+e.Message);}
  string wanted=preferId.Length>0?preferId:saveSelectedId;
  if(wanted.Length==0||saveEntries.All(e=>e.Id!=wanted))wanted=saveEntries.FirstOrDefault()?.Id??"";
  saveSelectedId=wanted;EnsureSaveSelectionVisible();
 }
 SaveGameEntry? SelectedSaveEntry=>saveEntries.FirstOrDefault(e=>e.Id==saveSelectedId);
 void SelectSaveEntry(string id){saveSelectedId=id;saveDeleteArmedId="";EnsureSaveSelectionVisible();}
 void EnsureSaveSelectionVisible()
 {
  const int visible=7;int index=saveEntries.FindIndex(e=>e.Id==saveSelectedId);int max=Math.Max(0,saveEntries.Count-visible);saveListOffset=Math.Clamp(saveListOffset,0,max);
  if(index>=0&&index<saveListOffset)saveListOffset=index;else if(index>=saveListOffset+visible)saveListOffset=index-visible+1;
 }
 void MoveSaveSelection(int delta)
 {
  if(saveEntries.Count==0)return;int index=saveEntries.FindIndex(e=>e.Id==saveSelectedId);if(index<0)index=0;else index=Math.Clamp(index+delta,0,saveEntries.Count-1);SelectSaveEntry(saveEntries[index].Id);
 }
 bool HandleSaveBrowserWheel(int wheel)
 {
  if(!SaveBrowserActive||wheel==0)return false;int max=Math.Max(0,saveEntries.Count-7);saveListOffset=Math.Clamp(saveListOffset+(wheel>0?-2:2),0,max);return true;
 }
 void HandleSaveBrowserKeyboard()
 {
  if(!SaveBrowserActive||SaveNameEditorOpen)return;
  if(Pressed(Keys.Escape)){if(saveBrowserFromTitle)CloseSaveBrowserToTitle();else ui.Screens.Back();return;}
  if(Pressed(Keys.Up))MoveSaveSelection(-1);if(Pressed(Keys.Down))MoveSaveSelection(1);if(Pressed(Keys.PageUp))MoveSaveSelection(-7);if(Pressed(Keys.PageDown))MoveSaveSelection(7);if(Pressed(Keys.Home)&&saveEntries.Count>0)SelectSaveEntry(saveEntries[0].Id);if(Pressed(Keys.End)&&saveEntries.Count>0)SelectSaveEntry(saveEntries[^1].Id);
  if(Pressed(Keys.Delete))ArmOrDeleteSelectedSave();
  if(Pressed(Keys.A)){preferences.AutoSaveEnabled=!preferences.AutoSaveEnabled;SavePreferences();Notify($"Autosaves {(preferences.AutoSaveEnabled?"enabled":"disabled")}.");}
  if(SaveBrowserSaving)
  {
   if(Pressed(Keys.N))BeginSaveNameEditor(null);if(Pressed(Keys.O)&&SelectedSaveEntry?.Metadata.Kind==SaveGameKind.Manual)BeginSaveNameEditor(SelectedSaveEntry);
  }
  else if(Pressed(Keys.Enter)&&SelectedSaveEntry is {} selected)LoadSaveEntry(selected);
 }
 void BeginSaveNameEditor(SaveGameEntry? overwrite)
 {
  if(!CanSaveJourney(out var reason)){Notify(reason);return;}
  if(overwrite!=null&&overwrite.Metadata.Kind!=SaveGameKind.Manual){Notify("Only named manual saves can be overwritten from this screen.");return;}
  saveOverwriteId=overwrite?.Id??"";saveNameBuffer=overwrite?.Metadata.Name??$"{world.CurrentAreaName} — Day {world.CampaignDay}";if(saveNameBuffer.Length>SaveNameMaximumLength)saveNameBuffer=saveNameBuffer[..SaveNameMaximumLength];ui.Modals.Open(UiModalId.SaveName);
 }
 void CancelSaveNameEditor(){if(SaveNameEditorOpen)ui.Modals.Close();saveNameBuffer=saveOverwriteId="";}
 void HandleSaveNameKeyboard()
 {
  if(!SaveNameEditorOpen)return;if(Pressed(Keys.Escape)){CancelSaveNameEditor();return;}if(Pressed(Keys.Enter)){CommitManualSave();return;}if(Pressed(Keys.Back)&&saveNameBuffer.Length>0)saveNameBuffer=saveNameBuffer[..^1];
 }
 void SaveBrowserTextInput(TextInputEventArgs e)
 {
  if(!SaveNameEditorOpen)return;char c=e.Character;if(c<' '||c=='\u007f'||saveNameBuffer.Length>=SaveNameMaximumLength)return;saveNameBuffer+=c;
 }
 void CommitManualSave()
 {
  if(!SaveNameEditorOpen)return;if(!CanSaveJourney(out var reason)){Notify(reason);return;}string name=SaveGameRepository.CleanName(saveNameBuffer,"Manual Save");
  try
  {
   bool overwriting=saveOverwriteId.Length>0;var entry=saveGames.SaveManual(world,name,saveOverwriteId);CaptureSaveThumbnail(entry);CancelSaveNameEditor();RefreshSaveEntries(entry.Id);saveSelectedId=entry.Id;Notify(overwriting?"Manual save overwritten.":"Manual save created.");
  }
  catch(Exception e){Notify("Cannot save: "+e.Message);}
 }
 void ArmOrDeleteSelectedSave()
 {
  var entry=SelectedSaveEntry;if(entry==null)return;if(!entry.Deletable){Notify("The legacy journey is read-only in the new save browser.");return;}
  if(saveDeleteArmedId!=entry.Id){saveDeleteArmedId=entry.Id;Notify("Press Delete again or click CONFIRM DELETE to remove this save.");return;}
  try{if(saveGames.Delete(entry)){InvalidateSaveThumbnail(entry.Id);saveDeleteArmedId="";RefreshSaveEntries();Notify("Save deleted.");}else Notify("Save could not be deleted.");}
  catch(Exception e){Notify("Cannot delete save: "+e.Message);}
 }
 void QuickSave()
 {
  // IIP38 invariant remains explicit: no snapshot may be taken mid-conversation.
  if(ui.Dialogue.IsOpen){Notify("Finish the conversation before saving.");return;}
  if(!CanSaveJourney(out var reason)){Notify(reason);return;}
  try{var entry=saveGames.SaveQuick(world);CaptureSaveThumbnail(entry);Notify("Quick save complete.");RefreshSaveEntries(entry.Id);}
  catch(Exception e){world.LogSystem("Cannot quick save: "+e.Message,true);Notify("Quick save failed.");}
 }
 void QuickLoad()
 {
  SaveGameEntry? entry;try{entry=saveGames.QuickSave();}catch(Exception e){Notify("Quick save could not be read: "+e.Message);return;}
  if(entry==null){Notify("No compatible quick save exists.");return;}LoadSaveEntry(entry);
 }
 void ContinueLatest()
 {
  SaveGameEntry? entry;try{entry=saveGames.LatestLoadable();}catch(Exception e){Notify("Saved journeys could not be read: "+e.Message);return;}
  if(entry==null){OpenSaveBrowser(false,true);return;}LoadSaveEntry(entry);
 }
 void LoadSaveEntry(SaveGameEntry entry)
 {
  if(!entry.Loadable){Notify(entry.CompatibilityText);return;}
  try
  {
   bool upgraded=entry.Compatibility==SaveCompatibility.Upgradeable;world=saveGames.Load(entry);Bind();Notify(upgraded?$"Loaded {entry.Metadata.Name}; save data upgraded to v{World.CurrentSaveVersion} in memory.":$"Loaded {entry.Metadata.Name}.");
  }
  catch(Exception e){RefreshSaveEntries(entry.Id);Notify("Cannot load save: "+e.Message);}
 }
 bool AutoSave(string reason)
 {
  if(!preferences.AutoSaveEnabled||!CanSaveJourney(out _))return false;
  try{var entry=saveGames.SaveAuto(world,reason,preferences.AutoSaveSlots);CaptureSaveThumbnail(entry);Notify("Autosaved — "+reason+".");return true;}
  catch(Exception e){world.LogSystem("Autosave failed: "+e.Message,true);return false;}
 }
 void UpdateAutosaveTriggers()
 {
  if(title)return;bool heart=world.Features.Any(f=>f.Name=="The Ember Heart"&&f.Used);bool travelChanged=world.TravelCount!=autosaveObservedTravelCount||world.CurrentTravelDestinationId!=autosaveObservedAreaId;
  if(!preferences.AutoSaveEnabled){autosaveObservedTravelCount=world.TravelCount;autosaveObservedAreaId=world.CurrentTravelDestinationId;autosaveObservedEmberHeart=heart;return;}
  // If arrival immediately launches combat, keep the trigger pending until exploration resumes.
  if(travelChanged&&AutoSave("Arrived at "+world.CurrentAreaName)){autosaveObservedTravelCount=world.TravelCount;autosaveObservedAreaId=world.CurrentTravelDestinationId;}
  if(heart&&!autosaveObservedEmberHeart&&AutoSave("Ember Heart recovered"))autosaveObservedEmberHeart=true;
 }
 void CaptureSaveThumbnail(SaveGameEntry entry)
 {
  if(screenshot!=null||uiCheck)return;
  try{Directory.CreateDirectory(Path.GetDirectoryName(entry.ThumbnailPath)!);using var stream=File.Create(entry.ThumbnailPath);canvas.SaveAsPng(stream,W,H);InvalidateSaveThumbnail(entry.Id);}catch{ /* thumbnails are deliberately optional */ }
 }
 void InvalidateSaveThumbnail(string id)
 {
  if(saveThumbnailCache.Remove(id,out var cached))cached.Texture.Dispose();
 }
 Texture2D? SaveThumbnail(SaveGameEntry entry)
 {
  if(string.IsNullOrWhiteSpace(entry.ThumbnailPath)||!File.Exists(entry.ThumbnailPath))return null;DateTime stamp=File.GetLastWriteTimeUtc(entry.ThumbnailPath);
  if(saveThumbnailCache.TryGetValue(entry.Id,out var cached)&&cached.Stamp==stamp)return cached.Texture;
  InvalidateSaveThumbnail(entry.Id);try{using var stream=File.OpenRead(entry.ThumbnailPath);var texture=Texture2D.FromStream(GraphicsDevice,stream);saveThumbnailCache[entry.Id]=(texture,stamp);return texture;}catch{return null;}
 }
 void DisposeSaveThumbnails(){foreach(var item in saveThumbnailCache.Values)item.Texture.Dispose();saveThumbnailCache.Clear();}

 void DrawSaveGameScreen(Rectangle r)=>DrawSaveBrowser(r,true);
 void DrawLoadGameScreen(Rectangle r)=>DrawSaveBrowser(r,false);
 void DrawSaveBrowser(Rectangle r,bool saving)
 {
  if(saveEntries.Count==0&&saveSelectedId.Length==0)RefreshSaveEntries();
  int x=r.X+28,y=r.Y+82;var list=new Rectangle(x,y,548,477);var detail=new Rectangle(list.Right+18,y,r.Right-(list.Right+46),477);Box(list,new Color(18,20,19),Gold*.38f);Box(detail,new Color(18,20,19),Gold*.38f);
  Text(saving?"NAMED MANUAL SAVES":"SAVED JOURNEYS",list.X+12,list.Y+10,Gold,.52f,true);Text($"{saveEntries.Count} entr{(saveEntries.Count==1?"y":"ies")}",list.Right-92,list.Y+14,Muted,.36f);
  const int rows=7,rowH=58;int start=Math.Clamp(saveListOffset,0,Math.Max(0,saveEntries.Count-rows));
  if(saveEntries.Count==0)Paragraph($"No saved journeys were found. Create a named manual save or use {BindingLabel(InputAction.QuickSave,false)} for the dedicated quick-save slot.",list.X+18,list.Y+64,list.Width-36,Muted,.50f,21);
  for(int row=0;row<rows&&start+row<saveEntries.Count;row++)
  {
   var entry=saveEntries[start+row];var q=new Rectangle(list.X+8,list.Y+39+row*rowH,list.Width-26,rowH-4);bool active=entry.Id==saveSelectedId;Color frame=entry.Loadable?Gold:new Color(115,74,65);Box(q,active?new Color(77,66,43)*.35f:new Color(28,30,28),active?Bright:frame*.48f);
   string name=entry.Metadata.Name.Length>38?entry.Metadata.Name[..35]+"...":entry.Metadata.Name;Text(name,q.X+9,q.Y+5,entry.Loadable?Bright:Muted,.43f,true);
   string when=entry.Metadata.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");Text($"{entry.KindText} · {when}",q.X+9,q.Y+28,Gold,.31f);
   string tag=entry.Compatibility switch{SaveCompatibility.Current=>"CURRENT",SaveCompatibility.Upgradeable=>"UPGRADE",SaveCompatibility.UnsupportedNewer=>"NEWER",_=>"INVALID"};Text(tag,q.Right-72,q.Y+28,entry.Loadable?Teal:Red,.30f,true);
   buttons.Add((q,()=>SelectSaveEntry(entry.Id)));if(q.Contains(cursor))tooltip=$"{entry.Metadata.Name} — {entry.CompatibilityText}. Click to inspect.";
  }
  var selected=SelectedSaveEntry;
  if(selected==null)Paragraph("Select a save on the left to see its campaign metadata and thumbnail.",detail.X+16,detail.Y+18,detail.Width-32,Muted,.48f,20);
  else DrawSaveDetails(detail,selected);

  int by=r.Bottom-67;
  if(saving)
  {
   Button(new(list.X,by,154,36),"NEW SAVE [N]",()=>BeginSaveNameEditor(null),true,.43f);
   bool manual=selected?.Metadata.Kind==SaveGameKind.Manual;SaveBrowserButton(new(list.X+164,by,154,36),"OVERWRITE [O]",manual,()=>BeginSaveNameEditor(selected),"Select a named manual save to overwrite it.");
   SaveBrowserButton(new(list.X+328,by,154,36),saveDeleteArmedId==selected?.Id?"CONFIRM DELETE":"DELETE",selected?.Deletable==true,ArmOrDeleteSelectedSave,"Select a deletable save first.");
  }
  else
  {
   SaveBrowserButton(new(list.X,by,188,36),"LOAD [ENTER]",selected?.Loadable==true,()=>{if(selected!=null)LoadSaveEntry(selected);},selected?.CompatibilityText??"Select a save first.");
   SaveBrowserButton(new(list.X+198,by,154,36),saveDeleteArmedId==selected?.Id?"CONFIRM DELETE":"DELETE",selected?.Deletable==true,ArmOrDeleteSelectedSave,"Select a deletable save first.");
   Button(new(list.X+362,by,120,36),"REFRESH",()=>RefreshSaveEntries(),false,.43f);
  }
  Button(new(detail.X,by,detail.Width/2-5,36),preferences.AutoSaveEnabled?"AUTOSAVES: ON [A]":"AUTOSAVES: OFF [A]",()=>{preferences.AutoSaveEnabled=!preferences.AutoSaveEnabled;SavePreferences();},preferences.AutoSaveEnabled,.38f);
  Button(new(detail.X+detail.Width/2+5,by,detail.Width/2-5,36),$"AUTO SLOTS: {preferences.AutoSaveSlots}",()=>{preferences.AutoSaveSlots=preferences.AutoSaveSlots>=5?1:preferences.AutoSaveSlots+1;SavePreferences();},false,.38f);
  if(SaveNameEditorOpen)DrawSaveNameEditor(r);
 }
 void SaveBrowserButton(Rectangle r,string label,bool enabled,Action action,string disabledReason)
 {
  if(enabled)Button(r,label,action,false,.42f);else{Box(r,new Color(23,25,24),Gold*.13f);Center(label,new(r.X,r.Center.Y-9,r.Width,22),Muted*.42f,.42f);buttons.Add((r,()=>Notify(disabledReason)));}
 }
 void DrawSaveDetails(Rectangle r,SaveGameEntry entry)
 {
  int pad=14;Text(entry.Metadata.Name,r.X+pad,r.Y+12,Bright,.50f,true);Text(entry.KindText,r.X+pad,r.Y+41,Gold,.34f,true);
  var shot=new Rectangle(r.X+pad,r.Y+67,r.Width-pad*2,176);Box(shot,new Color(9,11,12),Gold*.28f);var texture=SaveThumbnail(entry);if(texture!=null)batch.Draw(texture,shot,Color.White*.92f);else Center("NO THUMBNAIL",new(shot.X,shot.Center.Y-9,shot.Width,22),Muted*.55f,.42f);
  int y=shot.Bottom+13;string area=string.IsNullOrWhiteSpace(entry.Metadata.AreaName)?"Unknown area":entry.Metadata.AreaName;Text(area,r.X+pad,y,Bright,.43f,true);y+=27;
  Text(SaveGameRepository.CampaignTimeText(entry.Metadata.CampaignMinutes),r.X+pad,y,Muted,.38f);y+=23;Text(entry.Metadata.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),r.X+pad,y,Muted,.34f);y+=24;
  Text($"Party {entry.Metadata.PartyCount} · Highest level {Math.Max(1,entry.Metadata.HighestLevel)}",r.X+pad,y,Muted,.36f);y+=24;Text($"World save v{entry.Metadata.WorldVersion} · Game supports v{World.MinimumSaveVersion}–v{World.CurrentSaveVersion}",r.X+pad,y,Gold,.34f);y+=25;
  Color compat=entry.Loadable?Teal:Red;Paragraph(entry.CompatibilityText,r.X+pad,y,r.Width-pad*2,compat,.38f,18);y+=42;
  if(!string.IsNullOrWhiteSpace(entry.Metadata.Reason))Paragraph("Checkpoint: "+entry.Metadata.Reason,r.X+pad,y,r.Width-pad*2,Muted,.34f,17);
  if(entry.Error.Length>0)Paragraph("Read error: "+entry.Error,r.X+pad,r.Bottom-58,r.Width-pad*2,Red,.31f,16);
 }
 void DrawSaveNameEditor(Rectangle parent)
 {
  Fill(new(0,0,W,H),Color.Black*.68f);buttons.Add((new(0,0,W,H),()=>{}));var box=new Rectangle(parent.Center.X-300,parent.Center.Y-125,600,250);Panel(box);Center(saveOverwriteId.Length>0?"OVERWRITE MANUAL SAVE":"NAME MANUAL SAVE",new(box.X,box.Y+24,box.Width,34),Bright,.62f,true);
  Paragraph(saveOverwriteId.Length>0?"The selected slot will be replaced safely while its slot identity stays the same.":"Enter a descriptive name. The file name is generated separately, so punctuation is safe.",box.X+30,box.Y+72,box.Width-60,Muted,.46f,19);
  var input=new Rectangle(box.X+32,box.Y+126,box.Width-64,42);Box(input,new Color(11,14,14),Gold*.65f);string shown=saveNameBuffer+(Math.Sin(elapsed*7)>0?"|":"");Text(shown,input.X+12,input.Y+11,Bright,.46f);
  Button(new(box.X+120,box.Bottom-56,150,34),saveOverwriteId.Length>0?"OVERWRITE":"SAVE",CommitManualSave,true,.46f);Button(new(box.Right-270,box.Bottom-56,150,34),"CANCEL",CancelSaveNameEditor,false,.46f);
 }
}
