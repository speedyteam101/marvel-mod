using System.Collections.Generic;

namespace MarvelMod.Common.Suits
{
	// Ready-made suits the workshop can load as a starting point.
	public static class SuitPresets
	{
		public class Preset
		{
			public string Name { get; init; }
			public (SuitCategory Category, string Option)[] Settings { get; init; }

			public SuitConfig Build() {
				SuitConfig config = Base();
				foreach (var (category, option) in Settings) {
					config.Set(category, option);
				}
				return config;
			}
		}

		private static SuitCategory C(string key) => SuitCatalog.ByKey(key);

		// Red and gold, the default suit. Every other preset starts from this and changes some settings.
		private static SuitConfig Base() {
			var config = new SuitConfig();
			config.Set(SuitCatalog.Helmet, "Classic");
			config.Set(SuitCatalog.Faceplate, "Classic Mask");
			config.Set(SuitCatalog.Eyes, "Classic Slits");
			config.Set(SuitCatalog.Chest, "Classic");
			config.Set(SuitCatalog.Reactor, "Classic");
			config.Set(SuitCatalog.Shoulders, "Classic");
			config.Set(SuitCatalog.Gauntlets, "Classic");
			config.Set(SuitCatalog.Belt, "Classic");
			config.Set(SuitCatalog.Legs, "Classic");
			config.Set(SuitCatalog.Boots, "Classic");
			config.Set(SuitCatalog.Back, "None");
			config.Set(SuitCatalog.Pattern, "None");
			config.Set(SuitCatalog.Emblem, "None");
			config.Set(SuitCatalog.Finish, "Metallic");
			config.Set(SuitCatalog.PrimaryColour, "Rich Red");
			config.Set(SuitCatalog.SecondaryColour, "Pure Gold");
			config.Set(SuitCatalog.AccentColour, "Bright Gold");
			config.Set(SuitCatalog.TrimColour, "Titanium");
			config.Set(SuitCatalog.UndersuitColour, "Graphite");
			config.Set(SuitCatalog.PatternColour, "Pure Gold");
			config.Set(SuitCatalog.EmblemColour, "Pure Gold");
			config.Set(SuitCatalog.EyeColour, "Pale Sky");
			config.Set(SuitCatalog.ReactorColour, "Pale Cyan");
			config.Set(SuitCatalog.RepulsorColour, "Soft Cyan");
			config.Set(SuitCatalog.ThrusterColour, "Bright Orange");
			config.Set(SuitCatalog.GlowStrength, "High");
			config.Set(SuitCatalog.GlowPulse, "Steady");
			config.Set(SuitCatalog.ThrusterTrail, "Flame");
			config.Set(SuitCatalog.RepulsorMode, "Standard");
			config.Set(SuitCatalog.ShoulderWeapon, "Micro-Missiles");
			config.Set(SuitCatalog.UnibeamMode, "Focused");
			config.Set(SuitCatalog.Plating, "Standard");
			config.Set(SuitCatalog.Thrusters, "Standard");
			config.Set(SuitCatalog.PowerCore, "Balanced");
			return config;
		}

		public static SuitConfig Classic() => Base();

		// One preset per premium set: every piece plus the set's colours.
		private static Preset ForSet(SuitSet set) {
			var settings = new List<(SuitCategory, string)>(set.Pieces());
			foreach (var (key, colour) in set.Colours) {
				settings.Add((C(key), colour));
			}
			return new Preset { Name = $"{set.Name} Set", Settings = settings.ToArray() };
		}

		public static readonly List<Preset> All = new() {
			new Preset {
				Name = "Classic Red & Gold",
				Settings = System.Array.Empty<(SuitCategory, string)>()
			},
			new Preset {
				Name = "Cave Prototype",
				Settings = new[] {
					(C("helmet"), "Mark I Bucket"), (C("faceplate"), "Grill"), (C("eyes"), "Narrow"), (C("chest"), "Mark I Plated"),
					(C("shoulders"), "Heavy"), (C("gauntlets"), "Plain"), (C("belt"), "Utility"), (C("legs"), "Armoured"),
					(C("boots"), "Stomper"), (C("finish"), "Battle-Damaged"), (C("primaryColour"), "Gunmetal"),
					(C("secondaryColour"), "Steel"), (C("accentColour"), "Graphite"), (C("trimColour"), "Steel"),
					(C("eyeColour"), "White"), (C("reactorColour"), "Pale Sky"), (C("glowStrength"), "Low"),
					(C("thrusterTrail"), "Smoke"), (C("plating"), "Heavy"), (C("thrusters"), "Heavy-Lift"), (C("shoulderWeapon"), "Flare Pods")
				}
			},
			new Preset {
				Name = "Silver Test Suit",
				Settings = new[] {
					(C("finish"), "Chrome"), (C("primaryColour"), "Silver"), (C("secondaryColour"), "Titanium"),
					(C("accentColour"), "White"), (C("trimColour"), "Steel"), (C("faceplate"), "Smooth"),
					(C("plating"), "Light"), (C("thrusters"), "Racing")
				}
			},
			new Preset {
				Name = "Centurion",
				Settings = new[] {
					(C("helmet"), "Angular"), (C("chest"), "Gold Core"), (C("shoulders"), "Pauldron"), (C("gauntlets"), "Heavy"),
					(C("legs"), "Thigh Plates"), (C("boots"), "Heavy"), (C("primaryColour"), "Dark Red"),
					(C("secondaryColour"), "Silver"), (C("accentColour"), "White"), (C("reactorColour"), "Pale Sky"),
					(C("reactor"), "Triangle"), (C("plating"), "Reinforced")
				}
			},
			new Preset {
				Name = "Stealth Suit",
				Settings = new[] {
					(C("helmet"), "Stealth"), (C("faceplate"), "Mouthless"), (C("eyes"), "Narrow"), (C("chest"), "Stealth Plates"),
					(C("shoulders"), "Slim"), (C("legs"), "Slim"), (C("boots"), "Sleek"), (C("belt"), "None"),
					(C("finish"), "Stealth"), (C("primaryColour"), "Graphite"), (C("secondaryColour"), "Black"),
					(C("accentColour"), "Gunmetal"), (C("trimColour"), "Black"), (C("eyeColour"), "Pure Red"),
					(C("reactorColour"), "Dark Red"), (C("glowStrength"), "Dim"), (C("thrusterTrail"), "None"),
					(C("plating"), "Light"), (C("thrusters"), "Hover"), (C("repulsorMode"), "Piercing")
				}
			},
			new Preset {
				Name = "Patriot",
				Settings = new[] {
					(C("chest"), "V-Stripe"), (C("primaryColour"), "Dark Blue"), (C("secondaryColour"), "White"),
					(C("accentColour"), "Rich Red"), (C("trimColour"), "Silver"), (C("emblem"), "Star"),
					(C("emblemColour"), "White"), (C("legs"), "Outer Stripe"), (C("eyeColour"), "White"),
					(C("shoulderWeapon"), "Minigun"), (C("back"), "Missile Pods")
				}
			},
			new Preset {
				Name = "Heavy Gunner",
				Settings = new[] {
					(C("helmet"), "Bulky"), (C("faceplate"), "Vent Cheeks"), (C("chest"), "Heavy"), (C("shoulders"), "Missile Pod"),
					(C("gauntlets"), "Wrist Cannon"), (C("legs"), "Heavy"), (C("boots"), "Stomper"), (C("back"), "Power Pack"),
					(C("finish"), "Matte"), (C("primaryColour"), "Gunmetal"), (C("secondaryColour"), "Graphite"),
					(C("accentColour"), "Pure Orange"), (C("trimColour"), "Black"), (C("eyeColour"), "Bright Orange"),
					(C("plating"), "Heavy"), (C("shoulderWeapon"), "Heavy Rocket"), (C("repulsorMode"), "Heavy"), (C("powerCore"), "Overclocked")
				}
			},
			new Preset {
				Name = "Deep Space",
				Settings = new[] {
					(C("helmet"), "Rounded"), (C("faceplate"), "Full Visor"), (C("eyes"), "Visor Bar"), (C("chest"), "Trimmed"),
					(C("back"), "Jetpack"), (C("boots"), "Thruster"), (C("primaryColour"), "White"), (C("secondaryColour"), "Dark Blue"),
					(C("accentColour"), "Pure Blue"), (C("trimColour"), "Silver"), (C("eyeColour"), "Pure Gold"),
					(C("thrusterColour"), "Bright Sky"), (C("thrusterTrail"), "Ion"), (C("finish"), "Gloss"), (C("thrusters"), "Hover")
				}
			},
			new Preset {
				Name = "Arctic",
				Settings = new[] {
					(C("helmet"), "Sleek"), (C("chest"), "Racing"), (C("pattern"), "Panel Lines"), (C("primaryColour"), "Pale Sky"),
					(C("secondaryColour"), "White"), (C("accentColour"), "Pure Sky"), (C("patternColour"), "Soft Sky"),
					(C("eyeColour"), "Pure Cyan"), (C("finish"), "Gloss"), (C("thrusterColour"), "Pale Cyan"), (C("thrusterTrail"), "Plasma"),
					(C("unibeamMode"), "Wide")
				}
			},
			new Preset {
				Name = "Nanotech",
				Settings = new[] {
					(C("helmet"), "Sleek"), (C("faceplate"), "Tech Lines"), (C("chest"), "Hex Core"), (C("reactor"), "Triangle"),
					(C("gauntlets"), "Repulsor Glow"), (C("legs"), "Inner Stripe"), (C("boots"), "Thruster"), (C("pattern"), "Circuit"),
					(C("primaryColour"), "Rich Red"), (C("secondaryColour"), "Pure Gold"), (C("patternColour"), "Dark Red"),
					(C("finish"), "Gloss"), (C("glowPulse"), "Slow Pulse"), (C("thrusterTrail"), "Plasma"),
					(C("powerCore"), "Efficient"), (C("thrusters"), "Afterburner"), (C("repulsorMode"), "Twin")
				}
			},
		};

		static SuitPresets() {
			foreach (SuitSet set in SuitSets.All) {
				All.Add(ForSet(set));
			}
		}
	}
}
