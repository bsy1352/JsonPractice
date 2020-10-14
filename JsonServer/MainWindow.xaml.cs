using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
using Newtonsoft.Json.Linq;

namespace JsonServer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpListener server = null;
        TcpClient clientSocket = null;

        public Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();
        public MainWindow()
        {
            
            InitializeComponent();
            doMain();
            
        }

        public void doMain()
        {
            Console.WriteLine("D");
            Thread t = new Thread(this.InitSocket);
            t.IsBackground = true;
            t.Start();
        }

        private void InitSocket()
        {
            IPEndPoint localAddress = new IPEndPoint(IPAddress.Any, 9991);
            server = new TcpListener(localAddress);
            clientSocket = default(TcpClient);
            server.Start();

            while (true)
            {
                try
                {
                    clientSocket = server.AcceptTcpClient(); // client 소켓 접속 허용

                    NetworkStream stream = clientSocket.GetStream();

                    byte[] buffer = new byte[1024]; // 버퍼
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string user_name = Encoding.Unicode.GetString(buffer, 0, bytes);
                    user_name = user_name.Substring(0, user_name.IndexOf("$")); // client 사용자 명

                    
                    clientList.Add(clientSocket, user_name); // cleint 리스트에 추가
                    Console.WriteLine(user_name + " 님이 입장하셨습니다.");
                    SendMessageAll(user_name + " 님이 입장하셨습니다.", "", false); // 모든 client에게 메세지 전송

                    HandleClient h_client = new HandleClient(); // 클라이언트 추가
                    h_client.SendData += new HandleClient.DataSendHandler(SendData);
                    h_client.OnReceived += new HandleClient.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new HandleClient.DisconnectedHandler(h_client_OnDisconnected);
                    h_client.startClient(clientSocket, clientList);
                }

                catch (SocketException se) { break; }

                catch (Exception ex) { break; }

            }

            if (clientSocket != null)
            {
                clientSocket.Close(); // client 소켓 닫기
            }


        }

        void h_client_OnDisconnected(TcpClient clientSocket) // cleint 접속 해제 핸들러
        {
            if (clientList.ContainsKey(clientSocket))
                clientList.Remove(clientSocket);
        }

        private void OnReceived(string message, string user_name) // cleint로 부터 받은 데이터
        {
            if (message.Equals("leaveChat"))
            {
                string displayMessage = "leave user : " + user_name;
                Console.WriteLine(displayMessage);
                SendMessageAll("leaveChat", user_name, true);
            }
            else
            {
                string displayMessage = user_name + " : " + message;
                Console.WriteLine(displayMessage);
                SendMessageAll(message, user_name, true); // 모든 Client에게 전송
            }
        }

        public void SendMessageAll(string message, string user_name, bool flag)
        {
            foreach (var pair in clientList)
            {
                TcpClient client = pair.Key as TcpClient;
                NetworkStream stream = client.GetStream();
                byte[] buffer = null;

                if (flag)
                {
                    if (message.Equals("leaveChat"))
                        buffer = Encoding.Unicode.GetBytes(user_name + " 님이 대화방을 나갔습니다.");
                    else
                        buffer = Encoding.Unicode.GetBytes(user_name + " : " + message);
                }
                else
                {
                    buffer = Encoding.Unicode.GetBytes(message);
                }

                stream.Write(buffer, 0, buffer.Length); // 버퍼 쓰기
                stream.Flush();
            }
        }

        public void SendData(JArray data)
        {
            var pair = from list in clientList
                       select list.Key;
            //string s = "[{\"OrderNum\":20201234,\"OrderDetail\":\"자스민향\",\"OrderState\":\"제조 중\",\"OrderDate\":\"2020-10-12\"}]";
            
            TcpClient client = pair.FirstOrDefault() as TcpClient;
            NetworkStream stream = client.GetStream();

            byte[] send_JsonData = new byte[23500];
            string send_JsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
            send_JsonData = Encoding.Unicode.GetBytes(send_JsonString);
            
            stream.Write(send_JsonData, 0, send_JsonData.Length);
            stream.Flush();

        }


    }
}
