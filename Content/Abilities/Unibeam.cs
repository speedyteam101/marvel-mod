using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using MarvelMod.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Abilities
{
	// Hold to fire a beam from the Arc Reactor. It overheats after a few seconds and needs to cool down.
	// Needs a Mk II Arc Reactor or better. The suit's Unibeam Mode changes the beam.
	public class Unibeam : SuitAbility
	{
		public override int RequiredTier => 2;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 28;
			Item.height = 28;
			Item.damage = 45;
			Item.knockBack = 2f;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.channel = true;
			Item.autoReuse = false;
			Item.UseSound = SoundID.Item13;
			Item.shoot = ModContent.ProjectileType<UnibeamBeam>();
			Item.shootSpeed = 1f;
		}

		protected override float ModeDamage(SuitConfig suit) => UnibeamBeam.DamageMultiplier(suit);

		public override bool CanUseItem(Player player) {
			return base.CanUseItem(player)
				&& player.GetModPlayer<IronManPlayer>().unibeamCooldown <= 0
				&& player.ownedProjectileCounts[Item.shoot] == 0;
		}
	}
}
