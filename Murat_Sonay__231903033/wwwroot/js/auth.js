const apiOrigin = window.location.protocol === 'file:' ? 'http://localhost:5000' : window.location.origin;

async function apiFetch(url, options = {}) {
    const requestUrl = url.startsWith('/') ? `${apiOrigin}${url}` : url;
    const response = await fetch(requestUrl, { credentials: 'include', headers: { 'Content-Type': 'application/json' }, ...options });
    if (!response.ok) throw response;
    return response.json();
}

async function requireRole(expectedRole) {
    try {
        const user = await apiFetch('/api/auth/user');
        document.querySelector('#currentUser').textContent = `${user.fullName || user.username} · ${user.role}`;
        if (user.role !== expectedRole) {
            window.location.href = `${apiOrigin}/login.html`;
        }
        return user;
    } catch {
        window.location.href = `${apiOrigin}/login.html`;
    }
}

async function logout() {
    await fetch(`${apiOrigin}/api/auth/logout`, { method: 'POST', credentials: 'include' });
    window.location.href = `${apiOrigin}/login.html`;
}

document.addEventListener('click', (event) => {
    if (event.target.id === 'logoutButton') {
        event.preventDefault();
        logout();
    }
});
