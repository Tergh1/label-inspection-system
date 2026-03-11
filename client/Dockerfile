FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY client/client.csproj client/
RUN dotnet restore client/client.csproj

COPY client/ client/
RUN dotnet publish client/client.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "client.dll"]
