<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="WAPP_EduCore.auth.login" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Sign In</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:ital,wght@0,400;0,700;1,400&display=swap" rel="stylesheet" />
    <link href="../Content/auth.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="signup-container">
            
            <!-- Left Branding Hero Banner -->
            <div class="hero-section">
                <div class="hero-content">
                    <div class="brand-logo">
                        <img src="../images/EduCore_Logo.png" alt="EduCore Logo" class="brand-logo-img" />
                    </div>

                    <h1 class="hero-title">Learn your way.</h1>
                    <p class="hero-description">
                        An inclusive academic hub designed for flexible scheduling, deep engagement, and certified excellence.
                    </p>

                    <div class="stats-card">
                        <h3 class="stats-title">Today's Global Cohorts</h3>
                        <div class="stats-grid">
                            <div class="stat-item">
                                <span class="stat-value">12,480+</span>
                                <span class="stat-label">Active Students</span>
                            </div>
                            <div class="stat-item">
                                <span class="stat-value">98.4%</span>
                                <span class="stat-label">Completion Rate</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Right Login Form -->
            <div class="form-section">
                <div class="card">
                    <h2 class="card-title">Welcome back</h2>
                    <p class="card-subtitle">Access your personalized learning interface.</p>

                    <div class="form-grid-single">
                        <!-- Email Address -->
                        <div class="form-group">
                            <label for="txtEmail" class="form-label">Institutional Email Address</label>
                            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="form-control" placeholder="e.g. patrick@gmail.com"></asp:TextBox>
                        </div>

                        <!-- Password -->
                        <div class="form-group">
                            <div style="display: flex; justify-content: space-between; align-items: center;">
                                <label for="txtPassword" class="form-label">Password</label>
                                <a href="ForgotPassword.aspx" class="login-link" style="font-size: 13px;">Forgot password?</a>
                            </div>

                            <div class="password-wrapper">
                                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control" placeholder="••••••••••••"></asp:TextBox>
                                <button type="button" id="btnTogglePassword" class="toggle-password" aria-label="Show password" onclick="togglePassword()">
                                    <!-- Eye icon (shown when password is hidden) -->
                                    <svg id="iconEye" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                                        <circle cx="12" cy="12" r="3"></circle>
                                    </svg>
                                    <!-- Eye-off icon (shown when password is visible) -->
                                    <svg id="iconEyeOff" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                                        <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"></path>
                                        <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"></path>
                                        <path d="M14.12 14.12a3 3 0 1 1-4.24-4.24"></path>
                                        <line x1="1" y1="1" x2="23" y2="23"></line>
                                    </svg>
                                </button>
                            </div>
                        </div>
                    </div>

                    <!-- Submit Button -->
                    <asp:Button ID="btnLogin" runat="server" Text="Sign In" CssClass="btn-primary margin-top-md" OnClick="btnLogin_Click" />

                    <!-- Error/Status Message -->
                    <div class="error-container">
                        <asp:Label ID="lblErrorMessage" runat="server" CssClass="error-message" Text=""></asp:Label>
                    </div>

                    <!-- Footer Redirect -->
                    <div class="form-footer">
                        New to EduCore? <a href="signup.aspx" class="login-link">Create account</a>
                    </div>
                </div>
            </div>

        </div>
    </form>

    <script type="text/javascript">
        function togglePassword() {
    var input = document.getElementById('<%= txtPassword.ClientID %>');
    var eye = document.getElementById('iconEye');
    var eyeOff = document.getElementById('iconEyeOff');
    var btn = document.getElementById('btnTogglePassword');
    var showing = input.type === 'text';

    input.type = showing ? 'password' : 'text';
    eye.style.display = showing ? 'block' : 'none';
    eyeOff.style.display = showing ? 'none' : 'block';
    btn.setAttribute('aria-label', showing ? 'Show password' : 'Hide password');
}
</script>
</body>
</html>