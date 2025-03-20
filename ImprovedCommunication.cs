using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using ReLogic.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace ImprovedCommunication
{
    public class ImprovedCommunication : Mod
    {
        public static Asset<Texture2D> CrosshairIn;
        public static Asset<Texture2D> CrosshairOut;
        public static Asset<Texture2D> TargetIn;
        public static Asset<Texture2D> TargetOut;

        public static readonly PingMapLayerAdv.PingAdv[] pings = new PingMapLayerAdv.PingAdv[500];

        public static Asset<Texture2D>[] MapPings;
        public const int MapPingsMax = 13;

        public static UserInterface MapUI;
        public static FullscreenMapUI MapUIState;
        public static Vector2 MapDrawPos;
        public static float MapDrawScl;
        public static float MapDrawScl2;
        public static Vector2 MapDrawOff;
        public static Rectangle? MapDrawRec;
        public override void Load()
        {
            if (Main.dedServ) return;
            CrosshairIn = Assets.Request<Texture2D>("Assets/crosshair_in", AssetRequestMode.ImmediateLoad);
            CrosshairOut = Assets.Request<Texture2D>("Assets/crosshair_out", AssetRequestMode.ImmediateLoad);
            TargetIn = Assets.Request<Texture2D>("Assets/target_in", AssetRequestMode.ImmediateLoad);
            TargetOut = Assets.Request<Texture2D>("Assets/target_out", AssetRequestMode.ImmediateLoad);

            MapPings = new Asset<Texture2D>[MapPingsMax + 1];
            for (int i = 0; i <= MapPingsMax; i++)
            {
                MapPings[i] = Assets.Request<Texture2D>("Assets/ping_" + i, AssetRequestMode.ImmediateLoad);
            }

            MapUI = new();
            MapUIState = new();
            MapUI.SetState(MapUIState);

            On_Main.TriggerPing += On_Main_TriggerPing;
            On_Main.DrawMap += On_Main_DrawMap;
            On_MapIconOverlay.Draw += On_MapIconOverlay_Draw;
        }

        private void On_MapIconOverlay_Draw(On_MapIconOverlay.orig_Draw orig, MapIconOverlay self, Vector2 mapPosition, Vector2 mapOffset, Rectangle? clippingRect, float mapScale, float drawScale, ref string text)
        {
            orig(self, mapPosition, mapOffset, clippingRect, mapScale, drawScale, ref text);
            MapDrawPos = mapPosition;
            MapDrawScl = mapScale;
            MapDrawScl2 = drawScale;
            MapDrawOff = mapOffset;
            MapDrawRec = clippingRect;
        }

        public static Vector2 lastMapPos;
        private void On_Main_DrawMap(On_Main.orig_DrawMap orig, Main self, GameTime gameTime)
        {
            if (Main.resetMapFull && PingMapLayerAdv.Instance != null && Main.netMode == 1)
            {
                var p1 = GetPacket(5);
                p1.Write((byte)11);
                p1.Send(256);
            }

            lastMapPos = Main.mapFullscreenPos;
            orig(self, gameTime);

            if (!Main.mapFullscreen || PingMapLayerAdv.Instance == null) return;

            PlayerInput.SetZoom_UI();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            
            Utils.DrawBorderStringBig(Main.spriteBatch, Language.GetTextValue("Mods.ImprovedCommunication.FullscreenOldPingsText"), Main.ScreenSize.ToVector2() - Vector2.One * 4, Color.White, 0.4f, 1, 1);

            if (PingMapLayerAdv.EditingSlot == 1000) return;

            MapUI.Draw(Main.spriteBatch, gameTime);

            if (Main.mouseRight)
            {
                PingMapLayerAdv.EditingSlot = 1000;
                PingMapLayerAdv.SelectedSlot = 1000;
                if (MapUIState.editText.IsWritingText)
                    MapUIState.editText.ToggleTakingText();
            }
        }

        private void On_Main_TriggerPing(On_Main.orig_TriggerPing orig, Vector2 position)
        {
            if (Main.keyState.PressingControl())
            {
                PingMapLayerAdv.Instance.AddPingShort(position);
                return;
            }
            if (PingMapLayerAdv.EditingSlot != 1000)
            {
                if (MapUIState.root.IsMouseHovering) return;
                PingMapLayerAdv.Instance.EditingPos(position);
                return;
            }
            if (PingMapLayerAdv.SelectedSlot != 1000)
            {
                PingMapLayerAdv.EditingSlot = PingMapLayerAdv.SelectedSlot;
                PingMapLayerAdv.SelectedSlot = 1000;
                MapUIState.editText.SetContents(pings[PingMapLayerAdv.EditingSlot].text, true);
                return;
            }
            PingMapLayerAdv.EditingSlot = PingAdd(position);
            PingMapLayerAdv.SelectedSlot = 1000;
            MapUIState.editText.SetContents(pings[PingMapLayerAdv.EditingSlot].text, true);
        }
        public static int PingCount()
        {
            int c = 0;
            for (int i = 0; i < 500; i++)
            {
                if (pings[i] != null) c++;
            }
            return c;
        }
        public static void PingClear(int f = 0)
        {
            for (int i = f; i < 500; i++)
                pings[i] = null;
        }
        public int PingAdd(Vector2 position)
        {
            var i = PingAdd(new PingMapLayerAdv.PingAdv(position));
            if (i != 1000 && Main.netMode == 1)
            {
                var p = GetPacket(20);
                p.Write((byte)2);
                p.Write(i);
                p.WriteVector2(position);
                p.Send(-1, Main.myPlayer);
            }
            return i;
        }
        public static int PingAdd(PingMapLayerAdv.PingAdv ping)
        {
            for (int i = 0; i < 500; i++)
            {
                if (pings[i] != null) continue;
                pings[i] = ping;
                return i;
            }
            return 1000;
        }
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            switch (reader.ReadByte())
            {
                case 0:
                    var plr = Main.player[reader.ReadByte()].GetModPlayer<ICPlayer>();
                    plr.cursorPosition = reader.ReadVector2();
                    plr.cursorColor = reader.ReadRGB();
                    plr.cursorColorOut = reader.ReadRGB();
                    plr.cursorColorOut.A = reader.ReadByte();

                    if (Main.netMode == 2)
                    {
                        plr.SyncPlayer(-1, whoAmI, false);
                    }
                    break;
                case 1:         // Client -> Client
                    if (Main.netMode == 2)
                    {
                        var p = GetPacket(8); p.Write((byte)1);
                        p.WriteVector2(reader.ReadVector2());
                        p.Send(-1, whoAmI);
                        break;
                    }
                    PingMapLayerAdv.Instance.pingsShort.Add(new PingMapLayerAdv.PingShort(reader.ReadVector2()));
                    break;
                case 2:         // Client -> All
                    var j0_1 = reader.ReadInt32();
                    var j0_2 = reader.ReadVector2();
                    if (Main.netMode == 2)
                    {
                        var p = GetPacket(13); p.Write((byte)2);
                        p.Write(j0_1);
                        p.WriteVector2(j0_2);
                        p.Send(-1, whoAmI);
                    }
                    pings[j0_1] = new PingMapLayerAdv.PingAdv(j0_2);
                    break;
                case 3:         // Server -> Client     // Sync
                    pings[reader.ReadInt32()] = new PingMapLayerAdv.PingAdv(reader.ReadVector2())
                    {
                        typo = reader.ReadByte(),
                        color = reader.ReadByte(),
                        pulse = reader.ReadBoolean(),
                        scale = reader.ReadSingle(),
                        text = reader.ReadString(),
                    };
                    break;
                case 4:         // Server -> Client
                    PingClear(reader.ReadInt32());
                    break;
                case 5:         // Client -> All
                    var j1_1 = reader.ReadInt32();
                    var j1_2 = reader.ReadString();
                    if (Main.netMode == 2)
                    {
                        var p = GetPacket(); p.Write((byte)5);
                        p.Write(j1_1);
                        p.Write(j1_2);
                        p.Send(-1, whoAmI);
                    }
                    pings[j1_1].text = j1_2;
                    break;
                case 6:         // Client -> All
                    var j2_1 = reader.ReadInt32();
                    var j2_2 = reader.ReadSingle();
                    if (Main.netMode == 2)
                    {
                        var p = GetPacket(9); p.Write((byte)6);
                        p.Write(j2_1);
                        p.Write(j2_2);
                        p.Send(-1, whoAmI);
                    }
                    pings[j2_1].scale = j2_2;
                    break;
                case 7:         // Client -> All
                    var j3_1 = reader.ReadInt32();
                    var j3_2 = reader.ReadByte();
                    if (Main.netMode == 2)
                    {
                        var p = GetPacket(6); p.Write((byte)7);
                        p.Write(j3_1);
                        p.Write(j3_2);
                        p.Send(-1, whoAmI);
                    }
                    pings[j3_1].typo = j3_2;
                    break;
                case 8:         // Client -> All
                    var j4_1 = reader.ReadInt32();
                    var j4_2 = reader.ReadByte();
                    if (Main.netMode == 2)
                    {
                        var p = GetPacket(6); p.Write((byte)8);
                        p.Write(j4_1);
                        p.Write(j4_2);
                        p.Send(-1, whoAmI);
                    }
                    pings[j4_1].color = j4_2;
                    break;
                case 9:         // Client -> All
                    var j5_1 = reader.ReadInt32();
                    var j5_2 = reader.ReadBoolean();
                    if (Main.netMode == 2)
                    {
                        var p = GetPacket(6); p.Write((byte)9);
                        p.Write(j5_1);
                        p.Write(j5_2);
                        p.Send(-1, whoAmI);
                    }
                    pings[j5_1].pulse = j5_2;
                    break;
                case 10:        // Client -> All
                    var j6_1 = reader.ReadInt32();
                    if (Main.netMode == 2)
                    {
                        var p = GetPacket(6); p.Write((byte)10);
                        p.Write(j6_1);
                        p.Send(-1, whoAmI);
                    }
                    pings[j6_1] = null;
                    break;
                case 11:        // Client -> Server
                    var p1 = GetPacket(10);
                    p1.Write((byte)4);
                    p1.Write(PingCount());
                    p1.Send(whoAmI);
                    var i = 0;
                    foreach (var value in pings)
                    {
                        if (value == null) continue;
                        var p2 = GetPacket();
                        p2.Write((byte)3);
                        p2.Write(i);
                        p2.WriteVector2(value.position);
                        p2.Write((byte)value.typo);
                        p2.Write((byte)value.color);
                        p2.Write(value.pulse);
                        p2.Write(value.scale);
                        p2.Write(value.text);
                        p2.Send(whoAmI);
                        i++;
                    }
                    break;
                case 12:        // Client -> All
                    var j7_1 = reader.ReadInt32();
                    var j7_2 = reader.ReadVector2();
                    if (Main.netMode == 2)
                    {
                        var p = GetPacket(13); p.Write((byte)12);
                        p.Write(j7_1);
                        p.WriteVector2(j7_2);
                        p.Send(-1, whoAmI);
                    }
                    pings[j7_1].position = j7_2;
                    break;
            }
        }



        public static void DrawBorderStringBigInv(SpriteBatch spriteBatch, string text, Vector2 pos, Color color, float scale = 1f, bool invert = false, float anchorx = 0f, float anchory = 0f, int maxCharactersDisplayed = -1)
        {
            DynamicSpriteFont value = FontAssets.DeathText.Value;
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    Main.spriteBatch.DrawString(value, text, pos + new Vector2(i, j), Color.Black, 0, value.MeasureString(text) * new Vector2(anchorx, anchory), scale, invert ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
                }
            }
            Main.spriteBatch.DrawString(value, text, pos, color, 0, value.MeasureString(text) * new Vector2(anchorx, anchory), scale, invert ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
        }
    }
    public class ICSystem : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // int mouseItemIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Item / NPC Head");
            layers.Insert(0, new LegacyGameInterfaceLayer("ImprovedCommunication: Map Pings", DrawMapPings, InterfaceScaleType.Game));
            layers.Insert(1, new LegacyGameInterfaceLayer("ImprovedCommunication: Cursors", DrawCursors, InterfaceScaleType.UI));
        }
        public override void ClearWorld()
        {
            if (Main.netMode != 1) return;
            ImprovedCommunication.PingClear();
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)11);
            packet.Send(256);
            if (PingMapLayerAdv.Instance == null) return;
            PingMapLayerAdv.Instance.pingsShort.Clear();
        }
        public bool DrawCursors()
        {
            var cfg = ICConfig.Instance;
            if (!cfg.ShowPlayersCursors) return true;

            for (int i = 0; i < 255; i++)
            {
                var plr = Main.player[i];
                if (i == Main.myPlayer || plr == null || !plr.active) continue;

                if (cfg.ShowPlayersCursorsFriendlyOnly && plr.team != Main.LocalPlayer.team) continue;

                var player = plr.GetModPlayer<ICPlayer>();

                var pos = Main.ReverseGravitySupport(player.cursorPosition.ToScreenPosition());

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
                
                var offY = 2;

                switch (cfg.ShowPlayersCursorsStyle)
                {
                    case CursorsStyle.Target:
                    case CursorsStyle.Crosshair:
                        
                        float num = Main.cursorAlpha * 0.3f + 0.7f;
                        var col = player.cursorColor;
                        byte r = (byte)(player.cursorColor.R * Main.cursorAlpha);
                        byte g = (byte)(player.cursorColor.G * Main.cursorAlpha);
                        byte b = (byte)(player.cursorColor.B * Main.cursorAlpha);
                        byte a = (byte)(255f * num);

                        if (Main.ThickMouse)
                        {
                            for (int i1 = 0; i1 < 4; i1++)
                            {
                                Vector2 vector = Vector2.Zero;
                                switch (i1)
                                {
                                    case 0:
                                        vector = new Vector2(0f, 1f);
                                        break;
                                    case 1:
                                        vector = new Vector2(1f, 0f);
                                        break;
                                    case 2:
                                        vector = new Vector2(0f, -1f);
                                        break;
                                    case 3:
                                        vector = new Vector2(-1f, 0f);
                                        break;
                                }
                                Main.spriteBatch.Draw(cfg.ShowPlayersCursorsStyle == CursorsStyle.Target ? ImprovedCommunication.TargetOut.Value : ImprovedCommunication.CrosshairOut.Value, pos, null, player.cursorColorOut, 0, Vector2.One * 17 + vector, cfg.ShowPlayersCursorsScale * Main.cursorScale * 0.9f, SpriteEffects.None, 0);
                            }
                        }
                        Main.spriteBatch.Draw(cfg.ShowPlayersCursorsStyle == CursorsStyle.Target ? ImprovedCommunication.TargetIn.Value : ImprovedCommunication.CrosshairIn.Value, pos, null, new Color(r, g, b, a), 0, Vector2.One * 17, cfg.ShowPlayersCursorsScale * Main.cursorScale * 0.9f, SpriteEffects.None, 0);

                        offY = 18;

                        break;
                    default:
                        float num1 = Main.cursorAlpha * 0.3f + 0.7f;
                        var col1 = player.cursorColor;
                        byte r1 = (byte)(player.cursorColor.R * Main.cursorAlpha);
                        byte g1 = (byte)(player.cursorColor.G * Main.cursorAlpha);
                        byte b1 = (byte)(player.cursorColor.B * Main.cursorAlpha);
                        byte a1 = (byte)(255f * num1);

                        var of = Main.ThickMouse ? Vector2.One * 2 : Vector2.Zero;

                        if (Main.ThickMouse)
                        {
                            for (int i1 = 0; i1 < 4; i1++)
                            {
                                Vector2 vector = Vector2.Zero;
                                switch (i1)
                                {
                                    case 0:
                                        vector = new Vector2(0f, 1f);
                                        break;
                                    case 1:
                                        vector = new Vector2(1f, 0f);
                                        break;
                                    case 2:
                                        vector = new Vector2(0f, -1f);
                                        break;
                                    case 3:
                                        vector = new Vector2(-1f, 0f);
                                        break;
                                }

                                vector *= 1f;
                                vector += Vector2.One * 2f;
                                Main.spriteBatch.Draw(TextureAssets.Cursors[11].Value, pos + vector, null, player.cursorColorOut, 0, Vector2.One * 2, cfg.ShowPlayersCursorsScale * Main.cursorScale * 1.1f, SpriteEffects.None, 0);
                            }
                        }

                        Main.spriteBatch.Draw(TextureAssets.Cursors[0].Value, pos + of, null, new Color((byte)(r1 * 0.2f), (byte)(g1 * 0.2f), (byte)(b1 * 0.2f), (byte)(a1 * 0.5f)), 0, Vector2.Zero, cfg.ShowPlayersCursorsScale * Main.cursorScale * 1.1f, SpriteEffects.None, 0);
                        Main.spriteBatch.Draw(TextureAssets.Cursors[0].Value, pos + of + Vector2.One, null, new Color(r1, g1, b1, a1), 0, Vector2.Zero, cfg.ShowPlayersCursorsScale * Main.cursorScale, SpriteEffects.None, 0);

                        break;
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

                if (cfg.ShowPlayersCursorsNames)
                    Utils.DrawBorderString(
                        Main.spriteBatch, 
                        plr.name, 
                        pos - Vector2.UnitY * offY * cfg.ShowPlayersCursorsScale, 
                        cfg.ShowPlayersCursorsNamesColored ? Color.Lerp(Color.White, player.cursorColor, 0.5f) : Color.White,
                        cfg.ShowPlayersCursorsNamesScale,
                        0.5f,
                        1f
                    );
            }
            return true;
        }
        public bool DrawMapPings()
        {
            if (PingMapLayerAdv.Instance == null) return true;
            try
            {
                Main.spriteBatch.End();
                Vector2 vector2 = new Vector2(Main.instance.GraphicsDevice.Viewport.Width, -Main.instance.GraphicsDevice.Viewport.Height) * 0.5f;
                Vector2 translation = vector2 - vector2 / Main.GameViewMatrix.Zoom * new Vector2(1, 1);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);
                var cfg = ICConfig.Instance;

                if (!cfg.ShowInworldMarkers && !cfg.ShowInworldTextMarkers)
                    goto SkipBasicPings;

                // The FIRST time i use "goto" for about 9 years 20.02.2025

                var scale = 1.15f + (float)Math.Sin(Main._drawInterfaceGameTime.TotalGameTime.TotalSeconds * 6) * 0.15f;

                var yScale = Main.ReverseGravitySupport(Vector2.Zero).Y == 0 ? 1 : -1;

                foreach (var value in ImprovedCommunication.pings)
                {
                    if (value == null) continue;
                    var pos = Main.ReverseGravitySupport(value.position.ToWorldCoordinates(0, 0) - Main.screenPosition);

                    var s = scale;
                    if (!value.pulse) s = 1;
                    s *= value.scale;

                    if (value.typo == 13 && (value.text.Length == 0 || !cfg.ShowInworldTextMarkers))
                        continue;

                    if (value.typo != 13 && !cfg.ShowInworldMarkers)
                        continue;

                    if (value.typo != 13 || value.text.Length == 0)
                    {
                        Main.spriteBatch.Draw(
                            ImprovedCommunication.MapPings[value.typo].Value,
                            pos,
                            null,
                            value.GetColor(),
                            0,
                            ImprovedCommunication.MapPings[value.typo].Value.Size() * 0.5f,
                            s * 2,
                            Main.ReverseGravitySupport(Vector2.Zero).Y == 0 ? SpriteEffects.None : SpriteEffects.FlipVertically,
                            0
                        );
                    }
                    var s1 = value.typo == 13 ? s : value.scale;
                    var textPos = pos - Vector2.UnitY * (ImprovedCommunication.MapPings[value.typo].Value.Height * 0.5f * (value.typo != 13 || value.text.Length == 0 ? 1 : 0)) * s1 * 2 * yScale;

                    if (value.typo != 13 && !cfg.ShowInworldMarkersNames)
                        continue;

                    ImprovedCommunication.DrawBorderStringBigInv(
                        Main.spriteBatch,
                        value.text,
                        textPos,
                        Color.White,
                        s1 * 0.8f,
                        Main.ReverseGravitySupport(Vector2.Zero).Y != 0,
                        0.5f,
                        value.typo == 13 ? 0.5f : 1f * yScale
                    );
                }
            SkipBasicPings:
                SpriteFrame frame2 = new SpriteFrame(1, 5);
                DateTime now = DateTime.Now;
                foreach (var item in PingMapLayerAdv.Instance.pingsShort)
                {
                    var value = item.Value;

                    var pos = value.position.ToWorldCoordinates(0, 0) - Main.screenPosition;

                    double totalSeconds = (now - value.time).TotalSeconds;

                    int num = (int)(totalSeconds * 10.0);
                    frame2.CurrentRow = (byte)(num % frame2.RowCount);
                    var rect = frame2.GetSourceRectangle(TextureAssets.MapPing.Value);
                    Main.spriteBatch.Draw(
                        TextureAssets.MapPing.Value,
                        pos,
                        rect,
                        Color.White,
                        0,
                        rect.Size() * 0.5f,
                        2,
                        Main.ReverseGravitySupport(Vector2.Zero).Y == 0 ? SpriteEffects.None : SpriteEffects.FlipVertically,
                        0
                    );
                }
            }
            catch (Exception _) { }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            return true;
        }
        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.mapFullscreen && PingMapLayerAdv.EditingSlot != 1000)
                ImprovedCommunication.MapUI.Update(gameTime);
        }
        public override void SaveWorldData(TagCompound tag)
        {
            var list = new List<TagCompound>();
            foreach (var value in ImprovedCommunication.pings)
            {
                if (value == null) continue;
                list.Add(value.ToTag());
            }
            tag.Set("MapPings", list);
        }
        public override void LoadWorldData(TagCompound tag)
        {
            ImprovedCommunication.PingClear();
            var list = tag.Get<List<TagCompound>>("MapPings");
            foreach (var item in list)
            {
                ImprovedCommunication.PingAdd(new PingMapLayerAdv.PingAdv(item));
            }
        }
        public override void OnWorldUnload()
        {
            ImprovedCommunication.PingClear();
            if (PingMapLayerAdv.Instance == null) return;
            PingMapLayerAdv.Instance.pingsShort.Clear();
        }
    }
}
