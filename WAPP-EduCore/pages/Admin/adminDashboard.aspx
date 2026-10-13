<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="adminDashboard.aspx.cs" Inherits="WAPP_EduCore.pages.Admin.WebForm1" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">

<head runat="server">
    <title>EduCore - Admin Dashboard</title>

    <!-- Font -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />

    <!-- Our own styles (sidebar, cards, colors...) -->
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />

    <!-- ==================================================
         STYLES ONLY THE DASHBOARD NEEDS
         They are here, so you do NOT have to change admin.css.
         ================================================== -->
    <style type="text/css">

        /* ---------- Number boxes with a little icon ---------- */
        .stat-top {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
        }

        .stat-top svg {
            flex-shrink: 0;
            color: var(--blue);
        }

        /* ---------- Card title row  (title on the left, link on the right) ---------- */
        .card-head {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
        }

        .view-link {
            color: var(--blue);
            font-size: 12px;
            text-decoration: none;
        }

        .view-link:hover {
            text-decoration: underline;
        }

        /* ---------- Donut chart (Users by Role) ---------- */
        .donut-row {
            display: flex;
            align-items: center;
            gap: 40px;
            padding-top: 6px;
        }

        .donut {
            position: relative;
            width: 210px;
            height: 210px;
            flex-shrink: 0;
        }

        .donut-center {                    /* the text in the middle of the ring */
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
        }

        .donut-number {
            font-size: 26px;
            font-weight: 600;
        }

        .donut-label {
            font-size: 12px;
            color: var(--grey);
        }

        .legend-item {
            margin-bottom: 14px;
        }

        .legend-name {
            display: flex;
            align-items: center;
            gap: 8px;
            color: var(--grey);
            font-size: 13px;
        }

        .dot {
            display: inline-block;
            width: 9px;
            height: 9px;
            border-radius: 2px;
        }

        .legend-number {
            margin-top: 2px;
            font-size: 20px;
            font-weight: 600;
        }

        .legend-percent {
            margin-left: 6px;
            color: var(--grey);
            font-size: 12px;
            font-weight: 400;
        }

        /* ---------- Column chart (Courses by Topic) ---------- */
        .col-chart {
            display: flex;
            align-items: flex-end;
            gap: 16px;
            height: 190px;
            padding: 0 12px;
            border-bottom: 1px solid var(--line);
        }

        .col-labels {
            display: flex;
            gap: 16px;
            padding: 8px 12px 0 12px;
        }

        .col {
            flex: 1;
            text-align: center;
            color: var(--grey);
            font-size: 12px;
        }

        .col-value {
            margin-bottom: 4px;
            color: var(--text);
            font-size: 13px;
            font-weight: 700;
        }

        .col-bar {
            width: 56px;
            margin: 0 auto;
            border-radius: 6px 6px 0 0;
        }

        /* ---------- Bottom part: Recent Activity (left) + 2 boxes (right) ---------- */
        .dash-bottom {
            display: grid;
            grid-template-columns: 1.15fr 1fr;
            gap: 18px;
            align-items: start;
        }

        .stack {
            display: flex;
            flex-direction: column;
            gap: 18px;
        }

        /* ---------- Recent Activity ---------- */
        .activity-item {
            display: flex;
            align-items: flex-start;
            gap: 14px;
            padding: 14px 0;
            border-top: 1px solid var(--line);
        }

        .activity-icon {
            display: flex;
            align-items: center;
            justify-content: center;
            flex-shrink: 0;
            width: 36px;
            height: 36px;
            background: #EEF2FF;
            border-radius: 8px;
            color: var(--blue);
        }

        .activity-main {
            flex: 1;
        }

        .activity-name {
            font-size: 13px;
            font-weight: 600;
        }

        .activity-text,
        .activity-date {
            margin-top: 3px;
            color: var(--grey);
            font-size: 12px;
        }

        .activity-date {
            font-size: 11px;
        }

        .activity-kind {
            color: var(--grey);
            font-size: 11px;
            text-align: right;
        }

        .activity-footer {
            padding-top: 14px;
            border-top: 1px solid var(--line);
            font-size: 12px;
        }

        /* ---------- Needs Attention (yellow box) ---------- */
        .attention {
            padding: 20px;
            background: #FFF8EA;
            border: 1px solid var(--orange);
            border-radius: 12px;
        }

        .attention-title {
            display: flex;
            align-items: center;
            gap: 10px;
            margin-bottom: 8px;
            color: #6B4A10;
            font-size: 16px;
            font-weight: 600;
        }

        .attention-row {
            display: flex;
            align-items: center;
            gap: 16px;
            padding: 12px 0;
            border-top: 1px solid #F0DDB5;
        }

        .attention-number {
            width: 44px;
            color: #92600A;
            font-size: 28px;
            font-weight: 600;
        }

        .attention-text {
            flex: 1;
        }

        .attention-name {
            font-size: 13px;
            font-weight: 600;
        }

        .attention-sub {
            margin-top: 3px;
            color: #92600A;
            font-size: 11px;
        }

        .attention-btn {
            padding: 7px 12px;
            background: white;
            border: 1px solid var(--orange);
            border-radius: 8px;
            color: #92600A;
            font-size: 12px;
            text-decoration: none;
        }

        .attention-btn:hover {
            background: #FFF1D6;
        }

        /* ---------- Quick Actions ---------- */
        .qa-row {
            display: flex;
            align-items: center;
            gap: 12px;
            margin-bottom: 10px;
            padding: 12px 14px;
            background: #EEF2FF;
            border-radius: 8px;
            color: var(--blue);
            font-size: 13px;
            text-decoration: none;
        }

        .qa-row:hover {
            background: #E3E9FC;
        }

        .qa-name {
            width: 150px;
            font-weight: 500;
        }

        .qa-desc {
            flex: 1;
            color: var(--grey);
            font-size: 11px;
        }
    </style>
</head>

<body>
    <form id="form1" runat="server">


        <!-- ==================================================
             PART 1 : SIDEBAR (dark menu on the left)
             ================================================== -->
        <div class="sidebar">
            <img src="../../images/logo.png" alt="EduCore" class="logo" />

            <a class="menu-item active" href="adminDashboard.aspx">Dashboard</a>
            <a class="menu-item" href="userManagement.aspx">Users</a>
            <a class="menu-item" href="coursesManagement.aspx">Courses</a>
            <a class="menu-item" href="adminReports.aspx">Reports</a>

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
                    <h1>Admin Dashboard</h1>
                    <p>Platform overview</p>
                </div>
                <div>
                    <span class="avatar"><%= AdminInitials %></span>
                </div>
            </div>

            <div class="content">


                <!-- ==============================================
                     ROW 1 : SIX NUMBER BOXES
                     ============================================== -->
                <div class="stat-row six">

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Total Users</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblTotalUsers" runat="server"></asp:Label></div>
                        <div class="stat-note">All registered accounts</div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">New This Month</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 00-4-4H6a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><line x1="20" y1="8" x2="20" y2="14" /><line x1="17" y1="11" x2="23" y2="11" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblNewUsers" runat="server"></asp:Label></div>
                        <div class="stat-note">Joined this month</div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Total Courses</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z" /><path d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblTotalCourses" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblCoursesNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Total Lessons</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="12 2 2 7 12 12 22 7 12 2" /><polyline points="2 17 12 22 22 17" /><polyline points="2 12 12 17 22 12" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblLessons" runat="server"></asp:Label></div>
                        <div class="stat-note">Across all courses</div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Topics</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7" /><rect x="14" y="3" width="7" height="7" /><rect x="14" y="14" width="7" height="7" /><rect x="3" y="14" width="7" height="7" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblTopics" runat="server"></asp:Label></div>
                        <div class="stat-note">Different categories</div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Administrators</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblAdmins" runat="server"></asp:Label></div>
                        <div class="stat-note">Platform administrators</div>
                    </div>

                </div>


                <!-- ==============================================
                     ROW 2 : TWO CHARTS SIDE BY SIDE
                     ============================================== -->
                <div class="two-cols">

                    <!-- Chart 1 : donut (Users by Role) -->
                    <div class="card">
                        <div class="card-head">
                            <div>
                                <h3>Users by Role</h3>
                                <p class="sub">Account composition across the platform</p>
                            </div>
                            <a class="view-link" href="adminReports.aspx">View report →</a>
                        </div>

                        <div class="donut-row">
                            <asp:Literal ID="litDonut" runat="server"></asp:Literal>
                            <div>
                                <asp:Literal ID="litLegend" runat="server"></asp:Literal>
                            </div>
                        </div>
                    </div>

                    <!-- Chart 2 : columns (Courses by Topic) -->
                    <div class="card">
                        <div class="card-head">
                            <div>
                                <h3>Courses by Topic</h3>
                                <p class="sub"><asp:Label ID="lblTopicSubtitle" runat="server"></asp:Label></p>
                            </div>
                            <a class="view-link" href="adminReports.aspx">View report →</a>
                        </div>

                        <asp:Literal ID="litTopicChart" runat="server"></asp:Literal>
                    </div>

                </div>


                <!-- ==============================================
                     ROW 3 : RECENT ACTIVITY (left)  +  2 BOXES (right)
                     ============================================== -->
                <div class="dash-bottom">

                    <!-- ----- Recent Activity ----- -->
                    <div class="card">
                        <div class="card-head">
                            <div>
                                <h3>Recent Activity</h3>
                                <p class="sub">Latest five platform events</p>
                            </div>
                            <a class="view-link" href="userManagement.aspx">View all →</a>
                        </div>

                        <asp:Literal ID="litActivity" runat="server"></asp:Literal>

                        <div class="activity-footer">
                            <a class="view-link" href="userManagement.aspx">View all users</a>
                        </div>
                    </div>

                    <!-- ----- Right side : Needs Attention + Quick Actions ----- -->
                    <div class="stack">

                        <!-- Yellow box -->
                        <div class="attention">
                            <div class="attention-title">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" /><line x1="12" y1="9" x2="12" y2="13" /><line x1="12" y1="17" x2="12.01" y2="17" /></svg>
                                Needs Attention
                            </div>

                            <div class="attention-row">
                                <div class="attention-number"><asp:Label ID="lblNoLessons" runat="server"></asp:Label></div>
                                <div class="attention-text">
                                    <div class="attention-name">Courses without lessons</div>
                                    <div class="attention-sub">Add lessons so students can learn</div>
                                </div>
                                <a class="attention-btn" href="coursesManagement.aspx">Review courses</a>
                            </div>

                            <div class="attention-row">
                                <div class="attention-number"><asp:Label ID="lblNoImage" runat="server"></asp:Label></div>
                                <div class="attention-text">
                                    <div class="attention-name">Courses without an image</div>
                                    <div class="attention-sub">Courses missing a cover picture</div>
                                </div>
                                <a class="attention-btn" href="coursesManagement.aspx">Review courses</a>
                            </div>

                            <div class="attention-row">
                                <div class="attention-number"><asp:Label ID="lblIncomplete" runat="server"></asp:Label></div>
                                <div class="attention-text">
                                    <div class="attention-name">Incomplete profiles</div>
                                    <div class="attention-sub">Users missing date of birth or gender</div>
                                </div>
                                <a class="attention-btn" href="userManagement.aspx">Review users</a>
                            </div>
                        </div>

                        <!-- Quick Actions -->
                        <div class="card">
                            <h3>Quick Actions</h3>
                            <p class="sub">Everyday administration, one step away</p>

                            <a class="qa-row" href="coursesManagement.aspx">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z" /><path d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z" /></svg>
                                <span class="qa-name">Review Courses</span>
                                <span class="qa-desc">Check and update the course catalogue</span>
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12" /><polyline points="12 5 19 12 12 19" /></svg>
                            </a>

                            <a class="qa-row" href="userManagement.aspx">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" /></svg>
                                <span class="qa-name">Manage Users</span>
                                <span class="qa-desc">Maintain access and account roles</span>
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12" /><polyline points="12 5 19 12 12 19" /></svg>
                            </a>

                            <a class="qa-row" href="adminReports.aspx">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10" /><line x1="12" y1="20" x2="12" y2="4" /><line x1="6" y1="20" x2="6" y2="14" /></svg>
                                <span class="qa-name">View Reports</span>
                                <span class="qa-desc">See user, course and learning numbers</span>
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12" /><polyline points="12 5 19 12 12 19" /></svg>
                            </a>
                        </div>

                    </div>
                </div>

            </div>
        </div>

    </form>
</body>
</html>
