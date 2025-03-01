using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace ImprovedCommunication
{
    public class FullscreenMapUI : UIState
    {
        public UIPanel root;
        public UIAutoScaleTextTextPanel<LocalizedText> buttonDelete;
        public UIAutoScaleTextTextPanel<LocalizedText> buttonApply;
        public UIAutoScaleTextTextPanel<LocalizedText> buttonPulse;
        public UISearchBar editText;
        public override void OnInitialize()
        {
            root = new UIPanel()
            {
                Width = { Pixels = 300 },
                Height = { Pixels = 232 },
                HAlign = 0.5f
            };
            Append(root);

            buttonDelete = new UIAutoScaleTextTextPanel<LocalizedText>(Language.GetText("Mods.ImprovedCommunication.Delete"))
            {
                Width = { Precent = 0.5f, Pixels = -3 },
                Height = { Pixels = 24 },
                VAlign = 1,
                TextColor = Color.White,
                UseInnerDimensions = true,
                PaddingTop = 1,
                PaddingBottom = 1,
                BackgroundColor = new Color(151, 63, 63) * 0.8f
            };
            buttonDelete.OnLeftClick += (e, s) =>
            {
                PingMapLayerAdv.Instance.EditingDelete();
            };
            UICommon.WithFadedMouseOver(buttonDelete, new Color(171, 73, 73) * 1.1f, new Color(151, 63, 63) * 0.8f);
            root.Append(buttonDelete);

            buttonApply = new UIAutoScaleTextTextPanel<LocalizedText>(Language.GetText("Mods.ImprovedCommunication.Apply"))
            {
                Width = { Precent = 0.5f, Pixels = -3 },
                Height = { Pixels = 24 },
                VAlign = 1,
                HAlign = 1,
                TextColor = Color.White,
                UseInnerDimensions = true,
                PaddingTop = 1,
                PaddingBottom = 1,
                BackgroundColor = new Color(63, 151, 63) * 0.8f
            };
            UICommon.WithFadedMouseOver(buttonApply, new Color(73, 171, 73) * 1.1f, new Color(63, 151, 63) * 0.8f);
            buttonApply.OnLeftClick += (e, s) =>
            {
                PingMapLayerAdv.EditingSlot = 1000;
                Main.inputTextEnter = false;
                Main.inputTextEscape = true;
            };
            root.Append(buttonApply);
            buttonPulse = new UIAutoScaleTextTextPanel<LocalizedText>(Language.GetText("Mods.ImprovedCommunication.Pulse"))
            {
                Width = { Pixels = 90 },
                Height = { Pixels = 24 },
                Top = { Pixels = 30 },
                VAlign = 0,
                HAlign = 1,
                TextColor = Color.White,
                UseInnerDimensions = true,
                PaddingTop = 1,
                PaddingBottom = 1,
                BackgroundColor = new Color(63, 151, 63) * 0.8f
            };
            buttonPulse.WithFadedMouseOver();
            buttonPulse.OnLeftClick += (e, s) =>
            {
                PingMapLayerAdv.Instance.EditingPulse();
            };
            root.Append(buttonPulse);

            var bord = Main.Assets.Request<Texture2D>("Images/UI/PanelBorder");
            var inn = Main.Assets.Request<Texture2D>("Images/UI/PanelBackground");
            for (int i = 0; i < 16; i++)
            {
                var color = Color.White;
                if (i > 0)
                {
                    color = Main.hslToRgb((i - 1) / 16f, 0.8f, 0.65f);
                }
                var pan = new UIPanel(inn, bord, 9, 10)
                {
                    Width = { Pixels = 19 },
                    Height = { Pixels = 19 },
                    HAlign = 0.5f,
                    Left = { Pixels = (i - 8f) * 18 + 9f },
                    BackgroundColor = color
                };
                UICommon.WithFadedMouseOver(pan, color, color);
                var j = i;
                pan.OnLeftClick += (e, l) =>
                {
                    PingMapLayerAdv.Instance.EditingColor(j);
                };
                root.Append(pan);
            }
            //var tr = new Color(1, 1, 1, 0.2f);
            var tr = Color.Transparent;
            for (int i = 0; i < 14; i++)
            {
                var pan = new UIPanel(inn, bord, 9, 10)
                {
                    Width = { Pixels = 40 },
                    Height = { Pixels = 40 },
                    HAlign = 0.5f,
                    Left = { Pixels = (i % 7 - 3f) * 41 },
                    Top = { Pixels = 60 + i / 7 * 41 },
                    BackgroundColor = tr
                };
                pan.SetPadding(i == 13 ? 2 : 6);
                UICommon.WithFadedMouseOver(pan, tr, tr);
                var j = i;
                pan.OnLeftClick += (e, l) =>
                {
                    PingMapLayerAdv.Instance.EditingTypo(j);
                };
                if (i <= ImprovedCommunication.MapPingsMax)
                {
                    pan.Append(new UIImage(ImprovedCommunication.MapPings[i])
                    {
                        Width = { Precent = 1 },
                        Height = { Precent = 1 },
                        IgnoresMouseInteraction = true,
                        ScaleToFit = true
                    });
                }
                root.Append(pan);
            }
            var p = new UIPanel()
            {
                Width = { Precent = 1 },
                Height = { Pixels = 24 },
                VAlign = 1,
                Top = { Pixels = -34 }
            }; p.PaddingLeft = p.PaddingRight = 6;
            editText = new UISearchBar(Language.GetText("Mods.ImprovedCommunication.WriteName"), 1);
            editText.OnContentsChanged += PingMapLayerAdv.Instance.EditingText;
            p.OnLeftClick += (e, l) => { if (!editText.IsWritingText) editText.ToggleTakingText(); };
            p.Append(editText);
            root.Append(p);
        }
        public override void Update(GameTime gameTime)
        {
            if (PingMapLayerAdv.EditingSlot != 1000 && ImprovedCommunication.pings[PingMapLayerAdv.EditingSlot] == null)
            {
                PingMapLayerAdv.EditingSlot = 1000;
            }
            base.Update(gameTime);
            if (PingMapLayerAdv.EditingSlot == 1000) return;
            buttonPulse.BackgroundColor = ImprovedCommunication.pings[PingMapLayerAdv.EditingSlot].pulse ?
                (buttonPulse.IsMouseHovering ? new Color(73, 171, 73) * 1.1f : new Color(63, 151, 63) * 0.8f) :
                (buttonPulse.IsMouseHovering ? new Color(171, 73, 73) * 1.1f : new Color(151, 63, 63) * 0.8f);
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            IngameOptions.inBar = false;
            IngameOptions.valuePosition = root.GetInnerDimensions().Position() + new Vector2(TextureAssets.ColorBar.Value.Width, 40);
            float scale = (ImprovedCommunication.pings[PingMapLayerAdv.EditingSlot].scale - 0.75f) / 4.5f;
            scale = 0.75f + IngameOptions.DrawValueBar(spriteBatch, 1, scale) * 4.5f;
            if (IngameOptions.inBar && Main.mouseLeft)
            {
                PingMapLayerAdv.Instance.EditingScale(scale);
            }
            if (Main.mouseLeft && root.IsMouseHovering)
            {
                Main.mapFullscreenPos = ImprovedCommunication.lastMapPos;
            }
        }
        public void UpdateText(ref string text)
        {
            if (PingMapLayerAdv.EditingSlot == 1000) return;
            if (root.IsMouseHovering) text = "";
            if (buttonApply.IsMouseHovering) text = Language.GetTextValue("Mods.ImprovedCommunication.ApplyInfo");
            if (buttonDelete.IsMouseHovering) text = Language.GetTextValue("Mods.ImprovedCommunication.DeleteInfo");
            if (buttonPulse.IsMouseHovering) text = Language.GetTextValue("Mods.ImprovedCommunication.Pulse") + ": " + (ImprovedCommunication.pings[PingMapLayerAdv.EditingSlot].pulse ? Language.GetTextValue("Mods.ImprovedCommunication.ON") : Language.GetTextValue("Mods.ImprovedCommunication.OFF"));
        }
    }
}
