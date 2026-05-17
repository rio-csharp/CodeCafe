FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY .artifacts/api/ ./
ENTRYPOINT ["dotnet", "CodeCafe.WebApi.dll"]
