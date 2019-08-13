namespace Akzin.Crm.DataMigrator.Strategy
{
    public interface IEntityStrategyFactory
    {
        IEntityStrategy Create(string entityLogicalName);
    }
}