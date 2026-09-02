FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy build infrastructure
COPY src/Directory.Build.props src/Directory.Packages.props ./

# Copy source and restore as a layer
COPY src/ ./src/
RUN dotnet restore src/Hosts/E2e/E2e.csproj

# E2e is non-publishable by default outside this explicit image build.
RUN dotnet publish src/Hosts/E2e/E2e.csproj -c Release -o /app/publish --no-restore -p:IsPublishable=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vitarish.E2e.dll"]
