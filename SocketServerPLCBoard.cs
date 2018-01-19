using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;

namespace AutoControlSystem
{
    class SocketServerPLCBoard
    {
        private MySocket socketPLCBoard;
        private Dictionary<string, string> dReceiveData;
        private static object locker = new object();//

        private string GetIP()
         {
            string sip = GetIniFile.ReadIniData("PCIPAddress", "FixtureIP","error","config.ini");
            string hostName = Dns.GetHostName();//本机名   
            IPAddress[] addressList = Dns.GetHostAddresses(hostName);//会返回所有地址，包括IPv4和IPv6 
            string ipAddress = "";
            foreach (IPAddress ip in addressList)  
            {
                if (ip.ToString().StartsWith(sip))
                {
                    ipAddress = ip.MapToIPv4().ToString();
                    break;
                }
            }
            return ipAddress;
     }


        public bool openPLCBoardSocketServer()
        {
            bool bOpenResult = true;
            try
            {
                dReceiveData = new Dictionary<string, string>();
                string ip = GetIP();
                string port = GetIniFile.ReadIniData("PCIPAddress", "FixturePort", "error", "config.ini");
                socketPLCBoard = new MySocket(ip, Convert.ToInt32(port));
                socketPLCBoard.myEvent += new myDelegate(beginReadSocket);//plc/robot/camera/scanner is connected then read
                socketPLCBoard.Start();
            }
            catch(Exception ex)
            {

            }
            finally
            {

            }
            return bOpenResult;
        }
        public void beginReadSocket(Socket client)
        {
            while (true)
            {
                string sReceive="";
                string sCurrentReceive = socketPLCBoard.AsyncReveive(client);
                lock (locker)
                {
                    if (dReceiveData.ContainsKey(client.RemoteEndPoint.ToString()))
                    {
                        sReceive = dReceiveData[client.RemoteEndPoint.ToString()];
                    }
                    sReceive = sReceive + sCurrentReceive;
                    dReceiveData[client.RemoteEndPoint.ToString()] = sReceive;
                }
               
                //todo if error need create event to report error
            }
        }
        public void sendCommandToRobot(string command)
        {
            Dictionary<string,Socket> dClients = socketPLCBoard.getClient;
            string skey = getEndPointWithKey("Robot");
            Socket robotClient = dClients[skey];
            socketPLCBoard.AsyncSend(robotClient, command);           
        }
        public void sendCommandToPLC(string command)
        {
            Dictionary<string, Socket> dClients = socketPLCBoard.getClient;
            string skey = getEndPointWithKey("PLC");
            Socket robotClient = dClients[skey];
            socketPLCBoard.AsyncSend(robotClient, command);
        }
        public void sendCommandToScanner(string command)
        {
            Dictionary<string, Socket> dClients = socketPLCBoard.getClient;
            string skey = getEndPointWithKey("Scanner");
            Socket robotClient = dClients[skey];
            socketPLCBoard.AsyncSend(robotClient, command);
        }
        public void sendCommandToCamera(string command)
        {
            Dictionary<string, Socket> dClients = socketPLCBoard.getClient;
            string skey = getEndPointWithKey("Camera");
            Socket robotClient = dClients[skey];
            socketPLCBoard.AsyncSend(robotClient, command);
        }

        public string readDateFromPLC()
        {
            return readDateWithClientName("PLC");
        }
        public string readDateFromRobot()
        {
            return readDateWithClientName("Robot");
        }
        public string readDateFromScanner()
        {
            return readDateWithClientName("Scanner");
        }
        public string readDateFromCamera()
        {
            return readDateWithClientName("Camera");
        }
        private string readDateWithClientName(string clientName)
        {
            string sPLC = "";
            string splcEndpoint = getEndPointWithKey(clientName);
            lock (locker)
            {
                sPLC = dReceiveData[splcEndpoint];
                dReceiveData[splcEndpoint] = "";
            }
            return sPLC;
        }

        private string getEndPointWithKey(string key)
        {
            string sip = GetIniFile.ReadIniData("PLCIPAddress", key, "error", "config.ini");
            string sEndpoint = "";
            foreach (string akey in dReceiveData.Keys)
            {
                if (akey.StartsWith(sip))
                {
                    sEndpoint = akey;
                    break;
                }
            }
            return sEndpoint;
        }
        
    }
}
