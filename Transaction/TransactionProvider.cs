using LeoMongo.Database;
using MongoDB.Driver;
using Nito.AsyncEx;

namespace LeoMongo.Transaction
{
    internal sealed class TransactionProvider : ITransactionProvider, ISessionProvider
    {
        private readonly IDatabaseProvider _databaseProvider;
        private readonly AsyncLock _mutex;
        private IClientSessionHandle? _session;

        public TransactionProvider(IDatabaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
            _mutex = new AsyncLock();
        }

        public IClientSessionHandle Session
        {
            get
            {
                if (!InTransaction || _session == null)
                {
                    throw new InvalidOperationException("transaction not started");
                }

                return _session;
            }
        }

        public bool InTransaction { get; private set; }

        public async Task<Transaction> BeginTransaction()
        {
            using (await _mutex.LockAsync())
            {
                if (InTransaction)
                {
                    throw new InvalidOperationException("transaction already started");
                }

                _session = await _databaseProvider.StartSession();
                _session.StartTransaction();
                InTransaction = true;
                return new Transaction(_session);
            }
        }
    }
}