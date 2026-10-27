<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="adminSettings.aspx.cs" Inherits="WAPP_EduCore.pages.Admin.adminSettings" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">

<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>EduCore - Manage Account</title>

    <!-- Font -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />

    <!-- Core admin styles & Shared settings design stylesheet -->
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/settings.css" rel="stylesheet" type="text/css" />

    <!-- Sidebar and alignment styles matching adminReports.aspx -->
    <style type="text/css">
        /* ---------- Sidebar: a little icon before every menu name ---------- */
        .sidebar a.menu-item {
            display: flex;
            align-items: center;
            gap: 12px;
        }

        .menu-icon {
            width: 18px;
            height: 18px;
            flex-shrink: 0;
            fill: none;
            stroke: currentColor;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
        }

        /* ---------- The avatar + drop-down menu (top right) ---------- */
        .user-menu {
            position: relative;
            display: flex;
            align-items: center;
            gap: 12px;
        }

        .user-btn {
            display: flex;
            align-items: center;
            padding: 0;
            background: none;
            border: none;
            cursor: pointer;
        }

        .user-btn .avatar {
            margin-left: 0;
        }

        .user-dropdown {
            display: none;
            position: absolute;
            right: 0;
            top: 44px;
            min-width: 180px;
            padding: 6px;
            background: white;
            border: 1px solid var(--line);
            border-radius: 10px;
            box-shadow: 0 10px 30px rgba(15, 27, 76, 0.15);
            z-index: 50;
        }

        .user-dropdown.open {
            display: block;
        }

        .user-dropdown a {
            display: block;
            padding: 10px 12px;
            border-radius: 6px;
            color: var(--text);
            font-size: 13px;
            text-decoration: none;
        }

        .user-dropdown a:hover {
            background: #F1F5F9;
        }

        .user-dropdown a.logout {
            color: #DC2626;
        }
    </style>

    <!-- ==================================================
         JAVASCRIPT
         ================================================== -->
    <script type="text/javascript">
        // ----- The drop-down menu at the top right -----
        function toggleUserMenu(event) {
            event.stopPropagation();
            var dropdown = document.getElementById('userDropdown');
            if (dropdown) dropdown.classList.toggle('open');
        }

        document.addEventListener('click', function () {
            var dropdown = document.getElementById('userDropdown');
            if (dropdown) dropdown.classList.remove('open');
        });

        // ----- The eye button: show / hide a password -----
        function togglePassword(boxId, button) {
            var box = document.getElementById(boxId);
            if (!box) return;

            var showIt = (box.type == 'password');
            box.type = showIt ? 'text' : 'password';

            var slash = button.querySelector('.eye-slash');
            var open = button.querySelector('.eye-open');
            if (slash) slash.style.display = showIt ? 'none' : 'block';
            if (open) open.style.display = showIt ? 'block' : 'none';
        }
    </script>
</head>

<body>
    <form id="form1" runat="server">

        <!-- ==================================================
             PART 1 : ADMIN SIDEBAR (dark menu on the left)
             ================================================== -->
        <div class="sidebar">
            <img src="../../images/logo.png" alt="EduCore" class="logo" />

            <a class="menu-item" href="adminDashboard.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><rect x="3" y="3" width="7" height="7" /><rect x="14" y="3" width="7" height="7" /><rect x="14" y="14" width="7" height="7" /><rect x="3" y="14" width="7" height="7" /></svg>
                Dashboard
            </a>
            <a class="menu-item" href="userManagement.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 00-3-3.87" /><path d="M16 3.13a4 4 0 010 7.75" /></svg>
                Users
            </a>
            <a class="menu-item" href="coursesManagement.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><path d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z" /><path d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z" /></svg>
                Courses
            </a>
            <a class="menu-item" href="lessonManagement.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" /><polyline points="14 2 14 8 20 8" /><line x1="16" y1="13" x2="8" y2="13" /><line x1="16" y1="17" x2="8" y2="17" /></svg>
                Lessons
            </a>
            <a class="menu-item" href="quizManagement.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" /><path d="M9.09 9a3 3 0 015.83 1c0 2-3 3-3 3" /><line x1="12" y1="17" x2="12.01" y2="17" /></svg>
                Quizzes
            </a>
            <a class="menu-item" href="adminReports.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><line x1="18" y1="20" x2="18" y2="10" /><line x1="12" y1="20" x2="12" y2="4" /><line x1="6" y1="20" x2="6" y2="14" /></svg>
                Reports
            </a>

            <div class="me">
                <%= AdminName %>
                <small>Platform administrator</small>
            </div>
        </div>


        <!-- ==================================================
             PART 2 : EVERYTHING ON THE RIGHT SIDE
             ================================================== -->
        <div class="main">

            <!-- ---------- Top bar ---------- -->
            <div class="topbar">
                <div>
                    <h1>Manage Account</h1>
                    <p>Manage your profile information and security settings.</p>
                </div>

                <div class="user-menu">
                    <button type="button" class="user-btn" onclick="toggleUserMenu(event)" title="Account">
                        <span class="avatar"><%= AdminInitials %></span>
                    </button>

                    <!-- The dropdown options -->
                    <div class="user-dropdown" id="userDropdown">
                        <a href="adminSettings.aspx">Manage Account</a>
                        <asp:LinkButton ID="lnkLogout" runat="server" CssClass="logout" CausesValidation="false" OnClick="lnkLogout_Click">Logout</asp:LinkButton>
                    </div>
                </div>
            </div>

            <div class="content">

                <!-- Shared message box design matching settings.css -->
                <asp:Panel ID="pnlMsg" runat="server" Visible="false" CssClass="msg-box">
                    <asp:Literal ID="litMsg" runat="server" Mode="Encode"></asp:Literal>
                </asp:Panel>


                <!-- ==============================================
                     THE BANNER (initials, name, email, id)
                     ============================================== -->
                <div class="st-banner">
                    <span class="st-avatar"><asp:Literal ID="litInitials" runat="server" Mode="Encode"></asp:Literal></span>
                    <div>
                        <h3><asp:Literal ID="litBannerName" runat="server" Mode="Encode"></asp:Literal></h3>
                        <small><asp:Literal ID="litBannerEmail" runat="server" Mode="Encode"></asp:Literal> &middot; Admin ID: <asp:Literal ID="litBannerId" runat="server"></asp:Literal></small>
                        <small>Member since <asp:Literal ID="litSince" runat="server" Mode="Encode"></asp:Literal></small>
                    </div>
                </div>


                <div class="st-grid">

                    <!-- ---------- LEFT COLUMN ---------- -->
                    <div>

                        <!-- Personal Information -->
                        <div class="st-card">
                            <h3>Personal Information</h3>

                            <div class="st-field">
                                <label for="txtName">Full Name</label>
                                <asp:TextBox ID="txtName" runat="server" MaxLength="100"></asp:TextBox>
                            </div>

                            <div class="st-field">
                                <label for="txtEmail">Email Address</label>
                                <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="150"></asp:TextBox>
                            </div>

                            <div class="st-field">
                                <label for="txtDob">Date of Birth</label>
                                <asp:TextBox ID="txtDob" runat="server" TextMode="Date"></asp:TextBox>
                            </div>

                            <div class="st-field">
                                <label for="txtRole">Role</label>
                                <asp:TextBox ID="txtRole" runat="server" ReadOnly="true" CssClass="readonly"></asp:TextBox>
                            </div>
                        </div>
                    </div>


                    <!-- ---------- RIGHT COLUMN ---------- -->
                    <div>

                        <!-- Account & Security (with the eye buttons) -->
                        <div class="st-card">
                            <h3>Account &amp; Security</h3>

                            <asp:Panel ID="pnlPwMsg" runat="server" Visible="false" CssClass="msg-box">
                                <asp:Literal ID="litPwMsg" runat="server" Mode="Encode"></asp:Literal>
                            </asp:Panel>

                            <!-- Current password -->
                            <div class="st-field">
                                <label for="txtCurrentPw">Current Password</label>
                                <div class="pw-wrap">
                                    <asp:TextBox ID="txtCurrentPw" runat="server" ClientIDMode="Static" TextMode="Password"></asp:TextBox>
                                    <button type="button" class="pw-eye" onclick="togglePassword('txtCurrentPw', this)" title="Show / hide password">
                                        <svg class="eye-slash" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24" /><line x1="1" y1="1" x2="23" y2="23" /></svg>
                                        <svg class="eye-open" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" /></svg>
                                    </button>
                                </div>
                            </div>

                            <!-- New password -->
                            <div class="st-field">
                                <label for="txtNewPw">New Password</label>
                                <div class="pw-wrap">
                                    <asp:TextBox ID="txtNewPw" runat="server" ClientIDMode="Static" TextMode="Password"></asp:TextBox>
                                    <button type="button" class="pw-eye" onclick="togglePassword('txtNewPw', this)" title="Show / hide password">
                                        <svg class="eye-slash" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24" /><line x1="1" y1="1" x2="23" y2="23" /></svg>
                                        <svg class="eye-open" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" /></svg>
                                    </button>
                                </div>
                                <div class="st-hint">At least 8 characters.</div>
                            </div>

                            <!-- Confirm new password -->
                            <div class="st-field">
                                <label for="txtConfirmPw">Confirm New Password</label>
                                <div class="pw-wrap">
                                    <asp:TextBox ID="txtConfirmPw" runat="server" ClientIDMode="Static" TextMode="Password"></asp:TextBox>
                                    <button type="button" class="pw-eye" onclick="togglePassword('txtConfirmPw', this)" title="Show / hide password">
                                        <svg class="eye-slash" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24" /><line x1="1" y1="1" x2="23" y2="23" /></svg>
                                        <svg class="eye-open" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" /></svg>
                                    </button>
                                </div>
                            </div>

                            <asp:Button ID="btnPassword" runat="server" Text="Change Password"
                                CssClass="st-btn-solid" OnClick="btnPassword_Click" />
                        </div>

                    </div>
                </div>


                <!-- Cancel + Save -->
                <div class="st-actions">
                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="st-btn-ghost"
                        CausesValidation="false" OnClick="btnCancel_Click" />
                    <asp:Button ID="btnSave" runat="server" Text="Save Changes" CssClass="st-btn-solid"
                        OnClick="btnSave_Click" />
                </div>

            </div>
        </div>

    </form>
</body>
</html>