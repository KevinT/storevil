using System;
using System.Collections.Generic;
using System.IO;
using StorEvil.Core;
using StorEvil.Parsing;

namespace StorEvil.InPlace
{

    public abstract class DriverBase : IStoryHandler
    {
        protected JobResult Result = new JobResult();

        public abstract void HandleStory(Story story);

        public void Finished()
        {
           
        }

        public JobResult GetResult()
        {
            return Result;
        }
    }

    public class AssemblyGenerator
    {
        private readonly IScenarioPreprocessor _scenarioPreprocessor;

        public AssemblyGenerator(IScenarioPreprocessor scenarioPreprocessor)
        {
            _handlerGenerator = new HandlerCodeGenerator(scenarioPreprocessor);
            _compiler = new HandlerCompiler();

            _scenarioPreprocessor = scenarioPreprocessor;
        }

      
        private HandlerCodeGenerator _handlerGenerator;
        private HandlerCompiler _compiler;

        public string GenerateAssembly(Story story, IEnumerable<string> referencedAssemblies)
        {
            string sourceCode = _handlerGenerator.GetSourceCode(story, referencedAssemblies);
            return _compiler.CompileToFile(sourceCode, referencedAssemblies, GetAssemblyLocation());
        }

        private string GetAssemblyLocation()
        {
            string path = Path.GetDirectoryName(GetType().Assembly.Location) + "\\StorEvilTemp" +
                          Guid.NewGuid().ToString().Replace("-", "");

            Directory.CreateDirectory(path);

            return path + "\\StorevilTestAssembly.dll";
        }
    }
}