using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LukeFileUpload
{

    public partial class Form1 : Form
    {

        private AppDao mAppDao = new AppDao();
        private List<ItemData> mListData = new List<ItemData>();
        private String mLocFile;

        private const String EXE_VERSION = "1.0";

        public Form1()
        {
            InitializeComponent();
            setTitle("数据加载中，请稍等...");
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false; // 取消线程合法性检测            

            new Thread(new ThreadStart(loadData)).Start();
            var t = new Thread(new ParameterizedThreadStart(startCheckUp));
            t.SetApartmentState(ApartmentState.STA);
            t.Start(false);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void checkUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var t = new Thread(new ParameterizedThreadStart(startCheckUp));
            t.SetApartmentState(ApartmentState.STA);
            t.Start(true);
        }

        private void loadData()
        {
            //try
            //{
            mListData.Clear();
            dataGridViewMain.Rows.Clear();

            String response = HttpUtil.Get(HttpUtil.URL.FileConfigUrl, null);
            ResData resData = JsonConvert.DeserializeAnonymousType(response, new ResData());
            for (int i = 0; i < resData.data.Count; i++)
            {
                ItemData item = resData.data[i];
                response = HttpUtil.Get(item.desUrl, null);
                UpdateInfo info = JsonConvert.DeserializeAnonymousType(response, new UpdateInfo());
                item.updateInfo = info;
                item.locPath = mAppDao.getAppPathById(Int32.Parse(item.id));
                mListData.Add(item);
            }

            dataGridViewMain.Rows.Clear();
            mAppDao.updateOrInsert(mListData);

            for (int i = 0; i < mListData.Count; i++)
            {
                ItemData one = mListData[i];

                int index = dataGridViewMain.Rows.Add();
                dataGridViewMain.Rows[index].Cells[0].Value = one.name;
                dataGridViewMain.Rows[index].Cells[1].Value = one.updateInfo.version;
                dataGridViewMain.Rows[index].Cells[2].Value = one.locPath;
            }
            //}
            //catch (Exception e1)
            //{
            //    showMsg("数据加载失败\r\n" + e1.Message);
            //}
            setTitle(null);
        }

        private void startCheckUp(object obj)
        {
            bool isNeedTipAllReadyLatest = (bool)obj;
            try
            {
                String response = HttpUtil.Get(HttpUtil.URL.UpdateUrl, null);
                UpdateInfo data = JsonConvert.DeserializeAnonymousType(response, new UpdateInfo());
                if (PkgUtil.compareVersion(data.version, EXE_VERSION) > 0)
                {
                    if (MessageBox.Show(String.Format("当前版本:{0}\n最新版本:{1}\n是否下载?", EXE_VERSION, data.version), "升级提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        SaveFileDialog dialog = new SaveFileDialog();
                        dialog.FileName = data.appUrl.Substring(data.appUrl.LastIndexOf("/") + 1);
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            HttpDownloadFile(data.appUrl, dialog.FileName, Int32.Parse(data.size));
                        }
                    }
                }
                else
                {
                    if (isNeedTipAllReadyLatest)
                    {
                        MessageBox.Show("已经是最新版本 " + EXE_VERSION);
                    }
                }
            }
            catch (Exception e1)
            {
                showMsg(e1.Message);
            }

        }

        private void showMsg(String msg)
        {
            MessageBox.Show(msg, "提示");
        }

        private void setTitle(String msg)
        {
            if (msg == null)
            {
                this.Text = "Form";
            }
            else
            {
                this.Text = msg;
            }
        }

        private void dataGridViewMain_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == 3) // 上传
            {
                String locPath = mListData[e.RowIndex].locPath;
                if (String.IsNullOrEmpty(locPath))
                {
                    MessageBox.Show("请先设置本地apk路径");
                    return;
                }

                if (!File.Exists(locPath))
                {
                    MessageBox.Show("本地apk不存在,请先生成apk文件");
                    return;
                }

                String ver = getApkVersion(locPath);
                int compareRet = PkgUtil.compareVersion(ver, mListData[e.RowIndex].updateInfo.version);
                if (compareRet < 0)
                {
                    MessageBox.Show("当前版本小于远程服务器版本,无法上传");
                    return;
                }

                if (MessageBox.Show("远程版本:" + mListData[e.RowIndex].updateInfo.version + "\n本地版本:" + ver + "\n确认上传吗?", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    UploadInfo info = new UploadInfo();
                    info.id = mListData[e.RowIndex].id;
                    info.version = ver;
                    mLocFile = locPath;
                    new Thread(new ParameterizedThreadStart(startUploadFile)).Start(info);
                }

            }
            else if (e.ColumnIndex == 4) // 下载
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Filter = "apk文件| *.apk|任意文件| *.*";
                dialog.FileName = mListData[e.RowIndex].name;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    new Thread(new ParameterizedThreadStart(startDownFile)).Start(new String[] { mListData[e.RowIndex].updateInfo.appUrl, dialog.FileName, mListData[e.RowIndex].updateInfo.size }); //  (mListData[e.RowIndex].updateInfo.appUrl, dialog.FileName))
                }
            }
        }

        private void dataGridViewMain_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == 2)
            {
                String locaPath = mListData[e.RowIndex].locPath;
                if (!String.IsNullOrEmpty(locaPath) && File.Exists(locaPath))
                {
                    openFileDialog1.InitialDirectory = Path.GetDirectoryName(locaPath);
                }
                openFileDialog1.Title = "打开android studio中项目所有的apk文件";
                openFileDialog1.Filter = "apk文件|*.apk";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    mAppDao.updateLocPath(Int32.Parse(mListData[e.RowIndex].id), openFileDialog1.FileName);
                    mListData[e.RowIndex].locPath = openFileDialog1.FileName;
                    dataGridViewMain.Rows[e.RowIndex].Cells[2].Value = openFileDialog1.FileName;
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
        }


        private void startDownFile(Object obj)
        {
            String[] strs = (String[])obj;
            HttpDownloadFile(strs[0], strs[1], Int32.Parse(strs[2]));
        }

        private string HttpDownloadFile(string url, string path, int filesize)
        {
            setTitle("开始下载");
            // 设置参数
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

            //发送请求并获取相应回应数据
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            //直到request.GetResponse()程序才开始向目标网页发送Post请求
            Stream responseStream = response.GetResponseStream();

            //创建本地文件写入流
            Stream stream = new FileStream(path, FileMode.Create);

            byte[] bArr = new byte[1024];
            int totalSize = 0;
            int size = responseStream.Read(bArr, 0, (int)bArr.Length);
            totalSize = size;
            while (size > 0)
            {
                setTitle(String.Format("已下载{0}%", totalSize * 100 / filesize));
                stream.Write(bArr, 0, size);
                size = responseStream.Read(bArr, 0, (int)bArr.Length);
                totalSize += size;
            }
            stream.Close();
            responseStream.Close();
            setTitle(null);
            return path;
        }

        private String getApkVersion(String apkFile)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "aapt.exe";
            process.StartInfo.Arguments = "dump badging " + apkFile;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();

            //process.StandardInput.WriteLine(c);
            //process.StandardInput.AutoFlush = true;
            //process.StandardInput.WriteLine("exit");

            StreamReader reader = process.StandardOutput;//截取输出流

            string output = reader.ReadLine();//每次读取一行            

            if (!String.IsNullOrEmpty(output))
            {
                output = output.Substring(output.IndexOf("versionName='") + 13);
                output = output.Substring(0, output.IndexOf("'"));
                return output;
            }

            return null;
        }

        private void dataGridViewMain_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(MousePosition.X, MousePosition.Y);
            }
        }

        private void 刷新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadData();
        }

        private const String SERVER_IP = "zjjd.myhiott.com";
        private const int SERVER_PORT1 = 51007;
        private const int SERVER_PORT2 = 51008;

        private void startRecvData(object obj)
        {
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(SERVER_IP, SERVER_PORT2);
            NetworkStream ns = tcpClient.GetStream();

            byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
            ns.Write(jsonBytes, 0, jsonBytes.Length);

            {
                byte[] buf = new byte[1024];
                int r = ns.Read(buf, 0, buf.Length);
                if (r > 0)
                {
                    String txt = System.Text.Encoding.Default.GetString(buf, 0, r);
                    SendRetData data = JsonConvert.DeserializeAnonymousType(txt, new SendRetData());
                    if (data.isSuccess())
                    {
                        MessageBox.Show("上传成功");
                    }
                    else
                    {
                        MessageBox.Show("上传失败:" + data.description);
                    }
                }
            }

            ns.Close();
            tcpClient.Close();
        }

        private void startUploadFile(object obj)
        {
            UploadInfo upInfo = (UploadInfo)obj;
            FileInfo info = new FileInfo(mLocFile);
            upInfo.size = info.Length.ToString();
            upInfo.md5 = MD5Util.GetMD5(mLocFile);


            TcpClient tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect(SERVER_IP, SERVER_PORT1);
            }
            catch (Exception e)
            {
                MessageBox.Show("连接服务器" + SERVER_IP + " 失败");
                return;
            }
            NetworkStream ns = tcpClient.GetStream();
            /* 开启接收进程 */
            new Thread(new ParameterizedThreadStart(startRecvData)).Start(obj);

            setTitle("开始上传");
            using (FileStream fsRead = new FileStream(mLocFile, FileMode.Open))
            {
                byte[] buf = new byte[1024];
                try
                {
                    int totalSize = 0;
                    while (true)
                    {
                        int r = fsRead.Read(buf, 0, buf.Length);
                        if (r < 1) break;
                        totalSize += r;
                        setTitle(String.Format("已经上传{0}%", totalSize * 100 / Int32.Parse(upInfo.size)));
                        ns.Write(buf, 0, r);
                    }
                }
                catch (Exception e)
                {
                }
            }

            setTitle(null);
            ns.Close();
            tcpClient.Close();
        }
    }
}
