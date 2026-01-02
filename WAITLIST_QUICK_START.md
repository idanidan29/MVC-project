# Waitlist System - Quick Reference

## ✅ What's Been Implemented

### Backend Files Created/Modified:
- ✅ `Models/Waitlist.cs` - Database model
- ✅ `Data/WaitlistRepository.cs` - Data access layer
- ✅ `Services/EmailService.cs` - Email sending with HTML templates
- ✅ `Services/WaitlistService.cs` - Business logic for notifications
- ✅ `Controllers/BookingController.cs` - AddToCart endpoint with waitlist logic
- ✅ `Controllers/WaitlistController.cs` - Admin notification processing
- ✅ `Data/AppDbContext.cs` - Added Waitlist DbSet
- ✅ `Program.cs` - Registered all services

### Documentation:
- ✅ `WAITLIST_SYSTEM_GUIDE.md` - Comprehensive guide
- ✅ `Create_Waitlist_Table.sql` - Database migration
- ✅ `wwwroot/js/waitlist.js` - Frontend examples

## 🚀 How to Use

### 1. Setup Database
```sql
-- Run this SQL script:
Create_Waitlist_Table.sql
```

### 2. Configure Email (Optional - works without it)
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

### 3. Frontend - Add to Cart Button
```javascript
// Use this in your trip cards
<button onclick="addToCartWithWaitlist(@trip.TripID, 1)">
    Add to Cart
</button>

// JavaScript function from waitlist.js
function addToCartWithWaitlist(tripId, quantity) {
    fetch('/Booking/AddToCart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ TripId: tripId, Quantity: quantity })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            if (data.onWaitlist) {
                alert('Added to waitlist! We will notify you.');
            } else {
                alert('Added to cart!');
            }
        } else {
            alert(data.message);
        }
    });
}
```

## 📋 Testing Checklist

### Test 1: Add to Waitlist
1. ✅ Set a trip's `AvailableRooms = 0` in database
2. ✅ Try adding to cart via frontend
3. ✅ Check database: Should see record in Waitlist with Status='Waiting'
4. ✅ Try adding again: Should get "already on waitlist" message

### Test 2: Notify User
1. ✅ Manually set Status='Notified' in Waitlist table for test user
2. ✅ Call `/Waitlist/ProcessNotifications` (must be logged in as admin)
3. ✅ Check console logs: Should see "[SIMULATED EMAIL]" message
4. ✅ Check database: EmailSentAt and ExpiresAt should be updated

### Test 3: Normal Cart (Rooms Available)
1. ✅ Set trip's `AvailableRooms > 0`
2. ✅ Add to cart
3. ✅ Should add normally (no waitlist)

## 🔑 Key Endpoints

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/Booking/AddToCart` | POST | User | Add to cart or waitlist |
| `/Waitlist/ProcessNotifications` | POST | Admin | Send emails to notified users |

## 📊 Database Operations

### Check Waitlist
```sql
SELECT u.email, u.[first name], t.Destination, w.Status, w.CreatedAt, w.EmailSentAt
FROM Waitlist w
JOIN Users u ON w.UserId = u.Id
JOIN Trips t ON w.TripId = t.TripID
ORDER BY w.CreatedAt;
```

### Manually Notify User (for testing)
```sql
UPDATE Waitlist
SET Status = 'Notified'
WHERE WaitlistID = 1;
```

### Clear Waitlist (for testing)
```sql
DELETE FROM Waitlist;
```

## 🎯 Workflow

```
1. User clicks "Add to Cart"
   ↓
2. System checks: AvailableRooms == 0?
   ↓
   YES: Add to Waitlist (Status='Waiting')
   NO: Add to Cart normally
   ↓
3. When room opens up:
   Admin manually sets Status='Notified' for next user
   ↓
4. Admin clicks "Process Notifications" or calls endpoint
   ↓
5. System sends email to all Status='Notified' users
   ↓
6. EmailSentAt = NOW, ExpiresAt = NOW + 24 hours
   ↓
7. User receives email with 24-hour deadline
```

## 🐛 Troubleshooting

### Email not sending?
- ✅ Check console logs for "[SIMULATED EMAIL]" message
- ✅ System works in simulation mode without SMTP config
- ✅ Real emails require Gmail App Password (not regular password)

### User not added to waitlist?
- ✅ Check: Is Status already 'Waiting' for this user+trip?
- ✅ Check: Foreign key constraints (UserId and TripId must exist)

### Compilation errors?
- ✅ Run: `dotnet build` to check
- ✅ All files should compile without errors

## 📝 Next Steps (Optional Enhancements)

- [ ] Auto-expire notifications after 24 hours
- [ ] User dashboard to view their waitlist status
- [ ] Admin panel to manage waitlist
- [ ] Background service to auto-process notifications
- [ ] SMS notifications (via Twilio)
- [ ] Push notifications

## ✨ Summary

Your waitlist system is **ready to use**! 

- ✅ Users automatically join waitlist when no rooms
- ✅ Email notifications with beautiful HTML template
- ✅ 24-hour booking window after notification
- ✅ FIFO queue (first come, first served)
- ✅ Works in dev mode without email configuration
- ✅ Complete documentation and examples

Just run the SQL migration and start testing! 🎉
