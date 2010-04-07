using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using StorEvil.Core;
using StorEvil.Utility;

namespace StorEvil.Parsing
{
    public class StoryParser : IStoryParser
    {
        public Story Parse(string storyText, string id)
        {
            var storyParsingJob = new StoryParsingJob();
            return storyParsingJob.Parse(storyText, id);
        }
    }

    /// <summary>
    /// Parses a story.
    /// Instantiated newly for each story.
    /// (holds the state that is kept as the parsing process runs to construct a story)
    /// </summary>
    public class StoryParsingJob
    {
        private readonly List<IScenario> scenarios = new List<IScenario>();
        private readonly StringBuilder _storyName = new StringBuilder();
        private ScenarioBuildingInfo _currentScenario;
        private string _storyId;
        private List<string> _tags =  new List<string>();

        private Action<ScenarioLine> _currentLineHandler;
        private List<string> _storyTags = new List<string>();

        public Story Parse(string storyText, string storyId)
        {
            InitializeParsing(storyId);

            foreach (var line in ParseLines(storyText))
                HandleStoryTextLine(line);

            AddScenarioOrOutlineIfExists();

            FixEmptyScenarioNames();

            return GetStory();
        }

        private void InitializeParsing(string storyId)
        {
            _storyId = storyId;
            _currentLineHandler = AppendToStoryName;
        }

        private void HandleStoryTextLine(ScenarioLine line)
        {
           
            if (IsTags(line.Text))
            {
                HandleTags(line.Text);
                return;
            }
            if (IsNewScenarioOrOutline(line.Text))
            {
                InitializeNewScenario(line.Text);
                return;
            }

            if (IsStartOfExamples(line.Text))
            {
                _currentLineHandler = HandleScenarioExampleRow;
                return;
            }

            if (IsTableRow(line.Text) && !_currentScenario.IsOutline)
            {
                if (_currentScenario != null && _currentScenario.Lines.Count > 0)
                {
                    var last = _currentScenario.Lines.Last();
                    _currentScenario.Lines = _currentScenario.Lines.GetRange(0, _currentScenario.Lines.Count() - 1);
                    _currentScenario.Lines.Add(new ScenarioLine { Text = last.Text + "\r\n" + line.Text, LineNumber = last.LineNumber});
                    return;
                }
            }

            _currentLineHandler(line);
        }

        private void ApplyTagsFromLastLine()
        {
            if (_tags.Count ==0)
                return;

            if (_currentScenario != null)
                _currentScenario.Tags = _tags.ToArray();
            else
                _storyTags.AddRange(_tags);            

            _tags.Clear();
        }

        private void HandleTags(string line)
        {
            _tags.AddRange(GetTags(line));
        }

        private IEnumerable<string> GetTags(string line)
        {
            var tagFinder = new Regex(@"\@\w+");
            foreach (Match match in tagFinder.Matches(line))
            {
                yield return match.Value.Substring(1);
            }
        }

        private bool IsTags(string line)
        {
            return line.StartsWith("@");
        }

        private static bool IsTableRow(string line)
        {
            return line.StartsWith("|");
        }

        private Story GetStory()
        {
            return new Story(_storyId, _storyName.ToString().Trim(), scenarios) {Tags = _storyTags};
        }

        private static bool IsNewScenarioOrOutline(string line)
        {
            return IsScenarioOutlineHeader(line) || IsScenarioHeader(line);
        }

        private void InitializeNewScenario(string line)
        {
            AddScenarioOrOutlineIfExists();

            _currentScenario = new ScenarioBuildingInfo
                                   {
                                       Name = line.After(":").Trim(),
                                       IsOutline = IsScenarioOutlineHeader(line)
                                   };
            ApplyTagsFromLastLine();
            _currentLineHandler = HandleScenarioLine;
        }

        private void AppendToStoryName(ScenarioLine line)
        {
            _storyName.Append(line.Text + "\r\n");
            ApplyTagsFromLastLine();
        }

        private void AddScenarioOutline()
        {
            var innerScenario = BuildScenario();

            var count = _currentScenario.RowData.First().Count() - 1;
            var fieldNames = _currentScenario.RowData.First().Take(count).ToArray();
            var examples =
                _currentScenario.RowData.Skip(1).Select(x => x.Take(count).ToArray()).ToArray();

            var scenarioOutline = new ScenarioOutline(_storyId + "- outline -" + scenarios.Count,
                                                      _currentScenario.Name,
                                                      innerScenario,
                                                      fieldNames,
                                                      examples) { Tags = _currentScenario.Tags };
            scenarios.Add(
                scenarioOutline);
        }

        private Scenario BuildScenario()
        {
            return new Scenario(_storyId + "-" + scenarios.Count,
                                _currentScenario.Name,
                                _currentScenario.Lines.ToArray()) {Tags = _currentScenario.Tags};
        }

        private void AddScenarioOrOutlineIfExists()
        {
            if (_currentScenario == null)
                return;

            if (CurrentScenarioIsOutline())
                AddScenarioOutline();
            else
                AddScenario();
        }

        private bool CurrentScenarioIsOutline()
        {
            return _currentScenario.RowData != null &&
                   _currentScenario.RowData.Count() > 0;
        }

        private void AddScenario()
        {
            scenarios.Add(BuildScenario());
        }

        private void HandleScenarioLine(ScenarioLine line)
        {
            if (!IsComment(line.Text) && line.Text.Trim().Length > 0)
                _currentScenario.Lines.Add(line);
        }

        private void HandleScenarioExampleRow(ScenarioLine line)
        {
            if (_currentScenario != null && line.Text.StartsWith("|"))
                _currentScenario.RowData.Add(line.Text.Split('|').Skip(1));
        }

        private void FixEmptyScenarioNames()
        {
            foreach (var scenario in scenarios.Where(scenario => string.IsNullOrEmpty(scenario.Name)))
                SetScenarioNameToDefault(scenario);
        }

        private static void SetScenarioNameToDefault(IScenario scenario)
        {
            if (scenario is Scenario)
            {
                var s = scenario as Scenario;
                s.Name = string.Join("\r\n", s.Body.Select(l=>l.Text).ToArray());
            }
            else if (scenario is ScenarioOutline)
            {
                var s = scenario as ScenarioOutline;
                s.Name = string.Join("\r\n", s.Scenario.Body.Select(l => l.Text).ToArray());
            }
        }

        private static bool IsComment(string s)
        {
            return s.Trim().StartsWith("#");
        }

        private static bool IsStartOfExamples(string line)
        {
            return line.ToLower().StartsWith("examples:");
        }

        private static bool IsScenarioHeader(string line)
        {
            return line.ToLower().StartsWith("scenario:");
        }

        private static bool IsScenarioOutlineHeader(string line)
        {
            return line.ToLower().StartsWith("scenario outline:");
        }

        internal class ScenarioBuildingInfo
        {
            public string Name;
            public List<ScenarioLine> Lines = new List<ScenarioLine>();
            public List<IEnumerable<string>> RowData = new List<IEnumerable<string>>();
            public bool IsOutline;

            public string[] Tags;
        }

        private static IEnumerable<ScenarioLine> ParseLines(string text)
        {
            var lines = text.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
            return lines.Select((t, i) => new ScenarioLine {LineNumber = i + 1, Text = t.Trim()});
        }
    }
}