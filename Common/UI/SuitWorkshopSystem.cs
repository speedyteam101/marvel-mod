using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MarvelMod.Common.UI
{
	// Opens, updates and draws the Suit Workshop.
	public class SuitWorkshopSystem : ModSystem
	{
		private static UserInterface userInterface;
		private static SuitWorkshopState state;

		public static bool Visible => userInterface?.CurrentState != null;

		public override void Load() {
			if (!Main.dedServ) {
				userInterface = new UserInterface();
				state = new SuitWorkshopState();
				state.Activate();
			}
		}

		public override void Unload() {
			userInterface = null;
			state = null;
		}

		public static void Toggle() {
			if (Visible) {
				Close();
			}
			else {
				Open();
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
			layers.Insert(index, new LegacyGameInterfaceLayer("MarvelMod: Suit Workshop", () => {
				if (Visible) {
					userInterface.Draw(Main.spriteBatch, new GameTime());
				}
				return true;
			}, InterfaceScaleType.UI));
		}
	}
}
