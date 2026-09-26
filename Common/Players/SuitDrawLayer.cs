using MarvelMod.Common.Suits;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MarvelMod.Common.Players
{
	// Draws the player's custom suit in place of their body while suited up.
	// IronManPlayer.HideDrawLayers hides the vanilla body layers at the same time.
	public class SuitDrawLayer : PlayerDrawLayer
	{
		// One suit pixel is 2x2 screen pixels, like vanilla sprites.
		public const float Scale = 2f;

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			return !player.dead && player.GetModPlayer<IronManPlayer>().SuitActive;
		}

		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Torso, PlayerDrawLayers.OffhandAcc);

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			IronManPlayer modPlayer = player.GetModPlayer<IronManPlayer>();
			SuitTextureCache.Entry textures = SuitTextureCache.Get(modPlayer.Suit);

			// Anchor the feet to the bottom of the hitbox (the top when gravity is flipped).
			Vector2 feet = drawInfo.Position + new Vector2(player.width / 2f, player.gravDir > 0f ? player.height : 0f) - Main.screenPosition;
			feet = new Vector2((int)feet.X, (int)feet.Y);

			Point tile = (drawInfo.Center / 16f).ToPoint();
			Color light = Lighting.GetColor(tile.X, tile.Y) * (1f - drawInfo.shadow);
			Color glow = Color.White * modPlayer.GlowIntensity() * (1f - drawInfo.shadow);

			float rotation = 0f;
			if (modPlayer.flying && player.velocity.X != 0f) {
				rotation = MathHelper.Clamp(player.velocity.X * 0.03f, -0.35f, 0.35f); // lean into flight
			}

			Microsoft.Xna.Framework.Graphics.SpriteEffects effects = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
			if (player.direction == -1) {
				effects |= Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
			}
			if (player.gravDir < 0f) {
				effects |= Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipVertically;
			}
			Vector2 origin = player.gravDir < 0f ? new Vector2(SuitRenderer.Origin.X, SuitRenderer.FrameHeight - SuitRenderer.Origin.Y) : SuitRenderer.Origin;
			Rectangle source = SuitRenderer.FrameRect(modPlayer.frame);

			// The giant suit is the same design drawn twice as big.
			float scale = modPlayer.giantForm ? Scale * 2f : Scale;
			drawInfo.DrawDataCache.Add(new DrawData(textures.Body, feet, source, light, rotation, origin, scale, effects, 0));
			drawInfo.DrawDataCache.Add(new DrawData(textures.Glow, feet, source, glow, rotation, origin, scale, effects, 0));
		}
	}
}
