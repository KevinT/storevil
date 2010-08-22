using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StorEvil.Context;
using StorEvil.Events;
using StorEvil.InPlace;
using StorEvil.Utility;

namespace StorEvil.Interpreter
{
    public class StandardScenarioInterpreter : ScenarioInterpreter
    {       
        public StandardScenarioInterpreter(AssemblyRegistry assemblyRegistry) : base(new InterpreterForTypeFactory(assemblyRegistry), new MostRecentlyUsedContext())
        {
        }
    }

    public class ScenarioInterpreter
    {
        private readonly IInterpreterForTypeFactory _interpreterFactory;
        private readonly IAmbiguousMatchResolver _resolver;
        private string _lastSignificantFirstWord;

        public ScenarioInterpreter(IInterpreterForTypeFactory interpreterFactory, IAmbiguousMatchResolver resolver )
        {
            _interpreterFactory = interpreterFactory;
            _resolver = resolver;
        }

        [DebuggerStepThrough]
        public InvocationChain GetChain(ScenarioContext context, string line)
        {
            DebugTrace.Trace("Interpreting", line);
            var chains = (GetSelectedChains(context, line) ?? new InvocationChain[0]).ToArray();
            if (! chains.Any())
                DebugTrace.Trace(GetType().Name, "no match: " + line);

            if (chains.Count() > 1)
            {
                var chain = _resolver.ResolveMatch(line, chains);
                Notify(chain);
                return chain;
            }
            
            if (chains.Any())
            {
                var chain = chains.FirstOrDefault();
                Notify(chain);
                return chain;
            }

            return null;
                
        }

        [DebuggerStepThrough]
        private void Notify(InvocationChain chain)
        {
            foreach (var invocation in chain.Invocations)
            {
                StorEvilEvents.Bus.Raise(new MatchingMemberFound {Member = invocation.MemberInfo});
            }           
        }

        public void NewScenario()
        {
            _lastSignificantFirstWord = null;
        }

        [DebuggerStepThrough]
        private IEnumerable<InvocationChain> GetSelectedChains(ScenarioContext context, string line)
        {
            foreach (var linePermutation in GetPermutations(line))
            {
                foreach (var type in context.ImplementingTypes)
                {
                    var interpreter = _interpreterFactory.GetInterpreterForType(type);

                    IEnumerable<InvocationChain> chains = interpreter.GetChains(linePermutation);
                    foreach (var invocationChain in chains)
                    {
                        yield return invocationChain;
                    }                    
                }
            }
        }

        [DebuggerStepThrough]
        private IEnumerable<string> GetPermutations(string line)
        { 
            if (line.ToLower().StartsWith("and "))
            {
                yield return line;
                yield return line.ReplaceFirstWord(_lastSignificantFirstWord);
            }
            else
            {
                _lastSignificantFirstWord = line.Split(' ').First().Trim();
                yield return line;                
            }
        }
    }
}