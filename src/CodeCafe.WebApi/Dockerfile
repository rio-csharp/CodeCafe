FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

LABEL org.opencontainers.image.source="https://github.com/rio-csharp/CodeCafe"
LABEL org.opencontainers.image.licenses="MIT"

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY .artifacts/api/ ./
USER $APP_UID
ENTRYPOINT ["dotnet", "CodeCafe.WebApi.dll"]
