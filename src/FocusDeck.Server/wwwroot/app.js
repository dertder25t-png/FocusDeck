document.addEventListener('DOMContentLoaded', () => {
    // Initial data load
    checkServerHealth();
    loadConnectedServices();
    loadAvailableServices();
    loadAutomations();
});

// --- Constants ---
const API_BASE = '/api';

// --- API Fetch Functions ---

async function checkServerHealth() {
    const statusEl = document.getElementById('server-status');
    try {
        const response = await fetch(`${API_BASE}/health`);
        if (!response.ok) throw new Error(`Server returned ${response.status}`);
        const data = await response.json();
        statusEl.textContent = `Healthy (Uptime: ${data.uptime})`;
        statusEl.classList.add('healthy');
    } catch (error) {
        statusEl.textContent = `Error: ${error.message}`;
        statusEl.classList.add('error');
    }
}

async function loadConnectedServices() {
    const container = document.getElementById('connected-services');
    container.innerHTML = '<p>Loading...</p>';
    try {
        const response = await fetch(`${API_BASE}/services`);
        const services = await response.json();
        
        if (services.length === 0) {
            container.innerHTML = '<p>No services connected yet.</p>';
            return;
        }

        container.innerHTML = ''; // Clear loading
        services.forEach(service => {
            const el = document.createElement('div');
            el.className = 'service-card connected';
            el.innerHTML = `
                <strong>${service.service}</strong>
                <span class="status ${service.status.toLowerCase()}">${service.status}</span>
                <button class="disconnect-btn" data-id="${service.id}">Disconnect</button>
            `;
            container.appendChild(el);
        });

        // Add disconnect listeners
        container.querySelectorAll('.disconnect-btn').forEach(btn => {
            btn.addEventListener('click', () => disconnectService(btn.dataset.id));
        });

    } catch (error) {
        container.innerHTML = `<p class="error">Failed to load services: ${error.message}</p>`;
    }
}

async function loadAvailableServices() {
    const container = document.getElementById('service-list');
    container.innerHTML = ''; // Clear
    
    // These must match your ServiceType enum in C#
    const available = [
        'HomeAssistant', 
        'GoogleCalendar', 
        'GoogleDrive', 
        'Spotify', 
        'Canvas'
    ];
    
    available.forEach(serviceName => {
        const el = document.createElement('div');
        el.className = 'service-card available';
        el.textContent = serviceName;
        // THIS IS THE CLICK HANDLER YOU WERE MISSING
        el.onclick = () => showSetupGuide(serviceName);
        container.appendChild(el);
    });
}

async function loadAutomations() {
    const list = document.getElementById('automation-list');
    list.innerHTML = '<li>Loading...</li>';
    try {
        const response = await fetch(`${API_BASE}/automations`);
        const automations = await response.json();
        
        if (automations.length === 0) {
            list.innerHTML = '<li>No automations created yet.</li>';
            return;
        }

        list.innerHTML = ''; // Clear loading
        automations.forEach(auto => {
            const li = document.createElement('li');
            li.textContent = auto.name;
            const status = document.createElement('span');
            status.className = `status ${auto.isEnabled ? 'healthy' : 'error'}`;
            status.textContent = auto.isEnabled ? 'Enabled' : 'Disabled';
            li.appendChild(status);
            list.appendChild(li);
        });

    } catch (error) {
        list.innerHTML = `<li class="error">Failed to load automations: ${error.message}</li>`;
    }
}

async function disconnectService(serviceId) {
    if (!confirm('Are you sure you want to disconnect this service?')) {
        return;
    }
    try {
        const response = await fetch(`${API_BASE}/services/${serviceId}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            throw new Error(`Failed to disconnect: ${response.status}`);
        }
        // Reload services
        loadConnectedServices();
    } catch (error) {
        alert(`Error: ${error.message}`);
    }
}


// --- MODAL & SETUP GUIDE LOGIC ---

function closeModal() {
    document.getElementById('setup-modal').style.display = 'none';
}

async function showSetupGuide(serviceName) {
    const modal = document.getElementById('setup-modal');
    const modalBody = document.getElementById('modal-body');
    modalBody.innerHTML = '<p>Loading setup guide...</p>';
    modal.style.display = 'flex';

    try {
        // 1. Call the /setup endpoint
        const response = await fetch(`${API_BASE}/services/${serviceName}/setup`);
        if (!response.ok) {
            throw new Error(`Failed to load setup guide: ${response.status}`);
        }
        const guide = await response.json();

        // 2. Build the UI based on the response
        if (guide.setupType === 'Simple') {
            buildSimpleSetupForm(serviceName, guide);
        } else if (guide.setupType === 'OAuth') {
            buildOAuthSetup(serviceName, guide);
        }

    } catch (error) {
        modalBody.innerHTML = `<p class="error">Error: ${error.message}</p>`;
    }
}

function buildSimpleSetupForm(serviceName, guide) {
    const modalBody = document.getElementById('modal-body');
    
    let fieldsHtml = '';
    guide.fields.forEach(field => {
        fieldsHtml += `
            <div class="form-group">
                <label for="field-${field.key}">${field.label}</label>
                <input type="${field.inputType || 'text'}" id="field-${field.key}" data-key="${field.key}" />
                <p class="help-text">${field.helpText}</p>
            </div>
        `;
    });

    modalBody.innerHTML = `
        <h3>${guide.title}</h3>
        <p>${guide.description}</p>
        <form id="simple-setup-form">
            ${fieldsHtml}
            <button type="submit" class="save-btn">Save and Connect</button>
            <p id="form-error" class="error"></p>
        </form>
    `;

    // 3. Add submit listener to call the /connect endpoint
    document.getElementById('simple-setup-form').addEventListener('submit', (e) => {
        e.preventDefault();
        submitSimpleSetup(serviceName);
    });
}

function buildOAuthSetup(serviceName, guide) {
    const modalBody = document.getElementById('modal-body');
    modalBody.innerHTML = `
        <h3>${guide.title}</h3>
        <p>${guide.description}</p>
        <button id="oauth-connect-btn" class="save-btn">${guide.oauthButtonText}</button>
    `;

    // 3. Add click listener to call the /oauth/url endpoint
    document.getElementById('oauth-connect-btn').addEventListener('click', () => {
        startOAuthFlow(serviceName);
    });
}

async function submitSimpleSetup(serviceName) {
    const form = document.getElementById('simple-setup-form');
    const errorEl = document.getElementById('form-error');
    const inputs = form.querySelectorAll('input[data-key]');
    
    const credentials = {};
    inputs.forEach(input => {
        credentials[input.dataset.key] = input.value;
    });

    try {
        const response = await fetch(`${API_BASE}/services/connect/${serviceName}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(credentials),
        });

        if (!response.ok) {
            const errData = await response.json();
            throw new Error(errData.message || `Request failed with ${response.status}`);
        }

        // Success
        closeModal();
        loadConnectedServices(); // Refresh the list

    } catch (error) {
        errorEl.textContent = `Error: ${error.message}`;
    }
}

async function startOAuthFlow(serviceName) {
    try {
        // 1. Get the auth URL from the server
        const response = await fetch(`${API_BASE}/services/oauth/${serviceName}/url`);
        if (!response.ok) {
            throw new Error('Could not get auth URL from server.');
        }
        const data = await response.json();
        
        // 2. Redirect the user
        window.location.href = data.url;
        
    } catch (error) {
        document.getElementById('modal-body').innerHTML += `<p class="error">${error.message}</p>`;
    }
}
