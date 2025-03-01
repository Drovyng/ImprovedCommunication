using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ImprovedCommunication
{
    public class ICPlayer : ModPlayer
    {
        public Vector2 cursorPosition = new Vector2(-1000, -1000);
        public Color cursorColor = Color.White;
        public Color cursorColorOut = Color.White;
        public override void PostUpdate()
        {
            if (Main.netMode == 2) return;
            cursorColor = Main.mouseColor;
            if (Player.hasRainbowCursor)
                cursorColor = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.25f % 1f, 1f, 0.5f);
            cursorColor.A = 255;
            cursorPosition = Main.MouseWorld;
            cursorColorOut = Main.MouseBorderColor;

            if (Main.netMode == 1)
                SyncPlayer(-1, Main.myPlayer, false);
        }
        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            SyncPlayer(-1, Main.myPlayer, false);
        }
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)0);
            packet.Write((byte)Player.whoAmI);
            packet.WriteVector2(cursorPosition);
            packet.WriteRGB(cursorColor);
            packet.WriteRGB(cursorColorOut);
            packet.Write(cursorColorOut.A);
            packet.Send(toWho, fromWho);
        }
        public override void CopyClientState(ModPlayer targetCopy)
        {
            var m = targetCopy as ICPlayer;
            cursorPosition = m.cursorPosition;
            cursorColor = m.cursorColor;
            cursorColorOut = m.cursorColorOut;
        }
        public override void OnEnterWorld()
        {
            if (Main.netMode != 1 || PingMapLayerAdv.Instance == null) return;
            ImprovedCommunication.PingClear();
            PingMapLayerAdv.Instance.pingsShort.Clear();
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)11);
            packet.Write((uint)0);
            packet.Send(256);
        }
    }
}
