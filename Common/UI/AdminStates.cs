using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using MarvelMod.Common.Systems;
using MarvelMod.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MarvelMod.Common.UI
{
	// A text field that shows * for each character. Enter submits, Escape cancels.
	public class PasswordBox : UIPanel
	{
		private const int MaxLength = 32;

		public string Text = "";
		public Action<string> OnSubmit;
		public Action OnCancel;

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			PlayerInput.WritingText = true;
			Main.instance.HandleIME();
			string text = Main.GetInputText(Text);
			if (text.Length <= MaxLength) {
				Text = text;
			}
			if (Main.inputTextEnter) {
				OnSubmit?.Invoke(Text);
			}
			else if (Main.inputTextEscape) {
				OnCancel?.Invoke();
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);
			CalculatedStyle inner = GetInnerDimensions();
			bool caret = Main.GlobalTimeWrappedHourly % 1f < 0.5f;
			string shown = new string('*', Text.Length) + (caret ? "|" : "");
			Utils.DrawBorderString(spriteBatch, shown, new Vector2(inner.X + 4f, inner.Y + 2f), Color.White);
		}
	}

	// Asks for the admin password after "admin!" is typed in chat.
	public class AdminPasswordState : UIState
	{
		private UIPanel root;
		private PasswordBox box;
		private UIText message;

		public object InputOwner => box;

		public override void OnInitialize() {
			root = new UIPanel();
			root.Width.Set(360f, 0f);
			root.Height.Set(150f, 0f);
			root.HAlign = 0.5f;
			root.VAlign = 0.4f;
			root.BackgroundColor = new Color(30, 30, 40) * 0.95f;
			root.BorderColor = Color.Gold;
			Append(root);

			root.Append(new UIText("Admin password", 0.5f, large: true));

			box = new PasswordBox { OnSubmit = Submit, OnCancel = SuitWorkshopSystem.Close };
			box.Top.Set(40f, 0f);
			box.Width.Set(0f, 1f);
			box.Height.Set(36f, 0f);
			box.BackgroundColor = new Color(15, 15, 20);
			root.Append(box);

			message = new UIText("Type it and press Enter (Escape to cancel)", 0.8f);
			message.Top.Set(84f, 0f);
			root.Append(message);

			var cancel = new WorkshopButton("Cancel", SuitWorkshopSystem.Close);
			cancel.Top.Set(-34f, 1f);
			cancel.Left.Set(-90f, 1f);
			cancel.Width.Set(90f, 0f);
			root.Append(cancel);
		}

		public void Reset() {
			box.Text = "";
			message?.SetText("Type it and press Enter (Escape to cancel)");
		}

		private void Submit(string password) {
			if (AdminSystem.CheckPassword(password)) {
				SuitWorkshopSystem.OpenAdminPanel();
			}
			else {
				box.Text = "";
				message.SetText("[c/FF6060:Wrong password]");
				SoundEngine.PlaySound(SoundID.MenuClose);
			}
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			if (root.ContainsPoint(Main.MouseScreen)) {
				Main.LocalPlayer.mouseInterface = true;
			}
		}
	}

	// Admin tools for the local player: parts, items and money, and cheats.
	public class AdminPanelState : UIState
	{
		private UIPanel root;
		private UIText status;
		private readonly List<(WorkshopButton Button, Func<string> Label)> toggles = new();

		private static IronManPlayer ModPlayer => Main.LocalPlayer.GetModPlayer<IronManPlayer>();

		public override void OnInitialize() {
			root = new UIPanel();
			root.Width.Set(620f, 0f);
			root.Height.Set(420f, 0f);
			root.HAlign = 0.5f;
			root.VAlign = 0.5f;
			root.BackgroundColor = new Color(45, 20, 20) * 0.95f;
			root.BorderColor = Color.Gold;
			Append(root);

			root.Append(new UIText("Admin Panel", 0.6f, large: true));

			status = new UIText("", 0.85f);
			status.Top.Set(40f, 0f);
			root.Append(status);

			float y = 72f;
			Heading("Parts", ref y);
			Row(ref y, ("Unlock every part and weapon", UnlockAll), ("Reset purchases", ResetPurchases));

			Heading("Items and money", ref y);
			Row(ref y, ("Arc Reactor Mk I", () => Give(ModContent.ItemType<ArcReactorMk1>(), 1, "an Arc Reactor Mk I")),
				("Arc Reactor Mk II", () => Give(ModContent.ItemType<ArcReactorMk2>(), 1, "an Arc Reactor Mk II")),
				("Arc Reactor Mk III", () => Give(ModContent.ItemType<ArcReactorMk3>(), 1, "an Arc Reactor Mk III")));
			Row(ref y, ("Stark Tablet", () => Give(ModContent.ItemType<StarkTablet>(), 1, "a Stark Tablet")),
				("1 platinum", () => Give(ItemID.PlatinumCoin, 1, "1 platinum coin")),
				("10 platinum", () => Give(ItemID.PlatinumCoin, 10, "10 platinum coins")));

			Heading("Cheats (this session only)", ref y);
			Toggle(ref y, 0, () => $"God mode: {OnOff(ModPlayer.adminGodMode)}", () => ModPlayer.adminGodMode = !ModPlayer.adminGodMode);
			Toggle(ref y, 1, () => $"Super flight: {OnOff(ModPlayer.adminSuperFlight)}", () => ModPlayer.adminSuperFlight = !ModPlayer.adminSuperFlight);
			y += 40f;
			Toggle(ref y, 0, () => $"No Unibeam cooldown: {OnOff(ModPlayer.adminNoCooldowns)}", () => ModPlayer.adminNoCooldowns = !ModPlayer.adminNoCooldowns);
			var heal = new WorkshopButton("Full heal", FullHeal);
			Place(heal, 1, y);

			var close = new WorkshopButton("Close", SuitWorkshopSystem.Close);
			close.Left.Set(-100f, 1f);
			close.Top.Set(-38f, 1f);
			close.Width.Set(100f, 0f);
			root.Append(close);
		}

		private static string OnOff(bool on) => on ? "[c/7CFC00:ON]" : "[c/FF6060:OFF]";

		private void Heading(string text, ref float y) {
			var heading = new UIText(text, 0.9f);
			heading.Top.Set(y, 0f);
			root.Append(heading);
			y += 26f;
		}

		private void Place(WorkshopButton button, int column, float y) {
			button.Left.Set(column * 200f, 0f);
			button.Top.Set(y, 0f);
			button.Width.Set(194f, 0f);
			root.Append(button);
		}

		private void Row(ref float y, params (string Label, Action Action)[] buttons) {
			for (int i = 0; i < buttons.Length; i++) {
				Place(new WorkshopButton(buttons[i].Label, buttons[i].Action), i, y);
			}
			y += 40f;
		}

		private void Toggle(ref float y, int column, Func<string> label, Action flip) {
			WorkshopButton button = null;
			button = new WorkshopButton(label(), () => {
				flip();
				button.SetText(label());
			});
			Place(button, column, y);
			toggles.Add((button, label));
		}

		// Called when the panel opens, in case the player changed.
		public void Refresh() {
			status?.SetText("");
			foreach (var (button, label) in toggles) {
				button.SetText(label());
			}
		}

		private void UnlockAll() {
			int count = 0;
			foreach (ShopItem item in SuitShop.Items) {
				if (!item.Free && ModPlayer.OwnedParts.Add(item.Key)) {
					count++;
				}
			}
			status.SetText($"Unlocked {count} parts and weapons.");
		}

		private void ResetPurchases() {
			ModPlayer.OwnedParts.Clear();
			SuitPresets.Classic().CopyTo(ModPlayer.Suit);
			status.SetText("Purchases reset. Your suit is back to the classic parts.");
		}

		private void Give(int type, int stack, string name) {
			Player player = Main.LocalPlayer;
			player.QuickSpawnItem(player.GetSource_Misc("MarvelModAdmin"), type, stack);
			status.SetText($"Gave you {name}.");
		}

		private void FullHeal() {
			Player player = Main.LocalPlayer;
			player.statLife = player.statLifeMax2;
			player.statMana = player.statManaMax2;
			player.HealEffect(player.statLifeMax2);
			status.SetText("Healed.");
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			if (root.ContainsPoint(Main.MouseScreen)) {
				Main.LocalPlayer.mouseInterface = true;
			}
		}
	}
}
