using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;

namespace AutoControlSystem
{
    public partial class Form1 : Form
    {
        private SocketServerFixtureMac socketMac;
        private SocketServerPLCBoard socketPLC;
        public Form1()
        {
            InitializeComponent();
            //TCPIPServer aserver = new TCPIPServer("192.168.0.42", 8008);
            //aserver.StartListening();


            if (!PrepareFuction())//need 
            {
                MessageBox.Show("APP Init Error .");
            }
            //test code 
            //when cycle start set last updatetime = cyclestart time to cal idle time and free time
            //MySqlManager.GetInstance().updateTableWhenCycleStart();
            //MySqlManager.GetInstance().updateStationNameToFixtureTable("MacFixture1", "RHAC_01_011", "10.00.0.0");
            //List<string> test = MySqlManager.GetInstance().getIdleFixturesFromFixtureTable();

            //MySqlManager.GetInstance().updateFixtureTableWhenBeginTest(DateTime.Now, "123456789", test[0], "");
            //MySqlManager.GetInstance().insertFisrtTestHistory("FCC123456", test[0], "AAB");
            //string suutid = MySqlManager.GetInstance().updateFirstTestResultHistory("FCC123456", test[0], "fail");
            //MySqlManager.GetInstance().updateSecondTestHistory(test[0], suutid);
            //string uutid1 = MySqlManager.GetInstance().updateSecondTestResultHistory("FCC123456", test[0], "fail");
            //MySqlManager.GetInstance().updateThirdTestHistory(test[1], uutid1);
            //string uutid2 = MySqlManager.GetInstance().updateThirdTestResultHistory("FCC123456", test[1], "pass");


            //Console.WriteLine(test);

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //MySocket socket = new MySocket("192.168.0.42", 8008);
            //socket.myEvent += new myDelegate(run);
            //socket.Start();

        }

        private bool PrepareFuction()
        {
            bool bPrepare = false;
            //open mac mini socket server
            socketMac = new SocketServerFixtureMac();
            bPrepare = socketMac.openFixtureMacSocketServer();
            if (!bPrepare)
                return bPrepare;
            //open plc board socket server
            socketPLC = new SocketServerPLCBoard();
            bPrepare=socketPLC.openPLCBoardSocketServer();
            if (!bPrepare)
                return bPrepare;
            //create sqlite to save data
            bPrepare =MySqlManager.GetInstance().createTables();
            if (!bPrepare)
                return bPrepare;
            //load config file mac mini message to sqlite
            bPrepare = MySqlManager.GetInstance().createFixtureNameToFixtureTable();
            return bPrepare;
        }

        private void eventAccept()
        {
            socketMac.Recved += new TCPIPServerRecvedHandler(updateUI);
        }

        private void updateUI(string flag, string value)
        {
            //if flag == 0 , client disconnect(heart beat error / connection break
            //if flag == 1 , client connect
            //if flag == 2 , recieve msg from client
        }
        public void run(Socket client)
        {
            Console.WriteLine(client.RemoteEndPoint.ToString());
        }
    }
}
