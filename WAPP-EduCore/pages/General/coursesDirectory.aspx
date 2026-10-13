<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="coursesDirectory.aspx.cs" Inherits="WAPP_EduCore.pages.General.coursesDirectory" %>
<%@ Register Src="~/navDashboard.ascx" TagPrefix="uc" TagName="Nav" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Explore Courses | EduCore</title>
    <link runat="server" rel="stylesheet" href="~/Content/course.css" />
</head>
<body>
    <%-- No <form runat="server"> needed: filters and search are plain GET forms, so every URL can be shared. --%>
    <div class="app-shell">

        <uc:Nav ID="Nav1" runat="server" ActivePage="courses" />

        <div class="main">
            <header class="topbar">
                <h1>Explore Courses</h1>

                <form class="search" method="get" action="coursesDirectory.aspx" role="search">
                    <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7"/><path d="M20 20l-3.5-3.5"/></svg>
                    <input type="search" name="q" value="<%= Server.HtmlEncode(Query) %>" placeholder="Search courses, lessons, resources..." aria-label="Search courses" />
                    <input type="hidden" name="level" value="<%= Server.HtmlEncode(Level) %>" />
                    <input type="hidden" name="topic" value="<%= Server.HtmlEncode(Topic) %>" />
                    <input type="hidden" name="sort" value="<%= Server.HtmlEncode(Sort) %>" />
                </form>
            </header>

            <main class="content">
                <nav class="chips" aria-label="Course level">
                    <asp:Literal ID="litChips" runat="server" />
                </nav>

                <form class="filters" method="get" action="coursesDirectory.aspx">
                    <input type="hidden" name="q" value="<%= Server.HtmlEncode(Query) %>" />
                    <input type="hidden" name="level" value="<%= Server.HtmlEncode(Level) %>" />

                    <select name="topic" aria-label="Topic" onchange="this.form.submit()">
                        <asp:Literal ID="litTopics" runat="server" />
                    </select>

                    <select name="sort" aria-label="Sort by" onchange="this.form.submit()">
                        <asp:Literal ID="litSort" runat="server" />
                    </select>
                    <noscript><button class="btn" type="submit">Apply</button></noscript>
                </form>

                <section class="grid" aria-label="Courses">
                    <asp:Repeater ID="rptCourses" runat="server">
                        <ItemTemplate>
                            <article class="card">
                                <a class="card-cover" href="courseOverview.aspx?id=<%# Eval("course_id") %>"
                                   style="<%# CoverStyle(Eval("course_img")) %>"
                                   aria-label="<%# Enc(Eval("title")) %>"></a>
                                <div class="card-body">
                                    <div class="card-top">
                                        <span class="tag"><%# Enc(Eval("course_level")) %></span>
                                        <span class="card-meta"><%# Enc(Eval("category")) %></span>
                                    </div>
                                    <h3><%# Enc(Eval("title")) %></h3>
                                    <p class="by"><%# Enc(Eval("instructor")) %></p>
                                    <div class="card-foot">
                                        <span><%# LessonText(Eval("lesson_count")) %><%# DurationText(Eval("duration")) %></span>
                                        <a class="btn" href="courseOverview.aspx?id=<%# Eval("course_id") %>">View Course</a>
                                    </div>
                                </div>
                            </article>
                        </ItemTemplate>
                    </asp:Repeater>

                    <asp:Panel ID="pnlEmpty" runat="server" CssClass="empty" Visible="false">
                        No courses match your filters.
                        <a href="coursesDirectory.aspx">Clear filters</a>
                    </asp:Panel>
                </section>
            </main>
        </div>
    </div>
</body>
</html>
