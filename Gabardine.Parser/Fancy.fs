(*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*)
module public Fancy

open System.Xml
open Gabardine

let mutable html:System.IO.TextWriter = null

let pp = PrettyPrinter.InfixFormat

let printToElem (xml:XmlDocument) elemName s =
    let e = xml.CreateElement(elemName)
    e.InnerText <- s
    e


let htmlHistory start steps =
    
    let xml = new XmlDocument()

    let td = printToElem xml "td" 
    let th = printToElem xml "th" 

    let wrap elemName child =
        let e = xml.CreateElement(elemName)
        e.AppendChild(child) |> ignore
        e

    let cellsToRow cells =
        let elem = xml.CreateElement("tr")
        cells |> List.map elem.AppendChild |> ignore
        elem

    let header = List.map th ["Step"; "Term"; "Rule"] |> cellsToRow

    let start = List.map td ["0"; pp start; ""] |> cellsToRow

    let prettyHtml c t = 
        let terminal = new HtmlTerminal(xml)
        PrettyPrinter.PrintInfixFormat(t, terminal, c)
        terminal.Flush()
        terminal.Root

    let printRule (r:RewriteRule) = 
        let terminal = new HtmlTerminal(xml)
        if r.Name.Length > 0
        then
            terminal.PushFormat(TerminalFormat.Green)
            terminal.SendLine("{0}:", r.Name)
            terminal.PopFormat()
        PrettyPrinter.PrintInfixFormat(r.Composed(), terminal) 
        terminal.Flush();
        terminal.Root

    let createRow index (step:RewriteStep) =
        let root = step.Root
        let after = 
            step.Location.Replace(root, PrettyPrinter.Highlight.CreateTerm(step.After))
            |> prettyHtml TerminalFormat.LightBlue
        let transformed = step.Location.Replace(step.Root, step.After) |> pp
        let cells = [td ((index+1).ToString()); wrap "td" after; wrap "td" (printRule step.Rule)]
        cellsToRow cells

    let e = xml.CreateElement("table")
    e.AppendChild(header) |> ignore
    e.AppendChild(start) |> ignore
    let rows = Seq.mapi createRow steps
    rows |> Seq.iter (fun r -> e.AppendChild(r) |> ignore)
    
    xml.AppendChild(e) |> ignore
    do xml.WriteContentTo(new XmlTextWriter(html))


let rewriteVerbose (rw:RewriteSystem) t =
    let history = new RewriteHistory()
    let nf = rw.RewriteUnordered(t, history)
    if html = null
    then
        history.Steps |> Seq.iter (fun s -> s.Print(Terminal.Stdout, PrintFormat.Infix)) 
    else 
        htmlHistory t history.Steps
    nf

let prettyprint fmt term =
    match fmt with
        | Some(PrintFormat.Lisp) -> PrettyPrinter.LispFormat term |> Terminal.Stdout.SendLine
        | Some(PrintFormat.Tree) -> PrettyPrinter.PrintTree term |> Terminal.Stdout.SendLine
        | _ -> PrettyPrinter.InfixFormat term |> Terminal.Stdout.SendLine
