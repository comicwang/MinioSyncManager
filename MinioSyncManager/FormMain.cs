using MinioSyncCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinioSyncManager
{
    public partial class FormMain : Form
    {
        private MinioRestApi _restApi = null;
        private MinioRestApi _tragetrestApi = null;
        public FormMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //获取桶信息
            _restApi = new MinioRestApi(textBox1.Text, textBox2.Text, textBox3.Text);
            if (_restApi.CanConnect == false)
            {
                MessageBox.Show("连接信息不正确，请重新输入");
                return;
            }
            buckets[] bucks = _restApi.GetBuckets();
            if (bucks.Length > 0)
                btnExcute.Enabled = true;
            treeView1.Nodes.Clear();

            foreach (var item in bucks)
            {
                treeView1.Nodes.Add(item.name).Tag = item;
            }

        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //获取桶下级信息
            if (e.Node.Level == 0)
            {
                if (e.Node.Nodes.Count == 0)
                {
                    DocResult[] results = _restApi.GetObjects(e.Node.Text, string.Empty);
                    foreach (var item in results)
                    {
                        e.Node.Nodes.Add(item.name).Tag = item;
                    }
                }
                buckets bucketResult = e.Node.Tag as buckets;
                txtSourcePath.Text = bucketResult.name;
            }
            //获取下面级数信息
            else if (e.Node.Level > 0)
            {
                DocResult docResult = e.Node.Tag as DocResult;
                if (docResult.contentType == "")
                {
                    if (e.Node.Nodes.Count == 0)
                    {
                        DocResult[] results = _restApi.GetObjects(e.Node.FullPath.Split('\\')[0], e.Node.Text);
                        foreach (var item in results)
                        {
                            e.Node.Nodes.Add(item.name).Tag = item;
                        }
                    }
                    txtSourcePath.Text = e.Node.FullPath.Split('\\')[0] + "/" + docResult.name;
                }
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtBuckupPath.Text = folderBrowserDialog.SelectedPath;
                string text = txtBuckupPath.Text;
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
                txtBuckupPath.Enabled = true;
                linkLabel1.Enabled = true;
                groupBox5.Enabled = false;
            }
            else
            {
                txtBuckupPath.Enabled = false;
                linkLabel1.Enabled = false;
                groupBox5.Enabled = true;
            }

            SetExcuteEnable();
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            txtSourcePath.Enabled = radioButton4.Checked;
            SetExcuteEnable();
        }

        private ExcuteData _excuteData = null;

        private void btnExcute_Click(object sender, EventArgs e)
        {
            if (btnExcute.Text == "开始同步")
            {
                ExcuteData excuteData = new ExcuteData()
                {
                    modelAll = radioButton1.Checked,
                    pathAll = radioButton3.Checked,
                    buckupPath = txtBuckupPath.Text,
                    soucrPath = txtSourcePath.Text,
                    syncModel = radioButton5.Checked ? SyncModel.Buckup : SyncModel.Sync,
                    excuteTimes = (int)numericUpDown1.Value,
                    syncInfo = new MinioConnectionInfo()
                    {
                        uri = txtTargetUri.Text,
                        user = txtTargetUser.Text,
                        pwd = txtTargetPwd.Text
                    }
                };
                _excuteData = excuteData;
                //全量
                if (excuteData.modelAll)
                {
                    backgroundWorker1.RunWorkerAsync(excuteData);
                }
                //定时增量
                else
                {
                    _timer.Interval = excuteData.excuteTimes * 1000 * 60;
                    _timer.Start();
                    backgroundWorker1.RunWorkerAsync(excuteData);
                    toolStripStatusLabel5.Text = $"下次同步时间:{DateTime.Now.AddMinutes(_excuteData.excuteTimes).ToString("yyyy-MM-dd HH:mm:ss")}";
                }
                btnExcute.Text = "停止同步";
            }
            else
            {
                backgroundWorker1.CancelAsync();
                _timer.Stop();
                btnExcute.Text = "开始同步";
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy == false)
            {
                backgroundWorker1.RunWorkerAsync(_excuteData);
                
            }
            toolStripStatusLabel5.Text = $"下次同步时间:{DateTime.Now.AddMinutes(_excuteData.excuteTimes).ToString("yyyy-MM-dd HH:mm:ss")}";
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            ExcuteData excuteData = e.Argument as ExcuteData;
            _fileCount = 0;
            _excuteData = excuteData;
            //备份
            if (excuteData.syncModel == SyncModel.Buckup)
            {
                //检查备份
                if (string.IsNullOrEmpty(excuteData.buckupPath) || Directory.Exists(excuteData.buckupPath) == false)
                {
                    throw new InvalidOperationException("备份路径有误，请重新选择");
                }
                DateTime dateTime = DateTime.MinValue;
                int current = 0;
                //所有文件
                if (excuteData.pathAll)
                {
                    //全量备份
                    buckets[] buckets = _restApi.GetBuckets();
                               
                    foreach (var item in buckets)
                    {
                        //获取桶下所有文件信息
                        DocResult[] docResults = _restApi.GetObjects(item.name, string.Empty);
                        _fileCount += docResults.Where(t => t.contentType != "").Count();

                        foreach (var docResult in docResults)
                        {
                            BuckupDoc(item.name, docResult, ref current,ref dateTime);
                        }
                    }
                }
                //同步部分数据
                else if(excuteData.pathAll==false&&!string.IsNullOrEmpty(excuteData.soucrPath))
                {
                    string[] strs = excuteData.soucrPath.Split('/');
                    string prifix = string.Empty;
                    if (strs.Length > 1)
                    {
                        for (int i = 1; i < strs.Length; i++)
                        {
                            prifix += strs[i];
                            if (i < strs.Length - 1)
                                prifix += "/";
                        }
                    }
                    DocResult[] docResults = _restApi.GetObjects(strs[0], prifix);
                    _fileCount += docResults.Where(t => t.contentType != "").Count();
                    foreach (var docResult in docResults)
                    {
                        BuckupDoc(strs[0], docResult, ref current, ref dateTime);
                    }
                }
                e.Result = dateTime;
            }
            //同步
            else if (excuteData.syncModel == SyncModel.Sync)
            {
                //检查同步主机信息
                if (excuteData.syncInfo == null || string.IsNullOrEmpty(excuteData.syncInfo.uri) || string.IsNullOrEmpty(excuteData.syncInfo.user) || string.IsNullOrEmpty(excuteData.syncInfo.pwd))
                {
                    throw new InvalidOperationException("迁移主机连接信息不全");
                }
                //检查连接信息
                _tragetrestApi = new MinioRestApi(excuteData.syncInfo);
                if (_tragetrestApi.CanConnect == false)
                {
                    throw new InvalidOperationException("迁移主机连接信息不正确");
                }
                buckets[] targertBuckets = _tragetrestApi.GetBuckets();
                DateTime dateTime = DateTime.MinValue;
                int current = 0;
                _currentTargetResults = new List<DocResult>();
                //所有文件
                if (excuteData.pathAll)
                {
                    //全量备份
                    buckets[] buckets = _restApi.GetBuckets();
              
                    foreach (var item in buckets)
                    {
                        //不包含创建桶信息
                        if (targertBuckets == null || !targertBuckets.Any(t => t.name == item.name))
                        {
                            _tragetrestApi.CreateBucket(item.name);
                            //设置存储策略
                            Policy[] policies = _restApi.GetPolicies(item.name);
                            foreach (var policy in policies)
                            {
                                _tragetrestApi.SetBucketPolicy(item.name, policy.policy);
                            }
                        }
                        else
                        {
                            var tempObjs = _tragetrestApi.GetObjects(item.name, string.Empty);
                            if (tempObjs != null && tempObjs.Length > 0)
                                _currentTargetResults.AddRange(tempObjs.Where(t => t.contentType != ""));
                        }
                        //获取桶下所有文件信息
                        DocResult[] docResults = _restApi.GetObjects(item.name, string.Empty);
                        _fileCount += docResults.Where(t => t.contentType != "").Count();
                      
                        foreach (var docResult in docResults)
                        {
                            SyncDoc(item.name, docResult, ref current, ref dateTime);
                        }
                    }
                }
                //同步部分数据
                else if (excuteData.pathAll == false && !string.IsNullOrEmpty(excuteData.soucrPath))
                {
                    string[] strs = excuteData.soucrPath.Split('/');
                    string prifix = string.Empty;
                    if (strs.Length > 1)
                    {
                        for (int i = 1; i < strs.Length; i++)
                        {
                            prifix += strs[i];
                            if (i < strs.Length - 1)
                                prifix += "/";
                        }
                    }
                    //不包含创建桶信息
                    if (!targertBuckets.Any(t => t.name == strs[0]))
                    {
                        _tragetrestApi.CreateBucket(strs[0]);
                        //设置存储策略
                        Policy[] policies = _restApi.GetPolicies(strs[0]);
                        foreach (var policy in policies)
                        {
                            _tragetrestApi.SetBucketPolicy(strs[0], policy.policy);
                        }
                    }
                    else
                    {
                        var tempObjs = _tragetrestApi.GetObjects(strs[0], prifix);
                        if (tempObjs != null && tempObjs.Length > 0)
                            _currentTargetResults.AddRange(tempObjs.Where(t => t.contentType != ""));
                    }
                    DocResult[] docResults = _restApi.GetObjects(strs[0], prifix);
                    _fileCount += docResults.Where(t => t.contentType != "").Count();
                    foreach (var docResult in docResults)
                    {
                        SyncDoc(strs[0], docResult, ref current, ref dateTime);
                    }
                }
                e.Result = dateTime;
            }
        }

        private int _fileCount = 0;

        private List<DocResult> _currentTargetResults = new List<DocResult>();

        /// <summary>
        /// 递归迁移处理Doc
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="docResult"></param>
        /// <param name="total"></param>
        /// <param name="dateTime"></param>
        private void SyncDoc(string bucketName, DocResult docResult, ref int total, ref DateTime dateTime)
        {
            //文件夹，继续遍历
            if (docResult.size == 0 && docResult.contentType == "")
            {
                try
                {
                    //日期文件夹比较，当前天或者大于当前天(这里属于需求定制，但不影响通用性)
                    if (_excuteData.modelAll == false)
                    {
                        DateTime forlderTime = DateTime.MaxValue;
                        bool success = DateTime.TryParse(docResult.name.Replace("/", ""), out forlderTime);
                        if (success)
                        {
                            if (forlderTime.Date < LatestSyncTime.Date)
                                return;
                        }
                    }
                    DocResult[] results = _restApi.GetObjects(bucketName, docResult.name);
                    var tempObjs = _tragetrestApi.GetObjects(bucketName, docResult.name);
                    if (tempObjs != null && tempObjs.Length > 0)
                        _currentTargetResults.AddRange(tempObjs.Where(t => t.contentType != ""));
                    //文件时间比较
                    if (_excuteData.modelAll == false)
                        results = results.Where(t => t.contentType == "" || (t.lastModified.AddHours(8) > LatestSyncTime && t.contentType != "")).ToArray();
                    _fileCount += results.Where(t => t.contentType != "").Count();
                    foreach (var item in results)
                    {
                        SyncDoc(bucketName, item, ref total, ref dateTime);
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"获取{docResult.name}文件信息失败:{ex.Message}");
                }
            }
            //上传文件
            else
            {
                total++;
                //判断文件是否存在
                if (_currentTargetResults != null && _currentTargetResults.Any(t => t.name == docResult.name && t.size == docResult.size))
                {
                    _currentTargetResults.RemoveAll(t => t.name == docResult.name);
                    //已经存在，跳过同步
                }
                else
                {
                    try
                    {
                        backgroundWorker1.ReportProgress(total * 100 / _fileCount, $"开始上传文件{docResult.name}（{total}/{_fileCount}）");
                        Stream stream = _restApi.GetFileStream(bucketName, docResult.name);
                        string response = _tragetrestApi.UploadFile(bucketName, docResult.name, docResult.contentType, stream);
                        if (response == "")
                            backgroundWorker1.ReportProgress(total * 100 / _fileCount, $"文件{docResult.name}上传完成（{total}/{_fileCount}）");
                        //兼容超时问题，下载文件再上传
                        else
                        {
                            backgroundWorker1.ReportProgress(total * 100 / _fileCount, $"文件{docResult.name}上传失败:{response},开始尝试本地下载上传方式（{total}/{_fileCount}）");
                            string tmp = CreateTempPath(bucketName, docResult.name);
                            _restApi.DownloadFile(bucketName, docResult.name, tmp);
                            response = _tragetrestApi.UploadFile(bucketName, docResult.name, docResult.contentType, tmp);
                            if (response == "")
                                backgroundWorker1.ReportProgress(total * 100 / _fileCount, $"文件{docResult.name}上传完成（{total}/{_fileCount}）");
                            else
                                backgroundWorker1.ReportProgress(total * 100 / _fileCount, $"文件{docResult.name}上传失败:{response}（{total}/{_fileCount}）");
                        }
                        if (dateTime < docResult.lastModified.AddHours(8))
                            dateTime = docResult.lastModified.AddHours(8);
                    }
                    catch (Exception ex)
                    {
                        backgroundWorker1.ReportProgress(total * 100 / _fileCount, $"上传文件{docResult.name}失败:{ex.Message}");
                    }
                }

            }
        }
        /// <summary>
        /// 递归遍历处理Doc
        /// </summary>
        /// <param name="docResult"></param>
        private void BuckupDoc(string bucketName, DocResult docResult, ref int total,ref DateTime dateTime)
        {
            //文件夹，继续遍历
            if (docResult.size == 0 && docResult.contentType == "")
            {
                try
                {
                    //日期文件夹比较，当前天或者大于当前天(这里属于需求定制，但不影响通用性)
                    if (_excuteData.modelAll == false)
                    {
                        DateTime forlderTime = DateTime.MaxValue;
                        bool success = DateTime.TryParse(docResult.name.Replace("/", ""), out forlderTime);
                        if (success)
                        {
                            if (forlderTime.Date < LatestSyncTime.Date)
                                return;
                        }
                    }
                    DocResult[] results = _restApi.GetObjects(bucketName, docResult.name);
                    //文件时间比较
                    if (_excuteData.modelAll == false)
                        results = results.Where(t => t.contentType == "" || (t.lastModified.AddHours(8) > LatestSyncTime && t.contentType != "")).ToArray();
                    _fileCount += results.Where(t => t.contentType != "").Count();
                    foreach (var item in results)
                    {
                        BuckupDoc(bucketName, item, ref total, ref dateTime);
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"获取{docResult.name}文件信息失败:{ex.Message}");
                }
            }
            //下载文件
            else
            {
                total++;
                string savePath = CreatePath(bucketName, docResult.name);
                if (File.Exists(savePath))
                {
                    //backgroundWorker1.ReportProgress(total * 100 / _fileCount, $"文件{docResult.name}已存在（{total}/{_fileCount}）");
                }
                else
                {
                    try
                    {
                        backgroundWorker1.ReportProgress(total * 100 / _fileCount, $"开始下载文件{docResult.name}（{total}/{_fileCount}）");
                        _restApi.DownloadFile(bucketName, docResult.name, savePath);
                        if (dateTime < docResult.lastModified.AddHours(8))
                            dateTime = docResult.lastModified.AddHours(8);
                    }
                    catch (Exception ex)
                    {
                        backgroundWorker1.ReportProgress(total * 100 / _fileCount, $"下载文件{docResult.name}失败:{ex.Message}");
                    }
                }
            }
        }

        private string CreateTempPath(string bucketName, string fullName)
        {
            string[] forlders = fullName.Split('/');
            string tempPath = Path.Combine(Path.GetTempPath(), bucketName);
            if (Directory.Exists(tempPath) == false)
                Directory.CreateDirectory(tempPath);
            for (int i = 0; i < forlders.Length - 1; i++)
            {
                tempPath = Path.Combine(tempPath, forlders[i]);
                if (Directory.Exists(tempPath) == false)
                {
                    Directory.CreateDirectory(tempPath);
                }
            }
            tempPath = Path.Combine(tempPath, forlders[forlders.Length - 1]);
            return tempPath;
        }

        private string CreatePath(string bucketName, string fullName)
        {
            string[] forlders = fullName.Split('/');
            string tempPath = Path.Combine(_excuteData.buckupPath, bucketName);
            for (int i = 0; i < forlders.Length - 1; i++)
            {
                tempPath = Path.Combine(tempPath, forlders[i]);
                if (Directory.Exists(tempPath) == false)
                {
                    Directory.CreateDirectory(tempPath);
                }
            }
            tempPath = Path.Combine(tempPath, forlders[forlders.Length - 1]);
            return tempPath;
        }

        private void AppendLog(string logInfo)
        {
            backgroundWorker1.ReportProgress(0, logInfo);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
            {
                textBox9.AppendText($"{e.UserState}\n");
                lblMessage.Text = e.UserState.ToString();
            }
            else if (e.ProgressPercentage > 0 && e.ProgressPercentage <= 100)
            {
                progressBar1.Value = e.ProgressPercentage;
                textBox9.AppendText($"{e.UserState}\n");
                lblMessage.Text = e.UserState.ToString();
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == false && e.Error == null)
            {
                if (e.Result != null)
                {
                    DateTime temp = DateTime.Parse(e.Result.ToString());
                    if (temp != DateTime.MinValue && temp > LatestSyncTime && _excuteData.modelAll == false)
                    {
                        LatestSyncTime = temp;
                        toolStripStatusLabel3.Text = $"上次同步时间:{latesSyncTime.ToString("yyyy-MM-dd HH:mm:ss")}";
                    }
                    textBox9.AppendText($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:同步完成\n");
                }
            }
            else
            {
                textBox9.AppendText(e.Error.Message + "\n");
            }
            if (_excuteData.modelAll)
            {
                btnExcute.Text = "开始同步";
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            groupBox6.Enabled = radioButton2.Checked;
        }


        /// <summary>
        /// 维护最近同步日期
        /// </summary>
        string configPath = Path.Combine(Application.StartupPath, "sync.dat");

        private DateTime latesSyncTime = DateTime.MinValue;
        private DateTime LatestSyncTime
        {
            get
            {
                if (latesSyncTime != DateTime.MinValue)
                    return latesSyncTime;
                if (File.Exists(configPath))
                {
                    string[] txts = File.ReadAllLines(configPath);
                    if (txts.Length > 0)
                    {
                        DateTime temp = DateTime.MinValue;
                        bool success = DateTime.TryParse(txts[0], out temp);
                        if (success)
                            return temp;
                    }
                }
                return DateTime.MinValue;
            }
            set
            {
                if (File.Exists(configPath))
                    File.Delete(configPath);
                File.WriteAllLines(configPath, new string[] { value.ToString("yyyy-MM-dd HH:mm:ss") });
                latesSyncTime = value;
                btnChecked.Enabled = latesSyncTime > DateTime.MinValue;
            }
        }

        private Timer _timer = new Timer();
        private void FormMain_Load(object sender, EventArgs e)
        {
            textBox1.BindComplete(EventTrigger.LostFocus);
            textBox2.BindComplete(EventTrigger.LostFocus);
            textBox3.BindComplete(EventTrigger.LostFocus);
            txtBuckupPath.BindComplete(EventTrigger.Changed);
            txtTargetUri.BindComplete(EventTrigger.LostFocus);
            txtTargetUser.BindComplete(EventTrigger.LostFocus);
            txtTargetPwd.BindComplete(EventTrigger.LostFocus);
            //执行定时备份的Timer
            _timer.Tick += Timer_Tick;
            //当前时间刷新的Timer
            Timer timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick1;
            timer.Start();
            //定期写日志，清空输出框的Timer
            Timer timerLog = new Timer();
            timerLog.Interval = 1000 * 60 * 10; //10分钟执行一次
            timerLog.Tick += TimerLog_Tick;
            timerLog.Start();

            toolStripStatusLabel3.Text = $"上次同步时间:{LatestSyncTime.ToString("yyyy-MM-dd HH:mm:ss")}";
            button1.Enabled = !string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrEmpty(textBox2.Text) && !string.IsNullOrEmpty(textBox3.Text);
            btnChecked.Enabled = LatestSyncTime > DateTime.MinValue;
        }

        private void TimerLog_Tick(object sender, EventArgs e)
        {
            textBox9.AppendLog2File();
        }



        private void Timer_Tick1(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = $"当前时间:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = !string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrEmpty(textBox2.Text) && !string.IsNullOrEmpty(textBox3.Text);
        }

        private void txtSourcePath_TextChanged(object sender, EventArgs e)
        {
            SetExcuteEnable();
        }

        private void SetExcuteEnable()
        {
            btnExcute.Enabled = (radioButton4.Checked == false || (radioButton4.Checked && !string.IsNullOrEmpty(txtSourcePath.Text))) && (radioButton5.Checked == false || (radioButton5.Checked && !string.IsNullOrEmpty(txtBuckupPath.Text) && Directory.Exists(txtBuckupPath.Text))) && (radioButton6.Checked == false || (radioButton6.Checked && !string.IsNullOrEmpty(txtTargetUri.Text) && !string.IsNullOrEmpty(txtTargetUser.Text) && !string.IsNullOrEmpty(txtTargetPwd.Text)));

            btnTest.Enabled = !string.IsNullOrEmpty(txtTargetUri.Text) && !string.IsNullOrEmpty(txtTargetUser.Text) && !string.IsNullOrEmpty(txtTargetPwd.Text);
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            MinioRestApi minioRestApi = new MinioRestApi(txtTargetUri.Text, txtTargetUser.Text, txtTargetPwd.Text);
            if (minioRestApi.CanConnect)
            {
                MessageBox.Show("连接成功");
            }
            else
            {
                MessageBox.Show("连接失败");
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            textBox9.AppendLog2File();
        }

        private void btnChecked_Click(object sender, EventArgs e)
        {
            FormCheck formCheck = new FormCheck(new MinioConnectionInfo() { uri = textBox1.Text, user = textBox2.Text, pwd = textBox3.Text }, new CheckData()
            {
                pathAll = radioButton3.Checked,
                backupPath = txtBuckupPath.Text,
                syncModel = radioButton5.Checked ? SyncModel.Buckup : SyncModel.Sync,
                targetConnectionInfo = new MinioConnectionInfo() { uri = txtTargetUri.Text, user = txtTargetUser.Text, pwd = txtTargetPwd.Text }
            });
            formCheck.Show();
        }
    }


    public class ExcuteData
    {
        /// <summary>
        /// 同步模式，全量？增量
        /// </summary>
        public bool modelAll { get; set; }
        /// <summary>
        /// 全路径同步 是？否
        /// </summary>
        public bool pathAll { get; set; }
        /// <summary>
        /// 非全路径指定路径
        /// </summary>
        public string soucrPath { get; set; }
        /// <summary>
        /// 同步方式  备份？迁移
        /// </summary>
        public SyncModel syncModel { get; set; }
        /// <summary>
        /// 备份方式的文件夹路径
        /// </summary>
        public string buckupPath { get; set; }
        /// <summary>
        /// 增量同步的定时执行周期
        /// </summary>
        public int excuteTimes { get; set; }
        /// <summary>
        /// 迁移方式的迁移主机信息
        /// </summary>
        public MinioConnectionInfo syncInfo { get; set; }
    }

    public enum SyncModel
    {
        Buckup,
        Sync
    }
}
