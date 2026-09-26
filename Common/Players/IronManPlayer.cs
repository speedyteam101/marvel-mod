using MarvelMod.Common.Suits;
using MarvelMod.Common.Systems;
using MarvelMod.Common.UI;
using MarvelMod.Content.Abilities;
using MarvelMod.Content.Buffs;
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

		public SuitStats Stats => SuitStats.For(Suit, ReactorTier);

		// Flight state and the sprite frame to show.
		public bool flying;
		public SuitFrame frame = SuitFrame.Idle;
		private int walkCounter;

		// Ticks until the Unibeam can fire again.
		public int unibeamCooldown;

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
		}

		public override void LoadData(TagCompound tag) {
			if (tag.ContainsKey("suit")) {
				Suit = SuitConfig.Load(tag.GetCompound("suit"));
			}
			IList<TagCompound> slots = tag.GetList<TagCompound>("savedDesigns");
			for (int i = 0; i < DesignSlots && i < slots.Count; i++) {
				SavedDesigns[i] = slots[i].Count == 0 ? null : SuitConfig.Load(slots[i]);
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
