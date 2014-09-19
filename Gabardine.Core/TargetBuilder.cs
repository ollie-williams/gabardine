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
using System.Diagnostics.Debug;

namespace Gabardine
{
    internal class TargetBuilder
    {
        readonly TargetBuilderStep[] steps;
        readonly int[] variables;

        internal TargetBuilder(TargetBuilderStep[] steps, int[] variables)
        {
            this.steps = steps;
            this.variables = variables;
        }

        internal Term Build(Term[] bindings)
        {
            Stack<Term> stack = new Stack<Term>();
            int j = 0;

            for (int i = 0; i < steps.Length; ++i) {

                if (steps[i].Significance == StepSignificance.None) {
                    BuildRegular(steps[i], stack);
                    continue;
                }

                Assert(steps[i].Op.Kind == OperatorKind.PatternVariable, "Expected a variable.");
                int variable = variables[j++];
                Assert(variable < bindings.Length, "Variable index out of range.");
                Term binding = bindings[variable];

                switch (steps[i].Significance) {
                    case StepSignificance.OnlyVariableInstance:
                        // If the operator is a variable, get the appropriate binding from the array. No other occurrances.
                        stack.Push(binding);
                        break;
                    
                    case StepSignificance.None:
                        Assert(false, "This case should have been handled.");
                        break;
                }
            }

            Assert(stack.Count == 1, "Only the result should be left on the stack.");
            return stack.Pop();
        }

        private void BuildRegular(TargetBuilderStep step, Stack<Term> stack)
        {
            Assert(step.Significance == StepSignificance.None);
            Assert(step.Op.Kind != OperatorKind.PatternVariable, "Expected something other than a variable.");
            Assert(stack.Count >= step.Op.Arity, "Expected there to be sufficient terms available on the stack.");

            Term term = new Term(step.Op, stack.PopFIFO(step.Op.Arity));

            // If the operator is special, handle it in code directly
            switch(Special.GetKind(step.Op)) {
                case Special.Kind.Subs:
                    term = Substitution(term[0], term[1], term[2]);
                    break;
                case Special.Kind.Lambda:
                    term = FreshenLambda(term);
                    break;
                case Special.Kind.Fresh:
                    term = Freshen(term);
                    break;
                case Special.Kind.IsParam:
                    bool isparam = 
                        term[0].Op.Kind == OperatorKind.Parameter 
                        || term[0].Op.Kind == OperatorKind.LetVariable
                        || term[0].Op.Kind == OperatorKind.LambdaVariable;
                    term = isparam ? Special.True : Special.False;
                    break;
                case Special.Kind.IsConst:
                    // If we can tell immediately that the term is a constant, convert
                    // it to True. Otherwise carry on.
                    bool isConst = term[0].Op.IsConstant;
                    term = isConst ? Special.True : term;
                    break;
                case Special.Kind.Inherit:
                    term = TryImmediateInherit(term);
                    break;
                case Special.Kind.Breakout:
                    term = BreakoutDispatcher.Handle(term[0]);   
                    break;
                default:
                    break;
            }
            
            stack.Push(term);
        }

        Term TryImmediateInherit(Term term)
        {
            Assert(term.Op == Special.Inherit);
            if (term[0].Op.Kind == OperatorKind.LetVariable) {
                return ((LetVariable)term[0].Op).Binding;
            }
            return term;
        }

        Term FreshenLambda(Term lambda)
        {
            Assert(lambda.Op == Special.Lambda, "Expected a lambda expression.");
            var parameter = lambda[0];
            if (parameter.Op != Special.Fresh) {
                return lambda;
            }

            var body = lambda[1];
            Term newParam = new LambdaVariable(parameter[0].Op.Name + '\'').CreateTerm();
            body = Replacement.Replace(body, parameter[0], newParam);
            return Special.Lambda[newParam, body];
        }

        Term Freshen(Term variable)
        {
            if (variable[0].Op.Kind != OperatorKind.LambdaVariable) {
                throw new ArgumentException("Can only freshen lambda variables.");
            }
            return new LambdaVariable(variable[0].Op.Name + '\'').CreateTerm();
        }

        private Term Substitution(Term body, Term oldTerm, Term newTerm)
        {
            Stack<Term> stack = new Stack<Term>();
            stack.Push(body);

            Stack<Term> args = new Stack<Term>();

            while(stack.Count > 0) {
                Term head = stack.Pop();

                if (ReferenceEquals(head, null)) {
                    head = stack.Pop();
                    Term fresh = new Term(head.Op, args.PopFIFO(head.Arity));
                    args.Push(fresh);
                    continue;
                }

                if (head == oldTerm) {
                    args.Push(newTerm);
                    continue;
                }

                //subs(lambda(x, M), y, N) -> lambda(z, subs(replace(M, x, z), y, N))
                if (head.Op == Special.Lambda) {
                    var x = head[0];
                    var M = head[1];
                    var z = new Operator(0, OperatorKind.LambdaVariable, x.Op.Name);
                    var rep = Replacement.Replace(M, x, z);

                    stack.Push(head); 
                    stack.Push(null);
                    args.Push(z);
                    stack.Push(rep);
                    continue;
                }

                stack.Push(head);
                stack.Push(null);
                for (int i = head.Arity-1; i >=0; --i) {
                    stack.Push(head[i]);
                }
            }

            Assert(args.Count == 1, "Expected only result to remain.");
            return args.Pop();
        }
    }

    enum StepSignificance
    {
        /// <summary>
        /// Take no action. Appropriate for non-variables.
        /// </summary>
        None,

        /// <summary>
        /// A variable which appears only once in the target can be safely copied across.
        /// </summary>
        OnlyVariableInstance,
    };


    class TargetBuilderStep
    {
        readonly Operator op;
        public StepSignificance Significance { get; set; }

        public TargetBuilderStep(Operator op, StepSignificance significance)
        {
            this.op = op;
            Significance = significance;
        }

        public Operator Op { get { return op; } }
    }

    class TargetBuilderFactory
    {
        readonly Dictionary<Operator, int> variableMap;

        public TargetBuilderFactory(Dictionary<Operator, int> variableMap)
        {
            this.variableMap = variableMap;
        }

        
    }
}