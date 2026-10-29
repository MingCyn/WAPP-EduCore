<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courseAnalysis.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.courseAnalysis" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Course Analytics</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css?v=3" rel="stylesheet" type="text/css" />
    <link href="../../Content/courseBuilder.css?v=4" rel="stylesheet" type="text/css" />
    <link href="../../Content/courseAnalysis.css?v=1" rel="stylesheet" type="text/css" />
</head>
<style>
    .badge.status-on-track,
    .badge.status-attention,
    .badge.status-risk {
        display: inline-block;
        width: auto;
        white-space: nowrap;
        text-align: center;
        font-weight: 700;
    }

    .badge.status-on-track {
        background: #DCFCE7;
        color: #14532D;
    }

    .badge.status-attention {
        background: #FEF3C7;
        color: #78350F;
    }

    .badge.status-risk {
        background: #FEE2E2;
        color: #991B1B;
    }

    .notice.ca-note {
        margin-bottom: 24px;
    }
</style>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">

            <%-- Shared navigation include --%>
            <% Server.Execute("tutorNav.aspx", false); %>

            <main class="dashboard-container">

                <!-- Breadcrumb -->
                <nav class="ca-crumb" aria-label="Breadcrumb">
                    <a href="courses.aspx">My Courses</a> &gt; 
                    <asp:HyperLink ID="LnkCourseDetails" runat="server"><asp:Literal ID="LitCrumbTitle" runat="server" /></asp:HyperLink> &gt; 
                    Analytics
                </nav>

                <!-- Page Header -->
                <div class="ca-head">
                    <div>
                        <h1 class="ca-title">Course Analytics: <asp:Literal ID="LitTitle" runat="server" /></h1>
                        <p class="ca-subtitle">Track student enrollment performance, overall progression, and assessment metrics.</p>
                    </div>
                </div>

                <asp:Label ID="LblStatus" runat="server" CssClass="page-status" Visible="false" />
                <asp:Panel ID="PnlNoAssessment" runat="server" CssClass="notice warning ca-note" Visible="false">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="12" cy="12" r="9" />
                        <path d="M12 8v5" />
                        <circle cx="12" cy="16.5" r=".6" fill="currentColor" />
                    </svg>
                    <span><strong>No analytics yet.</strong> This course doesn't have an assessment, so there are no results to analyse. Add an assessment to a lesson in the Course Builder and the analytics will appear here.</span>
                </asp:Panel>

                <asp:Panel ID="PnlAnalytics" runat="server"> </asp:Panel>


                <!-- Summary Metrics Grid -->
                <div class="ca-metrics-grid">
                    <div class="ca-metric-card">
                        <span class="ca-metric-num"><asp:Literal ID="LitTotalEnrolled" runat="server" Text="0" /></span>
                        <span class="ca-metric-label">Total Enrolled Students</span>
                    </div>
                    <div class="ca-metric-card">
                        <span class="ca-metric-num"><asp:Literal ID="LitAvgProgress" runat="server" Text="0%" /></span>
                        <span class="ca-metric-label">Average Course Progress</span>
                    </div>
                    <div class="ca-metric-card">
                        <span class="ca-metric-num"><asp:Literal ID="LitAvgScore" runat="server" Text="0%" /></span>
                        <span class="ca-metric-label">Average Assessment Score</span>
                    </div>
                </div>

                <!-- Enrolled Students & Progress Table Section -->
                <section class="ca-section">
                    <div class="ca-card-head">
                        <h2 class="ca-section-title">Enrolled Students List & Progress</h2>
                    </div>

                    <asp:Panel ID="PnlStudentList" runat="server">
                        <div class="ca-table-container">
                            <table class="ca-table">
                                <thead>
                                    <tr>
                                        <th>Student Name</th>
                                        <th>Email</th>
                                        <th>Enrolled Date</th>
                                        <th>Progress</th>
                                        <th>Status</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="RptStudents" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td><strong><%#: Eval("FullName") %></strong></td>
                                                <td><%#: Eval("Email") %></td>
                                                <td><%#: Eval("EnrolledAt") %></td>
                                                <td>
                                                    <div class="ca-progress-wrap">
                                                        <div class="ca-progress-bar" style='width: <%#: Eval("ProgressPercent") %>%;'></div>
                                                        <span class="ca-progress-text"><%#: Eval("ProgressPercent") %>%</span>
                                                    </div>
                                                </td>
                                                <td><span class="badge <%#: Eval("StatusClass") %>"><%#: Eval("StatusText") %></span></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="PnlNoStudents" runat="server" CssClass="ca-empty" Visible="false">
                        No students have enrolled in this course yet.
                    </asp:Panel>
                </section>

            </main>
        </div>
    </form>

    <script>
var navLink = document.querySelector('.nav-link[href="courses.aspx"]');
if (navLink) navLink.classList.add('active');
</script>
</body>
</html>