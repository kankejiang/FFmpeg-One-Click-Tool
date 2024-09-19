using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace TestDownload
{
    internal class Program
    {
        static private string url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
        static private string saveTo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg-master-latest-win64-gpl.zip");
        static private HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine($"开始下载 {url}");
            Console.WriteLine($"");
            Console.WriteLine($"下载速度过慢可以复制上面的链接到迅雷上下载，然后把压缩包复制到当前文件夹下。 ");
            Console.WriteLine($"");
            Console.WriteLine($"注意先关闭这个窗口再复制到当前文件夹下,然后重新启动会继续操作。 ");
            Console.WriteLine($"");
            if (File.Exists(saveTo))
            {
                Console.WriteLine("文件已存在，跳过下载。");
            }
            else
            {
                _ = startDownloadAsync().Result;
            }

            if (File.Exists(saveTo))
            {
                try
                {
                    ExtractZipFile(saveTo);
                    string extractedFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg-master-latest-win64-gpl");
                    string destinationPath = @"C:\ffmpeg-master-latest-win64-gpl";

                    if (Directory.Exists(extractedFolderPath))
                    {
                        CopyFolder(extractedFolderPath, destinationPath);
                        Console.WriteLine($"已将文件夹从 '{extractedFolderPath}' 复制到 '{destinationPath}'。");

                        bool envVarAdded = AddPathToEnvironmentVariable(@"C:\ffmpeg-master-latest-win64-gpl\bin");
                        if (envVarAdded)
                        {
                            Console.WriteLine("环境变量配置成功，已将 C:\\ffmpeg-master-latest-win64-gpl\\bin 添加到用户环境变量 PATH 中。");
                        }
                        else
                        {
                            Console.WriteLine("环境变量配置失败或路径已存在于环境变量中。");
                        }
                    }
                    else
                    {
                        Console.WriteLine("解压后的文件夹不存在，无法进行后续操作。");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"出现错误：{ex.Message}");
                }
            }

            Console.WriteLine("按回车键退出...");
            Console.ReadLine();
        }

        static private async Task<int> startDownloadAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            await Console.Out.WriteLineAsync($"[{stopwatch.ElapsedMilliseconds} 毫秒] 正在发送 HTTP 请求...");

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            await Console.Out.WriteLineAsync($"[{stopwatch.ElapsedMilliseconds} 毫秒] HTTP 响应已获取。");

            long? contentLen = response.Content.Headers.ContentLength;
            long totalLen = contentLen.HasValue ? contentLen.Value : -1;

            await Console.Out.WriteLineAsync($"[{stopwatch.ElapsedMilliseconds} 毫秒] 总下载长度为 {totalLen}。");

            await Console.Out.WriteAsync("是否继续下载？Y/N：");

            var k = Console.ReadKey();

            while (k.KeyChar != 'y' && k.KeyChar != 'Y')
            {
                return -1;
            }

            await Console.Out.WriteLineAsync();

            await Console.Out.WriteLineAsync($"[{stopwatch.ElapsedMilliseconds} 毫秒] 下载开始。");

            File.Delete(saveTo);
            using var downloadFile = File.Create(saveTo);

            await Console.Out.WriteLineAsync($"[{stopwatch.ElapsedMilliseconds} 毫秒] 文件已创建。");

            using (var download = await response.Content.ReadAsStreamAsync())
            {
                var buffer = new byte[81920];

                long totalBytesRead = 0;

                int bytesRead;

                while ((bytesRead = await download.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
                {
                    await downloadFile.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                    totalBytesRead += bytesRead;

                    int progressPercentage = (int)((double)totalBytesRead / totalLen * 100);
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"进度：[{new string('#', progressPercentage / 2)}{new string('-', 50 - progressPercentage / 2)}] {progressPercentage}%");
                }
            }

            await Console.Out.WriteLineAsync($"[{stopwatch.ElapsedMilliseconds} 毫秒] 文件下载完成。");

            stopwatch.Stop();

            return 0;
        }

        static void ExtractZipFile(string zipFilePath)
        {
            string extractPath = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(extractPath))
            {
                Directory.CreateDirectory(extractPath);
            }

            using (ICSharpCode.SharpZipLib.Zip.ZipFile zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(zipFilePath))
            {
                foreach (ZipEntry entry in zip)
                {
                    if (!entry.IsDirectory)
                    {
                        string entryFileName = entry.Name;
                        string fullExtractPath = Path.Combine(extractPath, entryFileName);
                        string directoryPath = Path.GetDirectoryName(fullExtractPath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        byte[] buffer = new byte[4096];
                        using (Stream zipStream = zip.GetInputStream(entry))
                        using (FileStream streamWriter = File.Create(fullExtractPath))
                        {
                            StreamUtils.Copy(zipStream, streamWriter, buffer);
                        }
                    }
                }
            }
            Console.WriteLine("文件解压完成。");
        }

        static void CopyFolder(string sourceFolder, string destinationFolder)
        {
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            foreach (string file in Directory.GetFiles(sourceFolder))
            {
                string dest = Path.Combine(destinationFolder, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceFolder))
            {
                string dest = Path.Combine(destinationFolder, Path.GetFileName(dir));
                CopyFolder(dir, dest);
            }
        }

        static bool AddPathToEnvironmentVariable(string pathToAdd)
        {
            string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            if (!currentPath.Contains(pathToAdd))
            {
                Environment.SetEnvironmentVariable("PATH", currentPath + ";" + pathToAdd, EnvironmentVariableTarget.User);
                return true;
            }
            return false;
        }
    }
}