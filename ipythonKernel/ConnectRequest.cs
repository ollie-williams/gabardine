/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved.
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;

namespace ipythonKernel
{

    struct ConnectReply : ITypedMessage
    {
        /// <summary>
        /// The port the shell ROUTER socket is listening on.
        /// </summary>
        public int shell_port { get; set; }

        /// <summary>
        /// The port the PUB socket is listening on.
        /// </summary>
        public int iopub_port { get; set; }

        /// <summary>
        /// The port the stdin ROUTER socket is listening on.
        /// </summary>
        public int stdin_port { get; set; }

        /// <summary>
        /// The port the heartbeat socket is listening on.
        /// </summary>
        public int hb_port { get; set; }

        public ShellMessageType Type {  get { return ShellMessageType.connect_reply; } }
    }

    class ConnectRequest : ShellMessage
    {
        public ConnectRequest(Shell shell, string[] idents, ShellMessageHeader header, ShellMessageHeader parentHeader)
            : base(shell, idents, header, parentHeader)
        { }

        public override bool Handle()
        {
            ConnectReply reply = new ConnectReply {
                shell_port = shell.Sockets.CnxInfo.shell_port,
                iopub_port = shell.Sockets.CnxInfo.iopub_port,
                stdin_port = shell.Sockets.CnxInfo.stdin_port,
                hb_port = shell.Sockets.CnxInfo.hb_port
            };
            SendMessage(shell.Sockets.ShellSocket, reply);
            return true;
        }
    }
}