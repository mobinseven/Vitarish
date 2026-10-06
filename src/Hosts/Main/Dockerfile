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
# Restrict outbound TLS (cipher suites, groups, minimum version) through the
# system OpenSSL defaults that .NET uses on Linux, and prove it with real
# handshakes; the build fails if the policy does not hold.
COPY src/Hosts/Main/openssl-outbound-tls.cnf src/Hosts/Main/verify-outbound-tls.sh /tmp/
RUN grep -q '^providers = provider_sect$' /etc/ssl/openssl.cnf \
    && sed -i 's/^providers = provider_sect$/providers = provider_sect\nssl_conf = vitarish_ssl_sect/' /etc/ssl/openssl.cnf \
    && cat /tmp/openssl-outbound-tls.cnf >> /etc/ssl/openssl.cnf \
    && sh /tmp/verify-outbound-tls.sh \
    && rm /tmp/openssl-outbound-tls.cnf /tmp/verify-outbound-tls.sh
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vitarish.Main.dll"]
