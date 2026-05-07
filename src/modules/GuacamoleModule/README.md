# 远程桌面集成说明

## 架构说明

轻量级远程桌面方案，无需 Docker，无需外部依赖：

```
主服务器 (ApiServer)
  └─ WebSocket 代理
       ├─ VNC 代理 → Station:5900
       └─ SSH 代理 → Station:22

浏览器
  ├─ noVNC (纯JS) → WebSocket → 主服务器代理 → Station VNC
  └─ xterm.js (纯JS) → WebSocket → 主服务器代理 → Station SSH

Station (StationApiServer)
  ├─ RemoteDesktopService (检测/管理 VNC/SSH)
  └─ API: /api/station/remote/*
```

## 组件说明

### 主服务器 (ApiServer)

| 组件 | 功能 |
|------|------|
| VncWebSocketProxy | VNC WebSocket→TCP 代理 |
| SshWebSocketProxy | SSH WebSocket→TCP 代理 (SSH.NET) |
| RemoteDesktopApi | WebSocket 端点 |
| StationRemoteStatusApi | 查询 Station 远程服务状态 |

### Station (StationApiServer)

| 组件 | 功能 |
|------|------|
| RemoteDesktopService | 检测/启动 VNC/SSH 服务 |
| RemoteDesktopApi | 管理 API 端点 |

## API 端点

### 主服务器

```
WebSocket: ws://host/api/remote/vnc?host={stationIp}&port=5900
WebSocket: ws://host/api/remote/ssh?host={stationIp}&port=22&username=xxx&password=xxx

GET  /api/station-remote/status/{stationIp}     查询 Station 远程服务状态
POST /api/station-remote/vnc/start/{stationIp}   请求 Station 启动 VNC
POST /api/station-remote/ssh/start/{stationIp}   请求 Station 启动 SSH
```

### Station

```
GET  /api/station/remote/status           获取 VNC/SSH 服务状态
POST /api/station/remote/vnc/start        启动 VNC 服务
POST /api/station/remote/ssh/start        启动 SSH 服务
```

## 使用方式

### 1. 查询 Station 远程服务状态

```bash
curl http://localhost:8687/api/station-remote/status/192.168.1.100
```

响应：
```json
{
  "vncInstalled": true,
  "vncRunning": false,
  "vncPort": 5900,
  "sshInstalled": true,
  "sshRunning": true,
  "sshPort": 22
}
```

### 2. 请求 Station 启动服务

```bash
# 启动 VNC
curl -X POST http://localhost:8687/api/station-remote/vnc/start/192.168.1.100?port=5900

# 启动 SSH
curl -X POST http://localhost:8687/api/station-remote/ssh/start/192.168.1.100?port=22
```

### 3. 浏览器访问

```
VNC: http://localhost:5066/remote/vnc/192.168.1.100?port=5900
SSH: http://localhost:5066/remote/ssh/192.168.1.100?port=22&username=root&password=xxx
```

## Station 配置要求

### VNC 服务

**Windows:**
- TightVNC: https://www.tightvnc.com/
- RealVNC: https://www.realvnc.com/
- UltraVNC: https://www.uvnc.com/

**Linux:**
```bash
# Ubuntu/Debian
sudo apt install tigervnc-standalone-server
vncserver :0 -geometry 1920x1080

# 或使用 x11vnc
sudo apt install x11vnc
x11vnc -display :0 -rfbport 5900
```

### SSH 服务

**Windows:**
```powershell
# Windows 10+ 自带 OpenSSH Server
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
Start-Service sshd
Set-Service -Name sshd -StartupType Automatic
```

**Linux:**
```bash
sudo apt install openssh-server
sudo systemctl start sshd
sudo systemctl enable sshd
```

## 安全建议

1. 生产环境使用 WSS (WebSocket over TLS)
2. 限制可访问的 IP 范围
3. SSH 建议使用密钥认证
4. VNC 设置强密码

## 文件结构

```
CJ.Plug.StationApiServer/
├── Services/
│   └── RemoteDesktopService.cs    # VNC/SSH 检测和管理
└── Apis/
    └── RemoteDesktopApi.cs        # Station API 端点

CJ.Plug.GuacamoleApi/
├── Services/
│   ├── VncWebSocketProxy.cs       # VNC WebSocket 代理
│   └── SshWebSocketProxy.cs       # SSH WebSocket 代理
└── Apis/
    ├── RemoteDesktopApi.cs        # WebSocket 端点
    └── StationRemoteStatusApi.cs  # Station 状态查询

CJ.Plug.GuacamoleUI/
├── Pages/
│   ├── RemoteViewer.razor         # 路由页面
│   ├── VncViewer.razor            # VNC 查看器
│   └── SshTerminal.razor          # SSH 终端
└── wwwroot/
    ├── noVncInterop.js            # noVNC JS 互操作
    └── xtermInterop.js            # xterm.js JS 互操作
```
