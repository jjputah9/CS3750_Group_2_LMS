📋 Profile Feature - LMS Project
🎯 Overview
A user profile management system for the Learning Management System (LMS) that allows users to manage their personal information, contact details, and social links.

✨ Features
Personal Information: First name, last name, birth date (with age validation ≥ 16)

Address Management: Complete address fields with line 1, line 2, city, state, and zip code

Contact Information: Phone number with validation

Social Links: Up to 3 website/URL links with URL validation

Responsive Design: Clean, mobile-friendly UI built with Bootstrap 5

Form Validation: Client-side and server-side validation

Database Integration: SQL Server with Entity Framework Core

🏗️ Architecture
Model: UserProfile.cs - Data model with validation attributes

Data Layer: ProfileDbContext.cs - Entity Framework Core DbContext

Service Layer: ProfileService.cs - Business logic and data operations

Presentation: Razor Pages with clean, modern UI

📁 File Structure
text
LMS/
├── Models/
│   └── UserProfile.cs                    # Profile data model
├── Data/
│   └── ProfileDbContext.cs               # Database context
├── Services/
│   ├── IProfileService.cs                # Service interface
│   └── ProfileService.cs                 # Service implementation
├── Pages/
│   └── Profile/
│       ├── Profile.cshtml               # Profile page view
│       └── Profile.cshtml.cs            # Page model and logic
└── Program.cs                           # Service registration
🚀 Setup Instructions
1. Database Migration
powershell
# In Package Manager Console
Add-Migration CreateProfileTable -Context ProfileDbContext
Update-Database -Context ProfileDbContext
2. Database Connection
Ensure appsettings.json has:

json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LMSDB;Trusted_Connection=True"
  }
}
3. Access Profile Page
Navigate to: /Profile

Or click "Profile" in the main navigation bar

🔧 Technical Details
Database Table: UserProfiles
sql
CREATE TABLE UserProfiles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(MAX) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    BirthDate DATETIME2 NOT NULL,
    AddressLine1 NVARCHAR(100),
    AddressLine2 NVARCHAR(100),
    City NVARCHAR(50),
    State NVARCHAR(50),
    Zip NVARCHAR(10),
    Phone NVARCHAR(20),
    Link1 NVARCHAR(500),
    Link2 NVARCHAR(500),
    Link3 NVARCHAR(500),
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    LastUpdatedDate DATETIME2 DEFAULT GETDATE()
)
Validation Rules
First Name: Required, max 50 characters

Last Name: Required, max 50 characters

Birth Date: Required, must be ≥ 16 years old

Phone: Optional, must be valid phone format if provided

Links: Optional, must be valid URLs if provided

Dependencies
ASP.NET Core 6.0+

Entity Framework Core 6.0

Bootstrap 5.0

jQuery Validation

👥 Integration Points
Modified Files
Program.cs: Added ProfileDbContext and ProfileService registration

Pages/Shared/_Layout.cshtml: Added Profile link to navigation

🧪 Testing
Manual Test Cases
Form Validation

Submit empty form → Show required field errors

Enter invalid phone → Show format error

Enter invalid URL → Show URL validation error

Enter birth date < 16 years → Show age restriction error

Database Operations

Save profile → Data persists in database

Edit profile → Updates existing record

Refresh page → Loads saved data

UI/UX

Responsive design works on mobile/desktop

Clear success/error messages

Intuitive form layout

📱 User Flow
User clicks "Profile" in navigation

System loads existing profile data (if any)

User updates information in form

System validates input

On successful validation, data saves to database

Success message displays to user

🎨 Design Decisions
Why Separate DbContext?
Avoids conflicts with existing Identity DbContext

Isolates profile data from authentication data

Easier to maintain and test independently

Why Razor Pages?
Fits existing project architecture

Simple page-based routing

Built-in anti-forgery protection

Easy form handling with model binding

Validation Strategy
Client-side: Immediate feedback with jQuery Validation

Server-side: Robust validation in PageModel

Business rules: Age validation in service layer

🔄 Future Enhancements
Profile picture upload

Email notifications on profile updates

Audit logging of profile changes

Export profile data (PDF/CSV)

Integration with user dashboard

🐛 Known Issues
None currently reported

🤝 Team Collaboration Notes
This feature is self-contained and won't affect other modules

Database migrations are separate from main Identity migrations

Service is registered with Scoped lifetime for proper DI
