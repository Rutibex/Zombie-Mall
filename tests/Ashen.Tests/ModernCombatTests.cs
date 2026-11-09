using Ashen.Core;
using System.Text.Json.Nodes;
static class ModernCombatTests
{
 public static void Run(Action<bool,string> check)
 {
  var definition=CampaignLoader.Load(CampaignLoader.BundledTacticalPath);
  World Arena(int cover=2)
  {
   var c=CampaignLoader.Clone(definition);var area=c.Areas.Single();area.Props.Clear();area.Props.Add(new(){Id="prop.tactical.test",Kind="counter",Position=new(11,16),CoverBonus=cover});
   var world=World.New(c);foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;
   world.PartyLeader.Pos=new(8,16);var enemy=world.Actors.First(e=>e.Enemy);enemy.Pos=new(12,16);enemy.MaxHp=1000;enemy.Hp=1000;world.Reveal();return world;
  }
  var w=Arena();var a=w.PartyLeader;var foe=w.Actors.First(e=>e.Enemy);string crowbar=a.Equipment.Weapon!.InstanceId;
  check(w.ModernTacticalCombat&&a.UsesModernCombat&&definition.SchemaVersion==8,"Phase 10 schema 8 explicitly selects modern tactical rules");
  check(a.AttackBonus==a.AbilityModifier(Ability.Strength)+a.ProficiencyBonus&&a.DamageBonus==a.AbilityModifier(Ability.Strength),"Phase 10 survivor melee uses Strength plus proficiency and Strength damage");
  w.AssignQuickWeapon(a,crowbar,1);check(w.Equip(a,a.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"))&&w.ReloadFirearm(a),"Phase 10 pistol can be equipped and loaded normally");
  check(a.AttackBonus==a.Dex+a.ProficiencyBonus&&a.DamageBonus==a.Dex,"Phase 10 firearm uses Dexterity plus proficiency and Dexterity damage");
  check(foe.AttackBonus==3&&foe.DamageBonus==1||foe.AttackBonus==2&&foe.DamageBonus==0,"Phase 10 authored zombie attack profile is retained");
  check(w.Blocked(new(11,16))&&!w.SightBlocked(new(11,16))&&!w.CanStand(new Cell(11,16).Center,a),"Phase 10 low cover blocks movement but permits sight");
  check(w.Line(a.Position,foe.Position)&&w.CanSee(a.Position,foe.Position)&&w.Visible.Contains(foe.Pos),"Phase 10 sight and targeting agree across low furniture");
  check(w.CoverAgainst(a,foe)==2&&w.CoverAgainst(foe,a)==0,"Phase 10 cover is directional and only protects a nearby defender");
  w.StartCombat(foe.Group);w.Order=[a.Id,foe.Id];w.Turn=0;
  void Reset()=>a.Economy.Reset(a.Character.SpeedFeet,a.Character.AttacksPerAction);
  Reset();var preview=w.PreviewAttack(a,foe,AttackKind.Basic);
  check(preview.Allowed&&preview.HitChance==70&&preview.MinDamage==3&&preview.MaxDamage==13,"Phase 10 preview combines two firearm dice, Dexterity and half-cover AC");
  check(w.DescribeAttackContext(a,foe).Contains("cover +2 AC")&&w.DescribeAttackContext(a,foe).Contains("effective AC 10"),"Phase 10 attack context explains effective AC and cover");
  var savedPosition=a.Position;var savedRandom=w.RandomState;w.PreviewAttack(a,foe,AttackKind.Basic);check(a.Position==savedPosition&&w.RandomState==savedRandom,"Phase 10 previews consume no randomness and do not move actors");
  check(w.Attack(a,foe,AttackKind.Basic)&&a.EquippedAmmunitionCount==11&&w.MessageHistory.Any(m=>m.Summary.Contains("cover +2 AC")),"Phase 10 actual covered shot consumes one round and records its tactical modifiers");
  Reset();foe.Pos=new(9,16);w.Reveal();check(w.Threatened(a)&&w.AttackModifiers(a,foe).Disadvantage&&w.PreviewAttack(a,foe,AttackKind.Basic).HitChance==64,"Phase 10 adjacent melee hostile imposes close-fire disadvantage");
  a.Character.SetCondition("helped","Helped",true);var modifiers=w.AttackModifiers(a,foe);check(modifiers.Advantage&&modifiers.Disadvantage&&w.PreviewAttack(a,foe,AttackKind.Basic).HitChance==80,"Phase 10 advantage and disadvantage cancel once");a.Character.SetCondition("helped","Helped",false);
  foe.Character.SetCondition("unconscious","Unconscious",true);check(!w.Threatened(a),"Phase 10 unconscious hostile does not threaten a shooter");foe.Character.SetCondition("unconscious","Unconscious",false);
  foe.Pos=new(12,16);foe.Character.SetCondition("dodging","Dodge",true);check(w.PreviewAttack(a,foe,AttackKind.Basic).HitChance==49,"Phase 10 Dodge combines correctly with covered ranged target");foe.Character.SetCondition("dodging","Dodge",false);
  a.Character.SetCondition("prone","Prone",true);check(w.AttackModifiers(a,foe).Disadvantage,"Phase 10 existing prone condition affects attack rolls");a.Character.SetCondition("prone","Prone",false);
  check(w.ActivateQuickWeapon(a,1)&&!a.Economy.BonusActionAvailable&&a.Economy.ActionAvailable,"Phase 10 combat quick switch spends Bonus Action and retains Action");
  int pistolSlot=a.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol");w.AssignQuickWeapon(a,a.Inventory.Items[pistolSlot].InstanceId,0);
  check(!w.ActivateQuickWeapon(a,0)&&a.Equipment.Weapon!.InstanceId==crowbar,"Phase 10 second switch fails without changing equipment");
  check(!w.WeaponLine(a,foe)&&w.Line(a.Position,foe.Position),"Phase 10 low furniture permits a shot but blocks a melee strike through it");
  Reset();check(w.ActivateQuickWeapon(a,0)&&a.EquippedAmmunitionCount==11,"Phase 10 switching back retains exact loaded rounds");
  Reset();check(w.ExecuteStandardAction(a,StandardActionKind.Disengage)&&a.Character.HasCondition("disengaged")&&!a.Economy.ActionAvailable,"Phase 10 Disengage retains normal Action economy");
  Reset();check(w.ExecuteStandardAction(a,StandardActionKind.Ready)&&a.Character.HasCondition("readying")&&!a.Economy.ActionAvailable,"Phase 10 loaded firearm can ready an attack with one Action");
  a.Character.SetCondition("readying","Readied Action",false);Reset();w.UnloadFirearm(a);Reset();check(!w.ExecuteStandardAction(a,StandardActionKind.Ready)&&a.Economy.ActionAvailable,"Phase 10 empty firearm cannot waste Action on invalid Ready");
  check(w.ExecuteTacticsStep(a)==TacticsStepResult.Acted&&a.EquippedAmmunitionCount==11&&!a.Economy.ActionAvailable,"Phase 10 automatic tactics reloads through ordinary Action cost");
  // A tall blocker retains total obstruction; three-quarter cover uses the authored +5 value.
  var heavy=Arena(5);var ha=heavy.PartyLeader;var ht=heavy.Actors.First(e=>e.Enemy);heavy.Equip(ha,ha.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));check(heavy.CoverAgainst(ha,ht)==5,"Phase 10 three-quarter cover provides +5 AC without stacking");
  var wall=Arena(0);check(!wall.Line(wall.PartyLeader.Position,wall.Actors.First(e=>e.Enemy).Position)&&!wall.CanSee(wall.PartyLeader.Position,wall.Actors.First(e=>e.Enemy).Position),"Phase 10 tall furniture still blocks sight and attacks");
  var chairDefinition=CampaignLoader.Clone(definition);chairDefinition.Areas[0].Props.Clear();chairDefinition.Areas[0].Props.Add(new(){Id="prop.tactical.chair",Kind="chair",Position=new(11,16),BlocksSight=false});var chairWorld=World.New(chairDefinition);foreach(var door in chairWorld.Features.Where(f=>f.Kind=="door"))door.Used=true;
  check(chairWorld.Blocked(new(11,16))&&chairWorld.Line(new Cell(8,16).Center,new Cell(12,16).Center)&&chairWorld.CanSee(new Cell(8,16).Center,new Cell(12,16).Center),"Phase 10 low chairs block movement without becoming opaque walls or granting cover");
  var old=World.New(CampaignLoader.Load(CampaignLoader.BundledFirearmsPath));var oa=old.PartyLeader;old.Equip(oa,oa.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));check(!old.ModernTacticalCombat&&!oa.UsesModernCombat&&oa.AttackBonus==5&&oa.DamageBonus==3,"Phase 10 previous firearm campaign retains authored bonuses");
  string path=Path.Combine(Path.GetTempPath(),"zombie-tactics-"+Guid.NewGuid()+".json");w.Mode=Mode.Explore;w.Save(path);var loaded=World.Load(path);check(loaded.Version==World.CurrentSaveVersion&&loaded.ModernTacticalCombat&&loaded.PartyLeader.UsesModernCombat&&loaded.PartyLeader.EquippedAmmunitionCount==11,"Phase 10 save/load rebinds tactical rules and loaded firearm");
  old.Save(path);var root=JsonNode.Parse(File.ReadAllText(path))!;root["Version"]=22;File.WriteAllText(path,root.ToJsonString());loaded=World.Load(path);check(loaded.Version==World.CurrentSaveVersion&&!loaded.ModernTacticalCombat&&loaded.Campaign!.SchemaVersion==7,"Phase 10 v22 saves migrate without enabling new rules");File.Delete(path);
  var bad=CampaignLoader.Clone(definition);bad.SchemaVersion=7;check(CampaignLoader.Validate(bad).Count>0,"Phase 10 rejects tactical metadata in schema 7");
  bad=CampaignLoader.Clone(definition);bad.Areas[0].Props[0].CoverBonus=3;check(CampaignLoader.Validate(bad).Count>0,"Phase 10 rejects unsupported cover modifier");
  bad=CampaignLoader.Clone(definition);bad.Areas[0].Props[0].CoverBonus=2;bad.Areas[0].Props[0].BlocksMovement=false;check(CampaignLoader.Validate(bad).Count>0,"Phase 10 rejects cover on non-solid decoration");
  // Deterministic actual rolls: the same d20 hits in the open and misses behind half cover.
  var shot=Arena();var shooter=shot.PartyLeader;var victim=shot.Actors.First(e=>e.Enemy);shot.Equip(shooter,shooter.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));shot.ReloadFirearm(shooter);shot.StartCombat(victim.Group);shot.Order=[shooter.Id,victim.Id];shot.Turn=0;
  uint SeedFor(int wanted){for(uint seed=1;seed<10000;seed++){shot.RandomState=seed;if(shot.Roll(20)==wanted)return seed;}throw new Exception("No test seed");}
  var nearMiss=SeedFor(5);shot.RandomState=nearMiss;int hp=victim.Hp;
  check(shot.Attack(shooter,victim,AttackKind.Basic)&&victim.Hp==hp,"Phase 10 actual half cover stops a roll that would hit base AC");
  shooter.Economy.Reset(shooter.Character.SpeedFeet,1);victim.Pos=new(10,16);shot.Reveal();shot.RandomState=nearMiss;
  check(shot.Attack(shooter,victim,AttackKind.Basic)&&victim.Hp<hp&&hp-victim.Hp is >=3 and <=13,"Phase 10 identical open shot hits with Dexterity-based damage");
  shooter.Economy.Reset(shooter.Character.SpeedFeet,1);shot.RandomState=SeedFor(1);hp=victim.Hp;shot.Attack(shooter,victim,AttackKind.Basic);check(victim.Hp==hp,"Phase 10 natural one still misses");
  shooter.Economy.Reset(shooter.Character.SpeedFeet,1);shot.RandomState=SeedFor(20);hp=victim.Hp;shot.Attack(shooter,victim,AttackKind.Basic);check(victim.Hp<hp,"Phase 10 natural twenty still hits and rolls critical damage");
  var ready=Arena();var ra=ready.PartyLeader;var rt=ready.Actors.First(e=>e.Enemy);ready.Equip(ra,ra.Inventory.Items.FindIndex(s=>s.ItemId=="item.mall.pistol"));ready.ReloadFirearm(ra);ready.StartCombat(rt.Group);ready.Order=[ra.Id,rt.Id];ready.Turn=0;ready.ReactionPolicyResolver=(_,_)=>ReactionPolicy.Auto;
  check(ready.ExecuteStandardAction(ra,StandardActionKind.Ready),"Phase 10 modern Ready can be armed through standard action authority");ready.EndTurn();
  check(ra.EquippedAmmunitionCount==11&&!ra.Economy.ReactionAvailable&&!ra.Character.HasCondition("readying")&&ready.Active==rt,"Phase 10 readied gun fires once, spends Reaction and leaves initiative with triggering enemy");
  var retreat=Arena();var runner=retreat.PartyLeader;var pursuer=retreat.Actors.First(e=>e.Enemy);pursuer.Pos=new(9,16);retreat.StartCombat(pursuer.Group);retreat.Order=[runner.Id,pursuer.Id];retreat.Turn=0;retreat.ReactionPolicyResolver=(_,_)=>ReactionPolicy.Ask;
  check(retreat.ExecuteStandardAction(runner,StandardActionKind.Disengage)&&retreat.MoveTo(runner,new Cell(6,16).Center),"Phase 10 survivor can Disengage and retreat with ordinary movement");for(int i=0;i<120&&retreat.IsMoving(runner);i++)retreat.UpdateMovement(1f/30);
  check(retreat.PendingReaction==null&&runner.Pos==new Cell(6,16)&&pursuer.Economy.ReactionAvailable,"Phase 10 Disengage prevents actual opportunity interrupt without spending hostile Reaction");
  ExpeditionTests.RunFullRoute((ok,name)=>check(ok,name.Replace("Phase 8","Phase 10 tactical")),CampaignLoader.BundledTacticalPath);
 }
}
