using System.Linq.Expressions;
using LeoMongo.Transaction;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LeoMongo.Database
{
    public abstract class RepositoryBase<T> : IRepositoryBase where T : EntityBase
    {
        private readonly IDatabaseProvider _databaseProvider;
        private readonly ITransactionProvider _transactionProvider;
        private IMongoCollection<T>? _collection;

        protected RepositoryBase(ITransactionProvider transactionProvider,
            IDatabaseProvider databaseProvider)
        {
            _transactionProvider = transactionProvider;
            _databaseProvider = databaseProvider;
        }

        private IMongoCollection<T> Collection =>
            _collection ??= GetCollection<T>(CollectionName);

        private IClientSessionHandle Session
        {
            get
            {
                var sessionProvider = (ISessionProvider) _transactionProvider;
                return sessionProvider.Session;
            }
        }

        protected UpdateDefinitionBuilder<T> UpdateDefBuilder => new();

        public abstract string CollectionName { get; }

        protected IMongoQueryable<T> Query()
        {
            if (_transactionProvider.InTransaction)
            {
                return Collection.AsQueryable(Session);
            }

            return Collection.AsQueryable();
        }

        protected IMongoQueryable<MasterDetails<ObjectId, ObjectId>> QueryIncludeDetail<TDetail>(
            IRepositoryBase detailRepository,
            Expression<Func<TDetail, ObjectId>> foreignKeySelector,
            Expression<Func<T, bool>>? masterFilter = null)
            where TDetail : EntityBase
        {
            return QueryIncludeDetail(detailRepository, foreignKeySelector, (master, details) =>
                new MasterDetails<ObjectId, ObjectId>
                {
                    Master = master.Id,
                    Details = details.Select(d => d.Id)
                }, masterFilter);
        }

        protected IMongoQueryable<MasterDetails<TMasterField, TDetailField>> QueryIncludeDetail<TDetail, TMasterField,
            TDetailField>(
            IRepositoryBase detailRepository,
            Expression<Func<TDetail, ObjectId>> foreignKeySelector,
            Expression<Func<T, IEnumerable<TDetail>, MasterDetails<TMasterField, TDetailField>>> resultSelector,
            Expression<Func<T, bool>>? masterFilter = null)
            where TDetail : EntityBase
        {
            if (typeof(TMasterField) == typeof(T)
                || typeof(TDetailField) == typeof(TDetail))
            {
                throw new NotSupportedException("A projection must not include the document itself (MongoDB Driver limitation)");
            }

            // automatically applies transaction
            IMongoQueryable<T> query = Query();

            if (masterFilter != null)
            {
                query = query.Where(masterFilter);
            }

            var joinedQuery = query
                .GroupJoin(GetCollection<TDetail>(detailRepository.CollectionName), m => m.Id,
                    foreignKeySelector, resultSelector);
            return joinedQuery;
        }

        // it WOULD be awesome to get the master and detail documents in such a way
        // BUT a Projection (in MongoDB Driver) MAY NOT include the document itself, so this will lead to an error
        // see https://stackoverflow.com/questions/47383632/project-or-group-does-not-support-document
        //
        //protected IMongoQueryable<MasterDetails<T, TDetail>> QueryIncludeDetail<TDetail>(
        //    IRepositoryBase detailRepository,
        //    Expression<Func<TDetail, ObjectId>> foreignKeySelector, Expression<Func<T, bool>>? masterFilter = null)
        //    where TDetail : EntityBase
        //{
        //    // automatically applies transaction
        //    IMongoQueryable<T> query = Query();

        //    if (masterFilter != null)
        //    {
        //        query = query.Where(masterFilter);
        //    }

        //    IMongoQueryable<MasterDetails<T, TDetail>> joinedQuery = query
        //        .GroupJoin(GetCollection<TDetail>(detailRepository.CollectionName), m => m.Id, foreignKeySelector,
        //            (master, details) => new MasterDetails<T, TDetail>
        //            {
        //                Master = master,
        //                Details = details
        //            });
        //    return joinedQuery;
        //}

        protected async Task<T> InsertOneAsync(T document)
        {
            if (_transactionProvider.InTransaction)
            {
                await Collection.InsertOneAsync(Session, document);
            }
            else
            {
                await Collection.InsertOneAsync(document);
            }

            return document;
        }

        protected async Task<IReadOnlyCollection<T>> InsertManyAsync(IReadOnlyCollection<T> documents)
        {
            if (_transactionProvider.InTransaction)
            {
                await Collection.InsertManyAsync(Session, documents);
            }
            else
            {
                await Collection.InsertManyAsync(documents);
            }

            return documents;
        }

        protected Task<UpdateResult> UpdateOneAsync(ObjectId id, UpdateDefinition<T> updateDefinition)
        {
            return _transactionProvider.InTransaction
                ? Collection.UpdateOneAsync(Session, GetIdFilter(id), updateDefinition)
                : Collection.UpdateOneAsync(GetIdFilter(id), updateDefinition);
        }

        protected Task<UpdateResult> UpdateManyAsync(Expression<Func<T, bool>> filter,
            UpdateDefinition<T> updateDefinition)
        {
            return _transactionProvider.InTransaction
                ? Collection.UpdateManyAsync(Session, filter, updateDefinition)
                : Collection.UpdateManyAsync(filter, updateDefinition);
        }

        protected Task<ReplaceOneResult> ReplaceOneAsync(T document)
        {
            var id = document.Id;
            return _transactionProvider.InTransaction
                ? Collection.ReplaceOneAsync(Session, GetIdFilter(id), document)
                : Collection.ReplaceOneAsync(GetIdFilter(id), document);
        }

        protected Task<DeleteResult> DeleteOneAsync(ObjectId id)
        {
            return _transactionProvider.InTransaction
                ? Collection.DeleteOneAsync(Session, GetIdFilter(id))
                : Collection.DeleteOneAsync(GetIdFilter(id));
        }

        protected Task<DeleteResult> DeleteManyAsync(Expression<Func<T, bool>> filter)
        {
            return _transactionProvider.InTransaction
                ? Collection.DeleteManyAsync(Session, filter)
                : Collection.DeleteManyAsync(filter);
        }
        
        protected IAggregateFluent<T> Aggregate()
        {
            return Collection.Aggregate();
        }
        
        protected IAggregateFluent<TResponse> GraphLookup<TCollection,TResponse>(
            IRepositoryBase fromRepository,
            FieldDefinition<TCollection, string> connectFromField, 
            FieldDefinition<TCollection, string> connectToField,
            AggregateExpressionDefinition<T,string> startWith,
            FieldDefinition<TResponse, IEnumerable<T>> exportField,
            String? depthField = null
        )
        {
            return Collection.Aggregate()
                .GraphLookup<TCollection, string, string, string, T, IEnumerable<T>, TResponse>(
                    from: GetCollection<TCollection>(fromRepository.CollectionName),
                    connectFromField: connectFromField,
                    connectToField: connectToField,
                    startWith: startWith,
                    @as: exportField,
                    depthField: depthField);
        }

        private IMongoCollection<TCollection> GetCollection<TCollection>(string collectionName) =>
            _databaseProvider.Database.GetCollection<TCollection>(collectionName);

        private static Expression<Func<T, bool>> GetIdFilter(ObjectId id) => t => t.Id == id;
    }
}