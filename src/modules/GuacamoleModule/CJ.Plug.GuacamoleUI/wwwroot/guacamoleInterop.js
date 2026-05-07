// Guacamole JS Interop 模块
// 用于管理 Guacamole iframe 的生命周期

window.guacamoleInterop = {
    /**
     * 初始化 Guacamole iframe
     * @param {string} iframeId - iframe 元素 ID
     * @param {string} url - Guacamole 嵌入 URL
     */
    initialize: function (iframeId, url) {
        const iframe = document.getElementById(iframeId);
        if (iframe) {
            iframe.src = url;

            // 监听 iframe 加载事件
            iframe.onload = function () {
                console.log('Guacamole iframe loaded:', url);
            };

            iframe.onerror = function (error) {
                console.error('Guacamole iframe error:', error);
            };
        } else {
            console.error('Guacamole iframe not found:', iframeId);
        }
    },

    /**
     * 销毁 Guacamole 连接
     * @param {string} iframeId - iframe 元素 ID
     */
    dispose: function (iframeId) {
        const iframe = document.getElementById(iframeId);
        if (iframe) {
            iframe.src = 'about:blank';
        }
    },

    /**
     * 全屏切换
     * @param {string} containerId - 容器元素 ID
     */
    toggleFullscreen: function (containerId) {
        const container = document.getElementById(containerId);
        if (!document.fullscreenElement) {
            container.requestFullscreen().catch(err => {
                console.error('Fullscreen error:', err);
            });
        } else {
            document.exitFullscreen();
        }
    },

    /**
     * 调整 iframe 大小以适应容器
     * @param {string} iframeId - iframe 元素 ID
     */
    resizeToFit: function (iframeId) {
        const iframe = document.getElementById(iframeId);
        if (iframe) {
            iframe.style.width = '100%';
            iframe.style.height = '100vh';
        }
    }
};
