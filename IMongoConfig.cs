namespace LeoMongo
{
    public interface IMongoConfig
    {
        string ConnectionString { get; }
        string DatabaseName { get; }
    }
}