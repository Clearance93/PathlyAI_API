# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
# We copy all csproj files found in the workspace to ensure references are resolved
COPY ["PathlyAI_API/PathlyAI_API.csproj", "PathlyAI_API/"]
COPY ["Pathly Data/Pathly Data.csproj", "Pathly Data/"]
COPY ["Pathly Utility/Pathly Utility.csproj", "Pathly Utility/"]
COPY ["PathlyInterfaces/PathlyInterfaces.csproj", "PathlyInterfaces/"]
COPY ["Pathly Models/Pathly Models.csproj", "Pathly Models/"]
COPY ["Pathly DTOs/Pathly DTOs.csproj", "Pathly DTOs/"]
COPY ["Pathly Enums/Pathly Enums.csproj", "Pathly Enums/"]
COPY ["Pathly Services/Pathly Services.csproj", "Pathly Services/"]
COPY ["Pathly Core/Pathly Core.csproj", "Pathly Core/"]
COPY ["Pathly Helper/Pathly Helper.csproj", "Pathly Helper/"]

RUN dotnet restore "PathlyAI_API/PathlyAI_API.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish the application
WORKDIR "/src/PathlyAI_API"
RUN dotnet publish "PathlyAI_API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=build /app/publish .

# Ensure tessdata is included if it's needed at runtime (based on your csproj)
# The csproj copies it to the output directory, so it should be in /app/publish/tessdata
ENTRYPOINT ["dotnet", "PathlyAI_API.dll"]
