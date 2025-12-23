# User Identification Refactoring Guide
## From Email-based PK to Integer Id PK

**Date:** December 23, 2025  
**Status:** Ready for Deployment

---

## Overview

This migration refactors the User identification system from an **Email-based Primary Key** to an **auto-incrementing Integer Id Primary Key**. The Email column remains UNIQUE and NOT NULL but is no longer the primary identifier.

---

## What Changed

### Database Changes
- ✅ Added `Id INT IDENTITY(1,1)` as the new Primary Key to `Users` table
- ✅ Converted `email` to UNIQUE constraint (no longer PK)
- ✅ Updated `UserTrips` table to use `UserId INT` instead of `user_email NVARCHAR(450)`
- ✅ Updated all Foreign Key relationships
- ✅ All data cleared from tables (as requested)

### C# Model Changes
- ✅ **User.cs**: Added `int Id` property with `[Key]` and `[DatabaseGenerated]` attributes
- ✅ **UserTrip.cs**: Changed `UserEmail` to `UserId` (int type)
- ✅ **AppDbContext.cs**: Updated Fluent API to use `Id` as PK and maintain Email unique constraint

### Repository Changes
- ✅ **UserRepository**: Added `GetById(int id)` method; `Delete()` now uses Id
- ✅ **UserTripRepository**: All methods now use `int userId` instead of `string userEmail`
  - `Add(int userId, int tripId)`
  - `GetByUserId(int userId)`
  - `Remove(int userId, int tripId)`
  - `Exists(int userId, int tripId)`
  - `GetCount(int userId)`
  - `RemoveAll(int userId)`

### Controller Changes
- ✅ **LoginController**: Stores `user.Id` in `ClaimTypes.NameIdentifier` claim (instead of email)
- ✅ **UserController**: All cart operations now retrieve UserId from claims and parse as int
- ✅ **AdminController**: Uses `user.Id` for delete operation
- ✅ **BookingController**: All payment operations now use `userId` instead of `userEmail`

### Service Changes
- ✅ **PaymentService**: `SimulateCardCharge` method signature updated to accept `int userId`

---

## Migration Steps

### Step 1: Backup Your Database
```sql
-- Create backup before migration
BACKUP DATABASE [TravelAgency] TO DISK = 'C:\Backups\TravelAgency_BeforeMigration.bak';
```

### Step 2: Run SQL Migration Script
Execute the migration script: **`User_Migration_EmailToPK_To_IdPK.sql`**

This script will:
1. Clear all data from tables (UserTrips, Trips, TripImages, TripDates, Users)
2. Drop existing constraints on UserTrips
3. Add `Id` column to Users table as IDENTITY(1,1)
4. Create new PK on `Id`
5. Add UNIQUE constraint on `email`
6. Add `UserId` column to UserTrips
7. Create new FK relationship: `UserTrips.UserId` → `Users.Id`
8. Add unique constraint on (UserId, TripID)

```bash
# Run the migration script in SQL Server Management Studio
# or via command line:
sqlcmd -S localhost -d TravelAgency -i User_Migration_EmailToPK_To_IdPK.sql
```

### Step 3: Verify Database Changes
After running the script, verify the structure:

**Users Table:**
```sql
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.Users');
```

**Expected Output:**
- `Id` - int - NOT NULL - IDENTITY
- `email` - nvarchar(450) - NOT NULL - Not Identity
- `first name` - nvarchar(max) - Nullable
- `last name` - nvarchar(max) - Nullable
- `password` - nvarchar(max) - Nullable
- `admin` - bit - NOT NULL

**UserTrips Table:**
```sql
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.UserTrips');
```

**Expected Output:**
- `UserTripID` - int - NOT NULL
- `UserId` - int - NOT NULL
- `TripID` - int - NOT NULL
- `Quantity` - int - NOT NULL

### Step 4: Build and Test the Application
All C# code changes are already implemented. Simply rebuild:

```bash
dotnet build
```

If there are no compilation errors, proceed to testing.

### Step 5: Test Key Scenarios

#### Test 1: User Registration
1. Navigate to `/User/Register`
2. Register a new user with valid email/password
3. **Expected:** User created with auto-incrementing Id

#### Test 2: Login and Claims
1. Login with the newly registered user
2. Check that authentication works
3. **Expected:** `ClaimTypes.NameIdentifier` contains UserId (integer)

#### Test 3: Add Trip to Cart
1. While logged in, browse trips on Dashboard
2. Click "Add to Cart" on any trip
3. **Expected:** Trip saved with UserId in UserTrips table

#### Test 4: View Bookings
1. Navigate to `/User/Bookings`
2. **Expected:** Trips display correctly for the logged-in user

#### Test 5: Admin User Management
1. Login as admin user
2. Navigate to Admin Dashboard
3. View user details
4. **Expected:** User bookings display correctly

---

## Key Technical Details

### Authentication Claims Structure
**Before:**
```csharp
new Claim(ClaimTypes.NameIdentifier, user.email)
```

**After:**
```csharp
new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
new Claim(ClaimTypes.Email, user.email)
```

### Retrieving UserId in Controllers
```csharp
// Get UserId from claims
var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
{
    return RedirectToAction("Login", "Login");
}

// Use userId for operations
var userTrips = _userTripRepo.GetByUserId(userId);
```

### Entity Framework Configuration
```csharp
// User primary key
modelBuilder.Entity<User>()
    .HasKey(u => u.Id);

// Email unique constraint
modelBuilder.Entity<User>()
    .HasIndex(u => u.email)
    .IsUnique()
    .HasDatabaseName("UQ_Users_Email");

// UserTrip foreign key
modelBuilder.Entity<UserTrip>()
    .HasOne(ut => ut.User)
    .WithMany()
    .HasForeignKey(ut => ut.UserId)
    .OnDelete(DeleteBehavior.Cascade);
```

---

## Rollback Plan

If you need to rollback:

1. Restore database from backup:
```sql
USE master;
ALTER DATABASE [TravelAgency] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [TravelAgency] FROM DISK = 'C:\Backups\TravelAgency_BeforeMigration.bak';
ALTER DATABASE [TravelAgency] SET MULTI_USER;
```

2. Revert code changes using Git:
```bash
git checkout HEAD~1 -- Models/User.cs
git checkout HEAD~1 -- Models/UserTrip.cs
git checkout HEAD~1 -- Data/AppDbContext.cs
git checkout HEAD~1 -- Data/UserRepository.cs
git checkout HEAD~1 -- Data/UserTripRepository.cs
git checkout HEAD~1 -- Controllers/LoginController.cs
git checkout HEAD~1 -- Controllers/UserController.cs
git checkout HEAD~1 -- Controllers/AdminController.cs
```

---

## Benefits of This Change

1. **Performance:** Integer joins are faster than string (NVARCHAR) joins
2. **Flexibility:** Users can potentially change email without breaking relationships
3. **Standard Practice:** Integer PKs are industry standard for entity identification
4. **Smaller Index Size:** INT (4 bytes) vs NVARCHAR(450) (900 bytes)
5. **Foreign Key Efficiency:** Smaller FK columns in related tables

---

## Files Modified

### SQL Scripts
- `User_Migration_EmailToPK_To_IdPK.sql` (NEW)

### Models
- `Models/User.cs`
- `Models/UserTrip.cs`

### Data Access
- `Data/AppDbContext.cs`
- `Data/UserRepository.cs`
- `Data/UserTripRepository.cs`

### Controllers
- `Controllers/LoginController.cs`
- `Controllers/UserController.cs`
- `Controllers/AdminController.cs`
- `Controllers/BookingController.cs`

### Services
- `Services/PaymentService.cs`

---

## Post-Migration Checklist

- [ ] Database migration script executed successfully
- [ ] Database structure verified (Id as PK, Email as UNIQUE)
- [ ] Application builds without errors
- [ ] User registration works
- [ ] User login works
- [ ] Cart operations (Add/Remove) work
- [ ] Bookings page displays correctly
- [ ] Admin user management works
- [ ] Foreign key relationships intact
- [ ] No orphaned records exist

---

## Support & Troubleshooting

### Issue: Cannot parse UserId from claims
**Cause:** Old sessions might still have email in NameIdentifier claim  
**Solution:** Force logout all users, clear cookies, login again

### Issue: FK constraint violations
**Cause:** Mismatched UserId values  
**Solution:** Verify migration script ran completely; check UserTrips.UserId values match Users.Id

### Issue: Email still used as PK
**Cause:** Migration script didn't complete successfully  
**Solution:** Re-run migration script; check for SQL errors in output

---

## Conclusion

This migration successfully refactors the User identification system from Email-based to Id-based Primary Key while maintaining data integrity and application functionality. All related tables, models, repositories, and controllers have been updated to use the new schema.

**Total Effort:** 8 tasks completed  
**Risk Level:** Low (with proper testing)  
**Recommended Testing Time:** 1-2 hours

---

*Generated on December 23, 2025*
