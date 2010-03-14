using System;
using System.Collections.Generic;
using System.Linq;
using StorEvil.Context;
using StorEvil.Utility;

namespace StorEvil.Interpreter
{
    public class ScenarioInterpreter
    {
        private readonly InterpreterForTypeFactory _interpreterFactory;
        private string _lastSignificantFirstWord = null;

        public ScenarioInterpreter(InterpreterForTypeFactory interpreterFactory)
        {
            _interpreterFactory = interpreterFactory;
        }

        public InvocationChain GetChain(ScenarioContext storyContext, string line)
        {
            return GetSelectedChain(storyContext, line);
        }

        public void NewScenario()
        {
            _lastSignificantFirstWord = null;
        }

        private InvocationChain GetSelectedChain(ScenarioContext storyContext, string line)
        {
            foreach (var linePermutation in GetPermutations(line))
            {
                foreach (var type in storyContext.ImplementingTypes)
                {
                    var interpreter = _interpreterFactory.GetInterpreterForType(type);

                    InvocationChain chain = interpreter.GetChain(linePermutation);
                    if (chain != null)
                        return chain;
                }
            }
            return null;
        }

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