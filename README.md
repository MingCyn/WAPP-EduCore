# WAPP-EduCore

Welcome to the **WAPP-EduCore** repository! This is an ASP.NET Web Application (.NET Framework) built using Web Forms and integrated with PostgreSQL.

Please follow this guide carefully to set up your local development environment, understand the project folder structure, and adhere to our Git workflow to prevent merge conflicts.

---

## 🛠️ Initial Setup Instructions

### 1. Clone the Repository
* Open **Visual Studio**.
* On the startup screen, select **Clone a repository**.
* Enter the repo URL: `https://github.com/MingCyn/WAPP-EduCore.git`
* Set your local path and click **Clone**.

---

### 2. Open the Solution
* In Visual Studio, navigate to the cloned folder and open **`WAPP-EduCore.sln`**.
* ⚠️ **Do NOT** open individual `.aspx` files directly via Folder View—always open through the `.sln` file so Visual Studio loads project dependencies correctly.

---

### 3. Configure Connection String in `Web.config`
* Our database is hosted in the cloud on **Supabase**, so **you do NOT need to install PostgreSQL or pgAdmin locally**.
* Open **`Web.config`** located at the root of the project.
* Locate the `<connectionStrings>` section and fill in our shared database password:

```xml
<configuration>
  <connectionStrings>
    <add name="SupabasePostgres" 
         connectionString="Server=db.iqrwnjvshxcqdkwfnilt.supabase.co;Port=5432;Database=postgres;User Id=postgres;Password=YOUR_TEAM_PASSWORD_HERE;Ssl Mode=Require;Trust Server Certificate=true;" 
         providerName="Npgsql" />
  </connectionStrings>
  ...
</configuration>

```
### 4. **Restore NuGet Packages:**
   * Right-click `Solution 'WAPP-EduCore'` in **Solution Explorer** -> click **Restore NuGet Packages**.
   * Go to **Build** > **Build Solution** (`Ctrl` + `Shift` + `B`) to ensure all dependencies (such as `Npgsql`) build correctly.



## 📁 Folder & Project Architecture

To keep the repository organized, please place new files inside their designated folders:

```text
WAPP-EduCore/
│
├── WAPP-EduCore.sln              <-- Solution File
│
└── WAPP-EduCore/                 <-- Main Project Folder
    ├── App_Data/                 <-- Local assets / temporary database scripts
    ├── auth/                     <-- Login.aspx, Signup.aspx
    ├── pages/                    
    │   ├── General/              <-- Dashboard, Courses, Discussion, Messages, Profile
    │   ├── Student/              <-- MyCourses, Calendar, Assessment, VirtualLab, etc.
    │   ├── Tutor/                <-- CourseBuilder, AssessmentBuilder, etc.
    │   └── Admin/                <-- AdminDashboard, UserManagement, ActivityLog, etc.
    ├── Models/                   <-- C# Data Classes (User.cs, Course.cs, etc.)
    ├── Data/                     <-- DatabaseHelper.cs & Repositories (PostgreSQL queries)
    ├── Services/                 <-- Business & Validation Logic (AuthenticationService.cs)
    ├── Content/                  <-- CSS Stylesheets
    ├── Scripts/                  <-- JavaScript files
    └── Site.Master               <-- Global Master Layout Page
```

---

## 🔄 Git Workflow (Push & Pull Rules)

To prevent `.csproj` file conflicts and code overwrites, follow this simple rule: **ALWAYS PULL BEFORE YOU START WORKING.**

### Step 1: Start of Work (Pull Latest Changes)
Before making any changes or adding new files:
1. Open the **Git Changes** tab in Visual Studio (`Ctrl` + `Alt` + `F7`).
2. Click the **Pull** button (`↓` downward arrow).
3. Ensure your local branch is up to date with `main`.

### Step 2: Making Changes
* Work on your assigned pages or classes.
* Notice the icons in Solution Explorer:
  * `🔒` **Blue Lock:** File is saved and synchronized with GitHub.
  * `✓` **Red Check Mark:** File has local changes waiting to be committed.

### Step 3: End of Work (Commit & Push Changes)
When your feature or task is ready:
1. Save all your files (`Ctrl` + `Shift` + `S`).
2. Open the **Git Changes** tab (`Ctrl` + `Alt` + `F7`).
3. Check the **Changes** list to verify you are only committing intended files.
4. Enter a descriptive commit message (e.g., `Add UI layout for Student/Calendar.aspx`).
5. Click **Commit All**.
6. Click the **Pull** button (`↓`) one more time to fetch any changes pushed by teammates while you were working.
7. Click the **Push** button (`↑` upward arrow) to send your code to GitHub.

---

## ⚠️ Important Guidelines to Prevent Conflicts

1. **Avoid Renaming/Moving Pages Shared by Others:** In Web Forms, moving or renaming a page alters `WAPP-EduCore.csproj`. Coordinate with the team before restructuring existing folders.
2. **Do NOT Commit Build Artifacts:** Folders like `bin/`, `obj/`, `.vs/`, and `packages/` are automatically ignored via `.gitignore`. Do not bypass `.gitignore` settings.
3. **Relative URL Linking:** When linking between subfolders in HTML or ASP.NET controls, always use the root prefix `~` (e.g., `href="~/Pages/General/Dashboard.aspx"`).
4. **Database Credentials:** Do not hardcode private PostgreSQL passwords in C# source files. Configure your connection string in `Web.config`.
