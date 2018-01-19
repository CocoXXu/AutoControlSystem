using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Data;
namespace AutoControlSystem
{
    public static class GetIniFile
    {

        //   [DllImport("kernel32")]

        ////private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        //   private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);


        //   [DllImport("kernel32")]
        //   //private static extern long GetPrivateProfileString(string section, string key,
        //   //    string def, StringBuilder retVal, int size, string filePath);
        //   private static extern long GetPrivateProfileString(string section, string key,
        //       string def, [MarshalAs(UnmanagedType.LPArray)]byte[] buffer, int size, string filePath);

        [DllImport("kernel32")]
        public static extern bool WritePrivateProfileString(byte[] section, byte[] key, byte[] val, string filePath);
        [DllImport("kernel32")]
        public static extern int GetPrivateProfileString(byte[] section, byte[] key, byte[] def, byte[] retVal, int size, string filePath);
        //与ini交互必须统一编码格式
        private static byte[] getBytes(string s, string encodingName)
        {
            return null == s ? null : Encoding.GetEncoding(encodingName).GetBytes(s);
        }

        public static string ReadString(string section, string key, string def, string fileName, string encodingName = "utf-8", int size = 1024)
        {
            byte[] buffer = new byte[size];
            int count = GetPrivateProfileString(getBytes(section, encodingName), getBytes(key, encodingName), getBytes(def, encodingName), buffer, size, fileName);
            return Encoding.GetEncoding(encodingName).GetString(buffer, 0, count).Trim();
        }
        public static bool WriteString(string section, string key, string value, string fileName, string encodingName = "utf-8")
        {
            return WritePrivateProfileString(getBytes(section, encodingName), getBytes(key, encodingName), getBytes(value, encodingName), fileName);
        }

        public static string ReadIniData(string Section, string Key, string NoText, string iniFilePath)
        {
            string path = Directory.GetCurrentDirectory();//获取当前根目录
            iniFilePath = path + "//" + iniFilePath;
            if (File.Exists(iniFilePath))
            {
                try
                {
                   return ReadString(Section, Key, "", iniFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return String.Empty;
                }

            }
            else
            {
                return String.Empty;
            }
        }
    }
}
