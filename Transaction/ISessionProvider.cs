using MongoDB.Driver;

namespace LeoMongo.Transaction
{
    internal interface ISessionProvider
    {
        IClientSessionHandle Session { get; }
    }
}