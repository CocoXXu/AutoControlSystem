using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Net;

namespace AutoControlSystem
{
    public delegate void TCPIPServerRecvedHandler(String flag, String strRecv);
    class SocketServerFixtureMac
    {
        private MySocket socketFixtureMac;
        private Dictionary<string, string> dReceiveData;
        private static object locker = new object();//
        public event TCPIPServerRecvedHandler Recved;
        private string GetIP()
        {
            string sip = GetIniFile.ReadIniData("PCIPAddress", "FixtureIP", "error", "config.ini");
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
       /*
        * call when app is open , open server to connect mac mini
        * */
        public bool openFixtureMacSocketServer()
        {
            bool bOpenResult = true;
            try
            {
                dReceiveData = new Dictionary<string, string>();
                string ip = GetIP();
                string port = GetIniFile.ReadIniData("PCIPAddress", "FixturePort", "error", "config.ini");
                socketFixtureMac = new MySocket(ip, Convert.ToInt32(port));
                socketFixtureMac.myEvent += new myDelegate(beginReadSocket);//all fixtureClient
                socketFixtureMac.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {

            }
            return bOpenResult;
        }
        public void beginReadSocket(Socket client)
        {
            if (Recved != null)
                Recved("1", client.RemoteEndPoint.ToString());//1-connect
        }
        public void sendCommandToFixture1(string command)
        {
            Dictionary<string, Socket> dClients = socketFixtureMac.getClient;
            string skey = getEndPointWithKey("MacFixture1");
            Socket FixtureClient = dClients[skey];
            socketFixtureMac.AsyncSend(FixtureClient, command);
        }

        public void sendCommandToFixture2(string command)
        {
            Dictionary<string, Socket> dClients = socketFixtureMac.getClient;
            string skey = getEndPointWithKey("MacFixture2");
            Socket FixtureClient = dClients[skey];
            socketFixtureMac.AsyncSend(FixtureClient, command);
        }

        public void sendCommandToFixture3(string command)
        {
            Dictionary<string, Socket> dClients = socketFixtureMac.getClient;
            string skey = getEndPointWithKey("MacFixture3");
            Socket FixtureClient = dClients[skey];
            socketFixtureMac.AsyncSend(FixtureClient, command);
        }
        public void sendCommandToFixture4(string command)
        {
            Dictionary<string, Socket> dClients = socketFixtureMac.getClient;
            string skey = getEndPointWithKey("MacFixture4");
            Socket FixtureClient = dClients[skey];
            socketFixtureMac.AsyncSend(FixtureClient, command);
        }

        public void sendCommandToFixtureWithStationName(string command,string stationName)
        {
            string sip = getIPWithStationName(stationName);
            string sEndpoint = "";
            Dictionary<string, Socket> dClients = socketFixtureMac.getClient;
            foreach (string akey in dClients.Keys)
            {
                if (akey.StartsWith(sip))
                {
                    sEndpoint = akey;
                    break;
                }
            }
            Socket FixtureClient = dClients[sEndpoint];
            socketFixtureMac.AsyncSend(FixtureClient, command);
        }

        
        private string getIPWithStationName(string stationName)
        {
            string sip = "";
            for (int i = 1; i < 5; i++)
            {
                string sectionName = "MacFixture" + i.ToString();
                string sStation = GetIniFile.ReadIniData(sectionName, "stationName", "error", "config.ini");
                if (sStation == stationName)
                {
                    sip = GetIniFile.ReadIniData(sectionName, "IPAddress", "error", "config.ini");
                    break;
                }
            }
            return sip;
        }

        
        public string readDateWithFixtureMacNumber(string FixtureMacNumber)
        {
            string sPLC = "";
            string splcEndpoint = getEndPointWithKey(FixtureMacNumber);
            lock (locker)
            {
                sPLC = dReceiveData[splcEndpoint];
                dReceiveData[splcEndpoint] = "";
            }
            return sPLC;
        }
        public string readDateWithStationName(string stationName)
        {
            string sPLC = "";
            string sip = getIPWithStationName(stationName);
            string sMacEndpoint="";
            foreach(string akey in dReceiveData.Keys)
            {
                if (akey.StartsWith(sip))
                {
                    sMacEndpoint = akey;
                    break;
                }
            }
            lock (locker)
            {
                sPLC = dReceiveData[sMacEndpoint];
                dReceiveData[sMacEndpoint] = "";
            }
            return sPLC;
        }

        private string getEndPointWithKey(string key)
        {
            string sip = GetIniFile.ReadIniData(key, "IPAddress", "error", "config.ini");
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
