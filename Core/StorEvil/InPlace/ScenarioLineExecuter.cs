using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using StorEvil.Context;
using StorEvil.Core;
using StorEvil.Interpreter;

namespace StorEvil.InPlace
{
    public class ScenarioLineExecuter
    {
        public ScenarioLineExecuter(MemberInvoker memberInvoker, 
                                    ScenarioInterpreter scenarioInterpreter,
                                    IResultListener listener)
        {
            _memberInvoker = memberInvoker;
            _listener = listener;
            _scenarioInterpreter = scenarioInterpreter;
        }

        private readonly IResultListener _listener;

        private readonly MemberInvoker _memberInvoker;
        private readonly ScenarioInterpreter _scenarioInterpreter;
        private object _lastResult;
        private readonly ImplementationHelper _implementationHelper = new ImplementationHelper();

        public bool ExecuteLine(Scenario scenario, ScenarioContext storyContext, string line)
        {
            // Debug.WriteLine("SLE: " + line);
            InvocationChain chain = GetMatchingChain(storyContext, line);

            if (chain == null)
            {
                var suggestion = _implementationHelper.Suggest(line);
                _listener.ScenarioPending(new ScenarioPendingInfo(scenario, line, suggestion));
                return false;
            }

            if (!ExecuteChain(scenario, storyContext, chain, line))
                return false;

            _listener.Success(scenario, line);
            return true;
        }

        private bool ExecuteChain(Scenario scenario, ScenarioContext storyContext, InvocationChain chain, string line)
        {
            string successPart = "";
            _lastResult = null;
            foreach (var invocation in chain.Invocations)
            {
                try
                {
                    InvokeContextMember(storyContext, invocation);
                    successPart += invocation.MatchedText + " ";
                }
                catch (TargetInvocationException ex)
                {
                    if (ex.InnerException is ScenarioPendingException)
                    {
                        _listener.ScenarioPending(new ScenarioPendingInfo(scenario, line));
                    }
                    else
                    {
                        _listener.ScenarioFailed(new ScenarioFailureInfo(scenario, successPart.Trim(),
                                                                         invocation.MatchedText, GetExceptionMessage(ex)));
                    }

                    return false;
                }
            }

            return true;
        }

        private void InvokeContextMember(ScenarioContext storyContext, Invocation invocation)
        {
            MemberInfo info = invocation.MemberInfo;
            var declaringType = info.DeclaringType;
            var context = _lastResult ?? storyContext.GetContext(declaringType);
            _lastResult = _memberInvoker.InvokeMember(info, invocation.ParamValues, context);
        }

        private InvocationChain GetMatchingChain(ScenarioContext storyContext, string line)
        {
            var chain = _scenarioInterpreter.GetChain(storyContext, line);

            return chain;
        }

        private static string GetExceptionMessage(Exception exception)
        {
            var ex = exception.InnerException ?? exception;

            var noStackTrace = ex is AssertionException;

            return noStackTrace ? ex.Message : ex.Message + "\r\n" + ex;
        }
    }
}