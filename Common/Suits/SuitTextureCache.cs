using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace MarvelMod.Common.Suits
{
	// Builds suit sprite sheets on demand and keeps the most recent ones, keyed by the design.
	// Only call Get from the main thread (drawing code), since it creates textures.
	public class SuitTextureCache : ModSystem
	{
		public class Entry
		{
			public Texture2D Body;
			public Texture2D Glow;
		}

		// Enough for every player in a full server plus workshop edits, so a texture is never evicted while it is still being drawn.
		private const int MaxEntries = 300;

		private static readonly Dictionary<string, Entry> entries = new();
		private static readonly LinkedList<string> recent = new();

		public static Entry Get(SuitConfig config) {
			string key = Convert.ToBase64String(config.Values);
			if (entries.TryGetValue(key, out Entry entry)) {
				recent.Remove(key);
				recent.AddFirst(key);
				return entry;
			}

			int width = SuitRenderer.SheetWidth;
			int height = SuitRenderer.FrameHeight;
			var body = new Color[width * height];
			var glow = new Color[width * height];
			SuitRenderer.Render(config, body, glow);

			entry = new Entry {
				Body = new Texture2D(Main.instance.GraphicsDevice, width, height),
				Glow = new Texture2D(Main.instance.GraphicsDevice, width, height)
			};
			entry.Body.SetData(body);
			entry.Glow.SetData(glow);

			entries[key] = entry;
			recent.AddFirst(key);
			while (recent.Count > MaxEntries) {
				string oldest = recent.Last.Value;
				recent.RemoveLast();
				Dispose(entries[oldest]);
				entries.Remove(oldest);
			}
			return entry;
		}

		private static void Dispose(Entry entry) {
			entry.Body.Dispose();
			entry.Glow.Dispose();
		}

		public override void Unload() {
			var old = new List<Entry>(entries.Values);
			entries.Clear();
			recent.Clear();
			Main.QueueMainThreadAction(() => old.ForEach(Dispose));
		}
	}
}
