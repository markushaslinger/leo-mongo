namespace LeoMongo.Database
{
    public sealed class MasterDetails<TMaster, TDetail>
    {
        public required TMaster Master { get; set; }
        public IEnumerable<TDetail>? Details { get; set; }
    }
}