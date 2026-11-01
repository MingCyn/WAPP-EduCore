<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="adminNav.aspx.cs" Inherits="WAPP_EduCore.pages.Admin.adminNav" %>

<div class="sidebar">
    <img src="../../images/logo.png" alt="EduCore" class="logo" />

    <a class="menu-item" href="adminDashboard.aspx">
        <svg class="menu-icon ic-dash" viewBox="0 0 24 24" aria-hidden="true">
            <rect x="3" y="3" width="7" height="7" rx="1.6" />
            <rect x="14" y="3" width="7" height="7" rx="1.6" />
            <rect x="14" y="14" width="7" height="7" rx="1.6" />
            <rect x="3" y="14" width="7" height="7" rx="1.6" />
        </svg><span>Dashboard</span></a>

    <a class="menu-item" href="userManagement.aspx">
        <svg class="menu-icon ic-hop" viewBox="0 0 24 24" aria-hidden="true">
            <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
            <circle cx="9" cy="7" r="4" />
            <path d="M23 21v-2a4 4 0 00-3-3.87" />
            <path d="M16 3.13a4 4 0 010 7.75" />
        </svg><span>Users</span></a>

    <a class="menu-item" href="coursesManagement.aspx">
        <svg class="menu-icon ic-book" viewBox="0 0 24 24" aria-hidden="true">
            <path class="ic-book-l" d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z" />
            <path class="ic-book-r" d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z" />
        </svg><span>Courses</span></a>

    <a class="menu-item" href="lessonManagement.aspx">
        <svg class="menu-icon ic-hop" viewBox="0 0 24 24" aria-hidden="true">
            <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" />
            <polyline points="14 2 14 8 20 8" />
            <line x1="16" y1="13" x2="8" y2="13" />
            <line x1="16" y1="17" x2="8" y2="17" />
        </svg><span>Lessons</span></a>

    <a class="menu-item" href="quizManagement.aspx">
        <svg class="menu-icon ic-hop" viewBox="0 0 24 24" aria-hidden="true">
            <circle cx="12" cy="12" r="10" />
            <path d="M9.09 9a3 3 0 015.83 1c0 2-3 3-3 3" />
            <line x1="12" y1="17" x2="12.01" y2="17" />
        </svg><span>Quizzes</span></a>

    <a class="menu-item" href="adminReports.aspx">
        <svg class="menu-icon ic-bars" viewBox="0 0 24 24" aria-hidden="true">
            <line x1="18" y1="20" x2="18" y2="10" stroke-width="3" />
            <line x1="12" y1="20" x2="12" y2="4" stroke-width="3" />
            <line x1="6" y1="20" x2="6" y2="14" stroke-width="3" />
        </svg><span>Reports</span></a>

    <a class="menu-item" href="powerBiDashboard.aspx">
        <svg class="menu-icon ic-hop" viewBox="0 0 24 24" aria-hidden="true">
            <path d="M21 21H3V3" />
            <path d="M18 9l-5 5-3-3-4 4" />
        </svg><span>Analytics (BI)</span></a>

    <!-- LOGGED IN ADMIN + LOGOUT (bottom of the sidebar) -->
    <div class="me">
        <span class="me-avatar"><%= AdminInitials %></span>
        <div class="me-text">
            <strong><%= AdminName %></strong>
            <small>Platform administrator</small>
        </div>
    </div>
    <a class="logout-btn" href="?logout=1">Log out</a>
</div>

<script>
(function () {
    var page = location.pathname.split('/').pop().toLowerCase();
    document.querySelectorAll('.sidebar .menu-item').forEach(function (a) {
        var h = (a.getAttribute('href') || '').toLowerCase();
        a.classList.toggle('active', h === page);
    });
})();
</script>
