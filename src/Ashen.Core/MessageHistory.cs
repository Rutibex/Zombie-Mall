using System.Text.Json.Serialization;
namespace Ashen.Core;

/// <summary>Semantic categories used by the persistent Infinity-style message log.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageCategory
{
 Narrative,
 Combat,
 Roll,
 Damage,
 Healing,
 Condition,
 Reaction,
 Quest,
 Loot,
 Rest,
 Travel,
 System,
 Error
}

/// <summary>
/// One durable log event. Summary is always suitable for concise feedback; Detail may
/// expose the underlying 5e arithmetic without forcing that density on every player.
/// </summary>
public sealed class MessageEntry
{
 public int Sequence {get;set;}
 public MessageCategory Category {get;set;}
 public string Summary {get;set;}="";
 public string Detail {get;set;}="";
 public string DisplayText(bool detailed)=>detailed&&!string.IsNullOrWhiteSpace(Detail)?Detail:Summary;
 public string CategoryLabel=>Category switch
 {
  MessageCategory.Narrative=>"STORY",
  MessageCategory.Combat=>"COMBAT",
  MessageCategory.Roll=>"ROLL",
  MessageCategory.Damage=>"DAMAGE",
  MessageCategory.Healing=>"HEAL",
  MessageCategory.Condition=>"STATUS",
  MessageCategory.Reaction=>"REACTION",
  MessageCategory.Quest=>"QUEST",
  MessageCategory.Loot=>"LOOT",
  MessageCategory.Rest=>"REST",
  MessageCategory.Travel=>"TRAVEL",
  MessageCategory.System=>"SYSTEM",
  MessageCategory.Error=>"NOTICE",
  _=>"LOG"
 };
}
