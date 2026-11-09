namespace Ashen.Core;

public sealed partial class World
{
 /// <summary>Current initiative owner followed by the remaining living participants in cyclic order.</summary>
 public IReadOnlyList<Actor> InitiativeSequence()
 {
  if(Mode!=Mode.Combat||Order.Count==0)return Array.Empty<Actor>();
  var result=new List<Actor>(Order.Count);
  for(int offset=0;offset<Order.Count;offset++)
  {
   int index=(Turn+offset)%Order.Count;string id=Order[index];
   var actor=Actors.FirstOrDefault(a=>a.Id==id);
   if(actor?.Alive==true)result.Add(actor);
  }
  return result;
 }

 /// <summary>Adds a living actor to a running combat while preserving the current turn owner.</summary>
 public bool AddInitiativeParticipant(Actor actor,int? initiative=null,string reason="joins the battle")
 {
  if(Mode!=Mode.Combat||!actor.Alive||Order.Contains(actor.Id,StringComparer.Ordinal))return false;
  string? activeId=Active?.Id;
  if(initiative is null)
  {
   int dex=actor.AbilityModifier(Ability.Dexterity),roll=Roll(20);if(LowerTraits(actor).Contains("upper-fast"))roll=Math.Max(roll,Roll(20));actor.Initiative=roll+dex;
   Message(MessageCategory.Roll,$"{actor.Name} initiative {actor.Initiative}.",$"Initiative entry: {actor.Name} rolls {roll} + {dex} = {actor.Initiative}.");
  }
  else actor.Initiative=initiative.Value;
  actor.Economy.Reset(actor.EffectiveSpeedFeet,actor.Character.AttacksPerAction);
  Order.Add(actor.Id);SortInitiativeOrder();
  if(activeId!=null){int current=Order.FindIndex(id=>id==activeId);if(current>=0)Turn=current;}
  Message(MessageCategory.Combat,$"{actor.Name} {reason} (initiative {actor.Initiative}).");
  return true;
 }

 /// <summary>Removes an actor from a running combat and keeps the current owner stable when possible.</summary>
 public bool RemoveInitiativeParticipant(string actorId,string reason="leaves initiative")
 {
  if(Mode!=Mode.Combat)return false;
  int removed=Order.FindIndex(id=>id==actorId);if(removed<0)return false;
  var actor=Actors.FirstOrDefault(a=>a.Id==actorId);string? activeId=Active?.Id;
  bool removingActive=activeId==actorId;string? successor=null;bool wraps=false;
  if(removingActive&&Order.Count>1)
  {
   for(int step=1;step<Order.Count;step++)
   {
    int index=(removed+step)%Order.Count;var candidate=Actors.FirstOrDefault(a=>a.Id==Order[index]);
    if(candidate?.Alive==true&&candidate.Id!=actorId){successor=candidate.Id;wraps=index<removed;break;}
   }
  }
  Order.RemoveAt(removed);
  if(actor!=null)Message(MessageCategory.Combat,$"{actor.Name} {reason}.");
  if(Order.Count==0){Turn=0;return true;}
  if(!removingActive&&activeId!=null)
  {
   int current=Order.FindIndex(id=>id==activeId);Turn=current>=0?current:Math.Clamp(Turn,0,Order.Count-1);return true;
  }
  if(successor!=null)
  {
   if(wraps)Round++;
   int next=Order.FindIndex(id=>id==successor);Turn=next>=0?next:0;BeginTurn();
  }
  else Turn=Math.Clamp(Turn,0,Order.Count-1);
  return true;
 }

 void SortInitiativeOrder()
 {
  Order=Order.Select(id=>Actors.FirstOrDefault(a=>a.Id==id)).Where(a=>a!=null)
   .Cast<Actor>().OrderByDescending(a=>a.Initiative)
   .ThenByDescending(a=>a.AbilityModifier(Ability.Dexterity))
   .ThenBy(a=>a.Id,StringComparer.Ordinal).Select(a=>a.Id).ToList();
 }
}
