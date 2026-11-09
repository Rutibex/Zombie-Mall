using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum InputAction
 {
  OpenMap,OpenJournal,OpenInventory,OpenCharacter,OpenSpellbook,OpenAbilities,OpenHelp,ToggleInterface,
  SelectParty1,SelectParty2,SelectParty3,SelectParty4,SelectParty5,SelectParty6,SelectAllParty,FollowLeader,StopSelection,ToggleTactics,
  Attack,OpenBattleAbilities,Heal,Potion,Dodge,EndTurn,Pause,ToggleGrid,
  PanLeft,PanRight,PanUp,PanDown,FocusActor,HighlightInteractables,
  QuickSave,QuickLoad,ToggleMute,ToggleFullscreen,OpenSurvivorClasses,OpenEquipmentCatalogue,OpenWorkshop,OpenContracts
 }
 enum BindingCategory { Screens, Party, Combat, Camera, System }
 readonly record struct KeyChord(Keys Key,bool Control=false,bool Shift=false,bool Alt=false);
 readonly record struct InputBindingDefinition(InputAction Action,BindingCategory Category,string Label,string Help,KeyChord Primary,KeyChord? Secondary=null);

 static readonly InputBindingDefinition[] InputBindings=
 [
  new(InputAction.OpenMap,BindingCategory.Screens,"Area Map","Open or close the current Area Map.",new(Keys.M)),
  new(InputAction.OpenJournal,BindingCategory.Screens,"Journal","Open or close the campaign Journal.",new(Keys.J)),
  new(InputAction.OpenInventory,BindingCategory.Screens,"Inventory","Open or close Inventory for the inspected companion.",new(Keys.I)),
  new(InputAction.OpenCharacter,BindingCategory.Screens,"Character Record","Open or close Character Record.",new(Keys.C),new(Keys.R)),
  new(InputAction.OpenSpellbook,BindingCategory.Screens,"Spellbook","Open or close the spellbook/preparation screen.",new(Keys.B)),
  new(InputAction.OpenAbilities,BindingCategory.Screens,"Abilities","Open or close the class feature and ability page.",new(Keys.K)),
  new(InputAction.OpenSurvivorClasses,BindingCategory.Screens,"Survivor Classes","Choose a survivor class, advance or use class abilities.",new(Keys.F7)),
  new(InputAction.OpenContracts,BindingCategory.Screens,"Refuge Contracts","Review reusable contracts and finite encounter plans.",new(Keys.F12)),
  new(InputAction.OpenWorkshop,BindingCategory.Screens,"Workshop / Barter","From gameplay: repair, craft, dismantle or barter at the refuge.",new(Keys.F6)),
  new(InputAction.OpenEquipmentCatalogue,BindingCategory.Screens,"Equipment Catalogue","Browse the source equipment catalogue and use owned gear.",new(Keys.F8)),
  new(InputAction.OpenHelp,BindingCategory.Screens,"How To Play","Open or close the controls/help page.",new(Keys.F1)),
  new(InputAction.ToggleInterface,BindingCategory.Screens,"Hide / Show Interface","Hide the permanent shell for an unobstructed world view; press again to restore it.",new(Keys.F10)),

  new(InputAction.SelectParty1,BindingCategory.Party,"Select Party 1","Select/inspect portrait slot 1.",new(Keys.D1)),
  new(InputAction.SelectParty2,BindingCategory.Party,"Select Party 2","Select/inspect portrait slot 2.",new(Keys.D2)),
  new(InputAction.SelectParty3,BindingCategory.Party,"Select Party 3","Select/inspect portrait slot 3.",new(Keys.D3)),
  new(InputAction.SelectParty4,BindingCategory.Party,"Select Party 4","Select/inspect portrait slot 4.",new(Keys.D4)),
  new(InputAction.SelectParty5,BindingCategory.Party,"Select Party 5","Select/inspect portrait slot 5.",new(Keys.D5)),
  new(InputAction.SelectParty6,BindingCategory.Party,"Select Party 6","Select/inspect portrait slot 6.",new(Keys.D6)),
  new(InputAction.SelectAllParty,BindingCategory.Party,"Select All","Select all living controllable companions.",new(Keys.G)),
  new(InputAction.FollowLeader,BindingCategory.Party,"Follow Leader","Toggle exploration Follow Leader mode.",new(Keys.O)),
  new(InputAction.StopSelection,BindingCategory.Party,"Stop Selected","Stop movement/orders for the selected companions.",new(Keys.X)),
  new(InputAction.ToggleTactics,BindingCategory.Party,"Party Tactics","Toggle optional party tactics automation.",new(Keys.A,true)),

  new(InputAction.Attack,BindingCategory.Combat,"Basic Attack","Arm the normal hostile attack cursor.",new(Keys.A)),
  new(InputAction.OpenBattleAbilities,BindingCategory.Combat,"Battle Abilities","Open the selected combatant's expandable battlefield ability strip.",new(Keys.S)),
  new(InputAction.Heal,BindingCategory.Combat,"Healing Target","Arm an available healing feature for an ally target.",new(Keys.H)),
  new(InputAction.Potion,BindingCategory.Combat,"Use Potion","Use the selected character's healing potion quick behavior.",new(Keys.P)),
  new(InputAction.Dodge,BindingCategory.Combat,"Dodge","Spend the active player combatant's Action on Dodge.",new(Keys.D)),
  new(InputAction.EndTurn,BindingCategory.Combat,"End Turn","End the active player turn, using the existing unused-Action confirmation policy.",new(Keys.Enter)),
  new(InputAction.Pause,BindingCategory.Combat,"Pause Exploration","Pause/resume exploration. Combat remains turn-based.",new(Keys.Space)),
  new(InputAction.ToggleGrid,BindingCategory.Combat,"Grid Overlay","Toggle the exploration/combat floor grid overlay.",new(Keys.OemTilde)),

  new(InputAction.PanLeft,BindingCategory.Camera,"Pan Left","Hold to pan the world/Area Map left.",new(Keys.Left)),
  new(InputAction.PanRight,BindingCategory.Camera,"Pan Right","Hold to pan the world/Area Map right.",new(Keys.Right)),
  new(InputAction.PanUp,BindingCategory.Camera,"Pan Up","Hold to pan the world/Area Map up.",new(Keys.Up)),
  new(InputAction.PanDown,BindingCategory.Camera,"Pan Down","Hold to pan the world/Area Map down.",new(Keys.Down)),
  new(InputAction.FocusActor,BindingCategory.Camera,"Focus Actor","Center the camera on the inspected/active character.",new(Keys.F)),
  new(InputAction.HighlightInteractables,BindingCategory.Camera,"Highlight / Instant Help","Hold to reveal discovered interactables and force immediate tooltips.",new(Keys.Tab)),

  new(InputAction.QuickSave,BindingCategory.System,"Quick Save","Write the dedicated quick-save slot.",new(Keys.F5)),
  new(InputAction.QuickLoad,BindingCategory.System,"Quick Load","Load the compatible dedicated quick-save slot.",new(Keys.F9)),
  new(InputAction.ToggleMute,BindingCategory.System,"Mute Audio","Toggle persistent master audio mute.",new(Keys.M,true)),
  new(InputAction.ToggleFullscreen,BindingCategory.System,"Fullscreen","Toggle persisted Windowed/Fullscreen display mode.",new(Keys.F11))
 ];

 BindingCategory settingsBindingCategory=BindingCategory.Screens;
 InputAction? rebindingAction;
 int rebindingSlot;
 string bindingStatus="";
 bool interfaceHidden;
 bool KeyBindingCaptureActive=>rebindingAction.HasValue;
 bool ShiftHeld=>keys.IsKeyDown(Keys.LeftShift)||keys.IsKeyDown(Keys.RightShift);
 bool AltHeld=>keys.IsKeyDown(Keys.LeftAlt)||keys.IsKeyDown(Keys.RightAlt);
 bool InterfaceViewportExpanded=>interfaceHidden&&!title&&!UiBlocksWorld;
 Rectangle HiddenWorldViewport=>new(ui.Layout.OuterMargin,ui.Layout.OuterMargin,W-ui.Layout.OuterMargin*2,H-ui.Layout.OuterMargin*2);

 static InputBindingDefinition BindingDefinition(InputAction action)=>InputBindings.First(x=>x.Action==action);
 static string BindingStorageKey(InputAction action,int slot)=>$"{action}.{(slot==0?"Primary":"Secondary")}";
 static string SerializeChord(KeyChord chord)
 {
  var parts=new List<string>(4);if(chord.Control)parts.Add("Ctrl");if(chord.Shift)parts.Add("Shift");if(chord.Alt)parts.Add("Alt");parts.Add(chord.Key.ToString());return string.Join("+",parts);
 }
 static bool TryParseChord(string text,out KeyChord chord)
 {
  chord=default;if(string.IsNullOrWhiteSpace(text))return false;bool ctrl=false,shift=false,alt=false;Keys key=Keys.None;
  foreach(var part in text.Split('+',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries))
  {
   if(part.Equals("Ctrl",StringComparison.OrdinalIgnoreCase)){ctrl=true;continue;}
   if(part.Equals("Shift",StringComparison.OrdinalIgnoreCase)){shift=true;continue;}
   if(part.Equals("Alt",StringComparison.OrdinalIgnoreCase)){alt=true;continue;}
   if(!Enum.TryParse(part,true,out key))return false;
  }
  if(key==Keys.None||IsModifierKey(key))return false;chord=new(key,ctrl,shift,alt);return true;
 }
 static bool IsModifierKey(Keys key)=>key is Keys.LeftControl or Keys.RightControl or Keys.LeftShift or Keys.RightShift or Keys.LeftAlt or Keys.RightAlt;
 static bool IsPlatformReserved(KeyChord chord)=>chord.Key==Keys.Escape||(chord.Alt&&chord.Key==Keys.F4)||chord.Key.ToString().Contains("Windows",StringComparison.OrdinalIgnoreCase);
 static string FriendlyKeyName(Keys key)=>key switch
 {
  Keys.D0=>"0",Keys.D1=>"1",Keys.D2=>"2",Keys.D3=>"3",Keys.D4=>"4",Keys.D5=>"5",Keys.D6=>"6",Keys.D7=>"7",Keys.D8=>"8",Keys.D9=>"9",
  Keys.OemTilde=>"`",Keys.OemPlus=>"+",Keys.OemMinus=>"-",Keys.OemOpenBrackets=>"[",Keys.OemCloseBrackets=>"]",Keys.OemPipe=>"\\",Keys.OemSemicolon=>";",Keys.OemQuotes=>"'",Keys.OemComma=>",",Keys.OemPeriod=>".",Keys.OemQuestion=>"/",
  Keys.Left=>"Left",Keys.Right=>"Right",Keys.Up=>"Up",Keys.Down=>"Down",Keys.Enter=>"Enter",Keys.Space=>"Space",Keys.Tab=>"Tab",_=>key.ToString()
 };
 static string FriendlyChord(KeyChord chord)
 {
  var parts=new List<string>(4);if(chord.Control)parts.Add("Ctrl");if(chord.Shift)parts.Add("Shift");if(chord.Alt)parts.Add("Alt");parts.Add(FriendlyKeyName(chord.Key));return string.Join("+",parts);
 }
 void SanitizeKeyBindings()
 {
  preferences.KeyBindings??=[];
  var validKeys=InputBindings.SelectMany(d=>new[]{BindingStorageKey(d.Action,0),BindingStorageKey(d.Action,1)}).ToHashSet(StringComparer.Ordinal);
  foreach(var stale in preferences.KeyBindings.Keys.Where(k=>!validKeys.Contains(k)).ToArray())preferences.KeyBindings.Remove(stale);
  foreach(var def in InputBindings)
  {
   string p=BindingStorageKey(def.Action,0),s=BindingStorageKey(def.Action,1);
   if(!preferences.KeyBindings.TryGetValue(p,out var primary)||(!string.IsNullOrEmpty(primary)&&!TryParseChord(primary,out _)))preferences.KeyBindings[p]=SerializeChord(def.Primary);
   if(!preferences.KeyBindings.TryGetValue(s,out var secondary)||(!string.IsNullOrEmpty(secondary)&&!TryParseChord(secondary,out _)))preferences.KeyBindings[s]=def.Secondary is {} chord?SerializeChord(chord):"";
  }
 }
 KeyChord? BindingChord(InputAction action,int slot=0)
 {
  string key=BindingStorageKey(action,slot);if(!preferences.KeyBindings.TryGetValue(key,out var value)||string.IsNullOrWhiteSpace(value))return null;return TryParseChord(value,out var chord)?chord:null;
 }
 bool ChordModifiersMatch(KeyChord chord)=>ControlHeld==chord.Control&&ShiftHeld==chord.Shift&&AltHeld==chord.Alt;
 bool ChordPressed(KeyChord chord)=>Pressed(chord.Key)&&ChordModifiersMatch(chord);
 bool ChordHeld(KeyChord chord)=>keys.IsKeyDown(chord.Key)&&ChordModifiersMatch(chord);
 bool BindingPressed(InputAction action)=>BindingChord(action,0) is { } a&&ChordPressed(a)||BindingChord(action,1) is { } b&&ChordPressed(b);
 bool BindingHeld(InputAction action)=>BindingChord(action,0) is { } a&&ChordHeld(a)||BindingChord(action,1) is { } b&&ChordHeld(b);
 string BindingLabel(InputAction action,bool includeSecondary=true)
 {
  var a=BindingChord(action,0);var b=BindingChord(action,1);string first=a is {} x?FriendlyChord(x):"Unbound";
  if(!includeSecondary||b is not {} y)return first;return first+" / "+FriendlyChord(y);
 }
 string PartySlotBindingSummary()=>string.Join(" / ",new[]{InputAction.SelectParty1,InputAction.SelectParty2,InputAction.SelectParty3,InputAction.SelectParty4,InputAction.SelectParty5,InputAction.SelectParty6}.Select(a=>BindingLabel(a,false)));
 void BeginKeyRebind(InputAction action,int slot)
 {
  rebindingAction=action;rebindingSlot=Math.Clamp(slot,0,1);bindingStatus=$"Press a key or chord for {BindingDefinition(action).Label}. Esc cancels; Delete clears.";
 }
 void CancelKeyRebind(string status="Binding change cancelled."){rebindingAction=null;bindingStatus=status;}
 void HandleKeyBindingCapture()
 {
  if(rebindingAction is not { } action)return;
  if(Pressed(Keys.Escape)){CancelKeyRebind();return;}
  if((Pressed(Keys.Delete)||Pressed(Keys.Back))&&!ControlHeld&&!ShiftHeld&&!AltHeld){SetBinding(action,rebindingSlot,null);CancelKeyRebind($"{BindingDefinition(action).Label} slot cleared.");SavePreferences();return;}
  var newlyPressed=keys.GetPressedKeys().Where(k=>!oldKeys.IsKeyDown(k)&&!IsModifierKey(k)).ToArray();if(newlyPressed.Length==0)return;
  var chord=new KeyChord(newlyPressed[0],ControlHeld,ShiftHeld,AltHeld);
  if(IsPlatformReserved(chord)){bindingStatus=$"{FriendlyChord(chord)} is reserved for cancel/platform behavior. Choose another binding.";return;}
  AssignBinding(action,rebindingSlot,chord);SavePreferences();rebindingAction=null;
 }
 void SetBinding(InputAction action,int slot,KeyChord? chord)=>preferences.KeyBindings[BindingStorageKey(action,slot)]=chord is {} value?SerializeChord(value):"";
 void AssignBinding(InputAction action,int slot,KeyChord chord)
 {
  string displaced="";
  foreach(var def in InputBindings)
  {
   for(int s=0;s<2;s++)
   {
    if(def.Action==action&&s==slot)continue;var existing=BindingChord(def.Action,s);if(existing is {} e&&e.Equals(chord)){SetBinding(def.Action,s,null);displaced=def.Label;}
   }
  }
  SetBinding(action,slot,chord);
  bindingStatus=string.IsNullOrEmpty(displaced)?$"{BindingDefinition(action).Label} bound to {FriendlyChord(chord)}.":$"{BindingDefinition(action).Label} bound to {FriendlyChord(chord)}; removed conflicting binding from {displaced}.";
 }
 void ResetKeyBindings(BindingCategory? category=null)
 {
  foreach(var def in InputBindings.Where(d=>category==null||d.Category==category))
  {SetBinding(def.Action,0,def.Primary);SetBinding(def.Action,1,def.Secondary);}
  bindingStatus=category is {} c?$"{c} bindings restored to defaults.":"All keyboard bindings restored to defaults.";rebindingAction=null;SavePreferences();
 }
 void ToggleInterfaceVisibility()
 {
  if(title)return;if(UiBlocksWorld){Notify("Close the active panel before hiding the gameplay interface.");return;}interfaceHidden=!interfaceHidden;Notify(interfaceHidden?$"Interface hidden. Press {BindingLabel(InputAction.ToggleInterface,false)} to restore it.":"Interface restored.");
 }

 void DrawControlsOptions(Rectangle r)
 {
  int x=r.X+18,y=r.Y+13;Text("KEYBOARD BINDINGS",x,y,Gold,.76f);Text("Click a slot, then press a key or modifier chord. Conflicts are reassigned explicitly.",x+205,y+3,Muted,.40f);
  var categories=Enum.GetValues<BindingCategory>();int gap=6,catW=(r.Width-36-gap*(categories.Length-1))/categories.Length;
  for(int i=0;i<categories.Length;i++)
  {
   var c=categories[i];var q=new Rectangle(x+i*(catW+gap),y+30,catW,28);Button(q,c.ToString().ToUpperInvariant(),()=>settingsBindingCategory=c,settingsBindingCategory==c,.40f);
  }
  int rowY=y+70;var defs=InputBindings.Where(d=>d.Category==settingsBindingCategory).ToArray();int labelW=250,slotW=175,rowH=33,rowGap=4;
  Text("COMMAND",x,rowY-19,Gold,.42f);Text("PRIMARY",x+labelW+8,rowY-19,Gold,.42f);Text("SECONDARY",x+labelW+slotW+18,rowY-19,Gold,.42f);
  for(int i=0;i<defs.Length;i++)
  {
   var def=defs[i];int yy=rowY+i*(rowH+rowGap);var row=new Rectangle(x,yy,r.Width-36,rowH);if(i%2==0)Fill(row,new Color(255,255,255)*.025f);Text(def.Label,x+7,yy+8,Bright,.43f);
   for(int slot=0;slot<2;slot++)
   {
    int sx=x+labelW+8+slot*(slotW+10);var q=new Rectangle(sx,yy+2,slotW,rowH-4);var chord=BindingChord(def.Action,slot);string label=chord is {} c?FriendlyChord(c):"UNBOUND";bool active=rebindingAction==def.Action&&rebindingSlot==slot;Button(q,active?"PRESS KEY...":label,()=>BeginKeyRebind(def.Action,slot),active,.42f);
    if(q.Contains(cursor))tooltip=def.Help+" Click to rebind; while listening, Esc cancels and Delete clears the slot.";
   }
  }
  int footer=r.Bottom-71;var resetCategory=new Rectangle(x,footer,220,30);Button(resetCategory,$"RESET {settingsBindingCategory.ToString().ToUpperInvariant()}",()=>ResetKeyBindings(settingsBindingCategory),false,.42f);
  var resetAll=new Rectangle(x+230,footer,180,30);Button(resetAll,"RESET ALL",()=>ResetKeyBindings(),false,.45f);
  Text("Esc is reserved for deterministic cancel/back. Alt+F4 and Windows keys remain platform-reserved.",x,footer+37,Muted,.36f);
  if(!string.IsNullOrWhiteSpace(bindingStatus))Text(bindingStatus,x+425,footer+7,rebindingAction!=null?Bright:Gold,.37f);
 }
}
