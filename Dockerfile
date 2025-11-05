# 多阶段构建 - .NET 应用
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制项目文件
COPY *.csproj ./
RUN dotnet restore

# 复制所有代码并构建
COPY . .
RUN dotnet publish -c Release -o /app/publish

# 运行时镜像
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .

# 设置入口点
ENTRYPOINT ["dotnet", "SystemManagerMcp.dll"]
