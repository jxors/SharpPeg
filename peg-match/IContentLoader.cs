namespace PegMatch
{
    interface IContentLoader
    {
        string Name { get; }

        ContentCharData ReadAllChars();
    }
}