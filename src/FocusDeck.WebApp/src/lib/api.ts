// A simple fetch wrapper to add the auth token to requests
export const apiFetch = async (url: string, options: RequestInit = {}) => {
  const token = localStorage.getItem('focusdeck_access_token');
  const headers: Record<string, string> = {
    ...options.headers as Record<string, string>,
    'Content-Type': 'application/json',
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(url, { ...options, headers });

  if (response.status === 401) {
    // Handle 401 Unauthorized
    // Clear tokens just in case
    localStorage.removeItem('focusdeck_access_token');
    localStorage.removeItem('focusdeck_refresh_token');
    localStorage.removeItem('focusdeck_user');

    // Redirect to login
    const redirectUrl = encodeURIComponent(window.location.pathname + window.location.search);
    window.location.href = `/login?redirectUrl=${redirectUrl}`;

    // Stop execution and throw error
    throw new Error('Unauthorized');
  }

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'API request failed');
  }

  return response.json();
};
