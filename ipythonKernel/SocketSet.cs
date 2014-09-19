/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved.
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using ZeroMQ;

namespace ipythonKernel
{
    class SocketSet
    {
        ZmqSocket hbSocket;
        ZmqSocket shellSocket;
        ZmqSocket controlSocket;
        ZmqSocket stdinSocket;
        ZmqSocket iopubSocket;
        ZmqContext context;
        ConnectionInformation cnxInfo;

        public SocketSet(ZmqContext context, ConnectionInformation cnxInfo)
        {
            this.context = context;
            this.cnxInfo = cnxInfo;
        }

        public void Connect()
        {
            hbSocket = MakeSocket(SocketType.REP, cnxInfo.hb_port);
            shellSocket = MakeSocket(SocketType.ROUTER, cnxInfo.shell_port);
            controlSocket = MakeSocket(SocketType.ROUTER, cnxInfo.control_port);
            stdinSocket = MakeSocket(SocketType.ROUTER, cnxInfo.stdin_port);
            iopubSocket = MakeSocket(SocketType.PUB, cnxInfo.iopub_port);
        }

        public ZmqSocket HbSocket { get { return hbSocket; } }
        public ZmqSocket ShellSocket { get { return shellSocket; } }
        public ZmqSocket ControlSocket { get { return controlSocket; } }
        public ZmqSocket StdinSocket { get { return stdinSocket; } }
        public ZmqSocket IOPubSocket { get { return iopubSocket; } }

        public ConnectionInformation CnxInfo { get { return cnxInfo; } }

        ZmqSocket MakeSocket(SocketType type, int port)
        {
            var socket = context.CreateSocket(type);
            socket.Bind(string.Format("{0}://{1}:{2}", cnxInfo.transport, cnxInfo.ip, port));
            return socket;
        }
    }
}
