﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StorEvil.Context;
using StorEvil.Core;
using StorEvil.Infrastructure;
using StorEvil.InPlace;
using StorEvil.Interpreter;
using StorEvil.Utility;

namespace StorEvil.StubGeneration
{
    public class StubGenerator : IStoryHandler
    {
        private readonly ScenarioInterpreter _scenarioInterpreter;
        private readonly ImplementationHelper _implementationHelper;
        public readonly ITextWriter SuggestionWriter;
        private readonly ISessionContext _context;
        private readonly List<string> _suggestions = new List<string>();

        public StubGenerator(ScenarioInterpreter scenarioInterpreter, ImplementationHelper implementationHelper, ITextWriter suggestionWriter, ISessionContext context)
        {
            _scenarioInterpreter = scenarioInterpreter;
            SuggestionWriter = suggestionWriter;
            _context = context;
            _implementationHelper = implementationHelper;
        }

        public void HandleStory(Story story)
        {
            var context = _context.GetContextForStory();
            foreach (var scenario in story.Scenarios)
            {
                var scenarioContext = context .GetScenarioContext();
                foreach (var line in GetLines(scenario))
                {
                    if (null == _scenarioInterpreter.GetChain(scenarioContext, line))
                    {
                        var suggestedCode = _implementationHelper.Suggest(line) + "\r\n";
                        AddSuggestion(suggestedCode);
                    }
                }
            }
        }

        public JobResult HandleStories(IEnumerable<Story> stories)
        {
            foreach (var story in stories)
            {
                HandleStory(story);
            }
            PrintSuggestions();
            return new JobResult();
        }

        private void AddSuggestion(string suggestedCode)
        {
            string existingSuggestion = ExistingSuggestion(suggestedCode);
            if (existingSuggestion == null)
            {
                
                _suggestions.Add(suggestedCode);
                return;
            }
            if (suggestedCode.Until("\r\n") != existingSuggestion.Until("\r\n"))
            {
           
                _suggestions.Remove(existingSuggestion);
                _suggestions.Add(suggestedCode.Until("\r\n") + "\r\n" + existingSuggestion);
            }
        }

        private string ExistingSuggestion(string suggestedCode)
        {
            return _suggestions.FirstOrDefault(s => ExtractDeclaration(s) == ExtractDeclaration(suggestedCode));
        }

        private string ExtractDeclaration(string suggestedCode)
        {
            return suggestedCode.Split(new[] { "\r\n"}, StringSplitOptions.None)[1];
        }

        private IEnumerable<string> GetLines(IScenario scenario)
        {
            return scenario.Body.Select(x=>x.Text);
        }

        private void PrintSuggestions()
        {
            var joined = "        " + string.Join("\r\n", _suggestions.OrderBy(x=>x).ToArray()).Replace("\r\n", "\r\n        ");
            
            string classFormat = @"

namespace Your.Namespace.Here 
{{
    [StorEvil.Context]
    public class StubContextClass 
    {{
{0}
    }}
}}";
            var classDefinition = string.Format(classFormat, joined);
            SuggestionWriter.Write(classDefinition);

        }
    }
}