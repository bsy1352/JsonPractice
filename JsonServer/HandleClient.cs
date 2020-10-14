using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsonServer
{
    public class HandleClient
    {
        TcpClient clientSocket = null;
        public Dictionary<TcpClient, string> clientList = null;
        private JArray jArray = new JArray();
        public void startClient(TcpClient clientSocket, Dictionary<TcpClient, string> clientList)
        {
            this.clientSocket = clientSocket;
            this.clientList = clientList;

            Thread t_hanlder = new Thread(doChat);
            t_hanlder.IsBackground = true;
            t_hanlder.Start();
        }

        public delegate void MessageDisplayHandler(string message, string user_name);
        public event MessageDisplayHandler OnReceived;

        public delegate void DisconnectedHandler(TcpClient clientSocket);
        public event DisconnectedHandler OnDisconnected;

        public delegate void DataSendHandler(JArray jObject);
        public event DataSendHandler SendData;

        private void doChat()
        {
            NetworkStream stream = null;
            try
            {
                byte[] buffer = new byte[1024];
                string msg = string.Empty;
                int bytes = 0;
                int MessageCount = 0;

                while (true)
                {                   //서버니깐 받는거 밖에 없다. 클라이언트 끼리의 내용을 서버에서 보여줌
                    MessageCount++;
                    stream = clientSocket.GetStream();
                    bytes = stream.Read(buffer, 0, buffer.Length);
                    msg = Encoding.Unicode.GetString(buffer, 0, bytes);
                    msg = msg.Substring(0, msg.IndexOf("$"));


                    switch (msg)
                    {
                        case "ShowOrders":
                            Thread newTread = new Thread(getOrderList);
                            newTread.IsBackground = true;
                            newTread.Start();
                            newTread.Join();

                            SendData(jArray);
                            Console.WriteLine(jArray);
                            break;

                        default:
                            break;

                    }

                    if (OnReceived != null)
                        OnReceived(msg, clientList[clientSocket].ToString());


                }
            }
            catch (SocketException se)
            {
                Trace.WriteLine(string.Format("doChat - SocketException : {0}", se.Message));

                if (clientSocket != null)
                {
                    if (OnDisconnected != null)
                        OnDisconnected(clientSocket);

                    clientSocket.Close();
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("doChat - Exception : {0}", ex.Message));

                if (clientSocket != null)
                {
                    if (OnDisconnected != null)
                        OnDisconnected(clientSocket);

                    clientSocket.Close();
                    stream.Close();
                }
            }
        }
        private void getOrderList()
        {
            jArray.Clear();
            Orders orders = new Orders();
            string strconn = "Server=127.0.0.1;Database=granco;Uid=admin;Pwd=1qhdtldbs!;";
            MySqlConnection conn = new MySqlConnection(strconn);
            MySqlCommand cmd;
            MySqlDataReader reader;

            conn.Open();
            string select = "Select * from orders";
            //string select = "Select Ethanol, Aloe_gel, Scent_Oil from recipe where R_Name = 'Orange'";
            cmd = new MySqlCommand(select, conn);
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                orders.OrderNum = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                orders.OrderDate = reader.IsDBNull(2) ? "null" : reader.GetString(2);
                orders.OrderDetail = reader.IsDBNull(1) ? "null" : reader.GetString(1);
                orders.OrderState = reader.IsDBNull(3) ? "null" : reader.GetString(3);

                var jsonString = JObject.FromObject(orders);
                jArray.Add(jsonString);
            }


            reader.Close();

            conn.Close();




        }
    }
}
