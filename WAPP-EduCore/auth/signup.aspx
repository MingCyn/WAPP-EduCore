<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="signup.aspx.cs" Inherits="WAPP_EduCore.auth.signup" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Create Account</title>
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

            <!-- Right Signup Form -->
            <div class="form-section">
                <div class="card">
                    <h2 class="card-title">Create your EduCore account</h2>
                    <p class="card-subtitle">Start learning, teaching, or managing your institution.</p>

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
                    <asp:Button ID="btnSignUp" runat="server" Text="Sign Up" CssClass="btn-primary" OnClick="btnSignUp_Click" />

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
    </script>
</body>
</html>