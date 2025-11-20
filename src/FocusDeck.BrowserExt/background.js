// Handle keyboard shortcuts or context menus if needed
chrome.commands.onCommand.addListener((command) => {
    if (command === "capture_page") {
        chrome.tabs.query({active: true, currentWindow: true}, async (tabs) => {
            const activeTab = tabs[0];
            if (!activeTab) return;

            // Get settings
            const stored = await chrome.storage.sync.get(['serverUrl', 'apiKey', 'selectedProjectId']);
            if (!stored.serverUrl) return;

            chrome.tabs.sendMessage(activeTab.id, { action: "scrape_content" }, async (response) => {
                 if (chrome.runtime.lastError || !response) {
                    console.error("Error capturing:", chrome.runtime.lastError);
                    return;
                }

                const payload = {
                    url: activeTab.url,
                    title: activeTab.title,
                    content: response.content,
                    kind: response.kind || 'page',
                    projectId: stored.selectedProjectId
                };

                // Post to server
                try {
                    await fetch(`${stored.serverUrl}/v1/browser/capture`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Authorization': stored.apiKey ? `Bearer ${stored.apiKey}` : ''
                        },
                        body: JSON.stringify(payload)
                    });
                    console.log("Captured via shortcut");
                } catch (e) {
                    console.error("Failed to capture:", e);
                }
            });
        });
    }
});
