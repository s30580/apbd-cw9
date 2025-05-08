using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : Controller
{
    private readonly IDbService _dbService;
    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }
    
    [HttpPost]
    public async Task<IActionResult> NewWareHouseProduct([FromBody] WarehouseProcuct wp)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        if (!await _dbService.DoProductExist(wp.IdProduct))
        {
            return NotFound($"Product with ID: {wp.IdProduct} not found");
        }

        if (!await _dbService.DoWarehouseExist(wp.IdWarehouse))
        {
            return NotFound($"Warehouse with ID: {wp.IdWarehouse} not found");
        }
        
        if (!await _dbService.DoOrderExist(wp.IdProduct, wp.Amount))
        {
            return NotFound($"Order with ID: {wp.IdProduct} and Amount: {wp.Amount} not found");
        }
        
        int idOrder = await _dbService.GetOrderID(wp.IdProduct, wp.CreatedAt);
        if (idOrder == -1)
        {
            return BadRequest("Wrong date, Provided date must be later than the order creation date.");
        }

        if (await _dbService.IsRealized(idOrder))
        {
            return Conflict($"Order with ID: {idOrder} exists");
        }

        if (!await _dbService.UpdateFullfilledAt(idOrder))
        {
            return StatusCode(500,"Internal server error when updating order");
        }
        var result = await _dbService.InsertProductWarehouse(wp.IdWarehouse,wp.IdProduct,idOrder,wp.Amount);
        if (result == -1)
        {
            return StatusCode(500,"Internal server error when inserting");
        }
        return StatusCode(201,result);  
    }

    [HttpPost("procedure")]
    public async Task<IActionResult> ProcedureAsync([FromBody] WarehouseProcuct wp)
    {
        var result = await _dbService.ProcedureAsync(wp.IdProduct,wp.IdWarehouse,wp.Amount, wp.CreatedAt);
        if(result==-1)return StatusCode(500,"Inernal procedure error");
        return StatusCode(201,result);  
    }
}