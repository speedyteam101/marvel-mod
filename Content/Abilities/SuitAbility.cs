using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Abilities
{
	// Base class for suit weapons. IronManPlayer puts these in the inventory while suited up and removes them afterwards.
	// They can't be kept: they vanish when you suit down, if your Arc Reactor tier is too low, or if they're dropped.
	public abstract class SuitAbility : ModItem
	{
		public static readonly List<SuitAbility> All = new();

		// Lowest Arc Reactor tier that grants this ability.
		public virtual int RequiredTier => 1;

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 0; // can't be researched or duplicated
			All.Add(this);
		}

		public override void Unload() {
			All.Clear();
		}

		public override void SetDefaults() {
			Item.maxStack = 1;
			Item.value = 0;
			Item.rare = ItemRarityID.Red;
			Item.DamageType = DamageClass.Ranged;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.autoReuse = true;
		}

		public static SuitConfig SuitOf(Player player) => player.GetModPlayer<IronManPlayer>().Suit;

		public virtual bool IsAllowed(Player player) {
			IronManPlayer modPlayer = player.GetModPlayer<IronManPlayer>();
			return modPlayer.SuitActive && modPlayer.ReactorTier >= RequiredTier;
		}

		public override bool CanUseItem(Player player) => IsAllowed(player);

		public override void UpdateInventory(Player player) {
			if (!IsAllowed(player)) {
				Item.TurnToAir();
			}
		}

		// Dropped in the world: disappear.
		public override void PostUpdate() {
			Item.TurnToAir();
		}

		// Flat damage added as bosses are beaten, so the suit keeps up through the game.
		public static int ProgressionBonus() {
			int bonus = 0;
			if (NPC.downedBoss3) bonus += 10;
			if (Main.hardMode) bonus += 20;
			if (NPC.downedMechBossAny) bonus += 20;
			if (NPC.downedPlantBoss) bonus += 25;
			if (NPC.downedMoonlord) bonus += 50;
			return bonus;
		}

		public static float TierMultiplier(int tier) => tier switch { 3 => 2.4f, 2 => 1.6f, _ => 1f };

		// Damage multiplier for the suit's current Systems option (1 = unchanged).
		protected virtual float ModeDamage(SuitConfig suit) => 1f;

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			damage.Base += ProgressionBonus();
			damage *= TierMultiplier(player.GetModPlayer<IronManPlayer>().ReactorTier) * ModeDamage(SuitOf(player));
		}
	}
}
