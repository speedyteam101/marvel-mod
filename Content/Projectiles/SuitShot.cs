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
	public enum ShotKind
	{
		Repulsor,
		HeavyRepulsor,
		Missile,
		Bullet,
		Flare,
		Laser,
		Rocket,
		ClusterBomb,
		Bomblet,
		Flame,
		Plasma,
		Grenade,
		MegaRepulsor
	}

	// Every suit projectile except the Unibeam and melee weapons. ai[0] is the ShotKind; ai[1] = 1 makes a repulsor piercing.
	// ai[2] counts grenade bounces.
	public class SuitShot : ModProjectile
	{
		private ShotKind Kind => (ShotKind)(int)Projectile.ai[0];

		private bool Explosive => Kind is ShotKind.HeavyRepulsor or ShotKind.Missile or ShotKind.Rocket or ShotKind.ClusterBomb
			or ShotKind.Bomblet or ShotKind.Plasma or ShotKind.Grenade or ShotKind.MegaRepulsor;

		private bool HasGravity => Kind is ShotKind.Flare or ShotKind.ClusterBomb or ShotKind.Bomblet or ShotKind.Grenade;

		public override void SetDefaults() {
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 120;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1; // each target is only hit once by one shot
		}

		// Kind-specific setup, done on the first tick on every client (ai[] is synced at spawn).
		private void Init() {
			switch (Kind) {
				case ShotKind.Repulsor:
					Projectile.extraUpdates = 1;
					Projectile.timeLeft = 60;
					if (Projectile.ai[1] == 1f) {
						Projectile.penetrate = 5;
					}
					break;
				case ShotKind.HeavyRepulsor:
					Projectile.Resize(22, 22);
					Projectile.extraUpdates = 1;
					Projectile.timeLeft = 70;
					break;
				case ShotKind.Missile:
					Projectile.timeLeft = 180;
					break;
				case ShotKind.Bullet:
					Projectile.Resize(6, 6);
					Projectile.extraUpdates = 2;
					Projectile.timeLeft = 60;
					break;
				case ShotKind.Flare:
					Projectile.Resize(10, 10);
					Projectile.timeLeft = 120;
					break;
				case ShotKind.Laser:
					Projectile.Resize(8, 8);
					Projectile.extraUpdates = 3;
					Projectile.penetrate = 4;
					Projectile.timeLeft = 60;
					break;
				case ShotKind.Rocket:
					Projectile.Resize(16, 16);
					Projectile.timeLeft = 150;
					break;
				case ShotKind.ClusterBomb:
					Projectile.Resize(14, 14);
					Projectile.timeLeft = 90;
					break;
				case ShotKind.Bomblet:
					Projectile.Resize(8, 8);
					Projectile.timeLeft = 90;
					break;
				case ShotKind.Flame:
					Projectile.Resize(16, 16);
					Projectile.timeLeft = 35;
					Projectile.penetrate = 4;
					Projectile.localNPCHitCooldown = 10;
					break;
				case ShotKind.Plasma:
					Projectile.Resize(24, 24);
					Projectile.timeLeft = 120;
					break;
				case ShotKind.Grenade:
					Projectile.Resize(12, 12);
					Projectile.timeLeft = 100;
					break;
				case ShotKind.MegaRepulsor:
					Projectile.Resize(44, 44);
					Projectile.timeLeft = 90;
					break;
			}
		}

		private Color ShotColour() {
			SuitConfig suit = Main.player[Projectile.owner].GetModPlayer<IronManPlayer>().Suit;
			return Kind switch {
				ShotKind.Bullet => new Color(255, 220, 120),
				ShotKind.Flare => suit.Colour(SuitCatalog.ThrusterColour),
				ShotKind.Flame => new Color(255, 140, 40),
				ShotKind.Grenade => suit.Colour(SuitCatalog.WeaponColour),
				_ => suit.Colour(SuitCatalog.RepulsorColour)
			};
		}

		public override void AI() {
			if (Projectile.localAI[0] == 0f) {
				Projectile.localAI[0] = 1f;
				Init();
			}

			Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, ShotColour().ToVector3() * 0.5f);

			if (HasGravity) {
				Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.2f, 14f);
			}

			switch (Kind) {
				case ShotKind.Missile:
					// Short launch, then home in on the closest enemy.
					if (++Projectile.localAI[1] > 15f) {
						NPC target = FindTarget(700f);
						float speed = 12f;
						Vector2 wanted = target != null
							? (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * speed
							: Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
						Projectile.velocity = Vector2.Lerp(Projectile.velocity, wanted, 0.08f);
					}
					SmokeTrail();
					break;
				case ShotKind.Rocket:
					if (Projectile.velocity.Length() < 18f) {
						Projectile.velocity *= 1.06f;
					}
					SmokeTrail();
					break;
				case ShotKind.Flame:
					Projectile.velocity *= 0.96f;
					if (Main.rand.NextBool(2)) {
						Dust flame = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 0, default, 1.6f);
						flame.noGravity = true;
					}
					break;
				case ShotKind.Plasma:
					Dust plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.TintableDustLighted, 0f, 0f, 0, ShotColour(), 1.1f);
					plasma.noGravity = true;
					break;
				case ShotKind.Flare:
					if (Main.rand.NextBool(2)) {
						Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
						dust.noGravity = true;
						dust.scale = 1.3f;
					}
					break;
			}
		}

		private void SmokeTrail() {
			Dust smoke = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity, DustID.Smoke, -Projectile.velocity * 0.1f, 120, default, 1.1f);
			smoke.noGravity = true;
			Dust fire = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f, DustID.Torch, Vector2.Zero, 0, default, 1.2f);
			fire.noGravity = true;
		}

		private NPC FindTarget(float range) {
			NPC best = null;
			float bestDistance = range;
			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.CanBeChasedBy(Projectile)) {
					continue;
				}
				float distance = Vector2.Distance(npc.Center, Projectile.Center);
				if (distance < bestDistance && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1)) {
					best = npc;
					bestDistance = distance;
				}
			}
			return best;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (Kind is ShotKind.Flare or ShotKind.Flame) {
				target.AddBuff(BuffID.OnFire3, 240);
			}
		}

		// Grenades bounce twice before exploding; everything else stops at walls.
		public override bool OnTileCollide(Vector2 oldVelocity) {
			if (Kind != ShotKind.Grenade || Projectile.ai[2] >= 2f) {
				return true;
			}
			Projectile.ai[2]++;
			if (Projectile.velocity.X != oldVelocity.X) {
				Projectile.velocity.X = -oldVelocity.X * 0.6f;
			}
			if (Projectile.velocity.Y != oldVelocity.Y) {
				Projectile.velocity.Y = -oldVelocity.Y * 0.6f;
			}
			SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
			return false;
		}

		public override void OnKill(int timeLeft) {
			if (Explosive) {
				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
				if (Projectile.owner == Main.myPlayer) {
					int size = Kind switch {
						ShotKind.Rocket => 150,
						ShotKind.Plasma => 160,
						ShotKind.MegaRepulsor => 220,
						ShotKind.Grenade => 90,
						ShotKind.HeavyRepulsor => 90,
						ShotKind.Missile => 60,
						_ => 50
					};
					Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SuitExplosion>(),
						Projectile.damage, Projectile.knockBack, Projectile.owner, size);

					if (Kind == ShotKind.ClusterBomb) {
						for (int i = 0; i < 6; i++) {
							Vector2 velocity = new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-6f, -2f));
							Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, velocity, Type,
								(int)(Projectile.damage * 0.6f), Projectile.knockBack, Projectile.owner, (float)ShotKind.Bomblet);
						}
					}
				}
			}
			else {
				Color colour = ShotColour();
				for (int i = 0; i < 8; i++) {
					Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.TintableDustLighted, 0f, 0f, 0, colour, 0.9f);
					dust.noGravity = true;
					dust.velocity *= 2f;
				}
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Vector2 position = Projectile.Center - Main.screenPosition;

			if (Kind is ShotKind.Missile or ShotKind.Rocket or ShotKind.ClusterBomb or ShotKind.Bomblet or ShotKind.Grenade) {
				Texture2D missile = ModContent.Request<Texture2D>("MarvelMod/Content/Projectiles/SuitMissile").Value;
				float scale = Kind switch { ShotKind.Rocket => 1.4f, ShotKind.Bomblet or ShotKind.Grenade => 0.7f, _ => 1f };
				Main.EntitySpriteDraw(missile, position, null, lightColor, Projectile.rotation + MathHelper.PiOver2,
					missile.Size() / 2f, scale, SpriteEffects.None, 0);
				return false;
			}

			// Energy shots: the glowing orb texture, stretched along the direction of travel. A = 0 draws additively.
			Texture2D orb = ModContent.Request<Texture2D>(Texture).Value;
			Color colour = ShotColour() with { A = 0 };
			Vector2 stretch = Kind switch {
				ShotKind.HeavyRepulsor => new Vector2(2.4f, 1.8f),
				ShotKind.Laser => new Vector2(3.5f, 0.45f),
				ShotKind.Bullet => new Vector2(2f, 0.35f),
				ShotKind.Flare => new Vector2(1f, 1f),
				ShotKind.Flame => new Vector2(1.2f, 1.2f) * (1.8f - Projectile.timeLeft / 35f),
				ShotKind.Plasma => new Vector2(2.8f, 2.4f),
				ShotKind.MegaRepulsor => new Vector2(5f, 3.6f),
				_ => new Vector2(1.8f, 1f)
			};
			Main.EntitySpriteDraw(orb, position, null, colour, Projectile.rotation, orb.Size() / 2f, stretch, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(orb, position, null, Color.White with { A = 0 } * 0.8f, Projectile.rotation, orb.Size() / 2f, stretch * 0.5f, SpriteEffects.None, 0);
			return false;
		}
	}
}
