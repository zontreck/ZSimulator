using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Bot
{
    class Waiter
    {
        public static void Main(string[] args)
        {
            Process[] proc = Process.GetProcessesByName("Simulator");
            if (proc.Length == 0)
            {
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("Joined Simulator.dll process. Will wait until terminated");
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    proc = Process.GetProcessesByName("Simulator");
                    if (proc.Length == 0)
                    {
                        break;
                    }
                }
                Console.WriteLine("Simulator.dll is no longer running");
            }

            Environment.Exit(0);
        }
    }
}
