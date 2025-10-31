// API Base URL
const API_BASE = '/api';

// State
let decks = [];
let currentDeckId = null;

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    loadDecks();
    updateStats();
});

// Tab Switching
function switchTab(tabName) {
    // Update tab buttons
    document.querySelectorAll('.tab').forEach(tab => {
        tab.classList.remove('active');
    });
    event.target.classList.add('active');

    // Update tab content
    document.querySelectorAll('.tab-content').forEach(content => {
        content.classList.remove('active');
    });
    document.getElementById(tabName).classList.add('active');

    // Load data if switching to decks tab
    if (tabName === 'decks') {
        loadDecks();
    }
}

// Load Decks
async function loadDecks() {
    try {
        const response = await fetch(`${API_BASE}/decks`);
        if (!response.ok) throw new Error('Failed to load decks');
        
        decks = await response.json();
        renderDecks();
        updateStats();
    } catch (error) {
        console.error('Error loading decks:', error);
        showAlert('Error loading decks: ' + error.message, 'error');
    }
}

// Render Decks
function renderDecks() {
    const container = document.getElementById('decksList');
    
    if (decks.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-state-icon">📚</div>
                <h3>No decks yet</h3>
                <p>Create your first deck to get started!</p>
            </div>
        `;
        return;
    }

    container.innerHTML = decks.map(deck => `
        <div class="card">
            <div class="card-header">
                <span class="card-title">${escapeHtml(deck.name || 'Untitled Deck')}</span>
                <div class="card-actions">
                    <button class="btn-primary btn-small" onclick="editDeck('${deck.id}')">✏️ Edit</button>
                    <button class="btn-danger btn-small" onclick="deleteDeck('${deck.id}')">🗑️ Delete</button>
                </div>
            </div>
            <p style="color: #666; font-size: 14px;">
                ${deck.cards ? deck.cards.length : 0} cards
            </p>
            ${deck.cards && deck.cards.length > 0 ? `
                <details style="margin-top: 10px;">
                    <summary style="cursor: pointer; color: #667eea; font-weight: 500;">View Cards</summary>
                    <ul style="margin-top: 10px; padding-left: 20px;">
                        ${deck.cards.slice(0, 5).map(card => `<li style="margin: 5px 0;">${escapeHtml(card)}</li>`).join('')}
                        ${deck.cards.length > 5 ? `<li style="color: #999;">... and ${deck.cards.length - 5} more</li>` : ''}
                    </ul>
                </details>
            ` : ''}
        </div>
    `).join('');
}

// Show Create Deck Modal
function showCreateDeckModal() {
    currentDeckId = null;
    document.getElementById('modalTitle').textContent = 'Create New Deck';
    document.getElementById('deckForm').reset();
    document.getElementById('deckId').value = '';
    document.getElementById('modalAlert').innerHTML = '';
    document.getElementById('deckModal').classList.add('active');
}

// Edit Deck
function editDeck(id) {
    const deck = decks.find(d => d.id === id);
    if (!deck) return;

    currentDeckId = id;
    document.getElementById('modalTitle').textContent = 'Edit Deck';
    document.getElementById('deckId').value = id;
    document.getElementById('deckName').value = deck.name || '';
    document.getElementById('deckCards').value = deck.cards ? deck.cards.join('\n') : '';
    document.getElementById('modalAlert').innerHTML = '';
    document.getElementById('deckModal').classList.add('active');
}

// Save Deck
async function saveDeck(event) {
    event.preventDefault();

    const name = document.getElementById('deckName').value.trim();
    const cardsText = document.getElementById('deckCards').value.trim();
    const cards = cardsText ? cardsText.split('\n').filter(c => c.trim()) : [];
    const id = document.getElementById('deckId').value;

    const deckData = {
        name,
        cards
    };

    try {
        let response;
        if (id) {
            // Update existing deck
            deckData.id = id;
            response = await fetch(`${API_BASE}/decks/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(deckData)
            });
        } else {
            // Create new deck
            response = await fetch(`${API_BASE}/decks`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(deckData)
            });
        }

        if (!response.ok) {
            const error = await response.text();
            throw new Error(error || 'Failed to save deck');
        }

        showModalAlert(id ? 'Deck updated successfully!' : 'Deck created successfully!', 'success');
        
        setTimeout(() => {
            closeModal();
            loadDecks();
        }, 1000);

    } catch (error) {
        console.error('Error saving deck:', error);
        showModalAlert('Error: ' + error.message, 'error');
    }
}

// Delete Deck
async function deleteDeck(id) {
    if (!confirm('Are you sure you want to delete this deck? This action cannot be undone.')) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/decks/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) throw new Error('Failed to delete deck');

        showAlert('Deck deleted successfully!', 'success');
        loadDecks();
    } catch (error) {
        console.error('Error deleting deck:', error);
        showAlert('Error deleting deck: ' + error.message, 'error');
    }
}

// Close Modal
function closeModal() {
    document.getElementById('deckModal').classList.remove('active');
    document.getElementById('deckForm').reset();
    currentDeckId = null;
}

// Update Statistics
function updateStats() {
    document.getElementById('deckCount').textContent = decks.length;
    
    const totalCards = decks.reduce((sum, deck) => {
        return sum + (deck.cards ? deck.cards.length : 0);
    }, 0);
    document.getElementById('cardCount').textContent = totalCards;
}

// Show Alert
function showAlert(message, type) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type}`;
    alertDiv.textContent = message;
    
    const content = document.querySelector('.content');
    content.insertBefore(alertDiv, content.firstChild);

    setTimeout(() => alertDiv.remove(), 5000);
}

// Show Modal Alert
function showModalAlert(message, type) {
    const alertDiv = document.getElementById('modalAlert');
    alertDiv.className = `alert alert-${type}`;
    alertDiv.textContent = message;
}

// Save Configuration
function saveConfig() {
    const config = {
        port: document.getElementById('serverPort').value,
        environment: document.getElementById('environment').value,
        maxDecks: document.getElementById('maxDecks').value,
        enableAuth: document.getElementById('enableAuth').value === 'true'
    };

    console.log('Configuration:', config);
    showAlert('Configuration saved! (Note: This is a demo. Restart server to apply changes.)', 'success');
}

// Export Data
async function exportData() {
    try {
        const response = await fetch(`${API_BASE}/decks`);
        if (!response.ok) throw new Error('Failed to export data');
        
        const data = await response.json();
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `focusdeck-backup-${new Date().toISOString().split('T')[0]}.json`;
        a.click();
        URL.revokeObjectURL(url);
        
        showAlert('Data exported successfully!', 'success');
    } catch (error) {
        console.error('Error exporting data:', error);
        showAlert('Error exporting data: ' + error.message, 'error');
    }
}

// Clear All Data
async function clearAllData() {
    if (!confirm('⚠️ WARNING: This will delete ALL decks permanently! Are you absolutely sure?')) {
        return;
    }

    if (!confirm('Last chance! This action CANNOT be undone. Delete everything?')) {
        return;
    }

    try {
        // Delete all decks one by one
        const deletePromises = decks.map(deck => 
            fetch(`${API_BASE}/decks/${deck.id}`, { method: 'DELETE' })
        );
        
        await Promise.all(deletePromises);
        
        showAlert('All data cleared successfully!', 'success');
        loadDecks();
    } catch (error) {
        console.error('Error clearing data:', error);
        showAlert('Error clearing data: ' + error.message, 'error');
    }
}

// Utility: Escape HTML
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Close modal when clicking outside
document.getElementById('deckModal').addEventListener('click', (e) => {
    if (e.target.id === 'deckModal') {
        closeModal();
    }
});
