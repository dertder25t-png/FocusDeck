document.addEventListener('DOMContentLoaded', async () => {
    const serverUrlInput = document.getElementById('serverUrl');
    const apiKeyInput = document.getElementById('apiKey');
    const projectSelect = document.getElementById('projectSelect');
    const refreshProjectsBtn = document.getElementById('refreshProjects');
    const capturePageBtn = document.getElementById('capturePage');
    const snapshotTabsBtn = document.getElementById('snapshotTabs');
    const statusDiv = document.getElementById('status');

    // Load settings
    const stored = await chrome.storage.sync.get(['serverUrl', 'apiKey', 'selectedProjectId']);
    if (stored.serverUrl) serverUrlInput.value = stored.serverUrl;
    if (stored.apiKey) apiKeyInput.value = stored.apiKey;

    // Initial load of projects if server url is present
    if (stored.serverUrl) {
        loadProjects(stored.serverUrl, stored.apiKey, stored.selectedProjectId);
    }

    // Save settings when changed
    serverUrlInput.addEventListener('change', () => {
        chrome.storage.sync.set({ serverUrl: serverUrlInput.value });
    });
    apiKeyInput.addEventListener('change', () => {
        chrome.storage.sync.set({ apiKey: apiKeyInput.value });
    });
    projectSelect.addEventListener('change', () => {
        chrome.storage.sync.set({ selectedProjectId: projectSelect.value });
    });

    refreshProjectsBtn.addEventListener('click', () => {
        loadProjects(serverUrlInput.value, apiKeyInput.value, projectSelect.value);
    });

    capturePageBtn.addEventListener('click', async () => {
        const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
        const activeTab = tabs[0];

        // Send message to content script to scrape
        chrome.tabs.sendMessage(activeTab.id, { action: "scrape_content" }, async (response) => {
            if (chrome.runtime.lastError) {
                statusDiv.textContent = "Error: " + chrome.runtime.lastError.message;
                statusDiv.className = "status error";
                return;
            }

            if (response) {
                await sendCapture(response, activeTab);
            }
        });
    });

    snapshotTabsBtn.addEventListener('click', async () => {
        const tabs = await chrome.tabs.query({ currentWindow: true });
        const payload = {
            tabs: tabs.map(t => ({ url: t.url, title: t.title })),
            projectId: projectSelect.value
        };

        try {
            await postData('/v1/browser/snapshot', payload); // Fixed endpoint path
            statusDiv.textContent = "Tabs snapshot saved!";
            statusDiv.className = "status";
        } catch (e) {
            statusDiv.textContent = "Error: " + e.message;
            statusDiv.className = "status error";
        }
    });

    async function loadProjects(baseUrl, apiKey, selectedId) {
        try {
            const res = await fetch(`${baseUrl}/v1/projects`, {
                headers: {
                    'Authorization': apiKey ? `Bearer ${apiKey}` : '',
                    'Accept': 'application/json'
                }
            });
            if (!res.ok) throw new Error('Failed to load projects');
            const projects = await res.json();

            projectSelect.innerHTML = '<option value="">Select Project...</option>';
            projects.forEach(p => {
                const opt = document.createElement('option');
                opt.value = p.id;
                opt.textContent = p.title;
                if (p.id === selectedId) opt.selected = true;
                projectSelect.appendChild(opt);
            });
        } catch (e) {
            console.error(e);
            statusDiv.textContent = "Could not load projects.";
            statusDiv.className = "status error";
        }
    }

    async function sendCapture(contentData, tab) {
        const payload = {
            url: tab.url,
            title: tab.title,
            content: contentData.content,
            kind: contentData.kind || 'page',
            projectId: projectSelect.value
        };

        try {
            await postData('/v1/browser/capture', payload);
            statusDiv.textContent = "Page captured!";
            statusDiv.className = "status";
        } catch (e) {
            statusDiv.textContent = "Error: " + e.message;
            statusDiv.className = "status error";
        }
    }

    async function postData(endpoint, data) {
        const baseUrl = serverUrlInput.value;
        const apiKey = apiKeyInput.value;

        if (!baseUrl) throw new Error("Server URL is missing.");

        const res = await fetch(`${baseUrl}${endpoint}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': apiKey ? `Bearer ${apiKey}` : ''
            },
            body: JSON.stringify(data)
        });

        if (!res.ok) {
            const text = await res.text();
            throw new Error(`Server error: ${res.statusText} - ${text}`);
        }
    }
});
