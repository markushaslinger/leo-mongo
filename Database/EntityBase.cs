using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LeoMongo.Database
{
    public abstract class EntityBase
    {
        [BsonId] public ObjectId Id { get; set; }
    }
}