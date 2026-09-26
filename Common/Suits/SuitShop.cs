using System.Collections.Generic;
using System.Text;

namespace MarvelMod.Common.Suits
{
	// One option sold in the Parts Store.
	public class ShopItem
	{
		public SuitCategory Category { get; init; }
		public int Index { get; init; }
		public long Price { get; init; }
		public ShopRequirement Requirement { get; init; }
		public SuitSet Set { get; init; }

		// Free options are unlocked from the start: the first option of each category and everything in the classic suit.
		public bool Free { get; init; }

		public string Name => Category.OptionName(Index);

		// Save key. Uses the option's name, so reordering options doesn't change what a player owns.
		public string Key => $"{Category.Key}/{Name}";
	}

	// What the Parts Store sells and for how much. Colours, Glow Strength and Glow Pulse are always free.
	public static class SuitShop
	{
		public const long Copper = 1;
		public const long Silver = 100;
		public const long Gold = 100 * Silver;
		public const long Platinum = 100 * Gold;

		// Premium options that aren't part of a set: (category key, option) -> price and progression requirement.
		private static readonly Dictionary<(string, string), (long Price, ShopRequirement Requirement)> Specials = new() {
			[("finish", "Obsidian")] = (8 * Gold, ShopRequirement.Hardmode),
			[("finish", "Radiant")] = (20 * Gold, ShopRequirement.Plantera),
			[("thrusterTrail", "Blood")] = (5 * Gold, ShopRequirement.Hardmode),
			[("thrusterTrail", "Holy Light")] = (10 * Gold, ShopRequirement.MechBoss),
			[("thrusterTrail", "Frost")] = (10 * Gold, ShopRequirement.MechBoss),
			[("thrusterTrail", "Void")] = (20 * Gold, ShopRequirement.Cultist),
		};

		private static List<ShopItem> items;
		private static Dictionary<(int, int), ShopItem> lookup;

		public static IReadOnlyList<ShopItem> Items {
			get {
				Build();
				return items;
			}
		}

		public static bool IsSold(SuitCategory category) {
			return !category.IsColour && category != SuitCatalog.GlowStrength && category != SuitCatalog.GlowPulse;
		}

		// null if the option isn't sold (always free).
		public static ShopItem Get(SuitCategory category, int index) {
			Build();
			return lookup.TryGetValue((category.Index, index), out ShopItem item) ? item : null;
		}

		private static void Build() {
			if (items != null) {
				return;
			}
			items = new List<ShopItem>();
			lookup = new Dictionary<(int, int), ShopItem>();
			SuitConfig classic = SuitPresets.Classic();

			foreach (SuitCategory category in SuitCatalog.All) {
				if (!IsSold(category)) {
					continue;
				}
				for (int i = 0; i < category.Count; i++) {
					string name = category.OptionName(i);
					SuitSet set = SuitSets.Owning(category, name);
					long price;
					ShopRequirement requirement = ShopRequirement.None;

					if (set != null) {
						price = set.PricePerPiece;
						requirement = set.Requirement;
					}
					else if (Specials.TryGetValue((category.Key, name), out var special)) {
						(price, requirement) = special;
					}
					else if (category.Group == SuitGroup.Systems) {
						price = 5 * Gold;
					}
					else if (category == SuitCatalog.ThrusterTrail) {
						price = 1 * Gold;
					}
					else {
						price = 50 * Silver + i * 50 * Silver; // later options in a list cost a little more
					}

					var item = new ShopItem {
						Category = category,
						Index = i,
						Price = price,
						Requirement = requirement,
						Set = set,
						Free = set == null && requirement == ShopRequirement.None && (i == 0 || classic[category] == i)
					};
					items.Add(item);
					lookup[(category.Index, i)] = item;
				}
			}
		}

		public static void Unload() {
			items = null;
			lookup = null;
		}

		// "1p 5g 20s" with each coin in its colour, using chat tags (UIText renders them).
		public static string FormatPrice(long copper) {
			if (copper <= 0) {
				return "[c/B5C0C1:free]";
			}
			var text = new StringBuilder();
			void Part(long amount, string suffix, string colour) {
				if (amount > 0) {
					if (text.Length > 0) {
						text.Append(' ');
					}
					text.Append($"[c/{colour}:{amount}{suffix}]");
				}
			}
			Part(copper / Platinum, "p", "DCDCC6");
			Part(copper % Platinum / Gold, "g", "E0C95C");
			Part(copper % Gold / Silver, "s", "B5C0C1");
			Part(copper % Silver, "c", "F68A60");
			return text.ToString();
		}
	}
}
