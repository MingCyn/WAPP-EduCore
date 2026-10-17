<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Sidebar.ascx.cs" %>
<div class="sidebar">
    <img src="../../images/logo.png" alt="EduCore" class="logo" />

    <a href="~/pages/Student/studentDashboard.aspx" class="menu-item" runat="server"><img src="../../images/dashboard_icon.png" alt="" class="menu-icon" /><span>Dashboard</span></a>
    <a href="~/pages/Student/myCourses.aspx" class="menu-item" runat="server"><img src="../../images/mycourse_icon.png" alt="" class="menu-icon" /><span>My Courses</span></a>
    <a href="~/pages/Student/calendar.aspx" class="menu-item" runat="server"><img src="../../images/calendar_icon.png" alt="" class="menu-icon" /><span>Calendar</span></a>
    <a href="~/pages/Student/messages.aspx" class="menu-item" runat="server"><img src="../../images/messages_icon.png" alt="" class="menu-icon" /><span>Messages</span></a>
    <a href="~/pages/General/discussion.aspx" class="menu-item" runat="server"><img src="../../images/grades_icon.png" alt="" class="menu-icon" /><span>Grades</span></a>
    <a href="~/pages/Student/certificates.aspx" class="menu-item" runat="server"><img src="../../images/certificates_icon.png" alt="" class="menu-icon" /><span>Certificates</span></a>
    <a href="~/pages/Student/settings.aspx" class="menu-item" runat="server"><img src="../../images/settings_icon.png" alt="" class="menu-icon" /><span>Settings</span></a>

    <div class="me">
        <img src="../../images/profile-placeholder.png" alt="Profile" style="width:36px;height:36px;border-radius:50%;vertical-align:middle;margin-right:8px;" />
        <div style="display:inline-block;vertical-align:middle">
            <div><strong id="lblName" runat="server">Student Name</strong></div>
            <small id="lblId" runat="server">Student ID: 00000</small>
        </div>
    </div>
</div>

<script>
    (function () {
        var current = location.pathname.toLowerCase().split('/').pop();
        document.querySelectorAll('.sidebar .menu-item').forEach(function (a) {
            if (a.getAttribute('href').toLowerCase().split('/').pop() === current) a.classList.add('active');
        });
    })();
</script>