using System;
using System.Collections.Generic;

namespace MarvelMod.Common.Suits
{
	public enum SuitGroup
	{
		Armour,
		Paint,
		Colours,
		Effects,
		Systems,
		Weapons
	}

	// One row of the Suit Workshop: a list of named options, or a colour slot that picks from SuitPalette.
	public class SuitCategory
	{
		public int Index { get; internal set; }

		// Save key. Never rename one, or saved suits lose that setting.
		public string Key { get; }
		public string Name { get; }
		public SuitGroup Group { get; }
		public bool IsColour => options == null;

		// Short explanation shown in the workshop; used for the options that change gameplay.
		public string Description { get; }

		private readonly string[] options;

		public SuitCategory(string key, string name, SuitGroup group, string[] options, string description = null) {
			Key = key;
			Name = name;
			Group = group;
			this.options = options;
			Description = description;
		}

		public int Count => IsColour ? SuitPalette.Count : options.Length;

		public string OptionName(int index) => IsColour ? SuitPalette.Names[index] : options[index];

		public int IndexOf(string optionName) {
			int index = IsColour ? SuitPalette.IndexOf(optionName) : Array.IndexOf(options, optionName);
			if (index < 0) {
				throw new ArgumentException($"'{optionName}' is not an option of {Name}");
			}
			return index;
		}
	}

	// Every customisation category, in the order the workshop lists them.
	public static class SuitCatalog
	{
		public static readonly List<SuitCategory> All = new();

		// Armour pieces
		public static readonly SuitCategory Helmet = Add("helmet", "Helmet", SuitGroup.Armour, SuitParts.Names(SuitParts.Helmets));
		public static readonly SuitCategory Faceplate = Add("faceplate", "Faceplate", SuitGroup.Armour, SuitParts.Names(SuitParts.Faceplates));
		public static readonly SuitCategory Eyes = Add("eyes", "Eyes", SuitGroup.Armour, SuitParts.Names(SuitParts.Eyes));
		public static readonly SuitCategory Chest = Add("chest", "Chest", SuitGroup.Armour, SuitParts.Names(SuitParts.Chests));
		public static readonly SuitCategory Reactor = Add("reactor", "Arc Reactor", SuitGroup.Armour, SuitParts.Names(SuitParts.Reactors));
		public static readonly SuitCategory Shoulders = Add("shoulders", "Shoulders", SuitGroup.Armour, SuitParts.Names(SuitParts.Shoulders));
		public static readonly SuitCategory Gauntlets = Add("gauntlets", "Gauntlets", SuitGroup.Armour, SuitParts.Names(SuitParts.Gauntlets));
		public static readonly SuitCategory Belt = Add("belt", "Belt", SuitGroup.Armour, SuitParts.Names(SuitParts.Belts));
		public static readonly SuitCategory Legs = Add("legs", "Legs", SuitGroup.Armour, SuitParts.Names(SuitParts.Legs));
		public static readonly SuitCategory Boots = Add("boots", "Boots", SuitGroup.Armour, SuitParts.Names(SuitParts.Boots));
		public static readonly SuitCategory Back = Add("back", "Back Module", SuitGroup.Armour, SuitParts.BackModules);

		// Paint job
		public static readonly SuitCategory Pattern = Add("pattern", "Pattern", SuitGroup.Paint, SuitParts.Patterns);
		public static readonly SuitCategory Emblem = Add("emblem", "Emblem", SuitGroup.Paint, SuitParts.Names(SuitParts.Emblems));
		public static readonly SuitCategory Finish = Add("finish", "Finish", SuitGroup.Paint, SuitParts.Finishes);

		// Colour slots
		public static readonly SuitCategory PrimaryColour = Add("primaryColour", "Primary Colour", SuitGroup.Colours, null);
		public static readonly SuitCategory SecondaryColour = Add("secondaryColour", "Secondary Colour", SuitGroup.Colours, null);
		public static readonly SuitCategory AccentColour = Add("accentColour", "Accent Colour", SuitGroup.Colours, null);
		public static readonly SuitCategory TrimColour = Add("trimColour", "Trim Colour", SuitGroup.Colours, null);
		public static readonly SuitCategory UndersuitColour = Add("undersuitColour", "Undersuit Colour", SuitGroup.Colours, null);
		public static readonly SuitCategory PatternColour = Add("patternColour", "Pattern Colour", SuitGroup.Colours, null);
		public static readonly SuitCategory EmblemColour = Add("emblemColour", "Emblem Colour", SuitGroup.Colours, null);
		public static readonly SuitCategory EyeColour = Add("eyeColour", "Eye Glow", SuitGroup.Colours, null);
		public static readonly SuitCategory ReactorColour = Add("reactorColour", "Reactor Glow", SuitGroup.Colours, null);
		public static readonly SuitCategory RepulsorColour = Add("repulsorColour", "Repulsor Glow", SuitGroup.Colours, null);
		public static readonly SuitCategory ThrusterColour = Add("thrusterColour", "Thruster Colour", SuitGroup.Colours, null);

		// Visual effects
		public static readonly SuitCategory GlowStrength = Add("glowStrength", "Glow Strength", SuitGroup.Effects,
			new[] { "Dim", "Low", "Medium", "High", "Blinding" });
		public static readonly SuitCategory GlowPulse = Add("glowPulse", "Glow Pulse", SuitGroup.Effects,
			new[] { "Steady", "Slow Pulse", "Fast Pulse", "Flicker" });
		public static readonly SuitCategory ThrusterTrail = Add("thrusterTrail", "Thruster Trail", SuitGroup.Effects,
			new[] { "Flame", "Plasma", "Sparks", "Smoke", "Rainbow", "Ion", "Electric", "None", "Blood", "Holy Light", "Frost", "Void" });

		// Systems: these change how the suit plays (see SuitStats and the ability items).
		public static readonly SuitCategory RepulsorMode = Add("repulsorMode", "Repulsor Mode", SuitGroup.Systems,
			new[] { "Standard", "Rapid", "Spread", "Heavy", "Piercing", "Twin" },
			"Changes how the Repulsors ability fires");
		public static readonly SuitCategory ShoulderWeapon = Add("shoulderWeapon", "Shoulder Weapon", SuitGroup.Systems,
			new[] { "Micro-Missiles", "Minigun", "Flare Pods", "Shoulder Laser", "Heavy Rocket", "Cluster Bombs" },
			"Which weapon the Shoulder Weapon ability fires");
		public static readonly SuitCategory UnibeamMode = Add("unibeamMode", "Unibeam Mode", SuitGroup.Systems,
			new[] { "Focused", "Wide", "Pulse", "Overcharge" },
			"Changes the chest Unibeam (Mk II reactor and up)");
		public static readonly SuitCategory Plating = Add("plating", "Armour Plating", SuitGroup.Systems,
			new[] { "Light", "Standard", "Reinforced", "Heavy", "Vibranium Alloy" },
			"Trades defense against speed");
		public static readonly SuitCategory Thrusters = Add("thrusters", "Thrusters", SuitGroup.Systems,
			new[] { "Standard", "Racing", "Heavy-Lift", "Hover", "Afterburner" },
			"Changes flight speed and handling");
		public static readonly SuitCategory PowerCore = Add("powerCore", "Power Core", SuitGroup.Systems,
			new[] { "Balanced", "Overclocked", "Efficient", "Regenerative", "Unstable" },
			"Trades damage, attack speed and regeneration");

		// Attached weapons (see SuitWeapons). Both slots offer the same weapons; one purchase unlocks a weapon for both.
		public static readonly SuitCategory WeaponSlot1 = Add("weaponSlot1", "Weapon Slot I", SuitGroup.Weapons, SuitWeapons.Names(),
			"Used by the Weapon I ability; stowed on your back");
		public static readonly SuitCategory WeaponSlot2 = Add("weaponSlot2", "Weapon Slot II", SuitGroup.Weapons, SuitWeapons.Names(),
			"Used by the Weapon II ability; stowed on your back");
		public static readonly SuitCategory WeaponColour = Add("weaponColour", "Weapon Colour", SuitGroup.Weapons, null);

		public static int Count => All.Count;

		// Sum of every option in every category.
		public static int TotalOptions {
			get {
				int total = 0;
				foreach (SuitCategory category in All) {
					total += category.Count;
				}
				return total;
			}
		}

		// Number of distinct suits: the product of every category's option count.
		public static double TotalCombinations {
			get {
				double product = 1;
				foreach (SuitCategory category in All) {
					product *= category.Count;
				}
				return product;
			}
		}

		public static SuitCategory ByKey(string key) => All.Find(c => c.Key == key);

		private static SuitCategory Add(string key, string name, SuitGroup group, string[] options, string description = null) {
			var category = new SuitCategory(key, name, group, options, description) { Index = All.Count };
			All.Add(category);
			return category;
		}

		public static string GroupName(SuitGroup group) => group switch {
			SuitGroup.Armour => "Armour",
			SuitGroup.Paint => "Paint Job",
			SuitGroup.Colours => "Colours",
			SuitGroup.Effects => "Effects",
			SuitGroup.Weapons => "Weapons",
			_ => "Systems"
		};
	}
}
