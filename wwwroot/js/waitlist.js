// Waitlist System - Frontend Integration

// Add item to cart with waitlist support
function addToCartWithWaitlist(tripId, quantity) {
    const button = event.target;
    button.disabled = true;
    button.innerHTML = '<i class="spinner-border spinner-border-sm"></i> Adding...';

    fetch('/Booking/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            TripId: tripId,
            Quantity: quantity
        })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            if (data.onWaitlist) {
                // User was added to waitlist
                showNotification('success', data.message);
                button.innerHTML = '<i class="bi bi-bell-fill"></i> On Waitlist';
                button.classList.add('btn-warning');
            } else {
                // Added to cart normally
                showNotification('success', data.message);
                button.innerHTML = '<i class="bi bi-cart-check-fill"></i> Added to Cart';
                updateCartCount();
            }
        } else {
            // Error
            showNotification('error', data.message);
            button.disabled = false;
            button.innerHTML = '<i class="bi bi-cart-plus"></i> Add to Cart';
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('error', 'An error occurred. Please try again.');
        button.disabled = false;
        button.innerHTML = '<i class="bi bi-cart-plus"></i> Add to Cart';
    });
}

// Update trip card button based on room availability
function updateTripCardButton(tripId, availableRooms) {
    const button = document.querySelector(`[data-trip-id="${tripId}"] .add-to-cart-btn`);
    
    if (availableRooms === 0) {
        button.innerHTML = '<i class="bi bi-bell"></i> Join Waitlist';
        button.classList.remove('btn-primary');
        button.classList.add('btn-warning');
    } else {
        button.innerHTML = '<i class="bi bi-cart-plus"></i> Add to Cart';
        button.classList.remove('btn-warning');
        button.classList.add('btn-primary');
    }
}

// Admin: Process waitlist notifications for users with "Notified" status
function processWaitlistNotifications() {
    showConfirm(
        'Send Notifications',
        'Send notification emails to all users with status "Notified"?',
        function() {
            fetch('/Waitlist/ProcessNotifications', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showAlert('Success', 'Waitlist notifications processed successfully!', 'success');
                } else {
                    showAlert('Error', 'Failed to process notifications: ' + data.message, 'error');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                showAlert('Error', 'An error occurred while processing notifications.', 'error');
            });
        }
    );
}

// Example 4: Admin - Notify Next User (after cancellation or adding rooms)
function notifyNextUserInLine(tripId) {
    // This would be called after:
    // 1. A user cancels their booking
    // 2. Admin increases AvailableRooms
    
    // Backend would do: _waitlistRepo.NotifyNextUser(tripId);
    // Then call: processWaitlistNotifications();
    
    fetch('/Waitlist/NotifyNext', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ TripId: tripId })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            console.log('Next user in waitlist notified');
        }
    });
}

// Utility: Show notification
function showNotification(type, message) {
    // Bootstrap toast or alert
    const alertClass = type === 'success' ? 'alert-success' : 'alert-danger';
    const icon = type === 'success' ? 'check-circle-fill' : 'exclamation-triangle-fill';
    
    const alert = `
        <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
            <i class="bi bi-${icon}"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    document.getElementById('notifications-container').innerHTML = alert;
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        const alertElement = document.querySelector('.alert');
        if (alertElement) {
            alertElement.classList.remove('show');
            setTimeout(() => alertElement.remove(), 150);
        }
    }, 5000);
}

// Utility: Update cart count badge
function updateCartCount() {
    fetch('/User/GetCartCount')
        .then(response => response.json())
        .then(data => {
            const badge = document.getElementById('cart-count-badge');
            if (badge) {
                badge.textContent = data.count;
            }
        });
}

// ============================================
// HTML EXAMPLES
// ============================================

/*
<!-- Trip Card with Waitlist Support -->
<div class="trip-card" data-trip-id="1">
    <h3>Paris Adventure</h3>
    <p>Available Rooms: <span class="available-rooms">0</span></p>
    
    <!-- Button changes based on availability -->
    <button class="btn add-to-cart-btn" onclick="addToCartWithWaitlist(1, 1)">
        <i class="bi bi-bell"></i> Join Waitlist
    </button>
</div>

<!-- Notification Container (add to your layout) -->
<div id="notifications-container" style="position: fixed; top: 20px; right: 20px; z-index: 9999;">
</div>

<!-- Admin Panel - Process Notifications Button -->
<div class="admin-panel">
    <button class="btn btn-primary" onclick="processWaitlistNotifications()">
        <i class="bi bi-envelope"></i> Send Waitlist Notifications
    </button>
</div>
*/
