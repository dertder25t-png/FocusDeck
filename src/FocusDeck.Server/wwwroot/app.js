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
        this.loadFromAPI();
        
        // Set today's date as default for task form
        const today = new Date().toISOString().split('T')[0];
        document.getElementById('taskDueDate').value = today;
        
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
        // Create Automation Button
        const createBtn = document.getElementById('createAutomationBtn');
        if (createBtn) {
            createBtn.addEventListener('click', () => this.openAutomationModal());
        }

        // Connect Service Button
        const connectBtn = document.getElementById('connectServiceBtn');
        if (connectBtn) {
            connectBtn.addEventListener('click', () => this.openConnectServiceModal());
        }

        // Modal buttons
        document.getElementById('closeAutomationModal')?.addEventListener('click', () => this.closeAutomationModal());
        document.getElementById('cancelAutomationBtn')?.addEventListener('click', () => this.closeAutomationModal());
        document.getElementById('saveAutomationBtn')?.addEventListener('click', () => this.saveAutomation());
        document.getElementById('addActionBtn')?.addEventListener('click', () => this.addActionField());

        document.getElementById('closeConnectServiceModal')?.addEventListener('click', () => this.closeConnectServiceModal());

        // History modal buttons
        document.getElementById('closeHistoryModal')?.addEventListener('click', () => this.closeAutomationHistoryModal());
        document.getElementById('closeHistoryModalBtn')?.addEventListener('click', () => this.closeAutomationHistoryModal());

        // Service cards
        document.querySelectorAll('.service-card').forEach(card => {
            card.addEventListener('click', (e) => {
                const service = e.currentTarget.dataset.service;
                this.connectService(service);
            });
        });

        // Trigger service change
        const triggerService = document.getElementById('triggerService');
        if (triggerService) {
            triggerService.addEventListener('change', () => this.updateTriggerTypes());
        }

        this.loadAutomations();
        this.loadConnectedServices();
        this.loadAutomationStats();
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

        if (this.connectedServices.length === 0) {
            container.innerHTML = `
                <div class="empty-state-small">
                    <p>No services connected yet. Click "Connect New Service" to get started.</p>
                </div>
            `;
            return;
        }

        container.innerHTML = this.connectedServices.map(service => `
            <div class="service-badge">
                <span class="service-icon">${this.getServiceIcon(service.service)}</span>
                <span class="service-name">${service.service}</span>
                <button class="btn-icon-small" onclick="app.disconnectService('${service.id}')" title="Disconnect">
                    ❌
                </button>
            </div>
        `).join('');
    }

    getServiceIcon(service) {
        const icons = {
            'FocusDeck': '🎯',
            'GoogleCalendar': '📅',
            'Canvas': '🎓',
            'HomeAssistant': '🏠',
            'Spotify': '🎵',
            'GoogleDrive': '📁'
        };
        return icons[service] || '🔗';
    }

    openAutomationModal(automationId = null) {
        const modal = document.getElementById('automationModal');
        modal.classList.add('active');

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
        document.getElementById('connectServiceModal').classList.add('active');
    }

    closeConnectServiceModal() {
        document.getElementById('connectServiceModal').classList.remove('active');
    }

    updateTriggerTypes() {
        const service = document.getElementById('triggerService').value;
        const typeSelect = document.getElementById('triggerType');

        const triggersByService = {
            'FocusDeck': [
                { value: 'time.specific', label: 'At Specific Time' },
                { value: 'session.started', label: 'Session Started' },
                { value: 'session.completed', label: 'Session Completed' },
                { value: 'task.created', label: 'Task Created' },
                { value: 'task.completed', label: 'Task Completed' },
                { value: 'task.due_approaching', label: 'Task Due Soon' }
            ],
            'GoogleCalendar': [
                { value: 'google_calendar.event_start', label: 'Event Starts' },
                { value: 'google_calendar.event_end', label: 'Event Ends' },
                { value: 'google_calendar.event_created', label: 'New Event Created' }
            ],
            'Canvas': [
                { value: 'canvas.assignment_due', label: 'Assignment Due' },
                { value: 'canvas.new_grade', label: 'New Grade Posted' },
                { value: 'canvas.new_announcement', label: 'New Announcement' }
            ],
            'HomeAssistant': [
                { value: 'home_assistant.webhook', label: 'Webhook Received' }
            ],
            'Spotify': [
                { value: 'spotify.playback_started', label: 'Playback Started' }
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
                    <option value="timer.start">Start Timer</option>
                    <option value="task.create">Create Task</option>
                    <option value="notification.show">Show Notification</option>
                    <option value="lights.set_scene">Set Lighting Scene</option>
                    <option value="spotify.play_playlist">Play Spotify Playlist</option>
                    <option value="home_assistant.turn_on">Turn On Device</option>
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
                const result = await response.json();
                this.showToast(`Failed to connect: ${result.message}`, 'error');
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

        // Update buttons
        const checkUpdatesBtn = document.getElementById('checkUpdatesBtn');
        if (checkUpdatesBtn) {
            checkUpdatesBtn.addEventListener('click', () => this.checkForUpdates());
            // Auto-check for updates on page load
            setTimeout(() => this.checkForUpdates(), 500);
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

        document.getElementById('updateServerBtn').addEventListener('click', () => this.updateServer());

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

    checkForUpdates() {
        this.showToast('Checking for updates...', 'info');
        
        // Check GitHub for latest version
        fetch('https://api.github.com/repos/dertder25t-png/FocusDeck/commits/master')
            .then(response => response.json())
            .then(data => {
                const lastCommit = data.sha.substring(0, 7);
                const commitDate = new Date(data.commit.author.date);
                const message = data.commit.message;
                
                this.showToast(`Latest version: ${lastCommit} - ${message}`, 'info');
                this.showUpdateModal();
            })
            .catch(error => {
                console.error('Error checking updates:', error);
                this.showToast('Could not check for updates. View update guide instead.', 'info');
                this.showUpdateModal();
            });
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

    async checkForUpdates() {
        const checkButton = document.getElementById('checkUpdatesBtn');
        const updateButton = document.getElementById('updateServerBtn');
        const updateInfo = document.getElementById('updateAvailableInfo');
        const currentInfo = document.getElementById('currentVersionInfo');
        const latestInfo = document.getElementById('latestVersionInfo');

        if (checkButton) {
            checkButton.disabled = true;
            checkButton.innerHTML = '<span>⏳</span> Checking...';
        }

        try {
            const response = await fetch('/api/server/check-updates');
            const result = await response.json();

            if (response.ok) {
                // Update current version info (always show something)
                if (currentInfo) {
                    if (result.currentCommit && result.currentCommit !== 'unknown') {
                        const date = result.currentDate && result.currentDate !== 'unknown' 
                            ? new Date(result.currentDate).toLocaleDateString() 
                            : 'Unknown date';
                        currentInfo.textContent = `${result.currentCommit} - ${date}`;
                    } else {
                        currentInfo.textContent = 'Version tracking not available (not a git repository or not on Linux)';
                    }
                }

                if (result.updateAvailable) {
                    // Show update available
                    if (updateInfo) updateInfo.style.display = 'block';
                    if (latestInfo) {
                        const latestDate = new Date(result.latestDate).toLocaleDateString();
                        latestInfo.innerHTML = `<strong>${result.latestCommit}</strong> - ${latestDate}<br/>${this.escapeHtml(result.latestMessage)}`;
                    }
                    if (updateButton) updateButton.style.display = 'inline-flex';
                    this.showToast('✨ Updates available!', 'success');
                } else {
                    // No updates
                    if (updateInfo) updateInfo.style.display = 'none';
                    if (updateButton) updateButton.style.display = 'none';
                    if (result.currentCommit !== 'unknown') {
                        this.showToast('✅ You\'re up to date!', 'success');
                    }
                }
            } else {
                // Handle error response
                if (currentInfo) {
                    currentInfo.textContent = result.message || 'Unable to check version';
                }
                // Don't show error toast on first load, only if user manually checks
                if (checkButton && checkButton.innerHTML.includes('Checking')) {
                    this.showToast(`ℹ️ ${result.message}`, 'info');
                }
            }
        } catch (error) {
            // Handle network error
            if (currentInfo) {
                currentInfo.textContent = 'Unable to check version (server may be starting)';
            }
            console.error('Failed to check for updates:', error);
            // Don't show error toast on auto-check
            if (checkButton && checkButton.innerHTML.includes('Checking')) {
                this.showToast(`❌ Failed to check for updates: ${error.message}`, 'error');
            }
        } finally {
            if (checkButton) {
                checkButton.disabled = false;
                checkButton.innerHTML = '<span>🔍</span> Check for Updates';
            }
        }
    }

    async updateServer() {
        if (!confirm('⚠️ This will update and restart the server.\n\nThe server will be unavailable for 30-60 seconds.\n\nContinue?')) {
            return;
        }

        const updateButton = document.getElementById('updateServerBtn');
        const checkButton = document.getElementById('checkUpdatesBtn');
        const statusBox = document.getElementById('updateStatusBox');
        const statusTitle = document.getElementById('updateStatusTitle');
        const statusMessage = document.getElementById('updateStatusMessage');

        // Disable buttons and show status
        if (updateButton) {
            updateButton.disabled = true;
            updateButton.innerHTML = '<span>⏳</span> Updating...';
        }
        if (checkButton) checkButton.disabled = true;
        if (statusBox) statusBox.style.display = 'block';

        try {
            this.showToast('🚀 Starting server update... This will take 30-60 seconds.', 'info');
            
            const response = await fetch('/api/server/update', {
                method: 'POST',
            });

            const result = await response.json();

            if (response.ok) {
                this.showToast(`✅ ${result.message}`, 'success');
                
                if (statusTitle) statusTitle.textContent = 'Server is updating...';
                if (statusMessage) statusMessage.textContent = 'Rebuilding and restarting. The page will reload automatically when complete.';
                
                // Start checking for server to come back online
                let attempts = 0;
                const maxAttempts = 60; // 60 seconds
                
                const checkServerInterval = setInterval(async () => {
                    attempts++;
                    
                    if (statusMessage) {
                        statusMessage.textContent = `Waiting for server to restart... (${attempts}/${maxAttempts}s)`;
                    }
                    
                    try {
                        const healthCheck = await fetch('/api/server/health', { 
                            method: 'GET',
                            cache: 'no-cache'
                        });
                        
                        if (healthCheck.ok) {
                            clearInterval(checkServerInterval);
                            
                            if (statusTitle) statusTitle.textContent = 'Update complete!';
                            if (statusMessage) statusMessage.textContent = 'Server is back online. Reloading page in 2 seconds...';
                            
                            this.showToast('✅ Update complete! Reloading page...', 'success');
                            
                            // Store update time
                            localStorage.setItem('focusdeck-last-update', new Date().toISOString());
                            
                            // Reload page
                            setTimeout(() => {
                                window.location.reload();
                            }, 2000);
                        }
                    } catch (err) {
                        // Server still down, keep waiting
                    }
                    
                    if (attempts >= maxAttempts) {
                        clearInterval(checkServerInterval);
                        
                        if (statusTitle) statusTitle.textContent = 'Update may be complete';
                        if (statusMessage) statusMessage.textContent = 'Please manually refresh the page (F5 or Ctrl+R)';
                        if (updateButton) {
                            updateButton.disabled = false;
                            updateButton.innerHTML = '<span>🔄</span> Refresh Page';
                            updateButton.onclick = () => window.location.reload();
                        }
                        if (checkButton) checkButton.disabled = false;
                        
                        this.showToast('⚠️ Server may be updated. Please refresh manually.', 'warning');
                    }
                }, 1000);

            } else {
                this.showToast(`❌ Update failed: ${result.message}`, 'error');
                if (updateButton) {
                    updateButton.disabled = false;
                    updateButton.innerHTML = '<span>�</span> Update Server Now';
                }
                if (checkButton) checkButton.disabled = false;
                if (statusBox) statusBox.style.display = 'none';
            }
        } catch (error) {
            this.showToast(`❌ Update request failed: ${error.message}`, 'error');
            if (updateButton) {
                updateButton.disabled = false;
                updateButton.innerHTML = '<span>�</span> Update Server Now';
            }
            if (checkButton) checkButton.disabled = false;
            if (statusBox) statusBox.style.display = 'none';
        }
    }

    async checkUpdateStatus() {
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
}

// Initialize app
let app;
document.addEventListener('DOMContentLoaded', () => {
    app = new FocusDeckApp();
});
