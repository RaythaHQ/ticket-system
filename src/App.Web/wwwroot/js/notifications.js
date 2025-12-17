/**
 * Real-time notification system using SignalR.
 * Provides toast notifications and ticket presence tracking.
 */
(function () {
    'use strict';

    // Configuration
    const TOAST_DURATION = 5000; // 5 seconds
    const MAX_VISIBLE_TOASTS = 5;
    const TOAST_STACK_OFFSET = 10; // px between stacked toasts

    // State
    let connection = null;
    let currentTicketId = null;
    let toastContainer = null;
    let activeToasts = [];
    let pendingTicketJoin = null; // Ticket to join once connected
    let isConnected = false;

    // Notification type colors
    const TYPE_COLORS = {
        'ticket_assigned': { bg: '#0d6efd', icon: 'bi-person-check' },
        'comment_added': { bg: '#198754', icon: 'bi-chat-dots' },
        'status_changed': { bg: '#6c757d', icon: 'bi-arrow-repeat' },
        'sla_approaching': { bg: '#ffc107', icon: 'bi-clock', textDark: true },
        'sla_breach': { bg: '#dc3545', icon: 'bi-exclamation-triangle' },
        'ticket_reopened': { bg: '#fd7e14', icon: 'bi-arrow-counterclockwise' },
        'info': { bg: '#0dcaf0', icon: 'bi-info-circle', textDark: true },
        'success': { bg: '#198754', icon: 'bi-check-circle' },
        'warning': { bg: '#ffc107', icon: 'bi-exclamation-triangle', textDark: true },
        'error': { bg: '#dc3545', icon: 'bi-x-circle' }
    };

    // Notification sound
    let notificationSound = null;

    /**
     * Initialize the notification system.
     */
    async function init() {
        createToastContainer();
        initNotificationSound();
        await connectToHub();
    }

    /**
     * Create the toast container element.
     */
    function createToastContainer() {
        if (document.getElementById('notification-toast-container')) return;

        toastContainer = document.createElement('div');
        toastContainer.id = 'notification-toast-container';
        toastContainer.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            display: flex;
            flex-direction: column;
            gap: 10px;
            max-width: 380px;
            pointer-events: none;
        `;
        document.body.appendChild(toastContainer);
    }

    /**
     * Initialize the notification sound.
     */
    function initNotificationSound() {
        // Create a short, pleasant notification sound using Web Audio API
        try {
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();

            notificationSound = {
                play: function () {
                    const oscillator = audioContext.createOscillator();
                    const gainNode = audioContext.createGain();

                    oscillator.connect(gainNode);
                    gainNode.connect(audioContext.destination);

                    oscillator.frequency.setValueAtTime(880, audioContext.currentTime); // A5
                    oscillator.frequency.setValueAtTime(1108.73, audioContext.currentTime + 0.1); // C#6

                    gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
                    gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.3);

                    oscillator.start(audioContext.currentTime);
                    oscillator.stop(audioContext.currentTime + 0.3);
                }
            };
        } catch (e) {
            console.log('Web Audio API not supported, sound disabled');
            notificationSound = { play: function () { } };
        }
    }

    /**
     * Connect to the SignalR hub.
     */
    async function connectToHub() {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR library not loaded');
            return;
        }

        try {
            connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/notifications')
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Warning)
                .build();

            // Handle incoming notifications
            connection.on('ReceiveNotification', handleNotification);

            // Handle presence updates
            connection.on('TicketViewersChanged', handleViewersChanged);

            // Connection state events
            connection.onreconnecting(() => {
                console.log('SignalR reconnecting...');
            });

            connection.onreconnected(() => {
                console.log('SignalR reconnected');
                isConnected = true;
                // Re-join ticket view if we were viewing one
                if (currentTicketId) {
                    joinTicketView(currentTicketId);
                }
            });

            connection.onclose(() => {
                console.log('SignalR disconnected');
                isConnected = false;
            });

            await connection.start();
            isConnected = true;

            // Join pending ticket view if any
            if (pendingTicketJoin !== null) {
                joinTicketView(pendingTicketJoin);
                pendingTicketJoin = null;
            }

        } catch (err) {
            console.error('SignalR connection failed:', err);
            // Retry after 5 seconds
            setTimeout(connectToHub, 5000);
        }
    }

    /**
     * Handle incoming notification.
     */
    function handleNotification(notification) {
        showToast(notification);

        if (notification.playSound) {
            notificationSound.play();
        }
    }

    /**
     * Show a toast notification.
     */
    function showToast(notification) {
        const typeConfig = TYPE_COLORS[notification.type] || TYPE_COLORS['info'];
        const textColor = typeConfig.textDark ? '#212529' : '#ffffff';

        const toast = document.createElement('div');
        toast.className = 'notification-toast';
        toast.style.cssText = `
            background: ${typeConfig.bg};
            color: ${textColor};
            padding: 16px;
            border-radius: 12px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
            cursor: ${notification.url ? 'pointer' : 'default'};
            pointer-events: auto;
            transform: translateX(120%);
            transition: transform 0.3s cubic-bezier(0.68, -0.55, 0.265, 1.55), opacity 0.3s ease;
            opacity: 0;
            min-width: 300px;
            max-width: 380px;
        `;

        toast.innerHTML = `
            <div style="display: flex; align-items: flex-start; gap: 12px;">
                <i class="bi ${typeConfig.icon}" style="font-size: 24px; flex-shrink: 0;"></i>
                <div style="flex: 1; min-width: 0;">
                    <div style="font-weight: 600; font-size: 14px; margin-bottom: 4px;">${escapeHtml(notification.title)}</div>
                    <div style="font-size: 13px; opacity: 0.9; word-wrap: break-word;">${escapeHtml(notification.message)}</div>
                </div>
                <button class="toast-close" style="
                    background: transparent;
                    border: none;
                    color: ${textColor};
                    opacity: 0.7;
                    cursor: pointer;
                    padding: 0;
                    font-size: 18px;
                    line-height: 1;
                    flex-shrink: 0;
                ">×</button>
            </div>
        `;

        // Click to navigate
        if (notification.url) {
            toast.addEventListener('click', (e) => {
                if (!e.target.classList.contains('toast-close')) {
                    window.location.href = notification.url;
                }
            });
        }

        // Close button
        toast.querySelector('.toast-close').addEventListener('click', (e) => {
            e.stopPropagation();
            dismissToast(toast);
        });

        // Add to container
        toastContainer.appendChild(toast);
        activeToasts.push(toast);

        // Limit visible toasts
        while (activeToasts.length > MAX_VISIBLE_TOASTS) {
            const oldest = activeToasts.shift();
            if (oldest.parentNode) {
                dismissToast(oldest, true);
            }
        }

        // Animate in
        requestAnimationFrame(() => {
            toast.style.transform = 'translateX(0)';
            toast.style.opacity = '1';
        });

        // Auto dismiss
        setTimeout(() => {
            if (toast.parentNode) {
                dismissToast(toast);
            }
        }, TOAST_DURATION);
    }

    /**
     * Dismiss a toast notification.
     */
    function dismissToast(toast, immediate = false) {
        const idx = activeToasts.indexOf(toast);
        if (idx > -1) {
            activeToasts.splice(idx, 1);
        }

        if (immediate) {
            toast.remove();
        } else {
            toast.style.transform = 'translateX(120%)';
            toast.style.opacity = '0';
            setTimeout(() => toast.remove(), 300);
        }
    }

    /**
     * Join a ticket view for presence tracking.
     */
    async function joinTicketView(ticketId) {
        if (!isConnected || !connection || connection.state !== signalR.HubConnectionState.Connected) {
            // Queue this for when we connect
            pendingTicketJoin = ticketId;
            return;
        }

        // Leave previous ticket view
        if (currentTicketId && currentTicketId !== ticketId) {
            await leaveTicketView(currentTicketId);
        }

        currentTicketId = ticketId;

        try {
            await connection.invoke('JoinTicketView', ticketId);
            const viewers = await connection.invoke('GetTicketViewers', ticketId);
            handleViewersChanged(viewers);
        } catch (err) {
            console.error('Error joining ticket view:', err);
        }
    }

    /**
     * Leave a ticket view.
     */
    async function leaveTicketView(ticketId) {
        if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
            return;
        }

        try {
            await connection.invoke('LeaveTicketView', ticketId || currentTicketId);
            currentTicketId = null;
        } catch (err) {
            console.error('Error leaving ticket view:', err);
        }
    }

    /**
     * Handle viewers changed event.
     */
    function handleViewersChanged(viewers) {
        const container = document.getElementById('ticket-presence-container');
        if (!container) return;

        if (!viewers || viewers.length === 0) {
            container.innerHTML = '';
            container.style.display = 'none';
            return;
        }

        container.style.display = 'block';

        // Create avatar stack
        const avatars = viewers.slice(0, 5).map((viewer, idx) => {
            const initials = getInitials(viewer.userName);
            const color = getAvatarColor(viewer.userId);
            return `
                <div class="viewer-avatar" title="${escapeHtml(viewer.userName)} is viewing" style="
                    width: 32px;
                    height: 32px;
                    border-radius: 50%;
                    background: ${color};
                    color: white;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-size: 12px;
                    font-weight: 600;
                    border: 2px solid white;
                    margin-left: ${idx > 0 ? '-8px' : '0'};
                    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    position: relative;
                    z-index: ${10 - idx};
                ">${initials}</div>
            `;
        }).join('');

        const moreCount = viewers.length > 5 ? viewers.length - 5 : 0;
        const moreIndicator = moreCount > 0 ? `
            <span style="margin-left: 8px; font-size: 13px; color: var(--staff-text-muted);">
                +${moreCount} more
            </span>
        ` : '';

        container.innerHTML = `
            <div style="display: flex; align-items: center;">
                <span style="margin-right: 8px; font-size: 13px; color: var(--staff-text-muted);">
                    <i class="bi bi-eye me-1"></i>Also viewing:
                </span>
                <div style="display: flex; align-items: center;">
                    ${avatars}
                    ${moreIndicator}
                </div>
            </div>
        `;
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
        const colors = [
            '#6366f1', '#8b5cf6', '#ec4899', '#f43f5e',
            '#f97316', '#eab308', '#22c55e', '#14b8a6',
            '#06b6d4', '#3b82f6'
        ];
        const hash = userId.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
        return colors[hash % colors.length];
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

    // Expose public API
    window.AppNotifications = {
        init: init,
        joinTicketView: joinTicketView,
        leaveTicketView: leaveTicketView,
        showToast: showToast
    };

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
