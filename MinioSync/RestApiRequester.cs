using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MinioSyncCore
{
    public class RestApiRequester
    {
        private string _uri = string.Empty;
        private string _user = string.Empty;
        private string _psw = string.Empty;
        /// <summary>
        /// 构建一个WebRpc请求器，并请求Token信息
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="user"></param>
        /// <param name="psw"></param>
        public RestApiRequester(string uri,string user,string psw)
        {
            _uri = uri;
            _user = user;
            _psw = psw;
            System.Net.ServicePointManager.DefaultConnectionLimit = 50;
            SetToken();
        }

        private string _token = string.Empty;
        /// <summary>
        /// 获取Token信息
        /// </summary>
        public string Token { get { return _token; } }
        /// <summary>
        ///获取Minio是否能够链接
        /// </summary>
        public bool CanConnect { get { return !string.IsNullOrEmpty(_token); } }

        private void SetToken()
        {
            string result = CreateRequest("web.Login", "{\"username\":\"" + _user + "\",\"password\":\"" + _psw + "\"}", true);
            TokenResult tokenResult = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResult>(result);
            _token= tokenResult.result?.token;
        }
        /// <summary>
        /// 创建一个请求
        /// </summary>
        /// <param name="method">请求方法</param>
        /// <param name="paramList">请求参数</param>
        /// <param name="ignoreToken">是否忽略Token</param>
        /// <returns></returns>
        public string CreateRequest(string method, string paramList, bool ignoreToken = false)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{_uri}/minio/webrpc");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36";
            if (ignoreToken == false)
            {
                request.Headers["Authorization"] = $"Bearer {_token}";
            }
            string strJson = "{ \"id\":1,\"jsonrpc\":\"2.0\",\"params\":" + paramList + ",\"method\":\"" + method + "\"}";
            Encoding encoding = Encoding.UTF8;
            byte[] byteArray = Encoding.UTF8.GetBytes(strJson);
            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(byteArray, 0, byteArray.Length);
            }
            string responseData = String.Empty;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), encoding))
                    {
                        responseData = reader.ReadToEnd();
                    }
                    //if (responseData.Contains("error"))
                    //    throw new InvalidOperationException(responseData);
                    return responseData;
                }
            }
            catch (System.Net.WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("401"))
                {
                    SetToken();
                    return CreateRequest(method, paramList);
                }
                return null;
            }
        }
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="bucketName">存储桶名称</param>
        /// <param name="path">文件路径</param>
        /// <param name="savePath">存储路径</param>
        public void DownloadFile(string bucketName, string path, string savePath)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{_uri}/{bucketName}/{path}");
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36";
            WebResponse respone = request.GetResponse();
            Stream netStream = respone.GetResponseStream();

            using (Stream fileStream = new FileStream(savePath, FileMode.Create))
            {
                byte[] read = new byte[1024];
                int realReadLen = netStream.Read(read, 0, read.Length);
                while (realReadLen > 0)
                {
                    fileStream.Write(read, 0, realReadLen);
                    realReadLen = netStream.Read(read, 0, read.Length);
                }
                netStream.Close();
                fileStream.Close();
            }
        }

        /// <summary>
        /// 获取Minio文件流信息
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public Stream GetFileStream(string bucketName, string path)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{_uri}/{bucketName}/{path}");
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36";
            request.KeepAlive = true;
            WebResponse respone = request.GetResponse();
            Stream netStream = respone.GetResponseStream();
            return netStream;
        }

        /// <summary>
        /// 上传Minio文件到Minio
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="path"></param>
        /// <param name="contentType"></param>
        /// <param name="stream">minio文件流</param>
        public string UploadFile(string bucketName, string path, string contentType,Stream stream,long fileSize=0)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{_uri}/minio/upload/{bucketName}/{path}");
            request.Method = "PUT";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36";
            request.Headers["Authorization"] = $"Bearer {_token}";
            request.ContentType = contentType;
            request.KeepAlive = true;
            if (fileSize > 0)
                request.ContentLength = fileSize;
            using (stream)
            {
                byte[] buffer = new byte[102400];
                int bytesRead = 0;
                int offset = 0;
                using (Stream reqStream = request.GetRequestStream())
                {
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        reqStream.Write(buffer, 0, bytesRead);
                        offset += bytesRead;
                    }
                    //将文件内容写进内存流
                    string responseData = String.Empty;
                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                            {
                                responseData = reader.ReadToEnd();
                            }
                            if (responseData.Contains("error"))
                                throw new InvalidOperationException(responseData);
                            return responseData;
                        }
                    }
                    catch (System.Net.WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("401"))
                        {
                            SetToken();
                            return UploadFile(bucketName, path, contentType, stream);
                        }
                        return ex.Message;
                    }
                }           
            }
        }

        /// <summary>
        /// 上传本地文件到Minio
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="path"></param>
        /// <param name="contentType"></param>
        /// <param name="uploadPath"></param>
        public string UploadFile(string bucketName, string path, string contentType,string uploadPath)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{_uri}/minio/upload/{bucketName}/{path}");
            request.Method = "PUT";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.183 Safari/537.36";
             request.Headers["Authorization"] = $"Bearer {_token}";
            request.ContentType = contentType;
            request.KeepAlive = false;
            if (File.Exists(uploadPath) == false)
            {
                throw new InvalidOperationException("上传文件路径不正确");
            }
            using (FileStream fileStream = new FileStream(uploadPath, FileMode.Open))
            {
                request.ContentLength = fileStream.Length;
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                int offset = 0;
                using (Stream reqStream = request.GetRequestStream())
                {
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        reqStream.Write(buffer, 0, bytesRead);
                        offset += bytesRead;
                    }
                    //将文件内容写进内存流
                    string responseData = String.Empty;
                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                            {
                                responseData = reader.ReadToEnd();
                            }
                            if (responseData.Contains("error"))
                                throw new InvalidOperationException(responseData);
                            return responseData;
                        }
                    }
                    catch (System.Net.WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("401"))
                        {
                            SetToken();
                            return UploadFile(bucketName, path, contentType, uploadPath);
                        }
                        return ex.Message;
                    }
                }
               
            }
        }
    }
}
