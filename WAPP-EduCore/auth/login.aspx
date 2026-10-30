<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="WAPP_EduCore.auth.login" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Sign In</title>
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

                    <!-- Google Sign-In Button with Calendar Scope -->
                    <button type="button" class="btn-primary" onclick="redirectToGoogle()" style="background: #ffffff; color: #1e293b; border: 1px solid #e2e8f0; margin-top: 12px; display: flex; align-items: center; justify-content: center; gap: 10px; box-shadow: 0 4px 10px rgba(0,0,0,0.03);">
                        <svg width="18" height="18" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48">
                            <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
                            <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
                            <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
                            <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.46-9.93l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
                        </svg>
                        Sign in with Google
                    </button>

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

        function redirectToGoogle() {
            var clientId = "828548947210-evje5v1phtlnamtftjh5rld14vdhfhnk.apps.googleusercontent.com"; 
            var redirectUri = "https://localhost:44383/auth/google-callback.aspx";
            
            // Includes the requested Google Calendar events write scope
            var scope = encodeURIComponent("email profile https://www.googleapis.com/auth/calendar.events");
            
            var authUrl = `https://accounts.google.com/o/oauth2/v2/auth?client_id=${clientId}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=code&scope=${scope}&access_type=offline&prompt=consent`;
            
            window.location.href = authUrl;
        }
    </script>
</body>
</html>