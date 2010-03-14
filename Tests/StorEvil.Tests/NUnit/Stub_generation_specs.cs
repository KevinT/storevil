using System.Reflection;
using NUnit.Framework;
using Rhino.Mocks;
using StorEvil.Context;
using StorEvil.Core;
using StorEvil.Infrastructure;
using StorEvil.InPlace;
using StorEvil.Interpreter;
using StorEvil.StubGeneration;
using StorEvil.Utility;

namespace StorEvil.NUnit
{
    [TestFixture]
    public class Stub_generation_specs
    {
        private StubGenerator Generator;
        private Story TestStory;
        private string Suggestions;
        private ITextWriter FakeWriter;

        [SetUp]
        public void SetupContext()
        {
            FakeWriter = MockRepository.GenerateMock<ITextWriter>();

            Generator =
                new StubGenerator(new ScenarioInterpreter(new InterpreterForTypeFactory(new ExtensionMethodHandler())),
                                  new ImplementationHelper(), FakeWriter);

            TestStory = new Story("foo", "", new[] { new Scenario("", "", new[] { "first line", "second line" }) });

            Generator.HandleStory(TestStory, new StoryContext());
            Generator.Finished();

            Suggestions = (string) FakeWriter.GetArgumentsForCallsMadeOn(x => x.Write(Arg<string>.Is.Anything))[0][0];            
        }

        [Test]
        public void Should_suggest_methods()
        {
            Suggestions.ShouldContain("first_line()");
            Suggestions.ShouldContain("second_line()");
        }

        [Test]
        public void Generated_code_should_compile()
        {
            CreateTestAssembly(Suggestions).ShouldNotBeNull();
        }

        private static Assembly CreateTestAssembly(string code)
        {
            const string header = "using System;\r\n";
            return TestHelper.CreateAssembly(header + "\r\n" + code);
        }
    }
}