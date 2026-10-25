<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="studentDashboard.aspx.cs" Inherits="WAPP_EduCore.pages.Student.WebForm2" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Student Dashboard</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <style>
        a.course-card{display:block;color:inherit;text-decoration:none}
        a.course-card:hover{box-shadow:0 8px 22px rgba(16,24,40,0.12)}
        .courses{flex-wrap:wrap}
        .db-empty{padding:26px 10px;font-size:13px;color:#7b8697}

        /* ---------- fonts: same serif headings as the other student pages ---------- */
        .topbar .greeting{font-family:'Merriweather',Georgia,serif;font-size:26px;font-weight:700;color:#111c3d}
        .hero h2{font-family:'Merriweather',Georgia,serif;font-size:28px;font-weight:700;line-height:1.3}
        .section-title{font-family:'Merriweather',Georgia,serif;font-size:19px;font-weight:700;color:#111c3d}
        .section-title a{font-family:'Inter','Segoe UI',Arial,sans-serif;font-size:13px;font-weight:500}
        .course-card h4{font-family:'Merriweather',Georgia,serif;font-size:16px;font-weight:700;line-height:1.3;color:#111c3d}
        .right-col .card h4{font-family:'Merriweather',Georgia,serif;font-size:17px;font-weight:700;color:#111c3d}
        .stat .num{font-family:'Merriweather',Georgia,serif;font-size:24px;font-weight:700;color:#243f86}
        .deadline-item strong,.achievements strong{font-weight:600;color:#111c3d}
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />
            <main class="main">
                <div class="topbar">
                    <div class="greeting"><asp:Literal ID="litGreeting" runat="server" Mode="Encode"></asp:Literal></div>
                </div>

                <div class="hero">
                    <div style="flex:1">
                        <h2>Learn your way, on your schedule.</h2>
                        <p><asp:Literal ID="litHeroText" runat="server" Mode="Encode"></asp:Literal></p>
                        <asp:HyperLink ID="lnkContinue" runat="server" CssClass="btn"
                            Style="display:inline-block;text-decoration:none" NavigateUrl="~/pages/Student/myCourses.aspx">Continue Learning</asp:HyperLink>
                    </div>
                    <div class="thought">
                        <strong>TODAY'S THOUGHT</strong>
                        <p style="margin-top:8px;font-style:italic;color:#2b2b2b">"The beautiful thing about learning is that no one can take it away from you."</p>
                        <small>&mdash; B.B. King</small>
                    </div>
                </div>

                <div class="content">
                    <div class="left-col">
                        <div>
                            <div class="section-title">
                                <div>My Active Courses</div>
                                <a href="myCourses.aspx" style="font-size:13px;color:#3561b3">View all courses</a>
                            </div>

                            <asp:Panel ID="pnlNoCourses" runat="server" Visible="false" CssClass="db-empty">
                                <asp:Literal ID="litNoCourses" runat="server" Mode="Encode"></asp:Literal>
                            </asp:Panel>

                            <div class="courses">
                                <asp:Repeater ID="rptCourses" runat="server">
                                    <ItemTemplate>
                                        <a class="course-card" href='<%# Eval("Url") %>'>
                                            <div class="thumb" style="<%# Eval("ThumbStyle") %>"></div>
                                            <div class="meta">
                                                <h4><%#: Eval("Title") %></h4>
                                                <small>Tutor: <%#: Eval("Tutor") %></small>
                                                <div class="progress"><span style='width:<%# Eval("Percent") %>%'></span></div>
                                                <small><%# Eval("Percent") %>% complete</small>
                                            </div>
                                        </a>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>

                        <div class="stats">
                            <div class="stat">
                                <small>Lessons This Week</small>
                                <div class="num"><asp:Literal ID="litWeek" runat="server"></asp:Literal></div>
                                <small style="color:#9aa6b8"><asp:Literal ID="litWeekNote" runat="server" Mode="Encode"></asp:Literal></small>
                            </div>
                            <div class="stat">
                                <small>Current Streak</small>
                                <div class="num"><asp:Literal ID="litStreak" runat="server"></asp:Literal></div>
                                <small style="color:#9aa6b8"><asp:Literal ID="litStreakNote" runat="server" Mode="Encode"></asp:Literal></small>
                            </div>
                            <div class="stat">
                                <small>Active Courses</small>
                                <div class="num"><asp:Literal ID="litActive" runat="server"></asp:Literal></div>
                                <small style="color:#9aa6b8"><asp:Literal ID="litActiveNote" runat="server" Mode="Encode"></asp:Literal></small>
                            </div>
                        </div>
                    </div>

                    <div class="right-col">
                        <div class="card">
                            <h4 style="margin-bottom:8px">Upcoming Deadlines</h4>
                            <asp:Panel ID="pnlNoDeadlines" runat="server" Visible="false" CssClass="db-empty">
                                No upcoming deadlines. Add one in your calendar.
                            </asp:Panel>
                            <div class="deadline">
                                <asp:Repeater ID="rptDeadlines" runat="server">
                                    <ItemTemplate>
                                        <div class="deadline-item">
                                            <div>
                                                <strong><%#: Eval("Title") %></strong>
                                                <div style="color:#7b8697;font-size:13px"><%#: Eval("Sub") %></div>
                                            </div>
                                            <div class="badge"><%#: Eval("Badge") %></div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>

                        <div class="card" style="margin-top:16px">
                            <h4 style="margin-bottom:8px">Recent Achievements</h4>
                            <asp:Panel ID="pnlNoAch" runat="server" Visible="false" CssClass="db-empty">
                                Finish a course or ace a quiz to earn your first achievement.
                            </asp:Panel>
                            <ul class="achievements" style="list-style:none">
                                <asp:Repeater ID="rptAch" runat="server">
                                    <ItemTemplate>
                                        <li>
                                            <div style="width:36px;height:36px;border-radius:8px;background:<%# Eval("Tint") %>"></div>
                                            <div>
                                                <strong><%#: Eval("Title") %></strong>
                                                <div style="color:#7b8697;font-size:13px"><%#: Eval("Sub") %></div>
                                            </div>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>