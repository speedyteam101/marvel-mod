using MarvelMod.Common.Suits;
using MarvelMod.Common.Systems;
using MarvelMod.Common.UI;
using MarvelMod.Content.Abilities;
using MarvelMod.Content.Buffs;
using MarvelMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MarvelMod.Common.Players
{
	// Everything about being Iron Man: the suit design, suiting up, flight, stats, the sprite and the ability items.
	public class IronManPlayer : ModPlayer
	{
		public const int DesignSlots = 5;

		// The suit design, edited in the Suit Workshop. Synced to other players.
		public SuitConfig Suit = SuitPresets.Classic();

		// Designs saved in the workshop's slots (null = empty slot).
		public SuitConfig[] SavedDesigns = new SuitConfig[DesignSlots];

		// Highest Arc Reactor equipped this frame (set by the accessories), and the value from the last full update.
		public int reactorTierThisFrame;
		public int ReactorTier { get; private set; }

		public bool SuitActive => Player.HasBuff(ModContent.BuffType<IronManSuit>()) && ReactorTier > 0;

		public SuitStats Stats {
			get {
				SuitStats stats = SuitStats.For(Suit, ReactorTier);
				if (adminSuperFlight) {
					stats.FlightSpeed *= 2f;
					stats.FlightAcceleration *= 2f;
				}
				return stats;
			}
		}

		// Flight state and the sprite frame to show.
		public bool flying;
		public SuitFrame frame = SuitFrame.Idle;
		private int walkCounter;

		// Ticks until the Unibeam can fire again.
		public int unibeamCooldown;

		// Admin panel cheats (not saved).
		public bool adminGodMode;
		public bool adminSuperFlight;
		public bool adminNoCooldowns;

		// Attached weapons: which way the next melee swing goes, and ticks left of Riot Shield blocking.
		public int swingSide = 1;
		public int shieldTimer;

		// Parts bought in the Parts Store, by ShopItem.Key.
		public HashSet<string> OwnedParts = new();

		private int lifeStealTimer;

		public bool Owns(SuitCategory category, int index) {
			ShopItem item = SuitShop.Get(category, index);
			return item == null || item.Free || OwnedParts.Contains(item.Key);
		}

		// Parts of this design the player doesn't own yet.
		public List<ShopItem> MissingParts(SuitConfig design) {
			var missing = new List<ShopItem>();
			foreach (SuitCategory category in SuitCatalog.All) {
				if (!Owns(category, design[category])) {
					missing.Add(SuitShop.Get(category, design[category]));
				}
			}
			return missing;
		}

		// Buys one part. Returns a message for the store to show.
		public string Buy(ShopItem item) {
			if (item.Free || OwnedParts.Contains(item.Key)) {
				return $"You already own {item.Name}.";
			}
			if (!ShopRequirements.Met(item.Requirement)) {
				return $"Not for sale yet: {ShopRequirements.Text(item.Requirement)}.";
			}
			if (!Player.BuyItem(item.Price)) {
				return "You can't afford that.";
			}
			OwnedParts.Add(item.Key);
			SoundEngine.PlaySound(SoundID.Coins);
			return $"Bought {item.Name}!";
		}

		// Price of the pieces of a set the player is still missing, with the whole-set discount.
		public long SetPrice(SuitSet set) {
			int missing = 0;
			foreach (var (category, option) in set.Pieces()) {
				if (!Owns(category, category.IndexOf(option))) {
					missing++;
				}
			}
			return set.PricePerPiece * missing * 85 / 100;
		}

		public int SetPiecesOwned(SuitSet set) {
			int owned = 0;
			foreach (var (category, option) in set.Pieces()) {
				if (Owns(category, category.IndexOf(option))) {
					owned++;
				}
			}
			return owned;
		}

		// Buys every missing piece of a set at once, 15% cheaper.
		public string BuySet(SuitSet set) {
			long price = SetPrice(set);
			if (price == 0) {
				return $"You already own the whole {set.Name} set.";
			}
			if (!ShopRequirements.Met(set.Requirement)) {
				return $"Not for sale yet: {ShopRequirements.Text(set.Requirement)}.";
			}
			if (!Player.BuyItem(price)) {
				return "You can't afford that.";
			}
			foreach (var (category, option) in set.Pieces()) {
				OwnedParts.Add(SuitShop.Get(category, category.IndexOf(option)).Key);
			}
			SoundEngine.PlaySound(SoundID.Coins);
			return $"Bought the {set.Name} set!";
		}

		public override void ResetEffects() {
			reactorTierThisFrame = 0;
		}

		public void ToggleSuit() {
			int buff = ModContent.BuffType<IronManSuit>();
			if (Player.HasBuff(buff)) {
				Player.ClearBuff(buff);
				SoundEngine.PlaySound(SoundID.Item37, Player.Center);
			}
			else if (ReactorTier > 0) {
				Player.AddBuff(buff, 2);
				SoundEngine.PlaySound(SoundID.Item113, Player.Center);
				for (int i = 0; i < 30; i++) {
					Dust dust = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.Electric);
					dust.noGravity = true;
					dust.velocity *= 1.5f;
				}
			}
			else {
				Main.NewText("Equip an Arc Reactor to suit up.", Color.Orange);
			}
		}

		public override void ProcessTriggers(TriggersSet triggersSet) {
			if (IronManKeybinds.SuitUp.JustPressed) {
				ToggleSuit();
			}
			if (IronManKeybinds.Workshop.JustPressed) {
				SuitWorkshopSystem.Toggle();
			}
			if (IronManKeybinds.Store.JustPressed) {
				SuitWorkshopSystem.ToggleStore();
			}
		}

		public override void PostUpdateEquips() {
			ReactorTier = reactorTierThisFrame;
			int buff = ModContent.BuffType<IronManSuit>();
			if (ReactorTier == 0 && Player.HasBuff(buff)) {
				Player.ClearBuff(buff);
			}
			if (!SuitActive) {
				return;
			}

			Stats.Apply(Player);
			Player.noFallDmg = true;
			Player.gills = true; // sealed helmet
			if (ReactorTier >= 3) {
				Player.lavaImmune = true;
				Player.fireWalk = true;
			}
			ApplySetEffects(SuitSets.WornBy(Suit));

			if (shieldTimer > 0) {
				// Blocking with the Riot Shield.
				Player.statDefense += 20;
				Player.endurance += 0.35f;
				Player.noKnockback = true;
			}
		}

		private static readonly int[] GodlyImmunities = {
			BuffID.Poisoned, BuffID.Venom, BuffID.OnFire, BuffID.OnFire3, BuffID.Burning, BuffID.Bleeding, BuffID.Confused,
			BuffID.Slow, BuffID.Weak, BuffID.BrokenArmor, BuffID.Silenced, BuffID.Cursed, BuffID.Darkness, BuffID.Blackout,
			BuffID.Chilled, BuffID.Frozen, BuffID.Ichor, BuffID.CursedInferno, BuffID.Frostburn, BuffID.Frostburn2,
			BuffID.Electrified, BuffID.Obstructed, BuffID.Stoned, BuffID.Webbed, BuffID.WitheredArmor, BuffID.WitheredWeapon
		};

		// Set bonus effects that aren't plain stats (those are in SuitStats).
		private void ApplySetEffects(SuitSet set) {
			if (set == SuitSets.Vampiric) {
				Player.buffImmune[BuffID.Bleeding] = true;
			}
			else if (set == SuitSets.Infernal) {
				Player.buffImmune[BuffID.OnFire] = true;
				Player.buffImmune[BuffID.OnFire3] = true;
				Player.buffImmune[BuffID.Burning] = true;
				Player.lavaImmune = true;
				Player.fireWalk = true;
			}
			else if (set == SuitSets.Titan) {
				Player.thorns += 0.5f;
			}
			else if (set == SuitSets.Cryo) {
				Player.buffImmune[BuffID.Chilled] = true;
				Player.buffImmune[BuffID.Frozen] = true;
				Player.buffImmune[BuffID.Frostburn] = true;
				Player.buffImmune[BuffID.Frostburn2] = true;
			}
			else if (set == SuitSets.Void) {
				Player.buffImmune[BuffID.Darkness] = true;
				Player.buffImmune[BuffID.Blackout] = true;
				Player.buffImmune[BuffID.Obstructed] = true;
			}
			else if (set == SuitSets.Dragon) {
				Player.buffImmune[BuffID.OnFire] = true;
				Player.buffImmune[BuffID.OnFire3] = true;
				Player.buffImmune[BuffID.Burning] = true;
				Player.lavaImmune = true;
				Player.fireWalk = true;
			}
			else if (set == SuitSets.Shinobi) {
				Player.aggro -= 400;
			}
			else if (set == SuitSets.Pharaoh) {
				Player.buffImmune[BuffID.Poisoned] = true;
				Player.buffImmune[BuffID.Venom] = true;
				Player.buffImmune[BuffID.Slow] = true;
				Player.buffImmune[BuffID.Weak] = true;
			}
			else if (set == SuitSets.Godly) {
				foreach (int buff in GodlyImmunities) {
					Player.buffImmune[buff] = true;
				}
				Lighting.AddLight(Player.Top, 0.9f, 0.8f, 0.4f);
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			SetOnHit(target, damageDone, false);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			SetOnHit(target, damageDone, proj.type == ModContent.ProjectileType<SuitExplosion>());
		}

		// On-hit set bonuses. fromExplosion stops holy light from triggering more holy light.
		private void SetOnHit(NPC target, int damageDone, bool fromExplosion) {
			if (!SuitActive || Player.whoAmI != Main.myPlayer) {
				return;
			}
			SuitSet set = SuitSets.WornBy(Suit);
			if (set == SuitSets.Vampiric) {
				DrinkBlood(target, damageDone);
			}
			else if (set == SuitSets.Infernal) {
				target.AddBuff(BuffID.OnFire3, 240);
			}
			else if (set == SuitSets.Cryo) {
				target.AddBuff(BuffID.Frostburn2, 240);
			}
			else if (set == SuitSets.Storm && Main.rand.NextFloat() < 0.25f) {
				ChainLightning(target, damageDone);
			}
			else if (set == SuitSets.Dragon) {
				target.AddBuff(BuffID.Daybreak, 180);
				if (Main.rand.NextFloat() < 0.15f) {
					BreatheFire(target, damageDone);
				}
			}
			else if (set == SuitSets.Pharaoh) {
				target.AddBuff(BuffID.Venom, 240);
				target.AddBuff(BuffID.Ichor, 240);
			}
			else if (set == SuitSets.Cyber && !fromExplosion && ++cyberHits >= 5) {
				cyberHits = 0;
				Vector2 launch = new Vector2(Player.direction * 3f, -6f * Player.gravDir);
				Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, launch, ModContent.ProjectileType<SuitShot>(),
					System.Math.Max(1, damageDone * 3 / 5), 3f, Player.whoAmI, (float)ShotKind.Missile);
			}
			else if (set == SuitSets.Godly && !fromExplosion && Main.rand.NextFloat() < 0.1f) {
				Projectile.NewProjectile(Player.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<SuitExplosion>(),
					System.Math.Max(1, damageDone), 4f, Player.whoAmI, 100f, 1f);
			}
		}

		private int cyberHits;

		// Dragon set: a short burst of flame from the helmet towards the target.
		private void BreatheFire(NPC target, int damageDone) {
			Vector2 mouth = Player.Center + new Vector2(Player.direction * 8f, -14f * Player.gravDir);
			Vector2 aim = (target.Center - mouth).SafeNormalize(Vector2.UnitX * Player.direction);
			SoundEngine.PlaySound(SoundID.Item34, mouth);
			for (int i = 0; i < 4; i++) {
				Projectile.NewProjectile(Player.GetSource_FromThis(), mouth, aim.RotatedByRandom(0.2f) * 10f, ModContent.ProjectileType<SuitShot>(),
					System.Math.Max(1, damageDone / 4), 0.5f, Player.whoAmI, (float)ShotKind.Flame);
			}
		}

		private void DrinkBlood(NPC target, int damageDone) {
			if (lifeStealTimer > 0 || target.immortal || target.lifeMax <= 5 || target.friendly || Player.statLife >= Player.statLifeMax2) {
				return;
			}
			int heal = System.Math.Clamp(damageDone * 8 / 100, 1, 12);
			heal = System.Math.Min(heal, Player.statLifeMax2 - Player.statLife);
			Player.statLife += heal;
			Player.HealEffect(heal);
			lifeStealTimer = 6;
			for (int i = 0; i < 4; i++) {
				Dust.NewDust(target.position, target.width, target.height, DustID.Blood);
			}
		}

		// Strikes the closest other enemy for half the damage.
		private void ChainLightning(NPC source, int damageDone) {
			NPC best = null;
			float bestDistance = 320f;
			foreach (NPC npc in Main.ActiveNPCs) {
				if (npc.whoAmI == source.whoAmI || !npc.CanBeChasedBy()) {
					continue;
				}
				float distance = Vector2.Distance(npc.Center, source.Center);
				if (distance < bestDistance) {
					best = npc;
					bestDistance = distance;
				}
			}
			if (best == null) {
				return;
			}
			best.SimpleStrikeNPC(System.Math.Max(1, damageDone / 2), best.Center.X > source.Center.X ? 1 : -1, false, 0f, DamageClass.Ranged);
			for (float t = 0f; t <= 1f; t += 0.08f) {
				Vector2 point = Vector2.Lerp(source.Center, best.Center, t) + Main.rand.NextVector2Circular(6f, 6f);
				Dust dust = Dust.NewDustPerfect(point, DustID.Electric, Vector2.Zero, 0, default, 0.8f);
				dust.noGravity = true;
			}
			SoundEngine.PlaySound(SoundID.Item94, best.Center);
		}

		// Admin god mode: nothing can hurt you.
		public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) {
			return adminGodMode;
		}

		// Void and Shinobi sets: sometimes attacks pass straight through.
		public override bool FreeDodge(Player.HurtInfo info) {
			if (!SuitActive) {
				return false;
			}
			SuitSet set = SuitSets.WornBy(Suit);
			float chance = set == SuitSets.Void ? 0.12f : set == SuitSets.Shinobi ? 0.08f : 0f;
			if (Main.rand.NextFloat() >= chance) {
				return false;
			}
			Player.SetImmuneTimeForAllTypes(Player.longInvince ? 90 : 60);
			for (int i = 0; i < 20; i++) {
				Dust dust = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.Shadowflame);
				dust.noGravity = true;
				dust.velocity *= 2f;
			}
			return true;
		}

		public override void UpdateLifeRegen() {
			if (SuitActive) {
				Player.lifeRegen += Stats.LifeRegen * 2; // lifeRegen is in half-health per second
			}
		}

		// Flight, part 1: horizontal speed and gravity. Runs before vanilla applies gravity.
		public override void PostUpdateRunSpeeds() {
			if (!SuitActive || !flying) {
				return;
			}
			SuitStats stats = Stats;
			Player.maxRunSpeed = Math.Max(Player.maxRunSpeed, stats.FlightSpeed);
			Player.accRunSpeed = Math.Max(Player.accRunSpeed, stats.FlightSpeed);
			Player.runAcceleration = Math.Max(Player.runAcceleration, stats.FlightAcceleration);
			if (Player.controlJump || Player.controlDown || stats.Hover) {
				Player.gravity = 0f;
			}
			Player.maxFallSpeed = Math.Max(Player.maxFallSpeed, stats.FlightSpeed);
		}

		// Flight, part 2: hold Jump in the air to fly up, hold Down to dive. Runs after gravity, before the player moves.
		public override void PreUpdateMovement() {
			// Standing on something (hovering in mid-air can also leave velocity.Y at exactly 0).
			Vector2 below = Player.gravDir > 0f ? Player.BottomLeft : Player.TopLeft - new Vector2(0f, 2f);
			bool grounded = Player.velocity.Y == 0f && Collision.SolidCollision(below, Player.width, 2, true);
			if (!SuitActive || Player.mount.Active || Player.grappling[0] >= 0 || Player.pulley || Player.dead) {
				flying = false;
				return;
			}
			if (grounded && !Player.controlJump) {
				flying = false;
			}
			if (!flying && !grounded && Player.controlJump && Player.jump == 0) {
				flying = true;
			}
			if (!flying) {
				return;
			}

			SuitStats stats = Stats;
			float up = -Player.gravDir;
			float vy = Player.velocity.Y * up; // positive = moving "up" for the current gravity
			if (Player.controlJump) {
				vy = Math.Min(vy + stats.FlightAcceleration * 2f, stats.FlightSpeed * 0.75f);
			}
			else if (Player.controlDown) {
				vy = Math.Max(vy - stats.FlightAcceleration * 2f, -stats.FlightSpeed);
			}
			else if (stats.Hover) {
				vy *= 0.8f;
			}
			Player.velocity.Y = vy * up;
			Player.fallStart = (int)(Player.position.Y / 16f);
		}

		public override void PostUpdate() {
			if (unibeamCooldown > 0) {
				unibeamCooldown--;
			}
			if (adminNoCooldowns) {
				unibeamCooldown = 0;
			}
			if (lifeStealTimer > 0) {
				lifeStealTimer--;
			}
			if (shieldTimer > 0) {
				shieldTimer--;
			}

			UpdateFrame();

			if (SuitActive) {
				SuitEffects();
			}

			if (Player.whoAmI == Main.myPlayer) {
				UpdateAbilityItems();
			}
		}

		private void UpdateFrame() {
			bool aiming = Player.itemAnimation > 0 && Player.HeldItem.useStyle == ItemUseStyleID.Shoot;
			bool airborne = Player.velocity.Y != 0f;

			if (flying) {
				frame = aiming ? SuitFrame.FlyAim : SuitFrame.Fly;
			}
			else if (aiming) {
				frame = SuitFrame.Aim;
			}
			else if (airborne) {
				frame = SuitFrame.Jump;
			}
			else if (Math.Abs(Player.velocity.X) > 0.2f) {
				// Faster running steps the walk cycle faster.
				walkCounter += (int)Math.Clamp(Math.Abs(Player.velocity.X), 1f, 6f);
				frame = SuitFrame.Walk1 + walkCounter / 24 % 4;
			}
			else {
				walkCounter = 0;
				frame = SuitFrame.Idle;
			}
		}

		// Glow brightness from the Glow Strength and Glow Pulse options, 0-1.
		public float GlowIntensity() {
			float strength = 0.4f + 0.15f * Suit[SuitCatalog.GlowStrength];
			float t = (float)Main.timeForVisualEffects / 60f;
			float pulse = Suit.OptionName(SuitCatalog.GlowPulse) switch {
				"Slow Pulse" => 0.75f + 0.25f * MathF.Sin(t * 2f),
				"Fast Pulse" => 0.7f + 0.3f * MathF.Sin(t * 8f),
				"Flicker" => (int)(t * 12f) * 7919 % 10 < 2 ? 0.45f : 1f,
				_ => 1f
			};
			return Math.Clamp(strength * pulse, 0f, 1f);
		}

		// Eye and reactor light, and thruster exhaust while flying.
		private void SuitEffects() {
			float glow = GlowIntensity();
			Lighting.AddLight(Player.Center + new Vector2(0f, -14f * Player.gravDir), Suit.Colour(SuitCatalog.EyeColour).ToVector3() * 0.5f * glow);
			Lighting.AddLight(Player.Center, Suit.Colour(SuitCatalog.ReactorColour).ToVector3() * 0.6f * glow);

			bool thrusting = flying && (Player.controlJump || Player.controlLeft || Player.controlRight || Stats.Hover);
			if (!thrusting || Main.dedServ) {
				return;
			}

			Color colour = Suit.Colour(SuitCatalog.ThrusterColour);
			Vector2 feet = Player.Bottom;
			if (Player.gravDir < 0f) {
				feet = Player.Top;
			}
			Lighting.AddLight(feet, colour.ToVector3() * 0.5f);

			string trail = Suit.OptionName(SuitCatalog.ThrusterTrail);
			if (trail == "None") {
				return;
			}
			foreach (float side in new[] { -5f, 5f }) {
				Vector2 position = feet + new Vector2(side, 0f);
				Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(1.5f, 3.5f) * Player.gravDir);
				Dust dust = trail switch {
					"Flame" => Dust.NewDustPerfect(position, DustID.Torch, velocity, 0, default, 1.4f),
					"Sparks" => Dust.NewDustPerfect(position, DustID.Electric, velocity * 1.5f, 0, colour, 0.6f),
					"Smoke" => Dust.NewDustPerfect(position, DustID.Smoke, velocity * 0.5f, 100, colour, 1.2f),
					"Rainbow" => Dust.NewDustPerfect(position, DustID.RainbowMk2, velocity, 0,
						Main.hslToRgb((float)Main.timeForVisualEffects / 90f % 1f, 1f, 0.6f), 0.9f),
					"Ion" => Dust.NewDustPerfect(position, DustID.TintableDustLighted, velocity * 0.6f, 0, colour, 0.8f),
					"Electric" => Dust.NewDustPerfect(position, DustID.Electric, velocity, 0, colour, 0.8f),
					"Blood" => Dust.NewDustPerfect(position, DustID.Blood, velocity, 0, default, 1.3f),
					"Holy Light" => Dust.NewDustPerfect(position, DustID.GoldFlame, velocity, 0, default, 1.4f),
					"Frost" => Dust.NewDustPerfect(position, DustID.IceTorch, velocity, 0, default, 1.4f),
					"Void" => Dust.NewDustPerfect(position, DustID.Shadowflame, velocity, 0, default, 1.3f),
					_ => Dust.NewDustPerfect(position, DustID.TintableDustLighted, velocity, 0, colour, 1.3f) // Plasma
				};
				dust.noGravity = true;
			}
		}

		// While suited up, the player is given the ability items their reactor tier unlocks, and they are taken away afterwards.
		private void UpdateAbilityItems() {
			for (int i = 0; i < Player.inventory.Length; i++) {
				if (Player.inventory[i].ModItem is SuitAbility ability && !ability.IsAllowed(Player)) {
					Player.inventory[i].TurnToAir();
				}
			}
			if (Main.mouseItem.ModItem is SuitAbility held && !held.IsAllowed(Player)) {
				Main.mouseItem.TurnToAir();
			}
			if (!SuitActive) {
				return;
			}

			foreach (SuitAbility ability in SuitAbility.All) {
				int type = ability.Type;
				if (!ability.IsAllowed(Player) || Player.HasItem(type) || Main.mouseItem.type == type) {
					continue;
				}
				// First empty slot of the main inventory, hotbar first. Coin and ammo slots start at 50.
				for (int i = 0; i < 50; i++) {
					if (Player.inventory[i].IsAir) {
						Player.inventory[i].SetDefaults(type);
						break;
					}
				}
			}
		}

		// While suited up, only the suit sprite is drawn in place of the player's body.
		public override void HideDrawLayers(PlayerDrawSet drawInfo) {
			if (!SuitActive || Player.dead) {
				return;
			}
			PlayerDrawLayer suitLayer = ModContent.GetInstance<SuitDrawLayer>();
			foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.DrawOrder) {
				if (layer == suitLayer
					|| layer == PlayerDrawLayers.HeldItem
					|| layer == PlayerDrawLayers.ProjectileOverArm
					|| layer == PlayerDrawLayers.MountBack
					|| layer == PlayerDrawLayers.MountFront
					|| layer == PlayerDrawLayers.FrozenOrWebbedDebuff
					|| layer == PlayerDrawLayers.WebbedDebuffBack
					|| layer == PlayerDrawLayers.ElectrifiedDebuffBack
					|| layer == PlayerDrawLayers.ElectrifiedDebuffFront
					|| layer == PlayerDrawLayers.IceBarrier) {
					continue;
				}
				layer.Hide();
			}
		}

		public override void SaveData(TagCompound tag) {
			tag["suit"] = Suit.Save();
			var slots = new List<TagCompound>();
			for (int i = 0; i < DesignSlots; i++) {
				slots.Add(SavedDesigns[i]?.Save() ?? new TagCompound());
			}
			tag["savedDesigns"] = slots;
			tag["ownedParts"] = new List<string>(OwnedParts);
		}

		public override void LoadData(TagCompound tag) {
			if (tag.ContainsKey("suit")) {
				Suit = SuitConfig.Load(tag.GetCompound("suit"));
			}
			IList<TagCompound> slots = tag.GetList<TagCompound>("savedDesigns");
			for (int i = 0; i < DesignSlots && i < slots.Count; i++) {
				SavedDesigns[i] = slots[i].Count == 0 ? null : SuitConfig.Load(slots[i]);
			}

			if (tag.ContainsKey("ownedParts")) {
				OwnedParts = new HashSet<string>(tag.GetList<string>("ownedParts"));
			}
			else {
				// Characters from before the Parts Store keep every part they were already using.
				GrantParts(Suit);
				foreach (SuitConfig design in SavedDesigns) {
					if (design != null) {
						GrantParts(design);
					}
				}
			}
		}

		private void GrantParts(SuitConfig design) {
			foreach (SuitCategory category in SuitCatalog.All) {
				ShopItem item = SuitShop.Get(category, design[category]);
				if (item != null && !item.Free) {
					OwnedParts.Add(item.Key);
				}
			}
		}

		// Multiplayer: other players need our design to draw our suit and colour our projectiles.
		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
			MarvelMod.SendSuit(Player, toWho, fromWho);
		}

		public override void CopyClientState(ModPlayer targetCopy) {
			Suit.CopyTo(((IronManPlayer)targetCopy).Suit);
		}

		public override void SendClientChanges(ModPlayer clientPlayer) {
			if (!Suit.SameAs(((IronManPlayer)clientPlayer).Suit)) {
				MarvelMod.SendSuit(Player, -1, -1);
			}
		}
	}
}
