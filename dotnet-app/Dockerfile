# Build stage: restore dependencies and publish the .NET app for production.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Set the build working directory.
WORKDIR /src

# Copy only the project file first to improve Docker cache reuse.
COPY AccordIntakeApi.csproj .

# Restore NuGet packages required by the app.
RUN dotnet restore

# Copy the rest of the application source code into the build stage.
COPY . .

# Publish a release build into a folder ready to run in the final image.
RUN dotnet publish -c Release -o /app/publish

# Runtime stage: use the lightweight ASP.NET Core runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Set the runtime working directory.
WORKDIR /app

# Copy the published app files from the build stage.
COPY --from=build /app/publish .

# Inform Docker which port the app listens on.
EXPOSE 8080

# Start the compiled ASP.NET Core application.
ENTRYPOINT ["dotnet", "AccordIntakeApi.dll"]
