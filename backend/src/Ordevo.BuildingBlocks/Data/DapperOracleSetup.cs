using System.Data;
using Dapper;

namespace Ordevo.BuildingBlocks.Data;

public static class DapperOracleSetup
{
    private static bool _applied;

    public static void Apply()
    {
        if (_applied) return;
        _applied = true;

        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new BoolTypeHandler());
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
    }

    private sealed class BoolTypeHandler : SqlMapper.TypeHandler<bool>
    {
        public override bool Parse(object value) => value is not null && Convert.ToInt32(value) != 0;

        public override void SetValue(IDbDataParameter parameter, bool value)
        {
            parameter.DbType = DbType.Int32;
            parameter.Value = value ? 1 : 0;
        }
    }

    private sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value) => value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => DateTimeOffset.Parse(value.ToString()!)
        };

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
            => parameter.Value = value.UtcDateTime;
    }
}
