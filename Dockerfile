
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY *.sln ./
COPY InventoryManagement.Web/*.csproj ./InventoryManagement.Web/
RUN dotnet restore


FROM build AS publish
WORKDIR /src/InventoryManagement.Web
RUN dotnet publish -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryManagement.Web.dll"]
