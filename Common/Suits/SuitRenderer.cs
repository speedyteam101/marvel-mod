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
		// The suit is drawn on a 27 x 27 grid (feet on row 26, body centred on column 12) with a margin for the outline.
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
				}
			}

			public void Rect(int x0, int y0, int x1, int y1, SuitRole role) {
				for (int y = y0; y <= y1; y++) {
					for (int x = x0; x <= x1; x++) {
						Set(x, y, role);
					}
				}
			}

			// Draws a rectangle and its mirror image across the body's centre column.
			public void MirrorRect(int x0, int y0, int x1, int y1, SuitRole role) {
				Rect(x0, y0, x1, y1, role);
				Rect(Mirror(x1), y0, Mirror(x0), y1, role);
			}

			// shift(column) returns an (dx, dy) offset per mask column, used to move each leg separately.
			public void Mask(string[] mask, int x0, int y0, bool mirror = false, Func<int, (int dx, int dy)> shift = null) {
				for (int row = 0; row < mask.Length; row++) {
					string line = mask[row];
					for (int col = 0; col < line.Length; col++) {
						char c = line[col];
						if (c == '.') {
							continue;
						}
						(int dx, int dy) = shift?.Invoke(col) ?? (0, 0);
						int x = x0 + col;
						if (mirror) {
							x = Mirror(x);
						}
						Set(x + dx, y0 + row + dy, RoleOf(c));
					}
				}
			}
		}

		private static int Mirror(int x) => 24 - x;

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

		private static void DrawFrame(SuitConfig config, SuitFrame frame, Canvas c) {
			bool aiming = frame is SuitFrame.Aim or SuitFrame.FlyAim;
			bool flying = frame is SuitFrame.Fly or SuitFrame.FlyAim;

			// Leg offsets: walking lifts one leg at a time, jumping tucks both, flying brings them together.
			(int dx, int dy) left = (0, 0), right = (0, 0);
			switch (frame) {
				case SuitFrame.Walk1: left = (0, -1); break;
				case SuitFrame.Walk3: right = (0, -1); break;
				case SuitFrame.Jump: left = (0, -1); right = (0, -1); break;
			}
			if (flying) {
				left = (1, 0);
				right = (-1, 0);
			}

			DrawBack(c, config[SuitCatalog.Back]);

			c.Mask(SuitParts.Legs[config[SuitCatalog.Legs]].Mask, 8, 18, shift: col => col < 4 ? left : col > 4 ? right : (0, 0));
			c.Mask(SuitParts.Boots[config[SuitCatalog.Boots]].Mask, 7, 24, shift: col => col < 5 ? left : col > 5 ? right : (0, 0));

			// Upper arms and gauntlets. The front (right-hand) arm is raised instead when aiming.
			string[] gauntlet = SuitParts.Gauntlets[config[SuitCatalog.Gauntlets]].Mask;
			c.Rect(5, 12, 6, 14, SuitRole.Primary);
			c.Mask(gauntlet, 4, 14);
			if (!aiming) {
				c.Rect(18, 12, 19, 14, SuitRole.Primary);
				c.Mask(gauntlet, 4, 14, mirror: true);
			}

			c.Mask(SuitParts.Chests[config[SuitCatalog.Chest]].Mask, 7, 9);
			c.Mask(SuitParts.Belts[config[SuitCatalog.Belt]].Mask, 8, 16);
			c.Mask(SuitParts.Emblems[config[SuitCatalog.Emblem]].Mask, 10, 14);
			c.Mask(SuitParts.Reactors[config[SuitCatalog.Reactor]].Mask, 10, 11);

			if (aiming) {
				c.Rect(18, 12, 22, 13, SuitRole.Primary);
				c.Rect(22, 11, 24, 14, SuitRole.Secondary);
				c.Rect(25, 12, 25, 13, SuitRole.Repulsor);
			}

			string[] shoulder = SuitParts.Shoulders[config[SuitCatalog.Shoulders]].Mask;
			c.Mask(shoulder, 4, 9);
			c.Mask(shoulder, 4, 9, mirror: true);

			c.Mask(SuitParts.Helmets[config[SuitCatalog.Helmet]].Mask, 8, 0);
			c.Mask(SuitParts.Faceplates[config[SuitCatalog.Faceplate]].Mask, 9, 5);
			c.Mask(SuitParts.Eyes[config[SuitCatalog.Eyes]].Mask, 9, 5);

			ApplyPattern(c, config[SuitCatalog.Pattern]);
		}

		private static void DrawBack(Canvas c, int style) {
			switch (style) {
				case 1: // Jetpack
					c.MirrorRect(2, 8, 3, 15, SuitRole.Secondary);
					c.MirrorRect(2, 7, 3, 7, SuitRole.Trim);
					c.MirrorRect(2, 16, 3, 16, SuitRole.Dark);
					break;
				case 2: // Flight Fins
					c.MirrorRect(2, 5, 3, 6, SuitRole.Accent);
					c.MirrorRect(1, 7, 3, 8, SuitRole.Accent);
					c.MirrorRect(0, 9, 3, 10, SuitRole.Accent);
					break;
				case 3: // Wing Blades
					for (int i = 0; i < 6; i++) {
						c.MirrorRect(5 - i, 8 - i, 5 - i, 9 - i, SuitRole.Accent);
					}
					for (int i = 0; i < 4; i++) {
						c.MirrorRect(3 - i, 10 + i, 3 - i, 10 + i, SuitRole.Accent);
					}
					break;
				case 4: // Twin Tanks
					c.MirrorRect(5, 3, 6, 8, SuitRole.Secondary);
					c.MirrorRect(5, 2, 6, 2, SuitRole.Trim);
					break;
				case 5: // Missile Pods
					c.MirrorRect(4, 5, 7, 8, SuitRole.Secondary);
					c.MirrorRect(4, 5, 4, 5, SuitRole.Accent);
					c.MirrorRect(6, 5, 6, 5, SuitRole.Accent);
					c.MirrorRect(4, 6, 7, 6, SuitRole.Dark);
					break;
				case 6: // Antennae
					c.MirrorRect(6, 1, 6, 8, SuitRole.Trim);
					c.MirrorRect(6, 0, 6, 0, SuitRole.Accent);
					break;
				case 7: // Power Pack
					c.Rect(4, 6, 20, 8, SuitRole.Secondary);
					c.MirrorRect(5, 7, 6, 7, SuitRole.Reactor);
					break;
				case 8: // Radiator Fins
					for (int i = 0; i < 5; i++) {
						c.MirrorRect(1, 6 + 2 * i, 3, 6 + 2 * i, SuitRole.Trim);
					}
					break;
				case 9: // Rocket Boosters
					c.MirrorRect(1, 5, 3, 15, SuitRole.Secondary);
					c.MirrorRect(1, 4, 3, 4, SuitRole.Accent);
					c.MirrorRect(2, 3, 2, 3, SuitRole.Accent);
					c.MirrorRect(1, 16, 3, 16, SuitRole.Thruster);
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
			int finish = config[SuitCatalog.Finish];
			(float highlight, float shadow) = finish switch {
				1 => (0.10f, 0.15f), // Matte
				2 => (0.55f, 0.45f), // Chrome
				3 => (0.40f, 0.20f), // Gloss
				4 => (0.10f, 0.20f), // Stealth
				_ => (0.30f, 0.30f)  // Metallic, Battle-Damaged
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

					if (finish == 4) {
						colour = Shade(Desaturate(colour, 0.5f), -0.45f);
					}

					bool lit = c.Get(x, y - 1) == SuitRole.None || c.Get(x - 1, y) == SuitRole.None;
					bool shaded = c.Get(x, y + 1) == SuitRole.None || c.Get(x + 1, y) == SuitRole.None;
					float amount = (lit ? highlight : 0f) - (shaded ? shadow : 0f);

					if (finish == 2 && (x + y) % 4 == 0) {
						amount += 0.2f; // chrome banding
					}
					if (finish == 3 && lit && c.Get(x - 1, y) == SuitRole.None && c.Get(x, y - 1) == SuitRole.None) {
						amount = 0.75f; // gloss specular corner
					}
					colour = Shade(colour, Math.Clamp(amount, -0.9f, 0.9f));

					if (finish == 5) {
						int h = Hash(x, y);
						if (h % 9 == 0) {
							colour = Shade(colour, -0.5f); // scorch marks
						}
						else if (h % 13 == 0) {
							colour = Color.Lerp(colour, new Color(170, 170, 170), 0.6f); // scratched to bare metal
						}
					}

					body[index] = colour;
					glow[index] = Color.Transparent;
				}
			}
		}
	}
}
