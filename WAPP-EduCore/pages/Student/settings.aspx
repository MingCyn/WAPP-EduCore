<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="settings.aspx.cs" Inherits="WAPP_EduCore.pages.Student.studentProfile" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Manage Account</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <style type="text/css">
        /* ---------- banner ---------- */
        .st-banner{display:flex;align-items:center;gap:18px;padding:24px 28px;margin-bottom:22px;background:#0f1a46;color:#fff;border-radius:14px}
        .st-avatar{display:flex;align-items:center;justify-content:center;flex-shrink:0;width:72px;height:72px;border-radius:50%;background:#2a4496;font-size:24px;font-weight:700}
        .st-banner h3{margin:0 0 4px;font-family:'Merriweather',Georgia,serif;font-size:20px}
        .st-banner small{display:block;margin-top:2px;color:#cbd8ee;font-size:13px}

        /* ---------- two columns of cards ---------- */
        .st-grid{display:grid;grid-template-columns:minmax(0,1.6fr) minmax(0,1fr);gap:22px}
        .st-card{padding:22px 24px;background:#fff;border:1px solid #ebe7e1;border-radius:12px}
        .st-card h3{margin:0 0 16px;font-family:'Merriweather',Georgia,serif;font-size:18px;color:#111c3d}

        /* ---------- fields ---------- */
        .st-field{margin-bottom:14px}
        .st-field label{display:block;margin-bottom:6px;font-size:13px;color:#59637a}
        .st-field input[type=text],.st-field input[type=email],.st-field input[type=date],.st-field input[type=password]{width:100%;height:40px;padding:0 12px;border:1px solid #e3e3e3;border-radius:8px;background:#fbfbfb;font-family:inherit;font-size:14px;color:#16223b;box-sizing:border-box}
        .st-field input.readonly{background:#F1F3F9;color:#64748B;cursor:not-allowed}
        .st-field input:focus{outline:none;border-color:#4f67cc}
        .st-hint{margin-top:6px;font-size:12px;color:#8a8478}

        /* ---------- password box with the eye inside ---------- */
        .pw-wrap{position:relative}
        .pw-wrap input{padding-right:44px !important}
        .pw-eye{position:absolute;top:50%;right:10px;transform:translateY(-50%);padding:4px;background:none;border:none;color:#64748B;cursor:pointer;line-height:0}
        .pw-eye:hover{color:#4f67cc}
        .pw-eye .eye-open{display:none}

        /* ---------- buttons ---------- */
        .st-actions{display:flex;justify-content:flex-end;gap:12px;margin-top:22px}
        .st-btn-solid{padding:10px 20px;border:0;border-radius:8px;background:#243f86;color:#fff;font-size:14px;font-weight:600;cursor:pointer}
        .st-btn-solid:hover{background:#1b3270}
        .st-btn-ghost{padding:10px 20px;border:1px solid #e0e0e0;border-radius:8px;background:#fff;color:#16223b;font-size:14px;cursor:pointer}

        /* ---------- avatar + drop-down menu (top right) ---------- */
        .user-menu{position:relative}
        .user-btn{display:flex;align-items:center;padding:0;background:none;border:none;cursor:pointer}
        .st-mini-avatar{display:inline-block;width:34px;height:34px;border-radius:50%;background:#E0E7FF;color:#4F67CC;font-size:12px;font-weight:700;line-height:34px;text-align:center}
        .user-dropdown{display:none;position:absolute;right:0;top:44px;min-width:180px;padding:6px;background:#fff;border:1px solid var(--line);border-radius:10px;box-shadow:0 10px 30px rgba(15,27,76,0.15);z-index:50}
        .user-dropdown.open{display:block}
        .user-dropdown a{display:block;padding:10px 12px;border-radius:6px;color:var(--text);font-size:13px;text-decoration:none}
        .user-dropdown a:hover{background:#F1F5F9}
        .user-dropdown a.logout{color:#DC2626}

        @media(max-width:1000px){.st-grid{grid-template-columns:1fr}}
    </style>
    <script type="text/javascript">
// drop-down menu at the top right
function toggleUserMenu(event) {
    event.stopPropagation();
    document.getElementById('userDropdown').classList.toggle('open');
}
document.addEventListener('click', function () {
    var d = document.getElementById('userDropdown');
    if (d) d.classList.remove('open');
});

// the eye button: show or hide a password
function togglePassword(boxId, button) {
    var box = document.getElementById(boxId);
    var showIt = (box.type == 'password');
    box.type = showIt ? 'text' : 'password';
    button.querySelector('.eye-slash').style.display = showIt ? 'none' : 'block';
    button.querySelector('.eye-open').style.display = showIt ? 'block' : 'none';
}
</script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />

            <main class="main">
                <div class="topbar">
                    <div>
                        <div class="title">Manage Account</div>
                        <div class="subtitle">Manage your profile information and security rules.</div>
                    </div>

                    <div class="user-menu">
                        <button type="button" class="user-btn" onclick="toggleUserMenu(event)" title="Account">
                            <span class="st-mini-avatar"><asp:Literal ID="litMiniInitials" runat="server" Mode="Encode"></asp:Literal></span>
                        </button>
                        <div class="user-dropdown" id="userDropdown">
                            <a href="settings.aspx">Manage Account</a>
                            <asp:LinkButton ID="lnkLogout" runat="server" CssClass="logout" CausesValidation="false" OnClick="lnkLogout_Click">Logout</asp:LinkButton>
                        </div>
                    </div>
                </div>

                <asp:Panel ID="pnlMsg" runat="server" Visible="false" CssClass="msg ok">
                    <asp:Literal ID="litMsg" runat="server" Mode="Encode"></asp:Literal>
                </asp:Panel>

                <div class="st-banner">
                    <span class="st-avatar"><asp:Literal ID="litInitials" runat="server" Mode="Encode"></asp:Literal></span>
                    <div>
                        <h3><asp:Literal ID="litBannerName" runat="server" Mode="Encode"></asp:Literal></h3>
                        <small><asp:Literal ID="litBannerEmail" runat="server" Mode="Encode"></asp:Literal> &middot; Student ID: <asp:Literal ID="litBannerId" runat="server"></asp:Literal></small>
                        <small>Member since <asp:Literal ID="litSince" runat="server" Mode="Encode"></asp:Literal></small>
                    </div>
                </div>

                <div class="st-grid">
                    <!-- left column -->
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

                    <!-- right column -->
                    <div>
                        <div class="st-card">
                            <h3>Account &amp; Security</h3>

                            <asp:Panel ID="pnlPwMsg" runat="server" Visible="false" CssClass="msg error">
                                <asp:Literal ID="litPwMsg" runat="server" Mode="Encode"></asp:Literal>
                            </asp:Panel>

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
                                CssClass="st-btn-solid" CausesValidation="false" OnClick="btnPassword_Click" />
                        </div>
                    </div>
                </div>

                <div class="st-actions">
                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="st-btn-ghost"
                        CausesValidation="false" OnClick="btnCancel_Click" />
                    <asp:Button ID="btnSave" runat="server" Text="Save Changes" CssClass="st-btn-solid"
                        OnClick="btnSave_Click" />
                </div>
            </main>
        </div>
    </form>
</body>
</html>