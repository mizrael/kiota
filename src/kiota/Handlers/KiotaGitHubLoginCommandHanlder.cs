using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.Authentication.DeviceCode;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace kiota.Handlers;

internal class KiotaGitHubLoginCommandHandler : BaseKiotaCommandHandler
{
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        CancellationToken cancellationToken = (CancellationToken)context.BindingContext.GetService(typeof(CancellationToken));
        var (loggerFactory, logger) = GetLoggerAndFactory<GitHubAuthenticationProvider>(context);
        using (loggerFactory) {
            try {
                return await LoginAsync(logger, cancellationToken);
            } catch (Exception ex) {
    #if DEBUG
                logger.LogCritical(ex, "error signing in to GitHub: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
    #else
                logger.LogCritical("error signing in to GitHub: {exceptionMessage}", ex.Message);
                return 1;
    #endif
            }
            
        }
    }
    private async Task<int> LoginAsync(ILogger logger, CancellationToken cancellationToken) {
        var authenticationProvider = new GitHubAuthenticationProvider(Configuration.Search.GitHub.AppId,
                                                                        "repo",
                                                                        new List<string> { "api.github.com"},
                                                                        httpClient,
                                                                        DisplayGitHubDeviceCodeLoginMessage,
                                                                        logger);
        var dummyRequest = new RequestInformation() {
            HttpMethod = Method.GET,
            URI = new Uri("https://api.github.com/user"),
        };
        await authenticationProvider.AuthenticateRequestAsync(dummyRequest, cancellationToken: cancellationToken);
        if(dummyRequest.Headers.TryGetValue("Authorization", out var authHeaderValue) && authHeaderValue is string authHeader && authHeader.StartsWith("bearer", StringComparison.OrdinalIgnoreCase)) {
            DisplaySuccess("Authentication successful.");
            DisplaySearchBasicHint();
            DisplayGitHubLogoutHint();
            return 0;
        } else {
            DisplayError("Authentication failed. Please try again.");
            return 1;
        }
    }
}
