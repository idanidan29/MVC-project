# ✅ User Refactoring Completion Checklist

**Migration:** Email PK → Integer Id PK  
**Date:** December 23, 2025  
**Status:** ✅ All Code Changes Complete

---

## 📋 Pre-Migration Checklist

- [ ] **Backup Database**
  ```sql
  BACKUP DATABASE [TravelAgency] TO DISK = 'C:\Backups\TravelAgency_BeforeMigration.bak';
  ```

- [ ] **Backup Code** (Git commit)
  ```bash
  git add .
  git commit -m "Pre-migration checkpoint before User Id refactor"
  ```

- [ ] **Review Migration Script**
  - Open: `User_Migration_EmailToPK_To_IdPK.sql`
  - Verify database name matches your setup
  - Review all steps

---

## 🗄️ Database Migration Steps

### Step 1: Execute SQL Script
- [ ] Open SQL Server Management Studio
- [ ] Connect to your database server
- [ ] Open `User_Migration_EmailToPK_To_IdPK.sql`
- [ ] Execute the script
- [ ] Review output for any errors

### Step 2: Verify Database Structure
- [ ] **Users Table:**
  ```sql
  SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users';
  ```
  - Verify `Id INT IDENTITY(1,1)` exists
  - Verify `Id` is PRIMARY KEY
  - Verify `email` has UNIQUE constraint

- [ ] **UserTrips Table:**
  ```sql
  SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserTrips';
  ```
  - Verify `UserId INT` column exists
  - Verify `user_email` column is REMOVED

- [ ] **Foreign Keys:**
  ```sql
  SELECT 
      fk.name AS ForeignKeyName,
      OBJECT_NAME(fk.parent_object_id) AS TableName,
      COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName
  FROM sys.foreign_keys fk
  JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
  WHERE fk.parent_object_id = OBJECT_ID('dbo.UserTrips');
  ```
  - Verify `FK_UserTrips_Users_UserId` exists
  - Verify old `FK_UserTrips_Users_user_email` is removed

---

## 💻 Application Build & Test

### Step 3: Build Application
- [ ] Open terminal in project directory
- [ ] Run build command:
  ```bash
  dotnet build
  ```
- [ ] Verify **0 Errors**
- [ ] Check for any warnings

### Step 4: Review Code Changes
All changes are already implemented! Just verify these files:

#### Models ✅
- [ ] `Models/User.cs` - Has `int Id` property
- [ ] `Models/UserTrip.cs` - Has `int UserId` property

#### Data Layer ✅
- [ ] `Data/AppDbContext.cs` - Fluent API updated
- [ ] `Data/UserRepository.cs` - Has `GetById(int)`, `Delete(int)`
- [ ] `Data/UserTripRepository.cs` - Methods use `int userId`

#### Controllers ✅
- [ ] `Controllers/LoginController.cs` - Stores `user.Id` in claims
- [ ] `Controllers/UserController.cs` - Cart operations use `userId`
- [ ] `Controllers/AdminController.cs` - User management uses `userId`
- [ ] `Controllers/BookingController.cs` - Payment operations use `userId`

#### Services ✅
- [ ] `Services/PaymentService.cs` - `SimulateCardCharge(int userId, ...)`

---

## 🧪 Functional Testing

### Test Suite 1: Authentication
- [ ] **Register New User**
  1. Navigate to `/User/Register`
  2. Fill form: email, first name, last name, password
  3. Submit form
  4. Expected: User created successfully, redirected to login
  5. Verify in DB:
     ```sql
     SELECT Id, email, [first name], [last name] FROM Users ORDER BY Id DESC;
     ```

- [ ] **Login**
  1. Navigate to `/Login`
  2. Enter registered email and password
  3. Click Login
  4. Expected: Redirected to Dashboard
  5. Verify claims in browser dev tools (Application > Cookies)

- [ ] **Logout**
  1. Click Logout
  2. Expected: Redirected to Login page
  3. Try accessing protected route
  4. Expected: Redirected to Login

### Test Suite 2: Cart Operations (User Role)
- [ ] **Add Trip to Cart**
  1. Login as regular user
  2. Browse trips on Dashboard
  3. Click "Add to Cart" on any trip
  4. Expected: Success message displayed
  5. Verify in DB:
     ```sql
     SELECT ut.UserTripID, ut.UserId, ut.TripID, ut.Quantity, u.email
     FROM UserTrips ut
     JOIN Users u ON ut.UserId = u.Id;
     ```

- [ ] **View Bookings**
  1. Navigate to `/User/Bookings`
  2. Expected: Trips display correctly with images
  3. Verify trip details match database

- [ ] **Remove from Cart**
  1. On Bookings page, click Remove button
  2. Expected: Trip removed, success message shown
  3. Verify in DB:
     ```sql
     SELECT COUNT(*) FROM UserTrips WHERE UserId = <your_user_id>;
     ```

- [ ] **Add Multiple Quantities**
  1. Add same trip multiple times
  2. Expected: Quantity increments (not duplicate entries)

### Test Suite 3: Admin Operations
- [ ] **View All Users**
  1. Login as admin user
  2. Navigate to Admin Dashboard
  3. Expected: All users display in list

- [ ] **View User Details**
  1. Click on a user
  2. Expected: User details and bookings display
  3. Verify bookings match database

- [ ] **Edit User**
  1. Click Edit on a user
  2. Change first name or last name
  3. Save changes
  4. Expected: User updated successfully
  5. Verify in DB:
     ```sql
     SELECT * FROM Users WHERE Id = <user_id>;
     ```

- [ ] **Delete User**
  1. Click Delete on a user (not yourself!)
  2. Confirm deletion
  3. Expected: User removed from list
  4. Verify in DB:
     ```sql
     SELECT * FROM Users WHERE Id = <user_id>;
     -- Should return no rows
     ```
  5. Verify CASCADE DELETE:
     ```sql
     SELECT * FROM UserTrips WHERE UserId = <user_id>;
     -- Should return no rows
     ```

### Test Suite 4: Payment Operations
- [ ] **Buy Now (Single Trip)**
  1. Click "Buy Now" on a trip
  2. Enter payment details
  3. Submit payment
  4. Expected: Payment success message
  5. Trip removed from cart

- [ ] **Checkout Cart**
  1. Add multiple trips to cart
  2. Navigate to checkout
  3. Complete payment
  4. Expected: All trips processed
  5. Cart cleared after successful payment

- [ ] **PayPal Simulation**
  1. Use PayPal payment option
  2. Expected: Simulated PayPal order created
  3. Payment captured successfully

---

## 🔍 Data Integrity Checks

### Check 1: User Primary Keys
```sql
-- All users should have unique, auto-incrementing Ids
SELECT Id, email FROM Users ORDER BY Id;
```
- [ ] Ids are sequential integers
- [ ] No NULL Ids
- [ ] Emails are unique

### Check 2: Foreign Key Relationships
```sql
-- All UserTrips should reference valid Users
SELECT 
    ut.UserTripID,
    ut.UserId,
    u.email,
    ut.TripID
FROM UserTrips ut
LEFT JOIN Users u ON ut.UserId = u.Id
WHERE u.Id IS NULL;
```
- [ ] Result should be empty (no orphaned records)

### Check 3: Unique Constraints
```sql
-- No duplicate (UserId, TripID) combinations
SELECT UserId, TripID, COUNT(*)
FROM UserTrips
GROUP BY UserId, TripID
HAVING COUNT(*) > 1;
```
- [ ] Result should be empty

---

## 📊 Performance Validation

### Index Usage
```sql
-- Check indexes on Users table
EXEC sp_helpindex 'Users';
```
- [ ] PK index on `Id`
- [ ] Unique index on `email`

```sql
-- Check indexes on UserTrips table
EXEC sp_helpindex 'UserTrips';
```
- [ ] PK index on `UserTripID`
- [ ] Unique index on `(UserId, TripID)`
- [ ] FK index on `UserId`

---

## 🐛 Troubleshooting

### Issue: "Cannot parse UserId from claims"
**Symptom:** User can't add to cart after login  
**Cause:** Old session cookies still have email in NameIdentifier  
**Fix:**
- Clear browser cookies
- Force logout all users
- Login again

### Issue: "Foreign key constraint violation"
**Symptom:** Error when adding to cart  
**Cause:** UserId doesn't match any User.Id  
**Fix:**
```sql
-- Check user exists
SELECT * FROM Users WHERE Id = <user_id>;

-- Check UserTrips references
SELECT * FROM UserTrips WHERE UserId = <user_id>;
```

### Issue: "Cannot find user by email"
**Symptom:** Login fails with "Invalid email or password"  
**Cause:** User data deleted, or email changed  
**Fix:**
- Register new user
- Or restore from backup

---

## ✅ Final Verification

- [ ] All tests passed
- [ ] No compilation errors
- [ ] No runtime exceptions
- [ ] Database schema correct
- [ ] Foreign keys working
- [ ] Authentication working
- [ ] Cart operations working
- [ ] Admin operations working
- [ ] Payment operations working

---

## 📝 Post-Migration Actions

- [ ] **Create New Git Commit**
  ```bash
  git add .
  git commit -m "Complete: User refactor from Email PK to Integer Id PK"
  ```

- [ ] **Tag Release**
  ```bash
  git tag -a v2.0-user-id-refactor -m "User identification refactored to integer Id PK"
  git push origin v2.0-user-id-refactor
  ```

- [ ] **Update Documentation**
  - [ ] API documentation (if any)
  - [ ] Database schema diagrams
  - [ ] Developer onboarding docs

- [ ] **Notify Team**
  - [ ] Email team about changes
  - [ ] Update project wiki/confluence
  - [ ] Schedule code review session

- [ ] **Monitor Production** (if deploying)
  - [ ] Check application logs
  - [ ] Monitor database performance
  - [ ] Watch for authentication errors
  - [ ] Track cart operation success rates

---

## 🎉 Success Criteria

All of the following must be TRUE:

✅ SQL migration script executed without errors  
✅ Application builds without errors  
✅ Users can register successfully  
✅ Users can login successfully  
✅ UserId stored in claims (not email)  
✅ Cart operations work correctly  
✅ Bookings page displays user's trips  
✅ Admin can manage users  
✅ Payment operations complete successfully  
✅ Foreign key relationships intact  
✅ No orphaned records in database  
✅ All tests passed  

---

## 📚 Documentation

- **Migration Guide:** `MIGRATION_GUIDE.md`
- **Summary:** `REFACTORING_SUMMARY.md`
- **SQL Script:** `User_Migration_EmailToPK_To_IdPK.sql`

---

**Migration Completed By:** _________________________  
**Date:** _________________________  
**Verification By:** _________________________  
**Sign-off:** _________________________

---

*Checklist generated on December 23, 2025*
