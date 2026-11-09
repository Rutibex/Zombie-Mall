using System.Text.Json.Serialization;
namespace Ashen.Core;

/// <summary>
/// IIP39 extends the IIP38 presentation-neutral conversation graph with optional 5e
/// ability/skill checks. The Core owns modifiers, deterministic d20 rolls and outcome
/// branching; the MonoGame panel only previews the acting speaker's modifier and presents
/// the authored result node.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DialogueCheckRollMode { Normal, Advantage, Disadvantage }

public sealed record DialogueCheckDefinition(
 int DifficultyClass,
 string SuccessNodeId,
 string FailureNodeId,
 Skill? Skill=null,
 Ability? Ability=null,
 Ability? AbilityOverride=null,
 DialogueCheckRollMode RollMode=DialogueCheckRollMode.Normal,
 bool ShowDifficultyClass=true,
 bool SuccessEndsConversation=false,
 bool FailureEndsConversation=false,
 bool Supernatural=false);

public sealed record DialogueCheckResult(
 string ActorId,
 string Label,
 int DifficultyClass,
 int Modifier,
 int FirstRoll,
 int SecondRoll,
 int ChosenRoll,
 int Total,
 bool Success,
 DialogueCheckRollMode RollMode);

public static class DialogueCheckRules
{
 public static void ValidateOrThrow(DialogueCheckDefinition check)
 {
  bool hasSkill=check.Skill.HasValue,hasAbility=check.Ability.HasValue;
  if(hasSkill==hasAbility)throw new InvalidDataException("Dialogue check must define exactly one Skill or Ability.");
  if(check.AbilityOverride.HasValue&&!hasSkill)throw new InvalidDataException("Dialogue AbilityOverride is legal only for a skill check.");
  if(check.DifficultyClass<1||check.DifficultyClass>30)throw new InvalidDataException("Dialogue check DC must be 1-30.");
  if(!Enum.IsDefined(check.RollMode))throw new InvalidDataException("Dialogue check roll mode is invalid.");
  if(string.IsNullOrWhiteSpace(check.SuccessNodeId)&&!check.SuccessEndsConversation)throw new InvalidDataException("Dialogue check needs a success node or success ending.");
  if(string.IsNullOrWhiteSpace(check.FailureNodeId)&&!check.FailureEndsConversation)throw new InvalidDataException("Dialogue check needs a failure node or failure ending.");
 }
 public static Ability AbilityFor(DialogueCheckDefinition check)
 {
  ValidateOrThrow(check);
  if(check.Skill is { } skill)return check.AbilityOverride??SkillRules.AbilityFor(skill);
  return check.Ability!.Value;
 }
 public static int Modifier(Actor actor,DialogueCheckDefinition check)
 {
  ValidateOrThrow(check);
  if(check.Skill is { } skill)return actor.Character.SkillModifier(skill,check.AbilityOverride);
  return actor.AbilityModifier(check.Ability!.Value);
 }
 public static string Label(DialogueCheckDefinition check)
 {
  ValidateOrThrow(check);
  if(check.Skill is { } skill)
  {
   string name=SkillRules.DisplayName(skill);var normal=SkillRules.AbilityFor(skill);
   if(check.AbilityOverride is { } alternate&&alternate!=normal)return $"{alternate} ({name})";
   return name;
  }
  return check.Ability!.Value.ToString();
 }
 public static string Signed(int value)=>value>=0?$"+{value}":value.ToString();
 public static string RollModeLabel(DialogueCheckRollMode mode)=>mode switch
 {
  DialogueCheckRollMode.Advantage=>"ADVANTAGE",
  DialogueCheckRollMode.Disadvantage=>"DISADVANTAGE",
  _=>""
 };
}

/// <summary>IIP40 generic companion/dialogue gating. Empty fields mean no restriction.</summary>
public sealed record DialogueConditionDefinition(
 string CompanionId="",
 int MinimumApproval=-100,
 int MaximumApproval=100,
 string RequiredFlag="",
 string ForbiddenFlag="",
 bool RequireAlive=true);

public sealed record DialogueApprovalChange(string ActorId,int Delta,string Reason="");

public sealed record DialogueConsequenceDefinition(
 DialogueApprovalChange[]? ApprovalChanges=null,
 string[]? SetFlags=null,
 string[]? ClearFlags=null,
 string[]? QueueCompanionConversations=null,
 string SwitchPlayerSpeakerId="");

public sealed record CompanionInterjectionDefinition(
 string Id,
 string ActorId,
 string Text,
 DialogueConditionDefinition[]? Conditions=null,
 bool Once=true,
 DialogueConsequenceDefinition? Consequence=null);

public sealed record DialogueChoiceDefinition(
 string Id,
 string Text,
 string NextNodeId="",
 bool EndsConversation=false,
 DialogueCheckDefinition? Check=null,
 string SpeakerActorId="",
 DialogueConditionDefinition[]? Conditions=null,
 DialogueConsequenceDefinition? Consequence=null);

public sealed record DialogueNodeDefinition(
 string Id,
 string Text,
 DialogueChoiceDefinition[] Choices,
 string SpeakerActorId="",
 CompanionInterjectionDefinition[]? Interjections=null);

public sealed record DialogueDefinition(
 string Id,
 string SpeakerName,
 int SpeakerPortrait,
 string StartNodeId,
 DialogueNodeDefinition[] Nodes,
 string ScriptedPlayerSpeakerId="",
 bool CompanionConversation=false,
 string CompletionFlag="")
{
 public DialogueNodeDefinition Node(string id)=>Nodes.FirstOrDefault(n=>n.Id.Equals(id,StringComparison.Ordinal))
  ??throw new KeyNotFoundException($"Unknown dialogue node '{id}' in '{Id}'.");
 public void ValidateOrThrow()
 {
  if(string.IsNullOrWhiteSpace(Id)||string.IsNullOrWhiteSpace(SpeakerName)||string.IsNullOrWhiteSpace(StartNodeId))throw new InvalidDataException("Dialogue identity is incomplete.");
  if(Nodes.Select(n=>n.Id).Distinct(StringComparer.Ordinal).Count()!=Nodes.Length)throw new InvalidDataException($"Dialogue '{Id}' contains duplicate node IDs.");
  _=Node(StartNodeId);
  foreach(var n in Nodes)
  {
   foreach(var interjection in n.Interjections??[])
   {
    if(string.IsNullOrWhiteSpace(interjection.Id)||string.IsNullOrWhiteSpace(interjection.ActorId)||string.IsNullOrWhiteSpace(interjection.Text))throw new InvalidDataException($"Dialogue '{Id}' contains an incomplete companion interjection.");
    CompanionConversationRules.ValidateConditions(interjection.Conditions);CompanionConversationRules.ValidateConsequence(interjection.Consequence);
   }
   foreach(var c in n.Choices)
   {
    CompanionConversationRules.ValidateConditions(c.Conditions);CompanionConversationRules.ValidateConsequence(c.Consequence);
    if(c.Check is { } check)
    {
     DialogueCheckRules.ValidateOrThrow(check);
     if(!check.SuccessEndsConversation)_=Node(check.SuccessNodeId);
     if(!check.FailureEndsConversation)_=Node(check.FailureNodeId);
    }
    else if(!c.EndsConversation&&!string.IsNullOrWhiteSpace(c.NextNodeId))_=Node(c.NextNodeId);
   }
  }
 }
}

public static class DialogueCatalog
{
 static readonly DialogueDefinition[] dialogues=
 [
  new(
   "warden-serel",
   "Warden Serel",
   2,
   "greeting",
   [
    new("greeting","A translucent warden watches the Covenant approach. His voice is little more than dust moving across stone. \"Living feet in the crypt again. If you seek the Ember Heart, you are already walking where the oathless dead want you to go.\"",
    [
     new("heart","We came for the Ember Heart. What waits below?","heart"),
     new("warden","Who are you?","warden"),
     new("read","Study the warden's warning before answering.",Check:new DialogueCheckDefinition(12,"read-success","read-failure",Skill:Skill.Insight,ShowDifficultyClass:false)),
     new("leave","We will leave you to your vigil.",EndsConversation:true)
    ],Interjections:[
     new("mira-ward-age","mage","Mira studies the wall behind the warden. \"Those cuts predate the saint's seal. Whatever oath he means, it was inherited rather than invented here.\"")
    ]),
    new("read-success","The warning is real, but fear is not what holds Serel here. He is ashamed. Every time he speaks of the lower chambers, his gaze returns to the old oath carved into the wall behind him.",
    [
     new("oath-read","You are not only guarding the dead. What did you promise?","oath"),
     new("heart-read","Whatever happened, help us keep it from happening again.","heart")
    ]),
    new("read-failure","Whatever emotion remains in the dead warden's face is too distant to read with certainty. His warning may be caution, accusation, or simply the habit of an oath kept too long.",
    [
     new("warden-fail","Then tell us who you are.","warden"),
     new("heart-fail","Tell us about the Ember Heart.","heart")
    ]),
    new("heart","\"The Ashen Prior broke the old seal and carried the Heart into the deepest chamber. Open the ironbound door, keep the last-dawn shrine behind you, and do not mistake silence for safety.\"",
    [
     new("prior","Tell me about the Ashen Prior.","prior"),
     new("trust","You stayed here to protect this place. Trust us with the whole truth.",Check:new DialogueCheckDefinition(11,"trust-success","trust-failure",Skill:Skill.Persuasion),Consequence:new(ApprovalChanges:[new("cleric",1,"Orren approves of offering the dead warden respect instead of treating him as an obstacle."),new("mage",1,"Mira approves of seeking the complete history before acting.")],SetFlags:["serel-asked-with-respect"])) ,
     new("thanks","That is enough. Thank you.",EndsConversation:true)
    ]),
    new("trust-success","Serel's outline steadies. \"Then hear the part I do not tell pilgrims. I opened the inner ward for the Prior before I understood what grief had made of him. The broken seal bears my hand as well as his. If you find the black iron key below the shrine, destroy it when the Heart is safe.\"",
    [
     new("trust-oath","What promise are you guarding now?","oath"),
     new("trust-end","We will remember the key and your warning.",EndsConversation:true)
    ]),
    new("trust-failure","The warden's expression closes. \"Trust is a word the living spend cheaply. I have paid for mine already. Take the warning I gave you and call it enough.\"",
    [
     new("trust-back","Then tell us about the Prior.","prior"),
     new("trust-leave","Very well.",EndsConversation:true)
    ]),
    new("warden","\"Serel. Warden of Saint Orlan when my blood was warm. I remained because somebody had to remember which promises were made down here.\"",
    [
     new("oath","What promise are you guarding?","oath"),
     new("heart2","Then tell us how to reach the Heart.","heart")
    ]),
    new("prior","\"He was a keeper once. Grief hollowed him out before death did. Now he commands the oathless and calls theft a restoration. Do not listen when he tells you the dead deserve the Heart more than the living.\"",
    [
     new("remember","Search your own memory for the old covenant's wording.",Check:new DialogueCheckDefinition(13,"memory-success","memory-failure",Ability:Ability.Intelligence,ShowDifficultyClass:false)),
     new("continue","We will remember.",EndsConversation:true)
    ]),
    new("memory-success","The formula clicks into place: the old covenant was reciprocal. The dead were promised remembrance, but the living were promised warmth. Removing the Heart violated both halves of the compact, no matter who claims ownership.",
    [
     new("memory-oath","Then the Prior is breaking the covenant he claims to restore.","oath")
    ]),
    new("memory-failure","You remember fragments of funerary language, but not enough to separate oath from ornament. Serel watches without correcting you.",
    [
     new("memory-oath2","Then tell us the promise in your own words.","oath")
    ]),
    new("oath","\"The Heart was never treasure. It was a trust: warmth held for the town above, and memory held for those below. Return it to the Ember Hearth when this is done.\"",
    [
     new("understood","We will return it.",EndsConversation:true,Consequence:new(ApprovalChanges:[new("knight",1,"Garrick approves of committing the Covenant to restore the Heart."),new("cleric",1,"Orren approves of honoring both halves of the old compact.")],SetFlags:["promised-heart-return"])),
     new("orren-oath","Then the living still owe the dead remembrance. We will honor both halves of the promise.",EndsConversation:true,SpeakerActorId:"cleric",Conditions:[new("cleric",MinimumApproval:-25)],Consequence:new(ApprovalChanges:[new("cleric",2,"Orren approves of being allowed to speak for the covenant's religious obligation.")],SetFlags:["orren-spoke-for-covenant"]))
    ],Interjections:[
     new("orren-recognition","cleric","Orren lowers his head. \"That is not a relic oath. It is hospitality stretched across generations. The town owes the dead memory just as surely as it needs their warmth.\"")
    ])
   ]),
  new(
   "sister-maelin",
   "Sister Maelin",
   3,
   "greeting",
   [
    new("greeting","The shrine keeper studies the dust on your boots before she looks at your faces. \"You came up from the old crypt road. Saint Orlan has been waiting for someone to do that and still be breathing.\"",
    [
     new("town","What has happened in Saint Orlan?","town"),
     new("roads","What can you tell us about the roads beyond town?","roads"),
     new("leave","We should keep moving.",EndsConversation:true)
    ]),
    new("town","\"The bells ring without hands, pilgrims dream of locked tombs, and every child in the shrine quarter knows the Ember Hearth has gone cold. People are frightened, but fear has not yet become panic.\"",
    [
     new("hearth","Where is the Ember Hearth?","hearth"),
     new("roads2","And the northern road?","roads")
    ]),
    new("roads","\"North is Briarwood Road. East of that lies Ashen Ford, though the safe approach changes after every hard rain. The old watchtower beyond the ford has shown lights again.\"",
    [
     new("lights","What do you know about those lights?","lights"),
     new("thanks","Useful. Thank you.",EndsConversation:true),
     new("town2","What about the town itself?","town")
    ]),
    new("lights","Maelin folds her hands into her sleeves. \"Enough to know that I would rather not send another group toward them on rumor alone.\"",
    [
     new("persuade","Trust us with what you know. We can decide what risks to take.",Check:new DialogueCheckDefinition(12,"lights-success","persuasion-failure",Skill:Skill.Persuasion),Consequence:new(ApprovalChanges:[new("cleric",1,"Orren approves of treating Maelin as an ally."),new("bard",1,"Elara approves of asking openly for the story rather than manipulating it.")],SetFlags:["maelin-approach-persuasion"])),
     new("deceive","The Watch already briefed us. I only need you to confirm the details.",Check:new DialogueCheckDefinition(14,"lights-success","deception-failure",Skill:Skill.Deception),Consequence:new(ApprovalChanges:[new("rogue",2,"Rook appreciates the audacity of the bluff."),new("knight",-1,"Garrick dislikes lying about the Watch."),new("ranger",-1,"Sylva dislikes using a false claim when the trail can be investigated directly.")],SetFlags:["maelin-approach-deception"])),
     new("intimidate","If you keep useful information from us and someone dies, that choice belongs to you.",Check:new DialogueCheckDefinition(13,"lights-success","intimidation-failure",Skill:Skill.Intimidation),Consequence:new(ApprovalChanges:[new("knight",-2,"Garrick disapproves of threatening a cooperative shrine keeper."),new("cleric",-2,"Orren disapproves of turning responsibility into a threat."),new("rogue",1,"Rook grudgingly respects the pressure tactic even if he expects consequences.")],SetFlags:["maelin-approach-intimidation"],QueueCompanionConversations:["argument-maelin-threat"])),
     new("back","Then leave it at rumor.","roads")
    ],Interjections:[
     new("sylva-tracks","ranger","Sylva's eyes narrow. \"Boot prints that begin in the river are not a rumor. They are a trail with the first half washed away.\"")
    ]),
    new("lights-success","Maelin lowers her voice. \"Three nights ago a pilgrim saw a blue-white lantern moving along the watchtower roof. The tower has no roof access left. Yesterday we found boot prints at the ford that began in the water and ended at the north road without ever turning back.\"",
    [
     new("lights-end","That is worth knowing. Thank you.",EndsConversation:true),
     new("lights-town","Anything else happening in town?","town")
    ]),
    new("persuasion-failure","She considers you, then shakes her head. \"Courage is not the same thing as preparation. Come back when I know enough to give you something firmer than fear.\"",
    [
     new("persuasion-back","Then tell us about the road itself.","roads"),
     new("persuasion-end","Fair enough.",EndsConversation:true)
    ]),
    new("deception-failure","One eyebrow rises. \"The Watch captain has not spoken to an adventuring company in six days, and I had breakfast with her this morning. Try a less convenient story next time.\"",
    [
     new("deception-back","Then forget I said that. Tell us what you can.","roads"),
     new("rook-own-it","That one was mine. Bad lie, worse timing. Don't hold it against the rest of them.","roads",SpeakerActorId:"rogue",Conditions:[new("rogue",MinimumApproval:1)],Consequence:new(ApprovalChanges:[new("rogue",1,"Rook appreciates being allowed to own the failed deception instead of letting the party absorb it.")],SetFlags:["rook-owned-maelin-lie"])),
     new("deception-end","We will be going now.",EndsConversation:true)
    ]),
    new("intimidation-failure","Maelin does not raise her voice. \"Threats are still threats when dressed as responsibility. If you want my help, speak to me as an ally.\"",
    [
     new("intimidation-back","You're right. Tell us what you can about the road.","roads"),
     new("intimidation-end","We are done here.",EndsConversation:true)
    ]),
    new("hearth","\"At the center of the shrine district. If you recover the Heart, take it there before anything else. The old wards were built around that fire.\"",
    [
     new("understood","Then that is where it will go.",EndsConversation:true)
    ])
   ]),
  new(
   "banter-briarwood-signs",
   "Sylva",
   1,
   "sylva",
   [
    new("sylva","The road bends beneath blackthorn and old standing stones. Sylva slows without being asked. \"This is where the deer stopped crossing. They were not avoiding hunters. They were avoiding the stones.\"",
    [
     new("mira-reply","Then the stones may be carrying the disturbance farther than the crypt wards.","mira",SpeakerActorId:"mage",Consequence:new(SwitchPlayerSpeakerId:"mage"))
    ],SpeakerActorId:"ranger"),
    new("mira","Mira traces a weathered groove without touching it. \"Or they are reacting to something using the same pattern. Old wards often survive as fragments. Fragments can still resonate.\"",
    [
     new("elara-reply","And songs survive as fragments too. Three villages north of here sing about stones that 'remember the road.' I thought it was metaphor.","elara",SpeakerActorId:"bard",Consequence:new(SwitchPlayerSpeakerId:"bard"))
    ],SpeakerActorId:"mage"),
    new("elara","Elara looks from the stones to the dark trees. \"I am beginning to dislike how often the old songs turn out to be instructions.\"",
    [
     new("garrick-end","Then we treat the stones as part of the problem until proven otherwise. Stay close and keep moving.",EndsConversation:true,SpeakerActorId:"knight",Consequence:new(ApprovalChanges:[new("ranger",1,"Sylva approves of taking her warning seriously."),new("mage",1,"Mira approves of treating the ward fragments as evidence."),new("bard",1,"Elara approves of respecting oral history as evidence.")],SetFlags:["briarwood-banter-complete"]))
    ],SpeakerActorId:"bard")
   ],CompanionConversation:true,CompletionFlag:"banter-briarwood-signs-complete"),
  new(
   "argument-maelin-threat",
   "Garrick",
   0,
   "garrick",
   [
    new("garrick","Once Maelin is out of earshot, Garrick stops. \"That was unnecessary. We can demand urgency without making allies wonder whether we are the next danger through the door.\"",
    [
     new("agree","You're right. I pushed too hard.","orren",Consequence:new(ApprovalChanges:[new("knight",2,"Garrick approves of acknowledging the mistake."),new("cleric",2,"Orren approves of repairing the party's tone before it becomes habit."),new("rogue",-1,"Rook thinks the retreat was a little too quick.")],SetFlags:["maelin-argument-conceded"])),
     new("defend","We needed the information. Pressure was justified.","rook",Consequence:new(ApprovalChanges:[new("knight",-1,"Garrick remains uneasy with coercion as a default tool."),new("cleric",-1,"Orren dislikes defending the threat instead of examining it."),new("rogue",2,"Rook approves of standing behind a hard tactic once chosen.")],SetFlags:["maelin-argument-defended"])),
     new("split","The tactic was ugly, but the risk was real. Next time we try persuasion first.","orren",Consequence:new(ApprovalChanges:[new("knight",1,"Garrick approves of setting a clearer boundary for future pressure."),new("cleric",1,"Orren approves of admitting the moral cost without ignoring the danger."),new("rogue",1,"Rook accepts a pragmatic compromise.")],SetFlags:["maelin-argument-compromise"]))
    ],SpeakerActorId:"knight"),
    new("orren","Orren gives Garrick a sideways glance. \"Good. Regret is useful when it changes the next decision. Maelin deserved better than a threat dressed as arithmetic.\"",
    [new("orren-end","Point taken.",EndsConversation:true)],SpeakerActorId:"cleric"),
    new("rook","Rook folds his arms. \"Sometimes a locked mouth is just another locked door. I would rather pick it cleanly, but I am not pretending pressure never works.\"",
    [new("rook-end","We know where each other stands.",EndsConversation:true)],SpeakerActorId:"rogue")
   ],CompanionConversation:true,CompletionFlag:"argument-maelin-threat-complete"),
  new(
   "banter-heart-home",
   "Orren",
   3,
   "orren",
   [
    new("orren","Saint Orlan's roofs come into view with the Ember Heart still in the Covenant's keeping. Orren exhales. \"I have carried dying people farther than this. Somehow this feels heavier.\"",
    [new("elara","Because everyone in town knows what the Heart means, even if they do not know the old words for it.","elara",SpeakerActorId:"bard")],SpeakerActorId:"cleric"),
    new("elara","Elara watches the shrine bells above the wall. \"Then let them see us return it. Stories are dangerous when only the frightened version survives.\"",
    [new("garrick","Agreed. Straight to the Ember Hearth. No detours, no victory lap.",EndsConversation:true,SpeakerActorId:"knight",Consequence:new(ApprovalChanges:[new("cleric",1,"Orren approves of putting restoration before celebration."),new("bard",1,"Elara approves of making the return visible to the town."),new("knight",1,"Garrick approves of finishing the mission before celebrating.")],SetFlags:["heart-home-banter-complete"]))],SpeakerActorId:"bard")
   ],CompanionConversation:true,CompletionFlag:"banter-heart-home-complete"),
  new(
   "campfire-first-long",
   "Elara",
   5,
   "elara",
   [
    new("elara","The first full camp settles into the peculiar silence that follows shared danger. Elara turns a cup between her hands. \"We should decide what we remember about days like this before the frightening version decides for us.\"",
    [new("garrick","Remember that we kept watch, counted supplies, and got everyone to morning. The rest can become a song later.","garrick",SpeakerActorId:"knight")],SpeakerActorId:"bard"),
    new("garrick","Garrick checks the dark beyond the fire. \"Fine. Put this in the song: nobody sleeps through their watch, and nobody wanders off alone because they heard a mysterious bell.\"",
    [new("orren","A stirring chorus. I look forward to the rhyme for 'camp discipline.'",EndsConversation:true,SpeakerActorId:"cleric",Consequence:new(ApprovalChanges:[new("bard",1,"Elara approves of making the camp part of the party's shared story."),new("knight",1,"Garrick approves of treating rest as preparation rather than carelessness."),new("cleric",1,"Orren approves of the party ending the watch in better spirits.")],SetFlags:["campfire-first-long-complete"]))],SpeakerActorId:"knight")
   ],CompanionConversation:true,CompletionFlag:"campfire-first-long-complete")
 ];
 static DialogueCatalog(){foreach(var dialogue in dialogues)dialogue.ValidateOrThrow();}
 public static IReadOnlyList<DialogueDefinition> All=>dialogues;
 public static bool TryGet(string id,out DialogueDefinition definition)
 {
  definition=dialogues.FirstOrDefault(d=>d.Id.Equals(id,StringComparison.OrdinalIgnoreCase))!;
  return definition!=null;
 }
 public static DialogueDefinition Get(string id)=>TryGet(id,out var definition)?definition:throw new KeyNotFoundException($"Unknown dialogue '{id}'.");
}

public sealed partial class World
{
 [JsonIgnore] public Action<Feature>? DialogueRequested {get;set;}
 public bool IsDialogueFeature(Feature feature)=>feature.Kind.Equals("npc",StringComparison.OrdinalIgnoreCase)&&!string.IsNullOrWhiteSpace(feature.DialogueId)&&TryGetDialogue(feature.DialogueId,out _);

 public DialogueCheckResult ResolveDialogueCheck(Actor actor,DialogueCheckDefinition check)
 {
  DialogueCheckRules.ValidateOrThrow(check);if(!actor.Alive||actor.Enemy)throw new InvalidOperationException("Only a living party speaker can make a dialogue check.");
  if(SurvivorSkillAdvantage(actor,check.Skill,check.Ability??check.AbilityOverride))check=check with{RollMode=check.RollMode==DialogueCheckRollMode.Disadvantage?DialogueCheckRollMode.Normal:DialogueCheckRollMode.Advantage};
  int modifier=DialogueCheckRules.Modifier(actor,check),first=Roll(20),second=0,chosen=first;
  if(check.RollMode!=DialogueCheckRollMode.Normal)
  {
   second=Roll(20);chosen=check.RollMode==DialogueCheckRollMode.Advantage?Math.Max(first,second):Math.Min(first,second);
  }
  if(chosen+modifier<check.DifficultyClass&&actor.ClassIs("charismatic")&&(check.Ability??check.AbilityOverride??(check.Skill.HasValue?SkillRules.AbilityFor(check.Skill.Value):Ability.Strength))==Ability.Charisma&&SpendSurvivorResource(actor,"social-reroll")){second=Roll(20);chosen=second;}
  bool automatic=chosen+modifier<check.DifficultyClass&&!check.Supernatural&&actor.ClassIs("charismatic",9)&&check.Skill is Skill.Persuasion or Skill.Deception&&SpendSurvivorResource(actor,"negotiator");
  if(automatic)Message(MessageCategory.Roll,"Master Negotiator secures this ordinary social check without changing the recorded dice.");
  int total=chosen+modifier;bool success=automatic||total>=check.DifficultyClass;string label=DialogueCheckRules.Label(check);
  if(success)actor.Statistics.SuccessfulSkillChecks++;else actor.Statistics.FailedSkillChecks++;
  string mode=DialogueCheckRules.RollModeLabel(check.RollMode);string dice=check.RollMode==DialogueCheckRollMode.Normal?$"d20 {first}":$"d20 {first}/{second} {mode.ToLowerInvariant()} -> {chosen}";
  Message(MessageCategory.Roll,$"{actor.Name} {(success?"succeeds":"fails")} a {label} dialogue check.",$"{actor.Name}: {label} check {dice} {DialogueCheckRules.Signed(modifier)} = {total} vs DC {check.DifficultyClass}: {(success?"SUCCESS":"FAILURE")}.");
  return new(actor.Id,label,check.DifficultyClass,modifier,first,second,chosen,total,success,check.RollMode);
 }

 public void NormalizeDialogueContent()
 {
  static void Ensure(string destinationId,List<Feature> features)
  {
   Feature? desired=destinationId switch
   {
    "saint-orlans-crypt"=>new(){Kind="npc",Name="Warden Serel",Pos=new(8,14),DialogueId="warden-serel",DialoguePortrait=2},
    "saint-orlan"=>new(){Kind="npc",Name="Sister Maelin",Pos=new(18,12),DialogueId="sister-maelin",DialoguePortrait=3},
    _=>null
   };
   if(desired!=null&&!features.Any(f=>f.Kind=="npc"&&f.DialogueId==desired.DialogueId))features.Add(desired);
  }
  Ensure(CurrentTravelDestinationId,Features);
  foreach(var area in StoredTravelAreas)Ensure(area.DestinationId,area.Features);
 }
}
