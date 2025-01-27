using CSCore;
using CSCore.SoundOut;
using LibVLCSharp.Shared;
using SharpCompress.Archives;
using System.Text;

namespace WinFormsApp1
{
	public class FilePreviewManager
    {
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
    }
} 