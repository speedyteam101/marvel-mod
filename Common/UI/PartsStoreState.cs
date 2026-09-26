using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace MarvelMod.Common.UI
{
	// The Parts Store: buy suit parts with coins. Pick something on the left to try it on in the preview.
	public class PartsStoreState : UIState
	{
		private enum Tab
		{
			Sets,
			Armour,
			Paint,
			Systems,
			Weapons
		}

		private const float ListTop = 108f;

		private UIPanel root;
		private UIText moneyText;
		private UIText statusText;
		private UIList itemList;
		private UIText detailsText;
		private WorkshopButton buyButton;
		private WorkshopButton buySetButton;
		private WorkshopButton equipButton;
		private readonly List<(Tab Tab, WorkshopButton Button)> tabButtons = new();
		private readonly List<(WorkshopButton Button, ShopItem Item, SuitSet Set)> rows = new();

		private Tab tab = Tab.Sets;
		private ShopItem selectedItem;
		private SuitSet selectedSet = SuitSets.Vampiric;
		private bool giantSelected;
		private WorkshopButton giantButton;

		private static IronManPlayer ModPlayer => Main.LocalPlayer.GetModPlayer<IronManPlayer>();

		public override void OnInitialize() {
			root = new UIPanel();
			root.Width.Set(900f, 0f);
			root.Height.Set(600f, 0f);
			root.HAlign = 0.5f;
			root.VAlign = 0.5f;
			root.BackgroundColor = new Color(40, 28, 30) * 0.95f;
			root.BorderColor = new Color(220, 170, 60);
			Append(root);

			root.Append(new UIText("Stark Industries Parts Store", 0.6f, large: true));

			moneyText = new UIText("", 0.9f);
			moneyText.Left.Set(560f, 0f);
			moneyText.Top.Set(8f, 0f);
			root.Append(moneyText);

			statusText = new UIText("", 0.85f);
			statusText.Top.Set(40f, 0f);
			root.Append(statusText);

			float x = 0f;
			foreach (var (t, label) in new[] { (Tab.Sets, "Premium Sets"), (Tab.Weapons, "Weapons"), (Tab.Armour, "Armour"), (Tab.Paint, "Paint & Effects"), (Tab.Systems, "Systems") }) {
				Tab captured = t;
				var button = new WorkshopButton(label, () => SelectTab(captured), 0.9f);
				button.Left.Set(x, 0f);
				button.Top.Set(66f, 0f);
				button.Width.Set(150f, 0f);
				root.Append(button);
				tabButtons.Add((t, button));
				x += 156f;
			}

			itemList = new UIList { ListPadding = 3f, ManualSortMethod = _ => { } };
			itemList.Top.Set(ListTop, 0f);
			itemList.Width.Set(390f, 0f);
			itemList.Height.Set(-ListTop - 50f, 1f);
			root.Append(itemList);

			var scrollbar = new UIScrollbar();
			scrollbar.Left.Set(394f, 0f);
			scrollbar.Top.Set(ListTop, 0f);
			scrollbar.Height.Set(-ListTop - 50f, 1f);
			itemList.SetScrollbar(scrollbar);
			root.Append(scrollbar);

			var preview = new SuitPreview(TryOn);
			preview.Left.Set(420f, 0f);
			preview.Top.Set(ListTop, 0f);
			preview.Width.Set(200f, 0f);
			preview.Height.Set(230f, 0f);
			preview.BackgroundColor = new Color(70, 60, 60) * 0.9f;
			root.Append(preview);

			var tryOnLabel = new UIText("Try-on preview", 0.75f);
			tryOnLabel.Left.Set(420f, 0f);
			tryOnLabel.Top.Set(ListTop + 234f, 0f);
			root.Append(tryOnLabel);

			buyButton = AddAction(ListTop + 262f, Buy);
			buySetButton = AddAction(ListTop + 306f, BuySet);
			equipButton = AddAction(ListTop + 350f, Equip);

			detailsText = new UIText("", 0.8f) { IsWrapped = true };
			detailsText.Left.Set(636f, 0f);
			detailsText.Top.Set(ListTop, 0f);
			detailsText.Width.Set(240f, 0f);
			detailsText.Height.Set(400f, 0f);
			root.Append(detailsText);

			var workshop = new WorkshopButton("Suit Workshop", SuitWorkshopSystem.Open);
			workshop.Left.Set(-236f, 1f);
			workshop.Top.Set(-38f, 1f);
			workshop.Width.Set(130f, 0f);
			root.Append(workshop);

			var close = new WorkshopButton("Close", SuitWorkshopSystem.Close);
			close.Left.Set(-100f, 1f);
			close.Top.Set(-38f, 1f);
			close.Width.Set(100f, 0f);
			root.Append(close);
		}

		private WorkshopButton AddAction(float top, System.Action action) {
			var button = new WorkshopButton("", action, 0.8f);
			button.Left.Set(420f, 0f);
			button.Top.Set(top, 0f);
			button.Width.Set(200f, 0f);
			root.Append(button);
			return button;
		}

		public void Refresh() {
			statusText?.SetText("");
			SelectTab(tab);
		}

		private void SelectTab(Tab newTab) {
			tab = newTab;
			float view = itemList.ViewPosition;
			itemList.Clear();
			rows.Clear();

			giantButton = null;
			if (tab == Tab.Sets) {
				giantButton = new WorkshopButton("", SelectGiant);
				giantButton.Width.Set(0f, 1f);
				itemList.Add(giantButton);
				foreach (SuitSet set in SuitSets.All) {
					AddRow(SetLabel(set), null, set);
					foreach (var (category, option) in set.Pieces()) {
						AddRow("    " + ItemLabel(SuitShop.Get(category, category.IndexOf(option)), true), SuitShop.Get(category, category.IndexOf(option)), null);
					}
				}
			}
			else {
				SuitCategory lastCategory = null;
				foreach (ShopItem item in SuitShop.Items) {
					if (item.Free || item.Hidden || item.Set != null || TabOf(item.Category) != tab) {
						continue;
					}
					if (item.Category != lastCategory) {
						lastCategory = item.Category;
						var header = new WorkshopButton(item.Category.Name, null, 0.85f, header: true);
						header.Width.Set(0f, 1f);
						itemList.Add(header);
					}
					AddRow(ItemLabel(item, false), item, null);
				}
			}
			itemList.Recalculate();
			itemList.ViewPosition = view;
			UpdateLabels();
		}

		private static Tab TabOf(SuitCategory category) => category.Group switch {
			SuitGroup.Armour => Tab.Armour,
			SuitGroup.Systems => Tab.Systems,
			SuitGroup.Weapons => Tab.Weapons,
			_ => Tab.Paint
		};

		private void AddRow(string label, ShopItem item, SuitSet set) {
			var button = new WorkshopButton(label, () => Select(item, set));
			button.Width.Set(0f, 1f);
			rows.Add((button, item, set));
			itemList.Add(button);
		}

		private static string SetLabel(SuitSet set) {
			int owned = ModPlayer.SetPiecesOwned(set);
			string status = owned == set.PieceCount ? "[c/7CFC00:owned]" : $"{owned}/{set.PieceCount} owned";
			return $"[c/FFD700:{set.Name} Set]  {status}";
		}

		private static string ItemLabel(ShopItem item, bool showCategory) {
			string name = showCategory ? $"{item.Category.Name}: {item.Name}" : item.Name;
			return $"{name}  {PriceTag(item)}";
		}

		private static string PriceTag(ShopItem item) {
			if (ModPlayer.Owns(item.Category, item.Index)) {
				return "[c/7CFC00:owned]";
			}
			if (!ShopRequirements.Met(item.Requirement)) {
				return "[c/FF6060:not yet]";
			}
			return SuitShop.FormatPrice(item.Price);
		}

		private void SelectGiant() {
			giantSelected = true;
			selectedItem = null;
			selectedSet = null;
			statusText.SetText("");
			UpdateLabels();
		}

		private static string GiantLabel() {
			string status = ModPlayer.OwnsGiantSuit ? "[c/7CFC00:owned]" : SuitShop.FormatPrice(IronManPlayer.GiantSuitPrice);
			return $"[c/FF9030:GIANT SUIT]  {status}";
		}

		private static string GiantDetails() {
			return "[c/FF9030:Giant Suit]\n"
				+ (ModPlayer.OwnsGiantSuit ? "[c/7CFC00:Owned]" : $"Price: {SuitShop.FormatPrice(IronManPlayer.GiantSuitPrice)}")
				+ "\n\nWhile suited up, press the Giant Suit key (G) to become a giant version of your suit, twice the size."
				+ "\n+80 defense, +25% damage reduction, +50% damage and no knockback, but slower."
				+ "\n\nNine giant abilities replace your normal ones: Titan Punch, Ground Pound, Mega Repulsor, Missile Barrage,"
				+ " Giant Unibeam, Shockwave Clap, Rocket Charge, Shield Dome and Orbital Strike.";
		}

		private void Select(ShopItem item, SuitSet set) {
			giantSelected = false;
			selectedItem = item;
			selectedSet = set;
			statusText.SetText("");
			UpdateLabels();
		}

		// The player's suit with the selection applied, for the preview. A whole set is shown in its own colours.
		private SuitConfig TryOn() {
			SuitConfig design = ModPlayer.Suit.Clone();
			if (selectedSet != null) {
				foreach (var (category, option) in selectedSet.Pieces()) {
					design.Set(category, option);
				}
				foreach (var (key, colour) in selectedSet.Colours) {
					design.Set(SuitCatalog.ByKey(key), colour);
				}
			}
			else if (selectedItem != null) {
				design[selectedItem.Category] = selectedItem.Index;
			}
			return design;
		}

		// The set the current selection belongs to (the set itself, or the set of the selected piece).
		private SuitSet ContextSet => selectedSet ?? selectedItem?.Set;

		private void UpdateLabels() {
			moneyText.SetText($"Your money: {SuitShop.FormatPrice(TotalMoney(Main.LocalPlayer))}");

			foreach (var (t, button) in tabButtons) {
				button.Selected = t == tab;
			}
			foreach (var (button, item, set) in rows) {
				button.Selected = (item != null && item == selectedItem) || (set != null && set == selectedSet);
				if (item != null) {
					button.Locked = !ModPlayer.Owns(item.Category, item.Index);
					button.SetText((tab == Tab.Sets ? "    " : "") + ItemLabel(item, tab == Tab.Sets));
				}
				else {
					button.SetText(SetLabel(set));
				}
			}

			if (giantButton != null) {
				giantButton.SetText(GiantLabel());
				giantButton.Selected = giantSelected;
				giantButton.Locked = !ModPlayer.OwnsGiantSuit;
			}
			if (giantSelected) {
				buyButton.SetText(ModPlayer.OwnsGiantSuit ? "Owned" : $"Buy: {SuitShop.FormatPrice(IronManPlayer.GiantSuitPrice)}");
				buySetButton.SetText("(no set selected)");
				equipButton.SetText(ModPlayer.giantForm ? "Turn back to normal size" : "Transform (G)");
				detailsText.SetText(GiantDetails());
				return;
			}

			SuitSet context = ContextSet;
			if (context != null && ModPlayer.SetPrice(context) > 0) {
				buySetButton.SetText($"Buy whole set: {SuitShop.FormatPrice(ModPlayer.SetPrice(context))}");
			}
			else {
				buySetButton.SetText(context != null ? "Set owned" : "(no set selected)");
			}

			if (selectedSet != null) {
				buyButton.SetText("Select a piece to buy it alone");
				equipButton.SetText("Equip whole set");
				detailsText.SetText(SetDetails(selectedSet));
			}
			else if (selectedItem != null) {
				bool owned = ModPlayer.Owns(selectedItem.Category, selectedItem.Index);
				buyButton.SetText(owned ? "Owned" : $"Buy: {SuitShop.FormatPrice(selectedItem.Price)}");
				equipButton.SetText("Equip");
				detailsText.SetText(ItemDetails(selectedItem));
			}
			else {
				buyButton.SetText("Select something to buy");
				equipButton.SetText("Equip");
				detailsText.SetText("");
			}
		}

		private static string RequirementLine(ShopRequirement requirement) {
			if (requirement == ShopRequirement.None) {
				return "";
			}
			string colour = ShopRequirements.Met(requirement) ? "7CFC00" : "FF6060";
			return $"\n[c/{colour}:Requires: {ShopRequirements.Text(requirement)}]";
		}

		private static string SetDetails(SuitSet set) {
			return $"[c/FFD700:{set.Name} Set]\n"
				+ $"{ModPlayer.SetPiecesOwned(set)}/{set.PieceCount} pieces owned\n"
				+ $"{SuitShop.FormatPrice(set.PricePerPiece)} per piece, or 15% off when buying the rest of the set"
				+ RequirementLine(set.Requirement)
				+ $"\n\n[c/FFD700:Set bonus] (wear all {set.PieceCount} pieces while suited up):\n{set.Bonus}"
				+ $"\n\nLoad the \"{set.Name} Set\" preset in the Suit Workshop to also get its colours.";
		}

		private static string ItemDetails(ShopItem item) {
			string text = $"{item.Name}\n[c/B0B0B0:{item.Category.Name}]";
			text += ModPlayer.Owns(item.Category, item.Index) ? "\n[c/7CFC00:Owned]" : $"\nPrice: {SuitShop.FormatPrice(item.Price)}";
			text += RequirementLine(item.Requirement);
			if (item.Category.Group == SuitGroup.Weapons) {
				WeaponDef weapon = SuitWeapons.All[item.Index];
				string type = weapon.Melee ? "melee" : "ranged";
				text += $"\n\n{weapon.Description}\n{weapon.Damage} base damage ({type}), use time {weapon.UseTime}"
					+ "\n\nAttach it to either weapon slot. While suited up, use it with the Weapon I or Weapon II ability.";
			}
			else if (item.Category.Description != null) {
				text += $"\n\n{item.Category.Description}";
			}
			if (item.Set != null) {
				text += $"\n\nPart of the [c/FFD700:{item.Set.Name} Set]. Wear all {item.Set.PieceCount} pieces for the set bonus:\n{item.Set.Bonus}";
			}
			return text;
		}

		// Coins in the inventory and all banks, as Player.CanAfford counts them.
		private static long TotalMoney(Player player) {
			long inventory = Utils.CoinsCount(out _, player.inventory, 58, 57, 56, 55, 54);
			long bank = Utils.CoinsCount(out _, player.bank.item);
			long bank2 = Utils.CoinsCount(out _, player.bank2.item);
			long bank3 = Utils.CoinsCount(out _, player.bank3.item);
			long bank4 = Utils.CoinsCount(out _, player.bank4.item);
			return Utils.CoinsCombineStacks(out _, inventory, bank, bank2, bank3, bank4);
		}

		private void Buy() {
			if (giantSelected) {
				statusText.SetText(ModPlayer.BuyGiantSuit());
				SelectTab(tab);
				return;
			}
			if (selectedItem == null) {
				return;
			}
			statusText.SetText(ModPlayer.Buy(selectedItem));
			SelectTab(tab);
		}

		private void BuySet() {
			SuitSet set = ContextSet;
			if (set == null) {
				return;
			}
			statusText.SetText(ModPlayer.BuySet(set));
			SelectTab(tab);
		}

		private void Equip() {
			IronManPlayer modPlayer = ModPlayer;
			if (giantSelected) {
				string problem = modPlayer.ToggleGiant();
				statusText.SetText(problem ?? (modPlayer.giantForm ? "You are now the giant suit." : "Back to normal size."));
				UpdateLabels();
				return;
			}
			if (selectedSet != null) {
				int missing = selectedSet.PieceCount - modPlayer.SetPiecesOwned(selectedSet);
				if (missing > 0) {
					statusText.SetText($"You still need {missing} piece{(missing == 1 ? "" : "s")} of this set.");
					return;
				}
				foreach (var (category, option) in selectedSet.Pieces()) {
					modPlayer.Suit.Set(category, option);
				}
				statusText.SetText($"Equipped the {selectedSet.Name} set.");
			}
			else if (selectedItem != null) {
				if (!modPlayer.Owns(selectedItem.Category, selectedItem.Index)) {
					statusText.SetText("Buy it first.");
					return;
				}
				if (selectedItem.Category.Group == SuitGroup.Weapons) {
					// Fill an empty weapon slot first; if both are full, replace slot I.
					SuitCategory slot = modPlayer.Suit[SuitCatalog.WeaponSlot1] == 0 || modPlayer.Suit[SuitCatalog.WeaponSlot2] != 0
						? SuitCatalog.WeaponSlot1 : SuitCatalog.WeaponSlot2;
					if (modPlayer.Suit[SuitCatalog.WeaponSlot1] == selectedItem.Index || modPlayer.Suit[SuitCatalog.WeaponSlot2] == selectedItem.Index) {
						statusText.SetText($"{selectedItem.Name} is already attached.");
						return;
					}
					modPlayer.Suit[slot] = selectedItem.Index;
					statusText.SetText($"Attached {selectedItem.Name} to {slot.Name}.");
					UpdateLabels();
					return;
				}
				modPlayer.Suit[selectedItem.Category] = selectedItem.Index;
				statusText.SetText($"Equipped {selectedItem.Name}.");
			}
			UpdateLabels();
		}

		private int moneyRefreshTimer;

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			if (++moneyRefreshTimer >= 30) {
				moneyRefreshTimer = 0;
				moneyText.SetText($"Your money: {SuitShop.FormatPrice(TotalMoney(Main.LocalPlayer))}");
			}
			if (root.ContainsPoint(Main.MouseScreen)) {
				Main.LocalPlayer.mouseInterface = true;
				PlayerInput.LockVanillaMouseScroll("MarvelMod/PartsStore");
			}
		}
	}
}
