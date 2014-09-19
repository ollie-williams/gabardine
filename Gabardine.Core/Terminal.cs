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
using System.IO;
using System.Text;
using System.Xml;

namespace Gabardine
{
    public enum TerminalFormat
    {
        Black = 0,
        Blue,
        Green,
        Cyan,
        Red,
        Purple,
        Brown,
        Gray,
        DarkGray,
        LightBlue,
        LightGreen,
        LightCyan,
        LightRed,
        LightPurple,
        Yellow,
        White,
        Default,
    }

    public abstract class Terminal
    {
        Stack<TerminalFormat> formatStack = new Stack<TerminalFormat>();

        public abstract void Send(string str, params object[] args);

        public virtual void Send(char c)
        {
            Send(c.ToString());
        }

        public virtual void Send(object obj)
        {
            Send(obj.ToString());
        }

        public virtual void SendLine()
        {
            Send('\n');
        }

        public virtual void SendLine(object obj)
        {
            Send(obj);
            Send('\n');
        }

        public virtual void SendLine(string str, params object[] args)
        {
            if (args.Length > 0) {
                Send(string.Format(str, args));
            } else {
                Send(str);
            }
            Send('\n');
        }

        public virtual void PushFormat(TerminalFormat format)
        {
            SetFormat(format);
            formatStack.Push(format);
        }

        public virtual void PopFormat()
        {
            if (formatStack.Count > 0) {
                formatStack.Pop();
            }

            if (formatStack.Count == 0) {
                SetFormat(TerminalFormat.Default);
                return;
            }

            SetFormat(formatStack.Peek());
        }

        protected abstract void SetFormat(TerminalFormat format);

        static Terminal stdout = new ManagedConsole();
        static Terminal stderr = new ManagedConsole();

        public static Terminal Stdout
        {
            get { return stdout; }
            set { stdout = value; }
        }

        public static Terminal Stderr
        {
            get { return stderr; }
            set { stderr = value; }
        }

        public static void Write(string str, params object[] args)
        {
            Stdout.Send(str, args);
        }

        public static void Write(object obj)
        {
            Stdout.Send(obj);
        }

        public static void Write(char c)
        {
            Stdout.Send(c);
        }

        public static void WriteLine()
        {
            Stdout.SendLine();
        }

        public static void WriteLine(object obj)
        {
            Stdout.SendLine(obj);
        }

        public static void WriteLine(string str, params object[] args)
        {
            Stdout.SendLine(str, args);
        }
    }

    public class AnsiTerminal : Terminal
    {
        static readonly string[] formats = new string[] {
            "\u001B[0;30m",
            "\u001B[0;34m",
            "\u001B[0;32m",
            "\u001B[0;36m",
            "\u001B[0;31m",
            "\u001B[0;35m",
            "\u001B[0;33m",
            "\u001B[0;37m",
            "\u001B[1;30m",
            "\u001B[1;34m",
            "\u001B[1;32m",
            "\u001B[1;36m",
            "\u001B[1;31m",
            "\u001B[1;35m",
            "\u001B[1;33m",
            "\u001B[1;37m",
            "\u001B[0m",
        };

        readonly StringBuilder sb;

        public AnsiTerminal(StringBuilder sb)
        {
            this.sb = sb;
        }

        public StringBuilder Builder {  get { return sb; } }

        public override void Send(string str, params object[] args)
        {
            if (args.Length > 0) {
                sb.AppendFormat(str, args);
            } else {
                sb.Append(str);
            }
        }

        public override void Send(char c)
        {
            sb.Append(c);
        }

        protected override void SetFormat(TerminalFormat format)
        {
            sb.Append(formats[(int)format]);
        }
    }

    public class ManagedConsole : Terminal
    {
        static readonly ConsoleColor defaultColor = System.Console.ForegroundColor;

        static readonly ConsoleColor[] formats = new ConsoleColor[] {
            ConsoleColor.Black,
            ConsoleColor.DarkBlue,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkRed,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkYellow,
            ConsoleColor.Gray,
            ConsoleColor.DarkGray,
            ConsoleColor.Blue,
            ConsoleColor.Green,
            ConsoleColor.Cyan,
            ConsoleColor.Red,
            ConsoleColor.Magenta,
            ConsoleColor.Yellow,
            ConsoleColor.White
        };

        public override void Send(char c)
        {
            System.Console.Write(c);
        }

        public override void Send(string str, params object[] args)
        {
            System.Console.Write(str, args);
        }

        public override void SendLine(string str, params object[] args)
        {
            if (args.Length > 0) {
                Console.WriteLine(str, args);
            } else {
                Console.WriteLine(str);
            }
        }

        protected override void SetFormat(TerminalFormat format)
        {
            if (format == TerminalFormat.Default) {
                System.Console.ForegroundColor = defaultColor;
                return;
            }

            System.Console.ForegroundColor = formats[(int)format];
        }
    }

    public class HtmlTerminal : Terminal
    {
        readonly XmlElement root;
        XmlElement current;
        readonly XmlDocument xml;
        readonly StringBuilder sb = new StringBuilder();

        public HtmlTerminal(XmlDocument xml)
        {
            this.xml = xml;
            root = xml.CreateElement("p");
            current = xml.CreateElement("span");
            root.AppendChild(current);
        }

        public XmlElement Root { get { return root; } }

        static readonly string[] formats = new string[] {
            "Black",
            "DarkBlue",
            "DarkGreen",
            "DarkCyan",
            "DarkRed",
            "DarkMagenta",
            "DarkYellow",
            "Gray",
            "DarkGray",
            "Blue",
            "Green",
            "Cyan",
            "Red",
            "Magenta",
            "Yellow",
            "White"
        };

        public override void Send(string str, params object[] args)
        {
            sb.AppendFormat(str, args);
        }

        public void Flush()
        {
            current.InnerText = sb.ToString();
            sb.Clear();
        }

        protected override void SetFormat(TerminalFormat format)
        {
            throw new NotImplementedException();
        }

        public override void PushFormat(TerminalFormat format)
        {
            if (format == TerminalFormat.Default) {
                throw new NotImplementedException();
            }

            Flush();

            var parent = current;
            current = xml.CreateElement("span");
            parent.AppendChild(current);

            var style = xml.CreateAttribute("style");
            style.Value = string.Format("color:{0}", formats[(int)format]);
            current.Attributes.Append(style);
        }

        public override void PopFormat()
        {
            Flush();

            var parent = current.ParentNode;
            current = xml.CreateElement("span");
            parent.AppendChild(current);
        }
    }
}