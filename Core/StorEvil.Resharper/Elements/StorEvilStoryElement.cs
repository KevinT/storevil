using JetBrains.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework;
using StorEvil.Configuration;

namespace StorEvil.Resharper.Elements
{
    public class StorEvilStoryElement : StorEvilUnitTestElement
    {
        public ConfigSettings Config { get; set; }
        public string Id { get; set; }
        private readonly UnitTestNamespace _namespace;

        public StorEvilStoryElement(StorEvilTestProvider provider, UnitTestElement parent, IProject project, string title, ConfigSettings config, string id)
            : base(provider, parent, project, title)
        {
            Config = config;
            Id = id;
            _namespace = new UnitTestNamespace(project.Name + " " + title);
        }

        public override UnitTestNamespace GetNamespace()
        {
            return _namespace;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as StorEvilStoryElement);
        }

        public bool Equals(StorEvilStoryElement other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(other._namespace, _namespace) && Equals(other.Id, Id);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result*397) ^ (_namespace != null ? _namespace.GetHashCode() : 0);
                result = (result*397) ^ (Id != null ? Id.GetHashCode() : 0);
                return result;
            }
        }
    }
}