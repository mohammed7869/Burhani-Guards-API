using BurhaniGuards.Api.BusinessModel;
using Dapper;
using Dapper.Contrib.Extensions;

namespace BurhaniGuards.Api.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> Search(string _defaultOrderBy = "Id DESC", string whereCondition = "", object? searchParams = null);
    Task<T?> Retrieve(long id);
    Task<int> Insert(T obj);
    Task<bool> Update(T obj);
    Task<bool> Delete(long id);
    Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, DynamicParameters? parameters = null);
    Task<IEnumerable<T>?> FindOneByField(Dictionary<string, object> fieldValues);
}

public class GenericRepository<T> : IGenericRepository<T> where T : BaseModel
{
    private readonly DapperContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _tableName;

    public GenericRepository(DapperContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;

        var tableAttribute = typeof(T).GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute), false)
                   .FirstOrDefault() as System.ComponentModel.DataAnnotations.Schema.TableAttribute;
        _tableName = tableAttribute?.Name ?? typeof(T).Name;
    }

    private object? TableName => typeof(T).GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute), false).FirstOrDefault();

    public async Task<IEnumerable<T>> Search(string defaultOrderBy, string whereCondition, object? searchParams)
    {
        string? tableName = null;
        if (TableName != null)
        {
            tableName = ((System.ComponentModel.DataAnnotations.Schema.TableAttribute)TableName).Name;
        }

        using (var connection = _context.CreateConnection())
        {
            string sql = $"SELECT * FROM {tableName} WHERE {whereCondition} ORDER BY {defaultOrderBy}";
            return await connection.QueryAsync<T>(sql, searchParams);
        }
    }

    public async Task<T?> Retrieve(long id)
    {
        using (var connection = _context.CreateConnection())
        {
            var result = await connection.GetAsync<T>(id);
            return result;
        }
    }

    public async Task<int> Insert(T obj)
    {
        using (var connection = _context.CreateConnection())
        {
            return await connection.InsertAsync(obj);
        }
    }

    public async Task<bool> Update(T obj)
    {
        using (var connection = _context.CreateConnection())
        {
            var exists = await connection.GetAsync<T>(obj.Id);
            if (exists == null)
                return false;
            return await connection.UpdateAsync(obj);
        }
    }

    public async Task<bool> Delete(long id)
    {
        using (var connection = _context.CreateConnection())
        {
            var exists = await connection.GetAsync<T>(id);
            if (exists == null)
                return false;
            return await connection.DeleteAsync(exists);
        }
    }

    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, DynamicParameters? parameters = null)
    {
        using (var connection = _context.CreateConnection())
        {
            return await connection.QueryAsync<TResult>(sql, parameters);
        }
    }

    public async Task<IEnumerable<T>?> FindOneByField(Dictionary<string, object> fieldValues)
    {
        if (fieldValues == null || !fieldValues.Any())
            return null;

        using var connection = _context.CreateConnection();

        // Construct WHERE clause dynamically
        var whereClause = string.Join(" AND ", fieldValues.Select(f => $"{f.Key} = @{f.Key}"));

        string sql = $"SELECT * FROM {_tableName} WHERE {whereClause}";

        var parameters = new DynamicParameters();
        foreach (var fieldValue in fieldValues)
        {
            parameters.Add($"@{fieldValue.Key}", fieldValue.Value);
        }

        return await connection.QueryAsync<T>(sql, parameters);
    }
}

