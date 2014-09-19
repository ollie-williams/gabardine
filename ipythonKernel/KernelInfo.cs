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
    class KernelInfoReply : ITypedMessage
    {
        // Version of messaging protocol. The first integer indicates
        // major version. It is incremented when there is any backward
        // incompatible change. The second integer indicates minor
        // version. It is incremented when there is any backward
        // compatible change.
        public int[] protocol_version { get; set; }

        // The kernel implementation name (e.g. 'ipython' for the
        // IPython kernel)
        public string implementation { get; set; }

        // Implementation version number. The version number of the
        // kernel's implementation (e.g. IPython.__version__ for the
        // IPython kernel)
        public int[] implementation_version { get; set; }

        // Programming language in which kernel is implemented.
        // Kernel included in IPython returns 'python'.
        public string language { get; set; }

        // Language version number. It is Python version number (e.g.,
        // '2.7.3') for the kernel included in IPython.
        public int[] language_version { get; set; }

        // A banner of information about the kernel, which may be
        // desplayed in console environments.
        public string banner { get; set; }

        public ShellMessageType Type { get { return ShellMessageType.kernel_info_reply; } }
    }

    class KernelInfoRequest : ShellMessage
    {
        public KernelInfoRequest(Shell shell, string[] idents, ShellMessageHeader header, ShellMessageHeader parentHeader)
            : base(shell, idents, header, parentHeader)
        { }

        public override bool Handle()
        {
            KernelInfoReply reply = new KernelInfoReply {
                protocol_version = new int[] { 4, 0 },
                implementation = "gabardine",
                implementation_version = new int[] { 0, 1 },
                language = "C#",
                language_version = new int[] { 6, 0 },
                banner = "Welcome to the Gabardine iPython kernel."
            };

            SendMessage(shell.Sockets.ShellSocket, reply);
            return true;
        }
    }
}
