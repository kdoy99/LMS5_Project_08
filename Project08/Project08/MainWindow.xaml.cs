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

using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Windows.Threading;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Net;
using System.Timers;
using System.IO;

namespace Project08
{        
    public partial class MainWindow : Window
    {
        // 클라이언트 소켓
        Socket ClientSocket;

        // 타이머 변수 만들기
        public DispatcherTimer timer = new DispatcherTimer();

        // 데이터 변수
        // 메모리
        private double total_memory;
        private double free_memory;
        private double remain_memory;        
        // CPU
        private float cpuValue;
        // HDD
        private string HDDName;
        private long TotalSize;
        private long FreeSize;
        private long UseSize;

        // 메모리 차트용
        public SeriesCollection PieData { get; set; } // 바인딩 데이터 1 (파이 차트)
        public SeriesCollection memorySeries { get; set; } // 바인딩 데이터 2 (라인 차트)
        public ChartValues<double> memoryUsage { get; set; } // 라인 차트 바인딩 데이터 용 차트 밸류
        

        // CPU 차트용
        public SeriesCollection PieCPU { get; set; } // 바인딩 데이터 1 (파이 차트)
        public SeriesCollection cpuSeries { get; set; } // 바인딩 데이터 2 (라인 차트)
        public ChartValues<double> cpuUsage { get; set; } // 라인 차트 바인딩 데이터 용 차트 밸류

        private PerformanceCounter cpuCounter; // CPU 파이 차트용

        // HDD 차트용
        public SeriesCollection PieHDD { get; set; } // 바인딩 데이터 (파이 차트)


        public MainWindow()
        {
            InitializeComponent();            

            // Memory 변수 초기화
            memoryUsage = new ChartValues<double>();
                        
            // Binding, 메모리 파이차트
            PieData = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "사용 중",
                    Values = new ChartValues<double> { 0 },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.Y:F2} GB"
                },
                new PieSeries
                {
                    Title = "남은 공간",
                    Values = new ChartValues<double> { 0 },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.Y:F2} GB"
                }
            };

            // Binding, 메모리 라인차트
            memorySeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Memory 사용량 (GB)",
                    Values = memoryUsage,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 5
                }
            };

            // CPU 변수 초기화
            cpuUsage = new ChartValues<double>();

            // Binding, CPU 파이차트
            PieCPU = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "사용 중",
                    Values = new ChartValues<double> { 0 },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.Y:F2}%"
                },
                new PieSeries
                {
                    Title = "남은 공간",
                    Values = new ChartValues<double> { 100 },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.Y:F2}%"
                }
            };

            // Binding, CPU 라인차트
            cpuSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "CPU 사용량 (%)",
                    Values = cpuUsage,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 5
                }
            };

            // 퍼포먼스 카운터 초기화 및 미리 호출
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();


            // Binding, HDD 파이차트
            PieHDD = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "사용 중",
                    Values = new ChartValues<long>{ 0 },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.Y:F2} GB"
                },
                new PieSeries
                {
                    Title = "남은 공간",
                    Values = new ChartValues<long>{ 100 },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.Y:F2} GB"
                }
            };

            DataContext = this;
        }

        private void memoryUpdate()
        {
            // 컴퓨터 메모리 데이터 불러오고 텍스트 박스에 저장함
            ManagementClass cls = new ManagementClass("Win32_OperatingSystem");
            ManagementObjectCollection instances = cls.GetInstances();

            foreach (ManagementObject info in instances)
            {
                total_memory = double.Parse(info["TotalVisibleMemorySize"].ToString()) / (1024 * 1024);
                free_memory = double.Parse(info["FreePhysicalMemory"].ToString()) / (1024 * 1024);
                remain_memory = total_memory - free_memory;                             
            }

            // HDD 정보
            DriveInfo[] hddDrives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in hddDrives)
            {
                if (drive.DriveType == DriveType.Fixed)
                {
                    HDDName = drive.Name.ToString();
                    TotalSize = drive.TotalSize / 1024 / 1024 / 1024;
                    FreeSize = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                    UseSize = (drive.TotalSize - drive.AvailableFreeSpace) / 1024 / 1024 / 1024;
                }
            }

            // memory 파이 차트 업데이트
            PieData[0].Values[0] = remain_memory;
            PieData[1].Values[0] = free_memory;

            // memory 라인 차트 업데이트
            memoryUsage.Add(Math.Round(remain_memory, 2));
            if (memoryUsage.Count > 60)
                memoryUsage.RemoveAt(0);

            // CPU 용 데이터 변수 지정
            cpuValue = cpuCounter.NextValue();
            float cpuFree = 100 - cpuValue;

            // CPU 파이 차트 업데이트
            PieCPU[0].Values[0] = Math.Round(cpuValue, 2);
            PieCPU[1].Values[0] = Math.Round(cpuFree, 2);

            // CPU 라인 차트 업데이트
            cpuUsage.Add(Math.Round(cpuValue, 2));

            // 데이터 포인트 수 제한
            if (cpuUsage.Count > 60)
                cpuUsage.RemoveAt(0);

            // HDD 파이 차트 업데이트
            PieHDD[0].Values[0] = UseSize;
            PieHDD[1].Values[0] = FreeSize;
            HDD_Name.Text = $"HDD 사용량 ({HDDName})";

        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            int port = Convert.ToInt32(PortBox.Text); // PortBox 안에 있는 값을 port 변수에 저장
            ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endPoint = new IPEndPoint(IPAddress.Parse(IPBox.Text), port); // IP와 Port 값 따와서 사용
            var args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = endPoint;
            args.Completed += ServerConnected;
            ClientSocket.ConnectAsync(args);
        }

        private void ServerConnected(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // 서버 연결 성공시 서버로부터 제어 요청 받기                
                ReceiveControl();                
            }
        }

        private void ReceiveControl()
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(new byte[1024], 0, 1024);
            args.Completed += ControlReceived;
            ClientSocket.ReceiveAsync(args);
        }

        private void ControlReceived(object sender, SocketAsyncEventArgs e)
        {
            string json = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
            var control = JsonConvert.DeserializeObject<ClientData>(json);            
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            // 타이머 1번 발생 = timer_Tick
            memoryUpdate();
            SendDeviceInfo();
        }

        private void SendDeviceInfo()
        {
            // 소켓 O, 서버 연결 O 확인
            if (ClientSocket == null || !ClientSocket.Connected)
                return;

            try
            {
                // 1. 전송할 데이터 엔터티 객체에 준비
                var info = new ClientData
                {
                    // 메모리
                    TotalMemory = total_memory,
                    FreeMemory = free_memory,
                    RemainMemory = remain_memory,
                    // CPU
                    cpuValueData = cpuValue,
                    // HDD
                    HDDname = HDDName,
                    totalSize = TotalSize,
                    freeSize = FreeSize,
                    useSize = UseSize
                };

                // 2. 객체를 json 문자열로 직렬화
                string json = JsonConvert.SerializeObject(info);

                // 3. 문자열 byte 배열로 변환
                byte[] bytesToSend = Encoding.UTF8.GetBytes(json);

                // 4. SocketAsyncEventArgs 객체에 전송할 데이터 설정
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(bytesToSend, 0, bytesToSend.Length);

                // 5. 비동기적으로 전송
                ClientSocket.SendAsync(args);
            }
            catch { }
        }
        // 클라이언트 창 로드될 때 발생되는 이벤트
        private void ClientWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 타이머 설정
            timer.Interval = TimeSpan.FromMilliseconds(1000); // 1초
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }        
    }
}
