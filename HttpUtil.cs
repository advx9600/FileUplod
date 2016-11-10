using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LukeFileUpload
{
    class MD5Util
    {
        public static string GetMD5(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, System.IO.FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }  
    }
    class PkgUtil
    {
        private static String VERSION = "1.0";

        public static String getVersion(){
            return VERSION;
        }

        public static int compareVersion(String version1, String version2)
        {
            if (version1.Equals(version2))
            {
                return 0;
            }

            String[] version1Array = version1.Split('.');
            String[] version2Array = version2.Split('.');

            int index = 0;
            int minLen = Math.Min(version1Array.Length, version2Array.Length);
            int diff = 0;  

            while (index < minLen
                    && (diff = Int32.Parse(version1Array[index]) - Int32.Parse(version2Array[index])) == 0)
            {
                index++;
            }

            if (diff == 0)
            {
                for (int i = index; i < version1Array.Length; i++)
                {
                    if (Int32.Parse(version1Array[i]) > 0)
                    {
                        return 1;
                    }
                }

                for (int i = index; i < version2Array.Length; i++)
                {
                    if (Int32.Parse(version2Array[i]) > 0)
                    {
                        return -1;
                    }
                }

                return 0;
            }
            else
            {
                return diff > 0 ? 1 : -1;
            }
        }
    }
    class HttpUtil
    {
        public static class URL
        {
            private static String BaseUrl = "http://zjjd.myhiott.com:8081/upgrade/default/";
            public static String UpdateUrl = BaseUrl + "csharp/appupload/update.json";
            public static String FileConfigUrl = BaseUrl + "csharp/appupload/file_config.json";
        }

        public static string Get(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (String.IsNullOrEmpty(postDataStr) ? "" : "?" + postDataStr));
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }
    }
}
