FROM node:24-alpine AS frontend-build

WORKDIR /workspace/src/DividendHarvest.Web
COPY src/DividendHarvest.Web/package.json src/DividendHarvest.Web/pnpm-lock.yaml ./
RUN corepack enable && pnpm install --frozen-lockfile
COPY src/DividendHarvest.Web/ ./
RUN pnpm build

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend-build

WORKDIR /workspace
COPY . .
COPY --from=frontend-build /workspace/src/DividendHarvest/wwwroot ./src/DividendHarvest/wwwroot
RUN dotnet restore DividendHarvest.slnx
RUN dotnet publish src/DividendHarvest/DividendHarvest.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0
RUN mkdir -p /app/data
COPY --from=backend-build /app/publish ./
VOLUME ["/app/data"]
EXPOSE 8080
ENTRYPOINT ["dotnet", "DividendHarvest.dll"]
