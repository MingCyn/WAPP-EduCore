<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courseOverview.aspx.cs" Inherits="WAPP_EduCore.pages.Student.courseOverview" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Course Overview</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />
            <main class="main">
                <div class="topbar">
                    <div class="title"><asp:Literal ID="litTopTitle" runat="server" Mode="Encode"></asp:Literal></div>
                </div>

                <asp:Label ID="lblMessage" runat="server" Visible="false"
                    Style="display:block;margin:0 0 16px;color:#2563eb"></asp:Label>

                <div class="course-hero" id="heroBox" runat="server">
                    <div>
                        <div class="chips">
                            <span class="chip"><asp:Literal ID="litLevel" runat="server" Mode="Encode"></asp:Literal></span>
                        </div>
                        <h2><asp:Literal ID="litTitle" runat="server" Mode="Encode"></asp:Literal></h2>
                        <div class="hero-meta">
                            <span class="hero-avatar"><asp:Literal ID="litInitial" runat="server" Mode="Encode"></asp:Literal></span>
                            <span>Instructed by <asp:Literal ID="litInstructor" runat="server" Mode="Encode"></asp:Literal></span>
                            <span class="hero-dot">&bull;</span>
                            <span><asp:Literal ID="litStudents" runat="server" Mode="Encode"></asp:Literal> Enrolled Students</span>
                        </div>
                    </div>
                </div>

                <div class="detail-layout">
                    <div>
                        <asp:Panel ID="pnlLearn" runat="server" CssClass="panel">
                            <h3>What You'll Learn</h3>
                            <ul class="learn-list">
                                <asp:Repeater ID="rptLearn" runat="server">
                                    <ItemTemplate>
                                        <li><span><%#: Container.DataItem %></span></li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </asp:Panel>

                        <h3 class="syllabus-title" id="syllabus">Course Syllabus</h3>

                        <asp:Panel ID="pnlNoLessons" runat="server" Visible="false" CssClass="panel"
                            Style="color:#7b8697">
                            No lessons have been added to this course yet.
                        </asp:Panel>

                        <asp:Repeater ID="rptLessons" runat="server">
                            <ItemTemplate>
                                <div class="lesson-row">
                                    <span class="lesson-no">LESSON <%# Container.ItemIndex + 1 %></span>
                                    <div>
                                        <span class="lesson-name"><%#: Eval("lesson_title") %></span>
                                        <span class="lesson-desc"><%#: Eval("lesson_description") %></span>
                                    </div>
                                    <span class="lesson-done"><%# Convert.ToBoolean(Eval("done")) ? "&#10003;" : "" %></span>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <div>
                        <div class="access-card">
                            <div class="access-label">Your Access Status</div>
                            <div class="access-title"><asp:Literal ID="litStatus" runat="server"></asp:Literal></div>

                            <asp:PlaceHolder ID="phEnrolled" runat="server">
                                <asp:HyperLink ID="lnkStart" runat="server" CssClass="btn-start" NavigateUrl="#syllabus">Start Learning</asp:HyperLink>
    
                                <asp:PlaceHolder ID="phGuest" runat="server" Visible="false">
                                    <div style="text-align: center; margin-top: 14px; font-size: 13px; color: #6b7385;">
                                        Already have an account? <a href="../../Auth/login.aspx" style="color: #243f86; font-weight: 600; text-decoration: underline;">Log in to enroll</a>
                                    </div>
                                </asp:PlaceHolder>
                            </asp:PlaceHolder>
                            <asp:PlaceHolder ID="phNotEnrolled" runat="server">
                                <asp:Button ID="btnEnroll" runat="server" CssClass="btn-start"
                                    Text="Enroll Now" OnClick="btnEnroll_Click" />
                            </asp:PlaceHolder>

                            <ul class="access-facts">
                                <li>Duration: <asp:Literal ID="litDuration" runat="server" Mode="Encode"></asp:Literal></li>
                                <li>Lessons: <asp:Literal ID="litLessonCount" runat="server"></asp:Literal></li>
                                <li>Verified Certificate Included</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>