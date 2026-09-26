using MarvelMod.Common.Suits;
using MarvelMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Abilities
{
	// Palm repulsor blasts. The suit's Repulsor Mode changes how they fire.
	public class Repulsors : SuitAbility
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 28;
			Item.height = 28;
			Item.damage = 28;
			Item.knockBack = 3f;
			Item.useTime = 18;
			Item.useAnimation = 18;
			Item.UseSound = SoundID.Item12;
			Item.shoot = ModContent.ProjectileType<SuitShot>();
			Item.shootSpeed = 14f;
		}

		protected override float ModeDamage(SuitConfig suit) => suit.OptionName(SuitCatalog.RepulsorMode) switch {
			"Rapid" => 0.55f,
			"Spread" => 0.6f,
			"Heavy" => 2.4f,
			"Piercing" => 0.9f,
			"Twin" => 0.6f,
			_ => 1f
		};

		// Higher is faster.
		public override float UseSpeedMultiplier(Player player) => SuitOf(player).OptionName(SuitCatalog.RepulsorMode) switch {
			"Rapid" => 2f,
			"Heavy" => 0.5f,
			_ => 1f
		};

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
			Vector2 hand = player.MountedCenter + direction * 22f;
			float repulsor = (float)ShotKind.Repulsor;

			switch (SuitOf(player).OptionName(SuitCatalog.RepulsorMode)) {
				case "Rapid":
					Projectile.NewProjectile(source, hand, velocity.RotatedByRandom(0.08f), type, damage, knockback, player.whoAmI, repulsor);
					break;
				case "Spread":
					for (int i = -1; i <= 1; i++) {
						Projectile.NewProjectile(source, hand, velocity.RotatedBy(i * 0.18f), type, damage, knockback, player.whoAmI, repulsor);
					}
					break;
				case "Heavy":
					Projectile.NewProjectile(source, hand, velocity * 0.8f, type, damage, knockback * 2f, player.whoAmI, (float)ShotKind.HeavyRepulsor);
					break;
				case "Piercing":
					Projectile.NewProjectile(source, hand, velocity * 1.3f, type, damage, knockback, player.whoAmI, repulsor, 1f);
					break;
				case "Twin":
					Vector2 side = direction.RotatedBy(MathHelper.PiOver2) * 7f;
					Projectile.NewProjectile(source, hand + side, velocity, type, damage, knockback, player.whoAmI, repulsor);
					Projectile.NewProjectile(source, hand - side, velocity, type, damage, knockback, player.whoAmI, repulsor);
					break;
				default:
					Projectile.NewProjectile(source, hand, velocity, type, damage, knockback, player.whoAmI, repulsor);
					break;
			}
			return false;
		}
	}
}
