<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="complete-profile.aspx.cs" Inherits="WAPP_EduCore.auth.complete_profile" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Complete Your Profile</title>
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

                    <h1 class="hero-title">Almost there.</h1>
                    <p class="hero-description">
                        We just need a few more details to set up your account and tailor your learning experience.
                    </p>
                </div>
            </div>

            <!-- Right Form Section -->
            <div class="form-section">
                <div class="card">
                    <h2 class="card-title">Complete your profile</h2>
                    <p class="card-subtitle">Please provide your Date of Birth, Gender, and choose your account type.</p>

                    <div class="form-grid">
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
                                <asp:ListItem Text="Other" Value="Other"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>

                    <!-- Password and Confirm Password Row (Stacked or Balanced Grid) -->
                    <div class="form-grid" style="margin-top: 16px;">
                        <div class="form-group">
                            <label for="txtPassword" class="form-label">Password (Optional)</label>
                            <div class="password-wrapper">
                                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control" placeholder="At least 8 chars"></asp:TextBox>
                            </div>
                        </div>

                        <div class="form-group">
                            <label for="txtConfirmPassword" class="form-label">Confirm password</label>
                            <div class="password-wrapper">
                                <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" CssClass="form-control" placeholder="Repeat password"></asp:TextBox>
                            </div>
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
                    <asp:Button ID="btnComplete" runat="server" Text="Save and Continue" CssClass="btn-primary" OnClick="btnComplete_Click" Style="margin-top: 15px;" />

                    <!-- Error/Status Message -->
                    <div class="error-container">
                        <asp:Label ID="lblErrorMessage" runat="server" CssClass="error-message" Text=""></asp:Label>
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