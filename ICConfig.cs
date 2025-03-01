using System;
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace ImprovedCommunication
{
    public enum CursorsStyle : byte
    {
        Classic,
        Crosshair,
        Target
    }
    public class ICConfig : ModConfig
    {
        public static ICConfig Instance => ModContent.GetInstance<ICConfig>();
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("PlayersCursors")]
        [DefaultValue(true)]
        public bool ShowPlayersCursors;
        [DefaultValue(false)]
        public bool ShowPlayersCursorsFriendlyOnly;
        [DefaultValue(true)]
        public bool ShowPlayersCursorsNames;
        [DefaultValue(false)]
        public bool ShowPlayersCursorsNamesColored;
        [DefaultValue(1)]
        [Slider]
        [Range(0.5f, 2.5f)]
        [Increment(0.1f)]
        public float ShowPlayersCursorsNamesScale;
        [DefaultValue(1)]
        [Slider]
        [Range(0.5f, 2.5f)]
        [Increment(0.1f)]
        public float ShowPlayersCursorsScale;
        [DefaultValue(CursorsStyle.Crosshair)]
        [Slider]
        public CursorsStyle ShowPlayersCursorsStyle;
        [Header("MapNMarkers")]
        [DefaultValue(true)]
        public bool ShowInworldMarkers;
        [DefaultValue(true)]
        public bool ShowInworldMarkersNames;
        [DefaultValue(true)]
        public bool ShowInworldTextMarkers;
        [DefaultValue(true)]
        public bool CorrectMapMarkersScale;
        [DefaultValue(2)]
        [Slider]
        [Range(1f, 4f)]
        [Increment(0.25f)]
        public float ScaleCorrectedOnMinimap;
        [DefaultValue(1)]
        [Slider]
        [Range(1f, 4f)]
        [Increment(0.25f)]
        public float ScaleCorrectedOnFullMap;
    }
}
