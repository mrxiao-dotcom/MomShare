@echo off
chcp 65001 >nul
echo ========================================
echo   快速检查脚本
echo ========================================
echo.

echo [1] 检查端口 5000 是否监听:
netstat -ano | findstr ":5000" | findstr "LISTENING"
if %errorlevel% equ 0 (
    echo ✓ 服务正在运行
) else (
    echo ✗ 服务未运行，请启动 WPF 程序
)
echo.

echo [2] 检查防火墙规则:
netsh advfirewall firewall show rule name="MomShares Web Server (Port 5000)" 2>nul
if %errorlevel% equ 0 (
    echo ✓ 防火墙规则存在
) else (
    echo ✗ 防火墙规则不存在，请运行 配置防火墙.cmd
)
echo.

echo [3] 本机IP地址:
ipconfig | findstr /i "IPv4"
echo.

echo [4] 所有监听端口（包含5000）:
netstat -ano | findstr "LISTENING" | findstr ":5000"
echo.

echo ========================================
echo   检查完成
echo ========================================
echo.
echo 如果服务正在运行但外部无法访问:
echo 1. 运行 配置防火墙.cmd（需要管理员权限）
echo 2. 检查云服务器安全组配置
echo.
pause

