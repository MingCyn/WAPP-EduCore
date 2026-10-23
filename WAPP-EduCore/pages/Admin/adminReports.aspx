<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="adminReports.aspx.cs" Inherits="WAPP_EduCore.pages.Admin.adminReports" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">

<head runat="server">
    <title>EduCore - Reports</title>

    <!-- Font -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />

    <!-- Our own styles (colors, cards, tables...) -->
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />

    <!-- Small extra styles only this page needs (so admin.css does not change) -->
    <style type="text/css">

        /* The label on the left and a little icon on the right (top of every number box) */
        .stat-top {
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .stat-icon {
            width: 16px;
            height: 16px;
            fill: none;
            stroke: currentColor;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
            color: var(--blue);
        }

        .stat-icon.brown {
            color: #B45309;
        }

        /* Title on the left, "Snapshot" date on the right */
        .title-row {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
        }

        /* "Top 5 · Highest enrolment" on the left, "Ranked from..." on the right */
        .mini-row {
            display: flex;
            justify-content: space-between;
            align-items: baseline;
        }

        .mini-row .note-right {
            color: var(--grey);
            font-size: 12px;
        }

        /* ---------- Sidebar: a little icon before every menu name ---------- */
        .sidebar a.menu-item {
            display: flex;
            align-items: center;
            gap: 12px;                  /* space between the icon and the name */
        }

        .menu-icon {
            width: 18px;
            height: 18px;
            flex-shrink: 0;
            fill: none;
            stroke: currentColor;       /* the icon has the same color as the name */
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
            display: none;                 /* hidden until JavaScript adds "open" */
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

    <script type="text/javascript">
// ----- The drop-down menu at the top right -----
function toggleUserMenu(event) {
    event.stopPropagation();   // so the click does not reach "document" below
    document.getElementById('userDropdown').classList.toggle('open');
}

document.addEventListener('click', function () {
    document.getElementById('userDropdown').classList.remove('open');
});
</script>
</head>

<body>
    <form id="form1" runat="server">


        <!-- ==================================================
             PART 1 : SIDEBAR (dark menu on the left)
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
            <a class="menu-item active" href="adminReports.aspx">
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
                    <h1>Reports</h1>
                    <p>Review platform data by function</p>
                </div>
                <div class="user-menu">
                    <button type="button" class="user-btn" onclick="toggleUserMenu(event)" title="Account">
                        <span class="avatar"><%= AdminInitials %></span>
                    </button>

                    <!-- The 2 options -->
                    <div class="user-dropdown" id="userDropdown">
                        <a href="adminSettings.aspx">Manage Account</a>
                        <asp:LinkButton ID="lnkLogout" runat="server" CssClass="logout" CausesValidation="false" OnClick="lnkLogout_Click">Logout</asp:LinkButton>
                    </div>
                </div>
            </div>

            <div class="content">

                <!-- ---------- The 3 tabs ---------- -->
                <div class="tabs">
                    <asp:LinkButton ID="lnkUsers" runat="server" CommandArgument="users" OnClick="Tab_Click">Users</asp:LinkButton>
                    <asp:LinkButton ID="lnkCourses" runat="server" CommandArgument="courses" OnClick="Tab_Click">Courses</asp:LinkButton>
                    <asp:LinkButton ID="lnkLearning" runat="server" CommandArgument="learning" OnClick="Tab_Click">Learning Performance</asp:LinkButton>
                </div>


                <!-- ==============================================
                     TAB 1 : USERS
                     ============================================== -->
                <asp:Panel ID="pnlUsers" runat="server">

                    <!-- Title -->
                    <div class="title-row">
                        <div>
                            <h2 style="margin:0;">User analytics</h2>
                            <p class="sub">Account growth, role composition and engagement</p>
                        </div>
                        <asp:Label ID="lblSnapshot" runat="server" CssClass="note"></asp:Label>
                    </div>

                    <!-- 4 number boxes -->
                    <div class="stat-row four">

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Total users</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 00-3-3.87" /><path d="M16 3.13a4 4 0 010 7.75" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblTotalUsers" runat="server"></asp:Label></div>
                            <div class="stat-note">All registered accounts</div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Active users</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblActiveUsers" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblActiveNote" runat="server"></asp:Label></div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>New registrations</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="8.5" cy="7" r="4" /><line x1="20" y1="8" x2="20" y2="14" /><line x1="23" y1="11" x2="17" y2="11" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblNewUsers" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblNewNote" runat="server"></asp:Label></div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Inactive users</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="8.5" cy="7" r="4" /><line x1="23" y1="11" x2="17" y2="11" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblInactiveUsers" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblInactiveNote" runat="server"></asp:Label></div>
                        </div>

                    </div>

                    <!-- 2 charts side by side -->
                    <div class="two-cols">

                        <div class="card">
                            <h3>Registrations over time</h3>
                            <p class="sub"><asp:Label ID="lblRegSub" runat="server"></asp:Label></p>
                            <asp:Literal ID="litLineChart" runat="server"></asp:Literal>
                            <asp:Literal ID="litRegNote" runat="server"></asp:Literal>
                        </div>

                        <div class="card">
                            <h3>Users by role</h3>
                            <p class="sub">All <asp:Label ID="lblRoleSub" runat="server"></asp:Label> accounts · counts by assigned role</p>
                            <asp:Literal ID="litRoleBars" runat="server"></asp:Literal>
                        </div>

                    </div>

                    <!-- Role table -->
                    <div class="card">
                        <h3>Role table</h3>
                        <p class="sub">Account totals and activity by role</p>

                        <div class="table-wrap">
                            <table class="data">
                                <tr>
                                    <th>Role</th>
                                    <th>Total users</th>
                                    <th>Share of users</th>
                                    <th>Active users</th>
                                    <th>Inactive users</th>
                                    <th>Activity rate</th>
                                </tr>
                                <asp:Literal ID="litRoleTable" runat="server"></asp:Literal>
                            </table>
                        </div>

                        <div class="note">Active users did something in the last 30 days (logged in, enrolled, finished a lesson, took a quiz, posted, replied, sent a message, or created their account). The admin who is viewing this page counts as active. Roles are mutually exclusive; totals include inactive accounts.</div>
                    </div>

                </asp:Panel>


                <!-- ==============================================
                     TAB 2 : COURSES
                     ============================================== -->
                <asp:Panel ID="pnlCourses" runat="server">

                    <!-- Title -->
                    <div class="title-row">
                        <div>
                            <h2 style="margin:0;">Course analytics</h2>
                            <p class="sub">Subjects, lessons and enrolment across courses</p>
                        </div>
                        <asp:Label ID="lblSnapshotCourses" runat="server" CssClass="note"></asp:Label>
                    </div>

                    <!-- 4 number boxes -->
                    <div class="stat-row four">

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Total courses</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z" /><path d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblTotalCourses" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblTotalNote" runat="server"></asp:Label></div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Biology</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M11 20A7 7 0 019.8 6.1C15.5 5 17 4.48 19 2c1 2 2 4.18 2 8 0 5.5-4.78 10-10 10z" /><path d="M2 21c0-3 1.85-5.36 5.08-6C9.5 14.52 12 13 13 12" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblBiology" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblBiologyNote" runat="server"></asp:Label></div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Chemistry</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M10 2v7.527a2 2 0 01-.211.896L4.72 20.55a1 1 0 00.9 1.45h12.76a1 1 0 00.9-1.45l-5.069-10.127A2 2 0 0114 9.527V2" /><path d="M8.5 2h7" /><path d="M7 16h10" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblChemistry" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblChemistryNote" runat="server"></asp:Label></div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Physics</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><circle cx="12" cy="12" r="1" /><path d="M20.2 20.2c2.04-2.03.02-7.36-4.5-11.9-4.54-4.52-9.87-6.54-11.9-4.5-2.04 2.03-.02 7.36 4.5 11.9 4.54 4.52 9.87 6.54 11.9 4.5z" /><path d="M15.7 15.7c4.52-4.54 6.54-9.87 4.5-11.9-2.03-2.04-7.36-.02-11.9 4.5-4.52 4.54-6.54 9.87-4.5 11.9 2.03 2.04 7.36.02 11.9-4.5z" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblPhysics" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblPhysicsNote" runat="server"></asp:Label></div>
                        </div>

                    </div>

                    <!-- Status chart + Enrolment per course -->
                    <div class="two-cols">

                        <div class="card">
                            <h3>Lessons by subject</h3>
                            <p class="sub">Number of lessons in Biology, Chemistry and Physics courses</p>
                            <asp:Literal ID="litSubjectLessons" runat="server"></asp:Literal>
                        </div>

                        <div class="card">
                            <h3>Enrolment per course</h3>
                            <p class="sub">Five highest-enrolment published courses</p>
                            <asp:Literal ID="litEnrolBars" runat="server"></asp:Literal>
                        </div>

                    </div>

                    <!-- 2 more charts side by side -->
                    <div class="two-cols">

                        <div class="card">
                            <h3>Which subject has the most courses?</h3>
                            <p class="sub">Number of courses in each topic (top 6)</p>
                            <asp:Literal ID="litTopicBars" runat="server"></asp:Literal>
                        </div>

                        <div class="card">
                            <h3>Lessons per course</h3>
                            <p class="sub">Five courses with the most lessons</p>
                            <asp:Literal ID="litLessonBars" runat="server"></asp:Literal>
                        </div>

                    </div>

                    <!-- Top 5 and Lowest 5 tables -->
                    <div class="card">
                        <h3>Top / lowest 5 courses</h3>
                        <p class="sub">Enrolment ranking · published courses only</p>

                        <div class="mini-row">
                            <div class="mini-title blue">Top 5 · Highest enrolment</div>
                            <span class="note-right">Ranked from highest to lowest</span>
                        </div>
                        <div class="table-wrap">
                            <table class="data">
                                <tr>
                                    <th>Rank</th>
                                    <th>Course title</th>
                                    <th>Instructor</th>
                                    <th>Level</th>
                                    <th>Topic</th>
                                    <th>Status</th>
                                    <th>Enrolments</th>
                                </tr>
                                <asp:Literal ID="litTopCourses" runat="server"></asp:Literal>
                            </table>
                        </div>

                        <div class="mini-row">
                            <div class="mini-title brown">Lowest 5 · Lowest enrolment</div>
                            <span class="note-right">Ranked from lowest to highest</span>
                        </div>
                        <div class="table-wrap">
                            <table class="data">
                                <tr>
                                    <th>Rank</th>
                                    <th>Course title</th>
                                    <th>Instructor</th>
                                    <th>Level</th>
                                    <th>Topic</th>
                                    <th>Status</th>
                                    <th>Enrolments</th>
                                </tr>
                                <asp:Literal ID="litLowCourses" runat="server"></asp:Literal>
                            </table>
                        </div>

                        <div class="note"><asp:Label ID="lblRankNote" runat="server"></asp:Label></div>
                    </div>

                </asp:Panel>


                <!-- ==============================================
                     TAB 3 : LEARNING PERFORMANCE
                     ============================================== -->
                <asp:Panel ID="pnlLearning" runat="server">

                    <!-- Title -->
                    <div class="title-row">
                        <div>
                            <h2 style="margin:0;">Learning performance</h2>
                            <p class="sub">Completion and quiz outcomes · five-course reporting cohort</p>
                        </div>
                        <asp:Label ID="lblSnapshotLearning" runat="server" CssClass="note"></asp:Label>
                    </div>

                    <!-- 4 number boxes -->
                    <div class="stat-row four">

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Cohort enrolments</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M22 10L12 5 2 10l10 5 10-5z" /><path d="M6 12v5c3 3 9 3 12 0v-5" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblEnrol" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblEnrolNote" runat="server"></asp:Label></div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Course completions</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" /><polyline points="22 4 12 14.01 9 11.01" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblDone" runat="server"></asp:Label></div>
                            <div class="stat-note">Enrolments marked complete</div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Completion rate</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" /><polyline points="8 12 11 15 16 9" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblRate" runat="server"></asp:Label></div>
                            <div class="stat-note">Completions ÷ enrolments</div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Average quiz score</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M16 4h2a2 2 0 012 2v14a2 2 0 01-2 2H6a2 2 0 01-2-2V6a2 2 0 012-2h2" /><rect x="8" y="2" width="8" height="4" rx="1" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblQuiz" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblQuizNote" runat="server"></asp:Label></div>
                        </div>

                    </div>

                    <!-- 2 charts side by side -->
                    <div class="two-cols">

                        <div class="card">
                            <h3>Completion rate by course</h3>
                            <p class="sub">Share of enrolled learners who completed the course</p>
                            <asp:Literal ID="litCompletionBars" runat="server"></asp:Literal>
                        </div>

                        <div class="card">
                            <h3>Quiz score by topic</h3>
                            <p class="sub">Average scored quiz results by topic</p>
                            <asp:Literal ID="litQuizBars" runat="server"></asp:Literal>
                        </div>

                    </div>

                    <!-- Performance table -->
                    <div class="card" style="margin-bottom:18px;">
                        <h3>Performance table</h3>
                        <p class="sub">Course-level learning outcomes for the reporting cohort</p>

                        <div class="table-wrap">
                            <table class="data">
                                <tr>
                                    <th>Course</th>
                                    <th>Topic</th>
                                    <th>Enrolments</th>
                                    <th>Completions</th>
                                    <th>Completion rate</th>
                                    <th>Avg. quiz score</th>
                                </tr>
                                <asp:Literal ID="litPerfTable" runat="server"></asp:Literal>
                            </table>
                        </div>

                        <div class="note">Completion rates are enrolment-weighted; the quiz summary is the mean of topic scores.</div>
                    </div>

                    <!-- Yellow tip box -->
                    <div class="focus-box">
                        <asp:Literal ID="litFocus" runat="server"></asp:Literal>
                    </div>

                </asp:Panel>

            </div>
        </div>

    </form>
</body>
</html>
