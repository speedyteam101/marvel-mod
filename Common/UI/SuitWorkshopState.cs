using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace MarvelMod.Common.UI
{
	// The Suit Workshop: pick a category on the left, see the suit in the middle, choose an option on the right.
	// Edits apply straight to the local player's suit (and sync to other players).
	public class SuitWorkshopState : UIState
	{
		private const float ListTop = 70f;
		private const float OptionsLeft = 470f;

		private UIPanel root;
		private UIList categoryList;
		private UIText statsText;
		private UIText optionTitle;
		private UIText optionDescription;
		private UIText optionValue;
		private UIList optionList;
		private UIScrollbar optionScrollbar;
		private ColourGrid colourGrid;
		private WorkshopButton presetButton;
		private WorkshopButton slotButton;
		private UIText statusText;

		private readonly List<(SuitCategory Category, WorkshopButton Button)> categoryButtons = new();
		private readonly List<WorkshopButton> optionButtons = new();
		private SuitCategory selected = SuitCatalog.Helmet;
		private int presetIndex;
		private int slotIndex;
		private readonly Random random = new();

		private static IronManPlayer ModPlayer => Main.LocalPlayer.GetModPlayer<IronManPlayer>();
		private static SuitConfig Suit => ModPlayer.Suit;

		public override void OnInitialize() {
			root = new UIPanel();
			root.Width.Set(860f, 0f);
			root.Height.Set(590f, 0f);
			root.HAlign = 0.5f;
			root.VAlign = 0.5f;
			root.BackgroundColor = new Color(25, 32, 60) * 0.95f;
			Append(root);

			var title = new UIText("Stark Suit Workshop", 0.6f, large: true);
			root.Append(title);

			var summary = new UIText($"{SuitCatalog.TotalOptions} options in {SuitCatalog.Count} categories  -  {SuitCatalog.TotalCombinations:0.##E+0} possible suits", 0.8f);
			summary.Top.Set(38f, 0f);
			root.Append(summary);

			statusText = new UIText("", 0.85f);
			statusText.Left.Set(OptionsLeft - 60f, 0f);
			statusText.Top.Set(8f, 0f);
			root.Append(statusText);

			BuildCategoryList();
			BuildPreview();
			BuildOptionArea();
			BuildBottomBar();
		}

		private void BuildCategoryList() {
			categoryList = new UIList { ListPadding = 3f, ManualSortMethod = _ => { } };
			categoryList.Top.Set(ListTop, 0f);
			categoryList.Width.Set(200f, 0f);
			categoryList.Height.Set(-ListTop - 90f, 1f);
			root.Append(categoryList);

			var scrollbar = new UIScrollbar();
			scrollbar.Top.Set(ListTop, 0f);
			scrollbar.Left.Set(204f, 0f);
			scrollbar.Height.Set(-ListTop - 90f, 1f);
			categoryList.SetScrollbar(scrollbar);
			root.Append(scrollbar);

			SuitGroup? group = null;
			foreach (SuitCategory category in SuitCatalog.All) {
				if (group != category.Group) {
					group = category.Group;
					categoryList.Add(FullWidth(new WorkshopButton(SuitCatalog.GroupName(category.Group), null, 0.85f, header: true)));
				}
				SuitCategory captured = category;
				var button = FullWidth(new WorkshopButton(category.Name, () => SelectCategory(captured)));
				categoryButtons.Add((category, button));
				categoryList.Add(button);
			}
		}

		private static WorkshopButton FullWidth(WorkshopButton button) {
			button.Width.Set(0f, 1f);
			return button;
		}

		private void BuildPreview() {
			var preview = new SuitPreview();
			preview.Left.Set(228f, 0f);
			preview.Top.Set(ListTop, 0f);
			preview.Width.Set(230f, 0f);
			preview.Height.Set(260f, 0f);
			preview.BackgroundColor = new Color(60, 70, 90) * 0.9f;
			root.Append(preview);

			statsText = new UIText("", 0.75f);
			statsText.Left.Set(228f, 0f);
			statsText.Top.Set(ListTop + 270f, 0f);
			root.Append(statsText);
		}

		private void BuildOptionArea() {
			optionTitle = new UIText("", 1f);
			optionTitle.Left.Set(OptionsLeft, 0f);
			optionTitle.Top.Set(ListTop, 0f);
			root.Append(optionTitle);

			optionDescription = new UIText("", 0.75f);
			optionDescription.Left.Set(OptionsLeft, 0f);
			optionDescription.Top.Set(ListTop + 26f, 0f);
			root.Append(optionDescription);

			var previous = new WorkshopButton("<", () => Step(-1), 0.9f);
			previous.Left.Set(OptionsLeft, 0f);
			previous.Top.Set(ListTop + 48f, 0f);
			previous.Width.Set(40f, 0f);
			root.Append(previous);

			optionValue = new UIText("", 0.9f) { HAlign = 0f };
			optionValue.Left.Set(OptionsLeft + 50f, 0f);
			optionValue.Top.Set(ListTop + 56f, 0f);
			root.Append(optionValue);

			var next = new WorkshopButton(">", () => Step(1), 0.9f);
			next.Left.Set(OptionsLeft + 320f, 0f);
			next.Top.Set(ListTop + 48f, 0f);
			next.Width.Set(40f, 0f);
			root.Append(next);

			optionList = new UIList { ListPadding = 3f, ManualSortMethod = _ => { } };
			optionList.Left.Set(OptionsLeft, 0f);
			optionList.Top.Set(ListTop + 92f, 0f);
			optionList.Width.Set(340f, 0f);
			optionList.Height.Set(-ListTop - 92f - 90f, 1f);

			optionScrollbar = new UIScrollbar();
			optionScrollbar.Left.Set(OptionsLeft + 346f, 0f);
			optionScrollbar.Top.Set(ListTop + 92f, 0f);
			optionScrollbar.Height.Set(-ListTop - 92f - 90f, 1f);
			optionList.SetScrollbar(optionScrollbar);

			colourGrid = new ColourGrid(() => Suit[selected], index => SetOption(index));
			colourGrid.Left.Set(OptionsLeft, 0f);
			colourGrid.Top.Set(ListTop + 96f, 0f);
		}

		private void BuildBottomBar() {
			float row1 = -76f;
			float row2 = -38f;
			float x = 0f;

			void Add(WorkshopButton button, float width, float top) {
				button.Left.Set(x, 0f);
				button.Top.Set(top, 1f);
				button.Width.Set(width, 0f);
				root.Append(button);
				x += width + 6f;
			}

			Add(new WorkshopButton("Random Armour", () => Randomise(SuitGroup.Armour, SuitGroup.Paint)), 150f, row1);
			Add(new WorkshopButton("Random Colours", () => Randomise(SuitGroup.Colours, SuitGroup.Effects)), 150f, row1);
			Add(new WorkshopButton("Random Everything", () => Randomise(SuitGroup.Armour, SuitGroup.Paint, SuitGroup.Colours, SuitGroup.Effects, SuitGroup.Systems)), 170f, row1);
			presetButton = new WorkshopButton("", () => CyclePreset(1));
			Add(presetButton, 250f, row1);
			Add(new WorkshopButton("Load", ApplyPreset), 90f, row1);

			x = 0f;
			slotButton = new WorkshopButton("", CycleSlot);
			Add(slotButton, 150f, row2);
			Add(new WorkshopButton("Save Design", SaveSlot), 150f, row2);
			Add(new WorkshopButton("Load Design", LoadSlot), 150f, row2);
			var storeButton = new WorkshopButton("Parts Store", SuitWorkshopSystem.OpenStore);
			storeButton.Left.Set(-236f, 1f);
			storeButton.Top.Set(row2, 1f);
			storeButton.Width.Set(130f, 0f);
			root.Append(storeButton);

			var close = new WorkshopButton("Close", SuitWorkshopSystem.Close);
			close.Left.Set(-100f, 1f);
			close.Top.Set(row2, 1f);
			close.Width.Set(100f, 0f);
			root.Append(close);
		}

		// Called when the workshop opens: the player may have changed since last time.
		public void Refresh() {
			statusText?.SetText("");
			SelectCategory(selected);
		}

		private void SelectCategory(SuitCategory category) {
			selected = category;
			root.RemoveChild(optionList);
			root.RemoveChild(optionScrollbar);
			root.RemoveChild(colourGrid);
			optionList.Clear();
			optionButtons.Clear();

			if (category.IsColour) {
				root.Append(colourGrid);
			}
			else {
				for (int i = 0; i < category.Count; i++) {
					int index = i;
					var button = FullWidth(new WorkshopButton(OptionLabel(category, i), () => SetOption(index)));
					button.Locked = !ModPlayer.Owns(category, i);
					optionButtons.Add(button);
					optionList.Add(button);
				}
				root.Append(optionList);
				root.Append(optionScrollbar);
			}
			UpdateLabels();
		}

		private static string OptionLabel(SuitCategory category, int index) {
			ShopItem item = SuitShop.Get(category, index);
			string name = category.OptionName(index);
			if (item?.Set != null) {
				name += $" [c/FFD700:({item.Set.Name})]";
			}
			if (ModPlayer.Owns(category, index)) {
				return name;
			}
			return $"{name}  [c/909090:locked - {SuitShop.FormatPrice(item.Price)}]";
		}

		private void SetOption(int index) {
			if (!ModPlayer.Owns(selected, index)) {
				ShopItem item = SuitShop.Get(selected, index);
				statusText.SetText($"Locked: {SuitShop.FormatPrice(item.Price)} in the Parts Store");
				return;
			}
			Suit[selected] = index;
			UpdateLabels();
		}

		// Moves to the next or previous option the player owns.
		private void Step(int direction) {
			int index = Suit[selected];
			for (int i = 0; i < selected.Count; i++) {
				index = (index + direction + selected.Count) % selected.Count;
				if (ModPlayer.Owns(selected, index)) {
					SetOption(index);
					return;
				}
			}
		}

		private void Randomise(params SuitGroup[] groups) {
			Suit.Randomise(random, ModPlayer.Owns, groups);
			UpdateLabels();
		}

		private void CyclePreset(int direction) {
			presetIndex = (presetIndex + direction + SuitPresets.All.Count) % SuitPresets.All.Count;
			UpdateLabels();
		}

		private void ApplyPreset() {
			SuitConfig preset = SuitPresets.All[presetIndex].Build();
			int missing = ModPlayer.MissingParts(preset).Count;
			if (missing > 0) {
				statusText.SetText($"Preset needs {missing} part{(missing == 1 ? "" : "s")} you don't own (Parts Store)");
				return;
			}
			// Presets are looks: keep the weapons you have attached.
			foreach (SuitCategory category in SuitCatalog.All) {
				if (category.Group == SuitGroup.Weapons) {
					preset[category] = Suit[category];
				}
			}
			preset.CopyTo(Suit);
			statusText.SetText($"Loaded preset: {SuitPresets.All[presetIndex].Name}");
			UpdateLabels();
		}

		private void CycleSlot() {
			slotIndex = (slotIndex + 1) % IronManPlayer.DesignSlots;
			UpdateLabels();
		}

		private void SaveSlot() {
			ModPlayer.SavedDesigns[slotIndex] = Suit.Clone();
			statusText.SetText($"Saved to slot {slotIndex + 1}");
			UpdateLabels();
		}

		private void LoadSlot() {
			SuitConfig design = ModPlayer.SavedDesigns[slotIndex];
			if (design == null) {
				statusText.SetText($"Slot {slotIndex + 1} is empty");
				return;
			}
			int missing = ModPlayer.MissingParts(design).Count;
			if (missing > 0) {
				statusText.SetText($"Slot {slotIndex + 1} uses {missing} part{(missing == 1 ? "" : "s")} you don't own (Parts Store)");
				return;
			}
			design.CopyTo(Suit);
			statusText.SetText($"Loaded slot {slotIndex + 1}");
			UpdateLabels();
		}

		private void UpdateLabels() {
			int value = Suit[selected];
			optionTitle.SetText(selected.Name);
			optionDescription.SetText(selected.Description ?? (selected.IsColour ? "Pick a colour (hover for its name)" : ""));
			optionValue.SetText($"{selected.OptionName(value)}   ({value + 1} / {selected.Count})");

			for (int i = 0; i < optionButtons.Count; i++) {
				optionButtons[i].Selected = i == value;
			}
			foreach (var (category, button) in categoryButtons) {
				button.Selected = category == selected;
			}

			int tier = Math.Max(1, ModPlayer.ReactorTier);
			string reactor = ModPlayer.ReactorTier > 0 ? $"Mk {tier} reactor" : "Mk 1 reactor (none equipped)";
			statsText.SetText($"Suit stats with a {reactor}:\n{SuitStats.For(Suit, tier)}{SetProgress()}");

			presetButton.SetText($"Preset: {SuitPresets.All[presetIndex].Name}");
			bool empty = ModPlayer.SavedDesigns[slotIndex] == null;
			slotButton.SetText($"Slot {slotIndex + 1}{(empty ? " (empty)" : "")}");
		}

		// "Vampiric set: 7/10 pieces" for the set the design is closest to completing.
		private static string SetProgress() {
			SuitSet best = null;
			int bestWorn = 0;
			foreach (SuitSet set in SuitSets.All) {
				int worn = set.PiecesWorn(Suit);
				if (worn > bestWorn) {
					best = set;
					bestWorn = worn;
				}
			}
			if (best == null || bestWorn == best.PieceCount) {
				return "";
			}
			return $"\n[c/FFD700:{best.Name} set: {bestWorn}/{best.PieceCount} pieces worn]";
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			if (root.ContainsPoint(Main.MouseScreen)) {
				Main.LocalPlayer.mouseInterface = true;
				PlayerInput.LockVanillaMouseScroll("MarvelMod/SuitWorkshop");
			}
		}
	}
}
