using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StorEvil.Context.Matchers
{
    public class ContextMemberNameSplitter
    {
        private static readonly Regex SplitMemberNameRegex = new Regex("[A-Z]?[a-z]*");

        public IEnumerable<string> SplitMemberName(string name)
        {
            return name.Contains("_") ? 
                SplitAtUnderscores(name) : 
                SplitAtCamelCaseBoundaries(name);
        }

        private static IEnumerable<string> SplitAtCamelCaseBoundaries(string name)
        {
            var matches = SplitMemberNameRegex.Matches(name);

            foreach (Match m in matches)
                if (!string.IsNullOrEmpty(m.Value.Trim()))
                    yield return m.Value.Trim();
        }

        private static IEnumerable<string> SplitAtUnderscores(string name)
        {
            return name.Split('_').Select(x=> x.Trim()).Where( x=> !string.IsNullOrEmpty( x));
        }
    }
}