using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MarvelMod.Common.UI
{
	// Opens, updates and draws the Suit Workshop and the Parts Store (one at a time).
	public class SuitWorkshopSystem : ModSystem
	{
		private static UserInterface userInterface;
		private static SuitWorkshopState state;
		private static PartsStoreState store;

		public static bool Visible => userInterface?.CurrentState != null;
		public static bool WorkshopOpen => Visible && userInterface.CurrentState == state;
		public static bool StoreOpen => Visible && userInterface.CurrentState == store;

		public override void Load() {
			if (!Main.dedServ) {
				userInterface = new UserInterface();
				state = new SuitWorkshopState();
				state.Activate();
				store = new PartsStoreState();
				store.Activate();
			}
		}

		public override void Unload() {
			userInterface = null;
			state = null;
			store = null;
		}

		// Toggles the Suit Workshop. Opening it closes the Parts Store and vice versa.
		public static void Toggle() {
			if (WorkshopOpen) {
				Close();
			}
			else {
				Open();
			}
		}

		public static void ToggleStore() {
			if (StoreOpen) {
				Close();
			}
			else {
				OpenStore();
			}
		}

		public static void Open() {
			if (userInterface == null) {
				return;
			}
			state.Refresh();
			userInterface.SetState(state);
			SoundEngine.PlaySound(SoundID.MenuOpen);
		}

		public static void OpenStore() {
			if (userInterface == null) {
				return;
			}
			store.Refresh();
			userInterface.SetState(store);
			SoundEngine.PlaySound(SoundID.MenuOpen);
		}

		public static void Close() {
			if (userInterface == null) {
				return;
			}
			userInterface.SetState(null);
			SoundEngine.PlaySound(SoundID.MenuClose);
		}

		public override void UpdateUI(GameTime gameTime) {
			if (Visible) {
				userInterface.Update(gameTime);
			}
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
			if (index < 0) {
				return;
			}
			layers.Insert(index, new LegacyGameInterfaceLayer("MarvelMod: Suit Workshop and Parts Store", () => {
				if (Visible) {
					userInterface.Draw(Main.spriteBatch, new GameTime());
				}
				return true;
			}, InterfaceScaleType.UI));
		}
	}
}
