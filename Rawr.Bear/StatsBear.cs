﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Rawr.Bear
{
	public class StatsBear : Stats
	{
		public float BonusMangleDamageMultiplier { get; set; }
		public float BonusMaulDamageMultiplier { get; set; }
		public float BonusEnrageDamageMultiplier { get; set; }
		public float MangleCooldownReduction { get; set; }
		public float BonusSwipeDamageMultiplier { get; set; }
		public float FurySwipesChance { get; set; }
		public float HasteOnFeralCharge { get; set; }
		public float BonusPulverizeDuration { get; set; }
		public float BonusLacerateCritChance { get; set; }
		public float BonusFaerieFireStacks { get; set; }
        public float BonusSurvivalInstinctsDurationMultiplier { get; set; }

		public WeightedStat[] TemporaryArmorUptimes { get; set; }

	}
}