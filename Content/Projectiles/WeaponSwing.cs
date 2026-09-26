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
	// A melee attached weapon in use: swing, spin, thrust, whip or shield. Follows the owner.
	// ai[0] = weapon index (SuitWeapons.All), ai[1] = swing direction (+1 or -1). velocity is the aim direction.
	public class WeaponSwing : ModProjectile
	{
		public override string Texture => "MarvelMod/Content/Projectiles/SuitShot";

		private WeaponDef Weapon => SuitWeapons.All[Math.Clamp((int)Projectile.ai[0], 0, SuitWeapons.All.Length - 1)];

		private Player Owner => Main.player[Projectile.owner];

		private float Timer {
			get => Projectile.localAI[0];
			set => Projectile.localAI[0] = value;
		}

		private float Duration {
			get => Projectile.localAI[1];
			set => Projectile.localAI[1] = value;
		}

		// Set each tick: where the weapon points and how far its tip reaches.
		private float angle;
		private float reach;
		private bool shockwaveDone;

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.ownerHitCheck = true;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1; // each enemy once per swing
			Projectile.timeLeft = 600;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI() {
			Player owner = Owner;
			WeaponDef weapon = Weapon;
			if (!owner.active || owner.dead || weapon.Kind == WeaponKind.None) {
				Projectile.Kill();
				return;
			}

			if (Duration == 0f) {
				Duration = Math.Max(4, owner.itemAnimationMax);
				if (weapon.Kind is WeaponKind.Spin or WeaponKind.Shield) {
					Projectile.localNPCHitCooldown = weapon.Kind == WeaponKind.Spin ? 12 : 20;
				}
			}
			Timer++;
			Projectile.Center = owner.MountedCenter;
			owner.heldProj = Projectile.whoAmI;

			Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
			float aimAngle = aim.ToRotation();
			float progress = Math.Clamp(Timer / Duration, 0f, 1f);
			float side = Projectile.ai[1] == 0f ? 1f : Projectile.ai[1];

			switch (weapon.Kind) {
				case WeaponKind.Swing:
					// Ease in and out, facing the aim direction; the swing goes the other way each time.
					float eased = (float)(0.5 - 0.5 * Math.Cos(progress * Math.PI));
					angle = aimAngle + side * owner.direction * MathHelper.ToRadians(weapon.Arc) * (eased - 0.5f);
					reach = weapon.Reach;
					break;
				case WeaponKind.Spin:
					angle = aimAngle + side * owner.direction * MathHelper.TwoPi * progress;
					reach = weapon.Reach;
					break;
				case WeaponKind.Thrust:
					angle = aimAngle;
					reach = weapon.Reach * (float)Math.Sin(progress * Math.PI);
					if (weapon.Name == "Energy Lance" && progress < 0.5f) {
						// Charge along with the lance.
						owner.velocity = aim * 13f;
						owner.fallStart = (int)(owner.position.Y / 16f);
						owner.immune = true;
						owner.immuneTime = Math.Max(owner.immuneTime, 6);
					}
					break;
				case WeaponKind.Whip:
					angle = aimAngle;
					reach = weapon.Reach * (float)Math.Sin(progress * Math.PI);
					break;
				case WeaponKind.Shield:
					AimShield(owner);
					angle = Projectile.velocity.ToRotation();
					reach = 26f;
					break;
			}

			if (weapon.Kind != WeaponKind.Shield && Timer >= Duration) {
				Projectile.Kill();
				return;
			}
			Projectile.rotation = angle;

			if (weapon.Energy || weapon.Kind == WeaponKind.Whip) {
				Lighting.AddLight(Tip(), WeaponColour().ToVector3() * 0.6f);
			}
		}

		// Held while the use button is held. Blocks, pushes enemies and reflects projectiles.
		private void AimShield(Player owner) {
			if (Projectile.owner == Main.myPlayer) {
				if (!owner.channel || !owner.GetModPlayer<IronManPlayer>().SuitActive) {
					Projectile.Kill();
					return;
				}
				Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
				if (Vector2.Dot(aim, Projectile.velocity.SafeNormalize(Vector2.Zero)) < 0.995f) {
					Projectile.netUpdate = true;
				}
				Projectile.velocity = aim;
			}
			owner.ChangeDir(Projectile.velocity.X >= 0f ? 1 : -1);
			owner.itemTime = 2;
			owner.itemAnimation = 2;
			Projectile.timeLeft = 2;
			owner.GetModPlayer<IronManPlayer>().shieldTimer = 2;

			// Reflection is done where hostile projectiles are simulated: the server, or single player.
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}
			Rectangle shield = ShieldBox();
			foreach (Projectile other in Main.ActiveProjectiles) {
				if (other.hostile && !other.friendly && other.damage > 0 && other.Hitbox.Intersects(shield)) {
					other.velocity = -other.velocity;
					other.hostile = false;
					other.friendly = true;
					other.damage *= 2;
					other.netUpdate = true;
					SoundEngine.PlaySound(SoundID.Item150, other.Center);
				}
			}
		}

		private Rectangle ShieldBox() {
			Vector2 centre = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 26f;
			return new Rectangle((int)centre.X - 20, (int)centre.Y - 26, 40, 52);
		}

		private Vector2 Tip() => Projectile.Center + angle.ToRotationVector2() * reach;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (Weapon.Kind == WeaponKind.Shield) {
				return ShieldBox().Intersects(targetHitbox);
			}
			float point = 0f;
			float width = Weapon.Kind == WeaponKind.Whip ? 14f : 24f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Tip(), width, ref point);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			switch (Weapon.Name) {
				case "Battle Axe":
					modifiers.ArmorPenetration += 20;
					break;
				case "Katana":
					if (Main.rand.NextFloat() < 0.2f) {
						modifiers.SetCrit();
					}
					break;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			Player owner = Owner;
			switch (Weapon.Name) {
				case "Reaper Scythe":
					if (!target.immortal && target.lifeMax > 5 && owner.statLife < owner.statLifeMax2) {
						int heal = Math.Clamp(damageDone / 25, 1, 5);
						owner.statLife = Math.Min(owner.statLife + heal, owner.statLifeMax2);
						owner.HealEffect(heal);
					}
					break;
				case "Warhammer":
					if (!shockwaveDone && Projectile.owner == Main.myPlayer) {
						shockwaveDone = true;
						Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<SuitExplosion>(),
							damageDone / 2, 8f, Projectile.owner, 110f);
						SoundEngine.PlaySound(SoundID.Item14, target.Center);
					}
					break;
			}
		}

		private Color WeaponColour() => Owner.GetModPlayer<IronManPlayer>().Suit.Colour(SuitCatalog.WeaponColour);

		// Weapon textures are light grey, so drawing them with a colour tints them.
		private Color Tint(Color light) {
			Color colour = WeaponColour();
			if (Weapon.Energy) {
				return colour with { A = 0 }; // energy weapons glow
			}
			return new Color(light.ToVector3() * colour.ToVector3() * 1.2f);
		}

		public override bool PreDraw(ref Color lightColor) {
			WeaponDef weapon = Weapon;
			if (weapon.Kind == WeaponKind.Whip) {
				DrawWhip();
				return false;
			}
			if (weapon.Texture == null) {
				return false;
			}
			Texture2D texture = ModContent.Request<Texture2D>($"MarvelMod/Content/Weapons/{weapon.Texture}").Value;
			Vector2 direction = angle.ToRotationVector2();

			// Textures point straight up with the grip at the bottom; scale them to the weapon's reach.
			float length = weapon.Kind == WeaponKind.Shield ? texture.Height : weapon.Reach;
			float scale = length / texture.Height;
			Vector2 grip = weapon.Kind == WeaponKind.Thrust
				? Projectile.Center + direction * (reach - weapon.Reach + 12f)
				: Projectile.Center + direction * 6f;
			float rotation = angle + MathHelper.PiOver2;
			SpriteEffects effects = SpriteEffects.None;
			Vector2 origin = new(texture.Width / 2f, texture.Height);
			if (weapon.Kind == WeaponKind.Shield) {
				// The shield stays upright in front of you, centred on its box.
				grip = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 26f;
				rotation = 0f;
				origin = texture.Size() / 2f;
				effects = Projectile.velocity.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			}

			Main.EntitySpriteDraw(texture, grip - Main.screenPosition, null, Tint(lightColor), rotation, origin, scale, effects, 0);
			if (weapon.Energy) {
				Main.EntitySpriteDraw(texture, grip - Main.screenPosition, null, Color.White with { A = 0 } * 0.6f, rotation, origin, scale * 0.8f, effects, 0);
			}
			return false;
		}

		// A wavy line of energy from the hand to the tip.
		private void DrawWhip() {
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Rectangle source = new(0, 0, 1, 1);
			Color colour = WeaponColour() with { A = 0 };
			Vector2 start = Projectile.Center;
			Vector2 direction = angle.ToRotationVector2();
			Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
			const int Segments = 24;
			Vector2 previous = start;
			for (int i = 1; i <= Segments; i++) {
				float t = i / (float)Segments;
				float wave = (float)Math.Sin(t * 10f - Timer * 0.6f) * 10f * t * (1f - t);
				Vector2 point = start + direction * reach * t + normal * wave;
				Vector2 segment = point - previous;
				Main.EntitySpriteDraw(pixel, previous - Main.screenPosition, source, colour, segment.ToRotation(), new Vector2(0f, 0.5f),
					new Vector2(segment.Length() + 1f, 4f - 2f * t), SpriteEffects.None, 0);
				Main.EntitySpriteDraw(pixel, previous - Main.screenPosition, source, Color.White with { A = 0 } * 0.7f, segment.ToRotation(), new Vector2(0f, 0.5f),
					new Vector2(segment.Length() + 1f, 1.5f), SpriteEffects.None, 0);
				previous = point;
			}
		}
	}
}
