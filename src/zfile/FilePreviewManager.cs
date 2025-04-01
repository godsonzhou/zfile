using CSCore;
using CSCore.SoundOut;
using LibVLCSharp.Shared;
using SharpCompress.Archives;
using System.Diagnostics;
using System.Text;
//using MF.Core;
//using MF.MediaFoundation;
namespace Zfile
{
	public class FilePreviewManager : IDisposable
	{
		private bool disposed = false;

		//public static void getPreview(string videoFilePath, string thumbnailPath)
		//{
		//	// 视频文件路径
		//	//string videoFilePath = @"C:\path\to\your\video.mp4";
		//	// 缩略图保存路径
		//	//string thumbnailPath = @"C:\path\to\save\thumbnail.jpg";
		//	try
		//	{
		//		// 创建 LibVLC 实例
		//		using (var libVLC = new LibVLC())
		//		{
		//			// 创建媒体播放器实例
		//			using (var mediaPlayer = new MediaPlayer(libVLC))
		//			{
		//				// 创建媒体实例
		//				using (var media = new Media(libVLC, new FileInfo(videoFilePath)))
		//				{
		//					// 将媒体加载到播放器中
		//					mediaPlayer.Media = media;
		//					// 打开媒体但不播放
		//					mediaPlayer.Open();
		//					// 等待一段时间，让媒体准备好
		//					Thread.Sleep(1000);
		//					// 抓屏获取缩略图
		//					using (var snapshot = mediaPlayer.TakeSnapshot(0, 0, 0, 0))
		//					{
		//						if (snapshot != null)
		//						{
		//							// 将缩略图保存为 JPEG 格式
		//							snapshot.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Jpeg);
		//							Console.WriteLine("缩略图已成功保存到: " + thumbnailPath);
		//						}
		//						else
		//						{
		//							Console.WriteLine("无法获取缩略图。");
		//						}
		//					}
		//				}
		//			}
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		Console.WriteLine("发生错误: " + ex.Message);
		//	}
		//}
		//public void GetThumbnailWithWMF(string videoPath, string outputPath)
		//{
		//	// 初始化COM组件
		//	MFStartup(MF_VERSION.MF_VERSION);

		//	// 创建SourceReader
		//	IMFSourceReader reader;
		//	MFCreateSourceReaderFromURL(videoPath, null, out reader);

		//	// 设置输出格式为RGB32
		//	reader.SetCurrentMediaType(MF_SOURCE_READER.FirstVideoStream,
		//		IntPtr.Zero, new IMFMediaType());

		//	// 读取第一帧
		//	reader.ReadSample(MF_SOURCE_READER.FirstVideoStream, 0,
		//		out _, out _, out _, out IMFSample sample);

		//	// 将帧转换为图像并保存
		//	// （需进一步处理IMFSample，此处省略具体转换代码）
		//}
		public void GetThumbnailWithFFmpeg(string videoPath, string outputPath)
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = "ffmpeg.exe",
				Arguments = $"-i \"{videoPath}\" -ss 00:00:01 -vframes 1 -vf \"scale=320:-1\" -q:v 2 \"{outputPath}\"",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true
			};

			using var process = new Process { StartInfo = startInfo };
			process.Start();
			process.WaitForExit();
		}
		public void GetThumbnail(string videoPath, string outputPath)
		{
			using var libVLC = new LibVLC();
			using var media = new Media(libVLC, videoPath);
			using var mediaPlayer = new MediaPlayer(media);

			mediaPlayer.Play();

			// 等待视频初始化（实际需更稳健的同步机制）
			Thread.Sleep(1000);

			// 截取当前帧（可能需要调整尺寸）
			mediaPlayer.TakeSnapshot(0, outputPath, 0, 0);
			mediaPlayer.Stop();
		}

		public Control CreatePreviewControl(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new Label { Text = "文件不存在", Dock = DockStyle.Fill };
            }

            var extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                var ext when Constants.TextFileExtensions.Contains(ext) => CreateTextViewer(filePath),
                ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => CreateOfficeViewer(filePath),
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" => CreateImageViewer(filePath),
                ".mp3" or ".wav" or ".wma" or ".aac" => CreateAudioPlayer(filePath),
                ".mp4" or ".avi" or ".mkv" or ".mov" => CreateVideoPlayer(filePath),
                ".zip" or ".rar" or ".7z" => CreateArchiveViewer(filePath),
                _ => new Label { Text = "不支持的文件格式", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }
            };
        }

        private Control CreateTextViewer(string filePath)
        {
            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Text = File.ReadAllText(filePath)
            };
            return textBox;
        }

        private Control CreateOfficeViewer(string filePath)
        {
            var webBrowser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                Url = new Uri(filePath)
            };
            return webBrowser;
        }

        private Control CreateImageViewer(string filePath)
        {
            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = Image.FromFile(filePath),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            return pictureBox;
        }

        private Control CreateAudioPlayer(string filePath)
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var playButton = new Button { Text = "Play", Dock = DockStyle.Top };
            var stopButton = new Button { Text = "Stop", Dock = DockStyle.Top };

            try
            {
                var waveOut = new WaveOut();
                IWaveSource audioFile = Path.GetExtension(filePath).ToLower() switch
                {
                    ".wav" => new CSCore.Codecs.WAV.WaveFileReader(filePath),
                    ".mp3" => new CSCore.Codecs.MP3.Mp3MediafoundationDecoder(filePath),
                    ".flac" => new CSCore.Codecs.FLAC.FlacFile(filePath),
                    ".wma" => new CSCore.Codecs.WMA.WmaDecoder(filePath),
                    ".aac" => new CSCore.Codecs.AAC.AacDecoder(filePath),
                    _ => throw new NotSupportedException("不支持的音频格式")
                };

                waveOut.Initialize(audioFile);
                playButton.Click += (s, e) => waveOut.Play();
                stopButton.Click += (s, e) => waveOut.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法播放音频文件: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            panel.Controls.Add(stopButton);
            panel.Controls.Add(playButton);
            return panel;
        }

        private Control CreateVideoPlayer(string filePath)
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var playButton = new Button { Text = "Play", Dock = DockStyle.Top };
            var stopButton = new Button { Text = "Stop", Dock = DockStyle.Top };

            string libVlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
            Core.Initialize(libVlcPath);

            using var libVLC = new LibVLC();
            var mediaPlayer = new MediaPlayer(libVLC);
            mediaPlayer.Play(new Media(libVLC, filePath, FromType.FromPath));
			
            var videoView = new LibVLCSharp.WinForms.VideoView
            {
                MediaPlayer = mediaPlayer,
                Dock = DockStyle.Fill
            };

            playButton.Click += (s, e) => mediaPlayer.Play();
            stopButton.Click += (s, e) => mediaPlayer.Stop();

            panel.Controls.Add(stopButton);
            panel.Controls.Add(playButton);
            panel.Controls.Add(videoView);

            return panel;
        }

        private Control CreateArchiveViewer(string filePath)
        {
            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill
            };

            try
            {
                using (var archive = ArchiveFactory.Open(filePath))
                {
                    var sb = new StringBuilder();
                    foreach (var entry in archive.Entries)
                    {
                        sb.AppendLine($"{entry.Key} ({entry.Size} bytes)");
                    }
                    textBox.Text = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                textBox.Text = $"无法读取压缩文件: {ex.Message}";
            }

            return textBox;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                }

                // 释放非托管资源
                disposed = true;
            }
        }

        ~FilePreviewManager()
        {
            Dispose(false);
        }
    }
} 