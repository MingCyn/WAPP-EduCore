<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="tutorNav.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.tutorNav" %>

<header class="navbar">
    <div class="nav-brand">
        <img src="~/images/EduCore_Logo.png" alt="EduCore Logo" class="nav-logo" runat="server" />
    </div>
    <nav class="nav-links">
        <a href="tutorDashboard.aspx" class="nav-link">Dashboard</a>
        <a href="courses.aspx" class="nav-link">My Courses</a>
        <a href="students.aspx" class="nav-link">Students</a>
        <a href="assessments.aspx" class="nav-link">Assessments</a>
        <a href="analytics.aspx" class="nav-link">Analytics</a>
        <a href="messages.aspx" class="nav-link">Messages</a>
    </nav>
    <div class="nav-profile">
        <span class="profile-name"><%: DisplayName %></span>
        <img id="ImgAvatar" src="~/images/tutor-avatar.png" alt="Tutor" class="profile-avatar" runat="server" onerror="this.style.display='none'" />
    </div>
</header>