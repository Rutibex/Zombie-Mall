using Ashen.Core;
using System.Text.Json.Nodes;
static class ScavengingTests
{
 public static void Run(Action<bool,string> check)
 {
  CampaignDefinition Definition()=>CampaignLoader.Load(CampaignLoader.BundledScavengingPath);
  World Fresh(){var x=World.New(Definition());foreach(var e in x.Actors.Where(a=>a.Enemy))e.Hp=0;foreach(var f in x.Features.Where(f=>f.Kind=="door"))f.Used=true;return x;}
  Feature Site(World w)=>w.Features.Single(f=>f.Id=="feature.mall.supplies");
  void Beside(World w,Feature f){w.PartyLeader.Pos=f.Pos.Neighbors().First(p=>!w.Blocked(p)&&!w.Actors.Any(a=>a!=w.PartyLeader&&a.Alive&&a.Pos==p));w.Reveal();}
  void Establish(World w){var f=w.Features.Single(f=>f.Id=="feature.mall.shelter");Beside(w,f);check(w.Interact(f),"Phase 7 shelter inspected before use");}
  var w=Fresh();var hero=w.PartyLeader;var site=Site(w);var cache=w.Features.Single(f=>f.Id=="feature.mall.closet-box");
  check(w.Campaign!.SchemaVersion==5&&w.Campaign.Items.Single(i=>i.Name=="Canned Beans").ResourceSv==3&&w.Campaign.Items.Single(i=>i.Name=="Bottled Water").ResourceSv==2,"Phase 7 explicit module survival values are separate from barter prices");
  check(hero.Equipment.All().All(s=>s.ResourceUnits.Count==0)&&site.Contents.Items.All(s=>s.ResourceUnits.Count==0),"Phase 7 starter gear and existing shelter stock have no scavenged provenance");
  check(w.Features.Where(f=>f.Id!=site.Id).SelectMany(f=>f.Contents.Items).Sum(s=>w.UnsecuredSv(s))==37,"Phase 7 first section has exactly 37 recoverable SV");
  check(hero.Character.NextLevelExperience==250&&SurvivalProgression.EarnedRank(249)==1&&SurvivalProgression.EarnedRank(250)==2&&SurvivalProgression.EarnedRank(12000)==10,"Phase 7 survivor XP uses module thresholds without granting class levels");
  Beside(w,cache);check(w.TakeContainerItem(hero,cache,1,2)&&hero.Character.Experience==0&&w.SecuredSurvivalValue==0,"Phase 7 picking up supplies awards no XP");
  var beans=hero.Inventory.Items.Single(i=>i.ItemId=="item.commodity.001");var ids=beans.ResourceUnits.ToArray();check(ids.Length==2&&ids.Distinct().Count()==2,"Phase 7 each scavenged unit has a unique source identity");
  Beside(w,site);int before=hero.Inventory.Count(beans.ItemId);check(!w.StoreContainerItem(hero,site,hero.Inventory.Items.IndexOf(beans),1)&&hero.Inventory.Count(beans.ItemId)==before&&w.ResourceXpAwarded==0,"Phase 7 unestablished shelter denies deposit atomically");
  Establish(w);Beside(w,site);check(w.StoreContainerItem(hero,site,hero.Inventory.Items.IndexOf(beans),1)&&w.SecuredSurvivalValue==3&&w.ResourceXpAwarded==2&&w.ResourceXpRemainder==1&&w.Party.All(a=>a.Character.Experience==1),"Phase 7 first secured can splits XP evenly and retains integer remainder");
  check(beans.ResourceUnits.Count==1&&beans.ResourceUnits[0]!=ids[0],"Phase 7 partial deposit leaves only the remaining unit on the carried stack");
  check(w.SecureCarriedResources(hero,site)==1&&w.SecuredSurvivalValue==6&&w.ResourceXpRemainder==0&&w.Party.All(a=>a.Character.Experience==3),"Phase 7 bulk deposit merges with existing stock and distributes the saved remainder");
  var stored=site.Contents.Items.Single(s=>s.ItemId==beans.ItemId);check(stored.Quantity==5&&stored.ResourceUnits.Count==2,"Phase 7 merged starting and scavenged stock preserves only eligible unit identities");
  check(w.TakeContainerItem(hero,site,site.Contents.Items.IndexOf(stored),2),"Phase 7 previously credited supplies can be withdrawn");
  var withdrawn=hero.Inventory.Items.Single(s=>s.ItemId==beans.ItemId);check(w.UnsecuredSv(withdrawn)==0&&w.StoreContainerItem(hero,site,hero.Inventory.Items.IndexOf(withdrawn),2)&&w.SecuredSurvivalValue==6&&w.Party.All(a=>a.Character.Experience==3),"Phase 7 withdraw and redeposit cannot farm XP");
  var cafe=w.Features.Single(f=>f.Id=="feature.mall.cafe-box");Beside(w,cafe);check(w.TakeContainerItem(hero,cafe,0,2),"Phase 7 water can be scavenged from another shop");
  var partner=w.Party.Skip(1).First();int index=hero.Inventory.Items.FindIndex(s=>s.ItemId=="item.commodity.004");check(w.TransferItem(hero,partner,index,1),"Phase 7 partial companion transfer preserves provenance");
  var water=partner.Inventory.Items.Single(s=>s.ItemId=="item.commodity.004");check(water.ResourceUnits.Count==1&&hero.Inventory.Items.Single(s=>s.ItemId==water.ItemId).ResourceUnits.Count==1,"Phase 7 split transfer creates no duplicate resource IDs");
  partner.Pos=hero.Pos.Neighbors().First(p=>!w.Blocked(p));w.Reveal();check(w.DropInventoryItem(partner,0,1)&&w.PickupGroundItem(partner,0,1)&&partner.Inventory.Items.Single(s=>s.ItemId==water.ItemId).ResourceUnits.Count==1,"Phase 7 drop and pickup retains the physical resource identity");
  Beside(w,site);check(w.SecureCarriedResources(hero,site)==1&&w.SecuredSurvivalValue==8,"Phase 7 only the returned share is credited");
  partner.Pos=new(14,27);w.Reveal();check(w.SecureCarriedResources(partner,site)==1&&w.SecuredSurvivalValue==10&&w.ResourceXpAwarded==10&&w.Quests.Single(q=>q.Id==w.Campaign!.Id+".objectives").Objectives.Single(o=>o.Id=="objective.mall.secure-resources").Completed,"Phase 7 secured-resource milestone follows deposited value");
  var file=Path.Combine(Path.GetTempPath(),"zombie-resources-"+Guid.NewGuid()+".json");
  try
  {
   w.Save(file);var loaded=World.Load(file);check(loaded.Version==World.CurrentSaveVersion&&loaded.SecuredSurvivalValue==10&&loaded.ResourceXpAwarded==10&&loaded.Party.All(a=>a.Character.Experience==5),"Phase 7 XP ledger and survivor XP persist through save/load");
   site=Site(loaded);Beside(loaded,site);stored=site.Contents.Items.First(s=>s.ResourceUnits.Count>0);int quantity=stored.ResourceUnits.Count;check(loaded.TakeContainerItem(loaded.PartyLeader,site,site.Contents.Items.IndexOf(stored),quantity)&&loaded.SecureCarriedResources(loaded.PartyLeader,site)==0&&loaded.SecuredSurvivalValue==10,"Phase 7 reload cannot reset eligibility on already credited resources");
   var root=JsonNode.Parse(File.ReadAllText(file))!.AsObject();root["ResourceXpRemainder"]=4;File.WriteAllText(file,root.ToJsonString());bool rejected=false;try{World.Load(file);}catch(InvalidDataException){rejected=true;}check(rejected,"Phase 7 inconsistent saved SV/XP ledger is rejected");
   w.Save(file);root=JsonNode.Parse(File.ReadAllText(file))!.AsObject();root["SecuredResourceUnits"]!.AsArray().Add("fake-unit");File.WriteAllText(file,root.ToJsonString());rejected=false;try{World.Load(file);}catch(InvalidDataException){rejected=true;}check(rejected,"Phase 7 unknown secured source IDs are rejected");
   var old=World.New(CampaignLoader.Load(CampaignLoader.BundledSurvivorPath));old.Save(file);root=JsonNode.Parse(File.ReadAllText(file))!.AsObject();root["Version"]=19;root.Remove("SecuredResourceUnits");root.Remove("ResourceXpAwarded");root.Remove("ResourceXpRemainder");File.WriteAllText(file,root.ToJsonString());loaded=World.Load(file);check(loaded.Version==World.CurrentSaveVersion&&loaded.Campaign!.SchemaVersion==4&&loaded.SecuredSurvivalValue==0&&loaded.Actors.SelectMany(a=>a.Inventory.Items).All(s=>s.ResourceUnits.Count==0),"Phase 7 v19 migration does not invent provenance or retroactive XP");
  }
  finally{if(File.Exists(file))File.Delete(file);}
  w=Fresh();hero=w.PartyLeader;site=Site(w);cache=w.Features.Single(f=>f.Id=="feature.mall.closet-box");Beside(w,cache);w.TakeContainerItem(hero,cache,1,2);check(hero.Inventory.Remove("item.commodity.001",1),"Phase 7 consuming/removing a unit destroys its provenance");Establish(w);Beside(w,site);w.SecureCarriedResources(hero,site);check(w.SecuredSurvivalValue==3,"Phase 7 a resource lost before return grants no XP");
  w=Fresh();hero=w.PartyLeader;site=Site(w);cache=w.Features.Single(f=>f.Id=="feature.mall.closet-box");Beside(w,cache);w.TakeContainerItem(hero,cache,1,2);Establish(w);Beside(w,site);site.Contents.MaxSlots=site.Contents.UsedSlots;
  // Force a different item type to need a new slot.
  w.AddItem(hero,"item.melee.002");var knife=hero.Inventory.Items.Single(s=>s.ItemId=="item.melee.002");int serial=w.NextItemInstanceNumber;check(!w.StoreContainerItem(hero,site,hero.Inventory.Items.IndexOf(knife),1)&&w.ResourceXpAwarded==0&&w.NextItemInstanceNumber==serial,"Phase 7 full stockpile denies transfer before consuming IDs or awarding XP");
  w=Fresh();hero=w.PartyLeader;site=Site(w);cache=w.Features.Single(f=>f.Id=="feature.mall.closet-box");Beside(w,cache);w.TakeContainerItem(hero,cache,1,2);Establish(w);Beside(w,site);var enemy=w.Actors.First(a=>a.Enemy);enemy.Hp=enemy.MaxHp;enemy.Pos=new(15,28);
  check(!w.PreviewSecuring(hero,site,hero.Inventory.Items[0]).Allowed&&w.SecureCarriedResources(hero,site)==0&&w.SecuredSurvivalValue==0,"Phase 7 nearby hostile sight prevents securing in an unsafe shelter");
  enemy.Hp=0;w.Mode=Mode.Combat;check(!w.StoreContainerItem(hero,site,0)&&w.SecuredSurvivalValue==0,"Phase 7 combat cannot secure supplies");w.Mode=Mode.Explore;hero.Character.Experience=int.MaxValue;
  check(!w.StoreContainerItem(hero,site,0)&&w.SecuredSurvivalValue==0,"Phase 7 experience overflow is denied before transfer");
  // Inventory primitives must preserve unit identities even across mixed stacks and capacity boundaries.
  var inv=new InventoryState{MaxSlots=10};var sample=new ItemStack{InstanceId="test-a",ItemId="item.commodity.001",CampaignGear=w.ResolveItemDefinition("item.commodity.001"),Quantity=18};inv.Add(sample);sample=new ItemStack{InstanceId="test-b",ItemId="item.commodity.001",CampaignGear=w.ResolveItemDefinition("item.commodity.001"),Quantity=5};sample.ResourceUnits=["a","b","c","d","e"];inv.Add(sample);
  check(inv.Items.Count==2&&inv.Items[0].Quantity==20&&inv.Items[0].ResourceUnits.SequenceEqual(new[]{"a","b"})&&inv.Items[1].ResourceUnits.SequenceEqual(new[]{"c","d","e"}),"Phase 7 stack overflow partitions provenance across destination stacks");
  var clone=inv.Items[0].Clone();clone.ResourceUnits.Clear();check(inv.Items[0].ResourceUnits.Count==2,"Phase 7 cloned inventories do not alias provenance lists");
  var part=inv.TakeAt(0,1)!;check(part.ResourceUnits.SequenceEqual(new[]{"a"})&&inv.Items[0].ResourceUnits.SequenceEqual(new[]{"b"}),"Phase 7 partial stack extraction moves each token once");
  w=Fresh();hero=w.PartyLeader;cache=w.Features.Single(f=>f.Id=="feature.mall.cafe-box");Beside(w,cache);w.TakeContainerItem(hero,cache,1,1);int knifeIndex=hero.Inventory.Items.FindIndex(s=>s.ItemId=="item.melee.002");string knifeUnit=hero.Inventory.Items[knifeIndex].ResourceUnits.Single();
  check(w.Equip(hero,knifeIndex,EquipmentSlot.MainHand)&&hero.Equipment.MainHand!.ResourceUnits.Single()==knifeUnit,"Phase 7 equipping a scavenged weapon preserves its unit identity");
  check(w.Unequip(hero,EquipmentSlot.MainHand)&&hero.Inventory.Items.Any(s=>s.ResourceUnits.Contains(knifeUnit)),"Phase 7 unequipping returns the same uncredited resource");
  try
  {
   w.Save(file);var loaded=World.Load(file);check(loaded.PartyLeader.Inventory.Items.Any(s=>s.ResourceUnits.Contains(knifeUnit))&&loaded.SecuredSurvivalValue==0,"Phase 7 save/load while carrying finds retains future eligibility");
   var root=JsonNode.Parse(File.ReadAllText(file))!.AsObject();var actorNodes=root["Actors"]!.AsArray();var packs=actorNodes[0]!["Inventory"]!["Items"]!.AsArray();var copy=JsonNode.Parse(packs.First(n=>n!["ResourceUnits"]!.AsArray().Count>0)!.ToJsonString())!;copy["InstanceId"]="duplicate-resource-copy";packs.Add(copy);File.WriteAllText(file,root.ToJsonString());bool rejected=false;try{World.Load(file);}catch(InvalidDataException){rejected=true;}check(rejected,"Phase 7 duplicated live provenance is rejected even with a different stack ID");
  }
  finally{if(File.Exists(file))File.Delete(file);}
  var travel=CampaignLoader.Load(CampaignLoader.BundledTravelPath);travel.SchemaVersion=5;travel.Items.Single(i=>i.Id=="item.commodity.001").ResourceSv=3;travel.Areas[0].Features.Single(f=>f.Id=="feature.fixture.supply-box").Scavengable=true;travel.Areas[1].Features.Single(f=>f.Kind=="container").SecuringSite=true;
  w=World.New(travel);foreach(var e in w.Actors.Where(a=>a.Enemy))e.Hp=0;cache=w.Features.Single(f=>f.Kind=="container");int original=cache.Contents.Items[0].Quantity;Beside(w,cache);w.TakeContainerItem(w.PartyLeader,cache,0,1);
  void GatherAndTravel(string destination)
  {
   var route=w.Campaign!.Connections.Single(r=>r.FromAreaId==w.CurrentTravelDestinationId&&r.ToAreaId==destination);var exit=w.Features.Single(f=>f.Id==route.ExitFeatureId);var cells=w.Floors.Where(p=>!w.Blocked(p)&&p.Center.Distance(exit.Pos.Center)<2.5f).OrderBy(p=>p.Distance(exit.Pos)).ToArray();int n=0;foreach(var actor in w.Party)actor.Pos=cells[n++];w.Reveal();check(w.TravelTo(destination),"Phase 7 resource expedition travels to "+destination);
  }
  GatherAndTravel("area.travel.service");foreach(var e in w.Actors.Where(a=>a.Enemy))e.Hp=0;site=w.Features.Single(f=>f.Kind=="container");Beside(w,site);check(w.SecureCarriedResources(w.PartyLeader,site)==1&&w.SecuredSurvivalValue==3,"Phase 7 finds carried between areas can be secured in another area");
  GatherAndTravel("loc.lower.01");cache=w.Features.Single(f=>f.Kind=="container");check(cache.Contents.Items[0].Quantity==original-1&&w.SecuredSurvivalValue==3,"Phase 7 revisiting a source preserves depletion and the global credited ledger");
  void Reject(Action<CampaignDefinition> mutate,string label){var x=Definition();mutate(x);check(CampaignLoader.Validate(x).Count>0,"Phase 7 rejects "+label);}
  Reject(x=>x.SchemaVersion=4,"resource metadata in older schema");Reject(x=>x.Items[0].ResourceSv=-1,"negative survival value");Reject(x=>x.Areas[0].Features.First(f=>f.SecuringSite).Scavengable=true,"stockpile declared as loot source");Reject(x=>x.Areas[0].Features.First(f=>f.SecuringSite).SecuringFlag="missing","unknown shelter establishment flag");
 }
}
