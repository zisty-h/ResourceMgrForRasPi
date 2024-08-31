using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace ResourceMgrForRasPi
{
    internal class Program
    {
        private static long[] previousBytesSent;
        private static long[] previousBytesReceived;

        public static string CPUHistory;
        public static string MemHistory;
        public static string NetSHistory;
        public static string NetRHistory;

        static async Task Main()
        {
            Console.Clear();

            // 監視の間隔（ミリ秒）
            int interval = 1000;

            // 初期のネットワーク統計とディスクI/Oを取得
            previousBytesSent = GetNetworkBytesSent();
            previousBytesReceived = GetNetworkBytesReceived();

            Console.WriteLine("+──────────────────────────────────+");
            Console.WriteLine("│RMFR(Resource-Mgr-For-RaspberryPi)│");
            Console.WriteLine("+──────────────────────────────────+");
            Console.WriteLine("Now loading. Plese wait....(Initlazzing....)");
            


            while (true)
            {
                // 非同期で情報を取得
                var cpuUsageTask = GetCpuUsageAsync();
                var memoryUsageTask = Task.Run(() => GetMemoryUsage());
                var networkStatusTask = Task.Run(() => GetNetworkStatus());

                // 1秒待機
                var delayTask = Task.Delay(interval);

                await delayTask;

                // キー入力処理
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.E)
                    {
                        Console.Clear();
                        Console.Write("Exit?[y/n]> ");
                        if (Console.ReadLine()?.Trim().ToLower() == "y")
                        {
                            Console.Clear();
                            break;
                        }
                        Console.Clear();
                    }
                    else if (key == ConsoleKey.R)
                    {
                        CPUHistory = "";
                        MemHistory = "";
                        NetSHistory = "";
                        NetRHistory = "";
                        Console.Clear();
                    }
                }

                // コンソールのカーソル位置を初期化
                Console.SetCursorPosition(0, 3);

                // 取得した情報を表示
                var cpuUsage = await cpuUsageTask;
                var (totalMemory, usedMemory) = await memoryUsageTask;
                var (bytesSentDelta, bytesReceivedDelta) = await networkStatusTask;

                // CPU Usage
                Console.WriteLine($"[CPU]      ───────────────────────────────────────────────────────────────┐");
                DisplayCPUStatus(cpuUsage);

                // Memory Usage
                Console.WriteLine($"[Memory]   ───────────────────────────────────────────────────────────────┐");
                DisplayMemStatus(usedMemory, totalMemory);

                // Network Status
                Console.WriteLine($"[Network]  ───────────────────────────────────────────────────────────────┐");
                DisplayNetworkStatus(bytesSentDelta, bytesReceivedDelta);

                // 前回のネットワーク統計を更新
                previousBytesSent = GetNetworkBytesSent();
                previousBytesReceived = GetNetworkBytesReceived();
            }
        }

        static async Task<float> GetCpuUsageAsync()
        {
            try
            {
                var cpuUsageString = await RunCommandAsync("mpstat 1 1 | awk '/Average/ {print 100 - $12}'");
                if (float.TryParse(cpuUsageString.Trim(), out float cpuUsage))
                {
                    return cpuUsage;
                }
                else
                {
                    try
                    {
                        var cpuUsageStringJP = await RunCommandAsync("mpstat 1 1 | awk '/平均値/ {print 100 - $12}'");
                        if (float.TryParse(cpuUsageStringJP.Trim(), out float cpuUsageJP))
                        {
                            return cpuUsageJP;
                        }
                        else
                        {
                            Console.WriteLine("Error: Unable to parse CPU usage.");
                            return 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting CPU usage: {ex.Message}");
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting CPU usage: {ex.Message}");
                return 0;
            }
        }

        static async Task<string> RunCommandAsync(string command)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                using (var reader = process.StandardOutput)
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running command: {ex.Message}");
                return string.Empty;
            }
        }

        static async Task<(float totalMemory, float usedMemory)> GetMemoryUsage()
        {
            try
            {
                var memoryInfo = await RunCommandAsync("free -m");
                var lines = memoryInfo.Split('\n');
                var memoryLine = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // memoryLine[1] = total memory, memoryLine[2] = used memory
                var totalMemory = float.Parse(memoryLine[1]);
                var usedMemory = float.Parse(memoryLine[2]);

                return (totalMemory, usedMemory);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting memory usage: {ex.Message}");
                return (0, 0);
            }
        }


        static long[] GetNetworkBytesSent()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .Select(ni => ni.GetIPv4Statistics().BytesSent)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting network bytes sent: {ex.Message}");
                return new long[0];
            }
        }

        static long[] GetNetworkBytesReceived()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .Select(ni => ni.GetIPv4Statistics().BytesReceived)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting network bytes received: {ex.Message}");
                return new long[0];
            }
        }

        static async Task<(long bytesSentDelta, long bytesReceivedDelta)> GetNetworkStatus()
        {
            try
            {
                var currentBytesSent = GetNetworkBytesSent();
                var currentBytesReceived = GetNetworkBytesReceived();

                // 送信と受信の差分を計算
                long bytesSentDelta = currentBytesSent.Sum() - previousBytesSent.Sum();
                long bytesReceivedDelta = currentBytesReceived.Sum() - previousBytesReceived.Sum();

                return (bytesSentDelta, bytesReceivedDelta);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting network status: {ex.Message}");
                return (0, 0);
            }
        }
        static void DisplayCPUStatus(float cpuUsage)
        {
            Console.Write($"CPU Usage:\t");
            SetColor(cpuUsage);

            Console.Write($"▌{FormatPercentage(cpuUsage)}% ");
            Console.Write("\t");
            if (cpuUsage <= 20)
            {
                CPUHistory = "0" + CPUHistory;
            }
            else if (cpuUsage <= 70)
            {
                CPUHistory = "1" + CPUHistory;
            }
            else if (cpuUsage <= 90)
            {
                CPUHistory = "2" + CPUHistory;
            }
            else
            {
                CPUHistory = "3" + CPUHistory;
            }

            if (CPUHistory.Length > 32)
            {
                CPUHistory = CPUHistory.Substring(0, 33);
            }

            for (int i = 0; i < CPUHistory.Length; i++)
            {
                if (CPUHistory[i] == '0')
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (CPUHistory[i] == '1')
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else if (CPUHistory[i] == '2')
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                }
                Console.Write('▂');
            }
            Console.WriteLine();
            Console.ResetColor();
            Console.WriteLine();
        }

        static void DisplayMemStatus(float usedMemory,float totalMemory)
        {
            var memoryUsagePercent = (usedMemory / totalMemory) * 100;
            Console.Write($"Memory Usage:\t");
            SetColor(memoryUsagePercent);
            Console.WriteLine($"▌{FormatMemory(usedMemory)} MB / {FormatMemory(totalMemory)} MB  ");
            Console.Write($"\t\t▌{FormatPercentage(memoryUsagePercent)}%");

            Console.Write("\t\t");
            if (memoryUsagePercent <= 20)
            {
                MemHistory = "0" + MemHistory;
            }
            else if (memoryUsagePercent <= 70)
            {
                MemHistory = "1" + MemHistory;
            }
            else if (memoryUsagePercent <= 90)
            {
                MemHistory = "2" + MemHistory;
            }
            else
            {
                MemHistory = "3" + MemHistory;
            }

            if (MemHistory.Length > 32)
            {
                MemHistory = MemHistory.Substring(0, 33);
            }

            for (int i = 0; i < MemHistory.Length; i++)
            {
                if (MemHistory[i] == '0')
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (MemHistory[i] == '1')
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else if (MemHistory[i] == '2')
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                }
                Console.Write('▂');
            }
            Console.WriteLine();
            Console.ResetColor();
            Console.WriteLine();
        }

        static void DisplayNetworkStatus(long bytesSentDelta, long bytesReceivedDelta)
        {
            // 送信状態を表示
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("Sending:\t");
            if (bytesSentDelta > 0) 
            {
                NetSHistory = "1" + NetSHistory;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("▌Active    ");
            }
            else
            {
                NetSHistory = "0" + NetSHistory;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("▌Not Active");
            }
            Console.Write("\t");
            if (NetSHistory.Length > 32)
            {
                NetSHistory = NetSHistory.Substring(0, 33);
            }
            for (int i = 0; i < NetSHistory.Length; i++)
            {
                if (NetSHistory[i] == '1')
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else 
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.Write('▂');
            }
            Console.WriteLine();
            Console.ResetColor();


            // 受信状態を表示
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("Receiving:\t");
            if (bytesReceivedDelta > 0)
            {
                NetRHistory = "1" + NetRHistory;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("▌Active    ");
            }
            else
            {
                NetRHistory = "0" + NetRHistory;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("▌Not Active");
            }
            Console.Write("\t");
            if (NetRHistory.Length > 32)
            {
                NetRHistory = NetRHistory.Substring(0, 33);
            }
            for (int i = 0; i < NetRHistory.Length; i++)
            {
                if (NetRHistory[i] == '1')
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.Write('▂');
            }
            Console.WriteLine();
            Console.ResetColor();
        }

        static void SetColor(float Percent)
        {
            if (Percent <= 20)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if (Percent <= 70)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else if (Percent <= 90)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Red;
            }
        }

        

        static string FormatMemory(float memory) => $"{memory:00.00}";
        static string FormatPercentage(float percentage) => $"{percentage:00.00}";
    }
}