using System.Diagnostics;
using System.Reflection;

namespace RMFR_Installer
{
    internal class Program
    {

        static void Main()
        {
            Console.Clear();
            Console.WriteLine("+─────────────────────────────────────────────+");
            Console.WriteLine("│RMFR(Resource-Mgr-For-RaspberryPi) Installer│");
            Console.WriteLine("+─────────────────────────────────────────────+");
            Console.WriteLine("Copyright © 2024 Syobosyoboonn(Zisty) All Rights Reserved.");
            Console.WriteLine();

            if (!IsRunningAsRoot())
            {
                Console.WriteLine("Plese run as root.");
                Environment.Exit(0);
            }

            Console.WriteLine("Plese enter install folder.");
            Console.Write("[Press Enter(/usr/bin)/Type Any Path]>");
            string pathInp = Console.ReadLine();
            Console.WriteLine();


            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                var resourceName = "RMFR_Installer.Resources.rmfr";

                List<string> resourceNames = new List<string>(assembly.GetManifestResourceNames());

                string outputFilePath = Path.Combine(pathInp, "rmfr");
                if (pathInp == "" || pathInp == null)
                {
                    outputFilePath = Path.Combine("/usr/bin", "rmfr");
                }

                using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                    {
                        Console.WriteLine("Resource not found.");
                        return;
                    }

                    // ファイルとして書き込む
                    using (FileStream fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }
                Process chmodProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = "777 " + outputFilePath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
            }
            catch(Exception ex) 
            {
                Console.WriteLine("Error:" + ex.ToString()); 
                Environment.Exit(0);
            }

            Console.WriteLine() ;
            Console.WriteLine("Done!");
            Console.WriteLine("Try it:\trmfr");
        }

        static bool IsRunningAsRoot()
        {
            try
            {
                string statusFilePath = "/proc/self/status";
                if (File.Exists(statusFilePath))
                {
                    foreach (var line in File.ReadLines(statusFilePath))
                    {
                        if (line.StartsWith("Uid:"))
                        {
                            var uid = line.Split('\t')[1].Trim();
                            return uid == "0";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while checking root status: {ex.Message}");
            }

            return false;
        }
    }
}

