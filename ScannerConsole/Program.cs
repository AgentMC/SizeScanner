using System;
using System.Threading;
using ScannerCore;

namespace ScannerConsole
{
    class Program
    {
        static void Main()
        {
            var scanner = new DriveScanner();
            FsItem root;

            var worker = new Thread(() => root = scanner.ScanDirectory("Z:\\Backup\\Mike"));
            var s = System.Diagnostics.Stopwatch.StartNew();
            worker.Start();

            while (worker.IsAlive)
            {
                Console.WriteLine($"Current: {scanner.CurrentScanned}");
                Thread.Sleep(100);
            }
            s.Stop();
            Console.WriteLine($"Elapsed: {s.ElapsedMilliseconds / 1000.0} seconds.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

    }
}
