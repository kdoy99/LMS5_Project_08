using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json;

using SQLite;

namespace Server
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window               
    {
        Socket ServerSocket;
        Socket ClientSocket;

        // 메모리 차트용
        public SeriesCollection PieData { get; set; } // 바인딩 데이터 1 (파이 차트)
        public SeriesCollection memorySeries { get; set; } // 바인딩 데이터 2 (라인 차트)
        public ChartValues<double> memoryUsage { get; set; } // 라인 차트 바인딩 데이터 용 차트 밸류


        // CPU 차트용
        public SeriesCollection PieCPU { get; set; } // 바인딩 데이터 1 (파이 차트)
        public SeriesCollection cpuSeries { get; set; } // 바인딩 데이터 2 (라인 차트)
        public ChartValues<double> cpuUsage { get; set; } // 라인 차트 바인딩 데이터 용 차트 밸류

        // HDD 차트용
        public SeriesCollection PieHDD { get; set; } // 바인딩 데이터 (파이 차트)

        private double cpuFree;

        List<Data> dataList;

        public MainWindow()
        {
            InitializeComponent();

            // 메모리 변수 초기화
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
                    Values = new ChartValues<double> { 100 },
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

            ReadNoticeDB();
        }
        private void ReadNoticeDB()
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath)) // databasePath : Notice 정보 들어있는 DB
            {
                connection.CreateTable<Data>();
                dataList = connection.Query<Data>("SELECT * FROM Data ORDER BY RANDOM() LIMIT 30");
            }

            if (dataList != null)
            {
                HistoryList.ItemsSource = dataList;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endPoint = new IPEndPoint(IPAddress.Any, Convert.ToInt32(portBox.Text));
            ServerSocket.Bind(endPoint);
            ServerSocket.Listen(10);
            AcceptClient();
        }

        private void AcceptClient()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += ClientAccepted;

            ServerSocket.AcceptAsync(args);
            AddLog("서비스가 시작되었습니다.");
        }

        private void ClientAccepted(object sender, SocketAsyncEventArgs e)
        {
            // 클라이언트와 연결 완료
            AddLog("클라이언트와 연결되었습니다.");
            // 클라이언트를 상대하기 위해서 동적으로 생성된 소켓을
            // 멤버변수에 저장
            ClientSocket = e.AcceptSocket;
            ReceiveInfo();
        }

        private void ReceiveInfo()
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(new byte[1024], 0, 1024);
            args.Completed += DataReceived;
            ClientSocket.ReceiveAsync(args);
        }

        private void DataReceived(object sender, SocketAsyncEventArgs e)
        {
            //1. 도착한 바이트 배열을 Json 문자열로 변환
            string json = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);

            //2. Json 문자열을 객체로 역직렬화
            try
            {
                var deviceinfo = JsonConvert.DeserializeObject<Data>(json);

                AddLog(json);

                //3. 객체의 내용을 UI에 반영
                RefreshDeviceInfo(deviceinfo);
            }
            catch { }

            //4. 다시 수신 작업 수행
            ReceiveInfo();
        }

        private void RefreshDeviceInfo(Data data)
        {            
            // 여기에 차트값들 집어넣기
            Action action = () =>
            {
                double percent = (data.RemainMemory / data.TotalMemory) * 100;

                TotalMemory.Text = "총 메모리 (GB) : " + data.TotalMemory.ToString("F2");
                FreeMemory.Text = "사용 가능한 메모리 (GB) : " + data.FreeMemory.ToString("F2");
                RemainMemory.Text = "사용 중인 메모리 (GB) : " + data.RemainMemory.ToString("F2");
                MemoryTitle.Text = "메모리 사용량 (%) : " + percent.ToString("F2");
                MemoryBar.Value = percent;

                // memory 파이 차트 업데이트
                PieData[0].Values[0] = data.RemainMemory;
                PieData[1].Values[0] = data.FreeMemory;

                // memory 라인 차트 업데이트
                memoryUsage.Add(Math.Round(data.RemainMemory, 2));
                if (memoryUsage.Count > 60)
                    memoryUsage.RemoveAt(0);

                // CPU 용 데이터 변수 지정                
                cpuFree = 100 - data.cpuValueData;

                // CPU 파이 차트 업데이트
                PieCPU[0].Values[0] = Math.Round(data.cpuValueData, 2);
                PieCPU[1].Values[0] = Math.Round(cpuFree, 2);

                // CPU 라인 차트 업데이트
                cpuUsage.Add(Math.Round(data.cpuValueData, 2));

                // 데이터 포인트 수 제한
                if (cpuUsage.Count > 60)
                    cpuUsage.RemoveAt(0);

                // HDD 파이 차트 업데이트
                PieHDD[0].Values[0] = data.useSize;
                PieHDD[1].Values[0] = data.freeSize;
                HDD_Name.Text = $"HDD 사용량 ({data.HDDname})";

                // 지정된 경로에 생성할 DB 연결 객체 생성
                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    // Data 클래스 정의를 기반으로 SQLite DB Table 생성 (테이블이 없을 경우, 있으면 X)
                    connection.CreateTable<Data>();

                    // UI 컨트롤에 입력된 데이터를 data 객체 형태로, 생성한 SQLite DB Table에 삽입                    
                    connection.Insert(data);

                    
                }
                
            };                

            Dispatcher.Invoke(action);
        }

        private void AddLog(string log)
        {
            //메인 스레드에서 UI 속성을 접근하는 로직이 수행되도록 위임            
            Action action = () => { txtLog.AppendText(log + "\r\n"); };
            Dispatcher.Invoke(action);
        }

        
    }
}
