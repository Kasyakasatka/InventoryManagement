# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the app's source code
COPY . ./

# Publish the application for production
RUN dotnet publish -c Release -o out

# Use the official ASP.NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build-env /app/out .

# Run the application
ENTRYPOINT ["dotnet", "InventoryManagement.Web.dll"]
