/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved.
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System.Text;
using Gabardine;
using Gabardine.Parser;

namespace ipythonKernel
{
    class Engine
    {
        readonly Context context;
        readonly StringBuilder htmlBuilder;
        readonly ScriptParser parser;
        int executionCounter = 0;

        public Engine()
        {
            this.context = new Context();
            this.parser = new ScriptParser(context);
            htmlBuilder = new StringBuilder();
            var sw = new System.IO.StringWriter(htmlBuilder);
            Fancy.html = sw;
        }

        public int ExecutionCounter
        {
            get { return executionCounter; }
            set { executionCounter = value; }
        }

        public StringBuilder HTML
        {
            get { return htmlBuilder; }
        }

        public void Execute(string code)
        {
            try {
                parser.Parse(code);
            } catch(System.Exception e) {
                Terminal.Stderr.PushFormat(TerminalFormat.LightRed);
                Terminal.Stderr.SendLine("Gabardine threw an exception");
                Terminal.Stderr.PopFormat();
                Terminal.Stderr.SendLine(e.ToString());
            }
        }

    }
}
