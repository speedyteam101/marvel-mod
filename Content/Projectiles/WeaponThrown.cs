using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Projectiles
{
	// A thrown attached weapon: spear, chakram or buzzsaw blade. ai[0] = weapon index, ai[1] = bounces / returning flag.
	public class WeaponThrown : ModProjectile
	{
		public override string Texture => "MarvelMod/Content/Projectiles/SuitShot";

		private WeaponDef Weapon => SuitWeapons.All[Math.Clamp((int)Projectile.ai[0], 0, SuitWeapons.All.Length - 1)];

		private float Timer {
			get => Projectile.localAI[0];
			set => Projectile.localAI[0] = value;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 240;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}

		public override void AI() {
			if (Timer == 0f) {
				Projectile.penetrate = Weapon.Kind switch { WeaponKind.Spear => 3, WeaponKind.Buzzsaw => 6, _ => -1 };
				if (Weapon.Kind == WeaponKind.Spear) {
					Projectile.localNPCHitCooldown = -1;
				}
			}
			Timer++;

			switch (Weapon.Kind) {
				case WeaponKind.Spear:
					if (Timer > 15f) {
						Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.3f, 16f);
					}
					Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
					break;
				case WeaponKind.Chakram:
					Projectile.rotation += 0.5f;
					bool returning = Projectile.ai[1] == 1f || Timer > 25f;
					if (returning) {
						Projectile.ai[1] = 1f;
						Projectile.tileCollide = false;
						Player owner = Main.player[Projectile.owner];
						Vector2 toOwner = owner.Center - Projectile.Center;
						if (toOwner.Length() < 24f || !owner.active || owner.dead) {
							Projectile.Kill();
							return;
						}
						Projectile.velocity = Vector2.Lerp(Projectile.velocity, toOwner.SafeNormalize(Vector2.Zero) * 18f, 0.15f);
					}
					break;
				case WeaponKind.Buzzsaw:
					Projectile.rotation += 0.6f;
					if (Main.rand.NextBool(3)) {
						Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron).noGravity = true;
					}
					break;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			switch (Weapon.Kind) {
				case WeaponKind.Chakram:
					Projectile.ai[1] = 1f; // hit a wall: come back
					Projectile.netUpdate = true;
					return false;
				case WeaponKind.Buzzsaw:
					if (++Projectile.ai[1] > 4f) {
						return true;
					}
					if (Projectile.velocity.X != oldVelocity.X) {
						Projectile.velocity.X = -oldVelocity.X;
					}
					if (Projectile.velocity.Y != oldVelocity.Y) {
						Projectile.velocity.Y = -oldVelocity.Y;
					}
					SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
					return false;
				default:
					SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
					return true;
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			WeaponDef weapon = Weapon;
			if (weapon.Texture == null) {
				return false;
			}
			Texture2D texture = ModContent.Request<Texture2D>($"MarvelMod/Content/Weapons/{weapon.Texture}").Value;
			Color colour = Main.player[Projectile.owner].GetModPlayer<IronManPlayer>().Suit.Colour(SuitCatalog.WeaponColour);
			Color tint = new(lightColor.ToVector3() * colour.ToVector3() * 1.2f);
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, tint, Projectile.rotation, texture.Size() / 2f, 1f, SpriteEffects.None, 0);
			return false;
		}
	}
}
