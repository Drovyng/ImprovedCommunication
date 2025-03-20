using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace ImprovedCommunication
{
    public class PingMapLayerAdv : ModMapLayer
    {
        public static PingMapLayerAdv Instance => ModContent.GetInstance<PingMapLayerAdv>();
        public static int SelectedSlot = 1000;
        public static int EditingSlot = 1000;
        public class PingAdv
        {
            public string text = "";
            public float scale = 2;
            public bool pulse = true;
            public int typo;
            public int color;
            public Vector2 position;

            public PingAdv(Vector2 pos)
            {
                position = pos;
            }
            public PingAdv(TagCompound tag)
            {
                text = tag.GetString("text");
                typo = tag.GetInt("typo");
                color = tag.GetInt("color");
                scale = tag.GetFloat("scale");
                pulse = tag.GetBool("pulse");
                position = new Vector2(tag.GetFloat("posX"), tag.GetFloat("posY"));
            }
            public Color GetColor()
            {
                if (color == 0) return Color.White;
                return Main.hslToRgb((color - 1) / 16f, 0.8f, 0.65f);
            }
            public TagCompound ToTag()
            {
                var tag = new TagCompound();
                tag.Set("text", text);
                tag.Set("typo", typo);
                tag.Set("color", color);
                tag.Set("scale", scale);
                tag.Set("pulse", pulse);
                tag.Set("posX", position.X);
                tag.Set("posY", position.Y);
                return tag;
            }
        }
        public struct PingShort
        {
            public Vector2 position;
            public DateTime time;
            public PingShort(Vector2 pos)
            {
                position = pos;
                time = DateTime.Now;
            }
        }
        public readonly SlotVector<PingShort> pingsShort = new SlotVector<PingShort>(50);

        public void EditingDelete()
        {
            if (EditingSlot == 1000) return;
            ImprovedCommunication.pings[EditingSlot] = null;
            if (Main.netMode == 1)
            {
                var p = Mod.GetPacket(80);
                p.Write((byte)10);
                p.Write(EditingSlot);
                p.Send(-1, Main.myPlayer);
            }
            EditingSlot = 1000;
            if (ImprovedCommunication.MapUIState.editText.IsWritingText)
                ImprovedCommunication.MapUIState.editText.ToggleTakingText();
        }
        public void EditingPulse()
        {
            if (EditingSlot == 1000) return;
            ImprovedCommunication.pings[EditingSlot].pulse = !ImprovedCommunication.pings[EditingSlot].pulse;
            if (Main.netMode == 1)
            {
                var p = Mod.GetPacket(80);
                p.Write((byte)9);
                p.Write(EditingSlot);
                p.Write(ImprovedCommunication.pings[EditingSlot].pulse);
                p.Send(-1, Main.myPlayer);
            }
        }
        public void EditingColor(int color)
        {
            if (EditingSlot == 1000) return;
            ImprovedCommunication.pings[EditingSlot].color = color;
            if (Main.netMode == 1)
            {
                var p = Mod.GetPacket(80);
                p.Write((byte)8);
                p.Write(EditingSlot);
                p.Write((byte)color);
                p.Send(-1, Main.myPlayer);
            }
        }
        public void EditingTypo(int typo)
        {
            if (EditingSlot == 1000) return;
            ImprovedCommunication.pings[EditingSlot].typo = typo;
            if (Main.netMode == 1)
            {
                var p = Mod.GetPacket(80);
                p.Write((byte)7);
                p.Write(EditingSlot);
                p.Write((byte)typo);
                p.Send(-1, Main.myPlayer);
            }
        }
        public void EditingScale(float scale)
        {
            if (EditingSlot == 1000) return;
            if (ImprovedCommunication.pings[EditingSlot].scale == scale) return;
            ImprovedCommunication.pings[EditingSlot].scale = scale;
            if (Main.netMode == 1)
            {
                var p = Mod.GetPacket(80);
                p.Write((byte)6);
                p.Write(EditingSlot);
                p.Write(scale);
                p.Send(-1, Main.myPlayer);
            }
        }
        public void EditingText(string text)
        {
            if (EditingSlot == 1000) return;
            ImprovedCommunication.pings[EditingSlot].text = text;
            if (Main.netMode == 1)
            {
                var p = Mod.GetPacket();
                p.Write((byte)5);
                p.Write(EditingSlot);
                p.Write(text);
                p.Send(-1, Main.myPlayer);
            }
        }
        public void EditingPos(Vector2 position)
        {
            if (EditingSlot == 1000) return;
            ImprovedCommunication.pings[EditingSlot].position = position;
            if (Main.netMode == 1)
            {
                var p = Mod.GetPacket(13);
                p.Write((byte)12);
                p.Write(EditingSlot);
                p.WriteVector2(position);
                p.Send(-1, Main.myPlayer);
            }
        }
        public uint AddPingShort(Vector2 position)
        {
            if (Main.netMode == 1)
            {
                var p = Mod.GetPacket(10);
                p.Write((byte)1);
                p.WriteVector2(position);
                p.Send(-1, Main.myPlayer);
            }
            return pingsShort.Add(new PingShort(position)).Value;
        }

        private static readonly RasterizerState OverflowHiddenRasterizerState = new RasterizerState
        {
            CullMode = CullMode.CullCounterClockwiseFace,
            ScissorTestEnable = true
        };
        public override void Draw(ref MapOverlayDrawContext context, ref string text)
        {
            var cfg = ICConfig.Instance;
            var rect = Main.spriteBatch.GraphicsDevice.ScissorRectangle;
            var num23453 = Main.mapFullscreen ? Main.mapFullscreenScale : ((Main.mapStyle != 1) ? Main.mapOverlayScale : Main.mapMinimapScale);
            var scl = Main.UIScale;
            if (!Main.mapFullscreen && Main.mapStyle == 1)
                scl *= Main.MapScale;
            if (Main.mapFullscreen)
                scl = 1;
            var mat = Matrix.CreateScale(scl);

            if (ImprovedCommunication.MapDrawRec == null && !Main.mapFullscreen)
            {
                var vec1 = new Vector2(Main.miniMapX, Main.miniMapY);
                var vec2 = vec1 + new Vector2(Main.miniMapWidth, Main.miniMapHeight);

                vec1 = Vector2.Transform(vec1, mat);
                vec2 = Vector2.Transform(vec2, mat);

                ImprovedCommunication.MapDrawRec = new Rectangle((int)vec1.X, (int)vec1.Y, (int)(vec2.X - vec1.X), (int)(vec2.Y - vec1.Y));
            }

            try
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, mat);
                if (ImprovedCommunication.MapDrawRec.HasValue)
                    Main.spriteBatch.GraphicsDevice.ScissorRectangle = ImprovedCommunication.MapDrawRec.Value;
                SpriteFrame frame = new SpriteFrame(1, 1);
                if (!Main.mapFullscreen)
                {
                    EditingSlot = 1000;
                    SelectedSlot = 1000;
                }
                if (EditingSlot != 1000 && ImprovedCommunication.pings[EditingSlot] == null)
                {
                    EditingSlot = 1000;
                }
                var scale = 1.15f + (float)Math.Sin(Main._drawInterfaceGameTime.TotalGameTime.TotalSeconds * 6) * 0.15f;
                SelectedSlot = 1000;

                var cs = cfg.CorrectMapMarkersScale ? (Main.mapFullscreen ? cfg.ScaleCorrectedOnFullMap : cfg.ScaleCorrectedOnMinimap) * ImprovedCommunication.MapDrawScl / 8 : 1;
                var id = -1;
                foreach (var value in ImprovedCommunication.pings)
                {
                    id++;
                    if (value == null) continue;

                    var hovered = context.Draw(
                        ImprovedCommunication.MapPings[value.typo].Value,
                        value.position,
                        Color.Transparent,
                        frame,
                        value.scale * cs,
                        value.scale * cs,
                        Alignment.Center
                    ).IsMouseOver;

                    if (hovered && SelectedSlot == 1000 && EditingSlot == 1000)
                    {
                        text = Language.GetTextValue("Mods.ImprovedCommunication.EditText");
                        SelectedSlot = id;
                    }

                    var s = scale;
                    if (!value.pulse) s = 1;
                    if (SelectedSlot == id) s *= 1.3f;
                    if (EditingSlot != 1000 && EditingSlot != id) s = 1f;
                    s *= value.scale;

                    if (value.typo != 13 || value.text.Length == 0)
                    {
                        context.Draw(
                            ImprovedCommunication.MapPings[value.typo].Value,
                            value.position,
                            value.GetColor(),
                            frame,
                            s * cs,
                            s * cs,
                            Alignment.Center
                        );
                    }
                    var s1 = ImprovedCommunication.MapDrawScl2 * (value.typo == 13 ? s : value.scale) * cs;
                    var textPos = (value.position -
                        Vector2.UnitY * ImprovedCommunication.MapPings[value.typo].Value.Height * 0.5f * (value.typo != 13 || value.text.Length == 0 ? 1 : 0) *
                        (value.typo == 13 ? s : value.scale) * cs / ImprovedCommunication.MapDrawScl2 / ImprovedCommunication.MapDrawScl - ImprovedCommunication.MapDrawPos) *
                        ImprovedCommunication.MapDrawScl + ImprovedCommunication.MapDrawOff;

                    if (!ImprovedCommunication.MapDrawRec.HasValue || ImprovedCommunication.MapDrawRec.Value.Contains(textPos.ToPoint()))
                    {
                        Utils.DrawBorderStringBig(
                            Main.spriteBatch,
                            value.text,
                            textPos,
                            Color.White,
                            s1 * 0.4f,
                            0.5f,
                            value.typo == 13 ? 0.5f : 1f
                        );
                    }
                }

                SpriteFrame frame2 = new SpriteFrame(1, 5);
                DateTime now = DateTime.Now;
                foreach (var item in pingsShort)
                {
                    var value = item.Value;
                    double totalSeconds = (now - value.time).TotalSeconds;

                    int num = (int)(totalSeconds * 10.0);
                    frame2.CurrentRow = (byte)(num % frame2.RowCount);

                    context.Draw(
                        TextureAssets.MapPing.Value,
                        value.position,
                        frame2,
                        Alignment.Center
                    );
                    if (totalSeconds >= 30)
                        pingsShort.Remove(item.Id);
                }
                if (EditingSlot != 1000)
                {
                    text = Language.GetTextValue("Mods.ImprovedCommunication.EditingText");
                    ImprovedCommunication.MapUIState.UpdateText(ref text);
                }
            }
            catch (Exception _) { }

            Main.spriteBatch.GraphicsDevice.ScissorRectangle = rect;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, mat);
        }
    }
}
