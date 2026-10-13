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
                            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control" placeholder="••••••••••••"></asp:TextBox>
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
                        New to estudio? <a href="signup.aspx" class="login-link">Create account</a>
                    </div>
                </div>
            </div>

        </div>
    </form>
</body>
</html>