// xterm.js JavaScript Interop
// 用于 SSH 终端连接

window.xtermInterop = {
    term: null,
    socket: null,
    connected: false,
    fitAddon: null,

    /**
     * 连接到 SSH 服务器
     * @param {string} terminalId - 终端容器元素 ID
     * @param {string} wsUrl - WebSocket URL
     * @returns {boolean} 是否成功连接
     */
    connect: function (terminalId, wsUrl) {
        return new Promise((resolve) => {
            try {
                const container = document.getElementById(terminalId);
                if (!container) {
                    console.error('Terminal container not found:', terminalId);
                    resolve(false);
                    return;
                }

                // 检查 xterm.js 是否加载
                if (typeof Terminal === 'undefined') {
                    console.error('xterm.js library not loaded.');
                    resolve(false);
                    return;
                }

                // 创建终端
                this.term = new Terminal({
                    cursorBlink: true,
                    fontSize: 14,
                    fontFamily: 'Menlo, Monaco, "Courier New", monospace',
                    theme: {
                        background: '#1a1a1a',
                        foreground: '#f0f0f0',
                        cursor: '#f0f0f0',
                        cursorAccent: '#1a1a1a',
                        selectionBackground: '#264f78'
                    },
                    allowTransparency: true,
                    scrollback: 10000
                });

                // 加载 FitAddon
                if (typeof FitAddon !== 'undefined') {
                    this.fitAddon = new FitAddon.FitAddon();
                    this.term.loadAddon(this.fitAddon);
                }

                this.term.open(container);

                // 适应大小
                if (this.fitAddon) {
                    this.fitAddon.fit();
                    window.addEventListener('resize', () => this.fitAddon.fit());
                }

                // 连接 WebSocket
                this.socket = new WebSocket(wsUrl);

                this.socket.onopen = () => {
                    console.log('SSH WebSocket connected');
                    this.connected = true;

                    // 发送终端大小
                    if (this.socket.readyState === WebSocket.OPEN) {
                        const size = `${this.term.cols},${this.term.rows}`;
                        this.socket.send(JSON.stringify({ type: 'resize', cols: this.term.cols, rows: this.term.rows }));
                    }

                    resolve(true);
                };

                this.socket.onmessage = (event) => {
                    if (this.term) {
                        if (typeof event.data === 'string') {
                            this.term.write(event.data);
                        } else {
                            // 二进制数据
                            const reader = new FileReader();
                            reader.onload = () => {
                                this.term.write(new Uint8Array(reader.result));
                            };
                            reader.readAsArrayBuffer(event.data);
                        }
                    }
                };

                this.socket.onclose = (e) => {
                    console.log('SSH WebSocket closed:', e.code, e.reason);
                    this.connected = false;
                    if (this.term) {
                        this.term.write('\r\n\x1b[33m[连接已断开]\x1b[0m\r\n');
                    }
                };

                this.socket.onerror = (error) => {
                    console.error('SSH WebSocket error:', error);
                    resolve(false);
                };

                // 终端输入 -> WebSocket
                this.term.onData((data) => {
                    if (this.socket && this.socket.readyState === WebSocket.OPEN) {
                        this.socket.send(data);
                    }
                });

                // 终端大小变化
                this.term.onResize(({ cols, rows }) => {
                    if (this.socket && this.socket.readyState === WebSocket.OPEN) {
                        this.socket.send(JSON.stringify({ type: 'resize', cols, rows }));
                    }
                });

            } catch (error) {
                console.error('SSH connection error:', error);
                resolve(false);
            }
        });
    },

    /**
     * 断开 SSH 连接
     */
    disconnect: function () {
        if (this.socket) {
            this.socket.close();
            this.socket = null;
        }
        if (this.term) {
            this.term.dispose();
            this.term = null;
        }
        this.connected = false;
    },

    /**
     * 写入数据到终端
     * @param {string} data - 要写入的数据
     */
    write: function (data) {
        if (this.term) {
            this.term.write(data);
        }
    },

    /**
     * 清屏
     */
    clear: function () {
        if (this.term) {
            this.term.clear();
        }
    },

    /**
     * 获取终端大小
     * @returns {{ cols: number, rows: number }}
     */
    getSize: function () {
        if (this.term) {
            return { cols: this.term.cols, rows: this.term.rows };
        }
        return { cols: 80, rows: 24 };
    },

    /**
     * 设置焦点
     */
    focus: function () {
        if (this.term) {
            this.term.focus();
        }
    }
};
