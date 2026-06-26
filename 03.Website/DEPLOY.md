# CJ.Plug 官网部署指南

## 项目概述

纯静态网站，无需后端服务、数据库或 Node.js 运行时。只需一个能托管静态文件的 Web 服务器即可运行。

## 目录结构

```
03.Website/
├── index.html              # 主页面（单页滚动设计）
├── css/
│   └── style.css           # 样式表
├── js/
│   └── main.js             # 交互脚本
├── images/                 # 截图/图片目录（替换为真实截图）
├── videos/                 # 演示视频目录
└── downloads/              # 下载包目录
    └── CJPlug-Trial-v2.0.0.zip
```

## 部署前准备

1. **替换截图**：将 `images/` 目录下的占位图替换为真实产品截图
2. **添加视频**：将演示视频放入 `videos/`，并在 `index.html` 中取消注释 `<video>` 标签并修改 `src`
3. **更新下载包**：替换 `downloads/` 目录下的 ZIP 文件为真实试用包
4. **修改链接**：将页面中的 `https://github.com/your-org/CJ.Plug` 替换为真实 GitHub 地址
5. **自定义颜色**：如需调整主题色，修改 `css/style.css` 顶部的 CSS 变量（`--color-primary` 等）

---

## 部署方案（任选其一）

### 方案一：Nginx（推荐，最通用）

适用于自有 VPS / 云服务器，已申请域名并完成 DNS 解析。

**1. 上传文件到服务器**
```bash
scp -r 03.Website/* user@your-server:/var/www/cjplug/
```

**2. 安装 Nginx**
```bash
# Ubuntu/Debian
sudo apt update && sudo apt install nginx -y

# CentOS/RHEL
sudo yum install nginx -y
```

**3. 配置站点**

创建配置文件 `/etc/nginx/sites-available/cjplug`：
```nginx
server {
    listen 80;
    server_name your-domain.com www.your-domain.com;

    root /var/www/cjplug;
    index index.html;

    # Gzip 压缩
    gzip on;
    gzip_types text/css application/javascript text/html image/svg+xml;
    gzip_min_length 256;

    # 静态资源缓存
    location ~* \.(css|js|jpg|jpeg|png|gif|svg|ico|woff2)$ {
        expires 30d;
        add_header Cache-Control "public, immutable";
    }

    # HTML 不缓存
    location / {
        try_files $uri $uri/ /index.html;
        add_header Cache-Control "no-cache";
    }

    # 视频文件
    location /videos/ {
        expires 7d;
        add_header Cache-Control "public";
    }
}
```

**4. 启用站点并配置 HTTPS**
```bash
sudo ln -s /etc/nginx/sites-available/cjplug /etc/nginx/sites-enabled/
sudo nginx -t                    # 测试配置
sudo systemctl reload nginx      # 重载

# 安装 Certbot 并获取免费 SSL 证书
sudo apt install certbot python3-certbot-nginx -y
sudo certbot --nginx -d your-domain.com -d www.your-domain.com
```

**5. 验证**
浏览器访问 `https://your-domain.com`，确认页面正常显示。

---

### 方案二：GitHub Pages（免费，零运维）

适合开源项目，域名通过 CNAME 指向 GitHub Pages。

**1. 创建仓库**
在 GitHub 新建仓库（如 `your-org/your-org.github.io` 或任意仓库名）。

**2. 推送代码**
```bash
cd 03.Website
git init
git add .
git commit -m "Initial website"
git remote add origin https://github.com/your-org/cjplug-website.git
git push -u origin main
```

**3. 启用 GitHub Pages**
进入仓库 `Settings > Pages`：
- Source: `Deploy from a branch`
- Branch: `main` / `/ (root)`
- 点击 Save

**4. 绑定自定义域名**
- 在 `Settings > Pages > Custom domain` 填入你的域名并 Save
- 勾选 `Enforce HTTPS`
- 在 DNS 服务商处添加记录：
  - 类型: `CNAME`
  - 主机记录: `@`（或 `www`）
  - 记录值: `your-org.github.io`

---

### 方案三：Cloudflare Pages（全球 CDN，免费）

**1. 推送代码到 GitHub / GitLab**

**2. 在 Cloudflare Pages 创建项目**
- 连接 Git 仓库
- Build settings（无需构建）：
  - Build command: 留空
  - Build output directory: `/`
- 点击 Deploy

**3. 绑定域名**
- 进入项目 `Custom domains`
- 添加你的域名，Cloudflare 会自动配置 DNS

---

### 方案四：腾讯云 COS + CDN / 阿里云 OSS

适合已有云账号的用户。

**1. 创建存储桶**
- 访问权限：公有读私有写
- 开启静态网站托管，索引文档设为 `index.html`

**2. 上传文件**
```bash
# 腾讯云 COSCMD
coscmd upload -r 03.Website/ / --delete

# 阿里云 OSSUTIL
ossutil cp -r 03.Website/ oss://your-bucket/ --update
```

**3. 绑定域名 + CDN**
- 在控制台绑定自定义域名
- 开启 CDN 加速（按需）
- 配置 SSL 证书（可在云平台免费申请）

---

## 本地预览

```bash
# Python 3
cd 03.Website
python -m http.server 8080
# 访问 http://localhost:8080

# 或使用 Nginx / Live Server 插件等
```

---

## DNS 解析设置（通用）

无论使用哪种方案，都需要在域名服务商处配置 DNS：

| 记录类型 | 主机记录 | 记录值 |
|---------|---------|--------|
| A | @ | 你的服务器 IP |
| A | www | 你的服务器 IP |
| CNAME | www | your-domain.com |

> 如果使用 GitHub Pages / Cloudflare Pages，将 A 记录替换为对应的 CNAME。

---

## 常见问题

**Q: 页面 CSS 样式不生效？**
A: 检查浏览器控制台是否有 404 错误，确保所有文件路径正确且已上传。

**Q: HTTPS 证书如何获取？**
A: 推荐使用 Let's Encrypt（免费），通过 Certbot 自动续期。云平台通常也提供免费 SSL。

**Q: 如何更新网站内容？**
A: 直接替换 `images/`、`videos/`、`downloads/` 下的文件，修改 `index.html` 内容后重新上传即可。
