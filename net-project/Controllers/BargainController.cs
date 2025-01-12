using Microsoft.AspNetCore.Mvc;
using NetProject.Domain.DataTransferObjects;
using NetProject.Domain.TransactionAggregates;
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
        
        [HttpPut("withdraw")]
        public async Task<IActionResult> Deposit([FromBody] WithdrawForCreation withdrawForCreation)
        {
            var res = await bargainUsecase.Withdraw(withdrawForCreation.AccountId, withdrawForCreation.Amount);
            return Ok(res);
        }
        
        [HttpPut("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferForCreation transferForCreation)
        {
            var tos = new List<To>();
            foreach (var toCreation in transferForCreation.Tos)
            {
                var to = new To();
                if (toCreation.Currency == "IDR")
                {
                    to.AccountId = toCreation.AccountId;
                    to.Amount = toCreation.Amount;
                    to.Currency = Currency.IDR;
                }
                else if (toCreation.Currency == "USD")
                {
                    to.AccountId = toCreation.AccountId;
                    to.Amount = toCreation.Amount;
                    to.Currency = Currency.USD;
                }
                else if (toCreation.Currency == "SGD")
                {
                    to.AccountId = toCreation.AccountId;
                    to.Amount = toCreation.Amount;
                    to.Currency = Currency.SGD;
                }
                else
                {
                    return NotFound("Currency not found");
                }
                tos.Add(to);
            }
            var res = await bargainUsecase.Transfer(transferForCreation.From, tos.ToArray());
            return Ok(res);
        }
    }
}
