<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="signup.aspx.cs" Inherits="WAPP_EduCore.auth.signup" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Create Account</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:ital,wght@0,400;0,700;1,400&display=swap" rel="stylesheet" />
    <link href="../Content/auth.css?v=1.0" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="signup-container">
            
            <!-- Left Branding Hero Banner -->
            <div class="hero-section">
                <div class="hero-content">
                    <div class="brand-logo">
                        <img src="../images/logo.png" alt="EduCore Logo" class="brand-logo-img" />
                    </div>

                    <h1 class="hero-title">Learn your way.</h1>
                    <p class="hero-description">
                        An inclusive academic hub designed for flexible scheduling, deep engagement, and certified excellence.
                    </p>

                    <div class="stats-card">
                        <h3 class="stats-title">Today's Global Cohorts</h3>
                        <div class="stats-grid">
                            <div class="stat-item">
                                <span class="stat-value"><asp:Literal ID="LitActiveStudents" runat="server" Text="0" /></span>
                                <span class="stat-label">Active Students</span>
                            </div>
                            <div class="stat-item">
                                <span class="stat-value"><asp:Literal ID="LitCoveredTopics" runat="server" Text="0" /></span>
                                <span class="stat-label">Covered Topics</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Right Signup Form -->
            <div class="form-section">
                <div class="card">
                    <h2 class="card-title">Create your EduCore account</h2>
                    <p class="card-subtitle">Start learning, teaching, or managing your institution.</p>

                    <!-- Google Sign-In Button First -->
                    <div class="form-group" style="margin-top: 20px; margin-bottom: 15px;">
                        <button type="button" class="btn-google" onclick="redirectToGoogle('<%= ConfigurationManager.AppSettings["GoogleClientId"] %>')" style="width: 100%; padding: 12px; background: #fff; border: 1px solid #cbd5e1; border-radius: 8px; font-weight: 600; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 10px;">
                            <svg width="18" height="18" viewBox="0 0 18 18">
                                <path fill="#4285F4" d="M17.64 9.2c0-.63-.06-1.25-.19-1.84H9v3.48h4.84c-.21 1.12-.84 2.07-1.78 2.71v2.25h2.88c1.68-1.55 2.7-3.83 2.7-6.6z" />
                                <path fill="#34A853" d="M9 18c2.43 0 4.47-.8 5.96-2.18l-2.88-2.25c-.81.54-1.85.9-3.08.9-2.37 0-4.38-1.6-5.1-3.75H.8v2.33C2.27 15.98 5.38 18 9 18z" />
                                <path fill="#FBBC05" d="M3.9 10.72c-.19-.54-.3-1.12-.3-1.72s.11-1.18.3-1.72V4.95H.8C.29 5.98 0 7.14 0 8.36s.29 2.38.8 3.41l3.1-2.33z" />
                                <path fill="#EA4335" d="M9 3.58c1.32 0 2.5.45 3.44 1.35l2.58-2.58C13.47.89 11.43 0 9 0 5.38 0 2.27 2.02.8 4.95l3.1 2.33c.72-2.15 2.73-3.7 5.1-3.7z" />
                            </svg>
                            Sign up with Google
                        </button>
                    </div>

                    <div style="text-align: center; margin: 15px 0; color: #94a3b8; font-size: 13px; display: flex; align-items: center; text-align: center;">
                        <span style="flex: 1; border-bottom: 1px solid #e2e8f0;"></span>
                        <span style="padding: 0 10px;">OR FILL IN DETAILS</span>
                        <span style="flex: 1; border-bottom: 1px solid #e2e8f0;"></span>
                    </div>

                    <div class="form-grid">
                        <!-- Full Name -->
                        <div class="form-group">
                            <label for="txtFullName" class="form-label">Full name</label>
                            <asp:TextBox ID="txtFullName" runat="server" CssClass="form-control" placeholder="e.g. Jordan Lee"></asp:TextBox>
                        </div>

                        <!-- Email Address -->
                        <div class="form-group">
                            <label for="txtEmail" class="form-label">Email address</label>
                            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="form-control" placeholder="you@gmail.com"></asp:TextBox>
                        </div>

                        <!-- Date of Birth -->
                        <div class="form-group">
                            <label for="txtDob" class="form-label">Date of Birth</label>
                            <asp:TextBox ID="txtDob" runat="server" TextMode="Date" CssClass="form-control"></asp:TextBox>
                        </div>

                        <!-- Gender -->
                        <div class="form-group">
                            <label for="ddlGender" class="form-label">Gender</label>
                            <asp:DropDownList ID="ddlGender" runat="server" CssClass="form-control ddl-control">
                                <asp:ListItem Text="Select Gender" Value="" Disabled="True" Selected="True"></asp:ListItem>
                                <asp:ListItem Text="Male" Value="Male"></asp:ListItem>
                                <asp:ListItem Text="Female" Value="Female"></asp:ListItem>
                            </asp:DropDownList>
                        </div>

                        <!-- Password -->
                        <div class="form-group">
                            <label for="txtPassword" class="form-label">Password</label>
                            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control" placeholder="At least 8 characters"></asp:TextBox>
                        </div>

                        <!-- Confirm Password -->
                        <div class="form-group">
                            <label for="txtConfirmPassword" class="form-label">Confirm password</label>
                            <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" CssClass="form-control" placeholder="Repeat password"></asp:TextBox>
                        </div>
                    </div>

                    <!-- Account Type Selector -->
                    <div class="form-group margin-top-md">
                        <label class="form-label">Account type</label>
                        <asp:HiddenField ID="hfRole" runat="server" Value="Student" />
                        <div class="role-selector">
                            <button type="button" class="role-btn active" id="btnStudent" onclick="selectRole('Student')">Student</button>
                            <button type="button" class="role-btn" id="btnTutor" onclick="selectRole('Tutor')">Tutor</button>
                        </div>
                    </div>

                    <!-- Submit Button -->
                    <asp:Button ID="btnSignUp" runat="server" Text="Create Account" CssClass="btn-primary" OnClick="btnSignUp_Click" Style="margin-top: 15px;" />

                    <!-- Error/Status Message -->
                    <div class="error-container">
                        <asp:Label ID="lblErrorMessage" runat="server" CssClass="error-message" Text=""></asp:Label>
                    </div>
                    <!-- Footer Redirect -->
                    <div class="form-footer">
                        Already have an account? <a href="Login.aspx" class="login-link">Log in</a>
                    </div>
                </div>
            </div>

        </div>

    </form>

    <script type="text/javascript">
        function selectRole(role) {
            document.getElementById('<%= hfRole.ClientID %>').value = role;
            var btnStudent = document.getElementById('btnStudent');
            var btnTutor = document.getElementById('btnTutor');

            if (role === 'Student') {
                btnStudent.classList.add('active');
                btnTutor.classList.remove('active');
            } else {
                btnTutor.classList.add('active');
                btnStudent.classList.remove('active');
            }
        }

function redirectToGoogle() {
    var clientId = "828548947210-evje5v1phtlnamtftjh5rld14vdhfhnk.apps.googleusercontent.com";
    var redirectUri = "https://localhost:44383/auth/google-callback.aspx"; // Updated port
    var scope = "email profile";
    var responseType = "code";

    var url = `https://accounts.google.com/o/oauth2/v2/auth?client_id=${clientId}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=${responseType}&scope=${encodeURIComponent(scope)}`;
    window.location.href = url;
}
    </script>

</body>
</html>