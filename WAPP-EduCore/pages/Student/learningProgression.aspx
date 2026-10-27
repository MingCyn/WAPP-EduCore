<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="learningProgression.aspx.cs" Inherits="WAPP_EduCore.pages.Student.WebForm6" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Lesson</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <link href="/Content/student-upgrade.css" rel="stylesheet" />
    <script src="/Content/fx.js"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />
            <main class="main">
                <div class="topbar">
                    <div class="lv-heading">
                        <span class="title"><asp:Literal ID="litCourseTitle" runat="server" Mode="Encode"></asp:Literal></span>
                        <span class="q-chip" id="chipBox" runat="server">Lesson <asp:Literal ID="litLessonNo" runat="server"></asp:Literal> of <asp:Literal ID="litLessonTotal" runat="server"></asp:Literal></span>
                    </div>
                    <div class="lv-top-right">
                        <span>Course progress: <strong><asp:Literal ID="litPct" runat="server"></asp:Literal>% Complete</strong></span>
                        <asp:HyperLink ID="lnkBackCourses" runat="server" NavigateUrl="~/pages/Student/myCourses.aspx" CssClass="btn-ghost">Back to Courses</asp:HyperLink>
                    </div>
                </div>

                <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="result-box">
                    <h3>This course has no lessons yet.</h3>
                    <p style="margin-top:12px"><a href="myCourses.aspx" class="btn-solid">Back to courses</a></p>
                </asp:Panel>

                <asp:Panel ID="pnlViewer" runat="server">
                    <div class="lv-layout">

                        <!-- left: syllabus -->
                        <div class="lv-side">
                            <h3>Course syllabus</h3>
                            <asp:Repeater ID="rptSyllabus" runat="server">
                                <ItemTemplate>
                                    <a class='<%# ItemCss(Eval("lesson_id"), Eval("done")) %>'
                                       href='<%# LessonHref(Eval("lesson_id")) %>'
                                       title='<%#: Eval("lesson_title") %>'>
                                        <span class="lv-check"></span>
                                        <span class="lv-name">Lesson <%# Container.ItemIndex + 1 %>: <%#: Eval("lesson_title") %></span>
                                    </a>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>

                        <!-- centre: lesson -->
                        <div class="lv-main">
                            <asp:Literal ID="litVideo" runat="server" Mode="PassThrough"></asp:Literal>
                            <h2 class="lv-title"><asp:Literal ID="litLessonTitle" runat="server" Mode="Encode"></asp:Literal></h2>
                            <p class="lv-desc"><asp:Literal ID="litLessonDesc" runat="server" Mode="Encode"></asp:Literal></p>

                            <asp:Panel ID="pnlContent" runat="server" CssClass="lv-content">
                                <asp:Literal ID="litContent" runat="server" Mode="PassThrough"></asp:Literal>
                            </asp:Panel>

                            <div class="lv-actions">
                                <asp:HyperLink ID="lnkPrev" runat="server" CssClass="btn-ghost">Previous Lesson</asp:HyperLink>
                                <asp:Button ID="btnComplete" runat="server" Text="Mark as Complete"
                                    CssClass="btn-solid" OnClick="btnComplete_Click" />
                                <span class="lv-done" id="doneBox" runat="server">&#10003; Lesson completed</span>
                                <asp:HyperLink ID="lnkQuiz" runat="server" CssClass="btn-ghost">Take Quiz</asp:HyperLink>
                                <asp:HyperLink ID="lnkNext" runat="server" CssClass="btn-solid">Next Lesson</asp:HyperLink>
                            </div>
                        </div>

                        <!-- right: tutor + progress -->
                        <div class="lv-right">
                            <div class="tutor-card">
                                <div class="tutor-head">
                                    <span class="tutor-avatar"><asp:Literal ID="litTutorInitial" runat="server" Mode="Encode"></asp:Literal></span>
                                    <div>
                                        <div class="tutor-name"><asp:Literal ID="litTutorName" runat="server" Mode="Encode"></asp:Literal></div>
                                        <div class="tutor-role"><asp:Literal ID="litTutorRole" runat="server" Mode="Encode"></asp:Literal></div>
                                    </div>
                                </div>
                            </div>

                            <asp:HyperLink ID="lnkDiscuss" runat="server" CssClass="btn-ghost" Style="display:block;text-align:center;margin-bottom:22px">Open Lesson Discussion</asp:HyperLink>
                            <h3>Your progress</h3>
                            <div class="lv-bar"><div class="lv-bar-fill" id="barFill" runat="server"></div></div>
                            <small style="color:#6b7385"><asp:Literal ID="litDoneCount" runat="server"></asp:Literal> of <asp:Literal ID="litTotalCount" runat="server"></asp:Literal> lessons completed</small>
                        </div>

                    </div>
                </asp:Panel>
            </main>
        </div>
    </form>
</body>
</html>