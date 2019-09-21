using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace DatabaseCaseExtractor.Logger
{
    [ProviderAlias("DatabaseExtractor")]
    public class DatabaseCaseExtractorLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopeProvider;
        private readonly ConcurrentDictionary<string, DatabaseCaseExtractorLogger> _loggers = new ConcurrentDictionary<string, DatabaseCaseExtractorLogger>();
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName,
                new DatabaseCaseExtractorLogger());
        }

        public void Dispose()
        {
            
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }
    }
}
