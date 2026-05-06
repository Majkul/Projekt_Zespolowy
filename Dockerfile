FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ProjektZespolowyGr3/ProjektZespolowyGr3.csproj ProjektZespolowyGr3/
RUN dotnet restore ProjektZespolowyGr3/ProjektZespolowyGr3.csproj

COPY ProjektZespolowyGr3/ ProjektZespolowyGr3/
RUN dotnet publish ProjektZespolowyGr3/ProjektZespolowyGr3.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

RUN mkdir -p wwwroot/uploads && chown -R appuser:appgroup wwwroot/uploads

COPY --from=build --chown=appuser:appgroup /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/ || exit 1

USER appuser

ENTRYPOINT ["dotnet", "ProjektZespolowyGr3.dll"]
