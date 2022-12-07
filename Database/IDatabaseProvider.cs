using MongoDB.Driver;

namespace LeoMongo.Database
{
    public interface IDatabaseProvider
    {
        IMongoDatabase Database { get; }
        Task<IClientSessionHandle> StartSession();
    }
}