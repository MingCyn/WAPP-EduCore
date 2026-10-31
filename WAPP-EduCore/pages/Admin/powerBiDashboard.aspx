<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="powerBiDashboard.aspx.cs" Inherits="WAPP_EduCore.pages.Admin.powerBiDashboard" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">

<head runat="server">
    <title>EduCore - Power BI Dashboard</title>

    <!-- Font -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />

    <!-- Our own styles (colors, cards, tables...) -->
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />

    <!-- Small extra styles only this page needs -->
    <style type="text/css">
        /* Title row */
        .title-row {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 24px;
        }

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

        /* ---------- Embedded Power BI iframe Wrapper ---------- */
        .bi-container {
            width: 100%;
            height: 75vh;
            border-radius: 12px;
            overflow: hidden;
            border: 1px solid var(--line);
            background: #ffffff;
        }

        .bi-container iframe {
            width: 100%;
            height: 100%;
            border: none;
        }
    </style>

    <script type="text/javascript">
        // ----- The drop-down menu at the top right -----
        function toggleUserMenu(event) {
            event.stopPropagation();
            document.getElementById('userDropdown').classList.toggle('open');
        }

        document.addEventListener('click', function () {
            document.getElementById('userDropdown').classList.remove('open');
        });
    </script>
</head>

<body>
    <form id="form1" runat="server">
        
         <% Server.Execute("adminNav.aspx", false); %>

        <!-- ==================================================
             PART 2 : EVERYTHING ON THE RIGHT SIDE
             ================================================== -->
        <div class="main">

            <!-- ---------- Top bar ---------- -->
            <div class="topbar">
                <div>
                    <h1>Analytics</h1>
                    <p>Live interactive data visualization powered by Power BI</p>
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

            <!-- ---------- Main Content Area ---------- -->
            <div class="content">

                <div class="card">
                    <div class="title-row">
                        <div>
                            <h2 style="margin:0;">PowerBI real time dashboard</h2>
                            <p class="sub">Direct connection reflecting real-time analytics and user metrics</p>
                        </div>
                    </div>

                    <!-- Embedded Power BI iframe Container -->
                    <div class="bi-container">
                        <iframe title="Educore" width="800" height="486" src="https://app.powerbi.com/view?r=eyJrIjoiMWQ4MTJiOTMtMjIxOC00ZTMwLWFmOWEtMjNmYTc5NWMxY2U1IiwidCI6IjBmZWQwM2EzLTQwMmQtNDYzMy1hOGNkLThiMzA4ODIyMjUzZSIsImMiOjEwfQ%3D%3D&pageName=33a9e0121e8178a65824" frameborder="0" allowFullScreen="true"></iframe>
                    </div>
                </div>

            </div>
        </div>

    </form>
</body>
</html>