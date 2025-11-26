// Listen for messages
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.action === "scrape_content") {
        const data = scrapePage();
        sendResponse(data);
    }
});

function scrapePage() {
    let kind = 'page';
    let content = document.body.innerText;

    // AI Chat Scraping
    const chatScrapers = {
        'chatgpt.com': () => {
            const chatElements = document.querySelectorAll('[data-message-author-role]');
            if (!chatElements.length) return null;
            return Array.from(chatElements).map(el => {
                const role = el.getAttribute('data-message-author-role');
                const innerContent = el.querySelector('div.prose') || el;
                return `[${role.toUpperCase()}]:\n${innerContent.innerText}`;
            }).join('\n\n---\n\n');
        },
        'claude.ai': () => {
            const chatElements = document.querySelectorAll('.font-user-message, .font-claude-message');
            if (!chatElements.length) return null;
            return Array.from(chatElements).map(el => el.innerText).join('\n\n---\n\n');
        },
        'gemini.google.com': () => {
            const chatElements = document.querySelectorAll('.user-query, .model-response-text');
            if (!chatElements.length) return null;
            return Array.from(chatElements).map(el => {
                const role = el.classList.contains('user-query') ? 'user' : 'assistant';
                return `[${role.toUpperCase()}]:\n${el.innerText}`;
            }).join('\n\n---\n\n');
        }
    };

    for (const domain in chatScrapers) {
        if (window.location.hostname.includes(domain)) {
            const chatContent = chatScrapers[domain]();
            if (chatContent) {
                kind = 'ai_chat';
                content = chatContent;
                return { kind, content };
            }
        }
    }

    // Code Block Scraping
    const codeBlocks = document.querySelectorAll('pre > code');
    if (codeBlocks.length > 0) {
        kind = 'code_snippet';
        content = Array.from(codeBlocks).map(el => el.innerText).join('\n\n---\n\n');
    }

    return { kind, content };
}
