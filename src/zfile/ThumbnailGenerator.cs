using Ghostscript.NET.Rasterizer;
//using iTextSharp.text.pdf;
//using iTextSharp.text.pdf.parser;
//using Microsoft.WindowsAPICodePack.Shell;
//using OpenQA.Selenium;
//using OpenQA.Selenium.Chrome;
using Microsoft.WindowsAPICodePack.Shell;
using PdfiumViewer;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class ThumbnailGenerator
{
	public static Image GeneratePDFThumbnailByGhostscript(string pdfPath, int width = 200)
	{
		using (var rasterizer = new GhostscriptRasterizer())
		{
			rasterizer.Open(pdfPath);
			var img = rasterizer.GetPage(300, 0); // 300 DPI，第0页
			string outputPath = Path.GetTempFileName();
			img.Save(outputPath, ImageFormat.Jpeg);
			return img;
		}
	}
	//private static Image GeneratePDFThumbnail(string filePath)
	//{
	//	//using (PdfReader reader = new PdfReader(filePath))
	//	//{
	//	//    using (System.Drawing.Bitmap pageImage = iTextSharp.text.pdf.parser.PdfImageObject.GetImage(reader.GetPageN(1)))
	//	//    {
	//	//        int thumbnailWidth = 100;
	//	//        int thumbnailHeight = 100;
	//	//        return pageImage.GetThumbnailImage(thumbnailWidth, thumbnailHeight, null, IntPtr.Zero);
	//	//    }
	//	//}
	//	return null;
	//}
	public static Image GeneratePDFThumbnailByPDFium(string pdfPath, int thumbnailWidth = 64)
	{
		using (var document = PdfDocument.Load(pdfPath))
		{
			// 获取页面原始尺寸（单位：点）
			var pageSize = document.PageSizes[0];
			float pageWidth = pageSize.Width;  // 例如 595 点（A4 宽度）
			float pageHeight = pageSize.Height; // 例如 842 点（A4 高度）

			// 计算缩放比例（基于宽度或高度中更小的一侧）
			float scale = Math.Min(
				thumbnailWidth / pageWidth,
				64 / pageHeight // 如果固定最大高度为64
			);

			// 计算缩略图实际尺寸
			int thumbnailHeight = (int)(pageHeight * scale);

			// 创建目标位图
			using (var bitmap = new Bitmap(thumbnailWidth, thumbnailHeight))
			{
				using (var graphics = Graphics.FromImage(bitmap))
				{
					graphics.Clear(Color.White);
					graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
					graphics.SmoothingMode = SmoothingMode.HighQuality;

					// 计算实际渲染尺寸（可能小于缩略图尺寸）
					int renderWidth = (int)(pageWidth * scale);
					int renderHeight = (int)(pageHeight * scale);

					// 居中渲染区域
					int offsetX = (thumbnailWidth - renderWidth) / 2;
					int offsetY = (thumbnailHeight - renderHeight) / 2;

					// 渲染完整页面到居中位置
					document.Render(
						page: 0,
						graphics: graphics,
						dpiX: 96, // / scale, // 根据缩放调整 DPI
						dpiY: 96, // / scale,
						bounds: new Rectangle(
							x: offsetX,
							y: offsetY,
							width: renderWidth,
							height: renderHeight
						),
						flags: PdfRenderFlags.ForPrinting
					);
				}

				// 返回深拷贝
				return new Bitmap(bitmap);
			}
		}
	}
	public static Image GeneratePDFThumbnailByPDFiumbak(string pdfPath, int thumbnailWidth = 64)
	{
		// 加载 PDF 文件
		using (var document = PdfDocument.Load(pdfPath))
		{
			// 获取第一页的尺寸（以点为单位）
			var pageSize = document.PageSizes[0];

			// 计算缩略图的高度（保持宽高比）
			float scale = thumbnailWidth / pageSize.Width;
			int thumbnailHeight = (int)(pageSize.Height * scale);
			if (thumbnailHeight > 64) 
				thumbnailHeight = 64;
			// 创建位图并渲染 PDF 页面
			using (var bitmap = new Bitmap(thumbnailWidth, thumbnailHeight))
			{
				using (var graphics = Graphics.FromImage(bitmap))
				{
					graphics.Clear(Color.White); // 设置背景为白色
					document.Render(
						page: 0,
						graphics: graphics,
						dpiX: 96 * scale, // 根据缩放调整 DPI
						dpiY: 96 * scale,
						//bounds: new Rectangle(0, 0, thumbnailWidth, thumbnailHeight),
						bounds: new Rectangle(0, 0, (int)pageSize.Width, (int)pageSize.Height),
						flags: PdfRenderFlags.ForPrinting
					);
				}

				return new Bitmap(bitmap);
			}
		}		
	}
	private static string BuildFFmpegArgs(string input, string output)
	{
		return string.Format(
			"-y -loglevel error " +
			"-hwaccel auto " +          // 启用自动硬件加速
			"-ss 00:01:00 " +      // 定位到0.5秒（更接近关键帧）
			"-i \"{0}\" " +
			"-vf \"scale=320:-1:flags=fast_bilinear\" " + // 快速缩放算法
			"-vframes 1 " +
			"-q:v 31 " +               // 适当降低质量2-31， try 2-5 for thumbnails
			"-f image2 \"{1}\"",       // 强制输出为图片格式
			input, output);
	}
	private static int RunFFmpegCommand(string input, string output)
	{
		var args = BuildFFmpegArgs(input, output);//$"-y -loglevel error -i \"{input}\" -ss 00:01:10 -vframes 1 -vf \"scale=320:-1\" -q:v 2 \"{output}\"";

		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "ffmpeg.exe",
				Arguments = args,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true
			},
			EnableRaisingEvents = true
		};

		process.Start();
		process.WaitForExit(5000); // 5秒超时

		if (!process.HasExited)
		{
			process.Kill();
			return -1;
		}

		return process.ExitCode;
	}
	// 内存缓存（线程安全）
	private static readonly ConcurrentDictionary<string, Lazy<Task<Image>>> _thumbnailCache
		= new ConcurrentDictionary<string, Lazy<Task<Image>>>();

	public static Task<Image> GetThumbnailAsync(string filePath)
	{
		return _thumbnailCache.GetOrAdd(filePath, key =>
			new Lazy<Task<Image>>(() => GenerateThumbnailAsync(key))).Value;
	}

	private static async Task<Image> GenerateThumbnailAsync(string filePath)
	{
		string tempFile = Path.GetTempFileName();

		try
		{
			// 异步执行FFmpeg
			var exitCode = await RunFFmpegAsync(filePath, tempFile);
			if (exitCode != 0) return null;

			// 异步读取文件
			using var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
			var buffer = new byte[fs.Length];
			await fs.ReadAsync(buffer, 0, buffer.Length);

			return Image.FromStream(new MemoryStream(buffer));
		}
		finally
		{
			SafeDelete(tempFile);
		}
	}
	// 增强的文件删除方法
	private static void SafeDelete(string path)
	{
		const int maxRetry = 3;
		for (var i = 0; i < maxRetry; i++)
		{
			try
			{
				if (File.Exists(path))
					File.Delete(path);
				return;
			}
			catch (IOException)
			{
				if (i == maxRetry - 1) throw;
				Thread.Sleep(100 * (i + 1)); // 递增等待
			}
			catch { throw; }
		}
	}

	private static Task<int> RunFFmpegAsync(string input, string output)
	{
		var args = BuildFFmpegArgs(input, output);

		var tcs = new TaskCompletionSource<int>();
		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "ffmpeg.exe",
				Arguments = args,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true
			},
			EnableRaisingEvents = true
		};

		process.Exited += (sender, e) =>
		{
			tcs.TrySetResult(process.ExitCode);
			process.Dispose();
		};

		process.Start();
		return tcs.Task;
	}

	public static bool GetThumbnailWithWMF(string filePath, out Image image)
	{
		image = null;
		try
		{
			var shellFile = ShellFile.FromFilePath(filePath);
			var thumbnail = shellFile.Thumbnail.ExtraLargeBitmap;
			image = new Bitmap(thumbnail);
			return true;
		}
		catch
		{
			return false;
		}
	}
	public static bool GetThumbnailForVideo(string filePath, out Image image)
	{
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			return GetThumbnailWithWMF(filePath, out image);
		}
		else
		{
			return GetThumbnailWithFFmpeg(filePath, out image);
		}
	}
	public static bool GetThumbnailWithFFmpeg(string filePath, out Image image)
	{
		try
		{
			// 同步获取异步结果（适用于无法改造调用方的情况）
			var task = GetThumbnailAsync(filePath);
			if (task.Wait(TimeSpan.FromSeconds(3))) // 设置合理超时
			{
				image = task.Result;
				return true;
			}
			image = null;
			return false;
		}
		catch
		{
			image = null;
			return false;
		}
	}
	public static bool GetThumbnail(string filePath, out Image image)
    {
        try
        {
            string extension = Path.GetExtension(filePath).ToLower();

            if (IsImageFile(extension))
            {
                using (Image originalImage = Image.FromFile(filePath))
                {
                    int thumbnailWidth = 100;
                    int thumbnailHeight = 100;
                    image = originalImage.GetThumbnailImage(thumbnailWidth, thumbnailHeight, null, IntPtr.Zero);
                    return true;
                }
            }
            else if (IsVideoFile(extension))
            {
				//using (ShellObject shellObject = ShellObject.FromParsingName(filePath))
				//{
				//    image = shellObject.Thumbnail.SmallBitmap;
				//    return true;
				//}
				//string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
				//image = null;
				//try
				//{
				//	// 同步执行FFmpeg命令
				//	var exitCode = RunFFmpegCommand(filePath, tempFile);
				//	if (exitCode != 0 || !File.Exists(tempFile))
				//		return false;

				//	// 使用文件流确保完全写入
				//	using var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read);
				//	image = Image.FromStream(fs);
				//	return true;
				//}
				//catch (Exception ex)
				//{
				//	Debug.WriteLine($"生成缩略图失败: {ex.Message}");
				//	return false;
				//}
				//finally
				//{
				//	// 清理临时文件
				//	//if (File.Exists(tempFile))
				//	//	File.Delete(tempFile);
				//}
				return GetThumbnailForVideo(filePath, out image);
			}
            else if (IsPDFFile(extension))
            {
                image = GeneratePDFThumbnailByPDFium(filePath);
                return true;
            }
            else if (IsAudioFile(extension))
            {
                image = GetDefaultAudioThumbnail();
                return true;
            }
            else if (IsOfficeFile(extension))
            {
                // 这里可以添加 Office 文档转缩略图的具体逻辑
                image = GetDefaultOfficeThumbnail();
                return true;
            }
            else if (IsHTMLorMarkdownFile(extension))
            {
                image = GenerateHTMLorMarkdownThumbnail(filePath);
                return true;
            }
            else
            {
                image = null;
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating thumbnail: {ex.Message}");
            image = null;
            return false;
        }
    }

    private static bool IsImageFile(string extension)
    {
        string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif" };
        return Array.IndexOf(imageExtensions, extension) >= 0;
    }

    private static bool IsVideoFile(string extension)
    {
        string[] videoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".mkv", ".rmvb", ".rm", ".mpg", ".mpeg" };
        return Array.IndexOf(videoExtensions, extension) >= 0;
    }

    private static bool IsPDFFile(string extension)
    {
        return extension == ".pdf";
    }

    private static bool IsAudioFile(string extension)
    {
        string[] audioExtensions = { ".mp3", ".wav", ".ogg" };
        return Array.IndexOf(audioExtensions, extension) >= 0;
    }

    private static bool IsOfficeFile(string extension)
    {
        string[] officeExtensions = { ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
        return Array.IndexOf(officeExtensions, extension) >= 0;
    }

    private static bool IsHTMLorMarkdownFile(string extension)
    {
        string[] htmlMdExtensions = { ".html", ".htm", ".md" };
        return Array.IndexOf(htmlMdExtensions, extension) >= 0;
    }

  

    private static Image GetDefaultAudioThumbnail()
    {
        // 这里可以返回一个默认的音频图标
        return null;
    }

    private static Image GetDefaultOfficeThumbnail()
    {
        // 这里可以返回一个默认的 Office 文档图标
        return null;
    }

    private static Image GenerateHTMLorMarkdownThumbnail(string filePath)
    {
		//ChromeOptions options = new ChromeOptions();
		//options.AddArgument("--headless");

		//using (IWebDriver driver = new ChromeDriver(options))
		//{
		//    driver.Navigate().GoToUrl(new Uri(Path.GetFullPath(filePath)));
		//    Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
		//    using (MemoryStream ms = new MemoryStream(screenshot.AsByteArray))
		//    {
		//        using (Image fullImage = Image.FromStream(ms))
		//        {
		//            int thumbnailWidth = 100;
		//            int thumbnailHeight = 100;
		//            return fullImage.GetThumbnailImage(thumbnailWidth, thumbnailHeight, null, IntPtr.Zero);
		//        }
		//    }
		//}
		return null;
	}
}

