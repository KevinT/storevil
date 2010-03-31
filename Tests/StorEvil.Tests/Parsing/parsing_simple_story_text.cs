using System.Linq;
using NUnit.Framework;
using StorEvil.Core;
using StorEvil.Utility;

namespace StorEvil.Parsing
{
    [TestFixture]
    public class parsing_simple_story_text
    {
        private Story Result;

        private const string testSingleStoryText =
            @"
As a user I want to do something
Scenario: Doing something
Given some condition
"
            + "\t   \r\n" +
            @"     
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
            Result.Scenarios.Count().ShouldEqual(1);
        }

        [Test]
        public void Should_Parse_Scenario_Name()
        {
            ((Scenario) Result.Scenarios.First()).Name.ShouldEqual("Doing something");
        }

        [Test]
        public void Should_Parse_1st_Scenario_Text()
        {
            ((Scenario)Result.Scenarios.First()).Body.First().Text.ShouldEqual("Given some condition");
        }

        [Test]
        public void Should_Ignore_comment_Scenario_Text()
        {
            ((Scenario) Result.Scenarios.First()).Body.Any(x => x.Text.Contains("comment")).ShouldEqual(false);
        }

        [Test]
        public void Should_Ignore_blank_lines_that_start_with_tabs_or_spaces()
        {
            ((Scenario) Result.Scenarios.First()).Body.Count().ShouldEqual(3);
        }
    }

    [TestFixture]
    public class parsing_a_story_with_multiple_lines_of_text
    {
        private Story Result;

        private const string testSingleStoryText =
            @"
As a user I want to do something

and something else
so that I can achieve a goal

Scenario: Doing something
Given some condition
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
            Result.Summary.ShouldEqual("As a user I want to do something\r\n\r\nand something else\r\nso that I can achieve a goal");
        }
    }

    [TestFixture]
    public class parsing_tags
    {
        private Story Result;

        private const string testStoryText =
            @"
@foo @bar
As a user I want to do something

and something else
so that I can achieve a goal

@bar @baz
Scenario: Doing something
Given some condition
";

        [SetUp]
        public void SetupContext()
        {
            var parser = new StoryParser();
            Result = parser.Parse(testStoryText, null);
        }

        [Test]
        public void Story_Should_Have_Tag()
        {
           Result.Tags.ElementsShouldEqual("foo", "bar");
        }

        [Test]
        public void Scenario_Should_Have_Tag()
        {
            Result.Scenarios.First().Tags.ElementsShouldEqual("bar", "baz");
        }
    }
}