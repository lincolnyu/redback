namespace Redback.WebGraph
{
    public interface ICommonGraph : ISiteGraph
    {
        string BaseDirectory { get; }

        void Setup(string baseDirectory, string startHost, GraphObject root);
    }
}
