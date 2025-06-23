using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GUI_for_ATS
{
    public static class Helper
    {
        static readonly string PasswordHash = "=0m04F[d!=AHZW37?y3Ab1@z4"; //len 25
        static readonly string SaltKey = "A1n1Z#5c=?Vk:055|3kv+GXXj"; // len 25
        static readonly string VIKey = "t:dqRg1S}37YS#99"; // VIKey must be exactly 16 characters long.

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

                if (VIKey == null || VIKey.Length != 16)
                    throw new ArgumentException("VIKey must be exactly 16 characters long.");

                using (var keyDerivation = new Rfc2898DeriveBytes(
                    password: PasswordHash,
                    salt: Encoding.ASCII.GetBytes(SaltKey),
                    iterations: 100000))
                {
                    byte[] keyBytes = keyDerivation.GetBytes(32); // 256-bit key

                    using (var aes = new AesCryptoServiceProvider())
                    {
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.Key = keyBytes;
                        aes.IV = Encoding.ASCII.GetBytes(VIKey);

                        using (var memoryStream = new MemoryStream())
                        {
                            using (var encryptor = aes.CreateEncryptor())
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                return Convert.ToBase64String(memoryStream.ToArray());
                            }
                        }
                    }
                }
            }
            catch (CryptographicException ex)
            {
                System.Diagnostics.Trace.WriteLine($"Cryptographic error during encryption: {ex.Message}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Unexpected error during encryption: {ex.Message}");
                return string.Empty;
            }
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);

                if (VIKey == null || VIKey.Length != 16)
                    throw new ArgumentException("VIKey must be exactly 16 characters long.");

                using (var keyDerivation = new Rfc2898DeriveBytes(
                    password: PasswordHash,
                    salt: Encoding.ASCII.GetBytes(SaltKey),
                    iterations: 100000)) // Removed HashAlgorithmName parameter for .NET 4.8
                {
                    byte[] keyBytes = keyDerivation.GetBytes(32);

                    using (var aes = new AesCryptoServiceProvider()) // Changed to AesCryptoServiceProvider
                    {
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.Key = keyBytes;
                        aes.IV = Encoding.ASCII.GetBytes(VIKey);

                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        using (var decryptor = aes.CreateDecryptor())
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        using (var streamReader = new StreamReader(cryptoStream, Encoding.UTF8))
                        {
                            return streamReader.ReadToEnd(); // More efficient than CopyTo for strings
                        }
                    }
                }
            }
            catch (FormatException)
            {
                System.Diagnostics.Trace.WriteLine("Invalid Base64 input");
                return string.Empty;
            }
            catch (CryptographicException ex)
            {
                System.Diagnostics.Trace.WriteLine($"Decryption failed: {ex.Message}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Unexpected error: {ex.Message}");
                return string.Empty;
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000; // 32×32
                                                          // Use 0x000000001 for small icons (16×16) if needed.

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        //public static ImageSource? GetFileIcon(string filePath)
        //{
        //    try
        //    {
        //        SHFILEINFO shinfo = new();
        //        SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);

        //        if (shinfo.hIcon == IntPtr.Zero)
        //            return null;

        //        return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
        //            shinfo.hIcon,
        //            System.Windows.Int32Rect.Empty,
        //            BitmapSizeOptions.FromEmptyOptions());
        //    }
        //    catch
        //    {
        //        return Helper.CreatePlaceholderIcon();
        //    }
        //}


        public static ImageSource GetFileIcon(string filePath)
        {
            try
            {
                SHFILEINFO shinfo = new SHFILEINFO();

                IntPtr result = SHGetFileInfo(
                    filePath,
                    0,
                    ref shinfo,
                    (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
                    SHGFI_ICON | SHGFI_LARGEICON);

                if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
                    return Helper.CreatePlaceholderIcon();

                // Convert HICON → ImageSource
                ImageSource icon =
                    System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        shinfo.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                // Free native HICON handle
                DestroyIcon(shinfo.hIcon);

                return icon;
            }
            catch
            {
                return Helper.CreatePlaceholderIcon();
            }
        }
        public static ImageSource CreatePlaceholderIcon(int width = 64, int height = 64)
        {
            //DrawingVisual drawingVisual = new();
            //using (DrawingContext dc = drawingVisual.RenderOpen())
            //{
            //    dc.DrawRectangle(Brushes.LightGray, null, new Rect(0, 0, width, height));
            //}

            //RenderTargetBitmap bmp = new(width, height, 96, 96, PixelFormats.Pbgra32);
            //bmp.Render(drawingVisual);
            //return bmp;
            DrawingVisual drawingVisual = new DrawingVisual();

            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.LightGray, null, new Rect(0, 0, width, height));
            }

            RenderTargetBitmap bmp = new RenderTargetBitmap(
                width,
                height,
                96,               // dpiX
                96,               // dpiY
                PixelFormats.Pbgra32);

            bmp.Render(drawingVisual);
            return bmp;
        }
    }
}
