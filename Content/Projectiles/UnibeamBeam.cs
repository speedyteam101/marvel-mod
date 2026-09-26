using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using MarvelMod.Content.Abilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Projectiles
{
	// The chest beam. Follows the cursor while the Unibeam (or the giant suit's Giant Unibeam) is held, until it overheats.
	// velocity is only used as the aim direction; the beam itself doesn't move.
	public class UnibeamBeam : ModProjectile
	{
		private const float MaxLength = 1100f;

		public override string Texture => "MarvelMod/Content/Projectiles/SuitShot";

		private float Timer {
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		private float Length {
			get => Projectile.localAI[0];
			set => Projectile.localAI[0] = value;
		}

		private Player Owner => Main.player[Projectile.owner];

		private SuitConfig Suit => Owner.GetModPlayer<IronManPlayer>().Suit;

		private string Mode => Suit.OptionName(SuitCatalog.UnibeamMode);

		public static float DamageMultiplier(SuitConfig suit) => suit.OptionName(SuitCatalog.UnibeamMode) switch {
			"Wide" => 0.6f,
			"Pulse" => 1.4f,
			"Overcharge" => 2.2f,
			_ => 1f
		};

		private int MaxDuration => Mode switch { "Pulse" => 240, "Overcharge" => 90, _ => 180 };

		private int Cooldown => Mode switch { "Pulse" => 300, "Overcharge" => 600, _ => 360 };

		// ai[1] = 1: fired by the giant suit, three times as wide.
		private bool Giant => Projectile.ai[1] == 1f;

		private float Width => (Giant ? 3f : 1f) * Mode switch { "Wide" => 26f, "Overcharge" => 18f, "Pulse" => 12f, _ => 10f };

		// Pulse mode is only on for half of every 20 ticks.
		private bool Firing => Mode != "Pulse" || Timer % 20 < 10;

		public override void SetDefaults() {
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 2;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI() {
			Player owner = Owner;
			bool allowed = owner.HeldItem.ModItem is SuitAbility ability && ability is (Unibeam or GiantUnibeam) && ability.IsAllowed(owner);
			if (!owner.active || owner.dead || !allowed || Timer >= MaxDuration
				|| (Projectile.owner == Main.myPlayer && !owner.channel)) {
				Projectile.Kill();
				return;
			}

			Timer++;
			Projectile.timeLeft = 2;
			Projectile.Center = owner.MountedCenter + new Vector2(0f, -4f * owner.gravDir);

			if (Projectile.owner == Main.myPlayer) {
				Vector2 aim = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);
				if (Vector2.Dot(aim, Projectile.velocity) < 0.999f) {
					Projectile.netUpdate = true;
				}
				Projectile.velocity = aim;
			}
			Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);

			owner.ChangeDir(Projectile.velocity.X >= 0f ? 1 : -1);
			owner.heldProj = Projectile.whoAmI;
			owner.itemTime = 2;
			owner.itemAnimation = 2;

			// The beam stops at the first solid tile.
			float length = 0f;
			while (length < MaxLength) {
				Vector2 point = Projectile.Center + Projectile.velocity * length;
				if (WorldGen.SolidTile((int)(point.X / 16f), (int)(point.Y / 16f))) {
					break;
				}
				length += 8f;
			}
			Length = length;

			Color colour = Suit.Colour(SuitCatalog.ReactorColour);
			Vector2 end = Projectile.Center + Projectile.velocity * Length;
			if (Firing) {
				for (float d = 0f; d < Length; d += 64f) {
					Lighting.AddLight(Projectile.Center + Projectile.velocity * d, colour.ToVector3() * 0.6f);
				}
				Dust dust = Dust.NewDustPerfect(end, DustID.TintableDustLighted, Main.rand.NextVector2Circular(3f, 3f), 0, colour, 1.2f);
				dust.noGravity = true;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (!Firing) {
				return false;
			}
			float collisionPoint = 0f;
			Vector2 end = Projectile.Center + Projectile.velocity * Length;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, Width, ref collisionPoint);
		}

		public override void OnKill(int timeLeft) {
			Owner.GetModPlayer<IronManPlayer>().unibeamCooldown = Cooldown;
		}

		public override bool PreDraw(ref Color lightColor) {
			if (Length <= 0f) {
				return false;
			}
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Rectangle source = new(0, 0, 1, 1);
			Vector2 start = Projectile.Center - Main.screenPosition;
			float rotation = Projectile.velocity.ToRotation();
			float strength = Firing ? 1f : 0.25f;
			float flicker = 0.9f + 0.1f * (float)System.Math.Sin(Timer * 0.8f);
			Color colour = Suit.Colour(SuitCatalog.ReactorColour) with { A = 0 };

			// Outer glow, then the colour band, then a white-hot core. A = 0 draws additively.
			Main.EntitySpriteDraw(pixel, start, source, colour * 0.35f * strength, rotation, new Vector2(0f, 0.5f),
				new Vector2(Length, Width * 1.8f * flicker), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(pixel, start, source, colour * strength, rotation, new Vector2(0f, 0.5f),
				new Vector2(Length, Width * flicker), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(pixel, start, source, Color.White with { A = 0 } * strength, rotation, new Vector2(0f, 0.5f),
				new Vector2(Length, Width * 0.35f), SpriteEffects.None, 0);

			Texture2D orb = ModContent.Request<Texture2D>(Texture).Value;
			Main.EntitySpriteDraw(orb, start, null, colour * strength, 0f, orb.Size() / 2f, Width / 8f + 0.6f, SpriteEffects.None, 0);
			return false;
		}
	}
}
