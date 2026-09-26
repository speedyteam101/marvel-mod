using MarvelMod.Common.Players;
using MarvelMod.Common.Suits;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MarvelMod.Common.UI
{
	// A clickable text button that highlights when hovered or selected.
	public class WorkshopButton : UITextPanel<string>
	{
		private static readonly Color Normal = new Color(63, 82, 151) * 0.8f;
		private static readonly Color Hover = new(90, 110, 190);
		private static readonly Color Chosen = new(190, 60, 50);
		private static readonly Color Header = new Color(20, 26, 50) * 0.9f;

		public bool Selected;
		private readonly bool isHeader;

		public WorkshopButton(string text, Action onClick, float textScale = 0.8f, bool header = false) : base(text, textScale) {
			isHeader = header;
			SetPadding(6f);
			if (!header) {
				OnLeftClick += (_, _) => {
					SoundEngine.PlaySound(SoundID.MenuTick);
					onClick?.Invoke();
				};
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			BackgroundColor = isHeader ? Header : Selected ? Chosen : IsMouseHovering ? Hover : Normal;
			BorderColor = Selected ? Color.Gold : Color.Black;
			base.DrawSelf(spriteBatch);
		}
	}

	// The 96-colour palette as a clickable grid. Hover a swatch to see its name.
	public class ColourGrid : UIElement
	{
		private const int SwatchWidth = 28;
		private const int SwatchHeight = 24;
		private const int Gap = 2;

		private readonly Func<int> getSelected;
		private readonly Action<int> onPick;

		public ColourGrid(Func<int> getSelected, Action<int> onPick) {
			this.getSelected = getSelected;
			this.onPick = onPick;
			int rows = (SuitPalette.Count + SuitPalette.Columns - 1) / SuitPalette.Columns;
			Width.Set(SuitPalette.Columns * (SwatchWidth + Gap), 0f);
			Height.Set(rows * (SwatchHeight + Gap), 0f);
		}

		private int IndexAt(Vector2 screenPosition) {
			CalculatedStyle dims = GetDimensions();
			int col = (int)((screenPosition.X - dims.X) / (SwatchWidth + Gap));
			int row = (int)((screenPosition.Y - dims.Y) / (SwatchHeight + Gap));
			if (col < 0 || row < 0 || col >= SuitPalette.Columns) {
				return -1;
			}
			int index = row * SuitPalette.Columns + col;
			return index < SuitPalette.Count ? index : -1;
		}

		public override void LeftClick(UIMouseEvent evt) {
			base.LeftClick(evt);
			int index = IndexAt(evt.MousePosition);
			if (index >= 0) {
				SoundEngine.PlaySound(SoundID.MenuTick);
				onPick(index);
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			CalculatedStyle dims = GetDimensions();
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Rectangle source = new(0, 0, 1, 1);
			int selected = getSelected();
			int hovered = IsMouseHovering ? IndexAt(Main.MouseScreen) : -1;

			for (int i = 0; i < SuitPalette.Count; i++) {
				int x = (int)dims.X + i % SuitPalette.Columns * (SwatchWidth + Gap);
				int y = (int)dims.Y + i / SuitPalette.Columns * (SwatchHeight + Gap);
				if (i == selected || i == hovered) {
					Color border = i == selected ? Color.White : Color.Gray;
					spriteBatch.Draw(pixel, new Rectangle(x - 2, y - 2, SwatchWidth + 4, SwatchHeight + 4), source, border);
				}
				spriteBatch.Draw(pixel, new Rectangle(x, y, SwatchWidth, SwatchHeight), source, SuitPalette.Colors[i]);
			}

			if (hovered >= 0) {
				Main.hoverItemName = SuitPalette.Names[hovered];
			}
		}
	}

	// Live, animated preview of the design being edited.
	public class SuitPreview : UIPanel
	{
		// Frame and how many ticks to show it: stand, walk, aim, fly, fly and aim.
		private static readonly (SuitFrame Frame, int Ticks)[] Sequence = {
			(SuitFrame.Idle, 60), (SuitFrame.Walk1, 8), (SuitFrame.Walk2, 8), (SuitFrame.Walk3, 8), (SuitFrame.Walk4, 8),
			(SuitFrame.Walk1, 8), (SuitFrame.Walk2, 8), (SuitFrame.Walk3, 8), (SuitFrame.Walk4, 8),
			(SuitFrame.Aim, 45), (SuitFrame.Jump, 20), (SuitFrame.Fly, 45), (SuitFrame.FlyAim, 45)
		};

		private int timer;

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			timer++;
		}

		private SuitFrame CurrentFrame() {
			int total = 0;
			foreach (var step in Sequence) {
				total += step.Ticks;
			}
			int t = timer % total;
			foreach (var step in Sequence) {
				if (t < step.Ticks) {
					return step.Frame;
				}
				t -= step.Ticks;
			}
			return SuitFrame.Idle;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);
			IronManPlayer modPlayer = Main.LocalPlayer.GetModPlayer<IronManPlayer>();
			SuitTextureCache.Entry textures = SuitTextureCache.Get(modPlayer.Suit);

			CalculatedStyle inner = GetInnerDimensions();
			float scale = MathF.Floor(Math.Min(inner.Width, inner.Height) / SuitRenderer.FrameWidth);
			Vector2 feet = new(inner.X + inner.Width / 2f, inner.Y + inner.Height / 2f + SuitRenderer.FrameHeight * scale / 2f);
			Rectangle source = SuitRenderer.FrameRect(CurrentFrame());

			spriteBatch.Draw(textures.Body, feet, source, Color.White, 0f, SuitRenderer.Origin, scale, SpriteEffects.None, 0f);
			spriteBatch.Draw(textures.Glow, feet, source, Color.White * modPlayer.GlowIntensity(), 0f, SuitRenderer.Origin, scale, SpriteEffects.None, 0f);
		}
	}
}
