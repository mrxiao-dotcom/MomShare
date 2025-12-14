@echo off
chcp 65001 >nul
echo ========================================
echo   配置 Windows 防火墙 - 开放端口 5000
echo ========================================
echo.
echo 注意: 此脚本需要管理员权限
echo.

:: 检查管理员权限
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo 错误: 需要管理员权限！
    echo 请右键点击此文件，选择"以管理员身份运行"
    pause
    exit /b 1
)

echo [1] 检查现有防火墙规则...
netsh advfirewall firewall show rule name="MomShares Web Server (Port 5000)" >nul 2>&1
if %errorlevel% equ 0 (
    echo 找到现有规则，将删除后重新创建...
    netsh advfirewall firewall delete rule name="MomShares Web Server (Port 5000)" >nul 2>&1
)

echo [2] 创建入站规则...
netsh advfirewall firewall add rule name="MomShares Web Server (Port 5000)" dir=in action=allow protocol=TCP localport=5000 profile=any
if %errorlevel% equ 0 (
    echo ✓ 防火墙规则创建成功
) else (
    echo ✗ 防火墙规则创建失败
    pause
    exit /b 1
)

echo [3] 验证规则...
netsh advfirewall firewall show rule name="MomShares Web Server (Port 5000)"
echo.

echo ========================================
echo   配置完成
echo ========================================
echo.
echo 防火墙规则已创建，端口 5000 已开放
echo.
echo 如果仍然无法从外部访问，请检查:
echo 1. 云服务器安全组配置（如果是云服务器）
echo 2. 路由器/防火墙的端口映射
echo 3. 服务是否正在运行
echo.
pause

