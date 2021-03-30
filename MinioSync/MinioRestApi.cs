using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MinioSyncCore
{
    public class MinioRestApi : RestApiRequester
    {
        /// <summary>
        /// 构造一个请求Minio的Api引擎
        /// </summary>
        /// <param name="uri">服务器地址</param>
        /// <param name="user">用户名</param>
        /// <param name="psw">密码</param>
        public MinioRestApi(string uri, string user, string psw) : base(uri, user, psw)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="minioConnectionInfo"></param>
        public MinioRestApi(MinioConnectionInfo minioConnectionInfo) : base(minioConnectionInfo.uri, minioConnectionInfo.user, minioConnectionInfo.pwd)
        {

        }

        /// <summary>
        /// 获取所有存储桶信息
        /// </summary>
        /// <returns></returns>
        public buckets[] GetBuckets()
        {
            string responseData = CreateRequest("web.ListBuckets", "{}");
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<BucketResult>(responseData);
            return result.result.buckets;
        }
        /// <summary>
        /// 创建存储桶信息
        /// </summary>
        /// <param name="bucketName"></param>
        public void CreateBucket(string bucketName)
        {
            string responseData = CreateRequest("web.MakeBucket", "{\"bucketName\":\""+ bucketName +"\"}");
        }
        /// <summary>
        /// 获取所有存储桶下面文件信息
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public DocResult[] GetObjects(string bucketName, string prefix)
        {
            string responseData = CreateRequest("web.ListObjects", "{ \"bucketName\":\"" + bucketName + "\",\"prefix\":\"" + prefix + "\"}");
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ObjectResult>(responseData);
            return result.result.objects;
        }   
        /// <summary>
        /// 获取存储桶的存储策略
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public Policy[] GetPolicies(string bucketName)
        {
            string responseData = CreateRequest("web.ListAllBucketPolicies", "{ \"bucketName\":\"" + bucketName + "\"}");
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<PolicyResult>(responseData);
            return result.result.policies;
        }
        /// <summary>
        /// 设备存储桶的存储策略
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="policy"></param>
        public void SetBucketPolicy(string bucketName,string policy)
        {
            string responseData = CreateRequest("web.SetBucketPolicy", "{\"bucketName\":\"" + bucketName + "\",\"prefix\":\"\",\"policy\":\"" + policy + "\"}");
        }
    }

    public class MinioConnectionInfo
    {
        public string uri { get; set; }

        public string user { get; set; }

        public string pwd { get; set; }
    }

    public class MinioResult
    {
        public DocResult docResult { get; set; }

        public string bucketName { get; set; }
    }

    public class DocResult
    {
        public string name { get; set; }

        public string contentType { get; set; }

        public int size { get; set; }

        public DateTime lastModified { get; set; }
    }

    public class DocClass
    {
        public DocResult[] objects { get; set; }

        public bool writable { get; set; }

        public string uiVersion { get; set; }
    }

    public class buckets
    {
        public string name { get; set; }

        public DateTime creationDate { get; set; }
    }

    public class BucketClass
    {
        public buckets[] buckets { get; set; }
    }

    public class BucketResult
    {
        public string jsonrpc { get; set; }

        public BucketClass result { get; set; }

        public string id { get; set; }
    }

    public class ObjectResult
    {
        public string jsonrpc { get; set; }

        public DocClass result { get; set; }

        public string id { get; set; }
    }

    public class TokenResult
    {
        public string jsonrpc { get; set; }

        public TokenClass result { get; set; }

        public string id { get; set; }
    }

    public class TokenClass
    {
        public string token { get; set; }

        public DateTime uiVersion { get; set; }
    }

    public class PolicyResult
    {
        public string jsonrpc { get; set; }

        public PolicyClass result { get; set; }

        public string id { get; set; }
    }

    public class PolicyClass
    {
        public Policy[] policies { get; set; }

        public DateTime uiVersion { get; set; }
    }

    public class Policy
    {
        public string bucket { get; set; }

        public string policy { get; set; }

        public string prefix { get; set; }
    }
}
