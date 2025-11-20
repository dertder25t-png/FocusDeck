// Listen for messages
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.action === "scrape_content") {
        const data = scrapePage();
        sendResponse(data);
    }
});

function scrapePage() {
    // Basic heuristics for now
    let kind = 'page';
    let content = document.body.innerText;

    // Detect AI Chat interfaces
    if (window.location.hostname.includes('chatgpt.com') || window.location.hostname.includes('openai.com')) {
        kind = 'ai_chat';
        // Try to find chat container
        // This is brittle and will need updates, but serves as a placeholder
        // Look for common chat selectors
        const chatElements = document.querySelectorAll('[data-message-author-role="assistant"], [data-message-author-role="user"]');
        if (chatElements.length > 0) {
            content = Array.from(chatElements).map(el => {
                const role = el.getAttribute('data-message-author-role');
                return `[${role.toUpperCase()}]:\n${el.innerText}`;
            }).join('\n\n---\n\n');
        }
    } else if (window.location.hostname.includes('claude.ai')) {
        kind = 'ai_chat';
        // Claude selectors (approximate)
        const chatElements = document.querySelectorAll('.font-user-message, .font-claude-message');
         if (chatElements.length > 0) {
            content = Array.from(chatElements).map(el => el.innerText).join('\n\n---\n\n');
        }
    }

    return {
        kind: kind,
        content: content
    };
}
