//using Microsoft.AspNetCore.Mvc;
//using System.Threading.Tasks;

//[ApiController]
//[Route("api/[controller]")]
//public class BotController : ControllerBase
//{
//    private readonly BotService _botService;

//    public BotController(BotService botService)
//    {
//        _botService = botService;
//    }

//    [HttpPost("notify")]
//    public async Task<IActionResult> Notify([FromQuery] string message)
//    {
//        await _botService.SendNotificationAsync(message);
//        return Ok();
//    }
//}