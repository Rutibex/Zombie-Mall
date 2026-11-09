using System.Text.Json.Serialization;
namespace Ashen.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DifficultyPreset { Story, Balanced, Veteran, Ruthless }

public readonly record struct DifficultyProfile(
 DifficultyPreset Id,string Name,float EnemyDamageScale,string Summary,string ExactRuleText);

/// <summary>
/// IIP45 difficulty is intentionally narrow and explicit. It changes only damage dealt
/// by hostile creatures to party members. It does not secretly alter d20 rolls, AC, DC,
/// XP, loot, encounter composition, or companion statistics.
/// </summary>
public static class DifficultyRules
{
 public static readonly DifficultyProfile[] Profiles=
 [
  new(DifficultyPreset.Story,"Story",.75f,"Forgiving combat for players focused on exploration and narrative.","Hostile damage to the party: 75%. Attack rolls, AC, DCs, XP, loot and encounter composition are unchanged."),
  new(DifficultyPreset.Balanced,"Balanced",1f,"The authored baseline rules and encounter numbers.","Hostile damage to the party: 100%. Attack rolls, AC, DCs, XP, loot and encounter composition are unchanged."),
  new(DifficultyPreset.Veteran,"Veteran",1.25f,"A sharper combat challenge without hidden accuracy changes.","Hostile damage to the party: 125%. Attack rolls, AC, DCs, XP, loot and encounter composition are unchanged."),
  new(DifficultyPreset.Ruthless,"Ruthless",1.5f,"High incoming damage for players who want dangerous encounters.","Hostile damage to the party: 150%. Attack rolls, AC, DCs, XP, loot and encounter composition are unchanged.")
 ];
 public static DifficultyProfile Profile(DifficultyPreset preset)
 {
  foreach(var profile in Profiles)if(profile.Id==preset)return profile;
  return Profiles[1];
 }
 public static int ScaleIncomingHostileDamage(int amount,DifficultyPreset preset)
 {
  if(amount<=0)return 0;float scale=Profile(preset).EnemyDamageScale;return Math.Max(1,(int)MathF.Round(amount*scale,MidpointRounding.AwayFromZero));
 }
}

public sealed partial class World
{
 /// <summary>Profile preference applied at runtime; not journey state and therefore not serialized.</summary>
 [JsonIgnore] public DifficultyPreset Difficulty {get;set;}=DifficultyPreset.Balanced;
 [JsonIgnore] public DifficultyProfile DifficultyProfile=>DifficultyRules.Profile(Difficulty);
}
