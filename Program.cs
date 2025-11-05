using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SystemManagerMcp;

class Program
{
    static async Task Main(string[] args)
    {
        var server = new McpServer();
        await server.RunAsync();
    }
}

public class McpServer
{
    public async Task RunAsync()
    {
        Console.Error.WriteLine("System Manager MCP Server 启动中...");
        
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        
        while (true)
        {
            var line = await Console.In.ReadLineAsync();
            if (line == null) break;
            
            try
            {
                var request = JsonSerializer.Deserialize<JsonElement>(line);
                var response = await HandleRequest(request);
                var responseJson = JsonSerializer.Serialize(response);
                await Console.Out.WriteLineAsync(responseJson);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"错误: {ex.Message}");
            }
        }
    }
    
    private async Task<object> HandleRequest(JsonElement request)
    {
        var method = request.GetProperty("method").GetString();
        
        return method switch
        {
            "initialize" => HandleInitialize(),
            "tools/list" => HandleListTools(),
            "tools/call" => await HandleToolCall(request),
            _ => new { error = "Unknown method" }
        };
    }
    
    private object HandleInitialize()
    {
        return new
        {
            protocolVersion = "2024-11-05",
            serverInfo = new
            {
                name = "system-manager-mcp",
                version = "1.0.0"
            },
            capabilities = new
            {
                tools = new { }
            }
        };
    }
    
    private object HandleListTools()
    {
        return new
        {
            tools = new object[]
            {
                new
                {
                    name = "get_system_info",
                    description = "获取系统信息（CPU、内存、OS）",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new
                {
                    name = "list_processes",
                    description = "列出当前运行的进程",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            top = new
                            {
                                type = "number",
                                description = "返回前N个进程（按内存排序）"
                            }
                        }
                    }
                },
                new
                {
                    name = "get_disk_info",
                    description = "获取磁盘空间信息",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                }
            }
        };
    }
    
    private Task<object> HandleToolCall(JsonElement request)
    {
        var toolName = request.GetProperty("params").GetProperty("name").GetString();
        var args = request.GetProperty("params").TryGetProperty("arguments", out var argsElement) 
            ? argsElement 
            : new JsonElement();
        
        try
        {
            var result = toolName switch
            {
                "get_system_info" => GetSystemInfo(),
                "list_processes" => ListProcesses(args),
                "get_disk_info" => GetDiskInfo(),
                _ => "Unknown tool"
            };
            
            return Task.FromResult<object>(new
            {
                content = new object[]
                {
                    new
                    {
                        type = "text",
                        text = result
                    }
                }
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult<object>(new
            {
                content = new object[]
                {
                    new
                    {
                        type = "text",
                        text = $"错误: {ex.Message}"
                    }
                }
            });
        }
    }
    
    private string GetSystemInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== 系统信息 ===");
        sb.AppendLine($"操作系统: {Environment.OSVersion}");
        sb.AppendLine($"处理器数: {Environment.ProcessorCount}");
        sb.AppendLine($"系统运行时间: {TimeSpan.FromMilliseconds(Environment.TickCount64)}");
        sb.AppendLine($"机器名: {Environment.MachineName}");
        sb.AppendLine($"用户名: {Environment.UserName}");
        
        // 内存信息（需要Process类）
        var process = Process.GetCurrentProcess();
        sb.AppendLine($"当前进程内存: {process.WorkingSet64 / 1024 / 1024} MB");
        
        return sb.ToString();
    }
    
    private string ListProcesses(JsonElement args)
    {
        int top = args.TryGetProperty("top", out var topElement) 
            ? topElement.GetInt32() 
            : 10;
        
        var processes = Process.GetProcesses()
            .OrderByDescending(p => {
                try { return p.WorkingSet64; }
                catch { return 0; }
            })
            .Take(top)
            .ToList();
        
        var sb = new StringBuilder();
        sb.AppendLine($"=== 前 {top} 个进程（按内存排序） ===");
        sb.AppendLine($"{"PID",-8} {"进程名",-30} {"内存(MB)",-15}");
        sb.AppendLine(new string('-', 60));
        
        foreach (var proc in processes)
        {
            try
            {
                sb.AppendLine($"{proc.Id,-8} {proc.ProcessName,-30} {proc.WorkingSet64 / 1024 / 1024,-15}");
            }
            catch
            {
                // 跳过无法访问的进程
            }
        }
        
        return sb.ToString();
    }
    
    private string GetDiskInfo()
    {
        var drives = DriveInfo.GetDrives();
        var sb = new StringBuilder();
        sb.AppendLine("=== 磁盘信息 ===");
        
        foreach (var drive in drives)
        {
            if (drive.IsReady)
            {
                sb.AppendLine($"\n驱动器: {drive.Name}");
                sb.AppendLine($"  类型: {drive.DriveType}");
                sb.AppendLine($"  文件系统: {drive.DriveFormat}");
                sb.AppendLine($"  总空间: {drive.TotalSize / 1024 / 1024 / 1024} GB");
                sb.AppendLine($"  可用空间: {drive.AvailableFreeSpace / 1024 / 1024 / 1024} GB");
                sb.AppendLine($"  使用率: {(1 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100:F2}%");
            }
        }
        
        return sb.ToString();
    }
}

