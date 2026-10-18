<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="settings.aspx.cs" Inherits="WAPP_EduCore.pages.Student.studentProfile" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Settings &amp; Preferences</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <style>
        .st-banner{display:flex;align-items:center;gap:18px;padding:24px 28px;margin-bottom:22px;background:#0f1a46;color:#fff;border-radius:14px}
        .st-avatar{display:flex;align-items:center;justify-content:center;flex-shrink:0;width:72px;height:72px;border-radius:50%;background:#2a4496;font-size:24px;font-weight:700}
        .st-banner h3{margin:0 0 4px;font-family:'Merriweather',Georgia,serif;font-size:20px}
        .st-banner small{display:block;margin-top:2px;color:#cbd8ee;font-size:13px}

        .st-grid{display:grid;grid-template-columns:1fr 420px;gap:22px}
        .st-card{padding:22px 24px;background:#fff;border:1px solid #ebe7e1;border-radius:12px}
        .st-card h3{margin:0 0 16px;font-family:'Merriweather',Georgia,serif;font-size:18px;color:#111c3d}

        .st-field{margin-bottom:14px}
        .st-field label{display:block;margin-bottom:6px;font-size:13px;color:#59637a}
        .st-field input[type=text],.st-field input[type=email],.st-field input[type=date],
        .st-field input[type=password],.st-field select{width:100%;height:40px;padding:0 12px;border:1px solid #e3e3e3;border-radius:8px;background:#fbfbfb;font-family:inherit;font-size:14px;color:#16223b;box-sizing:border-box}
        .st-field input:focus,.st-field select:focus{outline:none;border-color:#4f67cc}
        .st-field input[type=range]{width:100%;accent-color:#2f4f8a}
        .st-row{display:flex;justify-content:space-between;align-items:center}
        .st-goal{font-size:13px;color:#7b8697}
        .st-hint{margin-top:6px;font-size:12px;color:#8a8478}

        .chk{display:flex;align-items:center;gap:10px;padding:8px 0;font-size:14px}
        .chk input{width:16px;height:16px;accent-color:#2f4f8a}

        .st-actions{display:flex;justify-content:flex-end;gap:12px;margin-top:22px}
        .st-btn-solid{padding:10px 20px;border:0;border-radius:8px;background:#243f86;color:#fff;font-size:14px;font-weight:600;cursor:pointer}
        .st-btn-solid:hover{background:#1b3270}
        .st-btn-ghost{padding:10px 20px;border:1px solid #e0e0e0;border-radius:8px;background:#fff;color:#16223b;font-size:14px;cursor:pointer}

        /* accessibility options (applied on this page; can move to student.css to use site-wide) */
        html.a11y-contrast{filter:contrast(1.3)}
        html.a11y-large body{zoom:1.12}
        html.a11y-motion *,html.a11y-motion *::before,html.a11y-motion *::after{animation:none!important;transition:none!important}

        @media(max-width:1000px){.st-grid{grid-template-columns:1fr}}
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />

            <main class="main">
                <div class="topbar">
                    <div>
                        <div class="title">Settings &amp; Preferences</div>
                        <div class="subtitle">Manage your profile information, learning habits, and security rules.</div>
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
                                <label for="txtPhone">Contact Phone</label>
                                <asp:TextBox ID="txtPhone" runat="server" MaxLength="30"></asp:TextBox>
                            </div>
                            <div class="st-field">
                                <label for="txtProgram">Enrolled Program</label>
                                <asp:TextBox ID="txtProgram" runat="server" MaxLength="120"></asp:TextBox>
                            </div>
                        </div>

                        <div class="st-card" style="margin-top:18px">
                            <h3>Account &amp; Security</h3>
                            <asp:Panel ID="pnlPwMsg" runat="server" Visible="false" CssClass="msg error">
                                <asp:Literal ID="litPwMsg" runat="server" Mode="Encode"></asp:Literal>
                            </asp:Panel>
                            <div class="st-field">
                                <label for="txtCurrentPw">Current Password</label>
                                <asp:TextBox ID="txtCurrentPw" runat="server" TextMode="Password"></asp:TextBox>
                            </div>
                            <div class="st-field">
                                <label for="txtNewPw">New Password</label>
                                <asp:TextBox ID="txtNewPw" runat="server" TextMode="Password"></asp:TextBox>
                                <div class="st-hint">At least 8 characters.</div>
                            </div>
                            <div class="st-field">
                                <label for="txtConfirmPw">Confirm New Password</label>
                                <asp:TextBox ID="txtConfirmPw" runat="server" TextMode="Password"></asp:TextBox>
                            </div>
                            <asp:Button ID="btnPassword" runat="server" Text="Change Password"
                                CssClass="st-btn-solid" CausesValidation="false" OnClick="btnPassword_Click" />
                        </div>
                    </div>

                    <div>
                        <div class="st-card">
                            <h3>Learning Preferences</h3>
                            <div class="st-field">
                                <label for="ddlStyle">Preferred Learning Style</label>
                                <asp:DropDownList ID="ddlStyle" runat="server">
                                    <asp:ListItem Value="Visual &amp; Interactive Models">Visual &amp; Interactive Models</asp:ListItem>
                                    <asp:ListItem Value="Reading &amp; Writing">Reading &amp; Writing</asp:ListItem>
                                    <asp:ListItem Value="Audio &amp; Discussion">Audio &amp; Discussion</asp:ListItem>
                                    <asp:ListItem Value="Hands-on Practice">Hands-on Practice</asp:ListItem>
                                </asp:DropDownList>
                            </div>
                            <div class="st-field">
                                <div class="st-row">
                                    <label for="rngGoal">Daily Study Goal</label>
                                    <span class="st-goal" id="goalLbl"></span>
                                </div>
                                <input type="range" id="rngGoal" runat="server" min="0.5" max="6" step="0.5"
                                    oninput="document.getElementById('goalLbl').textContent = this.value + ' hrs / day'" />
                            </div>
                        </div>

                        <div class="st-card" style="margin-top:18px">
                            <h3>Accessibility Settings</h3>
                            <asp:CheckBox ID="chkContrast" runat="server" CssClass="chk" Text="High Contrast Text Mode" />
                            <asp:CheckBox ID="chkLarge" runat="server" CssClass="chk" Text="Larger System Font Scales" />
                            <asp:CheckBox ID="chkMotion" runat="server" CssClass="chk" Text="Reduced Interactive Motion" />
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

        <asp:Literal ID="litA11y" runat="server" Mode="PassThrough"></asp:Literal>
        <script>
(function () {
    var r = document.getElementById('rngGoal');
    if (r) document.getElementById('goalLbl').textContent = r.value + ' hrs / day';
})();
</script>
    </form>
</body>
</html>