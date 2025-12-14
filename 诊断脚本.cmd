@echo off
chcp 65001 >nul
echo ========================================
echo   服务器连接诊断
echo ========================================
echo.

echo [1] 检查端口 5000 监听状态:
netstat -ano | findstr ":5000" | findstr "LISTENING"
if %errorlevel% equ 0 (
    echo ✓ 端口 5000 正在监听
) else (
    echo ✗ 端口 5000 未监听
)
echo.

echo [2] 检查所有监听端口:
netstat -ano | findstr "LISTENING" | findstr ":5000"
echo.

echo [3] 检查本机IP地址:
ipconfig | findstr /i "IPv4"
echo.

echo [4] 测试本地连接:
echo 正在测试 localhost:5000...
powershell -Command "Test-NetConnection -ComputerName localhost -Port 5000 -WarningAction SilentlyContinue" 2>nul
if %errorlevel% neq 0 (
    echo 注意: 需要 PowerShell 来测试连接
    echo 或者手动在浏览器中访问 http://localhost:5000
)
echo.

echo ========================================
echo   诊断完成
echo ========================================
echo.
echo 建议操作:
echo 1. 检查 Windows 防火墙是否开放端口 5000
echo 2. 如果是云服务器，请在云控制台配置安全组
echo 3. 确认公网IP和内网IP的映射关系
echo.
pause

