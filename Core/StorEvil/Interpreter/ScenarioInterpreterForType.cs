using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StorEvil.Context.Matchers;
using StorEvil.Context.Matches;
using StorEvil.Interpreter.ParameterConverters;

namespace StorEvil.Interpreter
{
    public class ScenarioInterpreterForType
    {
        private readonly Type _type;
        private readonly InterpreterForTypeFactory _factory;
        private readonly List<IMemberMatcher> _memberMatchers = new List<IMemberMatcher>();
        private static readonly ParameterConverter _parameterConverter = new ParameterConverter();

        public ScenarioInterpreterForType(Type type, 
                                          ExtensionMethodHandler extensionMethodHandler,
                                          InterpreterForTypeFactory factory)
        {
            _type = type;
            _factory = factory;

            DebugTrace.Trace(GetType().Name, "Building interpreter for: " + type);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            foreach (MemberInfo member in GetMembers(flags)) 
                AddMatchers(member);

            // extension methods
            AddMatchersForExtensionMethods(extensionMethodHandler);
        }

        private MemberInfo[] GetMembers(BindingFlags flags)
        {
            var members = _type.GetMembers(flags);
           

            return FilterMembers(members);
        }

        private MemberInfo[] FilterMembers(MemberInfo[] members)
        {
            var ignore = new[] {"GetType", "ToString", "CompareTo", "GetTypeCode", "Equals", "GetHashCode"};
            return members.Where(m => !(m.MemberType == MemberTypes.Constructor || ignore.Contains(m.Name))).ToArray();
        }

        private void AddMatchersForExtensionMethods(ExtensionMethodHandler extensionMethodHandler)
        {
            foreach (var methodInfo in extensionMethodHandler.GetExtensionMethodsFor(_type))
            {
                DebugTrace.Trace(GetType().Name, "Added extension method matcher: " + methodInfo.Name);

                _memberMatchers.Add(new MethodNameMatcher(methodInfo));
            }
        }

        private void AddMatchers(MemberInfo member)
        {
            _memberMatchers.Add(GetMemberMatcher(member));

            DebugTrace.Trace(GetType().Name, "Added reflection matcher: " + member.Name);

            AddRegexMatchersIfAttributePresent(member);
        }

        private static IMemberMatcher GetMemberMatcher(MemberInfo member)
        {
            if (member is MethodInfo)
                return new MethodNameMatcher((MethodInfo) member);
            
            return new PropertyOrFieldNameMatcher(member);
        }

        private void AddRegexMatchersIfAttributePresent(MemberInfo member)
        {
            var regexAttrs = member.GetCustomAttributes(typeof (ContextRegexAttribute), true);
            foreach (var regexAttr in regexAttrs.Cast<ContextRegexAttribute>())
            {
                DebugTrace.Trace(GetType().Name, "Added regex matcher: " + member.Name + ", \"" + regexAttr.Pattern + "\"");

                _memberMatchers.Add(new RegexMatcher(regexAttr.Pattern, member));
            }
        }

        public InvocationChain GetChain(string line)
        {
            DebugTrace.Trace(this.GetType().Name, "Interpreting '" + line + "' with type:" + _type.Name);
              
            var partialMatches = new List<PartialMatch>();
            foreach (var member in _memberMatchers)
            {
                foreach (var currentMatch in member.GetMatches(line))
                {
                    if (currentMatch is ExactMatch)
                    {
                        DebugTrace.Trace(this.GetType().Name, "Exact match");
              
                        return new InvocationChain
                                   {Invocations = new[] {BuildInvocation(member.MemberInfo, currentMatch)}};
                    }
                    if (currentMatch is PartialMatch)
                    {
                        DebugTrace.Trace(this.GetType().Name, "Partial match -" + currentMatch.MatchedText);
                        partialMatches.Add((PartialMatch) currentMatch);
                    }
                }
            }

            return GetPartialMatchChain(line, partialMatches);
        }

        private InvocationChain GetPartialMatchChain(string line, IEnumerable<PartialMatch> partialMatches)
        {
            foreach (var partialMatch in partialMatches)
            {
                InvocationChain chain = GetChainFromPartialMatch(partialMatch, line);

                if (chain != null)
                    return chain;
            }
            return null;
        }

        private InvocationChain GetChainFromPartialMatch(PartialMatch partialMatch, string line)
        {
            var partialChain = new InvocationChain(BuildInvocation(partialMatch.MemberInfo, partialMatch));

            var matchedText = partialChain.Invocations.Last().MatchedText;
            var remainingLine = line.Substring(matchedText.Length).Trim();

            return TryToRecursivelyExtendPartialMatch(partialChain, remainingLine, partialMatch);
        }

        private static Invocation BuildInvocation(MemberInfo memberInfo, NameMatch currentMatch)
        {
            if (memberInfo is MethodInfo)
                return new Invocation(memberInfo, 
                    BuildParamValues((MethodInfo) memberInfo, currentMatch.ParamValues),
                    BuildRawParamValues((MethodInfo) memberInfo, currentMatch.ParamValues),
                                      currentMatch.MatchedText);

            return new Invocation(memberInfo, new string[0], new string[0], currentMatch.MatchedText);
        }

        private InvocationChain TryToRecursivelyExtendPartialMatch(InvocationChain chain, string remainingLine,
                                                                   PartialMatch partial)
        {
            var chainedMapper = _factory.GetInterpreterForType(partial.TerminatingType);
            var childChain = chainedMapper.GetChain(remainingLine);
            if (childChain == null)
                return null;

            var union = chain.Invocations.Union(childChain.Invocations);

            return new InvocationChain(union.ToArray());
        }

        private static IEnumerable<object> BuildParamValues(MethodBase member, Dictionary<string, object> paramValues)
        {
            var parameters = member.GetParameters();

            if (member.IsStatic)
                parameters = parameters.Skip(1).ToArray();

            foreach (ParameterInfo parameterInfo in parameters)
            {
                if (paramValues.ContainsKey(parameterInfo.Name))
                    yield return ConvertParam(paramValues[parameterInfo.Name].ToString(), parameterInfo.ParameterType);
                else
                    throw new ArgumentException("Could not resolve parameter " + parameterInfo.Name);
            }
        }

        private static IEnumerable<string> BuildRawParamValues(MethodBase member, Dictionary<string, object> paramValues)
        {
            var parameters = member.GetParameters();

            if (member.IsStatic)
                parameters = parameters.Skip(1).ToArray();

            foreach (ParameterInfo parameterInfo in parameters)
            {
                if (paramValues.ContainsKey(parameterInfo.Name))
                    yield return (string)paramValues[parameterInfo.Name];
            }
        }

        private static object ConvertParam(string s, Type t)
        {
            return _parameterConverter.Convert(s, t);
        }
    }
}