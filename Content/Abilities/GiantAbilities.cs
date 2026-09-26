using MarvelMod.Common.Players;
using MarvelMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod.Content.Abilities
{
	// The giant suit's nine abilities. They're only given in giant form, which also takes away the normal suit abilities.
	public abstract class GiantAbility : SuitAbility
	{
		public const int Count = 9;

		// Position in IronManPlayer.giantCooldowns.
		public abstract int Slot { get; }

		// Ticks before the ability can be used again (0 = only its use time).
		public virtual int Cooldown => 0;

		protected override bool GiantOnly => true;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 28;
			Item.height = 28;
			Item.UseSound = null;
		}

		public override bool CanUseItem(Player player) {
			return base.CanUseItem(player) && player.GetModPlayer<IronManPlayer>().giantCooldowns[Slot] <= 0;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			player.GetModPlayer<IronManPlayer>().giantCooldowns[Slot] = Cooldown;
			Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
			Use(player, source, aim, damage, knockback);
			return false;
		}

		protected abstract void Use(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim, int damage, float knockback);

		// Show the seconds left on the cooldown over the icon.
		public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			int ticks = Main.LocalPlayer.GetModPlayer<IronManPlayer>().giantCooldowns[Slot];
			if (ticks <= 0) {
				return;
			}
			string seconds = ((ticks + 59) / 60).ToString();
			Vector2 size = FontAssets.MouseText.Value.MeasureString(seconds) * 0.8f;
			Utils.DrawBorderString(spriteBatch, seconds, position - size / 2f, Color.OrangeRed, 0.8f);
		}

		protected static int Proj<T>() where T : ModProjectile => ModContent.ProjectileType<T>();
	}

	// 1. A huge punch in front of you.
	public class TitanPunch : GiantAbility
	{
		public override int Slot => 0;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.DamageType = DamageClass.Melee;
			Item.damage = 150;
			Item.knockBack = 16f;
			Item.useTime = Item.useAnimation = 30;
			Item.shoot = Proj<GiantAttack>();
			Item.shootSpeed = 1f;
		}

		protected override void Use(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim, int damage, float knockback) {
			Projectile.NewProjectile(source, player.MountedCenter, aim, Proj<GiantAttack>(), damage, knockback, player.whoAmI, (float)GiantAttackKind.Punch);
		}
	}

	// 2. Slam into the ground (in the air: dive first) for a big shockwave.
	public class GroundPound : GiantAbility
	{
		public override int Slot => 1;
		public override int Cooldown => 240;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.DamageType = DamageClass.Melee;
			Item.damage = 200;
			Item.knockBack = 12f;
			Item.useTime = Item.useAnimation = 20;
			Item.shoot = Proj<SuitExplosion>();
			Item.shootSpeed = 1f;
			Item.autoReuse = false;
		}

		protected override void Use(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim, int damage, float knockback) {
			player.GetModPlayer<IronManPlayer>().StartGroundPound(damage);
		}
	}

	// 3. An enormous repulsor blast.
	public class MegaRepulsor : GiantAbility
	{
		public override int Slot => 2;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.damage = 120;
			Item.knockBack = 10f;
			Item.useTime = Item.useAnimation = 45;
			Item.shoot = Proj<SuitShot>();
			Item.shootSpeed = 12f;
		}

		protected override void Use(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim, int damage, float knockback) {
			SoundEngine.PlaySound(SoundID.Item92, player.Center);
			Projectile.NewProjectile(source, player.MountedCenter + aim * 40f, aim * 12f, Proj<SuitShot>(), damage, knockback, player.whoAmI, (float)ShotKind.MegaRepulsor);
		}
	}

	// 4. A dozen homing missiles from the shoulders.
	public class MissileBarrage : GiantAbility
	{
		public override int Slot => 3;
		public override int Cooldown => 480;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.damage = 60;
			Item.knockBack = 4f;
			Item.useTime = Item.useAnimation = 30;
			Item.shoot = Proj<SuitShot>();
			Item.shootSpeed = 8f;
			Item.autoReuse = false;
		}

		protected override void Use(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim, int damage, float knockback) {
			SoundEngine.PlaySound(SoundID.Item61, player.Center);
			for (int i = 0; i < 12; i++) {
				Vector2 launch = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-9f, -5f) * player.gravDir);
				Vector2 shoulder = player.MountedCenter + new Vector2(Main.rand.NextFloat(-30f, 30f), -50f * player.gravDir);
				Projectile.NewProjectile(source, shoulder, launch, Proj<SuitShot>(), damage, knockback, player.whoAmI, (float)ShotKind.Missile);
			}
		}
	}

	// 5. A much wider, stronger Unibeam. Shares the Unibeam's overheat cooldown.
	public class GiantUnibeam : GiantAbility
	{
		public override int Slot => 4;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.damage = 90;
			Item.knockBack = 3f;
			Item.useTime = Item.useAnimation = 20;
			Item.channel = true;
			Item.autoReuse = false;
			Item.shoot = Proj<UnibeamBeam>();
			Item.shootSpeed = 1f;
		}

		public override bool CanUseItem(Player player) {
			return base.CanUseItem(player)
				&& player.GetModPlayer<IronManPlayer>().unibeamCooldown <= 0
				&& player.ownedProjectileCounts[Proj<UnibeamBeam>()] == 0;
		}

		protected override void Use(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim, int damage, float knockback) {
			SoundEngine.PlaySound(SoundID.Item13, player.Center);
			Projectile.NewProjectile(source, player.MountedCenter, aim, Proj<UnibeamBeam>(), damage, knockback, player.whoAmI, 0f, 1f);
		}
	}

	// 6. Clap to send a shockwave rolling along the ground.
	public class ShockwaveClap : GiantAbility
	{
		public override int Slot => 5;
		public override int Cooldown => 180;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.DamageType = DamageClass.Melee;
			Item.damage = 110;
			Item.knockBack = 9f;
			Item.useTime = Item.useAnimation = 25;
			Item.shoot = Proj<GiantAttack>();
			Item.shootSpeed = 1f;
			Item.autoReuse = false;
		}

		protected override void Use(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim, int damage, float knockback) {
			SoundEngine.PlaySound(SoundID.Item70, player.Center);
			Vector2 velocity = new Vector2(player.direction * 14f, 0f);
			Projectile.NewProjectile(source, player.Bottom + new Vector2(player.direction * 40f, -45f), velocity, Proj<GiantAttack>(), damage, knockback, player.whoAmI, (float)GiantAttackKind.Wave);
		}
	}

	// 7. Rocket towards the cursor, ramming everything on the way.
	public class RocketCharge : GiantAbility
	{
		public override int Slot => 6;
		public override int Cooldown => 300;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.DamageType = DamageClass.Melee;
			Item.damage = 130;
			Item.knockBack = 14f;
			Item.useTime = Item.useAnimation = 25;
			Item.shoot = Proj<GiantAttack>();
			Item.shootSpeed = 1f;
			Item.autoReuse = false;
		}

		protected override void Use(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim, int damage, float knockback) {
			SoundEngine.PlaySound(SoundID.Item74, player.Center);
			player.velocity = aim * 22f;
			player.immune = true;
			player.immuneTime = 30;
			player.fallStart = (int)(player.position.Y / 16f);
			Projectile.NewProjectile(source, player.MountedCenter, aim, Proj<GiantAttack>(), damage, knockback, player.whoAmI, (float)GiantAttackKind.Charge);
		}
	}

	// 8. An energy dome: 40% less damage taken and enemy projectiles bounce off, for 6 seconds.
	public class ShieldDome : GiantAbility
	{
		public override int Slot => 7;
		public override int Cooldown => 1800;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.useTime = Item.useAnimation = 20;
			Item.shoot = ProjectileID.None;
			Item.autoReuse = false;
		}

		public override bool? UseItem(Player player) {
			IronManPlayer modPlayer = player.GetModPlayer<IronManPlayer>();
			modPlayer.giantCooldowns[Slot] = Cooldown;
			modPlayer.domeTimer = 360;
			SoundEngine.PlaySound(SoundID.Item113, player.Center);
			return true;
		}

		protected override void Use(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim, int damage, float knockback) {
		}
	}

	// 9. Mark a spot; a few moments later a beam hits it from orbit.
	public class OrbitalStrike : GiantAbility
	{
		public override int Slot => 8;
		public override int Cooldown => 1200;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.damage = 350;
			Item.knockBack = 10f;
			Item.useTime = Item.useAnimation = 30;
			Item.shoot = Proj<GiantAttack>();
			Item.shootSpeed = 1f;
			Item.autoReuse = false;
		}

		protected override void Use(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim, int damage, float knockback) {
			SoundEngine.PlaySound(SoundID.Item8, player.Center);
			Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, Proj<GiantAttack>(), damage, knockback, player.whoAmI, (float)GiantAttackKind.Orbital);
		}
	}
}
