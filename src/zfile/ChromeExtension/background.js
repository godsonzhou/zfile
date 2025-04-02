// 原生消息主机名称
const nativeHostName = 'com.zfile.idm_integration';

// 初始化上下文菜单
chrome.runtime.onInstalled.addListener(() => {
    chrome.contextMenus.create({
        id: 'downloadWithZFileIDM',
        title: '使用ZFile IDM下载',
        contexts: ['link']
    });
});

// 处理上下文菜单点击
chrome.contextMenus.onClicked.addListener((info, tab) => {
    if (info.menuItemId === 'downloadWithZFileIDM' && info.linkUrl) {
        sendDownloadRequest(info.linkUrl, null, true, tab.url);
    }
});

// 处理工具栏图标点击
chrome.action.onClicked.addListener((tab) => {
    // 当用户点击扩展图标时，下载当前页面
    if (tab.url && tab.url.startsWith('http')) {
        sendDownloadRequest(tab.url, null, true, tab.url);
    }
});

// 拦截浏览器下载
chrome.downloads.onCreated.addListener((downloadItem) => {
    // 检查是否需要拦截此下载
    if (shouldInterceptDownload(downloadItem)) {
        // 取消浏览器下载
        chrome.downloads.cancel(downloadItem.id);

        // 使用IDM下载
        sendDownloadRequest(
            downloadItem.url,
            downloadItem.filename,
            false,
            downloadItem.referrer
        );
    }
});

// 判断是否应该拦截下载
function shouldInterceptDownload(downloadItem) {
    // 这里可以根据文件类型、大小等条件判断是否拦截
    // 示例：拦截大于5MB的文件
    return downloadItem.fileSize > 5 * 1024 * 1024;
}

// 发送下载请求到本地应用
function sendDownloadRequest(url, filename, saveAs, referrer) {
    // 准备下载请求
    const downloadRequest = {
        url: url,
        filename: filename || getFilenameFromUrl(url),
        saveAs: saveAs,
        referrer: referrer,
        headers: {},
        cookies: document.cookie
    };

    // 发送消息到本地应用
    chrome.runtime.sendNativeMessage(
        nativeHostName,
        downloadRequest,
        (response) => {
            if (chrome.runtime.lastError) {
                console.error('Native messaging error:', chrome.runtime.lastError);
                // 显示错误通知
                chrome.notifications.create({
                    type: 'basic',
                    iconUrl: 'images/icon48.png',
                    title: '下载错误',
                    message: '无法连接到ZFile IDM下载管理器，请确保应用已启动。'
                });
                return;
            }

            if (response && response.success) {
                // 显示成功通知
                chrome.notifications.create({
                    type: 'basic',
                    iconUrl: 'images/icon48.png',
                    title: '下载已开始',
                    message: `文件"${downloadRequest.filename}"已添加到下载队列。`
                });
            } else {
                // 显示错误通知
                chrome.notifications.create({
                    type: 'basic',
                    iconUrl: 'images/icon48.png',
                    title: '下载错误',
                    message: response ? response.message : '未知错误'
                });
            }
        }
    );
}

// 从URL中提取文件名
function getFilenameFromUrl(url) {
    try {
        const urlObj = new URL(url);
        const pathname = urlObj.pathname;
        const filename = pathname.substring(pathname.lastIndexOf('/') + 1);
        return filename || 'download';
    } catch (e) {
        return 'download';
    }
}