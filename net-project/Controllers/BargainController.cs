using Microsoft.AspNetCore.Mvc;
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

        [HttpPut]
        public IActionResult Deposit()
        {
            return Ok();
        }
        
        [HttpPut]
        public IActionResult Withdraw()
        {
            return Ok();
        }
        
        [HttpPut]
        public IActionResult Transfer()
        {
            return Ok();
        }
    }
}
