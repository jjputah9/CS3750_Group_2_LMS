```text
~/  
├─ .git/  
├─ .github/  
├─ .gitignore  
├─ README.md  
└─ LMS/  
   ├─ .vs/                              # Visual Studio workspace files (ignore)  
   ├─ LMS/                              # Actual project folder  
   │   ├─ Properties/                   # Project properties and settings
   │   ├─ wwwroot/                      # Static files (CSS, JS, images)
   │   ├─ Areas/                        # Identity pages (login/register)
   │   ├─ Data/                         # Database context & EF Core setup
   │   ├─ Pages/                        # Razor Pages (UI)
   │   ├─ appsettings.Development.json  # Local dev configuration
   │   └─ Program.cs                    # Application startup
   └─ LMS.slnx                          # Visual Studio solution user data (ignore)


Inside the /Areas/ folder you will find /Areas/Pages/Account/...
Inside this /Account/ folder are the pages for the Login and Register
All other pages are found inside /Pages/


To create a local database, after cloning the repo follow these steps:
Open Package Manager Console (Tools - NuGet - Package Manager Console)
Run:
Add-Migration InitialIdentity
Update-Database

Then your (local) database will be created. Note that as of this branch, all tables are default. To view the tables, click
(View - SQL Server Object Explorer) or (Ctrl+\, Ctrl+S)
You should see a database called aspnet-LMS-a5f........
Inside /Tables/ are a bunch of auto generated tables.
dbo.AspNetUsers is the important one
If you double click it, it will open a design page listing all the fields
Note that fields such as birthdate are not included.

dbo.AspNetUserRoles acts as a bridge between
dbo.AspNetUsers and dbo.AspNetRoles
The latter will be the table where user roles are stored (student/instructor)

The Register page (inside /Areas/) will need to be edited to require more fields upon signing up, and the dbo.AspNetUsers table will need to be updated to include assignment required fields.
```
