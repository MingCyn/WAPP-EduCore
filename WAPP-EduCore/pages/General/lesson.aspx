<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="lesson.aspx.cs" Inherits="WAPP_EduCore.pages.General.lesson" %>

<%@ Register Src="~/navDashboard.ascx" TagPrefix="uc" TagName="Nav" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title><asp:Literal ID="litLessonTitle" runat="server" /> | EduCore</title>
    <link rel="stylesheet" href="/Content/navDashboard.css" />
    <link rel="stylesheet" href="/Content/site.css" />
    <link rel="stylesheet" href="/Content/course.css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="app-shell">
            <!-- Sidebar Navigation -->
            <uc:Nav ID="Nav1" runat="server" ActivePage="courses" />

            <div class="main">
                <!-- Top Bar -->
                <header class="topbar">
                    <h1><asp:Literal ID="litCourseTitle" runat="server" /></h1>
                    <div class="topbar-actions" style="display: flex; align-items: center; gap: 16px;">
                        <span style="font-size: 13px; color: var(--muted);">Course progress: <strong style="color: var(--navy);">42% Complete</strong></span>
                        <a href="coursesDirectory.aspx" class="btn" style="background: #fff; color: var(--navy); border: 1px solid var(--line);">Back to Courses</a>
                    </div>
                </header>

                <!-- Main Layout Grid -->
                <div style="display: grid; grid-template-columns: 280px minmax(0, 1fr) 300px; gap: 20px; padding: 24px 28px;">
                    
                    <!-- Left: Course Syllabus / Lesson List -->
                    <aside class="panel" style="align-self: start; padding: 18px;">
                        <h3 style="font-size: 15px; margin-bottom: 14px;">Course Syllabus</h3>
                        <div style="display: flex; flex-direction: column; gap: 6px;">
                            <asp:Repeater ID="rptSyllabus" runat="server">
                                <ItemTemplate>
                                    <a href="lesson.aspx?id=<%# Eval("lesson_id") %>" 
                                       style="display: block; padding: 10px 12px; border-radius: 6px; font-size: 13px; text-decoration: none; 
                                              background: <%# IsCurrentLesson(Eval("lesson_id")) ? "var(--cream)" : "#fff" %>; 
                                              color: <%# IsCurrentLesson(Eval("lesson_id")) ? "var(--navy)" : "var(--ink)" %>;
                                              border: 1px solid <%# IsCurrentLesson(Eval("lesson_id")) ? "var(--cream-line)" : "var(--line)" %>;">
                                        <%# Enc(Eval("lesson_title")) %>
                                    </a>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </aside>

                    <!-- Center: Video & Lesson Content Player -->
                    <main style="display: flex; flex-direction: column; gap: 18px;">
                        <div class="panel" style="padding: 16px; background: var(--navy); color: #fff; border-radius: 10px;">
                            <div style="aspect-ratio: 16/9; background: #000; border-radius: 6px; display: grid; place-items: center; position: relative; overflow: hidden;">
                                <asp:Panel ID="pnlVideoEmbed" runat="server" Visible="false">
                                    <iframe id="iframeVideo" runat="server" style="width:100%; height:100%; border:0;" allowfullscreen></iframe>
                                </asp:Panel>
                                <asp:Panel ID="pnlVideoPlaceholder" runat="server" style="text-align: center; color: #cfd5ee;">
                                    <h3 style="color: #fff; margin-bottom: 6px;"><asp:Literal ID="litPlayerTitle" runat="server" /></h3>
                                    <p style="font-size: 12.5px; color: #98a1c9;">Interactive lesson module ready</p>
                                </asp:Panel>
                            </div>
                        </div>

                        <!-- Tab Content Block -->
                        <div class="panel" style="padding: 20px;">
                            <div style="display: flex; gap: 20px; border-bottom: 1px solid var(--line); padding-bottom: 10px; margin-bottom: 16px; font-weight: 500; font-size: 13.5px;">
                                <span style="color: var(--blue); border-bottom: 2px solid var(--blue); padding-bottom: 10px; margin-bottom: -11px;">Interactive Objects</span>
                                <span style="color: var(--muted); cursor: pointer;">Overview</span>
                                <span style="color: var(--muted); cursor: pointer;">Transcript</span>
                            </div>
                            <div style="font-size: 13.5px; line-height: 1.6; color: var(--ink);">
                                <asp:Literal ID="litLessonContent" runat="server" />
                            </div>
                        </div>
                    </main>

                    <!-- Right Column: Tutor Info & Notes -->
                    <aside style="display: flex; flex-direction: column; gap: 18px;">
                        <div class="panel" style="text-align: center; padding: 20px;">
                            <div style="width: 50px; height: 50px; border-radius: 50%; background: var(--blue); color: #fff; display: grid; place-items: center; font-weight: 600; margin: 0 auto 10px; font-size: 18px;">
                                <asp:Literal ID="litTutorInitial" runat="server" Text="T" />
                            </div>
                            <h4 style="font-family: var(--serif); font-size: 16px; margin: 0 0 2px;"><asp:Literal ID="litTutorName" runat="server" Text="EduCore Instructor" /></h4>
                            <p style="font-size: 11.5px; color: var(--muted); margin: 0 0 14px;">Tutor - Science Faculty</p>
                            <a href="messages.aspx" class="btn btn-block" style="background: var(--navy);">Ask Tutor</a>
                        </div>

                        <div class="panel" style="padding: 18px;">
                            <h3 style="font-size: 15px; margin-bottom: 10px;">My Saved Notes</h3>
                            <p style="font-size: 12px; color: var(--muted); line-height: 1.4;">Notes saved during lessons will appear here for easy exam review.</p>
                        </div>
                    </aside>

                </div>
            </div>
        </div>
    </form>
</body>
</html>