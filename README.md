# MVC Trip Booking Application

A comprehensive ASP.NET Core MVC web application for managing travel trips, bookings, and reservations. The platform includes user authentication, payment processing, waitlist management, and real-time notifications.

## Table of Contents
- [Project Overview](#project-overview)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Setup and Installation](#setup-and-installation)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Services and Components](#services-and-components)
- [Authentication and Authorization](#authentication-and-authorization)
- [Contributing](#contributing)

## Project Overview

This is a full-featured trip booking platform built with ASP.NET Core 8.0. The application enables users to browse trips, make bookings, manage reservations, and handle waitlists. It includes admin functionality for managing trips and users, along with automated services for reminders and waitlist expiration.

### Key Capabilities
- User registration and authentication via cookies
- Trip browsing with filtering and search
- Booking and cart management with expiration
- Payment processing
- Waitlist system with automatic expiration
- Email notifications for bookings and reminders
- Admin dashboard for trip and user management
- Real-time updates using SignalR
- Image upload support for trips (up to 100 MB)
- User feedback and rating system

## Technology Stack

### Backend
- ASP.NET Core 8.0
- Entity Framework Core 8.0.6
- SQL Server (Database)
- SignalR (Real-time communication)
- BCrypt.Net-Next 4.0.2 (Password hashing)

### Frontend
- ASP.NET Core Razor Views
- HTML5 / CSS / JavaScript
- Bootstrap (CSS Framework)
- SignalR Client

### Additional Tools
- Entity Framework Core Tools (for migrations)
- User Secrets (for sensitive configuration)

## Project Structure

### Root Level Files
- **Program.cs**: Application entry point with service registration and middleware configuration
- **MVC-project.csproj**: Project file with NuGet package dependencies
- **appsettings.json**: Configuration settings
- **appsettings.Development.json**: Development-specific settings

### Folder Organization

#### Controllers/
Handles HTTP requests and business logic orchestration:
- **AdminController.cs**: Admin-only operations for user and trip management
- **BookingController.cs**: Trip booking, checkout, and payment processing
- **DashboardController.cs**: User dashboard and trip management views
- **FilterController.cs**: Trip filtering and search functionality
- **InfoController.cs**: Public information pages and landing page
- **LoginController.cs**: User authentication and account management
- **UserController.cs**: User profile and account settings
- **WaitlistController.cs**: Waitlist operations and management

#### Data/
Database access layer using Entity Framework Core:
- **AppDbContext.cs**: DbContext for database configuration and entity mapping
- **UserRepository.cs**: User data operations
- **TripRepository.cs**: Trip data operations
- **BookingRepository.cs**: Booking data operations
- **UserTripRepository.cs**: User-trip relationship data
- **TripDateRepository.cs**: Trip date and scheduling data
- **TripImageRepository.cs**: Trip image management
- **TripRatingRepository.cs**: Trip ratings and reviews
- **WaitlistRepository.cs**: Waitlist management
- **FeedbackRepository.cs**: User feedback data operations

#### Models/
Entity models representing database tables and domain objects:
- **User.cs**: User account information
- **Trip.cs**: Trip details and information
- **TripDate.cs**: Available dates for trips
- **TripImage.cs**: Images associated with trips
- **TripRating.cs**: User ratings for trips
- **Booking.cs**: Booking records
- **UserTrip.cs**: Junction table for users and trips
- **UserFeedback.cs**: User feedback on trips
- **Waitlist.cs**: Waitlist entries
- **ErrorViewModel.cs**: Error display model

#### Services/
Business logic and external integrations:
- **EmailService.cs**: Email sending functionality for notifications
- **PasswordService.cs**: Password hashing and validation
- **PaymentService.cs**: Payment processing integration
- **WaitlistService.cs**: Waitlist management logic
- **WaitlistExpirationService.cs**: Background service for waitlist expiration
- **ReminderService.cs**: Trip reminder logic
- **ReminderHostedService.cs**: Background service for sending reminders

#### ViewModels/
Data transfer objects for view rendering:
- **AddTripViewModel.cs**: Form data for adding new trips
- **AddUserViewModel.cs**: Form data for adding new users
- **AdminDashboardViewModel.cs**: Data for admin dashboard view
- **LoginViewModel.cs**: Login form data
- **RegisterViewModel.cs**: User registration form data
- **TripDashboardViewModel.cs**: Trip management dashboard data
- **UserDetailsViewModel.cs**: User profile display data
- **UserTripViewModel.cs**: User trip information display
- **RatingSubmission.cs**: Trip rating submission data

#### Views/
Razor View templates organized by controller:
- **Admin/**: Admin panel views with partials for user and trip management
- **Dashboard/**: User dashboard views
- **Info/**: Public information pages
- **Login/**: Authentication views
- **User/**: User profile and account views
- **Shared/**: Layout files and shared components
  - **_ViewStart.cshtml**: Global view initialization
  - **_ViewImports.cshtml**: Global using statements

#### Hubs/
SignalR real-time communication:
- **TripHub.cs**: Real-time updates for trip availability and booking status

#### wwwroot/
Static files served to clients:
- **css/**: Stylesheets
- **js/**: Client-side JavaScript
- **lib/**: Third-party libraries

#### Properties/
Configuration files:
- **launchSettings.json**: Launch profiles for development and production

## Setup and Installation

### Prerequisites
- .NET 8.0 SDK or later
- SQL Server (local or cloud)
- Visual Studio 2022 or Visual Studio Code with C# extension

### Installation Steps

1. Clone the repository:
   ```
   git clone <repository-url>
   cd MVC-project
   ```

2. Restore NuGet packages:
   ```
   dotnet restore
   ```

3. Update the database connection string in appsettings.json

4. Apply migrations to create the database:
   ```
   dotnet ef database update
   ```

5. Build the project:
   ```
   dotnet build
   ```

6. Run the application:
   ```
   dotnet run
   ```

The application will be available at `https://localhost:5001` or the configured port.

## Configuration

### appsettings.json

The configuration file includes:

- **ConnectionStrings**: Database connection settings
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=MvcProjectDb;Trusted_Connection=true;"
  }
  ```

- **Logging**: Log level configuration

- **AllowedHosts**: Security setting for allowed hosts

### Environment Variables

Development environment variables can be managed using User Secrets:
```
dotnet user-secrets init
dotnet user-secrets set "EmailPassword" "your-email-password"
```

### Upload Limits

The application is configured to handle large file uploads:
- Multipart body size: 100 MB
- Kestrel max request body size: 100 MB

These can be modified in Program.cs if needed.

## API Endpoints

### Authentication
- `GET /Login` - Login page
- `POST /Login/Login` - Submit login credentials
- `GET /Login/Register` - Registration page
- `POST /Login/Register` - Submit registration
- `POST /Login/Logout` - Logout user

### Trip Browsing
- `GET /Info/Index` - Landing page with trips
- `GET /Info/TripDetails/{id}` - View trip details
- `POST /Filter/FilterTrips` - Filter trips by criteria
- `GET /Filter/Search` - Search trips

### Booking
- `GET /Booking/CheckoutInfo` - Checkout page
- `POST /Booking/ProcessPayment` - Process payment
- `GET /Booking/ConfirmBooking` - Booking confirmation
- `POST /Booking/CancelBooking` - Cancel booking

### User Dashboard
- `GET /Dashboard/Index` - User dashboard
- `GET /Dashboard/MyTrips` - User's booked trips
- `POST /Dashboard/UpdateTrip` - Update trip info

### Waitlist
- `POST /Waitlist/JoinWaitlist` - Join waitlist
- `POST /Waitlist/LeaveWaitlist` - Leave waitlist
- `GET /Waitlist/Status` - Check waitlist status

### User Management
- `GET /User/Profile` - User profile
- `POST /User/UpdateProfile` - Update profile
- `GET /User/Settings` - Account settings

### Admin
- `GET /Admin/Dashboard` - Admin dashboard
- `POST /Admin/AddTrip` - Create new trip
- `POST /Admin/EditTrip/{id}` - Edit trip
- `POST /Admin/DeleteTrip/{id}` - Delete trip
- `GET /Admin/Users` - Manage users
- `POST /Admin/AddUser` - Add user (admin)

## Services and Components

### EmailService
Handles all email communications:
- Booking confirmations
- Waitlist notifications
- Trip reminders
- Password reset emails

Configuration required in appsettings.json:
```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": 587,
  "SenderEmail": "your-email@gmail.com",
  "SenderPassword": "your-app-password"
}
```

### PaymentService
Processes trip payments:
- Validates payment information
- Integrates with payment gateway
- Records transaction details
- Handles refunds

### WaitlistService
Manages waitlist operations:
- Adds users to waitlist
- Removes users from waitlist
- Processes promotions when spots open
- Sends notifications

### WaitlistExpirationService (Background Service)
Runs periodically to:
- Expire old waitlist entries
- Clean up expired carts
- Send expiration notifications

Configured to run at specified intervals.

### ReminderService / ReminderHostedService
Sends trip reminders:
- Reminder emails before trip departure
- Configurable reminder timing
- Only sent to confirmed bookings

### PasswordService
Handles password security:
- Hashes passwords using BCrypt
- Validates password strength
- Verifies passwords during login

## Authentication and Authorization

### Authentication
The application uses ASP.NET Core Cookie Authentication:
- Users log in with email and password
- Passwords are hashed with BCrypt
- Session maintained via secure HTTP-only cookie
- Automatic redirection to login for protected pages

Configuration in Program.cs:
```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.Cookie.Name = "MvcProjectAuth";
    });
```

### Authorization
- `[Authorize]` attribute protects controllers/actions from unauthenticated users
- Admin-only features restricted with role checks
- User isolation - users can only access their own data

## Contributing

### Code Standards
- Follow C# naming conventions (PascalCase for classes, camelCase for variables)
- Use dependency injection for all services
- Keep controllers focused on routing and view selection
- Place business logic in services
- Use repositories for data access

### Database Changes
1. Create a migration: `dotnet ef migrations add MigrationName`
2. Review the generated migration
3. Update the database: `dotnet ef database update`
4. Commit migration files to version control

### Testing Recommendations
- Test authentication flows
- Validate booking calculations
- Verify email service integration
- Test waitlist promotion logic
- Validate payment processing


## Support and Troubleshooting

### Common Issues

**Database Connection Issues**
- Verify connection string in appsettings.json
- Ensure SQL Server is running
- Check database credentials
- Verify network connectivity

**Email Service Not Working**
- Check SMTP configuration
- Verify email and password in User Secrets
- Allow less secure app access if using Gmail
- Check firewall/network restrictions

**File Upload Errors**
- Ensure upload size doesn't exceed 100 MB limit
- Check disk space availability
- Verify file permissions

**SignalR Connection Issues**
- Ensure WebSocket support is enabled
- Check for reverse proxy configuration
- Verify HTTPS certificates

## License

This project is part of a private development initiative.

---

**Last Updated**: January 18, 2026
