Note:  
I am using  
Visual Studio Enterprise 2026 18.2.1  
I have Visual Studio Build Tools 2019 16.11.53  
.NET 10.0.102  
If the steps below do not fix your build errors, try using these versions instead  
  
  
Open the project in Visual Studio  
Right click on the project file (root, called LMS) and click 'Open in File Explorer'  
In the File Explorer, right click again and open in terminal  
Inside the terminal, run these commands one by one:  
dotnet add package Microsoft.EntityFrameworkCore  
dotnet add package Microsoft.EntityFrameworkCore.Relational  
dotnet add package Microsoft.EntityFrameworkCore.Tools  
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore  
dotnet add package Microsoft.EntityFrameworkCore.SqlServer  
dotnet add package Microsoft.EntityFrameworkCore.Sqlite  
dotnet restore    

Then close Visual Studio (may not be necessary)  
Repoen the project  
Right click LMS file  
Clean  
Rebuild    

At this point my project worked, but if yours still doesn't try updating all your software to what I have listed above
