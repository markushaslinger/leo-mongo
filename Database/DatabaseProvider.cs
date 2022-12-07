using MongoDB.Driver;

namespace LeoMongo.Database
{
    internal sealed class DatabaseProvider : IDatabaseProvider
    {
        private readonly MongoClient _client;

        public DatabaseProvider(IMongoConfig options)
        {
            _client = new MongoClient(options.ConnectionString);
            Database = _client.GetDatabase(options.DatabaseName);
        }

        public IMongoDatabase Database { get; }

        public Task<IClientSessionHandle> StartSession() => _client.StartSessionAsync();
    }
}