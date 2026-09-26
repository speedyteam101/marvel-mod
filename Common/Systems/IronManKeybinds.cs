using Terraria.ModLoader;

namespace MarvelMod.Common.Systems
{
	// All keys can be changed in Settings > Controls > Mod Controls.
	public class IronManKeybinds : ModSystem
	{
		public static ModKeybind SuitUp { get; private set; }
		public static ModKeybind Workshop { get; private set; }
		public static ModKeybind Store { get; private set; }

		public override void Load() {
			SuitUp = KeybindLoader.RegisterKeybind(Mod, "SuitUp", "I");
			Workshop = KeybindLoader.RegisterKeybind(Mod, "SuitWorkshop", "O");
			Store = KeybindLoader.RegisterKeybind(Mod, "PartsStore", "P");
		}

		public override void Unload() {
			SuitUp = null;
			Workshop = null;
			Store = null;
		}
	}
}
