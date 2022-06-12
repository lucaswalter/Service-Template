namespace Service.Data.Domain.Core;

public interface IVersionedEntity
{
    int Version { get; set; }
}

public class VersionedEntity : IVersionedEntity
{
    public int Version { get; set; }
}
