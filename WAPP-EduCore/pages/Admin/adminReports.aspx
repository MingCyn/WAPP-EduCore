<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="adminReports.aspx.cs" Inherits="WAPP_EduCore.pages.Admin.adminReports" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">

<head runat="server">
    <title>EduCore - Reports</title>

    <!-- Font -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />

    <!-- Our own styles (colors, cards, tables...) -->
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
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
                    <div style="display:flex; justify-content:space-between;">
                        <div>
                            <h2 style="margin:0;">User analytics</h2>
                            <p class="sub">Account growth and role composition</p>
                        </div>
                        <asp:Label ID="lblSnapshot" runat="server" CssClass="note"></asp:Label>
                    </div>

                    <!-- 4 number boxes -->
                    <div class="stat-row four">

                        <div class="card">
                            <div class="stat-label">Total users</div>
                            <div class="stat-number"><asp:Label ID="lblTotalUsers" runat="server"></asp:Label></div>
                            <div class="stat-note">All registered accounts</div>
                        </div>

                        <div class="card">
                            <div class="stat-label">Students</div>
                            <div class="stat-number"><asp:Label ID="lblStudents" runat="server"></asp:Label></div>
                            <div class="stat-note">Learners on the platform</div>
                        </div>

                        <div class="card">
                            <div class="stat-label">Tutors</div>
                            <div class="stat-number"><asp:Label ID="lblTutors" runat="server"></asp:Label></div>
                            <div class="stat-note">Teachers on the platform</div>
                        </div>

                        <div class="card">
                            <div class="stat-label">New registrations</div>
                            <div class="stat-number"><asp:Label ID="lblNewUsers" runat="server"></asp:Label></div>
                            <div class="stat-note">Joined this month</div>
                        </div>

                    </div>

                    <!-- 2 charts side by side -->
                    <div class="two-cols">

                        <div class="card">
                            <h3>Registrations over time</h3>
                            <p class="sub">New accounts per month (last 6 months)</p>
                            <asp:Literal ID="litLineChart" runat="server"></asp:Literal>
                        </div>

                        <div class="card">
                            <h3>Users by role</h3>
                            <p class="sub">Counts by assigned role</p>
                            <asp:Literal ID="litRoleBars" runat="server"></asp:Literal>
                        </div>

                    </div>

                    <!-- Role table -->
                    <div class="card">
                        <h3>Role table</h3>
                        <p class="sub">Account totals by role</p>

                        <div class="table-wrap">
                            <table class="data">
                                <tr>
                                    <th>Role</th>
                                    <th>Total users</th>
                                    <th>Share of users</th>
                                </tr>
                                <asp:Literal ID="litRoleTable" runat="server"></asp:Literal>
                            </table>
                        </div>

                        <div class="note">Roles are mutually exclusive.</div>
                    </div>

                </asp:Panel>


                <!-- ==============================================
                     TAB 2 : COURSES
                     ============================================== -->
                <asp:Panel ID="pnlCourses" runat="server">

                    <!-- Title -->
                    <h2 style="margin:0;">Course analytics</h2>
                    <p class="sub">Catalogue size and lesson content across courses</p>

                    <!-- 4 number boxes -->
                    <div class="stat-row four">

                        <div class="card">
                            <div class="stat-label">Total courses</div>
                            <div class="stat-number"><asp:Label ID="lblTotalCourses" runat="server"></asp:Label></div>
                            <div class="stat-note">In the catalogue</div>
                        </div>

                        <div class="card">
                            <div class="stat-label">Topics</div>
                            <div class="stat-number"><asp:Label ID="lblTopics" runat="server"></asp:Label></div>
                            <div class="stat-note">Different categories</div>
                        </div>

                        <div class="card">
                            <div class="stat-label">Total lessons</div>
                            <div class="stat-number"><asp:Label ID="lblLessons" runat="server"></asp:Label></div>
                            <div class="stat-note">Across all courses</div>
                        </div>

                        <div class="card">
                            <div class="stat-label">New courses</div>
                            <div class="stat-number"><asp:Label ID="lblNewCourses" runat="server"></asp:Label></div>
                            <div class="stat-note">Added this month</div>
                        </div>

                    </div>

                    <!-- 2 charts side by side -->
                    <div class="two-cols">

                        <div class="card">
                            <h3>Courses per topic</h3>
                            <p class="sub">How many courses each topic has</p>
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
                        <p class="sub">Ranked by number of lessons</p>

                        <div class="mini-title blue">Top 5 · Most lessons</div>
                        <div class="table-wrap">
                            <table class="data">
                                <tr>
                                    <th>Rank</th>
                                    <th>Course title</th>
                                    <th>Topic</th>
                                    <th>Lessons</th>
                                </tr>
                                <asp:Literal ID="litTopCourses" runat="server"></asp:Literal>
                            </table>
                        </div>

                        <div class="mini-title brown">Lowest 5 · Fewest lessons</div>
                        <div class="table-wrap">
                            <table class="data">
                                <tr>
                                    <th>Rank</th>
                                    <th>Course title</th>
                                    <th>Topic</th>
                                    <th>Lessons</th>
                                </tr>
                                <asp:Literal ID="litLowCourses" runat="server"></asp:Literal>
                            </table>
                        </div>
                    </div>

                </asp:Panel>


                <!-- ==============================================
                     TAB 3 : LEARNING PERFORMANCE (sample data)
                     ============================================== -->
                <asp:Panel ID="pnlLearning" runat="server">

                    <!-- Title -->
                    <h2 style="margin:0;">Learning performance</h2>
                    <p class="sub">Completion and quiz outcomes (sample data until enrolment tables exist)</p>

                    <!-- 4 number boxes -->
                    <div class="stat-row four">

                        <div class="card">
                            <div class="stat-label">Cohort enrolments</div>
                            <div class="stat-number"><asp:Label ID="lblEnrol" runat="server"></asp:Label></div>
                            <div class="stat-note">Across five reporting courses</div>
                        </div>

                        <div class="card">
                            <div class="stat-label">Course completions</div>
                            <div class="stat-number"><asp:Label ID="lblDone" runat="server"></asp:Label></div>
                            <div class="stat-note">Enrolments marked complete</div>
                        </div>

                        <div class="card">
                            <div class="stat-label">Completion rate</div>
                            <div class="stat-number"><asp:Label ID="lblRate" runat="server"></asp:Label></div>
                            <div class="stat-note">Completions ÷ enrolments</div>
                        </div>

                        <div class="card">
                            <div class="stat-label">Average quiz score</div>
                            <div class="stat-number"><asp:Label ID="lblQuiz" runat="server"></asp:Label></div>
                            <div class="stat-note">Mean across five courses</div>
                        </div>

                    </div>

                    <!-- 2 charts side by side -->
                    <div class="two-cols">

                        <div class="card">
                            <h3>Completion rate by course</h3>
                            <p class="sub">Share of learners who finished</p>
                            <asp:Literal ID="litCompletionBars" runat="server"></asp:Literal>
                        </div>

                        <div class="card">
                            <h3>Quiz score by topic</h3>
                            <p class="sub">Average quiz results</p>
                            <asp:Literal ID="litQuizBars" runat="server"></asp:Literal>
                        </div>

                    </div>

                    <!-- Performance table -->
                    <div class="card" style="margin-bottom:18px;">
                        <h3>Performance table</h3>
                        <p class="sub">Course-level learning outcomes</p>

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
