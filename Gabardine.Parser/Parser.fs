(*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*)
module Gabardine.Parser.Utils

open Gabardine
open FParsec
open System.Collections.Generic
open Gabardine.Parser.Print

/// Skip whitespace and comments
let skip<'a> : Parser<unit,'a> = 
    let comment = (pstring "/*") >>. skipCharsTillString "*/" true System.Int32.MaxValue
    let lineComment = (pstring "//") >>. skipRestOfLine true
    let loudComment = 
        (pstring "/!/") >>. spaces >>. restOfLine true >>= posPrint 
    skipMany (spaces1 <|> comment <|> lineComment <|> loudComment)

/// Parse a string and skip trailing whitespace
let str_ws s = pstring s >>. skip

let bracketed p = between (str_ws "(") (str_ws ")") p

type ParseState = { 
    scope : Namespace list }

let addOperator arity kind state (name:string) pos =
    let head = List.head state.scope
    let op = new Operator(arity, kind, new OperatorSyntax(name))
    do head.add(op, pos)
    op

let addParameter = addOperator 0 OperatorKind.Parameter
let addVariable = addOperator 0 OperatorKind.PatternVariable

let paddOp adder = getPosition |>> adder
let paddNull kind state name = paddOp (addOperator 0 kind state name)
    
let lookup state name arity =
    let nsLookup (ns:Namespace) = ns.lookup name arity
    List.tryPick nsLookup state.scope

let makeNullary (op:Operator) = op.CreateTerm()

let pushScope s = {s with scope = Namespace()::s.scope}
let popScope s = {s with scope = List.tail s.scope}

let push = updateUserState pushScope
let pop = updateUserState popScope
