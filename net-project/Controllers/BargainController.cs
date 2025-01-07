using System.Data.Odbc;
using Microsoft.AspNetCore.Mvc;
using NetProject.Domain.TransactionAggregates;

namespace net_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BargainController(OdbcConnection dbConnection) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            await dbConnection.OpenAsync();
            var command = new OdbcCommand("SELECT Id, OrderId, UserId, Amount, Status, CreatedAt, UpdatedAt FROM dbo.[Bargain]", dbConnection);
            var reader = await command.ExecuteReaderAsync();
            var bargains = new List<Bargain>();
            while (await reader.ReadAsync())
            {
                var bargain = new Bargain()
                {
                    Id = reader.GetInt32(0),
                    OrderId = reader.GetInt32(1),
                    UserId = reader.GetInt32(2),
                    Amount = reader.GetDecimal(3),
                    Status = reader.GetInt32(4),
                    CreatedAt = reader.GetDateTime(5),
                    UpdatedAt = reader.GetDateTime(6)
                };
                bargains.Add(bargain);
            }
            await dbConnection.CloseAsync();
            return Ok(bargains);
        }
    }
}
