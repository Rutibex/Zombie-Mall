using Ashen.Core;
using System.Text.Json.Nodes;
static class SurvivorTests
{
 public static void Run(Action<bool,string> check)
 {
  CampaignDefinition Definition()=>CampaignLoader.Load(CampaignLoader.BundledSurvivorPath);
  World Fresh()=>World.New(Definition());
  var c=Definition();var w=Fresh();
  check(c.SchemaVersion==4&&w.PartyCount==2&&c.Actors.Count(a=>a.Recruitable)==2&&w.Actors.Count(a=>a.Enemy)==3,"Phase 6 two starting survivors, two reserves and three placed basic enemies");
  check(w.Party.All(a=>a.Class=="Survivor"&&a.Character.Spellcasting.Count==0&&a.Character.Resources.Count==0&&a.SignatureFeature==null),"Phase 6 survivors do not inherit Fighter abilities or magic");
  var alex=w.PartyLeader;var sam=w.Party.Single(a=>a.Id=="actor.mall.sam");
  check(alex.SkillModifier(Skill.Athletics)==5&&sam.SkillModifier(Skill.Perception)==4&&sam.SkillModifier(Skill.Stealth)==5,"Phase 6 survivor training produces distinct skill modifiers");
  check(alex.Equipment.Weapon!.Definition.Name=="Crowbar"&&sam.Equipment.Weapon!.Definition.Name=="Kitchen Knife"&&sam.Die==4&&sam.Character.MainAttack.DamageType==DamageType.Slashing,"Phase 6 distinct equipment and damage types bind to attacks");
  check(alex.Character.Narrative.Biography.Contains("maintenance")&&sam.Character.Narrative.Personality.Contains("Watchful"),"Phase 6 authored survivor biographies and personalities reach character sheets");
  alex.Character.Experience=1000;check(!alex.CanLevelUp&&!LevelingRules.CanTakeClass(alex.Character,"Wizard",out _),"Phase 6 baseline survivors cannot enter fantasy advancement");
  var slow=w.Actors.First(a=>a.CreatureKind=="shambler");var fast=w.Actors.Single(a=>a.CreatureKind=="runner");
  check(slow.MaxHp==22&&slow.Ac==8&&slow.Character.SpeedFeet==20&&slow.AttackBonus==2&&slow.DamageBonus==0&&slow.Die==6,"Phase 6 shambler source profile is applied");
  check(fast.MaxHp==18&&fast.Ac==8&&fast.Character.SpeedFeet==40&&fast.AttackBonus==3&&fast.DamageBonus==1&&fast.Character.MainAttack.Name=="Lunge","Phase 6 runner source profile is applied");
  check(slow.AdjustDamage(20,DamageType.Poison)==0&&fast.AdjustDamage(20,DamageType.Poison)==0&&slow.AdjustDamage(7,DamageType.Slashing)==7,"Phase 6 zombies ignore poison damage but ordinary weapon damage remains");
  fast.Character.SetCondition("poisoned","Poisoned",true);alex.Character.SetCondition("poisoned","Poisoned",true);
  check(!fast.Character.HasCondition("poisoned")&&alex.Character.HasCondition("poisoned"),"Phase 6 poisoned immunity is enforced without immunizing living survivors");
  var f=w.Features.Single(f=>f.Id=="feature.mall.recruit-casey");int serial=w.NextItemInstanceNumber;
  check(!w.RunCampaignEvent("event.mall.recruit-casey")&&w.PartyCount==2&&w.NextItemInstanceNumber==serial&&!w.ExecutedCampaignEvents.Contains("event.mall.recruit-casey"),"Phase 6 distant recruitment is denied without consuming event or item IDs");
  foreach(var door in w.Features.Where(f=>f.Kind=="door"))door.Used=true;
  alex.Pos=new(12,27);sam.Pos=new(12,28);w.Reveal();
  check(w.Interact(f)&&w.PartyCount==3&&f.Used&&w.HasDialogueFlag("flag.mall.recruited-casey"),"Phase 6 nearby invitation recruits survivor and completes objective flag");
  var casey=w.Party.Single(a=>a.Id=="actor.mall.casey");
  check(casey.Class=="Survivor"&&casey.SkillModifier(Skill.Medicine)==4&&casey.Character.Narrative.Biography.Contains("office")&&w.CanStand(casey.Position,casey),"Phase 6 recruit has authored sheet, equipment and a legal distinct spawn");
  serial=w.NextItemInstanceNumber;check(!w.Interact(f)&&!w.RunCampaignEvent("event.mall.recruit-casey")&&w.PartyCount==3&&w.NextItemInstanceNumber==serial,"Phase 6 repeated invitation cannot duplicate party or gear");
  var robinFeature=w.Features.Single(f=>f.Id=="feature.mall.recruit-robin");alex.Pos=new(21,26);w.Reveal();
  check(w.Interact(robinFeature)&&w.PartyCount==4&&w.Party.Select(a=>a.Id).Distinct().Count()==4,"Phase 6 second survivor joins and party identities remain unique");
  var file=Path.Combine(Path.GetTempPath(),"zombie-survivors-"+Guid.NewGuid()+".json");
  try
  {
   casey.Hp-=4;w.Save(file);var loaded=World.Load(file);
   check(loaded.Version==World.CurrentSaveVersion&&loaded.PartyCount==4&&loaded.Party.Single(a=>a.Id==casey.Id).Hp==casey.Hp&&loaded.PartyOrder.SequenceEqual(w.PartyOrder),"Phase 6 roster, order and recruit health survive save/load");
   check(loaded.Features.Single(x=>x.Id==f.Id).Used&&!loaded.RunCampaignEvent("event.mall.recruit-casey")&&loaded.Party.Single(a=>a.Id=="actor.mall.robin").Equipment.Weapon!.Definition.Name=="Kitchen Knife","Phase 6 reload preserves recruitment ledger and rebinds recruit equipment");
   var old=World.New(CampaignLoader.Load(CampaignLoader.BundledMallPath));old.Save(file);var root=JsonNode.Parse(File.ReadAllText(file))!.AsObject();root["Version"]=18;File.WriteAllText(file,root.ToJsonString());var migrated=World.Load(file);
   check(migrated.Version==World.CurrentSaveVersion&&migrated.Campaign!.SchemaVersion==3&&migrated.PartyCount==2&&migrated.Party.All(a=>a.Class=="Fighter"),"Phase 6 v18 campaign migration preserves original actors instead of replacing them");
  }
  finally{if(File.Exists(file))File.Delete(file);}
  // Disturbance is local and separate from the later noise system.
  w=Fresh();alex=w.PartyLeader;sam=w.Party.Skip(1).First();foreach(var d in w.Features.Where(f=>f.Kind=="door"))d.Used=true;
  alex.Pos=new(36,11);sam.Pos=new(36,12);w.Reveal();w.CheckEncounter();check(w.Mode==Mode.Explore,"Phase 6 shambler remains dormant beyond ten feet");
  alex.Pos=new(38,11);w.Reveal();w.CheckEncounter();check(w.Mode==Mode.Combat&&w.CombatGroup==0,"Phase 6 nearby visible survivor disturbs shambler encounter");
  w=Fresh();foreach(var d in w.Features.Where(f=>f.Kind=="door"))d.Used=true;w.PartyLeader.Pos=new(36,23);w.Party.Skip(1).First().Pos=new(36,24);w.Reveal();w.CheckEncounter();
  check(w.Mode==Mode.Combat&&w.CombatGroup==2,"Phase 6 runner acquires survivors at longer range");
  fast=w.Actors.Single(a=>a.CreatureKind=="runner");check(fast.Economy.MovementMaximumFeet==40,"Phase 6 runner combat turn receives forty-foot movement budget");
  // Isolated combat fixture: exercise actual AI movement and attack rather than just inspecting data.
  w=Fresh();foreach(var door in w.Features.Where(f=>f.Kind=="door"))door.Used=true;w.PartyLeader.Pos=new(36,23);w.Party.Skip(1).First().Pos=new(36,24);w.Reveal();w.StartCombat(2);
  for(int i=0;i<20&&w.Active?.Enemy!=true;i++)w.EndTurn();fast=w.Actors.Single(a=>a.CreatureKind=="runner");int initialHp=w.Party.Sum(a=>a.Hp);var initialPos=fast.Position;
  for(int i=0;i<1200&&w.Mode==Mode.Combat&&w.Party.Sum(a=>a.Hp)==initialHp;i++){if(w.Active?.Enemy==true){if(!w.IsMoving(w.Active))w.EnemyTurn();w.UpdateMovement(1f/60);}else w.EndTurn();}
  check(fast.Position!=initialPos&&w.Party.Sum(a=>a.Hp)<initialHp,"Phase 6 runner AI paths around furniture and damages a survivor through real combat");
  w=Fresh();w.PartyLeader.Pos=new(12,27);w.Party.Skip(1).First().Pos=new(12,28);w.Reveal();
  // A shut back-office door does not matter inside the office, but blocked anchor does.
  w.PartyLeader.Pos=new(14,27);w.Reveal();serial=w.NextItemInstanceNumber;
  check(!w.RunCampaignEvent("event.mall.recruit-casey")&&w.PartyCount==2&&w.NextItemInstanceNumber==serial,"Phase 6 occupied recruit anchor fails atomically");
  var full=Definition();var area=full.Areas[0];for(int i=0;i<4;i++){var x=Definition().Actors[0];x.Id="actor.test.extra"+i;full.Actors.Add(x);area.Actors.Add(new(){ActorId=x.Id,Position=new(1+i%2,14+i/2)});}w=World.New(full);w.PartyLeader.Pos=new(12,27);w.Reveal();serial=w.NextItemInstanceNumber;
  check(w.PartyCount==6&&!w.RunCampaignEvent("event.mall.recruit-casey")&&!w.HasDialogueFlag("flag.mall.recruited-casey")&&w.NextItemInstanceNumber==serial,"Phase 6 full six-member party cannot consume recruit event or equipment IDs");
  void Reject(Action<CampaignDefinition> mutate,string label){var x=Definition();mutate(x);check(CampaignLoader.Validate(x).Count>0,"Phase 6 rejects "+label);}
  Reject(x=>x.SchemaVersion=3,"profiles in older schema");
  Reject(x=>x.Actors[0].Archetype="wizard","unknown archetype");
  Reject(x=>x.Actors[0].Enemy=true,"hostile survivor profile");
  Reject(x=>x.Actors.First(a=>a.Recruitable).Enemy=true,"hostile recruit");
  Reject(x=>x.Actors[0].Skills.Add(x.Actors[0].Skills[0]),"duplicate skill training");
  Reject(x=>x.Actors[0].AwarenessFeet=500,"invalid awareness range");
  Reject(x=>x.Events.First(e=>e.Effects.Any(e=>e.Handler=="recruit")).Once=false,"repeatable recruit event");
  Reject(x=>x.Events.First(e=>e.Effects.Any(e=>e.Handler=="recruit")).Effects[0].ActorId="actor.missing","missing recruit target");
  Reject(x=>x.Actors[0].Skills=null!,"null training");
 }
}
