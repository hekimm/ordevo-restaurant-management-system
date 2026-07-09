using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace Ordevo.BuildingBlocks.Data;

public sealed class OracleParams : SqlMapper.IDynamicParameters
{
    private readonly DynamicParameters _inner;

    public OracleParams(object? template = null) => _inner = new DynamicParameters(template);

    public OracleParams Add(string name, object? value)
    {
        _inner.Add(name, value);
        return this;
    }

    public T Get<T>(string name) => _inner.Get<T>(name);

    void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
    {
        if (command is OracleCommand oracleCommand)
            oracleCommand.BindByName = true;

        ((SqlMapper.IDynamicParameters)_inner).AddParameters(command, identity);
    }
}
