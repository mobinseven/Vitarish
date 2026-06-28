FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Directory.Build.props src/Directory.Packages.props ./
COPY src/Hosts/Main/Main.csproj src/Hosts/Main/
COPY src/Modules/Authentication/Api/Authentication.Api.csproj src/Modules/Authentication/Api/
COPY src/Modules/Authentication/Application/Authentication.Application.csproj src/Modules/Authentication/Application/
COPY src/Modules/Authentication/Contracts/Authentication.Contracts.csproj src/Modules/Authentication/Contracts/
COPY src/Modules/Authentication/Domain/Authentication.Domain.csproj src/Modules/Authentication/Domain/
COPY src/Modules/Authentication/Infrastructure/Authentication.Infrastructure.csproj src/Modules/Authentication/Infrastructure/
COPY src/Modules/Authentication/Module/Authentication.Module.csproj src/Modules/Authentication/Module/
RUN dotnet restore src/Hosts/Main/Main.csproj

COPY src/ .
RUN dotnet publish src/Hosts/Main/Main.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Main.dll"]
