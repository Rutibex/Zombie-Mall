using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum JournalTab { ActiveQuests, CompletedQuests, Story, User }
 enum JournalSort { Date, Name }
 sealed record JournalListItem(string Id,string Title,string Subtitle,int Chapter,string Category,int Minute,int Sequence,QuestState? Quest=null,JournalEntry? Entry=null,UserJournalNote? Note=null);
 sealed record JournalDetailLine(string Text,Color Color,float Scale=.52f,bool Bold=false,int GapAfter=0);

 JournalTab journalTab=JournalTab.ActiveQuests;
 JournalSort journalSort=JournalSort.Date;
 int journalChapterFilter;
 string journalCategoryFilter="";
 int journalListScroll,journalDetailScroll,journalDetailMaxScroll;
 string journalSelectedId="";
 Rectangle journalListRect,journalDetailRect;
 string journalNoteDraft="",editingJournalNoteId="";
 const int JournalNoteMaximumLength=800;
 bool JournalActive=>!title&&ui.Screens.Active==UiScreenId.Journal;
 bool JournalNoteEditorOpen=>ui.Modals.JournalNoteOpen;

 void ResetJournalView(bool keepTab=true)
 {
  if(!keepTab)journalTab=JournalTab.ActiveQuests;
  journalChapterFilter=0;journalCategoryFilter="";journalListScroll=journalDetailScroll=0;journalSelectedId="";
 }

 List<JournalListItem> JournalItems(bool applyFilters=true)
 {
  IEnumerable<JournalListItem> source=journalTab switch
  {
   JournalTab.ActiveQuests=>world.Quests.Where(q=>q.Status==QuestStatus.Active).Select(q=>new JournalListItem(q.Id,q.Title,$"{q.Category} · {World.JournalTimeText(q.StartedMinute)}",q.Chapter,q.Category,q.StartedMinute,world.QuestUpdates(q.Id).Select(e=>e.Sequence).DefaultIfEmpty(0).Max(),Quest:q)),
   JournalTab.CompletedQuests=>world.Quests.Where(q=>q.Status!=QuestStatus.Active).Select(q=>new JournalListItem(q.Id,q.Title,$"{q.Status.ToString().ToUpperInvariant()} · {World.JournalTimeText(q.ResolvedMinute>=0?q.ResolvedMinute:q.StartedMinute)}",q.Chapter,q.Category,q.ResolvedMinute>=0?q.ResolvedMinute:q.StartedMinute,world.QuestUpdates(q.Id).Select(e=>e.Sequence).DefaultIfEmpty(0).Max(),Quest:q)),
   JournalTab.Story=>world.StoryEntries().Select(e=>new JournalListItem(e.Id,e.Title,$"{e.Category} · {World.JournalTimeText(e.CampaignMinute)}",e.Chapter,e.Category,e.CampaignMinute,e.Sequence,Entry:e)),
   JournalTab.User=>world.UserJournalNotes.Select(n=>new JournalListItem(n.Id,n.Title,$"USER NOTE · {World.JournalTimeText(n.CampaignMinute)}",n.Chapter,n.Category,n.CampaignMinute,n.Sequence,Note:n)),
   _=>Enumerable.Empty<JournalListItem>()
  };
  if(applyFilters)
  {
   if(journalChapterFilter>0)source=source.Where(i=>i.Chapter==journalChapterFilter);
   if(!string.IsNullOrWhiteSpace(journalCategoryFilter))source=source.Where(i=>i.Category.Equals(journalCategoryFilter,StringComparison.OrdinalIgnoreCase));
  }
  source=journalSort==JournalSort.Name?source.OrderBy(i=>i.Title,StringComparer.OrdinalIgnoreCase).ThenByDescending(i=>i.Sequence):source.OrderByDescending(i=>i.Minute).ThenByDescending(i=>i.Sequence);
  return source.ToList();
 }

 void SetJournalTab(JournalTab tab)
 {
  if(journalTab==tab)return;journalTab=tab;journalCategoryFilter="";journalListScroll=journalDetailScroll=0;journalSelectedId="";
 }
 void ToggleJournalSort(){journalSort=journalSort==JournalSort.Date?JournalSort.Name:JournalSort.Date;journalListScroll=journalDetailScroll=0;}
 void CycleJournalChapter()
 {
  int max=Math.Max(1,world.Quests.Select(q=>q.Chapter).Concat(world.JournalEntries.Select(e=>e.Chapter)).Concat(world.UserJournalNotes.Select(n=>n.Chapter)).DefaultIfEmpty(1).Max());
  journalChapterFilter=journalChapterFilter>=max?0:journalChapterFilter+1;journalListScroll=journalDetailScroll=0;journalSelectedId="";
 }
 void CycleJournalCategory()
 {
  var categories=JournalItems(false).Select(i=>i.Category).Where(c=>!string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c=>c,StringComparer.OrdinalIgnoreCase).ToArray();
  if(categories.Length==0){journalCategoryFilter="";return;}
  int current=Array.FindIndex(categories,c=>c.Equals(journalCategoryFilter,StringComparison.OrdinalIgnoreCase));
  journalCategoryFilter=current<0?categories[0]:current>=categories.Length-1?"":categories[current+1];journalListScroll=journalDetailScroll=0;journalSelectedId="";
 }

 JournalListItem? EnsureJournalSelection(List<JournalListItem> items)
 {
  if(items.Count==0){journalSelectedId="";journalListScroll=journalDetailScroll=0;return null;}
  var selected=items.FirstOrDefault(i=>i.Id==journalSelectedId);
  if(selected==null){selected=items[0];journalSelectedId=selected.Id;journalDetailScroll=0;}
  int index=items.FindIndex(i=>i.Id==journalSelectedId);int visibleRows=Math.Max(1,(journalListRect.Height-16)/51);
  journalListScroll=Math.Clamp(journalListScroll,0,Math.Max(0,items.Count-visibleRows));
  if(index<journalListScroll)journalListScroll=index;else if(index>=journalListScroll+visibleRows)journalListScroll=index-visibleRows+1;
  return selected;
 }
 void SelectJournalItem(string id){if(journalSelectedId==id)return;journalSelectedId=id;journalDetailScroll=0;}

 bool HandleJournalWheel(int wheel)
 {
  if(!JournalActive||JournalNoteEditorOpen||wheel==0)return false;
  int delta=wheel>0?-2:2;
  if(journalListRect.Contains(cursor))
  {
   int max=Math.Max(0,JournalItems().Count-Math.Max(1,(journalListRect.Height-16)/51));journalListScroll=Math.Clamp(journalListScroll+delta,0,max);return true;
  }
  if(journalDetailRect.Contains(cursor))
  {
   journalDetailScroll=Math.Clamp(journalDetailScroll+delta,0,journalDetailMaxScroll);return true;
  }
  return false;
 }

 void UpdateJournalKeyboard()
 {
  if(!JournalActive||JournalNoteEditorOpen)return;
  var items=JournalItems();if(items.Count==0)return;var selected=EnsureJournalSelection(items);int index=selected==null?0:items.FindIndex(i=>i.Id==selected.Id);
  if(Pressed(Keys.Up))index=Math.Max(0,index-1);if(Pressed(Keys.Down))index=Math.Min(items.Count-1,index+1);
  if(Pressed(Keys.Home))index=0;if(Pressed(Keys.End))index=items.Count-1;
  if(index>=0&&index<items.Count&&items[index].Id!=journalSelectedId)SelectJournalItem(items[index].Id);
  if(Pressed(Keys.PageUp))journalDetailScroll=Math.Max(0,journalDetailScroll-7);if(Pressed(Keys.PageDown))journalDetailScroll=Math.Min(journalDetailMaxScroll,journalDetailScroll+7);
 }

 void OpenJournalNoteEditor(UserJournalNote? note=null)
 {
  editingJournalNoteId=note?.Id??"";journalNoteDraft=note?.Text??"";ui.Modals.Open(UiModalId.JournalNote);
 }
 void CancelJournalNoteEditor(){if(JournalNoteEditorOpen)ui.Modals.Close();editingJournalNoteId="";journalNoteDraft="";}
 void CommitJournalNoteEditor()
 {
  string text=journalNoteDraft.Trim();if(text.Length==0){Notify("Enter some text for the journal note.");return;}
  bool ok;string id=editingJournalNoteId;if(id.Length>0)ok=world.UpdateUserJournalNote(id,text);else{var note=world.AddUserJournalNote(text,journalChapterFilter>0?journalChapterFilter:null);ok=note!=null;if(note!=null)id=note.Id;}
  if(!ok){Notify("That journal note could not be saved.");return;}journalSelectedId=id;journalTab=JournalTab.User;journalDetailScroll=0;Notify(editingJournalNoteId.Length>0?"Journal note updated.":"Journal note added.");CancelJournalNoteEditor();
 }
 void DeleteJournalNote(string id)
 {
  if(world.DeleteUserJournalNote(id)){Notify("Journal note deleted.");journalSelectedId="";journalDetailScroll=0;}
 }
 void DeleteEditingJournalNote(){string id=editingJournalNoteId;if(id.Length>0&&world.DeleteUserJournalNote(id)){Notify("Journal note deleted.");journalSelectedId="";}CancelJournalNoteEditor();}
 void HandleJournalNoteKeyboard()
 {
  if(!JournalNoteEditorOpen)return;if(Pressed(Keys.Escape)){CancelJournalNoteEditor();return;}if(Pressed(Keys.Enter)){CommitJournalNoteEditor();return;}if(Pressed(Keys.Back)&&journalNoteDraft.Length>0)journalNoteDraft=journalNoteDraft[..^1];
 }
 void JournalTextInput(TextInputEventArgs e)
 {
  if(!JournalNoteEditorOpen)return;char c=e.Character;if(c<' '||c=='\u007f'||journalNoteDraft.Length>=JournalNoteMaximumLength)return;journalNoteDraft+=c;
 }

 void AddJournalWrapped(List<JournalDetailLine> lines,string text,int width,Color color,float scale=.52f,bool bold=false,int gapAfter=0)
 {
  foreach(var line in Wrap(text??"",width,scale))lines.Add(new(line,color,scale,bold));if(gapAfter>0)lines.Add(new("",color,scale,false,gapAfter));
 }
 List<JournalDetailLine> JournalDetailLines(JournalListItem item,int width)
 {
  var lines=new List<JournalDetailLine>();
  void H(string s){lines.Add(new(s,Ink,.54f,true,3));}
  void Meta(string s){lines.Add(new(s,new Color(98,76,48),.45f,false,4));}
  void Blank(int gap=8)=>lines.Add(new("",Ink,.4f,false,gap));
  if(item.Quest is {} quest)
  {
   H(quest.Title.ToUpperInvariant());Meta($"{quest.Status.ToString().ToUpperInvariant()} · CHAPTER {quest.Chapter} · {quest.Category} · {World.JournalTimeText(quest.Status==QuestStatus.Active?quest.StartedMinute:Math.Max(0,quest.ResolvedMinute))}");
   AddJournalWrapped(lines,quest.Summary,width,Ink,.53f,false,8);Blank();H("OBJECTIVES");
   foreach(var objective in quest.Objectives)
   {
    string mark=objective.Failed?"[!]":objective.Completed?"[x]":"[ ]";Color c=objective.Failed?Red:objective.Completed?new Color(70,101,77):Ink;AddJournalWrapped(lines,$"{mark} {objective.Text}",width,c,.52f,false,3);
   }
   if(quest.Status!=QuestStatus.Active&&!string.IsNullOrWhiteSpace(quest.Outcome)){Blank();H("OUTCOME");AddJournalWrapped(lines,quest.Outcome,width,quest.Status==QuestStatus.Completed?new Color(70,101,77):new Color(125,61,51),.52f,false,8);}
   var updates=world.QuestUpdates(quest.Id).OrderByDescending(e=>e.Sequence).ToArray();if(updates.Length>0){Blank();H("QUEST UPDATES");foreach(var update in updates){Meta(World.JournalTimeText(update.CampaignMinute));AddJournalWrapped(lines,update.Text,width,Ink,.52f,false,6);}}
  }
  else if(item.Entry is {} entry)
  {
   H(entry.Title.ToUpperInvariant());Meta($"CHAPTER {entry.Chapter} · {entry.Category.ToUpperInvariant()} · {World.JournalTimeText(entry.CampaignMinute)}");Blank();AddJournalWrapped(lines,entry.Text,width,Ink,.53f,false,8);
   lines.Add(new("Story entries are independent of quest objectives and remain in the chronicle after quests change state.",new Color(105,86,57),.34f,false));
  }
  else if(item.Note is {} note)
  {
   H(note.Title.ToUpperInvariant());Meta($"CHAPTER {note.Chapter} · USER NOTE · {World.JournalTimeText(note.CampaignMinute)}");Blank();AddJournalWrapped(lines,note.Text,width,Ink,.53f,false,8);
  }
  return lines;
 }

 void DrawJournalIip37(Rectangle r)
 {
  world.SyncJournalProgress();
  int x=r.X+27,y=r.Y+78,w=r.Width-54,h=r.Height-106;var book=new Rectangle(x,y,w,h);batch.Draw(paper,book,Color.White);Border(book,Gold,2);Border(new(book.X+7,book.Y+7,book.Width-14,book.Height-14),new Color(92,72,43),1);
  int tabY=book.Y+14,tabGap=6,tabW=Math.Min(150,(book.Width-84)/4);int tabsX=book.X+20;
  void TabButton(JournalTab tab,string label,int col){var q=new Rectangle(tabsX+col*(tabW+tabGap),tabY,tabW,33);Button(q,label,()=>SetJournalTab(tab),journalTab==tab,.48f);}
  TabButton(JournalTab.ActiveQuests,"QUESTS",0);TabButton(JournalTab.CompletedQuests,"DONE QUESTS",1);TabButton(JournalTab.Story,"JOURNAL",2);TabButton(JournalTab.User,"USER",3);
  if(journalTab==JournalTab.User)Button(new(book.Right-151,tabY,126,33),"NEW NOTE",()=>OpenJournalNoteEditor(),false,.44f);

  int controlsY=tabY+43;
  Button(new(book.X+20,controlsY,135,28),journalSort==JournalSort.Date?"BY DATE":"BY NAME",ToggleJournalSort,true,.39f);
  Button(new(book.X+162,controlsY,148,28),journalChapterFilter==0?"ALL CHAPTERS":$"CHAPTER {journalChapterFilter}",CycleJournalChapter,false,.36f);
  string categoryLabel=string.IsNullOrWhiteSpace(journalCategoryFilter)?"ALL CATEGORIES":journalCategoryFilter.ToUpperInvariant();Button(new(book.X+317,controlsY,188,28),categoryLabel,CycleJournalCategory,false,.33f);
  Text($"{world.Quests.Count(q=>q.Status==QuestStatus.Active)} active · {world.Quests.Count(q=>q.Status!=QuestStatus.Active)} resolved · {world.StoryEntries().Count()} story · {world.UserJournalNotes.Count} notes",book.X+22,controlsY+34,Ink,.45f);

  int bodyY=controlsY+56,bodyBottom=book.Bottom-22;int listW=Math.Clamp(book.Width*32/100,265,330);journalListRect=new(book.X+20,bodyY,listW,bodyBottom-bodyY);journalDetailRect=new(journalListRect.Right+11,bodyY,book.Right-(journalListRect.Right+31),bodyBottom-bodyY);
  Box(journalListRect,new Color(60,44,25)*.12f,new Color(95,73,43));Box(journalDetailRect,new Color(60,44,25)*.08f,new Color(95,73,43));

  var items=JournalItems();var selected=EnsureJournalSelection(items);const int rowH=51;int visibleRows=Math.Max(1,(journalListRect.Height-16)/rowH);int start=Math.Clamp(journalListScroll,0,Math.Max(0,items.Count-visibleRows));
  if(items.Count==0)Center("NO ENTRIES MATCH THESE FILTERS",new(journalListRect.X+8,journalListRect.Y+85,journalListRect.Width-16,30),new Color(105,86,57),.38f,true);
  for(int row=0;row<visibleRows&&start+row<items.Count;row++)
  {
   var item=items[start+row];var q=new Rectangle(journalListRect.X+8,journalListRect.Y+8+row*rowH,journalListRect.Width-29,rowH-5);bool active=item.Id==journalSelectedId;Box(q,active?new Color(114,91,55)*.25f:new Color(68,52,32)*.10f,active?new Color(110,84,49):new Color(95,73,43)*.44f);Text(item.Title.Length>31?item.Title[..28]+"...":item.Title,q.X+9,q.Y+5,Ink,.51f);Text(item.Subtitle.Length>40?item.Subtitle[..37]+"...":item.Subtitle,q.X+9,q.Y+27,new Color(100,80,55),.42f);buttons.Add((q,()=>SelectJournalItem(item.Id)));if(q.Contains(cursor))tooltip=$"{item.Title} — click to read.";
  }
  int maxListScroll=Math.Max(0,items.Count-visibleRows);Rectangle up=new Rectangle(journalListRect.Right-20,journalListRect.Y+8,14,25),down=new Rectangle(journalListRect.Right-20,journalListRect.Bottom-33,14,25);Button(up,"^",()=>journalListScroll=Math.Max(0,journalListScroll-1),journalListScroll<=0,.34f);Button(down,"v",()=>journalListScroll=Math.Min(maxListScroll,journalListScroll+1),journalListScroll>=maxListScroll,.34f);
  int listTrackY=up.Bottom+4,listTrackH=Math.Max(15,down.Y-listTrackY-4);Border(new(journalListRect.Right-15,listTrackY,4,listTrackH),new Color(95,73,43)*.40f);if(maxListScroll>0){int thumbH=Math.Max(15,listTrackH*visibleRows/Math.Max(visibleRows,items.Count));int thumbY=listTrackY+(int)((listTrackH-thumbH)*(journalListScroll/(float)maxListScroll));Fill(new(journalListRect.Right-14,thumbY,2,thumbH),new Color(105,82,49));}

  if(selected!=null)
  {
   int inset=18,textWidth=journalDetailRect.Width-inset*2-20;var lines=JournalDetailLines(selected,textWidth);int lineH=24,maxLines=Math.Max(1,(journalDetailRect.Height-66)/lineH);journalDetailMaxScroll=Math.Max(0,lines.Count-maxLines);journalDetailScroll=Math.Clamp(journalDetailScroll,0,journalDetailMaxScroll);
   int textY=journalDetailRect.Y+16;for(int i=0;i<maxLines&&i+journalDetailScroll<lines.Count;i++)
   {
    var line=lines[i+journalDetailScroll];if(line.Text.Length>0)Text(line.Text,journalDetailRect.X+inset,textY,line.Color,line.Scale,line.Bold);textY+=lineH+line.GapAfter;
   }
   Rectangle du=new Rectangle(journalDetailRect.Right-22,journalDetailRect.Y+12,15,25),dd=new Rectangle(journalDetailRect.Right-22,journalDetailRect.Bottom-42,15,25);Button(du,"^",()=>journalDetailScroll=Math.Max(0,journalDetailScroll-3),journalDetailScroll<=0,.34f);Button(dd,"v",()=>journalDetailScroll=Math.Min(journalDetailMaxScroll,journalDetailScroll+3),journalDetailScroll>=journalDetailMaxScroll,.34f);
   if(selected.Note is {} note)
   {
    Button(new(journalDetailRect.X+17,journalDetailRect.Bottom-39,100,27),"EDIT",()=>OpenJournalNoteEditor(note),false,.38f);Button(new(journalDetailRect.X+124,journalDetailRect.Bottom-39,100,27),"DELETE",()=>DeleteJournalNote(note.Id),false,.38f);
   }
  }
  else journalDetailMaxScroll=journalDetailScroll=0;
  if(JournalNoteEditorOpen)DrawJournalNoteEditor(r);
 }

 void DrawJournalNoteEditor(Rectangle r)
 {
  buttons.Add((ui.Layout.Screen,()=>{}));var modal=new Rectangle(r.Center.X-365,r.Center.Y-180,730,360);Box(modal,new Color(15,18,18),Gold);Border(new(modal.X+5,modal.Y+5,modal.Width-10,modal.Height-10),Gold*.28f);Center(editingJournalNoteId.Length>0?"EDIT JOURNAL NOTE":"ADD JOURNAL NOTE",new(modal.X,modal.Y+18,modal.Width,28),Bright,.62f,true);
  Text($"Chapter {(journalChapterFilter>0?journalChapterFilter:world.CurrentJournalChapter)} · {World.JournalTimeText(world.CampaignMinutes)}",modal.X+24,modal.Y+58,Muted,.43f);
  var field=new Rectangle(modal.X+24,modal.Y+87,modal.Width-48,188);Box(field,new Color(7,9,10),Gold*.45f);var source=journalNoteDraft.Length==0?new[]{"Type a personal journal note..."}:Wrap(journalNoteDraft,field.Width-24,.46f).ToArray();int maxLines=7,start=Math.Max(0,source.Length-maxLines);for(int i=start;i<source.Length;i++)Text(source[i]+(i==source.Length-1&&journalNoteDraft.Length<JournalNoteMaximumLength&&((int)(elapsed*2)%2==0)?"|":""),field.X+12,field.Y+12+(i-start)*23,journalNoteDraft.Length==0?Muted*.55f:Bright,.46f);
  Text($"{journalNoteDraft.Length}/{JournalNoteMaximumLength}   Enter saves · Esc cancels",modal.X+24,modal.Y+286,Muted,.39f);int by=modal.Bottom-53;CommandButton(new(modal.X+24,by,150,32),"SAVE",CommitJournalNoteEditor,journalNoteDraft.Trim().Length>0,"Enter note text before saving.",false,.50f);if(editingJournalNoteId.Length>0)Button(new(modal.X+184,by,150,32),"DELETE",DeleteEditingJournalNote,false,.47f);Button(new(modal.Right-174,by,150,32),"CANCEL",CancelJournalNoteEditor,false,.47f);
 }
}
