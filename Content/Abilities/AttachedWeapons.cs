using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using MarvelMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Abilities
{
	// Uses the weapon attached to one of the suit's weapon slots. Only given while that slot has a weapon.
	// The item changes to match the weapon: damage, speed, damage class and attack all come from SuitWeapons.
	public abstract class AttachedWeapon : SuitAbility
	{
		private const int BaseDamage = 50;
		private const int BaseUseTime = 20;

		protected abstract SuitCategory Slot { get; }

		public WeaponDef WeaponOf(Player player) => SuitWeapons.All[SuitOf(player)[Slot]];

		public override bool IsAllowed(Player player) => base.IsAllowed(player) && WeaponOf(player).Kind != WeaponKind.None;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 28;
			Item.height = 28;
			Item.damage = BaseDamage;
			Item.useTime = BaseUseTime;
			Item.useAnimation = BaseUseTime;
			Item.knockBack = 5f;
			Item.shoot = ModContent.ProjectileType<WeaponSwing>();
			Item.shootSpeed = 1f;
		}

		// Match the item to the attached weapon. Called every tick and right before each use.
		private void Configure(Player player) {
			WeaponDef weapon = WeaponOf(player);
			Item.DamageType = weapon.Melee ? DamageClass.Melee : DamageClass.Ranged;
			Item.channel = weapon.Kind == WeaponKind.Shield;
			Item.autoReuse = weapon.Kind != WeaponKind.Shield;
			Item.knockBack = weapon.Knockback;
			Item.UseSound = weapon.Kind switch {
				WeaponKind.Swing or WeaponKind.Spin or WeaponKind.Thrust or WeaponKind.Spear or WeaponKind.Chakram => SoundID.Item1,
				WeaponKind.Whip => SoundID.Item153,
				_ => null // the rest play their own sounds in Shoot
			};
		}

		public override void UpdateInventory(Player player) {
			base.UpdateInventory(player);
			if (!Item.IsAir) {
				Configure(player);
			}
		}

		public override bool CanUseItem(Player player) {
			if (!base.CanUseItem(player)) {
				return false;
			}
			Configure(player);
			// Only one shield at a time.
			return WeaponOf(player).Kind != WeaponKind.Shield || player.ownedProjectileCounts[ModContent.ProjectileType<WeaponSwing>()] == 0;
		}

		public override float UseSpeedMultiplier(Player player) => BaseUseTime / (float)WeaponOf(player).UseTime;

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			base.ModifyWeaponDamage(player, ref damage);
			damage *= WeaponOf(player).Damage / (float)BaseDamage;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			WeaponDef weapon = WeaponOf(player);
			int index = SuitOf(player)[Slot];
			Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
			Vector2 hand = player.MountedCenter + aim * 16f;
			IronManPlayer modPlayer = player.GetModPlayer<IronManPlayer>();
			int owner = player.whoAmI;

			switch (weapon.Kind) {
				case WeaponKind.Swing:
				case WeaponKind.Spin:
				case WeaponKind.Thrust:
				case WeaponKind.Whip:
				case WeaponKind.Shield:
					modPlayer.swingSide = -modPlayer.swingSide;
					Projectile.NewProjectile(source, player.MountedCenter, aim, ModContent.ProjectileType<WeaponSwing>(), damage, knockback, owner, index, modPlayer.swingSide);
					break;
				case WeaponKind.Spear:
				case WeaponKind.Chakram:
				case WeaponKind.Buzzsaw:
					if (weapon.Kind == WeaponKind.Buzzsaw) {
						SoundEngine.PlaySound(SoundID.Item23, hand);
					}
					Projectile.NewProjectile(source, hand, aim * weapon.Speed, ModContent.ProjectileType<WeaponThrown>(), damage, knockback, owner, index);
					break;
				case WeaponKind.Rail:
					SoundEngine.PlaySound(SoundID.Item72, hand);
					SoundEngine.PlaySound(SoundID.Item38, hand);
					player.velocity -= aim * 9f; // recoil
					Projectile.NewProjectile(source, player.MountedCenter, aim, ModContent.ProjectileType<RailBeam>(), damage, knockback, owner);
					break;
				case WeaponKind.Flame:
					if (Main.rand.NextBool(3)) {
						SoundEngine.PlaySound(SoundID.Item34, hand);
					}
					Projectile.NewProjectile(source, hand, aim.RotatedByRandom(0.12f) * weapon.Speed, ModContent.ProjectileType<SuitShot>(), damage, knockback, owner, (float)ShotKind.Flame);
					break;
				case WeaponKind.Plasma:
					SoundEngine.PlaySound(SoundID.Item92, hand);
					Projectile.NewProjectile(source, hand, aim * weapon.Speed, ModContent.ProjectileType<SuitShot>(), damage, knockback, owner, (float)ShotKind.Plasma);
					break;
				case WeaponKind.Grenade:
					SoundEngine.PlaySound(SoundID.Item61, hand);
					Projectile.NewProjectile(source, hand, aim * weapon.Speed + new Vector2(0f, -2f * player.gravDir), ModContent.ProjectileType<SuitShot>(), damage, knockback, owner, (float)ShotKind.Grenade);
					break;
				case WeaponKind.Arc:
					ChainLightning(player, source, hand, damage, knockback);
					break;
			}
			return false;
		}

		// Strikes the enemy nearest the cursor, then up to 3 more, each close to the last, for a little less damage each time.
		private static void ChainLightning(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 hand, int damage, float knockback) {
			var hit = new HashSet<int>();
			Vector2 from = hand;
			Vector2 searchCentre = Main.MouseWorld;
			float searchRange = 400f;
			SoundEngine.PlaySound(SoundID.Item94, hand);

			for (int jump = 0; jump < 4; jump++) {
				NPC best = null;
				float bestDistance = searchRange;
				foreach (NPC npc in Main.ActiveNPCs) {
					if (hit.Contains(npc.whoAmI) || !npc.CanBeChasedBy() || Vector2.Distance(npc.Center, player.Center) > 700f) {
						continue;
					}
					float distance = Vector2.Distance(npc.Center, searchCentre);
					if (distance < bestDistance && Collision.CanHitLine(from, 1, 1, npc.Center, 1, 1)) {
						best = npc;
						bestDistance = distance;
					}
				}
				if (best == null) {
					break;
				}
				hit.Add(best.whoAmI);
				for (float t = 0f; t <= 1f; t += 0.06f) {
					Vector2 point = Vector2.Lerp(from, best.Center, t) + Main.rand.NextVector2Circular(5f, 5f);
					Dust spark = Dust.NewDustPerfect(point, DustID.Electric, Vector2.Zero, 0, default, 0.9f);
					spark.noGravity = true;
				}
				Projectile.NewProjectile(source, best.Center, Vector2.Zero, ModContent.ProjectileType<SuitExplosion>(), damage, knockback, player.whoAmI, 36f, 2f);
				from = best.Center;
				searchCentre = best.Center;
				searchRange = 260f;
				damage = damage * 4 / 5;
			}
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			WeaponDef weapon = WeaponOf(Main.LocalPlayer);
			if (weapon.Kind == WeaponKind.None) {
				return;
			}
			tooltips.Add(new TooltipLine(Mod, "AttachedWeapon", $"[c/FFD700:{weapon.Name}]: {weapon.Description}"));
		}

		// Show the attached weapon's picture in the inventory, when it has one.
		public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			WeaponDef weapon = WeaponOf(Main.LocalPlayer);
			if (weapon.Texture == null) {
				return true;
			}
			Texture2D texture = ModContent.Request<Texture2D>($"MarvelMod/Content/Weapons/{weapon.Texture}").Value;
			Color colour = SuitOf(Main.LocalPlayer).Colour(SuitCatalog.WeaponColour);
			float fit = 30f / System.Math.Max(texture.Width, texture.Height);
			spriteBatch.Draw(texture, position, null, colour, MathHelper.PiOver4, texture.Size() / 2f, fit, SpriteEffects.None, 0f);
			return false;
		}
	}

	public class WeaponOne : AttachedWeapon
	{
		protected override SuitCategory Slot => SuitCatalog.WeaponSlot1;
	}

	public class WeaponTwo : AttachedWeapon
	{
		protected override SuitCategory Slot => SuitCatalog.WeaponSlot2;
	}
}
