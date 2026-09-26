using MarvelMod.Common.Players;
using MarvelMod.Common.UI;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Items
{
	// Left click opens the Suit Workshop, right click suits up or down. The same as the two keybinds.
	public class StarkTablet : ModItem
	{
		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(silver: 50);
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool? UseItem(Player player) {
			if (player.whoAmI != Main.myPlayer) {
				return true;
			}
			if (player.altFunctionUse == 2) {
				player.GetModPlayer<IronManPlayer>().ToggleSuit();
			}
			else {
				SuitWorkshopSystem.Toggle();
			}
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddRecipeGroup(RecipeGroupID.IronBar, 5)
				.AddIngredient(ItemID.Glass, 10)
				.AddIngredient(ItemID.FallenStar)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
