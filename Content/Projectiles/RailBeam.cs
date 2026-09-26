using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Projectiles
{
	// The Anti-Tank Railgun shot: an instant beam to the first wall, hitting every enemy on the way once, then fading.
	// velocity is the aim direction. The railgun itself is drawn at the shoulder while the beam lasts.
	public class RailBeam : ModProjectile
	{
		private const float MaxLength = 1800f;
		private const int Lifetime = 18;

		public override string Texture => "MarvelMod/Content/Projectiles/SuitShot";

		private float Length {
			get => Projectile.localAI[0];
			set => Projectile.localAI[0] = value;
		}

		public override void SetDefaults() {
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = Lifetime;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			Projectile.Center = owner.MountedCenter + new Vector2(0f, -10f * owner.gravDir);

			if (Length == 0f) {
				float length = 0f;
				while (length < MaxLength) {
					Vector2 point = Projectile.Center + direction * length;
					if (WorldGen.SolidTile((int)(point.X / 16f), (int)(point.Y / 16f))) {
						break;
					}
					length += 8f;
				}
				Length = System.Math.Max(length, 16f);

				Color colour = owner.GetModPlayer<IronManPlayer>().Suit.Colour(SuitCatalog.RepulsorColour);
				for (float d = 0f; d < Length; d += 24f) {
					Dust dust = Dust.NewDustPerfect(Projectile.Center + direction * d, DustID.TintableDustLighted, Main.rand.NextVector2Circular(2f, 2f), 0, colour, 1.2f);
					dust.noGravity = true;
				}
				for (int i = 0; i < 20; i++) {
					Dust.NewDustPerfect(Projectile.Center + direction * Length, DustID.Electric, Main.rand.NextVector2Circular(6f, 6f)).noGravity = true;
				}
			}
			// Only deals damage on the first few ticks; after that it's just the fading beam.
			Projectile.friendly = Projectile.timeLeft > Lifetime - 3;
			Lighting.AddLight(Projectile.Center + direction * Length * 0.5f, 0.6f, 0.8f, 1f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			float point = 0f;
			Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * Length;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, 22f, ref point);
		}

		public override bool PreDraw(ref Color lightColor) {
			Player owner = Main.player[Projectile.owner];
			SuitConfig suit = owner.GetModPlayer<IronManPlayer>().Suit;
			Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			float rotation = direction.ToRotation();
			float fade = Projectile.timeLeft / (float)Lifetime;
			Vector2 start = Projectile.Center - Main.screenPosition;

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Rectangle source = new(0, 0, 1, 1);
			Color colour = suit.Colour(SuitCatalog.RepulsorColour) with { A = 0 };
			Main.EntitySpriteDraw(pixel, start, source, colour * 0.4f * fade, rotation, new Vector2(0f, 0.5f), new Vector2(Length, 34f * fade), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(pixel, start, source, colour * fade, rotation, new Vector2(0f, 0.5f), new Vector2(Length, 16f * fade), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(pixel, start, source, Color.White with { A = 0 } * fade, rotation, new Vector2(0f, 0.5f), new Vector2(Length, 6f * fade), SpriteEffects.None, 0);

			// The gun, resting on the shoulder and pointing along the beam.
			Texture2D gun = ModContent.Request<Texture2D>("MarvelMod/Content/Weapons/Railgun").Value;
			Color tint = new(lightColor.ToVector3() * suit.Colour(SuitCatalog.WeaponColour).ToVector3() * 1.2f);
			Main.EntitySpriteDraw(gun, start - direction * 10f, null, tint, rotation + MathHelper.PiOver2, new Vector2(gun.Width / 2f, gun.Height * 0.7f), 1f,
				direction.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
			return false;
		}
	}
}
