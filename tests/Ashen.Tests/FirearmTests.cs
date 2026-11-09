using Ashen.Core;
using System.Text.Json.Nodes;
static class FirearmTests
{
 public static void Run(Action<bool,string> check)
 {
  var definition=CampaignLoader.Load(CampaignLoader.BundledFirearmsPath);
  World Fresh()=>World.New(CampaignLoader.Clone(definition));
  var w=Fresh();var a=w.PartyLeader;var b=w.Party.Skip(1).First();
  int Index(string id)=>a.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall."+id);
  check(definition.SchemaVersion==7&&w.CompletedExpedition==null,"Phase 9 schema 7 starts the preserved first expedition");
  check(w.Equip(a,Index("pistol"))&&a.Range==8&&a.DamageDice==2&&a.Die==6&&a.AttackDamageType==DamageType.Piercing,"Phase 9 equipped pistol derives 2d6 piercing and 40 foot range");
  check(!w.HasRequiredAmmunition(a),"Phase 9 empty firearm cannot fire despite loose rounds");
  check(w.ReloadFirearm(a)&&a.EquippedAmmunitionCount==12&&Index("ammo-9mm")==-1,"Phase 9 reload moves twelve actual compatible rounds into magazine");
  check(!w.ReloadFirearm(a)&&a.EquippedAmmunitionCount==12,"Phase 9 full magazine cannot consume more rounds");
  check(a.Equipment.Weapon!.Weight>3&&a.Equipment.Weapon.LoadedAmmunition!.ResourceUnits.Count==0,"Phase 9 loaded ammo has weight and starter rounds have no XP provenance");
  var clone=a.Equipment.Weapon.Clone();clone.LoadedAmmunition!.Quantity--;check(a.EquippedAmmunitionCount==12,"Phase 9 firearm clone deeply isolates loaded rounds");
  check(w.Unequip(a,EquipmentSlot.MainHand)&&w.TransferItem(a,b,Index("pistol")),"Phase 9 loaded gun can be unequipped and transferred");
  int bi=b.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol");check(b.Inventory.Items[bi].LoadedAmmunition?.Quantity==12,"Phase 9 transfer preserves magazine");
  check(w.DropInventoryItem(b,bi)&&w.PickupGroundItem(b,0),"Phase 9 loaded gun can be dropped and picked up");
  bi=b.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol");check(w.Equip(b,bi)&&b.EquippedAmmunitionCount==12,"Phase 9 dropped gun retains magazine on re-equip");
  string path=Path.Combine(Path.GetTempPath(),"zombie-firearm-"+Guid.NewGuid()+".json");w.Save(path);var loaded=World.Load(path);check(loaded.Party.First(x=>x.Id==b.Id).EquippedAmmunitionCount==12,"Phase 9 save/load retains loaded ammunition and definitions");
  check(w.UnloadFirearm(b)&&b.EquippedAmmunitionCount==0&&w.ItemCount(b,"item.mall.ammo-9mm")==12,"Phase 9 unloading returns exact physical ammunition");
  check(w.Equip(a,Index("revolver"))&&a.Range==6&&w.ReloadFirearm(a)&&a.EquippedAmmunitionCount==6,"Phase 9 revolver accepts only .38 Special and loads six");
  check(w.UnloadFirearm(a)&&w.Equip(a,Index("shotgun"))&&a.DamageDice==3&&w.ReloadFirearm(a)&&a.EquippedAmmunitionCount==2,"Phase 9 shotgun loads two shells and uses 3d6 damage");
  check(w.UnloadFirearm(a)&&!w.UnloadFirearm(a),"Phase 9 unloading empty magazine cannot duplicate ammunition");
  a.Inventory.Items.RemoveAll(s=>s.ItemId=="item.mall.ammo-shells");check(!w.ReloadFirearm(a)&&w.ItemCount(a,"item.mall.ammo-38")==6,"Phase 9 wrong calibre fails without consuming other ammunition");
  // Unit combat fixture: fixed legal corridor, high-HP target, no alteration to production campaign.
  w=Fresh();a=w.PartyLeader;var foe=w.Actors.First(x=>x.Enemy);foreach(var f in w.Features.Where(f=>f.Kind=="door"))f.Used=true;
  a.Pos=new(8,16);foe.Pos=new(12,16);foe.MaxHp=1000;foe.Hp=1000;w.Reveal();w.Equip(a,a.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));
  w.StartCombat(foe.Group);w.Order=[a.Id,foe.Id];w.Turn=0;
  void Reset()=>a.Economy.Reset(a.Character.SpeedFeet,a.Character.AttacksPerAction);
  Reset();check(!w.Attack(a,foe,AttackKind.Basic)&&a.Economy.ActionAvailable,"Phase 9 dry fire spends neither Action nor ammunition");
  check(w.ReloadFirearm(a)&&!a.Economy.ActionAvailable&&a.EquippedAmmunitionCount==12,"Phase 9 combat reload spends one Action");
  check(!w.Attack(a,foe,AttackKind.Basic)&&!w.UnloadFirearm(a),"Phase 9 reload prevents attacking or unloading on same spent Action");
  Reset();var preview=w.PreviewAttack(a,foe,AttackKind.Basic);check(preview.MaxDamage>=12,"Phase 9 attack preview includes both pistol damage dice");
  check(w.Attack(a,foe,AttackKind.Basic)&&a.EquippedAmmunitionCount==11,"Phase 9 valid ranged shot consumes exactly one round regardless of hit");
  Reset();foe.Pos=new(30,16);check(!w.Attack(a,foe,AttackKind.Basic)&&a.EquippedAmmunitionCount==11&&a.Economy.ActionAvailable,"Phase 9 out-of-range shot spends no Action or ammunition");
  foe.Pos=new(12,16);check(w.ReactionAttack(a,foe)&&a.EquippedAmmunitionCount==10,"Phase 9 reaction attack uses firearm range and consumes one round");
  for(int i=0;i<10;i++)w.ReactionAttack(a,foe);check(a.EquippedAmmunitionCount==0&&!w.ReactionAttack(a,foe),"Phase 9 exhausted magazine cannot make reaction attacks");
  w.Mode=Mode.Explore;w.Save(path);check(World.Load(path).PartyLeader.Equipment.Weapon!.LoadedAmmunition==null,"Phase 9 empty magazine saves without phantom rounds");
  // Finite source provenance survives loading, firing, unloading, and securing.
  w=Fresh();a=w.PartyLeader;foreach(var f in w.Features.Where(f=>f.Kind=="door"))f.Used=true;foreach(var e in w.Actors.Where(x=>x.Enemy))e.Hp=0;
  a.Inventory.Items.RemoveAll(s=>s.Definition.Kind==ItemKind.Ammunition);w.Equip(a,a.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));
  var cache=w.Features.First(f=>f.Contents.Items.Any(s=>s.ItemId=="item.mall.ammo-9mm"));a.Pos=cache.Pos.Neighbors().First(p=>!w.Blocked(p));w.Reveal();
  check(w.TakeContainerItem(a,cache,cache.Contents.Items.FindIndex(s=>s.ItemId=="item.mall.ammo-9mm")),"Phase 9 finite recovered ammo can be scavenged");
  check(w.ReloadFirearm(a)&&a.Equipment.Weapon!.LoadedAmmunition!.ResourceUnits.Count==6&&w.SecuredSurvivalValue==0,"Phase 9 loading preserves all recovered unit identities and earns no XP");
  w.Save(path);w=World.Load(path);a=w.PartyLeader;check(a.Equipment.Weapon!.LoadedAmmunition!.ResourceUnits.Count==6,"Phase 9 loaded recovered provenance survives save/load");
  foe=w.Actors.First(e=>e.Enemy);a.Pos=new(8,16);foe.Pos=new(12,16);foe.Hp=foe.MaxHp;w.Mode=Mode.Combat;
  check(w.ReactionAttack(a,foe)&&a.EquippedAmmunitionCount==5&&a.Equipment.Weapon!.LoadedAmmunition!.ResourceUnits.Count==5,"Phase 9 firing recovered ammo destroys one eligible resource unit");foe.Hp=0;w.Mode=Mode.Explore;
  check(w.UnloadFirearm(a)&&a.Inventory.Items.Single(s=>s.ItemId=="item.mall.ammo-9mm").ResourceUnits.Count==5,"Phase 9 unloading retains each recovered unit exactly once");
  var shelter=w.Features.First(f=>f.Id=="feature.mall.shelter");a.Pos=new(11,27);w.Reveal();w.Interact(shelter);a.Pos=new(14,27);w.Reveal();var site=w.Features.First(f=>f.Id==definition.Expedition!.StockpileFeatureId);
  check(w.SecureCarriedResources(a,site)>0&&w.SecuredSurvivalValue==5,"Phase 9 recovered rounds earn their exact per-round SV only on deposit");
  int earned=w.ResourceXpAwarded;check(w.SecureCarriedResources(a,site)==0&&w.ResourceXpAwarded==earned,"Phase 9 repeat deposit cannot farm ammunition XP");
  var bad=CampaignLoader.Clone(definition);bad.Items.First(i=>i.MagazineSize>0).MagazineSize=101;check(CampaignLoader.Validate(bad).Count>0,"Phase 9 invalid magazine capacity rejected");
  bad=CampaignLoader.Clone(definition);bad.SchemaVersion=6;check(CampaignLoader.Validate(bad).Count>0,"Phase 9 firearm metadata rejected in older schema");
  w=Fresh();a=w.PartyLeader;w.Equip(a,a.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));w.ReloadFirearm(a);w.Save(path);
  var json=JsonNode.Parse(File.ReadAllText(path))!;
  var invalid=a.Equipment.Weapon!.Clone();invalid.LoadedAmmunition!.Quantity=13;bool rejected=false;try{invalid.ValidateOrThrow();}catch(InvalidDataException){rejected=true;}check(rejected,"Phase 9 overfilled loaded magazine rejected");
  var old=World.New(CampaignLoader.Load(CampaignLoader.BundledExpeditionPath));old.Save(path);json=JsonNode.Parse(File.ReadAllText(path))!;json["Version"]=21;File.WriteAllText(path,json.ToJsonString());check(World.Load(path).Version==World.CurrentSaveVersion,"Phase 9 v21 expedition saves migrate without inventing firearms");File.Delete(path);
  ExpeditionTests.RunFullRoute((ok,name)=>check(ok,name.Replace("Phase 8","Phase 9 firearms")),CampaignLoader.BundledFirearmsPath);
 }
}
