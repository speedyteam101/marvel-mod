using System.Collections.Generic;
using Terraria;

namespace MarvelMod.Common.Suits
{
	// How far into the game the player must be before the store sells something.
	public enum ShopRequirement
	{
		None,
		Hardmode,
		MechBoss,
		Plantera,
		Golem,
		Cultist,
		MoonLord
	}

	public static class ShopRequirements
	{
		public static bool Met(ShopRequirement requirement) => requirement switch {
			ShopRequirement.Hardmode => Main.hardMode,
			ShopRequirement.MechBoss => NPC.downedMechBossAny,
			ShopRequirement.Plantera => NPC.downedPlantBoss,
			ShopRequirement.Golem => NPC.downedGolemBoss,
			ShopRequirement.Cultist => NPC.downedAncientCultist,
			ShopRequirement.MoonLord => NPC.downedMoonlord,
			_ => true
		};

		public static string Text(ShopRequirement requirement) => requirement switch {
			ShopRequirement.Hardmode => "Defeat the Wall of Flesh",
			ShopRequirement.MechBoss => "Defeat a mechanical boss",
			ShopRequirement.Plantera => "Defeat Plantera",
			ShopRequirement.Golem => "Defeat Golem",
			ShopRequirement.Cultist => "Defeat the Lunatic Cultist",
			ShopRequirement.MoonLord => "Defeat the Moon Lord",
			_ => ""
		};
	}

	// A premium armour set sold in the Parts Store. Wearing every piece while suited up gives the set bonus.
	// The bonus stats are added in SuitStats; the special effects live in IronManPlayer.
	public class SuitSet
	{
		public string Name { get; init; }
		public string Bonus { get; init; }
		public ShopRequirement Requirement { get; init; }
		public long PricePerPiece { get; init; }

		// The set's own parts. Each is added to the end of its category's option list.
		public SuitPart Helmet { get; init; }
		public SuitPart Faceplate { get; init; }
		public SuitPart Eyes { get; init; }
		public SuitPart Chest { get; init; }
		public SuitPart Reactor { get; init; }
		public SuitPart Shoulders { get; init; }
		public SuitPart Gauntlets { get; init; }
		public SuitPart Legs { get; init; }
		public SuitPart Boots { get; init; }
		public string Back { get; init; } // drawn in code, see SuitRenderer.DrawBack

		// Suggested colours, applied by the set's preset.
		public (string CategoryKey, string Colour)[] Colours { get; init; }

		// Every piece as (category, option name).
		public IEnumerable<(SuitCategory Category, string Option)> Pieces() {
			yield return (SuitCatalog.Helmet, Helmet.Name);
			yield return (SuitCatalog.Faceplate, Faceplate.Name);
			yield return (SuitCatalog.Eyes, Eyes.Name);
			yield return (SuitCatalog.Chest, Chest.Name);
			yield return (SuitCatalog.Reactor, Reactor.Name);
			yield return (SuitCatalog.Shoulders, Shoulders.Name);
			yield return (SuitCatalog.Gauntlets, Gauntlets.Name);
			yield return (SuitCatalog.Legs, Legs.Name);
			yield return (SuitCatalog.Boots, Boots.Name);
			yield return (SuitCatalog.Back, Back);
		}

		public int PieceCount => 10;

		// Buying the whole set at once is 15% cheaper.
		public long SetPrice => PricePerPiece * PieceCount * 85 / 100;

		public int PiecesWorn(SuitConfig config) {
			int worn = 0;
			foreach (var (category, option) in Pieces()) {
				if (config.OptionName(category) == option) {
					worn++;
				}
			}
			return worn;
		}

		public bool IsWorn(SuitConfig config) => PiecesWorn(config) == PieceCount;
	}

	public static class SuitSets
	{
		public static readonly SuitSet Vampiric = new() {
			Name = "Vampiric",
			Bonus = "+10% damage (+20% at night). Hits drink blood, healing you. Immune to Bleeding.",
			Requirement = ShopRequirement.Hardmode,
			PricePerPiece = 6 * Gold,
			Helmet = new("Vampiric Cowl",
				"A.......A",
				"AA.....AA",
				".APPSPPA.",
				".PPPSPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				"..PPPPP.."),
			Faceplate = new("Fanged Mask", ".SSSSS.", ".SSSSS.", ".SOOOS.", "..TST.."),
			Eyes = new("Blood Slits", "EE...EE", ".EE.EE."),
			Chest = new("Bat Crest",
				".PPPPPPPPP.",
				"SSPPPPPPPSS",
				"PSSPPPPPSSP",
				"PPSSPPPSSPP",
				"PPPSSSSSPPP",
				".PPPSSSPPP.",
				".PPPPSPPPP.",
				"..PPPPPPP.."),
			Reactor = new("Blood Heart", "RR.RR", "RRRRR", ".RRR."),
			Shoulders = new("Bat Wing Pauldrons", "A...", "AAPP", "PPPP", ".PP."),
			Gauntlets = new("Talons", ".SS.", "SSS.", ".SS.", ".PP.", "A..A"),
			Legs = new("Nightstalker Greaves", "PPPPPPPPP", "PPPP.PPPP", "SPPS.SPPS", "PPPP.PPPP", "PSSP.PSSP", "PPPP.PPPP"),
			Boots = new("Bat Boots", ".SSSS.SSSS.", "ASSSS.SSSSA", "SSSSS.SSSSS"),
			Back = "Bat Wings",
			Colours = new[] {
				("primaryColour", "Deep Red"), ("secondaryColour", "Black"), ("accentColour", "Dark Red"),
				("trimColour", "Graphite"), ("undersuitColour", "Black"), ("eyeColour", "Pure Red"),
				("reactorColour", "Bright Red"), ("repulsorColour", "Pure Red"), ("thrusterColour", "Rich Red")
			}
		};

		public static readonly SuitSet Infernal = new() {
			Name = "Infernal",
			Bonus = "+15% damage. Immune to fire and lava. Hits set enemies ablaze with Hellfire.",
			Requirement = ShopRequirement.Hardmode,
			PricePerPiece = 6 * Gold,
			Helmet = new("Flame Crest",
				".A..A..A.",
				".AA.A.AA.",
				"..PAAAP..",
				".PPPAPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				"..PPPPP.."),
			Faceplate = new("Magma Grill", ".SSSSS.", ".SSSSS.", ".FFFFF.", "..FFF.."),
			Eyes = new("Ember", ".......", "EE...EE"),
			Chest = new("Magma Veins",
				".PPPPPPPPP.",
				"PPFPPPPPFPP",
				"PFPPPPPPPFP",
				"PPFPPPPPFPP",
				"PPPFPPPFPPP",
				".PPPFPFPPP.",
				".PPPPFPPPP.",
				"..PPPPPPP.."),
			Reactor = new("Hellcore", ".RRR.", "RRFRR", ".RRR."),
			Shoulders = new("Brimstone Spikes", "A.A.", "AAPP", "PPPP", ".PP."),
			Gauntlets = new("Molten Fists", ".SS.", ".SS.", ".SS.", ".FF.", ".SS."),
			Legs = new("Cinder Greaves", "PPPPPPPPP", "PPFP.PFPP", "PPPP.PPPP", "PFPP.PPFP", "PPPP.PPPP", "FPPP.PPPF"),
			Boots = new("Lava Treads", ".SSSS.SSSS.", ".SSSS.SSSS.", "FFFFF.FFFFF"),
			Back = "Flame Wings",
			Colours = new[] {
				("primaryColour", "Graphite"), ("secondaryColour", "Deep Red"), ("accentColour", "Pure Orange"),
				("trimColour", "Black"), ("eyeColour", "Bright Yellow"), ("reactorColour", "Bright Orange"),
				("repulsorColour", "Bright Orange"), ("thrusterColour", "Pure Orange")
			}
		};

		public static readonly SuitSet Titan = new() {
			Name = "Titan",
			Bonus = "+40 defense, +8% damage reduction, no knockback, reflects 50% of contact damage. 10% slower.",
			Requirement = ShopRequirement.MechBoss,
			PricePerPiece = 12 * Gold,
			Helmet = new("Titan Dome",
				"..PPPPP..",
				".PPPPPPP.",
				"PPPPPPPPP",
				"PTPPPPPTP",
				"PPPPPPPPP",
				"PPPPPPPPP",
				"PPPPPPPPP",
				"PPPPPPPPP",
				".PPPPPPP."),
			Faceplate = new("Bulwark Face", "SSSSSSS", "SSSSSSS", "SOSOSOS", "SSSSSSS"),
			Eyes = new("Titan Visor", "EEEEEEE", "EEEEEEE"),
			Chest = new("Colossus Plate",
				"PPPPPPPPPPP",
				"PPPPPPPPPPP",
				"TPPSSSSSPPT",
				"TPPSSSSSPPT",
				"TPPSSSSSPPT",
				"PPPPPPPPPPP",
				"PTTTTTTTTTP",
				".PPPPPPPPP."),
			Reactor = new("Heavy Core", "TTTTT", "TRRRT", "TTTTT"),
			Shoulders = new("Colossus Pauldrons", "SSSS", "SSSS", "SSSS", "SPPS"),
			Gauntlets = new("Siege Fists", "SSSS", "SSSS", "SSSS", "PPPP", "SSSS"),
			Legs = new("Pillar Legs", "PPPPPPPPP", "PPPPOPPPP", "TPPPOPPPT", "PPPPOPPPP", "TPPPOPPPT", "PPPPOPPPP"),
			Boots = new("Titan Stompers", "SSSSS.SSSSS", "SSSSS.SSSSS", "TTTTT.TTTTT"),
			Back = "Reactor Stacks",
			Colours = new[] {
				("primaryColour", "Dark Red"), ("secondaryColour", "Rich Gold"), ("accentColour", "Pure Gold"),
				("trimColour", "Gunmetal"), ("eyeColour", "Pale Sky"), ("reactorColour", "Pale Cyan")
			}
		};

		public static readonly SuitSet Cryo = new() {
			Name = "Cryo",
			Bonus = "+15% crit chance. Immune to Chilled, Frozen and Frostburn. Hits inflict Frostbite.",
			Requirement = ShopRequirement.Plantera,
			PricePerPiece = 15 * Gold,
			Helmet = new("Glacier Helm",
				".A.....A.",
				".AA...AA.",
				"..PPPPP..",
				".PPPPPPP.",
				"APPPPPPPA",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				"..PPPPP.."),
			Faceplate = new("Frosted Visor", "SSSSSSS", "SSSSSSS", ".SAAAS.", "..SSS.."),
			Eyes = new("Frozen Band", "EEEEEEE", "......."),
			Chest = new("Crystal Plate",
				".PPPPPPPPP.",
				"PAPPPPPPPAP",
				"PPAPPPPPAPP",
				"PPPASSSAPPP",
				"PPASSSSSAPP",
				".ASSSSSSSA.",
				".PPSSSSSPP.",
				"..PPPPPPP.."),
			Reactor = new("Snowflake", ".R.R.", "RRRRR", ".R.R."),
			Shoulders = new("Icicle Pauldrons", "PSSS", "SSSS", "A.A.", "A..."),
			Gauntlets = new("Frost Bracers", ".AA.", "SSSS", ".SS.", ".SS.", ".PP."),
			Legs = new("Glacial Greaves", "PPPPPPPPP", "PPPP.PPPP", "APPA.APPA", "PPPP.PPPP", "SSSS.SSSS", "PPPP.PPPP"),
			Boots = new("Snow Treads", ".SSSS.SSSS.", "SSSSS.SSSSS", "AAAAA.AAAAA"),
			Back = "Crystal Shards",
			Colours = new[] {
				("primaryColour", "Soft Sky"), ("secondaryColour", "White"), ("accentColour", "Bright Cyan"),
				("trimColour", "Silver"), ("eyeColour", "Pale Cyan"), ("reactorColour", "Bright Cyan"),
				("repulsorColour", "Pale Cyan"), ("thrusterColour", "Pale Sky")
			}
		};

		public static readonly SuitSet Storm = new() {
			Name = "Storm",
			Bonus = "+25% move and flight speed. Hits have a 25% chance to arc lightning to another enemy.",
			Requirement = ShopRequirement.Golem,
			PricePerPiece = 20 * Gold,
			Helmet = new("Thunder Crest",
				"....AA...",
				"...AA....",
				"..PPAAP..",
				".PPPPAPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				"..PPPPP.."),
			Faceplate = new("Storm Guard", ".SSSSS.", "SSSSSSS", ".SAAAS.", "..SAS.."),
			Eyes = new("Lightning", ".E...E.", "EEE.EEE"),
			Chest = new("Bolt Plate",
				".PPPPPPPPP.",
				"PPPPPPAAPPP",
				"PPPPPAAPPPP",
				"PPPPAAAAPPP",
				"PPPPPPAAPPP",
				".PPPPAAPPP.",
				".PPPAAPPPP.",
				"..PPPPPPP.."),
			Reactor = new("Arc Coil", "RTRTR", "TRRRT", "RTRTR"),
			Shoulders = new("Tesla Coils", "..A.", ".TTT", "PPPP", ".PP."),
			Gauntlets = new("Arc Gauntlets", ".SS.", ".HS.", ".SH.", ".SS.", ".HH."),
			Legs = new("Surge Greaves", "PPPPPPPPP", "PAPP.PPAP", "PPAP.PAPP", "PAPP.PPAP", "PPAP.PAPP", "PPPP.PPPP"),
			Boots = new("Static Boots", ".SSSS.SSSS.", ".AAAA.AAAA.", "SSSSS.SSSSS"),
			Back = "Tesla Array",
			Colours = new[] {
				("primaryColour", "Dark Blue"), ("secondaryColour", "Gunmetal"), ("accentColour", "Bright Yellow"),
				("trimColour", "Silver"), ("eyeColour", "Pale Cyan"), ("reactorColour", "Bright Sky"),
				("repulsorColour", "Pale Sky"), ("thrusterColour", "Bright Sky")
			}
		};

		public static readonly SuitSet Void = new() {
			Name = "Void",
			Bonus = "+20% damage. 12% chance to phase through attacks. Immune to Darkness, Blackout and Obstructed.",
			Requirement = ShopRequirement.Cultist,
			PricePerPiece = 30 * Gold,
			Helmet = new("Void Hood",
				".........",
				"..PPPPP..",
				".PPPPPPP.",
				"PPPPPPPPP",
				"PPPPPPPPP",
				"PPPPPPPPP",
				"PPPPPPPPP",
				".PPPPPPP.",
				"..PPPPP.."),
			Faceplate = new("Abyss", "OOOOOOO", "OOOOOOO", ".OOOOO.", "..OOO.."),
			Eyes = new("Hollow Eyes", ".E...E.", ".E...E."),
			Chest = new("Rift Plate",
				".PPPPPPPPP.",
				"PPPPPOPPPPP",
				"PPPPOOOPPPP",
				"PPPOOOOOPPP",
				"PPPPOOOPPPP",
				".PPPPOPPPP.",
				".PPPPPPPPP.",
				"..PPPPPPP.."),
			Reactor = new("Singularity", ".OOO.", "OOROO", ".OOO."),
			Shoulders = new("Shade Mantle", "PPPP", "PPPP", "PPP.", "PP.."),
			Gauntlets = new("Shadow Grips", ".PP.", ".PP.", ".OO.", ".PP.", "O..O"),
			Legs = new("Phase Greaves", "PPPPPPPPP", "PPPP.PPPP", "PPPP.PPPP", "POPP.PPOP", "PPPP.PPPP", ".PP...PP."),
			Boots = new("Wisp Boots", ".PPPP.PPPP.", "..PPP.PPP..", "...E...E..."),
			Back = "Void Tendrils",
			Colours = new[] {
				("primaryColour", "Deep Violet"), ("secondaryColour", "Black"), ("accentColour", "Pure Violet"),
				("trimColour", "Graphite"), ("undersuitColour", "Black"), ("eyeColour", "Bright Magenta"),
				("reactorColour", "Soft Violet"), ("repulsorColour", "Bright Violet"), ("thrusterColour", "Pure Violet")
			}
		};

		public static readonly SuitSet Godly = new() {
			Name = "Godly",
			Bonus = "+30% damage, +10% crit, +30 defense, +10% damage reduction, +8 HP/s regen, +30% flight speed. "
				+ "Immune to most debuffs. Hits can call down holy light.",
			Requirement = ShopRequirement.MoonLord,
			PricePerPiece = 50 * Gold,
			Helmet = new("Divine Crown",
				"A.A.A.A.A",
				"AAAAAAAAA",
				".TPPPPPT.",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				"..PPPPP.."),
			Faceplate = new("Serene Mask", "SSSSSSS", ".SSSSS.", ".SSASS.", "..SSS.."),
			Eyes = new("Radiant Gaze", ".EE.EE.", "EEE.EEE"),
			Chest = new("Sunburst",
				".PPPPPPPPP.",
				"PAPPSSSPPAP",
				"PPASSSSSAPP",
				"PSSSSSSSSSP",
				"PPASSSSSAPP",
				".APPSSSPPA.",
				".PPPPSPPPP.",
				"..PPPPPPP.."),
			Reactor = new("Star Core", "R.R.R", ".RRR.", "R.R.R"),
			Shoulders = new("Seraph Pauldrons", "A.A.", "SSSS", "SSSP", ".PP."),
			Gauntlets = new("Hands of Light", ".AA.", ".SS.", ".SS.", ".SS.", ".HH."),
			Legs = new("Divine Greaves", "PPPPPPPPP", "SPPS.SPPS", "SPPS.SPPS", "ASSA.ASSA", "SPPS.SPPS", "SPPS.SPPS"),
			Boots = new("Winged Sandals", "ASSSS.SSSSA", ".SSSS.SSSS.", "SSSSS.SSSSS"),
			Back = "Angel Wings",
			Colours = new[] {
				("primaryColour", "White"), ("secondaryColour", "Pure Gold"), ("accentColour", "Bright Yellow"),
				("trimColour", "Champagne"), ("undersuitColour", "Ivory"), ("eyeColour", "Pale Yellow"),
				("reactorColour", "Pale Gold"), ("repulsorColour", "Pale Yellow"), ("thrusterColour", "Bright Gold")
			}
		};

		public static readonly SuitSet Dragon = new() {
			Name = "Dragon",
			Bonus = "+20% damage, +20% flight speed. Immune to fire and lava. Hits inflict Daybreak, and 15% of hits make you breathe fire at the target.",
			Requirement = ShopRequirement.Plantera,
			PricePerPiece = 18 * Gold,
			Helmet = new("Dragon Helm",
				"A......A.",
				"AA....AA.",
				".APPPPPA.",
				".PPPPPPP.",
				"SPPPPPPPS",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				"..PPPPP.."),
			Faceplate = new("Dragon Snout", "SSSSSSS", ".SSSSS.", ".STSTS.", "..TST.."),
			Eyes = new("Serpent Eyes", ".E...E.", "EE...EE"),
			Chest = new("Scale Plate",
				".PPPPPPPPP.",
				"PSPSPSPSPSP",
				"PPSPSPSPSPP",
				"PSPSPSPSPSP",
				"PPSPSPSPSPP",
				".SPSPSPSPS.",
				".PPSPSPSPP.",
				"..PPPPPPP.."),
			Reactor = new("Dragon Heart", ".RRR.", "RRRRR", "..R.."),
			Shoulders = new("Spined Pauldrons", "A.A.", "PPPP", "SSSS", ".PP."),
			Gauntlets = new("Talon Gauntlets", ".SS.", ".SS.", ".PP.", ".SS.", "AAAA"),
			Legs = new("Scaled Greaves", "PPPPPPPPP", "SPSP.PSPS", "PSPS.SPSP", "SPSP.PSPS", "PSPS.SPSP", "PPPP.PPPP"),
			Boots = new("Clawed Boots", ".SSSS.SSSS.", ".SSSS.SSSS.", "ASSSA.ASSSA"),
			Back = "Dragon Wings",
			Colours = new[] {
				("primaryColour", "Dark Green"), ("secondaryColour", "Rich Gold"), ("accentColour", "Bright Gold"),
				("trimColour", "Bronze"), ("undersuitColour", "Deep Green"), ("eyeColour", "Bright Yellow"),
				("reactorColour", "Bright Orange"), ("repulsorColour", "Bright Orange"), ("thrusterColour", "Pure Orange")
			}
		};

		public static readonly SuitSet Samurai = new() {
			Name = "Samurai",
			Bonus = "+20% melee damage, +15% melee speed, +10% crit. The Katana deals 25% more damage.",
			Requirement = ShopRequirement.MechBoss,
			PricePerPiece = 12 * Gold,
			Helmet = new("Kabuto Helm",
				"A.......A",
				".AA...AA.",
				"..AAAAA..",
				".TPPPPPT.",
				"TPPPPPPPT",
				".PPPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				"..PPPPP.."),
			Faceplate = new("Menpo Mask", ".SSSSS.", ".OSSSO.", ".SSSSS.", ".TTTTT."),
			Eyes = new("Ronin Slits", ".......", "EE.E.EE"),
			Chest = new("Lamellar Do",
				".PPPPPPPPP.",
				"TTTTTTTTTTT",
				"PPPPPPPPPPP",
				"TTTTTTTTTTT",
				"PPPPPPPPPPP",
				".TTTTTTTTT.",
				".PPPPPPPPP.",
				"..TTTTTTT.."),
			Reactor = new("Mon Crest", ".TRT.", "RTRTR", ".TRT."),
			Shoulders = new("Sode Plates", "TTTT", "PPPP", "TTTT", "PPPP"),
			Gauntlets = new("Kote Bracers", ".TT.", ".PP.", ".TT.", ".PP.", ".SS."),
			Legs = new("Haidate Guards", "TTTTTTTTT", "PPPP.PPPP", "TTTT.TTTT", "PPPP.PPPP", "PSSP.PSSP", "PPPP.PPPP"),
			Boots = new("Suneate Boots", ".TTTT.TTTT.", ".SSSS.SSSS.", "SSSSS.SSSSS"),
			Back = "Sashimono Banner",
			Colours = new[] {
				("primaryColour", "Rich Red"), ("secondaryColour", "Black"), ("accentColour", "Pure Gold"),
				("trimColour", "Gunmetal"), ("undersuitColour", "Black"), ("eyeColour", "White"),
				("reactorColour", "Pale Gold"), ("repulsorColour", "Pale Red")
			}
		};

		public static readonly SuitSet Shinobi = new() {
			Name = "Shinobi",
			Bonus = "+20% move speed, +15% crit. 8% chance to dodge attacks. Enemies are less likely to target you.",
			Requirement = ShopRequirement.Hardmode,
			PricePerPiece = 6 * Gold,
			Helmet = new("Shinobi Hood",
				".........",
				"..PPPPP..",
				".PPPPPPP.",
				"APPPPPPP.",
				"AAPPPPPP.",
				"A.PPPPPP.",
				".PPPPPPP.",
				".PPPPPPP.",
				"..PPPPP.."),
			Faceplate = new("Shinobi Wrap", ".......", ".......", "SSSSSSS", ".SSSSS."),
			Eyes = new("Shadow Slit", ".......", "..EEE.."),
			Chest = new("Wrapped Gi",
				".PPPPPPPPP.",
				"PPSPPPPPPPP",
				"PPPSPPPPPPP",
				"PPPPSPPPPPP",
				"PPPPPSPPPPP",
				".TTTTTTTTT.",
				".PPPPPPPPP.",
				"..PPPPPPP.."),
			Reactor = new("Shuriken Core", "R...R", ".RRR.", "R...R"),
			Shoulders = new("Wrap Guards", "..PP", ".PPP", ".PPP", ".PP."),
			Gauntlets = new("Hand Wraps", ".SS.", ".PP.", ".SS.", ".PP.", ".SS."),
			Legs = new("Tabi Legs", "PPPPPPPPP", "PPPP.PPPP", "PPPP.PPPP", "SSSS.SSSS", "PPPP.PPPP", "PPPP.PPPP"),
			Boots = new("Tabi Boots", ".SSSS.SSSS.", ".SSSS.SSSS.", ".SSSS.SSSS."),
			Back = "Twin Scabbards",
			Colours = new[] {
				("primaryColour", "Graphite"), ("secondaryColour", "Deep Blue"), ("accentColour", "Pure Red"),
				("trimColour", "Deep Red"), ("undersuitColour", "Black"), ("eyeColour", "Pale Sky"),
				("reactorColour", "Soft Sky"), ("thrusterTrail", "Smoke")
			}
		};

		public static readonly SuitSet Pharaoh = new() {
			Name = "Pharaoh",
			Bonus = "+12% damage, +20 defense. Hits inflict Venom and Ichor. Immune to Poisoned, Venom, Slow and Weak.",
			Requirement = ShopRequirement.Plantera,
			PricePerPiece = 15 * Gold,
			Helmet = new("Nemes Crown",
				"....A....",
				"..SSSSS..",
				".SPSPSPS.",
				"SPSPSPSPS",
				"SPPPPPPPS",
				"SPPPPPPPS",
				"SPPPPPPPS",
				"S.PPPPP.S",
				"S.PPPPP.S"),
			Faceplate = new("Golden Mask", "SSSSSSS", "SSSSSSS", ".SSSSS.", "..SAS.."),
			Eyes = new("Kohl Eyes", ".OO.OO.", ".EE.EE."),
			Chest = new("Pectoral Collar",
				".SSSSSSSSS.",
				"SASASASASAS",
				"PSSSSSSSSSP",
				"PPPPPPPPPPP",
				"PPPPPPPPPPP",
				".PPPPPPPPP.",
				".PPPSSSPPP.",
				"..PPPPPPP.."),
			Reactor = new("Scarab", ".RRR.", "RRRRR", "R.R.R"),
			Shoulders = new("Sun Discs", ".SS.", "SAAS", "SAAS", ".SS."),
			Gauntlets = new("Cuff Bands", ".SS.", ".PP.", ".PP.", ".SS.", ".PP."),
			Legs = new("Linen Wraps", "SSSSSSSSS", "PPPP.PPPP", "PSPP.PPSP", "PPPP.PPPP", "PPSP.PSPP", "PPPP.PPPP"),
			Boots = new("Sandals of Ra", ".PPPP.PPPP.", ".SSSS.SSSS.", "SSSSS.SSSSS"),
			Back = "Sun Disc",
			Colours = new[] {
				("primaryColour", "Ivory"), ("secondaryColour", "Pure Gold"), ("accentColour", "Rich Blue"),
				("trimColour", "Bronze"), ("undersuitColour", "Champagne"), ("eyeColour", "Bright Sky"),
				("reactorColour", "Bright Cyan"), ("repulsorColour", "Pale Gold")
			}
		};

		public static readonly SuitSet Cyber = new() {
			Name = "Cyber",
			Bonus = "+15% damage, +15% attack speed. Every 5th hit launches a homing missile.",
			Requirement = ShopRequirement.Cultist,
			PricePerPiece = 30 * Gold,
			Helmet = new("Neon Visor Helm",
				".........",
				"..PPPPP..",
				".PPPPPPP.",
				".PPPPPPPA",
				".PPPPPPP.",
				".PPPPPPP.",
				"TPPPPPPP.",
				".PPPPPPP.",
				"..PPPPP.."),
			Faceplate = new("Holo Visor", "HHHHHHH", "HHHHHHH", ".SSSSS.", "..SSS.."),
			Eyes = new("Scanline", "E.E.E.E", ".E.E.E."),
			Chest = new("Circuit Plate",
				".PPPPPPPPP.",
				"PHPPPPPPPHP",
				"PHPPPPPPPHP",
				"PHHHPPPHHHP",
				"PPPHPPPHPPP",
				".PPHHHHHPP.",
				".PPPPPPPPP.",
				"..PPPPPPP.."),
			Reactor = new("Data Core", "RRRRR", "RHRHR", "RRRRR"),
			Shoulders = new("Neon Pads", ".PPP", "HPPP", "PPPP", ".PH."),
			Gauntlets = new("Holo Gauntlets", ".SS.", ".HS.", ".SS.", ".SH.", ".SS."),
			Legs = new("Light Strips", "PPPPPPPPP", "PHPP.PPHP", "PHPP.PPHP", "PHPP.PPHP", "PHPP.PPHP", "PPPP.PPPP"),
			Boots = new("Hover Boots", ".SSSS.SSSS.", ".SSSS.SSSS.", ".HHH...HHH."),
			Back = "Data Wings",
			Colours = new[] {
				("primaryColour", "Black"), ("secondaryColour", "Graphite"), ("accentColour", "Bright Magenta"),
				("trimColour", "Gunmetal"), ("undersuitColour", "Black"), ("eyeColour", "Bright Magenta"),
				("reactorColour", "Bright Cyan"), ("repulsorColour", "Bright Cyan"), ("thrusterColour", "Bright Magenta")
			}
		};

		private const long Gold = 10000;

		// Only ever add sets to the end: their pieces are appended to the option lists in this order, and saved suits store option positions.
		public static readonly SuitSet[] All = { Vampiric, Infernal, Titan, Cryo, Storm, Void, Godly, Dragon, Samurai, Shinobi, Pharaoh, Cyber };

		// The set whose every piece is in this design, if any.
		public static SuitSet WornBy(SuitConfig config) {
			foreach (SuitSet set in All) {
				if (set.IsWorn(config)) {
					return set;
				}
			}
			return null;
		}

		// The set a single option belongs to, if any.
		public static SuitSet Owning(SuitCategory category, string option) {
			foreach (SuitSet set in All) {
				foreach (var piece in set.Pieces()) {
					if (piece.Category == category && piece.Option == option) {
						return set;
					}
				}
			}
			return null;
		}

		public static SuitPart[] Parts(System.Func<SuitSet, SuitPart> part) {
			var parts = new SuitPart[All.Length];
			for (int i = 0; i < All.Length; i++) {
				parts[i] = part(All[i]);
			}
			return parts;
		}
	}
}
