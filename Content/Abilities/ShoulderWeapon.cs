using MarvelMod.Common.Suits;
using MarvelMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Abilities
{
	// Fires whichever weapon the suit's Shoulder Weapon option picks.
	public class ShoulderWeapon : SuitAbility
	{
		private const int BaseUseTime = 30;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 28;
			Item.height = 28;
			Item.damage = 30;
			Item.knockBack = 4f;
			Item.useTime = BaseUseTime;
			Item.useAnimation = BaseUseTime;
			Item.shoot = ModContent.ProjectileType<SuitShot>();
			Item.shootSpeed = 10f;
		}

		private static int UseTimeFor(string weapon) => weapon switch {
			"Minigun" => 5,
			"Flare Pods" => 45,
			"Shoulder Laser" => 22,
			"Heavy Rocket" => 70,
			"Cluster Bombs" => 55,
			_ => 40 // Micro-Missiles
		};

		private static string Weapon(Player player) => SuitOf(player).OptionName(SuitCatalog.ShoulderWeapon);

		public override float UseSpeedMultiplier(Player player) => BaseUseTime / (float)UseTimeFor(Weapon(player));

		protected override float ModeDamage(SuitConfig suit) => suit.OptionName(SuitCatalog.ShoulderWeapon) switch {
			"Minigun" => 0.35f,
			"Flare Pods" => 0.5f,
			"Shoulder Laser" => 1.2f,
			"Heavy Rocket" => 3f,
			"Cluster Bombs" => 1f,
			_ => 0.8f
		};

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
			// Shoulder height, on the side the player faces.
			Vector2 shoulder = player.MountedCenter + new Vector2(player.direction * 8f, -12f * player.gravDir);
			int owner = player.whoAmI;

			switch (Weapon(player)) {
				case "Minigun":
					SoundEngine.PlaySound(SoundID.Item11, shoulder);
					Projectile.NewProjectile(source, shoulder, direction.RotatedByRandom(0.07f) * 16f, type, damage, knockback * 0.3f, owner, (float)ShotKind.Bullet);
					break;
				case "Flare Pods":
					SoundEngine.PlaySound(SoundID.Item20, shoulder);
					for (int i = 0; i < 5; i++) {
						Vector2 launch = direction.RotatedBy((i - 2) * 0.22f) * 9f + new Vector2(0f, -3f * player.gravDir);
						Projectile.NewProjectile(source, shoulder, launch, type, damage, knockback, owner, (float)ShotKind.Flare);
					}
					break;
				case "Shoulder Laser":
					SoundEngine.PlaySound(SoundID.Item12, shoulder);
					Projectile.NewProjectile(source, shoulder, direction * 16f, type, damage, knockback, owner, (float)ShotKind.Laser);
					break;
				case "Heavy Rocket":
					SoundEngine.PlaySound(SoundID.Item61, shoulder);
					Projectile.NewProjectile(source, shoulder, direction * 6f, type, damage, knockback * 2f, owner, (float)ShotKind.Rocket);
					break;
				case "Cluster Bombs":
					SoundEngine.PlaySound(SoundID.Item61, shoulder);
					Projectile.NewProjectile(source, shoulder, direction * 9f + new Vector2(0f, -3f * player.gravDir), type, damage, knockback, owner, (float)ShotKind.ClusterBomb);
					break;
				default: // Micro-Missiles: launched upward, then they home in
					SoundEngine.PlaySound(SoundID.Item61, shoulder);
					for (int i = 0; i < 3; i++) {
						Vector2 launch = new Vector2(direction.X * 3f + (i - 1) * 2f, -6f * player.gravDir);
						Projectile.NewProjectile(source, shoulder, launch, type, damage, knockback, owner, (float)ShotKind.Missile);
					}
					break;
			}
			return false;
		}
	}
}
