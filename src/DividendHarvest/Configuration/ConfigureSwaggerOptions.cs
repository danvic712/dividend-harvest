using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DividendHarvest.Configuration;

public sealed class ConfigureSwaggerOptions(
    IApiVersionDescriptionProvider apiVersionDescriptionProvider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = "Dividend Harvest API",
                    Version = description.ApiVersion.ToString(),
                    Description = description.IsDeprecated
                        ? "此 API 版本已弃用，请迁移到更新版本。"
                        : "A 股股息交易参考 API。"
                });
        }
    }
}
