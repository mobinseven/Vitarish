FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy build infrastructure
COPY src/Directory.Build.props src/Directory.Packages.props ./

# Copy all .csproj files and restore as a layer
COPY src/ ./src/
ARG MODULE_BUILD_PROFILE=All
RUN dotnet restore src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=$MODULE_BUILD_PROFILE

# Publish
RUN dotnet publish src/Hosts/Main/Main.csproj -c Release -o /app/publish --no-restore -p:ModuleBuildProfile=$MODULE_BUILD_PROFILE -p:OpenApiGenerateDocumentsOnBuild=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vitarish.Main.dll"]
