using Terraria;

namespace MarvelMod.Common.Suits
{
	// What a suit does in play, from the Arc Reactor tier and the Systems options.
	public struct SuitStats
	{
		public int Defense;
		public int DamageReduction; // percent
		public int Damage;          // percent, all classes
		public int AttackSpeed;     // percent, all classes
		public int Crit;            // percent, all classes
		public int MeleeDamage;     // percent, melee only
		public int MeleeSpeed;      // percent, melee only
		public int MoveSpeed;       // percent
		public int LifeRegen;       // health per second
		public float FlightSpeed;   // pixels per tick
		public float FlightAcceleration;
		public bool Hover;          // hold altitude when no direction is held
		public bool NoKnockback;
		public SuitSet Set;         // premium set whose bonus is active, if any

		public static SuitStats For(SuitConfig config, int tier) {
			tier = System.Math.Clamp(tier, 1, 3);
			var stats = new SuitStats {
				Defense = tier switch { 3 => 50, 2 => 28, _ => 10 },
				DamageReduction = tier switch { 3 => 16, 2 => 10, _ => 4 },
				Damage = tier switch { 3 => 45, 2 => 25, _ => 10 },
				Crit = tier switch { 3 => 12, 2 => 8, _ => 4 },
				MoveSpeed = tier switch { 3 => 30, 2 => 20, _ => 10 },
				LifeRegen = tier switch { 3 => 6, 2 => 3, _ => 1 },
				FlightSpeed = tier switch { 3 => 13f, 2 => 10f, _ => 7f },
				FlightAcceleration = tier switch { 3 => 0.6f, 2 => 0.45f, _ => 0.35f },
				NoKnockback = tier >= 2
			};

			float defense = stats.Defense;
			switch (config.OptionName(SuitCatalog.Plating)) {
				case "Light":
					defense *= 0.6f;
					stats.MoveSpeed += 10;
					stats.FlightSpeed *= 1.2f;
					break;
				case "Reinforced":
					defense *= 1.25f;
					stats.MoveSpeed -= 5;
					break;
				case "Heavy":
					defense *= 1.6f;
					stats.DamageReduction += 5;
					stats.MoveSpeed -= 15;
					stats.FlightSpeed *= 0.85f;
					stats.NoKnockback = true;
					break;
				case "Vibranium Alloy":
					defense *= 1.35f;
					stats.DamageReduction += 3;
					stats.MoveSpeed -= 5;
					break;
			}

			switch (config.OptionName(SuitCatalog.Thrusters)) {
				case "Racing":
					stats.FlightSpeed *= 1.35f;
					stats.FlightAcceleration *= 0.8f;
					break;
				case "Heavy-Lift":
					stats.FlightSpeed *= 0.9f;
					stats.FlightAcceleration *= 1.3f;
					break;
				case "Hover":
					stats.FlightSpeed *= 0.9f;
					stats.Hover = true;
					break;
				case "Afterburner":
					stats.FlightSpeed *= 1.6f;
					stats.FlightAcceleration *= 1.1f;
					stats.DamageReduction -= 3;
					break;
			}

			switch (config.OptionName(SuitCatalog.PowerCore)) {
				case "Overclocked":
					stats.Damage += 15;
					stats.LifeRegen -= 1;
					break;
				case "Efficient":
					stats.AttackSpeed += 12;
					break;
				case "Regenerative":
					stats.LifeRegen += 3;
					stats.Damage -= 5;
					break;
				case "Unstable":
					stats.Damage += 25;
					stats.Crit += 8;
					defense *= 0.85f;
					break;
			}

			stats.Set = SuitSets.WornBy(config);
			if (stats.Set == SuitSets.Vampiric) {
				stats.Damage += Main.dayTime ? 10 : 20;
			}
			else if (stats.Set == SuitSets.Infernal) {
				stats.Damage += 15;
			}
			else if (stats.Set == SuitSets.Titan) {
				defense += 40;
				stats.DamageReduction += 8;
				stats.NoKnockback = true;
				stats.MoveSpeed -= 10;
			}
			else if (stats.Set == SuitSets.Cryo) {
				stats.Crit += 15;
			}
			else if (stats.Set == SuitSets.Storm) {
				stats.MoveSpeed += 25;
				stats.FlightSpeed *= 1.25f;
			}
			else if (stats.Set == SuitSets.Void) {
				stats.Damage += 20;
			}
			else if (stats.Set == SuitSets.Godly) {
				stats.Damage += 30;
				stats.Crit += 10;
				defense += 30;
				stats.DamageReduction += 10;
				stats.LifeRegen += 8;
				stats.FlightSpeed *= 1.3f;
			}

			else if (stats.Set == SuitSets.Dragon) {
				stats.Damage += 20;
				stats.FlightSpeed *= 1.2f;
			}
			else if (stats.Set == SuitSets.Samurai) {
				stats.MeleeDamage += 20;
				stats.MeleeSpeed += 15;
				stats.Crit += 10;
			}
			else if (stats.Set == SuitSets.Shinobi) {
				stats.MoveSpeed += 20;
				stats.Crit += 15;
			}
			else if (stats.Set == SuitSets.Pharaoh) {
				stats.Damage += 12;
				defense += 20;
			}
			else if (stats.Set == SuitSets.Cyber) {
				stats.Damage += 15;
				stats.AttackSpeed += 15;
			}

			stats.Defense = (int)defense;
			stats.LifeRegen = System.Math.Max(0, stats.LifeRegen);
			stats.DamageReduction = System.Math.Max(0, stats.DamageReduction);
			return stats;
		}

		public void Apply(Player player) {
			player.statDefense += Defense;
			player.endurance += DamageReduction / 100f;
			player.GetDamage(Terraria.ModLoader.DamageClass.Generic) += Damage / 100f;
			player.GetAttackSpeed(Terraria.ModLoader.DamageClass.Generic) += AttackSpeed / 100f;
			player.GetCritChance(Terraria.ModLoader.DamageClass.Generic) += Crit;
			player.moveSpeed += MoveSpeed / 100f;
			player.GetDamage(Terraria.ModLoader.DamageClass.Melee) += MeleeDamage / 100f;
			player.GetAttackSpeed(Terraria.ModLoader.DamageClass.Melee) += MeleeSpeed / 100f;
			if (NoKnockback) {
				player.noKnockback = true;
			}
		}

		public override string ToString() {
			return $"+{Defense} defense, {DamageReduction}% damage reduction\n"
				+ $"+{Damage}% damage, +{Crit}% crit\n"
				+ $"+{AttackSpeed}% attack speed, +{MoveSpeed}% move speed\n"
				+ $"{LifeRegen} HP/s regen, flight speed {FlightSpeed:0.#}\n"
				+ (Hover ? "Hovers in place" : "No hover") + (NoKnockback ? ", no knockback" : "")
				+ (Set != null ? $"\n[c/FFD700:{Set.Name} set bonus active]" : "");
		}
	}
}
