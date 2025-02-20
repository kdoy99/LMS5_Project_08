using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
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
using Newtonsoft.Json;

namespace Server
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window               
    {
        Socket ServerSocket;
        Socket ClientSocket;
        public MainWindow()
        {
            InitializeComponent();
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
            Action action = () =>
            {
                TotalMemory.Text = "총 메모리 (GB) : " + data.TotalMemory.ToString("F2");
                FreeMemory.Text = "사용 가능한 메모리 (GB) : " + data.FreeMemory.ToString("F2");
                RemainMemory.Text = "사용 중인 메모리 (GB) : " + data.RemainMemory.ToString("F2");
                MemoryTitle.Text = "메모리 사용량 (%) : " + data.Percent;
                MemoryBar.Value = data.Percent;
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
