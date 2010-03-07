using System.Linq;
using NUnit.Framework;
using StorEvil.Core;
using StorEvil.Parsing;

namespace StorEvil.Parsing_story_text
{
    [TestFixture]
    public class parsing_simple_story_text
    {
        private Story Result;
        private const string testSingleStoryText = @"
As a user I want to do something
Scenario: Doing something
Given some condition
# this is a comment
When I take some action
Then I should expect some result
";

        [SetUp]
        public void SetupContext()
        {
            var parser = new StoryParser();
            Result = parser.Parse(testSingleStoryText, null);
        }

        [Test]
        public void Should_Parse_Summary()
        {
            

            Result.Summary.ShouldEqual("As a user I want to do something");
        }


        [Test]
        public void Should_Handle_Single_Scenario()
        {
           
            TestExtensionMethods.ShouldEqual(Result.Scenarios.Count(), 1);
        }

        [Test]
        public void Should_Parse_Scenario_Name()
        {
           
            ((Scenario) Result.Scenarios.First()).Name.ShouldEqual("Doing something");
        }

        [Test]
        public void Should_Parse_1st_Scenario_Text()
        {
            ((Scenario)Result.Scenarios.First()).Body.First().ShouldEqual("Given some condition");
        }

        [Test]
        public void Should_Ignore_comment_Scenario_Text()
        {
            ((Scenario)Result.Scenarios.First()).Body.Any(x=>x.Contains("comment")).ShouldEqual(false);
        }
    }
}