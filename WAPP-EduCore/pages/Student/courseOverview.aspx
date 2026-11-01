<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courseOverview.aspx.cs"
    Inherits="WAPP_EduCore.pages.Student.courseOverview" %>

<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Course Overview</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap"
        rel="stylesheet" />
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
                    <div class="title">
                        <asp:Literal ID="litTopTitle" runat="server" Mode="Encode"></asp:Literal></div>
                </div>

                <asp:Label ID="lblMessage" runat="server" Visible="false"
                    Style="display: block; margin: 0 0 16px; color: #2563eb"></asp:Label>

                <div class="course-hero" id="heroBox" runat="server">
                    <div>
                        <div class="chips">
                            <span class="chip">
                                <asp:Literal ID="litLevel" runat="server" Mode="Encode"></asp:Literal></span>
                        </div>
                        <h2>
                            <asp:Literal ID="litTitle" runat="server" Mode="Encode"></asp:Literal></h2>
                        <div class="hero-meta">
                            <span class="hero-avatar">
                                <asp:Literal ID="litInitial" runat="server" Mode="Encode"></asp:Literal></span>
                            <span>Instructed by
                                <asp:Literal ID="litInstructor" runat="server" Mode="Encode"></asp:Literal></span>
                            <span class="hero-dot">&bull;</span>
                            <span>
                                <asp:Literal ID="litStudents" runat="server" Mode="Encode"></asp:Literal>
                                Enrolled Students</span>
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

                        <%-- Added: Course References Panel --%>
                        <asp:Panel ID="pnlReferences" runat="server" CssClass="panel" Visible="false">
                            <h3>Course References & Textbooks</h3>
                            <ul class="learn-list" style="list-style-type: disc; padding-left: 20px;">
                                <asp:Repeater ID="rptReferences" runat="server">
                                    <ItemTemplate>
                                        <li style="margin-bottom: 8px; font-size: 14px; color: #4a5468; line-height: 1.5;">
                                            <span><%#: Container.DataItem %></span>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </asp:Panel>

                        <h3 class="syllabus-title" id="syllabus">Course Syllabus</h3>

                        <asp:Panel ID="pnlNoLessons" runat="server" Visible="false" CssClass="panel"
                            Style="color: #7b8697">
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
                            <div class="access-title">
                                <asp:Literal ID="litStatus" runat="server"></asp:Literal>
                            </div>
                            <asp:PlaceHolder ID="phEnrolled" runat="server">
                                <asp:HyperLink ID="lnkStart" runat="server" CssClass="btn-start" NavigateUrl="#syllabus">Start Learning</asp:HyperLink>
                            </asp:PlaceHolder>

                            <%-- Shown to guests AND signed-in students who are not enrolled --%>
                            <asp:PlaceHolder ID="PlaceHolder1" runat="server">
                                <asp:Button ID="Button1" runat="server" CssClass="btn-start"
                                    Text="Enroll Now" OnClick="btnEnroll_Click" />
                            </asp:PlaceHolder>

                            <asp:PlaceHolder ID="phGuest" runat="server" Visible="false">
                                <div style="text-align: center; margin-top: 14px; font-size: 13px; color: #6b7385;">
                                    Already have an account?
                                    <a href="../../Auth/login.aspx" style="color: #243f86; font-weight: 600; text-decoration: underline;">Log in</a>
                                </div>
                            </asp:PlaceHolder>
                            <asp:PlaceHolder ID="phNotEnrolled" runat="server">
                                <asp:Button ID="btnEnroll" runat="server" CssClass="btn-start"
                                    Text="Enroll Now" OnClick="btnEnroll_Click" />
                            </asp:PlaceHolder>

                            <ul class="access-facts">
                                <li>Duration:
                                    <asp:Literal ID="litDuration" runat="server" Mode="Encode"></asp:Literal></li>
                                <li>Lessons:
                                    <asp:Literal ID="litLessonCount" runat="server"></asp:Literal></li>
                                <li>Verified Certificate Included</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </main>
        </div>
        <div id="signupModal" class="su-overlay" style="display:none" onclick="if(event.target===this)closeSignupModal()">
    <div class="su-box" role="dialog" aria-modal="true" aria-labelledby="suTitle">
        <h3 id="suTitle">Sign up to enroll</h3>
        <p>You need an account to enroll in this course. Create one for free, or log in if you already have one.</p>
        <div class="su-actions">
            <a class="su-btn su-primary" href="../../Auth/signup.aspx">Sign Up</a>
            <a class="su-btn" href="../../Auth/login.aspx">Log In</a>
        </div>
        <button type="button" class="su-close" onclick="closeSignupModal()">Maybe later</button>
    </div>
</div>

<style>
    .su-overlay { position: fixed; inset: 0; background: rgba(15,26,70,.55); z-index: 9999;
                  align-items: center; justify-content: center; }
    .su-overlay.open { display: flex !important; }
    .su-box { background: #fff; border-radius: 14px; padding: 28px; max-width: 400px; width: 90%;
              text-align: center; box-shadow: 0 20px 50px rgba(0,0,0,.25); }
    .su-box h3 { margin: 0 0 8px; color: #1a2550; }
    .su-box p { margin: 0 0 20px; color: #6b7385; font-size: 14px; line-height: 1.5; }
    .su-actions { display: flex; gap: 10px; justify-content: center; }
    .su-btn { padding: 10px 22px; border-radius: 8px; border: 1px solid #243f86; color: #243f86;
              font-weight: 600; text-decoration: none; font-size: 14px; }
    .su-primary { background: #243f86; color: #fff; }
    .su-close { margin-top: 14px; background: none; border: 0; color: #7b8697; cursor: pointer; font-size: 13px; }
</style>

<script>
function openSignupModal() { document.getElementById('signupModal').classList.add('open'); }
function closeSignupModal() { document.getElementById('signupModal').classList.remove('open'); }
document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeSignupModal(); });
</script>
    </form>
</body>
</html>
