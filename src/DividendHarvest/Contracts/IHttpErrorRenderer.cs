using DividendHarvest.Application.Localization;

namespace DividendHarvest.Contracts;

public interface IHttpErrorRenderer
{
    ValueTask<bool> RenderAsync(
        HttpContext httpContext,
        LocalizedApplicationError error,
        CancellationToken cancellationToken);
}
