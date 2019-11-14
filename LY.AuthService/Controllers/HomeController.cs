using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LY.AuthService.Controllers
{
    [Route("authservice/api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 获取当前环境变量
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetEnvironment")]
        public string GetEnvironment()
        {
            return _webHostEnvironment.EnvironmentName;
        }
    }
}