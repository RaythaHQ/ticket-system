/**
 * Activity Stream - Real-time activity log using SignalR.
 * Shows live ticket activity and online users.
 */
(function () {
    'use strict';

    // Configuration
    const MAX_ACTIVITY_ITEMS = 100;
    const IDLE_TIMEOUT_MS = 5 * 60 * 1000; // 5 minutes
    const NEW_ITEM_HIGHLIGHT_MS = 3000;

    // State
    let connection = null;
    let isConnected = false;
    let isPaused = false;
    let idleTimeout = null;
    let isIdle = false;
    let activityCount = 0;

    // DOM Elements
    let streamContainer = null;
    let streamEmpty = null;
    let onlineUsersList = null;
    let usersEmpty = null;
    let onlineCount = null;
    let connectionStatus = null;
    let pauseButton = null;
    let clearButton = null;

    // Activity type configuration
    const ACTIVITY_TYPES = {
        'ticket_created': {
            icon: 'bi-plus-circle',
            class: 'ticket-created',
            label: 'created'
        },
        'ticket_updated': {
            icon: 'bi-pencil',
            class: 'ticket-updated',
            label: 'updated'
        },
        'ticket_assigned': {
            icon: 'bi-person-check',
            class: 'ticket-assigned',
            label: 'assigned'
        },
        'ticket_status_changed': {
            icon: 'bi-arrow-repeat',
            class: 'ticket-status-changed',
            label: 'status changed'
        },
        'comment_added': {
            icon: 'bi-chat-dots',
            class: 'comment-added',
            label: 'comment added'
        },
        'ticket_closed': {
            icon: 'bi-check-circle',
            class: 'ticket-closed',
            label: 'closed'
        },
        'ticket_reopened': {
            icon: 'bi-arrow-counterclockwise',
            class: 'ticket-reopened',
            label: 'reopened'
        },
        'contact_created': {
            icon: 'bi-person-plus',
            class: 'contact-created',
            label: 'contact created'
        },
        'contact_updated': {
            icon: 'bi-person-gear',
            class: 'contact-updated',
            label: 'contact updated'
        },
        'task_created': {
            icon: 'bi-plus-square',
            class: 'task-created',
            label: 'task created'
        },
        'task_completed': {
            icon: 'bi-check2-square',
            class: 'task-completed',
            label: 'task completed'
        },
        'task_reopened': {
            icon: 'bi-arrow-counterclockwise',
            class: 'task-reopened',
            label: 'task reopened'
        },
        'task_assigned': {
            icon: 'bi-person-check',
            class: 'task-assigned',
            label: 'task assigned'
        },
        'task_deleted': {
            icon: 'bi-trash',
            class: 'task-deleted',
            label: 'task deleted'
        },
        'task_due_date_changed': {
            icon: 'bi-calendar-event',
            class: 'task-updated',
            label: 'task due date changed'
        },
        'task_dependency_changed': {
            icon: 'bi-link-45deg',
            class: 'task-updated',
            label: 'task dependency changed'
        },
        'task_unblocked': {
            icon: 'bi-unlock',
            class: 'task-unblocked',
            label: 'task unblocked'
        }
    };

    // Avatar colors based on user ID
    const AVATAR_COLORS = [
        '#6366f1', '#8b5cf6', '#ec4899', '#f43f5e',
        '#f97316', '#eab308', '#22c55e', '#14b8a6',
        '#06b6d4', '#3b82f6'
    ];

    /**
     * Initialize the activity stream page.
     */
    async function init() {
        // Cache DOM elements
        streamContainer = document.getElementById('activity-stream');
        streamEmpty = document.getElementById('stream-empty');
        onlineUsersList = document.getElementById('online-users-list');
        usersEmpty = document.getElementById('users-empty');
        onlineCount = document.getElementById('online-count');
        connectionStatus = document.getElementById('connection-status');
        pauseButton = document.getElementById('btn-pause-stream');
        clearButton = document.getElementById('btn-clear-stream');

        // Setup event listeners
        if (pauseButton) {
            pauseButton.addEventListener('click', togglePause);
        }
        if (clearButton) {
            clearButton.addEventListener('click', clearStream);
        }

        // Setup idle detection
        setupIdleDetection();

        // Connect to SignalR
        await connectToHub();
    }

    /**
     * Connect to the SignalR hub.
     */
    async function connectToHub() {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR library not loaded');
            updateConnectionStatus('error', 'SignalR not available');
            return;
        }

        try {
            updateConnectionStatus('connecting', 'Connecting...');

            connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/notifications')
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Warning)
                .build();

            // Handle incoming activity events
            connection.on('ActivityReceived', handleActivityReceived);

            // Handle online users updates
            connection.on('OnlineUsersChanged', handleOnlineUsersChanged);

            // Connection state events
            connection.onreconnecting(() => {
                isConnected = false;
                updateConnectionStatus('reconnecting', 'Reconnecting...');
            });

            connection.onreconnected(async () => {
                isConnected = true;
                updateConnectionStatus('connected', 'Connected');
                // Re-join activity stream
                await connection.invoke('JoinActivityStream');
            });

            connection.onclose(() => {
                isConnected = false;
                updateConnectionStatus('disconnected', 'Disconnected');
            });

            await connection.start();
            isConnected = true;
            updateConnectionStatus('connected', 'Connected');

            // Join the activity stream group
            await connection.invoke('JoinActivityStream');

        } catch (err) {
            console.error('SignalR connection failed:', err);
            updateConnectionStatus('error', 'Connection failed');
            // Retry after 5 seconds
            setTimeout(connectToHub, 5000);
        }
    }

    /**
     * Handle incoming activity event.
     */
    function handleActivityReceived(activity) {
        if (isPaused) return;

        // Hide empty state
        if (streamEmpty) {
            streamEmpty.classList.add('hidden');
        }

        // Create activity item
        const item = createActivityItem(activity);

        // Insert at the top
        if (streamContainer) {
            streamContainer.insertBefore(item, streamEmpty?.nextSibling || streamContainer.firstChild);
        }

        // Limit the number of items
        activityCount++;
        if (activityCount > MAX_ACTIVITY_ITEMS) {
            const items = streamContainer.querySelectorAll('.activity-item');
            if (items.length > MAX_ACTIVITY_ITEMS) {
                items[items.length - 1].remove();
            }
        }

        // Remove highlight after delay
        setTimeout(() => {
            item.classList.remove('new-item');
        }, NEW_ITEM_HIGHLIGHT_MS);
    }

    /**
     * Create an activity item element.
     */
    function createActivityItem(activity) {
        const typeConfig = ACTIVITY_TYPES[activity.type] || {
            icon: 'bi-circle',
            class: 'default',
            label: 'activity'
        };

        const item = document.createElement('div');
        item.className = 'activity-item new-item';
        item.dataset.activityId = activity.id;

        // Build the message with clickable links
        let messageHtml = escapeHtml(activity.message);
        if (activity.ticketId && activity.url) {
            messageHtml = messageHtml.replace(
                new RegExp(`#${activity.ticketId}\\b`),
                `<a href="${escapeHtml(activity.url)}">#${activity.ticketId}</a>`
            );
        }

        // Build actor info
        const actorHtml = activity.actorName
            ? `<span class="activity-actor"><i class="bi bi-person"></i>${escapeHtml(activity.actorName)}</span>`
            : '';

        // Format timestamp
        const timestamp = new Date(activity.timestamp);
        const timeAgo = formatTimeAgo(timestamp);

        item.innerHTML = `
            <div class="activity-icon ${typeConfig.class}">
                <i class="bi ${typeConfig.icon}"></i>
            </div>
            <div class="activity-content">
                <div class="activity-message">${messageHtml}</div>
                ${activity.details ? `<div class="activity-details" title="${escapeHtml(activity.details)}">${escapeHtml(activity.details)}</div>` : ''}
                <div class="activity-meta">
                    ${actorHtml}
                    <span class="activity-time" title="${timestamp.toLocaleString()}">
                        <i class="bi bi-clock"></i> ${timeAgo}
                    </span>
                </div>
            </div>
        `;

        return item;
    }

    /**
     * Handle online users changed event.
     */
    function handleOnlineUsersChanged(users) {
        if (!onlineUsersList) return;

        // Update count
        if (onlineCount) {
            onlineCount.textContent = users.length;
        }

        // Show/hide empty state
        if (usersEmpty) {
            if (users.length === 0) {
                usersEmpty.classList.remove('hidden');
            } else {
                usersEmpty.classList.add('hidden');
            }
        }

        // Clear existing items (except empty state)
        const existingItems = onlineUsersList.querySelectorAll('.online-user-item');
        existingItems.forEach(item => item.remove());

        // Add user items
        users.forEach(user => {
            const item = createOnlineUserItem(user);
            onlineUsersList.appendChild(item);
        });
    }

    /**
     * Create an online user item element.
     */
    function createOnlineUserItem(user) {
        const item = document.createElement('div');
        item.className = 'online-user-item';
        item.dataset.userId = user.userId;

        const initials = getInitials(user.userName);
        const color = getAvatarColor(user.userId);
        const statusClass = user.isIdle ? 'idle' : 'online';
        const statusText = user.isIdle ? 'Idle' : 'Active';

        item.innerHTML = `
            <div class="online-user-avatar" style="background-color: ${color};">
                ${initials}
                <span class="online-status-indicator ${statusClass}"></span>
            </div>
            <div class="online-user-info">
                <div class="online-user-name">${escapeHtml(user.userName)}</div>
                <div class="online-user-status">${statusText}</div>
            </div>
        `;

        return item;
    }

    /**
     * Setup idle detection to track user activity.
     */
    function setupIdleDetection() {
        const resetIdle = () => {
            if (isIdle && isConnected) {
                isIdle = false;
                connection.invoke('ReportActivity').catch(console.error);
            }

            clearTimeout(idleTimeout);
            idleTimeout = setTimeout(() => {
                if (!isIdle && isConnected) {
                    isIdle = true;
                    connection.invoke('GoIdle').catch(console.error);
                }
            }, IDLE_TIMEOUT_MS);
        };

        // Track user activity
        document.addEventListener('mousemove', resetIdle);
        document.addEventListener('keypress', resetIdle);
        document.addEventListener('click', resetIdle);
        document.addEventListener('scroll', resetIdle);

        // Initial activity
        resetIdle();
    }

    /**
     * Toggle stream pause state.
     */
    function togglePause() {
        isPaused = !isPaused;

        if (pauseButton) {
            const icon = pauseButton.querySelector('i');
            if (isPaused) {
                icon.className = 'bi bi-play-fill';
                pauseButton.title = 'Resume stream';
                streamContainer?.classList.add('paused');
            } else {
                icon.className = 'bi bi-pause-fill';
                pauseButton.title = 'Pause stream';
                streamContainer?.classList.remove('paused');
            }
        }
    }

    /**
     * Clear the activity stream.
     */
    function clearStream() {
        if (!streamContainer) return;

        const items = streamContainer.querySelectorAll('.activity-item');
        items.forEach(item => item.remove());

        if (streamEmpty) {
            streamEmpty.classList.remove('hidden');
        }

        activityCount = 0;
    }

    /**
     * Update connection status indicator.
     */
    function updateConnectionStatus(status, text) {
        if (!connectionStatus) return;

        const colors = {
            connecting: 'text-warning',
            connected: 'text-success',
            reconnecting: 'text-warning',
            disconnected: 'text-danger',
            error: 'text-danger'
        };

        connectionStatus.innerHTML = `
            <i class="bi bi-circle-fill ${colors[status] || 'text-muted'}"></i>
            <span class="ms-1">${text}</span>
        `;
    }

    /**
     * Format a date as "time ago" string.
     */
    function formatTimeAgo(date) {
        const now = new Date();
        const diffMs = now - date;
        const diffSecs = Math.floor(diffMs / 1000);
        const diffMins = Math.floor(diffSecs / 60);
        const diffHours = Math.floor(diffMins / 60);
        const diffDays = Math.floor(diffHours / 24);

        if (diffSecs < 10) return 'just now';
        if (diffSecs < 60) return `${diffSecs}s ago`;
        if (diffMins < 60) return `${diffMins}m ago`;
        if (diffHours < 24) return `${diffHours}h ago`;
        if (diffDays < 7) return `${diffDays}d ago`;

        return date.toLocaleDateString();
    }

    /**
     * Get initials from a name.
     */
    function getInitials(name) {
        if (!name) return '?';
        const parts = name.trim().split(' ');
        if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
        return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
    }

    /**
     * Get a consistent color for a user based on their ID.
     */
    function getAvatarColor(userId) {
        if (!userId) return AVATAR_COLORS[0];
        const hash = userId.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
        return AVATAR_COLORS[hash % AVATAR_COLORS.length];
    }

    /**
     * Escape HTML to prevent XSS.
     */
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Cleanup when leaving the page.
     */
    function cleanup() {
        if (connection && isConnected) {
            connection.invoke('LeaveActivityStream').catch(console.error);
        }
        clearTimeout(idleTimeout);
    }

    // Handle page unload
    window.addEventListener('beforeunload', cleanup);

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
