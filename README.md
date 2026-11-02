
# WAPP-EduCore (Local Setup Guide)

Welcome to **WAPP-EduCore**! This guide walks you through extracting the project files, opening the solution in Visual Studio, building the project, and running it locally using **IIS Express** and **Google Chrome**.

Our production-ready application is also deployed and hosted on Microsoft Azure:

**Live Website:** [WAPP-EduCore](https://educore-bwgvd2fxafbdf0e8.southeastasia-01.azurewebsites.net/)

---

## 🛠️ Step-by-Step Setup Instructions

### Step 1: Extract the ZIP File

1. Locate the downloaded project ZIP file (e.g., `WAPP-EduCore.zip`).
2. Right-click the ZIP file and select **Extract All...** (or use an extraction tool such as 7-Zip or WinRAR).
3. Choose your preferred destination folder and click **Extract**.

   > **Tip:** Extract the project into a stable directory, such as your `source/repos` folder, before opening it in Visual Studio.

### Step 2: Open the Solution in Visual Studio

To ensure that all project files, references, and dependencies load correctly, always open the project through its solution file rather than browsing individual folders.

1. Launch **Visual Studio**.
2. From the startup window, select **Open a project or solution**, or navigate to **File > Open > Project/Solution...** (`Ctrl + Shift + O`).
3. Browse to the folder where you extracted the project.
4. Select **`WAPP-EduCore.sln`** (or `.slnx`, if applicable) and click **Open**.

   > ⚠️ **Important:** Do not open individual `.aspx` files directly through folder view. Opening the solution file ensures that Visual Studio can properly recognize the project structure and resolve its dependencies.

### Step 3: Verify the Solution Explorer

Once the project has finished loading, verify that Visual Studio recognizes the project structure correctly.

1. Locate the **Solution Explorer** panel on the right side of Visual Studio.
2. Ensure that **`Solution 'WAPP-EduCore'`** appears at the top, with the main project nested underneath.
3. Verify that the following project directories are visible:

   - `Auth/`
   - `Pages/`
   - `Models/`
   - `Data/`
   - `Content/`

4. Check that there are no missing files, unloaded projects, or broken reference indicators.

### Step 4: Build the Project

Before running the application, ensure that all project references and dependencies compile successfully.

1. Navigate to **Build > Build Solution** in the Visual Studio menu.
2. Alternatively, use the keyboard shortcut `Ctrl + Shift + B`.
3. Check the **Output** window at the bottom of Visual Studio.
4. Confirm that the build completes successfully with:

   - **0 Failed**
   - **0 Errors**

   > **Note:** Warnings may appear depending on your environment. Review them to ensure that none affect application functionality.

### Step 5: Run Locally Using IIS Express (Google Chrome)

1. Locate the run configuration dropdown in the Visual Studio toolbar.
2. Select **IIS Express** as the launch profile.
3. Ensure that **Google Chrome** is selected as your preferred browser.
4. Click the **IIS Express (Google Chrome)** run button or press `F5`.
5. Visual Studio will launch the local debugging server, and Google Chrome will automatically open the application.

Your local development instance of **WAPP-EduCore** is now ready to use!

---


## 🔐 Administrator Login Credentials

WAPP-EduCore provides a pre-configured administrator account for accessing the administrative dashboard.

| Credential | Details |
|---|---|
| **Email** | `admin@gmail.com` |
| **Password** | `adminEduCore` |

> ⚠️ **Important Notes:**
> - The administrator account is pre-configured in the system.
> - Administrator accounts cannot be registered externally through the user registration page.
> - Please use the provided credentials above to log in as an administrator.
> - Do not attempt to create another administrator account through the standard registration process.

---

## 🌐 Deployment

WAPP-EduCore is also deployed and accessible online through Microsoft Azure.

- **Hosting Platform:** Microsoft Azure App Service
- **Live Application:** [https://educore-bwgvd2fxafbdf0e8.southeastasia-01.azurewebsites.net/](https://educore-bwgvd2fxafbdf0e8.southeastasia-01.azurewebsites.net/)

---

**Thank you for exploring WAPP-EduCore!**
