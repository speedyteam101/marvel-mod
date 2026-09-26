using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria.ModLoader.IO;

namespace MarvelMod.Common.Suits
{
	// A complete suit design: one chosen option per SuitCatalog category.
	public class SuitConfig
	{
		public readonly byte[] Values = new byte[SuitCatalog.Count];

		public int this[SuitCategory category] {
			get => Values[category.Index];
			set => Values[category.Index] = (byte)Math.Clamp(value, 0, category.Count - 1);
		}

		public Color Colour(SuitCategory category) => SuitPalette.Colors[this[category]];

		public string OptionName(SuitCategory category) => category.OptionName(this[category]);

		public void Set(SuitCategory category, string optionName) => this[category] = category.IndexOf(optionName);

		public SuitConfig Clone() {
			var copy = new SuitConfig();
			CopyTo(copy);
			return copy;
		}

		public void CopyTo(SuitConfig other) => Array.Copy(Values, other.Values, Values.Length);

		public bool SameAs(SuitConfig other) => other != null && Values.AsSpan().SequenceEqual(other.Values);

		// Randomises the chosen groups, only picking options that allowed(category, index) accepts.
		public void Randomise(Random random, Func<SuitCategory, int, bool> allowed, params SuitGroup[] groups) {
			foreach (SuitCategory category in SuitCatalog.All) {
				if (Array.IndexOf(groups, category.Group) < 0) {
					continue;
				}
				var choices = new System.Collections.Generic.List<int>();
				for (int i = 0; i < category.Count; i++) {
					if (allowed(category, i)) {
						choices.Add(i);
					}
				}
				if (choices.Count > 0) {
					this[category] = choices[random.Next(choices.Count)];
				}
			}
		}

		// Saved by category key and option name, so adding categories or options later doesn't break old saves.
		public TagCompound Save() {
			var tag = new TagCompound();
			foreach (SuitCategory category in SuitCatalog.All) {
				tag[category.Key] = OptionName(category); // by name, so adding options later can't change saved suits
			}
			return tag;
		}

		// Missing keys (from older saves) keep the classic suit's value.
		public static SuitConfig Load(TagCompound tag) {
			SuitConfig config = SuitPresets.Classic();
			if (tag == null) {
				return config;
			}
			foreach (SuitCategory category in SuitCatalog.All) {
				if (!tag.ContainsKey(category.Key)) {
					continue;
				}
				if (tag[category.Key] is string name) {
					int index = category.IsColour ? System.Array.IndexOf(SuitPalette.Names, name) : IndexOfName(category, name);
					if (index >= 0) {
						config[category] = index;
					}
				}
				else {
					config[category] = tag.GetInt(category.Key); // saves from before options were saved by name
				}
			}
			return config;
		}

		private static int IndexOfName(SuitCategory category, string name) {
			for (int i = 0; i < category.Count; i++) {
				if (category.OptionName(i) == name) {
					return i;
				}
			}
			return -1;
		}

		public void Write(BinaryWriter writer) => writer.Write(Values);

		public void Read(BinaryReader reader) {
			byte[] data = reader.ReadBytes(Values.Length);
			foreach (SuitCategory category in SuitCatalog.All) {
				this[category] = data[category.Index];
			}
		}
	}
}
