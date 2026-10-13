<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Sidebar.ascx.cs" %>
<div class="sidebar">
    <img src="../../images/logo.png" alt="EduCore" class="logo" />

    <a href="~/pages/Student/studentDashboard.aspx" class="menu-item" runat="server">Dashboard</a>
    <a href="~/pages/Student/myCourses.aspx" class="menu-item" runat="server">My Courses</a>
    <a href="~/pages/Student/calendar.aspx" class="menu-item" runat="server">Calendar</a>
    <a href="~/pages/Student/messages.aspx" class="menu-item" runat="server">Messages</a>
    <a href="~/pages/General/discussion.aspx" class="menu-item" runat="server">Grades</a>
    <a href="~/pages/Student/certificates.aspx" class="menu-item" runat="server">Certificates</a>
    <a href="~/pages/Student/settings.aspx" class="menu-item" runat="server">Settings</a>

    <div class="me">
        <img src="../../images/profile-placeholder.png" alt="Profile" style="width:36px;height:36px;border-radius:50%;vertical-align:middle;margin-right:8px;" />
        <div style="display:inline-block;vertical-align:middle">
            <div><strong id="lblName" runat="server">Student Name</strong></div>
            <small id="lblId" runat="server">Student ID: 00000</small>
        </div>
    </div>
</div>
