using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 void PartySwitch(Rectangle r)
 {
  int count=Math.Max(1,world.PartyCount);
  Button(new(r.X+27,r.Y+22,100,32),"< Previous",()=>SelectCharacter((world.Selected+count-1)%count),false,.62f);
  Button(new(r.X+141,r.Y+22,100,32),"Next >",()=>SelectCharacter((world.Selected+1)%count),false,.62f);
 }
 static string DisplayId(string id)
 {
  if(string.IsNullOrWhiteSpace(id))return "None";
  return string.Join(" ",id.Split('-',StringSplitOptions.RemoveEmptyEntries).Select(part=>part.Length==0?part:char.ToUpperInvariant(part[0])+part[1..]));
 }
 static string AbilityName(Ability ability)=>ability.ToString().ToUpperInvariant();
 InformationModel GearInformation(Gear item,string context="",ItemStack? stack=null,Actor? viewer=null)
 {
  bool identified=stack?.Identified??item.IdentifiedByDefault;string visibleName=identified?item.Name:$"Unidentified {item.Kind.ToString().ToLowerInvariant()}";
  string visibleDescription=identified?item.Description:"Its exact properties are concealed until the item is identified.";
  var facts=new List<string>();if(!string.IsNullOrWhiteSpace(context))facts.Add(context);
  facts.Add($"Category: {item.Kind}");facts.Add($"Weight: {item.Weight:0.##} lb each");facts.Add($"Maximum stack: {item.MaxStack}");
  if(stack!=null){facts.Add($"Quantity: {stack.Quantity}");facts.Add($"Stack weight: {stack.Weight:0.##} lb");facts.Add($"Identification: {(stack.Identified?"Identified":"Unidentified")}");if(item.MaxCharges>0&&stack.Identified)facts.Add($"Charges: {stack.Charges}/{item.MaxCharges}");}
  if(stack!=null&&world.Campaign?.SchemaVersion>=5){facts.Add($"Unsecured value: {world.UnsecuredSv(stack)} SV");facts.Add(stack.ResourceUnits.Count==0?"Starting or untracked supply: no resource XP.":"Bring uncredited finds to the shelter stockpile. Previously credited units never award again.");}
  if(identified)
  {
   if(item.EquipSlot!=null)facts.Add($"Slots: {item.SlotSummary()}");
   if(item.TwoHanded)facts.Add("Hands: Two-handed");
   if(item.RequiresAmmunition)facts.Add($"Requires ammunition: {item.AmmunitionType}");
   if(item.IsAmmunition)facts.Add($"Ammunition type: {item.AmmoType}");
   if(item.EffectiveAttackModifier!=0)facts.Add($"Attack modifier: {(item.EffectiveAttackModifier>=0?"+":"")}{item.EffectiveAttackModifier}");
   if(item.EffectiveDamageModifier!=0)facts.Add($"Damage modifier: {(item.EffectiveDamageModifier>=0?"+":"")}{item.EffectiveDamageModifier}");
   if(item.EffectiveArmorModifier!=0)facts.Add($"Armor Class modifier: {(item.EffectiveArmorModifier>=0?"+":"")}{item.EffectiveArmorModifier}");
   if(item.Magical)facts.Add("Magic item: active whenever legally equipped; Hexmaster uses no attunement or global magic-item cap.");
   facts.Add($"Requirements: {item.EffectiveRequirements.Summary()}");if(viewer!=null&&!item.EffectiveRequirements.Allows(viewer,out var reason))facts.Add($"{viewer.Name} cannot equip/use it: {reason}");
   if(item.Value>0)facts.Add($"Base value: {item.Value} gp");
  }
  var inspection=stack!=null&&viewer!=null&&!string.IsNullOrWhiteSpace(stack.InstanceId)?new ItemInspection(viewer.Id,stack.InstanceId):null;
  return new($"item:{item.Id}:{stack?.InstanceId??"definition"}",visibleName,$"{item.Kind} item",visibleDescription,facts,"Right-click or Esc to close",inspection);
 }
 InformationModel FeatureInformation(Actor actor,FeatureEntry feature)
 {
  var facts=new List<string>{$"Source: {feature.Source}",$"Type: {feature.Kind}"};
  if(feature.Tags.Count>0)facts.Add("Tags: "+string.Join(", ",feature.Tags));
  if(!string.IsNullOrWhiteSpace(feature.Mechanics.ResourceId)&&actor.Character.Resource(feature.Mechanics.ResourceId) is {} resource)facts.Add($"Resource: {resource.Name} {resource.Current}/{resource.Maximum} ({resource.Recharge})");
  if(feature.Mechanics.RangeFeet>0)facts.Add($"Range: {feature.Mechanics.RangeFeet} ft");
  if(feature.Mechanics.AttacksPerAction>0)facts.Add($"Attacks per Attack action: {feature.Mechanics.AttacksPerAction}");
  return new($"feature:{actor.Id}:{feature.Id}",feature.Name,$"{feature.Kind} / {feature.Source}",string.IsNullOrWhiteSpace(feature.Description)?"No longer-form description has been authored for this feature yet.":feature.Description,facts);
 }
 InformationModel ResourceInformation(Actor actor,ResourcePool resource)=>new(
  $"resource:{actor.Id}:{resource.Id}",resource.Name,"Character resource",
  $"{resource.Name} is a tracked rechargeable resource on {actor.Name}'s character sheet.",
  new[]{$"Current: {resource.Current}/{resource.Maximum}",$"Recharge: {resource.Recharge}",$"Resource id: {resource.Id}"});
 InformationModel SpellInformation(Actor actor,SpellcastingSource source,string id,string state)
 {
  var slotText=source.Slots.Count==0?"No spell slots tracked for this source.":"Spell slots: "+string.Join(", ",source.Slots.Select(x=>$"L{x.Level} {x.Current}/{x.Maximum}"));
  if(!SpellCatalog.TryGet(id,out var spell))return new($"spell:{actor.Id}:{source.SourceId}:{id}:{state}",DisplayId(id),$"{state} / {source.SourceId}","No authoritative IIP30 spell definition exists for this id.",new[]{$"Spell id: {id}",slotText});
  var facts=new List<string>
  {
   $"Level: {(spell.Level==0?"Cantrip":spell.Level.ToString())} / {spell.School}",$"Casting time: {spell.CastingTime}",$"Range: {spell.RangeText()}",$"Components: {spell.ComponentsText()}",$"Duration: {spell.Duration.Display}",$"Casting ability: {AbilityName(source.CastingAbility)}",$"Spell save DC: {source.SpellSaveDc(actor.Character)}",$"Spell attack: +{source.SpellAttackBonus(actor.Character)}",$"Preparation model: {source.PreparationMode}",slotText
  };
  if(spell.Area.HasArea)facts.Add($"Area: {spell.Area}");if(spell.SavingThrow!=null)facts.Add($"Saving throw: {spell.SavingThrow}");if(spell.SpellAttack)facts.Add("Resolution: spell attack");if(spell.Concentration)facts.Add("Concentration: required");if(spell.Ritual)facts.Add($"Ritual: yes ({(source.CanRitualCast?"source can ritual cast":"source lacks Ritual Casting")})");if(spell.Upcast.HasScaling)facts.Add("Higher levels: "+spell.Upcast.Description);facts.Add($"Spell id: {id}");
  return new($"spell:{actor.Id}:{source.SourceId}:{id}:{state}",spell.Name,$"{state} / {source.SourceId}",spell.Description,facts);
 }
 int DrawSpellChips(Actor actor,SpellcastingSource source,IEnumerable<string> spells,string state,int x,int y,int width)
 {
  int cx=x,cy=y;foreach(var id in spells)
  {
   string label=DisplayId(id);int w=Math.Clamp((int)Measure(label,.45f)+20,78,190);if(cx+w>x+width){cx=x;cy+=31;}
   var chip=new Rectangle(cx,cy,w,26);Box(chip,new Color(20,24,23),Gold*.35f);Text(label,chip.X+8,chip.Y+5,Bright,.45f);
   var info=SpellInformation(actor,source,id,state);ui.Information.Register(chip,info,$"{label} — {state.ToLowerInvariant()}. Right-click for spell information.");
   cx+=w+6;
  }
  return cy+31;
 }
 void DrawAreaMapScreen(Rectangle r)=>DrawAreaMapIip35(r);
 void DrawWorldMapScreen(Rectangle r)=>DrawWorldMapIip36(r);
 void DrawSpellbook(Rectangle r)=>DrawSpellPreparationScreen(r);
 void DrawAbilities(Rectangle r)
 {
  PartySwitch(r);var a=world.Chosen;var c=a.Character;int x=r.X+32,y=r.Y+87;
  Portrait(a,new(x,y,190,224));Border(new(x,y,190,224),Gold,2);Text(a.Name,x,y+237,Bright,.73f,true);Text($"{a.Class}{(string.IsNullOrWhiteSpace(c.SubclassName)?"":" / "+c.SubclassName)}",x,y+273,Gold,.60f);
  int right=x+224;Text("RESOURCES",right,y,Gold,.76f);
  if(c.Resources.Count==0)Text("No rechargeable resources.",right,y+34,Muted,.58f);
  else
  {
   int rx=right;foreach(var resource in c.Resources.Take(4))
   {
    var q=new Rectangle(rx,y+31,145,53);Box(q,new Color(18,21,20),Gold*.35f);Text(resource.Name,q.X+8,q.Y+7,Bright,.48f);Text($"{resource.Current}/{resource.Maximum}  {resource.Recharge}",q.X+8,q.Y+28,Teal,.40f);
    ui.Information.Register(q,ResourceInformation(a,resource),$"{resource.Name}: {resource.Current}/{resource.Maximum}; recharges on {resource.Recharge}. Right-click for details.");rx+=153;
   }
  }
  Text("FEATURES & TRAITS",right,y+104,Gold,.76f);int fy=y+137;int shown=0;
  foreach(var feature in c.Features.OrderByDescending(f=>ClassAbilityRules.IsActionable(f)).Take(7))
  {
   var q=new Rectangle(right,fy,r.Right-right-35,52);Box(q,new Color(18,21,20),Gold*.25f);bool active=ClassAbilityRules.IsActionable(feature);Text((active?"◆ ":"· ")+feature.Name,q.X+10,q.Y+6,active?Bright:Muted,.55f);Text($"{(active?"ACTIVE / REACTION":"PASSIVE")}  ·  {feature.Kind}  /  {feature.Source}",q.X+10,q.Y+28,Gold,.36f);
   string tip=string.IsNullOrWhiteSpace(feature.Description)?feature.Name:feature.Description;
   ui.Information.Register(q,FeatureInformation(a,feature),tip+" Right-click for full details.");
   fy+=58;shown++;
  }
  if(c.Features.Count>shown)Text($"+ {c.Features.Count-shown} more features represented in character data",right,fy+2,Muted,.48f);
  Paragraph($"IIP34: ◆ marks features represented in the expandable battlefield ABILITIES panel. Passive traits remain here for reference. Hover for concise help; right-click for full details. Press the bound battlefield Abilities key ({BindingLabel(InputAction.OpenBattleAbilities,false)}) in the world or click ABILITIES on the action bar to activate/configure powers.",x,r.Bottom-76,r.Width-65,Muted,.54f,18);
 }
 void DrawInventory(Rectangle r)=>DrawInventoryScreen(r);
 void DrawCharacter(Rectangle r)=>DrawCharacterRecordScreen(r);
 void DrawJournal(Rectangle r)=>DrawJournalIip37(r);

 void DrawHelp(Rectangle r)
 {
  int x=r.X+40,y=r.Y+92;
  string[] left=["EXPLORATION",$"Click figure / portrait or party keys {PartySlotBindingSummary()}: select one","Shift/Ctrl portrait: add/remove selection","Double-click portrait: center camera","Right-click portrait: open Inventory","Right-click item/spell/ability: full details","Right-click while targeting: cancel target mode",$"{BindingLabel(InputAction.SelectAllParty,false)}: select all   Drag world: select many",$"Hold {BindingLabel(InputAction.HighlightInteractables,false)}: highlights + immediate tooltip",$"{BindingLabel(InputAction.StopSelection,false)}: stop selected   {BindingLabel(InputAction.FollowLeader,false)}: follow leader"];
  for(int i=0;i<left.Length;i++)Text(left[i],x,y+i*34,i==0?Gold:Muted,i==0?.9f:.79f);
  string[] right=["COMBAT","Enemies trigger d20 + Dexterity initiative","Movement comes from 5e Speed (5 ft = 1 unit)",$"{BindingLabel(InputAction.Attack,false)}: basic attack   {BindingLabel(InputAction.OpenBattleAbilities,false)}: class ability",$"{BindingLabel(InputAction.Heal,false)}: select healing, then click an ally",$"{BindingLabel(InputAction.ToggleTactics,false)}: toggle party Tactics AI",$"{BindingLabel(InputAction.EndTurn,false)}: End Turn; press again if an Action/Extra Attack remains","REACTION PROMPT: Enter/R accept, Esc declines","Opportunity / Shield / Ready / Counterspell / Cutting Words interrupts","Reaction policies + per-character tactics are in Settings"];
  for(int i=0;i<right.Length;i++)Text(right[i],x+490,y+i*34,i==0?Gold:Muted,i==0?.9f:.76f);
  Paragraph($"Permanent rail: {BindingLabel(InputAction.OpenMap,false)} Map, {BindingLabel(InputAction.OpenJournal,false)} Journal, {BindingLabel(InputAction.OpenInventory,false)} Inventory, {BindingLabel(InputAction.OpenCharacter)} Record, {BindingLabel(InputAction.OpenSpellbook,false)} Spellbook, {BindingLabel(InputAction.OpenAbilities,false)} Abilities. Multi-select shows Talk / Attack / Stop / Hold plus five formation slots; right-click a formation slot to reassign it. With 2+ selected, right-drag open terrain to set the destination and final formation facing; the party compresses through tight doors and reforms afterward. Screen edges, bound pan keys, or middle-drag pan. {BindingLabel(InputAction.QuickSave,false)} saves; {BindingLabel(InputAction.QuickLoad,false)} loads; {BindingLabel(InputAction.ToggleMute,false)} mutes audio; {BindingLabel(InputAction.ToggleFullscreen,false)} toggles fullscreen; {BindingLabel(InputAction.ToggleInterface,false)} hides/shows the shell.",x,y+383,950,Bright,.72f,25);
  Text(world.Campaign?.Description??"Reach the southern sanctum, claim the Ember Heart, and return to the entrance.",x,y+493,Gold,.83f);
 }
 void DrawMenu(Rectangle r)
 {
  Center("The journey waits for you.",new(r.X,r.Y+70,r.Width,34),Muted,.88f);
  int x=r.X+315,w=410,y=r.Y+122,h=42,gap=10;
  Button(new(x,y,w,h),"Return to game",()=>ui.Screens.ReturnToGame(),true,.82f);y+=h+gap;
  Button(new(x,y,w,h),"Save Game",()=>OpenSaveBrowser(true),false,.82f);y+=h+gap;
  Button(new(x,y,w,h),"Load Game",()=>OpenSaveBrowser(false),false,.82f);y+=h+gap;
  Button(new(x,y,198,h),$"Quick Save [{BindingLabel(InputAction.QuickSave,false)}]",QuickSave,false,.68f);
  Button(new(x+212,y,198,h),$"Quick Load [{BindingLabel(InputAction.QuickLoad,false)}]",QuickLoad,false,.68f);y+=h+gap;
  // Historical IIP44 label markers after IIP46 dynamic binding labels: "Quick Save [F5]" / "Quick Load [F9]"
  Button(new(x,y,w,h),preferences.AudioMuted?"Enable audio":"Mute audio",ToggleAudioMute,false,.78f);y+=h+gap;
  Button(new(x,y,w,h),"Settings / Controls",()=>ui.Screens.OpenChild(UiScreenId.Settings),false,.78f);y+=h+gap;
  Button(new(x,y,198,h),preferences.AutoSaveEnabled?"Autosaves: ON":"Autosaves: OFF",()=>{preferences.AutoSaveEnabled=!preferences.AutoSaveEnabled;SavePreferences();},false,.63f);
  Button(new(x+212,y,198,h),$"Auto slots: {preferences.AutoSaveSlots}",()=>{preferences.AutoSaveSlots=preferences.AutoSaveSlots>=5?1:preferences.AutoSaveSlots+1;SavePreferences();},false,.63f);y+=h+gap;
  Button(new(x,y,198,42),"Title screen",()=>{ui.Screens.ReturnToGame();title=true;});
  Button(new(x+212,y,198,42),"Quit",Exit);
  Center(audio?.Status??"Audio disabled for capture",new(r.X,r.Y+594,r.Width,22),Gold,.62f);
 }

 void DrawSettings(Rectangle r)
 {
  int x=r.X+48,tabY=r.Y+78,tabGap=8,tabW=(r.Width-96-tabGap*4)/5;
  void CategoryTab(int i,SettingsCategory category,string label)
  {
   var q=new Rectangle(x+i*(tabW+tabGap),tabY,tabW,34);Button(q,label,()=>settingsCategory=category,settingsCategory==category,.58f);
   if(q.Contains(cursor))ui.Tooltips.Offer($"options:category:{category}",$"Open {label.ToLowerInvariant()} options. Changes are saved to your profile immediately.");
  }
  CategoryTab(0,SettingsCategory.Graphics,"GRAPHICS");CategoryTab(1,SettingsCategory.Audio,"AUDIO");CategoryTab(2,SettingsCategory.Gameplay,"GAMEPLAY");CategoryTab(3,SettingsCategory.Accessibility,"ACCESSIBILITY");CategoryTab(4,SettingsCategory.Controls,"CONTROLS");
  var body=new Rectangle(x,tabY+48,r.Width-96,r.Height-151);Box(body,new Color(12,15,16)*.86f,Gold*.22f);
  switch(settingsCategory)
  {
   case SettingsCategory.Graphics:DrawGraphicsOptions(body);break;
   case SettingsCategory.Audio:DrawAudioOptions(body);break;
   case SettingsCategory.Gameplay:DrawGameplayOptions(body);break;
   case SettingsCategory.Accessibility:DrawAccessibilityOptions(body);break;
   case SettingsCategory.Controls:DrawControlsOptions(body);break;
  }
  Button(new(body.Right-250,body.Bottom-37,225,27),"RESTORE DEFAULTS",RestorePreferenceDefaults,false,.48f);
  Text("Profile settings save immediately · world save schema remains v14",body.X+18,body.Bottom-29,Muted,.37f);
 }
 void DrawGraphicsOptions(Rectangle r)
 {
  int x=r.X+24,y=r.Y+20;Text("DISPLAY",x,y,Gold,.83f);Text("Window mode, render target and presentation timing apply immediately.",x+140,y+4,Muted,.43f);
  int w=(r.Width-72)/2;
  var mode=new Rectangle(x,y+40,w,38);Button(mode,$"Display mode: {preferences.DisplayMode}",CycleDisplayMode,false,.61f);if(mode.Contains(cursor))tooltip=$"Switch between resizable windowed mode and fullscreen. {BindingLabel(InputAction.ToggleFullscreen,false)} uses the same persisted setting.";
  var res=new Rectangle(x+w+24,y+40,w,38);var preset=DisplayResolutions[preferences.ResolutionIndex];Button(res,$"Resolution: {preset.Label}",CycleResolution,false,.61f);if(res.Contains(cursor))tooltip="Cycle 1280x720, 1600x900, 1920x1080 and 2560x1440. The logical Infinity shell remains aspect-aware.";
  var vsync=new Rectangle(x,y+88,w,38);Button(vsync,$"Vertical sync: {(preferences.VSync?"ON":"OFF")}",ToggleVSync,preferences.VSync,.61f);if(vsync.Contains(cursor))tooltip="Synchronize presentation with the display refresh rate to reduce tearing.";
  var uiScale=new Rectangle(x+w+24,y+88,w,38);Button(uiScale,$"Interface size: {preferences.InterfaceScale*100:0}%",CycleInterfaceScale,false,.61f);if(uiScale.Contains(cursor))tooltip="Scale the permanent rails and bottom shell from 85% through 130% without stretching the game world.";
  var zoomReset=new Rectangle(x,y+136,w,38);Button(zoomReset,"Reset world zoom / center",()=>{zoom=1;Focus();SavePreferences();},false,.58f);if(zoomReset.Contains(cursor))tooltip="Return world zoom to 100% and center on the inspected/active character.";
  var actual=GraphicsDevice.PresentationParameters;Box(new(x+w+24,y+136,w,88),new Color(19,23,23),Gold*.20f);Text("CURRENT OUTPUT",x+w+38,y+147,Gold,.52f);Text($"Backbuffer {actual.BackBufferWidth} x {actual.BackBufferHeight}",x+w+38,y+171,Bright,.45f);Text($"Logical canvas {W} x {H}",x+w+38,y+192,Muted,.41f);
  Paragraph("Resolution changes alter the physical backbuffer. Zombie Mall still renders through the resolution-independent IIP2 logical canvas, so ultrawide displays gain world space instead of stretching UI art.",x,y+250,r.Width-48,Muted,.49f,22);
  Text("SHORTCUT",x,y+348,Gold,.58f);Text($"{BindingLabel(InputAction.ToggleFullscreen,false)} toggles Windowed / Fullscreen and updates this saved preference.",x+104,y+350,Bright,.47f);
 }
 void DrawAudioOptions(Rectangle r)
 {
  int x=r.X+24,y=r.Y+20;Text("AUDIO MIX",x,y,Gold,.83f);Text(audio?.Status??"Audio unavailable in capture/test mode",x+160,y+4,Muted,.43f);
  var mute=new Rectangle(x,y+40,230,36);Button(mute,preferences.AudioMuted?"AUDIO: MUTED":"AUDIO: ENABLED",ToggleAudioMute,!preferences.AudioMuted,.56f);
  void VolumeRow(int row,string key,string label,float value,string help)
  {
   int yy=y+94+row*58;Text(label, x,yy+8,Bright,.55f);Rectangle minus=new Rectangle(x+250,yy,48,34),read=new Rectangle(x+306,yy,150,34),plus=new Rectangle(x+464,yy,48,34);
   Button(minus,"-",()=>AdjustVolume(key,-.1f),false,.75f);Button(read,$"{value*100:0}%",()=>{},false,.60f);Button(plus,"+",()=>AdjustVolume(key,.1f),false,.75f);
   if(new Rectangle(x,yy,r.Width-48,36).Contains(cursor))tooltip=help;
  }
  VolumeRow(0,"master","Master volume",preferences.MasterVolume,"Scales every Zombie Mall audio category after mute state.");
  VolumeRow(1,"music","Music",preferences.MusicVolume,"Scales the generated ambient and combat score independently of sound effects.");
  VolumeRow(2,"effects","World effects",preferences.EffectsVolume,"Scales combat, spell, healing and loot sound effects.");
  VolumeRow(3,"ui","Interface",preferences.UiVolume,"Scales menus, portraits, inventory and turn notifications.");
  VolumeRow(4,"voices","Dialogue voices",preferences.VoiceVolume,"Reserved for spoken dialogue; portrait acknowledgments use Interface volume.");
  Button(new(x,y+390,250,34),"TEST WORLD EFFECT",()=>audio?.Play("loot"),false,.55f);
  Button(new(x+270,y+390,250,34),"TEST INTERFACE",()=>audio?.PlayUi("turn"),false,.55f);
  Paragraph($"{BindingLabel(InputAction.ToggleMute,false)} and the Options mute button share persistent master mute. Category levels are clamped from 0% to 100% in 10% steps.",x,y+440,r.Width-48,Muted,.49f,22);
 }
 void DrawGameplayOptions(Rectangle r)
 {
  int x=r.X+20,y=r.Y+16,col=(r.Width-60)/2;var profile=DifficultyRules.Profile(preferences.Difficulty);
  Text("RULES & CAMERA",x,y,Gold,.78f);
  var difficulty=new Rectangle(x,y+34,col,34);Button(difficulty,$"Difficulty: {profile.Name}",CycleDifficulty,false,.58f);if(difficulty.Contains(cursor))tooltip=profile.ExactRuleText;
  Paragraph(profile.ExactRuleText,x,y+74,col,Muted,.41f,18);
  var confirm=new Rectangle(x,y+130,col,32);Button(confirm,preferences.ConfirmEndTurn?"Unused Action warning: ON":"Unused Action warning: OFF",()=>{preferences.ConfirmEndTurn=!preferences.ConfirmEndTurn;ClearEndTurnConfirmation();SavePreferences();},preferences.ConfirmEndTurn,.48f);
  var autosave=new Rectangle(x,y+169,col/2-5,32);Button(autosave,preferences.AutoSaveEnabled?"Autosaves: ON":"Autosaves: OFF",()=>{preferences.AutoSaveEnabled=!preferences.AutoSaveEnabled;SavePreferences();},preferences.AutoSaveEnabled,.48f);
  var autoslots=new Rectangle(x+col/2+5,y+169,col/2-5,32);Button(autoslots,$"Auto slots: {preferences.AutoSaveSlots}",()=>{preferences.AutoSaveSlots=preferences.AutoSaveSlots>=5?1:preferences.AutoSaveSlots+1;SavePreferences();},false,.48f);
  var textSpeed=new Rectangle(x,y+208,col,32);Button(textSpeed,$"Dialogue text: {DialogueTextSpeedName()} ({preferences.DialogueTextSpeed:0} char/s)",CycleDialogueTextSpeed,false,.47f);
  var log=new Rectangle(x,y+247,col,32);Button(log,preferences.LogVerbosity==MessageVerbosity.Detailed?"Combat log: Detailed":"Combat log: Concise",()=>{preferences.LogVerbosity=preferences.LogVerbosity==MessageVerbosity.Detailed?MessageVerbosity.Concise:MessageVerbosity.Detailed;SavePreferences();},false,.46f);
  var tip=new Rectangle(x,y+286,col,32);Button(tip,$"Tooltip delay: {preferences.TooltipDelayMs} ms",CycleTooltipDelay,false,.48f);

  int rx=x+col+20;Text("CAMERA",rx,y+34,Gold,.61f);
  Button(new(rx,y+62,col,32),preferences.EdgeScroll?"Outside-area scrolling: ON":"Outside-area scrolling: OFF",()=>{preferences.EdgeScroll=!preferences.EdgeScroll;SavePreferences();},preferences.EdgeScroll,.46f);
  Button(new(rx,y+101,col,32),$"Pointer scroll speed: {preferences.ScrollSpeed:0}",()=>{preferences.ScrollSpeed=preferences.ScrollSpeed>=22?4:preferences.ScrollSpeed+3;SavePreferences();},false,.48f);
  Button(new(rx,y+140,col,32),$"Keyboard pan speed: {preferences.KeyboardScrollSpeed:0}",()=>{preferences.KeyboardScrollSpeed=preferences.KeyboardScrollSpeed>=24?4:preferences.KeyboardScrollSpeed+4;SavePreferences();},false,.48f);
  Button(new(rx,y+179,col,32),preferences.AutoFocus?"Automatic turn camera: ON":"Automatic turn camera: OFF",()=>{preferences.AutoFocus=!preferences.AutoFocus;SavePreferences();},preferences.AutoFocus,.46f);
  Button(new(rx,y+218,col,32),preferences.TacticsEnabled?"Global party tactics: ON":"Global party tactics: OFF",ToggleTactics,preferences.TacticsEnabled,.46f);

  // Keep the frequently changed tactical policies visible without requiring another child screen.
  Text("REACTION INTERRUPTS",x,y+326,Gold,.57f);Text("ASK / AUTO / NEVER",x+197,y+330,Muted,.37f);
  var reactionKinds=new[]{ReactionKind.OpportunityAttack,ReactionKind.Shield,ReactionKind.ReadiedAction,ReactionKind.Counterspell,ReactionKind.CuttingWords};
  int reactionW=(r.Width-40-(reactionKinds.Length-1)*6)/reactionKinds.Length;
  for(int i=0;i<reactionKinds.Length;i++)
  {
   var kind=reactionKinds[i];var q=new Rectangle(x+i*(reactionW+6),y+350,reactionW,30);Button(q,$"{ReactionLabel(kind)}: {ReactionPolicyText(ReactionPolicyFor(kind))}",()=>CycleReactionPolicy(kind),false,.31f);
   if(q.Contains(cursor))tooltip="ASK opens the reaction prompt; AUTO accepts when legal/useful; NEVER declines. Enemy reactions remain automatic.";
  }

  Text("PARTY TACTICS PROFILES",x,y+391,Gold,.53f);var party=world.Party.ToArray();int partyCount=Math.Min(6,party.Length);
  if(partyCount>0)
  {
   int bw=(r.Width-40-(partyCount-1)*6)/partyCount;
   for(int i=0;i<partyCount;i++)
   {
    var actor=party[i];var q=new Rectangle(x+i*(bw+6),y+415,bw,29);Button(q,$"{actor.Name}: {World.TacticsProfileName(actor.Tactics)}",()=>CycleTacticsProfile(actor),false,.29f);
    if(q.Contains(cursor))tooltip=World.TacticsProfileHint(actor.Tactics)+" Profile is journey state; the global tactics switch is a profile preference.";
   }
  }
 }

 void DrawAccessibilityOptions(Rectangle r)
 {
  int x=r.X+24,y=r.Y+20,w=(r.Width-72)/2;Text("READABILITY & MOTION",x,y,Gold,.83f);
  var textSize=new Rectangle(x,y+42,w,38);Button(textSize,$"Text size: {preferences.TextScale*100:0}%",CycleTextScale,false,.61f);if(textSize.Contains(cursor))tooltip="Cycle body text through 100%, 110%, 120% and 130%. Text measurement and wrapping use the same scale.";
  var uiSize=new Rectangle(x+w+24,y+42,w,38);Button(uiSize,$"Interface size: {preferences.InterfaceScale*100:0}%",CycleInterfaceScale,false,.61f);if(uiSize.Contains(cursor))tooltip="Scale fixed UI chrome while preserving a usable center viewport.";
  var contrast=new Rectangle(x,y+92,w,38);Button(contrast,preferences.HighContrast?"High contrast: ON":"High contrast: OFF",()=>{preferences.HighContrast=!preferences.HighContrast;SavePreferences();},preferences.HighContrast,.61f);if(contrast.Contains(cursor))tooltip="Increase gold/text luminance and selection contrast without relying on color alone for state labels.";
  var motion=new Rectangle(x+w+24,y+92,w,38);Button(motion,preferences.ReducedMotion?"Reduced motion: ON":"Reduced motion: OFF",()=>{preferences.ReducedMotion=!preferences.ReducedMotion;floats.Clear();SavePreferences();},preferences.ReducedMotion,.61f);if(motion.Contains(cursor))tooltip="Disable camera easing, moving combat floaters and decorative pulsing while preserving the underlying information.";
  var message=new Rectangle(x,y+142,w,38);Button(message,$"Message window: {ui.Messages.HeightMode}",()=>ui.Messages.CycleHeight(),false,.58f);if(message.Contains(cursor))tooltip="Cycle collapsed, standard and expanded combat/message log heights.";
  var tooltipDelay=new Rectangle(x+w+24,y+142,w,38);Button(tooltipDelay,$"Tooltip delay: {preferences.TooltipDelayMs} ms",CycleTooltipDelay,false,.58f);if(tooltipDelay.Contains(cursor))tooltip=$"Set hover-help delay from immediate to 1.2 seconds. Hold {BindingLabel(InputAction.HighlightInteractables,false)} to force help immediately.";
  var dialogueSpeed=new Rectangle(x,y+192,w,38);Button(dialogueSpeed,$"Dialogue text: {DialogueTextSpeedName()}",CycleDialogueTextSpeed,false,.58f);if(dialogueSpeed.Contains(cursor))tooltip="Choose instant, slow, normal or fast NPC text reveal. Enter reveals the full current line immediately.";
  var log=new Rectangle(x+w+24,y+192,w,38);Button(log,preferences.LogVerbosity==MessageVerbosity.Detailed?"Combat math: Detailed":"Combat math: Concise",()=>{preferences.LogVerbosity=preferences.LogVerbosity==MessageVerbosity.Detailed?MessageVerbosity.Concise:MessageVerbosity.Detailed;SavePreferences();},false,.58f);
  var persistent=new Rectangle(x,y+242,w,38);Button(persistent,preferences.PersistentTooltips?"Persistent help: ON":"Persistent help: OFF",()=>{preferences.PersistentTooltips=!preferences.PersistentTooltips;SavePreferences();},preferences.PersistentTooltips,.56f);if(persistent.Contains(cursor))tooltip="Keep the last tooltip visible for four seconds after moving the pointer away.";
  var largeCursor=new Rectangle(x+w+24,y+242,w,38);Button(largeCursor,preferences.LargeCursor?"Large cursor: ON":"Large cursor: OFF",()=>{preferences.LargeCursor=!preferences.LargeCursor;SavePreferences();},preferences.LargeCursor,.56f);if(largeCursor.Contains(cursor))tooltip="Enlarge the pointer and its context badge without changing its precise click hotspot.";
  Paragraph("High contrast preserves text and glyph cues. Reduced motion removes decorative animation. Dialogue remains fully written; instant reveal and persistent help improve reading comfort.",x,y+292,r.Width-48,Muted,.50f,23);
  Text("QUICK HELP",x,y+365,Gold,.58f);Text($"F6 / Shift+F6: move button focus · Enter: activate · {BindingLabel(InputAction.HighlightInteractables,false)}: highlights",x,y+392,Bright,.45f);
 }

 void DrawChronicle(Rectangle r)=>ui.Messages.DrawChronicle(r);
}
