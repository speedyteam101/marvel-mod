using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Projectiles
{
	// Invisible blast left behind by missiles, rockets, bombs and heavy repulsors, and the Godly set's holy light.
	// ai[0] is the blast diameter in pixels; ai[1] = 1 makes it holy light instead of fire.
	public class SuitExplosion : ModProjectile
	{
		public override string Texture => "MarvelMod/Content/Projectiles/SuitShot";

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 4;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI() {
			if (Projectile.localAI[0] != 0f) {
				return;
			}
			Projectile.localAI[0] = 1f;
			int size = (int)Projectile.ai[0];
			Projectile.Resize(size, size);

			bool holy = Projectile.ai[1] == 1f;
			if (holy) {
				Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, Projectile.Center);
				// A column of light falling onto the target.
				for (int i = 0; i < 30; i++) {
					Dust beam = Dust.NewDustPerfect(Projectile.Center - new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-6f, 6f), i * 12f),
						DustID.GoldFlame, new Microsoft.Xna.Framework.Vector2(0f, 4f), 0, default, 1.6f);
					beam.noGravity = true;
				}
			}
			for (int i = 0; i < size / 3; i++) {
				Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, holy ? DustID.GoldFlame : DustID.Torch, 0f, 0f, 0, default, 2f);
				fire.noGravity = true;
				fire.velocity *= 3f;
				Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
				smoke.velocity *= 1.5f;
			}
		}

		public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor) => false;
	}
}
