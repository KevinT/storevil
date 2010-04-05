using NUnit.Framework;
using Rhino.Mocks;
using StorEvil.Core;
using StorEvil.InPlace;

namespace StorEvil.InPlace_Compiled
{
    [TestFixture]
    public class When_scenario_does_not_map
        : InPlace.When_scenario_does_not_map, UsingCompiledRunner { }

    [TestFixture]
    public class When_scenario_step_is_pending
        : InPlace.When_scenario_step_is_pending, UsingCompiledRunner { }
}

namespace StorEvil.InPlace
{
    [TestFixture]
    public class When_scenario_does_not_map : InPlaceRunnerSpec<InPlaceRunnerTestContext>
    {
        private readonly Scenario TestScenario = BuildScenario("test",  "When scenario test does not map");

        [SetUp]
        public void SetupContext()
        {
            var story = new Story("test", "summary", new[] {TestScenario});

            RunStory(story);
        }

        [Test]
        public void Notifies_listener()
        {
            ResultListener.AssertWasCalled(x => x.ScenarioPending(Any<ScenarioPendingInfo>()));
        }
    }

    [TestFixture]
    public class When_scenario_step_is_pending : InPlaceRunnerSpec<InPlaceRunnerTestContext>
    {
        private readonly Scenario TestScenario = BuildScenario("test", "Pending scenario step" );

        [SetUp]
        public void SetupContext()
        {
            var story = new Story("test", "summary", new[] { TestScenario });

            RunStory(story);
        }

        [Test]
        public void Notifies_listener()
        {
            ResultListener.AssertWasCalled(x => x.ScenarioPending(Any<ScenarioPendingInfo>()));
        }
    }
}