using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Server.ExampleDataServices;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmericanStatesController : ControllerBase
    {
        [HttpGet("{search}")]
        public IEnumerable<string> Get(string search)
        {
            return AmericanStates.GetStates(search);
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return AmericanStates.GetStates();
        }
    }
}
