using System;
using System.Windows;
using System.Windows.Media;

namespace IOTrim.Service
{
    internal enum AppTheme
    {
        Light,
        Dark
    }

    internal static class ThemeService
    {
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        public static event EventHandler<AppTheme>? ThemeChanged;

        public static void ApplyTheme(AppTheme theme)
        {
            try
            {
                CurrentTheme = theme;
                var resources = Application.Current.Resources;

                if (theme == AppTheme.Dark)
                {
                    SetBrush(resources, "AppBackgroundBrush", "#0F172A");
                    SetBrush(resources, "AppSurfaceBrush", "#D91E293B");
                    SetBrush(resources, "AppSurfaceStrongBrush", "#FF1E293B");
                    SetBrush(resources, "AppTextBrush", "#F8FAFC");
                    SetBrush(resources, "ComboBoxTextColor", "Black");
                    SetBrush(resources, "AppMutedTextBrush", "#CBD5E1");
                    SetBrush(resources, "AppBorderBrush", "#334155");
                    SetBrush(resources, "AppNavHoverBrush", "#334155");
                    SetBrush(resources, "AppNavPressedBrush", "#475569");
                    SetBrush(resources, "AppDataGridBrush", "#E61E293B");
                    SetBrush(resources, "AppDataGridRowBrush", "#FF172033");
                    SetBrush(resources, "AppDataGridAltRowBrush", "#FF263449");
                    SetBrush(resources, "AppHeaderBrush", "#0EA5E9");
                    SetBrush(resources, "AppAccentBrush", "#38BDF8");
                    SetBrush(resources, "AppBlobOneBrush", "#334F46E5");
                    SetBrush(resources, "AppBlobTwoBrush", "#3338BDF8");
                    SetBrush(resources, "AppBlobThreeBrush", "#22F97316");
                    SetBrush(resources, "AppOverlayBrush", "#AA020617");
                    SetBrush(resources, "AppIconInfoBrush", "#2638BDF8");
                    SetBrush(resources, "AppIconSuccessBrush", "#2634D399");
                    SetBrush(resources, "AppGaugeInnerBrush", "#FF020617");
                    SetBrush(resources, "AppBlueCardBrush", "#262563EB");
                    SetBrush(resources, "AppOrangeCardBrush", "#26F97316");
                    SetBrush(resources, "AppBlueTextBrush", "#93C5FD");
                    SetBrush(resources, "AppOrangeTextBrush", "#FDBA74");
                    SetBrush(resources, "AppGreenTextBrush", "#86EFAC");
                    SetBrush(resources, "AppExportBrush", "#EF4444");
                    SetBrush(resources, "AppWarningBrush", "#F59E0B");
                }
                else
                {
                    SetBrush(resources, "AppBackgroundBrush", "#F6F8FB");
                    SetBrush(resources, "AppSurfaceBrush", "#CCFFFFFF");
                    SetBrush(resources, "AppSurfaceStrongBrush", "#FFFFFFFF");
                    SetBrush(resources, "AppTextBrush", "#111827");
                    SetBrush(resources, "ComboBoxTextColor", "Black");
                    SetBrush(resources, "AppMutedTextBrush", "#64748B");
                    SetBrush(resources, "AppBorderBrush", "#E5E7EB");
                    SetBrush(resources, "AppNavHoverBrush", "#F0F0F0");
                    SetBrush(resources, "AppNavPressedBrush", "#E5E5E5");
                    SetBrush(resources, "AppDataGridBrush", "#E6FFFFFF");
                    SetBrush(resources, "AppDataGridRowBrush", "#F4F7FB");
                    SetBrush(resources, "AppDataGridAltRowBrush", "#E8EDF5");
                    SetBrush(resources, "AppHeaderBrush", "#1769AA");
                    SetBrush(resources, "AppAccentBrush", "#2563EB");
                    SetBrush(resources, "AppBlobOneBrush", "#227C3AED");
                    SetBrush(resources, "AppBlobTwoBrush", "#2206B6D4");
                    SetBrush(resources, "AppBlobThreeBrush", "#18F97316");
                    SetBrush(resources, "AppOverlayBrush", "#80000000");
                    SetBrush(resources, "AppIconInfoBrush", "#E0F2FE");
                    SetBrush(resources, "AppIconSuccessBrush", "#DCFCE7");
                    SetBrush(resources, "AppGaugeInnerBrush", "#0F172A");
                    SetBrush(resources, "AppBlueCardBrush", "#EFF6FF");
                    SetBrush(resources, "AppOrangeCardBrush", "#FFF7ED");
                    SetBrush(resources, "AppBlueTextBrush", "#1D4ED8");
                    SetBrush(resources, "AppOrangeTextBrush", "#C2410C");
                    SetBrush(resources, "AppGreenTextBrush", "#22C55E");
                    SetBrush(resources, "AppExportBrush", "#DC2626");
                    SetBrush(resources, "AppWarningBrush", "#F59E0B");
                }

                ThemeChanged?.Invoke(null, theme);
                LogService.AddLog($"Application theme changed to {theme}.");
            }
            catch (Exception ex)
            {
                LogService.AddException("Apply application theme failed", ex);
            }
        }

        public static AppTheme ToggleTheme()
        {
            var newTheme = CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            ApplyTheme(newTheme);
            return newTheme;
        }

        private static void SetBrush(ResourceDictionary resources, string key, string colorCode)
        {
            resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorCode));
        }
    }
}
