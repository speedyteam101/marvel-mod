namespace MarvelMod.Common.Suits
{
	// How an attached weapon attacks.
	public enum WeaponKind
	{
		None,
		Swing,   // melee arc in front of you
		Spin,    // full circle around you
		Thrust,  // stab straight out and back
		Whip,    // long energy lash
		Shield,  // hold to block
		Spear,   // thrown, falls with gravity
		Chakram, // thrown, comes back
		Buzzsaw, // thrown, bounces off walls
		Rail,    // instant piercing beam
		Flame,   // short-range fire stream
		Plasma,  // slow orb, big explosion
		Grenade, // bouncing explosive
		Arc      // chain lightning
	}

	// How the weapon looks when stowed on the suit (see SuitRenderer.DrawStowedWeapon).
	public enum StowedShape
	{
		None,
		Blade,
		Katana,
		Axe,
		Scythe,
		Hammer,
		Spear,
		Shield,
		LongBarrel,
		Barrel,
		Disc,
		WristBlade,
		Coil
	}

	public class WeaponDef
	{
		public string Name { get; init; }
		public WeaponKind Kind { get; init; }
		public string Description { get; init; }
		public int Damage { get; init; }
		public int UseTime { get; init; }
		public bool Melee { get; init; }
		public float Knockback { get; init; } = 5f;

		// Reach in pixels for melee weapons; launch speed for thrown and fired ones.
		public float Reach { get; init; }
		public float Speed { get; init; }

		// Swing arc in degrees.
		public float Arc { get; init; } = 140f;

		// Glowing energy weapons are drawn brightly in the weapon colour.
		public bool Energy { get; init; }

		// Texture in Content/Weapons, drawn while the weapon is used. null = drawn in code.
		public string Texture { get; init; }

		public StowedShape Stowed { get; init; }
		public long Price { get; init; }
		public ShopRequirement Requirement { get; init; }
	}

	// Weapons that can be attached to the suit's two weapon slots. Index 0 is an empty slot.
	public static class SuitWeapons
	{
		private const long Gold = 10000;

		public static readonly WeaponDef[] All = {
			new() { Name = "None", Kind = WeaponKind.None },
			new() {
				Name = "Energy Sword", Kind = WeaponKind.Swing, Melee = true, Energy = true, Damage = 48, UseTime = 20, Reach = 56, Arc = 150,
				Texture = "EnergySword", Stowed = StowedShape.Blade, Price = 5 * Gold,
				Description = "A glowing blade that swings in a wide arc."
			},
			new() {
				Name = "Riot Shield", Kind = WeaponKind.Shield, Melee = true, Damage = 30, UseTime = 20, Knockback = 9f,
				Texture = "RiotShield", Stowed = StowedShape.Shield, Price = 5 * Gold,
				Description = "Hold to block: +20 defense, 35% less damage, no knockback. Destroys enemy projectiles that hit it and bashes enemies it touches."
			},
			new() {
				Name = "Battle Axe", Kind = WeaponKind.Swing, Melee = true, Damage = 70, UseTime = 32, Reach = 60, Arc = 140, Knockback = 8f,
				Texture = "BattleAxe", Stowed = StowedShape.Axe, Price = 6 * Gold,
				Description = "Slow, heavy chops that ignore 20 enemy defense."
			},
			new() {
				Name = "Throwing Spear", Kind = WeaponKind.Spear, Damage = 55, UseTime = 26, Speed = 17f,
				Texture = "ThrowingSpear", Stowed = StowedShape.Spear, Price = 4 * Gold,
				Description = "Throw a spear that pierces up to 3 enemies. A new one deploys from the suit each throw."
			},
			new() {
				Name = "Reaper Scythe", Kind = WeaponKind.Spin, Melee = true, Damage = 60, UseTime = 30, Reach = 68, Knockback = 6f,
				Texture = "ReaperScythe", Stowed = StowedShape.Scythe, Price = 12 * Gold, Requirement = ShopRequirement.Hardmode,
				Description = "Spins all the way around you. Every hit heals you a little."
			},
			new() {
				Name = "Katana", Kind = WeaponKind.Swing, Melee = true, Damage = 40, UseTime = 12, Reach = 60, Arc = 120, Knockback = 3f,
				Texture = "Katana", Stowed = StowedShape.Katana, Price = 8 * Gold,
				Description = "Very fast slashes with +20% crit chance."
			},
			new() {
				Name = "Anti-Tank Railgun", Kind = WeaponKind.Rail, Damage = 400, UseTime = 90, Knockback = 14f,
				Texture = "Railgun", Stowed = StowedShape.LongBarrel, Price = 40 * Gold, Requirement = ShopRequirement.MechBoss,
				Description = "A massive rail gun. Fires an instant beam that pierces every enemy in its path. Huge recoil."
			},
			new() {
				Name = "Warhammer", Kind = WeaponKind.Swing, Melee = true, Damage = 90, UseTime = 40, Reach = 60, Arc = 160, Knockback = 12f,
				Texture = "Warhammer", Stowed = StowedShape.Hammer, Price = 10 * Gold, Requirement = ShopRequirement.Hardmode,
				Description = "Crushing overhead swings. The first hit of each swing sends out a shockwave."
			},
			new() {
				Name = "Energy Whip", Kind = WeaponKind.Whip, Melee = true, Energy = true, Damage = 42, UseTime = 24, Reach = 240, Knockback = 2f,
				Stowed = StowedShape.Coil, Price = 8 * Gold,
				Description = "A long energy lash with huge reach."
			},
			new() {
				Name = "Chakram", Kind = WeaponKind.Chakram, Damage = 45, UseTime = 22, Speed = 14f,
				Texture = "Chakram", Stowed = StowedShape.Disc, Price = 6 * Gold,
				Description = "A bladed disc that flies out, cuts through everything and comes back."
			},
			new() {
				Name = "Flamethrower", Kind = WeaponKind.Flame, Damage = 14, UseTime = 5, Speed = 9f, Knockback = 0.5f,
				Stowed = StowedShape.Barrel, Price = 10 * Gold, Requirement = ShopRequirement.Hardmode,
				Description = "A short-range stream of fire that sets enemies ablaze and passes through crowds."
			},
			new() {
				Name = "Plasma Cannon", Kind = WeaponKind.Plasma, Damage = 120, UseTime = 55, Speed = 8f, Knockback = 8f,
				Stowed = StowedShape.Barrel, Price = 20 * Gold, Requirement = ShopRequirement.Plantera,
				Description = "Fires a slow orb of plasma that makes a huge explosion."
			},
			new() {
				Name = "Wrist Blades", Kind = WeaponKind.Thrust, Melee = true, Damage = 34, UseTime = 8, Reach = 36, Knockback = 2f,
				Texture = "WristBlade", Stowed = StowedShape.WristBlade, Price = 4 * Gold,
				Description = "Blades that spring out of the gauntlets for rapid stabs."
			},
			new() {
				Name = "Grenade Launcher", Kind = WeaponKind.Grenade, Damage = 65, UseTime = 35, Speed = 10f, Knockback = 6f,
				Stowed = StowedShape.Barrel, Price = 6 * Gold,
				Description = "Lobs grenades that bounce twice, then explode."
			},
			new() {
				Name = "Energy Lance", Kind = WeaponKind.Thrust, Melee = true, Energy = true, Damage = 75, UseTime = 36, Reach = 84, Knockback = 10f,
				Texture = "EnergyLance", Stowed = StowedShape.Spear, Price = 15 * Gold, Requirement = ShopRequirement.MechBoss,
				Description = "Charge forward and skewer enemies with a long energy lance."
			},
			new() {
				Name = "Arc Caster", Kind = WeaponKind.Arc, Damage = 50, UseTime = 24, Knockback = 1f,
				Stowed = StowedShape.Coil, Price = 18 * Gold, Requirement = ShopRequirement.Plantera,
				Description = "Lightning strikes the enemy nearest your cursor and chains to 3 more."
			},
			new() {
				Name = "Buzzsaw Launcher", Kind = WeaponKind.Buzzsaw, Damage = 40, UseTime = 20, Speed = 13f, Knockback = 3f,
				Texture = "Buzzsaw", Stowed = StowedShape.Disc, Price = 10 * Gold, Requirement = ShopRequirement.Hardmode,
				Description = "Fires saw blades that ricochet off walls up to 4 times."
			},
		};

		public static string[] Names() {
			var names = new string[All.Length];
			for (int i = 0; i < All.Length; i++) {
				names[i] = All[i].Name;
			}
			return names;
		}
	}
}
