//using BingXBotApi.DAL.Ctx;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace BingXBotApi.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class BingXController : ControllerBase
//    {
//        private readonly BingXService _bingXService;

//        public BingXController(BingXService bingXService)
//        {
//            _bingXService = bingXService;
//        }

//        [HttpGet("symbols")]
//        public async Task<IActionResult> GetSymbols()
//        {
//            var symbols = await _bingXService.GetSymbolsAsync();
//            return Ok(symbols);
//        } 
        
//        [HttpGet("FirstPairBuy")]
//        public async Task<IActionResult> GetFirstPairBuy([FromServices] AppDBContext appDBContext)
//        {
//            var symbols = await appDBContext.BingXBotTradePrace.ToListAsync();
//            return Ok(symbols);
//        }

//        [HttpPost("place-order")]
//        public async Task<IActionResult> PlaceMarketOrder([FromQuery] string symbol, [FromQuery] string side, [FromQuery] double quoteOrderQty)
//        {
//            //var response = await _bingXService.PlaceMarketOrderAsync(symbol, side, quoteOrderQty);
//            var response = await _bingXService.PlaceMarketOrderAsync(symbol);
//            return Ok(response);
//        }
//    }


//    //[ApiController]
//    //[Route("[controller]")]
//    //public class WeatherForecastController : ControllerBase
//    //{
//    //    private static readonly string[] Summaries = new[]
//    //    {
//    //        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//    //    };

//    //    private readonly ILogger<WeatherForecastController> _logger;

//    //    public WeatherForecastController(ILogger<WeatherForecastController> logger)
//    //    {
//    //        _logger = logger;
//    //    }

//    //    [HttpGet(Name = "GetWeatherForecast")]
//    //    public IEnumerable<WeatherForecast> Get()
//    //    {
//    //        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
//    //        {
//    //            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//    //            TemperatureC = Random.Shared.Next(-20, 55),
//    //            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
//    //        })
//    //        .ToArray();
//    //    }
//    //}
//}
