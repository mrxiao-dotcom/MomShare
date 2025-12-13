// API 基础配置
const API_BASE_URL = window.location.origin;
let authToken = localStorage.getItem('authToken') || null;
let currentUser = JSON.parse(localStorage.getItem('currentUser') || '{}');

// 同步 token 的函数
function syncToken() {
    authToken = localStorage.getItem('authToken') || null;
    if (authToken) {
        const userStr = localStorage.getItem('currentUser');
        if (userStr) {
            currentUser = JSON.parse(userStr);
        }
    }
}

// 初始化
document.addEventListener('DOMContentLoaded', () => {
    if (authToken && currentUser.username) {
        showMainPage();
    } else {
        showLoginPage();
    }
    setupEventListeners();
});

// 设置事件监听
function setupEventListeners() {
    // 登录表单
    document.getElementById('loginForm')?.addEventListener('submit', handleLogin);
    
    // 退出登录
    document.getElementById('logoutBtn')?.addEventListener('click', handleLogout);
    
    // 导航菜单
    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', (e) => {
            e.preventDefault();
            const page = item.getAttribute('data-page');
            switchPage(page);
        });
    });
    
    // 模态框关闭
    document.querySelector('.close')?.addEventListener('click', () => {
        document.getElementById('modal').classList.add('hidden');
    });
    
    // 工具栏按钮
    document.getElementById('addProductBtn')?.addEventListener('click', () => showAddProductModal());
    document.getElementById('addHolderBtn')?.addEventListener('click', () => showAddHolderModal());
    document.getElementById('addShareBtn')?.addEventListener('click', () => loadAllocationProducts());
    document.getElementById('addNetValueBtn')?.addEventListener('click', () => loadNetValues());
    document.getElementById('addDividendBtn')?.addEventListener('click', () => loadDividendProducts());
    document.getElementById('addCapitalIncreaseBtn')?.addEventListener('click', () => loadCapitalIncreases());
    document.getElementById('addAdvisorBtn')?.addEventListener('click', () => showAddAdvisorModal());
    document.getElementById('addManagerBtn')?.addEventListener('click', () => showAddManagerModal());
    document.getElementById('backupBtn')?.addEventListener('click', () => handleBackup());
}

// 显示登录页面
function showLoginPage() {
    document.getElementById('loginPage').classList.remove('hidden');
    document.getElementById('mainPage').classList.add('hidden');
}

// 显示主页面
function showMainPage() {
    document.getElementById('loginPage').classList.add('hidden');
    document.getElementById('mainPage').classList.remove('hidden');
    document.getElementById('currentUser').textContent = `当前用户: ${currentUser.username}`;
    loadInitialData();
}

// 切换页面
function switchPage(pageName) {
    // 更新导航状态
    document.querySelectorAll('.nav-item').forEach(item => {
        item.classList.remove('active');
    });
    document.querySelector(`[data-page="${pageName}"]`)?.classList.add('active');
    
    // 更新内容页面
    document.querySelectorAll('.content-page').forEach(page => {
        page.classList.remove('active');
    });
    document.getElementById(`${pageName}Page`)?.classList.add('active');
    
    // 加载对应数据
    loadPageData(pageName);
}

// 加载初始数据
function loadInitialData() {
    switchPage('dashboard');
}

// 加载页面数据
function loadPageData(pageName) {
    switch (pageName) {
        case 'dashboard':
            loadDashboard();
            break;
        case 'products':
            loadProducts();
            break;
        case 'holders':
            loadHolders();
            break;
        case 'shares':
            loadAllocationProducts();
            break;
        case 'advisors':
            loadAdvisors();
            break;
        case 'managers':
            loadManagers();
            break;
        case 'netvalues':
            loadNetValues();
            break;
        case 'dividends':
            loadDividendProducts();
            break;
        case 'capitalincreases':
            loadCapitalIncreases();
            break;
        case 'system':
            loadAdmins();
            break;
    }
}

// 加载仪表盘数据
async function loadDashboard() {
    try {
        // 加载统计数据
        const stats = await apiCall('/api/dashboard/stats');
        
        // 更新统计卡片
        document.getElementById('activeProductsCount').textContent = stats.ActiveProductsCount || stats.activeProductsCount || 0;
        document.getElementById('totalProductAmount').textContent = formatCurrency(stats.TotalProductAmount || stats.totalProductAmount || 0);
        document.getElementById('totalDividendAmount').textContent = formatCurrency(stats.TotalDividendAmount || stats.totalDividendAmount || 0);
        document.getElementById('dividendCount').textContent = stats.DividendCount || stats.dividendCount || 0;
        document.getElementById('investorCount').textContent = stats.InvestorCount || stats.investorCount || 0;
        
        const todayProfit = stats.TodayProfit || stats.todayProfit || 0;
        const profitElement = document.getElementById('todayProfit');
        profitElement.textContent = formatCurrency(todayProfit);
        if (todayProfit >= 0) {
            profitElement.style.color = '#27AE60';
        } else {
            profitElement.style.color = '#E74C3C';
        }

        // 加载周数据并绘制图表
        try {
            const weeklyData = await apiCall('/api/dashboard/weekly-amounts');
            console.log('周数据:', weeklyData);
            // 确保 weeklyData 是数组
            if (Array.isArray(weeklyData)) {
                drawWeeklyChart(weeklyData);
            } else {
                console.warn('周数据格式不正确，期望数组，实际收到:', typeof weeklyData, weeklyData);
                drawWeeklyChart([]);
            }
        } catch (chartError) {
            console.error('加载周数据失败:', chartError);
            // 即使周数据加载失败，也不影响其他统计数据的显示
            drawWeeklyChart([]);
        }
    } catch (error) {
        console.error('加载仪表盘数据失败:', error);
        alert('加载仪表盘数据失败: ' + error.message);
    }
}

// 格式化货币
function formatCurrency(amount) {
    return new Intl.NumberFormat('zh-CN', {
        style: 'currency',
        currency: 'CNY',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(amount);
}

// 绘制周权益变化折线图
function drawWeeklyChart(data) {
    const canvas = document.getElementById('weeklyAmountsChart');
    if (!canvas) {
        console.error('找不到图表画布元素');
        return;
    }

    // 确保 data 是数组
    if (!Array.isArray(data) || data.length === 0) {
        console.warn('周数据为空或格式不正确:', data);
        // 显示提示信息
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#999';
        ctx.font = '16px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('暂无数据', canvas.width / 2, canvas.height / 2);
        return;
    }

    const ctx = canvas.getContext('2d');
    const width = canvas.width;
    const height = canvas.height;
    
    // 先计算Y轴标签的最大宽度，动态调整左侧padding
    ctx.font = '11px Arial';
    const testLabels = ['1000万', '100万', '10万', '1万', '1000', '100'];
    const maxLabelWidth = Math.max(...testLabels.map(label => ctx.measureText(label).width));
    const leftPadding = Math.max(60, maxLabelWidth + 20); // 至少60，或根据文本宽度+20
    const rightPadding = 40;
    const topPadding = 30;
    const bottomPadding = 40;
    
    const padding = leftPadding; // 保留用于兼容
    const chartWidth = width - leftPadding - rightPadding;
    const chartHeight = height - topPadding - bottomPadding;

    // 清空画布
    ctx.clearRect(0, 0, width, height);

    // 提取数据
    const amounts = data.map(d => {
        const amt = d.Amount || d.amount || 0;
        return typeof amt === 'number' ? amt : parseFloat(amt) || 0;
    });
    const labels = data.map(d => d.WeekLabel || d.weekLabel || d.Week || d.week || '');
    
    // 确保有有效数据
    if (amounts.length === 0 || amounts.every(a => a === 0)) {
        console.warn('所有权益值都为0');
    }
    
    const minAmount = Math.min(...amounts);
    const maxAmount = Math.max(...amounts);
    const range = maxAmount - minAmount;
    
    // 计算坐标轴范围：最低值和最高值各扩展10%
    let minAxis, maxAxis;
    if (range < 0.01 || amounts.length === 1) {
        // 如果所有值相同或只有一个数据，以该值为中心，上下10%
        const centerValue = maxAmount > 0 ? maxAmount : (minAmount > 0 ? minAmount : 1);
        minAxis = centerValue * 0.9;
        maxAxis = centerValue * 1.1;
    } else {
        // 如果有多个不同值，最低值减10%，最高值加10%
        minAxis = minAmount * 0.9;
        maxAxis = maxAmount * 1.1;
    }
    
    // 确保坐标轴范围不为0
    const axisRange = maxAxis - minAxis;
    const adjustedRange = axisRange > 0.01 ? axisRange : 1;

    // 绘制背景网格
    ctx.strokeStyle = '#e0e0e0';
    ctx.lineWidth = 1;
    for (let i = 0; i <= 5; i++) {
        const y = padding + (chartHeight / 5) * i;
        ctx.beginPath();
        ctx.moveTo(padding, y);
        ctx.lineTo(width - padding, y);
        ctx.stroke();
    }

    // 绘制坐标轴
    ctx.strokeStyle = '#333';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(padding, padding);
    ctx.lineTo(padding, height - padding);
    ctx.lineTo(width - padding, height - padding);
    ctx.stroke();

    // 绘制折线
    ctx.strokeStyle = '#4A90E2';
    ctx.lineWidth = 3;
    ctx.beginPath();
    const pointCount = amounts.length;
    const stepX = chartWidth / (pointCount - 1 || 1);

    amounts.forEach((amount, index) => {
        const x = leftPadding + index * stepX;
        const y = height - bottomPadding - ((amount - minAxis) / adjustedRange) * chartHeight;
        
        if (index === 0) {
            ctx.moveTo(x, y);
        } else {
            ctx.lineTo(x, y);
        }
    });
    ctx.stroke();

    // 绘制数据点
    ctx.fillStyle = '#4A90E2';
    amounts.forEach((amount, index) => {
        const x = leftPadding + index * stepX;
        const y = height - bottomPadding - ((amount - minAxis) / adjustedRange) * chartHeight;
        
        ctx.beginPath();
        ctx.arc(x, y, 5, 0, Math.PI * 2);
        ctx.fill();
    });

    // 绘制标签和数值
    ctx.fillStyle = '#666';
    ctx.font = '12px Arial';
    ctx.textAlign = 'center';
    
    labels.forEach((label, index) => {
        const x = leftPadding + index * stepX;
        ctx.fillText(label, x, height - bottomPadding + 20);
    });

    // 绘制Y轴数值
    ctx.fillStyle = '#666';
    ctx.font = '11px Arial';
    ctx.textAlign = 'right';
    ctx.textBaseline = 'middle';
    
    // 显示从坐标轴最小值到最大值的5个刻度
    for (let i = 0; i <= 5; i++) {
        const value = minAxis + (axisRange / 5) * (5 - i);
        const y = topPadding + (chartHeight / 5) * i;
        // 简化显示，大数值用万为单位
        const displayValue = value >= 10000 
            ? (value / 10000).toFixed(1) + '万' 
            : value >= 1 
                ? value.toFixed(0) 
                : value.toFixed(2);
        ctx.fillText(displayValue, leftPadding - 10, y);
    }

    // 绘制标题
    ctx.fillStyle = '#333';
    ctx.font = 'bold 14px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('总产品权益 (元)', width / 2, topPadding - 10);
}

// API 调用函数
async function apiCall(endpoint, options = {}) {
    // 确保使用最新的 token（每次调用前同步）
    syncToken();
    const token = authToken;
    
    const url = `${API_BASE_URL}${endpoint}`;
    const headers = {
        'Content-Type': 'application/json'
    };
    
    // 如果有 token，添加到 header
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }
    
    const config = {
        method: options.method || 'GET',
        headers: headers
    };
    
    // 处理 body
    if (options.body && typeof options.body === 'object') {
        config.body = JSON.stringify(options.body);
    } else if (options.body) {
        config.body = options.body;
    }
    
    // 调试信息
    if (token) {
        console.log(`API调用: ${config.method} ${url}`, 'Token:', token.substring(0, 20) + '...');
    } else {
        console.log(`API调用: ${config.method} ${url}`, '无Token');
    }
    
    try {
        const response = await fetch(url, config);
        
        // 处理 401 未授权
        if (response.status === 401) {
            handleLogout();
            throw new Error('未授权，请重新登录');
        }
        
        // 处理 404 等错误
        if (!response.ok) {
            let errorMessage = '请求失败';
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || errorMessage;
            } catch (e) {
                errorMessage = `请求失败: ${response.status} ${response.statusText}`;
            }
            throw new Error(errorMessage);
        }
        
        // 解析响应数据
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            const data = await response.json();
            return data;
        } else {
            return await response.text();
        }
    } catch (error) {
        console.error('API调用错误:', error);
        throw error;
    }
}

// 登录处理
async function handleLogin(e) {
    e.preventDefault();
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const errorDiv = document.getElementById('loginError');
    
    errorDiv.textContent = '';
    
    try {
        const response = await apiCall('/api/auth/admin/login', {
            method: 'POST',
            body: { username, password }
        });
        
        // 处理大小写问题：API 返回 Token（PascalCase），但也要兼容 token（camelCase）
        authToken = response.Token || response.token;
        currentUser = {
            username: response.Username || response.username,
            userId: response.UserId || response.userId,
            userType: response.UserType || response.userType
        };
        
        if (!authToken) {
            throw new Error('登录响应中未找到 Token');
        }
        
        // 保存到 localStorage 和全局变量
        localStorage.setItem('authToken', authToken);
        localStorage.setItem('currentUser', JSON.stringify(currentUser));
        
        // 调试信息
        console.log('登录成功，Token已保存:', authToken.substring(0, 20) + '...');
        
        showMainPage();
    } catch (error) {
        errorDiv.textContent = error.message || '登录失败，请检查用户名和密码';
    }
}

// 退出登录
function handleLogout() {
    authToken = null;
    currentUser = {};
    localStorage.removeItem('authToken');
    localStorage.removeItem('currentUser');
    showLoginPage();
}

// 加载产品列表
async function loadProducts() {
    const tbody = document.getElementById('productsTableBody');
    tbody.innerHTML = '<tr><td colspan="5" class="loading">加载中...</td></tr>';
    
    try {
        const products = await apiCall('/api/products');
        if (products.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="loading">暂无数据</td></tr>';
            return;
        }
        
        tbody.innerHTML = products.map(product => {
            // 支持 PascalCase 和 camelCase
            const id = product.Id || product.id;
            const name = product.Name || product.name;
            const code = product.Code || product.code;
            const initialAmount = product.InitialAmount || product.initialAmount || 0;
            const totalAmount = product.TotalAmount || product.totalAmount || 0;
            const advisorName = (product.Advisor && (product.Advisor.Name || product.Advisor.name)) || '-';
            const managerName = (product.Manager && (product.Manager.Name || product.Manager.name)) || '-';
            const createdAt = product.CreatedAt || product.createdAt;
            
            return `
            <tr>
                <td>${id}</td>
                <td>${name}</td>
                <td>${code || '-'}</td>
                <td>${initialAmount.toFixed(2)}</td>
                <td>${totalAmount.toFixed(2)}</td>
                <td>${advisorName}</td>
                <td>${managerName}</td>
                <td>${new Date(createdAt).toLocaleString('zh-CN')}</td>
                <td>
                    <button class="btn btn-secondary" onclick="showEditProductModal(${id})" style="margin-right: 5px;">编辑</button>
                    <button class="btn btn-secondary" onclick="showDistributionPlanModal(${id})" style="margin-right: 5px;">分配方案</button>
                    <button class="btn btn-danger" onclick="deleteProduct(${id})">删除</button>
                </td>
            </tr>
        `;
        }).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="5" class="loading">加载失败: ${error.message}</td></tr>`;
    }
}

// 加载持有者列表
async function loadHolders() {
    const tbody = document.getElementById('holdersTableBody');
    tbody.innerHTML = '<tr><td colspan="8" class="loading">加载中...</td></tr>';
    
    try {
        const holders = await apiCall('/api/holders');
        if (holders.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="loading">暂无数据</td></tr>';
            return;
        }
        
        tbody.innerHTML = holders.map(holder => {
            // 支持 PascalCase 和 camelCase
            const id = holder.Id || holder.id;
            const name = holder.Name || holder.name;
            const phone = holder.Phone || holder.phone;
            const bankName = holder.BankName || holder.bankName;
            const bankAccount = holder.BankAccount || holder.bankAccount;
            const accountName = holder.AccountName || holder.accountName;
            const createdAt = holder.CreatedAt || holder.createdAt;
            
            return `
            <tr>
                <td>${id}</td>
                <td>${name}</td>
                <td>${phone}</td>
                <td>${bankName || '-'}</td>
                <td>${bankAccount || '-'}</td>
                <td>${accountName || '-'}</td>
                <td>${new Date(createdAt).toLocaleString('zh-CN')}</td>
                <td>
                    <button class="btn btn-secondary" onclick="showEditHolderModal(${id})" style="margin-right: 5px;">编辑</button>
                    <button class="btn btn-secondary" onclick="showChangePasswordModal(${id})" style="margin-right: 5px;">修改密码</button>
                    <button class="btn btn-danger" onclick="deleteHolder(${id})">删除</button>
                </td>
            </tr>
        `;
        }).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="8" class="loading">加载失败: ${error.message}</td></tr>`;
    }
}

// 加载产品列表用于筛选
let allProducts = [];
async function loadProductsForFilter() {
    try {
        allProducts = await apiCall('/api/products');
        const filter = document.getElementById('productFilter');
        if (filter) {
            filter.innerHTML = '<option value="">全部产品</option>' + 
                allProducts.map(p => {
                    const id = p.Id || p.id;
                    const name = p.Name || p.name;
                    return `<option value="${id}">${name}</option>`;
                }).join('');
            
            filter.addEventListener('change', (e) => {
                const productId = e.target.value;
                loadShares(productId);
            });
        }
    } catch (error) {
        console.error('加载产品列表失败:', error);
    }
}

// 加载份额列表
async function loadShares(productId = null) {
    const tbody = document.getElementById('sharesTableBody');
    tbody.innerHTML = '<tr><td colspan="7" class="loading">加载中...</td></tr>';
    
    try {
        let shares = await apiCall('/api/shares');
        
        // 如果指定了产品ID，进行筛选
        if (productId) {
            shares = shares.filter(s => {
                const pid = s.ProductId || s.productId;
                return pid == productId;
            });
        }
        
        // 确保返回的是数组
        if (!Array.isArray(shares)) {
            console.error('返回的数据不是数组:', shares);
            tbody.innerHTML = '<tr><td colspan="7" class="loading">数据格式错误</td></tr>';
            return;
        }
        
        if (shares.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="loading">暂无数据</td></tr>';
            return;
        }
        
        tbody.innerHTML = shares.map(share => {
            // 支持 PascalCase 和 camelCase
            const id = share.Id || share.id;
            const productName = share.ProductName || share.productName;
            const holderName = share.HolderName || share.holderName;
            const shareType = share.ShareType || share.shareType || 'Subordinate';
            const shareTypeName = shareType === 'Priority' ? '优先方' : '劣后方';
            const shareAmount = share.ShareAmount || share.shareAmount || 0;
            const investmentAmount = share.InvestmentAmount || share.investmentAmount || 0;
            const createdAt = share.CreatedAt || share.createdAt;
            
            return `
            <tr>
                <td>${id}</td>
                <td>${productName || '-'}</td>
                <td>${holderName || '-'}</td>
                <td>${shareTypeName}</td>
                <td>${shareAmount}</td>
                <td>${investmentAmount.toFixed(2)}</td>
                <td>${new Date(createdAt).toLocaleString('zh-CN')}</td>
            </tr>
        `;
        }).join('');
    } catch (error) {
        console.error('加载份额失败:', error);
        tbody.innerHTML = `<tr><td colspan="6" class="loading">加载失败: ${error.message}</td></tr>`;
    }
}

// 加载产品份额分配概览（产品维度）
async function loadAllocationProducts() {
    const tbody = document.getElementById('sharesTableBody');
    tbody.innerHTML = '<tr><td colspan="8" class="loading">加载中...</td></tr>';
    
    try {
        const products = await apiCall('/api/shares/summary');
        if (!Array.isArray(products) || products.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="loading">暂无数据</td></tr>';
            return;
        }
        
        tbody.innerHTML = products.map(p => {
            const id = p.Id || p.id;
            const name = p.Name || p.name;
            const code = p.Code || p.code || '-';
            const initialAmount = p.InitialAmount || p.initialAmount || 0;
            const totalAmount = p.TotalAmount || p.totalAmount || 0;
            const holderCount = p.HolderCount || p.holderCount || 0;
            const allocated = p.Allocated || p.allocated;
            return `
            <tr>
                <td>${id}</td>
                <td>${name}</td>
                <td>${code}</td>
                <td>${initialAmount.toFixed(2)}</td>
                <td>${totalAmount.toFixed(2)}</td>
                <td>${holderCount}</td>
                <td>${allocated ? '已分配' : '未分配'}</td>
                <td>
                    <button class="btn btn-secondary" onclick="showShareDetails(${id})" style="margin-right:6px;">查看份额</button>
                    <button class="btn btn-secondary" onclick="showAllocateModal(${id})" style="margin-right:6px;">编辑份额</button>
                    <button class="btn btn-danger" onclick="clearAllocation(${id})">清空份额</button>
                </td>
            </tr>
        `;
        }).join('');
    } catch (error) {
        console.error('加载产品份额概览失败:', error);
        tbody.innerHTML = `<tr><td colspan="8" class="loading">加载失败: ${error.message}</td></tr>`;
    }
}

function addAllocationRow(container, holders, holderId = null, shareType = 'Subordinate', shareAmount = '', investmentAmount = '') {
    const row = document.createElement('div');
    row.className = 'allocation-row';
    row.style.display = 'grid';
    row.style.gridTemplateColumns = '2fr 1fr 1fr 1fr auto';
    row.style.gap = '10px';
    row.style.marginBottom = '10px';
    row.style.alignItems = 'center';
    
    const holderOptions = holders.map(h => {
        const id = h.Id || h.id;
        const name = h.Name || h.name;
        const selected = holderId && id === holderId ? 'selected' : '';
        return `<option value="${id}" ${selected}>${name}</option>`;
    }).join('');
    
    const shareTypeSelected = shareType === 'Priority' ? 'selected' : '';
    const subordinateSelected = shareType !== 'Priority' ? 'selected' : '';
    
    // 统一高度样式，确保所有元素高度一致
    const commonStyle = 'height: 38px; padding: 8px 12px; font-size: 14px; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box;';
    
    row.innerHTML = `
        <select name="holderId" required style="${commonStyle}">
            <option value="">选择持有人</option>
            ${holderOptions}
        </select>
        <select name="shareType" required style="${commonStyle}">
            <option value="Subordinate" ${subordinateSelected}>劣后方</option>
            <option value="Priority" ${shareTypeSelected}>优先方</option>
        </select>
        <input type="number" name="shareAmount" step="0.01" min="0" placeholder="份额数量" value="${shareAmount}" required style="${commonStyle}" />
        <input type="number" name="investmentAmount" step="0.01" min="0" placeholder="实际投入资金" value="${investmentAmount || shareAmount}" required style="${commonStyle}" />
        <button type="button" class="btn btn-secondary remove-row" style="height: 38px; padding: 6px 12px; font-size: 12px; box-sizing: border-box;">删除</button>
    `;
    row.querySelector('.remove-row').addEventListener('click', () => {
        container.removeChild(row);
    });
    container.appendChild(row);
}

// 显示分配份额模态框（支持编辑）
async function showAllocateModal(productId) {
    try {
        const [product, holders, existingShares] = await Promise.all([
            apiCall(`/api/products/${productId}`),
            apiCall('/api/holders'),
            apiCall(`/api/shares/product/${productId}`).catch(() => []) // 如果还没有份额，返回空数组
        ]);

        const hasExistingShares = Array.isArray(existingShares) && existingShares.length > 0;
        const isEditMode = hasExistingShares;

        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>${isEditMode ? '编辑' : '分配'}产品份额</h2>
            <p style="margin: 10px 0;">产品：${product.Name || product.name}（代码：${product.Code || product.code || '-'}）</p>
            <p style="margin: 10px 0;">产品总权益：${product.TotalAmount || product.totalAmount}（总份额必须等于产品总权益）</p>
            <div class="form-group">
                <label>持有人分配</label>
                <div id="allocationContainer">
                    <div class="allocation-header" style="display: grid; grid-template-columns: 2fr 1fr 1fr 1fr auto; gap: 10px; margin-bottom: 8px; padding: 8px; background-color: #f5f5f5; border-radius: 4px; font-weight: bold; font-size: 13px; align-items: center;">
                        <div>持有人</div>
                        <div>份额类型</div>
                        <div>份额</div>
                        <div>实际出资</div>
                        <div>操作</div>
                    </div>
                </div>
                <button type="button" class="btn btn-secondary" id="addAllocationRowBtn" style="margin-top:10px;">新增持有人</button>
            </div>
            <div class="form-group">
                <strong>提示：</strong> 总份额必须等于产品总权益（${product.TotalAmount || product.totalAmount}），可以修改份额和实际投入资金。
            </div>
            <div class="form-actions">
                <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                <button type="button" class="btn btn-primary" id="saveAllocationBtn">保存</button>
            </div>
        `;

        const container = modalBody.querySelector('#allocationContainer');
        
        // 如果有现有份额，加载现有数据
        if (hasExistingShares) {
            existingShares.forEach(share => {
                const holderId = share.HolderId || share.holderId;
                const shareAmount = share.ShareAmount || share.shareAmount || 0;
                const investmentAmount = share.InvestmentAmount || share.investmentAmount || shareAmount;
                const shareType = share.ShareType || share.shareType || 'Subordinate';
                addAllocationRow(container, holders, holderId, shareType, shareAmount, investmentAmount);
            });
        } else {
            // 如果没有现有份额，添加一行空行
            addAllocationRow(container, holders);
        }

        document.getElementById('addAllocationRowBtn').addEventListener('click', () => addAllocationRow(container, holders));

        document.getElementById('saveAllocationBtn').addEventListener('click', async () => {
            const rows = container.querySelectorAll('.allocation-row');
            if (rows.length === 0) {
                alert('请至少添加一条持有人记录');
                return;
            }

            const allocations = [];
            for (const row of rows) {
                const holderId = parseInt(row.querySelector('select[name="holderId"]').value);
                const shareType = row.querySelector('select[name="shareType"]').value;
                const shareAmount = parseFloat(row.querySelector('input[name="shareAmount"]').value);
                const investmentAmount = parseFloat(row.querySelector('input[name="investmentAmount"]').value) || shareAmount;
                
                if (!holderId || !shareAmount || shareAmount <= 0) {
                    alert('请填写完整的持有人和份额');
                    return;
                }
                allocations.push({ 
                    HolderId: holderId, 
                    ShareType: shareType, 
                    ShareAmount: shareAmount,
                    InvestmentAmount: investmentAmount
                });
            }

            const totalAllocation = allocations.reduce((sum, a) => sum + a.ShareAmount, 0);
            const totalAmount = product.TotalAmount || product.totalAmount || 0;
            if (Math.abs(totalAllocation - totalAmount) > 0.0001) {
                alert(`分配总份额(${totalAllocation})必须等于产品总权益(${totalAmount})`);
                return;
            }

            try {
                const method = isEditMode ? 'PUT' : 'POST';
                await apiCall(`/api/shares/product/${productId}/allocate`, {
                    method: method,
                    body: { allocations }
                });
                closeModal();
                loadAllocationProducts();
                alert(isEditMode ? '份额更新成功' : '份额分配成功');
            } catch (error) {
                alert((isEditMode ? '更新' : '分配') + '失败: ' + error.message);
            }
        });

        document.getElementById('modal').classList.remove('hidden');
    } catch (error) {
        alert('加载数据失败: ' + error.message);
    }
}

// 查看产品份额详情
async function showShareDetails(productId) {
    try {
        const shares = await apiCall(`/api/shares/product/${productId}`);
        if (!Array.isArray(shares)) {
            alert('数据格式错误');
            return;
        }

        if (shares.length === 0) {
            alert('暂无份额记录');
            return;
        }

        const totalShares = shares.reduce((sum, s) => sum + (s.ShareAmount || s.shareAmount || 0), 0);

        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>产品份额详情</h2>
            <table class="data-table">
                <thead>
                    <tr>
                        <th>持有人</th>
                        <th>份额类型</th>
                        <th>份额数量</th>
                        <th>占比</th>
                        <th>投资金额</th>
                    </tr>
                </thead>
                <tbody id="shareDetailsTableBody">
                </tbody>
            </table>
        `;

        const tbody = modalBody.querySelector('#shareDetailsTableBody');
        tbody.innerHTML = shares.map(s => {
            const holderName = (s.Holder && (s.Holder.Name || s.Holder.name)) || s.holderName || '-';
            const shareType = s.ShareType || s.shareType || 'Subordinate';
            const shareTypeName = shareType === 'Priority' ? '优先方' : '劣后方';
            const amount = s.ShareAmount || s.shareAmount || 0;
            const investment = s.InvestmentAmount || s.investmentAmount || 0;
            const ratio = totalShares > 0 ? (amount / totalShares * 100).toFixed(2) : '0.00';
            return `
                <tr>
                    <td>${holderName}</td>
                    <td>${shareTypeName}</td>
                    <td>${amount}</td>
                    <td>${ratio}%</td>
                    <td>${investment.toFixed(2)}</td>
                </tr>
            `;
        }).join('');

        document.getElementById('modal').classList.remove('hidden');
    } catch (error) {
        alert('加载份额详情失败: ' + error.message);
    }
}

// 清空产品份额
async function clearAllocation(productId) {
    if (!confirm('确定要清空该产品的所有份额记录吗？此操作不可恢复。')) return;
    try {
        await apiCall(`/api/shares/product/${productId}/clear`, { method: 'DELETE' });
        loadAllocationProducts();
        alert('已清空份额，可以重新分配');
    } catch (error) {
        alert('清空失败: ' + error.message);
    }
}

// 分红管理
async function showDividendModal(productId) {
    try {
        const product = await apiCall(`/api/products/${productId}`);
        const plan = product.DistributionPlan || product.distributionPlan;
        if (!plan) {
            alert('产品未配置分配方案');
            return;
        }

        const totalAmount = product.TotalAmount || product.totalAmount || 0;
        const initialAmount = product.InitialAmount || product.initialAmount || 0;
        const distributable = totalAmount - initialAmount;

        if (distributable <= 0) {
            alert('当前无可分红金额（总权益未超过初始/累积权益）');
            return;
        }

        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>分红管理</h2>
            <p>产品：${product.Name || product.name}（${product.Code || product.code || '-'}）</p>
            <p>最新权益：${totalAmount.toFixed(2)}，初始权益：${initialAmount.toFixed(2)}，可分红金额：${distributable.toFixed(2)}</p>
            <form class="modal-form" id="dividendForm">
                <div class="form-group">
                    <label>分红金额（默认可分红金额，可手动调整，不得超过可分红额度）</label>
                    <input type="number" name="totalAmount" step="0.01" min="0" max="${distributable.toFixed(2)}" value="${distributable.toFixed(2)}" required>
                </div>
                <div class="form-group">
                    <label>分红日期</label>
                    <input type="date" name="dividendDate" value="${new Date().toISOString().split('T')[0]}">
                </div>
                <div class="form-group">
                    <label>预览分配（按当前输入金额计算，劣后方按持股比例拆分）</label>
                    <div id="dividendPreview" style="padding:10px; background:#f5f5f5; border-radius:5px;">
                        <div>加载中...</div>
                    </div>
                </div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                    <button type="submit" class="btn btn-primary">确定分红</button>
                </div>
            </form>
        `;

        document.getElementById('modal').classList.remove('hidden');

        const form = document.getElementById('dividendForm');
        const previewDiv = document.getElementById('dividendPreview');
        const updatePreview = async () => {
            const inputAmount = parseFloat(form.totalAmount.value) || 0;
            if (inputAmount <= 0) {
                previewDiv.innerHTML = '<div>金额需大于0</div>';
                return;
            }
            try {
                const preview = await apiCall(`/api/dividends/product/${productId}/preview?amount=${inputAmount}`);
                const p = preview.PriorityAmount || 0;
                const s = preview.SubordinateAmount || 0;
                const m = preview.ManagerAmount || 0;
                const a = preview.AdvisorAmount || 0;
                const subordinateDetails = preview.SubordinateDetails || [];

                const subordinateRows = subordinateDetails.length > 0 ? subordinateDetails.map(d => {
                    const holderName = d.HolderName || d.holderName || '-';
                    const amt = d.Amount || d.amount || 0;
                    const ratio = (d.Ratio || d.ratio || 0) * 100;
                    return `<div>劣后：${holderName} | 分红：${amt.toFixed(2)} | 占比：${ratio.toFixed(2)}%</div>`;
                }).join('') : '<div>劣后：无</div>';

                previewDiv.innerHTML = `
                    <div>优先方合计：${p.toFixed(2)}（无需分拆明细）</div>
                    <div style="margin-top:6px;">劣后方合计：${s.toFixed(2)}</div>
                    ${subordinateRows}
                    <div style="margin-top:6px;">管理方：${m.toFixed(2)}</div>
                    <div>投顾方：${a.toFixed(2)}</div>
                `;
            } catch (err) {
                previewDiv.innerHTML = `<div>预览失败: ${err.message}</div>`;
            }
        };
        form.totalAmount.addEventListener('input', updatePreview);

        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const amount = parseFloat(form.totalAmount.value) || 0;
            if (amount <= 0 || amount > distributable + 0.0001) {
                alert(`分红金额必须大于0且不超过可分红金额(${distributable.toFixed(2)})`);
                return;
            }
            const payload = {
                ProductId: productId,
                DividendDate: form.dividendDate.value || new Date().toISOString(),
                TotalAmount: amount
            };
            try {
                await apiCall('/api/dividends', {
                    method: 'POST',
                    body: payload
                });
                closeModal();
                loadDividendProducts();
                alert('分红创建成功');
            } catch (error) {
                alert('分红创建失败: ' + error.message);
            }
        });

        updatePreview();
    } catch (error) {
        alert('加载分红数据失败: ' + error.message);
    }
}

// 分红历史
async function showDividendHistory(productId) {
    try {
        const list = await apiCall(`/api/dividends/product/${productId}`);
        if (!Array.isArray(list) || list.length === 0) {
            alert('暂无分红记录');
            return;
        }
        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>分红历史</h2>
            <table class="data-table">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>总分红</th>
                        <th>优先方</th>
                        <th>劣后方</th>
                        <th>管理方</th>
                        <th>投顾方</th>
                        <th>分红日期</th>
                        <th>创建时间</th>
                    </tr>
                </thead>
                <tbody>
                    ${list.map(d => {
                        const id = d.Id || d.id;
                        const totalAmount = d.TotalAmount || d.totalAmount || 0;
                        const priority = d.PriorityAmount || d.priorityAmount || 0;
                        const subordinate = d.SubordinateAmount || d.subordinateAmount || 0;
                        const manager = d.ManagerAmount || d.managerAmount || 0;
                        const advisor = d.AdvisorAmount || d.advisorAmount || 0;
                        const date = d.DividendDate || d.dividendDate;
                        const createdAt = d.CreatedAt || d.createdAt;
                        return `
                            <tr>
                                <td>${id}</td>
                                <td>${totalAmount.toFixed(2)}</td>
                                <td>${priority.toFixed(2)}</td>
                                <td>${subordinate.toFixed(2)}</td>
                                <td>${manager.toFixed(2)}</td>
                                <td>${advisor.toFixed(2)}</td>
                                <td>${new Date(date).toLocaleDateString('zh-CN')}</td>
                                <td>${createdAt ? new Date(createdAt).toLocaleString('zh-CN') : '-'}</td>
                            </tr>
                        `;
                    }).join('')}
                </tbody>
            </table>
        `;
        document.getElementById('modal').classList.remove('hidden');
    } catch (error) {
        alert('加载分红历史失败: ' + error.message);
    }
}

// 加载净值产品列表（按产品管理净值）
async function loadNetValues() {
    const tbody = document.getElementById('netvaluesTableBody');
    tbody.innerHTML = '<tr><td colspan="5" class="loading">加载中...</td></tr>';
    
    try {
        const products = await apiCall('/api/products');
        if (!Array.isArray(products) || products.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="loading">暂无数据</td></tr>';
            return;
        }
        
        tbody.innerHTML = products.map(p => {
            const id = p.Id || p.id;
            const name = p.Name || p.name;
            const code = p.Code || p.code || '-';
            const current = p.TotalAmount || p.totalAmount || 0;
            const net = p.CurrentNetValue || p.currentNetValue || 1;
            return `
            <tr>
                <td>${id}</td>
                <td>${name} (${code})</td>
                <td>${net}</td>
                <td>${current.toFixed(2)}</td>
                <td>
                    <button class="btn btn-secondary" onclick="showNetValueModal(${id})" style="margin-right:6px;">录入净值</button>
                    <button class="btn btn-secondary" onclick="showNetValueHistory(${id})" style="margin-right:6px;">历史记录</button>
                    <button class="btn btn-secondary" onclick="showNetValueChart(${id})">净值走势</button>
                </td>
            </tr>
        `;
        }).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="5" class="loading">加载失败: ${error.message}</td></tr>`;
    }
}

async function showNetValueModal(productId) {
    try {
        const product = await apiCall(`/api/products/${productId}`);
        const modalBody = document.getElementById('modalBody');
        const defaultNet = product.CurrentNetValue || product.currentNetValue || 1;
        const today = new Date().toISOString().split('T')[0];

        modalBody.innerHTML = `
            <h2>录入净值 - ${product.Name || product.name} (${product.Code || product.code || '-'})</h2>
            <form class="modal-form" id="netValueForm">
                <div class="form-group">
                    <label>净值记录</label>
                    <div id="netValueRows" class="mini-table"></div>
                    <button type="button" class="btn btn-secondary" id="addNetValueRowBtn" style="margin-top:8px;">新增一行</button>
                </div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                    <button type="submit" class="btn btn-primary">保存</button>
                </div>
            </form>
        `;
        document.getElementById('modal').classList.remove('hidden');

        const rowsDiv = document.getElementById('netValueRows');
        const addBtn = document.getElementById('addNetValueRowBtn');

        let rows = [
            { date: today, value: defaultNet }
        ];

        function renderRows() {
            rowsDiv.innerHTML = `
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>日期</th>
                            <th>净值</th>
                            <th>操作</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${rows.map((r, idx) => `
                            <tr data-idx="${idx}">
                                <td><input type="date" class="net-date" value="${r.date}"></td>
                                <td><input type="number" class="net-value" step="0.0001" min="0" value="${r.value}"></td>
                                <td>${rows.length > 1 ? `<button type="button" class="btn btn-danger btn-sm remove-row">删除</button>` : '-'}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;

            rowsDiv.querySelectorAll('.net-date').forEach((input, idx) => {
                input.addEventListener('input', () => {
                    rows[idx].date = input.value;
                });
            });
            rowsDiv.querySelectorAll('.net-value').forEach((input, idx) => {
                input.addEventListener('input', () => {
                    rows[idx].value = input.value;
                });
            });
            rowsDiv.querySelectorAll('.remove-row').forEach(btn => {
                btn.addEventListener('click', () => {
                    const idx = parseInt(btn.closest('tr').dataset.idx, 10);
                    rows.splice(idx, 1);
                    renderRows();
                });
            });
        }

        addBtn.addEventListener('click', () => {
            rows.push({ date: today, value: defaultNet });
            renderRows();
        });

        renderRows();

        document.getElementById('netValueForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            // 收集并校验
            const payload = [];
            for (const r of rows) {
                const val = parseFloat(r.value);
                if (!r.date) {
                    alert('日期不能为空');
                    return;
                }
                if (isNaN(val) || val <= 0) {
                    alert('净值必须大于0');
                    return;
                }
                payload.push({
                    NetValueDate: r.date,
                    NetValue: val
                });
            }
            try {
                // 批量导入接口，后端会取最新日期更新当前净值
                await apiCall(`/api/products/${productId}/netvalues/batch`, {
                    method: 'POST',
                    body: payload
                });
                closeModal();
                loadNetValues();
                alert('录入成功');
            } catch (err) {
                alert('录入失败: ' + err.message);
            }
        });
    } catch (error) {
        alert('加载产品信息失败: ' + error.message);
    }
}

async function showNetValueHistory(productId) {
    try {
        const [product, netValues] = await Promise.all([
            apiCall(`/api/products/${productId}`),
            apiCall(`/api/products/${productId}/netvalues`)
        ]);

        if (!Array.isArray(netValues) || netValues.length === 0) {
            alert('暂无净值记录');
            return;
        }

        const modalBody = document.getElementById('modalBody');
        const rows = netValues.map(nv => {
            const id = nv.Id || nv.id;
            const value = nv.NetValue || nv.netValue || 0;
            const date = nv.NetValueDate || nv.netValueDate;
            const createdAt = nv.CreatedAt || nv.createdAt;
            return `
                <tr>
                    <td>${id}</td>
                    <td>${value}</td>
                    <td>${new Date(date).toLocaleDateString('zh-CN')}</td>
                    <td>${createdAt ? new Date(createdAt).toLocaleString('zh-CN') : '-'}</td>
                    <td><button class="btn btn-danger btn-sm" onclick="deleteNetValue(${productId}, ${id})">删除</button></td>
                </tr>
            `;
        }).join('');

        modalBody.innerHTML = `
            <h2>净值历史 - ${product.Name || product.name} (${product.Code || product.code || '-'})</h2>
            <table class="data-table">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>净值</th>
                        <th>日期</th>
                        <th>创建时间</th>
                        <th>操作</th>
                    </tr>
                </thead>
                <tbody>
                    ${rows}
                </tbody>
            </table>
        `;
        document.getElementById('modal').classList.remove('hidden');
    } catch (error) {
        alert('加载净值历史失败: ' + error.message);
    }
}

async function showNetValueChart(productId) {
    try {
        const [product, chartData] = await Promise.all([
            apiCall(`/api/products/${productId}`),
            apiCall(`/api/products/${productId}/netvalues/chart`)
        ]);

        if (!Array.isArray(chartData) || chartData.length === 0) {
            alert('暂无净值数据');
            return;
        }

        // 准备坐标
        const width = 700;
        const height = 260;
        const pad = 40;
        const xs = chartData.map((_, i) => i);
        const ys = chartData.map(d => d.value || d.Value || d.netValue || d.NetValue || 0);
        const minY = Math.min(...ys);
        const maxY = Math.max(...ys);
        const yRange = maxY - minY < 0.0001 ? 0.0001 : (maxY - minY);
        const xStep = chartData.length > 1 ? (width - pad * 2) / (chartData.length - 1) : 0;

        const points = chartData.map((d, i) => {
            const x = pad + i * xStep;
            const y = pad + (height - pad * 2) * (1 - ( (d.value ?? d.Value ?? d.netValue ?? d.NetValue ?? 0) - minY) / yRange);
            return { x, y, label: d.date || d.Date || '' };
        });

        const path = points.map((p, idx) => `${idx === 0 ? 'M' : 'L'}${p.x},${p.y}`).join(' ');

        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>净值走势 - ${product.Name || product.name} (${product.Code || product.code || '-'})</h2>
            <div style="overflow-x:auto;">
                <svg width="${width}" height="${height}" style="background:#fafafa;border:1px solid #eee;border-radius:4px;">
                    <g stroke="#ddd" stroke-width="1">
                        <line x1="${pad}" y1="${pad}" x2="${pad}" y2="${height - pad}"></line>
                        <line x1="${pad}" y1="${height - pad}" x2="${width - pad}" y2="${height - pad}"></line>
                    </g>
                    <path d="${path}" fill="none" stroke="#4a90e2" stroke-width="2"></path>
                    ${points.map(p => `<circle cx="${p.x}" cy="${p.y}" r="3" fill="#4a90e2"></circle>`).join('')}
                    ${points.map((p, idx) => idx % Math.max(1, Math.floor(points.length / 6)) === 0 ? `<text x="${p.x}" y="${height - pad + 15}" font-size="10" text-anchor="middle" fill="#666">${chartData[idx].date || chartData[idx].Date || ''}</text>` : '').join('')}
                    <text x="${pad - 10}" y="${pad + 10}" font-size="10" text-anchor="end" fill="#666">${maxY.toFixed(4)}</text>
                    <text x="${pad - 10}" y="${height - pad}" font-size="10" text-anchor="end" fill="#666">${minY.toFixed(4)}</text>
                </svg>
            </div>
            <div style="margin-top:10px;max-height:200px;overflow-y:auto;">
                ${chartData.map(d => {
                    const date = d.date || d.Date || d.NetValueDate || d.netValueDate || '';
                    const val = d.value || d.Value || d.netValue || d.NetValue || 0;
                    return `<div style="font-size:12px;color:#555;">${date}: ${val}</div>`;
                }).join('')}
            </div>
            <div class="form-actions" style="margin-top:10px;">
                <button class="btn btn-secondary" onclick="closeModal()">关闭</button>
            </div>
        `;
        document.getElementById('modal').classList.remove('hidden');
    } catch (error) {
        alert('加载净值走势失败: ' + error.message);
    }
}

// 加载分红产品列表
async function loadDividendProducts() {
    const tbody = document.getElementById('dividendsTableBody');
    tbody.innerHTML = '<tr><td colspan="6" class="loading">加载中...</td></tr>';
    
    try {
        // 复用 shares summary 获取产品、总权益、初始权益
        const products = await apiCall('/api/shares/summary');
        if (!Array.isArray(products)) {
            tbody.innerHTML = '<tr><td colspan="6" class="loading">数据格式错误</td></tr>';
            return;
        }
        if (products.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="loading">暂无数据</td></tr>';
            return;
        }
        
        tbody.innerHTML = products.map(p => {
            const id = p.Id || p.id;
            const name = p.Name || p.name;
            const code = p.Code || p.code || '-';
            const initialAmount = p.InitialAmount || p.initialAmount || 0;
            const totalAmount = p.TotalAmount || p.totalAmount || 0;
            const distributable = totalAmount - initialAmount;
            return `
            <tr>
                <td>${id}</td>
                <td>${name} (${code})</td>
                <td>${totalAmount.toFixed(2)}</td>
                <td>${initialAmount.toFixed(2)}</td>
                <td>${distributable.toFixed(2)}</td>
                <td>
                    <button class="btn btn-secondary" onclick="showDividendModal(${id})" style="margin-right:6px;">分红管理</button>
                    <button class="btn btn-secondary" onclick="showDividendHistory(${id})">分红历史</button>
                </td>
            </tr>
        `;
        }).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="6" class="loading">加载失败: ${error.message}</td></tr>`;
    }
}

// 加载增减资产品列表
async function loadCapitalIncreases() {
    const tbody = document.getElementById('capitalincreasesTableBody');
    tbody.innerHTML = '<tr><td colspan="5" class="loading">加载中...</td></tr>';
    
    try {
        const products = await apiCall('/api/shares/summary');
        if (!Array.isArray(products) || products.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="loading">暂无数据</td></tr>';
            return;
        }
        
        tbody.innerHTML = products.map(p => {
            const id = p.Id || p.id;
            const name = p.Name || p.name;
            const code = p.Code || p.code || '-';
            const initialAmount = p.InitialAmount || p.initialAmount || 0;
            const totalAmount = p.TotalAmount || p.totalAmount || 0;
            const netValue = p.CurrentNetValue || p.currentNetValue || 1;
            return `
            <tr>
                <td>${id}</td>
                <td>${name} (${code})</td>
                <td>${initialAmount.toFixed(2)}</td>
                <td>${totalAmount.toFixed(2)}</td>
                <td>${netValue.toFixed(4)}</td>
                <td>
                    <button class="btn btn-secondary" onclick="showCapitalAdjustModal(${id})" style="margin-right:6px;">增减资</button>
                    <button class="btn btn-secondary" onclick="showCapitalHistory(${id})">操作记录</button>
                </td>
            </tr>
        `;
        }).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="5" class="loading">加载失败: ${error.message}</td></tr>`;
    }
}

// 加载管理员列表
async function loadAdmins() {
    const tbody = document.getElementById('adminsTableBody');
    tbody.innerHTML = '<tr><td colspan="4" class="loading">加载中...</td></tr>';
    
    try {
        const admins = await apiCall('/api/system/admins');
        if (admins.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" class="loading">暂无数据</td></tr>';
            return;
        }
        
        tbody.innerHTML = admins.map(admin => {
            // 支持 PascalCase 和 camelCase
            const id = admin.Id || admin.id;
            const username = admin.Username || admin.username;
            const createdAt = admin.CreatedAt || admin.createdAt;
            
            return `
            <tr>
                <td>${id}</td>
                <td>${username}</td>
                <td>${new Date(createdAt).toLocaleString('zh-CN')}</td>
                <td>
                    <button class="btn btn-danger" onclick="deleteAdmin(${id})">删除</button>
                </td>
            </tr>
        `;
        }).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="4" class="loading">加载失败: ${error.message}</td></tr>`;
    }
}

// 显示添加产品模态框（已移动到文件末尾的完整版本）

// 显示添加持有者模态框
function showAddHolderModal() {
    const modalBody = document.getElementById('modalBody');
    modalBody.innerHTML = `
        <h2>新增持有者</h2>
        <form class="modal-form" id="addHolderForm">
            <div class="form-group">
                <label>姓名 <span style="color: red;">*</span></label>
                <input type="text" name="name" required>
            </div>
            <div class="form-group">
                <label>手机号（登录账号）<span style="color: red;">*</span></label>
                <input type="text" name="phone" required>
            </div>
            <div class="form-group">
                <label>密码 <span style="color: red;">*</span></label>
                <input type="password" name="password" required>
            </div>
            <div class="form-group">
                <label>联系电话</label>
                <input type="text" name="phoneNumber">
            </div>
            <div class="form-group">
                <label>邮箱</label>
                <input type="email" name="email">
            </div>
            <div class="form-group">
                <label>开户银行 <span style="color: red;">*</span></label>
                <input type="text" name="bankName" required>
            </div>
            <div class="form-group">
                <label>银行卡号 <span style="color: red;">*</span></label>
                <input type="text" name="bankAccount" required>
            </div>
            <div class="form-group">
                <label>户名 <span style="color: red;">*</span></label>
                <input type="text" name="accountName" required>
            </div>
            <div class="form-actions">
                <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                <button type="submit" class="btn btn-primary">保存</button>
            </div>
        </form>
    `;
    
    document.getElementById('modal').classList.remove('hidden');
    document.getElementById('addHolderForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const formData = new FormData(e.target);
        // 转换为 PascalCase 以匹配后端
        const data = {
            Name: formData.get('name'),
            Phone: formData.get('phone'),
            Password: formData.get('password'),
            PhoneNumber: formData.get('phoneNumber') || null,
            Email: formData.get('email') || null,
            BankName: formData.get('bankName') || null,
            BankAccount: formData.get('bankAccount') || null,
            AccountName: formData.get('accountName') || null
        };
        
        try {
            await apiCall('/api/holders', {
                method: 'POST',
                body: data
            });
            closeModal();
            loadHolders();
            alert('持有者创建成功');
        } catch (error) {
            alert('创建失败: ' + error.message);
        }
    });
}

// 显示编辑持有者模态框
async function showEditHolderModal(holderId) {
    try {
        const holder = await apiCall(`/api/holders/${holderId}`);
        
        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>编辑持有者</h2>
            <form class="modal-form" id="editHolderForm">
                <div class="form-group">
                    <label>姓名 <span style="color: red;">*</span></label>
                    <input type="text" name="name" value="${holder.Name || holder.name || ''}" required>
                </div>
                <div class="form-group">
                    <label>手机号</label>
                    <input type="text" name="phone" value="${holder.Phone || holder.phone || ''}" readonly style="background-color: #f5f5f5;">
                </div>
                <div class="form-group">
                    <label>联系电话</label>
                    <input type="text" name="phoneNumber" value="${holder.PhoneNumber || holder.phoneNumber || ''}">
                </div>
                <div class="form-group">
                    <label>邮箱</label>
                    <input type="email" name="email" value="${holder.Email || holder.email || ''}">
                </div>
                <div class="form-group">
                    <label>开户银行 <span style="color: red;">*</span></label>
                    <input type="text" name="bankName" value="${holder.BankName || holder.bankName || ''}" required>
                </div>
                <div class="form-group">
                    <label>银行卡号 <span style="color: red;">*</span></label>
                    <input type="text" name="bankAccount" value="${holder.BankAccount || holder.bankAccount || ''}" required>
                </div>
                <div class="form-group">
                    <label>户名 <span style="color: red;">*</span></label>
                    <input type="text" name="accountName" value="${holder.AccountName || holder.accountName || ''}" required>
                </div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                    <button type="submit" class="btn btn-primary">保存</button>
                </div>
            </form>
        `;
        
        document.getElementById('modal').classList.remove('hidden');
        document.getElementById('editHolderForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const formData = new FormData(e.target);
            // 转换为 PascalCase 以匹配后端
            const data = {
                Name: formData.get('name'),
                PhoneNumber: formData.get('phoneNumber') || null,
                Email: formData.get('email') || null,
                BankName: formData.get('bankName') || null,
                BankAccount: formData.get('bankAccount') || null,
                AccountName: formData.get('accountName') || null
            };
            
            try {
                await apiCall(`/api/holders/${holderId}`, {
                    method: 'PUT',
                    body: data
                });
                closeModal();
                loadHolders();
                alert('持有者信息更新成功');
            } catch (error) {
                alert('更新失败: ' + error.message);
            }
        });
    } catch (error) {
        alert('加载持有者信息失败: ' + error.message);
    }
}

// 显示修改密码模态框
function showChangePasswordModal(holderId) {
    const modalBody = document.getElementById('modalBody');
    modalBody.innerHTML = `
        <h2>修改密码</h2>
        <form class="modal-form" id="changePasswordForm">
            <div class="form-group">
                <label>新密码 <span style="color: red;">*</span></label>
                <input type="password" name="newPassword" required>
            </div>
            <div class="form-actions">
                <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                <button type="submit" class="btn btn-primary">保存</button>
            </div>
        </form>
    `;
    
    document.getElementById('modal').classList.remove('hidden');
    document.getElementById('changePasswordForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const formData = new FormData(e.target);
        const data = {
            NewPassword: formData.get('newPassword')
        };
        
        try {
            await apiCall(`/api/holders/${holderId}/password`, {
                method: 'PUT',
                body: data
            });
            closeModal();
            alert('密码修改成功');
        } catch (error) {
            alert('修改失败: ' + error.message);
        }
    });
}

// 将函数暴露到全局作用域
window.showEditHolderModal = showEditHolderModal;
window.showChangePasswordModal = showChangePasswordModal;

// 备份数据库
async function handleBackup() {
    if (!confirm('确定要备份数据库吗？')) return;
    
    try {
        const result = await apiCall('/api/system/backup', { method: 'POST' });
        alert('备份成功: ' + result.fileName);
    } catch (error) {
        alert('备份失败: ' + error.message);
    }
}

// 关闭模态框
function closeModal() {
    document.getElementById('modal').classList.add('hidden');
}

// 删除函数（全局，供HTML调用）
window.deleteProduct = async (id) => {
    if (!confirm('确定要删除这个产品吗？')) return;
    try {
        await apiCall(`/api/products/${id}`, { method: 'DELETE' });
        loadProducts();
        alert('删除成功');
    } catch (error) {
        alert('删除失败: ' + error.message);
    }
};

window.deleteHolder = async (id) => {
    if (!confirm('确定要删除这个持有者吗？')) return;
    try {
        await apiCall(`/api/holders/${id}`, { method: 'DELETE' });
        loadHolders();
        alert('删除成功');
    } catch (error) {
        alert('删除失败: ' + error.message);
    }
};

window.deleteNetValue = async (productId, id) => {
    if (!confirm('确定要删除这条净值记录吗？')) return;
    try {
        await apiCall(`/api/products/${productId}/netvalues/${id}`, { method: 'DELETE' });
        // 若处于历史弹窗，刷新历史；否则刷新列表
        const modalVisible = !document.getElementById('modal').classList.contains('hidden');
        if (modalVisible) {
            showNetValueHistory(productId);
        } else {
        loadNetValues();
        }
        alert('删除成功');
    } catch (error) {
        alert('删除失败: ' + error.message);
    }
};

window.deleteDividend = async (id) => {
    if (!confirm('确定要删除这条分红记录吗？')) return;
    try {
        await apiCall(`/api/dividends/${id}`, { method: 'DELETE' });
        loadDividends();
        alert('删除成功');
    } catch (error) {
        alert('删除失败: ' + error.message);
    }
};

async function showCapitalAdjustModal(productId) {
    try {
        const [product, holders] = await Promise.all([
            apiCall(`/api/products/${productId}`),
            apiCall('/api/holders')
        ]);

        const modalBody = document.getElementById('modalBody');

        const existingShares = (product.HolderShares || product.holderShares || []).map(hs => ({
            holderId: hs.HolderId || hs.holderId,
            holderName: (hs.Holder && hs.Holder.Name) || hs.holderName || (hs.holder && hs.holder.name) || '-',
            shareAmount: hs.ShareAmount || hs.shareAmount || 0,
            shareType: hs.ShareType || hs.shareType || 'Subordinate'
        }));

        const netValue = product.CurrentNetValue || product.currentNetValue || 1;
        const initialAmount = product.InitialAmount || product.initialAmount || 0;
        const totalAmount = product.TotalAmount || product.totalAmount || 0;

        const disabledReason = (() => {
            if (Math.abs(netValue - 1) > 0.0001) return '净值需为1才能增减资';
            if (initialAmount !== totalAmount) return '当前权益需等于初始权益才能增减资';
            return '';
        })();

        const holderOptions = holders.map(h => ({
            id: h.Id || h.id,
            name: h.Name || h.name
        }));

        modalBody.innerHTML = `
            <h2>增减资 - ${product.Name || product.name} (${product.Code || product.code || '-'})</h2>
            <p>初始权益：${initialAmount.toFixed(2)}，当前权益：${totalAmount.toFixed(2)}，净值：${netValue.toFixed(4)}</p>
            ${disabledReason ? `<div class="alert">${disabledReason}</div>` : ''}
            <form class="modal-form" id="capitalForm">
                <div class="form-group">
                    <label>类型</label>
                    <select name="type">
                        <option value="Increase">增资</option>
                        <option value="Decrease">减资</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>金额</label>
                    <input type="number" name="amount" step="0.01" min="0" required>
                </div>
                <div class="form-group">
                    <label>日期</label>
                    <input type="date" name="date" value="${new Date().toISOString().split('T')[0]}">
                </div>
                <div class="form-group">
                    <label>备注</label>
                    <textarea name="remarks" rows="2" placeholder="可选"></textarea>
                </div>
                <div class="form-group">
                    <div style="display:flex;justify-content:space-between;align-items:center;">
                        <label>持有人分配（1元=1份）</label>
                        <button type="button" class="btn btn-secondary" id="addHolderRowBtn">新增持有人</button>
                    </div>
                    <div id="capitalAllocTable" class="mini-table"></div>
                </div>
                <div id="capitalSummary" style="padding:8px;background:#f5f5f5;border-radius:4px;">-</div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                    <button type="submit" class="btn btn-primary" ${disabledReason ? 'disabled' : ''}>提交</button>
                </div>
            </form>
        `;

        document.getElementById('modal').classList.remove('hidden');

        const form = document.getElementById('capitalForm');
        const tableDiv = document.getElementById('capitalAllocTable');
        const summaryDiv = document.getElementById('capitalSummary');
        const addBtn = document.getElementById('addHolderRowBtn');

        let rows = existingShares.map(s => ({
            holderId: s.holderId,
            holderName: s.holderName,
            shareType: s.shareType || 'Subordinate',
            shareAmount: s.shareAmount,
            isNew: false,
            maxShare: s.shareAmount
        }));

        function renderRows() {
            tableDiv.innerHTML = `
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>持有人</th>
                            <th>份额/金额</th>
                            <th>份额类型</th>
                            <th>操作</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${rows.map(r => `
                            <tr data-holder-id="${r.holderId}" data-is-new="${r.isNew}" data-max="${r.maxShare}">
                                <td>${r.holderName}</td>
                                <td><input type="number" class="input-share" value="${r.shareAmount}" step="0.01" min="0"></td>
                                <td>${r.isNew ? `
                                    <select class="input-share-type">
                                        <option value="Subordinate" ${r.shareType === 'Subordinate' ? 'selected' : ''}>劣后</option>
                                        <option value="Priority" ${r.shareType === 'Priority' ? 'selected' : ''}>优先</option>
                                    </select>
                                ` : r.shareType}</td>
                                <td>${r.isNew ? `<button type="button" class="btn btn-danger btn-sm remove-row">移除</button>` : '-'}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;

            tableDiv.querySelectorAll('.input-share').forEach((input, idx) => {
                input.addEventListener('input', () => updateSummary());
            });
            tableDiv.querySelectorAll('.input-share-type').forEach((select, idx) => {
                select.addEventListener('change', () => updateSummary());
            });
            tableDiv.querySelectorAll('.remove-row').forEach((btn, idx) => {
                btn.addEventListener('click', () => {
                    const tr = btn.closest('tr');
                    const holderId = parseInt(tr.dataset.holderId, 10);
                    rows = rows.filter(r => r.holderId !== holderId || !r.isNew);
                    renderRows();
                    updateSummary();
                });
            });
        }

        function updateSummary() {
            const type = form.type.value;
            const amount = parseFloat(form.amount.value) || 0;
            const targetInitial = type === 'Decrease' ? initialAmount - amount : initialAmount + amount;
            const inputs = tableDiv.querySelectorAll('.input-share');
            let total = 0;
            inputs.forEach(input => {
                total += parseFloat(input.value || '0');
            });
            summaryDiv.innerHTML = `
                目标初始权益：${targetInitial.toFixed(2)}；分配合计：${total.toFixed(2)}；
                ${Math.abs(total - targetInitial) < 0.0001 ? '<span style="color:green;">已平衡</span>' : '<span style="color:red;">未平衡</span>'}
            `;
        }

        addBtn.addEventListener('click', () => {
            if (form.type.value === 'Decrease') {
                alert('减资不允许新增持有人');
                return;
            }
            const existingIds = rows.map(r => r.holderId);
            const available = holderOptions.filter(h => !existingIds.includes(h.id));
            if (available.length === 0) {
                alert('没有可添加的持有人');
                return;
            }
            const pick = available[0];
            rows.push({
                holderId: pick.id,
                holderName: pick.name,
                shareType: 'Subordinate',
                shareAmount: 0,
                isNew: true,
                maxShare: 0
            });
            renderRows();
            updateSummary();
        });

        form.type.addEventListener('change', () => {
            if (form.type.value === 'Decrease') {
                addBtn.disabled = true;
            } else {
                addBtn.disabled = false;
            }
            updateSummary();
        });

        form.amount.addEventListener('input', updateSummary);

        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const type = form.type.value;
            const amount = parseFloat(form.amount.value) || 0;
            const targetInitial = type === 'Decrease' ? initialAmount - amount : initialAmount + amount;
            const inputs = Array.from(tableDiv.querySelectorAll('tbody tr'));

            const allocations = inputs.map(tr => {
                const holderId = parseInt(tr.dataset.holderId, 10);
                const isNew = tr.dataset.isNew === 'true';
                const max = parseFloat(tr.dataset.max || '0');
                const shareInput = tr.querySelector('.input-share');
                const shareAmount = parseFloat(shareInput.value || '0');
                const shareTypeSelect = tr.querySelector('.input-share-type');
                const shareType = shareTypeSelect ? shareTypeSelect.value : (rows.find(r => r.holderId === holderId)?.shareType || 'Subordinate');
                return { holderId, isNew, max, shareAmount, shareType };
            });

            // 校验
            if (amount <= 0) {
                alert('金额必须大于0');
                return;
            }
            const total = allocations.reduce((sum, a) => sum + a.shareAmount, 0);
            if (Math.abs(total - targetInitial) > 0.0001) {
                alert('分配合计必须等于目标初始权益');
                return;
            }
            if (type === 'Decrease') {
                if (allocations.some(a => a.isNew)) {
                    alert('减资不允许新增持有人');
                    return;
                }
                const exceed = allocations.find(a => a.shareAmount > a.max + 0.0001);
                if (exceed) {
                    alert('减资后份额不能超过原持仓（只能减持或清仓）');
                    return;
                }
            }

            try {
                await apiCall('/api/capitalincreases', {
                    method: 'POST',
                    body: {
                        ProductId: productId,
                        Amount: amount,
                        Type: type,
                        IncreaseDate: form.date.value || new Date().toISOString(),
                        Remarks: form.remarks.value || '',
                        Allocations: allocations.map(a => ({
                            HolderId: a.holderId,
                            ShareAmount: a.shareAmount,
                            ShareType: a.shareType
                        }))
                    }
                });
                closeModal();
                loadCapitalIncreases();
                alert('操作成功');
            } catch (err) {
                alert('提交失败: ' + err.message);
            }
        });

        renderRows();
        updateSummary();
    } catch (error) {
        alert('加载增减资数据失败: ' + error.message);
    }
}

async function showCapitalHistory(productId) {
    try {
        const records = await apiCall(`/api/capitalincreases/product/${productId}`);
        if (!Array.isArray(records) || records.length === 0) {
            alert('暂无记录');
            return;
        }
        const modalBody = document.getElementById('modalBody');
        const rows = records.map(r => {
            const type = r.Type || r.type || 'Increase';
            const amountBefore = r.AmountBefore || r.amountBefore || 0;
            const amount = r.IncreaseAmount || r.increaseAmount || 0;
            const amountAfter = r.AmountAfter || r.amountAfter || 0;
            const date = r.IncreaseDate || r.increaseDate;
            const details = r.Details || r.details;
            let detailHtml = '';
            if (details) {
                try {
                    const parsed = JSON.parse(details);
                    if (Array.isArray(parsed) && parsed.length > 0) {
                        detailHtml = parsed.map(d => {
                            const name = d.HolderName || d.holderName || d.HolderId || d.holderId;
                            const share = d.ShareAmount || d.shareAmount || 0;
                            const rawType = d.ShareType || d.shareType || '';
                            const shareType = rawType === 'Priority' ? '优先' : rawType === 'Subordinate' ? '劣后' : (rawType || '-');
                            return `<div>${name}: ${share.toFixed(2)} (${shareType})</div>`;
                        }).join('');
                    }
                } catch (e) {
                    detailHtml = details;
                }
            }
            return `
                <tr>
                    <td>${r.Id || r.id}</td>
                    <td>${type === 'Decrease' ? '减资' : '增资'}</td>
                    <td>${amountBefore.toFixed(2)}</td>
                    <td>${amount.toFixed(2)}</td>
                    <td>${amountAfter.toFixed(2)}</td>
                    <td>${new Date(date).toLocaleDateString('zh-CN')}</td>
                    <td>${detailHtml || '-'}</td>
                    <td>
                        <button class="btn btn-danger btn-sm" onclick="deleteCapitalIncrease(${r.Id || r.id}, true)">删除</button>
                    </td>
                </tr>
            `;
        }).join('');

        modalBody.innerHTML = `
            <h2>增减资记录</h2>
            <table class="data-table">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>类型</th>
                        <th>操作前</th>
                        <th>金额</th>
                        <th>操作后</th>
                        <th>日期</th>
                        <th>明细</th>
                        <th>操作</th>
                    </tr>
                </thead>
                <tbody>
                    ${rows}
                </tbody>
            </table>
        `;
        document.getElementById('modal').classList.remove('hidden');
    } catch (error) {
        alert('加载记录失败: ' + error.message);
    }
}

window.deleteCapitalIncrease = async (id, keepModal) => {
    if (!confirm('确定要删除这条增减资记录吗？')) return;
    try {
        await apiCall(`/api/capitalincreases/${id}`, { method: 'DELETE' });
        loadCapitalIncreases();
        if (!keepModal) closeModal();
        alert('删除成功');
    } catch (error) {
        alert('删除失败: ' + error.message);
    }
};

window.deleteAdmin = async (id) => {
    if (!confirm('确定要删除这个管理员吗？')) return;
    try {
        await apiCall(`/api/system/admins/${id}`, { method: 'DELETE' });
        loadAdmins();
        alert('删除成功');
    } catch (error) {
        alert('删除失败: ' + error.message);
    }
};

window.closeModal = closeModal;

// 加载投顾列表
async function loadAdvisors() {
    const tbody = document.getElementById('advisorsTableBody');
    tbody.innerHTML = '<tr><td colspan="7" class="loading">加载中...</td></tr>';
    
    try {
        const advisors = await apiCall('/api/advisors');
        if (advisors.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="loading">暂无数据</td></tr>';
            return;
        }
        
        tbody.innerHTML = advisors.map(advisor => {
            const id = advisor.Id || advisor.id;
            const name = advisor.Name || advisor.name;
            const contactPerson = advisor.ContactPerson || advisor.contactPerson || '-';
            const phone = advisor.Phone || advisor.phone || '-';
            const email = advisor.Email || advisor.email || '-';
            const createdAt = advisor.CreatedAt || advisor.createdAt;
            
            return `
            <tr>
                <td>${id}</td>
                <td>${name}</td>
                <td>${contactPerson}</td>
                <td>${phone}</td>
                <td>${email}</td>
                <td>${new Date(createdAt).toLocaleString('zh-CN')}</td>
                <td>
                    <button class="btn btn-secondary" onclick="showEditAdvisorModal(${id})" style="margin-right: 5px;">编辑</button>
                    <button class="btn btn-danger" onclick="deleteAdvisor(${id})">删除</button>
                </td>
            </tr>
        `;
        }).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="7" class="loading">加载失败: ${error.message}</td></tr>`;
    }
}

// 加载管理方列表
async function loadManagers() {
    const tbody = document.getElementById('managersTableBody');
    tbody.innerHTML = '<tr><td colspan="7" class="loading">加载中...</td></tr>';
    
    try {
        const managers = await apiCall('/api/managers');
        if (managers.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="loading">暂无数据</td></tr>';
            return;
        }
        
        tbody.innerHTML = managers.map(manager => {
            const id = manager.Id || manager.id;
            const name = manager.Name || manager.name;
            const contactPerson = manager.ContactPerson || manager.contactPerson || '-';
            const phone = manager.Phone || manager.phone || '-';
            const email = manager.Email || manager.email || '-';
            const createdAt = manager.CreatedAt || manager.createdAt;
            
            return `
            <tr>
                <td>${id}</td>
                <td>${name}</td>
                <td>${contactPerson}</td>
                <td>${phone}</td>
                <td>${email}</td>
                <td>${new Date(createdAt).toLocaleString('zh-CN')}</td>
                <td>
                    <button class="btn btn-secondary" onclick="showEditManagerModal(${id})" style="margin-right: 5px;">编辑</button>
                    <button class="btn btn-danger" onclick="deleteManager(${id})">删除</button>
                </td>
            </tr>
        `;
        }).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="7" class="loading">加载失败: ${error.message}</td></tr>`;
    }
}

// 显示新增投顾模态框
function showAddAdvisorModal() {
    const modalBody = document.getElementById('modalBody');
    modalBody.innerHTML = `
        <h2>新增投顾</h2>
        <form class="modal-form" id="addAdvisorForm">
            <div class="form-group">
                <label>名称 <span style="color: red;">*</span></label>
                <input type="text" name="name" required>
            </div>
            <div class="form-group">
                <label>联系人</label>
                <input type="text" name="contactPerson">
            </div>
            <div class="form-group">
                <label>联系电话</label>
                <input type="text" name="phone">
            </div>
            <div class="form-group">
                <label>邮箱</label>
                <input type="email" name="email">
            </div>
            <div class="form-group">
                <label>备注</label>
                <textarea name="remarks" rows="3"></textarea>
            </div>
            <div class="form-actions">
                <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                <button type="submit" class="btn btn-primary">保存</button>
            </div>
        </form>
    `;
    
    document.getElementById('modal').classList.remove('hidden');
    document.getElementById('addAdvisorForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const formData = new FormData(e.target);
        const data = {
            Name: formData.get('name'),
            ContactPerson: formData.get('contactPerson') || null,
            Phone: formData.get('phone') || null,
            Email: formData.get('email') || null,
            Remarks: formData.get('remarks') || null
        };
        
        try {
            await apiCall('/api/advisors', {
                method: 'POST',
                body: data
            });
            closeModal();
            loadAdvisors();
            alert('投顾创建成功');
        } catch (error) {
            alert('创建失败: ' + error.message);
        }
    });
}

// 显示编辑投顾模态框
async function showEditAdvisorModal(advisorId) {
    try {
        const advisor = await apiCall(`/api/advisors/${advisorId}`);
        
        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>编辑投顾</h2>
            <form class="modal-form" id="editAdvisorForm">
                <div class="form-group">
                    <label>名称 <span style="color: red;">*</span></label>
                    <input type="text" name="name" value="${advisor.Name || advisor.name || ''}" required>
                </div>
                <div class="form-group">
                    <label>联系人</label>
                    <input type="text" name="contactPerson" value="${advisor.ContactPerson || advisor.contactPerson || ''}">
                </div>
                <div class="form-group">
                    <label>联系电话</label>
                    <input type="text" name="phone" value="${advisor.Phone || advisor.phone || ''}">
                </div>
                <div class="form-group">
                    <label>邮箱</label>
                    <input type="email" name="email" value="${advisor.Email || advisor.email || ''}">
                </div>
                <div class="form-group">
                    <label>备注</label>
                    <textarea name="remarks" rows="3">${advisor.Remarks || advisor.remarks || ''}</textarea>
                </div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                    <button type="submit" class="btn btn-primary">保存</button>
                </div>
            </form>
        `;
        
        document.getElementById('modal').classList.remove('hidden');
        document.getElementById('editAdvisorForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const formData = new FormData(e.target);
            const data = {
                Name: formData.get('name'),
                ContactPerson: formData.get('contactPerson') || null,
                Phone: formData.get('phone') || null,
                Email: formData.get('email') || null,
                Remarks: formData.get('remarks') || null
            };
            
            try {
                await apiCall(`/api/advisors/${advisorId}`, {
                    method: 'PUT',
                    body: data
                });
                closeModal();
                loadAdvisors();
                alert('投顾信息更新成功');
            } catch (error) {
                alert('更新失败: ' + error.message);
            }
        });
    } catch (error) {
        alert('加载投顾信息失败: ' + error.message);
    }
}

// 显示新增管理方模态框
function showAddManagerModal() {
    const modalBody = document.getElementById('modalBody');
    modalBody.innerHTML = `
        <h2>新增管理方</h2>
        <form class="modal-form" id="addManagerForm">
            <div class="form-group">
                <label>名称 <span style="color: red;">*</span></label>
                <input type="text" name="name" required>
            </div>
            <div class="form-group">
                <label>联系人</label>
                <input type="text" name="contactPerson">
            </div>
            <div class="form-group">
                <label>联系电话</label>
                <input type="text" name="phone">
            </div>
            <div class="form-group">
                <label>邮箱</label>
                <input type="email" name="email">
            </div>
            <div class="form-group">
                <label>备注</label>
                <textarea name="remarks" rows="3"></textarea>
            </div>
            <div class="form-actions">
                <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                <button type="submit" class="btn btn-primary">保存</button>
            </div>
        </form>
    `;
    
    document.getElementById('modal').classList.remove('hidden');
    document.getElementById('addManagerForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const formData = new FormData(e.target);
        const data = {
            Name: formData.get('name'),
            ContactPerson: formData.get('contactPerson') || null,
            Phone: formData.get('phone') || null,
            Email: formData.get('email') || null,
            Remarks: formData.get('remarks') || null
        };
        
        try {
            await apiCall('/api/managers', {
                method: 'POST',
                body: data
            });
            closeModal();
            loadManagers();
            alert('管理方创建成功');
        } catch (error) {
            alert('创建失败: ' + error.message);
        }
    });
}

// 显示编辑管理方模态框
async function showEditManagerModal(managerId) {
    try {
        const manager = await apiCall(`/api/managers/${managerId}`);
        
        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>编辑管理方</h2>
            <form class="modal-form" id="editManagerForm">
                <div class="form-group">
                    <label>名称 <span style="color: red;">*</span></label>
                    <input type="text" name="name" value="${manager.Name || manager.name || ''}" required>
                </div>
                <div class="form-group">
                    <label>联系人</label>
                    <input type="text" name="contactPerson" value="${manager.ContactPerson || manager.contactPerson || ''}">
                </div>
                <div class="form-group">
                    <label>联系电话</label>
                    <input type="text" name="phone" value="${manager.Phone || manager.phone || ''}">
                </div>
                <div class="form-group">
                    <label>邮箱</label>
                    <input type="email" name="email" value="${manager.Email || manager.email || ''}">
                </div>
                <div class="form-group">
                    <label>备注</label>
                    <textarea name="remarks" rows="3">${manager.Remarks || manager.remarks || ''}</textarea>
                </div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                    <button type="submit" class="btn btn-primary">保存</button>
                </div>
            </form>
        `;
        
        document.getElementById('modal').classList.remove('hidden');
        document.getElementById('editManagerForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const formData = new FormData(e.target);
            const data = {
                Name: formData.get('name'),
                ContactPerson: formData.get('contactPerson') || null,
                Phone: formData.get('phone') || null,
                Email: formData.get('email') || null,
                Remarks: formData.get('remarks') || null
            };
            
            try {
                await apiCall(`/api/managers/${managerId}`, {
                    method: 'PUT',
                    body: data
                });
                closeModal();
                loadManagers();
                alert('管理方信息更新成功');
            } catch (error) {
                alert('更新失败: ' + error.message);
            }
        });
    } catch (error) {
        alert('加载管理方信息失败: ' + error.message);
    }
}

// 显示新增份额操作模态框
async function showAddShareOperationModal() {
    try {
        const [products, holders] = await Promise.all([
            apiCall('/api/products'),
            apiCall('/api/holders')
        ]);
        
        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>新增份额操作（初始出资）</h2>
            <form class="modal-form" id="addShareForm">
                <div class="form-group">
                    <label>产品 <span style="color: red;">*</span></label>
                    <select name="productId" required>
                        <option value="">请选择产品</option>
                        ${products.map(p => {
                            const id = p.Id || p.id;
                            const name = p.Name || p.name;
                            return `<option value="${id}">${name}</option>`;
                        }).join('')}
                    </select>
                </div>
                <div class="form-group">
                    <label>持有者 <span style="color: red;">*</span></label>
                    <select name="holderId" required>
                        <option value="">请选择持有者</option>
                        ${holders.map(h => {
                            const id = h.Id || h.id;
                            const name = h.Name || h.name;
                            return `<option value="${id}">${name}</option>`;
                        }).join('')}
                    </select>
                </div>
                <div class="form-group">
                    <label>份额类型 <span style="color: red;">*</span></label>
                    <select name="shareType" required>
                        <option value="Subordinate">劣后方</option>
                        <option value="Priority">优先方</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>份额数量 <span style="color: red;">*</span></label>
                    <input type="number" name="shareAmount" step="0.01" min="0" required>
                </div>
                <div class="form-group">
                    <label>投资金额 <span style="color: red;">*</span></label>
                    <input type="number" name="investmentAmount" step="0.01" min="0" required>
                </div>
                <div class="form-group">
                    <label>交易日期</label>
                    <input type="date" name="transactionDate" value="${new Date().toISOString().split('T')[0]}">
                </div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                    <button type="submit" class="btn btn-primary">保存</button>
                </div>
            </form>
        `;
        
        document.getElementById('modal').classList.remove('hidden');
        document.getElementById('addShareForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const formData = new FormData(e.target);
            const data = {
                ProductId: parseInt(formData.get('productId')),
                HolderId: parseInt(formData.get('holderId')),
                ShareType: formData.get('shareType'),
                ShareAmount: parseFloat(formData.get('shareAmount')),
                InvestmentAmount: parseFloat(formData.get('investmentAmount')),
                TransactionDate: formData.get('transactionDate') || new Date().toISOString()
            };
            
            try {
                await apiCall('/api/shares/initial', {
                    method: 'POST',
                    body: data
                });
                closeModal();
                loadShares();
                alert('份额操作成功');
            } catch (error) {
                alert('操作失败: ' + error.message);
            }
        });
    } catch (error) {
        alert('加载数据失败: ' + error.message);
    }
}

// 显示编辑产品模态框
async function showEditProductModal(productId) {
    try {
        const [product, advisors, managers] = await Promise.all([
            apiCall(`/api/products/${productId}`),
            apiCall('/api/advisors'),
            apiCall('/api/managers')
        ]);
        
        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>编辑产品</h2>
            <form class="modal-form" id="editProductForm">
                <div class="form-group">
                    <label>产品名称 <span style="color: red;">*</span></label>
                    <input type="text" name="name" value="${product.Name || product.name || ''}" required>
                </div>
                <div class="form-group">
                    <label>产品代码</label>
                    <input type="text" name="code" value="${product.Code || product.code || ''}">
                </div>
                <div class="form-group">
                    <label>投顾</label>
                    <select name="advisorId">
                        <option value="">无</option>
                        ${advisors.map(a => {
                            const id = a.Id || a.id;
                            const name = a.Name || a.name;
                            const selected = (product.AdvisorId || product.advisorId) == id ? 'selected' : '';
                            return `<option value="${id}" ${selected}>${name}</option>`;
                        }).join('')}
                    </select>
                </div>
                <div class="form-group">
                    <label>管理方</label>
                    <select name="managerId">
                        <option value="">无</option>
                        ${managers.map(m => {
                            const id = m.Id || m.id;
                            const name = m.Name || m.name;
                            const selected = (product.ManagerId || product.managerId) == id ? 'selected' : '';
                            return `<option value="${id}" ${selected}>${name}</option>`;
                        }).join('')}
                    </select>
                </div>
                <div class="form-group">
                    <label>当前总金额</label>
                    <input type="number" name="totalAmount" step="0.01" min="0" value="${(product.TotalAmount || product.totalAmount || 0)}">
                </div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                    <button type="submit" class="btn btn-primary">保存</button>
                </div>
            </form>
        `;
        
        document.getElementById('modal').classList.remove('hidden');
        document.getElementById('editProductForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const formData = new FormData(e.target);
            const data = {
                Name: formData.get('name'),
                Code: formData.get('code') || null,
                AdvisorId: formData.get('advisorId') ? parseInt(formData.get('advisorId')) : null,
                ManagerId: formData.get('managerId') ? parseInt(formData.get('managerId')) : null,
                TotalAmount: formData.get('totalAmount') !== '' ? parseFloat(formData.get('totalAmount')) : null
            };
            
            try {
                await apiCall(`/api/products/${productId}`, {
                    method: 'PUT',
                    body: data
                });
                closeModal();
                loadProducts();
                alert('产品信息更新成功');
            } catch (error) {
                alert('更新失败: ' + error.message);
            }
        });
    } catch (error) {
        alert('加载产品信息失败: ' + error.message);
    }
}

// 显示分配方案模态框
async function showDistributionPlanModal(productId) {
    try {
        const plan = await apiCall(`/api/distributionplans/product/${productId}`);
        
        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>分配方案配置</h2>
            <form class="modal-form" id="distributionPlanForm">
                <div class="form-group">
                    <label>优先方比例 (%) <span style="color: red;">*</span></label>
                    <input type="number" name="priorityRatio" step="0.01" min="0" max="100" 
                           value="${plan.PriorityRatio || plan.priorityRatio || 30}" required>
                </div>
                <div class="form-group">
                    <label>劣后方比例 (%) <span style="color: red;">*</span></label>
                    <input type="number" name="subordinateRatio" step="0.01" min="0" max="100" 
                           value="${plan.SubordinateRatio || plan.subordinateRatio || 40}" required>
                </div>
                <div class="form-group">
                    <label>管理方比例 (%) <span style="color: red;">*</span></label>
                    <input type="number" name="managerRatio" step="0.01" min="0" max="100" 
                           value="${plan.ManagerRatio || plan.managerRatio || 10}" required>
                </div>
                <div class="form-group">
                    <label>投顾方比例 (%) <span style="color: red;">*</span></label>
                    <input type="number" name="advisorRatio" step="0.01" min="0" max="100" 
                           value="${plan.AdvisorRatio || plan.advisorRatio || 20}" required>
                </div>
                <div class="form-group">
                    <div id="ratioSum" style="padding: 10px; background: #f5f5f5; border-radius: 5px;">
                        比例总和: <span id="ratioSumValue">0</span>%
                    </div>
                </div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                    <button type="submit" class="btn btn-primary">保存</button>
                </div>
            </form>
        `;
        
        // 实时计算比例总和
        const form = document.getElementById('distributionPlanForm');
        const inputs = form.querySelectorAll('input[type="number"]');
        const updateSum = () => {
            let sum = 0;
            inputs.forEach(input => {
                sum += parseFloat(input.value) || 0;
            });
            document.getElementById('ratioSumValue').textContent = sum.toFixed(2);
            if (Math.abs(sum - 100) > 0.01) {
                document.getElementById('ratioSum').style.background = '#ffebee';
            } else {
                document.getElementById('ratioSum').style.background = '#e8f5e9';
            }
        };
        inputs.forEach(input => input.addEventListener('input', updateSum));
        updateSum();
        
        document.getElementById('modal').classList.remove('hidden');
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const formData = new FormData(e.target);
            const data = {
                PriorityRatio: parseFloat(formData.get('priorityRatio')),
                SubordinateRatio: parseFloat(formData.get('subordinateRatio')),
                ManagerRatio: parseFloat(formData.get('managerRatio')),
                AdvisorRatio: parseFloat(formData.get('advisorRatio'))
            };
            
            try {
                await apiCall(`/api/distributionplans/${plan.Id || plan.id}`, {
                    method: 'PUT',
                    body: data
                });
                closeModal();
                alert('分配方案更新成功');
            } catch (error) {
                alert('更新失败: ' + error.message);
            }
        });
    } catch (error) {
        alert('加载分配方案失败: ' + error.message);
    }
}

// 完善产品创建模态框
function showAddProductModal() {
    Promise.all([
        apiCall('/api/advisors'),
        apiCall('/api/managers')
    ]).then(([advisors, managers]) => {
        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <h2>新增产品</h2>
            <form class="modal-form" id="addProductForm">
                <div class="form-group">
                    <label>产品名称 <span style="color: red;">*</span></label>
                    <input type="text" name="name" required>
                </div>
                <div class="form-group">
                    <label>产品代码</label>
                    <input type="text" name="code">
                </div>
                <div class="form-group">
                    <label>初始权益 <span style="color: red;">*</span></label>
                    <input type="number" name="initialAmount" step="0.01" min="0" value="0" required>
                </div>
                <div class="form-group">
                    <label>投顾</label>
                    <select name="advisorId">
                        <option value="">无</option>
                        ${advisors.map(a => {
                            const id = a.Id || a.id;
                            const name = a.Name || a.name;
                            return `<option value="${id}">${name}</option>`;
                        }).join('')}
                    </select>
                </div>
                <div class="form-group">
                    <label>管理方</label>
                    <select name="managerId">
                        <option value="">无</option>
                        ${managers.map(m => {
                            const id = m.Id || m.id;
                            const name = m.Name || m.name;
                            return `<option value="${id}">${name}</option>`;
                        }).join('')}
                    </select>
                </div>
                <div class="form-group">
                    <label>优先方比例 (%)</label>
                    <input type="number" name="priorityRatio" step="0.01" min="0" max="100" value="30">
                </div>
                <div class="form-group">
                    <label>劣后方比例 (%)</label>
                    <input type="number" name="subordinateRatio" step="0.01" min="0" max="100" value="40">
                </div>
                <div class="form-group">
                    <label>管理方比例 (%)</label>
                    <input type="number" name="managerRatio" step="0.01" min="0" max="100" value="10">
                </div>
                <div class="form-group">
                    <label>投顾方比例 (%)</label>
                    <input type="number" name="advisorRatio" step="0.01" min="0" max="100" value="20">
                </div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">取消</button>
                    <button type="submit" class="btn btn-primary">保存</button>
                </div>
            </form>
        `;
        
        document.getElementById('modal').classList.remove('hidden');
        document.getElementById('addProductForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const formData = new FormData(e.target);
            const data = {
                Name: formData.get('name'),
                Code: formData.get('code') || null,
                InitialAmount: parseFloat(formData.get('initialAmount')) || 0,
                AdvisorId: formData.get('advisorId') ? parseInt(formData.get('advisorId')) : null,
                ManagerId: formData.get('managerId') ? parseInt(formData.get('managerId')) : null,
                PriorityRatio: parseFloat(formData.get('priorityRatio')) || 30,
                SubordinateRatio: parseFloat(formData.get('subordinateRatio')) || 40,
                ManagerRatio: parseFloat(formData.get('managerRatio')) || 10,
                AdvisorRatio: parseFloat(formData.get('advisorRatio')) || 20
            };
            
            try {
                await apiCall('/api/products', {
                    method: 'POST',
                    body: data
                });
                closeModal();
                loadProducts();
                alert('产品创建成功');
            } catch (error) {
                alert('创建失败: ' + error.message);
            }
        });
    }).catch(error => {
        alert('加载数据失败: ' + error.message);
    });
}

// 删除函数
window.deleteAdvisor = async (id) => {
    if (!confirm('确定要删除这个投顾吗？')) return;
    try {
        await apiCall(`/api/advisors/${id}`, { method: 'DELETE' });
        loadAdvisors();
        alert('删除成功');
    } catch (error) {
        alert('删除失败: ' + error.message);
    }
};

window.deleteManager = async (id) => {
    if (!confirm('确定要删除这个管理方吗？')) return;
    try {
        await apiCall(`/api/managers/${id}`, { method: 'DELETE' });
        loadManagers();
        alert('删除成功');
    } catch (error) {
        alert('删除失败: ' + error.message);
    }
};

window.showEditAdvisorModal = showEditAdvisorModal;
window.showEditManagerModal = showEditManagerModal;
window.showAddAdvisorModal = showAddAdvisorModal;
window.showAddManagerModal = showAddManagerModal;
window.showAddShareOperationModal = showAddShareOperationModal;
window.showEditProductModal = showEditProductModal;
window.showDistributionPlanModal = showDistributionPlanModal;

