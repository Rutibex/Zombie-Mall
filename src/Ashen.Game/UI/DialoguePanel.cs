using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 void DrawDialogueNpcWorld(Feature feature,V foot,float light)
 {
  int h=Math.Max(42,(int)(62*zoom)),w=(int)(46*zoom);var r=new Rectangle((int)foot.X-w/2,(int)foot.Y-h,w,h);var t=tex["portraits"];int art=Math.Clamp(feature.DialoguePortrait,0,5),sw=t.Width/3,sh=t.Height/2;
  batch.Draw(circle,new Rectangle((int)(foot.X-20*zoom),(int)(foot.Y-6*zoom),(int)(40*zoom),(int)(12*zoom)),Color.Black*.45f);
  batch.Draw(t,r,new Rectangle(art%3*sw,art/3*sh,sw,sh),Color.White*light);Border(r,Teal*(.72f*light),2);
  GroundRing(foot,22*zoom,Teal*(.70f*light));
 }

 /// <summary>
 /// IIP38-IIP40 lower-HUD conversation mode. IIP40 adds data-driven companion
 /// interjections, speaker overrides, approval/flag consequences, conditional choices,
 /// party arguments and queued companion-initiated banter without moving rules into UI.
 /// </summary>
 sealed class DialoguePanel
 {
  readonly CovenantGame g;
  DialogueDefinition? definition;
  DialogueNodeDefinition? node;
  Feature? source;
  string playerSpeakerId="";
  List<CompanionInterjectionDefinition> interjections=[];
  int selectedChoice;
  float nodeEnteredAt;bool revealAllText;
  int scroll;
  int maxScroll;
  Rectangle bounds,contentBounds;
  const int LineHeight=25;
  readonly record struct DialogueLine(string Text,int Choice=-1,bool ChoiceFirst=false);

  public DialoguePanel(CovenantGame game)=>g=game;
  public bool IsOpen=>definition!=null&&node!=null;
  public bool IsCompanionConversation=>definition?.CompanionConversation==true;
  public string PlayerSpeakerId=>playerSpeakerId;
  public string ActivePartySpeakerId=>NodeSpeaker?.Id??PlayerSpeaker?.Id??"";
  public string CurrentSpeakerName=>NodeSpeaker?.Name??definition?.SpeakerName??"";
  public int AvailableChoiceCount=>VisibleChoices().Length;
  public Rectangle Bounds=>bounds;

  public void BindWorld()
  {
   ResetWithoutCompletion();g.world.DialogueRequested=Open;
  }

  Actor? PlayerSpeaker=>g.world.Party.FirstOrDefault(a=>a.Id==playerSpeakerId&&a.Alive)??g.world.ResolveDialogueSpeaker();
  Actor? NodeSpeaker=>node==null||string.IsNullOrWhiteSpace(node.SpeakerActorId)?null:g.world.Party.FirstOrDefault(a=>a.Id==node.SpeakerActorId&&a.Alive);
  int CurrentSpeakerPortrait=>NodeSpeaker?.Portrait??definition?.SpeakerPortrait??0;

  DialogueChoiceDefinition[] VisibleChoices()
  {
   if(node==null)return[];var fallback=PlayerSpeaker;return node.Choices.Where(c=>g.world.DialogueChoiceAvailable(c,fallback)).ToArray();
  }

  void PrepareForConversation()
  {
   g.CancelInventoryDrag();g.CancelFormationRightPointer();g.ClearArmedSpell();g.ClearArmedFeatureAbility();g.pendingSpellPlan=null;g.quickSpellAssignmentSlot=-1;g.action=TargetIntent.Attack;g.actionBarPage=ActionBarPage.Primary;
   g.ui.Information.Close();g.ui.Modals.Clear();g.ui.Loot.Close();g.ui.Store.Close();g.ui.Services.Close();g.ui.Rest.Close();g.ui.Screens.ReturnToGame();g.leftWorldPress=g.boxDragging=false;g.world.StopAll();
  }

  void Begin(DialogueDefinition dialogue,Feature? feature,Actor player)
  {
   PrepareForConversation();source=feature;definition=dialogue;playerSpeakerId=player.Id;selectedChoice=0;scroll=0;maxScroll=0;EnterNode(dialogue.StartNodeId);
  }

  public void Open(Feature feature)
  {
   if(!g.world.IsDialogueFeature(feature)||!g.world.TryGetDialogue(feature.DialogueId,out var dialogue)){g.Notify("There is no conversation available here.");return;}
   var player=g.world.ResolveDialogueSpeaker(dialogue.ScriptedPlayerSpeakerId);if(player==null){g.Notify("No living companion is available to speak.");return;}Begin(dialogue,feature,player);
  }

  public bool TryOpenQueuedCompanionConversation()
  {
   if(IsOpen||g.title||g.world.Mode!=Mode.Explore||g.world.PendingReaction!=null||g.ui.Screens.IsOpen||g.ui.Loot.IsOpen||g.ui.Store.IsOpen||g.ui.Services.IsOpen||g.ui.Information.IsOpen||g.MapNoteEditorOpen||g.JournalNoteEditorOpen)return false;
   if(!g.world.TryTakeQueuedCompanionConversation(out var dialogue))return false;
   var player=g.world.ResolveDialogueSpeaker(dialogue.ScriptedPlayerSpeakerId);if(player==null)return false;Begin(dialogue,null,player);return true;
  }

  void EnterNode(string nodeId)
  {
   if(definition==null)return;node=definition.Node(nodeId);selectedChoice=0;scroll=0;nodeEnteredAt=g.elapsed;revealAllText=false;interjections=g.world.ResolveCompanionInterjections(definition,node).ToList();
   g.world.Message(MessageCategory.Narrative,$"{CurrentSpeakerName}: {node.Text}");
   foreach(var interjection in interjections)
   {
    var actor=g.world.Party.FirstOrDefault(a=>a.Id==interjection.ActorId);if(actor!=null)g.world.Message(MessageCategory.Narrative,$"{actor.Name}: {interjection.Text}");
   }
  }

  void ResetWithoutCompletion()
  {
   definition=null;node=null;source=null;playerSpeakerId="";interjections=[];selectedChoice=scroll=maxScroll=0;nodeEnteredAt=0;revealAllText=false;
  }

  public bool Close(bool log=true)
  {
   if(!IsOpen)return false;var completed=definition;if(log)g.world.Message(MessageCategory.Narrative,"Conversation ended.");
   ResetWithoutCompletion();if(completed!=null)g.world.CompleteCompanionConversation(completed);return true;
  }

  void ApplySpeakerSwitch(DialogueConsequenceDefinition? consequence)
  {
   string id=consequence?.SwitchPlayerSpeakerId??"";if(string.IsNullOrWhiteSpace(id))return;var switched=g.world.ResolveDialogueSpeaker(id);if(switched!=null)playerSpeakerId=switched.Id;
  }

  void Choose(int index)
  {
   if(node==null||definition==null)return;var choices=VisibleChoices();if(index<0||index>=choices.Length)return;
   var choice=choices[index];var actor=g.world.ResolveDialogueChoiceSpeaker(choice,PlayerSpeaker);if(actor==null){g.Notify("No living companion is available to make this response.");return;}
   g.world.Message(MessageCategory.Narrative,$"{actor.Name}: {choice.Text}");g.world.ApplyDialogueConsequence(choice.Consequence);ApplySpeakerSwitch(choice.Consequence);
   if(choice.Check is { } check)
   {
    var result=g.world.ResolveDialogueCheck(actor,check);bool ends=result.Success?check.SuccessEndsConversation:check.FailureEndsConversation;string next=result.Success?check.SuccessNodeId:check.FailureNodeId;
    if(ends||string.IsNullOrWhiteSpace(next)){Close();return;}EnterNode(next);return;
   }
   if(choice.EndsConversation||string.IsNullOrWhiteSpace(choice.NextNodeId)){Close();return;}EnterNode(choice.NextNodeId);
  }

  public bool HandleWheel(int delta)
  {
   if(!IsOpen||delta==0||!bounds.Contains(g.cursor))return false;int steps=delta/120;if(steps==0)steps=Math.Sign(delta);scroll=Math.Clamp(scroll-steps*3,0,maxScroll);return true;
  }

  bool TextFullyRevealed=>node==null||revealAllText||g.preferences.DialogueTextSpeed<=0||(g.elapsed-nodeEnteredAt)*g.preferences.DialogueTextSpeed>=node.Text.Length;
  string RevealedNodeText()
  {
   if(node==null)return "";if(TextFullyRevealed)return node.Text;int count=Math.Clamp(1+(int)((g.elapsed-nodeEnteredAt)*g.preferences.DialogueTextSpeed),1,node.Text.Length);return node.Text[..count];
  }

  public void HandleKeyboard()
  {
   if(!IsOpen)return;if(g.Pressed(Keys.Escape)){Close();return;}
   if(!TextFullyRevealed){if(g.Pressed(Keys.Enter)||g.Pressed(Keys.Space))revealAllText=true;return;}
   var choices=VisibleChoices();
   if(g.Pressed(Keys.Up)){selectedChoice=Math.Max(0,selectedChoice-1);EnsureChoiceVisible();}
   if(g.Pressed(Keys.Down)){selectedChoice=Math.Min(Math.Max(0,choices.Length-1),selectedChoice+1);EnsureChoiceVisible();}
   if(g.Pressed(Keys.PageUp))scroll=Math.Max(0,scroll-6);if(g.Pressed(Keys.PageDown))scroll=Math.Min(maxScroll,scroll+6);if(g.Pressed(Keys.Home))scroll=0;if(g.Pressed(Keys.End))scroll=maxScroll;
   Keys[] numeric=[Keys.D1,Keys.D2,Keys.D3,Keys.D4,Keys.D5,Keys.D6,Keys.D7,Keys.D8,Keys.D9];for(int i=0;i<Math.Min(numeric.Length,choices.Length);i++)if(g.Pressed(numeric[i])){Choose(i);return;}
   if(g.Pressed(Keys.Enter)&&choices.Length>0)Choose(Math.Clamp(selectedChoice,0,choices.Length-1));
  }

  void EnsureChoiceVisible()
  {
   int estimated=5+interjections.Count*2+selectedChoice*2;if(estimated<scroll)scroll=estimated;else if(estimated>scroll+9)scroll=estimated-9;
  }

  string ChoiceDisplayText(DialogueChoiceDefinition choice)
  {
   var actor=g.world.ResolveDialogueChoiceSpeaker(choice,PlayerSpeaker);string speaker=!string.IsNullOrWhiteSpace(choice.SpeakerActorId)&&actor!=null?$"[{actor.Name}] ":"";
   if(choice.Check is not { } check||actor==null)return speaker+choice.Text;
   int modifier=DialogueCheckRules.Modifier(actor,check);string dc=check.ShowDifficultyClass?$" vs DC {check.DifficultyClass}":"";string mode=DialogueCheckRules.RollModeLabel(check.RollMode);string modeText=string.IsNullOrWhiteSpace(mode)?"":$" · {mode}";
   return $"{speaker}[{DialogueCheckRules.Label(check)} {DialogueCheckRules.Signed(modifier)}{dc}{modeText}] {choice.Text}";
  }

  string ChoiceCheckHint(DialogueChoiceDefinition choice)
  {
   var actor=g.world.ResolveDialogueChoiceSpeaker(choice,PlayerSpeaker);if(choice.Check is not { } check||actor==null)return "";
   int modifier=DialogueCheckRules.Modifier(actor,check);string dc=check.ShowDifficultyClass?$"DC {check.DifficultyClass}":"difficulty hidden";string mode=DialogueCheckRules.RollModeLabel(check.RollMode);
   return $"{actor.Name} will roll {DialogueCheckRules.Label(check)} {DialogueCheckRules.Signed(modifier)} ({dc}{(string.IsNullOrWhiteSpace(mode)?"":$", {mode.ToLowerInvariant()}")}). The d20 roll occurs only after you choose this response.";
  }

  List<DialogueLine> Lines(int width)
  {
   var result=new List<DialogueLine>();if(node==null)return result;foreach(var line in g.Wrap(RevealedNodeText(),width,.59f))result.Add(new(line));
   if(!TextFullyRevealed){result.Add(new(""));result.Add(new("[Enter] reveal full line"));return result;}
   result.Add(new(""));
   foreach(var interjection in interjections)
   {
    var actor=g.world.Party.FirstOrDefault(a=>a.Id==interjection.ActorId);string label=actor?.Name??interjection.ActorId;foreach(var line in g.Wrap($"{label}: {interjection.Text}",width,.55f))result.Add(new(line));result.Add(new(""));
   }
   var choices=VisibleChoices();for(int i=0;i<choices.Length;i++)
   {
    string prefix=$"{i+1}. ";var wrapped=g.Wrap(prefix+ChoiceDisplayText(choices[i]),width,.55f).ToArray();if(wrapped.Length==0)wrapped=[prefix];for(int n=0;n<wrapped.Length;n++)result.Add(new(wrapped[n],i,n==0));result.Add(new(""));
   }
   if(choices.Length==0)result.Add(new("No available response. Press Esc to end the conversation."));return result;
  }

  public void Draw()
  {
   if(!IsOpen||definition==null||node==null)return;var center=g.ui.Layout.CenterBounds;int height=Math.Clamp((int)Math.Round(center.Height*.43),300,390);bounds=new(center.X,center.Bottom-height,center.Width,height);
   g.Panel(bounds);g.Box(new(bounds.X+12,bounds.Y+12,bounds.Width-24,bounds.Height-24),new Color(11,14,15)*.97f,Gold*.30f);g.buttons.Add((bounds,()=>{}));
   int portraitW=92,portraitH=112;var portrait=new Rectangle(bounds.X+25,bounds.Y+54,portraitW,portraitH);DrawSpeakerPortrait(portrait);g.Border(portrait,Gold,2);g.Center(CurrentSpeakerName.ToUpperInvariant(),new(bounds.X+12,portrait.Bottom+10,126,22),Bright,.42f);
   var player=PlayerSpeaker;string responder=player==null?"No lead speaker":IsCompanionConversation?$"Lead: {player.Name}":$"As {player.Name}";g.Center(responder,new(bounds.X+12,portrait.Bottom+37,126,20),Teal,.40f);
   g.Center(IsCompanionConversation?"PARTY CONVERSATION":"DIALOGUE",new(bounds.X+145,bounds.Y+18,bounds.Width-180,30),Bright,.64f,true);
   contentBounds=new(bounds.X+144,bounds.Y+54,bounds.Width-174,bounds.Height-91);g.Box(contentBounds,new Color(7,9,10)*.94f,Gold*.24f);
   int textWidth=contentBounds.Width-38;var lines=Lines(textWidth);int capacity=Math.Max(1,(contentBounds.Height-14)/LineHeight);maxScroll=Math.Max(0,lines.Count-capacity);scroll=Math.Clamp(scroll,0,maxScroll);int y=contentBounds.Y+7;int end=Math.Min(lines.Count,scroll+capacity);
   for(int i=scroll;i<end;i++)
   {
    var line=lines[i];if(line.Choice>=0)
    {
     bool selected=line.Choice==selectedChoice;Color c=selected?Bright:new Color(211,190,151);var row=new Rectangle(contentBounds.X+8,y-1,contentBounds.Width-27,LineHeight);if(selected)g.Fill(row,new Color(79,61,37)*.42f);g.Text(line.Text,contentBounds.X+12,y+1,c,.55f);
     int choiceIndex=line.Choice;g.buttons.Add((row,()=>Choose(choiceIndex)));if(row.Contains(g.cursor)){var choices=VisibleChoices();if(choiceIndex<choices.Length){var choice=choices[choiceIndex];string checkHint=ChoiceCheckHint(choice);var speaker=g.world.ResolveDialogueChoiceSpeaker(choice,PlayerSpeaker);string companionHint=!string.IsNullOrWhiteSpace(choice.SpeakerActorId)&&speaker!=null?$" {speaker.Name} will speak this line.":"";g.tooltip=$"Choose response {choiceIndex+1}. Number key {choiceIndex+1} selects it immediately.{companionHint}{(string.IsNullOrWhiteSpace(checkHint)?"":$" {checkHint}")}";}selectedChoice=choiceIndex;}
    }
    else g.Text(line.Text,contentBounds.X+12,y+1,Bright,.59f);y+=LineHeight;
   }
   DrawScrollbar(new(contentBounds.Right-13,contentBounds.Y+5,7,contentBounds.Height-10),lines.Count,capacity);
   g.Text(TextFullyRevealed?"1-9 choose · Enter selects · Wheel/Page keys scroll · Esc ends":$"Dialogue text: {g.DialogueTextSpeedName()} · Enter/Space reveals full line · Esc ends",bounds.X+145,bounds.Bottom-29,Muted,.43f);
  }

  void DrawSpeakerPortrait(Rectangle r)
  {
   var t=g.tex["portraits"];int art=Math.Clamp(CurrentSpeakerPortrait,0,5),sw=t.Width/3,sh=t.Height/2;g.batch.Draw(t,r,new Rectangle(art%3*sw,art/3*sh,sw,sh),Color.White);
  }

  void DrawScrollbar(Rectangle track,int total,int capacity)
  {
   g.Fill(track,new Color(5,7,8));g.Border(track,Gold*.28f);if(total<=capacity){g.Fill(new(track.X+2,track.Y+2,track.Width-4,track.Height-4),Gold*.22f);return;}int thumb=Math.Max(16,(int)(track.Height*(capacity/(float)total)));int range=Math.Max(1,track.Height-thumb-4);int y=track.Y+2+(int)(range*(scroll/(float)Math.Max(1,total-capacity)));g.Fill(new(track.X+2,y,track.Width-4,thumb),Gold*.76f);
  }
 }
}
