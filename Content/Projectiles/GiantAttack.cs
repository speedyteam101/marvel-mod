using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Projectiles
{
	public enum GiantAttackKind
	{
		Punch,   // big hitbox in front of the suit
		Charge,  // hitbox that rides along during a rocket charge
		Wave,    // shockwave that rolls along the ground
		Orbital  // target marker, then a beam from the sky
	}

	// Attacks of the giant suit that aren't ordinary shots. ai[0] = GiantAttackKind. velocity is the direction for Punch and Wave.
	public class GiantAttack : ModProjectile
	{
		private const int OrbitalDelay = 50;
		private const int OrbitalBeam = 20;

		public override string Texture => "MarvelMod/Content/Projectiles/SuitShot";

		private GiantAttackKind Kind => (GiantAttackKind)(int)Projectile.ai[0];

		private Player Owner => Main.player[Projectile.owner];

		private float Timer {
			get => Projectile.localAI[0];
			set => Projectile.localAI[0] = value;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.timeLeft = 600;
		}

		public override bool ShouldUpdatePosition() => Kind == GiantAttackKind.Wave;

		public override void AI() {
			if (Timer == 0f) {
				Setup();
			}
			Timer++;
			Player owner = Owner;
			Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);

			switch (Kind) {
				case GiantAttackKind.Punch:
					Projectile.Center = owner.MountedCenter + direction * 60f;
					if (Timer == 1f) {
						SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
						for (int i = 0; i < 20; i++) {
							Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, direction.X * 4f, direction.Y * 4f, 100, default, 1.6f);
						}
					}
					break;
				case GiantAttackKind.Charge:
					Projectile.Center = owner.MountedCenter;
					Dust fire = Dust.NewDustDirect(owner.position, owner.width, owner.height, DustID.Torch, -owner.velocity.X * 0.3f, -owner.velocity.Y * 0.3f, 0, default, 2f);
					fire.noGravity = true;
					break;
				case GiantAttackKind.Wave:
					// Roll along the ground: sink until the bottom touches the floor, climb small steps.
					Vector2 bottom = Projectile.Bottom;
					if (!Collision.SolidCollision(new Vector2(Projectile.position.X, bottom.Y), Projectile.width, 16)) {
						Projectile.position.Y += 8f;
					}
					else if (Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height - 16)) {
						Projectile.position.Y -= 8f;
					}
					for (int i = 0; i < 3; i++) {
						Dust dust = Dust.NewDustDirect(new Vector2(Projectile.position.X, Projectile.Bottom.Y - 20f), Projectile.width, 20, DustID.Smoke, 0f, -3f, 80, default, 1.8f);
						dust.noGravity = true;
					}
					break;
				case GiantAttackKind.Orbital:
					// Hurt only while the beam is firing.
					Projectile.friendly = Timer > OrbitalDelay;
					if (Timer == OrbitalDelay + 1) {
						SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);
						for (int i = 0; i < 40; i++) {
							Dust dust = Dust.NewDustDirect(new Vector2(Projectile.position.X, Projectile.Bottom.Y - 40f), Projectile.width, 40, DustID.Electric, 0f, -6f, 0, default, 1.5f);
							dust.noGravity = true;
						}
					}
					Lighting.AddLight(new Vector2(Projectile.Center.X, Projectile.Bottom.Y - 20f), 0.8f, 0.9f, 1f);
					break;
			}
		}

		private void Setup() {
			switch (Kind) {
				case GiantAttackKind.Punch:
					Projectile.Resize(110, 110);
					Projectile.timeLeft = 10;
					break;
				case GiantAttackKind.Charge:
					Projectile.Resize(120, 120);
					Projectile.timeLeft = 25;
					Projectile.localNPCHitCooldown = 10;
					break;
				case GiantAttackKind.Wave:
					Projectile.Resize(60, 90);
					Projectile.timeLeft = 50;
					Projectile.DamageType = DamageClass.Melee;
					break;
				case GiantAttackKind.Orbital:
					// A tall column ending at the target point.
					Vector2 target = Projectile.Center;
					Projectile.Resize(120, 900);
					Projectile.Bottom = target + new Vector2(0f, 40f);
					Projectile.timeLeft = OrbitalDelay + OrbitalBeam;
					Projectile.DamageType = DamageClass.Ranged;
					break;
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			if (Kind == GiantAttackKind.Orbital) {
				SuitConfig suit = Owner.GetModPlayer<IronManPlayer>().Suit;
				Color colour = suit.Colour(SuitCatalog.RepulsorColour) with { A = 0 };
				Texture2D pixel = TextureAssets.MagicPixel.Value;
				Rectangle source = new(0, 0, 1, 1);
				Vector2 groundPoint = new Vector2(Projectile.Center.X, Projectile.Bottom.Y - 40f) - Main.screenPosition;

				if (Timer <= OrbitalDelay) {
					// Blinking target marker while the strike charges.
					float blink = (int)(Timer / 5) % 2 == 0 ? 1f : 0.4f;
					float size = 60f * (1f - Timer / OrbitalDelay) + 20f;
					Main.EntitySpriteDraw(pixel, groundPoint, source, colour * blink, 0f, new Vector2(0.5f, 0.5f), new Vector2(size, 3f), SpriteEffects.None, 0);
					Main.EntitySpriteDraw(pixel, groundPoint, source, colour * blink, 0f, new Vector2(0.5f, 0.5f), new Vector2(3f, size), SpriteEffects.None, 0);
				}
				else {
					float fade = 1f - (Timer - OrbitalDelay) / OrbitalBeam;
					float height = Projectile.height;
					Main.EntitySpriteDraw(pixel, groundPoint, source, colour * 0.4f * fade, 0f, new Vector2(0.5f, 1f), new Vector2(Projectile.width, height), SpriteEffects.None, 0);
					Main.EntitySpriteDraw(pixel, groundPoint, source, colour * fade, 0f, new Vector2(0.5f, 1f), new Vector2(Projectile.width * 0.5f, height), SpriteEffects.None, 0);
					Main.EntitySpriteDraw(pixel, groundPoint, source, Color.White with { A = 0 } * fade, 0f, new Vector2(0.5f, 1f), new Vector2(Projectile.width * 0.15f, height), SpriteEffects.None, 0);
				}
			}
			return false;
		}
	}
}
