using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

namespace JsonClient
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient clientSocket = new TcpClient(); // 소켓

        NetworkStream stream = default(NetworkStream);

        string message = string.Empty;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try

            {

                IPEndPoint serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9991);

                clientSocket.Connect(serverAddress); // 접속 IP 및 포트

                stream = clientSocket.GetStream();

            }

            catch (Exception e2)

            {

                MessageBox.Show("서버가 실행중이 아닙니다.", "연결 실패!");

                Application.Current.Shutdown();
                Environment.Exit(0);

            }



            message = "채팅 서버에 연결 되었습니다.";

            DisplayText(message);



            byte[] buffer = Encoding.Unicode.GetBytes("테스트 사용자$");

            stream.Write(buffer, 0, buffer.Length);

            stream.Flush();



            Thread t_handler = new Thread(GetMessage);

            t_handler.IsBackground = true;

            t_handler.Start();


        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            textBox1.Focus();
            byte[] buffer = Encoding.Unicode.GetBytes(textBox1.Text + "$");
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
            textBox1.Text = "";

        }

        private void GetMessage() // 메세지 받기
        {

            while (true)
            {

                stream = clientSocket.GetStream();
                int BUFFERSIZE = clientSocket.ReceiveBufferSize;
                byte[] buffer = new byte[BUFFERSIZE];
                int bytes = stream.Read(buffer, 0, buffer.Length);


                string message = Encoding.Unicode.GetString(buffer, 0, bytes);

                //Json 거르기
                if (message.StartsWith("["))
                {

                    List<OrderData> orderlists = new List<OrderData>();

                    JArray array = JsonConvert.DeserializeObject<JArray>(message);

                    foreach (JObject data in array)
                    {
                        string datas = JsonConvert.SerializeObject(data);
                        orderlists.Add(JsonConvert.DeserializeObject<OrderData>(datas));
                        
                    }

                    DisplayText("Json 성공");
                    
                    testGrid.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        testGrid.ItemsSource = null;
                        testGrid.Items.Refresh();
                        testGrid.ItemsSource = orderlists;
                        
                    }));
                    

                    continue;
                }

                DisplayText(message);
            }


        }

    

        private void DisplayText(string text) // Server에 메세지 출력

        {

            if (textBlock1.Dispatcher.CheckAccess())
            {
                textBlock1.Dispatcher.BeginInvoke(new Action(delegate
                {
                    textBlock1.Text += (text + Environment.NewLine);
                }));

            }

            else
            {

                textBlock1.Dispatcher.BeginInvoke(new Action(delegate
                {
                    textBlock1.Text += (text + Environment.NewLine);
                }));
            }


        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter) // 엔터키 눌렀을 때

                Button_Click(this, e);

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            byte[] buffer = Encoding.Unicode.GetBytes("leaveChat" + "$");

            stream.Write(buffer, 0, buffer.Length);

            stream.Flush();

            Environment.Exit(Environment.ExitCode);

        }
    }
}
