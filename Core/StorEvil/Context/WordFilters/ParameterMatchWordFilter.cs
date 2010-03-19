using System.Linq;
using System.Reflection;

namespace StorEvil.Context.WordFilters
{
    /// <summary>
    /// word filter that matches parameters
    /// </summary>
    public class ParameterMatchWordFilter : WordFilter
    {
        private readonly ParameterInfo _paramInfo;

        public ParameterMatchWordFilter(ParameterInfo paramInfo)
        {
            _paramInfo = paramInfo;
        }

        public string ParameterName
        {
            get { return _paramInfo.Name; }
        }

        public bool IsTable
        {
            get
            {
                return _paramInfo.ParameterType.IsArray;
            }
        }

        public bool IsMultipleWordMatcher { get
        {
            return _paramInfo.GetCustomAttributes(typeof (MultipleWordsAttribute), false).Any();
        }}

        public bool IsMatch(string s)
        {          
            return true;
        }

        private bool IsTableString(string paramValue)
        {
            return paramValue.StartsWith("|");
        }
    }
}