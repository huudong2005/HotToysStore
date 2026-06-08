FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file sln và các file csproj vào để restore dependencies trước (tối ưu cache)
COPY ["ToyStore.sln", "./"]
COPY ["ToyStore/ToyStore.csproj", "ToyStore/"]
COPY ["Domain/Domain.csproj", "Domain/"] # Nếu Domain là một Class Library có chứa csproj

RUN dotnet restore

# Copy toàn bộ code và build
COPY . .
WORKDIR "/src/ToyStore"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ToyStore.dll"]