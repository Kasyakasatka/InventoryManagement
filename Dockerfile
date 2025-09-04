FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY *.sln ./
COPY *.csproj ./
COPY . ./
RUN dotnet restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /src/publish .
ENTRYPOINT ["dotnet", "InventoryManagement.Web.dll"]
