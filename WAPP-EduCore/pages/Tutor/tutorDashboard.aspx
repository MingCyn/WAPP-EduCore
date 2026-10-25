<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="tutorDashboard.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.tutorDashboard" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Tutor Dashboard</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
    <link href="~/Content/dashboard.css" rel="stylesheet" type="text/css" runat="server" />
    <style>
        .dash-status { margin-bottom: 16px; padding: 12px 16px; border-radius: 8px; background: #fdecec; border: 1px solid #f3c0c0; color: #a12626; font-size: 14px; }
        .dash-empty { padding: 20px; border: 1px dashed #cdd2de; border-radius: 12px; background: #faf9f7; color: #6b7285; font-size: 14px; line-height: 1.5; }
        .dash-empty a { color: #2f4fc0; font-weight: 600; text-decoration: none; }
        .dash-note { margin-top: 10px; font-size: 12px; color: #7b8194; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">

            <!-- Injected Reusable Navigation ASPX File -->
            <asp:Panel ID="pnlNavContainer" runat="server"></asp:Panel>

            <!-- Main Dashboard Body -->
            <main class="dashboard-container">

                <%-- Shown only if some figures could not be loaded --%>
                <asp:Label ID="LblStatus" runat="server" CssClass="dash-status" Visible="false" />

                <!-- Top Statistics Overview Cards (all values come from the database) -->
                <div class="stats-overview-grid">
                    <div class="stat-card">
                        <span class="stat-card-title">Total Students</span>
                        <h2 class="stat-card-value"><%: TotalStudentsText %></h2>
                        <span class="stat-trend <%: StudentTrendClass %>"><%: StudentTrendText %></span>
                    </div>
                    <div class="stat-card">
                        <span class="stat-card-title">Active Courses</span>
                        <h2 class="stat-card-value"><%: ActiveCoursesText %></h2>
                        <span class="stat-trend neutral"><%: ActiveCoursesNote %></span>
                    </div>
                    <div class="stat-card">
                        <span class="stat-card-title">Average Score</span>
                        <h2 class="stat-card-value"><%: AverageScoreText %></h2>
                        <span class="stat-trend neutral"><%: AverageScoreNote %></span>
                    </div>
                    <div class="stat-card">
                        <span class="stat-card-title">Completion Rate</span>
                        <h2 class="stat-card-value"><%: CompletionText %></h2>
                        <span class="stat-trend <%: CompletionClass %>"><%: CompletionNote %></span>
                    </div>
                </div>

                <!-- Content Split Grid (Left: Courses & Table, Right: Sidebar) -->
                <div class="content-split-grid">

                    <!-- Left Column -->
                    <div class="left-column">

                        <!-- My Courses Overview: your published courses (progress = completed lessons) -->
                        <section class="section-block">
                            <h3 class="section-heading">My Courses Overview</h3>
                            <div class="courses-grid">
                                <asp:Repeater ID="RptCourses" runat="server">
                                    <ItemTemplate>
                                        <div class="course-mini-card">
                                            <div class="course-card-header">
                                                <h4><%#: Eval("Title") %></h4>
                                                <a href='courseDetails.aspx?courseId=<%# Eval("CourseId") %>' class="action-link">View Details &rarr;</a>
                                            </div>
                                            <div class="course-card-meta">
                                                <span><%#: Eval("StudentsText") %></span>
                                                <span class="progress-text"><%# Eval("Progress") %>%</span>
                                            </div>
                                            <div class="progress-bar-container">
                                                <div class="progress-bar-fill" style="width: <%# Eval("Progress") %>%;"></div>
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                            <asp:Panel ID="PnlNoCourses" runat="server" CssClass="dash-empty" Visible="false">
                                You have no published courses yet. Open <a href="courses.aspx">My Courses</a> to build one and publish it.
                            </asp:Panel>
                        </section>

                        <!-- Active Student Progress: lessons completed (from the lesson_progress table) in your published courses -->
                        <section class="section-block">
                            <h3 class="section-heading">Active Student Progress</h3>
                            <asp:Panel ID="PnlStudentsTable" runat="server" CssClass="table-card">
                                <table class="data-table">
                                    <thead>
                                        <tr>
                                            <th>STUDENT NAME</th>
                                            <th>COURSE</th>
                                            <th>PROGRESS</th>
                                            <th>STATUS</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <asp:Repeater ID="RptStudents" runat="server">
                                            <ItemTemplate>
                                                <tr>
                                                    <td><strong><%#: Eval("Name") %></strong></td>
                                                    <td><%#: Eval("CourseTitle") %></td>
                                                    <td>
                                                        <div class="table-progress-wrapper">
                                                            <div class="progress-bar-container small"><div class="progress-bar-fill" style="width: <%# Eval("Progress") %>%;"></div></div>
                                                            <span><%# Eval("Progress") %>%</span>
                                                        </div>
                                                    </td>
                                                    <td><span class='badge <%# Eval("StatusClass") %>'><%#: Eval("StatusLabel") %></span></td>
                                                </tr>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </tbody>
                                </table>
                            </asp:Panel>
                            <asp:Panel ID="PnlNoStudents" runat="server" CssClass="dash-empty" Visible="false">
                                No student has started a lesson in your published courses yet. Progress will appear here as soon as students start.
                            </asp:Panel>
                        </section>

                    </div>

                    <!-- Right Column: latest quiz results -->
                    <div class="right-column">
                        <section class="section-block">
                            <h3 class="section-heading">Recent Quiz Results</h3>
                            <div class="review-card-list">
                                <asp:Repeater ID="RptRecent" runat="server">
                                    <ItemTemplate>
                                        <div class="review-item-card">
                                            <div class="review-info">
                                                <h4><%#: Eval("Student") %></h4>
                                                <p><%#: Eval("QuizTitle") %></p>
                                                <span class="time-ago"><%#: Eval("When") %></span>
                                            </div>
                                            <span class='badge <%# Eval("ScoreClass") %>'><%#: Eval("ScoreText") %></span>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                                <asp:Panel ID="PnlNoRecent" runat="server" CssClass="dash-empty" Visible="false">
                                    No quiz results yet.
                                </asp:Panel>
                            </div>
                        </section>
                    </div>

                </div>

            </main>
        </div>
    </form>
</body>
</html>
