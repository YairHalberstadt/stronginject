using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StrongInject.Samples.AspNetCore.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrongInject.Samples.AspNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersCache _usersCache;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUsersCache usersCache, ILogger<UsersController> logger)
        {
            _usersCache = usersCache;
            _logger = logger;
        }

        [HttpGet]
        public ValueTask<IEnumerable<User>> Get()
        {
            _logger.LogInformation($"Requesting users");
            return _usersCache.GetUsersList();
        }
    }
}
