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

            var worker = new Thread(() => root = scanner.ScanDrive("C:"));
            worker.Start();

            while (worker.IsAlive)
            {
                Console.WriteLine("{0:F}% done", scanner.Progress);
                Thread.Sleep(100);
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

    }
}
