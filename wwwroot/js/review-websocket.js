/**
 * WebSocket client for real-time tour reviews using SignalR
 */

class ReviewWebSocket {
    constructor(tourId) {
        this.tourId = tourId;
        this.connection = null;
        this.isConnected = false;
        this.callbacks = {
            onReceiveReview: null,
            onReviewUpdated: null,
            onReviewDeleted: null,
            onError: null,
            onConnected: null,
            onDisconnected: null
        };
    }

    /**
     * Initialize SignalR connection
     */
    async connect() {
        try {
            // Create SignalR connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/review")
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        // Exponential backoff: 0s, 2s, 10s, 30s
                        if (retryContext.previousRetryCount === 0) return 0;
                        if (retryContext.previousRetryCount === 1) return 2000;
                        if (retryContext.previousRetryCount === 2) return 10000;
                        return 30000;
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            this.setupEventHandlers();

            // Start connection
            await this.connection.start();
            this.isConnected = true;
            console.log(`SignalR connected for tour: ${this.tourId}`);

            // Join tour-specific group
            await this.connection.invoke("JoinTourGroup", this.tourId);
            console.log(`Joined tour group: ${this.tourId}`);

            if (this.callbacks.onConnected) {
                this.callbacks.onConnected();
            }

        } catch (error) {
            console.error("SignalR connection error:", error);
            this.isConnected = false;
            if (this.callbacks.onError) {
                this.callbacks.onError("Failed to connect to review service");
            }
        }
    }

    /**
     * Set up SignalR event handlers
     */
    setupEventHandlers() {
        // Handle new review received
        this.connection.on("ReceiveReview", (review) => {
            console.log("New review received:", review);
            if (this.callbacks.onReceiveReview) {
                this.callbacks.onReceiveReview(review);
            }
        });

        // Handle review updated
        this.connection.on("ReviewUpdated", (review) => {
            console.log("Review updated:", review);
            if (this.callbacks.onReviewUpdated) {
                this.callbacks.onReviewUpdated(review);
            }
        });

        // Handle review deleted
        this.connection.on("ReviewDeleted", (reviewId) => {
            console.log("Review deleted:", reviewId);
            if (this.callbacks.onReviewDeleted) {
                this.callbacks.onReviewDeleted(reviewId);
            }
        });

        // Handle errors from server
        this.connection.on("ReviewError", (message) => {
            console.error("Review error:", message);
            if (this.callbacks.onError) {
                this.callbacks.onError(message);
            }
        });

        // Handle reconnection
        this.connection.onreconnecting((error) => {
            console.warn("SignalR reconnecting...", error);
            this.isConnected = false;
        });

        this.connection.onreconnected((connectionId) => {
            console.log("SignalR reconnected:", connectionId);
            this.isConnected = true;
            // Rejoin tour group after reconnection
            this.connection.invoke("JoinTourGroup", this.tourId);
        });

        // Handle disconnection
        this.connection.onclose((error) => {
            console.log("SignalR disconnected:", error);
            this.isConnected = false;
            if (this.callbacks.onDisconnected) {
                this.callbacks.onDisconnected();
            }
        });
    }

    /**
     * Send a new review via WebSocket
     */
    async sendReview(rating, comment) {
        if (!this.isConnected) {
            throw new Error("Not connected to review service");
        }

        try {
            await this.connection.invoke("SendReview", {
                tourId: this.tourId,
                rating: rating,
                comment: comment || null
            });
        } catch (error) {
            console.error("Error sending review:", error);
            throw error;
        }
    }

    /**
     * Update an existing review via WebSocket
     */
    async updateReview(reviewId, rating, comment) {
        if (!this.isConnected) {
            throw new Error("Not connected to review service");
        }

        try {
            await this.connection.invoke("UpdateReview", {
                reviewId: reviewId,
                rating: rating,
                comment: comment || null
            });
        } catch (error) {
            console.error("Error updating review:", error);
            throw error;
        }
    }

    /**
     * Delete a review via WebSocket
     */
    async deleteReview(reviewId) {
        if (!this.isConnected) {
            throw new Error("Not connected to review service");
        }

        try {
            await this.connection.invoke("DeleteReview", reviewId, this.tourId);
        } catch (error) {
            console.error("Error deleting review:", error);
            throw error;
        }
    }

    /**
     * Set callback for receiving new reviews
     */
    onReceiveReview(callback) {
        this.callbacks.onReceiveReview = callback;
        return this;
    }

    /**
     * Set callback for review updates
     */
    onReviewUpdated(callback) {
        this.callbacks.onReviewUpdated = callback;
        return this;
    }

    /**
     * Set callback for review deletions
     */
    onReviewDeleted(callback) {
        this.callbacks.onReviewDeleted = callback;
        return this;
    }

    /**
     * Set callback for errors
     */
    onError(callback) {
        this.callbacks.onError = callback;
        return this;
    }

    /**
     * Set callback for connection established
     */
    onConnected(callback) {
        this.callbacks.onConnected = callback;
        return this;
    }

    /**
     * Set callback for disconnection
     */
    onDisconnected(callback) {
        this.callbacks.onDisconnected = callback;
        return this;
    }

    /**
     * Disconnect from SignalR
     */
    async disconnect() {
        if (this.connection) {
            try {
                await this.connection.invoke("LeaveTourGroup", this.tourId);
                await this.connection.stop();
                this.isConnected = false;
                console.log("SignalR disconnected");
            } catch (error) {
                console.error("Error disconnecting:", error);
            }
        }
    }
}

/**
 * Utility functions for review UI
 */
const ReviewUI = {
    /**
     * Create HTML for a review item
     */
    createReviewHTML(review, isOwnReview = false) {
        const stars = this.createStarsHTML(review.rating);
        const date = new Date(review.createdAt).toLocaleDateString('vi-VN');
        
        const editDeleteButtons = isOwnReview ? `
            <div class="ms-auto">
                <button class="btn btn-sm btn-outline-primary me-1" onclick="editReview('${review.reviewID}', ${review.rating}, '${(review.comment || '').replace(/'/g, "\\'")}')">
                    <i class="bi bi-pencil"></i> Sửa
                </button>
                <button class="btn btn-sm btn-outline-danger" onclick="deleteReview('${review.reviewID}')">
                    <i class="bi bi-trash"></i> Xóa
                </button>
            </div>
        ` : '';

        return `
            <div class="review-item p-3 mb-3 bg-light rounded" data-review-id="${review.reviewID}">
                <div class="d-flex align-items-center mb-2">
                    <div class="avatar-circle bg-primary text-white me-3">
                        ${review.userInitial}
                    </div>
                    <div class="flex-grow-1">
                        <h6 class="mb-0">${this.escapeHtml(review.userFullName)}</h6>
                        <div class="small">
                            ${stars}
                            <span class="text-muted ms-2">${date}</span>
                        </div>
                    </div>
                    ${editDeleteButtons}
                </div>
                ${review.comment ? `<p class="text-muted mb-0 ms-5">${this.escapeHtml(review.comment)}</p>` : ''}
            </div>
        `;
    },

    /**
     * Create HTML for star rating display
     */
    createStarsHTML(rating) {
        let starsHTML = '';
        for (let i = 1; i <= 5; i++) {
            if (i <= rating) {
                starsHTML += '<i class="bi bi-star-fill text-warning"></i>';
            } else {
                starsHTML += '<i class="bi bi-star-fill text-muted opacity-25"></i>';
            }
        }
        return starsHTML;
    },

    /**
     * Escape HTML to prevent XSS
     */
    escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    /**
     * Show toast notification
     */
    showToast(message, type = 'success') {
        // Using Bootstrap toast or simple alert
        const alertClass = type === 'success' ? 'alert-success' : 'alert-danger';
        const toast = `
            <div class="alert ${alertClass} alert-dismissible fade show position-fixed top-0 end-0 m-3" role="alert" style="z-index: 9999;">
                ${this.escapeHtml(message)}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;
        document.body.insertAdjacentHTML('beforeend', toast);
        
        // Auto dismiss after 3 seconds
        setTimeout(() => {
            const alertElement = document.querySelector('.alert');
            if (alertElement) {
                alertElement.remove();
            }
        }, 3000);
    }
};
