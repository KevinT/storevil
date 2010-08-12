﻿using System;
using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.TaskRunnerFramework;
using StorEvil.Core;
using StorEvil.Resharper.Runner;
//using System.Linq;


namespace StorEvil.Resharper.Tasks
{
    [Serializable]
    public class RunScenarioTask : RemoteTask, IEquatable<RunScenarioTask>
    {
        public bool Explicitly { get; set; }
        private const string MagicDelimiter = "$*$*$";

        private TaskScenarioLine[] Background;
        private TaskScenarioLine[] Body;
        private readonly string Name;
        private bool IsOutline;
        private string[] FieldNames;
        
        private string[][] Examples;

        public RunScenarioTask(IScenario scenario, bool explicitly)
            : base(StorEvilTaskRunner.RunnerId)
        {
            Logger.Log("RunScenarioTask - constructed\r\n" + scenario.Id + " - " + scenario.Name + "\r\n Explicitly:" + explicitly);
            Explicitly = explicitly;
            Id = scenario.Id;

            Name = scenario.Name;
            if (scenario is Scenario)
            {
                var s = scenario as Scenario;
                Body = ConvertLines(s.Body);
                Background = ConvertLines(s.Background);
            }
            else
            {
                var so = scenario as ScenarioOutline;
                Body = ConvertLines(so.Scenario.Body);
                Background = ConvertLines(so.Scenario.Background);
                Examples = so.Examples;
                FieldNames = so.FieldNames;
            }
        }

        private TaskScenarioLine[] ConvertLines(ScenarioLine[] lines)
        {
            var body = new TaskScenarioLine[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                body[i] = new TaskScenarioLine {LineNumber = lines[i].LineNumber, Text = lines[i].Text};
            return body;
        }

        public RunScenarioTask(XmlElement element) : base(element)
        {
            Logger.Log("RunScenarioTask - constructed from XML\r\n" + element.OuterXml);

            var type = GetXmlAttribute(element, "ScenarioType");
            Name = GetXmlAttribute(element, "Name");
            Id = GetXmlAttribute(element, "Id");
            Body = GetXmlLines(element, "Body");
            Background = GetXmlLines(element, "Background");
            if (type == "Scenario")
                LoadScenarioXml(element);
            else
                LoadScenarioOutlineXml(element);

            
        }

        private void LoadScenarioXml(XmlElement element)
        {
            IsOutline = false;
        }

        private void LoadScenarioOutlineXml(XmlElement element)
        {
            FieldNames = SplitValues(GetXmlAttribute(element, "FieldNames"));
            
            var exampleLines = GetXmlAttribute(element, "Examples").Split(new[] {"|||"}, StringSplitOptions.None);
            var examples = new List<string[]>();

            foreach (var exampleLine in exampleLines)
                examples.Add(SplitValues(exampleLine));

            Examples = examples.ToArray();
            IsOutline = true;
        }

        public override void SaveXml(XmlElement element)
        {
            Logger.Log("RunScenarioTask - SaveXml");
            base.SaveXml(element);
            SetXmlAttribute(element, "Name", Name);
            SetXmlAttribute(element, "Id", Id);
            SetXmlLines(element, Body, "Body");
            SetXmlLines(element, Background, "Background");
            if (IsOutline)
                SaveScenarioOutlineXml(element);
            else
                SaveScenarioXml(element);

            Logger.Log(element.OuterXml);
        
        }

        private void SaveScenarioXml(XmlElement element)
        {
            SetXmlAttribute(element, "ScenarioType", "Scenario");
        }

        private void SaveScenarioOutlineXml(XmlElement element)
        {
            SetXmlAttribute(element, "ScenarioType", "ScenarioOutline");
            SetXmlAttribute(element, "FieldNames", JoinValues(FieldNames));

            var examplesJoined = new List<string>();
            foreach (var example in Examples)
                examplesJoined.Add(JoinValues(example));

            var exampleValue = string.Join("|||", examplesJoined.ToArray());
            SetXmlAttribute(element, "Examples", exampleValue);
        }

        private TaskScenarioLine[] GetXmlLines(XmlElement element, string elementName)
        {
            var lines = new List<TaskScenarioLine>();

            var bodyElement = (XmlElement) element.GetElementsByTagName(elementName)[0];
            var lineElements = bodyElement.GetElementsByTagName("Line");

            foreach (XmlElement lineElement in lineElements)
            {
                lines.Add(new TaskScenarioLine { Text = lineElement.GetAttribute("Text"), LineNumber = int.Parse(lineElement.GetAttribute("LineNumber")) });
            }

            return lines.ToArray();
        }

        private string[] SplitValues(string text)
        {
            return text.Split(new[] {MagicDelimiter}, StringSplitOptions.None);
        }

        private void SetXmlLines(XmlElement element, IEnumerable<TaskScenarioLine> body, string elementName)
        {
            var bodyElement = element.OwnerDocument.CreateElement(elementName);
            element.AppendChild(bodyElement);
            foreach (var line in body)
            {
                var lineElement = element.OwnerDocument.CreateElement("Line");

                SetXmlAttribute(lineElement, "LineNumber", line.LineNumber.ToString());
                SetXmlAttribute(lineElement, "Text", line.Text);
                bodyElement.AppendChild(lineElement);
            }          
        }

        private string JoinValues(IEnumerable<string> values)
        {
            return string.Join("$*$*$", new List<string>(values).ToArray());
        }

        internal IScenario GetScenario()
        {
            if (IsOutline)
            {
                return new ScenarioOutline(Id, Name, new Scenario(Name, GetScenarioLines(Body)), FieldNames, Examples) { Background = GetScenarioLines(Background) };
            }
            else
            {
                return new Scenario(Id, Name, GetScenarioLines(Body)) { Background = GetScenarioLines(Background) };
            }
        }

        private ScenarioLine[] GetScenarioLines(TaskScenarioLine[] inLines)
        {
            var lines = new ScenarioLine[inLines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = new ScenarioLine {Text = inLines[i].Text, LineNumber = inLines[i].LineNumber};
            }

            return lines;
            
        }

        public bool Equals(RunScenarioTask other)
        {
            var equals = other != null && other.Id == Id && other.Explicitly == Explicitly;
            return equals;
        }

        public override bool Equals(RemoteTask other)
        {
            if (other is RunScenarioTask)
            {
                var runScenarioTask = (RunScenarioTask)other;
                return runScenarioTask.Id == Id && runScenarioTask.Explicitly == this.Explicitly;
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as RunScenarioTask);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result*397) ^ IsOutline.GetHashCode();
                result = (result*397) ^ Explicitly.GetHashCode();
                result = (result*397) ^ (Id != null ? Id.GetHashCode() : 0);
                return result;
            }
        }

        protected string Id { get; set; }

        public override string ToString()
        {
            return "RunScenarioTask \r\n Id=" + (Id ?? "") + "\r\n Name=" + (Name ?? "");
        }
    }

    [Serializable]
    internal class TaskScenarioLine
    {
        public string Text;
        public int LineNumber;
    }
}