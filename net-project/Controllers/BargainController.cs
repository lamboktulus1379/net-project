using Microsoft.AspNetCore.Mvc;
using NetProject.Domain.DataTransferObjects;
using NetProject.Usecase;

namespace net_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BargainController(IBargainUsecase bargainUsecase) : ControllerBase
    {
        [HttpGet("histories")]
        public async Task<IActionResult> GetHistories([FromQuery] string accountId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            endDate = endDate.AddHours(23).AddHours(59).AddMinutes(50);
            var histories = await bargainUsecase.GetHistories(accountId, startDate, endDate);
            return Ok(histories);
        }

        [HttpPut("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositForCreation depositForCreation)
        {
            var res = await bargainUsecase.Deposit(depositForCreation.AccountId, depositForCreation.Amount);
            return Ok(res);
        }
        
        [HttpPut]
        public IActionResult Withdraw()
        {
            return Ok();
        }
        
        [HttpPut("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferForCreation transferForCreation)
        {
            var res = await bargainUsecase.Transfer(transferForCreation.From, transferForCreation.To, transferForCreation.Amount);
            return Ok(res);
        }
    }
}
