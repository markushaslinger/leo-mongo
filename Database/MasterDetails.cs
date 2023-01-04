namespace LeoMongo.Database
{
    public sealed class MasterDetails<TMaster, TDetail>
    {
        public TMaster Master { get; set; } = default!;
        public IEnumerable<TDetail>? Details { get; set; }
    }
}