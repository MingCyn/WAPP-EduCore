<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courseOverview.aspx.cs" Inherits="WAPP_EduCore.pages.General.courseOverview" %>
<%@ Register Src="~/navDashboard.ascx" TagPrefix="uc" TagName="Nav" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title><asp:Literal ID="litTitle" runat="server" Text="Course" /> | EduCore</title>
<link runat="server" href="~/Content/course.css" rel="stylesheet" />

</head>
<body>
    <div class="app-shell">

        <uc:Nav ID="Nav1" runat="server" ActivePage="courses" />

        <div class="main">
            <header class="topbar">
                <h1><asp:Literal ID="litHeading" runat="server" Text="Course" /></h1>

                <form class="search" method="get" action="coursesDirectory.aspx" role="search">
                    <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7"/><path d="M20 20l-3.5-3.5"/></svg>
                    <input type="search" name="q" placeholder="Search courses, lessons, resources..." aria-label="Search courses" />
                </form>
            </header>

            <main class="content">
                <a class="back" href="coursesDirectory.aspx">&larr; Back to all courses</a>

                <%-- Course not found --%>
                <asp:Panel ID="pnlMissing" runat="server" Visible="false" CssClass="panel">
                    <h3>Course not found</h3>
                    <p>This course may have been removed. <a class="back" href="coursesDirectory.aspx">Browse available courses</a></p>
                </asp:Panel>

                <asp:Panel ID="pnlCourse" runat="server">

                    <section class="hero" id="hero" runat="server">
                        <div class="badges">
                            <span class="badge"><asp:Literal ID="litLevelBadge" runat="server" /></span>
                            <span class="badge gold"><asp:Literal ID="litCategoryBadge" runat="server" /></span>
                        </div>
                        <h2><asp:Literal ID="litCourseTitle" runat="server" /></h2>
                        <div class="hero-meta">
                            <span>Instructed by <asp:Literal ID="litInstructor" runat="server" /></span>
                            <span class="dot"></span>
                            <span><asp:Literal ID="litLessonCount" runat="server" /></span>
                        </div>
                    </section>

                    <div class="overview-grid">
                        <div>
                            <asp:Panel ID="pnlAbout" runat="server" CssClass="panel">
                                <h3>About this course</h3>
                                <p><asp:Literal ID="litDescription" runat="server" /></p>
                            </asp:Panel>

                            <asp:Panel ID="pnlLearn" runat="server" CssClass="panel">
                                <h3>What You'll Learn</h3>
                                <ul class="learn">
                                    <asp:Repeater ID="rptLearn" runat="server">
                                        <ItemTemplate>
                                            <li>
                                                <svg viewBox="0 0 24 24"><path d="M4 12l5 5L20 6"/></svg>
                                                <span><%# Enc(Container.DataItem) %></span>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </ul>
                            </asp:Panel>

                            <h2 class="section-title">Course Syllabus</h2>

                            <asp:Repeater ID="rptLessons" runat="server">
                                <ItemTemplate>
                                    <details class="lesson">
                                        <summary>
                                            <span class="lesson-no">LESSON <%# Container.ItemIndex + 1 %></span>
                                            <span class="lesson-title"><%# Enc(Eval("lesson_title")) %></span>
                                            <svg class="chev" viewBox="0 0 24 24"><path d="M6 9l6 6 6-6"/></svg>
                                        </summary>
                                        <div class="lesson-body">
                                            <%# Enc(Eval("lesson_description")) %>
                                            <div class="lesson-flags">
                                                <%# HasValue(Eval("video_url")) ? "<span class=\"flag\">Video</span>" : "" %>
                                                <%# HasQuiz(Container.DataItem) ? "<span class=\"flag\">Quiz</span>" : "" %>
                                            </div>
                                        </div>
                                    </details>
                                </ItemTemplate>
                            </asp:Repeater>

                            <asp:Panel ID="pnlNoLessons" runat="server" CssClass="panel" Visible="false">
                                <p>The syllabus for this course hasn't been published yet.</p>
                            </asp:Panel>
                        </div>

                        <aside class="access" aria-label="Enrollment">
                            <div class="eyebrow">Your access status</div>
                            <h3>You are Enrolled</h3>

                            <a class="btn btn-block" href="<%= StartLessonUrl %>">Start Learning</a>
                            <p class="alt">No account? <a href="<%= ResolveUrl("/Auth/signup.aspx") %>">Create one</a></p>

                            <ul>
                                <li>
                                    <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>
                                    Duration: <asp:Literal ID="litDuration" runat="server" />
                                </li>
                                <li>
                                    <svg viewBox="0 0 24 24"><path d="M2 5h7a3 3 0 013 3v12a2 2 0 00-2-2H2z"/><path d="M22 5h-7a3 3 0 00-3 3v12a2 2 0 012-2h8z"/></svg>
                                    Lessons: <asp:Literal ID="litLessonsSide" runat="server" />
                                </li>
                                <li>
                                    <svg viewBox="0 0 24 24"><path d="M9 11l3 3 8-8"/><path d="M20 12v7a2 2 0 01-2 2H6a2 2 0 01-2-2V6a2 2 0 012-2h9"/></svg>
                                    Quizzes: <asp:Literal ID="litQuizzes" runat="server" />
                                </li>
                            </ul>
                        </aside>
                    </div>
                </asp:Panel>
            </main>
        </div>
    </div>
</body>
</html>
