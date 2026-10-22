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
    </style>
</head>

<body>
    <form id="form1" runat="server">


        <!-- ==================================================
             PART 1 : SIDEBAR (dark menu on the left)
             ================================================== -->
        <div class="sidebar">
            <img src="../../images/logo.png" alt="EduCore" class="logo" />

            <a class="menu-item" href="adminDashboard.aspx">Dashboard</a>
            <a class="menu-item" href="userManagement.aspx">Users</a>
            <a class="menu-item" href="coursesManagement.aspx">Courses</a>
            <a class="menu-item active" href="adminReports.aspx">Reports</a>

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
                <div>
                    <span class="badge-admin">Administrator access only</span>
                    <span class="avatar"><%= AdminInitials %></span>
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

                        <div class="note">Active users did something in the last 30 days (enrolled, finished a lesson, took a quiz, posted, replied or sent a message). Roles are mutually exclusive; totals include inactive accounts.</div>
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
                            <p class="sub">Catalogue status and enrolment across courses</p>
                        </div>
                        <asp:Label ID="lblSnapshotCourses" runat="server" CssClass="note"></asp:Label>
                    </div>

                    <!-- 5 number boxes -->
                    <div class="stat-row five">

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
                                <span>Published</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" /><polyline points="22 4 12 14.01 9 11.01" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblPublished" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblPublishedNote" runat="server"></asp:Label></div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Pending review</span>
                                <svg class="stat-icon brown" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" /><polyline points="12 6 12 12 16 14" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblPending" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblPendingNote" runat="server"></asp:Label></div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Draft</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" /><polyline points="14 2 14 8 20 8" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblDraft" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblDraftNote" runat="server"></asp:Label></div>
                        </div>

                        <div class="card">
                            <div class="stat-label stat-top">
                                <span>Archived</span>
                                <svg class="stat-icon" viewBox="0 0 24 24"><polyline points="21 8 21 21 3 21 3 8" /><rect x="1" y="3" width="22" height="5" /><line x1="10" y1="12" x2="14" y2="12" /></svg>
                            </div>
                            <div class="stat-number"><asp:Label ID="lblArchived" runat="server"></asp:Label></div>
                            <div class="stat-note"><asp:Label ID="lblArchivedNote" runat="server"></asp:Label></div>
                        </div>

                    </div>

                    <!-- Status chart + Enrolment per course -->
                    <div class="two-cols">

                        <div class="card">
                            <h3>Status chart</h3>
                            <p class="sub">All courses by publication status</p>
                            <asp:Literal ID="litStatusChart" runat="server"></asp:Literal>
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
