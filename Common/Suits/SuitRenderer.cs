using Microsoft.Xna.Framework;
using System;

namespace MarvelMod.Common.Suits
{
	public enum SuitRole : byte
	{
		None,
		Primary,
		Secondary,
		Accent,
		Trim,
		Undersuit,
		Dark,
		Pattern,
		Emblem,
		// Glowing roles: drawn unlit on the glow layer.
		Eye,
		Reactor,
		Repulsor,
		Thruster
	}

	public enum SuitFrame
	{
		Idle,
		Walk1,
		Walk2,
		Walk3,
		Walk4,
		Jump,
		Fly,
		Aim,
		FlyAim
	}

	// Turns a SuitConfig into pixel art: a sheet of SuitFrame frames side by side, as two colour arrays.
	// "body" is lit by the world; "glow" (eyes, reactor, repulsors, thrusters) is drawn on top at full brightness.
	// Pure code with no Terraria calls, so the art can also be rendered outside the game (see tools/).
	public static class SuitRenderer
	{
		// The suit is drawn on a 27 x 27 grid (feet on row 26, body centred on column 12, facing right) with a margin for the outline.
		private const int OffsetX = 2;
		private const int OffsetY = 1;
		public const int FrameWidth = 29;
		public const int FrameHeight = 29;
		public static readonly int FrameCount = Enum.GetValues<SuitFrame>().Length;
		public static int SheetWidth => FrameWidth * FrameCount;

		// Bottom centre of the feet, in frame pixels. Symmetric, so it also works when the sprite is flipped.
		public static readonly Vector2 Origin = new(FrameWidth / 2f, 26 + OffsetY + 1);

		public static Rectangle FrameRect(SuitFrame frame) => new((int)frame * FrameWidth, 0, FrameWidth, FrameHeight);

		private class Canvas
		{
			public readonly SuitRole[] Roles = new SuitRole[FrameWidth * FrameHeight];

			public SuitRole Get(int x, int y) {
				x += OffsetX;
				y += OffsetY;
				if (x < 0 || y < 0 || x >= FrameWidth || y >= FrameHeight) {
					return SuitRole.None;
				}
				return Roles[y * FrameWidth + x];
			}

			public void Set(int x, int y, SuitRole role) {
				x += OffsetX;
				y += OffsetY;
				if (x >= 0 && y >= 0 && x < FrameWidth && y < FrameHeight) {
					Roles[y * FrameWidth + x] = role;
					Far[y * FrameWidth + x] = false;
				}
			}

			public void Rect(int x0, int y0, int x1, int y1, SuitRole role) {
				for (int y = y0; y <= y1; y++) {
					for (int x = x0; x <= x1; x++) {
						Set(x, y, role);
					}
				}
			}

			// Back modules are designed for a front view (a left half and its mirror image). In the side view only the
			// left half is drawn, moved right so it sits behind the torso.
			public void BackRect(int x0, int y0, int x1, int y1, SuitRole role) {
				Rect(x0 + BackShift, y0, x1 + BackShift, y1, role);
			}

			// Pixels of limbs on the far side of the body, drawn darker.
			public readonly bool[] Far = new bool[FrameWidth * FrameHeight];

			public bool IsFar(int x, int y) {
				x += OffsetX;
				y += OffsetY;
				return x >= 0 && y >= 0 && x < FrameWidth && y < FrameHeight && Far[y * FrameWidth + x];
			}

			// Draws mask columns colStart..colEnd (inclusive) with column colStart at x0.
			// reverse flips those columns left to right. overlay only paints over pixels that are already drawn,
			// so a faceplate can't stick out past the helmet. far marks the pixels as the darker, far-side limb.
			// rowShift(row) moves each row sideways, used to angle the legs when striding.
			public void Mask(string[] mask, int x0, int y0, int colStart = 0, int colEnd = int.MaxValue,
				bool reverse = false, bool overlay = false, bool far = false, Func<int, int> rowShift = null) {
				for (int row = 0; row < mask.Length; row++) {
					string line = mask[row];
					int last = Math.Min(colEnd, line.Length - 1);
					for (int col = colStart; col <= last; col++) {
						char c = line[reverse ? last - (col - colStart) : col];
						if (c == '.') {
							continue;
						}
						int x = x0 + col - colStart + (rowShift?.Invoke(row) ?? 0);
						int y = y0 + row;
						if (overlay && Get(x, y) == SuitRole.None) {
							continue;
						}
						Set(x, y, RoleOf(c));
						int px = x + OffsetX, py = y + OffsetY;
						if (px >= 0 && py >= 0 && px < FrameWidth && py < FrameHeight) {
							Far[py * FrameWidth + px] = far;
						}
					}
				}
			}

			// Darkens already-drawn armour pixels in a rectangle, to separate a limb from the body behind it.
			public void Seam(int x0, int y0, int x1, int y1) {
				for (int y = y0; y <= y1; y++) {
					for (int x = x0; x <= x1; x++) {
						SuitRole role = Get(x, y);
						if (role != SuitRole.None && !IsGlow(role)) {
							Set(x, y, SuitRole.Dark);
						}
					}
				}
			}

			public void FarRect(int x0, int y0, int x1, int y1, SuitRole role) {
				Rect(x0, y0, x1, y1, role);
				for (int y = y0; y <= y1; y++) {
					for (int x = x0; x <= x1; x++) {
						int px = x + OffsetX, py = y + OffsetY;
						if (px >= 0 && py >= 0 && px < FrameWidth && py < FrameHeight) {
							Far[py * FrameWidth + px] = true;
						}
					}
				}
			}
		}

		// How far back modules are moved right in the side view.
		private const int BackShift = 5;

		public static SuitRole RoleOf(char c) => c switch {
			'P' => SuitRole.Primary,
			'S' => SuitRole.Secondary,
			'A' => SuitRole.Accent,
			'T' => SuitRole.Trim,
			'J' => SuitRole.Undersuit,
			'O' => SuitRole.Dark,
			'#' => SuitRole.Emblem,
			'E' => SuitRole.Eye,
			'R' => SuitRole.Reactor,
			'H' => SuitRole.Repulsor,
			'F' => SuitRole.Thruster,
			_ => SuitRole.None // 'X' erases
		};

		public static bool IsGlow(SuitRole role) => role >= SuitRole.Eye;

		// body and glow must hold SheetWidth x FrameHeight pixels.
		public static void Render(SuitConfig config, Color[] body, Color[] glow) {
			for (int f = 0; f < FrameCount; f++) {
				var canvas = new Canvas();
				DrawFrame(config, (SuitFrame)f, canvas);
				Resolve(config, canvas, f, body, glow);
			}
		}

		// The suit is drawn in side view, facing right (the draw code flips it to face left). Parts are designed as
		// front-view masks, so each is adapted here: the front of the face and chest faces right, one leg and arm are in
		// front of the body and the other pair is drawn darker behind it.
		private static void DrawFrame(SuitConfig config, SuitFrame frame, Canvas c) {
			bool aiming = frame is SuitFrame.Aim or SuitFrame.FlyAim;
			bool flying = frame is SuitFrame.Fly or SuitFrame.FlyAim;

			// Stride of each foot (how far forward it is), near leg lift, and arm swing.
			int near = 0, far = 0, lift = 0, swing = 0;
			switch (frame) {
				case SuitFrame.Walk1: near = 3; far = -3; swing = -1; break;
				case SuitFrame.Walk2: near = 1; far = -1; break;
				case SuitFrame.Walk3: near = -3; far = 3; swing = 1; break;
				case SuitFrame.Walk4: near = -1; far = 1; break;
				case SuitFrame.Jump: near = 2; far = -1; lift = -1; break;
			}
			if (flying) {
				near = -2; // legs trail behind
				far = -3;
			}

			DrawBack(c, config.OptionName(SuitCatalog.Back));

			string[] legs = SuitParts.Legs[config[SuitCatalog.Legs]].Mask;
			string[] boots = SuitParts.Boots[config[SuitCatalog.Boots]].Mask;
			string[] gauntlet = SuitParts.Gauntlets[config[SuitCatalog.Gauntlets]].Mask;

			// Legs pivot at the hip: each row of the leg moves a bit further, and the boot moves the full stride.
			static Func<int, int> Leg(int stride) => row => (int)MathF.Round(stride * (row + 1) / 7f);

			// Far leg and boot (the right-hand leg of the front view; its outer side already points forward).
			c.Mask(legs, 10, 18, colStart: 5, colEnd: 8, far: true, rowShift: Leg(far));
			c.Mask(boots, 10 + far, 24, colStart: 6, colEnd: 10, far: true);

			// Far arm, mostly hidden behind the body.
			if (!aiming) {
				c.FarRect(11 - swing, 11, 12 - swing, 14, SuitRole.Primary);
				c.Mask(gauntlet, 10 - swing, 14, colStart: 0, colEnd: 3, reverse: true, far: true);
			}

			// Near leg and boot, with the toe turned forward.
			c.Mask(legs, 10, 18 + lift, colStart: 0, colEnd: 3, rowShift: Leg(near));
			c.Mask(boots, 10 + near, 24 + lift, colStart: 0, colEnd: 4, reverse: true);

			// Torso: the front half of the chest plate, with the reactor on the front edge.
			c.Mask(SuitParts.Chests[config[SuitCatalog.Chest]].Mask, 9, 9, colStart: 4, colEnd: 10);
			c.Mask(SuitParts.Belts[config[SuitCatalog.Belt]].Mask, 9, 16, colStart: 2, colEnd: 8);
			c.Mask(SuitParts.Reactors[config[SuitCatalog.Reactor]].Mask, 13, 11, colStart: 2, colEnd: 4, overlay: true);

			// Near arm: hanging and swinging, or held out forward when aiming.
			if (aiming) {
				c.Seam(11, 13, 15, 13);
				c.Rect(12, 11, 18, 12, SuitRole.Primary);
				c.Rect(19, 10, 21, 13, SuitRole.Secondary);
				c.Rect(22, 11, 22, 12, SuitRole.Repulsor);
			}
			else {
				c.Seam(10 + swing, 12, 10 + swing, 14);
				c.Seam(13 + swing, 12, 13 + swing, 14);
				c.Rect(11 + swing, 11, 12 + swing, 14, SuitRole.Primary);
				c.Mask(gauntlet, 10 + swing, 14, colStart: 0, colEnd: 3, reverse: true);
			}

			// Shoulder pad, with the emblem painted on it.
			c.Mask(SuitParts.Shoulders[config[SuitCatalog.Shoulders]].Mask, 10, 9, reverse: true);
			c.Mask(SuitParts.Emblems[config[SuitCatalog.Emblem]].Mask, 10, 9, colStart: 1, colEnd: 3, overlay: true);

			// Head: the faceplate and eyes sit on the front of the helmet.
			c.Mask(SuitParts.Helmets[config[SuitCatalog.Helmet]].Mask, 8, 0);
			c.Mask(SuitParts.Faceplates[config[SuitCatalog.Faceplate]].Mask, 13, 5, colStart: 3, colEnd: 6, overlay: true);
			c.Mask(SuitParts.Eyes[config[SuitCatalog.Eyes]].Mask, 14, 5, colStart: 4, colEnd: 6, overlay: true);

			ApplyPattern(c, config[SuitCatalog.Pattern]);
		}

		private static void DrawBack(Canvas c, string style) {
			switch (style) {
				case "Jetpack":
					c.BackRect(2, 8, 3, 15, SuitRole.Secondary);
					c.BackRect(2, 7, 3, 7, SuitRole.Trim);
					c.BackRect(2, 16, 3, 16, SuitRole.Dark);
					break;
				case "Flight Fins":
					c.BackRect(2, 5, 3, 6, SuitRole.Accent);
					c.BackRect(1, 7, 3, 8, SuitRole.Accent);
					c.BackRect(0, 9, 3, 10, SuitRole.Accent);
					break;
				case "Wing Blades":
					for (int i = 0; i < 6; i++) {
						c.BackRect(5 - i, 8 - i, 5 - i, 9 - i, SuitRole.Accent);
					}
					for (int i = 0; i < 4; i++) {
						c.BackRect(3 - i, 10 + i, 3 - i, 10 + i, SuitRole.Accent);
					}
					break;
				case "Twin Tanks":
					c.BackRect(5, 3, 6, 8, SuitRole.Secondary);
					c.BackRect(5, 2, 6, 2, SuitRole.Trim);
					break;
				case "Missile Pods":
					c.BackRect(4, 5, 7, 8, SuitRole.Secondary);
					c.BackRect(4, 5, 4, 5, SuitRole.Accent);
					c.BackRect(6, 5, 6, 5, SuitRole.Accent);
					c.BackRect(4, 6, 7, 6, SuitRole.Dark);
					break;
				case "Antennae":
					c.BackRect(6, 1, 6, 8, SuitRole.Trim);
					c.BackRect(6, 0, 6, 0, SuitRole.Accent);
					break;
				case "Power Pack":
					c.BackRect(4, 6, 7, 8, SuitRole.Secondary);
					c.BackRect(5, 7, 6, 7, SuitRole.Reactor);
					break;
				case "Radiator Fins":
					for (int i = 0; i < 5; i++) {
						c.BackRect(1, 6 + 2 * i, 3, 6 + 2 * i, SuitRole.Trim);
					}
					break;
				case "Rocket Boosters":
					c.BackRect(1, 5, 3, 15, SuitRole.Secondary);
					c.BackRect(1, 4, 3, 4, SuitRole.Accent);
					c.BackRect(2, 3, 2, 3, SuitRole.Accent);
					c.BackRect(1, 16, 3, 16, SuitRole.Thruster);
					break;

				// Premium set backs
				case "Bat Wings":
					c.BackRect(1, 4, 5, 4, SuitRole.Accent);
					c.BackRect(0, 5, 3, 11, SuitRole.Undersuit);
					c.BackRect(0, 5, 0, 11, SuitRole.Accent);
					c.BackRect(2, 5, 2, 10, SuitRole.Accent);
					c.BackRect(1, 11, 1, 11, SuitRole.None); // scalloped edge
					break;
				case "Flame Wings":
					c.BackRect(3, 3, 3, 5, SuitRole.Thruster);
					c.BackRect(2, 5, 3, 8, SuitRole.Thruster);
					c.BackRect(1, 7, 3, 10, SuitRole.Thruster);
					c.BackRect(0, 9, 3, 12, SuitRole.Thruster);
					c.BackRect(1, 2, 1, 4, SuitRole.Thruster);
					break;
				case "Reactor Stacks":
					c.BackRect(2, 2, 4, 2, SuitRole.Trim);
					c.BackRect(2, 3, 4, 14, SuitRole.Secondary);
					c.BackRect(3, 4, 3, 12, SuitRole.Reactor);
					break;
				case "Crystal Shards":
					c.BackRect(3, 2, 3, 7, SuitRole.Accent);
					c.BackRect(1, 4, 1, 9, SuitRole.Accent);
					c.BackRect(2, 6, 2, 11, SuitRole.Accent);
					c.BackRect(0, 8, 0, 12, SuitRole.Accent);
					break;
				case "Tesla Array":
					c.BackRect(4, 1, 4, 8, SuitRole.Trim);
					c.BackRect(4, 0, 4, 0, SuitRole.Repulsor);
					c.BackRect(2, 4, 2, 10, SuitRole.Trim);
					c.BackRect(2, 3, 2, 3, SuitRole.Repulsor);
					c.BackRect(2, 6, 4, 6, SuitRole.Trim);
					break;
				case "Void Tendrils":
					int[] xs = { 4, 3, 3, 2, 2, 1, 1, 0, 0, 1 };
					for (int i = 0; i < xs.Length; i++) {
						c.BackRect(xs[i], 3 + i, xs[i], 3 + i, i % 3 == 2 ? SuitRole.Eye : SuitRole.Dark);
					}
					break;
				case "Angel Wings":
					c.BackRect(0, 3, 3, 4, SuitRole.Accent);
					c.BackRect(0, 5, 4, 6, SuitRole.Secondary);
					c.BackRect(0, 7, 3, 8, SuitRole.Secondary);
					c.BackRect(1, 9, 3, 10, SuitRole.Secondary);
					c.BackRect(2, 11, 3, 12, SuitRole.Secondary);
					break;
			}
		}

		private static int Hash(int x, int y) => ((x * 73856093) ^ (y * 19349663) ^ 0x5bd1e995) & 0x7fffffff;

		// Recolours primary pixels that fall on the chosen pattern.
		private static void ApplyPattern(Canvas c, int pattern) {
			if (pattern == 0) {
				return;
			}
			for (int y = 0; y < 27; y++) {
				for (int x = 0; x < 27; x++) {
					if (c.Get(x, y) == SuitRole.Primary && OnPattern(pattern, x, y)) {
						c.Set(x, y, SuitRole.Pattern);
					}
				}
			}
		}

		private static bool OnPattern(int pattern, int x, int y) => pattern switch {
			1 => x == 12,                                                      // Racing Stripe
			2 => x == 10 || x == 14,                                           // Twin Stripes
			3 => ((y - Math.Abs(x - 12)) % 5 + 5) % 5 == 0,                     // Chevrons
			4 => y % 4 == 0 || x % 5 == 0,                                     // Panel Lines
			5 => y % 2 == 0 && (x + y / 2 % 2 * 2) % 4 == 0,                   // Hex Plating
			6 => (x * 3 + y * 2) % 7 == 0 || ((x * 3 + y * 2) % 7 == 1 && y % 3 == 0), // Tiger Stripes
			7 => x > 12,                                                       // Split Half
			8 => (x / 2 + y / 2) % 2 == 0,                                     // Checker
			9 => (x + y) % 4 == 0,                                             // Diagonal
			10 => (x % 4 == 1 && y % 3 != 0) || (y % 6 == 2 && x % 4 < 2),     // Circuit
			11 => Hash(x / 2, y / 2) % 3 == 0,                                 // Camo
			12 => y > 18 || (y > 14 && (x + y) % 2 == 0),                      // Lower Fade
			13 => x % 3 == 1 && y % 3 == 1,                                    // Dots
			_ => false
		};

		private static Color Shade(Color colour, float amount) {
			Color result = amount >= 0f ? Color.Lerp(colour, Color.White, amount) : Color.Lerp(colour, Color.Black, -amount);
			result.A = 255;
			return result;
		}

		private static Color Desaturate(Color colour, float amount) {
			int grey = (colour.R * 30 + colour.G * 59 + colour.B * 11) / 100;
			return Color.Lerp(colour, new Color(grey, grey, grey), amount);
		}

		private static Color BaseColour(SuitConfig config, SuitRole role) => role switch {
			SuitRole.Primary => config.Colour(SuitCatalog.PrimaryColour),
			SuitRole.Secondary => config.Colour(SuitCatalog.SecondaryColour),
			SuitRole.Accent => config.Colour(SuitCatalog.AccentColour),
			SuitRole.Trim => config.Colour(SuitCatalog.TrimColour),
			SuitRole.Undersuit => config.Colour(SuitCatalog.UndersuitColour),
			SuitRole.Dark => Shade(config.Colour(SuitCatalog.PrimaryColour), -0.7f),
			SuitRole.Pattern => config.Colour(SuitCatalog.PatternColour),
			SuitRole.Emblem => config.Colour(SuitCatalog.EmblemColour),
			SuitRole.Eye => config.Colour(SuitCatalog.EyeColour),
			SuitRole.Reactor => config.Colour(SuitCatalog.ReactorColour),
			SuitRole.Repulsor => config.Colour(SuitCatalog.RepulsorColour),
			SuitRole.Thruster => config.Colour(SuitCatalog.ThrusterColour),
			_ => Color.Transparent
		};

		// Colours every pixel of one frame: armour gets edge lighting based on the finish, glow roles go to the glow layer,
		// and a dark outline is drawn around the whole silhouette.
		private static void Resolve(SuitConfig config, Canvas c, int frameIndex, Color[] body, Color[] glow) {
			string finish = config.OptionName(SuitCatalog.Finish);
			(float highlight, float shadow) = finish switch {
				"Matte" => (0.10f, 0.15f),
				"Chrome" => (0.55f, 0.45f),
				"Gloss" => (0.40f, 0.20f),
				"Stealth" => (0.10f, 0.20f),
				"Obsidian" => (0.35f, 0.25f),
				"Radiant" => (0.65f, 0.10f),
				_ => (0.30f, 0.30f) // Metallic, Battle-Damaged
			};
			Color outline = Shade(config.Colour(SuitCatalog.PrimaryColour), -0.82f);
			int sheetWidth = SheetWidth;

			for (int py = 0; py < FrameHeight; py++) {
				for (int px = 0; px < FrameWidth; px++) {
					int x = px - OffsetX;
					int y = py - OffsetY;
					int index = py * sheetWidth + frameIndex * FrameWidth + px;
					SuitRole role = c.Get(x, y);

					if (role == SuitRole.None) {
						bool touches = c.Get(x - 1, y) != SuitRole.None || c.Get(x + 1, y) != SuitRole.None
							|| c.Get(x, y - 1) != SuitRole.None || c.Get(x, y + 1) != SuitRole.None;
						body[index] = touches ? outline : Color.Transparent;
						glow[index] = Color.Transparent;
						continue;
					}

					Color colour = BaseColour(config, role);
					if (IsGlow(role)) {
						// A dim copy on the body layer keeps the shape visible when the glow pulses down.
						body[index] = Shade(colour, -0.45f);
						glow[index] = Shade(colour, 0.15f);
						continue;
					}

					if (finish == "Stealth") {
						colour = Shade(Desaturate(colour, 0.5f), -0.45f);
					}
					else if (finish == "Obsidian") {
						colour = Shade(Desaturate(colour, 0.3f), -0.6f);
					}
					else if (finish == "Radiant") {
						colour = Shade(colour, 0.15f);
					}

					bool lit = c.Get(x, y - 1) == SuitRole.None || c.Get(x - 1, y) == SuitRole.None;
					bool shaded = c.Get(x, y + 1) == SuitRole.None || c.Get(x + 1, y) == SuitRole.None;
					float amount = (lit ? highlight : 0f) - (shaded ? shadow : 0f);

					if (finish == "Chrome" && (x + y) % 4 == 0) {
						amount += 0.2f; // chrome banding
					}
					if ((finish == "Gloss" || finish == "Obsidian") && lit && c.Get(x - 1, y) == SuitRole.None && c.Get(x, y - 1) == SuitRole.None) {
						amount = 0.75f; // gloss specular corner
					}
					colour = Shade(colour, Math.Clamp(amount, -0.9f, 0.9f));

					if (finish == "Battle-Damaged") {
						int h = Hash(x, y);
						if (h % 9 == 0) {
							colour = Shade(colour, -0.5f); // scorch marks
						}
						else if (h % 13 == 0) {
							colour = Color.Lerp(colour, new Color(170, 170, 170), 0.6f); // scratched to bare metal
						}
					}

					if (c.IsFar(x, y)) {
						colour = Shade(colour, -0.35f);
					}
					body[index] = colour;
					glow[index] = Color.Transparent;
				}
			}
		}
	}
}
