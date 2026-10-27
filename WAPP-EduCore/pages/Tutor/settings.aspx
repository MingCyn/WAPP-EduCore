<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="settings.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.settings" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>EduCore - Tutor Account Settings</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/settings.css" rel="stylesheet" type="text/css" />
    <style type="text/css">
        /* Specific adjustments for tutor profile layout container */
        .settings-content-wrapper {
            max-width: 1000px;
            margin: 0 auto;
        }
    </style>
    <script type="text/javascript">
        function stToggleMenu(event) {
            event.stopPropagation();
            var menu = document.getElementById('userDropdown');
            if (menu) menu.classList.toggle('open');
        }

        document.addEventListener('click', function () {
            var menu = document.getElementById('userDropdown');
            if (menu) menu.classList.remove('open');
        });

        function stTogglePassword(boxId, button) {
            var box = document.getElementById(boxId);
            if (!box) return;

            var showIt = (box.type === 'password');
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
        <div class="dashboard-layout">
            
            <%-- Shared tutor navigation include --%>
            <% Server.Execute("tutorNav.aspx", false); %>

            <main class="dashboard-container">
                <div class="settings-content-wrapper">
                    
                    <div class="page-header" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                        <div>
                            <h1 class="page-title">Manage Account</h1>
                            <p class="page-subtitle">Manage your tutor profile information and security settings.</p>
                        </div>

                        <div class="user-menu" style="position: relative;">
                            <button type="button" class="user-btn" onclick="stToggleMenu(event)" title="Account" style="background: none; border: none; cursor: pointer;">
                                <span class="st-mini-avatar"><asp:Literal ID="litMiniInitials" runat="server" Mode="Encode"></asp:Literal></span>
                            </button>
                            <div class="user-dropdown" id="userDropdown">
                                <a href="settings.aspx">Manage Account</a>
                                <asp:LinkButton ID="lnkLogout" runat="server" CssClass="logout" CausesValidation="false" OnClick="lnkLogout_Click">Logout</asp:LinkButton>
                            </div>
                        </div>
                    </div>

                    <!-- General page status message box -->
                    <asp:Panel ID="pnlMsg" runat="server" Visible="false" CssClass="msg-box">
                        <asp:Literal ID="litMsg" runat="server" Mode="Encode"></asp:Literal>
                    </asp:Panel>

                    <div class="st-banner">
                        <span class="st-avatar"><asp:Literal ID="litInitials" runat="server" Mode="Encode"></asp:Literal></span>
                        <div>
                            <h3><asp:Literal ID="litBannerName" runat="server" Mode="Encode"></asp:Literal></h3>
                            <small><asp:Literal ID="litBannerEmail" runat="server" Mode="Encode"></asp:Literal> &middot; Tutor ID: <asp:Literal ID="litBannerId" runat="server"></asp:Literal></small>
                            <small>Member since <asp:Literal ID="litSince" runat="server" Mode="Encode"></asp:Literal></small>
                        </div>
                    </div>

                    <div class="st-grid">
                        <!-- left column: Personal Information -->
                        <div>
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

                        <!-- right column: Security -->
                        <div>
                            <div class="st-card">
                                <h3>Account &amp; Security</h3>

                                <!-- Password-specific security message box -->
                                <asp:Panel ID="pnlPwMsg" runat="server" Visible="false" CssClass="msg-box">
                                    <asp:Literal ID="litPwMsg" runat="server" Mode="Encode"></asp:Literal>
                                </asp:Panel>

                                <div class="st-field">
                                    <label for="txtCurrentPw">Current Password</label>
                                    <div class="pw-wrap">
                                        <asp:TextBox ID="txtCurrentPw" runat="server" ClientIDMode="Static" TextMode="Password"></asp:TextBox>
                                        <button type="button" class="pw-eye" onclick="stTogglePassword('txtCurrentPw', this)" title="Show / hide password">
                                            <svg class="eye-slash" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24" /><line x1="1" y1="1" x2="23" y2="23" /></svg>
                                            <svg class="eye-open" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" /></svg>
                                        </button>
                                    </div>
                                </div>

                                <div class="st-field">
                                    <label for="txtNewPw">New Password</label>
                                    <div class="pw-wrap">
                                        <asp:TextBox ID="txtNewPw" runat="server" ClientIDMode="Static" TextMode="Password"></asp:TextBox>
                                        <button type="button" class="pw-eye" onclick="stTogglePassword('txtNewPw', this)" title="Show / hide password">
                                            <svg class="eye-slash" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24" /><line x1="1" y1="1" x2="23" y2="23" /></svg>
                                            <svg class="eye-open" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" /></svg>
                                        </button>
                                    </div>
                                    <div class="st-hint">At least 8 characters.</div>
                                </div>

                                <div class="st-field">
                                    <label for="txtConfirmPw">Confirm New Password</label>
                                    <div class="pw-wrap">
                                        <asp:TextBox ID="txtConfirmPw" runat="server" ClientIDMode="Static" TextMode="Password"></asp:TextBox>
                                        <button type="button" class="pw-eye" onclick="stTogglePassword('txtConfirmPw', this)" title="Show / hide password">
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

                    <div class="st-actions">
                        <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="st-btn-ghost"
                            CausesValidation="false" OnClick="btnCancel_Click" />
                        <asp:Button ID="btnSave" runat="server" Text="Save Changes" CssClass="st-btn-solid"
                            OnClick="btnSave_Click" />
                    </div>

                </div>
            </main>
        </div>
    </form>
</body>
</html>