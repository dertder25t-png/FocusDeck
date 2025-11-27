document.addEventListener('DOMContentLoaded', async () => {
    const serverUrlInput = document.getElementById('serverUrl');
    const apiKeyInput = document.getElementById('apiKey');
    const projectSelect = document.getElementById('projectSelect');
    const refreshProjectsBtn = document.getElementById('refreshProjects');
    const capturePageBtn = document.getElementById('capturePage');
    const snapshotTabsBtn = document.getElementById('snapshotTabs');
    const closeOldTabsBtn = document.getElementById('closeOldTabs');
    const dashboardLink = document.getElementById('dashboardLink');
    const statusDiv = document.getElementById('status');

    // Load settings and update UI
    const stored = await chrome.storage.sync.get(['serverUrl', 'apiKey', 'selectedProjectId']);
    serverUrlInput.value = stored.serverUrl || 'http://localhost:5000';
    apiKeyInput.value = stored.apiKey || '';
    updateDashboardLink(serverUrlInput.value);

    if (serverUrlInput.value) {
        loadProjects(serverUrlInput.value, apiKeyInput.value, stored.selectedProjectId);
    }

    // Save settings and update UI
    serverUrlInput.addEventListener('change', () => {
        const newUrl = serverUrlInput.value;
        chrome.storage.sync.set({ serverUrl: newUrl });
        updateDashboardLink(newUrl);
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
        try {
            statusDiv.textContent = 'Snapshotting tabs...';
            const tabs = await chrome.tabs.query({ currentWindow: true });
            const payload = {
                tabs: tabs.map(t => ({ url: t.url, title: t.title })),
                projectId: projectSelect.value
            };
            await postData('/v1/browser/tabs/snapshot', payload);
            statusDiv.textContent = "Tabs snapshot saved!";
        } catch (e) {
            showError("Error: " + e.message);
        }
    });

    closeOldTabsBtn.addEventListener('click', async () => {
        try {
            statusDiv.textContent = 'Closing old tabs...';
            const tabs = await chrome.tabs.query({ currentWindow: true, pinned: false });
            // Close tabs that are not active and older than 1 hour
            const oneHourAgo = Date.now() - (1000 * 60 * 60);
            const tabsToClose = tabs.filter(t => !t.active && t.lastAccessed < oneHourAgo);
            if(tabsToClose.length > 0) {
                 const tabIds = tabsToClose.map(({ id }) => id);
                 await chrome.tabs.remove(tabIds);
                 statusDiv.textContent = `Closed ${tabsToClose.length} old tabs.`;
            } else {
                 statusDiv.textContent = "No old tabs to close.";
            }
        } catch (e) {
            showError("Error: " + e.message);
        }
    });

    function updateDashboardLink(baseUrl) {
        if (baseUrl) {
            dashboardLink.href = baseUrl;
        }
    }

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
            showError("Could not load projects.");
        }
    }

    async function sendCapture(contentData, tab) {
        try {
            statusDiv.textContent = 'Capturing...';
            const payload = {
                url: tab.url,
                title: tab.title,
                content: contentData.content,
                kind: contentData.kind || 'page',
                projectId: projectSelect.value
            };
            await postData('/v1/browser/capture', payload);
            statusDiv.textContent = "Page captured!";
        } catch (e) {
            showError("Error: " + e.message);
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
                'Authorization': `Bearer ${apiKey}`
            },
            body: JSON.stringify(data)
        });

        if (!res.ok) {
            const text = await res.text();
            throw new Error(`Server error: ${res.statusText} - ${text}`);
        }
    }

    function showError(message) {
        statusDiv.textContent = message;
        statusDiv.className = 'status error';
    }
});
