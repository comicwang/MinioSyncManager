using Minio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinioSync
{
    public class MinioContext
    {
        public MinioClient minioClient = new MinioClient("172.104.0.251:8029", "minioadmin", "minioadmin");

        #region Bucket

        /// <summary>
        /// 获取所有Buckets名称集合
        /// </summary>
        /// <returns></returns>
        public List<string> GetBuckets()
        {
           var result=  minioClient.ListBucketsAsync();
            result.Wait();
            return result.Result.Buckets.Select(t=>t.Name).ToList();
        }

        /// <summary>
        /// 创建Bucket
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool MackBucket(string name)
        {
            try
            {
                var result = minioClient.MakeBucketAsync(name);
                result.Wait();
            }
            catch(Exception ex)
            {
                return false;
            }
            return true;
        }

        public bool BucketExits(string name)
        {
            try
            {
                var result = minioClient.BucketExistsAsync(name);
                result.Wait();
                return result.Result;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void RemoveBucket(string name)
        {
            var result = minioClient.RemoveBucketAsync(name);
            result.Wait();
        }

        public void ReadBuckets(string name,Action<Minio.DataModel.Item> action)
        {
            bool found = BucketExits(name);
            if (found)
            {
                var result= minioClient.ListObjectsAsync(name);
                result.Subscribe(action);
            }
        }

        #endregion
    }
}
