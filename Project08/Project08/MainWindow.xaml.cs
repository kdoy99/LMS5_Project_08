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
        public DispatcherTimer timer = new DispatcherTimer();

        private double total_memory;
        private double free_memory;
        private double remain_memory;
        private int percent;        
        public MainWindow()
        {
            InitializeComponent();            
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
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endPoint = new IPEndPoint(IPAddress.Parse(IPBox.Text), 10004);
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

        private void ClientWindow_Loaded(object sender, RoutedEventArgs e)
        {
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }
    }
}
