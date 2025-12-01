/**
 * TourSignalR - Shared SignalR client for real-time tour updates
 * Manages WebSocket connection to TourHub and provides event subscription API
 */

(function (window) {
    'use strict';

    let connection = null;
    let currentPageType = null;
    let reconnectAttempts = 0;
    const MAX_RECONNECT_ATTEMPTS = 5;
    const RECONNECT_DELAYS = [1000, 2000, 5000, 10000, 30000]; // Exponential backoff

    const eventHandlers = {
        tourPublished: [],
        tourUpdated: [],
        tourDeleted: []
    };

    /**
     * Initialize and connect to TourHub
     * @param {string} pageType - Type of page: "all", "trending", "domestic", or "international"
     */
    async function connect(pageType) {
        // Check if SignalR is available
        if (typeof signalR === 'undefined') {
            console.error('[TourSignalR] SignalR library not loaded. Please ensure it is included before tour-signalr.js');
            return;
        }

        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            console.log('[TourSignalR] Already connected');
            return;
        }

        currentPageType = pageType;

        try {
            // Build connection
            connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/tour')
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: () => {
                        if (reconnectAttempts < RECONNECT_DELAYS.length) {
                            return RECONNECT_DELAYS[reconnectAttempts++];
                        }
                        return null; // Stop reconnecting
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Setup event handlers
            connection.on('TourPublished', (tour) => {
                console.log('[TourSignalR] TourPublished:', tour);
                eventHandlers.tourPublished.forEach(handler => handler(tour));
            });

            connection.on('TourUpdated', (tour) => {
                console.log('[TourSignalR] TourUpdated:', tour);
                eventHandlers.tourUpdated.forEach(handler => handler(tour));
            });

            connection.on('TourDeleted', (tourId) => {
                console.log('[TourSignalR] TourDeleted:', tourId);
                eventHandlers.tourDeleted.forEach(handler => handler(tourId));
            });

            // Connection lifecycle handlers
            connection.onreconnecting((error) => {
                console.warn('[TourSignalR] Reconnecting...', error);
            });

            connection.onreconnected((connectionId) => {
                console.log('[TourSignalR] Reconnected:', connectionId);
                reconnectAttempts = 0;
                // Rejoin page group after reconnection
                if (currentPageType) {
                    connection.invoke('JoinPageGroup', currentPageType)
                        .catch(err => console.error('[TourSignalR] Error rejoining group:', err));
                }
            });

            connection.onclose((error) => {
                console.error('[TourSignalR] Connection closed:', error);
                if (reconnectAttempts < MAX_RECONNECT_ATTEMPTS) {
                    setTimeout(() => connect(currentPageType), RECONNECT_DELAYS[reconnectAttempts] || 5000);
                }
            });

            // Start connection
            await connection.start();
            console.log('[TourSignalR] Connected successfully');

            // Join page-specific group
            if (pageType) {
                await connection.invoke('JoinPageGroup', pageType);
                console.log(`[TourSignalR] Joined group: tours_${pageType}`);
            }

        } catch (error) {
            console.error('[TourSignalR] Connection error:', error);
            reconnectAttempts++;
            if (reconnectAttempts < MAX_RECONNECT_ATTEMPTS) {
                setTimeout(() => connect(currentPageType), RECONNECT_DELAYS[reconnectAttempts - 1] || 5000);
            }
        }
    }

    /**
     * Disconnect from TourHub
     */
    async function disconnect() {
        if (!connection) return;

        try {
            if (currentPageType) {
                await connection.invoke('LeavePageGroup', currentPageType);
            }
            await connection.stop();
            console.log('[TourSignalR] Disconnected');
        } catch (error) {
            console.error('[TourSignalR] Disconnect error:', error);
        }

        connection = null;
        currentPageType = null;
        reconnectAttempts = 0;
    }

    /**
     * Subscribe to TourPublished event
     * @param {function} callback - Handler function(tour)
     */
    function onTourPublished(callback) {
        if (typeof callback === 'function') {
            eventHandlers.tourPublished.push(callback);
        }
    }

    /**
     * Subscribe to TourUpdated event
     * @param {function} callback - Handler function(tour)
     */
    function onTourUpdated(callback) {
        if (typeof callback === 'function') {
            eventHandlers.tourUpdated.push(callback);
        }
    }

    /**
     * Subscribe to TourDeleted event
     * @param {function} callback - Handler function(tourId)
     */
    function onTourDeleted(callback) {
        if (typeof callback === 'function') {
            eventHandlers.tourDeleted.push(callback);
        }
    }

    /**
     * Get current connection state
     */
    function getConnectionState() {
        return connection ? connection.state : 'Disconnected';
    }

    // Public API
    window.TourSignalR = {
        connect,
        disconnect,
        onTourPublished,
        onTourUpdated,
        onTourDeleted,
        getConnectionState
    };

})(window);
