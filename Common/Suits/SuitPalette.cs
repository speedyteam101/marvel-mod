using Microsoft.Xna.Framework;
using System;

namespace MarvelMod.Common.Suits
{
	// The 96 paint colours every colour slot can use: 12 hues x 7 shades, then 12 metals and neutrals.
	// Laid out as 8 rows of 12 in the Suit Workshop colour grid.
	public static class SuitPalette
	{
		public const int Columns = 12;

		private static readonly (string Name, float Hue)[] Hues = {
			("Red", 0f), ("Orange", 24f), ("Gold", 42f), ("Yellow", 55f), ("Lime", 85f), ("Green", 125f),
			("Teal", 165f), ("Cyan", 185f), ("Sky", 205f), ("Blue", 225f), ("Violet", 265f), ("Magenta", 310f)
		};

		private static readonly (string Name, float Saturation, float Lightness)[] Shades = {
			("Deep", 0.85f, 0.18f), ("Dark", 0.80f, 0.28f), ("Rich", 0.80f, 0.38f), ("Pure", 0.85f, 0.50f),
			("Bright", 0.90f, 0.62f), ("Soft", 0.60f, 0.72f), ("Pale", 0.55f, 0.84f)
		};

		private static readonly (string Name, Color Color)[] Neutrals = {
			("White", new Color(245, 245, 245)), ("Ivory", new Color(236, 228, 206)), ("Silver", new Color(196, 200, 208)),
			("Titanium", new Color(160, 164, 170)), ("Steel", new Color(122, 130, 140)), ("Gunmetal", new Color(84, 90, 98)),
			("Graphite", new Color(52, 54, 58)), ("Black", new Color(22, 22, 26)), ("Bronze", new Color(160, 110, 50)),
			("Copper", new Color(190, 105, 70)), ("Rose Gold", new Color(222, 150, 140)), ("Champagne", new Color(230, 205, 150))
		};

		public static readonly Color[] Colors;
		public static readonly string[] Names;

		public static int Count => Colors.Length;

		static SuitPalette() {
			int count = Hues.Length * Shades.Length + Neutrals.Length;
			Colors = new Color[count];
			Names = new string[count];

			// Row by row: every hue in one shade, so the grid reads light-to-dark top-to-bottom.
			int i = 0;
			foreach (var shade in Shades) {
				foreach (var hue in Hues) {
					Colors[i] = FromHsl(hue.Hue / 360f, shade.Saturation, shade.Lightness);
					Names[i] = $"{shade.Name} {hue.Name}";
					i++;
				}
			}
			foreach (var neutral in Neutrals) {
				Colors[i] = neutral.Color;
				Names[i] = neutral.Name;
				i++;
			}
		}

		// Index of a colour by name, e.g. "Rich Red" or "Gunmetal". Throws if it doesn't exist, so typos in presets show up at load.
		public static int IndexOf(string name) {
			int index = Array.IndexOf(Names, name);
			if (index < 0) {
				throw new ArgumentException($"Unknown suit colour '{name}'");
			}
			return index;
		}

		private static Color FromHsl(float h, float s, float l) {
			float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
			float p = 2f * l - q;
			return new Color(HueToRgb(p, q, h + 1f / 3f), HueToRgb(p, q, h), HueToRgb(p, q, h - 1f / 3f));
		}

		private static float HueToRgb(float p, float q, float t) {
			if (t < 0f) t += 1f;
			if (t > 1f) t -= 1f;
			if (t < 1f / 6f) return p + (q - p) * 6f * t;
			if (t < 1f / 2f) return q;
			if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
			return p;
		}
	}
}
