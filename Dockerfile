
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY *.sln ./
COPY InventoryManagement.Web/*.csproj ./InventoryManagement.Web/
WORKDIR /app/InventoryManagement.Web
RUN dotnet restore


WORKDIR /app
COPY . .

WORKDIR /app/InventoryManagement.Web
RUN dotnet publish -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app


COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "InventoryManagement.Web.dll"]
