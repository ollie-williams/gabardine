/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved.
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using ZeroMQ;
using System.Threading.Tasks;
using System.Threading;
using System.Text;

namespace ipythonKernel
{
    struct ConnectionInformation
    {
        public int control_port { get; set; }
        public int hb_port { get; set; }
        public int iopub_port { get; set; }
        public string ip { get; set; }
        public int shell_port { get; set; }
        public int stdin_port { get; set; }
        public string transport { get; set; }
    }

    class Program
    {
        static ZmqSocket hbSocket;

        static void Main(string[] args)
        {
            // Process command line
            if (args.Length < 1) {
                throw new ArgumentException("Expected a connection file as argument.");
            }
            ConnectionInformation cnxInfo = ReadConnectionJson(args[0]);

            // Create sockets according to connection information
            ZmqContext zmqContext = ZmqContext.Create();
            SocketSet sockets = new SocketSet(zmqContext, cnxInfo);
            sockets.Connect();

            // Set terminal to iopub socket
            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();
            AttachTerminal(sockets.IOPubSocket, stdout, stderr);

            // Start heartbeat loop
            hbSocket = sockets.HbSocket;
            Task.Run(new Action(HeartbeatLoop));

            // Run shell loop
            Shell shell = new Shell(sockets, stdout, stderr);
            shell.Loop();
            Console.WriteLine("Bye bye.");
        }

        static void AttachTerminal(ZmqSocket iopubSocket, StringBuilder stdout, StringBuilder stderr)
        {
            Gabardine.Terminal.Stdout = new Gabardine.AnsiTerminal(stdout);
            Gabardine.Terminal.Stderr = new Gabardine.AnsiTerminal(stderr);
        }

        static void HeartbeatLoop()
        {
            Frame f = new Frame(1024);
            while (true) {
                // Receive the next frame
                hbSocket.ReceiveFrame(f);
                // Reply so that client knows kernel is running
                hbSocket.Send(f.Buffer);
            }
        }

        static ConnectionInformation ReadConnectionJson(string filename)
        {
            Console.WriteLine("Parsing JSON...");
            string contents = File.ReadAllText(filename);
            ConnectionInformation cnxInfo = JsonConvert.DeserializeObject<ConnectionInformation>(contents);
            return cnxInfo;
        }
        
    }
}
