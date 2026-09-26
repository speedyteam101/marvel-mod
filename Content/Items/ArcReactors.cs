using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MarvelMod.Content.Items
{
	// Wear an Arc Reactor to be able to suit up. Higher marks give a stronger suit and unlock more abilities.
	public abstract class ArcReactorBase : ModItem
	{
		public abstract int Tier { get; }

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TierStats());

		private string TierStats() {
			SuitStats stats = SuitStats.For(SuitPresets.Classic(), Tier);
			return $"Suit (standard systems): +{stats.Defense} defense, {stats.DamageReduction}% damage reduction, +{stats.Damage}% damage, +{stats.MoveSpeed}% move speed";
		}

		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 28;
			Item.accessory = true;
			Item.rare = Tier switch { 3 => ItemRarityID.Red, 2 => ItemRarityID.Pink, _ => ItemRarityID.Green };
			Item.value = Item.sellPrice(gold: Tier * Tier * 2);
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			IronManPlayer modPlayer = player.GetModPlayer<IronManPlayer>();
			if (Tier > modPlayer.reactorTierThisFrame) {
				modPlayer.reactorTierThisFrame = Tier;
			}
			// Even out of the suit, the reactor lights up your chest a little.
			Lighting.AddLight(player.Center, 0.1f * Tier, 0.25f * Tier, 0.3f * Tier);
		}

		// Only one reactor at a time.
		public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) {
			return !(equippedItem.ModItem is ArcReactorBase && incomingItem.ModItem is ArcReactorBase);
		}
	}

	public class ArcReactorMk1 : ArcReactorBase
	{
		public override int Tier => 1;

		public override void AddRecipes() {
			CreateRecipe()
				.AddRecipeGroup(RecipeGroupID.IronBar, 12)
				.AddIngredient(ItemID.FallenStar, 3)
				.AddIngredient(ItemID.Glass, 5)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	public class ArcReactorMk2 : ArcReactorBase
	{
		public override int Tier => 2;

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<ArcReactorMk1>()
				.AddIngredient(ItemID.HallowedBar, 10)
				.AddIngredient(ItemID.SoulofLight, 5)
				.AddIngredient(ItemID.SoulofMight, 5)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	public class ArcReactorMk3 : ArcReactorBase
	{
		public override int Tier => 3;

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<ArcReactorMk2>()
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient(ItemID.FragmentVortex, 10)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
