using IOTrim.ViewModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Threading;

namespace IOTrim.Views
{
    public partial class HomePage : Page
    {
        private readonly DispatcherTimer clockTimer;

        public double OEEValue { get; set; } = 78;
        public string OEEValueText => $"{OEEValue:0}";



        public HomePage()
        {
            InitializeComponent();
            DataContext = new HomeViewModel();
        }
    }
}
