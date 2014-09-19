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
open FParsec

type CodegenParser(expr) =
    
    let identifier =
        let valid = noneOf " \t\n=(),";
        many1Chars valid .>> skip
    let tmpLabel = pchar '%' >>. identifier .>> skip
    let tyParser = (TypeParser<ParseState>().typeParser .>> skip) <??> "a type"

    // Arguments
    let number = 
        let format =     NumberLiteralOptions.AllowMinusSign
                     ||| NumberLiteralOptions.AllowFraction
                     ||| NumberLiteralOptions.AllowExponent
        let convert (nl:NumberLiteral) = 
            if nl.IsInteger then (Term.Const(int nl.String), LowLevelType.Integer(32,true))
            else (Term.Const(float nl.String), LowLevelType.Float64)
        let decode ty nl =
            let (num, defaultType) = convert nl
            match ty with
                | Some(value) -> Arguments.Literal.CreateTerm(num, Term.Const(value))
                | None -> Arguments.Literal.CreateTerm(num, Term.Const(defaultType))
        pipe2 (opt tyParser) (numberLiteral format "number") decode .>> skip

    let tmpRef = 
        let wrap label = Arguments.Tmp.CreateTerm(Term.Const(label))
        tmpLabel |>> wrap

    let argument =
        let ch = tmpRef <|> number
        ch .>> skip

    // Type inference
    let staticType =
        let decode ty = TypeInference.Static.CreateTerm(Term.Const(ty))
        tyParser |>> decode

    let typeof =
        let decode (arg:Term) = TypeInference.Typeof.CreateTerm(arg)
        str_ws "typeof" >>. argument |>> decode

    let typeDec = staticType <|> typeof
   
    // Instructions
    let generate = 
        let parser = str_ws "generate" >>. expr
        let wrapper (t:Term) = Instructions.Generate.CreateTerm(t)
        parser |>> wrapper

    let binary =
        let add = str_ws "add" >>% BinaryOp.Add 
        let sub = str_ws "sub" >>% BinaryOp.Sub
        let mul = str_ws "mul" >>% BinaryOp.Mul
        let div = str_ws "div" >>% BinaryOp.Div
        let lt = str_ws "lt" >>% BinaryOp.LT
        let gt = str_ws "gt" >>% BinaryOp.GT
        let op = choice [add; sub; mul; div; lt; gt] .>> skip
        let decode o u v = Instructions.Binary.CreateTerm(Term.Const(o), u, v)
        pipe3 op argument argument decode

    let offset = 
        let decode bse off = Instructions.Offset.CreateTerm(bse, off)
        str_ws "offset" >>. pipe2 argument argument decode

    let alloc =
        let decode ty (size:Term) = Instructions.Alloc.CreateTerm(ty, size)
        str_ws "alloc" >>. pipe2 typeDec argument decode

    let cast =
        let decode ty arg = Instructions.Cast.CreateTerm(arg, Term.Const(ty))
        str_ws "cast" >>. pipe2 tyParser argument decode

    let call (op:Operator) = 
        let decode name (args:seq<Term>) = op.CreateTerm(Term.Const(name), ConsUtils.Cons(args))
        let args = bracketed (sepBy argument (pchar ',' .>> skip))
        str_ws "call" >>. pipe2 identifier args decode

    let local = 
        let decode (ty:Term) =  Instructions.Local.CreateTerm(ty)
        str_ws "local" >>. typeDec |>> decode

    let load = 
        let decode (arg:Term) = Instructions.Load.CreateTerm(arg)
        str_ws "load" >>. argument |>> decode

    let makeStruct =
        let decode (args:seq<Term>) = Instructions.Struct.CreateTerm(ConsUtils.Cons(args))
        str_ws "struct" >>. many argument |>> decode

    let field = 
        let decode ind arg = Instructions.Field.CreateTerm(Term.Const(ind), arg)
        str_ws "field" >>. pipe2 (pint32 .>> skip) argument decode

    let instruction =
        let ch = 
                generate
            <|> binary
            <|> offset
            <|> alloc
            <|> cast
            <|> local
            <|> load
            <|> makeStruct
            <|> field
            <|> (call Instructions.Call)
        ch .>> skip

    // Statements
    let tmpAssign =
        let parser = tmpLabel .>> str_ws "=" .>>. instruction .>> skip
        let decode (name, instr) =
            Statements.TmpAssign.CreateTerm(Term.Const(name), instr)
        parser |>> decode

    let returnStmt =
        let parser = str_ws "return" >>. argument
        let decode (t:Term) = Statements.Return.CreateTerm(t)
        parser |>> decode

    let copy =
        let dst = argument
        let src = str_ws "<-" >>. argument
        let size = argument
        let decode b a s = Statements.Copy.CreateTerm(b, a, s)
        str_ws "copy" >>. pipe3 dst src size decode     

    let store =
        let dst = argument
        let src = str_ws "<-" >>. argument
        let decode d s = Statements.Store.CreateTerm(d, s)
        str_ws "store" >>. pipe2 dst src decode

    let bbLabel = 
        let decode name = Statements.BasicBlock.CreateTerm(Term.Const(name))
        pchar ':' >>. identifier |>> decode

    let ifStmt = 
        let cond = str_ws "if" >>. argument
        let _then = str_ws "then" >>. identifier
        let _else = str_ws "else" >>. identifier
        let decode arg t e = Statements.If.CreateTerm(arg, Term.Const(t), Term.Const(e))
        pipe3 cond _then _else decode

    let goto =
        let decode dst = Statements.Goto.CreateTerm(Term.Const(dst))
        str_ws "goto" >>. identifier |>> decode

    let bind =
        let decode t arg = Statements.Bind.CreateTerm(t, arg)
        str_ws "bind" >>.pipe2 expr argument decode

    let unbind = 
        let decode (t:Term) = Statements.Unbind.CreateTerm(t)
        str_ws "unbind" >>. expr |>> decode

    let statement = 
        let pos = getPosition |>> fun p -> Statements.Comment.CreateTerm(Term.Const(p.ToString()))
        let ch = 
                tmpAssign   
            <|> returnStmt
            <|> copy
            <|> store
            <|> (call Statements.VoidCall)
            <|> bbLabel
            <|> ifStmt
            <|> goto
            <|> bind
            <|> unbind
        pos .>>. ch .>> skip

    let rec unpair s = 
        match s with 
            | (a,b)::tail -> a::b::(unpair tail)
            | [] -> []
        
    // Top-level structure
    let body (pattern:Term) = 
        let parser = between (str_ws "{") (str_ws "}") (many statement) .>> skip
        let pushandcons lst = 
            let push = Statements.Push.CreateTerm()
            //let opening = new Term(Statements.Comment, Term.Const(pattern.[0].ToString()))
            //let closing = new Term(Statements.Comment, Term.Const("end " + pattern.[0].ToString()))
            //ConsUtils.Cons( List.append (opening :: Statements.Push :: (unpair lst)) [closing]  )
            ConsUtils.Cons( push :: (List.map (fun (a,b) -> b) lst)) 

        let second = parser |>> pushandcons
        preturn pattern .>>. second

    let pattern = 
        let genwrap (t:Term) = Instructions.Generate.[t]
        expr |>> genwrap

    let full = pattern >>= body
    member x.Parser = full