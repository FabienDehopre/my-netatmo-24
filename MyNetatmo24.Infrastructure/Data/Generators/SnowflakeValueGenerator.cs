using IdGen;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace MyNetatmo24.Infrastructure.Data.Generators;

public class SnowflakeValueGenerator : ValueGenerator<long>
{
    private readonly IIdGenerator<long> _idGenerator;

    public SnowflakeValueGenerator(IIdGenerator<long> idGenerator)
    {
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    public override long Next(EntityEntry entry) => _idGenerator.CreateId();

    public override bool GeneratesTemporaryValues => false;
}