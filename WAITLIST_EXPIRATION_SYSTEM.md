# Waitlist Expiration System - Implementation Summary

## Overview
Automatic expiration system that removes trips from cart after 24 hours if payment is not completed, then processes the next user in the waitlist queue.

## What Was Implemented

### 1. **WaitlistExpirationService** (Background Service)
**File:** `Services/WaitlistExpirationService.cs`

**Features:**
- Runs automatically in the background every 5 minutes
- Checks for expired waitlist entries (24 hours after notification)
- Removes expired trips from user's cart
- Restores rooms to available inventory
- Processes next users in waitlist queue
- Sends email notifications to newly notified users
- Comprehensive logging for monitoring and debugging

**Key Methods:**
- `ExecuteAsync()` - Main loop that runs every 5 minutes
- `ProcessExpiredWaitlistEntries()` - Handles expired entry cleanup
- `ProcessWaitlistForTrip()` - Auto-adds trips to waiting users' carts

### 2. **WaitlistRepository Updates**
**File:** `Data/WaitlistRepository.cs`

**New Methods:**
```csharp
// Get all entries that have expired (24 hours passed)
GetExpiredEntries()

// Mark entry as "Booked" when user completes payment
MarkAsBooked(int userId, int tripId)
```

### 3. **UserTripRepository Updates**
**File:** `Data/UserTripRepository.cs`

**New Method:**
```csharp
// Get specific cart item for expiration processing
GetByUserIdAndTripId(int userId, int tripId)
```

### 4. **BookingController Updates**
**File:** `Controllers/BookingController.cs`

**Changes:** All payment methods now call `_waitlistRepo.MarkAsBooked()` after successful payment:
- `PayCard()` - Card payment
- `PayPalSimulate()` - Single trip PayPal payment
- `PayPalCartSimulate()` - Full cart PayPal payment
- `PayPalDateSimulate()` - Date-specific PayPal payment

This prevents the expiration service from removing trips that were already paid for.

### 5. **Program.cs Updates**
**File:** `Program.cs`

**Added:**
```csharp
builder.Services.AddHostedService<WaitlistExpirationService>();
```

Registers the background service to run automatically when the application starts.

## How It Works

### Complete Flow:

1. **User Gets Notified (from waitlist)**
   - Trip added to cart automatically
   - `ExpiresAt` set to 24 hours from now
   - Email sent with payment link
   - Status changed to "Notified"

2. **Scenario A: User Pays Within 24 Hours**
   - User completes payment via any method
   - `MarkAsBooked()` called, status changed to "Booked"
   - Cart cleared
   - Expiration service ignores this entry (not in "Notified" status)

3. **Scenario B: User Doesn't Pay (24 Hours Expire)**
   - Background service detects expired entry
   - Removes trip from user's cart
   - Restores rooms: `trip.AvailableRooms += quantity`
   - Marks entry status as "Expired"
   - Gets next waiting user for that trip
   - Adds trip to their cart (1 room)
   - Sends them an email notification
   - Sets their expiration to 24 hours from now
   - Cycle continues until all rooms are booked or waitlist is empty

## Monitoring & Logging

The service logs all actions at various levels:

**Information Level:**
- Service start/stop
- Number of expired entries found
- User notifications sent
- Successful processing completion

**Warning Level:**
- Cart items not found (user may have already paid/removed)
- Trips not found during processing

**Error Level:**
- Exceptions during entry processing
- Database errors
- Email sending failures

### View Logs:
- Visual Studio: Debug → Windows → Output
- Console: When running with `dotnet run`
- Application logs: Check configured logging provider

## Configuration

### Expiration Check Interval
**Current:** Every 5 minutes  
**Location:** `WaitlistExpirationService.cs` line 14

```csharp
private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
```

To change frequency:
- **1 minute:** `TimeSpan.FromMinutes(1)` - More responsive, more database queries
- **10 minutes:** `TimeSpan.FromMinutes(10)` - Less frequent checks
- **1 hour:** `TimeSpan.FromHours(1)` - Minimal resource usage

### Expiration Duration
**Current:** 24 hours after notification  
**Location:** `WaitlistRepository.cs` line 63

```csharp
entry.ExpiresAt = DateTime.Now.AddHours(24);
```

To change duration:
- **12 hours:** `AddHours(12)`
- **48 hours:** `AddHours(48)`
- **30 minutes (testing):** `AddMinutes(30)`

## Testing Guide

### Test Scenario 1: Normal Expiration
1. Set expiration to 1 minute for testing: `AddMinutes(1)`
2. User A books all available rooms
3. User B gets added to waitlist
4. User A removes trip from cart
5. User B gets trip added to cart + email
6. Wait 1-5 minutes (service checks every 5 min)
7. Check database: User B's entry should be marked "Expired"
8. Check User B's cart: Trip should be removed
9. Check trip: Rooms should be restored

### Test Scenario 2: Successful Payment
1. User gets trip added from waitlist
2. User completes payment before 24 hours
3. Check database: Status should be "Booked"
4. Expiration service should ignore this entry

### Test Scenario 3: Chain Reaction
1. Create 3 users on waitlist for same trip
2. Release 1 room
3. First user gets notified
4. Wait for expiration
5. Second user gets notified automatically
6. Verify FIFO ordering

## Database Schema

The `Waitlist` table tracks all statuses:

| Status | Meaning |
|--------|---------|
| `Waiting` | User on waitlist, not yet notified |
| `Notified` | Trip added to cart, 24-hour countdown started |
| `Booked` | User completed payment successfully |
| `Expired` | 24 hours passed without payment, trip removed |

## Performance Considerations

**Database Queries:**
- Service runs every 5 minutes
- Only queries entries where `Status = 'Notified'` and `ExpiresAt < NOW()`
- Indexed on `Status` and `ExpiresAt` for fast lookups

**Resource Usage:**
- Minimal CPU usage (only runs every 5 minutes)
- Low memory footprint (processes entries one at a time)
- Database transactions are quick (uses indexed queries)

## Troubleshooting

**Service not running:**
- Check console logs for startup message
- Verify `AddHostedService<WaitlistExpirationService>()` in Program.cs

**Trips not being removed:**
- Check `ExpiresAt` values in database
- Verify status is "Notified" not "Booked"
- Check logs for error messages

**Next user not being notified:**
- Verify there are users with status "Waiting"
- Check SMTP configuration for email sending
- Review logs for processing errors

**Rooms not being restored:**
- Check `ProcessExpiredWaitlistEntries()` logs
- Verify trip exists in database
- Check `AvailableRooms` value before/after

## Code Quality

✅ **Comprehensive error handling** - Try-catch blocks around all operations  
✅ **Detailed logging** - All actions logged with context  
✅ **Database transaction safety** - Each entry processed independently  
✅ **Null reference checks** - Safe navigation for all object access  
✅ **SOLID principles** - Single responsibility, dependency injection  
✅ **Clean code** - Well-commented, descriptive variable names  
✅ **Scalable design** - Handles multiple users and trips efficiently  

## Future Enhancements (Optional)

1. **Email reminders** - Send reminder at 23 hours, 1 hour before expiration
2. **Configurable expiration** - Admin can set per-trip expiration times
3. **Priority waitlist** - VIP users get priority in queue
4. **Analytics dashboard** - Track expiration rates, conversion metrics
5. **User notifications** - Push notifications via SignalR when spot opens
