using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        private Queue<string> messageQueue = new Queue<string>();

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="ip">������IP</param>
        /// <param name="port">�����Ķ˿�</param>
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
                //1.0 ʵ�����׽���(IP4Ѱ��Э��,��ʽЭ��,TCPЭ��)
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //2.0 ����IP����
                IPAddress address = IPAddress.Parse(_ip);
                //3.0 ��������˿�,����ip�Ͷ˿�
                IPEndPoint endPoint = new IPEndPoint(address, _port);
                //4.0 ���׽���
                _socket.Bind(endPoint);
                //5.0 �������������
                _socket.Listen(int.MaxValue);
                Console.WriteLine("����{0}��Ϣ�ɹ�", _socket.LocalEndPoint.ToString());
                //6.0 ��ʼ����
                Thread thread = new Thread(ListenClientConnect);
                thread.Start();

            }
            catch (Exception ex)
            {
                Console.WriteLine("����,{0}", ex.ToString());
            }
        }
        /// <summary>
        /// �����ͻ�������
        /// </summary>
        private void ListenClientConnect()
        {
            try
            {
                while (true)
                {
                    //Socket������������
                    Socket clientSocket = _socket.Accept();
                    clientSocket.Send(Encoding.UTF8.GetBytes("����˷�����Ϣ:"));
                    Thread thread = new Thread(ReceiveMessage);
                    thread.Start(clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("����,{0}", ex.ToString());
            }
        }

        /// <summary>
        /// ���տͻ�����Ϣ
        /// </summary>
        /// <param name="socket">���Կͻ��˵�socket</param>
        private void ReceiveMessage(object socket)
        {
            Socket clientSocket = (Socket)socket;
            while (true)
            {
                try
                {
                    //��ȡ�ӿͻ��˷���������
                    int length = clientSocket.Receive(buffer);
                    var data = Encoding.UTF8.GetString(buffer, 0, length);
                    messageQueue.Enqueue(data);
                    Console.WriteLine("���տͻ���{0},��Ϣ{1}", clientSocket.RemoteEndPoint.ToString(), data);
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
        public Boolean hasMessage()
        {
            return messageQueue.Count() != 0;

        }
        public string GetMessageBuffer()
        {
            if (hasMessage())
            {
                return messageQueue.Dequeue();
            }
            else
            {
                return null;
            }
        }
    }
}