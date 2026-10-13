<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="settings.aspx.cs" Inherits="WAPP_EduCore.pages.Student.studentProfile" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Settings & Preferences</title>
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <style>
        .profile-banner{background:#0f2540;color:#fff;padding:22px;border-radius:12px;display:flex;align-items:center;justify-content:space-between;margin-bottom:20px}
        .profile-info{display:flex;align-items:center;gap:16px}
        .profile-info .avatar-lg{width:72px;height:72px;border-radius:50%;background:#fff8;display:inline-block}
        .profile-info h3{margin:0;font-size:20px}
        .profile-info small{display:block;color:#d6e0f3}

        .settings-grid{display:grid;grid-template-columns:1fr 420px;gap:22px}
        .card{background:#fff;padding:18px;border-radius:10px;box-shadow:0 6px 18px rgba(16,24,40,0.04);}
        .field{margin-bottom:12px}
        .field label{display:block;color:#59637a;font-size:13px;margin-bottom:6px}
        .field input, .field select{width:100%;padding:10px;border-radius:8px;border:1px solid #ececec;background:#fbfbfb}

        .access-list{display:flex;flex-direction:column;gap:12px}
        .toggle{float:right}

        .actions{display:flex;justify-content:flex-end;gap:12px;margin-top:18px}
        .btn-primary{background:#2f4f8a;color:#fff;padding:10px 14px;border-radius:8px;border:none}
        .btn-ghost{background:#fff;border:1px solid #e6e6e6;padding:10px 14px;border-radius:8px}

        @media(max-width:1000px){.settings-grid{grid-template-columns:1fr}.profile-banner{flex-direction:column;align-items:flex-start}}
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />

            <main class="main">
                <div class="topbar">
                    <div>
                        <h1>Settings &amp; Preferences</h1>
                        <p style="color:#7b8697">Manage your profile information, learning habits, and security rules.</p>
                    </div>
                    <div class="search"><input placeholder="Search courses, modules, grades..." /></div>
                </div>

                <div class="profile-banner">
                    <div class="profile-info">
                        <div class="avatar-lg"></div>
                        <div>
                            <h3>Priya Sharma</h3>
                            <small>priya.sharma@estudio.edu · Student ID: 48922</small>
                            <small style="margin-top:6px;color:#cbd8ee">Academic Member since September 2024</small>
                        </div>
                    </div>
                    <div>
                        <button type="button" class="btn-ghost">Edit Photo</button>
                    </div>
                </div>

                <div class="settings-grid">
                    <div>
                        <div class="card">
                            <h3>Personal Information</h3>
                            <div class="field"><label>Full Legal Name</label><input value="Priya Sharma" /></div>
                            <div class="field"><label>Primary Academic Email</label><input value="priya.sharma@estudio.edu" /></div>
                            <div class="field"><label>Registered Contact Phone</label><input value="+1 (555) 234-5678" /></div>
                            <div class="field"><label>Enrolled Program</label><input value="B.Sc. in Computer Engineering &amp; Design" /></div>
                        </div>

                        <div class="card" style="margin-top:18px">
                            <h3>Account &amp; Security</h3>
                            <div style="display:flex;align-items:center;justify-content:space-between">
                                <div>
                                    <div style="font-weight:600">Two-Factor Authentication (2FA)</div>
                                    <div class="meta" style="color:#7b8697">Provides strong protection using SMS codes.</div>
                                </div>
                                <div style="display:flex;align-items:center;gap:12px">
                                    <div style="width:44px;height:26px;background:#e6eefc;border-radius:16px;display:inline-block;position:relative">
                                        <div style="width:22px;height:22px;background:#2f4f8a;border-radius:50%;position:absolute;top:2px;left:18px"></div>
                                    </div>
                                    <button class="btn-primary">Change Password</button>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div>
                        <div class="card">
                            <h3>Learning Preferences</h3>
                            <div class="field"><label>Preferred Learning Style</label>
                                <select><option>Visual &amp; Interactive Models</option></select>
                            </div>
                            <div class="field"><label>Daily Study Goal <span style="float:right;color:#7b8697">1.5 hrs / day</span></label>
                                <div style="background:#eef3fb;border-radius:8px;height:10px;overflow:hidden"><div style="width:45%;height:100%;background:#3561b3"></div></div>
                            </div>
                        </div>

                        <div class="card" style="margin-top:18px">
                            <h3>Accessibility Settings</h3>
                            <div class="access-list">
                                <div><label>High Contrast Text Mode</label><div class="toggle"> <input type="checkbox" /></div></div>
                                <div><label>Larger System Font Scales</label><div class="toggle"> <input type="checkbox" /></div></div>
                                <div><label>Reduced Interactive Motion</label><div class="toggle"> <input type="checkbox" /></div></div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="actions">
                    <button class="btn-ghost" type="button">Cancel</button>
                    <button class="btn-primary" type="button">Save Changes</button>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
