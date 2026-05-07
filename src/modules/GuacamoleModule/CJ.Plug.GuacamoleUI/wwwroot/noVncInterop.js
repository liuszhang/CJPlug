// noVNC JavaScript Interop
// 用于 VNC 远程桌面连接

window.noVncInterop = {
    rfb: null,
    connected: false,

    /**
     * 连接到 VNC 服务器
     * @param {string} containerId - 容器 div 元素 ID (不是 canvas)
     * @param {string} wsUrl - WebSocket URL
     * @returns {boolean} 是否成功连接
     */
    connect: function (containerId, wsUrl) {
        return new Promise((resolve) => {
            try {
                var container = document.getElementById(containerId);
                if (!container) {
                    console.error('[noVNC] Container element not found:', containerId);
                    resolve(false);
                    return;
                }

                // 检查 noVNC 库是否加载
                if (typeof RFB === 'undefined') {
                    console.error('[noVNC] RFB class not defined - library not loaded');
                    resolve(false);
                    return;
                }

                // 清理之前的连接
                if (this.rfb) {
                    try { this.rfb.disconnect(); } catch(e) {}
                    this.rfb = null;
                    this.connected = false;
                }

                // 清空容器
                container.innerHTML = '';

                console.log('[noVNC] Connecting to VNC:', wsUrl);

                // 创建 RFB 实例 — 构造函数会立即创建 WebSocket
                var rfb = new RFB(container, wsUrl, {
                    credentials: { password: '' },
                    shared: true
                });

                this.rfb = rfb;

                // 设置显示选项
                rfb.scaleViewport = true;
                rfb.resizeSession = true;
                rfb.viewOnly = false;

                // 超时处理 (10秒)
                var settled = false;
                var timeout = setTimeout(function() {
                    if (!settled) {
                        settled = true;
                        console.error('[noVNC] Connection timeout after 10s');
                        try { rfb.disconnect(); } catch(e) {}
                        resolve(false);
                    }
                }, 10000);

                // 监听连接成功
                rfb.addEventListener('connect', function () {
                    if (!settled) {
                        settled = true;
                        clearTimeout(timeout);
                        console.log('[noVNC] VNC connected successfully');
                        this.connected = true;
                        resolve(true);
                    }
                }.bind(this));

                // 监听断开连接
                rfb.addEventListener('disconnect', function (e) {
                    if (!settled) {
                        settled = true;
                        clearTimeout(timeout);
                        var detail = e && e.detail ? e.detail : 'unknown';
                        console.warn('[noVNC] VNC disconnected:', detail);
                        this.connected = false;
                        resolve(false);
                    }
                }.bind(this));

                // 监听需要凭据 (VNC密码)
                rfb.addEventListener('credentialsrequired', function (e) {
                    console.log('[noVNC] Credentials required, sending empty password');
                    rfb.sendCredentials({ password: '' });
                });

            } catch (error) {
                console.error('[noVNC] Connection error:', error);
                resolve(false);
            }
        });
    },

    /**
     * 断开 VNC 连接
     */
    disconnect: function () {
        if (this.rfb) {
            try {
                this.rfb.disconnect();
            } catch(e) {
                console.warn('[noVNC] Disconnect error:', e);
            }
            this.rfb = null;
            this.connected = false;
        }
    },

    /**
     * 适应屏幕大小
     */
    fitToScreen: function () {
        if (this.rfb) {
            this.rfb.scaleViewport = true;
        }
    },

    /**
     * 切换全屏
     * @param {string} elementId - 元素 ID
     */
    toggleFullscreen: function (elementId) {
        var element = document.getElementById(elementId);
        if (!element) return;
        if (!document.fullscreenElement) {
            element.requestFullscreen().catch(function(err) {
                console.error('[noVNC] Fullscreen error:', err);
            });
        } else {
            document.exitFullscreen();
        }
    },

    /**
     * 发送 Ctrl+Alt+Del
     */
    sendCtrlAltDel: function () {
        if (this.rfb) {
            this.rfb.sendCtrlAltDel();
        }
    }
};
