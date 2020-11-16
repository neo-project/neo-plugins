using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Neo.Plugins
{
    class CpuUsageMonitor
    {
        /// <summary>
        /// A dictionary that maps each thread ID with its respective Total Processor Time, in milliseconds
        /// </summary>
        private Dictionary<int, double> ThreadsProcessorTime;
        private Stopwatch Watch;

        /// <summary>
        /// Number of threads that were monitored
        /// </summary>
        public int ThreadCount { get => ThreadsProcessorTime.Count; }

        public CpuUsageMonitor()
        {
            ThreadsProcessorTime = new Dictionary<int, double>();
            Watch = Stopwatch.StartNew();
            // initialize processor time
            CheckAllThreads();
        }

        /// <summary>
        /// Checks the percentage of CPU usage of each thread of the current process.
        /// </summary>
        /// <param name="printEachThread">
        /// Specifies if the CPU usage information should be printed in the console.
        /// </param>
        /// <returns>
        /// Total CPU usage of the threads.
        /// </returns>
        public double GetCpuTotalProcessorTime(bool printEachThread = false)
        {
            var result = CheckAllThreads(printEachThread);
            return result.TotalUsage;
        }

        /// <summary>
        /// Checks the percentage of CPU usage of each thread of the current process.
        /// </summary>
        /// <param name="printEachThread">
        /// Specifies if the CPU usage information should be printed in the console.
        /// </param>
        /// <returns>
        /// An object with the total CPU usage and the CPU usage of each thread.
        /// </returns>
        public CpuUsage CheckAllThreads(bool printEachThread = false)
        {
            double total = 0;
            string result = "";
            var processName = Process.GetCurrentProcess().ProcessName;
            var lastProcessorTime = new Dictionary<int, double>();
            var threadUsage = new Dictionary<int, double>();

            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
            {
                var cpuUsage = CheckCPUThread(thread, out var newTime);
                lastProcessorTime.Add(thread.Id, newTime);
                threadUsage.Add(thread.Id, cpuUsage);

                result += $"{processName,10}/{thread.Id,-8}\t      CPU usage: {cpuUsage,8:0.00 %}\n";
                total += cpuUsage;
            }

            ThreadsProcessorTime = lastProcessorTime;
            Watch.Restart();

            if (printEachThread)
            {
                UpdateConsole(result);
            }

            return new CpuUsage(total, threadUsage);
        }

        public static void UpdateConsole(string str)
        {
            var visible = Console.CursorVisible;
            if (visible) Console.CursorVisible = false;

            var builder = new StringBuilder();
            builder.Append(' ', Console.WindowHeight * Console.WindowWidth);

            // Reset the cursor position to the top of the console and write out the string builder
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(builder);

            // Reset the cursor position to the top of the console and write out the string builder
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(str);

            if (visible) Console.CursorVisible = true;
        }

        /// <summary>
        /// Checks the percentage of CPU usage of the specified thread.
        /// </summary>
        /// <param name="thread">
        /// The thread to check the CPU usage.
        /// </param>
        /// <param name="newTime">
        /// When this method returns, contains the total processor time of the <code>thread</code>.
        /// or zero if any exception is thrown or <code>thread</code> is null. This parameter is
        /// passed uninitialized; any value originally supplied in result will be overwritten.
        /// </param>
        /// <returns>
        /// CPU usage percentage if it was possible to access the thread; otherwise, zero.
        /// </returns>
        private double CheckCPUThread(ProcessThread thread, out double newTime)
        {
            newTime = 0.0;
            try
            {
                newTime = thread.TotalProcessorTime.TotalMilliseconds;
                var oldTime = ThreadsProcessorTime.ContainsKey(thread.Id) ? ThreadsProcessorTime[thread.Id] : newTime;

                return (newTime - oldTime) / Watch.ElapsedMilliseconds;
            }
            catch
            {
                return 0;
            }
        }
    }
}
