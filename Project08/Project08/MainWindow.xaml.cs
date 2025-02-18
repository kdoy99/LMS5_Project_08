using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Threading;
using System.Management;

using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.WPF;
using LiveChartsCore.Drawing;
using System.Collections.ObjectModel;
using System.Net.Sockets;

namespace Project08
{        
    public partial class MainWindow : Window
    {
        Socket ClientSocket;
        public MainWindow()
        {
            InitializeComponent();
            memoryUpdate();
        }

        private void memoryUpdate()
        {
            ManagementClass cls = new ManagementClass("Win32_OperatingSystem");
            ManagementObjectCollection instances = cls.GetInstances();

            foreach (ManagementObject info in instances)
            {
                double total_memory = double.Parse(info["TotalVisibleMemorySize"].ToString());
                double free_memory = double.Parse(info["FreePhysicalMemory"].ToString());
                double remain_memory = total_memory - free_memory;
                double remain_memory_MB = (total_memory - free_memory) / 1024;

                int percent = 100 * (int)remain_memory / (int)total_memory;

                TotalMemory.Text = "총 메모리 (GB) : " + (total_memory/ (1024 * 1024)).ToString("F2");
                FreeMemory.Text = "사용 가능한 메모리 (GB) : " + (free_memory / (1024 * 1024)).ToString("F2");
                RemainMemory.Text = "사용 중인 메모리 (GB) : " + (remain_memory / (1024 * 1024)).ToString("F2");
                MemoryTitle.Text = "메모리 사용량 (%) : " + percent;
                MemoryBar.Value = percent;                
            }
        }

        



        // 분석 필요
        //public IEnumerable<ISeries> Series { get; set; } = [
        //        new StackedAreaSeries<double>([3, 2, 3, 5, 3, 4, 6]),
        //        new StackedAreaSeries<double>([6, 5, 6, 3, 8, 5, 2]),
        //        new StackedAreaSeries<double>([4, 8, 2, 8, 9, 5, 3])
        //    ];

        //public Axis[] XAxes { get; set; } =
        //{
        //    new Axis
        //    {
        //        Name = "axisname",
        //        NamePaint = new SolidColorPaint { Color = SKColors.Black },
        //         SeparatorsPaint = new SolidColorPaint
        //        {
        //            Color = SKColors.Gray,
        //            StrokeThickness = 2

        //        },
        //         MinStep=500
        //    }
        //};

        //public Axis[] YAxes { get; set; } =
        //{
        //    new Axis
        //    {
        //        Name = "Brightness(level)",
        //        MinStep=64,
        //         NamePaint = new SolidColorPaint { Color = SKColors.Black },
        //          SeparatorsPaint = new SolidColorPaint
        //        {
        //            Color = SKColors.Gray,
        //            StrokeThickness = 2

        //        },
        //    }
        //};
        //public ObservableCollection<RectangularSection> Sections { get; set; } = new()
        //{
        //   new RectangularSection
        //   {
        //       Xi =0,
        //       Xj = 0,
        //       Fill = new SolidColorPaint(new SKColor(255, 0, 0))


        //   }
        //};





    }
}
