using System.Linq;
using StorEvil.Context;
using StorEvil.Core;
using StorEvil.Events;
using StorEvil.Interpreter;

namespace StorEvil.InPlace
{
    public class InPlaceScenarioRunner
    {
        private readonly IEventBus _eventBus;
        private readonly ScenarioInterpreter _scenarioInterpreter;

        private readonly ScenarioLineExecuter _lineExecuter;

        public InPlaceScenarioRunner(IEventBus eventBus, ScenarioInterpreter scenarioInterpreter)
        {
            _eventBus = eventBus;
            _scenarioInterpreter = scenarioInterpreter;
            _lineExecuter = new ScenarioLineExecuter(scenarioInterpreter, eventBus);
        }

        public bool ExecuteScenario(Scenario scenario, ScenarioContext storyContext)
        {
            _eventBus.Raise(new ScenarioStarting { Scenario = scenario});
            //_scenarioInterpreter.NewScenario();

            foreach (var line in (scenario.Background ?? new ScenarioLine[0]).Union(scenario.Body))
            {
                LineStatus status = _lineExecuter.ExecuteLine(scenario, storyContext, line.Text);
                if (LineStatus.Failed == status)
                {
                    _eventBus.Raise(new ScenarioFinished { Scenario = scenario, Status = ExecutionStatus.Failed });
                    return false;
                }
                if (LineStatus.Pending == status)
                {
                    _eventBus.Raise(new ScenarioFinished { Scenario = scenario, Status = ExecutionStatus.Pending });
             
                    return true;
                }
            }

            _eventBus.Raise(new ScenarioFinished { Scenario = scenario, Status = ExecutionStatus.Passed});

            //_eventBus.Raise(new ScenarioSucceededEvent { Scenario = scenario });
            return true;
        }
    }

    
}