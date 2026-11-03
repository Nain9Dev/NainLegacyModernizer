# Gateway image: the strangler proxy with its embedded control panel.
# Configure it with environment variables (Modernizer__LegacyBaseAddress, Modernizer__Routes__0__Id, ...)
# or mount your own appsettings.json over /app/appsettings.json.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet publish src/NainLegacyModernizer.Gateway --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# appsettings.json binds the local demo to 127.0.0.1:8080 and that key wins over ASPNETCORE_URLS;
# a plain configuration variable is read after appsettings.json, so the container listens on every interface.
ENV Urls=http://0.0.0.0:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "NainLegacyModernizer.Gateway.dll"]
