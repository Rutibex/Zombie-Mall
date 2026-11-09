using System.Text.Json.Serialization;
namespace Ashen.Core;

public static class SurvivalProgression
{
 static readonly int[] thresholds=[0,0,250,500,1000,1750,2500,4000,6000,9000,12000];
 public static int RequiredForLevel(int level)=>level>10?int.MaxValue:thresholds[Math.Clamp(level,1,10)];
 public static int EarnedRank(int xp){int rank=1;while(rank<10&&xp>=thresholds[rank+1])rank++;return rank;}
}
public sealed record SecuringPreview(bool Allowed,string Reason,int SurvivalValue,int EachXp,int Remainder);
public sealed partial class World
{
 public List<string> SecuredResourceUnits {get;set;}=[];
 public int ResourceXpRemainder {get;set;}
 public int ResourceXpAwarded {get;set;}
 static string ResourceUnitId(string feature,int stack,int unit)=>$"{feature}:{stack}:{unit}";
 Dictionary<string,(string ItemId,int Sv)> ResourceCatalogue()
 {
  var result=new Dictionary<string,(string,int)>();if(Campaign==null)return result;
  foreach(var f in Campaign.Areas.SelectMany(a=>a.Features).Where(f=>f.Scavengable))for(int i=0;i<f.Contents.Count;i++)
  {var s=f.Contents[i];int sv=Campaign.Items.First(x=>x.Id==s.ItemId).ResourceSv;if(sv>0)for(int n=0;n<s.Quantity;n++)result.Add(ResourceUnitId(f.Id,i,n),(s.ItemId,sv));}
  return result;
 }
 [JsonIgnore] public int SecuredSurvivalValue {get{var catalogue=ResourceCatalogue();return SecuredResourceUnits.Sum(id=>catalogue[id].Sv);}}
 public bool IsSecuringSite(Feature f)=>Features.Contains(f)&&CurrentCampaignArea?.Features.Any(d=>d.Id==f.Id&&d.SecuringSite)==true;
 static IEnumerable<string> StackResourceUnits(ItemStack stack)=>stack.ResourceUnits.Concat(stack.LoadedAmmunition?.ResourceUnits??[]);
 public int UnsecuredSv(ItemStack stack)
 {
  var catalogue=ResourceCatalogue();return StackResourceUnits(stack).Where(id=>!SecuredResourceUnits.Contains(id)).Sum(id=>catalogue.TryGetValue(id,out var unit)&&(unit.ItemId==stack.ItemId||unit.ItemId==stack.LoadedAmmunition?.ItemId)?unit.Sv:0);
 }
 public SecuringPreview PreviewSecuring(Actor actor,Feature site,ItemStack? stack=null)
 {
  SecuringPreview Deny(string reason)=>new(false,reason,0,0,ResourceXpRemainder);
  if(!IsSecuringSite(site)||!Actors.Contains(actor)||actor.Enemy)return Deny("That is not an active shelter stockpile.");
  if(!ContainerAccessible(actor,site,out var reason))return Deny(reason);
  if(!Line(actor.Position,site.Pos.Center))return Deny("A wall or closed door blocks the stockpile.");
  var definition=CurrentCampaignArea!.Features.First(f=>f.Id==site.Id);
  if(definition.SecuringFlag!=""&&!HasDialogueFlag(definition.SecuringFlag))return Deny("Inspect and establish this shelter before securing supplies.");
  if(Actors.Any(e=>e.Enemy&&e.Alive&&e.Position.Distance(site.Pos.Center)<=7&&Line(e.Position,site.Pos.Center)))return Deny("A nearby hostile can see the stockpile. Make the shelter safe first.");
  int sv=stack==null?0:UnsecuredSv(stack);var recipients=Party.Where(a=>!a.Dead).ToArray();
  if(recipients.Length==0)return Deny("No survivors can receive experience.");
  int pooled=checked(ResourceXpRemainder+sv),each=pooled/recipients.Length;
  if(recipients.Any(a=>(long)a.Character.Experience+each>int.MaxValue))return Deny("This deposit would exceed a survivor's experience limit.");
  return new(true,"",sv,each,pooled%recipients.Length);
 }
 void CreditSecuredResources(Actor actor,Feature site,ItemStack moved)
 {
  if(!IsSecuringSite(site))return;var preview=PreviewSecuring(actor,site,moved);if(!preview.Allowed||preview.SurvivalValue==0)return;
  var recipients=Party.Where(a=>!a.Dead).ToArray();
  SecuredResourceUnits.AddRange(StackResourceUnits(moved).Where(id=>!SecuredResourceUnits.Contains(id)).Distinct());
  foreach(var member in recipients)member.Character.Experience+=preview.EachXp;
  ResourceXpAwarded+=preview.EachXp*recipients.Length;ResourceXpRemainder=preview.Remainder;
  Message(MessageCategory.Loot,$"Secured {preview.SurvivalValue} SV at {site.Name}.",$"{preview.EachXp} XP each for {recipients.Length} survivors; {ResourceXpRemainder} XP remains in the shared pool. Secured total: {SecuredSurvivalValue} SV.");
  AddStoryJournalEntry($"secured-resources-{SecuredResourceUnits.Count:D6}","Supplies Secured",$"{actor.Name} secured {preview.SurvivalValue} SV at {site.Name}. Each of {recipients.Length} survivors earned {preview.EachXp} XP. Shared pool: {ResourceXpRemainder} XP. Lifetime secured: {SecuredSurvivalValue} SV.","Scavenging");
  SyncCampaignObjectives();
 }
 public int SecureCarriedResources(Actor actor,Feature site)
 {
  if(!PreviewSecuring(actor,site).Allowed){LogSystem(PreviewSecuring(actor,site).Reason,true);return 0;}
  int count=0;for(int i=actor.Inventory.Items.Count-1;i>=0;i--){var stack=actor.Inventory.Items[i];if(UnsecuredSv(stack)>0&&StoreContainerItem(actor,site,i,stack.Definition.Kind==ItemKind.Weapon?stack.Quantity:stack.ResourceUnits.Count))count++;}return count;
 }
 void ValidateResourceState()
 {
  if(SecuredResourceUnits==null||SecuredResourceUnits.Distinct().Count()!=SecuredResourceUnits.Count||ResourceXpRemainder is <0 or >5||ResourceXpAwarded<0)throw new InvalidDataException("Invalid secured-resource ledger.");
  var catalogue=ResourceCatalogue();long sv=0;
  foreach(var id in SecuredResourceUnits){if(!catalogue.TryGetValue(id,out var unit))throw new InvalidDataException("Unknown secured resource.");sv+=unit.Sv;}
  if((long)ResourceXpAwarded+ResourceXpRemainder!=sv)throw new InvalidDataException("Resource XP and secured SV disagree.");
  var seen=new HashSet<string>();IEnumerable<ItemStack> stacks=Campaign==null?Actors.SelectMany(a=>a.Inventory.Items.Concat(a.Equipment.All())).Concat(SharedStorage.Items).Concat(Features.SelectMany(f=>f.Contents.Items)).Concat(GroundPiles.SelectMany(p=>p.Inventory.Items)):AllCampaignStacks();
  foreach(var stack in stacks){if(stack.ResourceUnits==null)throw new InvalidDataException("Null resource provenance.");if(stack.ResourceUnits.Count>0)stack.ValidateOrThrow();foreach(var id in stack.ResourceUnits)if(!seen.Add(id)||!catalogue.TryGetValue(id,out var unit)||unit.ItemId!=stack.ItemId)throw new InvalidDataException("Duplicate, unknown or mismatched resource provenance.");}
 }
}
