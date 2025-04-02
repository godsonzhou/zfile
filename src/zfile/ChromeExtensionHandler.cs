using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Zfile.Forms;

namespace Zfile
{
    /// <summary>
    /// Chrome扩展消息处理类，实现Native Messaging协议
    /// </summary>
    public static class ChromeExtensionHandler
    {
        private static bool _isListening = false;
        private static readonly object _lockObject = new object();

        /// <summary>
        /// 下载请求消息结构
        /// </summary>
        public class DownloadRequest
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("filename")]
            public string Filename { get; set; }

            [JsonPropertyName("saveAs")]
            public bool SaveAs { get; set; }

            [JsonPropertyName("cookies")]
            public string Cookies { get; set; }

            [JsonPropertyName("headers")]
            public Dictionary<string, string> Headers { get; set; }

            [JsonPropertyName("referrer")]
            public string Referrer { get; set; }
        }

        /// <summary>
        /// 响应消息结构
        /// </summary>
        public class Response
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("downloadId")]
            public string DownloadId { get; set; }
        }

        /// <summary>
        /// 启动Chrome扩展消息监听
        /// </summary>
        public static void StartListening()
        {
            lock (_lockObject)
            {
                if (_isListening)
                    return;

                _isListening = true;
                Task.Run(() => ListenForMessages());
            }
        }

        /// <summary>
        /// 停止Chrome扩展消息监听
        /// </summary>
        public static void StopListening()
        {
            lock (_lockObject)
            {
                _isListening = false;
            }
        }

        /// <summary>
        /// 监听Chrome扩展消息
        /// </summary>
        private static async Task ListenForMessages()
        {
            try
            {
                using (Stream stdin = Console.OpenStandardInput())
                {
                    while (_isListening)
                    {
                        // 读取消息长度（前4个字节）
                        byte[] lengthBytes = new byte[4];
                        int read = await stdin.ReadAsync(lengthBytes, 0, 4);
                        if (read != 4) break;

                        // 解析消息长度（小端序）
                        int messageLength = BitConverter.ToInt32(lengthBytes, 0);
                        if (messageLength <= 0) continue;

                        // 读取消息内容
                        byte[] messageBytes = new byte[messageLength];
                        read = await stdin.ReadAsync(messageBytes, 0, messageLength);
                        if (read != messageLength) break;

                        // 解析消息
                        string json = Encoding.UTF8.GetString(messageBytes);
                        await ProcessMessage(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chrome扩展消息监听错误: {ex.Message}");
            }
            finally
            {
                _isListening = false;
            }
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private static async Task ProcessMessage(string json)
        {
            try
            {
                var request = JsonSerializer.Deserialize<DownloadRequest>(json);
                if (request == null || string.IsNullOrEmpty(request.Url))
                {
                    SendResponse(new Response { Success = false, Message = "无效的下载请求" });
                    return;
                }

                // 生成下载ID
                string downloadId = Guid.NewGuid().ToString();

                // 确定保存路径
                string savePath = null;
                if (!request.SaveAs && !string.IsNullOrEmpty(request.Filename))
                {
                    // 使用默认下载目录 + 文件名
                    string downloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    downloadFolder = Path.Combine(downloadFolder, "Downloads");
                    Directory.CreateDirectory(downloadFolder); // 确保目录存在
                    savePath = Path.Combine(downloadFolder, request.Filename);
                }

                // 启动下载任务
                await Task.Run(() =>
                {
                    try
                    {
                        IdmIntegration.DownloadFile(request.Url, savePath);
                        SendResponse(new Response { Success = true, DownloadId = downloadId });
                    }
                    catch (Exception ex)
                    {
                        SendResponse(new Response { Success = false, Message = ex.Message });
                    }
                });
            }
            catch (JsonException ex)
            {
                SendResponse(new Response { Success = false, Message = $"JSON解析错误: {ex.Message}" });
            }
            catch (Exception ex)
            {
                SendResponse(new Response { Success = false, Message = $"处理下载请求错误: {ex.Message}" });
            }
        }

        /// <summary>
        /// 发送响应消息给Chrome扩展
        /// </summary>
        private static void SendResponse(Response response)
        {
            try
            {
                string json = JsonSerializer.Serialize(response);
                byte[] responseBytes = Encoding.UTF8.GetBytes(json);
                byte[] lengthBytes = BitConverter.GetBytes(responseBytes.Length);

                // 写入消息长度（小端序）
                Console.OpenStandardOutput().Write(lengthBytes, 0, 4);
                // 写入消息内容
                Console.OpenStandardOutput().Write(responseBytes, 0, responseBytes.Length);
                Console.OpenStandardOutput().Flush();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"发送响应错误: {ex.Message}");
            }
        }
    }
}