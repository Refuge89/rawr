﻿using System;
using System.Collections.Generic;
using System.Text;

/* Molotok's notes on stuff implimented
 * 
 *TO DO:
 * Divine Plea: causes 50% heals.  Also, does it take a GCD?  if so account for it.
 * Illuminated Healing - Your direct healing spells also place an absorb shield on your target for 12% of the amount healed lasting 15 sec. 
 *                       Each point of Mastery increases the absorb amount by an additional 1.50%.
 * 
 * 
 *Talents:
 *DONE(assumes Holy Spec always) Walk in the Light (for selecting Holy specialization)- 10% heal bonus
 *DONE Protector of the Innocent(0-3) - additional self heal when casting a targeted heal not on yourself
 * Judgement of the Pure(0-3) - increases haste 3% per point
 *DONE Clarity of Purpose(0-3) - reduce cast time of HL and DL by .15 per point
 * Last Word(0-2) - 30% extra WoG crit per point, when target below 35% health
 * Divine Favor(0-1) - increase haste/crit 20% for 20 secs.  3 min CD
 *DONE Infusion of Light(0-2) - 5% holy shock crit per point
 *                        - HS crit = -0.75 sec per point from next DL/HL
 * Daybreak(0-2) - FoL, DL, HL have 10% chance per point to make next HS not trigger its 6 sec CD.
 * Enlightened Judgements(0-2) - gives +50% spirit to hit per point
 *DONE                             - Judgement self heal
 *DONE Beacon of Light(0-1) - 50% of heals to beacon target
 * Speed of Light(0-3) - 1% haste per point
 *                     - reduce HR CD by 10 sec per point
 * Conviction(0-3) - 1% heal bonus per point, for 15 secs after a crit from non-periodic spell.  (or weapon swing)
 * Tower of Radiance(0-3) - healing beacon target with FoL or DL has 33% chance per point of giving a Holy Power
 * Blessed Life(0-2) - 50% chance to gain holy power when taking direct damage.  8 sec CD.
 *DONE Light of Dawn(0-1) - gives the spell.
 * 
 *
 *DONE Divinity(0-3) - 2% healing increase per point
 *DONE Crusade(0-3) - 10% per point increase HS heals
 *              - after killing something, next HL heals for 100% extra per point, for 15 sec.
 * 
 * 
 *Glyphs
 *Prime                                            
 *DONE Glyph of Word of Glory
 *DONE Glyph of Seal of Insight  
 *DONE Glyph of Holy Shock      
 * Glyph of Divine Favor
 *Major
 * Glyph of Beacon of Light
 *DONE Glyph of Divine Plea
 * Glyph of Cleansing
 *DONE Glyph of Divinity - 10% mana when casting LoH
 * Glyph of Salvation
 * Glyph of Long Word 
 *DONE Glyph of Light of Dawn  
 *DONE Glyph of Lay on Hands - reduced CD by 3 min.  (from 10 to 7)
 * 
 */

namespace Rawr.Healadin
{

    public static class HealadinConstants
    {
        // Spell coeficients
        // source:  http://elitistjerks.com/f76/t110847-holy_cataclysm_holy_compendium_4_0_6_a/
        public static float fol_coef = 0.8632f;
        public static float fol_mana = 7026.6f;
        public static float fol_min = 6907f;
        public static float fol_max = 7749f;

        public static float hl_coef = 0.432f;
        public static float hl_mana = 2342.2f;
        public static float hl_min = 4163f;
        public static float hl_max = 4637f;

        public static float dl_coef = 1.15306f;
        public static float dl_mana = 7729.3f;
        public static float dl_min = 11100f;
        public static float dl_max = 12366f;

        public static float hs_coef = 0.2689f;
        public static float hs_mana = 1873.8f;
        public static float hs_min = 2629f;
        public static float hs_max = 2847f;

        // Holy Radiance.  
        public static float hr_coef = 0.06695f;
        public static float hr_mana = 9368.8f; 
        public static float hr_min = 684f;  
        public static float hr_max = 684f;

        // Stats for 1 holy power.  Scales linearly with more holy power. (so just * by 2 or 3 when needed)
        public static float wog_coef_sp = 0.209f;  // regular spellpower coef
        public static float wog_coef_ap = 0.198f;  // Word of Glory also has Attack Power coef
        public static float wog_min = 2018f;
        public static float wog_max = 2248f;

        // Stats for 1 holy power, 1 target.  Hits up to 5 targets, 6 glyphed.
        public static float lod_coef = 0.198f;
        public static float lod_min = 605f;
        public static float lod_max = 673f;

        // Protector of the Innocent, talent.  0-3 points.  These stats assume 3 points.
        public static float poti_coef = 0.039233f; 
        public static float poti_min = 2481f;
        public static float poti_max = 2853f;

        //Judgement self heals from Enlightened Judgements talent (0-2 points, these stats assume 2 points)
        public static float ej_coef = 0.20206f;  
        public static float ej_min = 2605f;
        public static float ej_max = 2997f;


        public static float basemana = 23422;
    }

    public abstract class Heal
    {
        private Rotation _rotation;
        public Rotation Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                _stats = _rotation.Stats;
                _talents = _rotation.Talents;
            }
        }

        private Stats _stats;
        protected Stats Stats { get { return _stats; } }

        private PaladinTalents _talents;
        protected PaladinTalents Talents { get { return _talents; } }

        public Heal(Rotation rotation)
        {
            Rotation = rotation;
        }

        public float HPS() { return AverageHealed() / CastTime(); }
        public float MPS() { return AverageCost() / CastTime(); }
        public float HPM() 
        {
            if (BaseMana == 0)
                return 0f;
            else
                return AverageHealed() / AverageCost(); 
        }

        public float CastTime() { return (float)Math.Max(1f, (BaseCastTime - AbilityCastTimeReduction()) / (1f + Stats.SpellHaste));}

        public float AverageCost()
        {
            return (float)Math.Floor((BaseMana * (DivineIllumination ? 0.5f : 1f) - CostReduction())
                * (AbilityCostMultiplier()));
        }

        public float AverageHealed()
        {
            return BaseHealed() * (1f - ChanceToCrit()) + CritHealed() * ChanceToCrit();
        }

        // Illuminated Healing
        public float AverageShielded()
        {
            return AverageHealed() * (1f + StatConversion.GetMasteryFromRating(Stats.MasteryRating) * 0.1f);
        }

        public float CritHealed() { return BaseHealed() * 1.5f * (1f + Stats.BonusCritHealMultiplier) * AbilityCritMultiplier(); }

        public float BaseHealed()
        {
            float heal = AbilityHealed();

            heal *= Talents.GlyphOfSealOfInsight ? 1.05f : 1;
            heal *= 1f + Stats.HealingReceivedMultiplier;
            heal *= 1f - Rotation.DivinePleas * 15f / Rotation.FightLength * .5f;
            heal *= 1f + .01f * Talents.Divinity;
            heal *= AbilityHealedMultiplier();
            heal *= (1f + Stats.BonusHealingDoneMultiplier);

            // Walk in the Light
            heal *= (1f + 0.1f); // TODO: Figure out a way to detect the character's main spec

            return heal;
        }

        public float ChanceToCrit()
        {
            // TODO: figure out how to add Talents.DivineFavor into crit chance, when do we proc it, which casts are proced, etc.
            return (float)Math.Max(0f, (float)Math.Min(1f, Stats.SpellCrit + AbilityCritChance() + ExtraCritChance ));
        }
        public float CostReduction() { return Stats.SpellsManaCostReduction + Stats.HolySpellsManaCostReduction + AbilityCostReduction(); }

        public float ExtraCritChance { get; set; }
        public bool DivineIllumination { get; set; }

        protected abstract float AbilityHealed();
        protected virtual float AbilityCritChance() { return 0f; }
        protected virtual float AbilityCostReduction() { return 0f; }
        protected virtual float AbilityCastTimeReduction() { return 0f; }
        protected virtual float AbilityCostMultiplier() { return 1f; }
        protected virtual float AbilityHealedMultiplier() { return 1f; }
        protected virtual float AbilityCritMultiplier() { return 1f; }

        public abstract float BaseCastTime { get; }
        public abstract float BaseMana { get; }

        public override string ToString()
        {
            return string.Format("Average Heal: {0:N0}\nAverage Cost: {1:N0}\nHPS: {2:N0}\nHPM: {3:N2}\nCast Time: {4:N2} sec\nCrit Chance: {5:N2}%",
                AverageHealed(),
                AverageCost(),
                HPS(),
                HPM(),
                CastTime(),
                ChanceToCrit() * 100);
        }

        protected static float ClarityOfPurpose(int talentPoints) {
            switch (talentPoints) {
                case 1:
                    return 0.15f;
                case 2:
                    return 0.35f;
                case 3:
                    return 0.5f;
                default:
                    return 0f;
            }
        }
    }

    public class FlashOfLight : Heal
    {
        public FlashOfLight(Rotation rotation)
            : base(rotation)
        { }

        public override float BaseCastTime { get { return 1.5f; } }
        public override float BaseMana { get { return HealadinConstants.fol_mana; } } 

        protected override float AbilityHealed()
        {
            // TODO: calculate real spellpower somewhere in Healadin module, and use that instead of Stats.SpellPower + Stats.Intellect
            return (HealadinConstants.fol_min + HealadinConstants.fol_max) / 2f + ((Stats.SpellPower + Stats.Intellect) * HealadinConstants.fol_coef);
        }
    }

    public class HolyLight : Heal
    {
        public HolyLight(Rotation rotation)
            : base(rotation)
        { }

        public override float BaseCastTime { get { return 3f - ClarityOfPurpose(Talents.ClarityOfPurpose) - CastTimeReduction; } }
        public override float BaseMana { get { return HealadinConstants.hl_mana; } } 

        public float CastTimeReduction { get; set; }

        protected override float AbilityHealed()
        {
            // TODO: calculate real spellpower somewhere in Healadin module, and use that instead of Stats.SpellPower + Stats.Intellect
            return (HealadinConstants.hl_min + HealadinConstants.hl_max) / 2f + ((Stats.SpellPower + Stats.Intellect) * HealadinConstants.hl_coef);
        }
    }

    public class DivineLight : Heal
    {
        public DivineLight(Rotation rotation)
            : base(rotation)
        { }

        public override float BaseCastTime { get { return 3f - ClarityOfPurpose(Talents.ClarityOfPurpose); } }
        public override float BaseMana { get { return HealadinConstants.dl_mana; } } 

        protected override float AbilityHealed() {
            // TODO: calculate real spellpower somewhere in Healadin module, and use that instead of Stats.SpellPower + Stats.Intellect
            return (HealadinConstants.dl_min + HealadinConstants.dl_max) / 2f + ((Stats.SpellPower + Stats.Intellect) * HealadinConstants.dl_coef);
        }
    }

    public class HolyShock : Heal
    {
        public HolyShock(Rotation rotation)
            : base(rotation)
        { }

        public override float BaseCastTime { get { return 1.5f; } }
        public override float BaseMana { get { return HealadinConstants.hs_mana; } }
        protected override float AbilityCritChance()
        {
            return ((Talents.GlyphOfHolyShock ? 0.05f : 0f) + 0.05f * Talents.InfusionOfLight);
        }


        protected override float AbilityHealed()
        {
            // TODO: calculate real spellpower somewhere in Healadin module, and use that instead of Stats.SpellPower + Stats.Intellect
            return ((HealadinConstants.hs_min + HealadinConstants.hs_max) / 2f +
                     ((Stats.SpellPower+ Stats.Intellect) * HealadinConstants.hs_coef) * (1f + Talents.Crusade * 0.1f));
        }

        public float Usage()
        {
            return Casts() * AverageCost();
        }

        public float Casts()
        {
            return Rotation.ActiveTime * Rotation.CalcOpts.HolyShock / Cooldown();
        }

        public float Time()
        {
            return Casts() * CastTime();
        }

        public float Healed()
        {
            return Casts() * AverageHealed();
        }

        public float Cooldown()
        {
            return 6f; // TODO: Account for talent Daybreak
        }

        /* this must have been for an outdated version of IoL talent.  It no longer effects crit multiplier. (patch 4.1)
        protected override float AbilityCritMultiplier()
        {
            return 1f + (Talents.GlyphOfHolyShock ? 0.05f : 0f) + (Talents.InfusionOfLight * 0.05f);
        }*/

    }

    public class WordofGlory : Heal
    {
        public WordofGlory(Rotation rotation)
            : base(rotation)
        { }

        public override float BaseCastTime { get { return 1.5f; } }
        public override float BaseMana { get { return 0f; } }
        /* public override float ExtraCritChance { get { return (Talents.LastWord * 0.3f); } } how do I figure if the target is below 35% health? */ 
    

        protected override float AbilityHealed()
        {
            float attackpower = Stats.AttackPower + Stats.Strength * 2;
            attackpower *= (1f + Stats.BonusAttackPowerMultiplier);
            float holypower = 3f;  // assume 3 holypower for now
            float glyph_multiplier = 1f + (Talents.GlyphOfWordOfGlory ? 0.1f : 0f);
            // TODO: calculate real spellpower somewhere in Healadin module, and use that instead of Stats.SpellPower + Stats.Intellect
            return holypower * glyph_multiplier * (HealadinConstants.wog_min + HealadinConstants.wog_max) / 2f + 
                                ((Stats.SpellPower + Stats.Intellect) * HealadinConstants.wog_coef_sp) +
                                (attackpower * HealadinConstants.wog_coef_ap);
        }
    }

    public class LightofDawn : Heal
    {
        public LightofDawn(Rotation rotation)
            : base(rotation)
        { }

        public override float BaseCastTime { get { return 1.5f; } }
        public override float BaseMana { get { return 0f; } }

        protected override float AbilityHealed()
        {
            //TODO: find if we are glyphed for 6 targets healed. 
            float targets_healed = 5f + (Talents.GlyphOfLightOfDawn ? 1f : 0f);
            float holypower = 3f; // assume 3 holy power for now
            // TODO: calculate real spellpower somewhere in Healadin module, and use that instead of Stats.SpellPower + Stats.Intellect
            return holypower * targets_healed * ((HealadinConstants.lod_min + HealadinConstants.lod_max) / 2f + 
                                                 ((Stats.SpellPower + Stats.Intellect) * HealadinConstants.lod_coef));
        }
    }

    public class HolyRadiance : Heal
    {
        public HolyRadiance(Rotation rotation)
            : base(rotation)
        { }

        public override float BaseCastTime { get { return 1.5f; } }
        public override float BaseMana { get { return HealadinConstants.hr_mana; } }

        protected override float AbilityHealed()
        {
            float targets_healed = 6f;
            float ticks = 12f; // TODO, calculate # of ticks from haste.  11 assumes 5-15% haste. 12 = 15-25%
            // TODO: calculate real spellpower somewhere in Healadin module, and use that instead of Stats.SpellPower + Stats.Intellect
            return ticks * targets_healed * ((HealadinConstants.hr_min + HealadinConstants.hr_max) / 2f +
                                                 ((Stats.SpellPower + Stats.Intellect) * HealadinConstants.hr_coef));
        }
    }

    public class LayonHands : Heal
    {
        public LayonHands(Rotation rotation)
            : base(rotation)
        { }

        public override float BaseCastTime { get { return 1.5f; } }
        public override float BaseMana { get { return 0f; } }

        protected override float AbilityHealed()
        {
            return Stats.Health;
        }

        public float Cooldown()
        {
            return (Talents.GlyphOfLayOnHands ? 420f : 600f);
        }

        public float Casts()
        {
            return (1f + (float)Math.Floor(Rotation.ActiveTime / Cooldown()));
        }

    }

    public class ProtectoroftheInnocent : Heal
    {
        public ProtectoroftheInnocent(Rotation rotation)
            : base(rotation)
        { }

        public override float BaseCastTime { get { return 1.5f; } } // not cast really, but dont want to cause div by 0 errors(potential HPS calculations), so leaving this for now
        public override float BaseMana { get { return 0f; } }

        protected override float AbilityHealed()
        {
            // TODO: calculate real spellpower somewhere in Healadin module, and use that instead of Stats.SpellPower + Stats.Intellect
            return (((HealadinConstants.poti_min + HealadinConstants.poti_max) / 2f +
                    ((Stats.SpellPower + Stats.Intellect) * HealadinConstants.poti_coef)) *
                      (Talents.ProtectorOfTheInnocent / 3));
        }
    }

    public class EnlightenedJudgements : Heal
    {
        public EnlightenedJudgements(Rotation rotation)
            : base(rotation)
        { }

        public override float BaseCastTime { get { return 1.5f; } } // not cast really, but dont want to cause div by 0 errors(potential HPS calculations), so leaving this for now
        public override float BaseMana { get { return 0f; } }

        protected override float AbilityHealed()
        {
            // TODO: calculate real spellpower somewhere in Healadin module, and use that instead of Stats.SpellPower + Stats.Intellect
            return (((HealadinConstants.ej_min + HealadinConstants.ej_max) / 2f +
                    ((Stats.SpellPower + Stats.Intellect) * HealadinConstants.ej_coef)) *
                      (Talents.EnlightenedJudgements / 2));
        }
    }


    public abstract class Spell
    {

        private Rotation _rotation;
        public Rotation Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                _stats = _rotation.Stats;
                _talents = _rotation.Talents;
            }
        }

        private Stats _stats;
        protected Stats Stats { get { return _stats; } }

        private PaladinTalents _talents;
        protected PaladinTalents Talents { get { return _talents; } }

        public Spell(Rotation rotation)
        {
            Rotation = rotation;
        }

        public float Uptime { get; set; }
        public float Duration { get; set; }
        public float BaseCost { get; set; }

        public float Cost()
        {
            return (BaseCost - Stats.SpellsManaCostReduction - Stats.HolySpellsManaCostReduction);
        }

        public float Casts()
        {
            return (float)Math.Ceiling(Uptime / Duration);
        }

        public virtual float CastTime()
        {
            return (float)Math.Max(1f, 1.5f / (1f + Stats.SpellHaste));
        }

        public virtual float Time()
        {
            return Casts() * CastTime();
        }

        public virtual float Usage()
        {
            return Casts() * Cost();
        }

    }

    public class BeaconOfLight : Spell
    {
        public BeaconOfLight(Rotation rotation)
            : base(rotation)
        {
            Duration = 60f + (Talents.GlyphOfBeaconOfLight ? 30f : 0f);
            Uptime = Rotation.FightLength * Rotation.CalcOpts.BoLUp;
            BaseCost = 1405f; // TODO: Determine exact mana cost.  6% of base mana
        }

        public float HealingDone(float procableHealing)
        {
            return procableHealing * Rotation.CalcOpts.BoLUp * 0.5f;
        }

    }

    public class JudgementsOfThePure : Spell
    {
        public JudgementsOfThePure(Rotation rotation, float JudgementCasts)
            : base(rotation)
        {
            Duration = 60f / 7.5f / JudgementCasts;
            Uptime = Rotation.CalcOpts.Activity * Rotation.FightLength;
            BaseCost = HealadinConstants.basemana * 0.05f;
        }

        public override float CastTime()
        {
            return 1.5f;
        }

    }

    public class DivineIllumination : Spell
    {

        public HolyLight HL_DI { get; set; }

        public DivineIllumination(Rotation rotation)
            : base(rotation)
        {
            Duration = 180f;
            Uptime = Rotation.FightLength;
            BaseCost = 0f;
            HL_DI = new HolyLight(rotation) { DivineIllumination = true };
        }

        public float Healed()
        {
            return HL_DI.HPS() * Time();
        }

        public override float Time()
        {
            return 15f * Casts() * Rotation.CalcOpts.Activity;
        }

        public override float Usage()
        {
            return Time() * HL_DI.MPS();
        }

    }

    public class DivineFavor : Spell
    {

        public HolyLight HL_DF { get; set; }

        public DivineFavor(Rotation rotation)
            : base(rotation)
        {
            Duration = 120f;
            Uptime = Rotation.FightLength;
            BaseCost = 130f;
            HL_DF = new HolyLight(rotation) { ExtraCritChance = 1f };
        }

        public float Healed()
        {
            return HL_DF.AverageHealed() * Casts();
        }

        public override float Time()
        {
            return HL_DF.CastTime() * Casts();
        }

        public override float Usage()
        {
            return Casts() * (Cost() + HL_DF.AverageCost());
        }

    }
}