FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ProjektZespolowyGr3/ProjektZespolowyGr3.csproj ProjektZespolowyGr3/
RUN dotnet restore ProjektZespolowyGr3/ProjektZespolowyGr3.csproj

COPY ProjektZespolowyGr3/ ProjektZespolowyGr3/
RUN dotnet publish ProjektZespolowyGr3/ProjektZespolowyGr3.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p wwwroot/uploads && chmod 777 wwwroot/uploads

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

EXPOSE 5000

ENTRYPOINT ["dotnet", "ProjektZespolowyGr3.dll"]
