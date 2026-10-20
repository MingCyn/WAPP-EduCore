<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courses.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.courses" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>EduCore - My Courses</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&amp;family=Source+Serif+4:wght@600;700&amp;display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/courses.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">
            <header class="navbar">
                <div class="nav-brand">
                    <a href="tutorDashboard.aspx">
                        <img src="../../images/EduCore_Logo.png" alt="EduCore Logo" class="nav-logo" />
                    </a>
                </div>
                <nav class="nav-links" aria-label="Tutor navigation">
                    <a href="tutorDashboard.aspx" class="nav-link">Dashboard</a>
                    <a href="courses.aspx" class="nav-link active">My Courses</a>
                    <a href="studentManagement.aspx" class="nav-link">Students</a>
                    <a href="assessmentBuilder.aspx" class="nav-link">Assessments</a>
                    <a href="studentPerformance.aspx" class="nav-link">Analytics</a>
                    <a href="#" class="nav-link">Messages</a>
                </nav>
                <div class="nav-profile">
                    <span class="profile-name">Dr. Robert Miller</span>
                    <img src="../../images/tutor-avatar.png" alt="Dr. Robert Miller" class="profile-avatar" onerror="this.style.display='none'" />
                </div>
            </header>

            <main class="dashboard-container">
                <div class="page-header">
                    <div>
                        <h1 class="page-title">My Courses</h1>
                        <p class="page-subtitle">Manage your courses and lessons.</p>
                    </div>
                </div>

                <div class="cl-toolbar">
                    <input id="courseSearch" class="cl-search" type="search" placeholder="Search courses..." aria-label="Search courses" />
                    <asp:Literal ID="LitCount" runat="server" />
                </div>

                <asp:Label ID="LblError" runat="server" CssClass="page-status" Visible="false" />
                <asp:Panel ID="PnlEmpty" runat="server" CssClass="cl-empty" Visible="false">No courses were found.</asp:Panel>

                <div id="courseGrid" class="cl-grid">
                    <asp:Repeater ID="RptCourses" runat="server">
                        <ItemTemplate>
                            <a class="cl-card" href='<%# "courseBuilder.aspx?courseId=" + Eval("CourseId") %>' data-search='<%# Eval("SearchText") %>'>
                                <div class="cl-top">
                                    <span class="cl-topic"><%#: Eval("Topic") %></span>
                                    <span class="cl-id">#<%#: Eval("CourseId") %></span>
                                </div>
                                <h2 class="cl-title"><%#: Eval("Title") %></h2>
                                <div class="cl-stats">
                                    <span><%#: Eval("Duration") %></span>
                                    <span><%#: Eval("LessonsText") %></span>
                                </div>
                                <div class="cl-foot">
                                    <span><%#: Eval("CreatedText") %></span>
                                    <span class="cl-open">Open &rarr;</span>
                                </div>
                            </a>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </main>
        </div>
    </form>

    <script>
        (function () {
            var search = document.querySelector('input#courseSearch');
            var cards = document.querySelectorAll('.cl-card');
            if (!search) return;
            search.addEventListener('input', function () {
                // @ts-ignore Visual Studio types querySelector as Element although the selector targets an input.
                var query = search.value.toLowerCase().trim();
                Array.prototype.forEach.call(cards, function (card) {
                    card.hidden = query && card.getAttribute('data-search').indexOf(query) === -1;
                });
            });
        }());
    </script>
</body>
</html>
