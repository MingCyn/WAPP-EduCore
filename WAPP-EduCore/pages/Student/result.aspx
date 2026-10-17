<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="result.aspx.cs" Inherits="WAPP_EduCore.pages.Student.WebForm5" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>My Learning Progress</title>
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
                    <div>
                        <div class="title">My Learning Progress</div>
                        <div class="subtitle">Detailed breakdown of your academic milestones, scores, and activity streak.</div>
                    </div>
                </div>

                <div class="gr-stats">
                    <div class="gr-stat">
                        <div class="gr-label">Courses Completed</div>
                        <div class="gr-num"><asp:Literal ID="litCompleted" runat="server"></asp:Literal></div>
                        <div class="gr-sub">Out of <asp:Literal ID="litEnrolled" runat="server"></asp:Literal> enrolled</div>
                    </div>
                    <div class="gr-stat">
                        <div class="gr-label">Lessons Completed</div>
                        <div class="gr-num"><asp:Literal ID="litLessons" runat="server"></asp:Literal></div>
                        <div class="gr-sub"><asp:Literal ID="litLessonsWeek" runat="server"></asp:Literal> this week</div>
                    </div>
                    <div class="gr-stat">
                        <div class="gr-label">Current Streak</div>
                        <div class="gr-num"><asp:Literal ID="litStreak" runat="server"></asp:Literal> Days &#128293;</div>
                        <div class="gr-sub">Personal best: <asp:Literal ID="litBest" runat="server"></asp:Literal> days</div>
                    </div>
                    <div class="gr-stat">
                        <div class="gr-label">Certificates Earned</div>
                        <div class="gr-num"><asp:Literal ID="litCerts" runat="server"></asp:Literal></div>
                        <div class="gr-sub">Course certificates issued</div>
                    </div>
                </div>

                <div class="gr-row two">
                    <div class="gr-card">
                        <h3>Skill Radar Distribution</h3>
                        <asp:Literal ID="litRadar" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                    <div class="gr-card">
                        <h3>Weekly Learning Activity</h3>
                        <asp:Literal ID="litWeekly" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                </div>

                <div class="gr-row heat">
                    <div class="gr-card">
                        <h3>Daily Learning Intensity (Last 3 Months)</h3>
                        <asp:Literal ID="litHeat" runat="server" Mode="PassThrough"></asp:Literal>
                        <div class="hm-legend">
                            Less <span class="hm hm0"></span><span class="hm hm1"></span><span class="hm hm2"></span><span class="hm hm3"></span><span class="hm hm4"></span> More
                        </div>
                    </div>
                    <div class="gr-card">
                        <h3>Completed Milestones</h3>
                        <asp:Panel ID="pnlNoMilestones" runat="server" Visible="false" CssClass="gr-empty">
                            Finish a course or ace a quiz to earn your first milestone.
                        </asp:Panel>
                        <asp:Repeater ID="rptMilestones" runat="server">
                            <ItemTemplate>
                                <div class="ms-item">
                                    <span class="ms-dot"></span>
                                    <div class="ms-title"><%#: Eval("Title") %></div>
                                    <div class="ms-sub"><%#: Eval("Sub") %></div>
                                    <div class="ms-date"><%#: Eval("DateText") %></div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>