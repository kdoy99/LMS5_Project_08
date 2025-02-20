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

namespace Project08
{        
    public partial class MainWindow : Window
    {
        Socket ClientSocket;
        // 타이머 변수 만들기
        public DispatcherTimer timer = new DispatcherTimer();

        // 데이터 변수
        private double total_memory;
        private double free_memory;
        private double remain_memory;
        private int percent;

        // 차트용
        public SeriesCollection PieData { get; set; }
        public ChartValues<double> TotalRam { get; set; }
        public ChartValues<double> RamFree { get; set; }
        public ChartValues<double> RamUsed { get; set; }        


        public MainWindow()
        {
            InitializeComponent();
            TotalRam = new ChartValues<double> { 0 };
            RamFree = new ChartValues<double> { 0 };
            RamUsed = new ChartValues<double> { 0 };

            PieData = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Used",
                    Values = new ChartValues<double> { 0 },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.Y:F2} GB"
                },
                new PieSeries
                {
                    Title = "Free",
                    Values = new ChartValues<double> { 0 },
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
                percent = 100 * (int)remain_memory / (int)total_memory;

                TotalMemory.Text = "총 메모리 (GB) : " + total_memory.ToString("F2");
                FreeMemory.Text = "사용 가능한 메모리 (GB) : " + free_memory.ToString("F2");
                RemainMemory.Text = "사용 중인 메모리 (GB) : " + remain_memory.ToString("F2");
                MemoryTitle.Text = "메모리 사용량 (%) : " + percent;
                MemoryBar.Value = percent;
            }

            TotalRam[0] = Math.Round(total_memory, 2);
            RamUsed[0] = Math.Round(remain_memory, 2);
            RamFree[0] = Math.Round(free_memory, 2);

            // 파이 차트 업데이트
            PieData[0].Values[0] = remain_memory;
            PieData[1].Values[0] = free_memory;


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
                    TotalMemory = total_memory,
                    FreeMemory = free_memory,
                    RemainMemory = remain_memory,
                    Percent = percent
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
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }        
    }
}
