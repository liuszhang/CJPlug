// Capture WebSocket Interop
// 用于远程屏幕捕获连接

window.captureInterop = {
    ws: null,
    _currentBlobUrl: null,
    _frameCount: 0,

    /**
     * 连接到 Capture WebSocket 并在 img 元素上显示画面
     * @param {string} imgId - img 元素 ID
     * @param {string} wsUrl - WebSocket URL
     * @returns {Promise<boolean>} 是否成功连接
     */
    connect: function (imgId, wsUrl) {
        return new Promise((resolve) => {
            try {
                var img = document.getElementById(imgId);
                if (!img) {
                    console.error('[Capture] Image element not found:', imgId);
                    resolve(false);
                    return;
                }

                // 清理之前的连接
                this.disconnect();
                this._frameCount = 0;

                console.log('[Capture] Connecting to:', wsUrl);

                var ws = new WebSocket(wsUrl);
                ws.binaryType = 'arraybuffer';
                this.ws = ws;

                // 超时处理 (10秒)
                var settled = false;
                var timeout = setTimeout(function () {
                    if (!settled) {
                        settled = true;
                        console.error('[Capture] Connection timeout after 10s');
                        ws.close();
                        resolve(false);
                    }
                }, 10000);

                ws.onopen = function () {
                    if (!settled) {
                        settled = true;
                        clearTimeout(timeout);
                        console.log('[Capture] WebSocket connected');
                        resolve(true);
                    }
                };

                ws.onmessage = function (event) {
                    if (event.data instanceof ArrayBuffer) {
                        // Binary frame: JPEG image data → set as img src
                        this._frameCount++;
                        var blob = new Blob([event.data], { type: 'image/jpeg' });

                        // Revoke old URL to prevent memory leak
                        if (this._currentBlobUrl) {
                            URL.revokeObjectURL(this._currentBlobUrl);
                        }

                        this._currentBlobUrl = URL.createObjectURL(blob);
                        img.src = this._currentBlobUrl;
                    } else {
                        // Text message: log it (info/error metadata from station)
                        console.log('[Capture] Text:', event.data);
                        try {
                            var msg = JSON.parse(event.data);
                            if (msg.type === 'error') {
                                console.error('[Capture] Station error:', msg.message);
                            }
                        } catch (e) { /* not JSON, ignore */ }
                    }
                }.bind(this);

                ws.onerror = function (err) {
                    if (!settled) {
                        settled = true;
                        clearTimeout(timeout);
                        console.error('[Capture] WebSocket error:', err);
                        resolve(false);
                    }
                };

                ws.onclose = function (e) {
                    console.warn('[Capture] WebSocket closed:', e.code, e.reason);
                    // Cleanup blob URL
                    if (this._currentBlobUrl) {
                        URL.revokeObjectURL(this._currentBlobUrl);
                        this._currentBlobUrl = null;
                    }
                    if (!settled) {
                        settled = true;
                        clearTimeout(timeout);
                        resolve(false);
                    }
                    this.ws = null;
                }.bind(this);

            } catch (error) {
                console.error('[Capture] Connection error:', error);
                resolve(false);
            }
        });
    },

    /**
     * 断开 Capture WebSocket 连接
     */
    disconnect: function () {
        if (this.ws) {
            try {
                this.ws.close();
            } catch (e) {
                console.warn('[Capture] Disconnect error:', e);
            }
            this.ws = null;
        }
        if (this._currentBlobUrl) {
            URL.revokeObjectURL(this._currentBlobUrl);
            this._currentBlobUrl = null;
        }
        this._frameCount = 0;
    },

    /**
     * 设置捕获帧率
     * @param {number} fps - 每秒帧数
     */
    setFps: function (fps) {
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify({ type: 'fps', value: fps }));
        }
    },

    /**
     * 获取可捕获的窗口列表
     * @param {string} stationHost - Station 主机地址
     * @returns {Promise<object>} 窗口列表 JSON
     */
    getWindows: async function (stationHost) {
        try {
            var response = await fetch('/api/remote/capture/windows/' + encodeURIComponent(stationHost));
            if (!response.ok) {
                throw new Error('HTTP ' + response.status + ': ' + response.statusText);
            }
            return await response.json();
        } catch (error) {
            console.error('[Capture] getWindows error:', error);
            throw error;
        }
    }
};
