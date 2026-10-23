<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="studentManagement.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.WebForm4" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Student Management</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />

    <%-- The same style files as the other tutor pages --%>
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
    <%-- courseBuilder.css supplies the shared nav tweaks, page header and .btn styles --%>
    <link href="../../Content/courseBuilder.css?v=4" rel="stylesheet" type="text/css" />

    <%-- A few small extras that only this page needs (so the shared css files do not change) --%>
    <style type="text/css">

        /* This page has 3 number boxes (the shared grid is made for 4) */
        .stats-overview-grid.sm-stats { grid-template-columns: repeat(3, 1fr); }

        /* The "At Risk" number box gets an orange border (like the Figma) */
        .stat-card.sm-risk { border: 1.5px solid #E5A94B; }

        /* ---------- Search + filters ---------- */
        .sm-toolbar {
            display: flex;
            flex-wrap: wrap;
            gap: 12px;
            padding: 16px;
            margin-bottom: 20px;
            background: #fff;
            border: 1px solid #e1e4ea;
            border-radius: 14px;
        }

        .sm-input {
            height: 36px;
            padding: 0 12px;
            background: #fff;
            color: #0f1b4c;
            border: 1px solid #d5d9e2;
            border-radius: 8px;
            font-family: inherit;
            font-size: 13px;
        }

        .sm-search { flex: 1; min-width: 200px; max-width: 300px; }

        /* The dropdowns: the little arrow is drawn here so it is always shown */
        select.sm-input {
            appearance: none;
            -webkit-appearance: none;
            padding-right: 34px;
            background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%230F1B4C' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'/%3E%3C/svg%3E");
            background-repeat: no-repeat;
            background-position: right 12px center;
            background-size: 12px;
        }

        /* ---------- Student name with the round initials ---------- */
        .sm-student { display: flex; align-items: center; gap: 12px; }

        .sm-student a { color: inherit; text-decoration: none; }
        .sm-student a:hover { text-decoration: underline; }

        .sm-avatar {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 32px;
            height: 32px;
            flex-shrink: 0;
            background: #dce3f7;
            color: #0f1b4c;
            border-radius: 50%;
            font-size: 12px;
            font-weight: 700;
        }

        /* ---------- Under the table: "Showing 8 of 56" + Previous / Next ---------- */
        .sm-footer {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 16px;
            border-top: 1px solid #e1e4ea;
            color: #64748b;
            font-size: 12px;
        }

        .sm-pager { display: flex; align-items: center; gap: 10px; color: #0f1b4c; font-weight: 600; }

        .sm-empty { padding: 50px 20px; text-align: center; color: #64748b; }

        /* ---------- Right side boxes ---------- */
        .sm-box {
            padding: 24px;
            margin-bottom: 24px;
            background: #fff;
            border: 1px solid #e1e4ea;
            border-radius: 14px;
        }

        .sm-box .section-heading { margin-top: 0; }

        .sm-donut-row { display: flex; align-items: center; gap: 22px; }
        .sm-legend { display: grid; gap: 10px; font-size: 12px; }
        .sm-legend .dot { display: inline-block; width: 8px; height: 8px; margin-right: 8px; border-radius: 50%; }

        .sm-box .btn { width: 100%; justify-content: center; text-align: center; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">

            <%-- Shared nav: same include as the other tutor pages --%>
            <% Server.Execute("tutorNav.aspx", false); %>

            <main class="dashboard-container">

                <!-- Page header -->
                <div class="page-header">
                    <div>
                        <h1 class="page-title">Student Management</h1>
                        <p class="page-subtitle"><asp:Label ID="lblCourseLine" runat="server"></asp:Label></p>
                    </div>
                </div>

                <!-- ==============================================
                     3 NUMBER BOXES
                     ============================================== -->
                <div class="stats-overview-grid sm-stats">
                    <div class="stat-card">
                        <span class="stat-card-title">Class Average Score</span>
                        <h2 class="stat-card-value"><asp:Label ID="lblAvg" runat="server"></asp:Label></h2>
                        <span class="stat-trend neutral"><asp:Label ID="lblAvgNote" runat="server"></asp:Label></span>
                    </div>
                    <div class="stat-card">
                        <span class="stat-card-title">Completion Rate</span>
                        <h2 class="stat-card-value"><asp:Label ID="lblRate" runat="server"></asp:Label></h2>
                        <span class="stat-trend neutral"><asp:Label ID="lblRateNote" runat="server"></asp:Label></span>
                    </div>
                    <div class="stat-card sm-risk">
                        <span class="stat-card-title">At Risk Students</span>
                        <h2 class="stat-card-value"><asp:Label ID="lblRisk" runat="server"></asp:Label></h2>
                        <span class="stat-trend neutral">Needs urgent feedback</span>
                    </div>
                </div>

                <!-- Left: search + table.   Right: quick actions + chart -->
                <div class="content-split-grid">

                    <!-- ============ LEFT ============ -->
                    <div class="left-column">

                        <div class="sm-toolbar">
                            <%-- Type a name and press ENTER --%>
                            <asp:TextBox ID="txtSearch" runat="server" CssClass="sm-input sm-search" placeholder="Search students by name..." AutoPostBack="true" OnTextChanged="Filter_Changed"></asp:TextBox>

                            <asp:DropDownList ID="ddlStatus" runat="server" CssClass="sm-input" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                                <asp:ListItem Text="Status: All" Value=""></asp:ListItem>
                                <asp:ListItem Text="On Track" Value="On Track"></asp:ListItem>
                                <asp:ListItem Text="Needs Attention" Value="Needs Attention"></asp:ListItem>
                                <asp:ListItem Text="At Risk" Value="At Risk"></asp:ListItem>
                            </asp:DropDownList>

                            <asp:DropDownList ID="ddlSort" runat="server" CssClass="sm-input" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                                <asp:ListItem Text="Sort: Progress" Value="progress"></asp:ListItem>
                                <asp:ListItem Text="Sort: Score" Value="score"></asp:ListItem>
                                <asp:ListItem Text="Sort: Name" Value="name"></asp:ListItem>
                            </asp:DropDownList>
                        </div>

                        <asp:Panel ID="pnlTable" runat="server">
                            <div class="table-card">
                                <table class="data-table">
                                    <thead>
                                        <tr>
                                            <th>STUDENT NAME</th>
                                            <th>COURSE</th>
                                            <th>PROGRESS</th>
                                            <th>SCORE</th>
                                            <th>STATUS</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <%-- C# fills the rows here --%>
                                        <asp:Literal ID="litRows" runat="server" EnableViewState="false"></asp:Literal>
                                    </tbody>
                                </table>

                                <div class="sm-footer">
                                    <asp:Label ID="lblShowing" runat="server"></asp:Label>
                                    <div class="sm-pager">
                                        <asp:Button ID="btnPrev" runat="server" Text="Previous" CssClass="btn btn-outline btn-sm" OnClick="btnPrev_Click" />
                                        <asp:Label ID="lblPage" runat="server"></asp:Label>
                                        <asp:Button ID="btnNext" runat="server" Text="Next" CssClass="btn btn-outline btn-sm" OnClick="btnNext_Click" />
                                    </div>
                                </div>
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
                            <div class="table-card">
                                <div class="sm-empty">No students found. Try another name or change the filters.</div>
                            </div>
                        </asp:Panel>

                    </div>

                    <!-- ============ RIGHT ============ -->
                    <div class="right-column">

                        <div class="sm-box">
                            <h3 class="section-heading">Quick Actions</h3>
                            <%-- Downloads the whole class as a .csv file (Excel opens it) --%>
                            <asp:Button ID="btnExport" runat="server" Text="Export Class Report" CssClass="btn btn-primary" OnClick="btnExport_Click" />
                        </div>

                        <div class="sm-box">
                            <h3 class="section-heading">Class Distribution</h3>
                            <div class="sm-donut-row">
                                <asp:Literal ID="litDonut" runat="server"></asp:Literal>
                                <div class="sm-legend">
                                    <asp:Literal ID="litLegend" runat="server"></asp:Literal>
                                </div>
                            </div>
                        </div>

                    </div>

                </div>

            </main>
        </div>
    </form>

    <%-- Highlight "Students" in the shared nav
         (the nav link is called students.aspx now; both names work) --%>
    <script>
        var navLink = document.querySelector('.nav-link[href="studentManagement.aspx"], .nav-link[href="students.aspx"]');
        if (navLink) navLink.classList.add('active');
    </script>
</body>
</html>
