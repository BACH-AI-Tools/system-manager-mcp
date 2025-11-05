# System Manager MCP (C#)

一个基于 Model Context Protocol 的系统管理服务器，使用 C# 开发。

## 功能特性

- 📊 获取系统信息（CPU、内存、磁盘使用率）
- 🔍 进程管理和监控
- 💾 磁盘空间查询
- 🖥️ 操作系统信息

## 技术栈

- .NET 8.0
- MCP SDK for .NET

## 快速开始

### 安装依赖

```bash
dotnet restore
```

### 构建项目

```bash
dotnet build
```

### 运行

```bash
dotnet run
```

## MCP 工具列表

### 1. get-system-info
获取系统基本信息
- CPU 使用率
- 内存使用情况
- 操作系统版本

### 2. list-processes
列出当前运行的进程

### 3. get-disk-info
获取磁盘空间信息

## 开发

```bash
dotnet watch run
```

## 许可证

MIT

