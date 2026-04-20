using Microsoft.AspNetCore.Mvc;

namespace ToolMng.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToolAgentStatusController : ControllerBase
    {
        private bool _isServiceRunning = false;

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var host = HttpContext.Request.Host.Value;
            var scheme = HttpContext.Request.Scheme;
            var serviceAddress = $"{scheme}://{host}";
            Console.WriteLine($"当前服务地址: {serviceAddress}");
            return Ok(new { Scheme = scheme, Host = host, IsRunning = _isServiceRunning });
        }

        [HttpPost("start")]
        public IActionResult StartService()
        {
            _isServiceRunning = true;
            return Ok("Service started.");
        }

        [HttpPost("stop")]
        public IActionResult StopService()
        {
            _isServiceRunning = false;
            return Ok("Service stopped.");
        }
    }
}
