using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace AutoControlSystem
{
    public delegate void myDelegate(Socket client);
    
    class MySocket
    {
        private string _IPAdress;
        private Int32 _Port;
        private Dictionary<string, Socket> dClients;
        public event myDelegate myEvent;
        public string m_server_type = string.Empty;
        byte[] buf = new byte[1024];
        byte[] buf1 = new byte[1024];
       
        private Socket m_SocketServer = null;
        private Dictionary<Socket, string> dReceiveData;
        private static object locker = new object();//
        public MySocket(string ipAdress, Int32 port)
        {
            _IPAdress = ipAdress;
            _Port = port;
            dClients = new Dictionary<string, Socket>();
            dReceiveData = new Dictionary<Socket, string>();
        }
        public void Start()
        {
            //创建套接字  
            m_SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Parse(_IPAdress);
            IPEndPoint m_IPEndPoint = new IPEndPoint(ipAddress, _Port);
            m_SocketServer.Bind(m_IPEndPoint);
            m_SocketServer.Listen(6);
            m_SocketServer.BeginAccept(new AsyncCallback(Accept), m_SocketServer);
        }


        private void Accept(IAsyncResult ia)
        {
            try
            {
                m_SocketServer = ia.AsyncState as Socket;
                Socket socketClient = m_SocketServer.EndAccept(ia);
                if (myEvent != null)
                {
                    myEvent(socketClient);
                }
                dClients[socketClient.RemoteEndPoint.ToString()] = socketClient;
                m_SocketServer.BeginAccept(new AsyncCallback(Accept), m_SocketServer);
                socketClient.BeginReceive(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(Receive), socketClient);
            }
            catch
            { throw; }
        }


        private void Receive(IAsyncResult ia)
        {
            try
            {
                Socket socketClient = ia.AsyncState as Socket;
                int count = socketClient.EndReceive(ia);
                socketClient.BeginReceive(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(Receive), socketClient);
                string context = Encoding.ASCII.GetString(buf, 0, count);
                lock (locker)
                {
                    if (dReceiveData.ContainsKey(socketClient))
                    {
                        context = dReceiveData[socketClient] + context;
                    }
                    dReceiveData[socketClient] = context;
                }
                
                //Recved(m_server_type, context);

                if (context.Length > 7)
                {
                    // this.

                }
            }
            catch
            {
            }

        }

        public string AsyncReveive(Socket client)
        {
            lock (locker)
            {
                string sReceive = dReceiveData[client];
                dReceiveData[client] = "";
                return sReceive;
            }
            
        }

        public void AsyncSend(Socket socketClient, string result)
        {
            buf1 = Encoding.ASCII.GetBytes(result);

            try
            {
                socketClient.Send(buf1, 0, buf1.Length, SocketFlags.None);
            }
            catch
            { }
        }
        
        public Dictionary<string, Socket> getClient
        {
            get
            {
                return dClients;
            }

        }
    }
}
