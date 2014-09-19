/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved.
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ipythonKernel
{
    class ExecuteRequestInfo
    {
        // Source code to be executed by the kernel, one or more
        // lines.
        public string code { get; set; }

        // A boolean flag which, if True, signals the kernel to
        // execute this code as quietly as possible. silent=True
        // forces store_history to be False, and will *not*:       
        //   - broadcast output on the IOPUB channel
        //   - have an execute_result
        // The default is False.
        public bool silent { get; set; }

        // A boolean flag which, if True, signals the kernel to
        // populate history The default is True if silent is False.
        // If silent is True, store_history is forced to be False.
        public bool store_history { get; set; }

        // A dict mapping names to expressions to be evaluated in the
        // user's dict. The rich display-data representation of each
        // will be evaluated after execution. See the display_data
        // content for the structure of the representation data.
        public Dictionary<string, object> user_expressions { get; set; }

        // Some frontends do not support stdin requests. If raw_input
        // is called from code executed from such a frontend, a
        // StdinNotImplementedError will be raised.
        public bool allow_stdin { get; set; }
    }

    struct Payload
    {
        public string html { get; set; }
        public string source { get; set; }
        public int start_line_number { get; set; }
        public string text { get; set; }
    }

    enum ExecuteStatus { ok, error, abort };

    struct ExecuteReply : ITypedMessage
    {
        // One of: 'ok' OR 'error' OR 'abort'
        [JsonConverter(typeof(StringEnumConverter))]
        public ExecuteStatus status { get; set; }

        // The global kernel counter that increases by one with each
        // request that stores history. This will typically be used by
        // clients to display prompt numbers to the user. If the
        // request did not store history, this will be the current
        // value of the counter in the kernel.
        public int execution_count { get; set; }

        // 'payload' will be a list of payload dicts.  Each execution
        // payload is a dict with string keys that may have been
        // produced by the code being executed.  It is retrieved by
        // the kernel at the end of the execution and sent back to the
        // front end, which can take action on it as needed.  The only
        // requirement of each payload dict is that it have a 'source'
        // key, which is a string classifying the payload
        // (e.g. 'pager').
        public List<Payload> payload { get; set; }

        // Results for the user_expressions.
        public Dictionary<string, object> user_expressions { get; set; }

        public ShellMessageType Type { get { return ShellMessageType.execute_reply; } }
    }

    enum ExecutionState { busy, idle, starting };

    struct KernelStatus : ITypedMessage
    {
        // When the kernel starts to handle a message, it will enter
        // the 'busy' state and when it finishes, it will enter the
        // 'idle' state. The kernel will publish state 'starting'
        // exactly once at process startup.
        [JsonConverter(typeof(StringEnumConverter))]
        public ExecutionState execution_state { get; set; }

        public ShellMessageType Type { get { return ShellMessageType.status; } }
    }

    struct Pyin : ITypedMessage
    {
        // Source code to be executed, one or more lines
        public string code { get; set; }

        // The counter for this execution is also provided so that
        // clients can display it, since IPython automatically creates
        // variables called _iN (for input prompt In[N]).
        public int execution_count { get; set; }

        public ShellMessageType Type { get { return ShellMessageType.pyin; } }
    }


    class ExecuteRequest : ShellMessage
    {
        readonly ExecuteRequestInfo request;

        public ExecuteRequest(Shell shell, string[] idents, ShellMessageHeader header, ShellMessageHeader parentHeader, string content)
            : base(shell, idents, header, parentHeader)
        {
            request = JsonConvert.DeserializeObject<ExecuteRequestInfo>(content);
        }

        public override bool Handle()
        {
            // Tell client we're starting work
            PostState(ExecutionState.busy);
            Pyin pyin = new Pyin
            {
                code = request.code,
                execution_count = shell.Engine.ExecutionCounter++
            };
            SendMessage(shell.Sockets.IOPubSocket, pyin);

            //Console.WriteLine("I was asked to execute:\n {0}", request.code);
            shell.Engine.Execute(request.code);
            SendStderr();
            SendStdout();
            SendHTML();
            
            // Respond
            ExecuteReply reply = new ExecuteReply
            {
                status = ExecuteStatus.ok,
                execution_count = shell.Engine.ExecutionCounter++,
                payload = new List<Payload>(),
                user_expressions = new Dictionary<string, object>()
            };

            SendMessage(shell.Sockets.ShellSocket, reply);
            PostState(ExecutionState.idle);
            return true;
        }

        void PostState(ExecutionState state)
        {
            KernelStatus status = new KernelStatus { execution_state = state };
            SendMessage(shell.Sockets.IOPubSocket, status);
        }

        void SendStdout()
        {
            if (shell.StdOut.Length == 0) {
                return;
            }
            Stdout stdout = new Stdout { data = shell.StdOut.ToString() };
            SendMessage(shell.Sockets.IOPubSocket, stdout);
            shell.StdOut.Clear();
        }

        void SendStderr()
        {
            if (shell.StdErr.Length == 0) {
                return;
            }
            Stderr stderr = new Stderr { data = shell.StdErr.ToString() };
            SendMessage(shell.Sockets.IOPubSocket, stderr);
            shell.StdErr.Clear();
        }

        void SendHTML()
        {
            if (shell.Engine.HTML.Length == 0) {
                return;
            }

            DisplayData dd = new DisplayData();

            dd.source = "Gabardine";
            dd.metadata = new Dictionary<string, object>();
            dd.data = new Dictionary<string, object>();
            string html = shell.Engine.HTML.ToString();
            dd.data.Add("text/html", html);
            shell.Engine.HTML.Clear();

            SendMessage(shell.Sockets.IOPubSocket, dd);
        }
    }

}
