using System;
using System.Windows;
using System.Windows.Media;

namespace ForgeBlueprint.Services
{
    public static class ThemeService
    {
        private sealed record ThemePalette(
            Color AppBackground,
            Color Panel,
            Color PanelAlt,
            Color Surface,
            Color SurfaceAlt,
            Color Border,
            Color BorderStrong,
            Color Accent,
            Color AccentStrong,
            Color AccentPressed,
            Color TextPrimary,
            Color TextSecondary,
            Color AccentText,
            Color Danger,
            Color DangerBackground,
            Color DangerHover,
            Color DangerText,
            Color DangerHoverBorder,
            Color ScrollTrack,
            Color ScrollThumb,
            Color ScrollThumbHover,
            Color CheckFill,
            Color CheckMark);

        private static readonly ThemePalette Dark = new(
            ColorFromHex("#111315"),
            ColorFromHex("#1A1D21"),
            ColorFromHex("#20242A"),
            ColorFromHex("#1F2733"),
            ColorFromHex("#172131"),
            ColorFromHex("#2A2F36"),
            ColorFromHex("#4A5360"),
            ColorFromHex("#4C8DFF"),
            ColorFromHex("#6AA6FF"),
            ColorFromHex("#3F79D8"),
            ColorFromHex("#F2F4F7"),
            ColorFromHex("#A8B0BA"),
            ColorFromHex("#F8FBFF"),
            ColorFromHex("#B55A5A"),
            ColorFromHex("#2B1D1D"),
            ColorFromHex("#392323"),
            ColorFromHex("#F3DADA"),
            ColorFromHex("#D37B7B"),
            ColorFromHex("#15181D"),
            ColorFromHex("#4B5563"),
            ColorFromHex("#6B7280"),
            ColorFromHex("#0E141D"),
            ColorFromHex("#F4F7FB"));

        private static readonly ThemePalette Light = new(
            ColorFromHex("#FCFBF8"),
            ColorFromHex("#F7F3EC"),
            ColorFromHex("#F1EBE1"),
            ColorFromHex("#F4EFE6"),
            ColorFromHex("#EFE8DD"),
            ColorFromHex("#DDD4C8"),
            ColorFromHex("#CAC0B4"),
            ColorFromHex("#4C8DFF"),
            ColorFromHex("#6AA6FF"),
            ColorFromHex("#3F79D8"),
            ColorFromHex("#1C2430"),
            ColorFromHex("#837C71"),
            ColorFromHex("#FFFFFF"),
            ColorFromHex("#C96B6B"),
            ColorFromHex("#F7E8E8"),
            ColorFromHex("#F3DDDD"),
            ColorFromHex("#6A2F2F"),
            ColorFromHex("#D9AEAE"),
            ColorFromHex("#EAE2D7"),
            ColorFromHex("#CBBFAF"),
            ColorFromHex("#B8AA98"),
            ColorFromHex("#4C8DFF"),
            ColorFromHex("#FFFFFF"));

        public static void ApplyTheme(ResourceDictionary resources, string? themeName)
        {
            ThemePalette palette = string.Equals(themeName, "Light", StringComparison.OrdinalIgnoreCase) ? Light : Dark;

            SetBrush(
                resources,
                "ForgeVaultInfoOverlayBackdropBrush",
                string.Equals(themeName, "Light", StringComparison.OrdinalIgnoreCase)
                    ? ColorFromHex("#CCFFFFFF")
                    : ColorFromHex("#A60A0F16"));

            SetBrush(resources, "AppBackgroundBrush", palette.AppBackground);
            SetBrush(resources, "PanelBrush", palette.Panel);
            SetBrush(resources, "PanelAltBrush", palette.PanelAlt);
            SetBrush(resources, "SurfaceBrush", palette.Surface);
            SetBrush(resources, "BorderBrushCustom", palette.Border);
            SetBrush(resources, "AccentBrush", palette.Accent);
            SetBrush(resources, "AccentBrushStrong", palette.AccentStrong);
            SetBrush(resources, "AccentHoverBrush", palette.AccentStrong);
            SetBrush(resources, "TextPrimaryBrush", palette.TextPrimary);
            SetBrush(resources, "TextSecondaryBrush", palette.TextSecondary);
            SetBrush(resources, "DangerBrush", palette.Danger);
            SetBrush(resources, "ControlHoverBorderBrush", palette.BorderStrong);
            SetBrush(resources, "ControlPressedBrush", Mix(palette.Panel, palette.AppBackground, 0.55));
            SetBrush(resources, "ControlPressedBorderBrush", Mix(palette.Border, palette.PanelAlt, 0.55));
            SetBrush(resources, "AccentTextBrush", palette.AccentText);
            SetBrush(resources, "DangerBackgroundBrush", palette.DangerBackground);
            SetBrush(resources, "DangerHoverBrush", palette.DangerHover);
            SetBrush(resources, "DangerTextBrush", palette.DangerText);
            SetBrush(resources, "DangerHoverBorderBrush", palette.DangerHoverBorder);
            SetBrush(resources, "ScrollTrackBrush", palette.Border);
            SetBrush(resources, "ScrollThumbBrush", palette.TextSecondary);
            SetBrush(resources, "ScrollThumbHoverBrush", palette.TextPrimary);

            SetBrush(resources, "WindowBackgroundBrush", palette.AppBackground);
            SetBrush(resources, "PanelBackgroundBrush", palette.Panel);
            SetBrush(resources, "PanelAltBackgroundBrush", palette.PanelAlt);
            SetBrush(resources, "BorderBrushDark", palette.Border);
            SetBrush(resources, "PrimaryTextBrush", palette.TextPrimary);
            SetBrush(resources, "SecondaryTextBrush", palette.TextSecondary);
            SetBrush(resources, "AccentPressedBrush", palette.AccentPressed);
            SetBrush(resources, "InputBackgroundBrush", palette.Surface);

            SetBrush(resources, "DialogBackgroundBrush", palette.AppBackground);
            SetBrush(resources, "CardBrush", palette.Panel);
            SetBrush(resources, "CardBrushAlt", palette.PanelAlt);
            SetBrush(resources, "SurfaceAltBrush", palette.SurfaceAlt);
            SetBrush(resources, "BorderBrush", palette.Border);
            SetBrush(resources, "BorderStrongBrush", palette.BorderStrong);
            SetBrush(resources, "TextMutedBrush", palette.TextSecondary);
            SetBrush(resources, "TextSoftBrush", Mix(palette.TextPrimary, palette.TextSecondary, 0.32));
            SetBrush(resources, "SubtleHoverBrush", Mix(palette.PanelAlt, palette.Accent, 0.07));
            SetBrush(resources, "SubtlePressedBrush", Mix(palette.PanelAlt, palette.AppBackground, 0.40));
            SetBrush(resources, "CheckFillBrush", palette.CheckFill);
            SetBrush(resources, "CheckMarkBrush", palette.CheckMark);
        }

        private static void SetBrush(ResourceDictionary resources, string key, Color color)
        {
            var brush = new SolidColorBrush(color);
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }

            resources[key] = brush;
        }

        private static Color Mix(Color from, Color to, double amount)
        {
            return Color.FromRgb(
                Clamp(from.R + (to.R - from.R) * amount),
                Clamp(from.G + (to.G - from.G) * amount),
                Clamp(from.B + (to.B - from.B) * amount));
        }

        private static byte Clamp(double value)
        {
            if (value < 0)
                return 0;
            if (value > 255)
                return 255;
            return (byte)Math.Round(value);
        }

        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex)!;
        }
    }
}
