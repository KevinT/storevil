﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Funq;
using StorEvil.Configuration;
using StorEvil.Context.Matchers;
using StorEvil.Events;
using StorEvil.Interpreter;
using StorEvil.Utility;

namespace StorEvil.Core
{
    public class StorEvilGlossaryJob : IStorEvilJob
    {
        private readonly IStepProvider _stepProvider;
        private readonly IStepDescriber _stepDescriber;
        private readonly IEventBus _bus;

        public StorEvilGlossaryJob(IStepProvider stepProvider, IStepDescriber stepDescriber, IEventBus bus)
        {
            _stepProvider = stepProvider;
            _stepDescriber = stepDescriber;
            _bus = bus;
        }

        public int Run()
        {
            var descriptions = _stepProvider
                .GetSteps()
                .Select(x => _stepDescriber.Describe(x));

            foreach (var stepDescription in descriptions.OrderBy(x => x))
                _bus.Raise(new GenericInformation {Text = stepDescription});
            return 0;
        }
    }

    public interface IStepDescriber
    {
        string Describe(StepDefinition stepDefinition);
    }

    

    public class StepDefinition
    {
        public StepDefinition()
        {
            Children = new StepDefinition[0];
        }

        public Type DeclaringType { get; set; }

        public IMemberMatcher Matcher { get; set; }

        public IEnumerable<StepDefinition> Children { get; set; }
    }


    public class GlossaryConfigurator : ContainerConfigurator<GlossarySettings>
    {
        protected override void SetupCustomComponents(Container container, ConfigSettings configSettings, GlossarySettings customSettings)
        {
            container.EasyRegister<IStorEvilJob, StorEvilGlossaryJob>();
            container.EasyRegister<IStepDescriber, StepDescriber>();
            container.EasyRegister<IStepProvider, StepProvider>();
            container.EasyRegister<ContextTypeFactory>();
        }

    }

    public class GlossarySettings
    {
    }
}