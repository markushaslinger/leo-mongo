using LeoMongo.Database;
using LeoMongo.Transaction;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LeoMongo
{
    public static class Extensions
    {
        public static void AddLeoMongo<T>(this IServiceCollection services) where T : class, IMongoConfig
        {
            services.AddScoped<IDatabaseProvider, DatabaseProvider>();
            services.AddScoped<ITransactionProvider, TransactionProvider>();

            services.AddSingleton<IMongoConfig, T>();
        }

        public static async Task<IDictionary<TMasterField, List<TDetailField>?>> ToDictionaryAsync<TMasterField, TDetailField>(
            this IMongoQueryable<MasterDetails<TMasterField, TDetailField>> self) where TMasterField : notnull
        {
            var list = await self.ToListAsync();
            var dic = list.ToDictionary(md => md.Master,
                md => md.Details?.ToList());
            return dic;
        }

        internal static string FirstLetterLowerCase(this string self)
        {
            if (string.IsNullOrWhiteSpace(self))
            {
                throw new ArgumentNullException(nameof(self));
            }

            var arr = self.ToCharArray();
            arr[0] = char.ToLowerInvariant(arr[0]);
            return new string(arr);
        }
    }
}