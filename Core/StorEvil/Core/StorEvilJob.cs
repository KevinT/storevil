namespace StorEvil.Core
{
    /// <summary>
    /// Runs the process of mapping stories, building code, etc.
    /// </summary>
    public class StorEvilJob : IStorEvilJob
    {
        public IStoryProvider StoryProvider { get; set; }
        public IStoryHandler Handler { get; set; }

        public StorEvilJob(
            IStoryProvider storyProvider,
            IStoryHandler handler)

        {
            StoryProvider = storyProvider;
            Handler = handler;
        }

        public int Run()
        {       
            Handler.HandleStories(StoryProvider.GetStories());
            

            Handler.Finished();
            var results = Handler.GetResult();

            return results.Failed;
        }
    }

    public interface IStorEvilJob
    {
        int Run();
    }
}