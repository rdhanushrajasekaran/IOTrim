using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IOTrim.Service;

namespace IOTrim.CustomControls
{
    public enum PickerTheme
    {
        Light,
        Dark
    }

    public partial class ModernDateTimePicker : UserControl
    {
        private DateTime displayMonth = DateTime.Today;
        private DateTime? selectedDate;
        private TimeSpan selectedTime = DateTime.Now.TimeOfDay;

        public event EventHandler<DateTime?>? SelectedDateTimeChanged;

        public ModernDateTimePicker()
        {
            InitializeComponent();

            TimeIntervalMinutes = 15;
            StartTime = new TimeSpan(0, 0, 0);
            EndTime = new TimeSpan(23, 45, 0);

            ApplyTheme();
            LoadCalendar();
            LoadTimes();
            UpdateText();

            Loaded += ModernDateTimePicker_Loaded;
            Unloaded += ModernDateTimePicker_Unloaded;
            ThemeService.ThemeChanged += ThemeService_ThemeChanged;
        }

        private void ModernDateTimePicker_Loaded(object sender, RoutedEventArgs e)
        {
            SyncThemeWithApplication(ThemeService.CurrentTheme);
            ApplyShowTimeMode();
        }

        private void ModernDateTimePicker_Unloaded(object sender, RoutedEventArgs e)
        {
            ThemeService.ThemeChanged -= ThemeService_ThemeChanged;
        }

        private void ThemeService_ThemeChanged(object? sender, AppTheme theme)
        {
            Dispatcher.Invoke(() => SyncThemeWithApplication(theme));
        }

        private void SyncThemeWithApplication(AppTheme theme)
        {
            Theme = theme == AppTheme.Dark ? PickerTheme.Dark : PickerTheme.Light;
            InputTextBrush = (Brush)Application.Current.Resources["AppTextBrush"];
            InputBackground = (Brush)Application.Current.Resources["AppSurfaceStrongBrush"];
            InputBorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"];
            ApplyTheme();
        }

        public bool ShowTime
        {
            get => (bool)GetValue(ShowTimeProperty);
            set => SetValue(ShowTimeProperty, value);
        }

        public static readonly DependencyProperty ShowTimeProperty =
            DependencyProperty.Register(
                nameof(ShowTime),
                typeof(bool),
                typeof(ModernDateTimePicker),
                new PropertyMetadata(true, OnShowTimeChanged));

        private static void OnShowTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ModernDateTimePicker)d;
            control.ApplyShowTimeMode();
            control.UpdateText();
        }

        private void ApplyShowTimeMode()
        {
            if (TimeSection != null)
                TimeSection.Visibility = ShowTime ? Visibility.Visible : Visibility.Collapsed;

            if (TimeColumn != null)
                TimeColumn.Width = ShowTime ? new GridLength(130) : new GridLength(0);

            PopupWidth = ShowTime ? 560 : 420;
        }

        public PickerTheme Theme
        {
            get => (PickerTheme)GetValue(ThemeProperty);
            set => SetValue(ThemeProperty, value);
        }

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register(nameof(Theme), typeof(PickerTheme),
                typeof(ModernDateTimePicker),
                new PropertyMetadata(PickerTheme.Light, OnThemeChanged));

        public double PickerWidth
        {
            get => (double)GetValue(PickerWidthProperty);
            set => SetValue(PickerWidthProperty, value);
        }

        public static readonly DependencyProperty PickerWidthProperty =
            DependencyProperty.Register(nameof(PickerWidth), typeof(double),
                typeof(ModernDateTimePicker), new PropertyMetadata(520.0));

        public double PopupWidth
        {
            get => (double)GetValue(PopupWidthProperty);
            set => SetValue(PopupWidthProperty, value);
        }

        public static readonly DependencyProperty PopupWidthProperty =
            DependencyProperty.Register(nameof(PopupWidth), typeof(double),
                typeof(ModernDateTimePicker), new PropertyMetadata(560.0));

        public double InputHeight
        {
            get => (double)GetValue(InputHeightProperty);
            set => SetValue(InputHeightProperty, value);
        }

        public static readonly DependencyProperty InputHeightProperty =
            DependencyProperty.Register(nameof(InputHeight), typeof(double),
                typeof(ModernDateTimePicker), new PropertyMetadata(48.0));

        public double InputWidth
        {
            get => (double)GetValue(InputWidthProperty);
            set => SetValue(InputWidthProperty, value);
        }

        public static readonly DependencyProperty InputWidthProperty =
            DependencyProperty.Register(nameof(InputWidth), typeof(double),
                typeof(ModernDateTimePicker), new PropertyMetadata(48.0));

        public double InputFontSize
        {
            get => (double)GetValue(InputFontSizeProperty);
            set => SetValue(InputFontSizeProperty, value);
        }

        public static readonly DependencyProperty InputFontSizeProperty =
            DependencyProperty.Register(nameof(InputFontSize), typeof(double),
                typeof(ModernDateTimePicker), new PropertyMetadata(16.0));

        public CornerRadius InputCornerRadius
        {
            get => (CornerRadius)GetValue(InputCornerRadiusProperty);
            set => SetValue(InputCornerRadiusProperty, value);
        }

        public static readonly DependencyProperty InputCornerRadiusProperty =
            DependencyProperty.Register(nameof(InputCornerRadius), typeof(CornerRadius),
                typeof(ModernDateTimePicker), new PropertyMetadata(new CornerRadius(14)));

        public Thickness InputBorderThickness
        {
            get => (Thickness)GetValue(InputBorderThicknessProperty);
            set => SetValue(InputBorderThicknessProperty, value);
        }

        public static readonly DependencyProperty InputBorderThicknessProperty =
            DependencyProperty.Register(nameof(InputBorderThickness), typeof(Thickness),
                typeof(ModernDateTimePicker), new PropertyMetadata(new Thickness(1)));

        public Brush InputBorderBrush
        {
            get => (Brush)GetValue(InputBorderBrushProperty);
            set => SetValue(InputBorderBrushProperty, value);
        }

        public static readonly DependencyProperty InputBorderBrushProperty =
            DependencyProperty.Register(nameof(InputBorderBrush), typeof(Brush),
                typeof(ModernDateTimePicker),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(210, 200, 255))));

        public Brush InputBackground
        {
            get => (Brush)GetValue(InputBackgroundProperty);
            set => SetValue(InputBackgroundProperty, value);
        }

        public static readonly DependencyProperty InputBackgroundProperty =
            DependencyProperty.Register(nameof(InputBackground), typeof(Brush),
                typeof(ModernDateTimePicker),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(238, 232, 255))));

        public Brush InputTextBrush
        {
            get => (Brush)GetValue(InputTextBrushProperty);
            set => SetValue(InputTextBrushProperty, value);
        }

        public static readonly DependencyProperty InputTextBrushProperty =
            DependencyProperty.Register(nameof(InputTextBrush), typeof(Brush),
                typeof(ModernDateTimePicker),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(70, 80, 100))));

        public bool DisableFutureDates
        {
            get => (bool)GetValue(DisableFutureDatesProperty);
            set => SetValue(DisableFutureDatesProperty, value);
        }

        public static readonly DependencyProperty DisableFutureDatesProperty =
            DependencyProperty.Register(nameof(DisableFutureDates), typeof(bool),
                typeof(ModernDateTimePicker),
                new PropertyMetadata(false, OnCalendarChanged));

        public bool DisablePastDates
        {
            get => (bool)GetValue(DisablePastDatesProperty);
            set => SetValue(DisablePastDatesProperty, value);
        }

        public static readonly DependencyProperty DisablePastDatesProperty =
            DependencyProperty.Register(nameof(DisablePastDates), typeof(bool),
                typeof(ModernDateTimePicker),
                new PropertyMetadata(false, OnCalendarChanged));

        public DateTime? MinDate
        {
            get => (DateTime?)GetValue(MinDateProperty);
            set => SetValue(MinDateProperty, value);
        }

        public static readonly DependencyProperty MinDateProperty =
            DependencyProperty.Register(nameof(MinDate), typeof(DateTime?),
                typeof(ModernDateTimePicker),
                new PropertyMetadata(null, OnCalendarChanged));

        public DateTime? MaxDate
        {
            get => (DateTime?)GetValue(MaxDateProperty);
            set => SetValue(MaxDateProperty, value);
        }

        public static readonly DependencyProperty MaxDateProperty =
            DependencyProperty.Register(nameof(MaxDate), typeof(DateTime?),
                typeof(ModernDateTimePicker),
                new PropertyMetadata(null, OnCalendarChanged));

        public DateTime? SelectedDateTime
        {
            get => (DateTime?)GetValue(SelectedDateTimeProperty);
            set => SetValue(SelectedDateTimeProperty, value);
        }

        public static readonly DependencyProperty SelectedDateTimeProperty =
            DependencyProperty.Register(nameof(SelectedDateTime), typeof(DateTime?),
                typeof(ModernDateTimePicker),
                new PropertyMetadata(null, OnSelectedDateTimeChanged));

        public int TimeIntervalMinutes { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ModernDateTimePicker)d).ApplyTheme();
        }

        private static void OnCalendarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ModernDateTimePicker)d).LoadCalendar();
        }

        private static void OnSelectedDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModernDateTimePicker control = (ModernDateTimePicker)d;

            if (e.NewValue is DateTime dt)
            {
                control.selectedDate = dt.Date;
                control.selectedTime = dt.TimeOfDay;
                control.displayMonth = dt.Date;
            }
            else
            {
                control.selectedDate = null;
            }

            control.UpdateText();
            control.LoadCalendar();
            control.LoadTimes();
            control.SelectedDateTimeChanged?.Invoke(control, control.SelectedDateTime);
        }

        private void InputBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PickerPopup.IsOpen = !PickerPopup.IsOpen;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SelectedDateTime = null;
            PickerPopup.IsOpen = false;
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            displayMonth = displayMonth.AddMonths(-1);
            LoadCalendar();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            displayMonth = displayMonth.AddMonths(1);
            LoadCalendar();
        }

        private void LoadCalendar()
        {
            if (DaysGrid == null) return;

            DaysGrid.Children.Clear();
            txtMonth.Text = displayMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

            DateTime firstDay = new DateTime(displayMonth.Year, displayMonth.Month, 1);
            int startOffset = (int)firstDay.DayOfWeek;
            DateTime startDate = firstDay.AddDays(-startOffset);

            for (int i = 0; i < 42; i++)
            {
                DateTime date = startDate.AddDays(i);

                Button btn = new Button
                {
                    Content = date.Day.ToString(),
                    Width = 42,
                    Height = 42,
                    Margin = new Thickness(4),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Tag = date,
                    FontSize = 15,
                    Background = Brushes.Transparent
                };

                btn.Click += Date_Click;

                bool isOtherMonth = date.Month != displayMonth.Month;
                bool isSelected = selectedDate.HasValue && selectedDate.Value.Date == date.Date;
                bool isDisabled = IsDateDisabled(date);

                btn.Foreground = Theme == PickerTheme.Dark ? Brushes.White : Brushes.Black;

                if (isOtherMonth)
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(130, 140, 155));

                if (isSelected)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(105, 120, 255));
                    btn.Foreground = Brushes.White;
                }

                if (date.Date == DateTime.Today && !isSelected)
                {
                    btn.BorderThickness = new Thickness(1);
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(105, 120, 255));
                }

                if (isDisabled)
                {
                    btn.IsEnabled = false;
                    btn.Opacity = 0.30;
                }

                DaysGrid.Children.Add(btn);
            }
        }

        private void LoadTimes()
        {
            if (TimePanel == null) return;

            TimePanel.Children.Clear();

            TimeSpan roundedSelectedTime = new TimeSpan(selectedTime.Hours, selectedTime.Minutes, 0);

            for (TimeSpan time = StartTime; time <= EndTime; time = time.Add(TimeSpan.FromMinutes(TimeIntervalMinutes)))
            {
                Button btn = new Button
                {
                    Content = time.ToString(@"hh\:mm"),
                    Height = 40,
                    Margin = new Thickness(0, 4, 0, 4),
                    BorderThickness = new Thickness(0),
                    Tag = time,
                    FontSize = 15,
                    Cursor = Cursors.Hand
                };

                btn.Click += Time_Click;

                bool isSelected =
                    roundedSelectedTime.Hours == time.Hours &&
                    roundedSelectedTime.Minutes == time.Minutes;

                if (isSelected)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(105, 120, 255));
                    btn.Foreground = Brushes.White;
                }
                else
                {
                    btn.Background = Theme == PickerTheme.Dark
                        ? new SolidColorBrush(Color.FromRgb(42, 45, 70))
                        : new SolidColorBrush(Color.FromRgb(240, 238, 255));

                    btn.Foreground = Theme == PickerTheme.Dark
                        ? Brushes.White
                        : new SolidColorBrush(Color.FromRgb(80, 90, 255));
                }

                TimePanel.Children.Add(btn);
            }

            ApplyShowTimeMode();
        }

        private void Date_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DateTime date)
            {
                selectedDate = date.Date;

                if (ShowTime)
                {
                    SetSelectedDateTime();
                }
                else
                {
                    SelectedDateTime = selectedDate.Value.Date;
                    PickerPopup.IsOpen = false;

                    LoadCalendar();
                    UpdateText();
                }
            }
        }

        private void Time_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TimeSpan time)
            {
                selectedTime = time;
                SetSelectedDateTime();
                PickerPopup.IsOpen = false;
            }
        }

        private void SetSelectedDateTime()
        {
            if (selectedDate.HasValue)
            {
                SelectedDateTime = ShowTime
                    ? selectedDate.Value.Date.Add(selectedTime)
                    : selectedDate.Value.Date;
            }

            LoadCalendar();
            LoadTimes();
            UpdateText();
        }

        private bool IsDateDisabled(DateTime date)
        {
            if (DisableFutureDates && date.Date > DateTime.Today) return true;
            if (DisablePastDates && date.Date < DateTime.Today) return true;
            if (MinDate.HasValue && date.Date < MinDate.Value.Date) return true;
            if (MaxDate.HasValue && date.Date > MaxDate.Value.Date) return true;

            return false;
        }

        private void UpdateText()
        {
            if (txtSelected == null) return;

            if (SelectedDateTime.HasValue)
            {
                txtSelected.Text = ShowTime
                    ? SelectedDateTime.Value.ToString("dd-MMM-yyyy HH:mm")
                    : SelectedDateTime.Value.ToString("dd-MMM-yyyy");
            }
            else
            {
                txtSelected.Text = "Select date";
            }
        }

        private void ApplyTheme()
        {
            if (PickerPanel == null) return;

            if (Theme == PickerTheme.Light)
            {
                PickerPanel.Background = (Brush)Application.Current.Resources["AppSurfaceStrongBrush"];
                PickerPanel.BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"];
                Foreground = (Brush)Application.Current.Resources["AppTextBrush"];
            }
            else
            {
                PickerPanel.Background = (Brush)Application.Current.Resources["AppSurfaceStrongBrush"];
                PickerPanel.BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"];
                Foreground = (Brush)Application.Current.Resources["AppTextBrush"];
            }

            LoadCalendar();
            LoadTimes();
        }
    }
}