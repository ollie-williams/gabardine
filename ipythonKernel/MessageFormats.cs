/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved.
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ZeroMQ;

namespace ipythonKernel
{
    enum ShellMessageType
    {
        kernel_info_request, kernel_info_reply,
        execute_request, execute_reply,
        status,
        pyin,
        stream,
        shutdown_request, shutdown_reply,
        connect_request, connect_reply,
        display_data,

        // Not handled
        inspect_request, inspect_reply,
        complete_request, complete_reply,
        history_request, history_reply,
        object_info_request, object_info_reply,
    };

    interface ITypedMessage
    {
        [JsonIgnore]
        ShellMessageType Type { get; }
    }

    struct ShellMessageHeader
    {
        public string msg_id { get; set; }
        public string username { get; set; }
        public string session { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ShellMessageType msg_type { get; set; }
    }
    
    class Stdout : ITypedMessage
    {        
        // The name of the stream is one of 'stdout', 'stderr'
        public string name { get { return "stdout"; } }
        
        // The data is an arbitrary string to be written to that stream
        public string data { get; set; }

        public ShellMessageType Type { get { return ShellMessageType.stream; } }
    }

    class Stderr : ITypedMessage
    {
        // The name of the stream is one of 'stdout', 'stderr'
        public string name { get { return "stderr"; } }

        // The data is an arbitrary string to be written to that stream
        public string data { get; set; }

        public ShellMessageType Type { get { return ShellMessageType.stream; } }
    }

    abstract class ShellMessage
    {
        protected readonly Shell shell;
        readonly string[] idents;
        readonly ShellMessageHeader header;
        readonly ShellMessageHeader parentHeader;

        protected ShellMessage(Shell shell, string[] idents, ShellMessageHeader header, ShellMessageHeader parentHeader)
        {
            this.shell = shell;
            this.idents = idents;
            this.header = header;
            this.parentHeader = parentHeader;
        }

        public static ShellMessage Dispatch(Shell shell, string[] message)
        {
            int offset = Array.IndexOf<string>(message, "<IDS|MSG>");
            var idents = message.Take(offset).ToArray();
            var hmac = message[++offset];
            //Console.WriteLine("Got message. Header = {0}", message[offset + 1]);
            var header = JsonConvert.DeserializeObject<ShellMessageHeader>(message[++offset]);
            var parentHeader = JsonConvert.DeserializeObject<ShellMessageHeader>(message[++offset]);
            string metadata = message[++offset];
            string content = message[++offset];

            switch (header.msg_type) {
                case ShellMessageType.kernel_info_request:
                    return new KernelInfoRequest(shell, idents, header, parentHeader);
                case ShellMessageType.execute_request:
                    return new ExecuteRequest(shell, idents, header, parentHeader, content);
                case ShellMessageType.shutdown_request:
                    return new ShutdownRequest(shell, idents, header, parentHeader);
                case ShellMessageType.connect_request:
                    return new ConnectRequest(shell, idents, header, parentHeader);
                default:
                    Console.WriteLine("No handler for {0}", header.msg_type);
                    return null;
            }
        }

        public abstract bool Handle();

        protected void SendMessage<T>(ZmqSocket socket, T content) where T : ITypedMessage
        {
            ShellMessageHeader outHeader = new ShellMessageHeader {
                msg_type = content.Type,
                msg_id = Guid.NewGuid().ToString(),
                session = header.session,
                username = header.username
            };

            foreach (string ident in idents) {
                shell.Send(socket, ident, false);
            }
            shell.Send(socket, "<IDS|MSG>", false);
            shell.Send(socket, "", false);
            shell.SendJSON(socket, outHeader, false);
            shell.SendJSON(socket, header, false);
            shell.Send(socket, "{}", false);
            shell.SendJSON(socket, content, true);
        }
    }
}