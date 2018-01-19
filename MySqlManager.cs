using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace AutoControlSystem
{
    
    class MySqlManager  
    {
        private static MySqlManager instance;
        private static SQLiteConnection myConnection;
        private static object _lock = new object();
       
        private MySqlManager()
        {
            try
            {
                string path = Directory.GetCurrentDirectory() + "\\AutoControlSystem.sqlite";//获取当前根目录
                myConnection = new SQLiteConnection("data source = " + path);
                myConnection.Open();
            }catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                myConnection.Close();
            }
            
        }
        //Singleton mode to operate one sqlite, when create Singleton ,create a  AutoControlSystem.sqlite
        public static MySqlManager GetInstance()
        {
            if(instance == null)
            {
                lock (_lock)
                {
                    if (instance == null)
                        instance = new MySqlManager();
                }
            }
            return instance;
        }
        //need 2 table 
        public bool createTables()
        {
            bool bCreateResult = setupTable("FixtureTable", "(id integer PRIMARY KEY AUTOINCREMENT,fixtureName text NOT NULL,stationName text NOT NULL,PLCName text NOT NULL,IPAddress text NOT NULL,CurrentStatus text NOT NULL,passCount integer default 0,failCount integer default 0,testTime integer default 0,idleTime integer default 0,DUTID text,SN text,lastUpdateTime text)");
            if (bCreateResult)
            {
                bCreateResult = setupTable("testHistoryTable", @"(id integer PRIMARY KEY AUTOINCREMENT,
                                                               DUTID text NOT NULL,
                                                               SN test NOT NULL,
                                                               RetestStrategy text NOT NULL, 
                                                               FirstFixture text,
                                                               FirstResult text,
                                                               FirstBeginTime text,
                                                               FirstEndTime text,
                                                               SecondFixture text,
                                                               SecondResult text,
                                                               SecondBeginTime text,
                                                               SecondEndTime text,
                                                               ThirdFixture text,
                                                               ThirdResult text,
                                                               ThirdBeginTime text,
                                                               ThirdEndTime text)");
            }
            return bCreateResult;
        }
        //http://www.w3school.com.cn/sql/sql_top.asp
        /*INSERT INTO table_name (列1, 列2,...) VALUES (值1, 值2,....)*/
        /*UPDATE Person SET FirstName = 'Fred' WHERE LastName = 'Wilson' */
        /*DELETE FROM Person WHERE LastName = 'Wilson' */
        /*SELECT * FROM Persons WHERE City LIKE 'N%'*/
        private bool setupTable(string tableName, string TableKeys)
        {
            bool bCreateResult = true;
            try
            {
                //创表语句，IF NOT EXISTS防止创建重复的表，AUTOINCREMENT是自动增长关键字，real是数字类型
                myConnection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = myConnection;
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS " + tableName  + TableKeys;//"(id varchar(4),score int)";
                cmd.ExecuteNonQuery();
            }catch(Exception ex)
            {
                bCreateResult = false;
            }
            finally
            {
                myConnection.Close();
            }
            return bCreateResult;
        }
        //when open app / fix config file ,need create fixturename to fixture table, if no data , insert ,if same ,continue ,if different ,update
        public bool createFixtureNameToFixtureTable()
        {
            bool bInsert = true;
            try
            {
                myConnection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = myConnection;
                for (int i = 1; i < 5; i++)
                {
                    string sectionName = "MacFixture" + i.ToString();
                    string sStationName = GetIniFile.ReadIniData(sectionName, "stationName", "error", "config.ini");
                    string sPLCName = GetIniFile.ReadIniData(sectionName, "PLCName", "error", "config.ini");
                    string sIPAddress = GetIniFile.ReadIniData(sectionName, "IPAddress", "error", "config.ini");
                    
                    //stationName,PLCName,IPAddress
                    string selectCommand = "select stationName,PLCName,IPAddress from FixtureTable where fixtureName = '" + sectionName + "'";
                    cmd.CommandText = selectCommand;
                    int status = checkFixtureTable(cmd,sStationName,sPLCName,sIPAddress);
                    //id integer PRIMARY KEY AUTOINCREMENT,fixtureName text NOT NULL,stationName text NOT NULL,PLCName text NOT NULL,IPAddress text NOT NULL,CurrentStatus text NOT NULL,passCount integer default 0,failCount integer default 0,testTime integer default 0,idleTime integer default 0,DUTID text,SN text,lastUpdateTime text
                    if (status == 0)
                    {
                        string sqlcommand = @"insert into FixtureTable (fixtureName,stationName,PLCName,IPAddress,CurrentStatus) Values ('"
                                        + sectionName + "','" + sStationName + "','" + sPLCName + "','" + sIPAddress + "','init')";
                        cmd.CommandText = sqlcommand;
                    }else if(status == 2)
                    {
                        string sqlcommand = @"update FixtureTable set stationName='"+sStationName+
                                            "', PLCName='"+sPLCName+"',IPAddress='"+sIPAddress+ "',CurrentStatus = 'init' where fixtureName='"+sectionName+"'";
                        cmd.CommandText = sqlcommand;
                    }
                    else
                    {
                        continue;
                    }
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                //todo
                bInsert = false;
            }
            finally
            {
                myConnection.Close();
            }
            return bInsert;
        }
        /*
         * check fixturetable with config file 
         * if no data in fixturetable return 0, 
         * if  data is same with config ,then return 1 , if different 2
        **/   
         private int checkFixtureTable(SQLiteCommand sqliteCommand,string stationName,string PLCName,string IPAdress)
        {
            using (SQLiteDataReader reader = sqliteCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    if((reader[0].ToString() == stationName) &&(reader[1].ToString() == PLCName) && (reader[2].ToString() == IPAdress))
                    {
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }
        public bool updateTableWhenCycleStart()
        {
            bool bUpdate = true;
            string updateCommand = "update FixtureTable SET lastUpdateTime = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' where CurrentStatus = 'init'";
            try
            {
                myConnection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = myConnection;
                cmd.CommandText = updateCommand;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                //todo
                bUpdate = false;
            }
            finally
            {
                myConnection.Close();
            }
            return bUpdate;
        }
        /*
         * when re-mapping need update FixtureTable and config file
         **/
        public bool updateStationNameToFixtureTable(string fixtureName, string stationName,string ipAddress) {
            string supdateCommand =  "update FixtureTable set StationName = '"+ stationName + "',IPAddress='"+ipAddress+"' where fixtureName = '"+fixtureName+"'";
            return updateCommandWithSqlite(supdateCommand);
        }

        public bool updateCommandWithSqlite(string commandText)
        {
            bool bUpdateResult = true;
            try
            {
                myConnection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = myConnection;
                cmd.CommandText = commandText;
                cmd.ExecuteNonQuery();
            }catch(Exception ex)
            {
                bUpdateResult = false;
            }
            finally
            {
                myConnection.Close();
            }
            return bUpdateResult;
        }
        public Dictionary<string, string> getMessgaeWithSerialNum(string sn,string fixtureName){
            string selectCommand = "select DUTID from FixtureTable where SN ='"+sn+"' and fixtureName = '"+fixtureName+"'";
            Dictionary<string, string> dMessage = new Dictionary<string, string>();
            try
            {
                myConnection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = myConnection;
                cmd.CommandText = selectCommand;
                SQLiteDataReader reader = cmd.ExecuteReader();
                string dutID = "";
                if (reader.HasRows)
                {
                    reader.Read();
                    dutID = reader[0].ToString();
                }

                
                if (dutID == "")
                    return dMessage;
                selectCommand = "select * from  where DUTID='" + dutID + "'";
                cmd.CommandText = selectCommand;
                SQLiteDataReader reader1 = cmd.ExecuteReader();
                if (reader1.HasRows)
                {
                    reader1.Read();
                    dMessage["DUTID"] = reader1[1].ToString();
                    dMessage["SN"] = reader1[2].ToString();
                    dMessage["RetestStrategy"] = reader1[3].ToString();

                    dMessage["FirstFixture"] = reader1[4].ToString();
                    dMessage["FirstResult"] = reader1[5].ToString();
                    dMessage["FirstBeginTime"] = reader1[6].ToString();
                    dMessage["FirstEndTime"] = reader1[7].ToString();

                    dMessage["SecondFixture"] = reader1[8].ToString();
                    dMessage["SecondResult"] = reader1[9].ToString();
                    dMessage["SecondBeginTime"] = reader1[10].ToString();
                    dMessage["SecondEndTime"] = reader1[11].ToString();

                    dMessage["ThirdFixture"] = reader1[12].ToString();
                    dMessage["ThirdResult"] = reader1[13].ToString();
                    dMessage["ThirddBeginTime"] = reader1[14].ToString();
                    dMessage["ThirdEndTime"] = reader1[15].ToString();
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                myConnection.Close();
            }
            return dMessage;
        }

        //get all idle fixture and order by test count, when needed should test in less count fixture
        public List<string> getIdleFixturesFromFixtureTable(){
            string selectCommand = @"select fixtureName,passCount+failCount as totalcount from FixtureTable where CurrentStatus='idle' or CurrentStatus='init' ORDER BY totalcount";
            List<string> lIdleFixture = new List<string>();
            try
            {
                myConnection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = myConnection;
                cmd.CommandText = selectCommand;
                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lIdleFixture.Add(reader[0].ToString());
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                myConnection.Close();
            }
            return lIdleFixture;

        }

        //select pass/fail count test/idle time to show in ui
          public Dictionary<string,Dictionary<string,string>>  getFixturesTestCountFromFixtureTable()
          {
             Dictionary<string, Dictionary<string, string>> dFixtureTable = new Dictionary<string, Dictionary<string, string>>();
                string selectCommand = @"select fixtureName,passCount,failCount,testTime,idleTime from FixtureTable";
             try
            {
                myConnection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = myConnection;
                cmd.CommandText = selectCommand;
                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.HasRows)
                {
                    reader.Read();
                    Dictionary<string, string> dFixtureMessage = new Dictionary<string, string>();
                    dFixtureMessage["passCount"] = reader[1].ToString();
                    dFixtureMessage["failCount"] = reader[2].ToString();
                    dFixtureMessage["testTime"] = reader[2].ToString();
                    dFixtureMessage["idleTime"] = reader[2].ToString();
                    dFixtureTable[reader[0].ToString()] = dFixtureMessage;
                }
             }catch(Exception ex)
            {

            }
            finally
            {
                myConnection.Close();
            }
            return dFixtureTable;
        }
        private string DateDiff(DateTime DateTime1,DateTime DateTime2)
        {
            string dateDiff = null;
           
            TimeSpan ts1 = new TimeSpan(DateTime1.Ticks);
            TimeSpan ts2 = new TimeSpan(DateTime2.Ticks);
            TimeSpan ts = ts1.Subtract(ts2).Duration();
            dateDiff = ts.Days.ToString("X") + ts.Hours.ToString("X") + ts.Minutes.ToString("X") + ts.Seconds.ToString("X");
            return dateDiff;
        }

        private TimeSpan TimeInterval(DateTime DateTime1, DateTime DateTime2)
        {
            TimeSpan ts1 = new TimeSpan(DateTime1.Ticks);
            TimeSpan ts2 = new TimeSpan(DateTime2.Ticks);
            TimeSpan ts = ts1.Subtract(ts2).Duration();
            return ts;
        }
        public string updateFixtureTableWhenBeginTest(DateTime date, string serialNum, string fixtureName, string sDUTID)
        {
            DateTime DateTime1 = Convert.ToDateTime("1970-01-01 00:00:00");//yyyy-MM-dd hh:mm:ss
            string dateDiff = DateDiff(DateTime1,date);
            if (sDUTID == "") {
                sDUTID = dateDiff + serialNum;//new a unique DUTID(conatains time and serialnum)
            }
            string sFixtureCommand = "select idleTime,lastUpdateTime from FixtureTable where fixtureName='"+ fixtureName+"'";
            try
            {
                myConnection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = myConnection;
                if (serialNum == "")
                {
                    string selectSerial = "select SN from testHistoryTable where DUTID= '" + sDUTID + "'";
                    cmd.CommandText = selectSerial;
                    SQLiteDataReader readerSerial = cmd.ExecuteReader();
                    while (readerSerial.Read())
                    {
                        serialNum = readerSerial[0].ToString();
                    }
                    readerSerial.Close();

                }
                cmd.CommandText = sFixtureCommand;
                SQLiteDataReader reader = cmd.ExecuteReader();
                string updateCommand = "";
                while (reader.Read())
                {
                    string idletime = reader[0].ToString();
                    string lastUpdateTime = reader[1].ToString();
                    string timeinterval = TimeInterval(Convert.ToDateTime(lastUpdateTime), DateTime.Now).TotalSeconds.ToString();
                    int iTime = Convert.ToInt32(idletime) + (int)Convert.ToSingle(timeinterval);
                    updateCommand = "update FixtureTable set CurrentStatus='test', idleTime = " + iTime + ",lastUpdateTime='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',DUTID='" + sDUTID + "',SN='" + serialNum+"' where fixtureName='" + fixtureName + "'";
                }
                reader.Close();
                cmd.CommandText = updateCommand;
                cmd.ExecuteNonQuery();

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                myConnection.Close();
            }
            return sDUTID;
        }

        public bool clearFixtureAndSerialNum(string serial ,string fixture)
        {
            string updatecommand = "update FixtureTable set SN ='',DUTID= '' where SN ='" + serial + "' or fixtureName = '" + fixture + "'";
            return (updateCommandWithSqlite(updatecommand));
        }
        public bool clearFixtureAndDutNum(string dut, string fixture)
        {
            string updatecommand = "update FixtureTable set SN ='',DUTID= '' where DUTID ='" + dut + "' or fixtureName = '" + fixture + "'";
            return (updateCommandWithSqlite(updatecommand));
        }
        /*
         * when insert a new test process,what we need to is :
         * 1.select idletime and lastupdatetime from fixturetable
         * 2.update fixture table the sn ,fixturename,dutid,set idle time = idletime + currenttime - lastupdatetime
         * 3.insert test history the sn ,firstfixturename ,dutid,firststarttime,RetestStrategy
         */
        public bool insertFisrtTestHistory(string serialNum,string fixtureName,string retestStrategy)
        {
            DateTime timeNow = DateTime.Now;
            if (!clearFixtureAndSerialNum(serialNum, fixtureName))
                return false;
            string sDUTID = updateFixtureTableWhenBeginTest(timeNow, serialNum, fixtureName,"");//insert fixtureTable and get a DUTID
            if (sDUTID == @"") {
                return false;
              }
            string scommand = "insert into testHistoryTable (DUTID,SN,RetestStrategy,FirstFixture,FirstBeginTime) values ('"+sDUTID+"','"+serialNum+"','"+ retestStrategy+"','"+ fixtureName+"','"+timeNow.ToString("yyyy-MM-dd HH:mm:ss") + "')";
            return(updateCommandWithSqlite(scommand));
        }
        public string updateFirstTestResultHistory(string serialNum, string fixtureName, string result)
        {
            string dateNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string sDUTID = updateFixtureTableWhenFinishTestWithSerialNum(serialNum, fixtureName, result, dateNow);
            if (sDUTID == "")
                return "";
            else if (sDUTID.StartsWith("rhacok"))
            {
                sDUTID = sDUTID.Remove(0, 5);
                return sDUTID;
            }
            string scommand = "update testHistoryTable set FirstResult = '" + result + "',FirstEndTime='" +dateNow + "' where DUTID = '" + sDUTID + "'";
            if (updateCommandWithSqlite(scommand))
                return sDUTID;
            else
                return "";

        }
        public string updateFixtureTableWhenFinishTestWithSerialNum(string serialNum,string fixtureName,string result,string sdate)
        {
            string sfixtureCommand = "select CurrentStatus,DUTID,testTime,lastUpdateTime,passCount,failCount from FixtureTable where SN='"+ serialNum + "' and fixtureName ='"+fixtureName+"'";
            try
            {
                myConnection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = myConnection;
                cmd.CommandText = sfixtureCommand;
                SQLiteDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string currentStatus = reader[0].ToString();
                    string sDUTID = reader[1].ToString();
                    string testtime = reader[2].ToString();
                    string lastUpdateTime = reader[3].ToString();
                    DateTime dateLastUpdate = Convert.ToDateTime(lastUpdateTime);
                    //DateTime dateNow = DateTime.Now;

                    TimeSpan timeInterval = TimeInterval(dateLastUpdate, Convert.ToDateTime(sdate));
                    int itestTime = Convert.ToInt32(testtime) + (int)Convert.ToSingle(timeInterval.TotalSeconds.ToString());
                    if (currentStatus != "test")
                    {
                        return "rhacok"+ sDUTID;
                    }
                    string updateCommand = "";
                    if (result == "pass")
                    {
                        string passCount = reader[4].ToString();
                        int ipassCount = Convert.ToInt32(passCount) + 1;
                        updateCommand = "update FixtureTable set CurrentStatus='pass',testtime=" + passCount + ",passCount =" + ipassCount + ",lastUpdateTime ='" + sdate + "' where SN='" + serialNum + "'";
                    }
                    else
                    {
                        string failCount = reader[5].ToString();
                        int ifailCount = Convert.ToInt32(failCount) + 1;
                        updateCommand = "update FixtureTable set CurrentStatus='fail', testtime=" + itestTime + ",failCount =" + ifailCount + ",lastUpdateTime ='" + sdate + "' where SN='" + serialNum + "'";
                    }
                    reader.Close();
                    cmd.CommandText = updateCommand;
                    cmd.ExecuteNonQuery();
                    return sDUTID;
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                myConnection.Close();
            }
            return "";
        }

        public Dictionary<string,string> getNextFixtureWithDutid(string dutID)
        {
            string selectCommand = "select RetestStrategy,FirstFixture,FirstResult,SecondFixture,SecondResult,ThirdFixture,ThirdResult from testHistoryTable where DUTID='" + dutID + "'";
            List<string> ildeFixture = getIdleFixturesFromFixtureTable();
            Dictionary<string, string> dNextFixture = new Dictionary<string, string>();
            try
            {
                myConnection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = myConnection;
                cmd.CommandText = selectCommand;
                SQLiteDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string retest = reader[0].ToString();
                    string fixture1 = reader[1].ToString();
                    string fixture2 = reader[3].ToString();
                    string fixture3 = reader[5].ToString();
                    if ((reader[4].ToString() == "fail") && (reader[5].ToString() ==""))//2nd fail
                    {
                       foreach(string afixture in ildeFixture)
                        {
                            if((afixture != fixture2) &&(afixture != fixture1)){
                                dNextFixture["testTime"] = "3";
                                dNextFixture["fixtureName"] = afixture;
                            }
                        }
                    }else if((reader[2].ToString() == "fail") && (reader[4].ToString() == ""))//1st fail
                    {
                        dNextFixture["testTime"] = "2";
                        if (retest.StartsWith("AA"))
                        {
                            dNextFixture["fixtureName"] = fixture1;
                        }
                        else
                        {
                            foreach (string afixture in ildeFixture)
                            {
                                if ((afixture != fixture1))
                                {
                                    dNextFixture["fixtureName"] = afixture;
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {

            }
            finally
            {
                myConnection.Close();
            }
            return dNextFixture;
        }
        public bool updateSecondTestHistory(string fixtureName,string DUTID)
        {
            DateTime dateNow = DateTime.Now;
            if (!clearFixtureAndDutNum(DUTID, fixtureName))
                return false;
            string sDUTID = updateFixtureTableWhenBeginTest(dateNow,"",fixtureName,DUTID);
            if(sDUTID == "")
            {
                return false;
            }
            string scommand = "update testHistoryTable set SecondFixture = '"+fixtureName+"',SecondBeginTime='"+dateNow.ToString("yyyy-MM-dd HH:mm:ss") +"' where DUTID = '"+ sDUTID + "'";
            return (updateCommandWithSqlite(scommand));
        }

        public string updateSecondTestResultHistory(string serialNum,string fixtureName,string result)
        {
            DateTime dateNow = DateTime.Now;
            string sDUTID = updateFixtureTableWhenFinishTestWithSerialNum(serialNum, fixtureName, result, dateNow.ToString("yyyy-MM-dd HH:mm:ss"));
            if (sDUTID == "")
                return "";
            else if (sDUTID.StartsWith("rhacok"))
            {
                sDUTID = sDUTID.Remove(0, 5);
                return sDUTID;
            }
            string scommand = "update testHistoryTable SET SecondResult = '" + result+"',SecondEndTime='"+ dateNow.ToString("yyyy-MM-dd HH:mm:ss") +"' where DUTID = '"+ sDUTID+"'";
            if (updateCommandWithSqlite(scommand))
                return sDUTID;
            else
                return "";
        }

        public bool updateThirdTestHistory(string fixtureName,string DUTID)
        {
            DateTime dataNow = DateTime.Now;
            if (!clearFixtureAndDutNum(DUTID, fixtureName))
                return false;
            string sDUTID = updateFixtureTableWhenBeginTest(dataNow,"",fixtureName, DUTID);
            if (sDUTID == "")
                return false;
            string scommand = "update testHistoryTable SET ThirdFixture = '" + fixtureName + "',ThirdBeginTime='"+dataNow.ToString("yyyy-MM-dd HH:mm:ss")+ "' where DUTID = '"+ DUTID+"'";
            return (updateCommandWithSqlite(scommand));
        }

        public string updateThirdTestResultHistory(string serialNum,string fixtureName,string result)
        {
            string dateNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string sDUTID = updateFixtureTableWhenFinishTestWithSerialNum(serialNum, fixtureName, result, dateNow);
            if (sDUTID == "")
            {
                return "";
            }else if(sDUTID.StartsWith("rhacok"))
            {
                sDUTID = sDUTID.Remove(0, 5);
                return sDUTID;
            }
            string scommand = "update testHistoryTable SET ThirdResult = '" + result+"',ThirdEndTime ='"+ dateNow+"' where DUTID = '"+sDUTID+"'";
            if (updateCommandWithSqlite(scommand))
            {
                return sDUTID; ;
            }
            else
                return "";

        }

    }
}
