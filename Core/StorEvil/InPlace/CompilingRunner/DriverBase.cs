using System;
using System.Linq;
using StorEvil.Context;
using StorEvil.Core;
using StorEvil.Interpreter;
using StorEvil.Interpreter.ParameterConverters;
using StorEvil.Parsing;

namespace StorEvil.InPlace
{
    public abstract class DriverBase : MarshalByRefObject, IStoryHandler
    {
        protected JobResult Result = new JobResult();

        private readonly IResultListener ResultListener;
        private readonly ScenarioLineExecuter LineExecuter;
        private readonly SessionContext _context;

        private ScenarioContext CurrentScenarioContext;
        private Scenario CurrentScenario;
        
        protected DriverBase(IResultListener resultListener)
        {
            ResultListener = resultListener;
            
            ScenarioInterpreter = new ScenarioInterpreter(new InterpreterForTypeFactory(new ExtensionMethodHandler()), new MostRecentlyUsedContext());           
            LineExecuter = new ScenarioLineExecuter(ScenarioInterpreter, ResultListener);
            _context = new SessionContext();            
        } 

        protected void AddAssembly(string location)
        {
            _context.AddAssembly(location);
            ParameterConverter.AddCustomConverters(location);
        }

        protected object[] GetContexts()
        {
            return CurrentScenarioContext.Contexts.Values.ToArray();
        }

        protected Scenario[] GetScenarios(Story story)
        {           
            return story.Scenarios.SelectMany(s=> new ScenarioPreprocessor().Preprocess(s)).ToArray();
        }

        protected ScenarioInterpreter ScenarioInterpreter;

        public abstract void HandleStory(Story story);

        public void Finished()
        {          
            CurrentStoryContext.Dispose();
        }

        protected object[] ExecuteLine(string line)
        {
            LastStatus = LineExecuter.ExecuteLine(CurrentScenario, CurrentScenarioContext, line);

            return GetContexts();
        }

        public JobResult GetResult()
        {
            return Result;
        }

        protected StoryContext CurrentStoryContext;
        

        protected IDisposable StartScenario(Story story, Scenario scenario)
        {
            ResultListener.ScenarioStarting(scenario);

            if (CurrentStoryContext == null)
                CurrentStoryContext = _context.GetContextForStory();

            CurrentScenarioContext = CurrentStoryContext.GetScenarioContext();
            CurrentScenario = scenario;
            LastStatus = LineStatus.Passed;
            ScenarioInterpreter.NewScenario();

            return CurrentScenarioContext;
        }

        protected LineStatus LastStatus
        {
            get; set;
        }

        protected bool ShouldContinue
        {
            get
            {
                return LastStatus == LineStatus.Passed;
            }
        }

        protected void CollectScenarioResult()
        {
            if (LastStatus == LineStatus.Failed)
            {
                Result.Failed++;
            } 
            else if (LastStatus == LineStatus.Pending)
            {
                Result.Pending++;
            } 
            else
            {
                Result.Succeeded++; 
                ResultListener.ScenarioSucceeded(CurrentScenario);
            }
        }
    }
}