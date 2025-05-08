using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using NuGet.Protocol;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task DoSomethingAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 1);
            command.Parameters.AddWithValue("@Name", "Animal1");
        
            await command.ExecuteNonQueryAsync();
        
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 2);
            command.Parameters.AddWithValue("@Name", "Animal2");
        
            await command.ExecuteNonQueryAsync();
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task<int> ProcedureAsync(int idProduct, int idWarehouse, int amount,DateTime createdAt)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            command.CommandText = "AddProductToWarehouse";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@IdProduct", idProduct);
            command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);
            var result = await command.ExecuteScalarAsync();
            await transaction.CommitAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            Console.Write(e.Message);
            return -1;
        }
        
    }

    public async Task<bool> DoProductExist(int id)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();
        command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", id);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<bool> DoWarehouseExist(int id)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();
        command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        command.Parameters.AddWithValue("@IdWarehouse", id);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }
 
    public async Task<bool> DoOrderExist(int id, int amount)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();
        command.CommandText = "SELECT 1 FROM [Order] WHERE IdProduct = @IdProduct and Amount = @Amount";
        command.Parameters.AddWithValue("@IdProduct", id);
        command.Parameters.AddWithValue("@Amount", amount);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<int> GetOrderID(int id, DateTime date)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();
        command.CommandText = "SELECT CreatedAt, IdOrder FROM [Order] WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", id);
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")).ToUniversalTime();
            int idOrder = reader.GetInt32(reader.GetOrdinal("IdOrder"));

            if (createdAt < date.ToUniversalTime())
            {
                return idOrder;
            }   
        }
        return -1;
    }   

    public async Task<bool> IsRealized(int id)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();
        command.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
        command.Parameters.AddWithValue("@IdOrder", id);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<bool> UpdateFullfilledAt(int id)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            command.CommandText = "UPDATE [Order] SET FulfilledAt = @FulfilledAt Where IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@FulfilledAt", DateTime.Now);
            command.Parameters.AddWithValue("@IdOrder", id);
            var res = await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return res != 0;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw e;
        }
    }

    public async Task<int> InsertProductWarehouse(int IdWarehouse, int IdProduct, int IdOrder, int Amount)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();
        
        command.CommandText = "Select Price from Product Where IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", IdProduct);
        var price =  await command.ExecuteScalarAsync();
        command.Parameters.Clear();
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;
        try
        {
            command.CommandText =
                "Insert Into Product_Warehouse VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount,@Price,@CreatedAt)  Select SCOPE_IDENTITY();";
            command.Parameters.AddWithValue("@IdWarehouse", IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", IdProduct);
            command.Parameters.AddWithValue("@IdOrder", IdOrder);
            command.Parameters.AddWithValue("@Amount", Amount);
            command.Parameters.AddWithValue("@Price", Convert.ToDecimal(price) * Amount);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

            var res = await command.ExecuteScalarAsync();
            await transaction.CommitAsync();
            return Convert.ToInt32(res);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(); 
            Console.Write(e.Message);
            return -1;
        }
    }
}