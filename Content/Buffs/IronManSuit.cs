using MarvelMod.Common.Players;
using Terraria;
using Terraria.ModLoader;

namespace MarvelMod.Content.Buffs
{
	// Suited up. Lasts until you suit down, right-click the buff, remove the Arc Reactor or die.
	// The stats come from IronManPlayer, which reads the Arc Reactor tier and the suit's Systems options.
	public class IronManSuit : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			player.buffTime[buffIndex] = 2; // never runs out
		}

		public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare) {
			IronManPlayer modPlayer = Main.LocalPlayer.GetModPlayer<IronManPlayer>();
			buffName = $"Iron Man (Mk {modPlayer.ReactorTier} reactor){(modPlayer.giantForm ? ", giant suit" : "")}";
			tip = modPlayer.Stats.ToString();
		}
	}
}
