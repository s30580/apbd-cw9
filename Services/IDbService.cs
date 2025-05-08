namespace Tutorial9.Services;

public interface IDbService
{
    Task DoSomethingAsync();
    Task<int> ProcedureAsync(int idProduct, int idWarehouse, int amount,DateTime createdAt);

    Task<bool> DoProductExist(int id);
    Task<bool> DoWarehouseExist(int id);
    Task<bool> DoOrderExist(int id, int amount);
    
    Task<int> GetOrderID(int id, DateTime createdAt);
    Task<bool> IsRealized(int id);

    Task<bool> UpdateFullfilledAt(int id);

    Task<int> InsertProductWarehouse(int IdWarehouse,int IdProduct,int IdOrder,int Amount);
}