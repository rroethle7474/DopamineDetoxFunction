namespace DopamineDetoxFunction.Services
{
    public interface ITwitterEmbedService
    {
       public Task<string> GetHtmlEmbeddingAsync(string url);
    }
}
