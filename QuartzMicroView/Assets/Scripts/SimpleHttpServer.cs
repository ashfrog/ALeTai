using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public class SimpleHttpServer : MonoBehaviour
{
    private HttpListener listener;
    private Thread listenerThread;
    int port = 8080;

    [SerializeField]
    LitVCR litVCR;

    [SerializeField]
    TCPUDPServer tCPUDPServer;

    void Start()
    {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{port}/");
        listenerThread = new Thread(StartListener);
        listenerThread.Start();
        Debug.Log($"Server started on port {port}");
    }

    void Update()
    {

    }

    void StartListener()
    {
        listener.Start();
        while (listener.IsListening)
        {
            try
            {
                HttpListenerContext context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(ProcessRequest, context);
            }
            catch (Exception e)
            {
                Debug.Log($"Exception: {e.Message}");
            }
        }
    }
    // 读取一行数据
    private byte[] ReadLine(Stream stream, out string str)
    {
        str = null;
        var sb = new StringBuilder();
        using (var memoryStream = new MemoryStream())
        {
            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                memoryStream.WriteByte((byte)b);
                sb.Append((char)b);
                if (b == '\n')
                {
                    str = sb.ToString();
                    break;
                }
            }
            return memoryStream.ToArray();
        }
    }

    const string GENERATED_FILENAME = "uploadedFile_";
    void ProcessRequest(object state)
    {
        HttpListenerContext context = (HttpListenerContext)state;
        // 处理OPTIONS预检请求
        if (context.Request.HttpMethod == "OPTIONS")
        {
            HandleOptionsRequest(context);
        }
        else if (context.Request.HttpMethod == "POST")
        {
            HandleUploadFile(context);
        }
        else if (context.Request.HttpMethod == "GET")
        {
            if (context.Request.Url.AbsolutePath == "/filelist")
            {
                HandleFileListRequest(context);
            }
            else if (context.Request.Url.AbsolutePath == "/delete")
            {
                HandleDeleteRequest(context);
            }
            else if (context.Request.Url.AbsolutePath == "/control")
            {
                HandleControlRequest(context);
            }
            else if (context.Request.Url.AbsolutePath == "/rename")
            {
                HaldleRenameRequest(context);
            }
            else
            {
                SendJsonResponse(context, -1, "Not Found");
            }
        }
        else
        {
            SendJsonResponse(context, -1, "Method Not Allowed");
        }
        context.Response.Close();
    }

    private void HaldleRenameRequest(HttpListenerContext context)
    {
        Dictionary<string, string> parameters = GetQueryParameters(context.Request.Url);
        if (parameters.TryGetValue("sourcefilename", out string sourceFilename))
        {
            Debug.Log("Source Filename: " + sourceFilename);
            if (parameters.TryGetValue("destfilename", out string destFilename))
            {
                Debug.Log("Destination Filename: " + destFilename);
                try
                {
                    string sourcefile = Path.Combine(LitVCR.persistentDataPath, sourceFilename);
                    string destfile = Path.Combine(LitVCR.persistentDataPath, destFilename);
                    File.Move(sourcefile, destfile);
                    SendJsonResponse(context, 0, destfile);
                    litVCR.ReloadFileList();
                }
                catch (Exception ex)
                {
                    SendJsonResponse(context, -1, ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// 处理预检请求(OPTIONS方法),这是CORS机制的一部分
    /// </summary>
    /// <param name="context"></param>
    private void HandleOptionsRequest(HttpListenerContext context)
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, DELETE, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
        context.Response.StatusCode = 200;
        context.Response.Close();
    }

    private void HandleUploadFile(HttpListenerContext context)
    {
        try
        {
            if (context.Request.Url.AbsolutePath == "/upload")
            {
                // Extract the file name from the Content-Disposition header

                Stream inputStream = context.Request.InputStream;
                string fileName = "";

                bool isformdata = true; //以formdata格式传输的文件
                                        //ReadLine Skip the first 4 lines 前4中第二行里读取文件名
                byte[] firstlinebytes = ReadLine(inputStream, out string firstlinestr);//第一行----------------------------534377399431945851454483
                if (firstlinestr != null && firstlinestr.StartsWith("----"))
                {
                    ReadLine(inputStream, out string secondlinestr); //第二行文件名Content-Disposition: form-data; name="file"; filename="1.webp"
                    fileName = ExtractFileName(secondlinestr);
                    if (!String.IsNullOrEmpty(fileName))
                    {
                        fileName = fileName.Trim();
                    }
                    ReadLine(inputStream, out string thirdlinestr);//第三行Content-Type: image/webp
                    ReadLine(inputStream, out string fourthlinestr);//第四行 /r/n
                }
                else //binary方式传输文件 不需要去除前4行字符
                {
                    isformdata = false;
                }

                if (String.IsNullOrEmpty(fileName)) //文件名从第二行读取失败
                {
                    fileName = context.Request.Headers["filename"];
                    fileName = Uri.UnescapeDataString(fileName);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = GENERATED_FILENAME + DateTime.Now.Ticks;
                    }
                }



                string filePath = Path.Combine(LitVCR.persistentDataPath, fileName);
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[8192]; // 8KB buffer
                    int bytesRead;
                    long totalBytesRead = 0;

                    if (!isformdata) //binary方式传输 读取的第一行写入
                    {
                        fileStream.Write(firstlinebytes, 0, firstlinebytes.Length);
                    }

                    // Read the file data
                    while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                    }
                }

                Debug.Log($"文件接收并保存至: {filePath}");

                string responseMessage = $"{fileName}";
                SendJsonResponse(context, 0, responseMessage);

                litVCR.ReloadFileList();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"处理文件时出错: {e.Message}");
            SendJsonResponse(context, -1, $"处理文件时出错: {e.Message}");
        }
    }

    //从uri中提取参数
    private Dictionary<string, string> GetQueryParameters(Uri uri)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();

        string query = uri.Query;
        if (string.IsNullOrEmpty(query))
        {
            return parameters;
        }

        query = query.TrimStart('?');
        string[] pairs = query.Split('&');

        foreach (string pair in pairs)
        {
            string[] keyValue = pair.Split('=');
            if (keyValue.Length == 2)
            {
                string key = Uri.UnescapeDataString(keyValue[0]);
                string value = Uri.UnescapeDataString(keyValue[1]);
                parameters[key] = value;
            }
        }

        return parameters;
    }
    /// <summary>
    /// 列出文件列表
    /// </summary>
    /// <param name="context"></param>
    void HandleFileListRequest(HttpListenerContext context)
    {
        try
        {
            litVCR.ReloadFileList();
            var fileList = FileUtils.GetMediaFiles(LitVCR.persistentDataPath, false);
            SendJsonResponse(context, 0, fileList);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing file list request: {e.Message}");
            SendJsonResponse(context, -1, "Error processing file list request");
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="context"></param>
    void HandleDeleteRequest(HttpListenerContext context)
    {
        try
        {
            string filename = HttpUtility.ParseQueryString(context.Request.Url.Query).Get("filename");
            if (string.IsNullOrEmpty(filename))
            {
                SendJsonResponse(context, -1, "Filename parameter is required");
                return;
            }

            string filePath = Path.Combine(LitVCR.persistentDataPath, filename);
            if (!File.Exists(filePath))
            {
                SendJsonResponse(context, -1, "File not found");
                return;
            }

            File.Delete(filePath);
            litVCR.ReloadFileList();
            SendJsonResponse(context, 0, $"File {filename} has been deleted");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing delete request: {e}");
            SendJsonResponse(context, -1, $"Error processing delete request: {e.Message}");
        }
    }

    void HandleControlRequest(HttpListenerContext context)
    {
        try
        {
            string cmdstr = HttpUtility.ParseQueryString(context.Request.Url.Query).Get("cmdstr");
            if (!String.IsNullOrEmpty(cmdstr))
            {
                // 创建一个 TaskCompletionSource 来等待结果  防止主线程运行context已释放
                var tcs = new TaskCompletionSource<string>();

                Loom.QueueOnMainThread(() =>
                {
                    try
                    {
                        // 修改 ProcessCommand 方法，使其返回结果而不是直接使用 context
                        string result = tCPUDPServer.ProcessCommand(cmdstr, null, null, context);
                        tcs.SetResult(result);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                });

                // 等待结果，但设置一个超时
                if (Task.WaitAny(new[] { tcs.Task }, TimeSpan.FromSeconds(3)) == 0)
                {
                    if (tcs.Task.IsFaulted)
                    {
                        throw tcs.Task.Exception;
                    }
                    //SendJsonResponse(context, 0, tcs.Task.Result);
                }
                else
                {
                    SendJsonResponse(context, -1, "Command execution timed out");
                }
            }
            else
            {
                SendJsonResponse(context, -1, "Invalid command");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing cmd request: {e}");
            SendJsonResponse(context, -1, $"Error processing cmd request: {e.Message}");
        }
        finally
        {
            // 确保在处理完请求后关闭 context
            context.Response.Close();
        }
    }

    public static void SendJsonResponse(HttpListenerContext context, int code, object data)
    {
        var response = new { code = code, data = data };
        string jsonResponse = JsonConvert.SerializeObject(response);
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(jsonResponse);

        // 添加CORS头部
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, DELETE");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");

        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
    }
    public string ExtractFileName(string contentDisposition)
    {
        // Try to match UTF-8 encoded filename
        var matchUtf8 = Regex.Match(contentDisposition, @"filename\*=UTF-8''(?<encoded>.+)");
        if (matchUtf8.Success)
        {
            string encoded = matchUtf8.Groups["encoded"].Value;
            return Uri.UnescapeDataString(encoded);
        }

        // Try to match standard filename
        var matchStandard = Regex.Match(contentDisposition, @"filename=""(?<filename>.+?)""");
        if (matchStandard.Success)
        {
            return matchStandard.Groups["filename"].Value;
        }

        return null;
    }

    void OnApplicationQuit()
    {
        if (listener != null && listener.IsListening)
        {
            listener.Stop();
            listener.Close();
        }
    }
}