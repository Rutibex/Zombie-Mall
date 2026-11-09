using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QuestStatus { Active, Completed, Failed }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JournalEntryKind { Story, QuestUpdate }

public sealed class QuestObjectiveState
{
 public string Id {get;set;}="";
 public string Text {get;set;}="";
 public bool Completed {get;set;}
 public bool Failed {get;set;}
 public int UpdatedMinute {get;set;}
}

public sealed class QuestState
{
 public string Id {get;set;}="";
 public string Title {get;set;}="";
 public string Summary {get;set;}="";
 public string Category {get;set;}="Quest";
 public int Chapter {get;set;}=1;
 public QuestStatus Status {get;set;}=QuestStatus.Active;
 public int StartedMinute {get;set;}
 public int ResolvedMinute {get;set;}=-1;
 public string Outcome {get;set;}="";
 public List<QuestObjectiveState> Objectives {get;set;}=[];
}

/// <summary>
/// Durable authored/automatic journal event. QuestUpdate entries are linked to a quest;
/// Story entries deliberately remain independent from the quest objective list.
/// </summary>
public sealed class JournalEntry
{
 public string Id {get;set;}="";
 public JournalEntryKind Kind {get;set;}=JournalEntryKind.Story;
 public string Title {get;set;}="";
 public string Text {get;set;}="";
 public string Category {get;set;}="Story";
 public int Chapter {get;set;}=1;
 public int CampaignMinute {get;set;}
 public int Sequence {get;set;}
 public string QuestId {get;set;}="";
}

/// <summary>Player-authored journal note. Notes are campaign-global rather than area-local.</summary>
public sealed class UserJournalNote
{
 public string Id {get;set;}="";
 public string Title {get;set;}="";
 public string Text {get;set;}="";
 public string Category {get;set;}="User";
 public int Chapter {get;set;}=1;
 public int CampaignMinute {get;set;}
 public int Sequence {get;set;}
}

public sealed partial class World
{
 public List<QuestState> Quests {get;set;}=[];
 public List<JournalEntry> JournalEntries {get;set;}=[];
 public List<UserJournalNote> UserJournalNotes {get;set;}=[];
 public int NextJournalSequence {get;set;}=1;
 public int NextUserJournalNoteNumber {get;set;}=1;
 [JsonIgnore] public int CurrentJournalChapter=>Quests.Where(q=>q.Status==QuestStatus.Active).Select(q=>q.Chapter).DefaultIfEmpty(1).Max();

 public static string JournalTimeText(int campaignMinute)
 {
  int minutes=Math.Max(0,campaignMinute);return $"Day {1+minutes/1440}, {(minutes%1440)/60:00}:{minutes%60:00}";
 }
 public QuestState? Quest(string id)=>Quests.FirstOrDefault(q=>q.Id.Equals(id,StringComparison.Ordinal));
 public bool IsQuestActive(string id)=>Quest(id)?.Status==QuestStatus.Active;
 public IEnumerable<JournalEntry> QuestUpdates(string questId)=>JournalEntries.Where(e=>e.Kind==JournalEntryKind.QuestUpdate&&e.QuestId==questId).OrderBy(e=>e.Sequence);
 public IEnumerable<JournalEntry> StoryEntries()=>JournalEntries.Where(e=>e.Kind==JournalEntryKind.Story).OrderBy(e=>e.Sequence);

 public void InitializeJournalState()
 {
  Quests=[];JournalEntries=[];UserJournalNotes=[];NextJournalSequence=1;NextUserJournalNoteNumber=1;
  BootstrapJournal();SyncJournalProgress();
 }
 void BootstrapJournal()
 {
  AddStoryJournalEntry("story-oath","The Covenant's Oath","The Ember Heart was stolen from beneath Saint Orlan. The dead are stirring below the shrine district, and the Ashen Covenant has sworn to recover the relic before the old seals fail.","Story",1,CampaignMinutes);
  StartQuest(new QuestState
  {
   Id="recover-ember-heart",Title="Recover the Ember Heart",Category="Main Quest",Chapter=1,StartedMinute=CampaignMinutes,
   Summary="Descend into Saint Orlan's crypt, break through the Ashen Prior's congregation, and recover the stolen Ember Heart.",
   Objectives=
   [
    new(){Id="open-ironbound-door",Text="Enter the crypt and open the ironbound door."},
    new(){Id="clear-congregation",Text="Defeat the crypt sentinels and hollow congregation."},
    new(){Id="defeat-ashen-prior",Text="Defeat the Ashen Prior in the southern sanctum."},
    new(){Id="take-ember-heart",Text="Recover the Ember Heart from its altar."}
   ]
  },"The Ember Heart lies somewhere beneath Saint Orlan's crypt.");
 }
 public bool StartQuest(QuestState quest,string updateText="")
 {
  if(quest==null||string.IsNullOrWhiteSpace(quest.Id)||string.IsNullOrWhiteSpace(quest.Title)||Quest(quest.Id)!=null)return false;
  quest.Id=quest.Id.Trim();quest.Title=quest.Title.Trim();quest.Summary=(quest.Summary??"").Trim();quest.Category=string.IsNullOrWhiteSpace(quest.Category)?"Quest":quest.Category.Trim();quest.Chapter=Math.Max(1,quest.Chapter);quest.Status=QuestStatus.Active;quest.StartedMinute=Math.Max(0,quest.StartedMinute==0?CampaignMinutes:quest.StartedMinute);quest.ResolvedMinute=-1;quest.Outcome="";quest.Objectives??=[];
  foreach(var objective in quest.Objectives){objective.Id=(objective.Id??"").Trim();objective.Text=(objective.Text??"").Trim();objective.Completed=false;objective.Failed=false;objective.UpdatedMinute=quest.StartedMinute;}
  Quests.Add(quest);AddQuestJournalUpdate(quest.Id,string.IsNullOrWhiteSpace(updateText)?$"Quest begun: {quest.Title}.":updateText,quest.Title);return true;
 }
 public bool SetQuestObjective(string questId,string objectiveId,bool completed,string updateText="")
 {
  var quest=Quest(questId);if(quest==null||quest.Status!=QuestStatus.Active)return false;var objective=quest.Objectives.FirstOrDefault(o=>o.Id==objectiveId);if(objective==null||objective.Completed==completed&& !objective.Failed)return false;
  objective.Completed=completed;objective.Failed=false;objective.UpdatedMinute=CampaignMinutes;
  if(completed)AddQuestJournalUpdate(questId,string.IsNullOrWhiteSpace(updateText)?$"Objective completed: {objective.Text}":updateText,quest.Title);
  return true;
 }
 public bool CompleteQuest(string questId,string outcome="")
 {
  var quest=Quest(questId);if(quest==null||quest.Status!=QuestStatus.Active)return false;quest.Status=QuestStatus.Completed;quest.ResolvedMinute=CampaignMinutes;quest.Outcome=string.IsNullOrWhiteSpace(outcome)?"Completed.":outcome.Trim();
  AddQuestJournalUpdate(questId,$"Quest completed: {quest.Outcome}",quest.Title);return true;
 }
 public bool FailQuest(string questId,string outcome="")
 {
  var quest=Quest(questId);if(quest==null||quest.Status!=QuestStatus.Active)return false;quest.Status=QuestStatus.Failed;quest.ResolvedMinute=CampaignMinutes;quest.Outcome=string.IsNullOrWhiteSpace(outcome)?"Failed.":outcome.Trim();
  AddQuestJournalUpdate(questId,$"Quest failed: {quest.Outcome}",quest.Title);return true;
 }
 public bool AddStoryJournalEntry(string id,string title,string text,string category="Story",int chapter=1,int? atMinute=null)
 {
  if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(text)||JournalEntries.Any(e=>e.Id==id))return false;
  JournalEntries.Add(new(){Id=id.Trim(),Kind=JournalEntryKind.Story,Title=title.Trim(),Text=text.Trim(),Category=string.IsNullOrWhiteSpace(category)?"Story":category.Trim(),Chapter=Math.Max(1,chapter),CampaignMinute=Math.Max(0,atMinute??CampaignMinutes),Sequence=NextSequence()});return true;
 }
 void AddQuestJournalUpdate(string questId,string text,string title)
 {
  if(string.IsNullOrWhiteSpace(text))return;JournalEntries.Add(new(){Id=$"quest-update-{NextJournalSequence:D6}",Kind=JournalEntryKind.QuestUpdate,Title=title,Text=text.Trim(),Category="Quest Update",Chapter=Math.Max(1,Quest(questId)?.Chapter??1),CampaignMinute=CampaignMinutes,Sequence=NextSequence(),QuestId=questId});
 }
 int NextSequence()=>Math.Max(1,NextJournalSequence++);
 static string NoteTitle(string text,int number)
 {
  var clean=(text??"").Trim().Replace('\r',' ').Replace('\n',' ');if(clean.Length==0)return $"Note {number}";return clean.Length<=42?clean:clean[..39]+"...";
 }
 public UserJournalNote? AddUserJournalNote(string text,int? chapter=null)
 {
  text=(text??"").Trim();if(text.Length==0)return null;if(text.Length>800)text=text[..800];int number=Math.Max(1,NextUserJournalNoteNumber++);var note=new UserJournalNote{Id=$"journal-note-{number:D4}",Title=NoteTitle(text,number),Text=text,Chapter=Math.Max(1,chapter??CurrentJournalChapter),CampaignMinute=CampaignMinutes,Sequence=NextSequence()};UserJournalNotes.Add(note);return note;
 }
 public bool UpdateUserJournalNote(string id,string text)
 {
  var note=UserJournalNotes.FirstOrDefault(n=>n.Id==id);if(note==null)return false;text=(text??"").Trim();if(text.Length==0)return false;if(text.Length>800)text=text[..800];note.Text=text;note.Title=NoteTitle(text,ParseJournalNoteNumber(id));note.CampaignMinute=CampaignMinutes;note.Sequence=NextSequence();return true;
 }
 public bool DeleteUserJournalNote(string id)=>UserJournalNotes.RemoveAll(n=>n.Id==id)>0;
 static int ParseJournalNoteNumber(string id){int dash=id.LastIndexOf('-');return dash>=0&&int.TryParse(id[(dash+1)..],out var n)?Math.Max(1,n):1;}

 Feature? CryptFeature(string name)
 {
  if(CurrentTravelDestinationId=="saint-orlans-crypt")return Features.FirstOrDefault(f=>f.Name==name);
  return StoredTravelAreas.FirstOrDefault(a=>a.DestinationId=="saint-orlans-crypt")?.Features.FirstOrDefault(f=>f.Name==name);
 }
 IEnumerable<Actor> CryptEnemies()
 {
  if(CurrentTravelDestinationId=="saint-orlans-crypt")return Actors.Where(a=>a.Enemy);
  return StoredTravelAreas.FirstOrDefault(a=>a.DestinationId=="saint-orlans-crypt")?.Actors.Where(a=>a.Enemy)??Enumerable.Empty<Actor>();
 }
 bool CryptGroupCleared(int group){var enemies=CryptEnemies().Where(a=>a.Group==group).ToArray();return enemies.Length>0&&enemies.All(a=>!a.Alive);}

 /// <summary>
 /// Idempotent bridge from authoritative game state into the journal lifecycle. This permits
 /// v9 migration to reconstruct progress without guessing from old message prose.
 /// </summary>
 public void SyncJournalProgress()
 {
  if(Campaign!=null){SyncCampaignObjectives();return;}
  if(Quests==null||JournalEntries==null||UserJournalNotes==null)return;
  if(Quest("recover-ember-heart")==null)BootstrapJournal();
  SetQuestObjective("recover-ember-heart","open-ironbound-door",CryptFeature("Ironbound door")?.Used==true,"The ironbound door has been opened. The deeper crypt is accessible.");
  SetQuestObjective("recover-ember-heart","clear-congregation",CryptGroupCleared(0)&&CryptGroupCleared(1),"The crypt sentinels and hollow congregation have been defeated.");
  SetQuestObjective("recover-ember-heart","defeat-ashen-prior",CryptEnemies().FirstOrDefault(a=>a.Id=="boss") is {Alive:false},"The Ashen Prior has fallen.");
  bool relicRecovered=HasRelic||CryptFeature("The Ember Heart")?.Used==true;
  if(relicRecovered)SetQuestObjective("recover-ember-heart","take-ember-heart",true,"The Ember Heart has been recovered from the southern sanctum.");
  var recover=Quest("recover-ember-heart");
  if(recover?.Status==QuestStatus.Active&&recover.Objectives.All(o=>o.Completed))
  {
   CompleteQuest(recover.Id,"The Ember Heart has been recovered from the crypt.");
   AddStoryJournalEntry("story-heart-recovered","The Ember Heart Recovered","The Ashen Prior is defeated and the Ember Heart is again in mortal hands. It must be returned to the Ember Hearth in Saint Orlan before the old wards fail completely.","Story",1);
  }
  if(recover?.Status==QuestStatus.Completed&&Quest("restore-ember-heart")==null)
  {
   StartQuest(new QuestState
   {
    Id="restore-ember-heart",Title="Restore the Ember Heart",Category="Main Quest",Chapter=1,Summary="Carry the recovered Ember Heart back to Saint Orlan and restore it to the Ember Hearth.",
    Objectives=[new(){Id="return-to-saint-orlan",Text="Return to Saint Orlan with the Ember Heart."},new(){Id="restore-at-hearth",Text="Restore the Ember Heart at the Ember Hearth."}]
   },"The Ember Heart must be returned to Saint Orlan.");
  }
  if(IsQuestActive("restore-ember-heart")&&CurrentTravelDestinationId=="saint-orlan")SetQuestObjective("restore-ember-heart","return-to-saint-orlan",true,"The Covenant has returned to Saint Orlan with the relic.");
  if(IsQuestActive("restore-ember-heart")&&Mode==Mode.Victory)
  {
   SetQuestObjective("restore-ember-heart","restore-at-hearth",true,"The Ember Heart is restored to the Ember Hearth.");CompleteQuest("restore-ember-heart","The Ember Heart is restored and dawn returns to the vale.");
   AddStoryJournalEntry("story-dawn-restored","Dawn Returns to the Vale","The Ember Heart burns again in Saint Orlan. The bells fall silent, the dead return to stillness, and the Ashen Covenant steps into the first light of morning.","Story",1);
  }
 }
 public void RecordTravelArrival(string destinationId)
 {
  var def=ResolveTravelDestination(destinationId);if(destinationId=="saint-orlans-crypt")return;AddStoryJournalEntry($"arrival-{destinationId}",$"Arrival: {def.Name}",def.Description,"Travel",CurrentJournalChapter);
  SyncJournalProgress();
 }
 public void RecordTravelEvent(TravelEventDefinition evt)=>AddStoryJournalEntry($"travel-event-{TravelCount:D4}-{evt.Id}",evt.Name,evt.Text,"Travel",CurrentJournalChapter);

 public void NormalizeJournalState()
 {
  Quests??=[];JournalEntries??=[];UserJournalNotes??=[];NextJournalSequence=Math.Max(1,NextJournalSequence);NextUserJournalNoteNumber=Math.Max(1,NextUserJournalNoteNumber);
  if(Quests.Count==0){if(Campaign==null)BootstrapJournal();else InitializeCampaignJournal();}
  var questIds=new HashSet<string>(StringComparer.Ordinal);foreach(var q in Quests)
  {
   q.Id=(q.Id??"").Trim();q.Title=(q.Title??"").Trim();q.Summary=(q.Summary??"").Trim();q.Category=string.IsNullOrWhiteSpace(q.Category)?"Quest":q.Category.Trim();q.Chapter=Math.Max(1,q.Chapter);q.Objectives??=[];q.StartedMinute=Math.Max(0,q.StartedMinute);if(q.Status==QuestStatus.Active)q.ResolvedMinute=-1;
   var objectiveIds=new HashSet<string>(StringComparer.Ordinal);foreach(var o in q.Objectives){o.Id=(o.Id??"").Trim();o.Text=(o.Text??"").Trim();o.UpdatedMinute=Math.Max(0,o.UpdatedMinute);if(!objectiveIds.Add(o.Id))o.Id=$"objective-{objectiveIds.Count+1:D2}";}
   questIds.Add(q.Id);
  }
  var entryIds=new HashSet<string>(StringComparer.Ordinal);int seq=Math.Max(1,NextJournalSequence);
  foreach(var e in JournalEntries)
  {
   e.Id=(e.Id??"").Trim();e.Title=(e.Title??"").Trim();e.Text=(e.Text??"").Trim();e.Category=string.IsNullOrWhiteSpace(e.Category)?(e.Kind==JournalEntryKind.Story?"Story":"Quest Update"):e.Category.Trim();e.Chapter=Math.Max(1,e.Chapter);e.CampaignMinute=Math.Max(0,e.CampaignMinute);if(e.Sequence<=0)e.Sequence=seq++;if(string.IsNullOrWhiteSpace(e.Id)||!entryIds.Add(e.Id)){do{e.Id=$"journal-entry-{seq:D6}";seq++;}while(!entryIds.Add(e.Id));}
  }
  var noteIds=new HashSet<string>(StringComparer.Ordinal);int noteNumber=Math.Max(1,NextUserJournalNoteNumber);
  foreach(var n in UserJournalNotes)
  {
   n.Text=(n.Text??"").Trim();if(n.Text.Length>800)n.Text=n.Text[..800];n.Chapter=Math.Max(1,n.Chapter);n.Category="User";n.CampaignMinute=Math.Max(0,n.CampaignMinute);if(n.Sequence<=0)n.Sequence=seq++;if(string.IsNullOrWhiteSpace(n.Id)||!noteIds.Add(n.Id)){do{n.Id=$"journal-note-{noteNumber++:D4}";}while(!noteIds.Add(n.Id));}n.Title=NoteTitle(n.Text,ParseJournalNoteNumber(n.Id));
  }
  UserJournalNotes=UserJournalNotes.Where(n=>n.Text.Length>0).ToList();NextJournalSequence=Math.Max(seq,JournalEntries.Select(e=>e.Sequence+1).Concat(UserJournalNotes.Select(n=>n.Sequence+1)).DefaultIfEmpty(1).Max());NextUserJournalNoteNumber=Math.Max(noteNumber,UserJournalNotes.Select(n=>ParseJournalNoteNumber(n.Id)+1).DefaultIfEmpty(1).Max());
  SyncJournalProgress();
 }
 public void ValidateJournalState()
 {
  if(NextJournalSequence<1||NextUserJournalNoteNumber<1)throw new InvalidDataException("Invalid journal sequence state.");
  if(Quests.Any(q=>string.IsNullOrWhiteSpace(q.Id)||string.IsNullOrWhiteSpace(q.Title)||q.Chapter<1||q.Objectives.Any(o=>string.IsNullOrWhiteSpace(o.Id)||string.IsNullOrWhiteSpace(o.Text))))throw new InvalidDataException("Invalid quest journal state.");
  if(Quests.Select(q=>q.Id).Distinct(StringComparer.Ordinal).Count()!=Quests.Count)throw new InvalidDataException("Duplicate quest id.");
  if(Quests.Any(q=>q.Objectives.Select(o=>o.Id).Distinct(StringComparer.Ordinal).Count()!=q.Objectives.Count))throw new InvalidDataException("Duplicate quest objective id.");
  if(JournalEntries.Any(e=>string.IsNullOrWhiteSpace(e.Id)||string.IsNullOrWhiteSpace(e.Title)||string.IsNullOrWhiteSpace(e.Text)||e.Chapter<1||e.Sequence<1))throw new InvalidDataException("Invalid journal entry.");
  if(JournalEntries.Select(e=>e.Id).Distinct(StringComparer.Ordinal).Count()!=JournalEntries.Count)throw new InvalidDataException("Duplicate journal entry id.");
  if(JournalEntries.Any(e=>e.Kind==JournalEntryKind.QuestUpdate&&(string.IsNullOrWhiteSpace(e.QuestId)||Quest(e.QuestId)==null)))throw new InvalidDataException("Quest update references an unknown quest.");
  if(UserJournalNotes.Any(n=>string.IsNullOrWhiteSpace(n.Id)||string.IsNullOrWhiteSpace(n.Text)||n.Text.Length>800||n.Chapter<1||n.Sequence<1))throw new InvalidDataException("Invalid user journal note.");
  if(UserJournalNotes.Select(n=>n.Id).Distinct(StringComparer.Ordinal).Count()!=UserJournalNotes.Count)throw new InvalidDataException("Duplicate user journal note id.");
 }
}
