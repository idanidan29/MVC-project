# Waitlist System Implementation Guide

## Overview
This waitlist system allows users to join a waiting list when a trip has no available rooms. When a room becomes available, users are automatically notified via email and have 24 hours to complete their booking.

## Database Schema

### Waitlist Table
```sql
[WaitlistID] INT (Primary Key)
[UserId] INT (Foreign Key to Users.Id)
[TripId] INT (Foreign Key to Trips.TripID)
[Status] NVARCHAR(20) - Values: 'Waiting', 'Notified', 'Booked', 'Expired'
[CreatedAt] DATETIME - When user joined the waitlist
[ExpiresAt] DATETIME - When the notification expires (24 hours after notification)
[EmailSentAt] DATETIME - When the notification email was sent
```

## How It Works

### 1. Adding to Cart (When No Rooms Available)

**Endpoint:** `POST /Booking/AddToCart`

**Request Body:**
```json
{
  "TripId": 1,
  "Quantity": 2
}
```

**Logic:**
1. Check if `AvailableRooms == 0` for the trip
2. Check if user is already on the waitlist
3. If not, add user to waitlist with `Status = 'Waiting'`
4. Return success message

**Response (No Rooms):**
```json
{
  "success": true,
  "onWaitlist": true,
  "message": "No rooms available for Paris. You've been added to the waitlist and will be notified when a spot opens up!"
}
```

**Response (Already on Waitlist):**
```json
{
  "success": false,
  "onWaitlist": true,
  "message": "You are already on the waitlist for Paris. We'll notify you when a spot opens up!"
}
```

**Response (Rooms Available):**
```json
{
  "success": true,
  "message": "Paris (x2) added to cart!"
}
```

### 2. Notifying Users

**When to Notify:**
- When a room becomes available (user cancels booking, admin increases AvailableRooms)
- Change the user's status from 'Waiting' to 'Notified' in the database

**Processing Notifications:**

**Endpoint:** `POST /Waitlist/ProcessNotifications` (Admin only)

This triggers the `WaitlistService.ProcessPendingNotificationsAsync()` method which:

1. Queries: `SELECT * FROM Waitlist WHERE Status = 'Notified' AND EmailSentAt IS NULL`
2. Joins with Users and Trips tables
3. For each record:
   - Sends email: "A spot has opened up for your trip to [Destination]! You have 24 hours to book."
   - Updates `EmailSentAt = DateTime.Now`
   - Updates `ExpiresAt = DateTime.Now.AddHours(24)`

### 3. Email Configuration

Add to `appsettings.json`:
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "MVC Travel"
  }
}
```

**Development Mode:** If no email configuration is provided, emails are simulated (logged to console).

## Key Classes

### 1. WaitlistRepository
```csharp
// Check if user is on waitlist
bool IsUserOnWaitlist(int userId, int tripId)

// Add user to waitlist
bool AddToWaitlist(int userId, int tripId)

// Get users who need email notifications
List<Waitlist> GetPendingNotifications()

// Mark email as sent
void MarkEmailSent(int waitlistId)

// Get next waiting user (FIFO)
Waitlist? GetNextWaitingUser(int tripId)

// Notify next user when room available
void NotifyNextUser(int tripId)
```

### 2. EmailService
```csharp
// Send waitlist notification
Task<bool> SendWaitlistNotificationAsync(string email, string userName, string destination)
```

### 3. WaitlistService
```csharp
// Process all pending notifications
Task ProcessPendingNotificationsAsync()
```

## Usage Examples

### Frontend - Add to Cart Button
```javascript
function addToCart(tripId, quantity) {
    fetch('/Booking/AddToCart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ TripId: tripId, Quantity: quantity })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            if (data.onWaitlist) {
                alert('Added to waitlist! We will notify you when a spot opens up.');
            } else {
                alert('Added to cart!');
            }
        } else {
            alert(data.message);
        }
    });
}
```

### Admin - Notify Next User When Room Opens
```csharp
// After cancellation or increasing AvailableRooms
_waitlistRepo.NotifyNextUser(tripId);

// Then process notifications (can be manual or automated)
await _waitlistService.ProcessPendingNotificationsAsync();
```

## Status Flow

```
User joins waitlist
    ↓
Status = 'Waiting'
    ↓
Room becomes available
    ↓
Status = 'Notified' (admin changes this)
    ↓
Email sent automatically
    ↓
EmailSentAt = NOW, ExpiresAt = NOW + 24 hours
    ↓
User books → Status = 'Booked'
OR
24 hours pass → Status = 'Expired'
```

## Database Migration

Run the SQL script: `Create_Waitlist_Table.sql`

This will:
- Create the Waitlist table
- Add foreign keys to Users and Trips
- Create indexes for performance
- Add status constraint

## Testing

1. **Add to Waitlist:** Set a trip's `AvailableRooms = 0` and try adding to cart
2. **Check Database:** Verify record created with `Status = 'Waiting'`
3. **Notify User:** Manually set `Status = 'Notified'` in database
4. **Process Notifications:** Call `/Waitlist/ProcessNotifications` (as admin)
5. **Check Email:** Verify email was sent (check logs if in dev mode)
6. **Verify Database:** Check `EmailSentAt` and `ExpiresAt` are updated

## Notes

- Users are notified in FIFO order (first to join waitlist gets notified first)
- Each user can only be on the waitlist once per trip
- Emails include a 24-hour countdown
- System supports both real SMTP email and simulation mode for development
- Admin can manually trigger notification processing via endpoint
