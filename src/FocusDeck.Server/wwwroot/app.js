// ====================================
// FocusDeck Web App - Main JavaScript
// ====================================

class FocusDeckApp {
    constructor() {
        this.currentView = 'dashboard';
        this.tasks = [];
        this.decks = [];
        this.sessions = [];
        this.automations = [];
        this.connectedServices = [];
        this.timerState = {
            isRunning: false,
            isPaused: false,
            currentTime: 25 * 60,
            totalTime: 25 * 60,
            intervalId: null
        };
        this.settings = this.loadSettings();
        
        this.init();
    }

    init() {
        // Hide loading screen
        setTimeout(() => {
            document.getElementById('loadingScreen').style.display = 'none';
            document.getElementById('app').style.display = 'flex';
            this.initializeApp();
        }, 800);
    }

    initializeApp() {
        this.setupNavigation();
        this.setupDateTime();
        this.setupDashboard();
        this.setupPlanner();
        this.setupTimer();
        this.setupDecks();
        this.setupAutomations();
        this.setupSettings();
        this.setupGlobalEventListeners(); // Add this
        this.loadFromAPI();
        
        // Set today's date as default for task form
        const today = new Date().toISOString().split('T')[0];
        this.safeSetProperty('taskDueDate', 'value', today);
        
        console.log('FocusDeck initialized');
    }

    // ====================================
    // NAVIGATION
    // ====================================

    setupNavigation() {
        document.querySelectorAll('.nav-item').forEach(item => {
            item.addEventListener('click', (e) => {
                const view = e.currentTarget.dataset.view;
                this.switchView(view);
            });
        });

        // Quick actions
        document.querySelectorAll('.quick-action-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const action = e.currentTarget.dataset.action;
                this.handleQuickAction(action);
            });
        });
    }

    switchView(viewName) {
        // Update navigation
        document.querySelectorAll('.nav-item').forEach(item => {
            item.classList.toggle('active', item.dataset.view === viewName);
        });

        // Update views
        document.querySelectorAll('.view').forEach(view => {
            view.classList.remove('active');
        });
        document.getElementById(`${viewName}View`).classList.add('active');

        this.currentView = viewName;
    }

    handleQuickAction(action) {
        switch(action) {
            case 'start-timer':
                this.switchView('timer');
                break;
            case 'add-task':
                this.switchView('planner');
                document.getElementById('addTaskBtn').click();
                break;
            case 'new-deck':
                this.switchView('decks');
                this.openDeckModal();
                break;
            case 'view-calendar':
                this.switchView('calendar');
                break;
        }
    }

    // ====================================
    // DATE & TIME
    // ====================================

    setupDateTime() {
        this.updateDateTime();
        setInterval(() => this.updateDateTime(), 1000);
    }

    updateDateTime() {
        const now = new Date();
        
        // Update current date
        const dateOptions = { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' };
        document.getElementById('currentDate').textContent = now.toLocaleDateString('en-US', dateOptions);
        
        // Update current time
        const timeOptions = { hour: '2-digit', minute: '2-digit', second: '2-digit' };
        document.getElementById('currentTime').textContent = now.toLocaleTimeString('en-US', timeOptions);
    }

    // ====================================
    // DASHBOARD
    // ====================================

    setupDashboard() {
        this.updateDashboard();
    }

    updateDashboard() {
        // Update stats
        const completedToday = this.tasks.filter(t => t.completed && this.isToday(t.completedAt)).length;
        const totalStudyTime = this.getTotalStudyTime();
        
        this.safeSetText('dashCompletedTasks', completedToday);
        this.safeSetText('dashStudyTime', this.formatTime(totalStudyTime));
        this.safeSetText('dashStreak', '0'); // Implement streak logic
        this.safeSetText('dashProductivity', '0%'); // Implement productivity calculation
        
        // Update sidebar stats
        const activeTasks = this.tasks.filter(t => !t.completed).length;
        this.safeSetText('sidebarActiveTasks', activeTasks);
        this.safeSetText('sidebarCompletedToday', completedToday);
        
        const completionRate = this.tasks.length > 0 
            ? Math.round((completedToday / this.tasks.length) * 100)
            : 0;
        this.safeSetText('sidebarCompletionRate', `${completionRate}%`);
        
        // Update planner sidebar
        this.safeSetText('plannerActiveTasks', activeTasks);
        this.safeSetText('plannerCompletedToday', completedToday);
        this.safeSetText('plannerCompletionRate', `${completionRate}%`);
        
        this.updateRecentActivity();
    }

    updateRecentActivity() {
        const container = document.getElementById('recentActivity');
        // Implement activity tracking
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">📝</div>
                <p>No recent activity</p>
            </div>
        `;
    }

    getTotalStudyTime() {
        return this.sessions.reduce((total, session) => total + session.duration, 0);
    }

    isToday(date) {
        if (!date) return false;
        const d = new Date(date);
        const today = new Date();
        return d.toDateString() === today.toDateString();
    }

    // ====================================
    // PLANNER (MY DAY)
    // ====================================

    setupPlanner() {
        // Add Task Button
        document.getElementById('addTaskBtn').addEventListener('click', () => {
            document.getElementById('taskInputArea').style.display = 'block';
            document.getElementById('taskTitle').focus();
        });

        // Create First Deck (duplicate handler)
        const createFirstDeck = document.getElementById('createFirstDeck');
        if (createFirstDeck) {
            createFirstDeck.addEventListener('click', () => this.openDeckModal());
        }

        // Cancel Task Button
        document.getElementById('cancelTaskBtn').addEventListener('click', () => {
            this.clearTaskForm();
            document.getElementById('taskInputArea').style.display = 'none';
        });

        // Save Task Button
        document.getElementById('saveTaskBtn').addEventListener('click', () => {
            this.saveTask();
        });

        // Categories Button
        document.getElementById('categoriesBtn').addEventListener('click', () => {
            this.showToast('Category filtering coming soon!', 'info');
        });

        // Sort Button
        document.getElementById('sortBtn').addEventListener('click', () => {
            this.showToast('Sort options coming soon!', 'info');
        });

        // Filter Button
        document.getElementById('filterBtn').addEventListener('click', () => {
            this.showToast('Filter options coming soon!', 'info');
        });

        this.renderTasks();
    }

    saveTask() {
        const title = document.getElementById('taskTitle').value.trim();
        if (!title) {
            this.showToast('Please enter a task title', 'error');
            return;
        }

        const task = {
            id: Date.now(),
            title,
            category: document.getElementById('taskCategory').value,
            priority: document.getElementById('taskPriority').value,
            dueDate: document.getElementById('taskDueDate').value,
            dueTime: document.getElementById('taskDueTime').value,
            notes: document.getElementById('taskNotes').value,
            completed: false,
            createdAt: new Date().toISOString()
        };

        this.tasks.push(task);
        this.saveToLocalStorage();
        this.renderTasks();
        this.clearTaskForm();
        document.getElementById('taskInputArea').style.display = 'none';
        this.showToast('Task added successfully!', 'success');
        this.updateDashboard();
    }

    clearTaskForm() {
        document.getElementById('taskTitle').value = '';
        document.getElementById('taskCategory').value = '';
        document.getElementById('taskPriority').value = 'medium';
        const today = new Date().toISOString().split('T')[0];
        document.getElementById('taskDueDate').value = today;
        document.getElementById('taskDueTime').value = '';
        document.getElementById('taskNotes').value = '';
    }

    renderTasks() {
        const container = document.getElementById('tasksTimeline');
        
        if (this.tasks.length === 0) {
            container.innerHTML = `
                <div class="empty-state-large">
                    <div class="empty-icon-large">✓</div>
                    <h3>No Tasks Yet</h3>
                    <p>Click "Add Task" to create your first task</p>
                </div>
            `;
            return;
        }

        // Group tasks by date
        const grouped = this.groupTasksByDate(this.tasks);
        
        container.innerHTML = Object.keys(grouped).map(dateKey => {
            const tasks = grouped[dateKey];
            const isToday = dateKey === new Date().toDateString();
            
            return `
                <div class="day-section">
                    <div class="day-header">
                        <span class="day-label">
                            ${this.formatDateLabel(dateKey)}
                            ${isToday ? '<span class="today-badge">Today</span>' : ''}
                        </span>
                        <span class="task-count">${tasks.length} task${tasks.length !== 1 ? 's' : ''}</span>
                    </div>
                    <div class="tasks-list">
                        ${tasks.map(task => this.renderTaskItem(task)).join('')}
                    </div>
                </div>
            `;
        }).join('');

        // Add event listeners
        this.attachTaskEventListeners();
    }

    renderTaskItem(task) {
        return `
            <div class="task-item" data-task-id="${task.id}">
                <div class="task-checkbox">
                    <input type="checkbox" 
                           id="task-${task.id}" 
                           ${task.completed ? 'checked' : ''}
                           onchange="app.toggleTask(${task.id})" />
                    <label for="task-${task.id}"></label>
                </div>
                <div class="task-content">
                    <div class="task-title-text ${task.completed ? 'completed' : ''}">${this.escapeHtml(task.title)}</div>
                    <div class="task-badges">
                        ${task.category ? `<span class="badge badge-category">${task.category}</span>` : ''}
                        ${task.priority === 'urgent' ? `<span class="badge badge-urgent">Urgent</span>` : ''}
                        ${task.priority === 'high' ? `<span class="badge badge-high">High</span>` : ''}
                    </div>
                </div>
                <div class="task-actions">
                    <button class="task-action-btn" onclick="app.editTask(${task.id})" title="Edit">
                        <span>✏️</span>
                    </button>
                    <button class="task-action-btn delete" onclick="app.deleteTask(${task.id})" title="Delete">
                        <span>🗑️</span>
                    </button>
                </div>
            </div>
        `;
    }

    attachTaskEventListeners() {
        // Event listeners are handled via inline onclick for simplicity
    }

    groupTasksByDate(tasks) {
        const grouped = {};
        
        tasks.forEach(task => {
            const date = task.dueDate ? new Date(task.dueDate).toDateString() : 'No Date';
            if (!grouped[date]) {
                grouped[date] = [];
            }
            grouped[date].push(task);
        });

        return grouped;
    }

    formatDateLabel(dateStr) {
        if (dateStr === 'No Date') return dateStr;
        const date = new Date(dateStr);
        const options = { weekday: 'short', month: 'short', day: 'numeric' };
        return date.toLocaleDateString('en-US', options);
    }

    toggleTask(taskId) {
        const task = this.tasks.find(t => t.id === taskId);
        if (task) {
            task.completed = !task.completed;
            if (task.completed) {
                task.completedAt = new Date().toISOString();
            } else {
                delete task.completedAt;
            }
            this.saveToLocalStorage();
            this.renderTasks();
            this.updateDashboard();
        }
    }

    editTask(taskId) {
        this.showToast('Edit functionality coming soon!', 'info');
    }

    deleteTask(taskId) {
        if (confirm('Are you sure you want to delete this task?')) {
            this.tasks = this.tasks.filter(t => t.id !== taskId);
            this.saveToLocalStorage();
            this.renderTasks();
            this.updateDashboard();
            this.showToast('Task deleted', 'success');
        }
    }

    // ====================================
    // STUDY TIMER
    // ====================================

    setupTimer() {
        // Preset buttons
        document.querySelectorAll('.preset-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const minutes = parseInt(e.currentTarget.dataset.minutes);
                this.setTimerMinutes(minutes);
                
                // Update active state
                document.querySelectorAll('.preset-btn').forEach(b => b.classList.remove('active'));
                e.currentTarget.classList.add('active');
            });
        });

        // Custom time input
        document.getElementById('setCustomTimeBtn').addEventListener('click', () => {
            const minutes = parseInt(document.getElementById('customMinutes').value);
            if (minutes && minutes > 0 && minutes <= 180) {
                this.setTimerMinutes(minutes);
                document.getElementById('customMinutes').value = '';
            } else {
                this.showToast('Please enter a valid time (1-180 minutes)', 'error');
            }
        });

        // Control buttons
        document.getElementById('timerStartBtn').addEventListener('click', () => this.toggleTimer());
        document.getElementById('timerResetBtn').addEventListener('click', () => this.resetTimer());
        document.getElementById('timerSkipBtn').addEventListener('click', () => this.skipTimer());

        this.updateTimerDisplay();
    }

    setTimerMinutes(minutes) {
        if (this.timerState.isRunning) {
            this.pauseTimer();
        }
        this.timerState.currentTime = minutes * 60;
        this.timerState.totalTime = minutes * 60;
        this.updateTimerDisplay();
    }

    toggleTimer() {
        if (this.timerState.isRunning) {
            this.pauseTimer();
        } else {
            this.startTimer();
        }
    }

    startTimer() {
        this.timerState.isRunning = true;
        this.timerState.isPaused = false;
        
        const startBtn = document.getElementById('timerStartBtn');
        startBtn.innerHTML = '<span>⏸</span> Pause';
        
        document.getElementById('timerStatus').textContent = 'Focus time! 🎯';

        this.timerState.intervalId = setInterval(() => {
            this.timerState.currentTime--;
            this.updateTimerDisplay();

            if (this.timerState.currentTime <= 0) {
                this.completeTimer();
            }
        }, 1000);
    }

    pauseTimer() {
        this.timerState.isRunning = false;
        this.timerState.isPaused = true;
        clearInterval(this.timerState.intervalId);

        const startBtn = document.getElementById('timerStartBtn');
        startBtn.innerHTML = '<span>▶</span> Resume';
        
        document.getElementById('timerStatus').textContent = 'Paused';
    }

    resetTimer() {
        this.timerState.isRunning = false;
        this.timerState.isPaused = false;
        clearInterval(this.timerState.intervalId);

        this.timerState.currentTime = this.timerState.totalTime;
        
        const startBtn = document.getElementById('timerStartBtn');
        startBtn.innerHTML = '<span>▶</span> Start';
        
        document.getElementById('timerStatus').textContent = 'Ready to focus 🎯';
        
        this.updateTimerDisplay();
    }

    skipTimer() {
        if (confirm('Skip this session?')) {
            this.resetTimer();
            this.showToast('Session skipped', 'info');
        }
    }

    completeTimer() {
        clearInterval(this.timerState.intervalId);
        this.timerState.isRunning = false;

        // Save session
        const session = {
            id: Date.now(),
            duration: this.timerState.totalTime,
            notes: document.getElementById('sessionNotes').value,
            completedAt: new Date().toISOString()
        };
        this.sessions.push(session);
        this.saveToLocalStorage();

        // Reset timer
        this.resetTimer();

        // Show completion notification
        this.showToast('Session complete! Great work! 🎉', 'success');
        
        // Play sound if enabled
        if (this.settings.soundEffects) {
            this.playCompletionSound();
        }

        // Clear notes
        document.getElementById('sessionNotes').value = '';

        // Update stats
        this.updateTimerStats();
        this.updateDashboard();
    }

    updateTimerDisplay() {
        const minutes = Math.floor(this.timerState.currentTime / 60);
        const seconds = this.timerState.currentTime % 60;
        const display = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
        
        document.getElementById('timerDisplay').textContent = display;

        // Update progress circle
        const progress = (this.timerState.currentTime / this.timerState.totalTime) * 534;
        const circle = document.getElementById('timerProgress');
        circle.style.strokeDashoffset = 534 - progress;
    }

    updateTimerStats() {
        const todaySessions = this.sessions.filter(s => this.isToday(s.completedAt));
        const totalTime = todaySessions.reduce((sum, s) => sum + s.duration, 0);
        const avgTime = todaySessions.length > 0 ? totalTime / todaySessions.length : 0;

        this.safeSetText('totalTimeToday', this.formatTime(totalTime));
        this.safeSetText('sessionsCount', todaySessions.length);
        this.safeSetText('avgSession', `${Math.round(avgTime / 60)}m`);

        this.renderSessionHistory(todaySessions);
    }

    renderSessionHistory(sessions) {
        const container = document.getElementById('sessionHistory');
        
        if (sessions.length === 0) {
            container.innerHTML = `
                <div class="empty-state-small">
                    <div class="empty-icon-small">⏱️</div>
                    <p>No sessions yet</p>
                    <small>Start a timer to log your first session</small>
                </div>
            `;
            return;
        }

        container.innerHTML = sessions.map(session => `
            <div class="session-item">
                <div class="session-time">${this.formatTime(session.duration)}</div>
                <div class="session-timestamp">${new Date(session.completedAt).toLocaleTimeString()}</div>
                ${session.notes ? `<div class="session-notes">${this.escapeHtml(session.notes)}</div>` : ''}
            </div>
        `).join('');
    }

    playCompletionSound() {
        // Implement sound playback
    }

    // ====================================
    // DECKS
    // ====================================

    setupDecks() {
        document.getElementById('addDeckBtn').addEventListener('click', () => this.openDeckModal());
        document.getElementById('closeDeckModal').addEventListener('click', () => this.closeDeckModal());
        document.getElementById('cancelDeckBtn').addEventListener('click', () => this.closeDeckModal());
        document.getElementById('saveDeckBtn').addEventListener('click', () => this.saveDeck());

        document.getElementById('importDeckBtn').addEventListener('click', () => {
            this.showToast('Import functionality coming soon!', 'info');
        });

        document.getElementById('exportDecksBtn').addEventListener('click', () => {
            this.exportDecks();
        });

        this.renderDecks();
    }

    openDeckModal() {
        document.getElementById('deckModal').classList.add('active');
    }

    closeDeckModal() {
        document.getElementById('deckModal').classList.remove('active');
        this.clearDeckForm();
    }

    clearDeckForm() {
        document.getElementById('deckName').value = '';
        document.getElementById('deckDescription').value = '';
        document.getElementById('deckCategory').value = 'study';
    }

    async saveDeck() {
        const name = document.getElementById('deckName').value.trim();
        if (!name) {
            this.showToast('Please enter a deck name', 'error');
            return;
        }

        const deck = {
            id: Date.now(),
            name,
            description: document.getElementById('deckDescription').value,
            category: document.getElementById('deckCategory').value,
            cards: [],
            createdAt: new Date().toISOString()
        };

        try {
            // Try to save to API
            const response = await fetch('/api/decks', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(deck)
            });

            if (response.ok) {
                this.decks.push(deck);
                this.saveToLocalStorage();
                this.renderDecks();
                this.closeDeckModal();
                this.showToast('Deck created successfully!', 'success');
            }
        } catch (error) {
            console.error('Error saving deck:', error);
            // Save locally anyway
            this.decks.push(deck);
            this.saveToLocalStorage();
            this.renderDecks();
            this.closeDeckModal();
            this.showToast('Deck created (offline)', 'info');
        }
    }

    renderDecks() {
        const container = document.getElementById('decksGrid');
        
        if (!container) {
            console.warn('Decks container not found');
            return;
        }
        
        if (this.decks.length === 0) {
            container.innerHTML = `
                <div class="empty-state-large">
                    <div class="empty-icon-large">🗂️</div>
                    <h3>No Decks Yet</h3>
                    <p>Create your first deck to organize your study materials</p>
                    <button class="btn btn-primary mt-1" onclick="app.openDeckModal()">
                        <span>+</span> Create Deck
                    </button>
                </div>
            `;
            return;
        }

        container.innerHTML = this.decks.map(deck => `
            <div class="deck-card">
                <h3>${this.escapeHtml(deck.name)}</h3>
                <p>${this.escapeHtml(deck.description || 'No description')}</p>
                <div class="deck-meta">
                    <span>${deck.cards?.length || 0} cards</span>
                    <span>${deck.category}</span>
                </div>
            </div>
        `).join('');
    }

    exportDecks() {
        const data = JSON.stringify(this.decks, null, 2);
        const blob = new Blob([data], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `focusdeck-decks-${Date.now()}.json`;
        a.click();
        URL.revokeObjectURL(url);
        this.showToast('Decks exported!', 'success');
    }

    // ====================================
    // AUTOMATIONS
    // ====================================

    setupAutomations() {
        // This method is now less important, but we'll keep it for loading data
        this.loadAutomations();
        this.loadConnectedServices();
        this.loadAutomationStats();
    }

    setupGlobalEventListeners() {
        document.body.addEventListener('click', (e) => {
            // Create Automation Button
            if (e.target.closest('#createAutomationBtn')) {
                console.log('Create automation clicked via global listener');
                this.openAutomationModal();
            }

            // Connect Service Button
            if (e.target.closest('#connectServiceBtn')) {
                console.log('Connect service clicked via global listener');
                this.openConnectServiceModal();
            }

            // Service cards: open setup guide instead of connecting immediately
            const serviceCard = e.target.closest('.service-card');
            if (serviceCard) {
                const service = serviceCard.dataset.service;
                this.openServiceSetup(service);
            }
        });

        // Modal buttons
        document.getElementById('closeAutomationModal')?.addEventListener('click', () => this.closeAutomationModal());
        document.getElementById('cancelAutomationBtn')?.addEventListener('click', () => this.closeAutomationModal());
        document.getElementById('saveAutomationBtn')?.addEventListener('click', () => this.saveAutomation());
        document.getElementById('addActionBtn')?.addEventListener('click', () => this.addActionField());

    document.getElementById('closeConnectServiceModal')?.addEventListener('click', () => this.closeConnectServiceModal());

    // Service setup modal buttons
    document.getElementById('closeServiceSetupModal')?.addEventListener('click', () => this.closeServiceSetupModal());
    document.getElementById('cancelServiceSetupBtn')?.addEventListener('click', () => this.closeServiceSetupModal());
    document.getElementById('startServiceSetupBtn')?.addEventListener('click', () => this.startServiceSetup());

        // History modal buttons
        document.getElementById('closeHistoryModal')?.addEventListener('click', () => this.closeAutomationHistoryModal());
        document.getElementById('closeHistoryModalBtn')?.addEventListener('click', () => this.closeAutomationHistoryModal());

        // Trigger service change
        const triggerService = document.getElementById('triggerService');
        if (triggerService) {
            triggerService.addEventListener('change', () => this.updateTriggerTypes());
        }
    }

    async loadAutomations() {
        try {
            const response = await fetch('/api/automations');
            if (response.ok) {
                this.automations = await response.json();
                this.renderAutomations();
            }
        } catch (error) {
            console.error('Error loading automations:', error);
            // Load from localStorage fallback
            const stored = localStorage.getItem('automations');
            if (stored) {
                this.automations = JSON.parse(stored);
                this.renderAutomations();
            }
        }
    }

    async loadConnectedServices() {
        try {
            const response = await fetch('/api/services');
            if (response.ok) {
                this.connectedServices = await response.json();
                this.renderConnectedServices();
                
                // Check health of configured services after rendering
                setTimeout(() => this.checkAllServicesHealth(), 100);
            }
        } catch (error) {
            console.error('Error loading services:', error);
        }
    }

    async loadAutomationStats() {
        try {
            const response = await fetch('/api/automations/stats');
            if (response.ok) {
                const stats = await response.json();
                
                this.safeSetText('statsTotalAutomations', stats.totalAutomations?.toString() || '0');
                this.safeSetText('statsEnabledAutomations', stats.enabledAutomations?.toString() || '0');
                this.safeSetText('statsTotalExecutions', stats.totalExecutions?.toString() || '0');
                
                const successRate = stats.successRate || 0;
                this.safeSetText('statsSuccessRate', `${successRate.toFixed(1)}%`);
                
                // Update color based on success rate
                const successRateEl = document.getElementById('statsSuccessRate');
                if (successRateEl) {
                    if (successRate >= 90) {
                        successRateEl.style.color = 'var(--success)';
                    } else if (successRate >= 70) {
                        successRateEl.style.color = 'var(--warning)';
                    } else {
                        successRateEl.style.color = 'var(--danger)';
                    }
                }
            }
        } catch (error) {
            console.error('Error loading automation stats:', error);
        }
    }

    renderAutomations() {
        const container = document.getElementById('automationsList');
        if (!container) return;

        if (this.automations.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">🤖</div>
                    <h3>No Automations Yet</h3>
                    <p>Create your first automation to connect your workflow</p>
                </div>
            `;
            return;
        }

        container.innerHTML = this.automations.map(automation => `
            <div class="automation-card ${automation.isEnabled ? 'enabled' : 'disabled'}">
                <div class="automation-header">
                    <div class="automation-info">
                        <h3 class="automation-name">${this.escapeHtml(automation.name)}</h3>
                        <p class="automation-description">
                            <span class="trigger-badge">${this.getServiceIcon(automation.trigger.service)} ${automation.trigger.triggerType}</span>
                            → ${automation.actions.length} action${automation.actions.length !== 1 ? 's' : ''}
                        </p>
                    </div>
                    <div class="automation-actions">
                        <label class="toggle">
                            <input type="checkbox" ${automation.isEnabled ? 'checked' : ''} 
                                   onchange="app.toggleAutomation('${automation.id}')">
                            <span class="toggle-slider"></span>
                        </label>
                        <button class="btn-icon" onclick="app.viewAutomationHistory('${automation.id}', '${this.escapeHtml(automation.name)}')" title="View History">
                            📊
                        </button>
                        <button class="btn-icon" onclick="app.runAutomation('${automation.id}')" title="Run now">
                            ▶️
                        </button>
                        <button class="btn-icon" onclick="app.editAutomation('${automation.id}')" title="Edit">
                            ✏️
                        </button>
                        <button class="btn-icon" onclick="app.deleteAutomation('${automation.id}')" title="Delete">
                            🗑️
                        </button>
                    </div>
                </div>
                ${automation.lastRunAt ? `<p class="automation-last-run">Last run: ${new Date(automation.lastRunAt).toLocaleString()}</p>` : ''}
            </div>
        `).join('');
    }

    renderConnectedServices() {
        const container = document.getElementById('connectedServicesList');
        if (!container) return;

        // Separate configured and discovered (not configured) services
        const configured = this.connectedServices.filter(s => s.configured);
        const discovered = this.connectedServices.filter(s => !s.configured);

        let html = '';

        // Discovered section (services that can be added but aren't configured yet)
        if (discovered.length > 0) {
            html += `
                <div class="integrations-section">
                    <h3 class="section-header">Discovered</h3>
                    <div class="discovered-grid">
                        ${discovered.map(s => `
                            <div class="discovered-card">
                                <div class="discovered-icon">${this.getServiceIcon(s.service)}</div>
                                <div class="discovered-info">
                                    <div class="discovered-name">${s.service}</div>
                                    <div class="discovered-description">${this.getServiceDescription(s.service)}</div>
                                </div>
                                <div class="discovered-actions">
                                    <button class="btn-text" onclick="app.disconnectService('${s.id}')">Ignore</button>
                                    <button class="btn btn-primary btn-small" onclick="app.openServiceSetup('${s.service}', ${JSON.stringify(s.metadata || {})})">Add</button>
                                </div>
                            </div>
                        `).join('')}
                    </div>
                </div>
            `;
        }

        // Configured section
        if (configured.length > 0) {
            html += `
                <div class="integrations-section">
                    <h3 class="section-header">Configured</h3>
                    <div class="configured-grid">
                        ${configured.map(s => {
                            const entityCount = this.getServiceEntityCount(s);
                            const statusIcon = this.getServiceStatusIcon(s);
                            return `
                                <div class="integration-card" data-service-id="${s.id}" onclick="app.openServiceSetup('${s.service}', ${JSON.stringify(s.metadata || {})})">
                                    <div class="integration-header">
                                        <div class="integration-icon">${this.getServiceIcon(s.service)}</div>
                                        <div class="integration-title">${s.service}</div>
                                        <button class="btn-icon-more" onclick="event.stopPropagation(); app.showServiceMenu('${s.id}')" title="Options">⋮</button>
                                    </div>
                                    <div class="integration-footer">
                                        <span class="integration-count">${entityCount}</span>
                                        <div class="integration-status">
                                            ${statusIcon}
                                        </div>
                                    </div>
                                </div>
                            `;
                        }).join('')}
                    </div>
                </div>
            `;
        }

        if (configured.length === 0 && discovered.length === 0) {
            html = `
                <div class="empty-state-small">
                    <p>No services connected yet. Click "Connect New Service" to get started.</p>
                </div>
            `;
        }

        container.innerHTML = html;
    }

    getServiceDescription(service) {
        const descriptions = {
            'GoogleCalendar': 'Sync events and schedules',
            'HomeAssistant': 'Control smart home devices',
            'Spotify': 'Music playback control',
            'Canvas': 'LMS assignments and grades',
            'Notion': 'Notes and task management',
            'Todoist': 'Task synchronization',
            'Slack': 'Team notifications',
            'Discord': 'Community notifications',
            'PhilipsHue': 'Smart lighting control',
            'IFTTT': 'Automation platform',
            'Zapier': 'Workflow automation',
            'AppleMusic': 'Music playback'
        };
        return descriptions[service] || 'Integration service';
    }

    getServiceEntityCount(service) {
        // In a real implementation, this would come from the API
        // For now, use placeholder counts
        const counts = {
            'GoogleCalendar': '11 entities',
            'HomeAssistant': '2 devices',
            'Spotify': '1 service',
            'Canvas': '1 device',
            'Notion': '1 service',
            'Todoist': '1 service',
            'Slack': '1 service',
            'Discord': '1 service'
        };
        return counts[service.service] || '1 service';
    }

    getServiceStatusIcon(service) {
        // Show status badges based on configuration
        let badges = '';
        
        // Cloud connection badge
        if (['GoogleCalendar', 'Spotify', 'Notion', 'Todoist', 'Slack'].includes(service.service)) {
            badges += '<span class="status-badge" title="Cloud connected">☁️</span>';
        }
        
        // Local network badge
        if (['HomeAssistant', 'PhilipsHue'].includes(service.service)) {
            badges += '<span class="status-badge" title="Local network">🌐</span>';
        }

        // Warning badge for unconfigured issues
        if (service.metadata && service.metadata.warning) {
            badges += '<span class="status-badge status-warning" title="Needs attention">⚠️</span>';
        }

        return badges;
    }

    showServiceMenu(serviceId) {
        // TODO: Implement context menu for service options (reconfigure, disconnect, etc.)
        const service = this.connectedServices.find(s => s.id === serviceId);
        if (!service) return;
        
        if (confirm(`Disconnect ${service.service}?`)) {
            this.disconnectService(serviceId);
        }
    }

    getServiceIcon(service) {
        const icons = {
            'FocusDeck': '🎯',
            'GoogleCalendar': '📅',
            'Canvas': '🎓',
            'HomeAssistant': '🏠',
            'Spotify': '🎵',
            'GoogleDrive': '📁',
            'Notion': '📓',
            'Todoist': '✅',
            'Slack': '💬',
            'Discord': '🎮',
            'IFTTT': '🔗',
            'Zapier': '⚡',
            'PhilipsHue': '💡',
            'AppleMusic': '🎧'
        };
        return icons[service] || '🔗';
    }

    async checkServiceHealth(serviceId) {
        try {
            const response = await fetch(`/api/services/${serviceId}/health`, {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }
            
            const health = await response.json();
            return health;
        } catch (error) {
            console.error(`Health check failed for service ${serviceId}:`, error);
            return {
                healthy: false,
                status: 'error',
                message: error.message
            };
        }
    }

    async checkAllServicesHealth() {
        const configured = this.connectedServices.filter(s => s.configured);
        const healthChecks = await Promise.all(
            configured.map(s => this.checkServiceHealth(s.id))
        );

        // Update services with health data
        configured.forEach((service, index) => {
            const health = healthChecks[index];
            service.health = health;
            
            // Update status badges in the UI
            const card = document.querySelector(`[data-service-id="${service.id}"]`);
            if (card) {
                const statusContainer = card.querySelector('.integration-status');
                if (statusContainer && health) {
                    this.updateServiceStatusBadges(statusContainer, service, health);
                }
            }
        });
    }

    updateServiceStatusBadges(container, service, health) {
        let badges = '';
        
        // Health status badge
        if (health.healthy) {
            badges += '<span class="status-badge status-ok" title="Connected">✓</span>';
        } else {
            badges += '<span class="status-badge status-warning" title="' + (health.message || 'Not connected') + '">⚠️</span>';
        }
        
        // Cloud/Local badge
        if (['GoogleCalendar', 'Spotify', 'Notion', 'Todoist', 'Slack'].includes(service.service)) {
            badges += '<span class="status-badge" title="Cloud connected">☁️</span>';
        }
        
        if (['HomeAssistant', 'PhilipsHue'].includes(service.service)) {
            badges += '<span class="status-badge" title="Local network">🌐</span>';
        }

        container.innerHTML = badges;
    }

    openAutomationModal(automationId = null) {
        console.log('Opening automation modal...');
        const modal = document.getElementById('automationModal');
        console.log('Modal found:', !!modal);
        if (!modal) {
            console.error('Automation modal not found!');
            return;
        }
        modal.classList.add('active');
        console.log('Modal classes:', modal.className);

        if (automationId) {
            // Edit mode
            const automation = this.automations.find(a => a.id === automationId);
            if (automation) {
                document.getElementById('automationName').value = automation.name;
                document.getElementById('triggerService').value = automation.trigger.service;
                this.updateTriggerTypes();
                document.getElementById('triggerType').value = automation.trigger.triggerType;
                // Load actions
                this.loadActionsForEdit(automation.actions);
            }
        } else {
            // Create mode
            document.getElementById('automationName').value = '';
            document.getElementById('actionsList').innerHTML = '';
            this.updateTriggerTypes();
        }
    }

    closeAutomationModal() {
        document.getElementById('automationModal').classList.remove('active');
    }

    openConnectServiceModal() {
        console.log('Opening connect service modal...');
        const modal = document.getElementById('connectServiceModal');
        console.log('Modal found:', !!modal);
        if (!modal) {
            console.error('Connect service modal not found!');
            return;
        }
        modal.classList.add('active');
        console.log('Modal classes:', modal.className);

        // Ensure service cards open the guided setup and close this picker
        try {
            const cards = modal.querySelectorAll('.service-card');
            cards.forEach(card => {
                card.onclick = (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    const svc = card.getAttribute('data-service');
                    // Close the picker, then open the setup guide
                    this.closeConnectServiceModal();
                    this.openServiceSetup(svc);
                };
            });
        } catch (err) {
            console.warn('Failed to bind service-card clicks', err);
        }
    }

    closeConnectServiceModal() {
        document.getElementById('connectServiceModal').classList.remove('active');
    }

    // =============================
    // SERVICE SETUP (Guided)
    // =============================
    getServiceGuides() {
        // Inspired by Home Assistant integrations: show clear steps, links, and required items
        return {
            GoogleCalendar: {
                title: 'Connect Google Calendar',
                icon: '📅',
                description: 'Sync your study schedule with your Google Calendar.',
                steps: [
                    'Create a Google Cloud project and enable the Calendar API',
                    'Create OAuth 2.0 Client credentials',
                    'Add authorized redirect URI: https://your-domain.tld/api/services/google/callback (coming soon)',
                    'Click Start Setup to open the Google consent screen'
                ],
                links: [
                    { label: 'Enable Calendar API', url: 'https://console.cloud.google.com/apis/library/calendar-json.googleapis.com' },
                    { label: 'OAuth Credentials', url: 'https://console.cloud.google.com/apis/credentials' }
                ],
                flow: 'oauth'
            },
            Spotify: {
                title: 'Connect Spotify', 
                icon: '🎵',
                description: 'Control music playback during study sessions.',
                steps: [
                    'Go to Spotify Developer Dashboard and create an app',
                    'Set Redirect URI to: http://localhost:5000/api/services/oauth/spotify/callback',
                    'Copy your Client ID and Client Secret',
                    'Click Start Setup to authorize FocusDeck'
                ],
                links: [ 
                    { label: 'Spotify Developer Dashboard', url: 'https://developer.spotify.com/dashboard' },
                    { label: 'Spotify API Docs', url: 'https://developer.spotify.com/documentation/web-api' }
                ],
                fields: [
                    { id: 'spotifyClientId', label: 'Client ID', placeholder: 'Paste your Client ID' },
                    { id: 'spotifyClientSecret', label: 'Client Secret', placeholder: 'Paste your Client Secret' }
                ],
                flow: 'oauth'
            },
            HomeAssistant: {
                title: 'Connect Home Assistant', icon: '🏠',
                description: 'Trigger lights, scenes, and devices during focus sessions.',
                steps: [
                    'Open your Home Assistant profile and create a Long-Lived Access Token',
                    'Copy your Base URL (e.g., http://homeassistant.local:8123)',
                    'Paste both below and click Connect'
                ],
                links: [
                    { label: 'Create Long-Lived Access Token', url: 'https://my.home-assistant.io/redirect/profile/' },
                    { label: 'Home Assistant Docs: Long-Lived Tokens', url: 'https://www.home-assistant.io/docs/authentication/#your-account-profile' }
                ],
                fields: [
                    { id: 'haBaseUrl', label: 'Base URL', placeholder: 'http://homeassistant.local:8123' },
                    { id: 'haToken', label: 'Long-Lived Access Token', placeholder: 'Paste your token' }
                ],
                flow: 'token'
            },
            Notion: {
                title: 'Connect Notion', icon: '📓',
                description: 'Sync notes and tasks between FocusDeck and Notion.',
                steps: [
                    'Go to Notion Integrations and create a new internal integration',
                    'Copy the Internal Integration Token',
                    'Share your database(s) with the integration'
                ],
                links: [ 
                    { label: 'Create Notion Integration', url: 'https://www.notion.so/my-integrations' },
                    { label: 'Notion API Docs', url: 'https://developers.notion.com/docs/getting-started' }
                ],
                fields: [
                    { id: 'notionToken', label: 'Integration Token', placeholder: 'secret_...' }
                ],
                flow: 'token'
            },
            Todoist: {
                title: 'Connect Todoist', icon: '✅',
                description: 'Sync tasks with your Todoist projects.',
                steps: [
                    'Go to Todoist Settings → Integrations → Developer',
                    'Copy your API token'
                ],
                links: [ 
                    { label: 'Get Todoist API Token', url: 'https://todoist.com/prefs/integrations' },
                    { label: 'Todoist API Docs', url: 'https://developer.todoist.com/rest/v2/#overview' }
                ],
                fields: [
                    { id: 'todoistToken', label: 'API Token', placeholder: 'Paste your API token' }
                ],
                flow: 'token'
            },
            Slack: {
                title: 'Connect Slack', icon: '💬', description: 'Send study notifications to Slack channels.',
                steps: [
                    'Create a Slack app in your workspace',
                    'Add OAuth scopes (chat:write, channels:read)',
                    'Install app to workspace and copy Bot Token'
                ],
                links: [ 
                    { label: 'Create Slack App', url: 'https://api.slack.com/apps' },
                    { label: 'Slack OAuth Guide', url: 'https://api.slack.com/authentication/oauth-v2' }
                ],
                fields: [
                    { id: 'slackToken', label: 'Bot User OAuth Token', placeholder: 'xoxb-...' }
                ],
                flow: 'token'
            },
            Discord: {
                title: 'Connect Discord', icon: '🎮', description: 'Send notifications to your Discord server.',
                steps: [
                    'Create a Discord application and bot',
                    'Copy the Bot Token',
                    'Invite bot to your server with "Send Messages" permission'
                ],
                links: [ 
                    { label: 'Discord Developer Portal', url: 'https://discord.com/developers/applications' },
                    { label: 'Discord Bot Guide', url: 'https://discord.com/developers/docs/getting-started' }
                ],
                fields: [
                    { id: 'discordToken', label: 'Bot Token', placeholder: 'Paste your bot token' }
                ],
                flow: 'token'
            },
            PhilipsHue: {
                title: 'Connect Philips Hue', icon: '💡', description: 'Control your lighting scenes.',
                steps: [
                    'Find your Hue Bridge IP address',
                    'Press the link button on your Hue Bridge',
                    'Click Start Setup within 30 seconds to create a user'
                ],
                links: [ 
                    { label: 'Philips Hue API', url: 'https://developers.meethue.com/develop/get-started-2/' }
                ],
                fields: [
                    { id: 'hueIP', label: 'Bridge IP Address', placeholder: '192.168.1.x' }
                ],
                flow: 'token'
            },
            Canvas: {
                title: 'Connect Canvas LMS', icon: '🎓', 
                description: 'Get assignments and grades from your Canvas LMS.',
                steps: [
                    'Log into Canvas and go to Account → Settings',
                    'Scroll to "Approved Integrations" and click "+ New Access Token"',
                    'Give it a purpose (e.g., "FocusDeck") and generate',
                    'Copy the token (it will only be shown once!)'
                ],
                links: [ 
                    { label: 'Canvas Token Guide', url: 'https://community.canvaslms.com/t5/Instructor-Guide/How-do-I-manage-API-access-tokens-as-an-instructor/ta-p/1177' }
                ],
                fields: [
                    { id: 'canvasBaseUrl', label: 'Canvas Instance URL', placeholder: 'https://canvas.school.edu' },
                    { id: 'canvasToken', label: 'Access Token', placeholder: 'Paste your access token' }
                ],
                flow: 'token'
            },
            GoogleGenerativeAI: {
                title: 'Google Generative AI',
                icon: '🤖',
                description: 'Use Google\'s Gemini AI for smart study assistance.',
                steps: [
                    'Go to Google AI Studio',
                    'Click "Get API Key" and create or select a project',
                    'Copy your API key'
                ],
                links: [
                    { label: 'Get API key from here', url: 'https://aistudio.google.com/app/apikey' }
                ],
                fields: [
                    { id: 'googleAIKey', label: 'API key', placeholder: 'Paste your API key' }
                ],
                flow: 'token'
            },
                IFTTT: {
                    title: 'Connect IFTTT',
                    icon: '🔗',
                    description: 'Integrate with thousands of services via IFTTT.',
                    steps: [
                        'Go to IFTTT and connect the Webhooks service',
                        'Click on "Documentation" to view your webhook key',
                        'Copy your webhook key'
                    ],
                    links: [
                        { label: 'IFTTT Webhooks', url: 'https://ifttt.com/maker_webhooks' },
                        { label: 'IFTTT Create Applet', url: 'https://ifttt.com/create' }
                    ],
                    fields: [
                        { id: 'iftttWebhookKey', label: 'Webhook Key', placeholder: 'Paste your webhook key' }
                    ],
                    flow: 'token'
                },
                Zapier: {
                    title: 'Connect Zapier',
                    icon: '⚡',
                    description: 'Automate workflows with Zapier.',
                    steps: [
                        'Create a new Zap in Zapier',
                        'Use "Webhooks by Zapier" as the trigger',
                        'Copy the webhook URL provided by Zapier'
                    ],
                    links: [
                        { label: 'Create Zap', url: 'https://zapier.com/app/zaps' },
                        { label: 'Zapier Webhooks Guide', url: 'https://zapier.com/apps/webhook/integrations' }
                    ],
                    fields: [
                        { id: 'zapierWebhookUrl', label: 'Webhook URL', placeholder: 'https://hooks.zapier.com/hooks/catch/...' }
                    ],
                    flow: 'token'
                },
                AppleMusic: {
                    title: 'Connect Apple Music',
                    icon: '🎧',
                    description: 'Control Apple Music playback during study sessions.',
                    steps: [
                        'Sign up for Apple Developer Program',
                        'Create a MusicKit identifier and key',
                        'Download your private key (.p8 file)',
                        'Copy your Team ID and Key ID'
                    ],
                    links: [
                        { label: 'Apple Developer', url: 'https://developer.apple.com/' },
                        { label: 'MusicKit Documentation', url: 'https://developer.apple.com/documentation/musickit' }
                    ],
                    fields: [
                        { id: 'appleMusicTeamId', label: 'Team ID', placeholder: 'Your 10-character Team ID' },
                        { id: 'appleMusicKeyId', label: 'Key ID', placeholder: 'Your Key ID' },
                        { id: 'appleMusicPrivateKey', label: 'Private Key', placeholder: 'Paste .p8 file contents' }
                    ],
                    flow: 'token'
                },
                GoogleDrive: {
                    title: 'Connect Google Drive',
                    icon: '📁',
                    description: 'Access and organize study materials from Google Drive.',
                    steps: [
                        'Create a Google Cloud project',
                        'Enable Google Drive API',
                        'Create OAuth 2.0 credentials',
                        'Add redirect URI: http://localhost:5000/api/services/oauth/googledrive/callback'
                    ],
                    links: [
                        { label: 'Enable Drive API', url: 'https://console.cloud.google.com/apis/library/drive.googleapis.com' },
                        { label: 'OAuth Credentials', url: 'https://console.cloud.google.com/apis/credentials' }
                    ],
                    fields: [
                        { id: 'googleDriveClientId', label: 'Client ID', placeholder: 'Paste your Client ID' },
                        { id: 'googleDriveClientSecret', label: 'Client Secret', placeholder: 'Paste your Client Secret' }
                    ],
                    flow: 'oauth'
                }
        };
    }

    async openServiceSetup(service, metadata = {}) {
        // Always hide the service picker to prevent overlay conflicts
        this.closeConnectServiceModal();

        // Show modal immediately with loading state
        const modal = document.getElementById('serviceSetupModal');
        const body = document.getElementById('serviceSetupBody');
        const title = document.getElementById('serviceSetupTitle');
        
        if (modal) modal.classList.add('active');
        if (title) title.textContent = `Setting up ${service}...`;
        if (body) body.innerHTML = '<p class="setup-description">Loading setup guide from server...</p>';

        try {
            // Fetch guide from server
            const response = await fetch(`/api/services/${service}/setup`);
            if (!response.ok) {
                throw new Error(`Server returned ${response.status}`);
            }
            const serverGuide = await response.json();

            // Convert server response to frontend format
            const guide = {
                title: serverGuide.title || `Connect ${service}`,
                icon: '🔗',
                description: serverGuide.description || '',
                steps: serverGuide.steps || [],
                links: serverGuide.links || [],
                requiredServerConfig: serverGuide.requiredServerConfig || [],
                fields: [],
                flow: serverGuide.setupType === 'OAuth' ? 'oauth' : 'token'
            };

            // Convert server fields to frontend format
            if (serverGuide.fields) {
                guide.fields = serverGuide.fields.map(f => ({
                    id: f.key,
                    label: f.label,
                    placeholder: f.helpText || '',
                    type: f.inputType || 'text'
                }));
            }

            this.currentServiceSetup = { service, guide };

            // Render the guide
            if (title) title.textContent = `${guide.icon} ${guide.title}`;

            const stepsHtml = guide.steps?.length ? `
                <h3 class="section-title">Setup Instructions</h3>
                <ol class="setup-steps">${guide.steps.map(s => `<li>${this.escapeHtml(s)}</li>`).join('')}</ol>
            ` : '';

            const linksHtml = guide.links?.length ? `
                <h3 class="section-title">Helpful Links</h3>
                <div class="links-list">
                    ${guide.links.map(l => `<a href="${l.url}" target="_blank" class="doc-link">${this.escapeHtml(l.label)} →</a>`).join('')}
                </div>
            ` : '';

            const serverConfigHtml = guide.requiredServerConfig?.length ? `
                <h3 class="section-title">Required Server Configuration</h3>
                <div class="server-config-box">
                    <code>${guide.requiredServerConfig.map(line => this.escapeHtml(line)).join('<br>')}</code>
                </div>
                <p class="help-text">Add this to your FocusDeck server's <strong>appsettings.json</strong> file and restart the server before clicking the OAuth button below.</p>
            ` : '';

            const fieldsHtml = (guide.fields || []).map(f => `
                <div class="form-group">
                    <label class="form-label" for="${f.id}">${this.escapeHtml(f.label)}</label>
                    <input type="${f.type || 'text'}" id="${f.id}" class="input-field" placeholder="${this.escapeHtml(f.placeholder || '')}">
                    ${f.placeholder ? `<p class="help-text">${this.escapeHtml(f.placeholder)}</p>` : ''}
                </div>
            `).join('');

            if (body) {
                body.innerHTML = `
                    <p class="setup-description">${this.escapeHtml(guide.description || '')}</p>
                    ${stepsHtml}
                    ${linksHtml}
                    ${serverConfigHtml}
                    ${fieldsHtml ? `<h3 class="section-title">Required Information</h3>${fieldsHtml}` : ''}
                    <div class="help-text" style="margin-top: .5rem; color: var(--text-secondary);">
                        Mode: <strong>${guide.flow.toUpperCase()}</strong>
                    </div>
                `;
            }

            // Prefill if we have metadata
            try {
                if (guide.fields && metadata) {
                    for (const f of guide.fields) {
                        const el = document.getElementById(f.id);
                        if (el && metadata[f.id]) {
                            el.value = metadata[f.id];
                        }
                    }
                }
            } catch {}

        } catch (error) {
            console.error('Failed to load setup guide:', error);
            if (title) title.textContent = `Setup ${service}`;
            if (body) {
                body.innerHTML = `
                    <p class="error">Failed to load setup guide from server: ${error.message}</p>
                    <p class="help-text">The server may not have configuration for this service yet.</p>
                `;
            }
            this.showToast('Failed to load setup guide', 'error');
        }
    }

    closeServiceSetupModal() {
        document.getElementById('serviceSetupModal')?.classList.remove('active');
    }

    async startServiceSetup() {
        if (!this.currentServiceSetup) return;
        const { service, guide } = this.currentServiceSetup;

        try {
            // Collect all field values
            const payload = {};
            if (guide.fields) {
                for (const f of guide.fields) {
                    const el = document.getElementById(f.id);
                    if (el && el.value) {
                        payload[f.id] = el.value;
                    }
                }
            }

            if (guide.flow === 'oauth') {
                // For OAuth services, save the config first (clientId, clientSecret)
                if (payload.clientId || payload.clientSecret) {
                    const configPayload = {
                        clientId: payload.clientId || null,
                        clientSecret: payload.clientSecret || null,
                        apiKey: payload.apiKey || null
                    };
                    
                    const saveResp = await fetch(`/api/services/${service}/config`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(configPayload)
                    });

                    if (!saveResp.ok) {
                        this.showToast('Failed to save configuration', 'error');
                        return;
                    }
                    this.showToast('Configuration saved! Starting OAuth flow...', 'success');
                }

                // Now try to get OAuth URL from the server
                const resp = await fetch(`/api/services/oauth/${service}/url`);
                if (!resp.ok) {
                    const error = await resp.json();
                    if (error.needsConfig) {
                        this.showToast('Please enter your Client ID and Client Secret above first', 'warning');
                    } else {
                        this.showToast('OAuth not configured yet', 'error');
                    }
                    return;
                }
                
                const { url } = await resp.json();
                if (url) {
                    window.open(url, '_blank');
                    this.showToast('Opened authorization in a new tab. Complete the OAuth flow there.', 'info');
                    return;
                }
                
                this.showToast('OAuth flow not available', 'warning');
                return;
            }

            // Token/manual flow — send credentials directly to connect
            const connectPayload = {};
            for (const key in payload) {
                if (key.toLowerCase().includes('token')) {
                    connectPayload['access_token'] = payload[key];
                } else {
                    connectPayload[key] = payload[key];
                }
            }

            const response = await fetch(`/api/services/connect/${service}`, {
                method: 'POST', 
                headers: { 'Content-Type': 'application/json' }, 
                body: JSON.stringify(connectPayload)
            });
            
            if (response.ok) {
                this.showToast(`${service} connected!`, 'success');
                this.loadConnectedServices();
                this.closeServiceSetupModal();
                this.closeConnectServiceModal();
            } else {
                const text = await response.text();
                let msg = 'Failed to connect';
                try { const j = JSON.parse(text); msg = j.message || msg; } catch {}
                if (!msg || msg === 'undefined') msg = response.statusText || 'Failed to connect';
                this.showToast(msg, 'error');
            }
        } catch (err) {
            console.error('Service setup error:', err);
            this.showToast('Failed to start setup. See console for details.', 'error');
        }
    }

    updateTriggerTypes() {
        const service = document.getElementById('triggerService').value;
        const typeSelect = document.getElementById('triggerType');

        const triggersByService = {
            'FocusDeck': [
                { value: 'time.specific', label: '⏰ At Specific Time' },
                { value: 'time.recurring', label: '🔁 Recurring Schedule' },
                { value: 'session.started', label: '▶️ Session Started' },
                { value: 'session.completed', label: '✅ Session Completed' },
                { value: 'session.paused', label: '⏸️ Session Paused' },
                { value: 'break.started', label: '☕ Break Started' },
                { value: 'break.ended', label: '🔄 Break Ended' },
                { value: 'task.created', label: '📝 Task Created' },
                { value: 'task.completed', label: '✓ Task Completed' },
                { value: 'task.due_approaching', label: '⚠️ Task Due Soon' },
                { value: 'task.overdue', label: '🚨 Task Overdue' },
                { value: 'task.priority_high', label: '🔴 High Priority Task Added' },
                { value: 'deck.created', label: '🃏 Deck Created' },
                { value: 'study.session_milestone', label: '🎯 Study Milestone Reached' },
                { value: 'productivity.goal_met', label: '🏆 Daily Goal Met' },
                { value: 'productivity.streak', label: '🔥 Productivity Streak' }
            ],
            'GoogleCalendar': [
                { value: 'google_calendar.event_start', label: '📅 Event Starts' },
                { value: 'google_calendar.event_end', label: '🏁 Event Ends' },
                { value: 'google_calendar.event_created', label: '➕ New Event Created' },
                { value: 'google_calendar.event_updated', label: '✏️ Event Updated' },
                { value: 'google_calendar.event_cancelled', label: '❌ Event Cancelled' },
                { value: 'google_calendar.reminder', label: '🔔 Event Reminder (15 min)' },
                { value: 'google_calendar.all_day_event', label: '📆 All-Day Event' }
            ],
            'Canvas': [
                { value: 'canvas.assignment_due', label: '📚 Assignment Due' },
                { value: 'canvas.assignment_posted', label: '📝 New Assignment Posted' },
                { value: 'canvas.new_grade', label: '💯 New Grade Posted' },
                { value: 'canvas.new_announcement', label: '📢 New Announcement' },
                { value: 'canvas.discussion_post', label: '💬 New Discussion Post' },
                { value: 'canvas.quiz_available', label: '📋 Quiz Available' },
                { value: 'canvas.submission_graded', label: '✅ Submission Graded' }
            ],
            'HomeAssistant': [
                { value: 'home_assistant.webhook', label: '🔗 Webhook Received' },
                { value: 'home_assistant.device_state', label: '💡 Device State Changed' },
                { value: 'home_assistant.motion_detected', label: '👋 Motion Detected' },
                { value: 'home_assistant.door_opened', label: '🚪 Door Opened' },
                { value: 'home_assistant.temperature', label: '🌡️ Temperature Change' }
            ],
            'Spotify': [
                { value: 'spotify.playback_started', label: '▶️ Playback Started' },
                { value: 'spotify.playback_paused', label: '⏸️ Playback Paused' },
                { value: 'spotify.song_changed', label: '🎵 Song Changed' },
                { value: 'spotify.playlist_ended', label: '🏁 Playlist Ended' }
            ],
            'Notion': [
                { value: 'notion.page_created', label: '📄 Page Created' },
                { value: 'notion.page_updated', label: '✏️ Page Updated' },
                { value: 'notion.database_item_added', label: '➕ Database Item Added' },
                { value: 'notion.task_completed', label: '✅ Task Completed' }
            ],
            'Todoist': [
                { value: 'todoist.task_created', label: '📝 Task Created' },
                { value: 'todoist.task_completed', label: '✓ Task Completed' },
                { value: 'todoist.task_due', label: '⏰ Task Due' },
                { value: 'todoist.project_created', label: '📁 Project Created' }
            ],
            'Slack': [
                { value: 'slack.message_received', label: '💬 Message Received' },
                { value: 'slack.mention', label: '👤 Mentioned in Channel' },
                { value: 'slack.channel_joined', label: '🚪 Joined Channel' }
            ],
            'Discord': [
                { value: 'discord.message_received', label: '💬 Message Received' },
                { value: 'discord.mention', label: '👤 Mentioned' },
                { value: 'discord.voice_joined', label: '🎤 Joined Voice Channel' }
            ],
            'PhilipsHue': [
                { value: 'hue.light_on', label: '💡 Light Turned On' },
                { value: 'hue.light_off', label: '🌙 Light Turned Off' },
                { value: 'hue.scene_activated', label: '🎨 Scene Activated' }
            ]
        };

        const triggers = triggersByService[service] || [];
        typeSelect.innerHTML = triggers.map(t => 
            `<option value="${t.value}">${t.label}</option>`
        ).join('');
    }

    addActionField() {
        const container = document.getElementById('actionsList');
        const actionIndex = container.children.length;

        const actionHtml = `
            <div class="action-field" data-index="${actionIndex}">
                <select class="select-field action-type">
                    <optgroup label="⏱️ Timer Actions">
                        <option value="timer.start">Start Timer</option>
                        <option value="timer.pause">Pause Timer</option>
                        <option value="timer.stop">Stop Timer</option>
                        <option value="timer.set_duration">Set Timer Duration</option>
                        <option value="timer.start_break">Start Break</option>
                    </optgroup>
                    <optgroup label="📝 Task Actions">
                        <option value="task.create">Create Task</option>
                        <option value="task.complete">Complete Task</option>
                        <option value="task.set_priority">Set Task Priority</option>
                        <option value="task.add_tag">Add Tag to Task</option>
                        <option value="task.schedule">Schedule Task</option>
                    </optgroup>
                    <optgroup label="🔔 Notification Actions">
                        <option value="notification.show">Show Notification</option>
                        <option value="notification.sound">Play Sound</option>
                        <option value="notification.email">Send Email</option>
                        <option value="notification.desktop">Desktop Notification</option>
                    </optgroup>
                    <optgroup label="💡 Smart Home Actions">
                        <option value="lights.set_scene">Set Lighting Scene</option>
                        <option value="lights.turn_on">Turn On Lights</option>
                        <option value="lights.turn_off">Turn Off Lights</option>
                        <option value="lights.dim">Dim Lights</option>
                        <option value="lights.color">Set Light Color</option>
                        <option value="home_assistant.turn_on">Turn On Device</option>
                        <option value="home_assistant.turn_off">Turn Off Device</option>
                        <option value="home_assistant.set_temperature">Set Temperature</option>
                    </optgroup>
                    <optgroup label="🎵 Music Actions">
                        <option value="spotify.play_playlist">Play Spotify Playlist</option>
                        <option value="spotify.play">Resume Playback</option>
                        <option value="spotify.pause">Pause Playback</option>
                        <option value="spotify.next">Next Track</option>
                        <option value="spotify.set_volume">Set Volume</option>
                        <option value="music.play_focus">Play Focus Music</option>
                    </optgroup>
                    <optgroup label="🎯 FocusDeck Actions">
                        <option value="deck.open">Open Deck</option>
                        <option value="deck.review">Start Deck Review</option>
                        <option value="session.log">Log Study Session</option>
                        <option value="analytics.update">Update Analytics</option>
                    </optgroup>
                    <optgroup label="🔗 Integration Actions">
                        <option value="webhook.send">Send Webhook</option>
                        <option value="api.call">Call External API</option>
                        <option value="calendar.create_event">Create Calendar Event</option>
                        <option value="canvas.submit_assignment">Submit Canvas Assignment</option>
                    </optgroup>
                    <optgroup label="⚙️ System Actions">
                        <option value="system.log">Write to Log</option>
                        <option value="system.delay">Wait/Delay</option>
                        <option value="system.condition">Conditional Action</option>
                    </optgroup>
                </select>
                <button class="btn-icon" onclick="app.removeActionField(${actionIndex})">❌</button>
            </div>
        `;

        container.insertAdjacentHTML('beforeend', actionHtml);
    }

    removeActionField(index) {
        const field = document.querySelector(`.action-field[data-index="${index}"]`);
        if (field) field.remove();
    }

    async saveAutomation() {
        const name = document.getElementById('automationName').value;
        const triggerService = document.getElementById('triggerService').value;
        const triggerType = document.getElementById('triggerType').value;

        if (!name || !triggerType) {
            this.showToast('Please fill in all required fields', 'error');
            return;
        }

        const actions = Array.from(document.querySelectorAll('.action-field')).map(field => ({
            actionType: field.querySelector('.action-type').value,
            settings: {}
        }));

        const automation = {
            name,
            isEnabled: true,
            trigger: {
                service: triggerService,
                triggerType: triggerType,
                settings: {}
            },
            actions
        };

        try {
            const response = await fetch('/api/automations', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(automation)
            });

            if (response.ok) {
                this.showToast('Automation created!', 'success');
                this.loadAutomations();
                this.closeAutomationModal();
            } else {
                const result = await response.json();
                this.showToast(`Failed to create automation: ${result.message}`, 'error');
            }
        } catch (error) {
            console.error('Error saving automation:', error);
            // Save locally as fallback
            automation.id = Date.now().toString();
            automation.createdAt = new Date().toISOString();
            this.automations.push(automation);
            localStorage.setItem('automations', JSON.stringify(this.automations));
            this.renderAutomations();
            this.closeAutomationModal();
            this.showToast('Automation created (offline)', 'info');
        }
    }

    async toggleAutomation(id) {
        try {
            await fetch(`/api/automations/${id}/toggle`, { method: 'POST' });
            this.loadAutomations();
        } catch (error) {
            console.error('Error toggling automation:', error);
        }
    }

    async runAutomation(id) {
        try {
            await fetch(`/api/automations/${id}/run`, { method: 'POST' });
            this.showToast('Automation triggered!', 'success');
            this.loadAutomations();
        } catch (error) {
            console.error('Error running automation:', error);
            this.showToast('Failed to run automation', 'error');
        }
    }

    async deleteAutomation(id) {
        if (!confirm('Delete this automation?')) return;

        try {
            await fetch(`/api/automations/${id}`, { method: 'DELETE' });
            this.showToast('Automation deleted', 'success');
            this.loadAutomations();
        } catch (error) {
            console.error('Error deleting automation:', error);
        }
    }

    async viewAutomationHistory(id, name) {
        const modal = document.getElementById('automationHistoryModal');
        const title = document.getElementById('historyModalTitle');
        const tableBody = document.getElementById('historyTableBody');
        
        if (title) {
            title.textContent = `History: ${name}`;
        }

        try {
            // Fetch history from API
            const response = await fetch(`/api/automations/${id}/history?limit=50`);
            const history = await response.json();

            if (response.ok && history.length > 0) {
                // Calculate stats
                const totalExecutions = history.length;
                const successCount = history.filter(h => h.success).length;
                const successRate = ((successCount / totalExecutions) * 100).toFixed(1);
                const avgDuration = (history.reduce((sum, h) => sum + h.durationMs, 0) / totalExecutions).toFixed(0);
                const lastRun = history[0].executedAt;

                // Update stats
                this.safeSetText('historyTotalExecutions', totalExecutions.toString());
                this.safeSetText('historySuccessRate', `${successRate}%`);
                this.safeSetText('historyAvgDuration', `${avgDuration}ms`);
                this.safeSetText('historyLastRun', new Date(lastRun).toLocaleString());

                // Update table
                if (tableBody) {
                    tableBody.innerHTML = history.map(exec => {
                        const status = exec.success 
                            ? '<span style="color: var(--success);">✓ Success</span>' 
                            : '<span style="color: var(--error);">✗ Failed</span>';
                        const errorMsg = exec.errorMessage || '-';
                        const duration = `${exec.durationMs}ms`;
                        const time = new Date(exec.executedAt).toLocaleString();

                        return `
                            <tr>
                                <td>${time}</td>
                                <td>${status}</td>
                                <td>${duration}</td>
                                <td style="max-width: 300px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" title="${this.escapeHtml(errorMsg)}">${this.escapeHtml(errorMsg)}</td>
                            </tr>
                        `;
                    }).join('');
                }
            } else {
                // No history
                this.safeSetText('historyTotalExecutions', '0');
                this.safeSetText('historySuccessRate', '0%');
                this.safeSetText('historyAvgDuration', '0ms');
                this.safeSetText('historyLastRun', 'Never');

                if (tableBody) {
                    tableBody.innerHTML = `
                        <tr>
                            <td colspan="4" style="text-align: center; padding: 2rem; color: var(--text-secondary);">
                                No execution history yet
                            </td>
                        </tr>
                    `;
                }
            }

            // Show modal
            if (modal) modal.style.display = 'flex';

        } catch (error) {
            console.error('Error fetching automation history:', error);
            this.showToast('Failed to load history', 'error');
        }
    }

    closeAutomationHistoryModal() {
        const modal = document.getElementById('automationHistoryModal');
        if (modal) modal.style.display = 'none';
    }

    async connectService(service) {
        try {
            const response = await fetch(`/api/services/connect/${service}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({}) // Empty body for demo
            });

            if (response.ok) {
                this.showToast(`${service} connected!`, 'success');
                this.loadConnectedServices();
                this.closeConnectServiceModal();
            } else {
                const text = await response.text();
                let msg = 'Failed to connect';
                try { const j = JSON.parse(text); msg = j.message || msg; } catch {}
                if (!msg || msg === 'undefined') msg = response.statusText || 'Failed to connect';
                this.showToast(`Failed to connect: ${msg}`, 'error');
            }
        } catch (error) {
            console.error('Error connecting service:', error);
            this.showToast('Failed to connect service', 'error');
        }
    }

    async disconnectService(id) {
        if (!confirm('Disconnect this service?')) return;

        try {
            await fetch(`/api/services/${id}`, { method: 'DELETE' });
            this.showToast('Service disconnected', 'success');
            this.loadConnectedServices();
        } catch (error) {
            console.error('Error disconnecting service:', error);
            this.showToast('Failed to disconnect service', 'error');
        }
    }

    // ====================================
    // SETTINGS
    // ====================================

    setupSettings() {
        this.safeSetText('apiEndpoint', window.location.origin + '/api');

        // Token generation
        const generateTokenBtn = document.getElementById('generateTokenBtn');
        if (generateTokenBtn) {
            generateTokenBtn.addEventListener('click', () => this.generateToken());
        }

        // Update configuration check
        const checkConfigBtn = document.getElementById('checkConfigBtn');
        if (checkConfigBtn) {
            checkConfigBtn.addEventListener('click', () => this.checkUpdateConfiguration());
            // Auto-check on load
            setTimeout(() => this.checkUpdateConfiguration(), 800);
        }

        // Update buttons
        const checkUpdatesBtn = document.getElementById('checkUpdatesBtn');
        if (checkUpdatesBtn) {
            checkUpdatesBtn.addEventListener('click', () => this.checkForUpdates());
            // Auto-check for updates on page load
            setTimeout(() => this.checkForUpdates(), 500);
        }

        const updateServerBtn = document.getElementById('updateServerBtn');
        if (updateServerBtn) {
            updateServerBtn.addEventListener('click', () => this.updateServer());
        }

        const viewUpdateGuideBtn = document.getElementById('viewUpdateGuideBtn');
        if (viewUpdateGuideBtn) {
            viewUpdateGuideBtn.addEventListener('click', () => this.showUpdateModal());
        }

        // Update modal close buttons
        const closeUpdateModal = document.getElementById('closeUpdateModal');
        if (closeUpdateModal) {
            closeUpdateModal.addEventListener('click', () => {
                document.getElementById('updateModal').classList.remove('active');
            });
        }

        const closeUpdateModalBtn = document.getElementById('closeUpdateModalBtn');
        if (closeUpdateModalBtn) {
            closeUpdateModalBtn.addEventListener('click', () => {
                document.getElementById('updateModal').classList.remove('active');
            });
        }

        // Copy update command
        const copyUpdateCmd = document.getElementById('copyUpdateCmd');
        if (copyUpdateCmd) {
            copyUpdateCmd.addEventListener('click', () => this.copyUpdateCommand());
        }

        // Load last update time
        const lastUpdate = localStorage.getItem('focusdeck-last-update');
        if (lastUpdate) {
            const date = new Date(lastUpdate);
            this.safeSetText('lastUpdateTime', date.toLocaleDateString() + ' ' + date.toLocaleTimeString());
        }

        document.getElementById('clearCacheBtn').addEventListener('click', () => {
            if (confirm('Clear all cached data?')) {
                localStorage.clear();
                this.showToast('Cache cleared', 'success');
            }
        });

        document.getElementById('exportDataBtn').addEventListener('click', () => {
            this.exportAllData();
        });

        document.getElementById('resetDataBtn').addEventListener('click', () => {
            if (confirm('⚠️ This will delete ALL your data. Are you sure?')) {
                this.tasks = [];
                this.decks = [];
                this.sessions = [];
                this.saveToLocalStorage();
                this.showToast('All data reset', 'info');
                window.location.reload();
            }
        });

        // Save settings on change
        ['themeSetting', 'notificationsSetting', 'soundSetting', 'pomodoroDefault', 'shortBreakDefault', 'autoStartBreaks'].forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                element.addEventListener('change', () => this.saveSettings());
            }
        });
    }

    exportAllData() {
        const data = {
            tasks: this.tasks,
            decks: this.decks,
            sessions: this.sessions,
            settings: this.settings,
            exportedAt: new Date().toISOString()
        };

        const json = JSON.stringify(data, null, 2);
        const blob = new Blob([json], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `focusdeck-backup-${Date.now()}.json`;
        a.click();
        URL.revokeObjectURL(url);
        this.showToast('Data exported!', 'success');
    }

    async checkForUpdates() {
        this.showToast('Checking for updates...', 'info');
        
        try {
            // Use the server's check-updates endpoint which properly compares versions
            const response = await fetch('/api/server/check-updates');
            const data = await response.json();
            
            if (data.updateAvailable) {
                // Update available
                const doUpdate = confirm(
                    `Update Available!\n\n` +
                    `Current: ${data.currentCommit}\n` +
                    `Latest: ${data.latestCommit}\n\n` +
                    `Message: ${data.latestMessage}\n\n` +
                    `Do you want to update the server now?`
                );
                
                if (doUpdate) {
                    await this.performServerUpdate();
                }
            } else {
                this.showToast(data.message || 'You are running the latest version!', 'success');
            }
        } catch (error) {
            console.error('Error checking updates:', error);
            this.showToast('Could not check for updates', 'error');
        }
    }

    async performServerUpdate() {
        this.showToast('Starting server update...', 'info');
        
        try {
            const response = await fetch('/api/server/update', {
                method: 'POST'
            });
            
            if (response.ok) {
                const result = await response.json();
                this.showToast('Update started! Server will restart in a moment...', 'success');
                
                // Wait for server to restart and reload page
                setTimeout(() => {
                    this.showToast('Reloading page...', 'info');
                    setTimeout(() => {
                        window.location.reload();
                    }, 3000);
                }, 5000);
            } else {
                const error = await response.json();
                this.showToast(`Update failed: ${error.message}`, 'error');
            }
        } catch (error) {
            console.error('Error updating server:', error);
            this.showToast('Update failed. Check server logs for details.', 'error');
        }
    }

    showUpdateModal() {
        const modal = document.getElementById('updateModal');
        if (modal) {
            modal.classList.add('active');
        }
    }

    copyUpdateCommand() {
        const command = `cd ~/FocusDeck && \\
git pull origin master && \\
cd src/FocusDeck.Server && \\
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server && \\
sudo systemctl restart focusdeck`;

        navigator.clipboard.writeText(command).then(() => {
            this.showToast('Command copied to clipboard!', 'success');
            const btn = document.getElementById('copyUpdateCmd');
            if (btn) {
                btn.textContent = '✓ Copied!';
                setTimeout(() => {
                    btn.textContent = '📋 Copy';
                }, 2000);
            }
        }).catch(err => {
            console.error('Failed to copy:', err);
            this.showToast('Please copy the command manually', 'error');
        });
    }

    // ====================================
    // DATA MANAGEMENT
    // ====================================

    async loadFromAPI() {
        try {
            const response = await fetch('/api/decks');
            if (response.ok) {
                const apiDecks = await response.json();
                // Merge with local decks
                this.decks = [...apiDecks, ...this.decks];
                this.renderDecks();
            }
        } catch (error) {
            console.log('Loading from local storage only');
        }

        this.loadFromLocalStorage();
    }

    loadFromLocalStorage() {
        try {
            const data = localStorage.getItem('focusdeck-data');
            if (data) {
                const parsed = JSON.parse(data);
                this.tasks = parsed.tasks || [];
                this.sessions = parsed.sessions || [];
                
                this.renderTasks();
                this.updateDashboard();
                this.updateTimerStats();
            }
        } catch (error) {
            console.error('Error loading from localStorage:', error);
        }
    }

    saveToLocalStorage() {
        try {
            const data = {
                tasks: this.tasks,
                decks: this.decks,
                sessions: this.sessions
            };
            localStorage.setItem('focusdeck-data', JSON.stringify(data));
        } catch (error) {
            console.error('Error saving to localStorage:', error);
        }
    }

    loadSettings() {
        try {
            const saved = localStorage.getItem('focusdeck-settings');
            return saved ? JSON.parse(saved) : {
                theme: 'dark',
                notifications: true,
                soundEffects: true,
                pomodoroDefault: 25,
                shortBreak: 5,
                autoStartBreaks: false
            };
        } catch (error) {
            return {};
        }
    }

    saveSettings() {
        const settings = {
            theme: document.getElementById('themeSetting')?.value,
            notifications: document.getElementById('notificationsSetting')?.checked,
            soundEffects: document.getElementById('soundSetting')?.checked,
            pomodoroDefault: parseInt(document.getElementById('pomodoroDefault')?.value),
            shortBreak: parseInt(document.getElementById('shortBreakDefault')?.value),
            autoStartBreaks: document.getElementById('autoStartBreaks')?.checked
        };

        this.settings = settings;
        localStorage.setItem('focusdeck-settings', JSON.stringify(settings));
        this.showToast('Settings saved', 'success');
    }

    // ====================================
    // UTILITIES
    // ====================================

    safeSetText(elementId, text) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = text;
        }
    }

    safeSetHTML(elementId, html) {
        const element = document.getElementById(elementId);
        if (element) {
            element.innerHTML = html;
        }
    }

    // Safely set an element property (e.g., value) if the element exists
    safeSetProperty(elementId, prop, value) {
        const element = document.getElementById(elementId) || document.querySelector(`#${elementId}, ${elementId}`);
        if (!element) return;
        try {
            element[prop] = value;
        } catch (e) {
            // no-op
        }
    }

    showToast(message, type = 'info') {
        const toast = document.getElementById('toast');
        toast.textContent = message;
        toast.className = `toast ${type} show`;

        setTimeout(() => {
            toast.classList.remove('show');
        }, 3000);
    }

    formatTime(seconds) {
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        
        if (hours > 0) {
            return `${hours}h ${minutes}m`;
        }
        return `${minutes}m`;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    async updateServer() {
        const confirmation = 'This will update and restart the server.\n\nThe process typically takes 30-60 seconds.\n\nContinue?';
        if (!confirm(confirmation)) {
            return;
        }

        const updateButton = document.getElementById('updateServerBtn');
        const checkButton = document.getElementById('checkUpdatesBtn');
        const statusBox = document.getElementById('updateStatusBox');
        const statusTitle = document.getElementById('updateStatusTitle');
        const statusMessage = document.getElementById('updateStatusMessage');

        if (updateButton) {
            updateButton.disabled = true;
            updateButton.textContent = 'Updating...';
        }
        if (checkButton) {
            checkButton.disabled = true;
        }
        if (statusBox) {
            statusBox.style.display = 'block';
        }

        try {
            this.showToast('Starting server update. This may take up to a minute.', 'info');

            const response = await fetch('/api/update/trigger', { method: 'POST' });
            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.message || 'Update failed.');
            }

            if (statusTitle) statusTitle.textContent = 'Server is updating...';
            if (statusMessage) statusMessage.textContent = 'Waiting for the service to restart. The page will reload automatically.';
            this.showToast(result.message || 'Update started.', 'success');

            let attempts = 0;
            const maxAttempts = 60;
            const interval = setInterval(async () => {
                attempts += 1;
                if (statusMessage) {
                    statusMessage.textContent = `Waiting for server to restart... (${attempts}/${maxAttempts}s)`;
                }

                try {
                    const healthCheck = await fetch('/api/server/health', { cache: 'no-cache' });
                    if (healthCheck.ok) {
                        clearInterval(interval);
                        if (statusTitle) statusTitle.textContent = 'Update complete';
                        if (statusMessage) statusMessage.textContent = 'Server is back online. Reloading in two seconds...';
                        this.showToast('Update complete. Reloading page...', 'success');

                        localStorage.setItem('focusdeck-last-update', new Date().toISOString());
                        setTimeout(() => window.location.reload(), 2000);
                    }
                } catch {
                    // Server still restarting
                }

                if (attempts >= maxAttempts) {
                    clearInterval(interval);
                    if (statusTitle) statusTitle.textContent = 'Please refresh manually';
                    if (statusMessage) statusMessage.textContent = 'The server is taking longer than expected. Refresh the page to continue.';
                    this.showToast('Server may be updated. Refresh the page to continue.', 'warning');

                    if (updateButton) {
                        updateButton.disabled = false;
                        updateButton.textContent = 'Refresh Page';
                        updateButton.onclick = () => window.location.reload();
                    }
                    if (checkButton) {
                        checkButton.disabled = false;
                    }
                }
            }, 1000);
        } catch (error) {
            console.error('Update request failed:', error);
            this.showToast(error.message || 'Update failed. Check server logs for details.', 'error');

            if (updateButton) {
                updateButton.disabled = false;
                updateButton.textContent = 'Update Server Now';
                updateButton.onclick = () => this.updateServer();
            }
            if (checkButton) {
                checkButton.disabled = false;
            }
            if (statusBox) {
                statusBox.style.display = 'none';
            }
        }
    }    async checkUpdateStatus() {
        try {
            const response = await fetch('/api/server/update-status');
            if (response.ok) {
                const result = await response.json();
                return result;
            }
        } catch (error) {
            console.error('Failed to check update status:', error);
        }
        return null;
    }

    // ====================================
    // TOKEN GENERATION
    // ====================================

    async generateToken() {
        const usernameInput = document.getElementById('tokenUsername');
        const generateBtn = document.getElementById('generateTokenBtn');
        const resultBox = document.getElementById('tokenResultBox');
        const tokenDisplay = document.getElementById('tokenDisplay');
        const tokenUserDisplay = document.getElementById('tokenUserDisplay');
        const tokenExpiry = document.getElementById('tokenExpiry');

        const username = usernameInput?.value?.trim();
        if (!username) {
            this.showToast('Please enter a username', 'error');
            return;
        }

        if (generateBtn) {
            generateBtn.disabled = true;
            generateBtn.innerHTML = '<span>⏳</span> Generating...';
        }

        try {
            const response = await fetch('/api/auth/token', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ username })
            });

            const result = await response.json();

            if (response.ok) {
                // Show result
                if (resultBox) resultBox.style.display = 'block';
                if (tokenDisplay) {
                    tokenDisplay.textContent = result.token;
                    tokenDisplay.dataset.token = result.token;
                }
                if (tokenUserDisplay) tokenUserDisplay.textContent = result.username;
                if (tokenExpiry) {
                    const expiryDate = new Date(result.expiresAt);
                    tokenExpiry.textContent = expiryDate.toLocaleDateString() + ' ' + expiryDate.toLocaleTimeString();
                }

                this.showToast('✅ Token generated successfully!', 'success');
            } else {
                this.showToast(`❌ Failed to generate token: ${result.error}`, 'error');
            }
        } catch (error) {
            console.error('Failed to generate token:', error);
            this.showToast(`❌ Failed to generate token: ${error.message}`, 'error');
        } finally {
            if (generateBtn) {
                generateBtn.disabled = false;
                generateBtn.innerHTML = '<span>🔑</span> Generate Token';
            }
        }
    }

    async checkUpdateConfiguration() {
        const checkBtn = document.getElementById('checkConfigBtn');
        const configBox = document.getElementById('configStatusBox');
        const configTitle = document.getElementById('configStatusTitle');
        const configChecksList = document.getElementById('configChecksList');
        const configMessage = document.getElementById('configMessage');
        const platformInfo = document.getElementById('platformInfo');
        const updateSystemStatus = document.getElementById('updateSystemStatus');

        if (checkBtn) {
            checkBtn.disabled = true;
            checkBtn.innerHTML = '<span>⏳</span> Checking...';
        }

        try {
            const response = await fetch('/api/update/check-config');
            const result = await response.json();

            // Update platform info
            if (platformInfo) {
                platformInfo.textContent = result.platform;
            }

            // Update status
            if (updateSystemStatus) {
                if (result.isConfigured) {
                    updateSystemStatus.innerHTML = '<span style="color: var(--success)">✅ Ready</span>';
                } else {
                    updateSystemStatus.innerHTML = '<span style="color: var(--warning)">⚠️ Not Configured</span>';
                }
            }

            // Show configuration details
            if (configBox) configBox.style.display = 'block';
            if (configTitle) {
                if (result.isConfigured) {
                    configTitle.innerHTML = '✅ Configuration Status: Ready';
                } else {
                    configTitle.innerHTML = '⚠️ Configuration Status: Incomplete';
                }
            }

            // Show checks
            if (configChecksList && result.checks) {
                configChecksList.innerHTML = result.checks.map(check => {
                    const icon = check.passed ? '✅' : '❌';
                    const color = check.passed ? 'var(--success)' : 'var(--error)';
                    return `
                        <div style="display: flex; align-items: start; gap: 0.5rem; margin-bottom: 0.5rem;">
                            <span style="color: ${color}; font-size: 1.1rem;">${icon}</span>
                            <div>
                                <strong>${this.escapeHtml(check.name)}</strong><br>
                                <span style="color: var(--text-secondary); font-size: 0.85rem;">${this.escapeHtml(check.message)}</span>
                            </div>
                        </div>
                    `;
                }).join('');
            }

            // Show message
            if (configMessage) {
                configMessage.innerHTML = `<strong>${this.escapeHtml(result.message)}</strong>`;
                if (result.repositoryPath) {
                    configMessage.innerHTML += `<br><span style="color: var(--text-secondary);">Repository: ${this.escapeHtml(result.repositoryPath)}</span>`;
                }
                if (!result.isConfigured && result.platform === 'Linux') {
                    configMessage.innerHTML += `<br><br><code style="background: rgba(0,0,0,0.5); padding: 0.5rem; border-radius: 4px; display: block; margin-top: 0.5rem;">sudo bash configure-update-system.sh</code>`;
                }
            }

            if (result.isConfigured) {
                this.showToast('✅ Update system is configured', 'success');
            } else {
                this.showToast(`⚠️ ${result.message}`, 'warning');
            }
        } catch (error) {
            console.error('Failed to check configuration:', error);
            this.showToast(`❌ Failed to check configuration: ${error.message}`, 'error');
            if (configBox) configBox.style.display = 'none';
        } finally {
            if (checkBtn) {
                checkBtn.disabled = false;
                checkBtn.innerHTML = '<span>⚙️</span> Check Configuration';
            }
        }
    }
}

// Global function for token copying (called from HTML onclick)
function copyToken() {
    const tokenDisplay = document.getElementById('tokenDisplay');
    const token = tokenDisplay?.dataset?.token || tokenDisplay?.textContent;
    
    if (token) {
        navigator.clipboard.writeText(token).then(() => {
            if (window.app) {
                window.app.showToast('📋 Token copied to clipboard!', 'success');
            }
        }).catch(err => {
            console.error('Failed to copy token:', err);
            if (window.app) {
                window.app.showToast('❌ Failed to copy token', 'error');
            }
        });
    }
}

// Initialize app
let app;
document.addEventListener('DOMContentLoaded', () => {
    app = new FocusDeckApp();
    window.app = app; // Make globally accessible for HTML onclick handlers
});





