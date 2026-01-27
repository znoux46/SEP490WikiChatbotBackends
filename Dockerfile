# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and project files for dependency resolution
COPY ["WikiChatbotBackends.sln", "WikiChatbotBackends.sln"]
COPY ["src/WikiChatbotBackends.API/WikiChatbotBackends.API.csproj", "src/WikiChatbotBackends.API/"]
COPY ["src/WikiChatbotBackends.Application/WikiChatbotBackends.Application.csproj", "src/WikiChatbotBackends.Application/"]
COPY ["src/WikiChatbotBackends.Domain/WikiChatbotBackends.Domain.csproj", "src/WikiChatbotBackends.Domain/"]
COPY ["src/WikiChatbotBackends.Infrastructure/WikiChatbotBackends.Infrastructure.csproj", "src/WikiChatbotBackends.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "WikiChatbotBackends.sln"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/src/WikiChatbotBackends.API"
RUN dotnet build "WikiChatbotBackends.API.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "WikiChatbotBackends.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official ASP.NET Core runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 80
EXPOSE 80

# Set the entry point
ENTRYPOINT ["dotnet", "WikiChatbotBackends.API.dll"]
