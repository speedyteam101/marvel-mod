using MarvelMod.Common.UI;
using System;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace MarvelMod.Common.Systems
{
	// Typing "admin!" in chat opens a password prompt; the right password opens the admin panel for the rest of the session.
	// Only a SHA-256 hash of the password is stored here. The mod's source is readable, so this keeps casual players out;
	// it is not real security.
	public class AdminSystem : ModSystem
	{
		public const string ChatTrigger = "admin!";

		private const string PasswordHash = "a81408883dc0754dd2cc40872fb0afb3405574a0a4a311ee7225b062a780c7f8";

		// Unlocked until the game is closed or the mod reloads.
		public static bool Unlocked { get; private set; }

		public static bool CheckPassword(string password) {
			byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(password ?? ""));
			bool correct = Convert.ToHexString(hash).Equals(PasswordHash, StringComparison.OrdinalIgnoreCase);
			if (correct) {
				Unlocked = true;
			}
			return correct;
		}

		public override void Unload() {
			Unlocked = false;
		}

		// Runs every frame after input is read and just before the game handles chat.
		public override void PostUpdateInput() {
			if (Main.dedServ) {
				return;
			}

			// While the password box is open it takes the keyboard, so Enter doesn't open the chat.
			if (SuitWorkshopSystem.PasswordPromptOpen) {
				Main.CurrentInputTextTakerOverride = SuitWorkshopSystem.PasswordPromptInputOwner;
				Main.drawingPlayerChat = false;
				return;
			}

			if (!Main.drawingPlayerChat) {
				return;
			}
			// Typed characters wait in Main.keyInt until the chat reads them later this frame. If Enter is waiting and the
			// message would be "admin!", take the input so the chat never sends it.
			string pending = Main.chatText;
			for (int i = 0; i < Main.keyCount; i++) {
				int key = Main.keyInt[i];
				if (key == 13) { // Enter
					if (pending.Trim().Equals(ChatTrigger, StringComparison.OrdinalIgnoreCase)) {
						Main.keyCount = 0;
						Main.chatText = "";
						Main.ClosePlayerChat();
						Main.chatRelease = false; // don't reopen the chat while Enter is still held
						if (Unlocked) {
							SuitWorkshopSystem.OpenAdminPanel();
						}
						else {
							SuitWorkshopSystem.OpenPasswordPrompt();
						}
					}
					return;
				}
				if (key >= 32 && key != 127) {
					pending += Main.keyString[i];
				}
			}
		}
	}
}
