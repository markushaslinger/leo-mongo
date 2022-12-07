using MongoDB.Driver;

namespace LeoMongo.Transaction
{
    public sealed class Transaction : IDisposable
    {
        private readonly IClientSessionHandle _session;
        private bool _committed;

        public Transaction(IClientSessionHandle session)
        {
            _session = session;
        }

        public void Dispose()
        {
            if (!_committed)
            {
                // we usually don't want to die during dispose, so try/catch & log would be nice here
                _session.AbortTransaction();
            }

            _session.Dispose();
        }

        public async Task CommitAsync()
        {
            await _session.CommitTransactionAsync();
            _committed = true;
        }

        public Task RollbackAsync()
        {
            return _session.AbortTransactionAsync();
        }
    }
}