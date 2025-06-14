// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Получаем базовый URL API из серверной конфигурации
const apiBaseUrl = window.serverConfig?.apiBaseUrl || 'https://localhost:7123';

// Утилиты для работы с куками
function getCookie(name) {
    const cookies = document.cookie.split(';');
    for (let cookie of cookies) {
        const [cookieName, cookieValue] = cookie.trim().split('=');
        if (cookieName === name) {
            return decodeURIComponent(cookieValue);
        }
    }
    return null;
}

function setCookie(name, value, days) {
    const date = new Date();
    date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
    const expires = `expires=${date.toUTCString()}`;
    const secure = window.location.protocol === 'https:' ? 'secure;' : '';
    document.cookie = `${name}=${encodeURIComponent(value)}; ${expires}; path=/; ${secure}samesite=lax`;
}

function deleteCookie(name) {
    document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
}

// HTTP-клиент с автоматическим обновлением токенов
const httpClient = {
    async request(method, url, data = null, options = {}) {
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        const accessToken = getCookie('_secure_at');
        if (accessToken) {
            headers['Authorization'] = `Bearer ${accessToken}`;
        }

        const config = {
            method,
            headers,
            ...options,
            body: data ? JSON.stringify(data) : null
        };

        try {
            const response = await fetch(`${apiBaseUrl}${url}`, config);
            
            if (!response.ok) {
                const error = new Error(`HTTP error: ${response.status}`);
                error.response = response;
                throw error;
            }

            return await response.json();
            
        } catch (error) {
            // Обновляем токен при 401 ошибке
            if (error.response?.status === 401 && !options._retry) {
                try {
                    const refreshToken = getCookie('_secure_rt');
                    if (!refreshToken) {
                        redirectToLogin();
                        throw new Error('Отсутствует refresh token');
                    }

                    const tokens = await this.request('POST', '/api/auth/refresh-token', {
                        accessToken: accessToken,
                        refreshToken: refreshToken
                    }, { _retry: true });

                    setCookie('_secure_at', tokens.accessToken, tokens.expiresIn / 86400);
                    setCookie('_secure_rt', tokens.refreshToken, 7);

                    // Повторяем оригинальный запрос с новым токеном
                    headers['Authorization'] = `Bearer ${tokens.accessToken}`;
                    const retryResponse = await fetch(`${apiBaseUrl}${url}`, {
                        ...config,
                        headers,
                        _retry: true
                    });

                    if (!retryResponse.ok) {
                        throw new Error(`HTTP error: ${retryResponse.status}`);
                    }

                    return await retryResponse.json();
                    
                } catch (refreshError) {
                    console.error('Ошибка обновления токена:', refreshError);
                    redirectToLogin();
                    throw refreshError;
                }
            }
            
            console.error('Ошибка запроса:', error);
            throw error;
        }
    },

    get(url, options = {}) {
        return this.request('GET', url, null, options);
    },

    post(url, data, options = {}) {
        return this.request('POST', url, data, options);
    },

    put(url, data, options = {}) {
        return this.request('PUT', url, data, options);
    },

    delete(url, options = {}) {
        return this.request('DELETE', url, null, options);
    }
};  // <-- Закрывающая фигурная скобка для объекта httpClient

function redirectToLogin() {
    deleteCookie('_secure_at');
    deleteCookie('_secure_rt');
    window.location.href = '/account/login';
}