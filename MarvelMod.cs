using MarvelMod.Common.Players;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MarvelMod
{
	public class MarvelMod : Mod
	{
		private enum PacketType : byte
		{
			SyncSuit
		}

		// Sends a player's suit design. On a client, goes to the server, which forwards it to everyone else.
		public static void SendSuit(Player player, int toWho, int fromWho) {
			ModPacket packet = ModContent.GetInstance<MarvelMod>().GetPacket();
			packet.Write((byte)PacketType.SyncSuit);
			packet.Write((byte)player.whoAmI);
			player.GetModPlayer<IronManPlayer>().Suit.Write(packet);
			packet.Send(toWho, fromWho);
		}

		public override void Unload() {
			Common.Suits.SuitShop.Unload();
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			switch ((PacketType)reader.ReadByte()) {
				case PacketType.SyncSuit:
					int playerIndex = reader.ReadByte();
					if (Main.netMode == NetmodeID.Server) {
						playerIndex = whoAmI; // a client may only change its own suit
					}
					Player player = Main.player[playerIndex];
					player.GetModPlayer<IronManPlayer>().Suit.Read(reader);
					if (Main.netMode == NetmodeID.Server) {
						SendSuit(player, -1, whoAmI);
					}
					break;
			}
		}
	}
}
