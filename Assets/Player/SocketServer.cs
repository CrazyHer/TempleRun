using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace SocketUtil
{
    public class SocketServer
    {
        private string _ip = string.Empty;
        private int _port = 0;
        private Socket _socket = null;
        private byte[] buffer = new byte[1024 * 1024 * 2];

        public Queue<string> messageQueue = new Queue<string>();
        public int count = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">监听的IP</param>
        /// <param name="port">监听的端口</param>
        public SocketServer(string ip, int port)
        {
            this._ip = ip;
            this._port = port;
        }
        public SocketServer(int port)
        {
            this._ip = "0.0.0.0";
            this._port = port;
        }

        public void StartListen()
        {
            try
            {
                //1.0 实例化套接字(IP4寻找协议,流式协议,TCP协议)
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //2.0 创建IP对象
                IPAddress address = IPAddress.Parse(_ip);
                //3.0 创建网络端口,包括ip和端口
                IPEndPoint endPoint = new IPEndPoint(address, _port);
                //4.0 绑定套接字
                _socket.Bind(endPoint);
                //5.0 设置最大连接数
                _socket.Listen(int.MaxValue);
                Console.WriteLine("监听{0}消息成功", _socket.LocalEndPoint.ToString());
                //6.0 开始监听
                Thread thread = new Thread(ListenClientConnect);
                thread.Start();

            }
            catch (Exception ex)
            {
                Console.WriteLine("出错,{0}", ex.ToString());
            }
        }
        /// <summary>
        /// 监听客户端连接
        /// </summary>
        private void ListenClientConnect()
        {
            try
            {
                while (true)
                {
                    //Socket创建的新连接
                    Socket clientSocket = _socket.Accept();
                    Console.WriteLine("新客户端连接");
                    // clientSocket.Send(Encoding.UTF8.GetBytes("服务端发送消息:"));
                    Thread thread = new Thread(ReceiveMessage);
                    thread.Start(clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("出错,{0}", ex.ToString());
            }
        }

        /// <summary>
        /// 接收客户端消息
        /// </summary>
        /// <param name="socket">来自客户端的socket</param>
        private void ReceiveMessage(object socket)
        {
            Socket clientSocket = (Socket)socket;
            while (true)
            {
                try
                {
                    //获取从客户端发来的数据
                    int length = clientSocket.Receive(buffer);
                    var data = Encoding.UTF8.GetString(buffer, 0, length);
                    if (data.Length > 0 && count < 1000)
                    {
                        //Console.WriteLine(data);
                        messageQueue.Enqueue(data);
                        count++;

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    break;
                }
            }
        }
        private static T Desrialize<T>(T obj, string str)
        {
            try
            {
                obj = default(T);
                IFormatter formatter = new BinaryFormatter();
                byte[] buffer = Convert.FromBase64String(str);
                MemoryStream stream = new MemoryStream(buffer);
                obj = (T)formatter.Deserialize(stream);
                stream.Flush();
                stream.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("反序列化失败,原因:" + ex.Message);
            }
            return obj;
        }



        public Boolean hasMessage()
        {
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
            // return  number = 0?false:true;

        }
        public string GetMessageBuffer()
        {
            if (hasMessage())
            {
                count--;
                return messageQueue.Dequeue();
            }
            else
            {
                return null;
            }
        }
    }

    public class SocketClient
    {
        private Socket tcpClient;
        public SocketClient(string ip, int port)
        {
            tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("Send connect to server");
            IPAddress ipaddress = IPAddress.Parse(ip);
            EndPoint point = new IPEndPoint(ipaddress, port);
            tcpClient.Connect(point);
            Console.WriteLine("connected");


        }
        public void send(string a)
        {
            tcpClient.Send(Encoding.UTF8.GetBytes(a));
        }

        private string Serialize<T>(T obj)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                MemoryStream stream = new MemoryStream();
                formatter.Serialize(stream, obj);
                stream.Position = 0;
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                stream.Flush();
                stream.Close();
                return Convert.ToBase64String(buffer);
            }
            catch (Exception ex)
            {
                throw new Exception("序列化失败,原因:" + ex.Message);
            }
        }
        public void close()
        {
            tcpClient.Shutdown(SocketShutdown.Both);
            tcpClient.Close();
        }
    }
    [Serializable]
    public class Action
    {
        private double X_position;
        private int action_mark = 0;

        public Action(double X_position, int action_mark)
        {
            this.X_position = X_position;
            this.action_mark = action_mark;
        }

        public double getX_position()
        {
            return this.X_position;
        }
        public int getAction_mark()
        {
            return this.action_mark;
        }
    }
}