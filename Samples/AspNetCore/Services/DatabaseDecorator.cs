using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrongInject.Samples.AspNetCore.Services
{
    public class DatabaseDecorator : IDatabase
    {
        private readonly IDatabase _underlying;
        private readonly ILogger<DatabaseDecorator> _logger;

        public DatabaseDecorator(IDatabase underlying, ILogger<DatabaseDecorator> logger)
        {
            _underlying = underlying;
            _logger = logger;
        }

        public async Task<IEnumerable<T>> Get<T>()
        {
            _logger.LogInformation($"Requesting {typeof(T).Name}s from database...");

            try
            {
                var results = await _underlying.Get<T>();
                _logger.LogInformation($"Successfully recieved {results.Count()} {typeof(T).Name}s from database");
                return results;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when requesting {typeof(T).Name}s from database: {e}");
                throw;
            }

        }
    }
}
