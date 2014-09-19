(*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*)
namespace Gabardine.Parser

open Gabardine
open Gabardine.Codegen
open Gabardine.Parser.Utils
open Gabardine.Parser.Print
open FParsec

type FunctionParser(expr, statement:Parser<unit,ParseState>) =
    
    let type_parser = TypeParser().typeParser .>> skip

    let identifier =
        let valid = noneOf " \t\n=";
        many1Chars valid .>> skip

    let makeParameter = 
        let decode state name pos = (addParameter state name pos).CreateTerm()
        pipe3 getUserState identifier getPosition decode

    let wrap (op:Operator) (t:Term) = Some (op.CreateTerm(t))

    let out_param = 
        str_ws "out" >>. expr |>> wrap OutputDirectives.OutParam

    let return_stmt = 
        str_ws "return " >>. expr |>> wrap OutputDirectives.ReturnValue

    let other = statement >>% None

    let body = 
        let stmt = out_param <|> return_stmt <|> other
        let prepend u v = 
            match v with
                | Some(t) -> Special.Cons.CreateTerm(u, t)
                | None -> u
        let lst = manyTill stmt (pchar '}') .>> skip
        let decode l = 
            List.fold prepend (Special.Nil.CreateTerm()) l
        lst |>> decode

    let prms state = 
        let makeParam name pos = 
            match lookup state name 0 with
                | None -> ()
                | Some(op) -> 
                    do printYellow (pos.ToString())
                    do printCyan (sprintf "warning: variable %A will mask operator with same name defined at %s" name (op.pos.ToString()))
            addParameter state name pos
        let prm = pipe2 identifier getPosition makeParam
        manyTill prm (pchar '{') .>> skip

    // Get name, declare parameters, and get their ordering
    let opening = identifier .>>. (getUserState >>= prms)

    // Emission
    let emit emitter rw (name, prmOrder) term =
        do Emission.EmitFunction(emitter, rw, name, prmOrder, term)
        name

    // Soup to nuts
    member x.Parser emitter rw = pipe2 opening body (emit emitter rw)
    