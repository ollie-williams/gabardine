/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved.
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ZeroMQ;

namespace ipythonKernel
{
    class Shell
    {
        readonly Frame f;
        readonly SocketSet sockets;
        readonly Engine engine;
        readonly StringBuilder stdout;
        readonly StringBuilder stderr;

        public Shell(SocketSet sockets, StringBuilder stdout, StringBuilder stderr)
        {
            this.f = new Frame(1024);
            this.sockets = sockets;
            this.engine = new Engine();
            this.stdout = stdout;
            this.stderr = stderr;
        }

        public SocketSet Sockets { get { return sockets; } }
        public Engine Engine {  get { return engine; } }
        public StringBuilder StdOut { get { return stdout; } }
        public StringBuilder StdErr{ get { return stderr; } }

        public void Loop()
        {
            bool alive = true;
            while (alive) {
                var parts = GetAllMessageParts().ToArray();
                ShellMessage message = ShellMessage.Dispatch(this, parts);
                if (message != null) {
                    alive = message.Handle();
                }
            }
        }

        public void Send(ZmqSocket socket, string content, bool endOfMessage)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            socket.Send(bytes, bytes.Length, endOfMessage ? SocketFlags.None : SocketFlags.SendMore);
        }

        public void SendJSON<T>(ZmqSocket socket, T content, bool endOfMessage)
        {
            string str = JsonConvert.SerializeObject(content);
            Send(socket, str, endOfMessage);
        }

        IEnumerable<string> GetAllMessageParts()
        {
            sockets.ShellSocket.ReceiveFrame(f);
            yield return DecodeFrame(f);
            while (f.HasMore) {
                sockets.ShellSocket.ReceiveFrame(f);
                yield return DecodeFrame(f);
            }
        }

        static string DecodeFrame(Frame f)
        {
            return Encoding.UTF8.GetString(f.Buffer, 0, f.MessageSize);
        }
    }


}
