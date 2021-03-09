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
        public FormCheck(MinioConnectionInfo minioConnectionInfo,CheckData checkData)
        {
            InitializeComponent();
            _restApi = new MinioRestApi(minioConnectionInfo);
            _checkData = checkData;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            CheckData checkData = e.Argument as CheckData;
            if (checkData.pathAll)
            {
                if (checkData.syncModel == SyncModel.Buckup)
                {
                    total = 0;success = 0;erro = 0;
                    buckets[] buckets = _restApi.GetBuckets();
                    foreach (var bucket in buckets)
                    {
                        DocResult[] docResults = _restApi.GetObjects(bucket.name, string.Empty);
                        if (docResults!=null&&docResults.Length > 0)
                        {
                            total += docResults.Where(t => t.contentType != "").Count();
                            foreach (var doc in docResults)
                            {
                                CheckDoc(bucket.name, docResults[0]);
                             }
                        }
                    }
                }
            }
        }

        private int total, success, erro = 0;

        private void CheckDoc(string bucketName, DocResult docResult)
        {
            if (docResult.contentType == "")
            {
                DocResult[] docResults = _restApi.GetObjects(bucketName, docResult.name);
                total += docResults.Where(t => t.contentType != "").Count();
                foreach (var item in docResults)
                {
                    CheckDoc(bucketName, item);
                }
            }
            else
            {             
                //检查文件是否存在，大小是否一致
                if (_checkData.syncModel == SyncModel.Buckup)
                {
                    //转换为文件路径
                    string transformPath = Path.Combine(_checkData.backupPath, bucketName, docResult.name.Replace('/', '\\'));
                    bool successed = false;
                    long size = 0;
                    if (File.Exists(transformPath))
                    {
                        FileInfo fileInfo = new FileInfo(transformPath);
                        if (fileInfo.Length == docResult.size)
                            successed = true;
                        size = fileInfo.Length;
                    }
                    if (successed)
                        success++;
                    else
                        erro++;
                    backgroundWorker1.ReportProgress((success + erro) * 100 / total, new NodeData() { Data = docResult, NodeFullPath = bucketName + "\\" + docResult.name.Replace('/', '\\'), IsDir = false, Success = successed, targetPath = transformPath, targetSize = size });
                }
            }
        }

        private Dictionary<string, List<object[]>> _dicRows = new Dictionary<string, List<object[]>>();

        private string _lastDic = string.Empty;

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage >= 0 && e.ProgressPercentage <= 100)
            {
                progressBar1.Value = e.ProgressPercentage;
                NodeData nodeData = e.UserState as NodeData;
                var addedRow = dataGridView1.Rows.Add(nodeData.NodeFullPath, nodeData.Data.size, nodeData.targetPath, nodeData.targetSize, nodeData.Success ? "成功" : "失败");
                /***
                TreeNode treeNode = treeView1.Nodes.Find(nodeData.NodeFullPath, true).FirstOrDefault();
                //增加节点信息
                if (treeNode == null)
                {
                    string[] nodeInfo = nodeData.NodeFullPath.Split('\\');
                    if (nodeInfo.Length == 1)
                        treeView1.Nodes.Add(nodeData.NodeFullPath, nodeInfo[0]).Expand();
                    else
                    {
                        TreeNodeCollection treeNodeCollection = treeView1.Nodes;
                        string key = nodeInfo[0];
                        for (int i = 0; i < nodeInfo.Length - 1; i++)
                        {
                            treeNodeCollection = treeNodeCollection[key].Nodes;
                            key += "\\";
                            key += nodeInfo[i + 1];
                        }
                        if (nodeData.IsDir)
                            treeNodeCollection.Add(nodeData.NodeFullPath, nodeInfo[nodeInfo.Length - 1]).Expand();
                        else
                        {
                            treeNodeCollection.Add(nodeData.NodeFullPath, nodeInfo[nodeInfo.Length - 1]).Expand();
                           // listView1.Items.Add(nodeData.NodeFullPath.Replace('\\','-'));
                        }
                    }
                }
                else
                {
                    if (nodeData.IsDir == false)
                    {
                        treeNode.Text = treeNode.Text + (nodeData.Success ? "(通过)" : "（不通过）");
                        //listView2.Items.Add(nodeData.targetPath);
                        label1.Text = string.Format("检查{0}条数据，成功{1}，失败{2}条", total, success, erro);
                        Application.DoEvents();
                    }
                }
                treeView1.ExpandAll();
                ***/
                label1.Text = string.Format("检查{0}条数据，成功{1}，失败{2}条", total, success, erro);
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
