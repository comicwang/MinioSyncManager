using MinioSyncCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinioSyncManager
{
    public partial class FormCheck : Form
    {
        MinioRestApi _restApi = null;
        CheckData _checkData = null;
        MinioRestApi _targetApi = null;
        public FormCheck(MinioConnectionInfo minioConnectionInfo,CheckData checkData)
        {
            InitializeComponent();
            _restApi = new MinioRestApi(minioConnectionInfo);
            _checkData = checkData;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            CheckData checkData = e.Argument as CheckData;
            if (checkData.syncModel == SyncModel.Sync && _targetApi == null)
                _targetApi = new MinioRestApi(checkData.targetConnectionInfo);
            if (checkData.pathAll)
            {
                total = 0; success = 0;
                buckets[] buckets = _restApi.GetBuckets();
                foreach (var bucket in buckets)
                {
                    DocResult[] docResults = _restApi.GetObjects(bucket.name, string.Empty);
                    if (docResults != null && docResults.Length > 0)
                    {
                        total += docResults.Where(t => t.contentType != "").Count();
                        foreach (var doc in docResults)
                        {
                            CheckDoc(bucket.name, doc);
                        }
                    }
                }
            }
        }

        private int total, success = 0;
        private List<DocResult> _targetResult = new List<DocResult>();

        private void CheckDoc(string bucketName, DocResult docResult)
        {
            if (docResult.contentType == "")
            {
                DocResult[] docResults = _restApi.GetObjects(bucketName, docResult.name);
                if (_checkData.syncModel == SyncModel.Sync)
                    _targetResult.AddRange(_targetApi.GetObjects(bucketName, docResult.name));

                total += docResults.Where(t => t.contentType != "").Count();
                foreach (var item in docResults)
                {
                    CheckDoc(bucketName, item);
                }
            }
            else
            {
                bool successed = false;
                //检查文件是否存在，大小是否一致
                if (_checkData.syncModel == SyncModel.Buckup)
                {
                    //转换为文件路径
                    string transformPath = Path.Combine(_checkData.backupPath, bucketName, docResult.name.Replace('/', '\\'));

                    long size = 0;
                    if (File.Exists(transformPath))
                    {
                        FileInfo fileInfo = new FileInfo(transformPath);
                        if (fileInfo.Length == docResult.size)
                            successed = true;
                        size = fileInfo.Length;
                    }
                    success++;
                    if (successed == false)
                    {
                        backgroundWorker1.ReportProgress(success * 100 / total, new NodeData() { Data = docResult, NodeFullPath = bucketName + "\\" + docResult.name.Replace('/', '\\'), IsDir = false, Success = successed, targetPath = transformPath, targetSize = size });
                    }
                }
                else
                {
                    var tmp = _targetResult.Where(t => t.name == docResult.name && t.size == docResult.size).FirstOrDefault();
                    if (tmp != null)
                    {
                        _targetResult.Remove(tmp);
                        successed = true;
                    }
                    success++;
                    if (successed == false)
                    {
                        backgroundWorker1.ReportProgress(success * 100 / total, new NodeData() { Data = docResult, NodeFullPath = bucketName + "\\" + docResult.name.Replace('/', '\\'), IsDir = false, Success = successed, targetPath = docResult.name, targetSize = docResult.size });
                    }
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage >= 0 && e.ProgressPercentage <= 100)
            {
                progressBar1.Value = e.ProgressPercentage;
                NodeData nodeData = e.UserState as NodeData;
                var addedRow = dataGridView1.Rows.Add(nodeData.NodeFullPath, nodeData.Data.size, nodeData.targetPath, nodeData.targetSize);              
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        private void FormCheck_Load(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync(_checkData);
        }
    }

    public class NodeData
    {
        public string NodeFullPath { get; set; }

        public DocResult Data { get; set; }

        public bool IsDir { get; set; } = true;

        public bool Success { get; set; }

        public string targetPath { get; set; }

        public long targetSize { get; set; }
    }

    public class CheckData
    {
        public bool pathAll { get; set; }

        public SyncModel syncModel { get; set; }

        public string backupPath { get; set; }

        public MinioConnectionInfo targetConnectionInfo { get; set; }
    }
}
