namespace Redback.WebGraph
{
    public interface ICommonGraph : ISiteGraph
    {
        string BaseDirectory { get; }

        GraphObject RootObject { get; }

        void Setup(string baseDirectory, GraphObject root);

        void SetStartHost(string startHost);
    }
}
