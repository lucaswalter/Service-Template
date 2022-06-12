using NodaTime;

namespace Service.Data.Domain.Core;

public interface IAuditableModel
{
    Instant CreatedAt { get; set; }
    Instant UpdatedAt { get; set; }
}

public class AuditableEntity : IAuditableModel
{
    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
}
