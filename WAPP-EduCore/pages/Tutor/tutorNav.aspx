<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="tutorNav.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.tutorNav" %>

<header class="navbar">
    <div class="nav-brand">
        <img src="~/images/EduCore_Logo.png" alt="EduCore Logo" class="nav-logo" runat="server" />
    </div>
    <nav class="nav-links">
        <a href="tutorDashboard.aspx" class="nav-link">Dashboard</a>
        <a href="courses.aspx" class="nav-link">My Courses</a>
        <a href="studentManagement.aspx" class="nav-link">Students</a>
        <a href="messages.aspx" class="nav-link">Messages</a>
        <a href="settings.aspx" class="nav-link">Account</a>
    </nav>
    <div class="nav-profile">
        <span class="profile-name"><%: DisplayName %></span>
        <img id="ImgAvatar" src="~/images/tutor-avatar.png" alt="Tutor" class="profile-avatar" runat="server" onerror="this.style.display='none'" />
    </div>
</header>
<script>
    (function () {
        var groups = {
            'tutordashboard.aspx': ['tutordashboard.aspx'],
            'courses.aspx': ['courses.aspx', 'coursebuilder.aspx', 'coursedetails.aspx', 'courseanalysis.aspx', 'assessmentbuilder.aspx'],
            'studentmanagement.aspx': ['studentmanagement.aspx', 'studentperformance.aspx'],
            'messages.aspx': ['messages.aspx'],
            'settings.aspx': ['settings.aspx']
        };
        var page = location.pathname.split('/').pop().toLowerCase();
        var target = null;
        Object.keys(groups).forEach(function (href) {
            if (groups[href].indexOf(page) !== -1) target = href;
        });
        document.querySelectorAll('.nav-link').forEach(function (a) {
            var h = (a.getAttribute('href') || '').toLowerCase();
            a.classList.toggle('active', h === target);
        });
    })();
</script>