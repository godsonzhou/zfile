using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace zfile
{
    /// <summary>
    /// 后台图标管理器，负责在后台线程中生成图标并更新UI
    /// </summary>
    public class BackgroundIconManager : IDisposable
    {
        // 任务队列
        private readonly ConcurrentQueue<IconJob> _jobQueue = new ConcurrentQueue<IconJob>();
        
        // 缩略图管理器引用
        private readonly ThumbnailManager _thumbnailManager;
        private readonly IconManager _iconManager;
        
        // 取消令牌源
        private CancellationTokenSource _cancellationTokenSource;
        
        // 处理线程
        private Task _processingTask;
        
        // 是否正在处理
        private bool _isProcessing;
        
        // 批量更新计数器
        private int _batchCounter;
        
        // 批量更新阈值，每处理这么多项后更新一次UI
        private const int BatchUpdateThreshold = 100;
        
        // 进度回调
        private readonly Action<IconProgress> _progressCallback;
        
        // 当前进度信息
        private IconProgress _currentProgress;
        
        // 是否已释放
        private bool _disposed;

        /// <summary>
        /// 图标任务进度信息
        /// </summary>
        public class IconProgress
        {
            /// <summary>
            /// 总任务数
            /// </summary>
            public int TotalJobs { get; set; }
            
            /// <summary>
            /// 已完成任务数
            /// </summary>
            public int CompletedJobs { get; set; }
            
            /// <summary>
            /// 当前处理的文件名
            /// </summary>
            public string CurrentFile { get; set; }
            
            /// <summary>
            /// 是否已完成所有任务
            /// </summary>
            public bool IsCompleted => CompletedJobs >= TotalJobs;
            
            /// <summary>
            /// 待更新的ListViewItem集合
            /// </summary>
            public List<(ListView Item, string ImageKey, string filepath, ListViewItem item)> ItemsToUpdate { get; } = new ();
        }

        /// <summary>
        /// 图标任务
        /// </summary>
        private class IconJob
        {
            /// <summary>
            /// 需要处理的ListViewItem
            /// </summary>
            public ListView View { get; set; }
            
            /// <summary>
            /// 文件完整路径
            /// </summary>
            public List<string> FilePaths { get; set; }
            
            /// <summary>
            /// 所属的ImageList
            /// </summary>
            //public ImageList ImageList { get; set; }
            public List<ListViewItem> Items { get; set; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="thumbnailManager">缩略图管理器</param>
        /// <param name="iconManager">图标管理器</param>
        /// <param name="progressCallback">进度回调</param>
        public BackgroundIconManager(ThumbnailManager thumbnailManager, IconManager iconManager)
        {
            _thumbnailManager = thumbnailManager ?? throw new ArgumentNullException(nameof(thumbnailManager));
            _iconManager = iconManager ?? throw new ArgumentNullException(nameof(iconManager));
            //_progressCallback = progressCallback ?? throw new ArgumentNullException(nameof(progressCallback));
            _currentProgress = new IconProgress();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 将图标生成任务加入队列
        /// </summary>
        /// <param name="view">ListViewItem</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="imageList">图像列表</param>
        /// <param name="iconType">图标类型 (s: 小图标, l: 大图标/缩略图)</param>
        public void EnqueueJob(ListView view, List<string> filePaths, List<ListViewItem> Items)
        {
            if (view == null || filePaths.Count == 0)
                return;

            var job = new IconJob
            {
                View = view,
                FilePaths = filePaths,
                //ImageList = view.LargeImageList
				Items = Items
            };

            _jobQueue.Enqueue(job);
            _currentProgress.TotalJobs++;

            // 如果处理线程未启动，则启动它
            StartProcessingIfNeeded();
        }

        /// <summary>
        /// 启动处理线程（如果尚未启动）
        /// </summary>
        private void StartProcessingIfNeeded()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = Task.Run(() => ProcessJobsAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 异步处理任务队列
        /// </summary>
        private async Task ProcessJobsAsync(CancellationToken cancellationToken)
        {
            try
            {
                _batchCounter = 0;
                
                // 报告初始进度
                ReportProgress();

                while (!_jobQueue.IsEmpty && !cancellationToken.IsCancellationRequested)
                {
                    if (_jobQueue.TryDequeue(out IconJob job))
                    {
                        try
                        {
                            // 更新当前处理文件名
                            _currentProgress.CurrentFile = Path.GetFileName(job.FilePaths[0]);
                            ReportProgress();

                            // 根据图标类型处理
							// 大图标或缩略图
                            {
                                //await ProcessLargeIconOrThumbnailAsync(job);
                            }
							for (var idx = 0; idx < job.FilePaths.Count; idx ++)
							{
								var jobFilePath = job.FilePaths[idx];
								var key = Path.GetExtension(jobFilePath);
								string imageKey = key;

								// 尝试生成缩略图
								var thumb = _thumbnailManager.CreatePreview(jobFilePath, out string md5key);
								if (thumb != null)
								{
									Debug.Print("thumb generated: {0}, {1}", jobFilePath, md5key);
									imageKey = md5key;

									// 在UI线程上更新ImageList
									await Task.Run(() =>
									{
										try
										{
											if (job.View?.InvokeRequired == true)
											{
												job.View.Invoke(new Action(() =>
												{
													if (!job.View.LargeImageList.Images.ContainsKey(md5key))
														job.View.LargeImageList.Images.Add(md5key, thumb);
												}));
											}
											else
											{
												if (!job.View.LargeImageList.Images.ContainsKey(md5key))
													job.View.LargeImageList.Images.Add(md5key, thumb);
											}
										}
										catch (Exception ex)
										{
											Debug.Print($"更新ImageList异常: {ex.Message}");
										}
									});
								}
								else
								{
									// 如果没有缩略图，使用大图标
									if (!_iconManager.HasIconKey(key, true))
									{
										var icol = IconManager.GetIconByFileNameEx("FILE", jobFilePath, true);
										if (icol != null)
											_iconManager.AddIcon(key, icol, true);
									}
								}

								// 添加到待更新列表
								lock (_currentProgress.ItemsToUpdate)
								{
									_currentProgress.ItemsToUpdate.Add((job.View, imageKey, jobFilePath, job.Items[idx]));
								}
								// 更新完成计数
								_currentProgress.CompletedJobs++;
								_batchCounter++;

								// 批量更新UI
								if (_batchCounter >= BatchUpdateThreshold)
								{
									await UpdateUIAsync();
									_batchCounter = 0;
								}
							}

					
                        }
                        catch (Exception ex)
                        {
                            Debug.Print($"处理图标任务异常: {ex.Message}");
                            _currentProgress.CompletedJobs++;
                        }
                    }
                }

                // 处理完成后，执行最后一次UI更新
                await UpdateUIAsync();
                
                // 标记任务完成
                _currentProgress.CurrentFile = null;
                ReportProgress();
            }
            catch (Exception ex)
            {
                Debug.Print($"图标处理线程异常: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 处理小图标
        /// </summary>
        //private void ProcessSmallIcon(IconJob job)
        //{
        //    var key = Path.GetExtension(job.FilePath);
        //    if (!_iconManager.HasIconKey(key, false))
        //    {
        //        var ico = IconManager.GetIconByFileNameEx("FILE", job.FilePath);
        //        if (ico != null)
        //            _iconManager.AddIcon(key, ico, false);
        //    }

        //    // 在UI线程上更新ImageList和ListViewItem
        //    lock (_currentProgress.ItemsToUpdate)
        //    {
        //        _currentProgress.ItemsToUpdate.Add((job.Item, key));
        //    }
        //}

	

        /// <summary>
        /// 更新UI
        /// </summary>
        private async Task UpdateUIAsync()
        {
            List<(ListView Item, string ImageKey, string filepath, ListViewItem)> itemsToUpdate;
            
            // 获取并清空待更新项
            lock (_currentProgress.ItemsToUpdate)
            {
                if (_currentProgress.ItemsToUpdate.Count == 0)
                    return;
                    
                itemsToUpdate = new List<(ListView, string, string, ListViewItem)>(_currentProgress.ItemsToUpdate);
                _currentProgress.ItemsToUpdate.Clear();
            }

            // 按ListView分组
            var groupedItems = itemsToUpdate
                .GroupBy(x => x.Item)
                .Where(g => g.Key != null);

            foreach (var group in groupedItems)
            {
                var listView = group.Key;
                
                try
                {
                    if (listView.InvokeRequired)
                    {
                        await Task.Run(() =>
                        {
                            try
                            {
                                listView.Invoke(new Action(() =>
                                {
                                    UpdateListViewItems(group.ToList());
                                }));
                            }
                            catch (Exception ex)
                            {
                                Debug.Print($"更新ListView异常: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        UpdateListViewItems(group.ToList());
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"更新ListView异常: {ex.Message}");
                }
            }

            // 报告进度
            ReportProgress();
        }

        /// <summary>
        /// 更新ListView项的图标
        /// </summary>
        private void UpdateListViewItems(List<(ListView Item, string ImageKey, string filepath, ListViewItem)> items)
        {
            if (items.Count == 0 || items[0].Item == null)
                return;

            var listView = items[0].Item;
            
            try
            {
                listView.BeginUpdate();
                
                foreach (var (item, imageKey, filepath, i) in items)
                {
                    if (item != null && !string.IsNullOrEmpty(imageKey))
						i.ImageKey = imageKey;
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"更新ListViewItem异常: {ex.Message}");
            }
            finally
            {
                try
                {
                    listView.EndUpdate();
                }
                catch { }
            }
        }

        /// <summary>
        /// 报告进度
        /// </summary>
        private void ReportProgress()
        {
            try
            {
                _progressCallback?.Invoke(_currentProgress);
            }
            catch (Exception ex)
            {
                Debug.Print($"报告进度异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消当前所有任务
        /// </summary>
        public void CancelCurrentTasks()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                
                // 清空队列
                while (_jobQueue.TryDequeue(out _)) { }
                
                // 重置进度
                _currentProgress = new IconProgress();
                ReportProgress();
            }
            catch (Exception ex)
            {
                Debug.Print($"取消任务异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                CancelCurrentTasks();
                _cancellationTokenSource?.Dispose();
            }

            _disposed = true;
        }
    }
}