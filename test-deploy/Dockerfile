# ─────────────────────────────────────────────
# Stage 1: Build
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file và restore dependencies trước (tận dụng Docker layer cache)
COPY ["test-deploy.csproj", "./"]
RUN dotnet restore "test-deploy.csproj"

# Copy toàn bộ source code
COPY . .

# Build và publish ở chế độ Release
RUN dotnet publish "test-deploy.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ─────────────────────────────────────────────
# Stage 2: Runtime
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Cài đặt curl để dùng trong healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy artifacts từ build stage
COPY --from=build /app/publish .

# Cấu hình ASP.NET Core lắng nghe trên port 8080 (không phải 443/80)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entrypoint
ENTRYPOINT ["dotnet", "test-deploy.dll"]
