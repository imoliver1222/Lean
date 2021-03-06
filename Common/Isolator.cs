﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Logging;

namespace QuantConnect 
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Isolator class - create a new instance of the algorithm and ensure it doesn't 
    /// exceed memory or time execution limits.
    /// </summary>
    public class Isolator
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        /// <summary>
        /// Algo cancellation controls - cancel source.
        /// </summary>
        public static CancellationTokenSource cancellation = new CancellationTokenSource();

        /// <summary>
        /// Algo cancellation controls - cancellation token for algorithm thread.
        /// </summary>
        public static CancellationToken cancelToken = new CancellationToken();


        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Check if this task isolator is cancelled, and exit the analysis
        /// </summary>
        public static bool IsCancellationRequested
        {
            get 
            {
                return cancelToken.IsCancellationRequested;
            }
        }


        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Reset the cancellation token variables for a new task:
        /// </summary>
        public static void ResetCancelToken() 
        {
            cancellation = new CancellationTokenSource();
            cancelToken = cancellation.Token;
        }


        /// <summary>
        /// Execute a code block with a maximum limit on time and memory.
        /// </summary>
        /// <param name="timeSpan">Timeout in timespan</param>
        /// <param name="codeBlock">Action codeblock to execute</param>
        /// <param name="memoryCap">Maximum memory allocation, default 1024Mb</param>
        /// <returns>True if algorithm exited successfully, false if cancelled because it exceeded limits.</returns>
        public static bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock, long memoryCap = 1024)
        {
            var message = "";
            var dtEnd = DateTime.Now + timeSpan;

            //Convert to bytes
            memoryCap *= 1024 * 1024;

            ResetCancelToken();

            //Thread:
            var task = Task.Factory.StartNew(codeBlock, cancelToken);            

            while (!task.IsCompleted && DateTime.Now < dtEnd)
            {
                if (GC.GetTotalMemory(false) > memoryCap)
                {
                    if (GC.GetTotalMemory(true) > memoryCap)
                    {
                        message = "Execution Security Error: Memory Usage Maxed Out - " + Math.Round(Convert.ToDouble(memoryCap / (1024 * 1024))) + "MB max.";
                        break;
                    }
                }
                Thread.Sleep(1000);
            }

            if (task.IsCompleted == false && message == "")
            {
                message = "Execution Security Error: Operation timed out - " + timeSpan.TotalMinutes + " minutes max. Check for recursive loops.";
                Console.WriteLine("Isolator.ExecuteWithTimeLimit(): " + message);
            }

            if (message != "")
            {
                cancellation.Cancel();
                Log.Error("Security.ExecuteWithTimeLimit(): " + message);
                throw new Exception(message);
            }
            return task.IsCompleted;
        }

    }
}
