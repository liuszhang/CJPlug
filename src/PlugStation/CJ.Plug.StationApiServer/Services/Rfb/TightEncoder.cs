using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CJ.Plug.StationApiServer.Services.Rfb
{
    /// <summary>
    /// Tight 编码器：将 32bpp BGRX 帧缓冲的脏矩形编码为 RFB Tight JPEG 子编码数据。
    /// 规范：Tight 控制字节高 4 位 = 子编码（3 = JPEG），JPEG 数据 3 字节大端长度 + 对齐填充
    /// （32bpp → 16 字节对齐）。
    /// </summary>
    public static class TightEncoder
    {
        public const int EncodingRaw = 0;
        public const int EncodingTight = 7;

        /// <summary>
        /// 将帧缓冲中指定矩形区域编码为 Tight JPEG 子编码字节。
        /// </summary>
        /// <param name="frame">32bpp BGRX 帧缓冲（与 BitmapData 一致的 Stride 可能大于 Width*4）</param>
        /// <param name="stride">帧缓冲行字节数</param>
        /// <param name="x">矩形起点（画布坐标）</param>
        /// <param name="y">矩形起点（画布坐标）</param>
        /// <param name="w">矩形宽</param>
        /// <param name="h">矩形高</param>
        /// <param name="jpegQuality">JPEG 质量 1~100</param>
        /// <returns>完整的 rect 编码数据（含控制字节），失败返回 null</returns>
        public static byte[]? EncodeTightJpeg(byte[] frame, int stride, int x, int y, int w, int h, int jpegQuality = 75)
        {
            if (frame == null || w <= 0 || h <= 0 || x < 0 || y < 0)
                return null;

            try
            {
                // 从帧缓冲抠出矩形区域，构造 32bpp 位图
                using var bmp = new Bitmap(w, h, PixelFormat.Format32bppRgb);
                var bmpData = bmp.LockBits(new Rectangle(0, 0, w, h),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                try
                {
                    int dstStride = bmpData.Stride;
                    byte[] rowBuffer = new byte[dstStride];
                    for (int row = 0; row < h; row++)
                    {
                        int srcOffset = (y + row) * stride + x * 4;
                        Buffer.BlockCopy(frame, srcOffset, rowBuffer, 0, w * 4);
                        Marshal.Copy(rowBuffer, 0, bmpData.Scan0 + row * dstStride, w * 4);
                    }
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }

                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Jpeg);
                byte[] jpeg = ms.ToArray();
                if (jpeg.Length <= 0)
                    return null;

                // Tight JPEG 子编码（noVNC 0.6 解码规则）：
                //  控制字节高 4 位 = 子编码 9（JPEG）→ 0x90；低 4 位 = zlib 流重置标志（无）
                //  长度 = 变长编码（每字节 7 位数据 + 最高位续长，最多 3 字节）
                //  JPEG 数据直接跟随，无需对齐填充
                var lengthBytes = EncodeCompactLength(jpeg.Length);
                byte[] result = new byte[1 + lengthBytes.Length + jpeg.Length];
                result[0] = 0x90;
                Buffer.BlockCopy(lengthBytes, 0, result, 1, lengthBytes.Length);
                Buffer.BlockCopy(jpeg, 0, result, 1 + lengthBytes.Length, jpeg.Length);

                return result;
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "Tight JPEG 编码失败");
                return null;
            }
        }

        /// <summary>
        /// 原始编码（Raw，兜底/小矩形用）。
        /// </summary>
        public static byte[] EncodeRaw(byte[] frame, int stride, int x, int y, int w, int h)
        {
            byte[] result = new byte[w * h * 4];
            for (int row = 0; row < h; row++)
            {
                int src = (y + row) * stride + x * 4;
                Buffer.BlockCopy(frame, src, result, row * w * 4, w * 4);
            }
            return result;
        }

        /// <summary>
        /// Tight 变长长度编码：每字节 7 位数据 + 最高位表示"还有后续字节"（最多 3 字节）。
        /// 与 noVNC 0.6 _readData 的解析逻辑一一对应。
        /// </summary>
        public static byte[] EncodeCompactLength(int length)
        {
            if (length < 0) length = 0;
            if (length < 128)
                return new byte[] { (byte)length };
            if (length < 16384)
                return new byte[] { (byte)((length & 0x7F) | 0x80), (byte)(length >> 7) };
            return new byte[]
            {
                (byte)((length & 0x7F) | 0x80),
                (byte)(((length >> 7) & 0x7F) | 0x80),
                (byte)(length >> 14)
            };
        }
    }
}
