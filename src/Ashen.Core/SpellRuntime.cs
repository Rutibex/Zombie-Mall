namespace Ashen.Core;

/// <summary>IIP32 target/effect preview used by the battlefield spell strip and world-space targeting.</summary>
public sealed record SpellTargetPreview(
 bool Allowed,string Reason,SpellDefinition? Spell,SpellcastingSource? Source,int SlotLevel,
 int RangeFeet,bool Area,bool FriendlyTarget,int SaveDc,int SpellAttackBonus,
 int MinAmount,int MaxAmount,IReadOnlyList<string> AffectedActorIds);

public sealed partial class World
{
 static string SpellEffectSource(Actor caster,SpellDefinition spell)=>$"spell:{spell.Id}:{caster.Id}";
 static bool IsDirectionalArea(SpellDefinition spell)=>spell.Area.HasArea&&spell.Area.OriginatesFromCaster&&spell.Area.Shape is SpellAreaShape.Cone or SpellAreaShape.Line or SpellAreaShape.Cube;
 static bool WantsFriendlyTarget(SpellDefinition spell)=>spell.Effect.Disposition==SpellTargetDisposition.Friendly;
 static bool WantsHostileTarget(SpellDefinition spell)=>spell.Effect.Disposition==SpellTargetDisposition.Hostile;
 static int BaseTargetCount(SpellDefinition spell)=>spell.Id switch{"bless"=>3,_=>1};

 public bool IsManualSpell(SpellDefinition spell)=>spell.CastingTime is SpellCastingTime.Action or SpellCastingTime.BonusAction;

 public bool CanBeginSpell(Actor caster,SpellCastPlan plan,out string reason)
 {
  var validation=SpellcastingRules.ValidateCast(caster.Character,plan);if(!validation.Allowed||validation.Spell==null){reason=validation.Reason;return false;}
  var spell=validation.Spell;
  if(!IsManualSpell(spell)){reason=spell.CastingTime==SpellCastingTime.Reaction?"Reaction spells trigger through the reaction interrupt system.":$"{spell.Name} cannot be cast from the combat action strip because its casting time is {spell.CastingTime}.";return false;}
  if(Mode is Mode.Defeat or Mode.Victory){reason="Spellcasting is unavailable after the encounter has ended.";return false;}
  if(!caster.Alive){reason="An incapacitated character cannot cast spells.";return false;}
  if(Mode==Mode.Combat)
  {
   if(ReactionInterruptActive){reason="Resolve the current reaction interrupt first.";return false;}
   if(Active!=caster){reason="Only the active companion can cast a spell on this turn.";return false;}
   if(IsMoving(caster)){reason="Finish movement before casting.";return false;}
   var cost=SpellcastingRules.ActionCost(spell);
   if(cost==ActionResourceKind.Action&&!caster.Economy.ActionAvailable){reason="The Action has already been spent.";return false;}
   if(cost==ActionResourceKind.BonusAction&&!caster.Economy.BonusActionAvailable){reason="The Bonus Action has already been spent.";return false;}
  }
  reason="";return true;
 }

 public IReadOnlyList<int> LegalSpellSlotLevels(CharacterSheet character,SpellcastingSource source,SpellDefinition spell)
 {
  if(spell.Level==0)return [0];
  return source.Slots.Where(s=>s.Level>=spell.Level&&s.Current>0).OrderBy(s=>s.Level).Select(s=>s.Level).ToArray();
 }

 int UpcastDice(SpellDefinition spell,int slotLevel)
 {
  int dice=Math.Max(0,spell.Effect.Dice);
  if(spell.Level==0&&dice>0)
  {
   int level=1; // replaced by the caster-aware path before resolution where needed
   return dice;
  }
  if(spell.Upcast.Kind is SpellScalingKind.DamageDicePerSlot or SpellScalingKind.HealingDicePerSlot)
   dice+=Math.Max(0,slotLevel-spell.Level)*Math.Max(0,spell.Upcast.DicePerSlot);
  return dice;
 }

 int CantripDice(Actor caster,int baseDice)=>baseDice*Math.Max(1,caster.TotalLevel>=17?4:caster.TotalLevel>=11?3:caster.TotalLevel>=5?2:1);

 (int Min,int Max) SpellAmountRange(Actor caster,SpellcastingSource source,SpellDefinition spell,int slotLevel)
 {
  if(spell.Effect.Kind==SpellEffectKind.MagicMissile)
  {
   int darts=3+Math.Max(0,slotLevel-1);return(darts*(1+spell.Effect.FlatBonus),darts*(Math.Max(2,spell.Effect.DieSize)+spell.Effect.FlatBonus));
  }
  int dice=UpcastDice(spell,slotLevel);if(spell.Level==0)dice=CantripDice(caster,dice);
  int bonus=spell.Effect.FlatBonus+(spell.Effect.AddCastingModifier?caster.AbilityModifier(source.CastingAbility):0);
  return(Math.Max(0,dice+bonus),Math.Max(0,dice*Math.Max(2,spell.Effect.DieSize)+bonus));
 }

 static float Dot(WorldPoint a,WorldPoint b)=>a.X*b.X+a.Y*b.Y;
 static float Length(WorldPoint p)=>MathF.Sqrt(p.X*p.X+p.Y*p.Y);
 static WorldPoint Unit(WorldPoint p){float length=Length(p);return length<.001f?new WorldPoint(1,0):p*(1/length);}

 bool ActorInSpellArea(Actor caster,Actor actor,SpellDefinition spell,WorldPoint point)
 {
  if(!spell.Area.HasArea)return false;
  float sizeTiles=Math.Max(.1f,spell.Area.SizeFeet/5f);
  if(spell.Area.OriginatesFromCaster)
  {
   WorldPoint direction=Unit(point-caster.Position),relative=actor.Position-caster.Position;
   if(actor==caster&&spell.Effect.Kind==SpellEffectKind.Damage)return false;
   return spell.Area.Shape switch
   {
    SpellAreaShape.Sphere=>actor.Position.Distance(caster.Position)<=sizeTiles+.001f,
    SpellAreaShape.Cone=>Length(relative)<=sizeTiles+.001f&&Dot(Unit(relative),direction)>=.707f,
    SpellAreaShape.Line=>Dot(relative,direction)>=0&&Dot(relative,direction)<=sizeTiles+.001f&&MathF.Abs(relative.X*direction.Y-relative.Y*direction.X)<=.55f,
    SpellAreaShape.Cube=>MathF.Abs((actor.Position-(caster.Position+direction*(sizeTiles*.5f))).X)<=sizeTiles*.5f+.001f&&MathF.Abs((actor.Position-(caster.Position+direction*(sizeTiles*.5f))).Y)<=sizeTiles*.5f+.001f,
    _=>actor.Position.Distance(caster.Position)<=sizeTiles+.001f
   };
  }
  return spell.Area.Shape switch
  {
   SpellAreaShape.Cube=>MathF.Abs(actor.Position.X-point.X)<=sizeTiles*.5f+.001f&&MathF.Abs(actor.Position.Y-point.Y)<=sizeTiles*.5f+.001f,
   _=>actor.Position.Distance(point)<=sizeTiles+.001f
  };
 }

 bool SpellDispositionAllows(Actor caster,Actor target,SpellDefinition spell)
 {
  if(spell.Id=="revivify")return target.Enemy==caster.Enemy&&target.Hp<=0;
  if(WantsFriendlyTarget(spell))return target.Enemy==caster.Enemy&&!target.Dead;
  if(WantsHostileTarget(spell))return target.Enemy!=caster.Enemy&&target.Alive;
  return target.Alive;
 }

 IReadOnlyList<Actor> ExpandSpellTargets(Actor caster,SpellDefinition spell,int slotLevel,Actor? primary,WorldPoint point)
 {
  IEnumerable<Actor> candidates;
  if(spell.Area.HasArea)candidates=Actors.Where(a=>ActorInSpellArea(caster,a,spell,point));
  else if(primary!=null)candidates=[primary];
  else if(spell.Target==SpellTargetKind.Self)candidates=[caster];
  else candidates=[];
  candidates=candidates.Where(a=>SpellDispositionAllows(caster,a,spell));
  if(!spell.Area.HasArea&&primary!=null&&spell.Upcast.Kind==SpellScalingKind.TargetsPerSlot)
  {
   int count=BaseTargetCount(spell)+Math.Max(0,slotLevel-spell.Level)*Math.Max(1,spell.Upcast.TargetsPerSlot);
   var extra=Actors.Where(a=>a!=primary&&SpellDispositionAllows(caster,a,spell)&&a.Position.Distance(caster.Position)<=Math.Max(1,spell.RangeFeet/5f)+.001f&&Line(caster.Position,a.Position)).OrderBy(a=>a.Position.Distance(primary.Position));
   candidates=[primary,..extra.Take(Math.Max(0,count-1))];
  }
  return candidates.Distinct().ToArray();
 }

 public SpellTargetPreview PreviewSpell(Actor caster,SpellCastPlan plan,Actor? target,WorldPoint point)
 {
  var validation=SpellcastingRules.ValidateCast(caster.Character,plan);
  if(!validation.Allowed||validation.Spell==null||validation.Source==null)return new(false,validation.Reason,validation.Spell,validation.Source,validation.SlotLevel,0,false,false,0,0,0,0,[]);
  var spell=validation.Spell;var source=validation.Source;
  if(!CanBeginSpell(caster,plan,out var beginReason))return new(false,beginReason,spell,source,validation.SlotLevel,spell.RangeFeet,spell.Area.HasArea,WantsFriendlyTarget(spell),source.SpellSaveDc(caster.Character),source.SpellAttackBonus(caster.Character),0,0,[]);

  bool directionOnly=IsDirectionalArea(spell);
  bool needsCreature=spell.Target is SpellTargetKind.Creature or SpellTargetKind.CreatureOrObject;
  bool acceptsCreature=spell.Target is SpellTargetKind.Creature or SpellTargetKind.CreatureOrObject or SpellTargetKind.CreatureOrPoint;
  bool acceptsPoint=(spell.Target is SpellTargetKind.Point or SpellTargetKind.CreatureOrPoint)||directionOnly;
  bool selfImmediate=spell.Target==SpellTargetKind.Self&&!directionOnly;
  if(needsCreature&&target==null)return new(false,"Select a creature target.",spell,source,validation.SlotLevel,spell.RangeFeet,spell.Area.HasArea,WantsFriendlyTarget(spell),source.SpellSaveDc(caster.Character),source.SpellAttackBonus(caster.Character),0,0,[]);
  if(target!=null&&!acceptsCreature&&!selfImmediate)return new(false,"This spell requires a point rather than a creature.",spell,source,validation.SlotLevel,spell.RangeFeet,spell.Area.HasArea,WantsFriendlyTarget(spell),source.SpellSaveDc(caster.Character),source.SpellAttackBonus(caster.Character),0,0,[]);
  if(target!=null&&!SpellDispositionAllows(caster,target,spell))return new(false,WantsFriendlyTarget(spell)?"Choose a friendly target.":WantsHostileTarget(spell)?"Choose a hostile target.":"That creature is not a legal target.",spell,source,validation.SlotLevel,spell.RangeFeet,spell.Area.HasArea,WantsFriendlyTarget(spell),source.SpellSaveDc(caster.Character),source.SpellAttackBonus(caster.Character),0,0,[]);

  var aim=target?.Position??point;
  if(!selfImmediate&&!directionOnly)
  {
   float rangeTiles=Math.Max(spell.RangeFeet<=0?1:spell.RangeFeet/5f,spell.Target==SpellTargetKind.Self?0:1);
   if(caster.Position.Distance(aim)>rangeTiles+.001f)return new(false,$"Target is outside {spell.RangeText()} range.",spell,source,validation.SlotLevel,spell.RangeFeet,spell.Area.HasArea,WantsFriendlyTarget(spell),source.SpellSaveDc(caster.Character),source.SpellAttackBonus(caster.Character),0,0,[]);
   if(!Line(caster.Position,aim))return new(false,"No clear line of effect.",spell,source,validation.SlotLevel,spell.RangeFeet,spell.Area.HasArea,WantsFriendlyTarget(spell),source.SpellSaveDc(caster.Character),source.SpellAttackBonus(caster.Character),0,0,[]);
  }
  if(directionOnly&&caster.Position.Distance(point)<.05f)return new(false,"Move the cursor away from the caster to choose the spell direction.",spell,source,validation.SlotLevel,spell.RangeFeet,true,WantsFriendlyTarget(spell),source.SpellSaveDc(caster.Character),source.SpellAttackBonus(caster.Character),0,0,[]);
  if(acceptsPoint&&target==null&&!worldPointExplored(point))return new(false,"The destination must be explored.",spell,source,validation.SlotLevel,spell.RangeFeet,spell.Area.HasArea,WantsFriendlyTarget(spell),source.SpellSaveDc(caster.Character),source.SpellAttackBonus(caster.Character),0,0,[]);
  if(spell.Effect.Kind==SpellEffectKind.Teleport&&!CanStand(point,caster))return new(false,"The teleport destination is blocked or occupied.",spell,source,validation.SlotLevel,spell.RangeFeet,false,true,source.SpellSaveDc(caster.Character),source.SpellAttackBonus(caster.Character),0,0,[]);

  var affected=ExpandSpellTargets(caster,spell,validation.SlotLevel,target,point);var amount=SpellAmountRange(caster,source,spell,validation.SlotLevel);
  return new(true,"",spell,source,validation.SlotLevel,spell.RangeFeet,spell.Area.HasArea,WantsFriendlyTarget(spell),source.SpellSaveDc(caster.Character),source.SpellAttackBonus(caster.Character),amount.Min,amount.Max,affected.Select(a=>a.Id).ToArray());
 }

 bool worldPointExplored(WorldPoint point)=>Explored.Contains(point.Cell);

 bool RollSpellSave(Actor caster,Actor target,SpellcastingSource source,SpellDefinition spell)
 {
  if(spell.SavingThrow is not Ability ability)return false;if(target.UsesSurvivorClasses)return SurvivorSave(target,ability,source.SpellSaveDc(caster.Character),spell.Effect.ConditionId??"");int roll=Roll(20),bless=target.Character.HasCondition("blessed")?Roll(4):0,bonus=target.SaveModifier(ability)+bless,dc=source.SpellSaveDc(caster.Character);bool success=roll+bonus>=dc;LogSavingThrow(target,ability,roll,bonus,dc,success);return success;
 }

 bool RollSpellAttack(Actor caster,Actor target,SpellcastingSource source,SpellDefinition spell)
 {
  int roll=Roll(20),bonus=source.SpellAttackBonus(caster.Character),total=roll+bonus;bool hit=roll==20||(roll!=1&&total>=target.Ac);Message(MessageCategory.Roll,$"{caster.Name} casts {spell.Name} at {target.Name}: {(hit?"HIT":"MISS")}.",$"Spell attack: d20 {roll} + {bonus} = {total} vs AC {target.Ac}: {(hit?"HIT":"MISS")}.");return hit;
 }

 int RollSpellAmount(Actor caster,SpellcastingSource source,SpellDefinition spell,int slotLevel)
 {
  int dice=UpcastDice(spell,slotLevel);if(spell.Level==0)dice=CantripDice(caster,dice);return RollDamage(dice,Math.Max(2,spell.Effect.DieSize))+spell.Effect.FlatBonus+(spell.Effect.AddCastingModifier?caster.AbilityModifier(source.CastingAbility):0);
 }

 void ApplySpellCondition(Actor caster,Actor target,SpellDefinition spell,bool saved)
 {
  if(saved||string.IsNullOrWhiteSpace(spell.Effect.ConditionId))return;
  target.Character.SetCondition(spell.Effect.ConditionId,spell.Effect.ConditionName,true,SpellEffectSource(caster,spell),tags:["spell",spell.Concentration?"concentration-effect":"spell-effect"]);
  LogCondition(target,spell.Effect.ConditionName,true,spell.Name);
 }

 void HealFromSpell(Actor caster,Actor target,SpellcastingSource source,SpellDefinition spell,int slotLevel)
 {
  if(spell.Id=="revivify"&&target.Hp<=0){target.Character.SetCondition("dead","Dead",false);target.Character.SetCondition("unconscious","Unconscious",false);target.Hp=1;LogHealing(target,1,spell.Name);return;}
  int amount=spell.Effect.FlatBonus>0&&spell.Effect.Dice==0?spell.Effect.FlatBonus:Math.Max(1,RollSpellAmount(caster,source,spell,slotLevel));int before=target.Hp;target.Hp=Math.Min(target.MaxHp,target.Hp+amount);int restored=target.Hp-before;caster.Statistics.HealingDone+=restored;target.Statistics.HealingReceived+=restored;LogHealing(target,restored,spell.Name);
 }

 void ResolveSpellEffect(Actor caster,SpellcastingSource source,SpellDefinition spell,int slotLevel,Actor? primary,WorldPoint point)
 {
  if(spell.Effect.Kind==SpellEffectKind.Teleport){Stop(caster);caster.Position=point;caster.Face(point);Reveal();Message(MessageCategory.Combat,$"{caster.Name} casts {spell.Name} and reappears nearby.");return;}
  var affected=ExpandSpellTargets(caster,spell,slotLevel,primary,point).ToArray();
  if(spell.Effect.Kind==SpellEffectKind.MagicMissile&&primary!=null)
  {
   int darts=3+Math.Max(0,slotLevel-1),damage=0;for(int i=0;i<darts;i++)damage+=Roll(Math.Max(2,spell.Effect.DieSize))+spell.Effect.FlatBonus;Hurt(primary,damage,spell.DamageType??DamageType.Force,caster);Message(MessageCategory.Combat,$"{caster.Name}'s {spell.Name} strikes {primary.Name} with {darts} force darts.");return;
  }
  if(affected.Length==0&&(spell.Effect.Kind is SpellEffectKind.Utility or SpellEffectKind.None)){Message(MessageCategory.Combat,$"{caster.Name} casts {spell.Name}. {spell.Description}");return;}
  foreach(var target in affected.ToArray())
  {
   bool hit=!spell.SpellAttack||RollSpellAttack(caster,target,source,spell);if(!hit)continue;
   bool saved=spell.SavingThrow!=null&&RollSpellSave(caster,target,source,spell);
   if(spell.Effect.Kind==SpellEffectKind.Damage)
   {
    int damage=Math.Max(0,RollSpellAmount(caster,source,spell,slotLevel));damage=SurvivorSaveDamage(target,damage,spell.SavingThrow??Ability.Strength,saved,spell.Effect.HalfOnSave);if(damage>0)Hurt(target,damage,spell.DamageType??DamageType.Force,caster);ApplySpellCondition(caster,target,spell,saved);
   }
   else if(spell.Effect.Kind==SpellEffectKind.Healing)HealFromSpell(caster,target,source,spell,slotLevel);
   else if(spell.Effect.Kind==SpellEffectKind.Condition)ApplySpellCondition(caster,target,spell,saved);
   else if(spell.Effect.Kind is SpellEffectKind.Utility or SpellEffectKind.None)ApplySpellCondition(caster,target,spell,saved);
  }
  if(affected.Length==0)Message(MessageCategory.Combat,$"{caster.Name} casts {spell.Name}, but no creature is affected.");
 }

 public bool CastSpell(Actor caster,SpellCastPlan plan,Actor? target,WorldPoint point)
 {
  var preview=PreviewSpell(caster,plan,target,point);if(!preview.Allowed||preview.Spell==null||preview.Source==null){LogSystem(preview.Reason,true);return false;}
  var spell=preview.Spell;var source=preview.Source;
  ActionResourceKind? cost=SpellcastingRules.ActionCost(spell);
  if(Mode==Mode.Combat&&cost!=null&&!CanSpend(caster,cost.Value)){LogSystem(cost==ActionResourceKind.Action?"The Action has already been spent.":"The Bonus Action has already been spent.",true);return false;}
  string? previousConcentration=caster.Character.Concentration?.SpellId;
  if(Mode==Mode.Combat&&cost!=null&&!SpendResource(caster,cost.Value,spell.Name))return false;
  if(!string.IsNullOrWhiteSpace(previousConcentration))ClearSpellEffectsFrom(caster,previousConcentration!);
  if(!SpellcastingRules.SpendSpellResources(caster.Character,plan,out int slotLevel,out var reason)){LogSystem(reason,true);return false;}
  caster.Face(target?.Position??point);Message(MessageCategory.Combat,$"{caster.Name} casts {spell.Name}{(spell.Level>0?$" using a level {slotLevel} slot":"")}.");ResolveSpellEffect(caster,source,spell,slotLevel,target,point);Sound?.Invoke(spell.Effect.Kind==SpellEffectKind.Healing?"heal":"spell");CheckEnd();return true;
 }

 public void ClearSpellEffectsFrom(Actor caster,string spellId)
 {
  foreach(var actor in Actors)
  {
   var conditions=actor.Character.Conditions.Where(c=>c.Source.Equals($"spell:{spellId}:{caster.Id}",StringComparison.OrdinalIgnoreCase)).ToArray();
   foreach(var condition in conditions){actor.Character.SetCondition(condition.Id,condition.Name,false);LogCondition(actor,condition.Name,false,"concentration ended");}
  }
 }

 public bool BreakConcentration(Actor caster,string reason="")
 {
  string? spellId=caster.Character.Concentration?.SpellId;if(string.IsNullOrWhiteSpace(spellId))return SpellcastingRules.BreakConcentration(caster.Character);
  string name=SpellCatalog.TryGet(spellId,out var spell)?spell.Name:spellId;ClearSpellEffectsFrom(caster,spellId);bool ended=SpellcastingRules.BreakConcentration(caster.Character);if(ended)LogCondition(caster,$"Concentration: {name}",false,reason);return ended;
 }

 void ClearAllSpellEffects()
 {
  foreach(var actor in Actors)actor.Character.Conditions.RemoveAll(c=>c.Source.StartsWith("spell:",StringComparison.OrdinalIgnoreCase));
 }
}
