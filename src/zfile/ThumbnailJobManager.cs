//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Drawing;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace zfile
//{
//    /// <summary>
//    /// 缩略图任务管理器，负责在后台线程中生成缩略图并更新UI
//    /// </summary>
//    public class ThumbnailJobManager : IDisposable
//    {
//        // 任务队列
//        private readonly ConcurrentQueue<ThumbnailJob> _jobQueue = new ConcurrentQueue<ThumbnailJob>();
        
//        // 缩略图管理器引用
//        private readonly ThumbnailManager _thumbnailManager;
        
//        // 取消令牌源
//        private CancellationTokenSource _cancellationTokenSource;
        
//        // 处理线程
//        private Task _processingTask;
        
//        // 是否正在处理
//        private bool _isProcessing;
        
//        // 批量更新计数器
//        private int _batchCounter;
        
//        // 批量更新阈值，每处理这么多项后更新一次UI
//        private const int BatchUpdateThreshold = 5;
        
//        // 进度回调
//        private readonly Action<ThumbnailProgress> _progressCallback;
        
//        // 当前进度信息
//        private ThumbnailProgress _currentProgress;
        
//        // 是否已释放
//        private bool _disposed;

//        /// <summary>
//        /// 缩略图任务进度信息
//        /// </summary>
//        public class ThumbnailProgress
//        {
//            /// <summary>
//            /// 总任务数
//            /// </summary>
//            public int TotalJobs { get; set; }
            
//            /// <summary>
//            /// 已完成任务数
//            /// </summary>
//            public int CompletedJobs { get; set; }
            
//            /// <summary>
//            /// 当前处理的文件名
//            /// </summary>
//            public string CurrentFile { get; set; }
            
//            /// <summary>
//            /// 是否已完成所有任务
//            /// </summary>
//            public bool IsCompleted => CompletedJobs >= TotalJobs;
            
//            /// <summary>
//            /// 待更新的ListViewItem集合
//            /// </summary>
//            public List<(ListViewItem Item, string ImageKey)> ItemsToUpdate { get; } = new List<(ListViewItem, string)>();
//        }

//        /// <summary>
//        /// 缩略图任务
//        /// </summary>
//        private class ThumbnailJob
//        {
//            /// <summary>
//            /// 需要处理的ListViewItem
//            /// </summary>
//            public ListViewItem Item { get; set; }
            
//            /// <summary>
//            /// 文件完整路径
//            /// </summary>
//            public string FilePath { get; set; }
            
//            /// <summary>
//            /// 所属的ImageList
//            /// </summary>
//            public ImageList ImageList { get; set; }
//        }

//        /// <summary>
//        /// 构造函数
//        /// </summary>
//        /// <param name="thumbnailManager">缩略图管理器</param>
//        /// <param name="progressCallback">进度回调</param>
//        public ThumbnailJobManager(ThumbnailManager thumbnailManager, Action<ThumbnailProgress> progressCallback)
//        {
//            _thumbnailManager = thumbnailManager ?? throw new ArgumentNullException(nameof(thumbnailManager));
//            _progressCallback = progressCallback ?? throw new ArgumentNullException(nameof(progressCallback));
//            _currentProgress = new ThumbnailProgress();
//            _cancellationTokenSource = new CancellationTokenSource();
//        }

//        /// <summary>
//        /// 将缩略图生成任务加入队列
//        /// </summary>
//        /// <param name="item">ListViewItem</param>
//        /// <param name="filePath">文件路径</param>
//        /// <param name="imageList">图像列表</param>
//        public void EnqueueJob(ListViewItem item, string filePath, ImageList imageList)
//        {
//            if (item == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
//                return;

//            var job = new ThumbnailJob
//            {
//                Item = item,
//                FilePath = filePath,
//                ImageList = imageList
//            };

//            _jobQueue.Enqueue(job);
//            _currentProgress.TotalJobs++;

//            // 如果处理线程未启动，则启动它
//            StartProcessingIfNeeded();
//        }

//        /// <summary>
//        /// 启动处理线程（如果尚未启动）
//        /// </summary>
//        private void StartProcessingIfNeeded()
//        {
//            if (_isProcessing)
//                return;

//            _isProcessing = true;
//            _cancellationTokenSource = new CancellationTokenSource();
//            _processingTask = Task.Run(() => ProcessJobsAsync(_cancellationTokenSource.Token));
//        }

//        /// <summary>
//        /// 异步处理任务队列
//        /// </summary>
//        private async Task ProcessJobsAsync(CancellationToken cancellationToken)
//        {
//            try
//            {
//                _batchCounter = 0;
                
//                // 报告初始进度
//                ReportProgress();

//                while (!_jobQueue.IsEmpty && !cancellationToken.IsCancellationRequested)
//                {
//                    if (_jobQueue.TryDequeue(out ThumbnailJob job))
//                    {
//                        try
//                        {
//                            // 更新当前处理文件名
//                            _currentProgress.CurrentFile = Path.GetFileName(job.FilePath);
//                            ReportProgress();

//                            // 生成缩略图
//                            var thumb = _thumbnailManager.CreatePreview(job.FilePath, out string md5key);
                            
//                            if (thumb != null)
//                            {
//                                // 将更新操作添加到批处理列表
//                                lock (_currentProgress.ItemsToUpdate)
//                                {
//                                    _currentProgress.ItemsToUpdate.Add((job.Item, md5key));
//                                }

//                                // 在UI线程上更新ImageList
//                                await Task.Run(() =>
//                                {
//                                    try
//                                    {
//                                        if (job.Item.ListView?.InvokeRequired == true)
//                                        {
//                                            job.Item.ListView.Invoke(new Action(() =>
//                                            {
//                                                if (!job.ImageList.Images.ContainsKey(md5key))
//                                                    job.ImageList.Images.Add(md5key, thumb);
//                                            }));
//                                        }
//                                        else
//                                        {
//                                            if (!job.ImageList.Images.ContainsKey(md5key))
//                                                job.ImageList.Images.Add(md5key, thumb);
//                                        }
//                                    }
//                                    catch (Exception ex)
//                                    {
//                                        Debug.Print($"更新ImageList异常: {ex.Message}");
//                                    }
//                                });
//                            }

//                            // 更新完成计数
//                            _currentProgress.CompletedJobs++;
//                            _batchCounter++;

//                            // 批量更新UI
//                            if (_batchCounter >= BatchUpdateThreshold)
//                            {
//                                await UpdateUIAsync();
//                                _batchCounter = 0;
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            Debug.Print($"处理缩略图任务异常: {ex.Message}");
//                            _currentProgress.CompletedJobs++;
//                        }
//                    }
//                }

//                // 处理完成后，执行最后一次UI更新
//                await UpdateUIAsync();
                
//                // 标记任务完成
//                _currentProgress.CurrentFile = null;
//                ReportProgress();
//            }
//            catch (Exception ex)
//            {
//                Debug.Print($"缩略图处理线程异常: {ex.Message}");
//            }
//            finally
//            {
//                _isProcessing = false;
//            }
//        }

//        /// <summary>
//        /// 更新UI
//        /// </summary>
//        private async Task UpdateUIAsync()
//        {
//            List<(ListViewItem Item, string ImageKey)> itemsToUpdate;
            
//            // 获取并清空待更新项
//            lock (_currentProgress.ItemsToUpdate)
//            {
//                if (_currentProgress.ItemsToUpdate.Count == 0)
//                    return;
                    
//                itemsToUpdate = new List<(ListViewItem, string)>(_currentProgress.ItemsToUpdate);
//                _currentProgress.ItemsToUpdate.Clear();
//            }

//            // 按ListView分组
//            var groupedItems = itemsToUpdate
//                .GroupBy(x => x.Item.ListView)
//                .Where(g => g.Key != null);

//            foreach (var group in groupedItems)
//            {
//                var listView = group.Key;
                
//                try
//                {
//                    if (listView.InvokeRequired)
//                    {
//                        await Task.Run(() =>
//                        {
//                            try
//                            {
//                                listView.Invoke(new Action(() =>
//                                {
//                                    UpdateListViewItems(group.ToList());
//                                }));
//                            }
//                            catch (Exception ex)
//                            {
//                                Debug.Print($"更新ListView异常: {ex.Message}");
//                            }
//                        });
//                    }
//                    else
//                    {
//                        UpdateListViewItems(group.ToList());
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.Print($"更新ListView异常: {ex.Message}");
//                }
//            }

//            // 报告进度
//            ReportProgress();
//        }

//        /// <summary>
//        /// 更新ListView项的图标
//        /// </summary>
//        private void UpdateListViewItems(List<(ListViewItem Item, string ImageKey)> items)
//        {
//            if (items.Count == 0 || items[0].Item.ListView == null)
//                return;

//            var listView = items[0].Item.ListView;
            
//            try
//            {
//                listView.BeginUpdate();
                
//                foreach (var (item, imageKey) in items)
//                {
//                    if (item != null && !string.IsNullOrEmpty(imageKey))
//                    {
//                        item.ImageKey = imageKey;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.Print($"更新ListViewItem异常: {ex.Message}");
//            }
//            finally
//            {
//                try
//                {
//                    listView.EndUpdate();
//                }
//                catch { }
//            }
//        }

//        /// <summary>
//        /// 报告进度
//        /// </summary>
//        private void ReportProgress()
//        {
//            try
//            {
//                _progressCallback?.Invoke(_currentProgress);
//            }
//            catch (Exception ex)
//            {
//                Debug.Print($"报告进度异常: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// 取消当前所有任务
//        /// </summary>
//        public void CancelCurrentTasks()
//        {
//            try
//            {
//                _cancellationTokenSource.Cancel();
                
//                // 清空队列
//                while (_jobQueue.TryDequeue(out _)) { }
                
//                // 重置进度
//                _currentProgress = new ThumbnailProgress();
//                ReportProgress();
//            }
//            catch (Exception ex)
//            {
//                Debug.Print($"取消任务异常: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// 释放资源
//        /// </summary>
//        public void Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//        /// <summary>
//        /// 释放资源
//        /// </summary>
//        protected virtual void Dispose(bool disposing)
//        {
//            if (_disposed)
//                return;

//            if (disposing)
//            {
//                CancelCurrentTasks();
//                _cancellationTokenSource?.Dispose();
//            }

//            _disposed = true;
//        }
//    }
//}